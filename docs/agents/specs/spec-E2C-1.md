# Spec E2C-1 — primeiro lote de missões com regra própria + paraquedismo pelos perfis

## 1. Objetivo
Ligar `pinga_pinga`, `instrucao_stol`, `check_flight` e `ferry_permit`, transformar `navegacao_vfr` no triângulo com ponto de reportagem e fazer o gerador de paraquedismo usar os perfis do catálogo. Regras de conclusão em lógica pura (`Domain/Missions`) com testes; integração fina no `FlightMonitor`.

## 2. Fatos já levantados (não redescobrir)
- Regras de referência: `docs/plans/catalogo-missoes.md` › 6 (linhas 65, 80, 143, 144, 161), › 9.3 (pesos, linhas 236, 247, 290, 291, 301) e › 9.7.1 (faixas, linhas 363, 367, 378, 426, 441, 442).
- Padrão existente a seguir: rastreadores puros `TouchAndGoCounter`, `OverflyTracker`, `SurveyLegTracker`, `SkydiveDropTracker` (`src/FlightCareer.Domain/Missions/`); dados do contrato em `Mission.ContractData` (JSON) com classes em `ContractData.cs`; conclusão em `MissionCompletion.Evaluate` (`MissionCompletion.cs:17`), que hoje recebe contadores avulsos.
- Geradores especiais: `MissionBoardCore` (`def.Generator == "..."`, ~linhas 190–245) chama `MissionGenerator.Create*MissionSync(origin, def, random, referenceTime, ...)`; `MissionGenerator.CreateMissionObjectSync(origin, dest, distanceNm, def, pax, luggageLbs, cargoLbs, ...)` monta a missão e o pagamento pela distância passada.
- `FlightMonitor` (arquivo quente): rastreadores instanciados nos campos (linhas 42–45), `Reset` no início (~668), `Update` em `OnSimDataReceived` (~1047, cada pacote de `SimTelemetry` a ~1 s), resultado passado a `MissionCompletion.Evaluate` (~1824). **O voo termina** quando, depois de ter decolado, o avião está no solo com motor desligado e parado (~1343). Logo, toda escala intermediária é **parada completa com motor ligado**; cortar o motor numa escala encerra o voo (a missão é avaliada como incompleta).
- `SimTelemetry` tem: `Latitude`, `Longitude`, `Altitude` (MSL, ft), `GroundSpeed`, `Airspeed`, `VerticalSpeed`, `IsOnGround`, `IsEngineRunning`, `PlaneHeading`, `FlapsPercent`, `EngineRPM`, `MaxRPM`, `EngineN1`, `EngineType`, `FuelTotalGallons`, `CrosswindComponentKnots`, `SimulationRate`. **Não tem** posição do trem nem pressurização (não criar SimVar nova nesta tarefa).
- Tempo sempre simulado (`dt × SimulationRate`), nunca relógio real (AGENTS.md › 5).
- Paraquedismo hoje: `MissionGenerator.CreateSkydiveMissionSync` (~linha 832) sorteia `paxCount = random.Next(4, 11)` e bagagem `paxCount * 25.0` lb, ignorando os perfis do catálogo (`paraquedismo`: 35 % 3–6 pax, 50 % 8–14, 15 % 15–20 com `bigAirport`) e `luggagePerPersonKg` (12–16 kg de equipamento por saltador).

## 3. Arquivos
- Criar em `src/FlightCareer.Domain/Missions/`: `MultiStopTracker.cs`, `StolLandingTracker.cs`, `CheckFlightTracker.cs`, `FerryRestrictionMonitor.cs` e um record `MissionTrackerResults` (resultados de todos os rastreadores, para passar ao `Evaluate` sem crescer a lista de parâmetros).
- Mexer: `ContractData.cs` (classes novas), `MissionCompletion.cs` (parâmetro opcional `MissionTrackerResults? trackers = null`, mantendo os existentes), `MissionBoardCore.cs` (despacho dos geradores novos e perfil sorteado para o paraquedismo), `MissionGenerator.cs` (geradores novos + paraquedismo), `mission_catalog.json` (só os 6 tipos deste lote), `mission_texts.json` (≥ 3 variantes por tipo novo, com os marcadores usados), `FlightMonitor.cs` (integração fina, ver § 6), testes.
- **Fora do escopo:** HUD (cada rastreador expõe `StatusText`, que fica para a próxima tarefa), os outros 13 tipos desligados, pesos/faixas de qualquer outro tipo, `PartFlightSession`, `ComponentFlightSession`, `SimConnectService`, `SimTelemetry` (não acrescentar campos). Não regravar o snapshot `missions_golden.json` por outro motivo além destes 6 tipos.

