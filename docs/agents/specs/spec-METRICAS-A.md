# Spec METRICAS-A — Métricas de solo, partida e motor que as regras usam mas ninguém calcula

**Objetivo (1 frase):** calcular no `PartEventDeriver` as métricas das regras de freio, táxi em curva, óleo frio em potência alta, compensador, turbulência com piloto automático e transiente de potência em turboélice, e criar as regras de derrapagem e de partida com luzes acesas, para que esses abusos gerem desgaste em qualquer avião.

## Fatos já levantados (não redescobrir)
- Voo real com abusos (C152, 2026-11-22, REC `%LocalAppData%\FlightCareer\sensor-logs\20261122-1007-Cessna_C152.csv`): táxi rápido na terra com derrapagens, frenagem forte e partida com todas as luzes acesas **não geraram nenhum desgaste**. Replay: `dotnet run --project src/FlightCareer.Replay -- <csv>`.
- Das 27 métricas de `Domain/Parts/WearRuleMetrics.cs`, 14 são usadas em `Infrastructure/Data/wear_rules.json` sem nenhum código que as produza. Esta tarefa cobre 6 delas: `brakeEnergyMkJ`, `taxiTurnSpeedKnots`, `highPowerColdOilMinutes`, `trimCycles`, `turbulenceGExceedanceCycles`, `rapidPowerTransientCycles`. (As outras 8 ficam para a METRICAS-B.)
- Padrão existente: `PartEventDeriver.Update(SensorFrame frame, double dtSimSeconds, features)` devolve `DerivedPartEvent(metricKey, value, engineIndex)`; o `PartWearEngine.ApplyEvent` aplica a **maior faixa** cujo `threshold` ≤ valor (dano `eventDamagePct` por peça instalada). Métricas acumuladas (minutos, ciclos) seguem `CheckBatteryDeepDischarge` (~linha 213): emitem uma vez em cada cruzamento de threshold, com reset depois de um tempo fora da condição. Use **tempo simulado** (`dtSimSeconds`), nunca o relógio.
- Sensores confirmados no REC do C152: `gear-steer-angle-pct` (−100..100, % do máximo), `gear-is-skidding` e `gear-contact-point-is-skidding-0..2` (0/1; foram a 1 nas derrapagens), `brake-left-position`/`brake-right-position` (unidade **"position 32k"**, 0..32767), `brake-parking-position`, `af-design-speed-vs0` (33 kt no C152), `eng-throttle-lever-position:n` (0..100), luzes `light-landing-on`, `light-taxi-on`, `light-strobe-on`, `light-beacon-on`, `light-nav-on`, `eng-starter-active:n`. Ground speed e on-ground: use os mesmos ids que o `PartFlightTracker`/`PartEventDeriver` já usam. Confirme no `sensor_registry.json` os ids de temperatura do óleo, torque de turbina, posição do compensador, G e piloto automático usados pelas regras (`sensors` de cada regra); se faltar algum, acrescente ao **fim** do registro e ao `docs/plans/sensor-inventory.md` (há teste de paridade).
- A temperatura dos freios quase não sobe no C152 (+10 K), por isso a regra de freio passa a usar energia específica (não precisa de massa).

## Contrato por métrica (implementar no `PartEventDeriver`, lógica pura, com estado reiniciado em `Reset()`)
1. **Freio — trocar a métrica para `brakeEnergyPctRef`** (nova constante em `WearRuleMetrics`; atualizar a regra `brake_high_energy_antiskid` em `wear_rules.json` e o teste `WearRuleCatalogTests` que espera `brakeEnergyMkJ`; remover a constante antiga se ficar sem uso).
   - Ocorrência: no solo, GS > 5 kt, freio aplicado = média de `brake-left/right-position` / 32767 ≥ 0,30, **sem** freio de estacionamento. Durante a ocorrência, a cada quadro em que a GS cai, somar `Δ(v²/2)` com v em m/s (kt × 0,5144). Fim: freio < 0,10 por 2 s simulados ou GS < 2 kt. Emitir **uma vez** por ocorrência `100 × soma / ((1,3 × Vso)² / 2)` (Vso em m/s de `af-design-speed-vs0`; se ausente ou ≤ 0, usar 50 kt e diagnóstico único).
   - Faixas novas da regra: **60** (cautela), **100** (alerta), **150** (grave), mantendo as peças atuais de cada faixa. Peças inexistentes no avião (ex.: `gear.antiskid_system` no C152) são ignoradas pelo motor de desgaste; confirme.
