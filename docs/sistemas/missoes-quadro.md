# Missões e quadro

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Gera contratos no quadro de missões, valida requisitos operacionais em voo e apura a conclusão de contratos.
- **Onde está:** `Application/Services/MissionBoardCore` (geração compartilhada com bots), `Application/Services/MissionGenerator`, `Application/Services/PlacesDataService.cs`, `Application/Services/HubDataService` (`hubs_data.json`, hubs e aeródromos de instrução), `Domain/Missions/` (`MissionCatalog`, `MissionCompletion`, `OverflyTracker`, `TouchAndGoCounter`, `SurveyLegTracker`, `SkydiveDropTracker`, `MultiStopTracker`, `StolLandingTracker`, `CheckFlightTracker`, `FerryRestrictionMonitor`, `AerialWorkTracker`, `ContractMarkerBuilder`, `OverflyWaypointCodec`), `Infrastructure/Data/mission_catalog.json`, `Infrastructure/Data/mission_texts.json`.
- **Regras e integridade:**
  - Catálogo v2 (`mission_catalog.json`): 69 tipos em snake_case com perfis de capacidade física (`MinRunwayFt`, `Paved`), tags de aeroporto (`RequireAnyTag`, `originProfiles`), pesos e tripulação. `MissionRules.cs` opera como adaptador sobre o catálogo.
  - Jatos de linha (120+ pax) saem de/para aeroporto com `linha` curada ou com a etiqueta derivada `linha_auto:hub`: `linha_auto` de tier 1, ou tier 2 com `TierScore` ≥ 100 (`MissionBoardCore.WithDerivedTags`, 2026-11-25).
  - Pista mínima e pavimento: a melhor pista da origem e destinos elegíveis filtra previamente os perfis que a pista comporta; contratos inviáveis têm peso zerado na origem.
  - Bases de dados geográficos em `Data/places/` (`tourist_pois.json`, `training_aerodromes.json`, `dropzones.json`, `scenic_aerodromes.json`, `training_areas.json`) integradas via `PlacesDataService`.
  - Tipos com regras e rastreadores dedicados (`Domain/Missions/`):
    - Voo panorâmico e translado básico: sobrevoo de waypoints (`OverflyTracker`) usando formato de coordenadas `P:<nome>@<lat>,<lon>` (`OverflyWaypointCodec`) e POIs reais.
    - Circuitos de instrução: máquina de estados de toques e arremetidas (`TouchAndGoCounter`). Instrução local pede 1 ou 2 TGL e cheque prático pede 2 TGL, ambos cobrando faixa de altitude da área de instrução (`OverflyTracker` com folga de ±500 ft). Áreas de instrução reais do DECEA (`training_areas.json`) a ≤ 25 NM da origem são priorizadas; na ausência ou faixa inválida, ocorre fallback procedural idêntico (2026-11-25).
    - SAE e aerofotogrametria (`SurveyLegTracker`): varredura em pernas paralelas sobre área orientada dentro de tolerâncias de rumo, velocidade e altitude.
    - Lançamento de paraquedistas (`SkydiveDropTracker`): subida e estabilização de 30 s sobre a zona de salto (`dropzones.json`) e monitoramento de choque térmico na descida.
    - Multi-etapas (`MultiStopTracker`): escalas intermediárias com pouso completo ou toque e arremetida (`pinga_pinga`, `navegacao_vfr`), sem reabastecimento não autorizado.
    - Pouso STOL (`StolLandingTracker`): aproximação íngreme e parada curta em pista remota (`instrucao_stol`).
    - Voo de cheque técnico (`CheckFlightTracker`): perfil de manobras e potência retornando à origem (`check_flight`).
    - Translado de manutenção restrito (`FerryRestrictionMonitor`): limites de teto e flaps (`ferry_permit`).
    - Trabalho aéreo e busca e salvamento (`AerialWorkTracker`): modos `sectors` (patrulha ambiental cobrindo setores circulares), `route` (inspeção de linhas de transmissão/dutos na ordem com cálculo de desvio lateral), `orbit` (monitoramento de tráfego em órbita por tempo simulado acumulado) e `search` (varredura visual SAR em quadrado crescente ou pernas paralelas com alvo secreto posicionado a ≤ 0,15 NM da perna).
    - Ronda médica e odontológica (`saude_familia`): viagem de ida e volta (`round_trip`) atendendo comunidades isoladas com escala completa intermediária via `MultiStopTracker`.
  - Distâncias em geradores especiais (`check_flight`, `paraquedismo` e `pinga_pinga`) respeitam `profile.rangeNm` do catálogo; `pinga_pinga` controla teto acumulado com reserva por etapa restante e mínimo total (2026-11-25).
  - Conclusão avaliada puramente em `MissionCompletion.Evaluate` recebendo `MissionTrackerResults`. Contratos não cumpridos retornam ao mercado sem penalidade financeira.
  - Textos e briefing: `mission_texts.json` fornece variantes narrativas com marcadores dinâmicos. `ContractMarkerBuilder` projeta marcadores e polígonos no mapa tático do briefing (`FlightMapControl.xaml.cs`). Não testado visualmente no simulador.
