# Plano de correção: briefing VFR, mapas, pouso forçado e Hangar

Pedidos do usuário e análise de 2026-11-26. **Nada implementado ainda.**
Fontes: capturas do app, REC `20261126-0719-Cessna_C152.csv`, blackbox `flight_20261126_072028_SDCO_SDCO_PT-HRY.csv`, banco local (C152 PT-HRY).

## A. Pedidos diretos (pequenos)

- [x] **A1. Remover o recurso de waypoints do mapa** (código morto: nada adiciona pontos). Em `FlightMapControl.xaml`: quadro "WAYPOINTS ATIVOS"/`TxtDistance` e botão LIMPAR. No `.cs`: `_waypoints`, `_waypointRoute`, `BtnClear_Click`, `ShowWaypointsOverlay` (e o uso em `DebugOverlayWindow.xaml`); renomear `CreateWaypointCircle` e a região.
  **Não tocar** em `Mission.OverflyWaypoints`/`OverflyWaypointCodec` (pontos dos contratos). O LIMPAR dos filtros em `MissionsView.xaml` é outro botão e fica.
- [x] **A2. Texto:** "ponto de reportagem VFR" → "ponto de reporte VFR" (`mission_texts.json` linhas ~33, 47, 61, 71; `MissionGenerator.cs` ~1267-1412, inclusive o nome gravado do ponto). Missões já geradas guardam o texto antigo. Conferir golden/testes.
- [ ] **A3. Apagar a linha verde "Circuito de Tráfego: Toque e Arremetida (TGL)"** em missão de navegação (ver B1).

## B. Briefing e mapas

- [ ] **B1. Navegação tratada como TGL.** `BriefingViewModel.RebuildItineraryAndValidateAsync` (~327): toda missão com origem = destino, ou categoria "Local", vira TGL (texto verde, circuito âmbar falso, "(TGL)" no título "SDCO (TGL)"). Vale também para área de instrução, aerofoto, paraquedismo, STOL.
  Correção: TGL só com `RequiredTouchAndGoes > 0`; missão com `MultiStopContractData` vira pernas reais (origem → escalas → origem), com ação por perna ("Pouso completo em X" ou "Toque e arremetida em X" conforme `StopMode`).
- [ ] **B2. Escalas não aparecem em nenhum mapa** (briefing, HUD, quadro): `ContractMarkerBuilder` só desenha `OverflyWaypoints` e aerofoto/salto/aerial; ignora `MultiStopContractData.Stops`. Desenhar escalas numeradas e a linha do triângulo (origem → ponto de reporte → escalas → origem). Cobrir também STOL e outros contratos com aeródromo.
- [ ] **B3. Ponto de reporte fora do enquadramento no briefing:** `RouteLegs` é montado depois de `ContractMissions`, e `UpdateMultiLegRoute` dá zoom no circuito falso. Enquadrar rota + contrato num passo só. HUD: conferir no MSFS (usa o mesmo construtor, então hoje mostra só o ponto de reporte).
- [x] **B4. Missão após pouso forçado:** (POUSO-DANO: cancelada, XP 50 %, sem dinheiro) a origem passou a SNZG (47 NM), mas título e descrição seguem "SDCO ... 214 NM" com as escalas antigas. Definir: texto de resgate/retomada, ou missão falha.

## C. Último voo (SDCO → pouso forçado perto de SNZG): o que aconteceu

- Flap 10° de 10:29:29 a 10:30:18 UTC, até **147 kt** (limite do app 110; Vne 149 não ultrapassada). Registrado `flap_overspeed_vfe` (até "severe"), flap −0,3 % no total. O simulador não marcou dano de flap (`flap damage by speed` = 0).
- RPM até 2.820 (dano ~0,5 % por evento forte); `nw-is-weak-39` às 10:36:32 e falha nativa do motor às 10:39:05 (durante aceleração até 32×).
- Toque 1: **733 fpm**, 2,31 G, inclinação 18,5°, GS 57 kt; toque 2: 201 fpm. Pane de óleo após o toque.
- Registrado: 2 panes (motor 8.000, óleo 900), voo SDCO→SNZG, receita 0, 125 XP, missão mantida.

