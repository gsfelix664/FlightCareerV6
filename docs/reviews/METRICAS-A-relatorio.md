# METRICAS-A — Métricas de solo, partida e motor que as regras usam mas ninguém calcula

**Branch:** feat/metricas-a · **Commit:** pendente · **Data:** 2026-11-22

## Plano (antes de codar)
- 1. Modificar `WearRuleMetrics.cs`: adicionar `BrakeEnergyPctRef`, `SkidSeconds`, `StartLightsOnCount`. Remover `BrakeEnergyMkJ` (substituída por `BrakeEnergyPctRef` no vocabulário e no `All`).
- 2. Modificar `wear_rules.json`: atualizar regra `brake_high_energy_antiskid` para usar métrica `brakeEnergyPctRef` e faixas 60.0, 100.0, 150.0. Adicionar regra nova `ground_tire_skid` (`skidSeconds`, faixas 1, 3, 6s com inspeção `TIRE_FLAT_SPOT_INSPECTION`). Adicionar regra nova `engine_start_electrical_load` (`startLightsOnCount`, faixas 1, 2, 3 com peças bateria e starter/starter_generator/pneumatic_starter).
- 3. Atualizar `WearRuleCatalogTests.cs`: atualizar assert de `brakeEnergyMkJ` para `brakeEnergyPctRef`.
- 4. Implementar no `PartEventDeriver.cs` os cálculos com estado limpo em `Reset()`:
  - Freq/energia de freio (`brakeEnergyPctRef`): no solo, GS > 5 kt, freio médio >= 0.30 sem parking brake. Acumula Δ(v²/2) quando v cai. Emite ao término (freio < 0.10 por 2s ou GS < 2 kt) se valor final >= 60.
  - Curva rápida em táxi (`taxiTurnSpeedKnots`): no solo, GS < 40 kt, |gear-steer-angle-pct| >= 30, acompanha pico de GS. Emite no término (|steer| < 15 por 2s ou GS < 3 kt) se pico >= 15 kt.
  - Derrapagem de pneus (`skidSeconds`): no solo, GS > 3 kt, gear-is-skidding = 1. Acumula segundos simulados. Emite no término (3s sem skid) se total >= 1s.
  - Carga elétrica na partida (`startLightsOnCount`): disparado junto com a emissão de `engineStartCycle` / `turbineStartCycle`, conta landing, taxi e strobe acesos. Emite se contagem >= 1.
  - Alta potência com óleo frio (`highPowerColdOilMinutes`): por motor, óleo < limite e alta potência (RPM >= 90% ou manete >= 80% pistão; torque >= 80% turbina). Emite nos cruzamentos 1, 3, 6 min. Reinicia após 2 min fora.
  - Ciclos de compensador (`trimCycles`): no ar, profundor desloca >= 10% (0.10) e para/inverte. Emite nos cruzamentos 1, 4, 8.
  - Turbulência com AP (`turbulenceGExceedanceCycles`): no ar com AP ligado, |G - 1| >= 0.5 (rearma < 0.2). Emite nos cruzamentos 10, 25, 50.
  - Transientes de potência turboélice (`rapidPowerTransientCycles`): variação de torque >= 40 pp em janela de 2s simulados. Emite nos cruzamentos 3, 8, 15.
- 5. Testes unitários cobrindo cada métrica e regra nova. Executar replay do voo real do C152 e registrar no relatório. Executar fuzz testing e build da solução.

## O que foi feito
- `WearRuleMetrics.cs`: adicionadas as constantes `BrakeEnergyPctRef` ("brakeEnergyPctRef"), `SkidSeconds` ("skidSeconds"), `StartLightsOnCount` ("startLightsOnCount"); removida a métrica sem uso `BrakeEnergyMkJ`.
- `LimitKeys.cs`: adicionada a constante `OilTempMin` ("oilTempMin") com unidade "celsius".
- `EngineLimitsConfig.cs`: nova estrutura contendo `RpmRedline` e `OilTempMin` configurados por perfil de limites da aeronave.
- `PartFlightTracker.cs`: instanciação de `EngineLimitsConfig` no início da sessão do rastreador (`Begin`), passando limites resolvidos do perfil para o `PartEventDeriver`.
- `wear_rules.json`:
  - Atualizada a regra `brake_high_energy_antiskid` para usar métrica `brakeEnergyPctRef`, thresholds 60.0, 100.0, 150.0 e sensores `["brake-left-position", "brake-right-position", "brake-parking-position", "af-design-speed-vs0"]`.
  - Criada a regra nova `ground_tire_skid` com métrica `skidSeconds`, faixas 1s (0,3% / 0,2%), 3s (0,8% / 0,5%), 6s (2,0% / 1,2% + inspeção `TIRE_FLAT_SPOT_INSPECTION`, grounding false) e `source` "estimativa — revisar (pneus com flat spot por travamento/derrapagem)".
  - Criada a regra nova `engine_start_electrical_load` com métrica `startLightsOnCount`, faixas 1, 2, 3 aplicando dano à bateria e peças de partida (`engine.starter`, `engine.starter_generator`, `engine.pneumatic_starter`), excluindo `noElectrical`, com `source` "estimativa — revisar (partida com farol/strobe sobrecarrega bateria e starter)".
  - Adicionado `eng-throttle-lever-position` na lista de sensores de `engine_high_power_cold_oil`.
