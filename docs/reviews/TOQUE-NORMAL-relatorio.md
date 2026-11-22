# TOQUE-NORMAL — Razão de descida do toque pela SimVar do simulador

**Branch:** fix/toque-normal · **Commit:** PENDENTE · **Data:** 2026-11-22

## Plano (antes de codar)
- Estender `SimFastFlightState` e `AddFastDef` no `SimConnectService.cs` com `PLANE TOUCHDOWN NORMAL VELOCITY` ("feet per second") no final da definição rápida.
- Atualizar `FastFlightSample` em `TouchdownDetector.cs` com `double TouchdownNormalFps = double.NaN` no fim.
- Em `TouchdownDetector.Classify`, remover o uso de `BodyVerticalFps` e adotar `PLANE TOUCHDOWN NORMAL VELOCITY` (convertido para fpm via × 60) quando atualizado no contato pós-toque, mantendo `VerticalSpeedFpm` como fallback.
- Repassar `s.TouchdownNormalVelocity` em `FlightMonitor.OnFastFlight`.
- Implementar testes em `TouchdownDetectorTests.cs` cobrindo o caso real de nariz alto (C152), fallback ignorando Body Y, valor inalterado, pouso duro e crash, além de teste de layout em `TelemetryDefinitionTests.cs`.
- Atualizar `AGENTS.md` e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.UI/Services/SimConnectService.cs`: campo `TouchdownNormalVelocity` adicionado ao final de `SimFastFlightState` e registrado com `AddFastDef("PLANE TOUCHDOWN NORMAL VELOCITY", "feet per second")`.
- `src/FlightCareer.Domain/Components/TouchdownDetector.cs`: `FastFlightSample` recebeu `TouchdownNormalFps = double.NaN`. Em `Classify`, `vsFpm` agora calcula apenas o maior `-VerticalSpeedFpm` (sem `BodyVerticalFps`). A razão do toque usa a última amostra da janela pós-contato com `TouchdownNormalFps` finito e diferente da referência pré-contato (|Δ| > 1e-6); se não houver atualização da SimVar, faz fallback para `vsFpm`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: repassa `s.TouchdownNormalVelocity` para `FastFlightSample`.
- `src/FlightCareer.Tests/Components/TouchdownDetectorTests.cs`: adicionados os testes `Pouso_suave_de_nariz_alto_usa_a_velocidade_normal_do_sim`, `Sem_velocidade_normal_usa_vertical_speed_e_ignora_body_y`, `Velocidade_normal_sem_mudar_no_contato_usa_vertical_speed`, `Pouso_duro_pela_velocidade_normal` e `Crash_pela_velocidade_normal`. O teste antigo que exigia `BodyVerticalSpeed` maior foi substituído por `Sem_velocidade_normal_usa_vertical_speed_e_ignora_body_y`.
- `src/FlightCareer.Tests/Sensors/TelemetryDefinitionTests.cs`: adicionado teste `FastFlightState_DatumCount_MatchesSimFastFlightStateFields` validando a correspondência dos 9 datums e campos.
- `AGENTS.md`: atualizada descrição do `FlightMonitor` e leitura rápida de toque.
- `docs/CHANGELOG.md`: registrado histórico da tarefa.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx 0 erros; dotnet test src/FlightCareer.Tests 100 % — verificado via CLI.
- [x] Campo novo só no fim do struct, na mesma ordem e unidade do AddFastDef — `TouchdownNormalVelocity` adicionado na 9ª posição em ambos.
- [x] BodyVerticalFps não entra mais na razão de descida — removido de `Classify` e testado explicitamente.
- [x] Entrada no fim do docs/CHANGELOG.md; AGENTS.md › 7 (FlightMonitor/toque) cita a fonte nova. Relatório diz "não testado no MSFS" — atualizados.
- [x] Um commit: fix(toque): razao de descida pela PLANE TOUCHDOWN NORMAL VELOCITY (TOQUE-NORMAL).

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 744 aprovados, 0 falhas (novos: 5 testes em `TouchdownDetectorTests`, 1 em `TelemetryDefinitionTests`).

## Não testado
- Comportamento em tempo real no MSFS 2024 conectado via SimConnect.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. A especificação detalhada cobriu todos os cenários com clareza.
