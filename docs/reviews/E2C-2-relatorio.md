# E2C-2 — Trabalho aéreo e ronda de saúde

**Branch:** feat/e2c-2-trabalho-aereo · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar `AerialWorkTracker.cs` com 4 modos (`sectors`, `route`, `orbit`, `search`), rastreando amostras válidas no ar, faixa de altitude relativa à elevação de referência da origem e limites de IAS.
- Estender `ContractData.cs`, `MissionTrackerResults.cs`, `MissionCompletion.cs`, `ContractProgressFormatter.cs` e `ContractMarkerBuilder.cs` para suportar trabalho aéreo e ronda de saúde (`round_trip` com `MultiStopTracker`).
- Implementar 5 geradores em `MissionGenerator.cs` (`CreatePatrolMissionSync`, `CreateLineInspectionMissionSync`, `CreateTrafficOrbitMissionSync`, `CreateSarMissionSync`, `CreateRoundTripMissionSync`) e despachar em `MissionBoardCore.cs`.
- Atualizar `mission_catalog.json` (habilitar os 5 tipos, ajustar geradores e variantes SAR) e `mission_texts.json` (≥ 3 variantes por tipo).
- Integrar no `FlightMonitor.cs` (campo, reset, configure, update com `simDt`, inputs do HUD e resultados de missão).
- Criar testes unitários para o tracker, geradores, conclusão e HUD; atualizar snapshot `missions_golden.json`; rodar `board-report --boards 50`.

## O que foi feito
- `src/FlightCareer.Domain/Missions/ContractData.cs`: Adicionadas as classes `AerialWorkContractData` e `WorkPoint`.
- `src/FlightCareer.Domain/Missions/AerialWorkTracker.cs`: Implementado o rastreador puro com 4 modos (`sectors`, `route`, `orbit`, `search`), validação de altitude relativa a `RefElevationFt`, limites de velocidade `MaxIasKt`, acúmulo de tempo simulado `simDt`, cálculo de desvio lateral e razões de falha descritivas.
- `src/FlightCareer.Domain/Missions/MissionTrackerResults.cs`: Incluído o parâmetro opcional `AerialWorkTracker? AerialWork = null`.
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: Adicionadas validações para `patrol`, `line_inspection`, `traffic_orbit`, `sar_pattern` (via `AerialWork`) e `round_trip` (via `MultiStopTracker`).
- `src/FlightCareer.Domain/Missions/ContractProgressFormatter.cs`: Adicionada formatação de progresso em linha única para os 4 modos operacionais de trabalho aéreo com aviso de velocidade/altitude e conclusão (`✓`).
- `src/FlightCareer.Domain/Missions/ContractMarkerBuilder.cs`: Adicionada projeção de setores/pontos, área circular de órbita e datum/pontos SAR (mantendo o alvo SAR estritamente oculto no mapa tático).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: Implementados os geradores `CreatePatrolMissionSync`, `CreateLineInspectionMissionSync`, `CreateTrafficOrbitMissionSync`, `CreateSarMissionSync` (variantes `expanding_square` e `creeping_line`) e `CreateRoundTripMissionSync`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: Conectado o despacho dos 5 geradores (`patrol`, `line_inspection`, `traffic_orbit`, `sar_pattern`, `round_trip`) com recalibração de bagagem por passageiro.
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`: Habilitados os 5 tipos (`enabled: true`), ajustados seus geradores e configurados os perfis com variantes para `sar`.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: Adicionadas ≥ 3 variantes narrativas ricas para os 5 tipos (incluindo variantes específicas por padrão SAR: `sar:expanding_square` e `sar:creeping_line`).
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: Integrado o `_aerialWorkTracker` ao ciclo de monitoramento (reset, configure, update com `simDt`, inputs de HUD e resultados do voo).
- `src/FlightCareer.Tests/Missions/AerialWorkTrackerTests.cs`: Testes unitários para todos os 4 modos de `AerialWorkTracker`.
- `src/FlightCareer.Tests/Missions/PhaseE2C2MissionTests.cs`: Testes de integração para geradores, contratos, conclusão, textos sem `{}` e marcadores.
- `src/FlightCareer.Tests/Missions/MissionCatalog69Tests.cs`: Ajustada a contagem de tipos desabilitados de 13 para 8.
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`: Regravado snapshot golden de equivalência.
- `docs/CHANGELOG.md` e `docs/sistemas/missoes-quadro.md`: Documentação atualizada.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — Verificado com 0 erros e 81 avisos pré-existentes.
- [x] `dotnet test src/FlightCareer.Tests` 100% — Verificado com 1.118 testes aprovados (0 falhas).
- [x] Snapshot `missions_golden.json` regravado — 246 de 280 registros mudaram pelo efeito de roleta e ativação dos 5 novos tipos.
- [x] `board-report --boards 50` executado — Linhas coladas abaixo.
- [x] `docs/CHANGELOG.md` e `docs/sistemas/missoes-quadro.md` atualizados.
- [x] Declaração de limitações e não testado no MSFS incluída.

