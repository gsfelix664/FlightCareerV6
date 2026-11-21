# Spec MANUT-FAIXAS-A — Manutenção por faixas de saúde (domínio, voo, seguro e simulador)

**Objetivo (1 frase):** trocar a manutenção "troca tudo abaixo de 80 %" por faixas de saúde por peça (verde 100–40, amarelo 40–20 com desgaste acelerado, vermelho < 20 com pane por peça), sem interdição por óleo/ignição/TBO vencidos, inspeção anual que só inspeciona e seguro barato que nega perda total com defeito conhecido não corrigido.

Decisões do usuário: `docs/plans/economia-estabilidade.md` › 4.1. **Esta tarefa não mexe na tela do Hangar além do mínimo para compilar** (a lista de peças por card é a MANUT-FAIXAS-B).

## Fatos já levantados (não redescobrir)
- `Domain/Parts/PartReplacementPlanner.cs:35` `ReplaceBelowHealthPct = 80.0`; `Plan(...)` inclui peça se `IsBroken || Health < 80 || Evaluate(...).Level != None || part.Flag != None`.
- `Domain/Parts/PartInspectionFlags.cs`: `Evaluate` dá `Suspect` por vida ≥ 90 % (h, ciclos, meses) ou saúde < 40, `Broken` se `IsBroken`. Usado pelo Hangar (chips) e pelo planner.
- `Domain/Parts/PartWearEngine.cs:528-547`: perda natural = `70 / LifeHours` × maior multiplicador de estresse + soma de `lossPerHourPct`. Não existe aceleração por saúde baixa.
- `Domain/Components/FailureEngine.cs`: λ(h) por **componente** (barra) com `hazard` do `component_catalog.json` (`threshold 20`, `lambdaAtThreshold 0.02`, `lambdaAtZero 2.0`), `def.HazardScale`, um sorteio por componente, no máximo uma pane por componente por voo, modo escolhido por peso (`PickMode`, privado).
- Nas aeronaves com peças a barra é `AggregateHealth` = 0,7 × mín + 0,3 × média (`PartComponentBridge.cs:89`), então a pane hoje sai da barra, não da peça.
- `UI/Services/ComponentFlightSession.cs:163-167` chama `_failures.Evaluate(_components, data, dtHours)` e `ApplyMode(kind, mode, data)` (dano via `ApplyDamage` → `PartDamageSink` = `PartFlightSession.DamageGroup` quando `PartsDriven`). Ligação em `UI/Services/FlightMonitor.cs:313-314`.
- `component_catalog.json` linha 24: `"overdueMultiplier": 3.0` (só o `WearEngine` legado usa; continua só para aeronaves sem peças). `visibility.walkaroundHalfWidth = 10.0`.
- Interdição: `Application/Services/FlightService.cs:134-143` (TBO ≤ 0 → interdita; senão anual ≤ 0 → interdita) e igual em `UI/ViewModels/FerryFlightViewModel.cs:310-318`. `HangarViewModel.ClearInterdictionIfResolved` (linha 336) exige anual > 0 **e** TBO > 0.
- Inspeção anual (`HangarViewModel.Perform100hInspection`, linha 618) hoje **troca o óleo** e zera também o contador de 12 h.
- Seguro: `Application/Services/EconomyServices.cs:47` `MONTHLY_RATE = 0.01`, `MIN_PREMIUM = 250`, franquia 5 %, cobertura 70 %. Só cobre perda total (`HangarViewModel.ClaimInsurance`, linha 843). `Infrastructure/Services/EconomyServices.cs` só redireciona.
- Simulador de carreira: `Application/Simulation/CareerProgressionSimulator.cs` (usa `FailureEngine` na linha ~577, `PartReplacementPlanner.Plan` nas linhas ~622, 1073, 1115, `ApplyPolicyMaintenance` na 1020, seguro na 1367).

