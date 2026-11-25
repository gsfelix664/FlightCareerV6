# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25 (2ª sessão), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.071/1.071. VOO-VENTO-INSTRUCAO mergeada (revisão em `docs/reviews/VOO-VENTO-INSTRUCAO-revisao.md`).

## Feito nesta sessão (não testado no MSFS)
- Vento de través/cauda com proa verdadeira (`PlaneHeadingTrue`, `PLANE HEADING DEGREES TRUE` no fim do `SimTelemetry`/`AddToDef`).
- REC grava vento (velocidade, direção) e proas verdadeira e magnética (`sensor_registry.json`).
- Instrução local pede 1 ou 2 TGL; faixa de altitude da área (instrução local e cheque prático) cobrada com folga de 500 ft acima da elevação da origem.
- Áreas de instrução do simulador: o SimConnect não expõe espaço aéreo (só aeroporto, waypoint, NDB, VOR). Decisões em `docs/plans/missoes-locais-rp.md` › 4.
- Conferido com o usuário: ícones do Hangar ok; vento do sim estava certo (METAR de São Luís: 20 kt, rajada 30), danos do 2º voo foram normais. O vento "sem vento" era o mapa da tela de voo livre.

## Voos de teste (C152, SBJS, instrução local)
1º voo: PR-NRI, 07:36–07:56, carreira antiga. 2º voo: PT-WSQ, 08:38–08:47, carreira nova, sim rate de até ×16 (0,43 h de voo).

| Item | Resultado |
|---|---|
| Quique na decolagem não conta como pouso | **funcionou** (2º voo: `QUIQUE_DECOLAGEM`, 0,9 s no ar, 197 fpm; um só pouso contado, sem dano) |
| Avião inicial com peças / Hangar com ícones | peças **funcionaram** (73 no banco); ícones no Hangar **não conferidos** com o usuário |
| Vento na blackbox | **funcionou**: `AMBIENT WIND` 064° verdadeiro, 12–22 kt no solo, até 32 kt no ar (bate com a estimativa do 1º voo). O usuário via 9 kt / "sem vento" na tela do sim: falta saber qual tela |
| Motivo do contrato não cumprido, texto no briefing, área no HUD, ícones do HUD | não conferido com o usuário |
| Contrato de instrução local | não cumprido nos dois voos (0 de 3 TGL, só pouso final); passou a 2,0 NM do centro da área, a 2.650–2.800 ft MSL (~1.700 ft AGL) |
| Escalas com motor ligado / toques no triângulo | não testado |
| STOL, voo de cheque, translado | não testado (fase 2; cheque e translado não saem em tier 4) |
| Quadro: SDCO/SBTF/PABE sem jatos de linha; SBFI/SBPS com | não testado |

Outros achados do 2º voo:
- RPM acima da faixa vermelha (2.550) por boa parte do voo, pico 2.707: 4,6 % de dano espalhado por 12 peças do motor (regra funcionando; foi o piloto).
- Pneus principais 100 → 96 % num voo, com pouso de 41 fpm: inspeções `CROSSWIND_LANDING_LOAD` (vento de través real) e `HIGH_ENERGY_BRAKING`. Conferir a calibração se repetir.
- A faixa de altitude do texto da instrução ("3.500 a 4.500 ft AGL") não é cobrada: o ponto da área só pede raio de 3 NM e no máximo 6.000 ft.
- Áreas de instrução são procedurais (`MissionGenerator`, 8–15 NM num setor, longe de outros aeródromos). Dados reais: o Little Navmap/Navigraph tem 1.375 áreas tipo `TR` (training) no mundo, ~800 na Europa e **nenhuma no Brasil**; a licença do Navigraph não permite redistribuir. Decisão do usuário: híbrido (área real quando houver, de fonte com licença compatível, ex. openAIP, e procedural no resto).

## Corrigido em 2026-11-25 (não testado no MSFS)
- Quique na decolagem não é pouso: contato até 8 s depois de sair do chão, sem pouso antes; dano só se duro (≥ 400 fpm). Evento `QUIQUE_DECOLAGEM` na blackbox.
- Avião inicial nasce com peças (Hangar com ícones desde o início).
- Banco apagado a pedido do usuário (carreira de testes recomeça; cópia em `db-backup/flightcareer.db.bak-20261125` (com -shm e -wal), na pasta de saída do UI).

## Bugs em aberto
- Nenhum novo. Conferir no próximo voo: coluna `nav-heading-true` e vento no CSV do REC; contrato de instrução local cumprido passando na faixa.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa no PC do escritório (sem simulador) — escolha uma por sessão
1. **HUD com o `StatusText` dos rastreadores** ("Escala 2/4", "Passo 3/4", e a faixa da área de instrução atingida ou não). Recomendado primeiro: ajuda os próximos testes no MSFS. Modelo: **Opus** (spec + revisão de `FlightMonitor`/`DebugOverlayViewModel`), Gemini implementa.
2. **Pacote de correções de missões:** `check_flight` paga sempre por 30 NM; `pinga_pinga` passa de 400 NM no total; paraquedismo com 25 NM fixos. Lógica pura com teste e snapshot `missions_golden.json`. Modelo: **Sonnet** para a spec curta, Gemini implementa.
3. **Áreas de instrução reais (híbrido):** pesquisar licença e formato do openAIP (áreas tipo training/TRA no Brasil e no mundo), propor importação em tempo de desenvolvimento para `Data/places/` e fallback procedural. Modelo: **Opus** (pesquisa e plano em `docs/plans/missoes-locais-rp.md`).
4. **Próximo lote da E2C** (13 tipos desligados, ex. `instrucao_ifr`, `demo_flight`, `pulverizacao`). Modelo: **Opus** para a spec do lote.
5. `board-report --coverage` mais rápido (rotina, **Sonnet** + Gemini).
- Antes de qualquer uma, o usuário pode resolver as decisões abertas abaixo (perguntas rápidas, **Sonnet**).
- Não dá no escritório: trem travado no `ferry_permit` (SimVar nova no `SimTelemetry`, precisa conferir no sim) e os itens "Precisam do MSFS".

## Próxima tarefa no PC de casa (com MSFS)
Mais testes de missões no MSFS (Sonnet basta para ler logs): instrução local na faixa com 1–2 TGL; depois triângulo VFR com toque e arremetida (SBJS → SNPC → SSFL → SBJS, fase 1, se ainda estiver no quadro). Para STOL, cheque e translado, o usuário decide se libera a fase 2 no banco; cheque e translado pedem ainda um aeroporto tier 1–3. Conferir o quadro (SDCO/SBTF/PABE, SBFI/SBPS) e as telas corrigidas.

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
