# Variáveis de combustível do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) do SDK 2024 (72 SimVars: gerais, sistema de combustível novo `FUELSYSTEM *`, tanques legados `FUEL TANK *`).
Mesmo formato das outras análises. **C152** = `probe-runs/matriz-C152.csv`. Nenhuma obsoleta nesta página (`variaveis-obsoletas.md`); `FUEL SELECTED QUANTITY(_PERCENT)` só tem sentido com o sistema legado.
**Hoje o app** lê `FUEL TOTAL QUANTITY`, `FUEL TOTAL QUANTITY WEIGHT`, `FUEL TOTAL CAPACITY`, `FUEL WEIGHT PER GALLON`, `UNUSABLE FUEL TOTAL QUANTITY` (`SimTelemetry`) e calcula o combustível gasto como **inicial − final** (`FlightService`), e **não escreve** combustível no sim (a janela de abastecimento só cobra).

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | combustível **por tanque** (`FUEL LEFT/RIGHT QUANTITY`, `FUELSYSTEM TANK QUANTITY:n`), **desbalanceamento** esquerdo × direito, posição do seletor/válvulas (`FUELSYSTEM VALVE SWITCH/OPEN`), bombas, alimentação cruzada, autonomia com o fluxo (página de motor) |
| Qualidade do voo / pilotagem | **reserva no pouso** (VFR 30 min / IFR 45 min no fluxo de cruzeiro), tanque secando com o motor ligado (erro de gestão), desbalanceamento acima do limite, bomba auxiliar ligada na decolagem/pouso (asa baixa) |
| Missões e economia | **consumo real** sem depender de inicial − final (abastecer pelo menu no meio do voo distorce o cálculo); **despejo de combustível** (`FUEL DUMP ACTIVE`) cobrado; capacidade e combustível inutilizável para o planejamento da missão; **abastecimento do app gravado no sim** (tanques graváveis) |
| Integridade da carreira | **`UNLIMITED FUEL`** (combustível infinito do sim: voo sem custo), combustível **subindo** em voo (abastecimento pelo menu), tanques alterados no solo depois da cobrança |
| Desgaste e panes | **vazamento de combustível** (escrever a quantidade do tanque diminuindo, como o vazamento de óleo), bomba de combustível em pane (pressão na linha `FUELSYSTEM LINE FUEL PRESSURE`), válvula travada, contaminação (página de motor: `RECIP ENG FUEL AVAILABLE`) |

## 2. Tabela (o que interessa)

### Gerais
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `FUEL TOTAL QUANTITY` / `WEIGHT` | varia | Reg, Tel | — | Já usados. **Integridade:** subida em voo = abastecimento pelo menu. |
| `FUEL TOTAL QUANTITY EX1` / `WEIGHT EX1` | varia (+1,5 gal no C152) | — | — | Inclui o combustível inutilizável; usar a versão sem `EX1` para "utilizável". |
| **`UNLIMITED FUEL`** | constante (desligado) | — | — | **Integridade:** ligado = o sim não consome. Cobrar pelo fluxo integrado (página de motor) ou bloquear a missão; aviso no HUD. |
| `FUEL LEFT/RIGHT QUANTITY`, `FUEL LEFT/RIGHT CAPACITY` | varia / constante | — | — | **HUD/qualidade:** desbalanceamento (C152: diferença limite de alguns galões no POH); liga com `CG PERCENT LATERAL`. |
| `FUEL DUMP ACTIVE` / `FUEL DUMP SWITCH` | constante | — | — | **Economia:** combustível despejado pago; **missões:** pouso de emergência acima do MLW (jatos). |
| `FUEL CROSS FEED`, `FUEL TRANSFER PUMP ON`, `FUEL SELECTED TRANSFER MODE`, `FUEL PUMP` | constante | — | — | Bimotores/jatos: alimentação cruzada na falha de motor (procedimento) e transferência. |
| `NUM FUEL SELECTORS`, `FUEL TANK SELECTOR:n` | constante | — | — | Seletor (legado); posição "OFF" com motor ligado. No sistema novo usar as válvulas. |
| `NEW FUEL SYSTEM` | constante | Ctx | — | Decide entre `FUELSYSTEM *` (novo) e `FUEL TANK *` (legado). |

