# AREAS-INSTRUCAO — Áreas de instrução reais do DECEA

**Branch:** feat/areas-instrucao · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar script `tools/places/build_training_areas.py` que baixa/consome a camada WFS `ICA:airspace` do DECEA GeoAISWEB, filtra áreas R/D excluindo restrições incompatíveis e extrai 11 áreas de instrução e 16 de acrobacia com centroide, raio e cotas de piso/teto.
- Gerar `src/FlightCareer.Infrastructure/Data/places/training_areas.json` e registrar fonte oficial em `ATTRIBUTION.md`.
- Adicionar `TrainingAreaInfo` e método `FindTrainingAreas` em `IPlacesDataService` e `PlacesDataService` (com construtor de teste recebendo coleção opcional e ordenação por distância/ID).
- Implementar `TryResolveRealAreaBand` e cálculo de rumo inicial geodésico em `MissionGenerator.cs`, integrando a seleção de áreas reais nos geradores `CreateLocalInstructionMissionSync` e `CreateCheckrideMissionSync` com fallback procedural idêntico.
- Conectar `placesService` em `MissionBoardCore.cs` para os dois geradores.
- Criar suite de testes em `TrainingAreasTests.cs`, validar regressão completa e atualizar snapshot do golden.

## O que foi feito
- `tools/places/build_training_areas.py`: criado script com suporte a cache (`--cache`), tratamento robusto de encoding UTF-8/Latin-1 e regex calibrada para selecionar as 11 áreas de instrução e 16 de acrobacia do DECEA GeoAISWEB.
- `src/FlightCareer.Infrastructure/Data/places/training_areas.json`: gerado arquivo JSON compacto com as 27 áreas reais ordenadas por `id`.
- `src/FlightCareer.Infrastructure/Data/places/ATTRIBUTION.md`: adicionada atribuição oficial dos dados do DECEA (AIP Brasil).
- `src/FlightCareer.Application/Interfaces/IPlacesDataService.cs`: adicionado record `TrainingAreaInfo` e assinatura de `FindTrainingAreas(double lat, double lon, double maxNm = 25.0, string kind = "instrucao")`.
- `src/FlightCareer.Application/Services/PlacesDataService.cs`: implementado carregamento de `training_areas.json`, busca filtrada por distância e kind com ordenação determinística por distância e `Id`, propriedade `TrainingAreasCount` e construtor com suporte a mock nos testes.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: implementado `TryResolveRealAreaBand` (cálculo de base/topo AGL com elevação da origem, limites múltiplos de 500 ft e verificação de espessura mínima de 1.000 ft), helper `CalculateInitialBearing`, e suporte a áreas reais em `CreateLocalInstructionMissionSync` e `CreateCheckrideMissionSync`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: repasse de `placesService` aos geradores `local_instruction` e `checkride`.
- `src/FlightCareer.Tests/Missions/TrainingAreasTests.cs`: 8 testes unitários cobrindo conversão de cotas com elevação, faixas estreitas, FL elevado, filtros de distância/kind, centroide/nome em waypoints e descrições, equivalência do fallback procedural e leitura do JSON real.
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`: snapshot regravado.

## Lista final de áreas extraídas (27)
### Instrução (11)
| ID | Nome | Centroide (Lat, Lon) | Raio | Piso | Teto |
|---|---|---|---|---|---|
| SBR364 | JOÃO MONTEIRO | (-20.53768, -40.47393) | 6.5 NM | 0 SFC | 2500 MSL |
| SBR458 | INSTRUÇÃO 1 | (-23.25998, -46.55491) | 11.2 NM | 600 SFC | 5000 MSL |
| SBR459 | INSTRUÇÃO 2 | (-22.88667, -46.65988) | 6.3 NM | 600 SFC | 6000 MSL |
| SBR460 | INSTRUÇÃO 3 | (-22.76811, -46.55462) | 14.9 NM | 600 SFC | 7000 MSL |
| SBR466 | INSTRUÇÃO 4 | (-22.59153, -47.12507) | 7.5 NM | 600 SFC | 5000 MSL |
| SBR529 | AEROCLUBE DE FREDERICO WESTPHALLEN - SSOW | (-27.34733, -53.40333) | 3.0 NM | 0 SFC | 5000 SFC |
| SBR560 | CLUBE DE VOO ITAIPU | (-25.36452, -54.42729) | 8.7 NM | 0 SFC | 1500 MSL |
| SBR561 | Área Antonio Carlos | (-27.42701, -48.73565) | 5.2 NM | 0 SFC | 2500 MSL |
| SBR595 | Capivari | (-25.19624, -48.98857) | 8.8 NM | 500 SFC | 4000 MSL |
| SBR704 | SOLIMÕES | (-3.26727, -60.37763) | 8.8 NM | 0 SFC | 1500 MSL |
| SBR806 | Aeroclube de Tatuí | (-23.34887, -47.88198) | 3.4 NM | 0 SFC | 20000 MSL |

### Acrobacia (16)
| ID | Nome | Centroide (Lat, Lon) | Raio | Piso | Teto |
|---|---|---|---|---|---|
| SBR231 | INSTRUCAO ACROBACIA SIERRA BRAVO | (-22.78258, -47.33366) | 0.5 NM | 3000 MSL | 5000 MSL |
| SBR553 | BALSA NOVA ACROBACIA | (-25.53088, -49.57127) | 3.0 NM | 0 SFC | 5500 MSL |
| SBR557 | TORRES ACROBACIA | (-29.40267, -49.82268) | 1.5 NM | 0 SFC | 5000 MSL |
| SBR632 | Rambo Acrobacia | (-15.56296, -54.34424) | 0.5 NM | 0 SFC | 6000 SFC |
| SBR810 | Acro Echo | (-21.84395, -43.27613) | 2.4 NM | 4000 MSL | 7000 MSL |
| SBR821 | ACROBACIA COSTA ESMERALDA | (-27.17875, -48.62250) | 0.5 NM | 0 SFC | 1500 SFC |
| SBR824 | BOX DE ACROBACIA-TREINAMENTO | (-26.83346, -49.09276) | 0.7 NM | 0 SFC | 3500 SFC |
| SBR831 | ACROBACIA BRAGANÇA PAULISTA | (-22.97526, -46.56728) | 0.5 NM | 0 MSL | 7000 MSL |
| SBR832 | AGROSSOL ACROBACIA | (-21.78644, -47.05494) | 0.5 NM | 0 SFC | 3500 SFC |
| SBR847 | ACRO BRASIL ACROBACIA | (-22.41628, -46.90150) | 0.5 NM | 3500 MSL | 5500 MSL |
| SBR848 | AEROQUADRA ACROBACIA | (-23.31458, -48.03000) | 1.0 NM | 0 SFC | 6000 MSL |
| SBR851 | ACROBACIA ALEGRETE | (-29.80294, -55.89068) | 0.8 NM | 0 SFC | 5000 MSL |
| SBR852 | ACROBACIA PAPAGAERO | (-27.18041, -52.05111) | 1.0 NM | 0 SFC | 6000 MSL |
| SBR855 | AERO JERUSALEM ACROBACIA | (-14.10406, -52.18554) | 1.0 NM | 0 SFC | 7000 MSL |
| SBR867 | AFB Acrobacia Aérea | (-26.06080, -53.06488) | 0.5 NM | 0 SFC | 4000 MSL |
| SBR878 | SITIO BELA VISTA ACROBACIA | (-22.54198, -47.23008) | 0.5 NM | 0 SFC | 5000 MSL |

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — compilado com êxito.
- [x] `dotnet test src/FlightCareer.Tests` 100% — 1.095 testes aprovados, 0 falhas.
- [x] Snapshot `missions_golden.json`: regerado e verificado; diff ocorreu apenas em origens a ≤ 25 NM de áreas reais (SBKP, SBMT, SDCO); 11 origens longe das áreas permaneceram 100% idênticas (0 diffs).
- [x] Lista final de áreas (id, nome, kind, faixa) documentada no relatório.
- [x] `docs/CHANGELOG.md` e `docs/sistemas/missoes-quadro.md` atualizados.
- [x] Commit único e sem push.

## Diff do snapshot golden
```text
src/FlightCareer.Tests/Missions/Golden/missions_golden.json | 626 ++++++++++-----------
1 file changed, 313 insertions(+), 313 deletions(-)
```
- Total de registros alterados: 48 (de 280)
- Origens com alteração:
  - `SBKP` (14 registros alterados): a ≤ 25 NM de SBR466
  - `SBMT` (17 registros alterados): a ≤ 25 NM de SBR458
  - `SDCO` (17 registros alterados): a ≤ 25 NM de SBR806
- Origens sem alteração (0 diffs): `SBGR`, `SBGL`, `KJFK`, `SBRP`, `SBJR`, `SBDN`, `SBSJ`, `SBFI`, `SDUB`, `SWCA`, `SSKW`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 erros novos
- `dotnet test src/FlightCareer.Tests`: 1.095 aprovados, 0 falhas (8 novos em `TrainingAreasTests`)

## Não testado
- Voo no simulador (MSFS) e renderização visual dos polígonos/marcadores no mapa tático do briefing.

## Dúvidas e decisões para o revisor
- **Impacto no snapshot para outros subtipos nas 3 origens com área real:** Em `MissionCatalogEquivalenceTests`, `MissionGenerator.GenerateMissionsInMemory` gera 20 missões por origem compartilhando uma única instância de `Random` (`rng`). Como no ramo com área real a spec instrui a não executar o laço procedural de 8 tentativas nem sortear `altBase/altTop`, o consumo de números aleatórios é menor para `instrucao_local`. Isso avança/desloca o estado do gerador para os contratos subsequentes gerados dentro do mesmo batch daquela mesma origem. Origens sem área real permaneceram com determinismo rigorosamente idêntico (zero alterações em 11 das 14 origens).
