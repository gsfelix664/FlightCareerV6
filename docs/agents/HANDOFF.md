# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25 (7ª sessão, escritório), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.120/1.120. BR-PERF mergeada (revisão em `docs/reviews/BR-PERF-revisao.md`).

## Feito nesta sessão
- `board-report --coverage` cerca de 3× mais rápido nos quadros (10 aeroportos × 10 quadros: total 64 s → 31 s; ~14 s são o seed do banco), com resultados idênticos aos do código antigo. Agora a mesma semente repete o relatório entre execuções. O gerador do jogo usa o mesmo cache (vida de um quadro).

## Conferir no próximo voo (PC de casa)
- Instrução local saindo de SDCO/SBKP/SBMT com a área real do DECEA (nome no texto, ponto e faixa); coluna `nav-heading-true` e vento no CSV do REC; contrato de instrução local cumprido passando na faixa; linha "CONTRATO" do HUD (texto, quebra de linha, atualização); distâncias sorteadas do cheque e do paraquedismo.
- Tipos novos da E2C-2: linha do HUD de cada modo, marcadores no mapa (SAR sem o alvo), conclusão e motivo de falha. Atenção ao teto "acima do solo" em terreno mais alto que a origem (é relativo à elevação da origem).
- Do teste anterior (C152, SBJS): triângulo VFR com toque e arremetida, escalas com motor ligado, STOL, cheque e translado (fase 2) e o quadro de SDCO/SBTF/PABE × SBFI/SBPS ainda não testados. Pneus caindo 4 % num pouso de 41 fpm com través real: conferir a calibração se repetir.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa no PC do escritório (sem simulador) — escolha uma por sessão
1. Decisões abertas abaixo (perguntas rápidas, **Sonnet**).
2. HUD em modo tablet: spec (**Opus**), quando o usuário quiser começar.
- Os 8 tipos da E2C que sobraram precisam de SimVar a conferir no MSFS ou de dados: `adaptacao_noturna`, `evacuacao_por_do_sol` e o pôr do sol da `saude_familia` (hora/sol e luzes), `pulverizacao` (AGL real), `demo_flight` (rolagem de decolagem de referência), `instrucao_ifr`, `calibracao_auxilios` (geometria de aproximação, auxílio sintonizado), `reboque_planador`, `publicidade_aerea` (reboque nativo). Um lote que acrescente `PLANE ALT ABOVE GROUND` e as SimVars de hora/sol pode ser especificado no escritório, mas só fecha com o teste no sim.
- Não dá no escritório: trem travado no `ferry_permit` (SimVar nova no `SimTelemetry`) e os itens "Precisam do MSFS".

## Próxima tarefa no PC de casa (com MSFS)
Testes de missões (Sonnet basta para ler logs): itens de "Conferir no próximo voo". Para STOL, cheque, translado e os tipos da E2C-2 (fase 2–3), o usuário decide se libera a fase no banco; cheque e translado pedem ainda um aeroporto tier 1–3.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Decisões abertas do usuário:** bots agressivos do Médio (simulação com 20 bots em andamento em 2026-11-25); regra de `isolado` nos perfis (nos quadros conferidos as missões remotas quase dobraram; falta o usuário decidir). Resolvidas em 2026-11-25: CAN e peso de saúde (feitos na branch `fix/missoes-decisoes`, **sem merge nem push**), abastecimento da `saude_familia` (mantido), patrimônio e classe G (encerradas).
- **Autorizado, falta spec (Opus), uma por sessão:** as 7 melhorias de `docs/plans/missoes-locais-rp.md` › 3.

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos.
- Com o app aberto, `dotnet build` da solução falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`; os testes rode sem `OutDir`.
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`).
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
- Banco de testes apagado em 2026-11-25; cópia em `db-backup/flightcareer.db.bak-20261125` na pasta de saída do UI.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
