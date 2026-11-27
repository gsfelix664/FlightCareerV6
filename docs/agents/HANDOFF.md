# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-27 (24ª sessão, casa), Claude. Base `feat/component-health-system`. BRIEF-NAV (tarefa 4 do plano) feita pelo Gemini, aprovada e no merge.

## Estado
- **BRIEF-NAV feito** (A3, B1, B3): `Domain/Missions/BriefingLegPlanner` monta as pernas do briefing; TGL só com `RequiredTouchAndGoes > 0`; voo local aparece como "(LOCAL)"; escalas do contrato viram pernas reais ("Pouso completo"/"Toque e arremetida em X (escala do contrato)"), pagamento só na última perna; mapa enquadra rota + contrato juntos (`ApplyCombinedFraming`). Build ok, 1.277/1.277. Revisão: `docs/reviews/BRIEF-NAV-revisao.md`. **Não conferido com o app aberto nem no MSFS.**
- HANGAR-AVISOS, LIMPA-MAPA e POUSO-DANO feitos e não conferidos com o app aberto (detalhes nas revisões em `docs/reviews/`).
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (B2, D7–D9, E5, F) não implementado.
- PT-HRY: segue interditado até "Reparar tudo". Mapa unificado e OBJ-CARD-1/2 sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- No app/MSFS: briefing de uma navegação com escalas (SDCO → escalas → SDCO: sem "(TGL)", pernas com escala, ponto de reporte dentro do quadro do mapa); pouso duro e fora de pista; preços e avisos do Hangar.
- **Sessão Sonnet: tarefa 5, MAPA-ESCALAS** (B2, agora liberada) ou **tarefa 8, SEM-PORCENTO** (F1–F3). `HangarViewModel` nunca em paralelo.
- **Sessão Opus: tarefa 9, EXCEDENCIAS** (D7 slew/solo, D8 Vfe C152 = 85 kt em todos os dentes, decidido, D9 histerese do RPM; depois rever `InspectionDefectPolicy`).
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: AIRSIM-2 (spec Opus; só fecha com MSFS), HUD em modo tablet (quando pedido), 8 tipos da E2C.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0` (não a metade exata).

## Decisões abertas do usuário
- Altitude da área genérica e variedade de destinos na fase 1 (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
