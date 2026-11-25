# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25 (6ª sessão, escritório), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.118/1.118. E2C-2 mergeada (revisão em `docs/reviews/E2C-2-revisao.md`).

## Feito nesta sessão (não testado no MSFS)
- Lote E2C-2 ligado: `patrulha_vigilancia` (setores em qualquer ordem, ≤ 1.500 ft acima da origem, ≤ 130 kt), `inspecao_linhas` (traçado procedural na ordem, raio 0,5 NM, ≤ 1.000 ft, desvio lateral no HUD), `monitoramento_trafego` (órbita de 1,5 NM por 10–20 min simulados, 1.000–2.500 ft), `sar` (quadrado crescente ou rota paralela, alvo escondido no padrão, achado a ≤ 0,3 NM) e `saude_familia` (ida e volta com parada completa no destino isolado). Rastreador novo `AerialWorkTracker`. Decisões em `docs/plans/catalogo-missoes.md` › 11.
- Plano futuro anotado a pedido do usuário: HUD em modo tablet horizontal, com briefing, início do voo e acesso rápido a abastecimento, seguro e peças (`docs/plans/hud-modo-tablet.md`).

## Conferir no próximo voo (PC de casa)
- Das sessões de hoje: instrução local saindo de SDCO/SBKP/SBMT com a área real do DECEA (nome no texto, ponto e faixa); coluna `nav-heading-true` e vento no CSV do REC; contrato de instrução local cumprido passando na faixa; linha "CONTRATO" do HUD (texto, quebra de linha, atualização); distâncias sorteadas do cheque e do paraquedismo.
- Tipos novos da E2C-2: linha do HUD de cada modo, marcadores no mapa (SAR sem o alvo), conclusão e motivo de falha. Atenção ao teto "acima do solo" em terreno mais alto que a origem (é relativo à elevação da origem).
- Do teste anterior (C152, SBJS): triângulo VFR com toque e arremetida, escalas com motor ligado, STOL, cheque e translado (fase 2) e o quadro de SDCO/SBTF/PABE × SBFI/SBPS ainda não testados. Pneus caindo 4 % num pouso de 41 fpm com través real: conferir a calibração se repetir.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa no PC do escritório (sem simulador) — escolha uma por sessão
1. `board-report --coverage` mais rápido (rotina, **Sonnet** + Gemini).
2. Decisões abertas abaixo (perguntas rápidas, **Sonnet**).
3. HUD em modo tablet: spec (**Opus**), quando o usuário quiser começar.
- Os 8 tipos da E2C que sobraram precisam de SimVar a conferir no MSFS ou de dados: `adaptacao_noturna`, `evacuacao_por_do_sol` e o pôr do sol da `saude_familia` (hora/sol e luzes), `pulverizacao` (AGL real), `demo_flight` (rolagem de decolagem de referência), `instrucao_ifr`, `calibracao_auxilios` (geometria de aproximação, auxílio sintonizado), `reboque_planador`, `publicidade_aerea` (reboque nativo). Um lote que acrescente `PLANE ALT ABOVE GROUND` e as SimVars de hora/sol pode ser especificado no escritório, mas só fecha com o teste no sim.
- Não dá no escritório: trem travado no `ferry_permit` (SimVar nova no `SimTelemetry`) e os itens "Precisam do MSFS".

## Próxima tarefa no PC de casa (com MSFS)
Testes de missões (Sonnet basta para ler logs): itens de "Conferir no próximo voo". Para STOL, cheque, translado e os tipos da E2C-2 (fase 2–3), o usuário decide se libera a fase no banco; cheque e translado pedem ainda um aeroporto tier 1–3.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Decisões abertas do usuário:** restringir missão militar (CAN) a origens militares (saiu uma de SBJS, pista civil de terra); bots agressivos do Médio falem 100 % no dia 1095 após os aeroportos novos (era 67 %; só 3 bots); isolamento maior nos perfis (muda o sorteio de missões remotas); peso dos destinos de saúde (84–86 % ao preferido; baixar para ~100 se rígido); 7 melhorias de `docs/plans/missoes-locais-rp.md` › 3; patrimônio do Médio no 3º ano (~16 mi) × meta ≤ 20×; bots do Médio parados na classe G do dia 540 ao 1095; `saude_familia` pode abastecer no destino (`RequireNoRefuel = false`).

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos.
- Com o app aberto, `dotnet build` da solução falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`; os testes rode sem `OutDir`.
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`).
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
- Banco de testes apagado em 2026-11-25; cópia em `db-backup/flightcareer.db.bak-20261125` na pasta de saída do UI.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
