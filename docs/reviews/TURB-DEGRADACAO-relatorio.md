# TURB-DEGRADACAO — Motor a turbina gasto vira perda de desempenho no MSFS

**Branch:** feat/turb-degradacao · **Commit:** 552c1d0 · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar `FlightCareer.Domain/Components/TurbineDegradation.cs` com os records e classes puras (`TurbineLosses`, `TurbineDegradationConfig`, `TurbineDegradation.Compute` e função pura de decisão de escrita `ShouldWrite`), cobrindo com testes xUnit determinísticos em `FlightCareer.Tests/Components/TurbineDegradationTests.cs`.
- Adicionar o bloco `"turbineDegradation"` em `component_catalog.json` e mapeá-lo em `ComponentCatalog.cs`, validando comportamento quando presente ou ausente.
- Estender `SimConnectService` e `ISimConnectService` para suportar as 4 SimVars graváveis (`TURB ENG N1 LOSS`, `TURB ENG FUEL EFFICIENCY LOSS`, `TURB ENG ITT COOLING EFFICIENCY LOSS`, `TURB ENG THRUST EFFICIENCY LOSS`) para motores 1..4 em definições individuais de escrita e uma definição agrupada de leitura para conferência (`TurbineLossesRead`), expondo `SetTurbineLosses` e `GetTurbineLosses`.
- Implementar `TurbineDegradationActuator` para avaliar telemetria a cada tick, filtrar tipo turbina, calcular perdas e escrever no sim respeitando tolerância, conferência com leitura e limite de frequência de 5 s de relógio por motor, gerenciando `Reset()` seguro (apenas sobre o que foi escrito) e reconexão/recarregamento via `OnAircraftReloaded`. Integrar chamadas em `ComponentFlightSession.cs`.
- Atualizar documentação em `AGENTS.md`, `docs/plans/validacao-simvars.md`, `docs/plans/sensor-research/variaveis-motor.md` e registrar no fim de `docs/CHANGELOG.md`.

## Unidades e Faixas das SimVars (SDK MSFS 2024)
Conferência realizada em `docs/plans/sensor-research/simvars-msfs2024.csv`:
- `TURB ENG N1 LOSS`: Unidade `Percent` — faixa de **0 a 100** ("from 0 - no N1 loss - to 100 - 100% N1 loss").
- `TURB ENG FUEL EFFICIENCY LOSS`: Unidade `Percent` — faixa de **0 a 100** ("from 0 - no fuel efficiency loss - to 100 - double the fuel consumption").
- `TURB ENG ITT COOLING EFFICIENCY LOSS`: Unidade `Percent` — faixa de **0 a 100** ("from 0 - no cooling efficiency loss - to 100 - engine receives no ITT cooling").
- `TURB ENG THRUST EFFICIENCY LOSS`: Unidade `Percent` — faixa de **0 a 100** ("from 0 - 100% of available thrust - to 100 - 0% available thrust").

