# Variáveis de comandos de voo do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/) do SDK 2024 (96 SimVars: ailerons, profundor, leme, compensadores, flaps, spoilers, manche, fly-by-wire, asa dobrável, porta-aviões, Concorde).
Mesmo formato de `variaveis-diversos.md`. **C152** = `probe-runs/matriz-C152.csv`. O C152 não tem compensador de aileron/leme, spoiler, slat nem fly-by-wire: "constante" nesses grupos é esperado.

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | **`FLAPS CURRENT SPEED LIMITATION`** (Vfe do dente atual, ao vivo), posição de flap, compensador do profundor (ajuste para decolagem), `SPOILERS ARMED`, falhas de fly-by-wire |
| Qualidade do voo / passageiros | **suavidade dos comandos** (taxa de variação de `YOKE X/Y POSITION` e `RUDDER PEDAL POSITION`), voo fora de compensação (manche puxado/empurrado por muito tempo), flap mexido acima da Vfe, spoilers no pouso |
| Missões e treinamento | configuração de decolagem (flap e compensador no dente certo), voo manual × piloto automático (`YOKE * POSITION WITH AP`) para horas "à mão" no diário de bordo |
| Integridade da carreira | — (nada específico) |
| Desgaste e panes | deflexão × `DYNAMIC PRESSURE` (comando pleno acima da Va), **Vfe ao vivo** como camada de limite, **limites de G do sim** como camada de reserva, flap assimétrico, uso abusivo do leme em voo; **graváveis** como efeito de pane (compensador disparado, aileron travado); falhas de fly-by-wire como panes nativas |

## 2. Tabela (o que interessa)

### Superfícies e comandos
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `AILERON / ELEVATOR / RUDDER POSITION` | varia (±16K) | Reg | sim | Já no registro. |
| `AILERON LEFT/RIGHT DEFLECTION PCT`, `ELEVATOR DEFLECTION PCT`, `RUDDER DEFLECTION PCT` | varia (±1) | — | — | **Desgaste:** deflexão real da superfície × `DYNAMIC PRESSURE` → comando pleno acima da Va (plano › passo 4). Melhor que a posição do comando: já inclui limite de curso e piloto automático. |
| `AILERON LEFT/RIGHT DEFLECTION`, `ELEVATOR DEFLECTION`, `RUDDER DEFLECTION` (rad) | varia | — | sim | Idem em radianos. **Pane:** escrever deflexão = superfície travada (testar no sim). |
| `YOKE X/Y POSITION`, `RUDDER PEDAL POSITION` | varia | — | sim | **Qualidade:** taxa de variação (comando brusco) e reversões rápidas de leme → desconforto e, no leme, esforço na deriva. Entrada do piloto, sem o piloto automático. |
| `YOKE X/Y POSITION WITH AP` | varia | — | — | Comparar com `YOKE * POSITION`: mostra quando o piloto automático comanda → **horas de voo manual** no diário e na nota de treino. |
| `YOKE X/Y INDICATOR`, `RUDDER PEDAL INDICATOR` | varia | — | — | Redundantes (animação do cockpit). |

### Compensadores
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `ELEVATOR TRIM POSITION` / `ELEVATOR TRIM PCT` | varia | Reg (position) | sim (position) | **HUD/treino:** compensador fora da faixa de decolagem na corrida → aviso e nota. **Qualidade:** voo fora de compensação (manche longe do centro por minutos em voo nivelado). **Pane:** escrever = **compensador disparado** (trim runaway), pane clássica que o SDK não tem como evento. |
| `ELEVATOR TRIM INDICATOR`, `... PCT EX1/EX2` | varia | — | EX2 | Variantes; usar só uma (`PCT`). |
| `ELEVATOR TRIM UP/DOWN LIMIT`, `MIN/MAX/NEUTRAL` | constante | — | — | **Contexto:** normalizar o compensador e definir a faixa de decolagem por aeronave. |
| `AILERON TRIM PCT`, `RUDDER TRIM PCT` | constante (C152 não tem) | Reg | sim | Já em regra; provar num avião que tenha. Graváveis: compensador lateral/direcional disparado. |
| `* TRIM DISABLED` | constante | — | — | Compensador inoperante (pane nativa ou de aeronave) → detectar. |

