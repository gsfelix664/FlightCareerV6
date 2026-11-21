# Key Events de comandos de voo do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Flight Control Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-flight-control-events/) do SDK 2024 (115 linhas: ailerons, profundor, flaps, leme, slew, spoilers, Concorde). Nenhum marcado como obsoleto; "No longer used/Not currently used": `FLAPS_DETENTS_SET`, `MOUSE_AS_YOKE_*`, `*_CONCORDE_NOSE_VISOR*`. Mesmas regras de `eventos-eletricos.md` § 1.


> **Princípio do projeto (decisão de 2026-11-15, F4.2e):** falhar o **sistema** sem mexer no comando do piloto — a alavanca/chave continua livre e o efeito vem do lado do sistema (superfície do flap escrita, alternador desconectado do barramento). O mascaramento de comandos (`MaskedPilotInputs`) **foi abandonado** para flap e alternador. As propostas de "mascarar" abaixo só valem se o usuário decidir abrir exceção; a preferência é o efeito no sistema (SimVar ou evento que age no sistema).

## 1. Resumo por área

| Área | Eventos que mais rendem |
|---|---|
| Panes (enviar) | **compensador inoperante nativo** (`ELEVATOR/AILERON/RUDDER_TRIM_DISABLED_SET`), **compensador disparado** (`ELEVATOR_TRIM_SET`/`AXIS_ELEV_TRIM_SET` levando o compensador ao batente aos poucos) |
| Panes (mascarar) | flap (já feito), **spoilers inoperantes** (`SPOILERS_*`), compensador travado (`ELEV_TRIM_UP/DN`, `*_TRIM_SET`), superfície travada (`AXIS_AILERONS/ELEVATOR/RUDDER_SET` — perigoso, só com decisão) |
| Integridade | **`SLEW_ON`/`SLEW_SET`/`SLEW_TOGGLE` recebidos** (detecção imediata do slew; complementa `IS SLEW ACTIVE`) e **mascarar o slew no ar** (impedir em vez de só invalidar) |
| Procedimento (receber) | spoilers armados para o pouso (`SPOILERS_ARM_SET/ON`), uso do compensador, **auto-leme** (`AUTORUDDER_TOGGLE`: ajuda de pilotagem, como o auto-start) |

## 2. Tabela

### Compensadores (pane)
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| **`ELEVATOR_TRIM_DISABLED_SET`**, **`AILERON_TRIM_DISABLED_SET`**, **`RUDDER_TRIM_DISABLED_SET`** | `[0]` bool | **Pane nativa de compensador inoperante** (motor do trim/cabo): idempotente, o próprio sim desliga o trim. É também o **procedimento** do compensador disparado (piloto corta o trim). Confirmar pela SimVar `* TRIM DISABLED`. |
| `ELEVATOR_TRIM_SET`, `AXIS_ELEV_TRIM_SET` | `[0]` posição (−16383 a 16384) | **Compensador disparado (trim runaway):** o addon move o trim aos poucos até o batente (por tempo simulado, como o vazamento de óleo) até o piloto "cortar" (receber o evento de corte/disjuntor do trim) ou o voo acabar. Alternativa: escrever `ELEVATOR TRIM POSITION` (SimVar gravável). |
| `AILERON_TRIM_SET(_EX1)`, `RUDDER_TRIM_SET(_EX1)`, `RUDDER_TRIM_RESET` | posição | Idem para aileron/leme (aviões que têm). |
| `ELEV_TRIM_UP/DN`, `AILERON_TRIM_LEFT/RIGHT`, `RUDDER_TRIM_LEFT/RIGHT` | — | Receber (uso do trim); mascarar = trim travado na posição. |
| `*_TRIM_DISABLED_TOGGLE` | — | Só receber. |

### Superfícies (mascarar com cuidado)
| Evento | Uso proposto |
|---|---|
| `AXIS_AILERONS_SET`, `AILERON_SET`, `AXIS_ELEVATOR_SET`, `ELEVATOR_SET`, `AXIS_RUDDER_SET`, `RUDDER_SET`, `AXIS_SENSOR_*`, `*_LEFT/RIGHT/UP/DOWN`, `CENTER_AILER_RUDDER`, `*_CENTER` | Mascarar = **superfície travada** (pane gravíssima, quase sempre fatal) — só com decisão do usuário e nunca no profundor por inteiro; possível versão leve: curso limitado (cabo esgarçado). Receber = nada (a deflexão já é lida pelas SimVars). |

