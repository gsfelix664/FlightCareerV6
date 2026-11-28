# Spec HANGAR-ICONES-PECAS — Estado das peças por ícone (sem % visível) e horas corretas por grupo

**Objetivo (1 frase):** no Hangar, trocar a % de saúde das peças, dos grupos e das caixas do topo por ícones de estado (verde, amarelo, vermelho, "?"), mantendo a % só internamente, e corrigir o texto de horas de cada grupo, que hoje vem do catálogo antigo de componentes.

## Fatos já levantados (não redescobrir)
- Faixas (decisão 2026-11-20, `PartBandSettings` em `Domain/Parts/PartHealthBands.cs`): verde ≥ 40 %, amarelo 40–20 %, vermelho < 20 %. **Não** é 30 %.
- `Domain/Parts/PartHangarView.cs`: `PartHangarView.Build(...)` já calcula `PartKnownBand` (`Unknown`, `Green`, `Yellow`, `Red`, `Broken`) pela saúde **conhecida** (`AircraftPart.KnownHealth`, `KnownAtAirframeHours`), nunca pela real (`Health`). `Unknown` hoje só quando nunca vista. Ordem de gravidade em `BuildList`: Broken, Red, Yellow, Unknown, Green.
- Incerteza que já existe: `component_catalog.json` › `visibility.growthPerHour` = 0,4 (% por hora voada desde a última vez vista), lida em `ComponentCatalog.Visibility`.
- UI: `UI/ViewModels/ComponentRowViewModel.cs` (classes `ComponentRowViewModel`, `PartRowViewModel`, `HangarPalette`), `UI/ViewModels/HangarIcons.cs` (geometrias vetoriais dos grupos), `UI/ViewModels/PilotAircraftWrapper.cs` (`AirworthinessText`/`ConditionText`, linhas ~121–123), `UI/Views/HangarView.xaml` (caixas do topo nas linhas ~331/338; `RangeText` do grupo ~360/380; `LifeText` ~385; `HealthText`/`SeenText` da peça ~433/434).
- Texto de horas do grupo (`ComponentRowViewModel.LifeText`) usa `ComponentMaintenance.RemainingLife` com `lifeHours`/`lifeCycles` do `component_catalog.json` (óleo 50 h, ignição 500 h, pneus 250 pousos, freios 500…). Isso é do sistema antigo e está **errado** para aeronaves com peças: o jogo usa troca de óleo a cada 12 h (`PilotAircraft.HoursUntilNext50hInspection`), inspeção anual 25 h (`HoursUntilNext100hInspection`), revisão de ignição 125 h (`HoursUntilNext500hInspection`), TBO (`EngineTboRemainingHours`, total em `MaintenancePricing.TboHoursFor(EngineClass)`); constantes em `Domain/Components/MaintenancePricing.cs`.
- Aeronave com peças: `PartComponentBridge.HasParts(...)`. Aeronaves sem peças continuam no comportamento antigo (não mexer nelas).

## Regras (decisões do usuário, 2026-11-22)
1. **Estado exibido da peça** (função pura nova no Domain, ver contrato):
   - `Broken` (peça quebrada) e `Red` (conhecida < 20 %): ícone **vermelho de "quebrado"**.
   - `Yellow` (conhecida 40–20 %): **exclamação amarela**.
   - `Green` (conhecida ≥ 40 %): **símbolo verde de "em ordem"** (check).
   - **"?" (incerto):** nunca vista, **ou** quando a peça pode ter caído para uma faixa pior desde a última vez vista: `known − growthPerHour × horasDesdeVista < limite inferior da faixa conhecida` (verde: 40; amarelo: 20). Sem o teto de ±25 % aqui. Vermelho e quebrado nunca viram "?". O "?" é pintado com a cor da última faixa conhecida (cinza `Muted` se nunca vista) e mostra ao lado "visto há X h" (ou "nunca inspecionada").
2. **A % não aparece em lugar nenhum do Hangar** (peça, grupo, caixas do topo, tooltip). Os valores continuam no banco e no código.
3. **Cabeçalho do grupo** (no lugar do `RangeText`): ícone do **pior** estado entre as peças do grupo (ordem: quebrado/vermelho > amarelo > "?" > verde) + contagem compacta só dos estados presentes, ex.: `✓ 12 · ⚠ 2 · ? 1` (usar os mesmos ícones em tamanho pequeno, não caracteres Unicode, se ficar mais limpo).
4. **Caixas do topo:**
   - Aeronavegabilidade: pior estado entre as peças dos grupos **críticos** (`ComponentDefinition.Critical`), com ícone + texto: verde "Em ordem", amarelo "Atenção", vermelho "Risco de pane" (ou "Peça quebrada" se houver `Broken`), "?" "Incerta — faça uma inspeção".
   - Condição geral: contagem de todas as peças por estado (`✓ n · ⚠ n · ✖ n · ? n`).
5. **Texto de horas do grupo** (só aeronaves com peças): Motor → `TBO: X h restantes (de 500)`; Óleo → `Troca de óleo em X h (de 12)`; Ignição → `Revisão de ignição em X h (de 125)`; os demais grupos → sem texto de vida no cabeçalho (a vida fica na nota de cada peça, `LifeNote`, que já existe). Vencido: `… vencida há X h` em vermelho (mesma lógica de `LifeBrush`). Turbinas: Seção quente sem texto; Motor usa o TBO da turbina.
6. Sem peças (`HasParts == false`): manter tudo como está hoje.

