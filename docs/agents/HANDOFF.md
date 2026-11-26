# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (20ª sessão, casa), Claude. Base `feat/component-health-system`. PANE-PECAS (tarefa 1 do plano) feita pelo Gemini, aprovada e no merge.

## Estado
- **PANE-PECAS feito** (D2, D3, D4, E2): pane nativa quebra 1 peça (`NativeFailurePartMap`); reparar pane troca as peças ligadas (`DefectPartLink`), um preço só; trocar peça resolve a pane; peça quebrada que bloqueia voo interdita (`PartAirworthiness`, status "INTERDITADA (PEÇA)" e `FlightMonitor.StartMonitoring`). Revisão: `docs/reviews/PANE-PECAS-revisao.md`. Não testado no MSFS nem com o app aberto.
- PT-HRY no banco local ainda tem as 11 peças quebradas da pane antiga: agora aparece interditado até "Reparar tudo" (esperado).
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (A, B, C1, D1, D5–D11, E1, E3–E5, F) **não implementado**.
- Mapa unificado e cartão de objetivo (OBJ-CARD-1/2) seguem sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- **Sessão Opus: tarefa 2, POUSO-DANO** (C1, D1, D5, D6, D10, D11): ler o plano, spec com as chances de quebra de trem/pneu para o usuário decidir, delegar.
- No app (casa): conferir no Hangar do PT-HRY a interdição, "Reparar tudo" (preço do botão = cobrado antes da dificuldade) e que as panes somem.
- Alternativa rápida (Sonnet): tarefa 3, LIMPA-MAPA (remover waypoints/LIMPAR do mapa e "reporte VFR"). Não mexer em `OverflyWaypoints`.
- PC de casa: voo de conferência do mapa, cartão, Ctrl+Alt+O, posições salvas e cores.
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: **AIRSIM-2** (spec Opus: `UI/Services/SimAirportImporter`, versão do sim no `OnRecvOpen`, lotes, `SimAirportSyncStore`, DI, luzes de aproximação; só fecha com MSFS), HUD em modo tablet (quando pedido), 8 tipos da E2C (`catalogo-missoes.md`).

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`, `ProcessOilLeak`/`TransmitOilLeak`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Casa: conferir com `/context` se as skills desativadas sumiram; desligar no app conectores/recursos sem uso (usuário).

## Decisões abertas do usuário
- Plano novo: missão após pouso forçado (resgate ou falha); % fora da condição (desconto, nota, MTOW); Vfe do C152.
- Altitude da área genérica e variedade de destinos na fase 1 (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário: não analisar sem pedido.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git; `aeroportos-tiers.md`).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC). REC tem `sim_rate` (horas do Hangar = tempo simulado).
