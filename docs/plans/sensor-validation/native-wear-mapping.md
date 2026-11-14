# F1.4b — Tabelas do portão: wear and tear nativo → addon (PROPOSTA)

**Status:** proposta do revisor (Claude), 2026-11-14, com as decisões do usuário (plano › 0.1, opção B). Limites dos eventos aprovados (2026-11-14); Tabela 1, NO-GO e diferidas aguardando aprovação. Limiares e fatores definitivos saem da F1.4c. Alimenta F2.1, F2.2 e F4.2.
**Base:** `native-wear-C152.md` (único voo medido, em pista de terra e com abuso extremo), `DefectCatalog.cs`, `component_catalog.json`.
**SDK conferido:** página "Aircraft Misc. Variables" do SDK 2024 (`WEAR AND TEAR LEVEL:39` = `'PISTON_ENGINE'_n`; 1 = intacto, 0 = falhou; só leitura). A lista de IDs 0–49 é a do `sensor-inventory.md` › 12, aceita pelo MSFS no voo do C152. A unidade e a fórmula de `OVERSTRESS DAMAGE RATE` **não** estão documentadas: a conversão abaixo é hipótese a confirmar na F1.4c.

## Princípio (decisão do usuário)

- O addon calcula e grava a saúde de todas as peças (desgaste normal por hora/ciclo + estresse), igual em todas as aeronaves. O **nível** nativo nunca entra no Hangar.
- O addon define a taxa normal (vida útil de carreira) e a de estresse para qualquer avião. Quando o nativo acusa overstress (taxa acima do limiar), a **taxa do nativo substitui** a de estresse do addon naquele componente (Tabela 1). Uma fonte por vez, nunca a soma. A taxa é usada sempre que for reportada, com a opção de wear and tear ligada ou não.
- As taxas de estresse do addon são **calibradas para o nativo** (F1.4c), para o mesmo abuso custar parecido em aviões com e sem wear and tear.
- Por cima disso, reage a **eventos** do nativo (Tabela 2): **fraca** (`IS WEAK` 0 → 1) → dano de desgaste; **quebrada** (`IS FAILED` 0 → 1) → pane.
- Leitura numa definição de dados separada do `SimTelemetry`, em frequência baixa. Tudo vai para o log detalhado do voo.

## Tabela 1 — Overstress nativo → estresse da peça

Regras:
1. Enquanto `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` > **limiar**: `estresse (%/h) = min(taxa × nativeRateToLossPerHour, maxNativeLossPerHour)`.
2. Com o nativo acima do limiar, ele **substitui** o estresse dos sensores do addon (mesmo que seja menor). Abaixo do limiar, vale o estresse do addon (abuso que o nativo não modela continua sendo cobrado). O desgaste normal do addon continua sempre.
2a. A taxa conta mesmo com a opção de wear and tear desligada (no voo F1.4b ela continuou reportando; só o nível travou em 1,00).
3. Fator inicial = **conversão pura de unidade**, sem escala (decisão do usuário): hipótese "fração do nível por segundo" → `nativeRateToLossPerHour = 100 × 3600 = 360.000`. Ex.: taxa 0,0015 → 540 %/h → 40 s = −6%. No C152 o nível caiu 3,3% nesses 40 s, então a hipótese ainda precisa da F1.4c.
4. Teto `maxNativeLossPerHour` por componente, proposta inicial **1.000 %/h** (acima do maior valor visto no C152: 0,0020 → 720 %/h, para não cortar o nativo): protege contra valor absurdo ou mudança da fórmula num update do MSFS.
5. Limiar provisório **0,0001** em todos os componentes Ativo até a F1.4c medir o ruído em operação normal (`limiar = max(ruído normal × 3, 0,0001)`).
6. Cada trecho em overstress gera uma linha no blackbox: componente, início, fim, duração, perda aplicada ("overstress detectado pelo simulador: motor, 42 s, −3,1%").

Status: **Ativo** = usa a taxa; **Observar** = só log até a F1.4c; **Não usar**.