## Arquivos a criar/mexer
1. **`Domain/Parts/PartHealthBands.cs` (novo):** faixas e curva de desgaste acelerado (contrato abaixo).
2. **`Infrastructure/Data/component_catalog.json`:** novo bloco `"partBands": { "yellowBelowPct": 40.0, "redBelowPct": 20.0, "lowHealthWearMaxMultiplier": 2.0 }` com comentário; `visibility.walkaroundHalfWidth` → `0.0` (walkaround mostra o valor exato). `hazard` não muda. Ler o bloco no `ComponentCatalog` (padrões iguais aos valores acima se o bloco faltar).
3. **`Domain/Parts/PartWearEngine.cs`:** na linha 544, `normalRatePerHour` é multiplicado por `PartHealthBands.LowHealthWearMultiplier(part.Health, settings)` **antes** do multiplicador de estresse. Não mexer em mais nada do arquivo. As lâmpadas (ramo `usageSensor`, linhas ~503-526) também recebem o fator.
4. **`Domain/Parts/PartFailureEngine.cs` (novo):** pane por peça (contrato abaixo). Tornar `FailureEngine.PickMode` reutilizável (método estático interno ou público `PickMode(ComponentDefinition, EngineClass, IRandomSource)`) sem mudar o comportamento do legado.
5. **`UI/Services/PartFlightSession.cs`:** expor `IReadOnlyList<PartFailure> EvaluateFailures(double dtSimHours, Func<ComponentKind, bool> isInUse, IReadOnlyCollection<ComponentKind> alreadyFailed)` usando um `PartFailureEngine` criado no `Begin`/`Reset`, e `void BreakPart(Guid partId)` (saúde 0, `IsBroken`, `BrokenAtUtc`, pelo método que já existe no `PartWearEngine`, linha ~640).
6. **`UI/Services/ComponentFlightSession.cs` (quente — mudança mínima):** nova propriedade `Func<double, Func<ComponentKind, bool>, IReadOnlyCollection<ComponentKind>, IReadOnlyList<PartFailure>>? PartFailureSource` e `Action<Guid>? PartBreakSink`. No passo 3 do `ProcessTick`: se `PartsDriven && PartFailureSource != null`, usar `PartFailureSource(dtHours, k => ComponentUsage.IsInUse(k, data), failedKinds)`; para cada pane: `PartBreakSink?.Invoke(f.Part.Id)`, depois `ApplyMode(f.Kind, f.Mode, data)` e registrar o `Kind` como falhado no voo (um por componente por voo, igual ao legado). Senão, o caminho atual `_failures.Evaluate` sem mudança. A descrição do alerta deve citar a peça: `"{mode.Description} ({nome da peça} em {saúde antes da pane:F0}%)"` — criar uma sobrecarga de `ApplyMode` com o texto, sem mudar a original.
7. **`UI/Services/FlightMonitor.cs` (quente — só as linhas 263-264, 313-314, 319-320, 331):** ligar/desligar `PartFailureSource = _parts.EvaluateFailures` e `PartBreakSink = _parts.BreakPart` junto com `PartDamageSink`. Nada mais.
8. **`Domain/Parts/PartInspectionFlags.cs`:** `Evaluate` passa a usar só a saúde: `Broken` se `IsBroken` ou saúde < `redBelowPct` (motivo `"Faixa vermelha: {h:F0} %"` ou `"Quebrada"`), `Suspect` se saúde < `yellowBelowPct` (`"Faixa amarela: {h:F0} %"`). Vida/ciclos/calendário vencidos **não marcam mais**; criar `string? LifeNote(part, def, careerNow)` que devolve o texto de vida (ex.: `"Vida vencida: 130 / 120 h"`) para a UI mostrar como informação. Remover `LifeWarningFraction`/`LowHealthPct` ou fazê-los apontar para as faixas. `Evaluate` recebe as faixas (`PartBandSettings`, com sobrecarga que usa os padrões 40/20).
9. **`Domain/Parts/PartReplacementPlanner.cs`:** remover `ReplaceBelowHealthPct` e o critério automático. Novo contrato: `Plan(selectedParts, ...)` precifica **exatamente** as peças recebidas (motivo pela faixa: `"Quebrada"`, `"Faixa vermelha: x %"`, `"Faixa amarela: x %"`, `"Saúde x %"`); `SelectRecommended(parts, catalog, bands)` devolve quebradas + vermelhas (é o que "Reparar tudo" troca). `ApplyToPart` também limpa `FoundRedAtAnnualAirframeHours` e grava `KnownHealth = 100`, `KnownAtAirframeHours = airframeHours`.
10. **`Domain/Entities/AircraftPart.cs` + `Infrastructure/Persistence/AppDbContext.cs` (`EnsureSchemaUpdatedAsync`):** colunas novas `KnownHealth REAL NULL`, `KnownAtAirframeHours REAL NULL` (último valor visto e quando; usadas pela B), `FoundRedAtAnnualAirframeHours REAL NULL` (defeito achado na anual e não corrigido). `ALTER TABLE AircraftParts ADD COLUMN ... ` idempotente, como as demais.
11. **`Domain/Parts/PartKnowledge.cs` (novo):** `Reveal(IEnumerable<AircraftPart> parts, Func<PartDefinition,bool> inScope, PartCatalog, double airframeHours)` grava `KnownHealth = Health` e `KnownAtAirframeHours` nas peças do escopo; `MarkAnnualFindings(parts, catalog, bands, airframeHours)` grava `FoundRedAtAnnualAirframeHours` nas peças quebradas/vermelhas e limpa nas demais.
12. **`UI/ViewModels/HangarViewModel.cs` (quente — mínimo):**
    - `Walkaround`, `Perform50hInspection`, `Perform100hInspection`, `Perform500hInspection`, `PerformOverhaul`: chamar `PartKnowledge.Reveal` no escopo que cada um já usa para `ApplyInspection` (walkaround: `VisibleOnWalkaround`; troca de óleo: Motor + Óleo; anual: todas; ignição: Ignição; retífica: Motor + Seção quente + Óleo).
    - `Perform100hInspection`: **não troca mais o óleo** e **não zera** o contador de 12 h; só inspeciona, zera o contador de 25 h, chama `MarkAnnualFindings` e libera a interdição. Texto do diálogo atualizado ("não troca peças; reparos são orçados à parte").
    - `RepairComponent` e `RepairAll` com peças: o plano passa a ser `Plan(SelectRecommended(peças do grupo))` (quebradas + vermelhas). Se não houver nenhuma, o botão mostra "Em dia" (mesmo comportamento de hoje quando o plano está vazio). A troca de peça individual entra na B.
    - `ClearInterdictionIfResolved`: exige só anual > 0.
