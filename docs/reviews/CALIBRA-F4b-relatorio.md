# CALIBRA-F4b — Calibração do usuário em todas as peças, multiplicador de preço por classe, TBO e textos

**Branch:** feat/calibra-f4 · **Commit:** pendente · **Data:** 2026-11-19

## Plano (antes de codar)
- Mover a lógica de resolução de classes de aeronaves (A–J) para `Domain/Economy/AircraftEconomyClass.cs` com método `Resolve` e multiplicadores por classe (`GetPartPriceMultiplier`).
- Adicionar `partPriceMultiplier` por classe em `Infrastructure/Data/economy_targets.json`.
- Atualizar `PartPricing.cs` e `PartEconomySimulator.cs` para calcular preço de peça escalável como `partPrice × multiplicador da classe`, removendo uso de `repairCostPctOfAircraft` e `GetClassPartPriceCap`.
- Atualizar `CareerSimulator.cs` e `CareerSimulatorTests.cs` com os preços ÷ 4 e intervalos de inspeção 12/25/125 h.
- Atualizar `PartCatalog.cs` (`PartItemDto.LifeHours` como `double?`) e aplicar a calibração do usuário (153 peças de `calibracao-pecas-usuario-2026-11-19.json`) em `parts_catalog.json` (`engine.air_intake_filter` restrito a pistão).
- Unificar TBO: pistão 500 h, turbina 900 h em `component_catalog.json`, `PilotAircraft`, `MarketSimulationService`, `AppDbContext` (migração SQL) e testes.
- Atualizar limites de POH em `diamond-da62.json` (`vle` = 156 kt), `beechcraft-king-air.json` (`vne` = 263 kt) e `tools/aircraft_limits/poh_values.csv`.
- Regenerar `docs/plans/lista-pecas-hangar.md`.
- Escrever testes unitários requeridos (`PartPricing_MultiplicadorPorClasse`, `Catalogo_CalibracaoDoUsuario`, `Tbo_Pistao500_Turbina900` e ajuste em `Catalogo_Motor_CalibracaoDoUsuario`).
- Rodar `dotnet build`, `dotnet test` e `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 5`.
- Registrar no `CHANGELOG.md` e `AGENTS.md`.

## O que foi feito
- `src/FlightCareer.Domain/Economy/AircraftEconomyClass.cs`: criada classe no domínio com método de classificação de aeronaves `Resolve` (classes A a J) e mapeamento `GetPartPriceMultiplier` (A 0,75 · B 1,25 · C 1,25 · D 1,25 · E 1,5 · F 1,75 · G 1,75 · H 2,5 · I 3,25 · J 5,5).
- `src/FlightCareer.Infrastructure/Data/economy_targets.json`: adicionado `partPriceMultiplier` em cada classe sem tocar em `minCostPerHour`/`maxCostPerHour`.
- `src/FlightCareer.Domain/Parts/PartPricing.cs`: precificação de peças agora calcula `partPrice × multiplicador da classe` quando `PriceScalesWithAircraft == true`, e `partPrice` base quando fixo. Sobrecargas aceitam tanto a chave da classe ("A" a "J") quanto o multiplicador direto (`double`). Removido o uso ativo de `repairCostPctOfAircraft` e `GetClassPartPriceCap` (mantido como legado).
- `src/FlightCareer.Domain/Parts/PartEconomySimulator.cs`: ajustado para simular o custo operacional utilizando o multiplicador da classe econômica.
- `src/FlightCareer.Domain/Parts/PartCatalog.cs`: ajustado `PartItemDto.LifeHours` para `double?` e validação para suportar peças que possuem vida apenas por ciclos ou apenas calendárica (ex.: `oxygen.crew_cylinder`).
- `src/FlightCareer.Infrastructure/Data/parts_catalog.json`: atualizadas todas as 153 peças com os dados de `calibracao-pecas-usuario-2026-11-19.json` (incluindo `lifeHours`, `lifeCycles`, `calendarLifeMonths`, `partPrice`, `priceScalesWithAircraft`, `laborHours`, `downtimeHours`, `blocksFlightWhenBroken`, `visibleOnWalkaround`, `failureEffect`, `lifeSource` = "calibração do usuário 2026-11-19 (real ÷ 4)", e `engine.air_intake_filter` restrito a `{ "all": ["piston"] }`).
- `src/FlightCareer.Infrastructure/Data/component_catalog.json`: TBO de pistão atualizado de 450 h para 500 h (`lifeHours.Piston = 500`).
- `src/FlightCareer.Domain/Entities/PilotAircraft.cs`: default de `EngineTboRemainingHours` atualizado para 500 h.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: migração de banco atualizada para limitar `EngineTboRemainingHours > 500.0` para 500.0 h em frotas existentes de pistão.
- `src/FlightCareer.Infrastructure/Services/MarketSimulationService.cs`, `src/FlightCareer.UI/Models/MarketOffer.cs`, `src/FlightCareer.UI/ViewModels/CreatePilotViewModel.cs`: TBO padrão alinhado para 500 h pistão / 900 h turbina.
- `src/FlightCareer.Application/Simulation/CareerSimulator.cs`: intervalos de manutenção programada alinhados para 12 h (troca de óleo), 25 h (anual) e 125 h (ignição), e cálculo de substituição de peças passando a classe econômica para `PartPricing.CalculatePartPrice`.
- `src/FlightCareer.Infrastructure/Data/aircraft/diamond-da62.json`: `vle` atualizado para 156.0 kt, AFM usuário 2026-11-19, `dataVersion` 8.
- `src/FlightCareer.Infrastructure/Data/aircraft/beechcraft-king-air.json`: `vne` atualizado para 263.0 kt, AFM usuário 2026-11-19, `dataVersion` 8.
- `tools/aircraft_limits/poh_values.csv`: linhas correspondentes de DA62 e King Air atualizadas com os novos limites e referências.
- `docs/plans/lista-pecas-hangar.md`: regenerado a partir do novo catálogo de 153 peças.
- `src/FlightCareer.Tests/`:
  - `PartEconomyBalanceTests.cs`: atualizado para usar `AircraftEconomyClass.Resolve` e passar `classKey` para o simulador; ambos os testes verdes com faixas originais.
  - `CalibraF4Tests.cs`: novos testes `PartPricing_MultiplicadorPorClasse`, `Tbo_Pistao500_Turbina900` e atualização de `Catalogo_VidasComprimidasPor4`.
  - `PartCatalogTests.cs`: novo teste `Catalogo_CalibracaoDoUsuario` conferindo 5 peças de grupos distintos (`airframe.flaps`, `avionics.adc`, `oil.lubricant`, `gear.main_struts`, `lights.landing`) e atualização de `Catalogo_Motor_CalibracaoDoUsuario` e `CalendarLife_StationaryAgingPartsConfigured`.
  - `CareerSimulatorTests.cs`: atualizado `MaintenancePricing_MesmosValoresDaUI` ($212,50 / $625 / $362,50 / retífica 5% mín. $6.250) e `CareerSim_InspecaoVencida_BotFazInspecao`.
  - `LightingPartsTests.cs`, `VisibilityAndMigrationTests.cs`, `PartWearEngineTests.cs`, `PartBridgeAndMigrationTests.cs`: atualizados para os novos valores de catálogo/TBO.