| ID | Nativo | Componente | Peça F2.1 (provisória) | Status | Observação |
|---|---|---|---|---|---|
| 39 | PISTON_ENGINE | Engine | Motor › cilindros, válvulas | **Ativo** | C152: 0,0013–0,0020 só acima de ~2.600 RPM; ~0 em operação normal. |
| 40 | TURBINE_ENGINE | Engine + HotSection | Seção quente › turbina | **Ativo** | Mesma lógica do 39; calibrar no primeiro turboélice. |
| 37, 48 | TIRE, TAILWHEEL TIRE | Tires | Pneus | **Ativo** | Decisão do usuário: a taxa de 0,0093 na decolagem foi em pista de terra, com velocidade alta e abuso. A F1.4c separa asfalto × terra e normal × abuso. |
| 35, 47 | LANDING_GEAR, TAILWHEEL GEAR | LandingGear | Trem › amortecedores / bequilha | Observar | Caiu 1,8% na mesma decolagem; ativar se a F1.4c mostrar taxa ~0 em pouso e corrida normais. |
| 36 | BRAKE | Brakes | Freios | Observar | Parado no C152 (freios não simulados); repetir num avião com freios simulados. |
| 41 | OIL_TANK_INTEGRITY | Oil | Óleo › cárter/vedação | Observar | Sem overstress no voo; testar com temperatura do óleo no limite. |
| 28–34 | ALTERNATOR, BATTERY, IDG, RAT, BUS, CIRCUIT, RECTIFIERS | Electrical | Elétrico | Observar | Só envelhecimento por tempo no C152. |
| 5–8 | FLAPS + cabos | Airframe | Célula › flaps | Observar | Testar flap acima da Vfe. |
| 9–14, 19, 20 | AILERONS, ELEVATOR, RUDDER (+ cabos) | Airframe | Célula › comandos | Observar | Testar excesso de G. |
| 21–27 | HYDRAULIC * | LandingGear (hidráulico) | Hidráulico | Observar | Parado no C152; repetir num avião hidráulico. |
| 0–4 | FUEL * | — | Combustível | Observar | Sem barra hoje: entra na F2.1. |
| 42, 38, 49 | OIL AMOUNT, TIRE PRESSURE | — | — | Não usar | Quantidade/pressão, não estresse. |
| 43–46 | AUTOPILOT *, GENERAL | — | — | Não usar | Envelhecimento por tempo / agregado. |

## Tabela 2 — Eventos fraca / quebrada

### Regras do evento

1. Gatilho por **borda de subida** (0 → 1), lido junto com as panes nativas que o `PersistentFailureService` já detecta. Um evento de cada tipo por componente por voo.
2. **Fraca:** a saúde da peça fica no máximo em `nativeWeakHealthCap` = **35%** (proposta). Se já estiver abaixo, nada muda. 35% fica abaixo do limite do walkaround (40%), então a peça visível passa a ganhar a exclamação amarela no próximo walkaround, e acima do início dos break checks (30%). **Não** cria discrepância nem aviso na hora (regra 0.11: amarelo só quando o piloto descobre). Vai para o blackbox.
3. **Quebrada:** pane com o código da tabela abaixo, origem `Native`, e a saúde da peça fica no máximo em `nativeFailedHealthCap` = **15%** (proposta, abaixo do limiar de pane de 20%), para que reparar a pane sem trocar a peça não deixe uma peça "quebrada a 80%". Marca `IsBroken` quando existir peça (Fase 2).
4. **Sem duplicar:** se já há discrepância aberta com o mesmo código (do addon ou de outra leitura nativa, ex.: `ENG FAILED`), o evento vai só para o blackbox.
5. Pane sem forma de ser mantida no simulador (enforcement `None`): depois que o voo é reiniciado o MSFS esquece a pane, mas ela continua valendo como **NO-GO no Hangar** (o `StartMonitoring` bloqueia).
6. Os dois limites ficam no JSON (catálogo), não no código.

### Componente nativo → peça do addon → pane

**Componente** = barra atual do Hangar (`ComponentKind`). **Peça F2.1** = grupo/peça sugerido para o catálogo novo (nomes provisórios). **Código** = pane quando quebra (`DefectCatalog`).