## 4. Contratos e regras (valores na classe de dados/JSON, não espalhados no código)

**Parada completa numa escala** (usada por pinga, triângulo e STOL): no solo, `GroundSpeed < 3 kt` por ≥ 5 s simulados, a ≤ 1,5 NM da coordenada do aeródromo da escala. Depois precisa decolar de novo (no ar por ≥ 5 s) para contar a escala seguinte.

1. **`pinga_pinga`** (generator `multi_stop`): 3–4 etapas de 25–120 NM, total 100–400 NM (perfil do catálogo), escalas tier 2–4 em sequência (sem repetir aeródromo, cada uma mais longe da anterior que 25 NM), destino final = última escala. `MultiStopContractData { List<StopPoint> Stops (Icao, Lat, Lon, ElevationFt), bool RequireNoRefuel = true }`. Conclusão: parada completa em cada escala intermediária **na ordem** + pouso final no destino; `RequireNoRefuel`: combustível total não pode subir mais que max(1 gal, 2 %) entre a primeira decolagem e o pouso final ("Reabasteceu entre escalas"). Pax 2–14, carga 100–1.500 lb do perfil. Pagamento pela distância total da rota.
2. **`navegacao_vfr`** (generator `vfr_triangle`): origem → A → B → origem, A e B tier 3, etapas 25–90 NM, total 80–250 NM. Ponto de reportagem = coordenada `P:` no meio da primeira etapa (desvio lateral de 3–8 NM), em `OverflyWaypoints` (formato já existente). Reusa `MultiStopContractData` (Stops = A, B) + `OverflyTracker`. Conclusão: ponto de reportagem sobrevoado, paradas completas em A e B na ordem e pouso final na origem. 1 pax (instrutor), carga 0.
3. **`instrucao_stol`** (generator `stol_training`): pista curta/não pavimentada tier 4 a 10–40 NM da origem (pista conhecida obrigatória). `StolContractData { StripIcao, Lat, Lon, ElevationFt, RunwayLengthFt, RequiredLandings = 3, MaxStopDistanceFt, MinApproachAngleDeg = 4.0, CrosswindBonusKt = 8 }`, com `MaxStopDistanceFt = clamp(0,6 × RunwayLengthFt, 300, 1.000)`. Um pouso curto conta quando: toque na pista curta (≤ 1,5 NM), parada completa com distância do ponto do toque ao ponto de parada (lat/lon) ≤ `MaxStopDistanceFt`, e ângulo médio de aproximação ≥ `MinApproachAngleDeg` nos últimos 300 ft acima da elevação da pista (ângulo = atan(|VS| / GS) com unidades coerentes, média das amostras dessa faixa). Conclusão: `RequiredLandings` pousos curtos válidos e pouso final na origem. Vento de través ≥ `CrosswindBonusKt` num pouso válido é só aviso positivo no resultado (`Warning`), nunca reprova. 1 pax.
4. **`check_flight`** (generator `check_flight`): local, volta à origem, 0–1 pax (mecânico). `CheckFlightContractData { TestAltitudeFt (elevação da origem + 3.000, arredondado a 500), AltitudeToleranceFt = 300, PowerHoldSeconds = 10, HeadingHoldSeconds = 15, HeadingToleranceDeg = 10 }`. Passos **na ordem**: (1) atingir `TestAltitudeFt ± tol`; (2) potência máxima por `PowerHoldSeconds` (pistão: `EngineRPM ≥ 0,95 × MaxRPM`; turbina: `EngineN1 ≥ 90`); (3) manter cada rumo cardinal N, E, S, W (`PlaneHeading` ± tol) por `HeadingHoldSeconds` dentro da faixa de altitude, em qualquer ordem dos rumos; (4) pouso na origem. Motivo de falha lista o primeiro passo não cumprido.
5. **`ferry_permit`** (generator `ferry_permit`): destino preferencial `mro` (×6), 20–300 NM, 0 pax, 0 carga. Restrição sorteada: `max_altitude` (teto sorteado 8.000–10.000 ft MSL, ao menos elevação do destino e da origem + 2.000) ou `flaps_fixed` (flaps devem ficar no valor da decolagem). `FerryPermitContractData { Restriction, MaxAltitudeFt, FlapTolerancePct = 5 }`. Falha se a altitude passar de `MaxAltitudeFt + 200` por mais de 10 s simulados acumulados, ou se `FlapsPercent` variar mais de `FlapTolerancePct` em relação ao valor registrado na decolagem. Trem travado: **fora** (sem SimVar no `SimTelemetry`); anotar no relatório.
6. **Paraquedismo** (generator `skydive` existente): usar o perfil sorteado pelo `MissionBoardCore` (pax da faixa do perfil, `bigAirport` respeitado como os demais) e bagagem = soma por saltador de `luggagePerPersonKg` (12–16 kg, sorteio individual, em lb), como `MissionBoardCore.WithPersonLuggage` faz nos outros geradores. Sem mudança na regra de conclusão.

