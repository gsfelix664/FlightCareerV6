# Spec MANUT-FAIXAS-B — Lista de peças no Hangar com troca individual

**Objetivo (1 frase):** cada card de componente do Hangar mostra as peças do grupo com a saúde **conhecida** (último walkaround/inspeção), há quantas horas foi vista, cor da faixa, nota de vida e botão **Trocar** por peça; "Reparar tudo" e o botão do card passam a usar só o que o piloto sabe.

Base: `feat/component-health-system` (após MANUT-FAIXAS-A). Branch: `feat/manut-faixas-b`. Decisões do usuário: `docs/plans/economia-estabilidade.md` › 4.1.

## Fatos já levantados (não redescobrir)
- `AircraftPart.KnownHealth` (double?) e `KnownAtAirframeHours` (double?) já existem e persistem (`AppDbContext` linhas ~272/289). Gravados por `PartKnowledge.Reveal` (walkaround, inspeções, revisões) e por `PartReplacementPlanner.ApplyToPart` (100 %, horas atuais).
- `PartInspectionFlags.LifeNote(part, def, careerNow)` devolve a nota de vida/ciclos/calendário (≥ 90 % ou vencida) ou `null`.
- `PartHealthBands` / `PartBandSettings` (`YellowBelowPct` 40, `RedBelowPct` 20); no Hangar as faixas vêm de `_catalog.PartBands` (`ComponentCatalog`).
- `PartReplacementPlanner.SelectRecommended` (Domain/Parts/PartReplacementPlanner.cs:39) usa `part.Health` **real** → o Hangar revela saúde que o piloto não viu. O `Plan(...)` também escreve o motivo com `part.Health` real.
- `PilotAircraftWrapper.UpdateReplacementPlans` (UI/ViewModels/PilotAircraftWrapper.cs ~300–345) agrupa `Aircraft.Parts` por `ComponentKind` (`PartComponentMapping.CreateDefault().ResolveComponentKind(def.Group)`) e chama `SelectRecommended` + `Plan` por card. `Aircraft.Parts` já é carregado (`Include(a => a.Parts)` no `HangarViewModel`).
- **Bug a corrigir:** `HangarViewModel.RepairComponent` (~linha 356) e `RepairAll` (~linha 466), no caminho com peças, chamam `ApplyRegulatoryReset(aircraft, kind)`, que no `Engine` restaura o TBO inteiro e no `Ignition` zera a revisão de 125 h. Trocar uma peça avulsa do motor (ex.: arranque) devolve o TBO de graça. Só os serviços `PerformOverhaul` e a revisão de ignição podem fazer esse reset (eles já fazem por conta própria).
- `HangarPalette.ForHealth` (UI/ViewModels/ComponentRowViewModel.cs:26) usa 60/30/20; as faixas decididas são 40/20.
- Nome com motor: `def.PerEngine && engineCount > 1 && part.EngineIndex > 0 ? $"{def.Name} (motor {part.EngineIndex})" : def.Name` (repetido em 3 lugares; centralize no helper novo).
- Peças `minorPart` (lâmpadas) aparecem na lista como as demais.

## Arquivos a mexer
- **Novo** `src/FlightCareer.Domain/Parts/PartHangarView.cs`: lógica pura da linha de peça (contrato abaixo).
- `src/FlightCareer.Domain/Parts/PartReplacementPlanner.cs`: novo `SelectRecommendedKnown`; motivo do `Plan` pela saúde conhecida.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`: `PartRowViewModel` (novo, no mesmo arquivo ou arquivo próprio), coleção `Parts` no `ComponentRowViewModel`, `ForHealth` 40/20.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`: monta as linhas de peça por card; usa `SelectRecommendedKnown`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs` (quente): comando `ReplacePart`; remove `ApplyRegulatoryReset` dos caminhos **com peças** de `RepairComponent` e `RepairAll` (mantendo `ClearInterdictionIfResolved` se a interdição não for regulatória — na dúvida, não chame nada extra).
- `src/FlightCareer.UI/Views/HangarView.xaml`: `Expander` "Peças (n)" fechado por padrão em cada card, abaixo dos chips.
- Testes em `src/FlightCareer.Tests/Parts/PartHangarViewTests.cs` (novo) e `PartReplacementPlannerTests.cs`.

## Fora do escopo (não mexer)
- `CareerProgressionSimulator` (já usa `KnownHealth` por conta própria) e `SelectRecommended` antigo (mantê-lo; o simulador e testes existentes podem usá-lo).
- Preços, catálogos JSON, `PartWearEngine`, `PartFailureEngine`, voo (`ComponentFlightSession`, `FlightMonitor`), seguro.
- Caminho legado (aeronave sem peças): inalterado, inclusive o `ApplyRegulatoryReset` dele.
- `PerformOverhaul`, troca de óleo, ignição, anual, walkaround: inalterados.

## Contrato
```csharp
namespace FlightCareer.Domain.Parts;

public enum PartKnownBand { Unknown, Green, Yellow, Red, Broken }

public sealed record PartHangarRow(
    AircraftPart Part,
    PartDefinition Def,
    string DisplayName,        // com "(motor n)" quando aplicável
    double? KnownHealth,
    string HealthText,         // "62 %" | "—" (nunca vista)
    string SeenText,           // "visto agora" (< 0,05 h) | "visto há 3 h" (F0) | "nunca inspecionada"
    PartKnownBand Band,        // Broken se IsBroken; Unknown se KnownHealth null; senão pela faixa do KnownHealth
    string? LifeNote);         // PartInspectionFlags.LifeNote

