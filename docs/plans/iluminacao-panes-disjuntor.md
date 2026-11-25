# Plano — Iluminação no Hangar e panes por disjuntor/circuito

Criado em 2026-11-16 (Claude). **Estado (conferido em 2026-11-25):** parcial — lâmpadas como peças do grupo Iluminação e MEL noturna só com aviso (decisões 1–3 da § 7, 2026-11-17). Não implementado: panes por disjuntor/circuito e atuação no sim (§§ 3, 4 e F4), que dependem de teste no MSFS e das decisões 4 e 5 da § 7. Estado atual: `docs/sistemas/pecas.md`.
Origem: `docs/plans/sensor-research/variaveis-eletrica.md` (disjuntores, circuitos, linhas e relés graváveis no sistema elétrico v2) e a próxima fase do `AGENTS.md` › 7 ("Avaliar aviônicos: disjuntores por aeronave no MSFS 2024").

## 1. Objetivos
1. **Panes por sistema** usando o sistema elétrico do sim (disjuntor, circuito, linha), em vez de mascarar comandos do piloto: GPS, piloto automático, transponder, rádio, flap elétrico, aquecimento do pitot, barramento de aviônicos, alternador sem chegar ao barramento.
2. **Iluminação** como grupo de peças no Hangar (lâmpadas com vida útil), com pane de **lâmpada queimada** aplicada no sim e ligação com a MEL (NO-GO noturno).

## 2. O que dá e o que não dá
| Pane | Pelo sistema elétrico? | Observação |
|---|---|---|
| Magneto | **não** | O magneto não depende do sistema elétrico (nem no avião real). Continua por `RECIP ENG LEFT/RIGHT MAGNETO` (watchdog atual). |
| Flap elétrico | **sim** (pane elétrica) | Puxar o disjuntor/circuito do motor do flap: o flap para e o piloto vê o disjuntor saltado. É uma pane **diferente** da mecânica (travamento), que continua pelo método atual: ver § 2.1. |
| Alternador | sim (alternativa) | `ALTERNATOR CONNECTION ON`/`ALTERNATOR BREAKER PULLED` ou a linha alternador→barramento; hoje é `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`. Usar ao menos como **confirmação**. |
| GPS, piloto automático, transponder, rádio, aviônicos | sim | `BREAKER GPS/AUTOPILOT/XPNDR/NAVCOM1..4/AVNBUS1..2` ou `CIRCUIT BREAKER PULLED:n`. Hoje só existe `TOGGLE_ELECTRICAL_FAILURE` (total). |
| Barramento parcial | sim | `BUS BREAKER PULLED:n` / `LINE BREAKER PULLED:<linha>`. |
| Luzes (pouso, táxi, navegação, strobe, beacon, painel) | sim | Disjuntor ou `CIRCUIT SWITCH ON:n` do circuito da luz, com watchdog. |
| Trem elétrico | talvez | `CIRCUIT GEAR MOTOR ON` / disjuntor do motor do trem → extensão de emergência. Provar num retrátil elétrico. |

## 2.1 Flap: pane elétrica × pane mecânica (decisão de 2026-11-16, a confirmar)
São duas panes diferentes, com causa, sintoma, procedimento e reparo diferentes. As duas coexistem.

