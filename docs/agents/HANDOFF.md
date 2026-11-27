# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-27 (25ª sessão, casa), Claude. Base `feat/component-health-system`. MAPA-ESCALAS (tarefa 5 do plano) feita pelo Gemini, aprovada com ajuste do revisor e no merge.

## Estado
- **MAPA-ESCALAS feito** (B2): escalas numeradas, pista STOL e linha do contrato (roxa) no briefing, quadro e HUD. Build ok, 1.283/1.283. Revisão: `docs/reviews/MAPA-ESCALAS-revisao.md`. **Não conferido com o app aberto nem no MSFS.**
- BRIEF-NAV, HANGAR-AVISOS, LIMPA-MAPA e POUSO-DANO feitos e não conferidos com o app aberto (revisões em `docs/reviews/`).
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (D7–D9, E5, F) não implementado.
- PT-HRY: segue interditado até "Reparar tudo". Mapa unificado e OBJ-CARD-1/2 sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- No app/MSFS: navegação com escalas nos três mapas (escalas numeradas, linha origem → reporte → escalas → origem, sem "(TGL)"); trocar de aeroporto no quadro e ver de onde parte a linha; pouso duro e fora de pista; preços e avisos do Hangar.
- **Sessão Sonnet: tarefa 8, SEM-PORCENTO** (F1–F3). `HangarViewModel` nunca em paralelo.
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