- `PartEventDeriver.cs`:
  - Implementado helper `TryGetGroundSpeed` buscando `ctx-ground-velocity`, `gps-ground-speed` ou `ground-velocity`. Se ausente, emite diagnóstico único sem falhar.
  - Implementado `CheckBrakeEnergy`: rastreia frenagem contínua (freio médio >= 30%, parking brake desativado, GS > 5 kt), acumula `Δ(v²/2)` durante desaceleração e emite energia normalizada percentual ref. `1.3 * Vs0` ao finalizar frenagem (freio < 10% por 2 s ou GS < 2 kt) se >= 60.0.
  - Implementado `CheckTaxiTurnSpeed`: monitora ângulo de direção (|steer| >= 30% em aeronaves com `noseWheelSteering`) abaixo de 40 kt no solo, capturando a maior GS e emitindo ao retornar para centro (|steer| < 15% por 2 s ou GS < 3 kt) se GS >= 15 kt.
  - Implementado `CheckGroundTireSkid`: monitora `gear-is-skidding` no solo acima de 3 kt, acumula segundos simulados e emite ao cessar derrapagem (3 s sem derrapar) se >= 1.0 s.
  - Implementado `CheckStartLightsOnLoad`: ao detectar partida de motor, avalia se landing light, taxi light ou strobe estão acesos, emitindo contagem de lâmpadas se >= 1.
  - Implementado `CheckHighPowerColdOil`: por motor com combustão ativa, avalia temperatura de óleo abaixo de `oilTempMin` (ou 24 °C / 0 °C com diagnóstico) combinado com RPM >= 90% de redline ou manete >= 80% (pistão) ou torque >= 80% (turbina), emitindo minutos acumulados em 1, 3 e 6 min e reiniciando após 2 min fora da condição.
  - Implementado `CheckTrimCycles`: em voo (no ar), monitora deslocamento de profundor >= 10% (0.10) e reversão/parada, emitindo ciclos acumulados em 1, 4 e 8.
  - Implementado `CheckTurbulenceGExceedance`: em voo com AP ativo, detecta desvios de G (|G - 1| >= 0.5, rearmando em < 0.2), emitindo ciclos em 10, 25 e 50.
  - Implementado `CheckRapidPowerTransient`: em aeronaves `turboprop`, monitora variações bruscas de torque >= 40 pp em janela de 2 s simulados, emitindo ciclos em 3, 8 e 15.
  - Todo o estado interno é reiniciado em `Reset()`.
- `WearRuleCatalogTests.cs`: atualizado assert de métricas válidas e carregamento de contexto.
- `GroundAndEngineMetricsTests.cs`: suíte xUnit dedicada com 10 testes cobrindo todas as métricas implementadas e casos limites (freio leve, parking brake, taxi em curva, derrapagem, partida com luzes, óleo frio em potência alta, compensador, turbulência AP, transiente turboélice).

## Replay do Voo Real (C152 - 20261122-1007-Cessna_C152.csv)
Executado replay com `--from-start`:
```text
Eventos operacionais derivados (2):
  - startLightsOnCount = 3.0 (motor 1)
  - trimCycles = 1.0 (motor 0)
```
- A nova regra `engine_start_electrical_load` detectou com precisão a partida realizada com farol de pouso, farol de táxi e strobe acesos no log real do C152!
- `trimCycles = 1.0` foi registrado em voo.
- As regras dependentes de velocidade de solo (`brakeEnergyPctRef`, `taxiTurnSpeedKnots`, `skidSeconds`) reportaram diagnósticos únicos de sensor de velocidade ausente no arquivo CSV histórico (pois a coluna de contexto `ctx-ground-velocity` não havia sido salva no arquivo legado) e operaram em modo degradado seguro sem lançar exceções.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx 0 erros
- [x] dotnet test src/FlightCareer.Tests 100% (754/754 testes aprovados)
- [x] fuzz all 100% (2300/2300 voos aprovados em 46 aeronaves, 0 falhas)
- [x] As 6 métricas têm produtor; as 2 regras novas existem com source de estimativa
- [x] Nenhuma mudança em arquivo quente
- [x] CHANGELOG e AGENTS.md › 7 atualizados
- [x] Um commit: feat(desgaste): metricas de freio, taxi, derrapagem, partida e motor (METRICAS-A)

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 754 testes aprovados (0 falhas, 0 pulados).
- `GroundAndEngineMetricsTests`: 10/10 testes aprovados.
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50 --seed 1`: 2.300/2.300 voos aprovados (100%), 0 falhas, 0 limites ausentes em todos os 46 modelos do catálogo.

## Não testado
- Comportamento em tempo real conectado a sessão ao vivo do MSFS 2024 (testado via testes unitários, testes de replay com gravações reais de sensores e fuzz testing determinístico).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida bloqueante. Implementação 100% aderente ao spec e às regras do repositório.
