# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (22ª sessão, casa), Claude. Base `feat/component-health-system`. LIMPA-MAPA (tarefa 3 do plano) feita pelo Gemini, aprovada e no merge.

## Estado
- **LIMPA-MAPA feito** (A1, A2): waypoints manuais e botão LIMPAR removidos do mapa; "ponto de reportagem" → "ponto de reporte" (missões já geradas guardam o texto antigo). Build ok, 1.245/1.245. Revisão: `docs/reviews/LIMPA-MAPA-revisao.md`. Mapas não conferidos com o app aberto.
- POUSO-DANO feito (detalhe em `docs/reviews/POUSO-DANO-revisao.md`): pouso duro pode quebrar trem/pneu; fora de pista cancela a missão (XP 50 %, sem dinheiro); inspeções viram pane "Inspeção pendente". **Não testado no MSFS nem com o app aberto**; o Hangar vai mostrar mais itens pendentes após excedências.
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (A3, B1–B3, D7–D9, E1, E3–E5, F) não implementado.
- PT-HRY: segue interditado até "Reparar tudo". Mapa unificado e OBJ-CARD-1/2 sem teste no MSFS (`TESTES-PENDENTES.md`).

## Próximo passo
- No app/MSFS: pouso duro (≈700 fpm) e fora de pista (alerta de peça quebrada, Hangar, missão sumida, XP pela metade, blackbox "POUSO FORÇADO"); conferir os mapas sem o quadro de waypoints.
- **Sessão Sonnet: tarefa 7, HANGAR-AVISOS** (E1, E3, E4; texto do desconto pedido em E3). `HangarViewModel` nunca em paralelo.
- **Sessão Opus: tarefa 4, BRIEF-NAV** (A3, B1, B3; libera a 5, MAPA-ESCALAS) ou **tarefa 9, EXCEDENCIAS** (D7 slew/solo, D8 Vfe C152 = 85 kt em todos os dentes, decidido, D9 histerese do RPM; depois rever `InspectionDefectPolicy`).
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: AIRSIM-2 (spec Opus; só fecha com MSFS), HUD em modo tablet (quando pedido), 8 tipos da E2C.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0` (não a metade exata).

## Decisões abertas do usuário
- F4 decidida (2026-11-26): manter os % que não são condição; tarefa 8 só F1–F3.
- Altitude da área genérica e variedade de destinos na fase 1 (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
