# Variáveis de sistemas da aeronave do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) do SDK 2024 (280 SimVars: gerais 76, luzes 52, pneumático 40, hidráulico 28, lançamento de líquido 22, painel parcial 17, reboque/gancho 10, estações de carga 8, variômetro 7, lastro de água 6, pressurização 6, saídas 5, visão noturna 3).
Mesmo formato das outras análises. **C152** = `probe-runs/matriz-C152.csv`. Obsoletas: `INSTRUMENTS AVAILABLE`, `SYSTEMS AVAILABLE`, `WISKEY COMPASS INDICATION DEGREES` (use `MAGNETIC COMPASS`), `STROBE FLASH`, `EXIT *` (use `INTERACTIVE POINT *`). Rejeitadas: `KOHLSMAN SETTING HG` (use `EX1`), `LANDING LIGHT PBH`. Lembrete: **`PARTIAL PANEL STATIC` não existe** (já causou bug; `AGENTS.md` › 6).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | luzes ligadas (`LIGHT * ON`), avisos de estol/sobrevelocidade/GPWS, ajuste do altímetro (`KOHLSMAN SETTING MB`/`HG EX1`, `KOHLSMAN SETTING STD`), altitude de cabine e razão (pressurizados), aquecimento do pitot |
| Qualidade do voo / passageiros | **bola do coordenador de curva** (`TURN COORDINATOR BALL`: voo descoordenado), **sinais de cabine** (`CABIN SEATBELTS/NO SMOKING ALERT SWITCH`), **conforto da cabine** (razão de altitude de cabine `PRESSURIZATION CABIN ALTITUDE RATE`, temperatura `PNEUMATICS AREA TEMPERATURE`), avisos (estol, GPWS, sobrevelocidade), **luzes por fase** (beacon antes da partida, strobe na pista, pouso abaixo de 10.000 ft, navegação à noite), altímetro ajustado no pouso |
| Missões e economia | **carga da missão no sim** (`PAYLOAD STATION WEIGHT:n` gravável: passageiros/carga com o peso real), tipos de missão novos (combate a incêndio `LIQUID DROPPING *`, reboque de planador/faixa `TOW *`, planador `VARIOMETER *`), recarga do extintor (`FIRE BOTTLE DISCHARGED` = custo de manutenção), ELT acionado (busca e salvamento) |
| Integridade da carreira | **congelamentos** (`IS ALTITUDE/ATTITUDE/LATITUDE LONGITUDE FREEZE ON`: voo "congelado" = trapaça), ELT |
| Desgaste e panes | **painel parcial gravável** (`PARTIAL PANEL AIRSPEED/ALTIMETER/ATTITUDE/HEADING/COMPASS/VERTICAL VELOCITY/NAV/COMM/TRANSPONDER/ADF/...`: pane de instrumento **nativa por instrumento**), **vazamento hidráulico** (`HYDRAULIC RESERVOIR PERCENT` gravável), integridade hidráulica, pressurização (descompressão), deriva do giro (`GYRO DRIFT ERROR`), fonte estática alternativa (procedimento da pane de estática) |

## 2. Tabela (o que interessa)

### Gerais
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| **`TURN COORDINATOR BALL`** | **varia** | — | — | **Qualidade/passageiros:** bola fora do centro por tempo (glissada/derrapagem); complementa `INCIDENCE BETA`. |
| `TURN INDICATOR RATE`, `DELTA HEADING RATE` | varia | — | — | Curva padrão (3°/s) no IFR; curvas bruscas. |
| `CABIN SEATBELTS ALERT SWITCH`, `CABIN NO SMOKING ALERT SWITCH` | constante (C152 não tem) | — | — | **Passageiros:** cinto ligado na decolagem, pouso e turbulência (igual aos `CIRCUIT CABIN SIGNAL *` da página elétrica; usar o que o avião tiver). |
| `STALL WARNING`, `OVERSPEED WARNING`, `GPWS WARNING`, `GPWS SYSTEM ACTIVE` | constante | Reg (stall, overspeed) | — | **Qualidade:** tempo com aviso ativo; GPWS = aproximação perigosa do terreno. |
| `KOHLSMAN SETTING MB`, `KOHLSMAN SETTING HG EX1`, `KOHLSMAN SETTING STD`, `INDICATED ALTITUDE`, `INDICATED ALTITUDE CALIBRATED`, `PRESSURE ALTITUDE` | varia (parte) | — | — | **Procedimento:** altímetro ajustado (QNH) no pouso × `SEA LEVEL PRESSURE`; STD acima da altitude de transição. |
| `PITOT HEAT`, `PITOT HEAT SWITCH`, `PITOT ICE PCT`, `WINDSHIELD/STRUCTURAL/PANEL ANTI ICE` | varia / constante | Reg | — | Procedimento em gelo (já no registro em parte). |
| `ALTERNATE STATIC SOURCE OPEN` | constante | Reg | — | **Procedimento da pane de estática:** abrir a fonte alternativa (o addon pode dar crédito/nota). |
| `GYRO DRIFT ERROR`, `HEADING INDICATOR`, `ATTITUDE INDICATOR PITCH/BANK DEGREES`, `MAGNETIC COMPASS` | varia | — | — | **Desgaste de instrumentos:** deriva do giro direcional; atitude indicada × real = pane do horizonte (confirmação). |
| `SUCTION PRESSURE` | varia | Reg, Tel | sim | Já usado (vácuo). |
| `ANGLE OF ATTACK INDICATOR`, `STALL PROTECTION *`, `HAS STALL PROTECTION` | varia / constante | — | — | AoA no HUD (alternativa à `INCIDENCE ALPHA`); proteção de estol (jatos). |
| **`IS ALTITUDE FREEZE ON`, `IS ATTITUDE FREEZE ON`, `IS LATITUDE LONGITUDE FREEZE ON`** | constante | — | — | **Integridade:** qualquer congelamento durante o voo monitorado = voo invalidado (mesmo tratamento do slew no ar). |
| `ELT ACTIVATED` | constante | — | — | Acidente/pouso muito duro aciona o ELT; **missões:** busca e salvamento (futuro). |
| `FIRE BOTTLE DISCHARGED`, `FIRE BOTTLE SWITCH` | constante | — | — | **Procedimento:** extintor do motor disparado no fogo; **economia:** recarga do extintor = custo de manutenção. |
| `AUTOTHROTTLE ACTIVE`, `AVIONICS MASTER SWITCH` | constante | Reg (avionics) | — | Horas com automação (diário); aviônicos na partida (já em regra). |
| `SMOKE ENABLE`, `SMOKESYSTEM AVAILABLE` | constante | — | sim | Aviões de acrobacia (missões de show aéreo, futuro). |
| `CANOPY OPEN` | constante | — | sim | Canopi aberto em voo (aviões de canopi), igual às portas. |

