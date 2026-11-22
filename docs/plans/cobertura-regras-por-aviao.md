# Relatório de Cobertura das Regras de Desgaste por Aeronave

> Relatório gerado automaticamente cobrindo os 46 modelos do catálogo contra as 47 regras de desgaste.

## 0. Comparativo Antes / Depois (LIMITES-FALTANTES)

| Métrica | Antes (Fase Inicial) | Depois (LIMITES-FALTANTES) | Variação |
|---|---|---|---|
| **Média Ativas Geral** | 30,3 | 32,6 | +2,3 |
| **Média Inativas (Falta Parâm.) Geral** | 4,5 | 2,2 | -2,3 |
| **Média Não Aplicáveis (Feature) Geral** | 12,1 | 12,2 | +0,1 |

## 1. Resumo por Classe

| Classe | Aeronaves | Média Ativas | Média Inativas (Falta Parâm.) | Média Não Aplicáveis (Feature) |
|---|---|---|---|---|
| **Jet** | 15 | 32,4 | 3,6 | 11,0 |
| **Piston** | 19 | 30,6 | 1,5 | 14,9 |
| **Turboprop** | 12 | 35,9 | 1,8 | 9,3 |
| **Total Geral** | **46** | **32,6** | **2,2** | **12,2** |

## 2. Visão Geral dos 46 Modelos

| Modelo | Classe | Ativas | Inativas (Falta Parâm.) | Não Aplicáveis (Feature) | Total Regras |
|---|---|---|---|---|---|
| `737 Max 8` | Jet | 34 | 2 | 11 | 47 |
| `747-8i` | Jet | 32 | 4 | 11 | 47 |
| `A310` | Jet | 33 | 3 | 11 | 47 |
| `A320neo` | Jet | 34 | 2 | 11 | 47 |
| `A321` | Jet | 33 | 3 | 11 | 47 |
| `A330-200` | Jet | 32 | 4 | 11 | 47 |
| `A330-BelugaXL` | Jet | 34 | 2 | 11 | 47 |
| `A400M` | Turboprop | 36 | 3 | 8 | 47 |
| `Airbus A321LR` | Jet | 31 | 5 | 11 | 47 |
| `Airbus A330-300` | Jet | 31 | 5 | 11 | 47 |
| `Asobo TBM 930` | Turboprop | 38 | 1 | 8 | 47 |
| `ATR 42-600` | Turboprop | 38 | 2 | 7 | 47 |
| `ATR 72-600` | Turboprop | 38 | 2 | 7 | 47 |
| `Beechcraft Baron G58` | Piston | 34 | 1 | 12 | 47 |
| `Beechcraft Bonanza` | Piston | 30 | 4 | 13 | 47 |
| `Beechcraft Bonanza G36` | Piston | 33 | 1 | 13 | 47 |
| `Beechcraft Bonanza V35` | Piston | 30 | 4 | 13 | 47 |
| `Beechcraft King Air` | Turboprop | 38 | 1 | 8 | 47 |
| `Beechcraft King Air 250` | Turboprop | 35 | 4 | 8 | 47 |
| `Beechcraft King Air C90GTx` | Turboprop | 38 | 1 | 8 | 47 |
| `Boeing 747-400` | Jet | 31 | 5 | 11 | 47 |
| `Boeing 747-400 Global Supertanker` | Jet | 31 | 5 | 11 | 47 |
| `Boeing 787-10 Dreamliner` | Jet | 31 | 5 | 11 | 47 |
| `C172SP G1000` | Piston | 31 | 0 | 16 | 47 |
| `C208B` | Turboprop | 32 | 1 | 14 | 47 |
| `C400 Corvalis` | Piston | 31 | 2 | 14 | 47 |
| `CAP-4` | Piston | 27 | 0 | 20 | 47 |
| `Cessna 188 AGTruck` | Piston | 28 | 2 | 17 | 47 |
| `Cessna 207T Stationair` | Piston | 30 | 2 | 15 | 47 |
| `Cessna 404 Titan` | Piston | 32 | 4 | 11 | 47 |
| `Cessna 408 SkyCourier` | Turboprop | 31 | 2 | 14 | 47 |
| `Cessna C152` | Piston | 30 | 0 | 17 | 47 |
| `Cessna Citation CJ4` | Jet | 34 | 2 | 11 | 47 |
| `Cessna Citation Longitude` | Jet | 31 | 5 | 11 | 47 |
| `Cirrus SR22` | Piston | 30 | 1 | 16 | 47 |
| `DHC-6-300 Twin` | Turboprop | 32 | 1 | 14 | 47 |
| `Diamond DA40NG` | Piston | 33 | 1 | 13 | 47 |
| `Diamond DA62` | Piston | 37 | 1 | 9 | 47 |
| `ES30` | Turboprop | 37 | 2 | 8 | 47 |
| `Flight Design CT` | Piston | 30 | 1 | 16 | 47 |
| `NXCub` | Piston | 30 | 1 | 16 | 47 |
| `PC-12 NGX` | Turboprop | 38 | 1 | 8 | 47 |
| `Pilatus PC-24` | Jet | 34 | 2 | 11 | 47 |
| `Piper J-3 Cub` | Piston | 26 | 1 | 20 | 47 |
| `Piper PA28-236 Dakota` | Piston | 30 | 1 | 16 | 47 |
| `Robin DR400` | Piston | 30 | 1 | 16 | 47 |

