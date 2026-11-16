# Variáveis de motor do MSFS 2024 — usos no FlightCareer

> **Atenção (2026-11-16):** confira `variaveis-obsoletas.md` antes de usar qualquer item (ex.: `COWL FLAPS`, `ENGINE PRIMER`, `ENGINE MIXURE AVAILABLE`, `OIL AMOUNT`, `PROPELLER ADVANCED SELECTION`, `ENG RPM SCALER`, `GENERAL ENG FIRE DETECTED`).

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) do SDK 2024 (189 SimVars: gerais, turbina, pistão, hélice).
Mesmo formato de `variaveis-diversos.md`. **C152** = `probe-runs/matriz-C152.csv`. É a área **mais coberta** do registro: a maioria das variáveis de desgaste do motor já é lida (`Reg`). Aqui o foco é o que falta e os usos **fora do desgaste**. Turbina, turbo, hélice de passo variável, embandeiramento e reverso ficaram constantes no C152 (esperado).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | fluxo de combustível (`ENG FUEL FLOW GPH/PPH`), **% de potência** (`RECIP ENG BRAKE POWER PCT`), **temperatura do carburador** (`RECIP CARBURETOR TEMPERATURE`), combustível usado (`GENERAL ENG FUEL USED SINCE START`), autonomia (`ESTIMATED FUEL FLOW`) |
| Qualidade do voo / pilotagem | **empobrecimento da mistura** (`RECIP MIXTURE RATIO` × potência × fluxo), regime de cruzeiro (65–75 %), flapes de arrefecimento (`RECIP ENG COWL FLAP POSITION`) por fase, procedimento de gelo (`ENG ANTI ICE`, `RECIP ENG ALTERNATE AIR POSITION`, aquecimento do carburador), auto-feather armado na decolagem (bimotor), reverso no pouso |
| Missões e economia | **Hobbs do sim** (`GENERAL ENG HOBBS ELAPSED TIME`) para cobrança de horas e manutenção; combustível consumido real; **decolagem com potência reduzida** em jatos (`GENERAL ENG THROTTLE MANAGED MODE`: FLX × TOGA) = menos desgaste e custo; horas de APU (página elétrica) |
| Integridade da carreira | Hobbs/tempo do motor do sim × horas do app (detecta aceleração/edição); `GENERAL ENG ELAPSED TIME` (acumulado do sim, ~17.500 h no C152: não zera entre voos) |
| Desgaste e panes | já coberto no registro (CHT, EGT, óleo, RPM, detonação, saúde dos cilindros, TIT, torque, ITT, N1/N2); **graváveis** que podem virar efeito de pane: falha do turbo, falta de combustível, perdas de eficiência da turbina, limite de manete (perda parcial de potência) |

## 2. O que interessa (fora do que já está no registro)