### Luzes (liga com o plano `iluminacao-panes-disjuntor.md`)
| Variável | C152 | Grav. | Uso proposto |
|---|---|---|---|
| `LIGHT LANDING/TAXI/NAV/BEACON/STROBE/LOGO/WING/RECOGNITION/PANEL/CABIN` (chave) | varia (parte) | **sim** | Estado da chave; **pane de lâmpada queimada:** forçar a chave desligada (watchdog) quando não houver mapa de disjuntor. |
| `LIGHT * ON` (luz acesa de fato) | varia | — | **Desgaste:** horas acesas e ciclos por lâmpada; **procedimento:** luzes por fase; chave ligada e luz apagada = lâmpada queimada (confirmação). |
| `LIGHT STATES`, `LIGHT ON STATES` (máscaras) | varia | — | Leitura compacta de todas as luzes num campo só (máscara de bits). |
| `LIGHT * POWER SETTING`, `* INTENSITY`, cores ambientes | constante | sim | Não usar. |
| `STROBES AVAILABLE`, `HAS ANY CABIN SIGNAL LIGHT` | constante | — | Contexto (o avião tem strobe / sinais de cabine). |

### Painel parcial (pane de instrumento)
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| **`PARTIAL PANEL AIRSPEED/ALTIMETER/ATTITUDE/HEADING/COMPASS/VERTICAL VELOCITY`** | constante | Reg (parte) | **sim** | **Pane nativa por instrumento** (0 = ok, 1 = falha, 2 = em branco — conferir o enum na documentação): velocímetro, altímetro, horizonte, giro direcional, bússola, climb. Efeitos novos em `FailureEffect` para o componente Instrumentos. |
| `PARTIAL PANEL NAV/COMM/TRANSPONDER/ADF` | constante | — | **sim** | Pane de aviônicos por rádio (alternativa aos disjuntores quando o avião não tem mapa elétrico). |
| `PARTIAL PANEL VACUUM/PITOT/ELECTRICAL/ENGINE` | constante / varia | Reg, Tel | sim | Já lidos (confirmação das panes atuais). |
| `PARTIAL PANEL AVIONICS/FUEL INDICATOR/TURN COORDINATOR` | constante | — | — | Só leitura (confirmação). |

### Hidráulico
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `HYDRAULIC RESERVOIR PERCENT` | constante (sem hidráulico) | Reg | **sim** | **Pane: vazamento hidráulico** progressivo (mesmo modelo do óleo); reparo no Hangar. |
| `HYDRAULIC SYSTEM INTEGRITY`, `HYDRAULIC ACTUATOR PCT INTEGRITY`, `HYDRAULIC PRESSURE`, `HYDRAULIC PUMP PRESSURE/ACTIVE` | constante | Reg (parte) | — | Panes nativas e confirmação (bomba, atuador). |
| `HYDRAULIC VALVE TARGET POS`, `HYDRAULIC ACTUATOR ACTIVE`, `HYDRAULIC PTU *` | constante | — | sim | Específicos do avião; só com teste. |

