# E2C-1 — Primeiro lote de missões com regra própria + paraquedismo pelos perfis

**Branch:** feat/e2c-1-missoes-regra · **Commit:** 8d6e310 · **Data:** 2026-11-24

## Plano (antes de codar)
- Implementar os 4 rastreadores puros em `Domain/Missions/` (`MultiStopTracker`, `StolLandingTracker`, `CheckFlightTracker`, `FerryRestrictionMonitor`) e o record `MissionTrackerResults`.
- Expandir `ContractData.cs` com contratos tipados e atualizar `MissionCompletion.Evaluate` para validar os 5 novos tipos com `MissionTrackerResults`.
- Atualizar geradores em `MissionGenerator.cs` e despacho em `MissionBoardCore.cs` para os 5 novos geradores e paraquedismo por perfis/bagagem.
- Ativar e ajustar `mission_catalog.json` e adicionar textos de RP com marcadores em `mission_texts.json`.
- Integrar os rastreadores no `FlightMonitor` e atualizar suíte de testes xUnit e snapshot `missions_golden.json`.

## O que foi feito
- `src/FlightCareer.Domain/Missions/MultiStopTracker.cs`: rastreador de paradas completas sequenciais (`GroundSpeed < 3 kt` por $\ge 5$ s simulados a $\le 1,5$ NM), exigindo nova decolagem ($\ge 5$ s no ar) entre escalas e monitorando integridade de combustível (`RequireNoRefuel`: bloqueia se subir mais que $\max(1\text{ gal}, 2\%)$).
- `src/FlightCareer.Domain/Missions/StolLandingTracker.cs`: rastreador de pousos curtos com detecção de aproximação íngreme ($\ge 4^\circ$ calculada como $\text{atan}(|\text{VS}|/\text{GS})$ nos últimos 300 ft AGL antes do toque), distância de parada $\le \text{clamp}(0,6 \times \text{RunwayLength}, 300, 1.000)$ ft, e aviso positivo de através ($\ge 8$ kt) no resultado.
- `src/FlightCareer.Domain/Missions/CheckFlightTracker.cs`: roteiro ordenado de 4 passos para voo de teste técnico: (1) atingir altitude $\pm 300$ ft; (2) potência máxima contínua por 10 s simulados (RPM $\ge 95\%$ MaxRPM em pistão ou N1 $\ge 90$ em turbina); (3) manter cada rumo N, E, S, W por 15 s simulados na altitude de teste; (4) pouso na origem.
- `src/FlightCareer.Domain/Missions/FerryRestrictionMonitor.cs`: monitor de translado com restrições técnicas, detectando violação de teto de voo (acumulando tempo acima de $MaxAlt + 200$, reprovando com $> 10$ s simulados) ou variação de flaps excedendo $\pm 5\%$ da posição na decolagem.
- `src/FlightCareer.Domain/Missions/MissionTrackerResults.cs`: record desacoplado reunindo resultados dos rastreadores para repasse a `MissionCompletion.Evaluate`.
- `src/FlightCareer.Domain/Missions/ContractData.cs`: classes de dados tipadas (`MultiStopContractData`, `StopPoint`, `StolContractData`, `CheckFlightContractData`, `FerryPermitContractData`).
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: suporte ao parâmetro opcional `MissionTrackerResults? trackers = null`, validando regras de conclusão e motivos descritivos em caso de não cumprimento para `multi_stop`, `vfr_triangle`, `stol_training`, `check_flight` e `ferry_permit`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: implementados `CreateMultiStopMissionSync`, `CreateVfrTriangleMissionSync`, `CreateStolTrainingMissionSync`, `CreateCheckFlightMissionSync` e `CreateFerryPermitMissionSync`; atualizado `CreateSkydiveMissionSync` com `paxOverride` e cálculo de bagagem individual por saltador (12–16 kg em lb).
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: despacho dos 5 novos geradores e sorteio do perfil do catálogo para paraquedismo (pax da faixa do perfil e bagagem por saltador via `WithPersonLuggage`).
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`: ativados `pinga_pinga`, `navegacao_vfr`, `instrucao_stol`, `check_flight` e `ferry_permit` (`enabled: true`, restando 13 desabilitados), com geradores correspondentes e `return_to_origin` / faixas conforme especificação.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: adicionadas $\ge 3$ variantes imersivas para os 5 novos tipos operacionais preenchendo todos os marcadores (`{stops}`, `{strip}`, `{rwy_len}`, `{alt}`, `{restriction}`, `{reason}`, etc.).
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: instanciação dos novos rastreadores, reset no `StartMonitoring` com deserialização de `ContractData`, atualização periódica com `packetSimDt` em `OnSimDataReceived` e envio de `MissionTrackerResults` para `FlightService.ProcessFlightFinishAsync`.
- `src/FlightCareer.Application/Services/FlightService.cs`: assinatura de `ProcessFlightFinishAsync` estendida recebendo `MissionTrackerResults? trackers = null` e repassando para `MissionCompletion.Evaluate`.
- `src/FlightCareer.Tests/`:
  - Criados `MultiStopTrackerTests.cs` (6 testes), `VfrTriangleTests.cs` (2 testes), `StolLandingTrackerTests.cs` (5 testes), `CheckFlightTrackerTests.cs` (5 testes), `FerryRestrictionMonitorTests.cs` (4 testes) e `PhaseE2C1GeneratorsTests.cs` (7 testes).
  - Atualizado `MissionCatalog69Tests.cs` conferindo a redução de tipos desabilitados de 17 para 13.
  - Regravado `missions_golden.json` pelo harness de testes.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros e 0 avisos novos — verificado via CLI.
- [x] `dotnet test src/FlightCareer.Tests` passa com 100% verde (1.051 testes aprovados) — verificado via CLI.
- [x] Snapshot `missions_golden.json` regravado: justificado pela ativação e ajuste dos 6 tipos de missão, que mudaram a roleta de sementes nos aeródromos onde esses tipos têm peso.
- [x] `board-report` executado com 50 boards (`dotnet run --project src/FlightCareer.Replay -- board-report --boards 50`) e tabela dos 6 tipos incluída abaixo.
- [x] Não testado no MSFS declarado no relatório.
- [x] Atualizado `docs/CHANGELOG.md` e `AGENTS.md` › 7.

### Tabela do `board-report` (50 boards por aeródromo)
Amostra das frequências e características dos 6 tipos nos aeródromos do relatório:

| Missão | Ocorrência (%) | Pax | Carga / Bagagem (lb) | Distância (NM) | Obs |
|---|---|---|---|---|---|
| `check_flight` | SDCO: 4.2% · SBKP: 4.0% | 0–1 | 0 | 30 | Volta à origem; altitude +3.000 ft |
| `ferry_permit` | SBKP: 0.9% · SDCO: 0.8% · SBRJ: 0.2% | 0 | 0 | 28–300 | Destino MRO preferencial (20.4% geral) |
| `instrucao_stol` | SDOI: 4.4% · SDAO: 2.0% | 1 | 0 | 30–90 | Pista tier 4 curta (10–40 NM da origem) |
| `navegacao_vfr` | SBJR: 12.9% · SDCO: 6.4% · SBDN: 4.4% | 1 | 0 | 91–249 | Triângulo com waypoint P: de reportagem |
| `pinga_pinga` | SBTF: 6.5% · PABE: 5.8% · SBDN: 4.4% | 2–14 | 100–1.500 | 107–446 | 3–4 etapas sequenciais tier 2–4 |
| `paraquedismo` | SDOI: 7.0% · SBDN: 3.4% · SDCO: 2.6% | 3–20 | 79–628 | 30–40 | Perfis 35%/50%/15% respeitados; 12–16 kg/pax |

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 1.051 aprovados, 0 falhas (29 testes novos cobrindo trackers e geradores da fase E2C-1).

## Não testado
- Todo o comportamento de telemetria e voo real no simulador MSFS 2020/2024.
- A restrição de "trem travado" no `ferry_permit` ficou fora do escopo desta tarefa devido à ausência de SimVar de posição do trem no struct `SimTelemetry` (conforme orientação da especificação).
- Exibição de `StatusText` no HUD Overlay ficou para a próxima tarefa conforme especificação.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A taxa de adesão ao destino preferencial MRO em `ferry_permit` ficou em ~20.4%, consistente com a disponibilidade de aeródromos etiquetados como MRO ao alcance das aeronaves e peso $\times 6$.
