# MANUT-FAIXAS-B — Lista de peças no Hangar com troca individual

**Branch:** feat/manut-faixas-b · **Commit:** <pendente> · **Data:** 2026-11-21

## Plano (antes de codar)
- Criar `FlightCareer.Domain/Parts/PartHangarView.cs` com o enum `PartKnownBand` (Unknown, Green, Yellow, Red, Broken), record `PartHangarRow` e métodos estáticos `DisplayName`, `Build` e `BuildList` (ordenando por gravidade: Broken, Red, Yellow, Unknown, Green; desempatando por KnownHealth menor e DisplayName).
- Atualizar `FlightCareer.Domain/Parts/PartReplacementPlanner.cs` adicionando `SelectRecommendedKnown` (selecionando peças quebradas ou com KnownHealth < RedBelowPct, sem ler Health real) e ajustando a geração do texto `Reason` em `Plan` para usar estritamente `KnownHealth` (ou Broken).
- Implementar `PartRowViewModel` e atualizar `ComponentRowViewModel` em `FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`: adicionar coleção `Parts`, `HasParts`, `PartsHeader`, `SetParts` e ajustar `HangarPalette.ForHealth` para as faixas 40/20 (< 20 Red, < 40 Amber, senão Green).
- Atualizar `PilotAircraftWrapper.cs` para repassar `ReplacePartCommand`, chamar `SelectRecommendedKnown` em vez de `SelectRecommended`, construir `PartRowViewModel` para cada peça do grupo com `BuildList` e precificação individual de troca.
- Atualizar `HangarViewModel.cs`: adicionar comando `ReplacePart` (com parada, preço, `ApplyToParts` e `SyncComponentsFromParts`), e remover `ApplyRegulatoryReset` dos fluxos com peças de `RepairComponent` e `RepairAll`.
- Atualizar `HangarView.xaml`: adicionar `Expander` "Peças (n)" fechado por padrão logo abaixo dos chips de atenção no card de componente.
- Criar testes unitários em `FlightCareer.Tests/Parts/PartHangarViewTests.cs` cobrindo todos os cenários da especificação e ajustar asserções de texto de `PartReplacementPlannerTests.cs`.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartHangarView.cs`: Novo arquivo com `PartKnownBand`, `PartHangarRow` e `PartHangarView` (`DisplayName`, `Build`, `BuildList`). Centralizou o cálculo do nome formatado por motor e a lógica de apresentação de saúde conhecida (`KnownHealth`), tempo desde a última inspeção (`KnownAtAirframeHours`), faixas visíveis e notas de vida (`LifeNote`), além de ordenação por gravidade de faixas.
- `src/FlightCareer.Domain/Parts/PartReplacementPlanner.cs`:
  - Adicionado `SelectRecommendedKnown(parts, bands)` selecionando peças quebradas ou com `KnownHealth < bands.RedBelowPct` (nunca lê `part.Health`).
  - Atualizado `Plan`: o texto `Reason` passa a ser gerado a partir de `part.IsBroken` ou `part.KnownHealth` (ex: "Quebrada", "Faixa vermelha: X %", "Faixa amarela: X %", "Saúde X %" ou "Troca por escolha do piloto" quando null). Nenhuma string lê `part.Health`.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`:
  - `HangarPalette.ForHealth` atualizado para as faixas decididas: `< 20` Red, `< 40` Amber, senão Green.
  - Implementado `PartRowViewModel` expondo `Row`, `DisplayName`, `HealthText`, `SeenText`, `LifeNote`, `HasLifeNote`, `Brush`, `ReplaceText` e `ReplaceCommand`.
  - Adicionadas propriedades `Parts`, `HasParts`, `PartsHeader` e método `SetParts` em `ComponentRowViewModel`.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Recebe `replacePartCommand` opcional no construtor.
  - Em `UpdateReplacementPlans`: passa a usar `SelectRecommendedKnown` para o plano agregado do card, e constrói a lista ordenada de `PartRowViewModel` chamando `PartHangarView.BuildList` e `PartReplacementPlanner.Plan` individual por peça.
  - Em `SetFlaggedParts`: reutiliza `PartHangarView.DisplayName`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`:
  - Implementado `ReplacePartCommand` via `[RelayCommand] ReplacePart(PartRowViewModel row)`, executando `RunPaidServiceAsync` com custo individual, parada da peça, aplicação via `ApplyToParts` e sincronização via `SyncComponentsFromParts` sem conceder resets regulatórios.
  - Corrigido bug de reset indevido de regulamentação: removidas as chamadas de `ApplyRegulatoryReset` dos caminhos com peças de `RepairComponent` e `RepairAll`. A troca de componentes avulsos do motor ou da ignição não restaura mais o TBO nem a revisão de 125 h. O caminho legado sem peças permanece inalterado.
  - Removido texto informativo de reset de TBO da mensagem de reparo de componente quando a aeronave usa peças.
  - `LoadFleetAsync` passa `ReplacePartCommand` para o `PilotAircraftWrapper`.
- `src/FlightCareer.UI/Views/HangarView.xaml`:
  - Inserido `Expander` "Peças (n)" com `IsExpanded="False"` abaixo dos chips de atenção no card de componente.
  - Cada linha do Expander exibe `DisplayName` e `LifeNote`, `HealthText` colorido com `SeenText` e o botão `Trocar · {custo}`.
- `src/FlightCareer.Tests/Parts/PartHangarViewTests.cs`:
  - Nova suíte de testes unitários com 11 testes cobrindo todas as situações especificadas: nunca inspecionada, visto há X h, visto agora, faixas pelo conhecido vs real, prioridade de quebrada, yellow/red, per engine, life note, ordenação por gravidade, `SelectRecommendedKnown` e `Plan_MotivoPeloConhecido`.
- `src/FlightCareer.Tests/Parts/PartReplacementPlannerTests.cs` e `PartHealthBandsAndFailuresTests.cs`:
  - Atualizadas instâncias de teste com `KnownHealth` onde antes se asseria o texto "Saúde X %" ou faixas geradas pelo `Plan`, alinhando com a nova regra de que o motivo reporta o conhecimento do piloto.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [x] Nenhum texto/cor/seleção do Hangar no caminho com peças lê `AircraftPart.Health` (verificado via grep no diff: apenas `part.KnownHealth` e `Row.HealthText` são lidos).
- [x] Trocar peça avulsa do Motor ou da Ignição (botão do card, "Reparar tudo" ou "Trocar") **não** altera `EngineTboRemainingHours` nem `HoursUntilNext500hInspection` (chamadas de `ApplyRegulatoryReset` removidas dos fluxos com peças).
- [x] Lista de peças em `Expander` fechado por padrão; chips de atenção continuam como estão.
- [x] CHANGELOG (`## 2026-11-21 - Lista de peças no Hangar e troca individual (Gemini)`) e `AGENTS.md` › 7 (Hangar) atualizados.
- [x] Um commit: `feat(hangar): lista de pecas com saude conhecida e troca individual (MANUT-FAIXAS-B)`.
- [x] Relatório diz: "não visto na tela; não testado no MSFS".

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos normais/conhecidos de dependências de banco.
- `dotnet test src/FlightCareer.Tests`: 684 aprovados, 0 falhas (11 novos testes em `PartHangarViewTests.cs`).

## Não testado
- Não visto na tela; não testado no MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. As asserções de motivo do `Plan` nos testes legados foram mantidas através do preenchimento de `KnownHealth` nas entidades do teste, conforme instruído na spec.