## 3. Detalhamento por Aeronave

### `737 Max 8` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `747-8i` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 32 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A310` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 33 | **Inativas (Falta Parâm.):** 3 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A320neo` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A321` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 33 | **Inativas (Falta Parâm.):** 3 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A330-200` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 32 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A330-BelugaXL` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `A400M` (Turboprop)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, constantSpeedProp, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, multiEngine, noseWheelSteering, oxygen, pressurized, propDeIce, rat, retractableGear, slats, spoilers, thsTrim, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 36 | **Inativas (Falta Parâm.):** 3 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]

</details>

---

### `Airbus A321LR` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Airbus A330-300` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Asobo TBM 930` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `ATR 42-600` (Turboprop)

- **Features:** `antiskid, autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 7

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (7)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]

</details>

---

### `ATR 72-600` (Turboprop)

- **Features:** `antiskid, autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 7

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (7)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]

</details>

---

### `Beechcraft Baron G58` (Piston)

- **Features:** `autopilot, constantSpeedProp, deIceBoots, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, weatherRadar`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 12

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (12)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Beechcraft Bonanza` (Piston)

- **Features:** `autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 13

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (13)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Beechcraft Bonanza G36` (Piston)

- **Features:** `autopilot, constantSpeedProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine`
- **Regras Ativas:** 33 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 13

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (13)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Beechcraft Bonanza V35` (Piston)

- **Features:** `autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 13

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (13)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Beechcraft King Air` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `Beechcraft King Air 250` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 35 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `Beechcraft King Air C90GTx` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `Boeing 747-400` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Boeing 747-400 Global Supertanker` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Boeing 787-10 Dreamliner` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `C172SP G1000` (Piston)

- **Features:** `autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 0 | **Não Aplicáveis (Feature):** 16

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `avionics_start_with_avionics_master_on`, `battery_deep_discharge_damage`, `electrical_system_overload_amps`, `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `C208B` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fixedGear, glassCockpit, noseWheelSteering, oxygen, passengerCabin, propDeIce, singleEngine, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 32 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 14

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (14)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `C400 Corvalis` (Piston)

- **Features:** `autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, spoilers, turbocharged`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 14

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbo_overboost_tit`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (14)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `CAP-4` (Piston)

- **Features:** `carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel`
- **Regras Ativas:** 27 | **Inativas (Falta Parâm.):** 0 | **Não Aplicáveis (Feature):** 20

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`

<details>
<summary>Regras não aplicáveis por feature (20)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `flap_overspeed_vfe`: aeronave sem flaps (feature 'noFlaps')
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `taxi_high_speed_turn`: requer feature(s) obrigatória(s) [noseWheelSteering]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `avionics_start_with_avionics_master_on`: incompatível com feature(s) [noElectrical]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]
- `engine_start_electrical_load`: incompatível com feature(s) [noElectrical]

</details>

---

### `Cessna 188 AGTruck` (Piston)

