# LIMITES-V4 — Preencher limites faltantes do catálogo com aircraft_limits-v4.json

**Branch:** feat/limites-v4 · **Commit:** 98bcad7 · **Data:** 2026-11-17

## Plano (antes de codar)
1. Inspecionar as 30 aeronaves de `aircraft_limits-v4.json` e mapear cada uma para os 46 `aircraftId` do catálogo em `src/FlightCareer.Infrastructure/Data/aircraft/`.
2. Identificar aeronaves sem correspondência no catálogo (8) e com correspondência duvidosa (3), deixando-as fora da importação.
3. Extrair os campos válidos do v4 convertendo para o vocabulário fechado de `LimitKeys.cs` e `_schema.json`, ignorando campos sem chave no esquema (pesos, ventos, tetos, altitudes de pista, limites parciais de flap).
4. Gerar linhas em `tools/aircraft_limits/poh_values.csv` exclusivamente para (aeronave, chave) ausentes ou com `source: ClassDefault`, sem jamais sobrescrever POH, SimVar, Cfg ou NativeOnset existentes.
5. Executar `merge_limits.py` (dry-run e real), validar `git diff`, compilação, suíte de testes xUnit, 920 voos no Fuzz Testing determinístico e testes unitários Python.

## O que foi feito
- `tools/aircraft_limits/poh_values.csv`: adicionadas 130 linhas de limites operacionais para 17 aeronaves mapeadas a partir de `aircraft_limits-v4.json`, utilizando a referência padronizada:
  `"aircraft_limits-v4.json (usuário, 2026-11-17; declara <operational_manual>; não conferido no POH)"`.
