# Teste elétrico de solo — C172SP G1000, 2026-11-15

Log `20261115-1636-C172SP_G1000_Passengers.csv` (~7 min, REC, roteiro §7 do `ROTEIRO.md`). As 16 SimVars novas do spike foram aceitas pelo MSFS (nenhuma coluna vazia). Os marcadores entraram 1–2 s **depois** de cada ação.

## Linha do tempo (UTC)

| Hora | Evento no log | Marcador |
|---|---|---|
| 19:36:55–56 | `CIRCUIT ON:1` e `:3` → 1; `ELECTRICAL BUS AMPS:4` → 21 A | `teste-bateria-avionics-on` (19:36:58) |
| 19:39:21 | `CIRCUIT ON:3` → 0; `BUS AMPS:4` 20,2 → 1,4 A | `teste-avionics-off` (19:39:22) |
| 19:40:26–35 | `GENERAL ENG MASTER ALTERNATOR:1` → 1; starter 31–35 s; combustão às 33 s | — |
| 19:40:36 | `ELECTRICAL BUS VOLTAGE:1` → 28,0 V | `teste-partida` (19:40:36) |
| 19:41:47 | chave ALT → 0; `BUS VOLTAGE:1` 28,0 → 21,3 V (depois ~24 V) | `teste-alternador-off` (19:41:49) |
| 19:42:59–43:02 | chave ALT → 1; `BUS VOLTAGE:1` → 28,0 V | `teste-alternador-on` (19:43:00) |
| 19:43:14–19 | `RUDDER POSITION` −1,0 / +1,0 / −1,0 / +0,9 | `teste-leme` (19:43:20) |

## Resultado por SimVar

| SimVar | Resultado | Situação |
|---|---|---|
| `CIRCUIT ON:3` | 1 com aviônicos ligados, 0 desligados (borda no mesmo segundo da chave) | ✅ **reflete a chave de aviônicos neste avião** (índice específico do C172) |
| `ELECTRICAL BUS AMPS:4` | ~20 A com aviônicos, ~1,5–1,9 A sem | ✅ carga do barramento de aviônicos (índice específico do C172) |
| `ELECTRICAL BUS AMPS:1/2/3/5` | 7–12 / 5–6 / 20–27 / 1,6–1,9 A, variam com a carga | ✅ válidos (função de cada barramento não identificada) |
| `ELECTRICAL BUS VOLTAGE:1..5` | **iguais entre si** o tempo todo: ~24 V só bateria (23,65 após 2 min com 44 A), 28,0 V com alternador, queda instantânea a 21,3 V ao desligar o ALT | ✅ indicador de alternador carregando / bateria descarregando; não distingue barramentos (rede interligada) |
| `CIRCUIT ON:1/2` | 1 com master ligado | ✅ válidos (sem uso definido) |
| `GENERAL ENG MASTER ALTERNATOR:1` | segue a chave ALT | ✅ válido |
| `ELECTRICAL TOTAL LOAD AMPS` | 22–45 A; pico de 170 A no starter | ✅ válido (excluir as amostras com starter engatado) |
| `RUDDER POSITION` | −1,0 a +1,0 | ✅ válido (pendência do C172 resolvida) |
| `ELECTRICAL BATTERY VOLTAGE:1` (e sem índice) | 25,14 V constante | ❌ não atualiza neste avião |
| `ELECTRICAL BATTERY LOAD:1` (e sem índice) | 0 constante | ❌ não atualiza |
| `ELECTRICAL MAIN BUS VOLTAGE` (com e sem `:1`) | 0 | ❌ legada, sem valor no v2 |
| `ELECTRICAL GENALT BUS VOLTAGE/AMPS:1` | 0 | ❌ legada, sem valor no v2 |
| `AVIONICS MASTER SWITCH` | 1 o tempo todo | ❌ não reflete a chave do G1000 |

## Proposta para as três regras mudas (a decidir antes de mexer em `wear_rules.json`)

1. `avionics_start_with_avionics_master_on`: usar "aviônicos energizados" = `CIRCUIT ON:<n>` com o índice do circuito de aviônicos **por aeronave** (C172 G1000: 3). O índice vem do catálogo da aeronave (`Data/aircraft/<id>.json`, campo novo, origem `SimVar` com este log como referência); sem índice no catálogo, a regra fica desligada naquele avião (nada de `AVIONICS MASTER SWITCH`, que mente).
2. `battery_deep_discharge_damage`: usar `ELECTRICAL BUS VOLTAGE:1` (todos os barramentos têm a mesma tensão no C172) com o alternador fora (tensão < ~25 V em sistema de 28 V) e limiar de descarga profunda por tensão nominal do sistema (24 V: < 22 V; 12 V: < 11 V). `BATTERY VOLTAGE/LOAD` ficam fora.
3. `electrical_system_overload_amps`: `ELECTRICAL TOTAL LOAD AMPS` acima da capacidade do alternador da aeronave (limite novo no catálogo, ex.: C172SP 60 A pelo POH), ignorando as amostras com starter engatado.

Ainda não confirmado: se o índice global dos barramentos/circuitos muda entre aviões (esperado: sim, depende do `systems.cfg`). Cada avião novo precisa de uma gravação curta de solo para preencher o índice no catálogo.
