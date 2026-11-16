# Plano — Iluminação no Hangar e panes por disjuntor/circuito

Criado em 2026-11-16 (Claude). Status: **proposta**, aguardando decisões do usuário (§ 7) e testes no MSFS.
Origem: `docs/plans/sensor-research/variaveis-eletrica.md` (disjuntores, circuitos, linhas e relés graváveis no sistema elétrico v2) e a próxima fase do `AGENTS.md` › 7 ("Avaliar aviônicos: disjuntores por aeronave no MSFS 2024").

## 1. Objetivos
1. **Panes por sistema** usando o sistema elétrico do sim (disjuntor, circuito, linha), em vez de mascarar comandos do piloto: GPS, piloto automático, transponder, rádio, flap elétrico, aquecimento do pitot, barramento de aviônicos, alternador sem chegar ao barramento.
2. **Iluminação** como grupo de peças no Hangar (lâmpadas com vida útil), com pane de **lâmpada queimada** aplicada no sim e ligação com a MEL (NO-GO noturno).

## 2. O que dá e o que não dá
| Pane | Pelo sistema elétrico? | Observação |
|---|---|---|
| Magneto | **não** | O magneto não depende do sistema elétrico (nem no avião real). Continua por `RECIP ENG LEFT/RIGHT MAGNETO` (watchdog atual). |
| Flap elétrico | **sim** | Puxar o disjuntor/circuito do motor do flap: o flap para onde está e o piloto vê o disjuntor saltado. Substitui o mascaramento de `FLAPS HANDLE INDEX` **nos aviões com flap elétrico**. Flap hidráulico (jatos) continua como hoje. |
| Alternador | sim (alternativa) | `ALTERNATOR CONNECTION ON`/`ALTERNATOR BREAKER PULLED` ou a linha alternador→barramento; hoje é `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`. Usar ao menos como **confirmação**. |
| GPS, piloto automático, transponder, rádio, aviônicos | sim | `BREAKER GPS/AUTOPILOT/XPNDR/NAVCOM1..4/AVNBUS1..2` ou `CIRCUIT BREAKER PULLED:n`. Hoje só existe `TOGGLE_ELECTRICAL_FAILURE` (total). |
| Barramento parcial | sim | `BUS BREAKER PULLED:n` / `LINE BREAKER PULLED:<linha>`. |
| Luzes (pouso, táxi, navegação, strobe, beacon, painel) | sim | Disjuntor ou `CIRCUIT SWITCH ON:n` do circuito da luz, com watchdog. |
| Trem elétrico | talvez | `CIRCUIT GEAR MOTOR ON` / disjuntor do motor do trem → extensão de emergência. Provar num retrátil elétrico. |

## 3. Mapa elétrico por aeronave (pré-requisito)
- Índices e nomes de circuitos, linhas e relés **mudam por avião**. Novo bloco no perfil `Data/aircraft/<modelo>.json`, no padrão do `elecAvionicsCircuitIndex` já existente:
  ```json
  "electricalMap": {
    "circuits": { "flapMotor": 7, "pitotHeat": 9, "gps": 12, "autopilot": 13, "transponder": 14,
                  "landingLight": 20, "taxiLight": 21, "navLight": 22, "strobe": 23, "beacon": 24 },
    "lines":    { "alternatorToMainBus": "line_alt_main" },
    "source":   "Electrical System Debug (Developer Mode) — C152 2026-11-xx"
  }
  ```
- Levantamento: Developer Mode › Electrical System Debug + `ELECTRICAL CIRCUIT EXISTS:n` (o SimVarProbe pode listar os circuitos existentes). Começar pelo C152, C172 G1000, Baron, TBM.
- Aeronave sem o mapa: a pane cai no efeito atual (ou só no app, `effect: None`). Nunca chutar índice.