- **Features:** `constantSpeedProp, fixedGear, fuelInjected, piston, singleEngine, tailwheel`
- **Regras Ativas:** 28 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 17

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (17)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `taxi_high_speed_turn`: requer feature(s) obrigatória(s) [noseWheelSteering]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Cessna 207T Stationair` (Piston)

- **Features:** `autopilot, constantSpeedProp, convertibleCabin, fixedGear, fuelInjected, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, turbocharged`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 15

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbo_overboost_tit`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (15)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Cessna 404 Titan` (Piston)

- **Features:** `autopilot, constantSpeedProp, convertibleCabin, deIceBoots, fireProtection, fuelInjected, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar, wipers`
- **Regras Ativas:** 32 | **Inativas (Falta Parâm.):** 4 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `turbo_overboost_tit`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Cessna 408 SkyCourier` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fireProtection, fixedGear, fms, glassCockpit, iceDetector, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 14

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (14)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Cessna C152` (Piston)

- **Features:** `carbureted, fixedGear, fixedPitchProp, noAvionicsMaster, noseWheelSteering, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 0 | **Não Aplicáveis (Feature):** 17

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage`, `electrical_system_overload_amps`, `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (17)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `avionics_start_with_avionics_master_on`: incompatível com feature(s) [noAvionicsMaster]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Cessna Citation CJ4` (Jet)

- **Features:** `antiskid, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, retractableGear, spoilers, weatherRadar, wipers`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Cessna Citation Longitude` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, thsTrim, weatherRadar, wipers`
- **Regras Ativas:** 31 | **Inativas (Falta Parâm.):** 5 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_extended_overspeed_vle`** (Continuous): limite operacional 'vle' ausente no perfil e nos padrões de classe
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Cirrus SR22` (Piston)

- **Features:** `autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 16

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `DHC-6-300 Twin` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fixedGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, skis, turboprop, wipers`
- **Regras Ativas:** 32 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 14

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (14)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Diamond DA40NG` (Piston)

- **Features:** `autopilot, constantSpeedProp, diesel, fadec, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine, turbocharged`
- **Regras Ativas:** 33 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 13

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `turbo_overboost_tit`, `coolant_overheat`, `diesel_glow_plug_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (13)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Diamond DA62` (Piston)

- **Features:** `autopilot, constantSpeedProp, deIceBoots, diesel, fadec, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar`
- **Regras Ativas:** 37 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 9

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `turbo_overboost_tit`, `coolant_overheat`, `diesel_glow_plug_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (9)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `ES30` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, deIceBoots, electricHybrid, fireProtection, fms, galley, glassCockpit, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 37 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`gear_operation_overspeed_vlo`** (Continuous): limite operacional 'vlo' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `Flight Design CT` (Piston)

- **Features:** `autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 16

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `NXCub` (Piston)

- **Features:** `autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 16

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `PC-12 NGX` (Turboprop)

- **Features:** `autopilot, bleedAir, constantSpeedProp, convertibleCabin, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, lavatory, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers`
- **Regras Ativas:** 38 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 8

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turboprop_torque_exceedance`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `structural_deice_boot_cycles`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `turboprop_rapid_power_lever_movement`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (8)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]

</details>

---

### `Pilatus PC-24` (Jet)

- **Features:** `antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, weatherRadar, wipers`
- **Regras Ativas:** 34 | **Inativas (Falta Parâm.):** 2 | **Não Aplicáveis (Feature):** 11

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`speedbrake_airspeed_vno_overspeed`** (Continuous): limite operacional 'vno' ausente no perfil e nos padrões de classe
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`oil_temperature_high`, `oil_pressure_low`, `turbine_itt_overshoot`, `turbine_n1_overspeed`, `flap_overspeed_vfe`, `gear_extended_overspeed_vle`, `gear_operation_overspeed_vlo`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `gear_cycle_stress`, `pressurization_cycle_stress`, `turbine_start_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `pressurization_cycle_fuselage_stress`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `cabin_rapid_decompression_stress`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (11)</summary>

- `piston_rpm_overspeed`: requer feature(s) obrigatória(s) [piston]
- `piston_cht_overheat`: requer feature(s) obrigatória(s) [piston]
- `piston_shock_cooling`: requer feature(s) obrigatória(s) [piston]
- `piston_egt_excess`: requer feature(s) obrigatória(s) [piston]
- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `engine_starter_duty_cycle`: requer ao menos uma feature dentre [piston, turboprop]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]

</details>

---

### `Piper J-3 Cub` (Piston)

- **Features:** `carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel`
- **Regras Ativas:** 26 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 20

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`control_surface_abrupt_above_va`** (Continuous): limite operacional 'va' ausente no perfil e nos padrões de classe

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`