## Resultados do board-report (--boards 50)

### Frequência por Aeroporto
| Tipo | SBGR | KMEM | SBKP | SBTF | SDCO | SDOI | SBJR | SDAO | PABE | SBAN |
|---|---|---|---|---|---|---|---|---|---|---|
| `patrulha_vigilancia` | 0.0% | 0.6% | 0.5% | 2.8% | 0.8% | 3.6% | 1.3% | 5.3% | 1.7% | 1.2% |
| `inspecao_linhas` | 0.0% | 0.7% | 0.7% | 0.5% | 0.8% | 1.8% | 0.9% | 1.4% | 0.8% | 1.3% |
| `monitoramento_trafego` | 0.7% | 1.4% | 1.3% | 1.0% | 1.5% | 0.0% | 0.6% | 0.0% | 1.2% | 0.7% |
| `sar` | 0.3% | 0.8% | 0.6% | 0.5% | 0.8% | 1.3% | 0.3% | 1.0% | 0.6% | 2.3% |
| `saude_familia` | 0.0% | 4.3% | 1.3% | 5.8% | 1.7% | 1.3% | 2.8% | 0.6% | 7.2% | 1.0% |

### Parâmetros Físicos por Tipo
| Tipo | Pax (Mín/Méd/Máx · Dist) | Carga lbs (Mín/Méd/Máx · Dist) | Bagagem lbs (Mín/Méd/Máx · Dist) | Distância NM (Mín/Méd/Máx · Dist) |
|---|---|---|---|---|
| `patrulha_vigilancia` | 1/1.5/2 · (2) | 0/0/0 · (1) | 7/21/44 · (18) | 39/102/154 · (111) |
| `inspecao_linhas` | 1/1.0/1 · (1) | 0/0/0 · (1) | 7/14/22 · (8) | 74/147/200 · (107) |
| `monitoramento_trafego` | 1/1.5/2 · (2) | 0/25/50 · (51) | 7/21/44 · (18) | 25/36/40 · (15) |
| `sar` | 1/2.1/3 · (3) | 0/0/0 · (1) | 7/30/62 · (26) | 51/140/243 · (154) |
| `saude_familia` | 3/5.5/8 · (6) | 100/354/600 · (471) | 24/78/157 · (57) | 105/336/500 · (305) |

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 81 avisos (avisos legados de depreciação e nulidade).
- `dotnet test src/FlightCareer.Tests`: 1.118 testes aprovados, 0 falhas (23 testes novos cobrindo E2C-2).

## Não testado
- Comportamento dinâmico em voo dentro do simulador MSFS 2020/2024.
- Limitações da arquitetura mantidas:
  - Faixa "acima do solo" é avaliada em relação à elevação de referência da origem (`RefElevationFt`), pois a telemetria não dispõe de AGL real.
  - Traçados de linhas de transmissão, setores de patrulha e padrões SAR são gerados proceduralmente (com POIs reais quando disponíveis para nomeação do alvo).
  - Regra de pouso antes do pôr do sol na ronda de saúde permanece fora do escopo desta tarefa (aguarda suporte a hora solar no SimConnect).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente.
