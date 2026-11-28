# Spec HANGAR-ICONES — Exclamação amarela / vermelha das peças após inspeção

**Objetivo (1 frase):** o Hangar não lista as peças; depois de uma inspeção, cada card de componente mostra só as peças que precisam de atenção, com exclamação **amarela** (vida baixa ou vida/TBO da peça vencida) ou **vermelha** (quebrada), visível até o próximo voo.

Decisões do usuário (2026-11-17): Hangar não mostra todas as peças; ícone só durante/depois de uma revisão; aparece após a inspeção e fica até o próximo voo. Inspeções 50 h/100 h/completas revelam as peças do que inspecionaram; walkaround revela só as peças com `visibleOnWalkaround`.

## Fatos já levantados
- `Domain/Entities/AircraftPart.cs` já tem `PartFlag Flag` (`None=0`, `Suspect=1`, comentário "exclamação amarela no Hangar/Walkaround"), `PartFlagSource FlaggedBy` (`None`, `Walkaround`, `Inspection`), `FlaggedAtUtc`, `IsBroken`, `Health`, `HoursSinceReplacement`, `CyclesSinceReplacement`, `LastReplacedAtUtc`. Hoje ninguém marca `Flag` (só criação com `None`).
- `PartDefinition`: `LifeHours`, `LifeCycles`, `CalendarLifeMonths`, `VisibleOnWalkaround`, `Group`. Grupo → `ComponentKind` por `PartComponentMapping` em `Domain/Parts/PartComponentBridge.cs` (Iluminação → Electrical).
- `UI/ViewModels/HangarViewModel.cs` usa `IDbContextFactory` (contexto curto). Inspeções: `Perform50hInspection` (~l.342, componentes Engine e Oil), `Perform100hInspection` (~l.361, todos), `Perform500hInspection` (~l.381, Ignition), `PerformOverhaul` (Engine, HotSection, Oil — reparo), `Walkaround` (~l.319). Reparo por componente via `ComponentMaintenance`; serviços pagos por `RunPaidServiceAsync(... (aircraft, components) => ...)`.
- Cards: `UI/Views/HangarView.xaml` ~l.346-406, `ItemsControl ItemsSource="{Binding Components}"` com `UI/ViewModels/ComponentRowViewModel.cs`.
- Banco: coluna nova = propriedade + `ALTER TABLE ... ADD COLUMN ... DEFAULT` em `AppDbContext.EnsureSchemaUpdatedAsync` (arquivo quente: só acrescente a linha na lista, perto das outras `AircraftParts`, ~l.281-283). Teste de banco antigo: `ComponentPersistenceTests`.

## Contrato
- `AircraftPart`: nova propriedade `double? FlaggedAirframeHours` (+ `ALTER TABLE AircraftParts ADD COLUMN FlaggedAirframeHours REAL NULL;`). Enum `PartFlag`: acrescentar `Broken = 2` (não renumerar).
- **Novo** `Domain/Parts/PartInspectionFlags.cs` (lógica pura, testável):
  ```csharp
  public sealed record PartFlagResult(PartFlag Level, string Reason);
  public static class PartInspectionFlags {
      public const double LifeWarningFraction = 0.9;   // ≥ 90 % da vida (h, ciclos ou meses)
      public const double LowHealthPct = 40.0;          // saúde abaixo disso = amarelo
      public static PartFlagResult Evaluate(AircraftPart part, PartDefinition def, DateTime nowUtc);
      // Marca as peças no escopo; limpa a marca das peças do escopo que estão ok. Devolve quantas ficaram marcadas.
      public static int ApplyInspection(IEnumerable<AircraftPart> parts, PartCatalog catalog, Func<PartDefinition, bool> inScope,
                                        PartFlagSource source, double airframeHours, DateTime nowUtc);
      public static void ClearForKinds(IEnumerable<AircraftPart> parts, PartCatalog catalog, IReadOnlySet<ComponentKind> kinds); // após reparo
      public static bool IsVisible(AircraftPart part, double currentAirframeHours); // Flag != None && |FlaggedAirframeHours − atual| < 0,01
  }
  ```
  - Vermelho: `IsBroken` → `Broken`, "Quebrada".
  - Amarelo (`Suspect`), primeiro motivo que bater: horas ≥ 90 % de `LifeHours` ("Vida: 230 / 250 h" ou "Vida vencida: 260 / 250 h" se ≥ 100 %); ciclos ≥ 90 % de `LifeCycles`; meses desde `LastReplacedAtUtc` ≥ 90 % de `CalendarLifeMonths`; `Health < 40` ("Desgaste avançado"). Vida nula/0 = critério ignorado.
  - Sem motivo → `None`.