Pesos de origem dos tipos novos conforme `catalogo-missoes.md` › 9.3 (já estão no catálogo; só ligar `enabled: true` e ajustar perfis/faixas para § 9.7.1).

## 5. Testes (xUnit, nomes em português como os existentes)
- `MultiStopTrackerTests`: escalas na ordem → completa; escala pulada → falha "escala X não visitada"; ordem trocada → falha; toque sem parar (GS > 3 kt) não conta; combustível subindo 5 gal entre escalas → falha de reabastecimento; subida de 0,5 gal (ruído) não reprova.
- `VfrTriangleTests`: sem o ponto de reportagem → falha; completo → sucesso.
- `StolLandingTrackerTests`: parada a 250 ft com ângulo 5° → conta; parada a 1.200 ft → não conta; ângulo 3° → não conta; 3 válidos + pouso na origem → completa; través 10 kt gera `Warning`.
- `CheckFlightTrackerTests`: sequência completa (pistão e turbina) → completa; faltando o rumo W → falha citando o passo 3; rumos fora da faixa de altitude não contam; tempo com `SimulationRate` 4 conta 4×.
- `FerryRestrictionMonitorTests`: teto excedido 5 s → ok; 15 s → falha; flaps de 0 para 20 % → falha; variação de 3 % → ok.
- Geradores: N quadros com semente fixa por tipo novo → distâncias, número de escalas, tiers e pax dentro das faixas; `navegacao_vfr` sempre volta à origem e tem `P:` em `OverflyWaypoints`; paraquedismo: pax dentro das faixas dos perfis (3–20) e não mais 4–10 fixo, bagagem 12–16 kg × pax.
- `MissionCompletion`: chamadas antigas (sem `trackers`) continuam com o mesmo resultado.

## 6. Integração no `FlightMonitor` (passo a passo; arquivo quente — mexer só nisto)
1. Campos novos para os 4 rastreadores ao lado dos existentes (linhas 42–45).
2. No mesmo ponto do `Reset` existente (~668): ler `ContractData` da missão ativa e configurar o rastreador do tipo (ou desligar todos se não houver).
3. Em `OnSimDataReceived`, logo após `_overflyTracker.Update` (~1050): chamar `Update` dos rastreadores ativos com os campos do `SimTelemetry` e o `dt` simulado já usado no arquivo.
4. Na chamada de `MissionCompletion.Evaluate` (~1824): passar `trackers: new MissionTrackerResults(...)`.
- Não mudar a lógica de início/fim do voo, combustível, slew, toque ou crash. Ler o arquivo inteiro antes (armadilhas: AGENTS.md › 6).

## 7. Critérios de aceite
1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
2. Snapshot `missions_golden.json` regravado e o relatório lista o que mudou (esperado: só os 6 tipos deste lote; qualquer outro tipo alterado = explicar).
3. `board-report` (`dotnet run --project src/FlightCareer.Replay -- board-report --boards 50`): os 5 tipos aparecem com a frequência esperada e faixas coerentes; colar a tabela dos 6 tipos no relatório. **Não** rodar `career-stab` (o revisor roda).
4. Relatório declara o que não foi testado no MSFS (todo o comportamento em voo) e o que ficou fora (trem travado, HUD).
5. `docs/CHANGELOG.md` (`## 2026-11-24 - Primeiro lote de missões com regra própria (E2C-1) (Gemini)`) e `AGENTS.md` › 7 (bloco E2A: tipos ligados e rastreadores novos).
6. Um commit: `feat(missoes): pinga-pinga, triangulo VFR, STOL, voo de teste, translado restrito e paraquedismo pelos perfis (E2C-1)`.
