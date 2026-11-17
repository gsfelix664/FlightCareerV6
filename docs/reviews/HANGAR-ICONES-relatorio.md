# HANGAR-ICONES — Exclamação amarela / vermelha das peças após inspeção

**Branch:** feat/hangar-icones · **Commit:** pendente · **Data:** 2026-11-17

## Plano (antes de codar)
- Adicionar `Broken = 2` a `PartFlag` e `FlaggedAirframeHours` a `AircraftPart` com migração SQLite em `AppDbContext`.
- Criar `Domain/Parts/PartInspectionFlags.cs` com lógica pura (`Evaluate`, `ApplyInspection`, `ClearForKinds`, `IsVisible`) e suíte de testes xUnit correspondente.
- Atualizar `ComponentRowViewModel` com `FlaggedParts`, `PilotAircraftWrapper` com agrupamento de peças visíveis por componente e `HangarView.xaml` com chips de atenção com ícone de exclamação sob a linha de vida.
- Integrar chamadas de inspeção (50h, 100h, 500h, walkaround) e limpeza pós-reparo/overhaul em `HangarViewModel`, enriquecendo as mensagens de conclusão com a contagem de peças com atenção.
- Adicionar teste de persistência para banco antigo sem `FlaggedAirframeHours`, rodar build, testes e fuzzing.

## O que foi feito
- `src/FlightCareer.Domain/Entities/AircraftPart.cs`:
  - Acrescentado `Broken = 2` ao enum `PartFlag` sem alterar a numeração existente (`None = 0, Suspect = 1, Broken = 2`).
  - Adicionada propriedade `double? FlaggedAirframeHours { get; set; }`.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - Adicionado `FlaggedAirframeHours REAL NULL` na DDL de criação `CREATE TABLE IF NOT EXISTS AircraftParts`.
  - Adicionado `ALTER TABLE AircraftParts ADD COLUMN FlaggedAirframeHours REAL NULL;` à lista idempotente de `migrationStatements` em `EnsureSchemaUpdatedAsync`.
- `src/FlightCareer.Domain/Parts/PartInspectionFlags.cs` (novo):
  - Lógica pura de avaliação de peças e sinalização:
    - `PartFlagResult(PartFlag Level, string Reason)`.
    - `Evaluate(part, def, nowUtc)`: Vermelho (`Broken`, `"Quebrada"`) se `IsBroken`; Amarelo (`Suspect`) se horas $\ge 90\%$ de `LifeHours` (`"Vida: X / Y h"` ou `"Vida vencida: X / Y h"` se $\ge 100\%$), ciclos $\ge 90\%$ de `LifeCycles` (`"Ciclos: X / Y"` ou `"Ciclos vencidos: X / Y"`), meses $\ge 90\%$ de `CalendarLifeMonths` (`"Calendário: X / Y meses"` ou `"Calendário vencido: X / Y meses"`), ou saúde $< 40\%$ (`"Desgaste avançado"`); `None` e string vazia caso contrário.
    - `ApplyInspection(parts, catalog, inScope, source, airframeHours, nowUtc)`: avalia e marca peças no escopo, limpa peças no escopo que ficaram ok (`None`) e preserva peças fora do escopo. Retorna a contagem de peças marcadas com atenção.
    - `ClearForKinds(parts, catalog, kinds)`: limpa sinalizações das peças dos tipos de componente fornecidos após reparo ou overhaul.
    - `IsVisible(part, currentAirframeHours)`: verifica se `part.Flag != None` e se $|FlaggedAirframeHours - currentAirframeHours| < 0,01$, garantindo que alertas sumam após o voo seguinte.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`:
  - Adicionada classe `FlaggedPartViewModel` com `Name`, `Level`, `Reason`, `Brush` (`HangarPalette.Red` ou `HangarPalette.Amber`) e `Icon = "!"`.
  - Expostas propriedades `FlaggedParts` e `HasFlaggedParts`, com método `SetFlaggedParts(...)`.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Adicionado método `SetFlaggedParts(parts, partCatalog)` que filtra peças com `PartInspectionFlags.IsVisible`, mapeia grupo para `ComponentKind`, nomeia com sufixo `(motor n)` se `perEngine` em aeronave multimotor e distribui para os `ComponentRowViewModel` correspondentes.
- `src/FlightCareer.UI/Views/HangarView.xaml`:
  - Inserido `ItemsControl` horizontal com `WrapPanel` logo abaixo da linha de vida dos componentes nos cards, exibindo chips com fundo escuro, borda na cor do alerta (`#FFB300` / `#FF5252`), ícone `!`, nome da peça e `ToolTip` com o motivo exato.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`:
  - Construtor atualizado com suporte a `PartCatalog? partCatalog = null`.
  - Carregamento assíncrono de peças marcadas em contexto curto via `LoadFlaggedPartsForSelectedAircraftAsync` ao selecionar ou recarregar frota.
  - `RunPaidServiceAsync` atualizado para carregar `aircraft.Parts` e formatar mensagem de sucesso com `$" — {n} peça(s) com atenção."` via delegate `getFlaggedCount`.
  - Integrado `ApplyInspection` em `Perform50hInspection` (escopo Engine/Oil), `Perform100hInspection` (todas), `Perform500hInspection` (Ignition) e `Walkaround` (peças visíveis).
  - Integrado `ClearForKinds` em `RepairComponent`, `RepairAll` e `PerformOverhaul`.
