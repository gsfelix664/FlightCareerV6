# Variáveis de modelo de voo do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Flight Model Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/) do SDK 2024 (99 SimVars: gerais, centro de gravidade, pontos interativos, peso).
Mesmo formato das outras análises. **C152** = `probe-runs/matriz-C152.csv`. Obsoletas conferidas com `variaveis-obsoletas.md` (`SEMIBODY LOADFACTOR X/Z`, `CG MAX/MIN MACH`). Liga-se ao plano pendente `docs/plans/simconnect-wb-deteccao-aeronave.md` (peso e balanceamento).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | **peso e CG ao vivo** (`TOTAL WEIGHT`, `CG PERCENT` × `CG FWD/AFT LIMIT`), **ângulo de ataque/margem de estol** (`INCIDENCE ALPHA` × `STALL ALPHA`), derrapagem (`INCIDENCE BETA`), velocidades de projeto (Vs0, Vs1, Vr, Vc) como marcadores, altura de decisão do PFD, portas abertas |
| Qualidade do voo / passageiros | **solavanco** (`SEMIBODY LOADFACTOR YDOT`: taxa de variação da carga = sensação de "tranco"), voo descoordenado (`INCIDENCE BETA`, `BETA DOT`), aproximação do estol (alfa perto do `STALL ALPHA`), porta aberta em voo |
| Missões e economia | **capacidade de carga por aeronave** (`MAX TAKEOFF/LANDING/ZERO FUEL WEIGHT`, `EMPTY WEIGHT`), peso de decolagem/pouso dentro do limite, **CG fora do envelope** (NO-GO), planejamento (`DESIGN CRUISE ALT`, `ESTIMATED CRUISE SPEED`, `TYPICAL DESCENT RATE`), porte para taxas (`WING SPAN`, `MAX GROSS WEIGHT`), **jetway** (taxa de ponte de embarque), avião convencional (`IS TAIL DRAGGER`: habilitação na carreira) |
| Integridade da carreira | **peso mudando em voo** sem combustível correspondente (carga tirada/alterada no menu do sim), CG saltando em voo |
| Desgaste e panes | pouso/decolagem acima do peso (inspeção estrutural), operação fora do CG, solavanco/turbulência (fadiga), estol/parafuso (estrutura e comandos), **porta aberta com risco** (`IS ANY OPEN INTERACTIVE POINTS RISKING TO CAUSE CRASH`) → dano em dobradiça/porta |

## 2. Tabela (o que interessa)

### Gerais
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `G FORCE`, `MAX G FORCE` | varia | Reg, Tel | Já usados (desgaste e qualidade). `MIN G FORCE` (Reg) não mudou no C152: usar o mínimo de `G FORCE` no voo. |
| **`SEMIBODY LOADFACTOR YDOT`** | **varia (−1,8 a +4,3 /s)** | — | **Passageiros:** solavanco por tempo (substitui contar pacotes com G fora de 0,4–1,6 no `FlightQualityService`); **desgaste:** fadiga por turbulência. |
| `SEMIBODY LOADFACTOR Y` | varia | Reg | Fator de carga vertical; já no registro. |
| `INCIDENCE ALPHA` + `STALL ALPHA` (+ `ZERO LIFT ALPHA`) | alfa varia; estol constante | — | **HUD:** indicador de ângulo de ataque (% até o estol); **qualidade:** tempo perto do estol (alfa > 85–90 % do estol); **desgaste:** estol/buffet. |
| `INCIDENCE BETA`, `BETA DOT` | varia | — | **Qualidade/passageiros:** voo descoordenado (derrapagem/glissada) e "bola fora do centro"; no pouso com vento cruzado, alinhamento no toque. |
| `DYNAMIC PRESSURE`, `SIGMA SQRT` | varia | Reg (dyn) | Comando × Va (plano); `SIGMA SQRT` dá IAS↔TAS. |
| `DESIGN SPEED VS0/VS1/VC`, `DESIGN TAKEOFF SPEED`, `DESIGN SPEED MIN ROTATION`, `DESIGN SPEED CLIMB` | constante | Reg (Vs0/Vs1/Vc) | **HUD:** marcadores (Vr, Vy); **qualidade:** rotação abaixo de Vr; aproximação × 1,3·Vs0. |
| `DESIGN CRUISE ALT`, `ESTIMATED CRUISE SPEED`, `TYPICAL DESCENT RATE`, `MIN DRAG VELOCITY` | constante | — | **Missões:** tempo de voo, combustível e pagamento estimados por aeronave; altitude típica no briefing; melhor planeio (`MIN DRAG VELOCITY`) no HUD de emergência. |
| `REFERENCE SPEED MAX IAS (GEAR DOWN)`, `MACH MAX OPERATE` | constante | Reg | Camada SimVar de Vmo/Vle/Mmo (abaixo do POH). |
| `DECISION HEIGHT`, `DECISION ALTITUDE MSL` | constante (C152 sem PFD) | — | **Treino/missões IFR:** mínimos ajustados pelo piloto; descer abaixo sem pista à vista (futuro) e briefing. |
| `G LIMITER SETTING` | constante | — | Jatos/fly-by-wire: proteção de G ativa. |
| `IS TAIL DRAGGER`, `WING SPAN`, `WING AREA`, `STATIC CG TO GROUND`, `STATIC PITCH` | constante | — | **Contexto:** convencional (habilitação, missões de pista curta), porte/envergadura (taxas, aeroportos que comportam), dados do avião no Hangar. |
| `WING FLEX PCT` | constante | Reg | Já no registro (flexão da asa em turbulência). |
| `YAW STRING ANGLE/PCT EXTENDED` | constante | — | Planadores; fora da frota. |