- **Hangar** (`HangarViewModel` + `ComponentRowViewModel` + `HangarView.xaml`):
  - Carregar as peças do avião selecionado (contexto curto) e expor em cada `ComponentRowViewModel` uma coleção `FlaggedParts` só com as peças visíveis (`IsVisible`) do seu `ComponentKind`: nome da peça (+ " (motor n)" se `perEngine` e mais de um motor), nível e motivo.
  - No card, abaixo da linha de vida: chips horizontais (WrapPanel) com um ícone de exclamação (amarelo `#FFB300` / vermelho `#FF5252`, mesmos tons das marcas da barra) e o nome; `ToolTip` = motivo. Nada aparece se a lista estiver vazia. Sem tela/lista de todas as peças.
  - Inspeções: 50 h → escopo peças dos kinds Engine e Oil; 100 h → todas; 500 h → Ignition; Walkaround → `VisibleOnWalkaround`; Overhaul e reparo de componente → `ClearForKinds` dos kinds reparados (a peça volta a ser avaliada na próxima inspeção). Chamar dentro da mesma operação/contexto que já salva a inspeção. A mensagem de sucesso da inspeção ganha " — N peça(s) com atenção" quando N > 0.
  - "Até o próximo voo": garantido por `FlaggedAirframeHours` × `CurrentAirframeHours` (o voo soma horas). Não mexa em `FlightMonitor`/`FlightService`/`PartStateWriter`.

## Fora do escopo
- `parts_catalog.json`, `AircraftPartProvisioner`, `wear_rules.json` (outra tarefa em paralelo mexe neles). Arquivos de voo (`FlightMonitor`, `ComponentFlightSession`, `PartFlightSession`, `SimConnectService`, `SimFailureActuator`). Custos/reparo por peça.

## Testes (`Tests/Parts/PartInspectionFlagsTests.cs` + persistência)
- `Evaluate_Quebrada_Vermelho`; `Evaluate_Horas90Pct_Amarelo`; `Evaluate_VidaVencida_AmareloComTexto`; `Evaluate_Ciclos90Pct_Amarelo`; `Evaluate_Calendario90Pct_Amarelo` (nowUtc injetado); `Evaluate_SaudeBaixa_Amarelo`; `Evaluate_Ok_None`.
- `ApplyInspection_SoEscopo` (peça fora do escopo não muda); `ApplyInspection_LimpaPecaOk`; `ApplyInspection_Walkaround_SoVisiveis`.
- `IsVisible_DepoisDoVoo_Some` (horas mudaram).
- `ClearForKinds_LimpaSoOsKinds`.
- Banco antigo sem `FlaggedAirframeHours` sobe e grava (estender o teste de esquema existente).

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 5` sem falhas.
- [ ] Revisor vai ler `AppDbContext.cs` e o diff do Hangar linha a linha: mudanças mínimas.
- [ ] CHANGELOG (`## 2026-11-17 - Exclamação das peças no Hangar após inspeção (Gemini)`) e AGENTS.md › 7 (Hangar). "Não testado visualmente": diga o que foi e o que não foi conferido (sem executar a UI, descreva).
- [ ] Um commit: `feat(hangar): exclamacao amarela/vermelha das pecas apos inspecao (HANGAR-ICONES)`.
