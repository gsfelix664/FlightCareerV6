# Variáveis do sistema elétrico do MSFS 2024 — usos no FlightCareer

> **Atenção (2026-11-16):** esta análise foi feita antes de detectar as variáveis marcadas como obsoletas na documentação. Confira `variaveis-obsoletas.md` antes de usar qualquer item (ex.: `CRASH FLAG`, `CRASH SEQUENCE`, `STRUCT DAMAGEVISIBLE`, `SEMIBODY LOADFACTOR X/Z`, `COWL FLAPS`, `ENGINE PRIMER`, `ELEVATOR TRIM PCT` → `EX1`).

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) do SDK 2024 (111 SimVars: barramentos, baterias, disjuntores, circuitos, geradores, relés, energia externa, linhas, alternador, APU).
Mesmo formato de `variaveis-diversos.md`. **C152** = `probe-runs/matriz-C152.csv`. O C152 usa o **sistema elétrico novo** (v2, `NEW ELECTRICAL SYSTEM`): as variáveis legadas (`GENALT`, `MAIN BUS`, `AVIONICS BUS`, `BATTERY BUS`, `HOT BATTERY BUS`, `OLD CHARGING AMPS`) ficam 0 e só servem a aviões com sistema legado.
Índices v2+: barramento, bateria, circuito e gerador aceitam **nome ou índice**; os índices mudam por aeronave (Developer Mode › Electrical System Debug).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | carga da bateria (`ELECTRICAL BATTERY LEVEL`), tensão/amperagem do gerador e da bateria (carregando × descarregando), alternador desligado/desconectado, energia externa conectada, APU |
| Qualidade do voo / passageiros | **sinais de cabine** (`CIRCUIT CABIN SIGNAL GO/STANDBY/STOP`: aviso de cinto em turbulência, decolagem e pouso), aquecimento do pitot ligado em umidade com frio (procedimento) |
| Missões e economia | **energia externa (GPU)**: taxa da FBO e menos desgaste da bateria na partida; transponder ligado no espaço aéreo controlado; horas e ciclos da APU (custo e manutenção); avião elétrico (bateria = combustível) |
| Integridade da carreira | piloto reativando um disjuntor que o addon puxou (pane) ou religando um alternador em pane → watchdog/registro |
| Desgaste e panes | descarga profunda e capacidade estimada da bateria, carga do gerador × capacidade nominal (sobrecarga), ciclos do motor do flap e do trem (`CIRCUIT FLAP/GEAR MOTOR ON`), partidas (amperagem do circuito do starter); **disjuntores graváveis** como efeito de pane por sistema (GPS, piloto automático, transponder, flap, pitot heat, aviônicos) |

## 2. Tabela (o que interessa)

### Bateria
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `ELECTRICAL BATTERY LEVEL:n` | varia (97,6–100 %) | Reg | sim | **HUD:** carga. **Desgaste:** descarga profunda (já) e profundidade de cada ciclo. Não escrever. |
| `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT:n` | varia | Reg | — | Capacidade estimada: **saúde da bateria** comparável ao componente do addon; HUD de "bateria fraca". |
| `ELECTRICAL BATTERY VOLTAGE:n` / `LOAD:n` | varia (até 25,1 V / 174 A) | Reg | — | Carregando × descarregando (carga negativa/positiva); pico na partida → desgaste da bateria e do starter. |
| `ELECTRICAL MASTER BATTERY:n` | varia | Reg | sim | Já usado. |
| `BATTERY CONNECTION ON:n` / `BATTERY BREAKER PULLED:n` | constante | — | sim | **Pane:** bateria desconectada (efeito de falha da bateria); confirmação da escrita. |

### Barramentos e carga
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `ELECTRICAL BUS VOLTAGE:n` / `BUS AMPS:n` | varia `:0..2` | Reg | — | Já usados (sobrecarga, sag). **HUD:** tensão do barramento principal. |
| `ELECTRICAL TOTAL LOAD AMPS` | varia | Reg | — | Já usado (sobrecarga). |
| `BUS BREAKER PULLED:n` / `BUS CONNECTION ON:n` | constante | — | sim | **Pane:** perda de um barramento inteiro (aviônicos, essencial) em vez do `TOGGLE_ELECTRICAL_FAILURE` total. |
| `NEW ELECTRICAL SYSTEM` | constante | Ctx | — | Já no contexto: decide entre variáveis v2 e legadas. |
| Legadas (`ELECTRICAL MAIN/AVIONICS/BATTERY/HOT BATTERY BUS *`, `GENALT *`, `OLD CHARGING AMPS`) | 0 no C152 | Reg (algumas) | — | Só para aviões com sistema legado; manter como reserva. |