### Sistema de combustível novo (`FUELSYSTEM *`, índice = tanque/linha/bomba/válvula do avião)
| Variável | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|
| `FUELSYSTEM TANK QUANTITY:n` / `LEVEL:n` / `WEIGHT:n` | varia (`:1`, `:2`) | — | **sim** | **HUD:** por tanque. **Pane:** vazamento (diminuir a quantidade por tempo). **Economia:** gravar o abastecimento comprado no app. |
| `FUELSYSTEM TANK CAPACITY:n`, `TANK USABLE CAPACITY:n`, `TANK TOTAL QUANTITY:n` | constante / varia | — | — | Capacidade útil por tanque (planejamento, abastecimento). |
| `FUELSYSTEM LINE FUEL PRESSURE:n` / `ENGINE PRESSURE:n` | varia | Reg | — | Pressão de combustível; **pane de bomba** (queda de pressão); já no registro. |
| `FUELSYSTEM LINE FUEL FLOW:n` | varia | — | — | Fluxo por linha (vazamento: fluxo na linha maior que o consumo do motor). |
| `FUELSYSTEM PUMP ACTIVE:n` / `PUMP SWITCH:n` | varia / constante | Reg (active) | — | Bomba auxiliar ligada nas fases certas (procedimento); bomba em pane. |
| `FUELSYSTEM VALVE SWITCH:n` / `VALVE OPEN:n` | varia | — | — | Seletor/válvula; válvula fechada com motor ligado; **pane:** válvula travada (escrita? não gravável aqui — usar evento de válvula; testar). |
| `FUELSYSTEM JUNCTION SETTING:n`, `FUELSYSTEM TRIGGER STATUS:n` | constante | — | — | Específicos do avião; só se uma pane precisar. |
| `FUELSYSTEM LINE FUEL LEVEL:n` | varia (≈0) | — | — | Linha vazia = ar na linha (partida difícil); baixa prioridade. |

### Tanques legados (`FUEL TANK *`)
| Variável | C152 | Grav. | Uso proposto |
|---|---|---|---|
| `FUEL TANK LEFT/RIGHT MAIN QUANTITY/LEVEL` | varia | sim | Equivalente legado; usar quando `NEW FUEL SYSTEM` = 0. |
| `FUEL TANK CENTER(2/3) *`, `LEFT/RIGHT AUX *`, `LEFT/RIGHT TIP *`, `EXTERNAL1/2 *` | constante (C152 não tem) | sim | Aviões com mais tanques (legado). Mesmo uso: HUD, vazamento, abastecimento. |
| `* CAPACITY` | constante | — | Capacidade por tanque. |

## 3. Observações
- **Consumo na economia:** inicial − final falha se o piloto abastecer pelo menu do sim no meio do voo (ou ligar `UNLIMITED FUEL`). Proposta: somar `GENERAL ENG FUEL USED SINCE START` por motor (página de motor) ou integrar o fluxo por tempo simulado, e usar inicial − final só para conferência.
- **Abastecimento do app no sim:** a janela de abastecimento cobra mas não grava; com os tanques graváveis, o app pode **pôr no sim exatamente o que foi comprado** (e o piloto não abastece de graça pelo menu). Exige decisão do usuário (o app passaria a escrever combustível) e teste no MSFS por sistema (novo/legado).
- **Vazamento de combustível** como pane nova: o mesmo modelo do vazamento de óleo (`SimFailureActuator`), reduzindo `FUELSYSTEM TANK QUANTITY:n` por minuto; detectável pelo piloto pelo consumo anormal.

## 4. Não usar
- `FUEL SELECTED QUANTITY(_PERCENT)` (sistema legado, ambíguo no novo), `FUEL TOTAL QUANTITY EX1` para "utilizável", `FUELSYSTEM JUNCTION SETTING`/`TRIGGER STATUS` sem necessidade.
- Não escrever tanques fora de um fluxo decidido (abastecimento ou pane testada).

## 5. Próximos passos sugeridos
1. **Registro:** `UNLIMITED FUEL`, `FUEL LEFT/RIGHT QUANTITY`, `FUELSYSTEM TANK QUANTITY:1..N`, `FUELSYSTEM VALVE SWITCH:1..N`, `FUELSYSTEM LINE FUEL FLOW:1..N`, `FUEL DUMP ACTIVE`, `FUEL CROSS FEED`; contexto: `FUELSYSTEM TANK USABLE CAPACITY:1..N`, `NUM FUEL SELECTORS`.
2. **Economia/integridade:** consumo real por motor; detecção de `UNLIMITED FUEL` e de combustível subindo em voo (lógica pura com testes de replay).
3. **Pilotagem:** reserva no pouso, desbalanceamento e tanque secando (nota de gestão).
4. **Decisão do usuário:** o app gravar no sim o combustível comprado.
5. **Panes:** vazamento de combustível; bomba em pane pela pressão.
6. **No simulador (usuário):** abastecer pelo menu em voo (conferir a detecção), ligar `UNLIMITED FUEL`, bimotor com alimentação cruzada, jato com despejo, teste de escrita da quantidade do tanque (sistema novo e legado).