## 4. Atuação no sim (UI/Services)
- Novos valores em `FailureEffect` (ex.: `CircuitOff`, `BreakerPulled`, `LineOpen`) com o alvo (chave do `electricalMap`). Tratamento no `SimFailureActuator`; persistência pelo `PersistentFailureService` (discrepância entre voos, modo vigia).
- **Watchdog:** se o piloto rearmar o disjuntor (`CIRCUIT BREAKER PULLED:n` volta a 0) ou religar a chave, reaplicar no tick seguinte, como no magneto. Registrar no blackbox ("disjuntor rearmado pelo piloto; pane persiste").
- **Reparo:** reparar a discrepância no Hangar libera o circuito (mesmo fluxo de hoje).
- Arquivos quentes (`SimFailureActuator.cs`, `SimConnectService.cs`): spec passo a passo e revisão linha a linha (`CLAUDE.md`).

## 5. Iluminação no Hangar (Domain + catálogos)
- Novo grupo `PartGroup.Iluminacao = "Iluminação"` e peças em `parts_catalog.json`:

| Peça | Vida (h acesas) | Ciclos | Ao quebrar | MEL (proposta) |
|---|---|---|---|---|
| `lights.landing` farol de pouso | halógena ~300 h / LED ~5.000 h | sim | farol apagado | diferível; voo só diurno |
| `lights.taxi` farol de táxi | idem | sim | apagado | diferível |
| `lights.nav` navegação | ~1.000 h / LED longa | não | apagada | NO-GO noturno |
| `lights.strobe` anticolisão | ~1.000 h | sim (flash) | apagada | NO-GO noturno (anticolisão) |
| `lights.beacon` beacon | ~1.000 h | sim | apagado | NO-GO noturno se for a única anticolisão |
| `lights.panel` painel/instrumentos | longa | não | painel escuro | NO-GO noturno |

- **Desgaste:** horas com a luz acesa (`LIGHT LANDING`, `LIGHT TAXI`… — página Aircraft Lights Variables, a analisar) e ciclos de liga/desliga; LED ou halógena por aeronave (campo no perfil). Sem estresse térmico na fase 1.
- **Falha:** pela taxa por hora do `FailureEngine` (`p = 1 − e^(−λ·dt)`), não por pacote.
- **Custo:** peça barata, mão de obra curta, sem downtime longo: a pane "barata" que dá realismo sem parar a carreira.
- **Walkaround:** lâmpada queimada é visível no walkaround (`visibleOnWalkaround: true`).

## 6. Fases
1. **F1 — Levantamento (sem código):** analisar a página Aircraft Lights Variables; mapear circuitos/linhas do C152, C172, Baron e TBM no Developer Mode; teste de escrita no MSFS (puxar e rearmar disjuntor, desligar circuito, abrir linha) com o modo de panes do SimVarProbe (`validacao-simvars.md` › 1b). **Usuário no simulador.**
2. **F2 — Domínio:** grupo e peças de iluminação, `electricalMap` no perfil, novos `FailureEffect`, códigos no `DefectCatalog`; testes de lógica pura.
3. **F3 — Atuação:** `SimFailureActuator` + watchdog + `PersistentFailureService`; replay com fixtures; teste no MSFS.
4. **F4 — Flap elétrico por disjuntor:** trocar o mascaramento nos aviões mapeados; manter o atual nos demais.
5. **F5 — MEL:** NO-GO noturno de luzes e itens diferíveis (junto com a fase MEL do `AGENTS.md`).

## 7. Decisões do usuário
1. Iluminação como **grupo de peças** (dentro do componente Elétrico na barra antiga) ou componente próprio no Hangar?
2. Vida útil e preço das lâmpadas (halógena × LED) e se o perfil da aeronave diz qual ela usa.
3. Regras de MEL: quais luzes são NO-GO à noite e se o app bloqueia o voo noturno ou só avisa.
4. Flap elétrico: trocar o mascaramento pelo disjuntor assim que provado, ou manter os dois?
5. O piloto pode "rearmar" um disjuntor em pane uma vez (como na vida real, com risco) ou o watchdog sempre puxa de novo?

## 8. Testes previstos
- `ElectricalMap_SemMapa_UsaEfeitoAtual`.
- `Watchdog_DisjuntorRearmado_Reaplica` (lógica pura do estado desejado × lido).
- `Lampada_HorasAcesas_DesgastaPorTempoSimulado`.
- `Lampada_Falha_PorTaxaPorHora` (`IRandomSource` determinístico).
- `Mel_NavQueimada_NoGoNoturno`.
