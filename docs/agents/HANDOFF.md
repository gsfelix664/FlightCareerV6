# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25, Claude, PC de casa (com MSFS e Little Navmap). Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.063/1.063. Correções commitadas e mergeadas na base.

## Voo de teste (instrução local SBJS, C152 PR-NRI, 07:36–07:56)
| Item | Resultado |
|---|---|
| Escalas com motor ligado | não testado |
| Toques no triângulo | não testado |
| Pousos curtos STOL | não testado (fase 2) |
| Rumos do voo de cheque | não testado (fase 2; não sai em tier 4) |
| Teto do translado | não testado (fase 2; não sai em tier 4) |
| Quadro: SDCO/SBTF/PABE sem jatos de linha; SBFI/SBPS com | não testado |

Fora da lista:
- **$0 com qualidade 100 %:** correto pela regra; 0 de 3 TGL (no ar de 07:42:41 até o pouso final às 07:56:30). Qualidade não entra no pagamento. A posição passou a 2,0 NM do centro da área (raio 3 NM). A tela não dizia o motivo: corrigido.
- **Decolagem difícil:** sem pane. Vento real, reestimado sem usar a proa (GS gravada × trilha verdadeira, resíduo 4–6 kt): ~20 kt de ~070° verdadeiro de 0 a 1.500 ft, ~30 kt acima. Saída do chão (proa ~349° mag.) e pouso com cauda de ~6–15 kt e través forte. O usuário viu 9 kt / "sem vento" na tela do sim: a blackbox agora grava `WindDirDeg;WindKt` (`AMBIENT WIND *`) para comparar no próximo voo.
- **Área de instrução no HUD, texto do contrato no briefing, textos cortados de célula/motor/pneus:** corrigidos; **não conferidos na tela** (o app estava aberto; recompilar e abrir).

## Corrigido em 2026-11-25 (não testado no MSFS)
- Quique na decolagem não é pouso: contato até 8 s depois de sair do chão, sem pouso antes; dano só se duro (≥ 400 fpm). Evento `QUIQUE_DECOLAGEM` na blackbox.
- Avião inicial nasce com peças (Hangar com ícones desde o início).
- Banco apagado a pedido do usuário (carreira de testes recomeça; cópia em `db-backup/flightcareer.db.bak-20261125` (com -shm e -wal), na pasta de saída do UI).

## Bugs em aberto
1. **Vento de través errado:** `SimTelemetry.CrosswindComponentKnots`/`TailwindComponentKnots` subtraem proa **magnética** (`PLANE HEADING DEGREES MAGNETIC`) de direção do vento **verdadeira** (`AMBIENT WIND DIRECTION`); em SBJS o erro é de ~22°. Correção: acrescentar `PLANE HEADING DEGREES TRUE` no fim do `SimTelemetry` e do `AddToDef` (arquivos quentes). Afeta bônus STOL e XP de vento cruzado.
2. O REC (sensor-logs) não grava vento nem proa.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa (sessão nova, PC de casa)
1. Criar a carreira nova (base SBJS, C152) e conferir o Hangar com ícones. Continuar o voo de teste: triângulo VFR com toque e arremetida (SBJS → SNPC → SSFL → SBJS, fase 1, se ainda estiver no quadro). Para STOL, cheque e translado, o usuário decide se libera a fase 2 no banco; cheque e translado pedem ainda um aeroporto tier 1–3. Conferir o quadro (SDCO/SBTF/PABE, SBFI/SBPS) e as telas corrigidas.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`; no REC de hoje a SimVar ficou em ~99–100 sem pane); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Missões (dá para fazer no escritório):** HUD com o `StatusText` dos rastreadores da E2C-1 ("Escala 2/4", "Passo 3/4"); `check_flight` paga sempre por 30 NM; `pinga_pinga` passa de 400 NM no total; paraquedismo com 25 NM fixos; trem travado no `ferry_permit` (precisa de SimVar do trem no `SimTelemetry`); próximos lotes da E2C (13 tipos ainda desligados: `instrucao_ifr`, `adaptacao_noturna`, `demo_flight`, `saude_familia`, `pulverizacao`, `reboque_planador`, `publicidade_aerea`, `patrulha_vigilancia`, `inspecao_linhas`, `monitoramento_trafego`, `sar`, `calibracao_auxilios`, `evacuacao_por_do_sol`); `board-report --coverage` mais rápido.
- **Decisões abertas do usuário:** restringir missão militar (CAN) a origens militares (saiu uma de SBJS, pista civil de terra); bots agressivos do Médio falem 100 % no dia 1095 após os aeroportos novos (era 67 %; só 3 bots); isolamento maior nos perfis (muda o sorteio de missões remotas); peso dos destinos de saúde (84–86 % ao preferido; baixar para ~100 se rígido); 7 melhorias de `docs/plans/missoes-locais-rp.md` › 3; patrimônio do Médio no 3º ano (~16 mi) × meta ≤ 20×; bots do Médio parados na classe G do dia 540 ao 1095.

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos (falta de memória deu `Resource temporarily unavailable` no `agy`).
- Com o app aberto, `dotnet build` da solução falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`; os testes rode sem `OutDir` (8 testes de definições SimConnect procuram os fontes a partir da saída).
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`); no checkout com `autocrlf` o CRLF soma ~1 byte por linha.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