| | **Elétrica** (disjuntor saltado / fusível queimado) | **Mecânica** (travamento) |
|---|---|---|
| Causas | sobrecarga do motor do flap (motor gasto, acionar o flap acima da Vfe com carga aerodinâmica alta), curto na fiação, relé/chave | voo acima da Vfe com flap baixado, pancada (pouso duro, bird strike, FOD), trilho/rolete/cabo gasto; `FLAP DAMAGE BY SPEED` nativo |
| Sintoma no cockpit | flap para; **disjuntor visivelmente saltado** no painel; indicador parado | flap para **na posição em que estava**; disjuntor normal; pode haver **assimetria** (um lado) |
| Efeito no sim | disjuntor/circuito do motor do flap (`BREAKER FLAP` / `CIRCUIT BREAKER PULLED:n`) | posição congelada (método atual do `SimFailureActuator`: força `FLAPS HANDLE INDEX` e `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT`) |
| Procedimento do piloto | checklist: conferir o painel de disjuntores/fusíveis; **rearmar uma única vez** (regra real); se saltar de novo, deixar puxado e pousar sem flap | nada a fazer em voo; pouso com o flap travado (velocidade e pista maiores) |
| Resultado do rearme | sorteio pela causa: **transitória** (rearme resolve) × **persistente** (salta de novo em segundos). Rearmar várias vezes = risco (dano na fiação, cheiro de queimado, pane elétrica maior) | — |
| Fusível × disjuntor | **fusível** (aviões antigos): não rearma; o app trata como disjuntor que o watchdog sempre puxa (troca só no solo) | — |
| Registro | blackbox e nota de procedimento: conferiu o painel? rearmou uma vez? insistiu? | blackbox; dano nas peças do flap |
| Reparo no Hangar | barato: motor do flap, disjuntor/fusível, fiação | caro e com downtime: trilhos, roletes, atuador, inspeção estrutural da asa |

- **Ligação com as regras de desgaste:** acionar o flap acima da Vfe sobrecarrega o motor → chance de **pane elétrica** (disjuntor salta); voar acima da Vfe com flap baixado ou pancada → chance de **pane mecânica**. Motor do flap gasto → pane elétrica por taxa por hora.
- **Detectar o check do piloto:** `CIRCUIT BREAKER PULLED:n` do circuito do flap mudando de 1 para 0 sem o app ter mandado = rearme do piloto (provar no sim que a SimVar acompanha o disjuntor do cockpit 3D).
- **Flap hidráulico** (jatos): a pane "elétrica" vira perda de pressão/válvula; a mecânica é igual.
- Novos efeitos: `FlapCircuitOpen` (elétrica), `FlapJammed` (mecânica, o atual), `FlapAsymmetry` (mecânica de um lado).

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
4. **F4 — Flap elétrico × mecânico:** pane elétrica por disjuntor/fusível com rearme único (aviões mapeados) ao lado da pane mecânica atual (travamento e assimetria); ligação com as regras de Vfe e pancada.
5. **F5 — MEL:** NO-GO noturno de luzes e itens diferíveis (junto com a fase MEL do `AGENTS.md`).

## 7. Decisões do usuário
1. Iluminação como **grupo de peças** (dentro do componente Elétrico na barra antiga) ou componente próprio no Hangar?
2. Vida útil e preço das lâmpadas (halógena × LED) e se o perfil da aeronave diz qual ela usa.
3. Regras de MEL: quais luzes são NO-GO à noite e se o app bloqueia o voo noturno ou só avisa.
4. Flap: confirmar as duas panes (elétrica por disjuntor/fusível e mecânica por travamento), o rearme único e o sorteio transitória × persistente (§ 2.1).
5. O piloto pode "rearmar" um disjuntor em pane uma vez (como na vida real, com risco) ou o watchdog sempre puxa de novo?

**Decididas pelo usuário (2026-11-17):** 1. Iluminação = peças dentro do Elétrico. 2. Tudo halógena (sem campo LED). 3. MEL noturna só avisa por ora. 4 e 5 (flap elétrico × mecânico e rearme) continuam abertos.

## 8. Testes previstos
- `ElectricalMap_SemMapa_UsaEfeitoAtual`.
- `Watchdog_DisjuntorRearmado_Reaplica` (lógica pura do estado desejado × lido).
- `Lampada_HorasAcesas_DesgastaPorTempoSimulado`.
- `Lampada_Falha_PorTaxaPorHora` (`IRandomSource` determinístico).
- `Mel_NavQueimada_NoGoNoturno`.
