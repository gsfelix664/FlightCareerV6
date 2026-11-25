# Estado dos sistemas

Antes de mexer num sistema, leia o arquivo dele; ao mudar o estado, atualize o arquivo, não o `AGENTS.md`.

| Sistema | Arquivo | Classes/pastas-chave |
|---|---|---|
| Desgaste e falhas | `docs/sistemas/desgaste-falhas.md` | `Domain/Components/`, `ComponentFlightSession`, `SimFailureActuator` |
| Peças | `docs/sistemas/pecas.md` | `Domain/Parts/`, `PartFlightSession`, `PartStateWriter` |
| Manutenção e Hangar | `docs/sistemas/manutencao-hangar.md` | `HangarViewModel`, `WorkshopDowntime`, `PartPricing` |
| Panes persistentes | `docs/sistemas/panes-persistentes.md` | `AircraftDefect`, `DefectCatalog`, `PersistentFailureService` |
| Voo e integridade | `docs/sistemas/voo-integridade.md` | `FlightMonitor`, `TouchdownDetector`, `SlewMonitor` |
| Combustível | `docs/sistemas/combustivel.md` | `FuelUsageAccumulator`, `FuelIntegrityMonitor`, `FuelPriceService` |
| Economia e dificuldade | `docs/sistemas/economia-dificuldade.md` | `Domain/Economy/`, `EconomyServices`, `MarketSimulationService` |
| XP e fases | `docs/sistemas/xp-fases.md` | `XpCalculator`, `PilotLevel`, `CareerPhaseCatalog` |
| Missões e quadro | `docs/sistemas/missoes-quadro.md` | `MissionBoardCore`, `Domain/Missions/`, `PlacesDataService` |
| Aeroportos e tiers | `docs/sistemas/aeroportos-tiers.md` | `AirportTierClassifier`, `AirportProfileCatalog`, `CountryLocator` |
| Limites por aeronave | `docs/sistemas/limites-aeronave.md` | `AircraftLimitResolver`, `Data/aircraft/`, `AircraftLimitSeeder` |
| Sensores | `docs/sistemas/sensores.md` | `Domain/Sensors/`, `SensorFrameReader`, `FlightCareer.SimVarProbe` |
| HUD | `docs/sistemas/hud.md` | `DebugOverlayViewModel`, `FlightMapControl`, `HudHealthSummary` |

Histórico detalhado de cada tarefa: `docs/CHANGELOG.md` (busque pelo ID entre parênteses).