- `src/FlightCareer.Infrastructure/Data/aircraft/*.json` (17 arquivos atualizados via `merge_limits.py`, com incremento de `dataVersion`):
  - `737-max-8.json`: substituídos ClassDefault de `vne` (340 kt), `vfe:1` (250 kt), `oilTempMax` (140 °C), `ittMax` (1060 °C); preenchidos ausentes `vs0` (115 kt), `vs1` (148 kt), `vlo` (270 kt), `vfe:4` (215 kt), `vfe:6` (190 kt), `vfe:7` (175 kt), `gLimitPos` (2.5 g), `gLimitNeg` (-1.0 g).
  - `a320neo.json`: substituídos ClassDefault de `oilTempMax` (140 °C) e `ittMax` (1060 °C); preenchidos ausentes `vs0` (112 kt), `vs1` (145 kt).
  - `a330-belugaxl.json`: substituídos ClassDefault de `vne` (330 kt), `oilTempMax` (160 °C), `ittMax` (900 °C); preenchidos ausentes `vs0` (114 kt), `vs1` (148 kt), `vlo` (250 kt), `vfe:2` (196 kt), `gLimitPos` (2.5 g), `gLimitNeg` (-1.0 g).
  - `asobo-tbm-930.json`: preenchido ausente `mmo` (0.65 mach).
  - `atr-42-600.json`: substituídos ClassDefault de `vne` (250 kt), `vfe:1` (170 kt), `vfe:2` (150 kt), `oilTempMax` (125 °C), `ittMax` (800 °C); preenchidos ausentes `vs0` (81 kt), `vs1` (104 kt), `mmo` (0.55 mach), `vle` (170 kt), `vlo` (170 kt), `gLimitPos` (2.5 g), `gLimitNeg` (-1.0 g).
  - `atr-72-600.json`: substituídos ClassDefault de `vne` (250 kt), `vfe:1` (185 kt), `vfe:2` (150 kt), `oilTempMax` (125 °C), `ittMax` (800 °C); preenchidos ausentes `vs0` (85 kt), `vs1` (113 kt), `mmo` (0.55 mach), `vle` (170 kt), `vlo` (170 kt), `gLimitPos` (2.5 g), `gLimitNeg` (-1.0 g).
  - `beechcraft-baron-g58.json`: substituídos ClassDefault de `vne` (223 kt), `vfe:1` (152 kt), `vfe:2` (122 kt), `rpmRedline` (2700 rpm), `oilTempMax` (116 °C), `chtMax` (238 °C); preenchidos ausentes `vno` (195 kt), `vs0` (75 kt), `vs1` (84 kt), `vle` (152 kt), `vlo` (152 kt), `gLimitPos` (3.8 g), `gLimitNeg` (-1.52 g).
  - `beechcraft-bonanza-g36.json`: preenchido ausente `vlo` (154 kt).
  - `beechcraft-king-air-c90gtx.json`: substituídos ClassDefault de `vne` (226 kt), `vfe:1` (184 kt), `vfe:2` (148 kt), `oilTempMax` (105 °C), `ittMax` (805 °C); preenchidos ausentes `va` (169 kt), `vs0` (78 kt), `vs1` (88 kt), `mmo` (0.46 mach), `vle` (182 kt), `vlo` (182 kt), `gLimitPos` (3.72 g), `gLimitNeg` (-1.49 g), `rpmRedline` (1900 rpm).
  - `beechcraft-king-air.json`: substituídos ClassDefault de `oilTempMax` (110 °C) e `ittMax` (820 °C); preenchidos ausentes `gLimitPos` (3.17 g), `gLimitNeg` (-1.27 g), `rpmRedline` (1700 rpm).
  - `c208b.json`: substituídos ClassDefault de `vne` (175 kt), `vfe:1` (175 kt), `vfe:2` (150 kt), `vfe:3` (125 kt), `oilTempMax` (105 °C), `ittMax` (850 °C); preenchidos ausentes `va` (148 kt), `vs0` (61 kt), `vs1` (78 kt), `gLimitPos` (3.8 g), `gLimitNeg` (-1.52 g), `rpmRedline` (1900 rpm).
  - `cessna-citation-cj4.json`: substituídos ClassDefault de `oilTempMax` (135 °C) e `ittMax` (855 °C); preenchidos ausentes `vs0` (85 kt), `vs1` (109 kt), `vlo` (200 kt), `gLimitPos` (3.8 g), `gLimitNeg` (-1.52 g).
  - `diamond-da40ng.json`: preenchido ausente `va` (112 kt).
  - `diamond-da62.json`: preenchido ausente `va` (136 kt).
  - `nxcub.json`: substituído ClassDefault de `rpmRedline` (2700 rpm); preenchidos ausentes `vno` (108 kt), `va` (93 kt), `gLimitPos` (3.8 g), `gLimitNeg` (-1.52 g).
  - `pc-12-ngx.json`: substituídos ClassDefault de `oilTempMax` (105 °C) e `ittMax` (850 °C); preenchidos ausentes `va` (163 kt), `mmo` (0.48 mach), `vlo` (180 kt), `gLimitPos` (3.4 g), `gLimitNeg` (-1.36 g), `rpmRedline` (1700 rpm).
  - `pilatus-pc-24.json`: substituídos ClassDefault de `vne` (290 kt), `vfe:1` (200 kt), `vfe:2` (200 kt), `vfe:3` (140 kt), `oilTempMax` (135 °C), `ittMax` (855 °C); preenchidos ausentes `vs0` (81 kt), `vs1` (105 kt), `mmo` (0.74 mach), `vle` (200 kt), `vlo` (200 kt), `gLimitPos` (2.68 g), `gLimitNeg` (-1.0 g).
- `AGENTS.md` (seção 7 › Catálogo de limites): adicionada linha explicativa sobre a fonte `aircraft_limits-v4.json` não conferida no POH.
- `docs/CHANGELOG.md`: adicionada entrada da tarefa LIMITES-V4 no final do arquivo.

## Tabela de Mapeamento das 30 Aeronaves do v4

