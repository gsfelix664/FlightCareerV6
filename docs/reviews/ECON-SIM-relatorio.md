# ECON-SIM — Simulador de carreira com bots (relatório de balanceamento)

**Branch:** feat/econ-sim · **Commit:** pendente · **Data:** 2026-11-17

## Plano (antes de codar)
- 1. Extrair os custos de inspeções regulamentares (50h: $850, 100h: $2.500, 500h: $1.450) e retífica TBO (20% mín. $25.000) de `PilotAircraftWrapper.cs` para a classe pura `FlightCareer.Domain.Components.MaintenancePricing`, atualizando o wrapper para consumir essa classe sem alterar valores.
- 2. Adicionar sobrecarga determinística em `FuelPriceService.GetCurrentPrice` recebendo data fixa (`DateTime asOfDate`), mantendo a assinatura atual padrão (`DateTime.Today`).
- 3. Ajustar o construtor de `MissionGenerator` para aceitar `IRandomSource?` / `Random?` opcional mantendo comportamento padrão caso nulo.
- 4. Implementar o simulador puro em `src/FlightCareer.Application/Simulation/CareerSimulator.cs` com simulação diária por bot: geração e filtragem de missões, duração, consumo, taxas aeroportuárias (pouso, embarque, handling, navegação via `GroundServiceCalculator`), estacionamento estimado de pernoite, seguro mensal via `EconomyServices`, desgaste e panes via `FailureEngine`/peças (modos completo e `--fast`), política de manutenção com downtime e emissão dos relatórios `flights.csv`, `maintenance.csv`, `daily.csv` e `summary.md`.
- 5. Implementar o subcomando `career` no CLI `FlightCareer.Replay` (`Program.cs`) apontando para `%LocalAppData%/FlightCareer/career-sim/<data-hora>/` (ou pasta configurada com `--out`).
- 6. Criar suíte de testes xUnit `FlightCareer.Tests/Economy/CareerSimulatorTests.cs` cobrindo determinismo com mesma semente, inspeção vencida, conferência de taxas de destino, aeronave parada por pane NO-GO e validação de `MaintenancePricing`.
- 7. Executar build, testes e simulação com geração de `docs/reviews/ECON-SIM-resultado.md`, atualizar documentação (`AGENTS.md`, `CHANGELOG.md`) e gerar relatório final.
