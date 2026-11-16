# Variáveis "Miscellaneous" do MSFS 2024 — usos no FlightCareer

> **Atenção (2026-11-16):** esta análise foi feita antes de detectar as variáveis marcadas como obsoletas na documentação. Confira `variaveis-obsoletas.md` antes de usar qualquer item (ex.: `CRASH FLAG`, `CRASH SEQUENCE`, `STRUCT DAMAGEVISIBLE`, `SEMIBODY LOADFACTOR X/Z`, `COWL FLAPS`, `ENGINE PRIMER`, `ELEVATOR TRIM PCT` → `EX1`).

Criado em 2026-11-16 (Claude). Fonte: página [Miscellaneous Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/miscellaneous-variables/) do SDK 2024 (88 SimVars, categoria `Diversos` em `simvars-msfs2024.csv`).
Coluna **C152** = resultado do SimVarProbe em `probe-runs/matriz-C152.csv` (3 rodadas, 2026-11-16): `varia`, `constante` ou `rejeitada`. "Constante" num voo normal **não** prova que a variável está morta quando ela é uma opção do sim ou um evento raro (crash, fumaça).
Coluna **Hoje** = onde o app já lê: `Tel` (`SimTelemetry`), `Ctx` (`context_registry.json`, uma leitura no início), `Reg` (`sensor_registry.json`, REC e desgaste por peça), `Fail` (leitura rápida de panes).

Regra do plano `validacao-simvars.md`: nada entra no registro sem prova na matriz. Os usos abaixo são propostas; cada um vira tarefa própria.

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD (overlay) | `AMBIENT WIND *` (vento e componente cruzada), `AMBIENT TEMPERATURE`, `DENSITY ALTITUDE`, `AMBIENT IN CLOUD`/`AMBIENT VISIBILITY`, `AMBIENT PRECIP STATE` |
| Qualidade do voo / passageiros | `AMBIENT WIND Y` e `ENV CLOUD DENSITY` (turbulência: separar culpa do piloto × tempo), `AMBIENT IN CLOUD`, `AMBIENT PRECIP RATE`, `VELOCITY WORLD Y` (razão de descida real), `MOTION SIMULATION` (não contar pausa) |
| Missões e economia | `DENSITY ALTITUDE` (performance e peso máximo), `AMBIENT VISIBILITY`/`AMBIENT IN CLOUD` (VFR × IFR, bônus de tempo ruim), `AMBIENT PRECIP STATE`, `GROUND ALTITUDE` (pouso fora de aeroporto), `CATEGORY` |
| Integridade da carreira (anti-trapaça) | `REALISM CRASH DETECTION`, `REALISM CRASH WITH OTHERS`, `REALISM`, `MOTION SIMULATION`, `SIM DISABLED`, `USER INPUT ENABLED`, `VELOCITY WORLD X/Y/Z` (teletransporte/slew) |
| Desgaste e panes | ~~`CRASH SEQUENCE`~~ (obsoleta), `AMBIENT TEMPERATURE` contínua, `AMBIENT IN CLOUD` + temperatura (gelo), `AMBIENT PRECIP RATE`, `AMBIENT IN SMOKE`/`ENV SMOKE DENSITY` (a doc diz que fumaça aumenta o wear and tear nativo), `DENSITY ALTITUDE` |
| Diário de bordo / blackbox | `AMBIENT *` resumidos por fase (METAR "vivido" no voo), `MAGVAR`, `GROUND ALTITUDE` |

## 2. Tabela completa (o que interessa)

