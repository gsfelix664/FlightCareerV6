# Plano — Missões com locais reais e textos de RP (2026-11-22)

Pedido do usuário: instrução local, cheque, panorâmico, aerofoto e paraquedismo com locais reais (bancos globais), ida e volta ao mesmo aeródromo, texto de RP rico (mais de uma variante) e o mapa do briefing mostrando onde ir; avaliar melhorias nos outros tipos.

## 1. Bancos de locais (feito — `Infrastructure/Data/places/`, script `tools/places/build_places.py`)
| Arquivo | Conteúdo | Fonte |
|---|---|---|
| `tourist_pois.json` | 25.429 pontos (4.209 no Brasil): monumentos, montanhas, cachoeiras, faróis, castelos, pontes, parques nacionais, ilhas, barragens, catedrais, estádios, lagos, praias, portos, patrimônio UNESCO etc. Campos `id, n, lat, lon, k (tipo), cc (país), p (popularidade)` | Wikidata (CC0) |
| `training_aerodromes.json` | 458 aeródromos com escola/aeroclube (77 no Brasil) | `hubs_data` + nome no OurAirports + escolas/aeroclubes do OSM a ≤ 3 NM |
| `dropzones.json` | 160 zonas de paraquedismo com o aeródromo de decolagem (5 no Brasil, ex.: SDOI Boituva) | OSM `sport=parachuting` + nome no OurAirports |

Limites honestos: não existe base aberta global de escolas de voo nem de zonas de salto; a cobertura é a do OSM. Fora dessas listas o gerador continua gerando o contrato (com menor peso), como hoje. A altitude de salto não existe nas fontes: é regra por classe de aeronave.

## 2. Contratos (implementação: spec `MISSOES-LOCAIS`)
- **3.4.1 Instrução local:** aeródromos de instrução ganham peso maior. Área de instrução = ponto a 5–15 NM num rumo sorteado (longe de aeroportos grandes), faixa de altitude; manobras no texto (curvas de grande inclinação, estol com e sem motor, voo lento, planeio); exige sobrevoar a área e voltar com 1–3 TGL e pouso completo na origem.
- **3.4.3 Cheque:** origem = destino (hoje vai a outro aeródromo a 30–120 NM). Área de exame + pane simulada no texto + 2 TGL e pouso completo na origem; examinador a bordo.
- **3.5.1 Panorâmico:** 2–3 pontos turísticos reais a 5–30 NM (sorteio ponderado pela popularidade), com fallback nos aeródromos (comportamento atual).
- **3.6.1 Aerofoto:** área centrada num alvo real (barragem, porto, ponte, ilha, lago, estádio, parque nacional, praia) a 5–25 NM, com cliente no texto; regras de estabilidade atuais (±150 ft, ±10 kt, ±15°).
- **3.6.2 Paraquedismo:** zonas de salto reais ganham peso maior; zona = coordenada da DZ; altitude por classe (pistão 10.000–12.000 ft AGL, turboélice 12.000–14.000 ft AGL).
- **Pontos por coordenada:** `OverflyWaypoints` passa a aceitar pontos com coordenada e nome, mantendo o formato atual (ICAO).
- **Mapa do briefing:** marcadores do contrato (pontos, área de instrução, retângulo da aerofoto, zona de salto).
- **Textos de RP:** `mission_texts.json` com ≥ 3 variantes de título/descrição para **todos** os subtipos, com marcadores (`{origin}`, `{dest}`, `{pax}`, `{poi_list}`, `{alt}` …), sorteio determinístico pelo `Random` do gerador e fallback na descrição atual.

## 3. Melhorias propostas nos outros tipos (decisão do usuário, não implementadas)
1. **3.4.2 Navegação:** 2–3 pernas com pouso ou sobrevoo obrigatório em cada aeródromo (hoje é um destino só).
2. **Saúde (3.3.x, 4.3.1, 1.7.3):** destinos perto de hospitais reais (Wikidata), prazo visível no quadro e tempo de resposta contando no XP.
3. **Linha aérea (1.1.x, 2.1.x, 1.7.5):** preferir rotas reais (OpenFlights `routes.dat`, ODbL) e horários de banco.
4. **Agro (2.2, 3.2, 4.x):** destinos em pistas de fazenda reais (pistas não pavimentadas curtas do OurAirports em regiões agrícolas) e safra por estação do ano (relógio de carreira).
5. **Executiva / VIP:** destinos com resort, centro de negócios ou evento (pontos turísticos do banco novo) e nome de cliente fictício recorrente (fidelidade).
6. **Carga:** tipo de carga coerente com o destino (porto, mina, polo industrial) e restrição de carga perigosa.
7. **Translado (3.5.3, 1.3.3):** destino em aeródromo com oficina (`MaintenanceHubs`).

## 4. Decisões do usuário (2026-11-25)
- Instrução local pede 1 ou 2 TGL (3 era demais). `circuito_tgl` não mudou.
- A faixa de altitude da área de instrução é cobrada, sem muita precisão: folga de 500 ft, medida acima da elevação da origem; vale também para o cheque prático.
- Áreas de instrução do simulador na inicialização do banco: verificado que o SimConnect não expõe espaço aéreo nem área de treinamento (tipos de facility: aeroportos, waypoints, NDB, VOR e filhos; conferido na DLL do projeto e na documentação do SDK). Continua a decisão do híbrido (fonte externa com licença compatível, ex. openAIP, quando houver; procedural no resto).