13. **`Application/Services/FlightService.cs:134-143` e `UI/ViewModels/FerryFlightViewModel.cs:310-318`:** remover a interdição por TBO (fica só a anual). No carregamento da frota (`HangarViewModel.LoadFleetAsync` ou onde o Hangar já ajusta a aeronave), liberar aeronaves interditadas só por TBO (anual > 0 e motivo contendo "TBO").
14. **`Application/Services/EconomyServices.cs`:** `MONTHLY_RATE = 0.025 / 12` (2,5 %/ano), `MIN_PREMIUM = 50`. Novo `bool ClaimDenied(PilotAircraft a, out string reason)`: negado se alguma peça tem `FoundRedAtAnnualAirframeHours != null` (motivo: `"A inspeção anual apontou {peça} e ela não foi trocada"`). Redirecionar no `Infrastructure/Services/EconomyServices.cs`. `HangarViewModel.ClaimInsurance` mostra o motivo e não paga quando negado (a aeronave continua na frota para descarte).
15. **`Application/Simulation/CareerProgressionSimulator.cs`:** seguir as mesmas regras: pane por peça (`PartFailureEngine`) nas aeronaves com peças; sem interdição por TBO; anual só inspeciona; seguro novo e negação na perda total. Políticas:
    - **cauteloso:** faz troca de óleo, ignição e TBO no vencimento; após cada inspeção/revisão troca as peças **amarelas e vermelhas** reveladas; walkaround antes de cada voo (troca visíveis amarelas/vermelhas).
    - **equilibrado:** faz as revisões no vencimento; troca só vermelhas e quebradas reveladas.
    - **agressivo:** só a anual (obrigatória); pula óleo, ignição e TBO; troca só quebradas/NO-GO.
    - A simulação só pode trocar peças que foram reveladas (walkaround/inspeção/revisão) ou quebradas — o bot não "vê" a saúde oculta.
