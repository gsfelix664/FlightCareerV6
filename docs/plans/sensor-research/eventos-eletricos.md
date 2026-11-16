# Key Events elétricos do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-electrical-events/) do SDK 2024 (129 eventos: baterias, disjuntores, luzes, diversos, alternador, APU). Nenhum marcado como obsoleto. Complementa `eventos-e-input-events.md` (pesquisa anterior: eventos já usados e API de Input Events) e as análises de SimVars `variaveis-eletrica.md`/`variaveis-sistemas.md`.

## 1. Como os Key Events servem ao addon
- **Enviar** (addon → sim): é o caminho padrão para aplicar uma pane ou um serviço quando a SimVar não é gravável ou quando o sim precisa "executar" a ação (o efeito sai igual ao do botão do cockpit). Usar `SimConnectService.SendEvent(nome, dado)`, que mapeia cada evento uma vez (`AGENTS.md` › 6). Eventos com vários parâmetros (`[0]`, `[1]`, `[2]`) usam `TransmitClientEvent_EX1` (já usado em `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`).
- **Receber** (sim → addon): o mesmo evento mapeado num grupo de notificação avisa quando **o piloto** apertou o botão. É assim que o addon sabe que o piloto **conferiu/rearmou um disjuntor**, ligou uma luz ou o aquecimento do pitot, sem depender só da SimVar. Com prioridade "mascarável" (grupo `MaskedPilotInputs`, já usado para flap e alternador), o addon ainda pode **bloquear** o comando.
- **`_SET` × `_TOGGLE`**: preferir sempre a versão `_SET` (estado explícito, idempotente). `_TOGGLE` enviado duas vezes desfaz a pane (bug já conhecido); quando só existir toggle, conferir o estado pela SimVar antes (`SetFailure` já faz).

## 2. Resumo por área

| Área | Eventos que mais rendem |
|---|---|
| Panes elétricas | **`BREAKER_*_SET`** (GPS, piloto automático, transponder, flap, instrumentos, aviônicos 1/2, NAV/COM 1–3, ADF, coordenador, luzes de instrumento, campo do alternador, avisos) com `[0]` circuito e `[1]` estado; `ELECTRICAL_CIRCUIT_BREAKER_TOGGLE` (qualquer circuito); `ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE` (fio rompido, sem disjuntor saltado); `ELECTRICAL_LINE_BREAKER_SET`/`LINE_CONNECTION_SET` (barramento parcial); `ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE`; `ELECTRICAL_BATTERY_BREAKER_TOGGLE` / `ELECTRICAL_BUS_TO_BATTERY_CONNECTION_TOGGLE` (bateria morta) |
| Iluminação | `*_LIGHTS_SET` (pouso, táxi, navegação, beacon, strobe, logo, asa, reconhecimento, painel, cabine, glareshield, pedestal) com `[0]` estado e `[1]` circuito: lâmpada queimada = manter desligada (ou disjuntor da luz) |
| Serviços de solo | **`SET_EXTERNAL_POWER [índice, estado]`** / `TOGGLE_EXTERNAL_POWER`: conectar a GPU comprada no app; `ELECTRICAL_BUS_TO_EXTERNAL_POWER_CONNECTION_TOGGLE` |
| Procedimento (receber do piloto) | disjuntores rearmados, `ALTERNATOR_SET`/`TOGGLE_MASTER_ALTERNATOR`, `MASTER_BATTERY_SET`, luzes por fase, `TOGGLE_ELECTRIC_VACUUM_PUMP` (bomba de vácuo standby na pane de vácuo), `APU_EXTINGUISH_FIRE` |
| APU | `APU_STARTER`, `APU_OFF_SWITCH`, `APU_GENERATOR_SWITCH_SET`, `APU_BLEED_AIR_SOURCE_SET`, `APU_EXTINGUISH_FIRE`: horas/ciclos de APU e fogo na APU (pane + procedimento) |

## 3. Tabela

### Disjuntores (pane por sistema)
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| **`BREAKER_GPS_SET`, `BREAKER_AUTOPILOT_SET`, `BREAKER_XPNDR_SET`, `BREAKER_FLAP_SET`, `BREAKER_INST_SET`, `BREAKER_AVNBUS1/2_SET`, `BREAKER_NAVCOM1/2/3_SET`, `BREAKER_ADF_SET`, `BREAKER_TURNCOORD_SET`, `BREAKER_INSTLTS_SET`, `BREAKER_ALTFLD_SET`, `BREAKER_WARN_SET`, `BREAKER_AVNFAN_SET`** | `[0]` circuito, `[1]` estado (1 ligado, 0 aberto), `[2]` barramento de origem | **Pane elétrica por sistema** (plano `iluminacao-panes-disjuntor.md` › 2.1): disjuntor saltado, visível ao piloto e **rearmável** (o addon recebe o evento do rearme e decide: causa transitória → fica; persistente → abre de novo). `ALTFLD` = pane do campo do alternador (alternador sem excitação). |
| `BREAKER_*_TOGGLE` (mesma lista) | `[0]` circuito, `[1]` barramento | Só **receber** (piloto mexeu); para enviar, usar `_SET`. |
| `ELECTRICAL_CIRCUIT_BREAKER_TOGGLE` | `[0]` barramento, `[1]` circuito | Disjuntor de **qualquer** circuito (luzes, bomba, pitot) quando não houver `BREAKER_*` nomeado; conferir `CIRCUIT BREAKER PULLED:n` antes. |
| **`ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE`** | `[0]` barramento, `[1]` circuito | **Fio rompido/conector solto:** o circuito para **sem** disjuntor saltado (não rearma em voo) — a versão "persistente" da pane elétrica; e a pane pós-manutenção "conector solto" do catálogo. |
| `ELECTRICAL_CIRCUIT_TOGGLE` | `[0]` circuito | Liga/desliga o circuito (chave): item inoperante (MEL). |
| `ELECTRICAL_BUS_BREAKER_TOGGLE`, `ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE`, `ELECTRICAL_LINE_BREAKER_SET/TOGGLE`, `ELECTRICAL_LINE_CONNECTION_SET/TOGGLE` | barramentos / linha | **Barramento parcial** (aviônicos, essencial) em vez da falha elétrica total. Parâmetros de `LINE_*_SET` não estão na tabela da documentação: conferir antes de usar. |
| `ELECTRICAL_CIRCUIT_POWER_SETTING_SET` | `[0]` circuito, `[1]` % | Não usar (dimmer). |

