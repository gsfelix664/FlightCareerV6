# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (21ª sessão, casa), Claude. Base `feat/component-health-system`. POUSO-DANO (tarefa 2 do plano) feita pelo Gemini, aprovada e no merge.

## Estado
- **POUSO-DANO feito** (C1, D1, D5, D6, D10, D11 e B4): pouso duro pode quebrar trem/pneu (`LandingBreakModel`, calibração em `component_catalog.json` › `landing`: 1 %→5 % de 600 a 850 fpm, 15 %→30 % de 850 a 900; pneu ×1,5; inclinação ≥ 10° ×1,5; fora de pista ×2; desgaste até ×2; teto 60 %). Peça quebrada interdita. Pouso fora de pista: dano nas peças, missão cancelada, XP 50 %, sem dinheiro, relógio avança. Inspeções viram pane "Inspeção pendente" (só as 5 de toque interditam). `isGrounding` do `wear_rules.json` agora é lido. Blackbox: "POUSO_FORCADO" e G do toque. Revisão: `docs/reviews/POUSO-DANO-revisao.md`.
- **Não testado no MSFS nem com o app aberto.** Atenção: com `isGrounding` lido e inspeções virando pane, o Hangar vai mostrar mais itens pendentes depois de voos com excedências.
- Resto do plano `docs/plans/correcoes-voo-forcado-sdco.md` (A, B1–B3, D7–D9, E1, E3–E5, F) não implementado. Tarefa 6 (RESGATE) cancelada pela decisão B4.
- PT-HRY: segue interditado até "Reparar tudo" (peças da pane antiga). Mapa unificado e cartão de objetivo (OBJ-CARD-1/2) seguem sem teste no MSFS (`TESTES-PENDENTES.md`).
- `install-global.sh` rodado nesta máquina (casa) em 2026-11-26.

## Próximo passo
- No app/MSFS: um pouso duro (≈700 fpm) e um fora de pista; conferir alerta de peça quebrada, Hangar (peça quebrada, "Inspeção pendente"), missão sumida do quadro, XP pela metade, blackbox "POUSO FORÇADO".
- **Sessão Sonnet: tarefa 3, LIMPA-MAPA** (A1, A2: remover waypoints/LIMPAR do mapa e "reporte VFR"; não mexer em `OverflyWaypoints`).
- **Sessão Opus: tarefa 9, EXCEDENCIAS** (D7 slew/solo, D8 Vfe C152 = 85 kt em todos os dentes — decidido, D9 histerese do RPM). Depois dela, rever quais inspeções podem interditar (`InspectionDefectPolicy`).
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: AIRSIM-2 (spec Opus; só fecha com MSFS), HUD em modo tablet (quando pedido), 8 tipos da E2C.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0` (não a metade exata).

## Decisões abertas do usuário
- F4: tirar também o selo "- N%", a nota do voo e o "% MTOW"? Sugestão: manter.
- Altitude da área genérica e variedade de destinos na fase 1 (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
