# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25, Claude, PC de casa (com MSFS e Little Navmap). Base `feat/component-health-system`, igual ao GitHub. Build 0 erros; `dotnet test` 1.058/1.058.

## Feito na última sessão
- Jatos de linha em aeroporto só com `linha_auto`: tier 1, ou tier 2 com 100+ pontos (`docs/plans/tiers-por-movimento.md`, decisões de 2026-11-25).
- 115 aeroportos com voo regular na lista curada; perfis regenerados com o tier do jogo; regras de tier na versão 3.
- `career-stab` do Médio sem alertas (detalhes no CHANGELOG).

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa (sessão nova, PC de casa)
1. Voo de teste no MSFS (o usuário voa): escalas com motor ligado (o voo termina com motor cortado e parado), toques no triângulo, pousos curtos STOL, rumos do voo de cheque, teto do translado. Conferir também no quadro: SDCO/SBTF/PABE sem jatos de linha; SBFI/SBPS com.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Missões (dá para fazer no escritório):** HUD com o `StatusText` dos rastreadores da E2C-1 ("Escala 2/4", "Passo 3/4"); `check_flight` paga sempre por 30 NM; `pinga_pinga` passa de 400 NM no total; paraquedismo com 25 NM fixos; trem travado no `ferry_permit` (precisa de SimVar do trem no `SimTelemetry`); próximos lotes da E2C (13 tipos ainda desligados: `instrucao_ifr`, `adaptacao_noturna`, `demo_flight`, `saude_familia`, `pulverizacao`, `reboque_planador`, `publicidade_aerea`, `patrulha_vigilancia`, `inspecao_linhas`, `monitoramento_trafego`, `sar`, `calibracao_auxilios`, `evacuacao_por_do_sol`); `board-report --coverage` mais rápido.
- **Decisões abertas do usuário:** bots agressivos do Médio falem 100 % no dia 1095 após os aeroportos novos (era 67 %; só 3 bots); isolamento maior nos perfis (muda o sorteio de missões remotas); peso dos destinos de saúde (84–86 % ao preferido; baixar para ~100 se rígido); 7 melhorias de `docs/plans/missoes-locais-rp.md` › 3; patrimônio do Médio no 3º ano (~16 mi) × meta ≤ 20×; bots do Médio parados na classe G do dia 540 ao 1095.

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos (falta de memória deu `Resource temporarily unavailable` no `agy`).
- Com o app aberto, `dotnet build` falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`.
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`); no checkout com `autocrlf` o CRLF soma ~1 byte por linha.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/`; REC em `%LocalAppData%\FlightCareer\sensor-logs\`.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
