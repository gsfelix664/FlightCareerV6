# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25 (13ª sessão, escritório), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.165/1.165.

## Feito nesta sessão
- ROTAS-REAIS (spec do Claude, implementação do Gemini, revisão APROVADO em `docs/reviews/ROTAS-REAIS-revisao.md`, merge feito): as 8 missões de linha aérea dão multiplicador de peso (6 nas regulares, 4 nas regionais) ao destino que é rota real no OpenFlights `routes.dat` (2014, ODbL). Dados em `places/airline_routes.json` (`tools/places/build_routes.py`, gerado **sem** `--lnm`) e nota "Rota regular com demanda consolidada." no texto.
- Decisões do usuário (2026-11-25, no plano `missoes-locais-rp.md` › 4): OpenFlights aceito; horários de banco adiados.

## Conferir no próximo voo (PC de casa)
- Quadro de linha aérea (hub tier 1–2): destinos que são rota real aparecem com mais frequência e trazem a nota "Rota regular com demanda consolidada.". Se tiver a base do Little Navmap, regerar com `python tools/places/build_routes.py --cache <pasta> --lnm <base>`.
- Quadro de origem agro/fazenda: nota "Época de plantio/colheita da safra." ou "Entressafra: ..." na descrição de `insumos_agro`, coerente com a data do relógio de carreira (sul: set–dez plantio, jan–abr colheita, mai–ago entressafra).
- Quadro: linha "Prazo: ..." acima da carga (vermelha em missão médica/urgente; some em voo local) e se o limite bate com o bônus no fim do voo. Linha "Carga: ..." no pré-despacho (âmbar se perigosa), títulos longos de carga, mensagem "Carga Perigosa" ao aceitar `carga_dgr`/`combustivel_tambor` junto com contrato de passageiros.
- Instrução local saindo de SDCO/SBKP/SBMT com a área real do DECEA (nome no texto, ponto e faixa); coluna `nav-heading-true` e vento no CSV do REC; contrato de instrução local cumprido passando na faixa; linha "CONTRATO" do HUD (texto, quebra de linha, atualização); distâncias sorteadas do cheque e do paraquedismo.
- Tipos novos da E2C-2: linha do HUD de cada modo, marcadores no mapa (SAR sem o alvo), conclusão e motivo de falha. Atenção ao teto "acima do solo" em terreno mais alto que a origem (é relativo à elevação da origem).
- Do teste anterior (C152, SBJS): triângulo VFR com toque e arremetida, escalas com motor ligado, STOL, cheque e translado (fase 2) e o quadro de SDCO/SBTF/PABE × SBFI/SBPS ainda não testados. Pneus caindo 4 % num pouso de 41 fpm com través real: conferir a calibração se repetir.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa no PC do escritório (sem simulador) — escolha uma por sessão
1. Importador de aeroportos (`docs/plans/importador-aeroportos-simconnect.md`). As melhorias da § 3 de `missoes-locais-rp.md` estão todas feitas; horários de banco da linha aérea ficaram adiados (pedem spec **Opus** se o usuário quiser).
2. HUD em modo tablet: spec (**Opus**), quando o usuário quiser começar.
- Os 8 tipos da E2C que sobraram precisam de SimVar a conferir no MSFS ou de dados: `adaptacao_noturna`, `evacuacao_por_do_sol` e o pôr do sol da `saude_familia` (hora/sol e luzes), `pulverizacao` (AGL real), `demo_flight` (rolagem de decolagem de referência), `instrucao_ifr`, `calibracao_auxilios` (geometria de aproximação, auxílio sintonizado), `reboque_planador`, `publicidade_aerea` (reboque nativo). Um lote que acrescente `PLANE ALT ABOVE GROUND` e as SimVars de hora/sol pode ser especificado no escritório, mas só fecha com o teste no sim.
- Não dá no escritório: trem travado no `ferry_permit` (SimVar nova no `SimTelemetry`) e os itens "Precisam do MSFS".

## Próxima tarefa no PC de casa (com MSFS)
**Isolamento (primeiro):** regerar perfis com `ISOLADO_NM=200` e `250` e medir; passos em `docs/plans/catalogo-missoes.md` › 12 e `docs/sistemas/aeroportos-tiers.md`. Conferir também o percentual do destino preferido de saúde com peso 100 (não medido).

Testes de missões (Sonnet basta para ler logs): itens de "Conferir no próximo voo". Para STOL, cheque, translado e os tipos da E2C-2 (fase 2–3), o usuário decide se libera a fase no banco; cheque e translado pedem ainda um aeroporto tier 1–3.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Decisões abertas do usuário:** corte final de `isolado` (depois da medição em casa). Resolvidas em 2026-11-25 e mergeadas: CAN só em origem militar, peso de saúde 100, `saude_familia` abastece no destino, falência dos agressivos aceita, patrimônio e classe G encerradas.

## Notas de trabalho
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos.
- Com o app aberto, `dotnet build` da solução falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`; os testes rode sem `OutDir`.
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`).
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
- Banco de testes apagado em 2026-11-25; cópia em `db-backup/flightcareer.db.bak-20261125` na pasta de saída do UI.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