16. **`AGENTS.md` › 7 e `docs/CHANGELOG.md`:** atualizar Manutenção, Hangar e Panes; entrada `## 2026-11-20 - MANUT-FAIXAS-A (Gemini)`.

## Fora do escopo (não mexer)
- XAML e a lista de peças por card do Hangar (MANUT-FAIXAS-B). Pode ajustar textos de `ComponentRowViewModel`/`PilotAircraftWrapper` só se não compilar.
- Perda de desempenho no amarelo no simulador (depende do SDK; tarefa à parte). `TurbineDegradation` fica como está.
- `FailureEngine` legado, `WearEngine`, `HealthVisibility` (só o valor do catálogo muda), `SimFailureActuator`, `SimConnectService`, `SimTelemetry`, `PersistentFailureService`.
- Lâmpadas `minorPart`: continuam com `UsageFailureModel` e **não** entram no `PartFailureEngine`.
- Preços de peças, inspeções e retífica; `MissionGenerator`.

## Contrato
```csharp
namespace FlightCareer.Domain.Parts;

public enum PartBand { Green, Yellow, Red }

public sealed record PartBandSettings(double YellowBelowPct = 40.0, double RedBelowPct = 20.0, double LowHealthWearMaxMultiplier = 2.0);

public static class PartHealthBands
{
    public static PartBand Classify(double health, PartBandSettings s);   // < Red → Red; < Yellow → Yellow; senão Green
    // 1 em h ≥ Yellow; 1 + (Max − 1) · ((Yellow − h) / Yellow)² abaixo (h limitado a [0, 100])
    public static double LowHealthWearMultiplier(double health, PartBandSettings s);
}

public sealed record PartFailure(AircraftPart Part, ComponentKind Kind, FailureModeDefinition Mode, double HealthAtFailure);

public sealed class PartFailureEngine
{
    public PartFailureEngine(ComponentCatalog catalog, PartCatalog parts, EngineClass engineClass, IRandomSource random,
                             PartComponentMapping? mapping = null);
    // Para cada peça não-minor, não quebrada, de componente em uso e ainda não falhado no voo:
    //   λ = FailureEngine.HazardPerHour(part.Health, catalog.Hazard, componentDef.HazardScale); p = 1 − e^(−λ·dt)
    // Se der pane: modo por PickMode do componente; no máximo 1 pane por ComponentKind por voo (inclui alreadyFailed).
    public IReadOnlyList<PartFailure> Evaluate(IEnumerable<AircraftPart> parts, double dtHours,
        Func<ComponentKind, bool> isInUse, IReadOnlyCollection<ComponentKind> alreadyFailed);
    public void Reset();
}
```

