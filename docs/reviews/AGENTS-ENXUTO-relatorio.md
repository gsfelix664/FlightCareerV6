# AGENTS-ENXUTO — enxugar a seção 7 do AGENTS.md

**Branch:** docs/agents-enxuto · **Commit:** a ser gerado · **Data:** 2026-11-24

## Plano (antes de codar)
- Analisar a seção 7 atual de `AGENTS.md` (62.053 bytes) e mapear todos os arquivos, classes, regras e calibrações citados.
- Reorganizar o conteúdo por sistemas coesos (13 sistemas temáticos em vez de divisão por tarefas históricas).
- Remover números de calibração que já estão nos JSONs, algoritmos detalhados, repetições de IDs de tarefa e fórmulas matemáticas descritas no código.
- Garantir preservação das regras fundamentais, decisões ativas do usuário, contratos entre camadas e pendências.
- Manter seções 1 a 6 e 8 estritamente idênticas byte a byte.
- Reduzir o tamanho da seção 7 para menos de 22 KB (medido com awk).

## O que foi feito
- `AGENTS.md`: reescrita da seção 7 ("Estado atual dos sistemas"), consolidando 18 subtítulos históricos dispersos em 13 sistemas objetivos (`Desgaste e falhas`, `Peças`, `Manutenção e Hangar`, `Panes persistentes`, `Voo e integridade`, `Combustível`, `Economia e dificuldade`, `XP e fases`, `Missões e quadro`, `Aeroportos e tiers`, `Limites por aeronave`, `Sensores`, `HUD`).
- `docs/CHANGELOG.md`: adicionada entrada de conclusão `## 2026-11-24 - AGENTS.md › 7 enxuto (AGENTS-ENXUTO) (Gemini)`.

## Métricas de enxugamento
- **Tamanho antes:** 62.053 bytes (~60,6 KB)
- **Tamanho depois:** 20.669 bytes (~20,2 KB)
- **Redução:** -41.384 bytes (-66,7%), cumprindo a meta de $\le 22\text{ KB}$ (22.528 bytes).
- **Verificação das demais seções:** §§ 1–6 e § 8 testadas e confirmadas 100% idênticas byte a byte.

