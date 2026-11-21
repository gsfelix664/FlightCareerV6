# Key Events do sistema de combustível do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Fuel System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-fuel-system-events/) do SDK 2024 (108 linhas: alimentação cruzada, seletores, sistema de combustível novo, transferência, diversos). Nenhum obsoleto; `ENGINE_FUELFLOW_BUG_POSITION*` "não usado". Regras de `eventos-eletricos.md` § 1 e princípio F4.2e (falhar o sistema sem mexer no comando do piloto). Liga com a COMBUSTIVEL-REAL (já na base) e `variaveis-combustivel.md`.

## 1. Destaques

| Área | Eventos |
|---|---|
| **Integridade (receber)** | **`REPAIR_AND_REFUEL`** (repara **e** reabastece o avião inteiro — desfaz as panes do addon e enche os tanques: invalidar o voo e reaplicar as panes), **`ADD_FUEL_QUANTITY`** (abastecimento pelo menu: detecção imediata, complementa a vigilância da quantidade), **`REQUEST_FUEL_KEY`** (caminhão de combustível do próprio sim na vaga) |
| **Panes no sistema (enviar)** | **`FUELSYSTEM_VALVE_SET [válvula, 0]`** (válvula/linha travada fechada: tanque ou motor sem alimentação), **`FUELSYSTEM_PUMP_SET [bomba, 0]`** (bomba de combustível em pane), `FUELSYSTEM_JUNCTION_SET` (linha obstruída num ponto do sistema), `FUELSYSTEM_TRIGGER_SET` |
| **Procedimento (receber)** | seletor de tanque (`FUEL_SELECTOR_*`, `FUEL_SELECTOR_SET`), alimentação cruzada (`CROSS_FEED_*`), bomba elétrica (`ELECT_FUEL_PUMP*_SET`: 0 desl., 1 lig., 2 auto), bomba manual (`MANUAL_FUEL_PRESSURE_PUMP*`), despejo (`FUEL_DUMP_SWITCH_SET`), transferência (`SET_FUEL_TRANSFER_*`) |

## 2. Tabela

### Integridade e abastecimento
| Evento | Uso proposto |
|---|---|
| **`REPAIR_AND_REFUEL`** | Receber em voo monitorado → **voo invalidado** (motivo "reparo e abastecimento pelo menu") e o `PersistentFailureService` **reaplica** as panes abertas (o evento zera as falhas do sim). No solo fora do voo: o modo vigia já reaplica; o combustível é sincronizado no próximo `StartMonitoring`. A documentação diz que é ignorado com realismo forçado. |
| **`ADD_FUEL_QUANTITY`** | Receber em voo → mesmo caminho da COMBUSTIVEL-REAL (abastecimento em voo = invalidado), mas imediato. No solo antes do voo: a sincronização automática devolve o valor do app. |
| **`REQUEST_FUEL_KEY`** | Caminhão de combustível do MSFS 2024 na vaga. **Decisão do usuário:** (a) deixar a sincronização devolver o valor do app (o abastecimento do sim "não vale"; avisar no HUD para usar o abastecimento do app) ou (b) cobrar o que o caminhão pôs ao preço do aeroporto e aceitar o novo valor. |
| `RELEASE_DROP_TANK_*`, `ANTIDETONATION_TANK_VALVE_TOGGLE`, `NITROUS_TANK_VALVE_TOGGLE` | Não usar (militares/warbirds). |

### Sistema de combustível novo (`FUELSYSTEM_*`, índices do avião)
| Evento | Parâmetros | Uso proposto |
|---|---|---|
| **`FUELSYSTEM_VALVE_SET`** | `[0]` válvula, `[1]` 1 aberta / 0 fechada | **Pane: válvula travada fechada** — motor ou tanque sem alimentação sem mexer no seletor do piloto (quando a válvula for interna, não o seletor). Mapear no `electricalMap`/"mapa de combustível" por aeronave quais válvulas são internas. |
| **`FUELSYSTEM_PUMP_SET`** | `[0]` bomba, `[1]` 0/1/2 (auto) | **Pane de bomba de combustível** (mecânica ou elétrica) — perda de pressão; o piloto liga a bomba auxiliar/manual (procedimento). |
| `FUELSYSTEM_JUNCTION_SET` | `[0]` junção, `[1]` opção | **Linha obstruída** num ponto do sistema (contaminação, gelo na linha). |
| `FUELSYSTEM_TRIGGER_SET/ON/OFF` | `[0]` gatilho | Específico do avião; só com teste. |
| `*_OPEN/_CLOSE/_ON/_OFF/_TOGGLE` | índice | Receber. |