## Tabela de Custo/h Simulado por Classe (Metas Econômicas Originais)

| Classe | Exemplo | Custo/h Simulado | Faixa Alvo ($/h) | Multiplicador | Status |
|---|---|---|---|---|---|
| **A** | C152, J-3 Cub, DR400 | $103,32/h | $30 - $120/h | 0,75× | **OK** |
| **B** | Bonanza G36, SR22, C172SP | $118,68/h | $80 - $250/h | 1,25× | **OK** |
| **C** | SR22T, C400 Corvalis, C207T | $126,28/h | $80 - $250/h | 1,25× | **OK** |
| **D** | DA40 NG, DA62 | $111,80/h | $80 - $250/h | 1,25× | **OK** |
| **E** | Baron G58, C404 Titan, DA62 | $205,84/h | $150 - $550/h | 1,50× | **OK** |
| **F** | TBM 930, PC-12, C208B | $425,36/h | $250 - $900/h | 1,75× | **OK** |
| **G** | King Air 350, ATR 42/72, DHC-6, ES30 | $767,22/h | $450 - $2500/h | 1,75× | **OK** |
| **H** | CJ4, Longitude, PC-24 | $1135,44/h | $800 - $2000/h | 2,50× | **OK** |
| **I** | A320neo, 737 MAX 8, A321 | $1511,33/h | $1000 - $2500/h | 3,25× | **OK** |
| **J** | 747-8i, A330-200, A330-300, 787-10 | $5297,60/h | $2500 - $6000/h | 5,50× | **OK** |

*Todas as 46 aeronaves do catálogo também aprovadas 100% dentro das faixas-alvo de suas respectivas classes (`EconomySimulation_All46CapturedAircraft_FallWithinTargetBands`).*

## Critérios de aceite
- [x] Build 0 erros, 0 novos avisos
- [x] Testes xUnit 100% aprovados (`dotnet test`: 624 aprovados, 0 falhas)
- [x] `PartEconomyBalanceTests` (ambos) 100% verdes com as faixas originais de `economy_targets.json`
- [x] Fuzz testing com voos sintéticos (`dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 5`): 230 voos em 46 aeronaves, 230 aprovados (100%), 0 falhas
- [x] Limites de POH atualizados: DA62 (`vle` = 156 kt) e King Air (`vne` = 263 kt) com `dataVersion` 8 e CSV sincronizado
- [x] TBO unificado em 500 h pistão / 900 h turbina
- [x] Inspeções em 12 h / 25 h / 125 h e preços ÷ 4 ($212,50 / $625,00 / $362,50 / retífica 5% mín $6.250)
- [x] `docs/plans/lista-pecas-hangar.md` regenerado
- [x] Entradas no `CHANGELOG.md` e `AGENTS.md` atualizadas

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos
- `dotnet test src/FlightCareer.Tests`: 624 aprovados, 0 falhas (tempo: ~36 s)
  - Novos testes:
    - `PartPricing_MultiplicadorPorClasse`
    - `Catalogo_CalibracaoDoUsuario`
    - `Tbo_Pistao500_Turbina900`
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 5`: 230 voos, 230 aprovados (100%), 0 falhas

## Não testado
- Renderização visual no WPF em execução com simulador MSFS ativo (depende do MSFS 2024 conectado).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Todos os números de calibração, limites de POH e multiplicadores de classe bateram 100% com a especificação sem divergências.
