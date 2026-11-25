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

## 5. Áreas de instrução reais — pesquisa e proposta (2026-11-25, Claude)

### 5.1 Fontes avaliadas
| Fonte | Licença | O que tem | Veredito |
|---|---|---|---|
| **openAIP** (exportação por país `s3.openaip.net/openaip-system-exports/<cc>_asp.geojson`, sem chave; também `.json`, `.xml`, `.cup`, OpenAir) | **CC BY-NC 4.0** (rodapé de openaip.net: "OpenAIP data, unless otherwise stated, is licensed under CC BY-NC 4.0"). Proíbe uso comercial | Não tem tipo "área de treino": as de instrução aparecem como `type` 1 (restrita) e só se acha pelo nome. BR: 735 espaços, **4** de instrução (SBR458–460, SBR466). DE, GB: 0; FR: 2; US: 0 civis (só militares); AU: 1 (militar) | **Não usar**: quase nada a mais que o DECEA e a licença impede vender o addon |
| **DECEA GeoAISWEB** (WFS público `geoaisweb.decea.mil.br/geoserver`, camada `ICA:airspace`, GeoJSON, sem chave) | Dado público oficial (AIP); **sem licença explícita** encontrada | 1.859 espaços; `typ` R/D/P, `nam`, `txtrmk` ("CIV ACFT training", "MIL ACFT training"), limites (`lowerlimi1`/`codedistv1` SFC/MSL, `upperlimit`/`uplimituni` FT/FL), polígono. Codificação mista (latin-1 em parte dos nomes) | **Usar para o Brasil** (decisão do usuário pendente, § 5.4) |
| Little Navmap / Navigraph (1.375 áreas `TR`) | Navigraph não permite redistribuir | — | Descartado (já decidido) |
| FAA (EUA) | Domínio público | Só SUA militar (MOA, R-xxxx); não há área de instrução civil publicada | Nada a importar |
| Europa (eAIP de cada país) | Varia por país | Poucas áreas civis de instrução publicadas; no openAIP não aparecem | Fora do escopo agora |

Conclusão: **área real só no Brasil (DECEA)**; resto do mundo continua procedural (comportamento atual).

### 5.2 Áreas civis do DECEA (extração de 2026-11-25)
- **Instrução (11):** SBR458 INSTRUÇÃO 1 (600 ft SFC–5.000 MSL, raio ~11 NM), SBR459 INSTRUÇÃO 2 (–6.000), SBR460 INSTRUÇÃO 3 (–7.000), SBR466 INSTRUÇÃO 4 (–5.000) (região de SP/Campinas/Bragança); SBR595 Capivari (500 SFC–4.000 MSL, PR); SBR560 Clube de Voo Itaipu (SFC–1.500); SBR561 Área Antônio Carlos (SFC–2.500, SC); SBR364 João Monteiro (SFC–2.500, ES); SBR704 Solimões (SFC–1.500, AM); SBR529 Aeroclube de Frederico Westphalen (SFC–5.000 SFC); SBR806 Aeroclube de Tatuí (SFC–FL200).
- **Acrobacia (~17 "box"):** SBR231, 553, 557, 632, 821, 824, 831, 832, 847, 848, 851, 852, 855, 867, 878 etc., raio 0,4–3 NM. Úteis para um futuro contrato de acrobacia, não para a instrução local.
- **Excluídas:** voo livre/parapente/ultraleve, paraquedismo (já em `dropzones.json`), "Campo de Instrução" (Exército), "MIL ACFT training", treinamento de linha aérea (SBR841/842, 4.000 ft–FL100).

### 5.3 Implementação (spec `AREAS-INSTRUCAO`, feita em 2026-11-25; revisão em `docs/reviews/AREAS-INSTRUCAO-revisao.md`)
Na implementação o arquivo não guarda lista de aeródromos: o gerador busca as áreas a ≤ 25 NM da origem pela coordenada (`FindTrainingAreas`).
1. **Importação em tempo de desenvolvimento:** `tools/places/build_training_areas.py --cache <pasta>` baixa `ICA:airspace` do WFS, filtra `typ in (R, D)` e (`txtrmk` contém "CIV ACFT training" **ou** nome com INSTRU/AEROCLUBE/CLUBE DE VOO), exclui as listas do § 5.2, decodifica o nome (utf-8, senão latin-1) e grava `Infrastructure/Data/places/training_areas.json`: `{id, name, kind: "instrucao"|"acrobacia", lat, lon (centroide), radiusNm (maior distância centroide–vértice, mínimo 2), floorFt, floorRef: "SFC"|"MSL", ceilFt (FL×100), ceilRef, aerodromes: [ICAO a ≤ 25 NM do centroide, do OurAirports/`--lnm`, só os que existem no MSFS]}`. Polígono não entra (o contrato usa ponto + raio). Entrada no `ATTRIBUTION.md` (fonte DECEA/AIP Brasil, data da extração).
2. **Leitura:** `IPlacesDataService.FindTrainingAreas(string icao, double maxNm = 25)` → `IReadOnlyList<TrainingAreaInfo>` (só `kind == instrucao`).
3. **Gerador (`CreateLocalInstructionMissionSync` e `CreateCheckrideMissionSync`):** se houver área real para a origem, sorteia uma (com o `Random` do gerador) e usa centro, raio (limitado a 3–6 NM para o ponto do contrato) e faixa: base = max(piso, elevação + 1.500 ft), topo = min(teto, base + 1.500), convertendo SFC↔MSL pela elevação da origem; o texto cita o nome oficial ("Área SBR595 Capivari"). Sem área real, ou faixa resultante < 1.000 ft, cai no procedural atual. Peso de sorteio entre real × procedural: 100 % real quando existir (proposta).
4. **Mapa do briefing:** o marcador da área já existe; só o nome muda.
5. **Testes:** leitura do JSON; origem com área real → ponto no centroide com nome e faixa corretos; faixa inválida → procedural; origem sem área → procedural igual ao snapshot atual (o `missions_golden` só muda nas origens com área real, a conferir).

### 5.4 Decisões abertas do usuário
1. ~~Usar os dados do DECEA sem licença explícita?~~ **Decidido (2026-11-25): sim**, usar o DECEA citando a fonte (DECEA/AIP Brasil e data da extração no `ATTRIBUTION.md`).
2. openAIP descartado (2026-11-25): licença não comercial e quase nada além do DECEA; o usuário não se opôs.
3. **Decidido (2026-11-25): as áreas de acrobacia entram** no `training_areas.json` (`kind: "acrobacia"`), para um futuro contrato de acrobacia; a instrução local e o cheque leem só `kind == instrucao`.