public static class PartHangarView
{
    public static string DisplayName(AircraftPart part, PartDefinition def, int engineCount);
    public static PartHangarRow Build(AircraftPart part, PartDefinition def, int engineCount,
        double airframeHours, DateTime careerNow, PartBandSettings? bands = null);
    /// Ordem: Broken, Red, Yellow, Unknown, Green; dentro da faixa, menor KnownHealth primeiro; depois nome.
    public static IReadOnlyList<PartHangarRow> BuildList(IEnumerable<AircraftPart> parts, PartCatalog catalog,
        int engineCount, double airframeHours, DateTime careerNow, PartBandSettings? bands = null);
}

// PartReplacementPlanner
/// Quebradas (pane é visível ao piloto) ou KnownHealth < RedBelowPct. Nunca lê part.Health.
public static IReadOnlyList<AircraftPart> SelectRecommendedKnown(IEnumerable<AircraftPart> parts, PartBandSettings? bands = null);
```
- `Plan(...)`: o `Reason` passa a usar `KnownHealth`: quebrada → "Quebrada"; `KnownHealth` < vermelho → "Faixa vermelha: X %"; < amarelo → "Faixa amarela: X %"; com valor → "Saúde X %"; `KnownHealth` null → "Troca por escolha do piloto". Nenhum texto usa `part.Health`.
- Uma peça **quebrada** com `KnownHealth` antigo alto continua `Broken` (a quebra manda).
- `PartRowViewModel` (UI): expõe `DisplayName`, `HealthText`, `SeenText`, `LifeNote`, `HasLifeNote`, `Brush` (Broken/Red → `HangarPalette.Red`, Yellow → `Amber`, Green → `Green`, Unknown → `Muted`), `ReplaceText` = `"Trocar · {preço+mão de obra:C0}"` (do `PartReplacementPlanner.Plan` com só essa peça) e `ReplaceCommand` (o `IRelayCommand<PartRowViewModel>` do `HangarViewModel`, passado como o `RepairCommand` já é).
- `HangarViewModel.ReplacePart(PartRowViewModel row)`: `CanOperate()`; plano com só a peça; `RunPaidServiceAsync` com título "Trocar peça", mensagem com nome, motivo e custo, transação `"TROCA DE PEÇA: {nome}"`, `apply` = `ApplyToParts` + `SyncComponentsFromParts` (sem `ApplyRegulatoryReset`), `downtimeHours` = parada da peça.
- `HangarPalette.ForHealth`: `< 20` vermelho, `< 40` âmbar, senão verde (laranja deixa de ser usado ali).

## Testes que devem existir e passar
- `Build_NuncaInspecionada_UnknownETracoENuncaInspecionada`: KnownHealth null, IsBroken false → Band Unknown, HealthText "—", SeenText "nunca inspecionada".
- `Build_VistaHa3h_TextoComHoras`: KnownHealth 62, KnownAt 100, airframe 103 → Band Green, "62 %", "visto há 3 h".
- `Build_VistaAgora`: KnownAt = airframe → "visto agora".
- `Build_FaixasPeloConhecidoNaoPeloReal`: Health real 15, KnownHealth 55 → Green (não Red).
- `Build_QuebradaVenceConhecido`: IsBroken, KnownHealth 90 → Broken.
- `Build_YellowERed`: KnownHealth 30 → Yellow; 10 → Red.
- `Build_NomeComMotor`: PerEngine, 2 motores, EngineIndex 2 → "<nome> (motor 2)"; 1 motor → só o nome.
- `Build_LifeNotePropagada`: peça com horas ≥ vida → LifeNote não nula.
- `BuildList_OrdemPorGravidade`: Broken, Red, Yellow, Unknown, Green.
- `SelectRecommendedKnown_IgnoraSaudeReal`: Health 5 / KnownHealth null → fora; Health 50 / KnownHealth 10 → dentro; IsBroken → dentro.
- `Plan_MotivoPeloConhecido`: Health 5, KnownHealth null → "Troca por escolha do piloto"; KnownHealth 30 → "Faixa amarela: 30 %".
- Testes existentes de `SelectRecommended`/`Plan` continuam passando (ajuste só asserções de texto do motivo que dependiam de `Health`, explicando no relatório).

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [ ] Nenhum texto/cor/seleção do Hangar no caminho com peças lê `AircraftPart.Health` (grep no diff).
- [ ] Trocar peça avulsa do Motor ou da Ignição (botão do card, "Reparar tudo" ou "Trocar") **não** altera `EngineTboRemainingHours` nem `HoursUntilNext500hInspection`.
- [ ] Lista de peças em `Expander` fechado por padrão; chips de atenção continuam como estão.
- [ ] CHANGELOG (`## 2026-11-21 - Lista de peças no Hangar e troca individual (Gemini)`) e `AGENTS.md` › 7 (Hangar) atualizados.
- [ ] Um commit: `feat(hangar): lista de pecas com saude conhecida e troca individual (MANUT-FAIXAS-B)`.
- [ ] Relatório diz: "não visto na tela; não testado no MSFS".

## Armadilhas
- `HangarViewModel` é arquivo quente: mude só `RepairComponent`, `RepairAll` (caminho com peças) e acrescente `ReplacePart`. Não reorganize o resto.
- Contexto curto por operação (`IDbContextFactory`), como os outros comandos (`RunPaidServiceAsync` já faz).
- Sem `catch {}` silencioso.
- Depois do serviço, `RunPaidServiceAsync` recarrega a frota: as linhas de peça precisam ser remontadas no `Refresh`/construção do wrapper, não só uma vez.