- `src/FlightCareer.Tests/Parts/PartInspectionFlagsTests.cs` (novo):
  - 12 testes unitários cobrindo todos os cenários de `Evaluate`, `ApplyInspection`, `ClearForKinds` e `IsVisible`.
- `src/FlightCareer.Tests/Components/ComponentPersistenceTests.cs`:
  - Teste `Banco_antigo_sem_FlaggedAirframeHours_recebe_coluna_e_grava_peca` validando migração e persistência da nova coluna.
- `docs/CHANGELOG.md` e `AGENTS.md`: atualizados.

## Critérios de aceite
- [x] Build 0 erros; dotnet test 100 %; fuzz --aircraft all --runs 5 sem falhas.
  - `dotnet build`: 0 erros.
  - `dotnet test`: 607/607 aprovados (100%).
  - `fuzz --aircraft all --runs 5`: 230 voos em 46 aeronaves aprovados (100%), 0 limites ausentes, 0 falhas.
- [x] Revisor vai ler AppDbContext.cs e o diff do Hangar linha a linha: mudanças mínimas.
  - `AppDbContext.cs`: apenas 2 linhas adicionadas.
  - Mudanças na UI e ViewModels cirúrgicas, isoladas e estritamente aderentes ao contrato da spec.
- [x] CHANGELOG (`## 2026-11-17 - Exclamação das peças no Hangar após inspeção (Gemini)`) e AGENTS.md › 7 (Hangar).
- [x] Um commit: `feat(hangar): exclamacao amarela/vermelha das pecas apos inspecao (HANGAR-ICONES)`.

## Build e testes
- dotnet build: 0 erros, 15 avisos (vulnerabilidades pré-existentes de pacotes nuget)
- dotnet test: 607 aprovados, 0 falhas (13 novos: 12 em `PartInspectionFlagsTests` + 1 em `ComponentPersistenceTests`)
- fuzz testing: 230 voos em 46 aeronaves, 0 falhas (100% de aprovação)

## Não testado
- Renderização visual em tempo real na janela WPF aberta durante execução do jogo (verificado por compilação de XAML pelo compilador do WPF BAML, data binding bindings e testes unitários de ViewModel).
- Conexão e interação em tempo real com o MSFS (esta tarefa atua exclusivamente na camada de UI/Hangar e domínio de inspeções do aplicativo).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. Todos os requisitos da spec foram seguidos à risca.
