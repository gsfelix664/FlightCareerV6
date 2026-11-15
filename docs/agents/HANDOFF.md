# Passagem de sessão (Claude) — 2026-11-15

Leia junto: `AGENTS.md` (§6 regras, §7 estado), `docs/plans/pecas-desgaste-falhas.md` (diário no fim), `docs/CHANGELOG.md` (últimas entradas), `docs/agents/ORQUESTRACAO.md`.

## Estado
- Base `feat/component-health-system` em dia com o GitHub (push feito a cada merge). Worktrees do Gemini: `../FLIGHTCAREERV6-gemini` e `../FLIGHTCAREERV6-gemini2` (livres; use `FC_GEMINI_WORKTREE` para rodar duas tarefas em paralelo).
- **Regra do usuário:** tudo vai para o Gemini, inclusive arquivos quentes, com spec passo a passo + revisão linha a linha do Claude (`CLAUDE.md`). Push ao fim de cada trabalho.
- Concluído e aprovado hoje: F1.4c (C152), F2.0/F2.0a/F2.0b (catálogo de limites por aeronave, POH com evidência literal), F2.1/F2.1b/F2.1c (148 peças por setor, aprovadas), F2.2 + expansão (45 regras), F2.3 (motor de desgaste por peça, migração, preço com teto, custo/hora realista nas 46 aeronaves), F4.2c (pane do wear and tear pelo `IS FAILED`, testada no MSFS), leitura rápida + crash pelo addon (testado), F4.2d (crash do sim no solo), F4.2e (alternador pelo barramento com chave ON; flap pela superfície com alavanca livre — **não testado no MSFS pelos caminhos reais**), botões de teste de falhas no HUD, correção da falha pós-manutenção em avião novo.
- Banco do usuário foi resetado (backup no scratchpad da sessão antiga). Carreira nova: C152 PT-DEZ em perda total após os testes.

## Próximos passos (na ordem)
1. **F2.4 — peças no voo** (Gemini, spec detalhada; arquivos quentes `FlightMonitor`, `ComponentFlightSession`): `PartFlightSession` consome `SensorFrame` + `TouchdownEvent` + eventos de partida/ciclos, aplica `PartWearEngine`, salva peças (60 s, fim, cancelamento, perda total), `PartComponentBridge` passa a alimentar as barras e o `WearEngine` antigo para nas aeronaves com peças; `ApplyNativeFailed` na pane nativa. Testes de integração com frames sintéticos.
2. **Spike dos sensores elétricos** (Gemini, em paralelo): no C172 G1000, `AVIONICS MASTER SWITCH` ficou 1, `ELECTRICAL BATTERY VOLTAGE` fixo em 25,137 e `ELECTRICAL GENALT BUS VOLTAGE/AMPS/LOAD:1` em 0 (`docs/plans/sensor-validation/sensores-novos-C172.md`). Achar no SDK 2024 os nomes/índices certos (barramentos/circuitos) e propor teste. Leme (`RUDDER POSITION`) não foi testado.
3. Fase 3 (walkaround, revisões que só inspecionam, troca de peças com tempo de hangar, Hangar por setor — decisão de UI no plano › Fase 5).
4. Ideias registradas para depois em `docs/plans/pecas-sugestoes-usuario.md` (MEL, inspeção de 24 meses, cascata de energia, TBO comercial, top overhaul, vazamento de óleo em cascata, cabine/O₂).

## Pendências de teste no MSFS
F4.2e (pane real de alternador e flap), leme, pneu/trem com dano ligado e pista de terra (C152), crash declarado pelo sim (o MSFS 2024 não levantou `CRASH FLAG` em nenhum teste; o addon pega pelo `TouchdownDetector`).