### Seletores e alimentação cruzada
| Evento | Uso proposto |
|---|---|
| `FUEL_SELECTOR_SET`, `FUEL_SELECTOR(_2.._4)_*` (ALL/LEFT/RIGHT/CENTER/AUX/MAIN/OFF/ISOLATE/CROSSFEED) | Receber: **gestão de combustível** (trocar de tanque para equilibrar; seletor em OFF com motor ligado = erro). No sistema legado é o próprio seletor; não forçar (princípio F4.2e). |
| `CROSS_FEED_OPEN/OFF/TOGGLE/LEFT_TO_RIGHT/RIGHT_TO_LEFT` | Receber: alimentação cruzada na falha de motor e no desbalanceamento (bimotor; nota da emergência). |
| `FUEL_TANK_SELECTOR_*` | Códigos do `FUEL_SELECTOR_SET` (tabela de referência, não eventos). |

### Bombas, despejo e transferência
| Evento | Uso proposto |
|---|---|
| `ELECT_FUEL_PUMP_SET`, `ELECT_FUEL_PUMP1..4_SET` | `[0]` 0/1/2 — receber: bomba auxiliar ligada na decolagem/pouso (procedimento). |
| `MANUAL_FUEL_PRESSURE_PUMP`, `MANUAL_FUEL_PRESSURE_PUMP_SET` | Receber: bomba manual depois de pane de bomba (procedimento correto). |
| `FUEL_DUMP_SWITCH_SET`, `FUEL_DUMP_TOGGLE` | Receber: despejo (cobrar o combustível despejado; pouso acima do MLW evitado). |
| `SET_FUEL_TRANSFER_*`, `FUEL_TRANSFER_CUSTOM_INDEX_TOGGLE`, `MANUAL_FUEL_TRANSFER` | Receber (jatos: transferência entre tanques); pane de transferência (futuro). |

## 3. Decisões para o usuário
1. `REPAIR_AND_REFUEL` em voo invalida o voo e as panes são reaplicadas? (recomendado)
2. **Decidido (2026-11-16): cobra e aceita.** Caminhão de combustível do sim (`REQUEST_FUEL_KEY`): no solo antes do voo, o app detecta o evento e a subida da quantidade, **cobra a diferença ao preço do aeroporto** (`FuelPriceService`, transação "Combustível (caminhão do sim)"), atualiza `PilotAircraft.FuelAmount` e só então a sincronização da COMBUSTIVEL-REAL roda (sem devolver o valor antigo). Subida sem o evento continua sendo sincronizada de volta; em voo continua invalidando.
3. Panes de válvula/bomba/linha pelos eventos `FUELSYSTEM_*` (efeito no sistema, sem mexer no seletor)?

**Decididas pelo usuário (2026-11-19, `docs/tarefas_sem_IA.md`):**
1. **`REPAIR_AND_REFUEL` em voo ou após o off-block:** invalida o voo e reinjeta o combustível do banco e as panes. No solo antes do voo: não invalida, mas a COMBUSTIVEL-REAL devolve o combustível do banco e reaplica as panes.
2. (complemento) Caminhão: janela autorizada aberta no `REQUEST_FUEL_KEY` (solo, motores cortados), rollback suspenso enquanto sobe, cobrança quando estabiliza ~3 s.
3. **Panes `FUELSYSTEM_*`:** sim — bomba (`FUELSYSTEM_PUMP_OFF/SET`) e válvula/linha (`FUELSYSTEM_VALVE_CLOSE/SET`) atuam na camada interna, sem mexer no seletor da cabine.

## 4. Próximos passos
1. Teste no MSFS: `FUELSYSTEM_VALVE_SET`/`PUMP_SET` no C172 (índices pelo Fuel System Debug), receber `REPAIR_AND_REFUEL` e `ADD_FUEL_QUANTITY`, caminhão de combustível numa vaga.
2. Mapa de combustível por aeronave (válvulas internas × seletor, bombas), junto com o mapa elétrico.
