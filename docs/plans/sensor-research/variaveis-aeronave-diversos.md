# Variáveis diversas da aeronave do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Miscellaneous Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) do SDK 2024 (116 SimVars: estado da aeronave, posição/velocidade, alertas, wear and tear, structs, velocidade do ar, temperatura, ornitóptero).
Mesmo formato das outras análises. **C152** = `probe-runs/matriz-C152.csv`. Nenhuma obsoleta nesta página (`variaveis-obsoletas.md`); as `STRUCT *` foram rejeitadas (tipo vetor). As `WEAR AND TEAR *` seguem a decisão do `AGENTS.md` › 6 (só leitura; tabela em `native-wear-mapping.md`).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | **vento relativo ao avião** (`AIRCRAFT WIND X/Z`: cruzado e de proa direto, sem conta), master caution/warning, alertas (`WARNING LOW HEIGHT`, combustível, vácuo, tensão, óleo), TAS/Mach/TAT, barber pole (Vmo/Mmo dinâmico) |
| Qualidade do voo / passageiros | taxas e acelerações angulares (`ROTATION VELOCITY/ACCELERATION BODY X/Y/Z`: rolagem/arfagem/guinada bruscas), aceleração lateral (`ACCELERATION BODY X`), **precisão do pouso** (`PLANE TOUCHDOWN LATITUDE/LONGITUDE` × cabeceira e zona de toque; `PLANE TOUCHDOWN HEADING` × rumo da pista), rampa de aproximação (`SLOPE TO ATC RUNWAY`) |
| Missões e economia | **chegada ao estacionamento** (`PLANE IN PARKING STATE`: fim da missão e taxa de estacionamento), pouso fora da pista (`ON ANY RUNWAY`), piso (`SURFACE TYPE/CONDITION`), **pintura/matrícula** (`LIVERY NAME/FOLDER`: casar a aeronave da carreira com a do sim), **capas e travas** (`COVER ON`: decolar com capa do pitot/entrada de ar) |
| Integridade da carreira | **modo slew** (`IS SLEW ACTIVE`: mover o avião sem voar = voo invalidado), saltos de posição, aeronave controlada (`IS USER SIM`) |
| Desgaste e panes | wear and tear nativo (já no registro), **`WEAR AND TEAR EXPOSED PARTS LEVEL/LOWEST LEVEL`** (variou: desgaste das partes expostas), `WEAR AND TEAR CHECK PROGRESSION` (inspeção do MSFS 2024, gravável), `STRUCTURAL ICE PCT`, alertas do avião como confirmação de pane (vácuo, óleo, tensão) |

## 2. Tabela (o que interessa)

### Estado da aeronave
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| **`COVER ON:n`**, `COVER GENERIC ON:n` | **varia** | — | sim | **Walkaround/qualidade:** capa do pitot, da entrada de ar, calços, travas de comando. Decolar com capa = pane (pitot bloqueado, superaquecimento) e nota ruim; o walkaround do app pode mandar tirar. |
| **`IS SLEW ACTIVE`**, `IS SLEW ALLOWED` | constante | — | sim | **Integridade:** slew no ar = voo invalidado (teletransporte). **No solo é legítimo** (aeroporto bugado, vaga sem pushback): permitir antes de iniciar o monitoramento e no solo antes da decolagem, com distância limitada e registro no blackbox (ver § 5). |
| **`PLANE IN PARKING STATE`** | constante (teste não estacionou) | — | — | **Missões/economia:** fim da missão no pátio (cobrar estacionamento, liberar serviços de solo). Provar estacionando numa vaga. |
| `ON ANY RUNWAY`, `SURFACE TYPE`, `SURFACE CONDITION`, `SURFACE INFO VALID` | varia / constante | Ctx | — | Pouso fora da pista, piso e pista molhada/gelada (já no contexto; levar ao registro para leitura contínua). |
| `LIVERY NAME`, `LIVERY FOLDER`, `TITLE` | constante | Ctx (title) | — | **Carreira:** identificar a pintura/matrícula do avião carregado e casar com a aeronave da frota (aviso se o piloto carregar outro avião). |
| `STRUCTURAL ICE PCT` | constante (sem gelo) | Reg, Tel | — | Já usado (gelo). |
| `CONTRAILS CONDITIONS MET`, `WINDSHIELD RAIN EFFECT AVAILABLE`, `IS USER SIM` | constante | — | — | Baixa prioridade (`IS USER SIM` para confirmar que é o avião do jogador). |

