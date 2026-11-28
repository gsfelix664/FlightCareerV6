# MISSOES-LOCAIS — Contratos com locais reais, textos de RP e marcadores no mapa do briefing

**Branch:** feat/missoes-locais · **Commit:** d351a08 · **Data:** 2026-11-22

## Plano (antes de codar)
- Investigar contratos locais (3.4.1, 3.4.3, 3.5.1, 3.6.1, 3.6.2), bancos em `Data/places/` e mapa de briefing.
- Implementar pontos por coordenada no domínio: `P:<nome>@<lat>,<lon>[,<raioNM>[,<maxAglFt>]]`, `OverflyWaypointCodec`, `OverflyPoint` e `OverflyTracker`.
- Atualizar `FlightMonitor` para resolver coordenadas `P:` direto do codec e elevação da origem.
- Implementar `PlacesDataService` com lazy loading thread-safe e indexação espacial de 1° por lat/lon, integrando ao DI e a `HubDataService`.
- Remodelar geração de 3.4.1 (área 5–15 NM, 1–3 TGLs), 3.4.3 (origem=destino, 1 pax examinador, 2 TGLs, pane simulada), 3.5.1 (POIs reais ponderados por popularidade), 3.6.1 (centralização em POIs de infraestrutura/natureza) e 3.6.2 (coordenadas e nome da DZ, altitude por classe de motor).
- Ajustar pesos de aeródromo com DropZone (+40 em 3.6, proporção 80% 3.6.2 vs 20% 3.6.1) e aeródromo de instrução (+45 em 3.4, +15 em 3.5).
- Criar catálogo `mission_texts.json` com $\ge 3$ variantes imersivas para os 59 subtipos e renderizador `MissionTextCatalog`.
- Renderizar marcadores e áreas contratuais em `FlightMapControl` e integrar ao `BriefingViewModel` via `ContractMarkerBuilder`.
- Implementar testes unitários xUnit abrangentes para todos os componentes novos.

## O que foi feito
- `src/FlightCareer.Domain/Missions/OverflyPoint.cs`: Adicionados `RadiusNm = 2.0` e `MaxAglFt = 3000.0` com valores padrão.
- `src/FlightCareer.Domain/Missions/OverflyTracker.cs`: Atualizado para validar raio horizontal e teto sobre elevação com base nas propriedades do ponto.
- `src/FlightCareer.Domain/Missions/OverflyWaypointCodec.cs`: Criado codec para parse e formatação de tokens legados (ICAO) e tokens por coordenada `P:`.
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: Atualizado para validar waypoints e áreas utilizando o codec.
- `src/FlightCareer.Domain/Missions/MapContractMarker.cs`: Criados records `MapContractMarker`, `ContractMarkerKind`, `GeoPoint` e `SurveyMapArea`.
- `src/FlightCareer.Domain/Missions/ContractMarkerBuilder.cs`: Builder puro extraindo marcadores de contrato e computando cantos e pernas de aerofoto.
- `src/FlightCareer.Domain/Missions/MissionTextCatalog.cs`: Carregador e renderizador de variantes de texto com substituição limpa de marcadores e remoção de chaves não utilizadas.
- `src/FlightCareer.Application/Interfaces/IPlacesDataService.cs`: Interface para os dados de locais reais (POIs turísticos, aeródromos de treino e dropzones).
- `src/FlightCareer.Application/Services/PlacesDataService.cs`: Serviço com cache estático lazy, thread-safe, grade de 1° e suporte a injeção em memória nos testes.
- `src/FlightCareer.Application/Services/HubDataService.cs`: Unificação de `training_aerodromes.json` aos hubs de instrução.
- `src/FlightCareer.Application/Services/MissionRules.cs`: Ajuste do range de travessia do subtipo 3.4.3 para 20–40 NM (circuito local).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Remodelados os métodos de geração síncronos e em memória de 3.4.1, 3.4.3, 3.5.1, 3.6.1 e 3.6.2.
  - Implementado sorteio de área com rejeição de proximidade a aeródromos tier 1–2 em 3.4.1 e 3.4.3.
  - Implementado ajuste de pesos com vocação de DropZone e Instrução.
- `src/FlightCareer.Infrastructure/FlightCareer.Infrastructure.csproj`: Adicionado `Data\places\*.json` para cópia automática na pasta de saída.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: Criadas $\ge 3$ variantes em português para todos os 59 subtipos do catálogo.
- `src/FlightCareer.UI/App.xaml.cs`: Registrado `IPlacesDataService, PlacesDataService` como Singleton.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: Leitura de waypoints `P:` e aeroportos via codec com fallback de elevação da origem.
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`: Suporte a `ContractMarkers` e `SurveyArea`, desenhando pontos (amarelo), círculos tracejados de área, círculos neon de DZ (laranja) e retângulos com linhas de voo para aerofoto, com auto-enquadramento.
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`: Exposição de `ContractMarkers` e `SurveyArea`, populados por `ContractMarkerBuilder`.
- `src/FlightCareer.UI/Views/BriefingView.xaml`: Data bindings de `ContractMarkers` e `SurveyArea` no `FlightMapControl`.
- `src/FlightCareer.Tests/Missions/`: Implementadas classes de testes dedicadas (`OverflyWaypointCodecTests`, `OverflyTrackerTests`, `MissionCompletionLocalTests`, `ContractMarkerBuilderTests`, `MissionTextCatalogTests`, `PlacesDataServiceTests`, `MissionGeneratorLocalTests`).

## Critérios de aceite
- [x] Pontos por coordenada: formato `P:<nome>@<lat>,<lon>[,<raioNM>[,<maxAglFt>]]` codificado/decodificado com suporte a pt-BR, tolerância a nulos e coexistência com ICAO legado — testado em `OverflyWaypointCodecTests`.
- [x] Tracker e conclusão: raio e teto customizados respeitados; missão 3.4.1 e 3.4.3 validadas com área, TGLs e pouso na origem — testado em `OverflyTrackerTests` e `MissionCompletionLocalTests`.
- [x] Bancos de dados: `tourist_pois.json`, `training_aerodromes.json` e `dropzones.json` carregados e copiados pelo csproj — testado em `PlacesDataServiceTests`.
- [x] Vocação de aeródromos e pesos: DropZone adiciona peso em 3.6 e induz proporção ~80% de 3.6.2 contra ~20% de 3.6.1; aeródromos de treino ganham bônus de instrução e panorâmico — testado em `MissionGeneratorLocalTests`.
- [x] Subtipos locais remodelados (3.4.1, 3.4.3, 3.5.1, 3.6.1, 3.6.2) geram parâmetros operacionais corretos — testado em `MissionGeneratorLocalTests`.
- [x] Textos de RP: todos os 59 subtipos possuem $\ge 3$ variantes ricas sem chaves soltas `{}` — testado em `MissionTextCatalogTests`.
- [x] Marcadores no briefing: marcadores de Point, Area, DropZone e SurveyArea gerados com geometria correta e renderizados no `FlightMapControl` — testado em `ContractMarkerBuilderTests`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 925 aprovados, 0 falhas (27 testes novos criados).

## Não testado
- Renderização visual direta na interface WPF e comportamento interativo no MSFS 2020/2024 (execução em ambiente de automação headless/CLI).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva encontrada. A integração de `IPlacesDataService` como dependência opcional no `MissionGenerator` permitiu manter compatibilidade total com chamadas existentes sem regressões.