## Arquivos a mexer
- `src/FlightCareer.Domain/Parts/PartHangarView.cs`: novo enum `PartDisplayState` e função pura `PartHangarView.DisplayState(...)`; resumo por grupo/aeronave.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`: `PartRowViewModel` expõe ícone/cor/texto do estado e deixa de expor % na tela; `ComponentRowViewModel` ganha estado do grupo, contagem e o novo texto de horas.
- `src/FlightCareer.UI/ViewModels/HangarIcons.cs`: 4 geometrias novas (check, exclamação, quebrado, interrogação).
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs` e o ponto onde o Hangar monta as linhas (`HangarViewModel`), se precisar passar `growthPerHour`, horas da célula e os campos de manutenção.
- `src/FlightCareer.UI/Views/HangarView.xaml`: bindings das peças, dos grupos e das caixas do topo; legenda curta explicando os 4 ícones.
- `src/FlightCareer.Tests/Parts/PartDisplayStateTests.cs` (novo).

## Fora do escopo (não mexer)
- Regras de desgaste, inspeção, walkaround, reparo, `SelectRecommendedKnown`, preços, `AircraftStatusEvaluator` (faixa "PRONTA PARA VOO" do topo), mercado, HUD, `component_catalog.json`.
- Aeronaves sem peças.
- Arquivos quentes (`ComponentFlightSession`, `FlightMonitor`, `SimConnectService`…).

## Contrato
```csharp
namespace FlightCareer.Domain.Parts
{
    public enum PartDisplayState { Green, Yellow, Red, Broken, Uncertain }

    public sealed record PartDisplayInfo(
        PartDisplayState State,
        PartKnownBand LastKnownBand,   // cor do "?"; Unknown se nunca vista
        double? HoursSinceSeen);       // null se nunca vista

    public static partial class PartHangarView   // ou classe estática nova no mesmo arquivo
    {
        // Usa só KnownHealth/KnownAtAirframeHours/IsBroken, nunca Health.
        public static PartDisplayInfo DisplayState(AircraftPart part, double airframeHours,
            double growthPerHour, PartBandSettings? bands = null);

        // Pior estado (Broken > Red > Yellow > Uncertain > Green) e contagem por estado.
        public static (PartDisplayState Worst, IReadOnlyDictionary<PartDisplayState, int> Counts)
            Summarize(IEnumerable<PartDisplayInfo> items);
    }
}
```
`PartHangarRow` pode ganhar o campo `PartDisplayInfo Display` (preferível a recalcular na UI).

## Testes que devem existir e passar (`PartDisplayStateTests`)
- `Nunca_vista_e_incerta`: KnownHealth null → Uncertain, LastKnownBand Unknown, HoursSinceSeen null.
- `Verde_recem_vista_e_verde`: known 80, visto há 0 h → Green.
- `Verde_alta_continua_verde_por_muito_tempo`: known 80, visto há 50 h (80 − 20 = 60 ≥ 40) → Green.
- `Verde_perto_do_limite_vira_incerta`: known 43, visto há 10 h (43 − 4 = 39 < 40) → Uncertain, LastKnownBand Green, HoursSinceSeen 10.
- `Verde_perto_do_limite_ainda_verde`: known 43, visto há 5 h (43 − 2 = 41) → Green.
- `Amarela_vira_incerta_quando_pode_estar_vermelha`: known 25, visto há 15 h (25 − 6 = 19 < 20) → Uncertain, LastKnownBand Yellow.
- `Amarela_recente_e_amarela`: known 30, visto há 1 h → Yellow.
- `Vermelha_nunca_vira_incerta`: known 10, visto há 100 h → Red.
- `Quebrada_e_quebrada_mesmo_sem_inspecao`: IsBroken true, KnownHealth null → Broken.
- `Nao_usa_a_saude_real`: Health 5, KnownHealth 90 visto há 0 h → Green.
- `Resumo_pior_estado_e_contagem`: [Green, Green, Yellow, Uncertain] → Worst Yellow; Counts Green 2, Yellow 1, Uncertain 1.
- `Resumo_quebrada_e_pior_que_vermelha`: [Red, Broken, Green] → Worst Broken.
- Testes do texto de horas do grupo, se a lógica ficar numa função pura (recomendado: `GroupLifeText(kind, aircraft, engineClass)` no Domain): óleo com 7,5 h restantes → `Troca de óleo em 8 h (de 12)`; ignição vencida (−3) → texto "vencida há 3 h"; pneus → vazio.

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [ ] Nenhuma % visível no Hangar de uma aeronave com peças (peças, grupos, caixas do topo, tooltips).
- [ ] Ícones vetoriais (`Geometry`/`Path`), não emoji; cores da `HangarPalette`.
- [ ] Estado calculado só com a saúde conhecida (teste `Nao_usa_a_saude_real`).
- [ ] Texto de horas dos grupos Motor/Óleo/Ignição vem dos campos de manutenção da aeronave; demais grupos sem texto.
- [ ] Aeronave sem peças: tela igual à de hoje.
- [ ] Entrada no fim do `docs/CHANGELOG.md` e atualização do `AGENTS.md` › 7 (Hangar). Relatório diz "não visto na tela / não testado no MSFS".
- [ ] Um commit: `feat(hangar): estado das pecas por icone e horas de manutencao por grupo (HANGAR-ICONES-PECAS)`.

## Armadilhas
- `HangarViewModel` usa `IDbContextFactory` (contexto curto); não introduzir contexto longo.
- Sem `catch {}` silencioso.
- Não vazar a saúde real (`Health`) para a tela nem para o estado.
- `KnownAtAirframeHours` e as horas da célula estão na mesma escala (horas de célula); `HoursSinceSeen` = max(0, célula atual − KnownAtAirframeHours).
