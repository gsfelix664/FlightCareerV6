# Aeroportos e tiers

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Classifica aeroportos em tiers (1 a 4), enriquece dados de infraestrutura, resolve fronteiras e gerencia perfis de tráfego.
- **Onde está:** `Domain/Airports/` (`AirportTierClassifier`, `AirportTierRules`), `Domain/Missions/AirportProfileCatalog`, `Domain/Geo/CountryLocator`, `Infrastructure/Data/airport_tier_rules.json`, `Data/places/airport_profiles.json`, `DataSeeder`.
- **Regras e integridade:**
  - Classificação por pontuação combinada (`AirportTierClassifier`): cortes baseados em infraestrutura real (Little Navmap, OurAirports e etiquetas curadas). Pistas não pavimentadas têm teto de Tier 3.
  - Recálculo automático em lote no `DataSeeder.EnsureTiersUpToDateAsync` acionado por mudança de versão da regra (`TierRulesVersion`).
  - Etiquetas e vocações de aeroporto (`AirportProfileCatalog`): tags de malha aérea (`linha`, `linha_auto:hub`, `carga`, `hospital`, etc.) determinam a geração e os destinos de voos de linha regular e missões especiais. Jatos de linha exigem hubs conectados.
  - Resolução geográfica de países (`CountryLocator`): mapeia coordenadas geográficas contra fronteiras vetoriais (`country_borders.json`) com aproximação por aeroportos vizinhos (`AirportSpatialGrid`) para áreas costeiras e ilhas.
  - Relatório e auditoria via comando `tier-report` no CLI `FlightCareer.Replay`. Não testado no MSFS.