**Trem e pneus (pergunta do usuário):** sofreram dano.
- Pela peça: montantes −2,5 % (97,5), pneus principais 94,3 / nariz 95,1, asa −6 %, pontas −12 %, ailerons −8 %.
- Pelo pouso fora de pista: −35 pneus e −20 célula (`FlightService` ~189-193), gravados só nos campos antigos/componentes (pneus 59,4; célula e trem 71,2). **As peças não mudam**, o Hangar mostra "Em dia" e a próxima sincronização peça→componente tende a apagar a penalidade (conferir com teste).

**Por que o cárter quebrou e o trem não** (pergunta do usuário): são regras diferentes. O vazamento de óleo é um sorteio no pouso duro (`oilLeakMinFpm` 650, 50 % a 1000 fpm; a 733 fpm, cerca de 12 %). Trem e pneus não têm chance de quebra: só desgaste fixo por faixa (600–850 fpm: 2,5 % e 3,5 %; ≥ 850: 10 % e 12 %), e ≥ 900 fpm é perda total.

- [x] **C1. (entra na tarefa 2)** Pouso duro com chance de quebra de trem e pneu, crescente com fpm, inclinação e terreno fora de pista; calibração no JSON e consequência visível no Hangar. Chances e limites: decisão do usuário na spec.

## D. Bugs achados na análise (investigar com Opus)

- [x] **D1. Penalidade de pouso forçado não chega às peças** (acima). Aplicar nas peças, com teste. Reavaliar a severidade da faixa "hard" (600–850 fpm: só 2,5 % no trem).
- [x] **D2. Uma pane nativa quebrou 11 peças** (PANE-PECAS) no mesmo instante (ID 39): agora quebra exatamente 1 peça prioritária via `NativeFailurePartMap` (menor saúde, ordem de tabela, menor motor).
- [x] **D3. Cobrança em dobro:** (PANE-PECAS) unificado via `DefectPartLink`: reparar a pane inclui a substituição das peças ligadas com custo da pane zerado; trocar a peça física resolve a pane ligada no Hangar.
- [x] **D4. Peça quebrada não interdita:** (PANE-PECAS) `PartAirworthiness.GroundingParts` confere `BlocksFlightWhenBroken == true` e interdita o avião tanto no status do Hangar ("INTERDITADA (PEÇA)") quanto no `FlightMonitor.StartMonitoring`.
- [x] **D5. Inspeções de voo só vão para o log:** `OnInspection` só chama `LogEvent` (pouso duro, ponta de asa, flap, sobre-rotação, comandos). Não aparecem no Hangar nem no relatório.
- [x] **D6. `isGrounding` ignorado:** a regra de ponta de asa em `wear_rules.json` usa `isGrounding`; o DTO só lê `grounding`.
- [ ] **D7. Excedência falsa em solo:** aos 31 s, durante slew no pátio, IAS 377 kt gerou Vne "severe", dano e inspeção. Filtrar slew/solo/valor implausível.
- [ ] **D8. Vfe do C152 dente 1 = 110 kt** (vem do SimVar); dentes 2 e 3 = 85 (POH). Conferir no POH; se 85 vale para todo flap, o voo excedeu muito mais.
- [ ] **D9. `piston_rpm_overspeed`:** 2550 sem histerese gera dezenas de eventos (já em `achados-voo-sdco.md`).
- [x] **D10. Relógio de carreira** não avança no caminho de pouso fora de pista (`FlightService` ~188-261; confirmar se outro ponto avança).
- [x] **D11. Blackbox:** "Status Final: VOO_CONCLUIDO" em pouso forçado; resumo com G máx. 1,82 e o toque mediu 2,31.

## E. Hangar (pedidos e sugestões do usuário)

- [x] **E1. Aviso de oficina duplicado:** faixa "OFICINA ... Libera em" (`PilotAircraftWrapper` ~233) e cartão "Na oficina — libera em" (`WorkshopBannerText`). Manter um.
- [x] **E2. "Reparar todas as panes" × "Reparar tudo":** (PANE-PECAS) fluxo único implementado no Hangar: `RepairAll` inclui todas as peças recomendadas e todas as panes abertas (as ligadas a peças saem sem custo duplo e as sem peças cobram seu valor), habilitando o botão mesmo que só haja pane aberta.
- [x] **E3. Preço do botão ≠ cobrado:** botão 8.900, cobrado 7.120 (dificuldade ×0,80, avisada só na confirmação). Mostrar o preço final.
  Pedido do usuário (2026-11-26): trocar "Dificuldade Fácil: custos ×0,80" (`HangarViewModel` ~268) por só o desconto, em %: "Desconto da dificuldade Fácil: 20%" (acima de 1,00: "Acréscimo da dificuldade X: N%"). É % de dinheiro, permitido pela regra da parte F.
