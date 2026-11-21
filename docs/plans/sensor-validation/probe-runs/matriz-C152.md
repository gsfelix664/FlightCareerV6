# Matriz SimVarProbe — 20261116-0744-Cessna_C152, 20261116-0750-Cessna_C152, 20261116-0808-Cessna_C152

- Slots: 11118; variam: 748; constantes: 10100; rejeitadas: 270.
- Sensores do registro encontrados na sondagem: 418 de 421 (fora da lista da sonda: 3).

## Sensores usados em regras de desgaste que NÃO variaram

| Slot | Chave | Status | Regras |
|---|---|---|---|
| `AUTOPILOT MASTER` | `af-autopilot-master` | constante | autopilot_turbulence_servo_stress |
| `ANTISKID BRAKES ACTIVE` | `brake-antiskid-active` | constante | brake_high_energy_antiskid |
| `GEAR HANDLE POSITION` | `gear-handle-position` | constante | gear_operation_overspeed_vlo, gear_cycle_stress |
| `GEAR HYDRAULIC PRESSURE` | `gear-hydraulic-pressure` | constante | gear_cycle_stress |
| `GEAR POSITION` | `gear-position` | constante | gear_extended_overspeed_vle, gear_operation_overspeed_vlo |
| `GEAR TOTAL PCT EXTENDED` | `gear-total-pct-extended` | constante | gear_extended_overspeed_vle |
| `NOSEWHEEL MAX STEERING ANGLE` | `gear-nosewheel-max-steering-angle` | constante | taxi_high_speed_turn |
| `AILERON TRIM PCT` | `af-aileron-trim-pct` | constante | trim_actuator_cycle_stress |
| `RUDDER TRIM PCT` | `af-rudder-trim-pct` | constante | trim_actuator_cycle_stress |
| `SPOILERS HANDLE POSITION` | `af-spoilers-handle-position` | constante | speedbrake_airspeed_vno_overspeed |
| `GENERAL ENG FUEL PUMP ON:1` | `fuel-pump-on` | constante | fuel_pump_dry_run_empty_tank |
| `GENERAL ENG FUEL PUMP SWITCH:1` | `fuel-pump-switch` | constante | fuel_pump_dry_run_empty_tank |
| `MAX EGT` | `eng-max-egt` | constante | piston_egt_excess |
| `MAX OIL TEMPERATURE` | `oil-max-temperature` | constante | oil_temperature_high |
| `TURB ENG ITT:1` | `turb-itt` | constante | turbine_itt_overshoot, turbine_start_cycle |
| `TURB ENG MASTER STARTER SWITCH:1` | `turb-master-starter-switch` | constante | turbine_start_cycle |
| `TURB ENG MAX TORQUE PERCENT:1` | `turb-max-torque-percent` | constante | turboprop_torque_exceedance, turboprop_rapid_power_lever_movement |
| `TURB ENG N1:1` | `turb-n1` | constante | turbine_n1_overspeed |
| `TURB MAX ITT` | `turb-max-itt` | constante | turbine_itt_overshoot |
| `RECIP ENG COOLANT RESERVOIR PERCENT:1` | `eng-coolant-reservoir-percent` | constante | coolant_overheat |
| `RECIP MAX CHT` | `eng-max-cht` | constante | piston_cht_overheat |
| `MIN G FORCE` | `af-min-g-force` | constante | airframe_over_g_negative |
| `STRUCTURAL ICE PCT` | `inst-structural-ice-pct` | constante | structural_icing_exposure, structural_deice_boot_cycles |
| `WARNING OIL PRESSURE` | `oil-warning-pressure` | constante | oil_pressure_low |
| `PITOT ICE PCT` | `inst-pitot-ice-pct` | constante | structural_icing_exposure |
| `STRUCTURAL DEICE SWITCH` | `inst-structural-deice-switch` | constante | structural_icing_exposure, structural_deice_boot_cycles |
| `HYDRAULIC PUMP PRESSURE` | `hyd-pump-pressure` | constante | hydraulic_low_pressure_stress |
| `HYDRAULIC RESERVOIR PERCENT` | `hyd-reservoir-percent` | constante | hydraulic_low_pressure_stress |
| `HYDRAULIC PRESSURE` | `hyd-pressure` | constante | hydraulic_low_pressure_stress |
| `PRESSURIZATION CABIN ALTITUDE` | `af-pressurization-cabin-altitude` | constante | pressurization_cycle_stress, pressurization_cycle_fuselage_stress, cabin_rapid_decompression_stress |
| `PRESSURIZATION PRESSURE DIFFERENTIAL` | `af-pressurization-pressure-differential` | constante | pressurization_cycle_stress, pressurization_cycle_fuselage_stress, cabin_rapid_decompression_stress |

## Sensores usados em regras que variaram (validados neste avião)

`brake-left-temperature`, `brake-right-temperature`, `gear-contact-point-compression`, `gear-is-skidding`, `gear-steer-angle-pct`, `af-aileron-position`, `af-elevator-position`, `af-elevator-trim-position`, `af-flaps-handle-index`, `af-rudder-position`, `elec-bus-voltage-1`, `elec-total-load-amps`, `elec-master-battery`, `elec-circuit-on-3`, `turb-eng-torque`, `eng-egt`, `eng-max-reached-rpm`, `oil-pressure`, `oil-temperature`, `eng-rpm`, `eng-starter`, `eng-starter-active`, `eng-prop-rpm`, `eng-cht`, `eng-manifold-pressure`, `eng-tit`, `af-g-force`, `af-max-g-force`, `fuel-total-quantity`, `af-acceleration-body-x`, `gear-plane-touchdown-bank-degrees`, `gear-plane-touchdown-normal-velocity`, `gear-plane-touchdown-pitch-degrees`, `af-airspeed-indicated`, `af-airspeed-mach`

## Sensores do registro que não entraram na sondagem

`PROP DEICE SWITCH`, `WEAR AND TEAR LEVEL:39:1`, `WEAR AND TEAR LEVEL:'PISTON_ENGINE'_n`