### Geradores e alternador
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `ELECTRICAL GENERATOR AMPS/VOLTAGE/ACTIVE:n` | varia (74 A / 28 V) | Reg (REGRAS-C152) | — | **Desgaste:** carga × `alternatorRatedAmps` (sobrecarga real do gerador; hoje o catálogo só tem a capacidade em 2/46 aeronaves). **HUD:** amperímetro. |
| `ELECTRICAL GENERATOR SWITCH:n` | varia | — | sim | Chave do gerador; confirmação de pane de alternador. |
| `ALTERNATOR CONNECTION ON:n` / `ALTERNATOR BREAKER PULLED:n` | constante | — | sim | **Confirmação** da pane de alternador que o `SimFailureActuator` aplica (hoje por `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`); **integridade:** detectar o piloto religando. |
| `GENERAL ENG MASTER ALTERNATOR:n` | varia | Reg | — | Já usado. |

### Circuitos (índice = circuito; nomes por aeronave)
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `CIRCUIT ON:n`, `ELECTRICAL CIRCUIT AMPS/VOLTAGE:n` | varia (circuito 2 até 147 A: provável starter) | Reg (`CIRCUIT ON:3`) | **Desgaste:** amperagem do starter = duração e esforço da partida; consumo por sistema. |
| `CIRCUIT FLAP MOTOR ON` | varia | — | **Desgaste:** ciclos e tempo do motor do flap (peça de flap/atuador). |
| `CIRCUIT GEAR MOTOR ON` | constante (trem fixo) | — | **Desgaste:** ciclos do motor do trem num retrátil elétrico. |
| `CIRCUIT PITOT HEAT ON` | varia | — | **Qualidade/procedimento:** aquecimento do pitot em umidade visível com frio; **desgaste:** gelo no pitot. |
| `CIRCUIT CABIN SIGNAL GO/STANDBY/STOP` | constante (C152 não tem) | — | **Passageiros:** sinal de cinto ligado na decolagem, pouso e turbulência → nota; desligado em turbulência → reclamação. Provar num jato/turbo-hélice. |
| `CIRCUIT TRANSPONDER ON`, `CIRCUIT COM ON`, `CIRCUIT NAV ON`, `CIRCUIT NAVCOM1..3 ON`, `CIRCUIT MARKER BEACON ON` | varia | — | **HUD/missões:** transponder e rádios alimentados (missão IFR/espaço controlado); confirmação de pane de aviônicos. |
| `CIRCUIT AUTOPILOT ON`, `CIRCUIT AVIONICS ON`, `CIRCUIT GENERAL PANEL ON` | varia / constante | — | Piloto automático e aviônicos alimentados: confirmação de pane e horas de piloto automático. |
| `CIRCUIT STALL WARNING ON`, `CIRCUIT GEAR WARNING ON` | constante | — | Aviso de estol/trem inoperante = pane de segurança (NO-GO na MEL futura). |
| `CIRCUIT PROP SYNC ON`, `CIRCUIT AUTO FEATHER ON`, `CIRCUIT HYDRAULIC PUMP/VALVE ON`, `CIRCUIT STANDBY VACUUM ON`, `CIRCUIT AUTO BRAKES ON` | constante | — | Bimotores/turbo-hélices/jatos: procedimentos (auto feather na decolagem) e panes por sistema. Provar nesses aviões. |
| `CIRCUIT SWITCH ON:n` | varia (`:2`, `:5..7`) | — | Chave de cada circuito; **gravável**. |
| `CIRCUIT BREAKER PULLED:n`, `BREAKER *` (ADF, ALTFLD, AUTOPILOT, AVNBUS1/2, AVNFAN, FLAP, GPS, INST, INSTLTS, LTS PWR, NAVCOM1..4, TURNCOORD, WARN, XPNDR) | constante | Reg (circuit) | **Pane por sistema:** puxar o disjuntor certo = GPS, piloto automático, transponder, rádio, flap ou instrumentos inoperantes (próxima fase do `AGENTS.md`: "disjuntores por aeronave"). **Gravável**; testar escrita e se o piloto consegue rearmar (então o watchdog reaplica, como no magneto). |
| `ELECTRICAL CIRCUIT EXISTS:n` | constante | — | **Contexto:** mapear quais circuitos o avião tem antes de escolher a pane. |
| `CIRCUIT ELECTRIC ENGINE ON` | constante | — | Aviões elétricos (ex.: Velis): motor elétrico; bateria vira o "combustível" da economia. |