- [x] **E4. Texto amigável das panes:** tirar "SIMULADOR · VOANDO (PLANANDO...) · WEAR AND TEAR IS FAILED:39..." (e o prefixo "[Operacional]"). Ex.: "O motor parou de funcionar em voo". Técnico só no log/tooltip. Botão com "Reparar · valor" formatado.
- [ ] **E5. Peça "nunca inspecionada" com ✓ verde:** contradição (73/73 sem leitura).

### Sugestões do usuário (redesenho; fazer spec)

1. **Gestão por exceção:** carimbo "última checagem" no cabeçalho da seção; na linha, tempo só quando envelhecer ("~28h atrás"); origem por micro-ícone com tooltip (olho = walkaround, gota = óleo, prancheta = anual).
2. **Barra de 4 blocos, sem %:** 4 íntegro (verde), 3 bom (verde-lima), 2 atenção (amarelo), 1 crítico (vermelho), vazio com "?" = não inspecionado. Leitura antiga fica translúcida (45 %) ou contornada. *Claude: falta o estado "quebrada".*
3. Peça oculta mostra "Requer Troca de Óleo/Anual" no lugar do botão.
4. Card "Condição geral": "73 componentes" + barra proporcional por cor.
5. **Lista em grade de 2 colunas** de cards compactos: barra à esquerda, preço em pílula ("Trocar · $2.800") realçada no hover; 2ª linha só quando há informação (idade, alerta, inspeção que revela).
6. **Cabeçalho do sistema recolhido** com pílulas de resumo (ex.: 13 verde, 1 amarelo, 1 ?).
7. **Filtro no topo:** Todos / Atenção-Crítico / Não inspecionados, com contagem.
8. Walkaround ($0) dentro do card Inspeções, em ordem: Walkaround → Troca de óleo → Anual → Overhaul.
9. Hover numa inspeção realça (borda ciano) as peças que ela revela.

## F. Nenhum aviso com % (pedido do usuário, 2026-11-26)

Ex. visto: "RISCO DE PANE — Ignição / magnetos em ~0%: chance de pane a cada hora de voo."
Trocar por estado em palavras (os mesmos das peças). Ex.: "Ignição / magnetos com peça quebrada. Troque antes de voar." Levantamento por busca no código (não conferido tela a tela com o app aberto).

**F1. Hangar (saúde de peça/componente)**
- [ ] `ComponentMaintenance.cs` ~111 e ~114: avisos "RISCO DE PANE" e "ATENÇÃO" com `{worst.Range}`. Usam a saúde do componente mesmo com peças (revelam peça nunca inspecionada).
- [ ] `HealthVisibility.cs` ~18-19 (`"{min}%"`, `"~{min}%"`, faixa): origem do `RangeText` (`HangarView.xaml` ~419; confirmações em `HangarViewModel` ~569 e ~673).
- [ ] `PartHangarView.cs` ~235 (`HealthText` "NN %").
- [ ] Motivos "Faixa vermelha/amarela: NN %" e "Saúde NN %": `PartInspectionFlags.cs` ~39, ~44; `PartReplacementPlanner.cs` ~113-121.
- [ ] `HangarView.xaml` ~324 (tooltip "±10%") e ~372 (legenda "48–52%", "acima de 40%").
- [ ] "a 100%" em `HangarViewModel` (~523, 562, 569, 579, 667, 673, 691, 745, 832, 883) e `PilotAircraftWrapper` ~338: "como nova(o)".

**F2. Avisos em voo e panes**
- [ ] `ComponentFlightSession.cs` ~181, ~203, ~297 ("em NN%", "dano: NN%"); `SimFailureActuator.cs` ~176.
- [ ] Causa de pane em `PersistentFailureService.cs` ~440, ~455 (aparece na lista de panes; ver E4).

**F3. Mercado de usados (decidido pelo usuário em 2026-11-26: tirar o %)**
- [ ] Condição média, célula, motor, pneus e aviônicos em `MarketView.xaml` (~449-975): mostrar estado em palavras/cor, como no Hangar. Conferir se o valor exato aparece em outro texto do mercado.

