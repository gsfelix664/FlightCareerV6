# Passagem de sessão (Claude) — 2026-11-17 (manhã, PC do escritório)

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
  4. **ECON-SIM** — `FlightCareer.Replay career` (bots × missões reais × custos do jogo → `summary.md`). Funciona (C152, 3 dias, 23 s), mas a rodada grande (8 aviões × 5 bots × 90 dias) **não terminou em ~1 h**: rodar por avião ou otimizar antes.
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
6. Em casa (MSFS): Ctrl+E e caminhão chegam ao SimConnect; sinal do `PLANE PITCH DEGREES`; pausa pelo menu na nota; mapa elétrico (Developer Mode › Electrical System Debug) de C152, C172, Baron e TBM para o plano de iluminação F1.

## Pendências antigas que precisam do simulador
- SimvarWatcher com as listas novas; SimVarProbe com `--filter "^(?!ATC |AI )"`; escrita de turbina, combustível, slew; teste de panes (`BREAKER_*_SET`, `PARTIAL PANEL *`, `FUELSYSTEM_*`, `HYDRAULIC_ACTUATOR_ACTIVE_SET`, trim disabled); `FlightSave`/`FlightLoad`; Flow API; índices elétricos reais por avião; voos em outros aviões; F4.2e, crash declarado, pista de terra.