### Centro de gravidade
| Variável | C152 | Uso proposto |
|---|---|---|
| **`CG PERCENT`** | **varia (0,12–0,30)** | **HUD/briefing:** CG atual; **missões:** carregamento correto; **desgaste/integridade:** fora do envelope. |
| `CG FWD LIMIT`, `CG AFT LIMIT` (e `CG FEET *` equivalentes) | constante | Limites do envelope por aeronave (camada SimVar abaixo do POH). |
| `CG PERCENT LATERAL`, `CG FEET LATERAL (LEFT/RIGHT LIMIT)` | varia | Desbalanceamento lateral (combustível mal distribuído entre asas) → aviso no HUD; qualidade. |
| `CG FEET`, `STATIC CG TO GROUND` | constante | Dados para o W&B do plano `simconnect-wb-deteccao-aeronave.md`. |

### Peso
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| **`TOTAL WEIGHT`** | **varia (1.158–1.634 lb)** | Tel | **Missões:** peso de decolagem; **desgaste:** pouso/decolagem acima do peso; **integridade:** salto de peso em voo sem queima de combustível = carga alterada. |
| `MAX TAKEOFF WEIGHT`, `MAX LANDING WEIGHT`, `MAX ZERO FUEL WEIGHT` | constante | — | Limites (camada SimVar). Zero fuel: carga paga máxima da missão. |
| `EMPTY WEIGHT`, `MAX GROSS WEIGHT` | constante | Tel | **Missões/economia:** capacidade útil (MTOW − vazio − combustível) para gerar missões que cabem no avião. |
| `* MOI` (momentos de inércia) | varia | — | Não usar (física do sim). |

### Pontos interativos (portas, escotilhas, ponte de embarque)
| Variável | C152 | Uso proposto |
|---|---|---|
| **`INTERACTIVE POINT OPEN:n`** (substitui a obsoleta `EXIT OPEN`) | **varia** | Porta/janela aberta: **HUD** (aviso antes da partida/táxi); **passageiros:** embarque com porta aberta; **qualidade:** decolar com porta aberta. Gravável: abrir/fechar (não usar sem decisão). |
| `INTERACTIVE POINT TYPE:n` | constante | Tipo do ponto (porta principal, carga, emergência…) para saber qual está aberto. |
| **`IS ANY OPEN INTERACTIVE POINTS RISKING TO CAUSE CRASH`** | **varia** | **Desgaste/pane:** porta aberta em velocidade = dano na porta/dobradiça (e o próprio sim pode causar crash); **HUD:** alerta vermelho. |
| `INTERACTIVE POINT GOAL:n` | varia | Posição-alvo da porta (animação em curso). |
| `INTERACTIVE POINT JETWAY *` | constante | **Economia:** ponte de embarque acoplada = taxa de jetway (plano de taxas de solo). Provar num aeroporto com jetway. |
| `INTERACTIVE POINT POSX/Y/Z`, `BANK/HEADING/PITCH`, variantes `EX1` | constante | Geometria; não usar. |

## 3. Não usar
- Obsoletas: `SEMIBODY LOADFACTOR X/Z`, `CG MAX MACH`, `CG MIN MACH`.
- `LINEAR CL ALPHA`, `SIMULATED RADIUS`, `DESIGN SPAWN ALTITUDE *`, momentos de inércia, geometria dos pontos interativos, variantes `EX1`.

## 4. Próximos passos sugeridos
1. **Registro:** `SEMIBODY LOADFACTOR YDOT`, `INCIDENCE ALPHA`, `STALL ALPHA`, `INCIDENCE BETA`, `CG PERCENT`, `CG PERCENT LATERAL`, `TOTAL WEIGHT`, `INTERACTIVE POINT OPEN:0..N`, `IS ANY OPEN INTERACTIVE POINTS RISKING TO CAUSE CRASH`; contexto: `CG FWD/AFT LIMIT`, `MAX TAKEOFF/LANDING/ZERO FUEL WEIGHT`, `EMPTY WEIGHT`, `DESIGN TAKEOFF SPEED`, `DESIGN SPEED MIN ROTATION`, `DESIGN CRUISE ALT`, `ESTIMATED CRUISE SPEED`, `IS TAIL DRAGGER`, `WING SPAN`.
2. **Passageiros:** trocar a contagem por pacote do `FlightQualityService` por solavanco (`YDOT`) e descoordenação (`BETA`) por tempo, com testes.
3. **Missões:** capacidade útil e CG no gerador de missões (plano W&B); peso acima do MTOW/MLW como regra de desgaste e de missão.
4. **HUD:** AoA, peso/CG, Vr, portas.
5. **Integridade:** detector de salto de peso em voo.
6. **No simulador (usuário):** avião com PFD (DH), jato com jetway e limitador de G, voo com porta aberta (risco de crash), estol de propósito.