2. **`taxiTurnSpeedKnots`** (regra `taxi_high_speed_turn`, só `noseWheelSteering`): no solo, GS < 40 kt, |`gear-steer-angle-pct`| ≥ 30 → acompanhar a maior GS; fim quando |direção| < 15 por 2 s ou GS < 3 kt → emitir a maior GS se ≥ 15 (menor faixa). Uma emissão por curva.
3. **Regra nova `ground_tire_skid`** (métrica nova `skidSeconds`): no solo, GS > 3 kt e `gear-is-skidding` = 1 → acumular segundos simulados; fim após 3 s sem derrapar → emitir o total se ≥ 1 s. Faixas: 1 s (`tires.main` 0,3 %, `tires.nose` 0,2 %), 3 s (0,8 % / 0,5 %), 6 s (2,0 % / 1,2 % + inspeção `TIRE_FLAT_SPOT_INSPECTION`, não interdita). Sem `appliesTo` (peças ausentes são ignoradas). `source`: "estimativa — revisar (pneus com flat spot por travamento/derrapagem)".
4. **Regra nova `engine_start_electrical_load`** (métrica nova `startLightsOnCount`): no instante em que o deriver já emite `engineStartCycle`, contar quantas destas estão acesas: farol de pouso, farol de táxi, strobe (beacon e nav são normais na partida e **não** contam). Emitir a contagem se ≥ 1. Faixas: 1 (`electrical.battery` 0,3 %, `engine.starter` 0,2 %), 2 (0,6 / 0,5), 3 (1,0 / 0,8). Não se aplica a `noElectrical`. Turbinas: use a peça de partida que o catálogo de peças tiver (confira `parts_catalog.json`; se for outra, liste a do turbina na mesma faixa).
5. **`highPowerColdOilMinutes`** (por motor): temperatura do óleo abaixo do mínimo de operação (limite do catálogo da aeronave se existir, ex. `oilTempMin`; senão 24 °C pistão / 0 °C turbina, com diagnóstico) **e** potência alta (pistão: RPM ≥ 90 % do `rpmRedline` **ou** manete ≥ 80 %; turbina: torque ≥ 80 %) com o motor em combustão → acumular minutos; emitir nos cruzamentos de 1, 3 e 6 min; reiniciar após 2 min fora da condição.
6. **`trimCycles`**: um ciclo = o compensador de profundor anda ≥ 10 % do curso numa direção e depois para ou inverte; contar ciclos no voo (no ar) e emitir nos cruzamentos de 1, 4 e 8.
7. **`turbulenceGExceedanceCycles`**: com piloto automático ligado e no ar, um ciclo = |G − 1| ≥ 0,5 (rearma quando |G − 1| < 0,2); emitir nos cruzamentos de 10, 25 e 50.
8. **`rapidPowerTransientCycles`** (regra só `turboprop`): um ciclo = variação de torque ≥ 40 pontos percentuais dentro de 2 s simulados (subida ou descida); emitir nos cruzamentos de 3, 8 e 15.
- Cada regra só atua se os sensores existirem no frame; senão diagnóstico único (`ReportDiagnosticOnce`), como as regras elétricas.

## Fora do escopo
- As outras 8 métricas (gelo, degelo, diesel, bombas, hidráulico, pressurização, descompressão) — METRICAS-B.
- Arquivos quentes (`FlightMonitor`, `SimConnectService`, `ComponentFlightSession`…). Se precisar ligar algo na UI, **pare e descreva no relatório** em vez de mexer.

## Testes (xUnit, frames sintéticos; nomes sugeridos)
- `Frenagem_forte_do_c152_gera_evento_de_energia`: Vso 33 kt; GS 45 → 5 kt em 8 s com freio 0,8 → emite ≈ 100 × (45²−5²)/(1,3×33)² ≈ 108 (±3).
- `Frenagem_leve_nao_gera_evento` (freio 0,2) e `Freio_de_estacionamento_nao_conta`.
- `Curva_rapida_no_taxi`: direção 60 %, GS 20 kt por 4 s → emite 20; GS 10 kt → nada.
- `Derrapagem_de_4_s_emite_4`; derrapagem a GS 1 kt → nada.
- `Partida_com_farol_e_strobe_emite_2`; só beacon e nav → nada.
- `Oleo_frio_em_potencia_alta_emite_nos_cruzamentos` (1, 3 e 6 min) e reinício após 2 min fora.
- `Ciclos_de_compensador`, `Turbulencia_com_piloto_automatico`, `Transiente_de_torque_em_turboelice` (cruzamentos certos; nada quando a feature/sensor não existe).
- **Replay do voo real:** rodar o replay no REC citado e colar no relatório os eventos novos (frenagem, derrapagem, partida com luzes, curva no táxi). Se o REC não estiver no PC do executor, declare.
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50 --seed 1` continua 100 % (se o oráculo acusar desgaste "inesperado" por causa das regras novas, ajuste o gabarito do gerador com explicação, **sem** relaxar invariantes — `docs/agents/LICOES.md`).

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %; fuzz all 100 %.
- [ ] As 6 métricas têm produtor; as 2 regras novas existem com `source` de estimativa.
- [ ] Nenhuma mudança em arquivo quente.
- [ ] CHANGELOG e AGENTS.md › 7 (Peças no voo: métricas e regras novas). Relatório com o replay e "não testado no MSFS".
- [ ] Um commit: `feat(desgaste): metricas de freio, taxi, derrapagem, partida e motor (METRICAS-A)`.