## Testes que devem existir e passar (`FlightCareer.Tests/Parts/` e `Economy/`)
- `Bands_Classify_Limites`: 40 → Green, 39,9 → Yellow, 20 → Yellow, 19,9 → Red, 0 → Red.
- `LowHealthWear_Curva`: 100 → 1,0; 40 → 1,0; 20 → 1,25; 0 → 2,0 (máx 2).
- `PartWear_PecaAbaixoDe40_GastaMaisRapido`: mesma peça e mesmo tempo, saúde inicial 30 gasta 1 + ((40−30)/40)² ≈ 1,0625× a de saúde 80 (tolerância 1e-6, sem estresse).
- `PartFailure_AcimaDe20_NuncaFalha`: peças a 20,1 %, 10.000 h simuladas em passos de 0,01 h com `IRandomSource` fixo em 0 → nenhuma pane.
- `PartFailure_Em0_TaxaDe2PorHora`: probabilidade em 1 h com saúde 0 = 1 − e^(−2·scale) (usar a fórmula, não sorteio).
- `PartFailure_UmaPorComponentePorVoo`: dois pneus a 5 % com random que sempre falha → 1 pane de Pneus no voo.
- `PartFailure_IgnoraMinorEQuebradas`.
- `PartFailure_UsaPecaNaoBarra`: um pneu a 10 % e outro a 100 % (barra agregada > 20) → pode falhar (random 0) e a pane cita o pneu de 10 %.
- `Planner_NaoTrocaVerdeNemAmarelaSozinho`: `SelectRecommended` com peças a 70 %, 35 %, 15 % e quebrada → só 15 % e quebrada.
- `Planner_Plan_PrecificaSoAsSelecionadas`.
- `Flags_SoPelaSaude`: peça a 50 % com vida vencida → `None` e `LifeNote` com "Vida vencida"; 30 % → Suspect; 10 % → Broken.
- `AnnualInspection_NaoTrocaOleo_MarcaAchados`: (teste de domínio do `PartKnowledge`) anual revela todas as peças e marca `FoundRedAtAnnualAirframeHours` só nas vermelhas/quebradas; troca da peça limpa a marca.
- `Insurance_Premio_2v5PorCentoAoAno` e `Insurance_ClaimNegado_ComAchadoDaAnualNaoCorrigido`.
- `FlightService_TboVencido_NaoInterdita` e `FlightService_AnualVencida_Interdita` (ajustar `CalibraF4Tests`/`MaintenanceTests` existentes se esperavam interdição por TBO).
- `ComponentPersistenceTests`: banco antigo ganha as 3 colunas novas.
- `CareerProgression` (teste curto já existente): continua passando; acrescentar `Agressivo_PulaRevisoes_TemMaisPanesQueCauteloso` (ex.: C152, 1 bot, 120 dias, `--fast`, mesma seed; panes/100 h do agressivo > cauteloso). **Não rodar a ECON-STAB completa** (o revisor roda).

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [ ] Nenhuma ocorrência de `ReplaceBelowHealthPct`/80 % automático; "Reparar" troca só quebradas + vermelhas.
- [ ] Pane nas aeronaves com peças sai da peça (λ por peça), não da barra; aeronaves sem peças seguem o legado sem mudança.
- [ ] TBO, óleo e ignição vencidos não interditam; anual vencida interdita; anual não troca peças nem zera o contador de 12 h.
- [ ] Faixas e o máximo da curva vêm do `component_catalog.json`.
- [ ] Commit único: `feat(manutencao): faixas de saude por peca, pane por peca e seguro novo (MANUT-FAIXAS-A)`.

## Armadilhas (AGENTS.md › 6 e LICOES.md)
- Probabilidade sempre por tempo (`1 − e^(−λ·dt)`), dt em tempo **simulado**; nada de chance por pacote.
- `IRandomSource` injetado, nunca `new Random()`.
- Coluna nova = propriedade **e** `ALTER TABLE` em `EnsureSchemaUpdatedAsync`.
- Arquivos quentes (`ComponentFlightSession`, `FlightMonitor`, `HangarViewModel`, `AppDbContext`): só as linhas listadas.
- Contexto curto no Hangar (`IDbContextFactory`), sem `catch {}` silencioso.
- Não relaxar testes/oráculos existentes para passar; se um teste antigo contradiz uma decisão desta spec, ajuste citando a decisão no commit.
- `dotnet test` não compila a UI: rode também `dotnet build src/FlightCareer.slnx`.
