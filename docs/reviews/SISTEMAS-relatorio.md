# SISTEMAS — instruções sob demanda (AGENTS.md ≤ 15 KB)

**Branch:** docs/sistemas-sob-demanda · **Commit:** (a preencher após commit) · **Data:** 2026-11-25

## Plano (antes de codar)
- Mapear blocos da § 7 e decisões técnicas da § 6.
- Extrair para `docs/sistemas/` os 13 arquivos temáticos e o `README.md` com conteúdo copiado literalmente.
- Mover regras completas de quota (§§ 4.9 e 4.10) literalmente para `docs/agents/ORQUESTRACAO.md`.
- Condensar `AGENTS.md` (regras e armadilhas enxutas, comandos de quota na § 3, tabela índice na § 7) garantindo tamanho ≤ 15.000 bytes.
- Atualizar referências cruzadas nos arquivos de agentes.
- Executar validação de cobertura via script (`comm -23`) e conferir medição de bytes.

## O que foi feito
- `docs/sistemas/`: criados 13 arquivos temáticos (`desgaste-falhas.md`, `pecas.md`, `manutencao-hangar.md`, `panes-persistentes.md`, `voo-integridade.md`, `combustivel.md`, `economia-dificuldade.md`, `xp-fases.md`, `missoes-quadro.md`, `aeroportos-tiers.md`, `limites-aeronave.md`, `sensores.md`, `hud.md`) e `README.md`.
- `docs/agents/ORQUESTRACAO.md`: adicionada seção `## Sessões e economia de quota (vindo do AGENTS.md › 4.9 e 4.10)` preservando o texto literal dos itens 9 e 10.
- `AGENTS.md`: condensado de 39.707 bytes para 14.937 bytes; comandos de saída filtrada integrados na § 3; itens 9 e 10 da § 4 resumidos; armadilhas técnicas da § 6 condensadas sem perda de regras, apontando para `docs/sistemas/`; § 7 convertida em tabela índice com 13 sistemas.
- `CLAUDE.md`, `gemini.md`, `.antigravity/rules.md`, `docs/agents/MODELO-SPEC.md`, `tools/agents/prompt-executor.md` e `docs/plans/INSTRUCOES-GEMINI.md`: referências cruzadas atualizadas para apontar para `docs/sistemas/`.
- `docs/CHANGELOG.md`: adicionada entrada de registro ao final.

## Critérios de aceite
- [x] 1. `wc -c AGENTS.md` ≤ 15.000 (atual: 14.937 bytes).
- [x] 2. 13 arquivos em `docs/sistemas/` + README; cada bloco da § 7 está literal no seu arquivo.
- [x] 3. Saída do script de cobertura no relatório, com a explicação de cada linha listada.
- [x] 4. Referências da § 3.6 ajustadas; verificação com grep confirmou apenas referência ao índice na linha 3 de `AGENTS.md`.
- [x] 5. Não rodar build nem testes (só documentação).
- [x] 6. Entrada no fim de `docs/CHANGELOG.md`.
- [x] 7. Relatório `docs/reviews/SISTEMAS-relatorio.md`.
- [x] 8. Um commit: `docs(agents): estado dos sistemas sob demanda em docs/sistemas (SISTEMAS)`.

## Build e testes
- Não aplicável: alteração restrita a documentação (critério de aceite 5).

## Não testado
- Não aplicável: alteração restrita a documentação.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida ou contradição encontrada durante a migração.

## Medições de tamanho (bytes)

### Antes e depois
- `AGENTS.md`: 39.707 bytes → 14.937 bytes (-24.770 bytes, -62,4%)

### Arquivos em `docs/sistemas/`
| Arquivo | Bytes |
|---|---|
| `docs/sistemas/README.md` | 1.814 |
| `docs/sistemas/aeroportos-tiers.md` | 1.534 |
| `docs/sistemas/combustivel.md` | 3.538 |
| `docs/sistemas/desgaste-falhas.md` | 4.820 |
| `docs/sistemas/economia-dificuldade.md` | 2.141 |
| `docs/sistemas/hud.md` | 1.049 |
| `docs/sistemas/limites-aeronave.md` | 1.192 |
| `docs/sistemas/manutencao-hangar.md` | 2.071 |
| `docs/sistemas/missoes-quadro.md` | 3.201 |
| `docs/sistemas/panes-persistentes.md` | 1.348 |
| `docs/sistemas/pecas.md` | 2.350 |
| `docs/sistemas/sensores.md` | 1.307 |
| `docs/sistemas/voo-integridade.md` | 1.606 |
| `docs/sistemas/xp-fases.md` | 1.649 |
| **Total** | **29.620** |

## Verificação de cobertura (`comm -23 /tmp/orig.txt /tmp/novo.txt`)