### Posição, velocidade e pouso
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `ROTATION VELOCITY BODY X/Y/Z` | varia | — | **Passageiros:** taxa de rolagem/arfagem/guinada acima de um limite = manobra brusca (por tempo). |
| `ROTATION ACCELERATION BODY X/Y/Z` | varia | — | Arrancos de comando; complemento do solavanco (`SEMIBODY LOADFACTOR YDOT`). |
| `ACCELERATION BODY X/Y/Z` | varia | Reg | Já usados (lateral no toque); lateral contínua = voo descoordenado. |
| `ACCELERATION WORLD X/Y/Z`, `VELOCITY BODY Z`, `TOTAL VELOCITY`, `SURFACE RELATIVE GROUND SPEED` | varia | Tel (parte) | Baixa prioridade (já há velocidade e aceleração no corpo). |
| **`PLANE TOUCHDOWN LATITUDE/LONGITUDE`** | varia | — | **Qualidade/missões:** distância do toque até a cabeceira e a zona de toque (pouso longo/curto); pista restante. |
| `PLANE TOUCHDOWN HEADING DEGREES MAGNETIC/TRUE` | varia | — | Alinhamento com a pista no toque (crab). |
| `PLANE TOUCHDOWN BANK/PITCH DEGREES`, `NORMAL VELOCITY` | varia | Reg | Já no registro (pouso duro, toque com asa baixa). |
| `SLOPE TO ATC RUNWAY` | constante (sem pista do ATC) | — | **Qualidade:** rampa na aproximação (estabilizada × alta/baixa); só com pista atribuída pelo ATC. |
| `AIRCRAFT AGL`, `AIRCRAFT ALTITUDE ABOVE OBSTACLES`, `PLANE ALT ABOVE GROUND MINUS CG` | varia | Ctx (AGL) | **Qualidade/segurança:** voo baixo sobre obstáculos fora de pouso/decolagem; altura real da roda no toque. |
| `PLANE HEADING DEGREES GYRO` | varia | — | Desvio do giro direcional (pane de instrumento). |

### Alertas
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `MASTER CAUTION ACTIVE/ACKNOWLEDGED`, `MASTER WARNING ACTIVE/ACKNOWLEDGED` | constante | — | **HUD:** espelho dos alertas; **qualidade:** alerta ativo e não reconhecido por muito tempo. Graváveis: não escrever. |
| `WARNING LOW HEIGHT` | varia | — | Alerta de altura baixa (perto do solo fora de configuração de pouso). |
| `WARNING VACUUM(_LEFT/_RIGHT)` | varia | Reg (geral) | Confirmação da pane de vácuo aplicada pelo addon. |
| `WARNING FUEL(_LEFT/_RIGHT)`, `WARNING OIL PRESSURE`, `WARNING VOLTAGE` | constante | Reg (alguns) | Alertas do painel coerentes com as panes (óleo, elétrico, combustível). |

### Velocidade do ar e temperatura
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| **`AIRCRAFT WIND X/Y/Z`** | varia | — | **HUD:** componente de vento cruzado e de proa já no eixo do avião; **qualidade:** tolerância no pouso com vento. |
| `AIRSPEED BARBER POLE`, `BARBER POLE MACH` | constante / varia | Reg (barber) | Vmo/Mmo com a altitude (camada SimVar dos limites de velocidade nos jatos). |
| `AIRSPEED TRUE`, `AIRSPEED MACH`, `AIRSPEED INDICATED THEORETICAL` | varia | Reg | TAS/Mach no HUD; "theoretical" = sem erro do pitot (detectar pitot bloqueado: indicada ≠ teórica). |
| `TOTAL AIR TEMPERATURE`, `STANDARD ATM TEMPERATURE` | varia | Ctx (TAT) | Desvio ISA (desempenho, gelo); TAT nos jatos. |
| `AIRCRAFT CATEGORY`, `AIRCRAFT OBJECT CLASS` | constante | — | Contexto (avião/helicóptero). |