### Flaps e spoilers
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| **`FLAPS CURRENT SPEED LIMITATION`** | **varia (−1 a 110 kt)** | Reg | — | **Vfe do dente atual informada pelo sim** (−1 = sem limite, flap recolhido). Camada "SimVar" do `vfe:current` para as **22 de 46 aeronaves sem `vfe` no catálogo** (achado da revisão REGRAS-C152). **HUD:** "Vfe 110 kt" e margem. |
| `FLAPS HANDLE INDEX`, `FLAPS HANDLE PERCENT`, `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT` | varia | Reg, Tel | sim | Já usados (pane de flap no `SimFailureActuator`). |
| `FLAPS EFFECTIVE HANDLE INDEX` | varia | — | — | Dente **efetivo** (≠ comando quando o flap não acompanha): detecta flap travado/pane nativa. |
| `TRAILING EDGE FLAPS LEFT/RIGHT ANGLE` / `INDEX` | varia | — | — | **Flap assimétrico:** esquerdo ≠ direito → pane grave (detecção e efeito). |
| `FLAP SPEED EXCEEDED`, `FLAP DAMAGE BY SPEED` | constante | Reg, Fail | — | Já usados. |
| `LEADING EDGE FLAPS LEFT/RIGHT *` | constante | — | sim (percent) | Jatos: slats; Vfe dos slats e assimetria. Provar num jato. |
| `SPOILERS ARMED`, `SPOILERS HANDLE POSITION`, `SPOILERS LEFT/RIGHT POSITION`, `SPOILER AVAILABLE` | constante | Reg (handle) | sim (handle) | **HUD:** spoilers armados para pouso; **qualidade/treino:** pouso de jato sem armar; **desgaste:** spoiler aberto em alta velocidade (já há regra), assimetria. |

### Limites de G do simulador
| Variável | C152 | Uso proposto |
|---|---|---|
| `POSITIVE/NEGATIVE G LIMIT FLAPS UP/DOWN` | constante (+6/−4,8 e +4,2/−3,6) | Camada **SimVar** de `gLimitPos/Neg` (abaixo do POH, acima do padrão da classe), com o limite trocando com o flap. No C152 os valores **não** são os do POH (+4,4/−1,76): usar só quando não houver POH. |

### Fly-by-wire
| Variável | C152 | Uso proposto |
|---|---|---|
| `FLY BY WIRE ELAC/FAC/SEC FAILED` | constante | **Panes nativas** (Airbus): detectar por borda como as outras em `SimFailureState`; **HUD:** alerta. |
| `FLY BY WIRE ELAC/FAC/SEC SWITCH`, `FLY BY WIRE ALPHA PROTECTION` | constante | Treino: computador desligado em voo; proteção de alfa ativa = aproximação do estol (qualidade). |

## 3. Não usar
- Porta-aviões (`CATAPULT *`, `CABLE CAUGHT BY TAILHOOK`, `LAUNCHBAR *`, `HOLDBACK BAR`, `BLAST SHIELD`), `CONCORDE *`, `FOLDING WING *`, `ELEVON DEFLECTION`: fora do perfil das aeronaves da carreira.
- `AILERON AVERAGE DEFLECTION`, variantes `EX1/EX2` e `INDICATOR` (duplicam outras).
- Não escrever superfícies, compensadores ou flaps fora de um modo de pane testado (`validacao-simvars.md` › 1b).

## 4. Próximos passos sugeridos
1. **Limites:** camada SimVar para `vfe:current` (`FLAPS CURRENT SPEED LIMITATION`) e para `gLimitPos/Neg` (limites de G por flap), com prioridade abaixo do POH. Resolve parte das lacunas de Vfe do catálogo.
2. **Qualidade do voo:** suavidade dos comandos e voo fora de compensação (lógica pura, por tempo, com testes de replay).
3. **Treino/diário:** horas manuais × piloto automático; configuração de decolagem (flap e compensador).
4. **Panes:** compensador disparado e flap assimétrico como efeitos novos (testar escrita no sim); falhas de fly-by-wire como panes nativas.
5. **No simulador (usuário):** C172/retrátil com compensador de leme, jato com spoilers/slats/autobrake, Airbus (fly-by-wire), e teste de escrita do compensador do profundor.
