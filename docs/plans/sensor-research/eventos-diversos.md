# Key Events — sistemas gerais, instrumentos, aeronave diversos, rádio, diversos e câmera (MSFS 2024)

Criado em 2026-11-16 (Claude). Fontes (SDK 2024, `key-events/`): [aircraft-general-systems-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-general-systems-events/) (100), [aircraft-instrumentation-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-instrumentation-events/) (39), [aircraft-miscellaneous-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-miscellaneous-events/) (117), [aircraft-radio-navigation-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-radio-navigation-events/) (187), [miscellaneous-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/miscellaneous-events/) (177), [view-and-camera-events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/view-and-camera-events/) (109).
Regras de `eventos-eletricos.md` § 1 (enviar × receber, `_SET` × `_TOGGLE`) e princípio F4.2e (falhar o **sistema** sem mexer no comando do piloto). Obsoletos/sem uso marcados na documentação: `PNEUMATICS_JUNCTION_LINE_OPENING_STATUS_SET`, `VARIOMETER_SOUND_TOGGLE`, `LOW_HEIGHT_WARNING_*`, `BAIL_OUT`, armas, `AUTOCOORD_ON/OFF/SET`, `TOGGLE_RADAR/RADIO/RAD_INS_SWITCH`, `PRESSURIZATION_CLIMB_RATE_SET`, vários de interface.

## 1. Destaques

| Área | Eventos |
|---|---|
| **Panes nativas (enviar)** | `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_LEFT/RIGHT/TOTAL_BRAKE_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_VACUUM_FAILURE` (**já usados**; só existem como toggle → conferir o estado antes). Novas no lado do sistema: **`HYDRAULIC_ACTUATOR_ACTIVE_SET`** (atuador hidráulico desligado: trem/flap hidráulico sem movimento), `HYDRAULIC_VALVE_SET`, **`PNEUMATICS_PACK_SET`/`PNEUMATICS_VALVE_SET`** (pane de pressurização/ar-condicionado), **`GYRO_DRIFT_SET`** (deriva do giro direcional crescendo com o desgaste dos instrumentos), **`NOSE_WHEEL_STEERING_LIMIT_SET`** (bequilha com curso reduzido) |
| **Alertas e efeitos ao piloto (enviar)** | **`MASTER_CAUTION_SET` / `MASTER_WARNING_SET`** (acender o alarme nativo junto com a pane do addon), **`ELT_SET`/`ELT_ON`** (ELT acionado depois de acidente/pouso muito duro) |
| **Serviços de solo (enviar e receber)** | **`TOGGLE_PUSHBACK`, `TUG_HEADING`, `TUG_SPEED`, `TUG_DISABLE`** (pushback no HUD), **`REQUEST_POWER_SUPPLY`** (GPU), **`REQUEST_CATERING`**, **`REQUEST_LUGGAGE`**, **`TOGGLE_JETWAY`**, **`TOGGLE_RAMPTRUCK`**, `GROUND_SERVICE_ATC_MESSAGE` (plano `taxas-aeroporto-servicos-solo.md`: cobrar o que o piloto pediu ou chamar o que ele comprou no app); `OPEN/CLOSE/SET_AIRCRAFT_DOORS` (embarque), **`COVER_SET`** (capas e calços no pernoite) |
| **Integridade (receber)** | **`BACK_TO_FLY`** (tira o avião do chão e põe em voo = invalidar), **`SIM_RESET`, `SITUATION_RESET`, `RELOAD_USER_AIRCRAFT`** (zeram o estado/as falhas → reaplicar panes; em voo = invalidar), **`PAUSE_SET/ON/OFF/TOGGLE`** (pausa: não contar tempo/qualidade), `FORCE_END`, `ABORT`/`EXIT` (sim fechado no meio do voo: voo interrompido), `REPLAY_STOP`/`DEMO_*` (replay), **`TOOLS_QUICK_PREFLIGHT`** (tira todas as capas sem walkaround) |
| **Procedimentos (receber)** | trem (`GEAR_SET/UP/DOWN`), **extensão de emergência** (`GEAR_EMERGENCY_HANDLE_TOGGLE`, `GEAR_PUMP`), freio de estacionamento (`PARKING_BRAKE_SET`), **fogo** (`EXTINGUISH_ENGINE_FIRE`), aquecimento do pitot/para-brisa/degelo (`PITOT_HEAT_SET`, `WINDSHIELD_DEICE_SET`, `TOGGLE_STRUCTURAL_DEICE`), **fonte estática alternativa** (`TOGGLE_ALTERNATE_STATIC`), altímetro (`KOHLSMAN_SET`, `BAROMETRIC`, `BAROMETRIC_STD_PRESSURE`), alinhar o giro (`HEADING_GYRO_SET`), cinto (`CABIN_SEATBELTS_ALERT_SWITCH_TOGGLE`), aviônicos (`AVIONICS_MASTER_SET`), trava da bequilha (`TOGGLE_TAILWHEEL_LOCK`), despressurização (`PRESSURIZATION_PRESSURE_DUMP_SWITCH`), ELT desligado pelo piloto |
| **Missões futuras** | combate a incêndio (`LIQUID_DROPPING_SYSTEM_*`), reboque de planador (`TOW_PLANE_REQUEST/RELEASE`), helicóptero (guincho/gancho: `HOIST_*`, `SLING_PICKUP_RELEASE`, `GRAPPLE_HOOK_*`), lançamento de carga (`RELEASE_DROPPABLE_OBJECTS`) |

