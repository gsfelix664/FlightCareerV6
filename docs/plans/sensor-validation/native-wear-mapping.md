# F1.4b — Tabelas do portão: wear and tear nativo → addon (APROVADA)

**Status:** proposta do revisor (Claude), 2026-11-14, com as decisões do usuário (plano › 0.1, opção B). **Aprovada pelo usuário em 2026-11-14** (limites dos eventos, Tabela 1, NO-GO, diferidas, teto e limiar provisório). Limiares e fatores definitivos saem da F1.4c. Alimenta F2.1, F2.2 e F4.2.
**Base:** `native-wear-C152.md` (único voo medido, em pista de terra e com abuso extremo), `DefectCatalog.cs`, `component_catalog.json`.
**SDK conferido:** página "Aircraft Misc. Variables" do SDK 2024 (`WEAR AND TEAR LEVEL:39` = `'PISTON_ENGINE'_n`; 1 = intacto, 0 = falhou; só leitura). A lista de IDs 0–49 é a do `sensor-inventory.md` › 12, aceita pelo MSFS no voo do C152. A unidade e a fórmula de `OVERSTRESS DAMAGE RATE` **não** estão documentadas: a conversão abaixo é hipótese a confirmar na F1.4c.

> **Revisão 2026-11-15 (plano › 0.1, versão final):** o addon é a fonte da verdade e o nativo é só sensor. A taxa nativa **não** vira % de desgaste (sem `nativeRateToLossPerHour` nem `maxNativeLossPerHour`), e `IS WEAK` **não** gera dano. Valem as regras abaixo; o resto da tabela (IDs, peças, códigos de pane, NO-GO) continua aprovado.

## Princípio (decisão do usuário)

- O addon calcula e grava a saúde de todas as peças, o mais perto do real, igual em qualquer avião (MSFS 2024 com ou sem wear and tear, terceiros, MSFS 2020). O **nível** nativo nunca entra no Hangar: o nativo do C152 cair para 80% numa decolagem com manete toda à frente não significa motor a 80% no Hangar.
- A taxa de overstress nativa é **sensor**: com limite próprio do addon para o componente naquele modelo, só os sensores do addon decidem e a taxa vai para o log como conferência; sem limite próprio, em componente **Ativo**, taxa acima do limiar = faixa vermelha da regra do addon (dano do addon, realista e não linear).
- Evento: só **quebrada** (`IS FAILED` 0 → 1) → pane (Tabela 2). **Fraca** (`IS WEAK`) só vai para o blackbox.
- Leitura numa definição de dados separada do `SimTelemetry`, em frequência baixa. Tudo vai para o log detalhado do voo.

## Tabela 1 — Overstress nativo (sensor)

Regras:
1. Componente **Ativo** sem limite próprio do addon para o modelo: `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` > **limiar** → faixa vermelha na regra do addon. Com limite próprio, a taxa só é registrada (conferência: nativo acusou e o addon não → limite faltando no catálogo).
2. Nunca soma com a faixa vermelha dos sensores do addon e nunca converte a taxa em %.
3. A taxa conta mesmo com o dano por estresse desativado no sim (nível travado em 1,00, F1.4b e F1.4c); taxa > 0 com nível em exatamente 1,00 = dano desativado (chave anti-trapaça, blackbox).
4. Unidade medida no C152 (só informativa): fração do nível por segundo (razão 0,997).
5. Limiar **0,0001** (C152: taxa exatamente 0 até 2.600 RPM; `calibration-C152.md`).
6. Cada trecho em overstress gera uma linha no blackbox: componente, fonte, pico, duração.

Status: **Ativo** = usa a taxa como reserva (regra 1); **Observar** = só log até a F1.4c; **Não usar**.

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
2. **Fraca:** só blackbox; não altera a saúde (revisão 2026-11-15). O `nativeWeakHealthCap` sai do plano.
3. **Quebrada:** pane com o código da tabela abaixo, origem `Native`, e a saúde da peça vai a **0%** (decisão do usuário, 2026-11-14, no lugar dos 15% aprovados antes): peça quebrada = 0%, só a troca devolve a 100%. Marca `IsBroken` quando existir peça (Fase 2); peça `IsBroken` não roda break check. Saúde 0% **não** é perda total (plano › F4.2b).
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

1. ~~Limites: fraca → no máximo 35%, quebrada → no máximo 15%?~~ **Aprovado pelo usuário em 2026-11-14**; depois alterado pelo usuário: **quebrada → 0%**.
2. ~~Comandos, combustível e bateria quebrados valem como NO-GO mesmo sem manter a pane no simulador depois do reinício?~~ **Aprovado.**
3. ~~Piloto automático e flaps quebrados como diferidos?~~ **Aprovado.**
4. ~~Tabela 1: motor (39, 40) e pneus (37, 48) Ativo, o resto em Observar até a F1.4c?~~ **Aprovado.**
5. ~~Teto inicial de 1.000 %/h e limiar provisório 0,0001?~~ **Aprovado** (valores definitivos saem da F1.4c).

## Ainda falta dado de voo (não bloqueia a aprovação)
- **Hoje o addon não lê `IS WEAK` / `IS FAILED` em voo** (só o `SensorRecorder` grava, com REC ligado). A implementação entra na F4; a confirmação, no voo longo de overstress da F1.4c (item 9).
- Em que nível `IS WEAK` / `IS FAILED` ligam: no C152 o mínimo foi 96,6% e nunca ligaram. Pode ser que só aconteçam em abuso longo; a regra funciona igual, só fica rara.
- Bimotor: índice do 2º motor (hoje só o motor 1 tem código e evento).