## Sistemas cobertos (13 sistemas)
1. **Desgaste e falhas:** 9 componentes (`AircraftComponents`), motores de desgaste/falha/eventos, atuação no simulador (`SimFailureActuator`, `TurbineDegradationActuator`), ponte legada (`LegacyConditionBridge`), calibração em `component_catalog.json`.
2. **Peças:** ciclo de vida e estresse por peça (`PartWearEngine`, `PartFlightTracker`, `PartEventDeriver`), 29 métricas unificadas, agregação de barras (`SyncComponentsFromParts`), reconciliação inicial, peças de ciclo, iluminação/MEL, pré-voo sem desgaste contínuo, persistência (`PartStateWriter`) e provisionamento (`AircraftPartProvisioner`).
3. **Manutenção e Hangar:** contexto curto com `IDbContextFactory`, inspeções (12h, 25h anual que interdita, 125h, TBO não interdita) com fator 4×, oficina em tempo real (`WorkshopDowntime`) com bloqueio de voo e serviço expresso, precificação por classe econômica (`PartPricing`), lista de peças com ícones de estado (`✓`, `◐`, `⚠`, `✖`, `?`) sem porcentagens expostas.
4. **Panes persistentes:** discrepâncias entre voos (`AircraftDefect`, `DefectCatalog`, `PersistentFailureService`), NO-GO vs DIFERIDA, atuação e watchdog contínuos sem mascarar comandos, modo vigia fora de voo, reparo desacoplado da saúde física.
5. **Voo e integridade:** detecção de toque (`TouchdownDetector`), perda total (`CrashPolicy`), anti-cheat de slew e congelamentos (`SlewMonitor`), qualidade e conforto de passageiros (`FlightQualityService`), alertas em overlay (`AlertOverlayService`).
6. **Combustível:** abastecimento manual no sim pelo piloto (Decisão 2026-11-21) sem gravação nos tanques pelo addon, conferência com tolerância $\max(1\text{ gal}, 2\%)$, integridade contra `UNLIMITED FUEL` e reabastecimento em voo, consumo medido por `GENERAL ENG FUEL USED SINCE START:1..4`, caminhão do sim em modo vigia.
7. **Economia e dificuldade:** multiplicadores de dificuldade (`DifficultyEconomy` Fácil, Médio, Difícil), tarifas em `mission_pay.json`, taxas de aeroporto/solo (`airport_fees.json`), seguro aeronáutico com negação de sinistro por anual vencida/reprovada, conversão de cabine (`CabinConversion`), mercado de usados e relógio de carreira (`CareerClock`).
8. **XP e fases:** cálculo desacoplado (`XpCalculator.Compute`), penalidade de 10% por partida automática, níveis contínuos sem teto ($300 \times (L - 1)^2$), 7 fases da carreira com requisitos em `career_phases.json`, telemetria ambiental em `SimTelemetry`.
9. **Missões e quadro:** motor unificado `MissionBoardCore`, catálogo `mission_catalog.json` v2 com 69 tipos e perfis físicos (`MinRunwayFt`, `Paved`), dados de POIs em `Data/places/` (`PlacesDataService`), rastreadores dedicados de voo panorâmico, TGL, levantamento SAE, paraquedismo, multi-etapas, STOL, voo de cheque e translado restrito, avaliação pura em `MissionCompletion.Evaluate`, textos de RP em `mission_texts.json` e marcadores de briefing.
10. **Aeroportos e tiers:** classificação por pontuação combinada (`AirportTierClassifier`, `airport_tier_rules.json`), recálculo automático em lote por versão (`TierRulesVersion`), perfis e etiquetas em `airport_profiles.json`, fronteiras e resolução de países (`CountryLocator`).
11. **Limites por aeronave:** perfis em `Infrastructure/Data/aircraft/`, precedência estrita (POH → SimVar → Cfg → NativeOnset → ClassDefault), features especiais (`noElectrical`, `noFlaps`, `noAvionicsMaster`, `convertibleCabin`), ferramentas de calibração em solo.
12. **Sensores:** catálogo (`sensor_registry.json`, `context_registry.json`), expansão de layout em múltiplos de 256 preenchidos com `SIMULATION RATE`, descarte dinâmico de SimVars inválidas (`NAME_UNRECOGNIZED`), gravador HUD em `%LocalAppData%\FlightCareer\sensor-logs\`, sonda `SimVarProbe`.
13. **HUD:** overlay tático (`DebugOverlayViewModel`, `FlightMapControl`), rota e enquadramento automático, substituição de barras numéricas por ícones discretos de estado para evitar vazamento de saúde real oculta.

## Contradições encontradas no texto original
1. **Fonte das missões:** No bloco "Outros sistemas", afirmava-se que as missões procedurais dependiam de `MissionRules` e `hubs_data.json`. Nos blocos posteriores (E2A, TIER-A, CATALOGO-MISSOES), `MissionRules` virou apenas um adaptador fino sobre `mission_catalog.json` v2 (69 tipos), `MissionBoardCore` tornou-se o gerador unificado e `hubs_data.json` foi absorvido pelas bases de perfis e instrução. O texto enxuto adotou o modelo mais recente e correto (`mission_catalog.json` + `MissionBoardCore`).
2. **Abastecimento de combustível:** Menções antigas (Decisão 2026-11-16) citavam gravação direta nos tanques. A Decisão final de 2026-11-21 determinou abastecimento manual pelo piloto no MSFS sem gravação pelo addon. O texto enxuto unificou a regra sob a decisão de 2026-11-21.
3. **Saúde no HUD:** Trechos antigos mencionavam que barras numéricas legadas eram exibidas no HUD Overlay. O bloco `HUD-ICONES` removeu as porcentagens numéricas e adotou ícones vetoriais resumidos sem vazamento da saúde real. O texto enxuto manteve o comportamento do `HUD-ICONES`.
4. **Fonte da saúde de componentes:** No início do texto anterior, as 9 barras eram tratadas como entidades ativas diretamente desgastadas pelo `WearEngine`. Com a introdução de peças (`PartComponentBridge.HasParts`), as barras passaram a ser agregadas das peças físicas e o `WearEngine` legado fica inativo. O texto enxuto esclareceu que peças são a fonte de verdade para aeronaves que as possuem.

## Arquivos e classes citados antes que não são mais citados

### Arquivos e caminhos (29 itens)
- `AppDbContext.cs`, `BriefingViewModel.cs`, `Mission.cs`, `MissionBoardCore.cs`, `MissionGenerator.cs`, `MissionPayTable.cs`, `XpRules.cs`: as classes e serviços continuam citados normalmente, apenas a extensão `.cs` explícita foi suprimida para brevidade.
- `Domain/Missions/ContractMarkerBuilder.cs`, `Domain/Missions/MissionCatalog.cs`, `Domain/Missions/MissionTextCatalog.cs`, `Domain/Missions/OverflyWaypointCodec.cs`: classes mantidas sob o namespace/diretório consolidado `Domain/Missions/`.
- `aircrafts.csv`, `calibration_script.json`, `poh_values.csv`, `report.csv`, `strings.csv`, `values.csv`, `tier-report.csv`, `tier-report.md`, `title_map.csv`: artefatos intermediários de desenvolvimento ou de saída temporária de scripts Python.
- `tools/aircraft_limits/merge_limits.py`, `tools/analyze_sensor_log.py`, `tools/places/airport_profiles.py`, `tools/simvar_probe/README.md`: ferramentas auxiliares de desenvolvimento e documentação de suporte.
- `docs/plans/cobertura-regras-por-aviao.md`, `docs/plans/sensor-validation/ROTEIRO.md`, `native-wear-C152.md`, `native-wear-mapping.md`: documentos de planejamento e relatórios históricos já consolidados no código.
- `hubs_data.json`: base absorvida e substituída pelo `airport_profiles.json` e `training_aerodromes.json`.

### Classes e identificadores C# (145 itens)
- **Métodos e propriedades internas suprimidos:** `ActivationMode`, `AddToDef`, `AirborneMinutes`, `AircraftLoaded`, `AirportAdjustments`, `Approaches`, `AugmentOriginTags`, `CalculateNewExpirationDate`, `CareerClock.Advance`, `CareerClock.AdvanceFlight`, `CareerSimulator`, `CargaMax`, `CargoParking`, `ClearFailure`, `ClearSimFailures`, `ContractData`, `Crashed`, `CreateMissionFromSubCategory`, `CreateMissionFromSubCategoryInMemory`, `CsvType`, `DayOnly`, `DispatcherTimer`, `DisplayState`, `DistanceNm`, `EngineLimitsConfig`, `EnrichFromCsvAsync`, `EnsureSchemaUpdatedAsync`, `AppDbContext.EnsureSchemaUpdatedAsync`, `Expander`, `FilterCandidates`, `FilterOnlyCompatiblePayload`, `FinancialTransaction`, `FindPois`, `FlapOverSpeed`, `FlightLoaded`, `FlightMonitor.ProcessFuelTruckDelivery`, `FlightQualitySample`, `FlightService.ProcessFlightFinishAsync`, `FlightTelemetryLogger`, `ForceComplete`, `FuelPriceService.GetCurrentPrice`, `FuzzRunSummary`, `FuzzTestDefaults.WithTestElectricalIndices`, `GameLike`, `Gates`, `GenerateBoard`, `GenerateMissionsAsync`, `GenerateMissionsInMemory`, `Get`, `Green`, `GroupLifeInfo`, `HANDLING`, `HangarIcons.ForDisplayState`, `HangarPalette.ForDisplayState`, `HangarViewModel.RunPaidServiceAsync`, `HasTower`, `HubDataService.IsInstructionHub`, `IPlacesDataService`, `IlsEnds`, `InsuranceService.IsInsured`, `LANDING`, `LegsCompleted`, `LimitCandidates`, `LimitKeys`, `LinhaAutoHubMaxTier`, `LongestRunwayFt`, `LongestRunwayPaved`, `MainViewModel.AcceptSelectedMissionsAsync`, `Mission.OverflyWaypoints`, `MissionBoardCategory`, `MissionBoardCore.WithDerivedTags`, `MissionDefinition`, `MissionDestinationRule`, `MissionDisplayItem`, `MissionDisplayItem.XpDisplay`, `MissionGenerator.CalculateReward`, `MissionPayTable.Defaults`, `MissionProfileDefinition`, `NoGoNight`, `Normalize`, `OnAlert`, `OnDefectRecorded`, `OnObservedEvent`, `OverflyPoint`, `OverflyWaypoints`, `PartDefinition.CalendarLifeMonths`, `PartDisplayState.Worn`, `PartEventDeriver.ProducedMetrics`, `PartFailureEngine`, `PartFlightSession.ObservePreflight`, `PartFlightTracker.ObservePreflight`, `PartHangarView.GoodAbovePct`, `PartHangarView.Summarize`, `PartHealthBands`, `PartInspectionFlags.Evaluate`, `PartMigrationService.ProvisionPurchasedAircraft`, `PaxMax`, `PickProfile`, `Pilot.Level`, `PilotAircraftWrapper.IsInsured`, `ProcedurePenalties.ApplyAutoStartPenalty`, `ProcessFlightFinishAsync`, `ProgressBar`, `REALISM`, `RealCycles`, `RealHours`, `RequireNoRefuel`, `RequiredTouchAndGoes`, `ResolveMissingCountriesAsync`, `RuleCoverageService`, `RunwaySurface.IsUnpaved`, `STRING256`, `ScenicAerodromeInfo`, `ScenicPoint`, `ScheduledService`, `SensorLayout.Build`, `SetAlternatorConnected`, `SimConnectService.SetPersistentToggles`, `SubId`, `SyntheticFlapSelector`, `TITLE`, `ThemedDialog.ConfirmService`, `TierReporter`, `TierScore`, `Unknown`, `UsageFailureModel`, `WorkshopDescription`, `WriteFlapSurfacePercent`, `XpRules`, `Yellow`.
- **Classes de teste/harness removidas do resumo:** `SyntheticFlightFuzzTests`, `SyntheticFlightFuzzAllTests`, `SyntheticGeneratorTemperatureAndFlapTests`, `SyntheticGroundTruth`, `WearRuleMetricCoverageTests`.
- **Classes/enums legados removidos ou agregados:** `AirframeCondition`, `AvionicsCondition`, `EngineCondition`, `MagnetosCondition`, `OilCondition`, `TiresCondition` (agrupadas sob o conceito de colunas legadas de `PilotAircraft`), `PilotCategory` (substituída por `CareerPhase`), `AircraftSizeClass`, `AircraftSizeClassifier` (resumidos em `GroundServiceCalculator`).

## Critérios de aceite
- [x] `AGENTS.md` › 7 ≤ 22 KB (20.669 bytes medidos com `awk '/^## 7\./{f=1} /^## 8\./{f=0} f' AGENTS.md | wc -c`).
- [x] §§ 1–6 e 8 byte a byte iguais (verificado via script de comparação e `git diff`).
- [x] Relatório completo contendo tamanho antes/depois, lista dos 13 sistemas cobertos, lista dos arquivos/classes não mais citados e contradições encontradas.
- [x] Entrada no `docs/CHANGELOG.md`: `## 2026-11-24 - AGENTS.md › 7 enxuto (AGENTS-ENXUTO) (Gemini)`.
- [x] Não rodar build nem testes (apenas alteração documental).
- [x] Um commit: `docs(agents): enxuga o estado dos sistemas no AGENTS (AGENTS-ENXUTO)`.

## Build e testes
- Conforme instrução expressa no critério 4 da especificação ("Não rodar build nem testes (só documentação)"), build e testes foram dispensados por se tratar de modificação estritamente em documentação markdown.

## Não testado
- N/A (tarefa exclusivamente documental).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. Todas as decisões de corte seguiram rigorosamente as regras da especificação.
