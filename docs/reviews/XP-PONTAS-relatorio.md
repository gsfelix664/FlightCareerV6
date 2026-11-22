# XP-PONTAS — XP estimado no quadro, infrações, pista difícil no pagamento e usado coerente

**Branch:** feat/xp-pontas · **Commit:** <pendente> · **Data:** 2026-11-22

## Plano (antes de codar)
- 1. Implementar função pura `XpCalculator.Estimate(...)` com estimativa de minutos no ar, 1 perna (2 se circuito/TGL), M_Tipo, realismo 1,0, qualidade 1,0, bônus de pista e prazo no prazo (+15), conectando a `MissionDisplayItem.XpDisplay` exibindo "≈ N XP".
- 2. Criar `RunwaySurface` em `Domain/Entities` com `IsHard` e `IsUnpaved`, substituindo as duplicações de código no `FlightMonitor` e no `DataSeeder`.
- 3. Criar `FlightInfractionTracker` em `Domain/Xp` para rastrear bordas de estol no ar (exceto instrução), bordas de IAS > 250 kt abaixo de 10.000 ft, faróis desligados na decolagem e no toque final, e Vne; anexar linhas explicativas no `XpBreakdown.Lines`.
- 4. Adicionar SimVars `STALL WARNING`, `LIGHT LANDING` e `LIGHT TAXI` no final de `SimTelemetry` e `SimConnectService` com `AddToDef`.
- 5. Integrar detecção contínua no `FlightMonitor` e alimentar `VneOrStallInfractions` e `LightsOrSpeedInfractions`.
- 6. Atualizar `CalculateReward` no `MissionGenerator` para receber `destRunway`/`isDifficultRunway` (não pavimentada ou < 2.500 ft = +25% bush bonus), com cache `_runwayCache` por aeroporto e fallback seguro no tier 4.
- 7. Ajustar `PartMigrationService.ProvisionPurchasedAircraft` para calcular horas por `clamp((100 - saúde) / 30 × vidaHoras, 0, horas de célula)`, ciclos por `clamp((100 - saúde) / 30 × vidaCiclos, 0, pousos estimados)`, limitar peças do motor ao TSMO e preencher `KnownHealth = health`.
- 8. Adicionar testes unitários xUnit cobrindo cada requisito e rodar fuzz `--runs 10`.

## O que foi feito
- `src/FlightCareer.Domain/Entities/RunwaySurface.cs`: classe estática pura definindo `IsHard` e `IsUnpaved` a partir da lista oficial de superfícies duras (`ASPH`, `CON`, `CONC`, `HARD`, `BIT`, `PEM`, `ASP`).
- `src/FlightCareer.UI/Services/FlightMonitor.cs` e `src/FlightCareer.Infrastructure/Services/DataSeeder.cs`: duplicações de array de superfícies substituídas por chamadas a `RunwaySurface`.
- `src/FlightCareer.Domain/Xp/XpCalculator.cs`: implementada função pura `Estimate` e inclusão de linhas detalhadas de infrações no resumo do XP (`Lines`).
- `src/FlightCareer.UI/ViewModels/MissionDisplayItem.cs`: propriedade `EstimatedXp` calculada via `XpCalculator.Estimate` e `XpDisplay => $"≈ {EstimatedXp} XP"` exibida na coluna de XP ao lado do pagamento.
- `src/FlightCareer.Domain/Xp/FlightInfractionTracker.cs`: classe desacoplada de rastreamento de infrações com tratamento de bordas de subida, isolamento de voos de instrução para estol, detecção de luzes na decolagem e toque final e contagem de velocidade > 250 kt.
- `src/FlightCareer.Domain/Entities/SimTelemetry.cs`: campos `StallWarning`, `LightLanding` e `LightTaxi` acrescentados no final do struct com propriedades de leitura direta.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: mapeamento das 3 SimVars ao final da definição de `AircraftData` mantendo alinhamento estrito.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: instância de `FlightInfractionTracker` atualizada no loop de telemetria, decolagem e toques/pouso final, repassando os totais para `XpFlightInputs`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: `CalculateReward` recebe dados de pista do destino (`destRunway` / `isDifficultRunway`), aplicando o bônus bush de +25% para pistas não pavimentadas ou < 2.500 ft, com cache em memória `_runwayCache` e fallback retrocompatível no tier 4.
- `src/FlightCareer.Infrastructure/Services/PartMigrationService.cs` e `MarketSimulationService.cs`: provisionamento de aeronaves usadas com horas proporcionais à vida útil (fator 30%), ciclos limitados aos pousos estimados, peças do motor limitadas pelo TSMO anunciado e `KnownHealth = health`.
- `src/FlightCareer.Tests/Xp/XpEstimatorAndInfractionTests.cs`: testes unitários do estimador (182 XP para 60 NM a 100 kt instrução), bordas de estol/instrução, bordas de 250 kt, regras de luzes e classificação de superfícies.
- `src/FlightCareer.Tests/Economy/DifficultRunwayRewardTests.cs`: testes unitários de bônus bush para tier 2 com grama, tier 4 com asfalto longo e fallback sem dados.
- `src/FlightCareer.Tests/Parts/CoherentUsedAircraftProvisioningTests.cs`: testes unitários validando caso real PP-UZJ (7.221 h célula, TSMO 0,8 h, peças do motor <= 0,8 h, KnownHealth igual à saúde).
- Atualizados `AGENTS.md` e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx`.
- [x] `dotnet test` 100 % — 850/850 aprovados com 0 falhas.
- [x] Fuzz `--runs 10` sem regressão — 10/10 voos aprovados sem violação de invariantes.
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `feat(xp): xp estimado no quadro, infracoes, pista dificil no pagamento e usado coerente (XP-PONTAS)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 92 avisos (avisos conhecidos de dependência e de obsolescência de PilotCategory/CompanyTier).
- `dotnet test src/FlightCareer.Tests`: 850 aprovados, 0 falhas (16 novos testes em `XpEstimatorAndInfractionTests`, `DifficultRunwayRewardTests` e `CoherentUsedAircraftProvisioningTests`).
- `dotnet run --project src/FlightCareer.Replay -- fuzz --runs 10`: 10/10 voos sintéticos aprovados (100%), 0 falhas.

## Não testado
- Comportamento no simulador real MSFS 2024 / resposta ao vivo de `STALL WARNING`, `LIGHT LANDING` e `LIGHT TAXI` na aeronave conectada.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. O modelo adotado seguiu à risca as fórmulas e os casos de teste especificados.