<details>
<summary>Regras não aplicáveis por feature (20)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `flap_overspeed_vfe`: aeronave sem flaps (feature 'noFlaps')
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `taxi_high_speed_turn`: requer feature(s) obrigatória(s) [noseWheelSteering]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `avionics_start_with_avionics_master_on`: incompatível com feature(s) [noElectrical]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]
- `engine_start_electrical_load`: incompatível com feature(s) [noElectrical]

</details>

---

### `Piper PA28-236 Dakota` (Piston)

- **Features:** `autopilot, carbureted, constantSpeedProp, fixedGear, noseWheelSteering, passengerCabin, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 16

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

### `Robin DR400` (Piston)

- **Features:** `carbureted, fixedGear, fixedPitchProp, noseWheelSteering, passengerCabin, piston, singleEngine`
- **Regras Ativas:** 30 | **Inativas (Falta Parâm.):** 1 | **Não Aplicáveis (Feature):** 16

#### ⚠️ Regras que se aplicariam mas estão inativas (limite ou parâmetro ausente):
- **`avionics_start_with_avionics_master_on`** (Event): parâmetro 'elecAvionicsCircuitIndex' ausente no catálogo da aeronave

#### ✅ Regras Ativas:
`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_shock_cooling`, `piston_egt_excess`, `oil_temperature_high`, `oil_pressure_low`, `flap_overspeed_vfe`, `airframe_over_g_positive`, `airframe_over_g_negative`, `vne_airspeed_exceedance`, `touchdown_hard_landing`, `engine_starter_duty_cycle`, `control_surface_abrupt_above_va`, `trim_actuator_cycle_stress`, `structural_icing_exposure`, `touchdown_tailstrike`, `touchdown_wingtip_strike`, `touchdown_crosswind_crab`, `taxi_high_speed_turn`, `landing_cycle_structural_stress`, `brake_high_energy_antiskid`, `battery_deep_discharge_damage` (detecção em voo), `electrical_system_overload_amps` (detecção em voo), `autopilot_turbulence_servo_stress`, `fuel_pump_dry_run_empty_tank`, `hydraulic_low_pressure_stress`, `engine_high_power_cold_oil`, `propeller_deice_boot_cycles`, `ground_tire_skid`, `engine_start_electrical_load`

<details>
<summary>Regras não aplicáveis por feature (16)</summary>

- `turboprop_torque_exceedance`: requer feature(s) obrigatória(s) [turboprop]
- `turbine_itt_overshoot`: requer ao menos uma feature dentre [turboprop, jet]
- `turbine_n1_overspeed`: requer ao menos uma feature dentre [turboprop, jet]
- `gear_extended_overspeed_vle`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_operation_overspeed_vlo`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `gear_cycle_stress`: requer ao menos uma feature dentre [retractableGear, hydraulicGear]
- `pressurization_cycle_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turbo_overboost_tit`: requer feature(s) obrigatória(s) [turbocharged]
- `coolant_overheat`: requer feature(s) obrigatória(s) [diesel]
- `turbine_start_cycle`: requer ao menos uma feature dentre [turboprop, jet]
- `diesel_glow_plug_cycle`: requer feature(s) obrigatória(s) [diesel]
- `structural_deice_boot_cycles`: requer feature(s) obrigatória(s) [deIceBoots]
- `speedbrake_airspeed_vno_overspeed`: requer feature(s) obrigatória(s) [spoilers]
- `pressurization_cycle_fuselage_stress`: requer feature(s) obrigatória(s) [pressurized]
- `turboprop_rapid_power_lever_movement`: requer feature(s) obrigatória(s) [turboprop]
- `cabin_rapid_decompression_stress`: requer feature(s) obrigatória(s) [pressurized]

</details>

---