Saída do script:
```text
## 7. Estado atual dos sistemas
### Aeroportos e tiers
### Combustível
### Desgaste e falhas
### Economia e dificuldade
### HUD
### Limites por aeronave
### Manutenção e Hangar
### Missões e quadro
### Panes persistentes
### Peças
### Sensores
### Voo e integridade
### XP e fases
- **Contextos de longa duração (bug real já corrigido uma vez):** `MainViewModel`, `PilotRepository`, `FlightService`, `MissionGenerator`, `MarketViewModel`, `MarketSimulationService` e `FerryFlightViewModel` são singletons com um `AppDbContext` próprio para a sessão inteira. O EF devolve a entidade **em cache** numa nova consulta, então dados gravados por outro contexto ficam invisíveis e um `SaveChanges` posterior **sobrescreve** o banco com valores velhos (inspeções e gastos perdidos). Regras: código novo usa `IDbContextFactory<AppDbContext>` com contexto curto por operação; nos contextos longos existentes, chame `_context.ChangeTracker.Clear()` no início de cada operação que lê para alterar; nunca altere uma entidade carregada por outro contexto esperando que o seu `SaveChanges` a grave (copie os valores para a entidade do seu contexto). Teste de regressão: `FlightPersistenceTests`.
- **Frequências:** telemetria principal (`SimTelemetry`) a cada 1 s; panes (`SimFailureState`) a cada 6 frames; leitura rápida de voo (`SimFastFlightState`, toque/crash/G) a cada 2 frames; sensores genéricos a cada 30 frames. Cada uma numa definição separada.
- **Key Events observados sem mascarar:** use grupo próprio não mascarável (`GROUPS.ObservedPilotInputs = 3`, `maskable: false`, prioridade `SIMCONNECT_GROUP_PRIORITY_DEFAULT`). Eventos mapeados com `GetOrMapEvent` pertencem a um único grupo de notificação; roteie no `OnRecvEvent` por `e.uGroupID` (`OnObservedEvent` vs `OnMaskedEvent`). Atualmente observa `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY`.
- **SimVar com nome inválido desloca o struct:** o MSFS rejeita o datum (`NAME_UNRECOGNIZED`), o pacote fica menor e todos os campos seguintes chegam na posição errada (e o marshaling lê além do fim do pacote). Já aconteceu com `PARTIAL PANEL STATIC`, que **não existe** (não há SimVar do estado da tomada estática). O log `[SimConnect EXCEPTION]` agora mostra o nome rejeitado; `TelemetryDefinitionTests` confere a contagem de datums × campos.
- Coluna nova = propriedade na entidade **e** `ALTER TABLE ... ADD COLUMN ... DEFAULT ...` nessa lista. Tabela nova = `DbSet` + `CREATE TABLE IF NOT EXISTS` com os mesmos nomes e tipos que o EF geraria. Há um teste que simula um banco antigo (`ComponentPersistenceTests`).
- Não há migrations do EF. O banco nasce com `EnsureCreatedAsync()` e evolui por `AppDbContext.EnsureSchemaUpdatedAsync()` (lista de `ALTER TABLE` / `CREATE TABLE IF NOT EXISTS` idempotentes).
11. **Hooks obrigatórios** (uma vez por clone): `git config core.hooksPath "$(pwd)/.githooks"`. Bloqueiam commit/push em `main` e `feat/component-health-system`, mensagem fora do padrão, `push --force` e commit do banco. `FC_ALLOW_PROTECTED=1` libera as branches protegidas e é só do revisor (merge aprovado) ou do usuário. Nunca use `--no-verify`.
4. há entrada no `docs/CHANGELOG.md`, e este arquivo foi atualizado se o estado de algum sistema mudou.
5. **Ao terminar:** acrescente uma entrada no fim de `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (Agente)`) e, se mudou arquitetura ou o estado de um sistema, atualize a seção 7 deste arquivo.
7. **Orquestração:** o Claude especifica, revisa e faz o merge; o Gemini implementa num worktree isolado. Fluxo, checklist de revisão e boas práticas em `docs/agents/ORQUESTRACAO.md`; ferramentas em `tools/agents/`.
8. **Máquina nova:** siga `docs/agents/NOVA-MAQUINA.md` (`tools/agents/install-global.sh` instala as instruções globais do Claude, as ferramentas do Gemini e os hooks).
Fonte de verdade sobre **como trabalhar neste repositório** e **qual é o estado atual de cada sistema**.
```

### Explicação de cada linha listada
- `## 7. Estado atual dos sistemas`: cabeçalho renomeado para `## 7. Estado dos sistemas (sob demanda)`.
- `### <Sistema>` (13 cabeçalhos): viraram cabeçalhos de nível 1 (`# <Sistema>`) em cada arquivo de `docs/sistemas/<sistema>.md`.
- `- **Contextos de longa duração...`: condensado no AGENTS.md mantendo todos os singletons, classes, `IDbContextFactory`, `ChangeTracker.Clear()` e teste `FlightPersistenceTests`.
- `- **Frequências:...`: condensado no AGENTS.md removendo repetições de "a cada" e "Cada uma numa".
- `- **Key Events observados sem mascarar:...`: condensado no AGENTS.md mantendo grupo 3, `maskable: false`, `e.uGroupID` e os 3 eventos observados.
- `- **SimVar com nome inválido...`: condensado no AGENTS.md mantendo `NAME_UNRECOGNIZED`, deslocamento, `PARTIAL PANEL STATIC`, log e teste `TelemetryDefinitionTests`.
- `- Coluna nova = propriedade...`: combinado no bullet de migrations do banco no AGENTS.md.
- `- Não há migrations do EF...`: condensado em `- **Sem migrations EF:** ...` no AGENTS.md.
- `11. **Hooks obrigatórios...`: condensado no AGENTS.md mantendo hooks, branches protegidas, bypass e flag `FC_ALLOW_PROTECTED=1`.
- `4. há entrada no docs/CHANGELOG.md...`: atualizado no item 8.4 para apontar para `docs/sistemas/<sistema>.md`.
- `5. **Ao terminar:...`: atualizado no item 4.5 para apontar para `docs/sistemas/<sistema>.md`.
- `7. **Orquestração:...`: condensado no AGENTS.md mantendo papéis e links para `docs/agents/ORQUESTRACAO.md` e `tools/agents/`.
- `8. **Máquina nova:...`: condensado no AGENTS.md mantendo link para `docs/agents/NOVA-MAQUINA.md` e script de instalação.
- `Fonte de verdade sobre **como trabalhar neste repositório** e **qual é o estado atual de cada sistema**.`: atualizado na linha 3 do cabeçalho para indicar que o estado dos sistemas está em `docs/sistemas/` (índice na § 7).
