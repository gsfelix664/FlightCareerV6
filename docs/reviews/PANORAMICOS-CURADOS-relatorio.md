# PANORAMICOS-CURADOS — Aeródromos turísticos curados no voo panorâmico e ferry de entrega

**Branch:** feat/panoramicos-curados · **Commit:** <pendente> · **Data:** 2026-11-22

## Plano (antes de codar)
- Definir `ScenicPoint(Name, Lat, Lon)` e `ScenicAerodromeInfo(Icao, Name, Cc, Note, Points)` em `IPlacesDataService.cs` e adicionar `GetScenicAerodrome(icao)` ao contrato.
- Carregar `scenic_aerodromes.json` em `PlacesDataService` com lazy loading estático com lock, expondo `ScenicAerodromesCount` e construtor de testes com injeção de dados.
- Atualizar `CreateScenicTourMissionSync` em `MissionGenerator.cs` para, em aeródromos curados com ≥ 2 pontos, sortear 2–4 pontos curados (≥ 2 NM entre si, sem peso de popularidade, ordenados por azimute), calcular distância real e injetar marcadores `{scenic_name}` e `{scenic_note}`.
- Calibrar pesos em `GetCalibratedCategories`: se for aeródromo turístico curado, importar Categoria 3.5 caso ausente no tier, adicionar +40 em "3.5" e fixar pesos internos em 70 para 3.5.1 e 15/15 para 3.5.2 e 3.5.3.
- Adicionar em `mission_texts.json`: 2 variantes ao 3.5.1 com `{scenic_name}` e `{scenic_note}`; 2 variantes cada ao 3.5.3, 1.3.3 e 4.2.2 com foco em entrega de aeronave ao proprietário (tom de briefing em português, sem marcas ou nomes de pessoas reais).
- Criar suíte de testes cobrindo carga real dos 16 aeródromos, geração de voos panorâmicos curados vs fallback de POIs, skew de peso em 500 corridas e integridade dos textos.
- Verificar com `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests`.

## O que foi feito
- `src/FlightCareer.Application/Interfaces/IPlacesDataService.cs`:
  - Adicionados os records `ScenicPoint(string Name, double Lat, double Lon)` e `ScenicAerodromeInfo(string Icao, string Name, string Cc, string Note, IReadOnlyList<ScenicPoint> Points)`.
  - Adicionado o método `ScenicAerodromeInfo? GetScenicAerodrome(string icao)` na interface `IPlacesDataService`.
- `src/FlightCareer.Application/Services/PlacesDataService.cs`:
  - Implementado o carregamento lazy e thread-safe de `scenic_aerodromes.json` no método `LoadScenicAerodromes()`, acionado por `EnsureDataLoaded()`.
  - Adicionadas as propriedades e métodos `ScenicAerodromesCount` e `GetScenicAerodrome(string icao)`, com suporte a instâncias de teste com injeção customizada no construtor.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Atualizado `CreateScenicTourMissionSync`: quando a origem for um aeródromo turístico com $\ge 2$ pontos curados, seleciona determinística e aleatoriamente de 2 a 4 atrações com espaçamento mínimo de 2 NM (com loop de retentativas e fallback de par de máxima dispersão caso todos os pontos estejam a menos de 2 NM entre si), ordenadas por azimute em relação à origem, gerando waypoints `P:` com raio 1,5 NM e 3.000 ft AGL e distância pelo percurso real `origem ➔ pontos ➔ origem`.
  - Injetados os marcadores `{scenic_name}` e `{scenic_note}` no dicionário de substituição do catálogo de textos de briefing, com fallback natural para aeródromos sem curadoria via busca espacial `FindPois`.
  - Atualizado `GetCalibratedCategories`: detecta aeródromos turísticos curados, importa a Categoria 3.5 de `MissionRules` caso não exista no Tier do aeródromo (ex.: aeródromos Tier 1 como SBFI), adiciona bônus de peso +40 em "3.5" e repondera as subcategorias para 70 em 3.5.1 (voo panorâmico) e 15/15 em 3.5.2 (TGL) e 3.5.3 (translado básico).
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`:
  - `3.5.1`: Adicionadas 2 variantes utilizando `{scenic_name}` e `{scenic_note}`.
  - `3.5.3`: Adicionadas 2 variantes de entrega técnica / reposicionamento ao proprietário pós-revisão.
  - `1.3.3`: Adicionadas 2 variantes de translado executivo de entrega ao novo proprietário e pós-revisão.
  - `4.2.2`: Adicionadas 2 variantes de translado de entrega ao produtor rural e pós-revisão de entressafra.
- `src/FlightCareer.Tests/Missions/PlacesDataServiceTests.cs`:
  - Adicionado teste `LoadRealFiles_ScenicAerodromes_LoadedWith16Items_SbjrHasPointsAndNote` validando a presença dos 16 aeródromos, nota e atrações de SBJR.
- `src/FlightCareer.Tests/Missions/MissionGeneratorLocalTests.cs`:
  - Adicionados testes: `Subtype351_ScenicTour_WithCuratedAerodrome_UsesCuratedPointsAndAttributes`, `Subtype351_ScenicTour_NonCuratedAerodrome_UsesTouristPois`, `MissionGenerator_CuratedScenicAerodrome_WeightsAndGeneratesRoughly70PercentScenicTour` (500 corridas, validação de proporção estatística de ~70% na categoria 3.5) e `GetCalibratedCategories_CuratedScenicAerodrome_ImportsCategory35AndSkewsWeights` (importação e calibração em aeroporto Tier 1).
- `docs/CHANGELOG.md` e `AGENTS.md`:
  - Documentado o novo sistema de aeródromos turísticos curados e os novos textos de ferry de entrega.

## Critérios de aceite
- [x] Build 0 erros — Verificado via `dotnet build src/FlightCareer.slnx` (0 erros, 0 avisos).
- [x] `dotnet test` 100 % — Verificado via `dotnet test src/FlightCareer.Tests` (934 testes aprovados, 0 falhas).
- [x] CHANGELOG e AGENTS.md › 7 atualizados — Entradas registradas nas seções apropriadas.
- [x] Um commit: `feat(missoes): aerodromos turisticos curados no voo panoramico (PANORAMICOS-CURADOS)` — Pronto para commit na branch `feat/panoramicos-curados`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 934 aprovados, 0 falhas (5 novos testes em `PlacesDataServiceTests` e `MissionGeneratorLocalTests`)

## Não testado
- Renderização visual dos marcadores no mapa e no briefing da UI WPF.
- Execução em voo no Microsoft Flight Simulator (MSFS 2020 / MSFS 2024).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. Para aeródromos curados cujas atrações sejam muito concentradas geograficamente (< 2 NM entre si), implementou-se um algoritmo de busca com até 10 sorteios e fallback de máxima dispersão geométrica entre pares para garantir que uma rota cênica válida sempre seja formada sem falha do gerador.
