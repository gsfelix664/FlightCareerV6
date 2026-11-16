# Key Events de motor do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Engine Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-engine-events/) do SDK 2024 (228 linhas: anti-gelo, manete de condição, combustível/mistura, magnetos, gerais, diversos, hélice, manete, turbina, partida). Nenhum marcado como obsoleto; alguns "Not used in the simulation" (`MAGNETO1..4_SET`, `STARTER_GEN`, `STARTER_OFF`, `STARTER_START`). Mesmas regras de `eventos-eletricos.md` (§ 1: enviar × receber, `_SET` × `_TOGGLE`, grupo mascarado).
**Não há evento de pane de motor nesta página:** `TOGGLE_ENGINE1..4_FAILURE` e afins ficam na página de falhas (a analisar). `ISOLATE_TURBINE_*` anula empuxo e arrasto, mas a documentação diz que é **só para o DarkStar** — não usar.

## 1. Resumo por área

| Área | Eventos que mais rendem |
|---|---|
| Panes (enviar) | **combustível cortado no motor** (`SHUTOFF_VALVE_OFF [motor]`, `SET_FUEL_VALVE_ENG1..4 [valor]`: parada por falta de combustível sem mexer nos tanques), ignição da turbina desligada (`TURBINE_IGNITION_SWITCH_SET`: sem reacendimento), magneto (já usado: `MAGNETO1_OFF`) |
| Panes (mascarar o comando do piloto) | **manete travada** (cabo do manete: mascarar `THROTTLE*`/`AXIS_THROTTLE*`), **governador da hélice travado** (mascarar `PROP_PITCH*`/`AXIS_PROPELLER*`), **mistura travada**, **cowl flap travado** (`COWLFLAP*`, `INC/DEC_COWL_FLAPS*`), anti-gelo do motor inoperante (`ANTI_ICE_*`) — mesmo mecanismo do flap e do alternador (`MaskedPilotInputs`) |
| Procedimento (receber do piloto) | **cheque de magnetos** (`MAGNETO_LEFT/RIGHT/BOTH` no giro de motor), primer (`ENGINE_PRIMER`, `TOGGLE_PRIMER*`), bomba de combustível (`FUEL_PUMP`, `TOGGLE_ELECT_FUEL_PUMP*`), mistura (`MIXTURE_*`, `MIXTURE_SET_BEST`), partida (`STARTER*_SET`, `SET_STARTER*_HELD`, `TOGGLE_STARTER*`), **auto-start** (`ENGINE_AUTO_START`/`AUTO_SHUTDOWN`), embandeiramento e auto-feather (`TOGGLE_FEATHER_SWITCH*`, `TOGGLE_AUTOFEATHER_ARM`), reverso (`SET_REVERSE_THRUST_ON`, `THROTTLE_REVERSE_THRUST_*`), anti-gelo, degelo da hélice, sincronismo |
| Missões/economia | detentes do manete nos jatos (`THROTTLE_DETENT_NEXT/PREV`, interceptáveis): decolagem reduzida × TOGA (já proposto em `variaveis-motor.md`) |

## 2. Tabela

### Combustível e mistura
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| **`SHUTOFF_VALVE_OFF`** / `SHUTOFF_VALVE_ON` | `[0]` motor (0 = todos) | **Pane: parada por falta de combustível** (válvula/linha obstruída, contaminação) com `_ON/_OFF` idempotentes. Alternativa à SimVar `RECIP ENG FUEL AVAILABLE`. Receber `SHUTOFF_VALVE_*` do piloto = corte de combustível no checklist de fogo. |
| `SET_FUEL_VALVE_ENG1..4` | `[0]` valor | Válvula de combustível do motor (pane ou procedimento). Conferir o significado do valor (0/1) no teste. |
| `TOGGLE_FUEL_VALVE_ENG1..4`, `TOGGLE_FUEL_VALVE_ALL`, `SHUTOFF_VALVE_TOGGLE` | — | Só receber. |
| `FUEL_PUMP`, `TOGGLE_ELECT_FUEL_PUMP(1..4)` | motor | Receber: bomba auxiliar na partida/decolagem/pouso (procedimento); mascarar = bomba inoperante. |
| `MIXTURE_SET`, `MIXTURE1..4_SET`, `AXIS_MIXTURE*_SET`, `MIXTURE_RICH/LEAN/INCR/DECR*`, `MIXTURE_SET_BEST` | motor, valor | Receber (gestão da mistura); mascarar = **mistura travada** (cabo); `MIXTURE_LEAN` como corte de motor no fim do voo (procedimento). |

### Magnetos e partida
| Evento | Uso proposto |
|---|---|
| `MAGNETO1..4_OFF`, `MAGNETO_OFF` | **Já usado** (pane de magneto). |
| `MAGNETO_LEFT/RIGHT/BOTH`, `MAGNETO1..4_LEFT/RIGHT/BOTH`, `MAGNETO_INCR/DECR*`, `MAGNETO_START`, `MAGNETO_SET` | **Receber: cheque de magnetos** no giro (antes da decolagem). Bônus de procedimento; e é onde o piloto "descobre" uma pane de magneto/vela com queda de RPM (`ignition.spark_plugs`). `MAGNETO1..4_SET` não funciona no sim. |
| `STARTER1..4_SET`, `SET_STARTER1..4_HELD`, `SET_STARTER_ALL_HELD`, `TOGGLE_STARTER1..4`, `TOGGLE_ALL_STARTERS`, `JET_STARTER`, `TOGGLE_MASTER_STARTER_SWITCH` | Receber: duração e repetição das partidas (regra de starter já existe pela SimVar). Mascarar = **motor de partida inoperante** (pane: não dá partida). |
| `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` | Receber: piloto usou o auto-start (Ctrl+E) — opção de realismo: sem penalidade, ou nota de procedimento menor (decisão do usuário). |
| `ENGINE_PRIMER`, `TOGGLE_PRIMER(1..4)` | Receber: primer na partida a frio (excesso = motor afogado/fogo no carburador, regra futura). |
| `TOGGLE_MASTER_IGNITION_SWITCH`, `ENGINE_MODE_CRANK/NORM/IGN_START` | Receber (jatos: seletor de modo do motor). |