## 2. Observações por página

### Sistemas gerais
- Hidráulico: `HYDRAULIC_ACTUATOR_ACTIVE_SET [atuador, estado]` é a pane hidráulica "de verdade" de um sistema só (o `TOGGLE_HYDRAULIC_FAILURE` é total); `HYDRAULIC_VALVE_SET`, `HYDRAULIC_SWITCH_TOGGLE` (bomba). Índices por avião (Hydraulic System Debug).
- Pneumático: `PNEUMATICS_PACK_SET`, `PNEUMATICS_VALVE_SET(_EX1)`, `PNEUMATICS_VALVE_MODE_SET` → pane de pack/válvula de sangria (cabine sobe, esquenta). `PNEUMATICS_AREA_TEMPERATURE_SET` e `PNEUMATICS_TARGET_CABIN_ALTITUDE_SET`: não escrever (são do piloto).
- Trem: só receber (`GEAR_*`); pane de trem pelo lado do sistema (atuador/válvula hidráulica ou disjuntor do motor do trem), não pela alavanca.
- Freios: `PARKING_BRAKE_SET`, `ANTISKID_BRAKES_TOGGLE`, `BRAKES*`/`AXIS_*_BRAKE_*` — receber.

### Instrumentos
- `GYRO_DRIFT_SET`/`_EX1` (graus): **desgaste do giro direcional** → deriva maior; o piloto realinha (`HEADING_GYRO_SET` recebido = procedimento). `ATTITUDE_CAGE_BUTTON` recebido.
- `KOHLSMAN_SET/INC/DEC`, `BAROMETRIC`, `BAROMETRIC_STD_PRESSURE`: altímetro ajustado (QNH no pouso, STD acima da transição).
- `EGT_*`: só marcadores (bugs); não usar. `RESET_G_FORCE_INDICATOR`/`RESET_MAX_RPM_INDICATOR`: não usar.
- `GROUND_SERVICE_ATC_MESSAGE [id]`: pedido de serviço de solo pelo ATC (testar quais mensagens existem).

### Aeronave diversos
- Panes nativas `TOGGLE_*_FAILURE` confirmadas aqui (lista acima).
- `MASTER_CAUTION/WARNING_SET`: o addon acende o alarme quando aplica uma pane (mais imersão); receber `*_ACKNOWLEDGE`.
- `EXTINGUISH_ENGINE_FIRE [índice combinado]`: **ligar ao fogo progressivo do addon** — o extintor do piloto apaga (ou não, conforme o estágio) o fogo do `SimFailureActuator`; recarga cobrada.
- Pushback: `TOGGLE_PUSHBACK`, `TUG_HEADING`, `TUG_SPEED`, `TUG_DISABLE` (confirmados).
- `COVER_SET [capa, bool]`/`COVER_GENERIC_SET`: o addon põe capas e calços quando o avião pernoita; `TOOLS_QUICK_PREFLIGHT` recebido = walkaround pulado (sem penalidade grave; decisão do usuário).
- `OPEN/CLOSE/SET_AIRCRAFT_DOORS`, `CLOSE_AIRCRAFT_DOORS_CRASHING`: embarque/desembarque; fechar portas que causariam crash.
- `NOSE_WHEEL_STEERING_LIMIT_SET`: pane de bequilha (curso reduzido) no lado do sistema.
- `BACK_TO_FLY`: integridade (invalidar).
- `DECISION_HEIGHT_SET`, `SMOKE_*`, asa dobrável, catapulta, armas, paraquedismo: não usar.

