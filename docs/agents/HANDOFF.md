# Passagem de sessão

> Regra (`AGENTS.md` › 4.9): uma tarefa por sessão. Comece por aqui; ao terminar, **reescreva** este arquivo (não acumule passagens: o histórico fica no `docs/CHANGELOG.md` e no git), faça commit e push e peça ao usuário uma sessão nova.

**Última atualização:** 2026-11-25 (15ª sessão, casa), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.189/1.189.

## Feito nesta sessão
- Corte de `isolado` medido (150/200/250 NM) e definido pelo usuário em **200 NM**; `airport_profiles.json` e snapshot de missões regravados (`docs/plans/catalogo-missoes.md` › 12). Não testado no MSFS.
- Apagadas `.antigravity/` e `.gemini/`.
- Banco do Little Navmap **não** vai para o git: recorte em `references/lnm/little_navmap_msfs24.sqlite.gz` (PC de casa); o usuário leva para o escritório (uso: `docs/sistemas/aeroportos-tiers.md`).

## Conferir no próximo voo (PC de casa)
- Quadro de linha aérea (hub tier 1–2): destinos que são rota real aparecem com mais frequência e trazem a nota "Rota regular com demanda consolidada.". Se tiver a base do Little Navmap, regerar com `python tools/places/build_routes.py --cache <pasta> --lnm <base>`.
- Quadro de origem agro/fazenda: nota "Época de plantio/colheita da safra." ou "Entressafra: ..." na descrição de `insumos_agro`, coerente com a data do relógio de carreira (sul: set–dez plantio, jan–abr colheita, mai–ago entressafra).
- Quadro: linha "Prazo: ..." acima da carga (vermelha em missão médica/urgente; some em voo local) e se o limite bate com o bônus no fim do voo. Linha "Carga: ..." no pré-despacho (âmbar se perigosa), títulos longos de carga, mensagem "Carga Perigosa" ao aceitar `carga_dgr`/`combustivel_tambor` junto com contrato de passageiros.
- Instrução local saindo de SDCO/SBKP/SBMT com a área real do DECEA (nome no texto, ponto e faixa); coluna `nav-heading-true` e vento no CSV do REC; contrato de instrução local cumprido passando na faixa; linha "CONTRATO" do HUD (texto, quebra de linha, atualização); distâncias sorteadas do cheque e do paraquedismo.
- Tipos novos da E2C-2: linha do HUD de cada modo, marcadores no mapa (SAR sem o alvo), conclusão e motivo de falha. Atenção ao teto "acima do solo" em terreno mais alto que a origem (é relativo à elevação da origem).
- Do teste anterior (C152, SBJS): triângulo VFR com toque e arremetida, escalas com motor ligado, STOL, cheque e translado (fase 2) e o quadro de SDCO/SBTF/PABE × SBFI/SBPS ainda não testados. Pneus caindo 4 % num pouso de 41 fpm com través real: conferir a calibração se repetir.

## Testes de 22/11 sem registro de conferência (PC de casa)
Vieram do roteiro `TESTES-PC-2026-11-22.md` (apagado; completo no git). Não se sabe quais já foram vistos.
- Telas: fase/nível, quadro com "≈ XP" e fases bloqueadas, ícones de peça no Hangar sem %, serviço Normal/Expresso com "Na oficina" (fechando o app no meio), combustível no briefing, mapinha do HUD com a partida, processo encerra ao fechar.
- Voo normal: "TOQUE" do blackbox × REC, sem combustível cobrado no fim, detalhamento do XP, diário.
- Abusos: TGL a menos, pouso > 400 fpm, partida com faróis ligados, táxi rápido em curva, frenagem forte, derrapagem, estol, pausa e aceleração de tempo, crash no diário.
- Fase 2 (fotografia, paraquedistas), panorâmico incompleto, partida de turbina e regras elétricas; locais reais no briefing, área de instrução e Boituva.

## Pendente da configuração do PC de casa
- No app do Claude: desligar o conector **Claude Docs**, as **sugestões de prompt** e, se não usar, o **navegador do Claude** / Claude in Chrome (o usuário faz).

## Próxima tarefa no PC do escritório (sem simulador) — escolha uma por sessão
1. **AIRSIM-2** (spec **Opus**): cliente SimConnect `UI/Services/SimAirportImporter` (padrão `ContextSnapshotReader`, IDs a partir de 3000), versão do sim no `OnRecvOpen`, lista + detalhes em lotes, gravação pelo `SimAirportSyncStore`, registro no `App.xaml.cs`; acrescentar luzes de aproximação ao `SimRunwayData` para o `IsLighted` (plano › 7 e pendências da revisão AIRSIM-1). Implementa no escritório, mas só fecha com o teste no MSFS.
2. HUD em modo tablet: spec (**Opus**), quando o usuário quiser começar.
- Os 8 tipos da E2C que sobraram precisam de SimVar a conferir no MSFS ou de dados: `adaptacao_noturna`, `evacuacao_por_do_sol` e o pôr do sol da `saude_familia` (hora/sol e luzes), `pulverizacao` (AGL real), `demo_flight` (rolagem de decolagem de referência), `instrucao_ifr`, `calibracao_auxilios` (geometria de aproximação, auxílio sintonizado), `reboque_planador`, `publicidade_aerea` (reboque nativo). Um lote que acrescente `PLANE ALT ABOVE GROUND` e as SimVars de hora/sol pode ser especificado no escritório, mas só fecha com o teste no sim.
- Não dá no escritório: trem travado no `ferry_permit` (SimVar nova no `SimTelemetry`) e os itens "Precisam do MSFS".

## Próxima tarefa no PC de casa (com MSFS)
Testes de missões (Sonnet basta para ler logs): itens de "Conferir no próximo voo". Para STOL, cheque, translado e os tipos da E2C-2 (fase 2–3), o usuário decide se libera a fase no banco; cheque e translado pedem ainda um aeroporto tier 1–3. Falta medir o percentual do destino preferido de saúde com peso 100.

## Pendências
- **Precisam do MSFS:** óleo (inverter `GENERAL ENG OIL LEAKED PERCENT` no vazamento ativo, `ProcessOilLeak`/`TransmitOilLeak`); perda de desempenho no amarelo (`docs/plans/perda-desempenho-amarelo.md` › 3, passos 1–5); toque (`TOQUE-NORMAL`) a conferir contra o REC.
- **Decisões abertas do usuário:** nenhuma. Resolvidas em 2026-11-25 e mergeadas: CAN só em origem militar, peso de saúde 100, `saude_familia` abastece no destino, falência dos agressivos aceita, patrimônio e classe G encerradas.

## Notas de trabalho
- Repositório no GitHub é público: não commitar dados extraídos do simulador sem perguntar.
- Referência: clones de `atools` e `littlenavmap` em `C:/projetos/ref/` (fora do repositório; só consulta, código GPL, não copiar).
- PC do escritório tem 4 GB: uma tarefa do Gemini por vez e poucos programas abertos.
- Com o app aberto, `dotnet build` da solução falha ao copiar: use `-p:OutDir=<pasta do scratchpad>`; os testes rode sem `OutDir`.
- Tamanho do `AGENTS.md`: meça no git (`git cat-file -s HEAD:AGENTS.md`).
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
- Banco de testes apagado em 2026-11-25; cópia em `db-backup/flightcareer.db.bak-20261125` na pasta de saída do UI.
- `REGRAS_tiers.txt` na raiz (do usuário, não commitado): não analisar sem pedido.
