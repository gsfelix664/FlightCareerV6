# Aeroportos e tiers

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Classifica aeroportos em tiers (1 a 4), enriquece dados de infraestrutura, resolve fronteiras e gerencia perfis de tráfego.
- **Onde está:** `Domain/Airports/` (`AirportTierClassifier`, `AirportTierRules`), `Domain/Missions/AirportProfileCatalog`, `Domain/Geo/CountryLocator`, `Infrastructure/Data/airport_tier_rules.json`, `Data/places/airport_profiles.json`, `DataSeeder`.
- **Regras e integridade:**
  - Classificação por pontuação combinada (`AirportTierClassifier`): cortes baseados em infraestrutura real (Little Navmap, OurAirports e etiquetas curadas). Pistas não pavimentadas têm teto de Tier 3.
  - Recálculo automático em lote no `DataSeeder.EnsureTiersUpToDateAsync` acionado por mudança de versão da regra (`TierRulesVersion`).
  - Etiquetas e vocações de aeroporto (`AirportProfileCatalog`): tags de malha aérea (`linha`, `linha_auto:hub`, `carga`, `hospital`, etc.) determinam a geração e os destinos de voos de linha regular e missões especiais. Jatos de linha exigem hubs conectados.
  - Perfis (`airport_profiles.json`) gerados por `tools/places/airport_profiles.py <repo> <saida> <lnm> <json> <tier-report.csv>`: as heurísticas `isolado`/`interior_distante`/`fazenda`/`turistico_poi` usam o tier do jogo. Depois de mudar lista curada: gerar, rodar `tier-report`, gerar de novo e subir `Version` em `airport_tier_rules.json`. Regras na versão 3 (2026-11-25). Corte de `isolado`: 200 NM (decisão do usuário, 2026-11-25).
  - Banco do Little Navmap fora do git (repositório público): recorte (`airport`, `runway`, `metadata`) em `references/lnm/little_navmap_msfs24.sqlite.gz` no PC de casa, copiado à mão. Em outro PC, descompactar em `%AppData%\ABarthel\little_navmap_db\`; gera `tier-report` e perfis idênticos aos do banco completo.
  - Resolução geográfica de países (`CountryLocator`): mapeia coordenadas geográficas contra fronteiras vetoriais (`country_borders.json`) com aproximação por aeroportos vizinhos (`AirportSpatialGrid`) para áreas costeiras e ilhas.
  - Relatório e auditoria via comando `tier-report` no CLI `FlightCareer.Replay`. Não testado no MSFS.

## Sincronização com o simulador (AIRSIM-1)
- **Lógica e persistência:** `Domain/Airports/Sim/` (`SimAirportListItem`, `SimRunwayData`, `SimAirportDetail`, `SimSurface`, `SimAirportFingerprint`, `SimAirportDiff`, `SimAirportRecordBuilder`), entidades `SimSyncState` e `SimAirportQueueItem`, e serviço de persistência `Infrastructure/Services/SimAirportSyncStore`.
- **Campos novos em Airport:** `Source` ("Sim", "LittleNavmap", "Csv"), `SimRegion`, `SimFingerprint` e `SimCheckedUtc`.
- **Ainda não ligado (AIRSIM-2 e AIRSIM-3):** leitura da lista e detalhes via SimConnect (`SimAirportImporter`), enriquecimento de país por coordenadas para aeroportos novos (`CountryLocator`), e garantia nas missões e quadro para filtrar `IsInSimulator`.