| ICAO | Nome no v4 | Status de Mapeamento | aircraftId no Catálogo | Chaves Preenchidas (Novas ou Subst. ClassDefault) | Divergências com POH Existente (> 5%) |
|---|---|---|---|---|---|
| A20N | Airbus A320neo | Mapeada | `A320neo` | `vs0`, `vs1`, `oilTempMax`, `ittMax` | Nenhuma |
| C152 | Cessna 152 | Mapeada | `Cessna C152` | Nenhuma (perfil já 100% POH/SimVar) | Nenhuma |
| C172 | Cessna 172S Skyhawk G1000 | Mapeada | `C172SP G1000` | Nenhuma (perfil já 100% POH/SimVar) | Nenhuma |
| A339 | Airbus A330-900neo | Sem correspondência | — (não criar perfil) | — | — |
| B38M | Boeing 737 MAX 8 | Mapeada | `737 Max 8` | `vne`, `vfe:1`, `vfe:4`, `vfe:6`, `vfe:7`, `vs0`, `vs1`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| AT46 | ATR 42-600 | Mapeada | `ATR 42-600` | `vne`, `vfe:1`, `vfe:2`, `vs0`, `vs1`, `mmo`, `vle`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| AT76 | ATR 72-600 | Mapeada | `ATR 72-600` | `vne`, `vfe:1`, `vfe:2`, `vs0`, `vs1`, `mmo`, `vle`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| BLCF | Boeing 747-400LCF Dreamlifter | Sem correspondência | — (não criar perfil) | — | — |
| A337 | Airbus A330-743L BelugaXL | Mapeada | `A330-BelugaXL` | `vne`, `vfe:2`, `vs0`, `vs1`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| PC24 | Pilatus PC-24 | Mapeada | `Pilatus PC-24` | `vne`, `vfe:1`, `vfe:2`, `vfe:3`, `vs0`, `vs1`, `mmo`, `vle`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| C9GT | Beechcraft King Air C90 GTX | Mapeada | `Beechcraft King Air C90GTx` | `vne`, `vfe:1`, `vfe:2`, `va`, `vs0`, `vs1`, `mmo`, `vle`, `vlo`, `gLimitPos`, `gLimitNeg`, `rpmRedline`, `oilTempMax`, `ittMax` | Nenhuma |
| BE36 | Beechcraft Bonanza G36 | Mapeada | `Beechcraft Bonanza G36` | `vlo` | Nenhuma |
| B350 | Beechcraft King Air 350i | Mapeada | `Beechcraft King Air` | `gLimitPos`, `gLimitNeg`, `rpmRedline`, `oilTempMax`, `ittMax` | `vne`: POH atual 245 kt vs v4 263 kt (+7,3%) — mantido POH |
| C208 | Cessna 208B Grand Caravan EX | Mapeada | `C208B` | `vne`, `vfe:1`, `vfe:2`, `vfe:3`, `va`, `vs0`, `vs1`, `gLimitPos`, `gLimitNeg`, `rpmRedline`, `oilTempMax`, `ittMax` | Nenhuma |
| C25C | Cessna Citation CJ4 | Mapeada | `Cessna Citation CJ4` | `vs0`, `vs1`, `vlo`, `gLimitPos`, `gLimitNeg`, `oilTempMax`, `ittMax` | Nenhuma |
| SF50 | Cirrus Vision Jet G2 | Sem correspondência | — (não criar perfil) | — | — |
| CC19 | CubCrafters NXCub | Mapeada | `NXCub` | `vno`, `va`, `gLimitPos`, `gLimitNeg`, `rpmRedline` | Nenhuma |
| CC19_TW | CubCrafters XCub | Sem correspondência | — (não criar perfil) | — | — |
| TBM9 | Daher TBM 930 | Mapeada | `Asobo TBM 930` | `mmo` | Nenhuma |
| DA40 | Diamond DA40 NG | Mapeada | `Diamond DA40NG` | `va` | Nenhuma |
| DA62 | Diamond DA62 | Mapeada | `Diamond DA62` | `va` | `vle`: POH atual 201 kt vs v4 156 kt (-22,4%) — mantido POH |
| CTSL | Flight Design CTSL | Correspondência duvidosa | — (CTSL × CT) | — | — |
| ICON | ICON A5 | Sem correspondência | — (não criar perfil) | — | — |
| VL3 | JMB Aircraft VL-3 | Sem correspondência | — (não criar perfil) | — | — |
| PC6T | Pilatus PC-6 Porter | Sem correspondência | — (não criar perfil) | — | — |
| PC12 | Pilatus PC-12 NGX | Mapeada | `PC-12 NGX` | `va`, `mmo`, `vlo`, `gLimitPos`, `gLimitNeg`, `rpmRedline`, `oilTempMax`, `ittMax` | Nenhuma |
| CP10 | Robin CAP 10 | Correspondência duvidosa | — (CAP 10 × CAP-4) | — | — |
| DR40 | Robin DR400/100 Cadet | Correspondência duvidosa | — (DR400/100 × DR400) | — | — |
| SAVG | Zlin Aviation Savage Cub | Sem correspondência | — (não criar perfil) | — | — |
| BE58 | Beechcraft Baron G58 | Mapeada | `Beechcraft Baron G58` | `vne`, `vfe:1`, `vfe:2`, `vno`, `vs0`, `vs1`, `vle`, `vlo`, `gLimitPos`, `gLimitNeg`, `rpmRedline`, `oilTempMax`, `chtMax` | Nenhuma |

