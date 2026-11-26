# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (19ª sessão, casa), Claude. Base `feat/component-health-system`. Só documentação: análise do voo com pouso forçado e plano de correção.

## Estado
- Pouso forçado (SDCO → perto de SNZG, C152): 733 fpm, flap 10° até 147 kt, motor falhou em voo. Uma pane quebrou 11 peças; pagar as panes não as conserta; penalidade fora de pista não chega às peças; inspeções só no log.
- Briefing trata navegação como TGL; escalas não aparecem em nenhum mapa. Avisos do Hangar ainda mostram %.
- Tudo em `docs/plans/correcoes-voo-forcado-sdco.md` (itens A–F, tabela de tarefas e modelo). **Nada implementado.**
- Mapa unificado e cartão de objetivo (OBJ-CARD-1/2) seguem sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- **Sessão Opus: tarefa 1, PANE-PECAS** (D2, D3, D4, E2): ler o plano, escrever a spec, delegar ao Gemini.
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