### Gerais
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `ENG FUEL FLOW GPH:n` / `PPH:n` | varia (0–8,9 gph) | — (Reg tem `RECIP ENG FUEL FLOW`) | — | **HUD:** fluxo e autonomia; **qualidade:** consumo × potência (piloto que empobrece bem gasta menos). |
| `GENERAL ENG FUEL USED SINCE START:n` | varia | — | — | **Economia:** combustível consumido real por motor (conferir o cálculo atual do `FlightService`); diário de bordo. |
| `GENERAL ENG HOBBS ELAPSED TIME:n` | varia (s) | — | — | **Economia/manutenção:** horas Hobbs (aluguel por hora, inspeções 50/100 h); **integridade:** comparar com as horas que o app contou. |
| `GENERAL ENG ELAPSED TIME:n` | varia (≈17.500 h) | — (plano) | — | Acumulado do sim, **não** zera por voo: só a diferença no voo serve. Integridade (salto = edição/troca de avião). |
| `ESTIMATED FUEL FLOW`, `ENG FUEL FLOW BUG POSITION` | constante | — | — | HUD (jatos/G1000): estimativa e marcador. |
| `GENERAL ENG THROTTLE MANAGED MODE:n` | constante | — | — | **Jatos:** detente do manete (TOGA, FLX/MCT, CL): decolagem reduzida = menos estresse da seção quente; nota de procedimento. Provar num A320/737. |
| `GENERAL ENG REVERSE THRUST ENGAGED:n` | constante | Reg | — | **Qualidade/economia:** reverso no pouso (menos desgaste de freio); reverso em velocidade baixa demais → FOD/desgaste do motor. |
| `SHUTOFF VALVE PULLED`, `MASTER IGNITION SWITCH` | constante | — | — | **Treino/procedimento:** corte de combustível no fogo do motor (checklist de emergência). |
| `THROTTLE LOWER LIMIT`, `THROTTLE INPUT BLOCKED BY UPPER/LOWER BOUNDS` | constante | — | sim (bloqueios) | **Pane:** limitar o curso do manete = **perda parcial de potência** no sim (hoje só existe no app, `effect: None`). Testar. |
| `FULL THROTTLE THRUST TO WEIGHT RATIO`, `PROP THRUST:n` | varia | — | — | Desempenho (missões de pista curta/peso); baixa prioridade. |
| `COWL FLAPS:n` / `RECIP ENG COWL FLAP POSITION:n` | constante (C152 não tem) | Reg (recip) | sim (recip) | **Qualidade:** fechado na descida/aberto na subida (CHT); **pane:** cowl flap travado. |
| `ENGINE PRIMER`, `ENGINE CONTROL SELECT`, `ENGINE MIXURE AVAILABLE` | constante | — | — | Contexto/partida; baixa prioridade. |
| `BLEED AIR ENGINE:n`, `BLEED AIR SOURCE CONTROL:n` | constante | — | — | Jatos/turbo-hélices: sangria e pressurização (página de pressurização). |

### Pistão
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `RECIP CARBURETOR TEMPERATURE:n` | **varia (8–25 °C)** | Reg, Tel | — | **HUD:** termômetro do carburador (faixa amarela −15 a +5 °C); entrada direta do `CarbIcingMonitor`. |
| `RECIP MIXTURE RATIO:n` | varia | Reg | — | **Qualidade:** mistura rica demais em cruzeiro (consumo) ou pobre demais em alta potência (EGT/CHT); nota "gestão do motor". |
| `RECIP ENG BRAKE POWER PCT:n` | varia (até 122 %) | Reg | — | **HUD:** % de potência; **qualidade:** cruzeiro acima de 75 % sem necessidade; > 100 % = sobrepotência. |
| `RECIP ENG STARTER TORQUE:n` | varia | Reg | sim | Partidas longas/forçadas (já há regra de starter). |
| `RECIP ENG TURBOCHARGER FAILED:n` | constante | Reg | **sim** | **Pane:** escrever = **falha do turbo** real no sim (perda de potência em altitude). Efeito novo no `SimFailureActuator`. |
| `RECIP ENG FUEL AVAILABLE:n` | constante | Reg | **sim** | **Pane:** combustível indisponível = **parada por falta de combustível** (bomba, contaminação, gelo na linha) sem mexer nos tanques. |
| `RECIP ENG NUM CYLINDERS FAILED:n` | constante | Reg | — | Pane nativa de cilindro: detectar por borda (como `ENG FAILED`). |
| `RECIP ENG EMERGENCY BOOST ACTIVE/ELAPSED TIME:n` | constante | — | sim | Warbirds (WEP): tempo de boost → desgaste grave do motor. Só se a frota tiver. |
| `RECIP ENG ANTIDETONATION *`, `NITROUS *`, `SPRAYBAR *`, `SUPERCHARGER ACTIVE GEAR` | constante | — | — | Warbirds/corrida/agrícola; fora da frota atual (spraybar: **missão agrícola** do Cessna 188, futuro). |

