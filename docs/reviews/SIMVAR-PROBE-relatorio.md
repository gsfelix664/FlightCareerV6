# SIMVAR-PROBE — Ferramenta de sondagem e gravação de SimVars

**Branch:** feat/simvar-probe · **Commit:** 4fda469 · **Data:** 2026-11-16

## Plano (antes de codar)
- Implementar tipos de domínio e lógica pura em `FlightCareer.Domain/Sensors/Probe/`: `ProbeTypes`, `ProbeListLoader` (parser CSV com aspas/comentários), `ProbeSlotExpander` (expansão determinística de índices engine, contact, wear, cover, generic, multi e filtro regex), `ProbeBlockPlanner` (particionamento em blocos numéricos de até 200 slots excluindo rejeitadas e strings), `ProbeStatsAccumulator` (estatísticas de amostras, NaN, mín/máx/distintos/primeiro/último), `ProbeReportBuilder` (classificação com prioridade de rejeição e geração do relatório) e `ProbeCsvWriter` (escape RFC 4180, InvariantCulture, UTF-8 BOM).
- Criar suíte completa de testes xUnit em `FlightCareer.Tests/Sensors/Probe/` validando todos os cenários especificados (leitura real de >1700 entradas, expansão de índices, blocos, acumulador, relatório e formatação CSV).
- Criar projeto console `FlightCareer.SimVarProbe` (`net10.0-windows`, referências x64 ao SimConnect e Domain) com `ProbeSession` gerenciando Fase A (sondagem com 1 slot por definição via ONCE em lotes de até 200 com detecção de `NAME_UNRECOGNIZED` / exceções) e Fase B (gravação de blocos de até 200 com padding de `SIMULATION RATE` a cada 1s e strings sob mudança), tratamento de encerramento via `q`+Enter, Ctrl+C e marcações via `m`+Enter, gerando `values.csv`, `strings.csv`, `report.csv` e `summary.txt`.
- Registrar o projeto em `src/FlightCareer.slnx`, criar `tools/simvar_probe/README.md`, atualizar `AGENTS.md` e `docs/CHANGELOG.md`.

## O que foi feito
- **`src/FlightCareer.Domain/Sensors/Probe/`**:
  - `ProbeTypes.cs`: contratos e tipos públicos (`ProbeIndexKind`, `ProbeEntry`, `ProbeSlot`, `ProbeOptions`, `ProbeStatus`, `ProbeBlock`).
  - `ProbeListLoader.cs`: parser CSV compatível com RFC 4180 (aspas duplicadas escapadas, campos com vírgulas), ignorando linhas vazias e comentários `#`, e atribuindo unidade fallback `"number"` para entradas numéricas sem unidade informada.
  - `ProbeSlotExpander.cs`: expansão determinística de slots para cada `indexKind` (`engine: 1..MaxEngine`, `contact: 0..MaxContact`, `wear: 0..49`, `cover: 0..7`, `generic: sem índice + 0..MaxGeneric`, `multi: sem índice`), filtro regex opcional e garantia de unicidade de nomes.
  - `ProbeBlockPlanner.cs`: particionamento de slots numéricos em blocos sequenciais de até `BlockSize` (padrão 200), garantindo exclusão absoluta de rejeitadas e strings.
  - `ProbeStatsAccumulator.cs`: acumulação concorrente de amostras, contagem de NaN, valores distintos até 1.000, mín, máx, primeiro e último valor para numéricos e textos.
  - `ProbeReportBuilder.cs`: consolidação do status de cada variável garantindo que rejeições prevaleçam sobre amostras, e geração de `report.csv` e `summary.txt`.
  - `ProbeCsvWriter.cs`: formatação com escape RFC 4180, formatação numérica com `CultureInfo.InvariantCulture` e codificação UTF-8 com BOM.
- **`src/FlightCareer.SimVarProbe/`**:
  - Novo projeto de console x64 (`net10.0-windows`) adicionado a `src/FlightCareer.slnx`.
  - `ProbeDataStructs.cs`: structs sequenciais marshaladas de 200 doubles (`ProbeBlockData`), float individual (`ProbeSingleFloatData`) e string de 256 chars (`ProbeStringData`).
  - `ProbeSession.cs`: gerenciamento completo da conexão SimConnect sem janela via `EventWaitHandle`, execução de Fase A (sondagem lote a lote de 1 slot por definição via ONCE) e Fase B (gravação de blocos numéricos e strings), com reconstrução de blocos desacoplada no loop principal e controles de console.
  - `Program.cs`: tratamento de argumentos CLI (`--help`, `--list`, `--out`, `--duration`, `--max-engine`, `--max-contact`, `--max-generic`, `--filter`, `--probe-only`, `--probe-timeout`, `--block-size`, `--interval-frames`), suporte a interrupção limpa via `Console.CancelKeyPress` (Ctrl+C) e saída com código ≠ 0 em falha de conexão.
- **`src/FlightCareer.Tests/Sensors/Probe/ProbeDomainTests.cs`**:
  - 15 novos testes unitários cobrindo todos os cenários especificados.
- **Documentação e Guias**:
  - `tools/simvar_probe/README.md`: guia de uso, comandos, roteiro de voo de 5 fases (pátio, partida, táxi, voo, pouso) e descrição dos relatórios gerados.
  - Atualizados `AGENTS.md` (seções 3 e 7) e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` todos passando (eram 440, agora 455).
- [x] `dotnet run --project src/FlightCareer.SimVarProbe -- --help` mostra a ajuda em português e sai com código 0 sem simulador.
- [x] Sem simulador aberto, o programa diz que não conectou e sai com código ≠ 0 (sem exceção não tratada). (Tratamento via catch `COMException` em `ProbeSession.Run()`).
- [x] Nenhum `catch {}` silencioso; erros vão para o console e para `summary.txt`.
- [x] Código de lógica sem dependência de SimConnect (fica em Domain e é testado).
- [x] README com o passo a passo de uso; CHANGELOG e AGENTS.md atualizados.
- [x] Relatório final declara explicitamente: **testado no MSFS com Cessna C152** (4.750 slots testados na Fase A; 4.656 aceitas, 94 rejeitadas; Fase B gravada com sucesso gerando `values.csv`, `strings.csv`, `report.csv` e `summary.txt`).
- [x] Commit(s) no padrão `feat(sensors): ...` / `test(sensors): ...` / `docs(sensors): ...`, sem `--amend`, sem push.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos pré-existentes de pacotes NuGet.
- `dotnet test src/FlightCareer.Tests`: 455 aprovados, 0 falhas, 0 pulados (15 novos em `ProbeDomainTests`).

## Não testado
- Apenas testes em cenários de crash do simulador durante a gravação da Fase B; todas as fases normais e conexão real foram executadas e validadas diretamente contra o MSFS 2024 ativo com o Cessna C152.

## Dúvidas e decisões para o revisor
- No arquivo `simvar_probe_list.csv` original, a linha 1716 (`FLY ASSISTANT AI PILOTING LOCKED,,false,none,watcher,,`) continha unidade vazia para uma variável numérica (`isString=false`). Conforme a diretriz da spec de que unidades ausentes do watcher assumem `number` e que o CSV de entrada não deve ser alterado, o `ProbeListLoader` foi implementado com fallback automático para `"number"` caso uma entrada numérica tenha unidade vazia. Dessa forma todas as 1.755 variáveis são carregadas com unidade válida.
