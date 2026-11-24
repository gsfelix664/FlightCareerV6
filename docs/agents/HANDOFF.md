# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-24, Claude, PC do escritório (4 GB de RAM, sem MSFS e sem Little Navmap). Base `feat/component-health-system`, igual ao GitHub. Build 0 erros; `dotnet test` 1.057/1.057.

## Feito na última sessão
- E2A-L: camada antiga de missões removida (`MissionRules`, geradores antigos).
- Jato de linha só em hub (`linha` curada ou `linha_auto:hub` = `linha_auto` + tier ≤ 2) e destino forte nas missões de saúde (peso e teto 150; `medevac_remoto` vai da pista remota para cidade tier 2–3).
- E2C-1: `pinga_pinga`, `instrucao_stol`, `check_flight`, `ferry_permit` ligados; `navegacao_vfr` como triângulo; paraquedismo pelos perfis do catálogo. E2C-1b: `navegacao_vfr` com 3 variantes (triângulo 50 %, ida e volta 25 %, triângulo com toques 25 %).
- Regra de uma tarefa por sessão e enxugamento do `AGENTS.md` › 7.

## Próximo passo recomendado (PC de casa, com MSFS e Little Navmap)
1. Rodar `dotnet run --project src/FlightCareer.Replay -- tier-report` e `board-report` com o banco `little_navmap_msfs24.sqlite`; o usuário confere. Ver se SDCO, PABE e SBTF ficam tier 2 (se sim, considerar `LinhaAutoHubMaxTier = 1` em `MissionBoardCore`). Depois: `tools/places/airport_profiles.py` com o tier novo e `career-stab` para recalibrar a economia.
2. Voo de teste das missões novas no MSFS: escalas com motor ligado (o voo termina com motor cortado e parado), toques no triângulo, pousos curtos STOL, rumos do voo de teste, teto do translado.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Missões (dá para fazer no escritório):** HUD com o `StatusText` dos rastreadores da E2C-1 ("Escala 2/4", "Passo 3/4"); `check_flight` paga sempre por 30 NM; `pinga_pinga` passa de 400 NM no total; paraquedismo com 25 NM fixos; trem travado no `ferry_permit` (precisa de SimVar do trem no `SimTelemetry`); próximos lotes da E2C (13 tipos ainda desligados: `instrucao_ifr`, `adaptacao_noturna`, `demo_flight`, `saude_familia`, `pulverizacao`, `reboque_planador`, `publicidade_aerea`, `patrulha_vigilancia`, `inspecao_linhas`, `monitoramento_trafego`, `sar`, `calibracao_auxilios`, `evacuacao_por_do_sol`); `board-report --coverage` mais rápido.
- **Decisões abertas do usuário:** peso dos destinos de saúde (84–86 % ao preferido; baixar para ~100 se rígido); 7 melhorias de `docs/plans/missoes-locais-rp.md` › 3; patrimônio do Médio no 3º ano (~16 mi) × meta ≤ 20×; bots do Médio parados na classe G do dia 540 ao 1095.

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos (falta de memória deu `Resource temporarily unavailable` no `agy`).
- Com o app aberto, `dotnet build` falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/`; REC em `%LocalAppData%\FlightCareer\sensor-logs\`.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