### Wear and tear nativo
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `WEAR AND TEAR LEVEL/IS WEAK/IS FAILED/WEAK VALUE/OVERSTRESS DAMAGE RATE/OVERSTRESS FACTOR` | varia (parte) | Reg | Já no registro (decisão 0.1). |
| **`WEAR AND TEAR EXPOSED PARTS LEVEL`**, `LOWEST LEVEL` | **varia (0,86–1,0)** | Reg | Desgaste das partes expostas (pneus, para-brisa, bordo de ataque?): mapear para as peças do addon na calibração F1.4c. |
| `WEAR AND TEAR CHECK PROGRESSION:n` | constante | — | Progresso da inspeção do próprio MSFS 2024 (walkaround do sim), **gravável**. Possível integração: inspeção do Hangar refletida no sim (decisão futura; hoje nada é gravado no wear and tear). |

## 3. Não usar
- `STRUCT *` (rejeitadas), `EYEPOINT POSITION`, ornitóptero (`ORNITHOPTER *`), `AIRSPEED SELECT INDICATED OR TRUE`, `AIRSPEED TRUE RAW`, `WINDSHIELD WIND VELOCITY`.
- Não escrever posição, atitude, velocidades nem alertas (graváveis, mas quebrariam o voo ou serviriam de trapaça).

## 4. Próximos passos sugeridos
1. **Registro:** `COVER ON:0..N`, `IS SLEW ACTIVE`, `PLANE IN PARKING STATE`, `ON ANY RUNWAY`, `SURFACE CONDITION`, `ROTATION VELOCITY BODY X/Y/Z`, `PLANE TOUCHDOWN LATITUDE/LONGITUDE/HEADING DEGREES TRUE`, `AIRCRAFT WIND X/Z`, `MASTER CAUTION/WARNING ACTIVE`, `WARNING LOW HEIGHT`, `AIRSPEED INDICATED THEORETICAL`, `WEAR AND TEAR EXPOSED PARTS LEVEL`; contexto: `LIVERY NAME`, `LIVERY FOLDER`.
2. **Integridade:** slew ativo invalida o voo (junto com as travas de combustível).
3. **Qualidade/missões:** precisão do pouso (distância à cabeceira, alinhamento) e chegada ao estacionamento como fim de missão.
4. **Walkaround:** capas e travas no walkaround do app; decolar com capa vira pane.
5. **No simulador (usuário):** estacionar numa vaga, ligar slew em voo, colocar e tirar capas no walkaround do MSFS, aproximação com pista do ATC (rampa).

## 5. Slew no solo, pushback e matrícula (conversa com o usuário, 2026-11-16)
- **Slew (regra aprovada pelo usuário em 2026-11-16):** muitos aeroportos do MSFS têm a aeronave mal posicionada ou vaga sem pushback, e o piloto precisa reposicionar pelo slew. Regra: (a) livre **antes** de iniciar o monitoramento; (b) **no solo** depois de iniciado e antes da decolagem, permitido se o deslocamento total por slew for pequeno (ex.: ≤ 1 km, dentro do aeroporto de origem) — registrado no blackbox; (c) **no ar**, ou no solo com deslocamento grande (ex.: ir parar na cabeceira de outro aeroporto) = voo invalidado. Medir o deslocamento por `PLANE LATITUDE/LONGITUDE` enquanto `IS SLEW ACTIVE` = 1.
- **Pushback no HUD:** o SDK tem `PUSHBACK STATE` (gravável: reto/esquerda/direita), `PUSHBACK AVAILABLE`, `PUSHBACK ATTACHED`, `PUSHBACK WAIT` e o evento `TOGGLE_PUSHBACK` (confirmar os eventos de direção e velocidade do reboque na página de Key Events antes de usar). Um painel de pushback no HUD (empurrar reto/virar/parar) resolve vaga sem pushback do sim e vira serviço de solo cobrado (plano `taxas-aeroporto-servicos-solo.md`, `PUSHBACK`).
- **Matrícula:** `ATC ID` é **gravável** (string, página de rádio/ATC). O app pode gravar a matrícula da aeronave do Hangar no início do voo (o ATC passa a chamar pela matrícula da carreira). Aparecer **pintada** no avião depende da pintura usar a matrícula dinâmica (muitas pinturas do MSFS usam); confirmar no sim. Também graváveis: `ATC AIRLINE`, `ATC FLIGHT NUMBER` (voos de companhia), `ATC HEAVY`. Escrita de string exige definição com tipo `STRING32`/`STRING64`.
