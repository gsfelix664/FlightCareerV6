# HANGAR-AVISOS — aviso de oficina único, preço final nos botões e texto amigável das panes

**Branch:** feat/hangar-avisos · **Commit:** a ser gerado · **Data:** 2026-11-26

## Plano (antes de codar)
- Expandir `DifficultyEconomy` com `ApplyCost` e `CostAdjustmentText`, atualizando `HangarViewModel.RunPaidServiceAsync` e passando a dificuldade ao `PilotAircraftWrapper`.
- Implementar `DefectText` em `FlightCareer.Domain/Services/DefectText.cs` com métodos `Friendly` e `Technical`, cobrindo momentos de voo, catálogo completo de panes e inspeções.
- Criar `DefectRowViewModel` em `FlightCareer.UI/ViewModels/DefectRowViewModel.cs` e expor `DefectRows` no `PilotAircraftWrapper`, atualizando bindings e ToolTip no `HangarView.xaml`.
- Ajustar visibilidade única de oficina no Hangar via `ShowWorkshopBanner` (visível só quando o status card não é "OFICINA") e remover `OriginLabel` da lista de NO-GO em `MainViewModel.cs:957`.
- Adicionar testes de unidade para `DifficultyEconomy` e `DefectText` em `FlightCareer.Tests` e validar compilação e testes da solução.

## O que foi feito
- **`DifficultyEconomy.cs`**: implementados os métodos `double ApplyCost(double cost)` (arredondado em 2 casas caso o multiplicador seja diferente de 1.0) e `static string? CostAdjustmentText(DifficultyMode mode)` (formato `"Desconto da dificuldade Fácil: 20%"` e `"Acréscimo da dificuldade Difícil: 15%"`).
- **`HangarViewModel.cs`**: no método `RunPaidServiceAsync`, adotado `diffEcon.ApplyCost(cost)` e `DifficultyEconomy.CostAdjustmentText`, substituindo a mensagem textual multiplicadora `"custos ×0,80"`. Passagem de `_pilotDifficulty` para `PilotAircraftWrapper`.
- **`DefectText.cs`**: criado serviço puro em `FlightCareer.Domain/Services` para geração de texto amigável (`Friendly`) e técnico (`Technical`) de discrepâncias. O momento do evento mapeia as fases de voo (`VOANDO` → `" em voo"`, `POUSO` → `" no pouso"`, `ACIDENTE` → `" no acidente"`, fases em solo → `" em solo"`), combinando com as 26 bases do catálogo de panes, inspeções ou defeitos desconhecidos. O texto técnico formata origem, fase, causa e descrição, omitindo campos vazios.
- **`DefectRowViewModel.cs`**: criado ViewModel específico para linhas de panes no Hangar, encapsulando `FriendlyText`, `TechnicalText` e o botão formatado `RepairButtonText = $"Reparar · {economy.ApplyCost(defect.RepairCost):C0}"`.
- **`PilotAircraftWrapper.cs`**:
  - Recebe opcionalmente `DifficultyMode difficulty` no fim do construtor e inicializa a economia.
  - Passa a economia para `ComponentRowViewModel`.
  - Expõe `DefectRows` preenchido em `SetOpenDefects`.
  - Exibe valores com `_economy.ApplyCost` em `RepairAllDefectsText`, `RepairAllText`, `Text50h`, `Text100h`, `Text500h`, `TextOverhaul`, `TextToggleConfiguration`, `RefuelButtonText`, `InsurancePremiumPrice`, `InsuranceButtonText` e na troca física de peças em `UpdateReplacementPlans`.
  - Mantém brutos todos os valores numéricos de backend (`RepairCost`, `TotalRepairCost`, `DefectsRepairTotal`, etc.) para evitar dupla aplicação de multiplicador em `RunPaidServiceAsync`.
  - Adicionada propriedade `ShowWorkshopBanner => IsInWorkshop && Status.Text != "OFICINA";` com notificações em `RefreshWorkshop` e `SetOpenDefects`.
- **`ComponentRowViewModel.cs`**: recebe opcionalmente a economia no construtor e formata `RepairButtonText` com `_economy.ApplyCost(RepairCost)`.
- **`HangarView.xaml`**:
  - Banner de oficina conectado em `ShowWorkshopBanner`.
  - Lista de panes associada a `DefectRows` com `ToolTip="{Binding TechnicalText}"` na linha, texto amigável `FriendlyText` e botão com `Content="{Binding RepairButtonText}"` e `CommandParameter="{Binding Defect}"`.
  - Card de seguro atualizado para exibir `InsurancePremiumPrice`.
- **`MainViewModel.cs:957`**: removido `{d.OriginLabel}, ` da lista de NO-GO, exibindo `• {d.Title} ({data})`.
- **`correcoes-voo-forcado-sdco.md`**: marcados E1, E3 e E4 como concluídos (`[x]`) e atualizada a tabela de tarefas.
- **`manutencao-hangar.md`**: atualizado estado do sistema com preços ajustados por dificuldade e aviso único de oficina.
- **`CHANGELOG.md`**: adicionada entrada da tarefa.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros e 10 avisos (nenhum aviso novo gerado nas áreas editadas).
- [x] `dotnet test src/FlightCareer.Tests` passa com 1.267 testes aprovados (100% de sucesso).
- [x] `missions_golden.json` inalterado (validado via git status).
- [x] Declaração de que o Hangar não foi conferido visualmente com o app aberto incluída neste relatório.
- [x] Itens E1, E3, E4 marcados com `[x]` em `docs/plans/correcoes-voo-forcado-sdco.md`.
- [x] Atualização de `docs/CHANGELOG.md` e `docs/sistemas/manutencao-hangar.md`.
- [x] Commit único no padrão especificado.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 10 avisos pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 1.267 aprovados, 0 falhas.
  - Novos testes:
    - `DifficultyEconomyTests.ApplyCost_Facil_Aplica80PorCento`
    - `DifficultyEconomyTests.CostAdjustmentText_Facil_Desconto20`
    - `DifficultyEconomyTests.CostAdjustmentText_Dificil_Acrescimo15`
    - `DifficultyEconomyTests.CostAdjustmentText_Medio_Null`
    - `DefectTextTests.DefectText_Friendly_MotorEmVoo`
    - `DefectTextTests.DefectText_Friendly_CenariosDeFase` (Theory com 13 variações de fase)
    - `DefectTextTests.DefectText_Friendly_Inspecao`
    - `DefectTextTests.DefectText_Friendly_CodigoDesconhecido`
    - `DefectTextTests.DefectText_Friendly_TodosOsCodigos` (reflexão em todas as 26 constantes do catálogo)
    - `DefectTextTests.DefectText_Technical_OmiteVazios`

## Não testado
- Comportamento de layout visual no WPF e rendering com a aplicação e o simulador MSFS abertos em tempo real.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. A implementação seguiu integralmente o contrato estabelecido na especificação.