### Energia externa
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `EXTERNAL POWER AVAILABLE:n` | varia | — | — | **Missões/economia:** GPU disponível no pátio; cobrar taxa da FBO. |
| `EXTERNAL POWER ON:n` / `CONNECTION ON:n` | constante | Reg (on) | sim (connection) | Partida com GPU: menos desgaste da bateria e do starter; **qualidade/procedimento:** taxiar com GPU conectada. |
| `ELECTRICAL EXTERNAL POWER AMPS/VOLTAGE:n` | constante | — | — | Consumo da GPU (taxa por tempo). |

### APU (jatos e turbo-hélices grandes)
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `APU PCT RPM`, `APU SWITCH`, `APU PCT STARTER` | constante | Reg (rpm) | **Economia/manutenção:** horas e ciclos da APU (combustível e inspeção); **HUD:** APU ligada. |
| `APU GENERATOR ACTIVE/SWITCH`, `APU VOLTS`, `BLEED AIR APU`, `APU BLEED PRESSURE RECEIVED BY ENGINE` | constante | — | Partida dos motores pela APU (procedimento); pane do gerador da APU. |
| `APU ON FIRE DETECTED` | constante | Reg | Já no registro (pane/incêndio). |

## 3. Não usar
- Relés (`ELECTRICAL RELAY *`) e linhas (`LINE CONNECTION ON`, `LINE BREAKER PULLED`): específicos de cada modelo; só se uma pane precisar deles.
- `BUS LOOKUP INDEX`, `CIRCUIT POWER SETTING` (dimmers), `CIRCUIT HOIST/SLING/ROTOR BRAKE ON` (helicóptero), `CIRCUIT CABIN SIGNAL *` só quando o avião tiver.
- Não escrever `ELECTRICAL BATTERY LEVEL` nem as conexões fora de um modo de pane testado.

## 4. Observações
- O sistema novo (v2) dá corrente e tensão por gerador, bateria, barramento e circuito: dá para trocar a estimativa atual de sobrecarga (tensão do barramento em [0,88; 0,95)) por **amperes do gerador × capacidade nominal**, desde que o catálogo tenha `alternatorRatedAmps` (2/46 hoje; o cfg do avião ou o POH dão o valor).
- Disjuntores são o caminho para **panes por sistema** que o SDK não tem como evento (GPS, piloto automático, transponder, flap elétrico). Exige mapear os índices por aeronave (`ELECTRICAL CIRCUIT EXISTS` + Electrical System Debug) e o modo de teste de panes do SimVarProbe.

## 5. Próximos passos sugeridos
1. **Registro:** `ELECTRICAL GENERATOR SWITCH`, `ALTERNATOR CONNECTION ON`, `CIRCUIT FLAP MOTOR ON`, `CIRCUIT GEAR MOTOR ON`, `CIRCUIT PITOT HEAT ON`, `CIRCUIT CABIN SIGNAL *`, `EXTERNAL POWER AVAILABLE`, `APU SWITCH`; contexto: `ELECTRICAL CIRCUIT EXISTS:0..N`.
2. **Passageiros:** sinal de cinto × fase do voo × turbulência (lógica pura, com testes).
3. **Economia:** GPU (taxa e partida com energia externa) e horas de APU.
4. **Panes:** disjuntores por sistema (testar escrita e rearme no sim); confirmação da pane de alternador por `ALTERNATOR CONNECTION ON`.
5. **No simulador (usuário):** C172 e um bimotor (dois geradores), um jato com APU e sinais de cabine, e o teste de puxar/rearmar disjuntores.
