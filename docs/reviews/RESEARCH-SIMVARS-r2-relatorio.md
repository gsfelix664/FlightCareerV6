# RESEARCH-SIMVARS-r2 — Pesquisa de Variáveis, Auditoria do Registro e SDK 2024 (Rodada 2)

**Branch:** docs/pesquisa-variaveis · **Commit:** bcc4c48 · **Data:** 2026-11-15

## Plano (antes de codar)
- Investigar as páginas oficiais do SDK MSFS 2024 indicadas pelo usuário (SimConnect, SimVars por sistema, Key Events, Input Events, SimConnect Inspector e SimVarWatcher).
- Realizar auditoria rigorosa de todas as SimVars cadastradas em `src/FlightCareer.Infrastructure/Data/sensor_registry.json` (421 sensores), `context_registry.json` (25 variáveis) e nas definições fixas de `SimConnectService.cs` (`SimTelemetry`, `SimFailureState`, `SimFastFlightState`, definições de escrita).
- Documentar exclusivamente os problemas encontrados (nomes não encontrados, variáveis legadas com substituta no 2024, unidades incompatíveis/suspeitas e erros de indexação/escopo) em `docs/plans/sensor-research/auditoria-registro.md`.
- Investigar e detalhar em `docs/plans/sensor-research/eventos-e-input-events.md`:
  - Confirmação formal dos key events de falha que usamos e novos key events candidatos a panes (com parâmetros).
  - Funcionamento da API de Input Events do MSFS 2024 (`SimConnect_EnumerateInputEvents`, `SimConnect_GetInputEvent`, `SimConnect_SetInputEvent`, `SimConnect_SubscribeInputEvent`), problemas que ela resolve para nós (ex.: chave de aviônicos do G1000) e suas limitações técnicas.
  - Confirmação da sintaxe oficial de LVars diretas via SimConnect (`L:VARIABLE_NAME`), escopos suportados (`A:`, `E:`, `L:`, `I:`, `O:`, `Z:`) e variáveis com nome de componente (`SIMVAR:'ComponentName'_n`).
- Atualizar `variaveis-referencias.md` com os índices completos de `COVER ON:0..7`, seção detalhada sobre ferramentas de conferência ao vivo (SimConnect Inspector e SimVarWatcher com passo a passo para o usuário) e lista refinada de 20 candidatas ao teste de solo.
- Manter o código inalterado, rodar build e testes e documentar no CHANGELOG.

## O que foi feito
- `docs/plans/sensor-research/auditoria-registro.md`:
  - Auditoria completa do registro do FlightCareer contra a documentação oficial do MSFS 2024 SDK.
  - Levantamento de nomes inexistentes que disparam exceção: `FLAP SPEED EXCEEDED` e `GEAR SPEED EXCEEDED` (mensagens de UI da Carreira Asobo, não SimVars) e `ELECTRICAL BATTERY LEVEL`.
  - Mapeamento de erros críticos de indexação: sensores de ponto de contato (`CONTACT POINT COMPRESSION`, `CONTACT POINT IS SKIDDING`) e de combustível (`FUELSYSTEM PUMP ACTIVE`, `FUELSYSTEM LINE FUEL PRESSURE`) configurados erroneamente com `perEngine: true` (expandindo por motor em vez de por contact point / bomba / linha de combustível).
  - Identificação de SimVars que exigem índice e foram cadastradas sem índice (`WING FLEX PCT:1..2`, `CIRCUIT BREAKER PULLED:index`, `PNEUMATICS DUCT PRESSURE:index`, `PNEUMATICS PACK STATUS:index`).
  - Identificação de divergências de unidade (`pneu-pack-status` com `bool` quando o SDK define `percent over 100`, `elec-battery-estimated-capacity-pct` como `Percent` e `gear-skidding-factor` como `Percent Over 100`).
  - Análise das definições fixas: `TURB ENG CONDITION LEVER POSITION:1` com unidade `Percent` quando o SDK define `Enum` de 3 posições discretas; `BRAKE LEFT/RIGHT WEIGHT REMAINING` documentadas como `Percent Over 100`.
