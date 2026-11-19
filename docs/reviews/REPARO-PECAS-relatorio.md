# REPARO-PECAS — Reparo no Hangar troca peças inteiras (preço cheio)

**Branch:** feat/reparo-pecas · **Commit:** 03f9246 · **Data:** 2026-11-19

## Plano (antes de codar)
- Criar `Domain/Parts/PartReplacementPlanner.cs` com tipos `PartReplacementItem` e `PartReplacementPlan`, limites (`ReplaceBelowHealthPct = 80.0`), métodos `Plan` e `Apply`/`ApplyToParts`.
- Atualizar `ComponentRowViewModel` para integrar `ReplacementPlan`: exibir `"Trocar N peças · $X"` ou `"Em dia"` quando há peças e preservar o comportamento legado quando não há.
- Atualizar `PilotAircraftWrapper` para montar planos de substituição por grupo de componente a partir de `Aircraft.Parts` e `PartCatalog`.
- Atualizar `HangarViewModel` para listar as peças e motivos na confirmação de reparo, aplicar a substituição a preço cheio + mão de obra, recalcular a barra pelo agregado (`SyncComponentsFromParts`), aplicar parada da oficina e manter overhaul/inspeções consistentes.
- Criar suíte de testes unitários xUnit cobrindo todos os cenários exigidos e validar com build, `dotnet test` e fuzz testing em 46 modelos.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartReplacementPlanner.cs`:
  - Implementada a lógica pura de substituição física de peças inteiras.
  - Peças entram no plano se: `Health < 80.0`, `IsBroken`, ou marcadas em inspeção (`PartInspectionFlags.Evaluate(...)`).
  - Motivo legível gerado conforme o caso ("Quebrada", "Saúde X %", "Vida: X / Y h", etc.).
  - Custo consolidado somando preço integral da peça (com multiplicador de classe) + mão de obra por instância.
  - Parada na oficina calculada como a maior entre as peças do plano (trabalho paralelo).
  - Método `Apply` e helpers `ApplyToParts`/`ApplyToPart` para restaurar saúde a 100%, zerar horas/ciclos, atualizar contadores `LastReplaced*`/`LastShopService*` e limpar flags.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`:
  - Adicionados `ReplacementPlan` e método `SetReplacementPlan`.
  - Propriedades `RepairCost`, `CanRepair` e `RepairButtonText` passam a refletir o plano de peças nas aeronaves com peças ("Trocar 1 peça · $X", "Trocar N peças · $X" ou "Em dia"), mantendo comportamento legado quando não há peças.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Adicionado `UpdateReplacementPlans()` para computar o plano de cada componente com base nas peças instaladas e relógio de carreira.
  - Chamado no construtor, em `SetFlaggedParts` e em `Refresh()`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`:
  - `LoadFleetAsync` atualizado para carregar `Parts` da aeronave no `Include`.
  - Injetado `AircraftLimitResolver? limitResolver` para resolução de classe econômica no wrapper.
  - `RepairComponent` e `RepairAll` adaptados: listam na confirmação as peças a trocar com motivo e custo individual; aplicam a troca nas peças; sincronizam a barra pelo agregado (`SyncComponentsFromParts`), garantindo que barra = agregado (impedindo cura gratuita no voo seguinte); avançam o relógio de carreira pelo downtime das peças.
  - `PerformOverhaul`, `Perform50hInspection`, `Perform100hInspection` e `Perform500hInspection` adaptados para aplicar a troca física de peças inteiras aos grupos de motor, seção quente, óleo e ignição mantendo os valores tabelados dos serviços.
- `src/FlightCareer.Tests/Parts/PartReplacementPlannerTests.cs`:
  - Criados os 9 testes exigidos pela especificação:
    - `Planner_Freio98_SemPecaGasta_EmDia`
    - `Planner_PecaAbaixo80_TrocaPrecoCheio`
    - `Planner_PecaVidaVencida_Troca`
    - `Planner_PecaQuebrada_Troca`
    - `Planner_PorMotor_ContaInstancias`
    - `Planner_ParadaEhAMaior`
    - `Apply_ZeraHorasECiclosESaude100`
    - `Hangar_BarraIgualAgregadoAposTroca`
    - `Overhaul_TrocaPecasDoMotor`
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação atualizada.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx` (0 erros, 15 warnings de dependências conhecidas).
- [x] `dotnet test` 100 % — 639 aprovados, 0 falhas (35 s).
- [x] `fuzz --aircraft all --runs 5` — 230 voos em 46 aeronaves aprovados com 100 % e 0 falhas (99,16 s).
- [x] CHANGELOG e `AGENTS.md` › 7 Hangar atualizados.
- [x] Commits na branch `feat/reparo-pecas`. Sem push.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 639 aprovados, 0 falhas (novos: 9 testes em `PartReplacementPlannerTests`)
- Fuzz testing CLI: 46/46 aeronaves, 230/230 voos aprovados (0 falhas, 0 limites ausentes)

## Não testado
- Não testado na tela do aplicativo nem no MSFS (modo não interativo).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. O comportamento para aeronaves sem peças foi mantido 100% idêntico ao legado e aeronaves com peças agora trocam apenas peças físicas inteiras a preço cheio + mão de obra, recalculando as barras diretamente pelo agregado pós-serviço.