**F4. Decidido (usuário, 2026-11-26): manter os % que não são condição**
- Selo de desconto "- N%" do mercado (`MarketView.xaml` ~412).
- Taxas de dinheiro (corretagem 8%, expresso +50%, bônus/XP), nota do voo ("47%"), "% MTOW" e envelope no briefing, "100% EXATO", flap/N1/opacidade no HUD, restrição de flaps em ferry. Ficam como estão: selo "- N%", taxas, nota do voo, "% MTOW", envelope, "100% EXATO" e instrumentos do HUD. A tarefa 8 (SEM-PORCENTO) só tira % de condição (F1–F3).

Confirmado também pela captura: depois de pagar as panes o avião aparece como "RISCO DE PANE", não interditado, com peças quebradas (reforça D4).

## Tarefas e modelo recomendado (2026-11-26)

Uma tarefa por sessão, na ordem. "Modelo" = sessão do Claude (spec e revisão).

| # | ID | Itens | Modelo | Quem implementa | Depende de |
|---|---|---|---|---|---|
| 1 | PANE-PECAS | D2, D3, D4 e o fluxo único de E2 | Opus | Gemini; revisão linha a linha | — |
| 2 | ✅ POUSO-DANO | C1, D1, D5, D6, D10, D11 | Opus | Gemini; revisão linha a linha | 1 |
| 3 | LIMPA-MAPA | A1, A2 | Sonnet | Gemini (golden pode mudar) | — |
| 4 | BRIEF-NAV | A3, B1, B3 (`BriefingViewModel`, quente) | Opus | Gemini | 3 |
| 5 | MAPA-ESCALAS | B2 | Sonnet | Gemini | 4 |
| 6 | ~~RESGATE~~ | B4 decidido: cancelar a missão (entra na tarefa 2) | — | — | — |
| 7 | ✅ HANGAR-AVISOS | E1, E3, E4 | Sonnet | Gemini | 1 |
| 8 | SEM-PORCENTO | F1, F2, F3 | Sonnet | Gemini | 1, 7 |
| 9 | EXCEDENCIAS | D7, D8, D9 e pneus/combustível/pausa de `achados-voo-sdco.md` | Opus (investigação) | Claude pesquisa; Gemini corrige | 2 |
| 10 | HANGAR-REDESENHO | E5 e sugestões 1–9 | Opus (spec) | Gemini, em etapas | 1, 7, 8 |

3 a 5 não dependem de 1 e 2 e podem ir antes. `HangarViewModel` entra em 1, 7, 8 e 10: nunca em paralelo.

## Decisões do usuário (2026-11-26)
- **B4:** após pouso forçado fora de pista a missão é **cancelada** e não volta ao quadro; o piloto ganha **XP parcial (50 %)** e **nenhum dinheiro**. Entra na tarefa 2 (POUSO-DANO), que já mexe nesse trecho do `FlightService`; a tarefa 6 (RESGATE) deixa de existir.
- **D8:** Vfe do C152 = **85 kt** para todos os dentes de flap (vai para a tarefa 9).
- **C1:** chance de quebra de trem e pneu **baixa no pouso duro e mais alta no muito duro**. Calibração (Claude, 2026-11-26) em `component_catalog.json` › `landing`: 0 abaixo de 600 fpm; trem 1 %→5 % de 600 a 850 fpm; 15 %→30 % de 850 a 900 fpm (≥ 900 já é perda total); pneu ×1,5; inclinação ≥ 10° ×1,5; fora de pista (`ON ANY RUNWAY` = 0) ×2; peça gasta até ×2; teto 60 %. Quebra = peça `IsBroken` (interdita via `PartAirworthiness`), sem pane extra (evita cobrança em dobro).
- **D5 (Claude, 2026-11-26):** inspeção do voo vira pane persistente do addon (aparece no Hangar, custo base 400). Só as inspeções de toque (pouso duro, cauda, ponta de asa, vento cruzado, bequilha) respeitam o `grounding` do JSON; as demais ficam DIFERIDAS até a tarefa 9 tirar os falsos positivos (D7, D9).

- **F4:** manter os % que não são condição (selo "- N%", taxas, nota do voo, "% MTOW", instrumentos). A tarefa 8 só trata F1–F3.

## Decisões abertas do usuário
- Nenhuma neste plano.

Não testado no MSFS: tudo acima vem de leitura de código, log, REC e banco.