### Turbina
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `TURB ENG N1 LOSS`, `TURB ENG FUEL EFFICIENCY LOSS`, `TURB ENG ITT COOLING EFFICIENCY LOSS`, `TURB ENG THRUST EFFICIENCY LOSS` (por motor) | constante | — | **sim** | **Decisão do usuário:** em 2026-11-15 ficou "o addon é a fonte da verdade e nada é gravado no sim". Como são graváveis, a saúde da seção quente/compressor poderia virar **degradação de desempenho real** (ITT mais alta, mais consumo, menos empuxo) — motor gasto que o piloto sente. Hoje só leitura/conferência. |
| `TURB ENG CORRECTED N1/N2/FF`, `TURB ENG COMMANDED N1`, `TURB ENG THROTTLE COMMANDED N1` | constante | — | sim (corrected) | Jatos: monitorar margem (N1 comandado × real) = sinal de motor degradado; HUD. |
| `TURB ENG HIGH/LOW IDLE` | constante | — | — | Contexto (marcha lenta alta/baixa). |
| `TURB ENG JET THRUST`, `TURB ENG PRIMARY/REVERSE NOZZLE PERCENT`, `TURB ENG AFTERBURNER *` | constante | Reg (reverse) | — | Empuxo e reverso; pós-combustor fora da frota. |
| `TURB ENG FUEL AVAILABLE`, `TURB ENG TANK SELECTOR/TANKS USED` | constante | Reg (available) | — | Já lido; seletor para procedimento. |

### Hélice
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `PROP AUTO FEATHER ARMED:n`, `PANEL AUTO FEATHER SWITCH:n`, `PROP FEATHER SWITCH:n`, `PROP FEATHERING INHIBIT:n` | constante | — | — | **Treino (bimotor):** auto-feather armado na decolagem; embandeiramento correto na falha de motor (`PROP FEATHERED`, já no registro) → nota da emergência. |
| `PROP SYNC ACTIVE`, `PROP SYNC DELTA LEVER` | constante | — | — | **Passageiros:** hélices fora de sincronia = ruído/vibração na cabine (bimotores); qualidade. |
| `PROP BETA:n`, `PROP BETA FORCED *`, `PROP LOCK:n` | constante | Reg (beta) | sim (forced) | Turbo-hélices: beta/reverso no solo; trava de hélice na partida (ATR). |
| `PROP MAX RPM PERCENT:n` | varia | — | sim | Redundante com `GENERAL ENG PCT MAX RPM`. |

## 3. Não usar
- `ENG TILT *` (tilt-rotor), `STRUCT ENGINE POSITION` (rejeitada; vetor), `ENG RPM ANIMATION PERCENT`, `ENG RPM SCALER`, `PROP ROTATION ANGLE`, `GENERAL ENG COMBUSTION SOUND PERCENT`, `TURB ENG ANIMATION RPM`, `PROPELLER ADVANCED SELECTION`, `OIL AMOUNT` (FS7, legado).
- Variantes `EX1` e duplicatas `ENG *` × `GENERAL ENG *` × `RECIP ENG *` da mesma grandeza: usar só a que já está no registro.
- **Graváveis de leitura de motor** (`GENERAL ENG OIL PRESSURE/TEMPERATURE`, `GENERAL ENG EXHAUST GAS TEMPERATURE`, `RECIP ENG CYLINDER HEAD TEMPERATURE`, `GENERAL ENG RPM`): escrever falsearia os instrumentos; só num modo de pane testado e com decisão explícita (ex.: manômetro de óleo coerente com o vazamento).

## 4. Próximos passos sugeridos
1. **Registro:** `ENG FUEL FLOW GPH`, `GENERAL ENG FUEL USED SINCE START`, `GENERAL ENG HOBBS ELAPSED TIME`, `GENERAL ENG THROTTLE MANAGED MODE`, `PROP AUTO FEATHER ARMED`, `PROP SYNC ACTIVE`, `SHUTOFF VALVE PULLED`.
2. **Qualidade/pilotagem:** nota de "gestão do motor" (mistura, potência de cruzeiro, cowl flaps, carb heat) por tempo; lógica pura com testes de replay.
3. **Economia:** Hobbs do sim como base de horas cobradas e combustível real consumido; FLX × TOGA nos jatos.
4. **Panes:** falha do turbo e falta de combustível por SimVar gravável; perda parcial de potência por limite do manete (testar no sim).
5. **Decisão do usuário:** gravar as perdas de eficiência da turbina a partir da saúde dos componentes (reverteria a decisão de 2026-11-15).
6. **No simulador (usuário):** bimotor (auto-feather, sync), turbo (falha do turbo), jato (detentes do manete, reverso), turbo-hélice (beta, trava de hélice).