| Variável | Unidade | C152 | Hoje | HUD | Qualidade / pax | Missões / economia | Integridade | Desgaste / panes |
|---|---|---|---|---|---|---|---|---|
| `AMBIENT WIND VELOCITY` / `DIRECTION` | kt / ° (verdadeiro) | varia | Tel | vento atual e **componente cruzada/de cauda** na pista de destino (precisa do rumo da pista) | pouso com vento forte vale mais (normalizar nota do toque pelo vento) | bônus por pouso dentro do limite de vento cruzado demonstrado | — | `touchdown_crosswind_crab` com componente real em vez de só aceleração lateral |
| `AMBIENT WIND X/Y/Z` | m/s | varia | — | — | **`WIND Y` = rajada vertical**: G fora da faixa causado por turbulência não deveria pesar igual a manobra brusca do piloto | — | — | servos do piloto automático em turbulência (`autopilot_turbulence_servo_stress`) |
| `AMBIENT TEMPERATURE` | °C | varia | Tel, Ctx | OAT | conforto de cabine (calor/frio no solo com motor parado — ideia futura) | — | — | leitura **contínua** para partida a frio, gelo no carburador, choque térmico |
| `DENSITY ALTITUDE` | ft | varia | — | aviso "DA alta" na decolagem | — | performance: peso máximo de decolagem e pista mínima nas missões quentes/altas | — | decolagem quente e alta (temperatura de cilindro, potência máxima prolongada) |
| `AMBIENT DENSITY` | slug/ft³ | varia | — | TAS calculada | — | — | conferência do `DYNAMIC PRESSURE` | — |
| `AMBIENT PRESSURE` / `BAROMETER PRESSURE` | inHg / mb | varia | — | QNH de referência | — | — | ajuste do altímetro errado no pouso (comparar com `KOHLSMAN SETTING`, outra página) → nota de procedimento | — |
| `SEA LEVEL PRESSURE` / `SEA LEVEL AMBIENT TEMPERATURE` | mb / °C | constante / varia | — | QNH/temperatura de referência no briefing | — | — | — | — |
| `AMBIENT IN CLOUD` | bool | varia | — | "IMC" | passageiro não reclama de bank em nuvem? (decisão) | **VFR em IMC**: penalidade ou bloqueio em missão VFR; bônus em missão IFR | — | gelo (com temperatura 0 a −20 °C) quando `STRUCTURAL ICE PCT` não existir no avião |
| `ENV CLOUD DENSITY` | 0–1 | varia | — | — | intensidade da turbulência esperada | — | — | intensidade do gelo |
| `AMBIENT VISIBILITY` | m | varia | visibilidade | — | — | VFR × IFR, mínimos de pouso, bônus de tempo ruim | — | — |
| `AMBIENT PRECIP STATE` / `RATE` | máscara / mm | constante / varia | Tel, Ctx (state) | chuva/neve | — | bônus por tempo ruim; pista contaminada | — | gelo no carburador e pitot; `RATE` dá a intensidade |
| `AMBIENT IN SMOKE` / `ENV SMOKE DENSITY` | bool / 0–1 | constante | — | aviso de fumaça | — | — | — | **a doc diz que fumaça aumenta o wear and tear nativo**; calculado na câmera, não no avião (cuidado) |
| `STRUCT AMBIENT WIND` | struct | constante | — | — | — | — | — | — (usar `AMBIENT WIND X/Y/Z`) |
| ~~`CRASH FLAG`~~ (obsoleta) | enum | constante | Reg, Fail | — | — | — | — | ainda usado em produção (pane nativa): trocar pelo evento `Crashed` (ver `variaveis-obsoletas.md`) |
| ~~`CRASH SEQUENCE`~~ (obsoleta) | enum | constante | — | — | — | perda total e sinistro do seguro (F4.2b) | — | início e fim do crash: complemento do `CRASH FLAG` |
| `REALISM CRASH DETECTION` | bool | constante | — | aviso "detecção de crash desligada" | — | multiplicador de recompensa por realismo (ideia) | **crash desligado = `CRASH FLAG` nunca dispara**; o app julga o acidente sozinho (toque, G) | — |
| `REALISM CRASH WITH OTHERS` | bool | constante | — | — | — | — | idem para colisão com tráfego | — |
| `REALISM` | número | constante | — | — | — | multiplicador por realismo (decidir) | registrar no blackbox | — |
| `MOTION SIMULATION` | bool | varia | — | estado "PAUSADO" | **não contar violações durante pausa/menu** | — | falso em menu, pausa ativa e fase RTC de voo iniciado no ar | não desgastar com simulação parada |
| `SIM DISABLED` / `USER INPUT ENABLED` | bool | constante / varia | — | — | não contar violações em cena sem controle | — | cenas e carregamentos | idem |
| `SIM ON GROUND` | bool | varia | Tel, Ctx | — | já usado | — | — | já usado |
| `GROUND ALTITUDE` | m | varia | — | — | — | **pouso fora de aeroporto** com `ON ANY RUNWAY` (resgate, próxima fase) | — | — |
| `VELOCITY WORLD X/Y/Z`, `TOTAL WORLD VELOCITY` | ft/s | varia | — | — | `VELOCITY WORLD Y` no toque = razão de descida independente da atitude | — | **teletransporte/slew**: posição pula sem velocidade compatível → anular voo ou marcar | — |
| `MAGVAR` | ° | varia | — | rumo magnético da pista de destino (componente de vento) | — | — | — | — |
| `CATEGORY` | string | constante | — | — | — | barrar helicóptero/balão em missões de avião (hoje: `ATC TYPE`) | — | — |
| ~~`STRUCT DAMAGEVISIBLE`~~ (obsoleta) | ? | constante | — | — | — | — | — | sem documentação; ler depois de crash/impacto para descobrir |

