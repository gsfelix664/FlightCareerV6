# Spec REPARO-PECAS — Reparo no Hangar troca peças inteiras (preço cheio)

**Objetivo (1 frase):** o botão "Reparar" de cada grupo deixa de cobrar proporcional ao que falta na barra e passa a **trocar, a preço cheio + mão de obra, só as peças gastas** do grupo, que voltam a 100 % e zeram horas/ciclos.

Decisão do usuário (2026-11-19): "na vida real não tem como trocar apenas 5 % do freio: troca 100 % pagando o preço integral da peça". Escolha: trocar só as peças gastas (abaixo de 80 % ou marcadas na inspeção); sem peça gasta o botão fica "Em dia".

**Limite de tempo:** 1h30. Sem simulações longas.

## Fatos levantados
- Hoje: `Domain/Components/ComponentMaintenance.RepairCost(def, comp, preço)` = `max(minFullRepairCost, preço × repairCostFactor) × déficit/100` (freio a 98 % ≈ $9) e `Repair(...)` sobe a barra. Chamado pelo `HangarViewModel` (`RepairComponent`, `RepairAll`, retífica) e pela `ComponentRowViewModel` (`RepairCost`, `RepairButtonText`, `CanRepair`).
- Barra do grupo = agregado das peças (`PartComponentBridge.AggregateHealth`/`SyncComponentsFromParts`, `min×0,7 + média×0,3`; `minorPart` só na média). No início do voo, barra > agregado + 0,5 eleva as peças (`ReconcilePartsFromComponents`) — depois desta tarefa, o Hangar deve deixar barra = agregado, para a reconciliação não "consertar" peças de graça.
- Preço da peça: `PartPricing.CalculatePartPrice` (depois da CALIBRA-F4b: base × multiplicador da classe) + `CalculateLaborCost` (mão de obra × $100/h) + `CalculateDowntimeHours`.
- Marcas da inspeção: `Domain/Parts/PartInspectionFlags.Evaluate` (amarelo: vida ≥ 90 % h/ciclos/meses ou saúde < 40 %; vermelho: quebrada).
- Calendário de carreira (`CareerClock`, CALENDARIO-JOGO): serviços com tempo parado avançam o relógio — use-o se já estiver na base.

## Contrato
- **Novo** `Domain/Parts/PartReplacementPlanner.cs` (lógica pura):
  ```csharp
  public sealed record PartReplacementItem(AircraftPart Part, PartDefinition Def, double PartPrice, double LaborCost, double DowntimeHours, string Reason);
  public sealed record PartReplacementPlan(IReadOnlyList<PartReplacementItem> Items, double TotalCost, double DowntimeHours);
  public static class PartReplacementPlanner {
      public const double ReplaceBelowHealthPct = 80.0;
      public static PartReplacementPlan Plan(IEnumerable<AircraftPart> partsOfKind, PartCatalog catalog, /*preço*/ ..., DateTime careerNow);
      public static void Apply(PartReplacementPlan plan, double airframeHours, DateTime careerNow); // saúde 100, IsBroken=false, horas/ciclos 0, LastReplaced*, marca limpa
  }
  ```
  - Peça entra no plano se: `IsBroken`, `Health < 80`, ou `PartInspectionFlags.Evaluate(...).Level != None`. Motivo legível ("Quebrada", "Saúde 62 %", "Vida 230/250 h").
  - Custo = soma (preço da peça + mão de obra) por instância (peças por motor contam por motor). Parada do serviço = **maior** parada entre as peças (oficina em paralelo).
- **Hangar** (`HangarViewModel`, `ComponentRowViewModel`, `HangarView.xaml`): para aeronaves com peças (`PartComponentBridge.HasParts`), o custo/botão da linha vêm do plano ("Trocar 2 peças · $1,250" / "Em dia"); a confirmação lista as peças e o motivo; ao confirmar: cobra, aplica o plano, recalcula a barra pelo agregado (`SyncComponentsFromParts`), avança o relógio de carreira pela parada, salva (contexto curto). "Reparar tudo" soma os planos. **Aeronave sem peças**: mantém o comportamento antigo.
- **Retífica** (overhaul): continua com o preço do pacote (`MaintenancePricing`), mas aplica a troca a **todas** as peças dos grupos Motor/Seção quente/Óleo (100 %, horas zeradas).
- **Troca de óleo** (12 h): troca `oil.lubricant`/`oil.filter` (pistão) ou `oil.turbine_oil`/`oil.turbine_filter` (turbina) dentro do preço do serviço.

## Fora do escopo
- Preços/vidas do catálogo, pagamento de missões, cabine, panes persistentes (card "Panes em aberto" segue igual). Arquivos de voo quentes.

## Testes
- `Planner_Freio98_SemPecaGasta_EmDia` (custo 0, plano vazio); `Planner_PecaAbaixo80_TrocaPrecoCheio`; `Planner_PecaVidaVencida_Troca`; `Planner_PecaQuebrada_Troca`; `Planner_PorMotor_ContaInstancias`; `Planner_ParadaEhAMaior`; `Apply_ZeraHorasECiclosESaude100`; `Hangar_BarraIgualAgregadoAposTroca` (sem reconciliação gratuita no voo seguinte); `Overhaul_TrocaPecasDoMotor`.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 5`.
- [ ] CHANGELOG (`## 2026-11-19 - Reparo por troca de peça (Gemini)`); `AGENTS.md` › 7 Hangar.
- [ ] Commit(s); relatório `docs/reviews/REPARO-PECAS-relatorio.md`. "Não testado na tela".
