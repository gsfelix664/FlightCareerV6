# Atualização 2026-11-22 (Claude, PC de casa)
- Mergeados hoje: ECON-DIFF (revisão `docs/reviews/ECON-DIFF-revisao.md`), faixa "~51%", **HANGAR-ICONES-PECAS** (estado das peças por ícone, 5 estados: nova ≥ 80, gasta mas boa 80–40, atenção 40–20, quebrada < 20, "?" incerta; sem % no Hangar; horas de TBO/óleo/ignição corretas por grupo) e o combustível do briefing (lia o avião em cache). Suíte 737/737. Nada visto na tela.
- **Calibração do pagamento em andamento** na branch `feat/calibra-pagamento` (não commitada): marcos 30/60/90/180/365/540/730/1095 no simulador e um contador temporário `DBG-CAL` (receita por classe × tipo de missão) — **remover o `DBG-CAL` antes do commit**. Referência (escalas 1,0): 1ª troca dia 71–88 (na meta), classe F no dia 180 (meta 365), agressivo $8,3 mi no 1º ano (meta ≤ $2 mi), H no dia 1095. Escala 0,35 fora da instrução: 1ª troca no dia 124–165 (tarde demais), classe E no dia 365. Próximo: medir a receita por família (rodada `DBG-CAL` interrompida para o voo do usuário) e cortar só o que as classes B–G voam. Cada rodada de 1 ano (Médio + Difícil, 3 bots) leva ~11 min; 3 anos, ~38 min.
- **Correções futuras pedidas pelo usuário (HUD, `DebugOverlayWindow.xaml` / `DebugOverlayViewModel.cs`):**
  1. Mapinha do HUD (`Utils/FlightMapControl`): pôr um ponto na **partida**, igual ao ponto verde da chegada.
  2. HUD ainda mostra **%** de saúde (`AirframeCondition`/`EngineCondition`/`TiresCondition`, linhas ~96–101 e ~189–191). Deve mostrar só os ícones do Hangar (`PartHangarView.DisplayState`/`Summarize`, `HangarIcons`). Atenção: hoje são as colunas antigas = **saúde real**, ou seja, o HUD vaza a saúde oculta; usar a saúde conhecida como no Hangar.

---

# Passagem de sessão (Claude) — 2026-11-21, fim do dia (PC de casa, com MSFS)

Base `feat/component-health-system` no GitHub. Build 0 erros; `dotnet test` 697/697. Primeiros voos reais no MSFS 2024 com o app (C152 em SNEJ). Leia junto: `AGENTS.md` (§ 6 e § 7 atualizados hoje), `docs/CHANGELOG.md` (entradas de 2026-11-21).

## Feito hoje (mergeado e no GitHub)
1. **Óleo:** `SimFailureActuator.Reset` só desfaz vazamento criado pelo app (antes gravava 0 = esvaziava o óleo a cada voo, pois no MSFS 2024 a SimVar parece valer 100 sem pane). A inversão no vazamento ativo continua pendente.
2. **MANUT-FAIXAS-B** (Gemini, aprovada): lista "Peças (n)" no Hangar com saúde conhecida e **Trocar**; "Reparar tudo" só pelo conhecido; troca avulsa não devolve TBO/125 h; cores 40/20. **Não vista na tela.**
3. **Combustível:** escrita de `FUELSYSTEM TANK QUANTITY:n` define o total e a leitura devolve o utilizável → `FuelTank.ToSimWriteValue` soma o não utilizável (testado no C152); legado lê e grava o total (testado no TBM 930); 747-8i tem 8 tanques (testado). Depois o usuário decidiu **abastecimento manual**: compra no Hangar, coloca no sim (menu/EFB); o app **não grava** mais nos tanques; briefing e `StartMonitoring` conferem e bloqueiam se divergir; abastecer após iniciar invalida o voo. Gravação fica para panes futuras.
4. **Overlay de mensagens:** `AlertOverlayService` mostra os alertas do voo, a perda total e o resultado por cima do sim (sem roubar foco). **Não visto na tela.**
5. **Blackbox:** excedências agrupadas (`ExceedanceLogAggregator`: uma linha INICIO e uma FIM por faixa).
6. **RPM do pistão:** `piston_rpm_overspeed` só penaliza a partir da linha vermelha (atenção 100 %, alerta 104 %), decisão do usuário (subida a pleno no arco verde é normal).
7. **Peças de vida em ciclos** (pneus, freios, rodas) envelhecem a cada pouso no jogo (`PartWearEngine.ApplyLandingCycle`), como o simulador de carreira.
8. **ECON-STAB com as faixas:** 0 falências, cauteloso ≈ agressivo; margem ~90 % ainda deixa todos ricos (`economia-estabilidade.md` › 4.2). Decisões D (curva) e E (dificuldade) **aprovadas** (› 4.3).
9. **Plano de XP aprovado:** `docs/plans/xp-proficiencia.md` › 5.1 (minutos no ar, qualidade, bônus de dificuldade). **Implementação pendente.**

