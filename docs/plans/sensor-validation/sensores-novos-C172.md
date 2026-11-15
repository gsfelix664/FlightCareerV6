# Validação dos sensores novos (F2.1b) — C172SP G1000, 2026-11-15

Log `20261115-1534-C172SP_G1000_Passengers.csv` (4 min, REC, marcadores `teste-*`). Nenhuma SimVar rejeitada pelo MSFS.

| Sensor | SimVar | Resultado | Situação |
|---|---|---|---|
| Posição do profundor | `ELEVATOR POSITION` | −1,00 a +1,00, 100 valores | ✅ válido |
| Posição do aileron | `AILERON POSITION` | −0,90 a +1,00, 131 valores | ✅ válido |
| Posição do leme | `RUDDER POSITION` | 0 o voo todo | ⏳ não testado (o usuário não moveu os pedais); validar no próximo voo |
| Piloto automático | `AUTOPILOT MASTER` | 0 → 1 → 0 nos marcadores `teste-ap-on/off` | ✅ válido |
| Trim do profundor | `ELEVATOR TRIM POSITION` | −0,028 a 0,193 (radianos), 71 valores | ✅ válido |
| Trim de aileron / leme | `AILERON TRIM PCT`, `RUDDER TRIM PCT` | 0 | — o C172 não tem (esperado) |
| Master de aviônicos | `AVIONICS MASTER SWITCH` | **1 o voo todo**, mesmo com `teste-avionics-off` | ❌ não reflete a chave do G1000 |
| Tensão da bateria | `ELECTRICAL BATTERY VOLTAGE` | **25,137 V constante**, antes e depois da partida | ❌ suspeito (não varia com a carga do alternador) |
| Alternador | `ELECTRICAL GENALT BUS VOLTAGE/AMPS/LOAD:1` | **0 o voo todo**, com motor girando | ❌ índice/nome errado ou não suportado |

**Consequência:** as regras `avionics_start_with_avionics_master_on`, `battery_deep_discharge_damage` e `electrical_system_overload_amps` ficam **mudas** até achar as SimVars certas no MSFS 2024 (provável: barramentos/circuitos por índice). Comando brusco (profundor/aileron), trim e piloto automático estão prontos para a F2.4.
