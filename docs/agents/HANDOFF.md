# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-27 (26ª sessão, casa), Claude. Base `feat/component-health-system`. SEM-PORCENTO (tarefa 8 do plano) feita pelo Gemini, aprovada com ajustes do revisor e no merge.

## Estado
- **SEM-PORCENTO feito** (F1–F3): condição em palavras (`HealthWords`: Íntegro/Bom/Atenção/Crítico/Quebrado) no Hangar, avisos de voo, panes e mercado. Build ok, 1.301/1.301. Revisão: `docs/reviews/SEM-PORCENTO-revisao.md`. **Não conferido com o app aberto nem no MSFS.**
- MAPA-ESCALAS, BRIEF-NAV, HANGAR-AVISOS, LIMPA-MAPA e POUSO-DANO feitos e não conferidos com o app aberto (revisões em `docs/reviews/`).
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (D7–D9, E5) não implementado.
- PT-HRY: segue interditado até "Reparar tudo". Mapa unificado e OBJ-CARD-1/2 sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- Usuário vai fazer um voo de teste sem roteiro e pedir a análise do log em sessão nova (Opus).
- **Sessão Opus: NAV-IDA** (navegação VFR só de ida, peso 10, por nível; decisões em `docs/plans/navegacao-instrucao.md`).
- No app/MSFS: textos do Hangar e do mercado sem %; escalas nos três mapas; pouso duro e fora de pista; preços e avisos do Hangar.
- **Sessão Opus: tarefa 9, EXCEDENCIAS** (D7 slew/solo, D8 Vfe C152 = 85 kt em todos os dentes, decidido, D9 histerese do RPM; depois rever `InspectionDefectPolicy`).
- Depois: tarefa 10, HANGAR-REDESENHO (spec Opus; inclui E5). `HangarViewModel` nunca em paralelo.
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: AIRSIM-2 (spec Opus; só fecha com MSFS), HUD em modo tablet (quando pedido), 8 tipos da E2C.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0` (não a metade exata).
- Mercado: textos fixos de pneus e aviônicos podem contradizer o estado mostrado.

## Decisões abertas do usuário
- Altitude da área genérica (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