## Pendente (em ordem)
1. ~~**ECON-DIFF**~~ **mergeada em 2026-11-22** (`docs/reviews/ECON-DIFF-revisao.md`): Médio reproduz a base e a simulação é reprodutível.
2. **Calibração do pagamento** (D + E): ajustar `scale` por família em `mission_pay.json` com a ECON-STAB até a curva do Médio (1ª troca dia 60–90, turboélice ~1 ano, jato no 3º ano, agressivo ≤ 20× no 1º ano); Difícil 1,5× mais lento.
3. **XP** (spec para o Gemini a partir de `xp-proficiencia.md`), com os achados 2 e 3 do plano: avião usado do mercado com TBO acima do limite (PP-UZJ: 1.791 h com TBO 500) e peças com ~50 % e 0 h/0 ciclos; bônus de pista difícil no pagamento por `Runways.Surface`/comprimento em vez do tier. Taxa base e exigências calibradas na ECON-STAB.
4. **Óleo:** inverter a semântica de `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo (`ProcessOilLeak`/`TransmitOilLeak`) após confirmar no MSFS.
5. **Perda de desempenho no amarelo:** `docs/plans/perda-desempenho-amarelo.md` (teste no MSFS incompleto).
6. **Plano futuro (adiado pelo usuário): matrícula do Hangar no sim** (`ATC ID` gravável, STRING32; `variaveis-aeronave-diversos.md:81`). Teste de console, gravar ao iniciar e no `AircraftLoaded`, guardar o original.

## Voos reais de hoje (C152, SNEJ, 5.130 ft, pista de terra)
- **PS-BYN:** estol em subida com queda de asa (IAS 58 → 32 kt com nariz subindo, mistura toda rica, alta densidade) → crash detectado pelo addon → **perda total**. Nenhuma interferência do app. Não conferido se o seguro foi acionado.
- **PP-UZJ** (comprado usado): voo de instrução local de 17,7 min (7,4 min no ar), toque 645 fpm, nota 64 %, $344,60, 23 XP, combustível 0,6 gal conferido com o fluxo; desgaste por eventos coerente (pneus −8 %, montantes −3,5 %). O voo rodou com a versão anterior às correções 5–7.
- Para conferir no próximo voo: overlay (aparece, não rouba foco), conferência de combustível manual no briefing e no início, óleo normal no início, lista de peças no Hangar, blackbox com INICIO/FIM.

## Notas de trabalho
- Com o app aberto, `dotnet build` falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`.
- Testes de SimConnect sem o app: programa de console no scratchpad referenciando `FlightCareer.Domain` e a DLL do SimConnect da UI (usado hoje no combustível); sempre restaurar o estado do sim no fim.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/`; REC em `%LocalAppData%\FlightCareer\sensor-logs\` (o fim fica congelado após crash: o MSFS 2024 reposiciona o avião).
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar ainda.

---

# Passagem anterior — 2026-11-20 (PC de casa, com MSFS)

## Atualização 2026-11-20 (vale sobre o que está abaixo)
- **Manutenção por faixas (decisões do usuário):** `docs/plans/economia-estabilidade.md` › 4.1. Verde 100–40, amarelo 40–20 (desgaste acelerado até 2× e perda de desempenho), vermelho < 20 (pane por peça). Reparo sem limite automático de 80 %; revisões trocam o conjunto delas; walkaround mostra valor exato; só a anual interdita (e só inspeciona); seguro 2,5 %/ano, nega perda total com achado vermelho da anual não trocado.
- **MANUT-FAIXAS-A mergeada** (Gemini + ajustes do revisor; `docs/reviews/MANUT-FAIXAS-A-revisao.md`): build 0 erros, 673/673. **Nada testado no MSFS.**
- **Próximo:**
  1. **ECON-STAB** com as regras novas (`career-stab --days 720 --bots 3 --difficulty all --policy all --fast`, ~45 min): o cauteloso deixou de falir? o agressivo tem mais panes? Calibrar `lowHealthWearMaxMultiplier` e o seguro se preciso. Depois propostas D (curva de progressão) e E (dificuldade) do plano.
  2. **MANUT-FAIXAS-B** (Hangar): lista de peças por card com `KnownHealth`/`KnownAtAirframeHours` ("visto há X h"), cor da faixa, nota de vida (`PartInspectionFlags.LifeNote`) e botão **Trocar** por peça; "Reparar tudo" = quebradas + vermelhas. Spec ainda não escrita.
  3. **Bug provável do óleo:** no MSFS 2024 `GENERAL ENG OIL LEAKED PERCENT:1` = **óleo restante** (100 sem pane, cai devagar com o motor ligado, pressão normal; probe `20261120-0822-C172SP_G1000_Passengers`). O `SimFailureActuator` supõe 0 = sem vazamento (sobe até 100; limpeza grava 0 = esvazia). Confirmar e inverter.
  4. **Perda de desempenho no amarelo:** `docs/plans/perda-desempenho-amarelo.md`. Teste no MSFS incompleto: o Watcher deu `DATA_ERROR` (pedido com *Is String* marcado e índice 0); repetir passos 1–5 com a linha `:1` selecionada, *Is String* desmarcado, e o SimVarProbe gravando (filtro com `(:\d+)?$`, sair com `q`, não usar Ctrl+C). Lista do Watcher: `tools/simvar_probe/watcher/flightcareer-teste-desempenho.simvars`.
- `REGRAS_tiers.txt` (raiz, do usuário, não commitado): regras de missões Tier 1 — o usuário pediu para não analisar ainda.

---

# Passagem anterior — 2026-11-19 (PC do escritório)

## Atualização 2026-11-19 (vale sobre o que está abaixo)
- Base `feat/component-health-system` no GitHub; build 0 erros; `dotnet test` 655/655; **nada testado no MSFS nem visto na tela**.
- Mergeados hoje (todos aprovados): correção do **R$ → $** (cultura en-US no construtor do `App`; antes caía no `async OnStartup`), **CALIBRA-F4b** (as 153 peças com a calibração do usuário `docs/agents/specs/calibracao-pecas-usuario-2026-11-19.json`; preço = base × multiplicador da classe A 0,75…J 5,5; TBO 500/900 h; inspeções 12/25/125 h; DA62 Vle 156, King Air 350 Vne 263), junto com **CATALOGO-MOTOR** e **CALIBRA-F4**; **CALENDARIO-JOGO** (`CareerClock`: só avança com voo + 0,5 h e oficina; seguro e vida em meses usam ele); **REPARO-PECAS** (troca a preço cheio só peças gastas); **CABINE** (conversão só em `convertibleCabin`, custo/parada por porte, efeito nas missões); **ECON-STAB** (`FlightCareer.Replay career-stab`, carreira completa com compra/venda, 3 dificuldades × 3 estilos). Decisões do arquivo do usuário (`docs/tarefas_sem_IA.md`) registradas nos § 3 dos planos de eventos/combustível/API/iluminação.
- **Análise da economia:** `docs/plans/economia-estabilidade.md` — quem faz manutenção certa vai à falência e quem adia enriquece (limite de troca 80 % alto demais; pane só abaixo de 20 %; seguro 12 %/ano); margens 60–95 %; dificuldade só muda o saldo. Propostas A–F e **5 decisões do usuário** pendentes antes do rebalanceamento.
- Pendentes conhecidos: multiplicadores por classe duplicados (código e `economy_targets.json`) — ler do JSON; `FlightService.ProcessFlightCancellationAsync` não está ligado (cancelamento não avança o relógio); queda de utilização após o upgrade na simulação (investigar, plano › F); teste do tempo acelerado no MSFS (`ROTEIRO.md` › 8).
- Lição nova: `dotnet test` não compila a UI — sempre `dotnet build src/FlightCareer.slnx` (com o app aberto, `-p:OutDir=<pasta temporária>`).

---

(Histórico de 2026-11-17)

Leia junto: `AGENTS.md` (§3 comandos, §6 regras, §7 estado), `CLAUDE.md`, `docs/agents/LICOES.md` (lição nova: não relaxar metas/oráculos) e os vereditos em `docs/reviews/`. As specs desta sessão estão em `docs/agents/specs/` (as próximas também devem ser copiadas para lá ao delegar).

## Máquina e regras
- **PC do escritório: sem MSFS e sem SDK.** Build, testes, fuzz e simulador de carreira rodam; nada que precise do simulador.
- Divisão: código e dados → Gemini com spec (`tools/agents/gemini-task.sh <ID> <branch> <spec>` em segundo plano; worktrees `../FlightCareerV6-gemini` e `../FlightCareerV6-gemini2`, um por tarefa em paralelo via `FC_GEMINI_WORKTREE`; `FC_BASE=<branch>` para partir de outra branch). Claude escreve a spec, revisa, roda build/testes/fuzz, escreve `docs/reviews/<ID>-revisao.md`, faz merge e push com `FC_ALLOW_PROTECTED=1` (memória: push ao fim de cada trabalho aprovado).
- **Limite de 1h30 do executor:** tarefas grandes (calibração com otimização, simulação longa) estouram e terminam **sem commit**. Divida a tarefa ou peça para o Gemini não rodar a simulação completa (o revisor roda).
- Quota do Claude e do Gemini no fim: usuário volta no **sábado**.

## Estado da base (`feat/component-health-system`, enviada ao GitHub)
- Build 0 erros; `dotnet test` 612/612.
- Mergeados hoje (todos aprovados; **nada testado no MSFS**):
  1. **LIMITES-V4** — 130 limites do `tools/aircraft_limits/sources/aircraft_limits-v4.json` (fonte do usuário, não conferida) em 17 perfis; nenhum POH alterado.
  2. **LUZES-F2** — grupo Iluminação (6 lâmpadas halógenas, desgaste só acesa, queima por taxa, `minorPart`, `LightingMel` só avisa); sem atuação no sim.
  3. **HANGAR-ICONES** — Hangar não lista peças; após inspeção/walkaround, chips com exclamação amarela (vida ≥ 90 %, vencida ou saúde < 40 %) ou vermelha (quebrada) até o próximo voo. **Não visto na tela.**
  4. **Ícones dos grupos no Hangar** (Claude) — a barra de saúde virou ícone vetorial por grupo, verde/amarelo/vermelho pelas mesmas faixas; **não visto na tela** (confira no sábado).
  5. **ECON-SIM** — `FlightCareer.Replay career` (bots × missões reais × custos do jogo → `summary.md`). Funciona (C152, 3 dias, 23 s), mas a rodada grande (8 aviões × 5 bots × 90 dias) **não terminou em ~1 h**: rodar por avião ou otimizar antes.
- Lista de peças para o usuário: `docs/plans/lista-pecas-hangar.md` (ainda com os valores antigos; será regenerada pela CALIBRA-F4).

## Branches pendentes (no GitHub, **não mergeadas**)
1. **`feat/catalogo-motor`** (revisada, ajustes do revisor feitos): remove `electrical.external_lighting`; J-3/CAP-4 sem peças elétricas/aviônicas/flap; calibração do grupo Motor do usuário (preço, mão de obra, parada); provisionador remove peças que não se aplicam (pula a aeronave se as features não puderem ser lidas). `economy_targets.json` **restaurado** de propósito → `PartEconomyBalanceTests` (2) falha até a CALIBRA-F4. Só entra junto com ela.
2. **`feat/calibra-f4`** (parte da anterior; **parcial, não aprovada**): fator ÷ 4 (spec `docs/agents/specs/spec-CALIBRA-F4.md`). Ainda falham `PartEconomyBalanceTests` (2) e `Catalogo_Motor_CalibracaoDoUsuario`. O Gemini estourou o tempo tentando otimizar os percentuais com scripts (apagados). **Próximo passo:** revisar o diff; terminar a calibração de `repairCostPctOfAircraft` por grupo (o Claude define a regra — ex.: pct ÷ 4 e ajuste fino por grupo pela tabela do `PartEconomySimulator` — e o Gemini aplica em tarefa curta, sem simulação longa); depois merge das duas branches juntas.

## Decisões do usuário (2026-11-17)
- Iluminação = peças dentro do Elétrico; tudo halógena; MEL noturna só avisa. `electrical.external_lighting` removido.
- Colunas da tabela do usuário "Pode reparar?" e "Crítico em voo?" = NO-GO e walkaround (sem mudança). "× n" na mão de obra = n horas; "×" junto do preço = preço escala com o avião (mantido).
- **Fator de compressão ÷ 4** em toda vida de peça (horas e ciclos; base = catálogo de `83db087`), TBO (pistão 450 h, turbina 900 h) e inspeções: **12 h "Troca de óleo"**, **25 h "Inspeção anual"** (interdita), **125 h "Revisão de ignição"**; preços de inspeção/retífica ÷ 4. Calendário (meses) **não** comprimido; óleo pistão/turbina 3 meses. Calibração do óleo do usuário: `docs/agents/specs/calibracao-oleo-usuario.json`.
- Ícones das peças: após inspeção, até o próximo voo.
- Vida por calendário usa a data real (conta com o app fechado); calendário de jogo é ideia em aberto.

## Achados para o rebalanceamento das missões (próxima tarefa depois da CALIBRA-F4)
- Missões pagam muito acima do custo: no simulador o C152 fatura ~$1.988/h, margem 89 %, avião pago em ~34 h; 737 ~$37.600 por voo de 1 h. A taxa base fixa por missão ($220–5.500) e a tarifa por milha não dependem do avião → aviões leves rendem demais; progressão invertida (C152 se paga em dezenas de horas, 737 em milhares).
- Não há salário; seguro é mensal; estacionamento ainda não é cobrado.
- Plano: rodar `career` por avião na base com a CALIBRA-F4, o usuário define a margem-alvo por classe, depois recalibrar `MissionGenerator.CalculateReward` (taxa base e tarifa por milha por classe de avião) com um teste de margem por classe.

## O que o usuário faz até sábado (sem IA)
1. Tabelas de calibração dos demais grupos (formato da tabela do motor, vida real — o Claude aplica o ÷ 4).
2. Margem-alvo por classe (ou "horas para pagar o avião" desejadas) e referências reais de custo/receita por hora.
3. Conferir no POH: DA62 Vle (201 × 156 kt), King Air 350 Vne (245 × 263); variantes CTSL × CT, CAP 10 × CAP-4, DR400/100 × DR400.
4. Decisões: Vlo (extensão × menor entre extensão e recolhimento); auto-leme com −10 % XP?; calendário de jogo?; § 3 de `eventos-diversos.md`, `eventos-comandos.md`, `eventos-combustivel.md`, `simconnect-api.md`.
5. Abrir o app e conferir os chips do Hangar após uma inspeção.
6. Em casa (MSFS): Ctrl+E e caminhão chegam ao SimConnect; sinal do `PLANE PITCH DEGREES`; pausa pelo menu na nota; mapa elétrico (Developer Mode › Electrical System Debug) de C152, C172, Baron e TBM para o plano de iluminação F1. **Tempo acelerado:** voo a 4× e 16× para confirmar que horas, desgaste, inspeções, TBO e relógio de carreira contam o tempo simulado (`docs/plans/sensor-validation/ROTEIRO.md` › 8).

## Pendências antigas que precisam do simulador
- SimvarWatcher com as listas novas; SimVarProbe com `--filter "^(?!ATC |AI )"`; escrita de turbina, combustível, slew; teste de panes (`BREAKER_*_SET`, `PARTIAL PANEL *`, `FUELSYSTEM_*`, `HYDRAULIC_ACTUATOR_ACTIVE_SET`, trim disabled); `FlightSave`/`FlightLoad`; Flow API; índices elétricos reais por avião; voos em outros aviões; F4.2e, crash declarado, pista de terra.