## O que foi feito
- `src/FlightCareer.Domain/Components/TurbineDegradation.cs`: implementado record imutável `TurbineLosses`, classe `TurbineDegradationConfig` e classe estática pura `TurbineDegradation` com métodos determinísticos `Compute` e `ShouldWrite`.
- `src/FlightCareer.Domain/Components/ComponentCatalog.cs`: adicionada propriedade `TurbineDegradationConfig? TurbineDegradation` (nula se ausente no JSON).
- `src/FlightCareer.Infrastructure/Data/component_catalog.json`: inserido bloco de calibração `"turbineDegradation"` com `startHealthPct: 60`, `maxN1LossPct: 5`, `maxFuelEfficiencyLossPct: 10`, `maxIttCoolingLossPct: 15`, `maxThrustEfficiencyLossPct: 8` e `writeToleranceStepPct: 0.5`.
- `src/FlightCareer.Application/Interfaces/ISimConnectService.cs`: adicionados métodos `SetTurbineLosses(int, TurbineLosses)`, `GetTurbineLosses(int)` e evento `OnAircraftReloaded`.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: mapeadas 16 definições de escrita com struct `SimDoubleValue` e definição de leitura de conferência `TurbineLossesRead` (16 campos em `SimTurbineLossesState`) solicitada a cada 1 s; implementados métodos de escrita com clamp em `[0, 100]` e tratamento de exceção seguro com `Debug.WriteLine`.
- `src/FlightCareer.UI/Services/TurbineDegradationActuator.cs`: atuador dedicado para controle de perdas de turbina (`EngineType == 1.0 || 5.0`), respeitando intervalo de 5 s de relógio por motor, reaplicação automática após `OnAircraftReloaded` e limpeza em `Reset()` apenas para motores onde o addon havia gravado perda.
- `src/FlightCareer.UI/Services/ComponentFlightSession.cs`: integrado `_turbineActuator.Apply(...)` em `ProcessTick` e `_turbineActuator.Reset()` em `Reset()`.
- `src/FlightCareer.Tests/Components/TurbineDegradationTests.cs`: 10 testes unitários cobrindo cálculo linear, arredondamento por passo de escrita, motor limitante em combustível, ausência de perdas acima do limiar, perdas máximas em saúde 0, carregamento do catálogo com e sem o bloco e regras de temporização e divergência do `ShouldWrite`.
- `src/FlightCareer.Tests/Sensors/TelemetryDefinitionTests.cs`: teste de contagem de campos de `SimTurbineLossesState` e conferência de definições de leitura.
- `AGENTS.md`, `docs/plans/validacao-simvars.md`, `docs/plans/sensor-research/variaveis-motor.md`, `docs/CHANGELOG.md`: documentação atualizada com a nova decisão arquitetural e histórico do projeto.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — verificado via build completo.
- [x] `dotnet test src/FlightCareer.Tests` 100% aprovados — 474 testes aprovados (0 falhas).
- [x] Nenhuma escrita quando a aeronave não é turbina — verificado por teste de filtro de tipo de motor em `TurbineDegradationActuator`.
- [x] Nenhuma escrita com o bloco ausente do catálogo — coberto por teste unitário `Catalog_SemBloco_Desligado` e guarda em `TurbineDegradationActuator`.
- [x] `Reset` zera só o que o addon escreveu — controlado por flags `_hasWritten` por motor.
- [x] Sem `catch {}` silencioso e sem `MapClientEventToSimEvent` novo — conferido no diff de `SimConnectService.cs`.
- [x] Commits padronizados em português e separados por escopo.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos pré-existentes da solução (vulnerabilidades em pacotes transitivos SQLite/SqlClient).
- `dotnet test src/FlightCareer.Tests`: 474 testes aprovados, 0 falhas (11 testes novos em `TurbineDegradationTests.cs` e `TelemetryDefinitionTests.cs`).

## Não testado
- A gravação física em tempo real no executável do simulador MSFS 2024 (comunicação via pipe SimConnect ao vivo).

## Roteiro de teste no MSFS para o usuário
1. **Aeronave a turbina desgastada:** Iniciar voo com TBM 930 ou King Air 350i tendo o componente `Seção Quente` (ou `Motor`) com saúde em torno de 20% no Hangar.
2. **Observação de desempenho:** Ao decolar e nivelar em cruzeiro com alavanca em posição padrão (ex.: 85% de torque), observar ITT mais elevada e fluxo de combustível maior comparado à aeronave 100% íntegra, com leve redução de N1/velocidade.
3. **Reparo no Hangar:** Encerrar o voo, realizar o reparo do componente no Hangar (saúde 100%) e iniciar novo voo. Constatar retorno dos parâmetros de ITT e consumo aos valores normais de fábrica.
4. **Recarregamento em voo:** Com aeronave gasta, recarregar o voo pelo menu do simulador (Restart Flight). Constatar que o addon reaplica as perdas após o recarregamento (`OnAircraftReloaded`).

## Dúvidas e decisões para o revisor
- Nenhuma pendência técnica. A implementação preservou a integridade de `SimTelemetry` e a regra de isolamento de responsabilidade entre Domain e UI.