### Pressurização e pneumático
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| **`PRESSURIZATION CABIN ALTITUDE RATE`**, `PNEUMATICS CABIN ALTITUDE RATE` | constante (não pressurizado) | — | **Passageiros:** razão de cabine acima de ~500 ft/min subindo / ~300 ft/min descendo = desconforto no ouvido (reclamação). |
| `PRESSURIZATION CABIN ALTITUDE`, `PRESSURIZATION PRESSURE DIFFERENTIAL`, `PRESSURIZATION CABIN ALTITUDE GOAL`, `PRESSURIZATION DUMP SWITCH` | constante | Reg (parte) | Altitude de cabine > 10.000 ft = emergência (oxigênio; NO-GO futuro); despressurização. |
| **`PNEUMATICS AREA TEMPERATURE`**, `PNEUMATICS AREA TARGET TEMPERATURE` | constante | — | **Passageiros:** cabine muito quente/fria (embarque no calor sem ar-condicionado). |
| `PNEUMATICS PACK STATUS/SWITCH/FLOW`, `PNEUMATICS ENGINE BLEED AIR(_PRESSURE)`, `PNEUMATICS APU BLEED AIR`, `PNEUMATICS DUCT PRESSURE/TEMPERATURE` | constante | Reg (parte) | Procedimento (packs/sangria) e panes do sistema de pressurização. |

### Estações de carga (liga com o plano de peso e balanceamento)
| Variável | C152 | Grav. | Uso proposto |
|---|---|---|---|
| **`PAYLOAD STATION WEIGHT:n`** | **varia** | **sim** | **Missões:** o app grava o peso real dos passageiros e da carga da missão nas estações do avião (peso e CG corretos no sim). |
| `PAYLOAD STATION COUNT`, `PAYLOAD STATION NAME:n`, `PAYLOAD STATION OBJECT:n`, `PAYLOAD STATION NUM SIMOBJECTS:n` | constante / varia | — | Mapa das estações por avião (piloto, passageiros, bagageiro). |
| `DROPPABLE OBJECTS *` | constante | sim (type) | Missões de lançamento de carga (futuro). |

### Tipos de missão especiais (futuro)
| Grupo | Uso proposto |
|---|---|
| `LIQUID DROPPING *` (22) | **Combate a incêndio** (anfíbios do MSFS 2024): água recolhida e lançada → missão paga por lançamento. |
| `TOW CABLE *`, `TOW CONNECTION`, `TOW PLANE CONNECTION`, `TOW RELEASE HANDLE`, `GRAPPLE HOOK SET`, `LEAD POLE SET` | Reboque de planador e de faixa; gancho (helicóptero). |
| `VARIOMETER *`, `WATER BALLAST *` | Planadores. |
| `TAILHOOK *`, `NIGHT VISION *` | Fora do perfil da carreira. |

## 3. Não usar
- Obsoletas (`INSTRUMENTS AVAILABLE`, `SYSTEMS AVAILABLE`, `WISKEY COMPASS INDICATION DEGREES`, `STROBE FLASH`, `EXIT *`), rejeitadas (`KOHLSMAN SETTING HG`, `LANDING LIGHT PBH`).
- `AIRSPEED TRUE CALIBRATE`, `ANEMOMETER PCT RPM`, `ATTITUDE BARS POSITION/CAGE`, `AUDIO PANEL *`, `INTERCOM *`, `SPEAKER ACTIVE`, `DME SOUND`, `RAD INS SWITCH`, `SELECTED DME`, `EXTERNAL SYSTEM VALUE`, `GLASSCOCKPIT AUTOMATIC BRIGHTNESS`, cores e intensidades de luz, `PNEUMATICS COMPONENT *`/`JUNCTION *`.
- Não escrever `INDICATED ALTITUDE`, `PLANE HEADING DEGREES GYRO`, `AUTO COORDINATION`, `TRUE AIRSPEED SELECTED` (graváveis, mas falseiam instrumentos ou ajudas).

## 4. Próximos passos sugeridos
1. **Panes de instrumento nativas** por `PARTIAL PANEL *` (gravável) para o componente Instrumentos: velocímetro, altímetro, horizonte, giro direcional, climb, bússola e rádios. Testar a escrita no MSFS (modo de panes do SimVarProbe).
2. **Vazamento hidráulico** por `HYDRAULIC RESERVOIR PERCENT`.
3. **Integridade:** congelamentos (`IS * FREEZE ON`) invalidam o voo (junto com a SLEW-INTEGRIDADE).
4. **Passageiros:** bola do coordenador, sinais de cabine, razão e temperatura de cabine (por tempo, com testes).
5. **Missões:** gravar a carga nas estações (`PAYLOAD STATION WEIGHT`), com o plano de peso e balanceamento.
6. **Luzes:** horas acesas por lâmpada e luzes por fase (plano de iluminação).
7. **No simulador (usuário):** teste de escrita de `PARTIAL PANEL *` e `PAYLOAD STATION WEIGHT`; avião pressurizado (razão de cabine); jato com sinais de cabine; ligar um congelamento no menu de desenvolvedor.