## 3. Não usar
- **`AI *` (13):** suspeita das 3 quedas do MSFS no SimVarProbe (`HANDOFF.md`). Fora do registro até o teste com `--filter "^(?!ATC |AI )"`.
- **`STRUCT *` vetoriais** (`WORLD ACCELERATION`, `WORLDVELOCITY`, `SURFACE RELATIVE VELOCITY`, `WORLD ROTATION VELOCITY`): rejeitadas como número; exigem tipo XYZ do SimConnect e já há equivalentes escalares.
- **Graváveis que o app não deve escrever:** `VELOCITY WORLD *`, `SIM DISABLED`, `USER INPUT ENABLED`, `REALISM`, `MISSION SCORE`.
- Fauna, humanos, pilones, munição (CFS), animações, VR, TrackIR, paraquedas, `ARTIFICIAL GROUND ELEVATION`, `SIM SHOULD SET ON GROUND`, `VISUAL MODEL RADIUS`, `CONTROLLABLE`, `ANIMATION DELTA TIME`, `IDLE ANIMATION ID`.

## 4. Observações sobre a qualidade do voo atual
- `FlightQualityService.ProcessFrame` conta **uma violação por pacote** de telemetria (G, bank, pitch). Isso depende da frequência dos pacotes e fere a regra do `AGENTS.md` › 5 ("por tempo, não por pacote"). Ao mexer na nota dos passageiros, converter para **segundos em violação** (× `SIMULATION RATE`), pausar com `MOTION SIMULATION` = 0 e separar turbulência (`AMBIENT WIND Y`, `ENV CLOUD DENSITY`) de manobra do piloto.
- A nota do toque usa só `TouchdownFPM`; vento cruzado e pista contaminada (`AMBIENT PRECIP STATE`, `SURFACE CONDITION`) podem dar tolerância ou bônus.

## 5. Próximos passos sugeridos (tarefas separadas, depois da REGRAS-C152)
1. **Registro:** acrescentar ao `sensor_registry.json` `AMBIENT TEMPERATURE`, `AMBIENT WIND X/Y/Z`, `DENSITY ALTITUDE`, `AMBIENT IN CLOUD`, `ENV CLOUD DENSITY`, `AMBIENT PRECIP RATE`, `AMBIENT VISIBILITY`, `MOTION SIMULATION`, `USER INPUT ENABLED`, `VELOCITY WORLD Y`; ao contexto, `REALISM`, `REALISM CRASH DETECTION`, `REALISM CRASH WITH OTHERS`, `CATEGORY`, `SEA LEVEL PRESSURE`. Todas já aceitas pelo sim no C152.
2. **Qualidade do voo:** violações por tempo, pausa por `MOTION SIMULATION`, turbulência separada (com testes de lógica pura).
3. **HUD:** vento/componente cruzada, OAT, DA, IMC, aviso de crash desligado.
4. **Integridade:** aviso e regra de carreira quando a detecção de crash estiver desligada; detector de teletransporte.
5. **No simulador (usuário):** forçar um crash (`CRASH SEQUENCE`, `STRUCT DAMAGEVISIBLE`), voar em nuvem com temperatura negativa e em fumaça, pausar e abrir o menu em voo (`MOTION SIMULATION`).