## Campos do v4 Ignorados (Sem Chave no Esquema)
Conforme especificação, os seguintes grupos de campos presentes em `aircraft_limits-v4.json` foram ignorados por não pertencerem ao vocabulário fechado do esquema (`_schema.json` / `LimitKeys.cs`):
- **Pesos (`weights_kg.*`):** `empty_operating_min`, `max_ramp_wt`, `max_takeoff_mtow`, `max_landing_mlw`, `max_baggage_kg`, `max_taxi_mtw`, `max_zero_fuel_mzfw`.
- **Condições de vento e pista (`wind_and_runway.*`):** `max_demonstrated_crosswind_kt`, `max_tailwind_kt`, `max_crosswind_takeoff_kt`, `max_crosswind_landing_kt`, `max_crosswind_autoland_kt`, `max_runway_slope_pct`, `min_runway_width_m`, `unpaved_runway_certified`, `water_operations_certified`.
- **Altitudes e teto operacional (`envelope_and_altitude.*`):** `service_ceiling_ft`, `max_altitude_flaps_extended_ft`, `max_runway_altitude_ft`, `min_runway_altitude_ft`.
- **Fatores de carga parciais:** `g_load_flaps.*`, `g_load_clean_utility.*`, `g_load_clean_aerobatic.*` (o esquema adota exclusivamente os limites normais limpos `gLimitPos` e `gLimitNeg`).
- **Velocidades de subida e pneus:** `vx_best_angle_climb`, `vy_best_rate_climb`, `max_tire_ground_speed_kt`.
- **Sistemas / Motor auxiliares:** `fuel_capacity_usable_l`, `min_oil_temp_c`, `oil_pressure_psi.normal_min`, `oil_pressure_psi.normal_max`, `oil_pressure_psi.max`, `max_oil_temp_transient_c`, `max_reverse_min_speed_kt`, `autopilot_min_agl_ft.*`, `max_egt_start_*`, `max_itt_start_*`, `max_egt_mct_c`, `max_itt_mct_c`, `max_prop_rpm_pct`.

## Prova de Preservação de POH Existentes
A execução de script de conferência (`git diff`) confirmou 0 alterações em linhas com `source: "POH"` ou `source: "SimVar"`. Todos os 118 registros pré-existentes em `poh_values.csv` permaneceram intactos, assim como todos os limites de fonte POH já carregados nos JSONs.

## Critérios de aceite
- [x] Relatório com tabela completa: 30 aeronaves v4 → aircraftId (19 mapeadas, 8 sem correspondência, 3 duvidosas), chaves preenchidas, chaves ignoradas por grupo, e divergências com POH existente documentadas.
- [x] Nenhum valor POH existente alterado (conferido via diff contra `src/FlightCareer.Infrastructure/Data/aircraft/`).
- [x] Entrada no fim de `docs/CHANGELOG.md` (`## 2026-11-17 - Limites do aircraft_limits-v4 nos perfis (Gemini)`).
- [x] `AGENTS.md` › 7 (Catálogo de limites) atualizado com linha sobre a fonte v4 não conferida.
- [x] Um commit: `feat(limits): limites faltantes dos perfis a partir do aircraft_limits-v4 (LIMITES-V4)`.
- [x] Apenas fatos conferidos relatados (sem hipóteses não checadas).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 warnings conhecidos de pacotes herdados.
- `dotnet test src/FlightCareer.Tests`: 577 aprovados, 0 falhas (100% de aprovação).
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`: 920 voos procedurais executados em todas as 46 aeronaves do catálogo; 920 aprovados (100%), 0 limites ausentes, 0 falhas (tempo: 368,21 s).
- `python -m unittest discover -s tools/tests`: 48 aprovados em 0,163 s (OK).

## Não testado
- Comportamento em voo manual com MSFS 2024 conectado (não dependente de código C#, apenas catálogo estático de dados).

## Dúvidas e decisões para o revisor
- Decisão sobre as 3 aeronaves duvidosas mantida conforme instrução (`CTSL`, `CP10`, `DR40` não foram importadas). Caso o usuário deseje adotar os limites da variante CTSL no Flight Design CT ou do DR400/100 no DR400, basta mapeá-las em rodada posterior.