### Rádio e navegação
- `AVIONICS_MASTER_SET/_1_SET/_2_SET`: receber (aviônicos depois da partida; regra já existe).
- **`ELT_SET`/`ELT_ON`/`ELT_OFF`**: enviar depois de acidente; receber o piloto desligando.
- GPS (`GPS_*_BUTTON`, `GPS_OBS_*`), sintonia (`COM/NAV/ADF/XPNDR` dígitos), `FREQUENCY_SWAP`, TACAN, intercomunicador: não usar (o estado já vem pelas SimVars).

### Diversos (simulação)
- **Integridade:** `BACK_TO_FLY`, `SIM_RESET`, `SITUATION_RESET`, `RELOAD_USER_AIRCRAFT`, `FORCE_END`, `ABORT`, `EXIT`, `REPLAY_STOP`, `DEMO_*`.
- **Pausa:** `PAUSE_SET/ON/OFF/TOGGLE` (complementa `MOTION SIMULATION`).
- **Serviços:** `REQUEST_POWER_SUPPLY`, `REQUEST_CATERING`, `REQUEST_LUGGAGE`, `TOGGLE_JETWAY`, `TOGGLE_RAMPTRUCK` — o app chama o serviço comprado e cobra o que o piloto pediu pelo sim.
- `CAPTURE_SCREENSHOT`: possível captura automática do pouso para o diário de bordo (ideia).
- `AUTOCOORD_TOGGLE`/`AXIS_SENSOR_TOGGLE`: ajudas (mesma regra do auto-start/auto-leme, se o usuário quiser).
- Personagem (`PC_*`), painéis, som, rótulos, debug, macros: não usar.

### Câmera e vistas
- 109 eventos (vistas, zoom, ponto de vista, câmera de perseguição, eixos): **nenhum útil** para desgaste, panes ou economia. Única ideia: vista de cabine/externa para a captura automática do pouso.

## 3. Decisões para o usuário
1. Integridade: `BACK_TO_FLY`, `SIM_RESET`, `SITUATION_RESET`, `RELOAD_USER_AIRCRAFT` e `REPAIR_AND_REFUEL` (página de combustível) em voo **invalidam** o voo e as panes são reaplicadas?
2. Serviços de solo pelos eventos do sim (GPU, catering, bagagem, jetway, escada, pushback) entram no plano de taxas: o app **chama** o serviço comprado e **cobra** o que o piloto pedir direto no sim?
3. Alarme nativo (`MASTER_CAUTION/WARNING_SET`) junto com as panes do addon?
4. Fogo: o extintor do piloto (`EXTINGUISH_ENGINE_FIRE`) apaga o fogo progressivo do addon conforme o estágio?
5. Capas/calços no pernoite e `TOOLS_QUICK_PREFLIGHT` (walkaround pulado): efeito?

## 4. Próximos passos
1. Teste no MSFS: `HYDRAULIC_ACTUATOR_ACTIVE_SET` num retrátil, `PNEUMATICS_PACK_SET` num pressurizado, `GYRO_DRIFT_SET`, `NOSE_WHEEL_STEERING_LIMIT_SET`, `MASTER_CAUTION_SET`, `ELT_SET`, pushback (`TOGGLE_PUSHBACK`/`TUG_*`), `REQUEST_POWER_SUPPLY`, receber `BACK_TO_FLY`/`SIM_RESET`/`PAUSE_SET`.
2. CSV de Key Events (`keyevents-msfs2024.csv`) com coluna "usado pelo addon", gerado do extrator `scrape_events.py` (mover para `tools/simvar_probe/`).