### Flaps
| Evento | Uso proposto |
|---|---|
| `FLAPS_UP/DOWN/1..4`, `FLAPS_INCR/DECR`, `FLAPS_SET`, `AXIS_FLAPS_SET`, `FLAPS_CONTINUOUS_*` | Receber (uso do flap). **Não mascarar:** a pane de flap escreve a superfície (`WriteFlapSurfacePercent`) e deixa a alavanca livre (F4.2e). |

### Spoilers
| Evento | Uso proposto |
|---|---|
| `SPOILERS_ARM_SET/ON/OFF/TOGGLE` | Receber: **spoilers armados antes do pouso** (jatos; nota de procedimento). |
| `SPOILERS_SET/ON/OFF/TOGGLE/INC/DEC`, `AXIS_SPOILER_SET` | Receber: speedbrake em velocidade alta (regra já existe pela SimVar); mascarar = **spoilers inoperantes** (pane/MEL: pista maior). |

### Slew
| Evento | Uso proposto |
|---|---|
| **`SLEW_ON`, `SLEW_SET`, `SLEW_TOGGLE`** | Receber: detecção imediata do slew. **Mascarar quando o avião já decolou** = o slew simplesmente não liga em voo (impede a trapaça em vez de punir; a invalidação continua como rede de segurança). Não fere o princípio acima (slew não é comando de voo). No solo antes da decolagem, deixar passar (regra aprovada). |
| `SLEW_OFF` | Enviar: o addon pode desligar o slew se ele for ligado por outro caminho em voo. |
| `SLEW_AHEAD/LEFT/RIGHT/ALTIT/PITCH/BANK/HEADING_*`, `AXIS_SLEW_*`, `SLEW_FREEZE`, `SLEW_RESET` | Não usar (movimento dentro do slew; a distância já é medida pela posição). |

### Leme e diversos
| Evento | Uso proposto |
|---|---|
| `AUTORUDDER_TOGGLE` | Receber: **auto-leme ligado** (ajuda de pilotagem). Mesmo tratamento do auto-start: não invalida, perde um pouco de XP (decisão do usuário para o auto-start; confirmar se vale também aqui). |
| `RUDDER_AXIS_MINUS/PLUS` | Não usar. |
| `CHVPP_*`, `CENTER_NT361_CHECK`, `MOUSE_AS_YOKE_*`, `FLAPS_DETENTS_SET`, `*_CONCORDE_*` | Não usar (hardware antigo / sem uso no sim). |

## 3. Decisões para o usuário
1. **Decidido (2026-11-16): só invalidar** — o slew em voo **não** é bloqueado; `SLEW_ON/SET/TOGGLE` podem ser recebidos apenas para registro, e a regra continua a da SLEW-INTEGRIDADE (invalidação).
2. **Compensador:** pane de trim inoperante (`*_TRIM_DISABLED_SET`) e compensador disparado (trim indo ao batente até o piloto cortar)?
3. **Auto-leme:** mesma regra do auto-start (perde XP, não invalida)?
4. Superfície travada: não fazer (recomendação) ou fazer só a versão leve (curso limitado)?

**Decididas pelo usuário (2026-11-19, `docs/tarefas_sem_IA.md`):**
2. **Compensador — fazer as duas panes:** trim inoperante (`ELEVATOR/AILERON/RUDDER_TRIM_DISABLED_SET`, compensador congela) e trim disparado (o addon move `ELEVATOR TRIM POSITION` ~0,5–1°/s até o batente; para quando o piloto corta o sistema). Runaway só em aviões com trim elétrico/servo (Baron, King Air, TBM, Citation, airliners), por flag no JSON; trim mecânico (C152/C172) só tem inoperante.
3. **Auto-leme:** mesma regra do auto-start — não invalida, −10 % de XP por assistência ativa entre a decolagem e o pouso, acumulável até −30 % (`AUTO COORDINATION`).
4. **Superfície travada:** não travar superfícies primárias; usar só a versão leve (curso limitado a ±55–65 %) e flap travado/assimétrico.

## 4. Próximos passos
1. Teste no MSFS: `ELEVATOR_TRIM_DISABLED_SET` (o trim para? o piloto consegue religar?), mascarar `SLEW_ON` no ar, receber `SPOILERS_ARM_SET` e `AUTORUDDER_TOGGLE`.
2. (Conferido: o addon não mascara mais os comandos de flap; nada a fazer.)
