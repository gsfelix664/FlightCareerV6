# Testes pendentes no MSFS (PC de casa)

Lido sob demanda, só quando houver voo. Apague o item quando for conferido. Roteiro do próximo voo: `ROTEIRO-VOO.md`.

## Conferir no próximo voo
- Correções de 26/11 (SDCO): quadro **novo** com área "Aeroclube de Tatuí" (o quadro antigo mantém as genéricas); 2 toques e arremetidas + pouso final = 3 pousos no Hangar, status "VOANDO" entre eles, sem "BOUNCE"; ponto só marca a ±100 ft da faixa; mini-mapa do HUD com a área antes de iniciar o voo; texto "1.500 a 3.000 ft".
- Quadro de linha aérea (hub tier 1–2): destinos que são rota real aparecem com mais frequência e trazem a nota "Rota regular com demanda consolidada.". Se tiver a base do Little Navmap, regerar com `python tools/places/build_routes.py --cache <pasta> --lnm <base>`.
- Quadro de origem agro/fazenda: nota "Época de plantio/colheita da safra." ou "Entressafra: ..." na descrição de `insumos_agro`, coerente com a data do relógio de carreira (sul: set–dez plantio, jan–abr colheita, mai–ago entressafra).
- Quadro: linha "Prazo: ..." acima da carga (vermelha em missão médica/urgente; some em voo local) e se o limite bate com o bônus no fim do voo. Linha "Carga: ..." no pré-despacho (âmbar se perigosa), títulos longos de carga, mensagem "Carga Perigosa" ao aceitar `carga_dgr`/`combustivel_tambor` junto com contrato de passageiros.
- Instrução local saindo de SDCO/SBKP/SBMT com a área real do DECEA (nome no texto, ponto e faixa); coluna `nav-heading-true` e vento no CSV do REC; contrato cumprido passando na faixa; linha "CONTRATO" do HUD (texto, quebra de linha, atualização); distâncias sorteadas do cheque e do paraquedismo.
- Tipos novos da E2C-2: linha do HUD de cada modo, marcadores no mapa (SAR sem o alvo), conclusão e motivo de falha. Atenção ao teto "acima do solo" em terreno mais alto que a origem (é relativo à elevação da origem).
- Do teste anterior (C152, SBJS): triângulo VFR com toque e arremetida, escalas com motor ligado, STOL, cheque e translado (fase 2) e o quadro de SDCO/SBTF/PABE × SBFI/SBPS ainda não testados. Pneus caindo 4 % num pouso de 41 fpm com través real: conferir a calibração se repetir.
- Para STOL, cheque, translado e os tipos da E2C-2 (fase 2–3), o usuário decide se libera a fase no banco; cheque e translado pedem ainda um aeroporto tier 1–3. Falta medir o percentual do destino preferido de saúde com peso 100.

## Testes de 22/11 sem registro de conferência
Do roteiro `TESTES-PC-2026-11-22.md` (apagado; completo no git). Não se sabe quais já foram vistos.
- Telas: fase/nível, quadro com "≈ XP" e fases bloqueadas, ícones de peça no Hangar sem %, serviço Normal/Expresso com "Na oficina" (fechando o app no meio), combustível no briefing, mapinha do HUD com a partida, processo encerra ao fechar.
- Voo normal: "TOQUE" do blackbox × REC, sem combustível cobrado no fim, detalhamento do XP, diário.
- Abusos: TGL a menos, pouso > 400 fpm, partida com faróis ligados, táxi rápido em curva, frenagem forte, derrapagem, estol, pausa e aceleração de tempo, crash no diário.
- Fase 2 (fotografia, paraquedistas), panorâmico incompleto, partida de turbina e regras elétricas; locais reais no briefing, área de instrução e Boituva.