| ID | Nativo | Componente | Peça F2.1 (provisória) | Código ao quebrar | Novo? | NO-GO | Enforcement |
|---|---|---|---|---|---|---|---|
| 39, 40 | PISTON / TURBINE ENGINE | Engine (+ HotSection no 40) | Motor › cilindros / turbina | `ENGINE_FAILURE` | não | sim | NativeToggle (`TOGGLE_ENGINE1_FAILURE`) |
| 41 | OIL_TANK_INTEGRITY | Oil | Óleo › cárter/vedação | `OIL_LEAK` | não | sim | None (o vazamento do addon já existe como efeito) |
| 42 | OIL_TANK_AMOUNT | Oil | Óleo › nível | — (só blackbox) | — | — | — |
| 28 | ELECTRICAL_ALTERNATOR | Electrical | Elétrico › alternador | `ALTERNATOR` | não | sim | Watchdog |
| 29 | ELECTRICAL_BATTERY | Electrical | Elétrico › bateria | `BATTERY` | **sim** | sim | None |
| 30–34 | IDG, RAT, BUS, CIRCUIT, RECTIFIERS | Electrical | Elétrico › barra/chicote | `ELECTRICAL` | não | sim | NativeToggle (`TOGGLE_ELECTRICAL_FAILURE`) |
| 35, 47 | LANDING_GEAR, TAILWHEEL GEAR | LandingGear | Trem › amortecedores / bequilha | `GEAR_DAMAGE` | não | sim | None |
| 36 | BRAKE | Brakes | Freios › pastilhas/discos | `BRAKE_FAILURE` | não | sim | None |
| 37, 48 | TIRE, TAILWHEEL TIRE | Tires | Pneus | `TIRE_BLOWOUT` | não | sim | None |
| 38, 49 | TIRE PRESSURE | Tires | Pneus › pressão | `TIRE_FLAT` | **sim** | sim | None |
| 5, 6 | FLAPS LEFT/RIGHT | Airframe | Célula › flaps | `FLAP_DAMAGE` | não | não (diferida) | Watchdog |
| 7, 8 | FLAPS CABLES | Airframe | Célula › cabos de flap | `FLAP_LOCK` | não | não (diferida) | Watchdog |
| 9–14, 19, 20 | AILERONS, ELEVATOR, RUDDER (+ cabos) | Airframe | Célula › comandos | `FLIGHT_CONTROL` | **sim** | sim | None (não há evento do SDK) |
| 21–27 | HYDRAULIC * | LandingGear (hidráulico) | Hidráulico | `HYDRAULIC` | não | sim | None (avaliar `TOGGLE_HYDRAULIC_FAILURE`) |
| 0–4 | FUEL * | — (sem barra hoje) | Combustível | `FUEL_SYSTEM` | **sim** | sim | None |
| 43–45 | AUTOPILOT * | — (sem barra hoje) | Aviônicos › piloto automático | `AUTOPILOT` | **sim** | não (diferida) | None |
| 46 | GENERAL | — | — | — (só blackbox) | — | — | — |

Componente sem barra hoje (combustível, piloto automático): até a Fase 2, o evento **fraca** só vai para o blackbox; o **quebrada** já gera a pane.

**Códigos novos para o `DefectCatalog`:** `BATTERY`, `TIRE_FLAT`, `FLIGHT_CONTROL`, `FUEL_SYSTEM`, `AUTOPILOT` (custos base na mesma escala dos atuais, definidos na implementação).

## Decisões para o usuário

1. ~~Limites: fraca → no máximo 35%, quebrada → no máximo 15%?~~ **Aprovado pelo usuário em 2026-11-14.**
2. Comandos, combustível e bateria quebrados valem como **NO-GO** mesmo sem conseguirmos manter a pane no simulador depois do reinício?
3. Piloto automático e flaps quebrados como **diferidos** (permite voar)?
4. Tabela 1: motor (39, 40) e pneus (37, 48) **Ativo**, o resto em **Observar** até a F1.4c?
5. Teto inicial de **1.000 %/h** e limiar provisório **0,0001**?

## Ainda falta dado de voo (não bloqueia a aprovação)
- **Hoje o addon não lê `IS WEAK` / `IS FAILED` em voo** (só o `SensorRecorder` grava, com REC ligado). A implementação entra na F4; a confirmação, no voo longo de overstress da F1.4c (item 9).
- Em que nível `IS WEAK` / `IS FAILED` ligam: no C152 o mínimo foi 96,6% e nunca ligaram. Pode ser que só aconteçam em abuso longo; a regra funciona igual, só fica rara.
- Bimotor: índice do 2º motor (hoje só o motor 1 tem código e evento).