### Bateria e alternador
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| `ELECTRICAL_BATTERY_BREAKER_TOGGLE`, `ELECTRICAL_BUS_TO_BATTERY_CONNECTION_TOGGLE` | barramento, bateria | **Pane de bateria** (célula aberta): bateria fora do barramento sem mexer na chave. |
| `MASTER_BATTERY_SET`, `MASTER_BATTERY_ON/OFF`, `TOGGLE_MASTER_BATTERY`, `BATTERY1..N_SET`, `TOGGLE_MASTER_BATTERY_ALTERNATOR` | bateria, estado | **Receber** (procedimento: master na partida/corte). Não usar para pane (é a chave do piloto). |
| `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` | `[0]` barramento, `[1]` alternador | **Já usado** (pane de alternador). |
| `ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE` | barramento, alternador | Alternativa visível (disjuntor do alternador saltado, rearmável). |
| `ALTERNATOR_SET`, `ALTERNATOR_ON/OFF`, `TOGGLE_ALTERNATOR1..4`, `TOGGLE_MASTER_ALTERNATOR` | estado, alternador | **Já usado** `ALTERNATOR_OFF` no watchdog e mascarado; receber = piloto tentando religar. |

### Energia externa e diversos
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| **`SET_EXTERNAL_POWER`** | `[0]` energia externa, `[1]` estado | **Serviço de GPU** (plano `taxas-aeroporto-servicos-solo.md`): o app conecta a GPU que o piloto contratou e desconecta ao fim (cobrança por minuto). |
| `TOGGLE_EXTERNAL_POWER`, `ELECTRICAL_EXTERNAL_POWER_BREAKER_TOGGLE`, `ELECTRICAL_BUS_TO_EXTERNAL_POWER_CONNECTION_TOGGLE` | — | Receber (piloto conectou/desconectou); pane da tomada de GPU. |
| `TOGGLE_ELECTRIC_VACUUM_PUMP` | — | **Procedimento** da pane de vácuo (bomba standby ligada) → nota. |
| `ELECTRICAL_EXECUTE_PROCEDURE` | `[0]` procedimento, `[1]` inverso | Executa procedimentos definidos pelo avião (ex.: estado de partida). Possível uso futuro: preparar a aeronave ("cold & dark") no início da missão. Testar por avião. |

### Luzes
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| `LANDING/TAXI/NAV/BEACON/STROBES/LOGO/WING/RECOGNITION/PANEL/CABIN/GLARESHIELD/PEDESTRAL_LIGHTS_SET` | `[0]` estado, `[1]` circuito | **Lâmpada queimada** quando o avião não tiver mapa de disjuntor: manter desligada (watchdog, como o magneto); **receber** = luzes por fase (procedimento) e horas acesas. |
| `*_ON/_OFF/_TOGGLE`, `ALL_LIGHTS_TOGGLE` | circuito | Receber. |
| `LANDING_LIGHT_UP/DOWN/LEFT/RIGHT/HOME`, `LIGHT_POTENTIOMETER_*`, `*_POWER_SETTING_SET`, `LIGHT_AMBIENT_COLOR_SET` | — | Não usar. |

### APU
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| `APU_STARTER`, `APU_OFF_SWITCH` | — | Receber: ciclos de partida da APU (manutenção, combustível). |
| `APU_GENERATOR_SWITCH_SET`/`TOGGLE`, `APU_BLEED_AIR_SOURCE_SET`/`TOGGLE` | gerador, estado | Procedimento (APU alimentando elétrico/sangria na partida). |
| `APU_EXTINGUISH_FIRE` | gerador | Receber: extintor da APU disparado no fogo (procedimento; recarga cobrada). |

## 4. Decisões para o usuário
1. Pane elétrica rearmável por `BREAKER_*_SET` (o addon reabre conforme a causa) **e** pane persistente por `ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE` (fio rompido): usar os dois, como no plano do flap (§ 2.1)?
2. GPU contratada no app conectada pelo addon com `SET_EXTERNAL_POWER`?
3. Receber os eventos do piloto (rearme, luzes, bomba de vácuo, extintor) para a nota de procedimento?

## 5. Próximos passos
1. Teste no MSFS (modo de panes do SimVarProbe, `validacao-simvars.md` › 1b): `BREAKER_GPS_SET`/`BREAKER_FLAP_SET` com circuito do C172, conferir `CIRCUIT BREAKER PULLED:n`, rearmar pelo cockpit e conferir o evento recebido; `ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE` (efeito e se aparece no cockpit); `SET_EXTERNAL_POWER` numa vaga com GPU.
2. Mapa elétrico por aeronave (`iluminacao-panes-disjuntor.md` › 3): índices de circuito e barramento para os parâmetros.
3. Lista de eventos por página em CSV (`docs/plans/sensor-research/keyevents-msfs2024.csv`) com coluna "usado pelo addon", como a das SimVars.