### Turbina e manete de condição
| Evento | Uso proposto |
|---|---|
| `TURBINE_IGNITION_SWITCH_SET(1..4)`, `TURBINE_IGNITION_SWITCH_TOGGLE` | Receber (ignição contínua em chuva/gelo = procedimento); mascarar/forçar desligado = **sem reacendimento** após apagamento (pane de ignição da turbina). |
| `CONDITION_LEVER_*` (corte, baixa/alta marcha lenta, set por motor) | Receber (partida e corte do turbo-hélice). Não usar para pane (move a alavanca do piloto). |
| `ISOLATE_TURBINE_*` | **Não usar** (só DarkStar). |

### Hélice
| Evento | Uso proposto |
|---|---|
| `PROP_PITCH*`, `AXIS_PROPELLER*_SET`, `PROP_PITCH_*_EX1` | Mascarar = **governador travado** (passo fixo na posição atual); receber = uso da hélice. |
| `TOGGLE_FEATHER_SWITCHES`, `TOGGLE_FEATHER_SWITCH_1..4`, `TOGGLE_AUTOFEATHER_ARM` | Receber: embandeiramento na falha de motor e auto-feather armado na decolagem (nota da emergência, bimotor). |
| `PROP_LOCK_SET/ON/OFF/TOGGLE` | Receber (trava de hélice na partida/corte, ATR). |
| `PROP_FORCE_BETA_SET/ON/OFF`, `PROP_FORCE_BETA_VALUE_SET` | Força o beta da hélice a uma posição: possível pane grave (hélice presa em beta). Só com decisão e teste. |
| `TOGGLE_PROPELLER_DEICE`, `TOGGLE_PROPELLER_SYNC`, `PROPELLER_REVERSE_THRUST_*` | Receber (procedimento em gelo, conforto/sincronismo, reverso). |

### Manete e reverso
| Evento | Uso proposto |
|---|---|
| `THROTTLE*`, `AXIS_THROTTLE*`, `THROTTLE_*_EX1`, `THROTTLE_SET`, `THROTTLE_10..90`, `THROTTLE_FULL/CUT/IDLE/INCR/DECR*` | Mascarar **todos** = **manete travada** (pane clássica de cabo); limitar o valor máximo = **perda parcial de potência** (alternativa às SimVars `THROTTLE INPUT BLOCKED *`). Atenção: vários eventos e eixos de hardware; testar com joystick/manete física. |
| `THROTTLE_DETENT_NEXT/PREV`, `THROTTLE_RANGE_INCR/DECR` | Interceptáveis (documentação "Intercepting Throttle Events"): detentes FLX/TOGA dos jatos. |
| `SET_REVERSE_THRUST_ON/OFF`, `THROTTLE_REVERSE_THRUST_HOLD/TOGGLE`, `TOGGLE_THROTTLE1..4_REVERSE_THRUST` | Receber: reverso no pouso (freios poupados); mascarar = reverso inoperante (pane, MEL). |

### Diversos
| Evento | Uso proposto |
|---|---|
| `COWLFLAP1..4_SET`, `INC/DEC_COWL_FLAPS(1..4)` | Receber (cowl flaps por fase); mascarar = **cowl flap travado**. |
| `ANTI_ICE_SET(_ENG1..4)`, `ANTI_ICE_ON/OFF/TOGGLE*`, `ANTI_ICE_GRADUAL_SET*` | Receber: anti-gelo do motor em gelo (procedimento); mascarar = anti-gelo inoperante. |
| `ENGINE_BLEED_AIR_SOURCE_SET/TOGGLE` | Receber (sangria/pressurização); pane de sangria de um motor. |
| `OIL_COOLING_FLAPS_*`, `RADIATOR_COOLING_FLAPS_*` | Warbirds; fora da frota. |
| `PLASMA_*`, `THRUST_VECTOR_*`, `AXIS_THRUST_VECTOR_*`, `TOGGLE_AFTERBURNER*`, `ENGINE`, `ENGINE_MASTER_*` | Não usar. |

## 3. Decisões para o usuário
1. Panes por **mascaramento** de comando (manete, governador, mistura, cowl flap, partida, reverso): o piloto mexe e nada acontece, como num cabo partido. Implementar?
2. **Auto-start** (Ctrl+E): sem efeito ou nota de procedimento menor?
3. Parada por falta de combustível por `SHUTOFF_VALVE_OFF` (evento) ou por `RECIP ENG FUEL AVAILABLE` (SimVar)? Testar os dois no MSFS e ficar com o que o sim mantém.

## 4. Próximos passos
1. Página de **falhas** (`TOGGLE_ENGINE*_FAILURE`, vácuo, pitot, estática, hidráulico, freios) — onde ficam as panes nativas.
2. Teste no MSFS: `SHUTOFF_VALVE_OFF`/`SET_FUEL_VALVE_ENG1`, `TURBINE_IGNITION_SWITCH_SET`, mascaramento de `THROTTLE*` e `PROP_PITCH*` com manete física, receber `MAGNETO_LEFT/RIGHT` no giro.