- `docs/plans/sensor-research/eventos-e-input-events.md`:
  - Confirmação formal de todos os eventos de falha utilizados pelo `SimFailureActuator` e `SetAlternatorConnected` com URLs diretas do SDK.
  - Lista de novos key events candidatos a panes (`TOGGLE_ALTERNATE_STATIC`, `EXTINGUISH_ENGINE_FIRE`, `ELECTRICAL_CIRCUIT_BREAKER_TOGGLE`, `TOGGLE_ELECT_FUEL_PUMP1..4`).
  - Análise da API de Input Events: arquitetura com 4 métodos C++/C#, solução definitiva para o switch de aviônicos do G1000 e disjuntores, e limitações (hashes dinâmicos que exigem enumeração por aeronave).
  - Documentação da sintaxe nativa de LVars no SimConnect 2024, escopos (`A:`, `E:`, `L:`, `I:`, `O:`, `Z:`) e sintaxe de componentes nomeados (`SIMVAR:'ComponentName'_n`).
- `docs/plans/sensor-research/variaveis-referencias.md`:
  - Atualização dos índices de `COVER ON:0..7` (0 calços, 1 motores, 2 pitot, 3 tomadas estáticas, 4 rotores, 5 trem de pouso, 6 hélices, 7 para-brisa).
  - Criação da seção 4 "Ferramentas de Conferência ao Vivo" detalhando o SimConnect Inspector (Dev Mode) e o SimVarWatcher (SDK Sample), com passo a passo claro para o usuário testar qualquer variável ao vivo sem o FlightCareer.
  - Refinamento da lista de 20 candidatas prioritárias ao teste de solo.
- `docs/CHANGELOG.md`: Adicionada entrada da Rodada 2 da pesquisa realizada pelo Gemini.

## Critérios de aceite
- [x] Ler as páginas oficiais do SDK indicadas na especificação — URLs e citações incluídas em todos os documentos.
- [x] Auditoria do registro inteiro (`sensor_registry.json`, `context_registry.json`, definições fixas em `SimConnectService.cs`) — Gerado `auditoria-registro.md` contendo somente os problemas com causas e substitutas oficiais.
- [x] Key events de falha e de sistemas confirmados/negados com novos eventos mapeados — Detalhado na seção 1 de `eventos-e-input-events.md`.
- [x] Input Events (2024) analisados (leitura/escrita, o que resolve, limitações) — Detalhado na seção 2 de `eventos-e-input-events.md`.
- [x] LVars no SimConnect 2024 e sintaxe de componentes nomeados (`:'Bus_1'_n`) confirmados — Detalhado na seção 3 de `eventos-e-input-events.md`.
- [x] Ferramentas de conferência ao vivo (SimConnect Inspector e SimVarWatcher com passo a passo) — Inserido na seção 4 de `variaveis-referencias.md`.
- [x] Lista de 20 variáveis candidatas atualizada com `COVER ON:0..7` completo — Atualizado em `variaveis-referencias.md`.
- [x] Nenhuma alteração em código ou catálogo de regras (`src/` inalterado) — Verificado com `git status`.
- [x] Build e testes executados com sucesso — `dotnet build` e `dotnet test` passaram com 0 erros.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos (vulnerabilidades pré-existentes de pacotes NuGet legados).
- `dotnet test src/FlightCareer.Tests`: 319 aprovados, 0 falhas.

## Não testado
- Testes práticos no MSFS 2024: A tarefa é estritamente de pesquisa técnica e documentação com base no SDK oficial do MSFS 2024. A validação das 20 variáveis selecionadas ocorrerá durante o próximo voo de calibração/teste de solo com o usuário.

## Dúvidas e decisões para o revisor
- **Ajustes necessários no `sensor_registry.json` em tarefa futura:** A auditoria revelou três categorias urgentes de correção para uma futura tarefa de catálogo:
  1. Remover ou substituir `FLAP SPEED EXCEEDED` e `GEAR SPEED EXCEEDED` (strings de UI do sim) e `ELECTRICAL BATTERY LEVEL`.
  2. Corrigir as variáveis com `perEngine: true` que pertencem a outros subsistemas (`CONTACT POINT COMPRESSION`, `CONTACT POINT IS SKIDDING`, `FUELSYSTEM PUMP ACTIVE`, `FUELSYSTEM LINE FUEL PRESSURE`).
  3. Adicionar os índices obrigatórios em `WING FLEX PCT` (`:1` e `:2`), `CIRCUIT BREAKER PULLED:index` e componentes pneumáticos.
- **Adoção gradual da API de Input Events:** Recomendamos que a API de Input Events seja avaliada para a detecção do Avionics Master Switch no C172 G1000 na próxima fase de aviônicos, pois isso resolverá de forma definitiva o problema de acionamento de partida com aviônicos ligados sem depender de estimativas indiretas por barramento.
