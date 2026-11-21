# Spec ECON-DIFF — Botões de calibração do pagamento e economia por dificuldade

**Objetivo (1 frase):** tirar a tabela de pagamento das missões do código para um JSON com escala por família e fazer a dificuldade mexer em pagamento, custos e panes, **sem mudar nenhum resultado no Médio** (todas as escalas = 1); a calibração dos números é tarefa à parte do revisor.

Base: `feat/component-health-system`. Branch: `feat/econ-diff`. Decisões do usuário (2026-11-21): `docs/plans/economia-estabilidade.md` › 3 (propostas D e E aprovadas como estão).

## Fatos já levantados (não redescobrir)
- Pagamento: `MissionGenerator.CalculateReward(distance, payload, urgency, subId, paxCount, originTier, destTier, Random)` (Application/Services/MissionGenerator.cs ~724–827): tabela `baseFee` / `ratePerMile` / `ratePerPayloadLb` por prefixo de `subId` (if/else), depois urgência, fator do tier de origem (1,20/1,08/1,00/0,95), bush +25 % (destTier 4) e spread 0,88–1,14. Chamado em `GenerateMissionsInMemory` (~linha 352) e no `MainViewModel` (~linha 602, recálculo ao trocar a origem).
- Dificuldade: `DifficultyMode` (Domain/Entities/Pilot.cs), `Pilot.Difficulty`. Hoje só muda o saldo inicial (`Domain/Services/CareerStart.cs`: Easy 1e9, Medium 1e5, Hard 1e4). Teste `CareerProgressionTests` linha 27 confere o Easy 1e9.
- Panes por peça: `PartFailureEngine.Evaluate` (Domain/Parts/PartFailureEngine.cs ~68) usa `λ = FailureEngine.HazardPerHour(...)`, `p = 1 − e^(−λ·dt)`. Legado: `FailureEngine` (Domain/Components).
- Cobranças do jogo: `FlightService` (combustível e taxas de aeroporto no fim do voo), `HangarViewModel.RunPaidServiceAsync` (UI/ViewModels/HangarViewModel.cs ~199: confirma `cost`, confere saldo, `pilot.Balance -= cost`; todos os serviços do Hangar passam por ele, inclusive seguro), caminhão de combustível (`FuelTruckDelivery`, cobrado no `FlightMonitor`/serviço de vigia).
- Simulador: `Application/Simulation/CareerProgressionSimulator.cs` (`career-stab`): saldo de `CareerStart.GetInitialBalance(difficulty)` (~177), missões por `GenerateMissionsInMemory` (~306/337), débitos com `balance -= ...` / `balance += chosen.RewardMoney - fuelCost - totalFees` (~589), `partFailureEngine.Evaluate` em vários pontos.

## Arquivos a criar/mexer
1. **Novo** `src/FlightCareer.Domain/Economy/DifficultyEconomy.cs` (contrato abaixo).
2. `CareerStart.cs`: `EasyInitialBalance = 1_000_000.0`; atualizar o teste do Easy.
3. **Novo** `src/FlightCareer.Domain/Economy/MissionPayTable.cs` + `src/FlightCareer.Infrastructure/Data/mission_pay.json` (copiado para a saída como os outros JSON de `Data/`). Siga o padrão de carga de `PartCatalog.LoadDefault()`. Se o JSON faltar ou estiver inválido, use os padrões embutidos **idênticos** aos números de hoje e registre com `Debug.WriteLine`.
4. `MissionGenerator.CalculateReward`: lê a família pela tabela (mesmas regras de prefixo e a mesma ordem dos `if` atuais, incluindo `3.1/2.2/1.3` com `ratePerMile = base + max(1,pax) × perPax` e `2.1`/`1.1/1.2` com `+ pax × perPax`), multiplica o resultado final por `family.Scale × payMultiplier`. Novo parâmetro opcional `double payMultiplier = 1.0` no estático; propriedade `public double PayMultiplier { get; set; } = 1.0` na instância (usada pelo método de instância e por `GenerateMissionsAsync`). `GenerateMissionsInMemory` ganha `double payMultiplier = 1.0`.
5. Jogo: onde o `MainViewModel` (ou quem chama `GenerateMissionsAsync`) gera/recalcula missões, defina `PayMultiplier = DifficultyEconomy.For(pilot.Difficulty).PayMultiplier`.
6. Custos no jogo (multiplicar por `CostMultiplier`): combustível e taxas no `FlightService`; `RunPaidServiceAsync` (aplicar ao `cost` logo no início, antes da confirmação; quando ≠ 1, acrescentar à mensagem a linha `Dificuldade {nome}: custos ×{m:0.00}`); cobrança do caminhão de combustível. Não multiplique dentro de `PartPricing`/`MaintenancePricing`/`FuelPriceService` (são usados por testes e pelo simulador com outros fins).
7. Panes: `PartFailureEngine` e `FailureEngine` ganham `public double HazardMultiplier { get; set; } = 1.0`, que multiplica λ (antes do `1 − e^(−λ·dt)`). No início do voo, onde os motores de pane são criados/zerados para o voo (`ComponentFlightSession` / `PartFlightSession` — **arquivo quente**: só a linha que atribui o multiplicador a partir do piloto/aeronave já carregados; não reorganize nada), atribua `DifficultyEconomy.For(difficulty).FailureMultiplier`. Se a dificuldade do piloto não estiver acessível ali sem consulta nova ao banco, passe-a pelo método que já recebe a aeronave/piloto no `FlightMonitor.StartMonitoring` e explique no relatório.
8. Simulador: missões com `payMultiplier` da dificuldade do bot; `CostMultiplier` em combustível, taxas, peças, manutenção, seguro e retífica (em todo débito de custo); `HazardMultiplier` no `partFailureEngine` do bot.
9. CHANGELOG e `AGENTS.md` › 7 (Economia: dificuldade passa a mexer em pagamento/custos/panes; tabela de pagamento em `mission_pay.json`).

## Fora do escopo (não mexer)
- Valores da tabela de pagamento (copie os atuais; todas as `scale` = 1,0). A calibração é do revisor.
- Preços de peças, inspeções, seguro, combustível, taxas; `economy_targets.json`.
- Política de bots, escada de aviões, relatório do `career-stab` (exceto o que for preciso para aplicar os multiplicadores).
- Wear (`PartWearEngine`), eventos externos.

## Contrato
```csharp
namespace FlightCareer.Domain.Economy;
public sealed record DifficultyEconomy(double PayMultiplier, double CostMultiplier, double FailureMultiplier)
{
    public static DifficultyEconomy For(DifficultyMode mode); // Easy (1,25; 0,80; 1,00) · Medium (1,0; 1,0; 1,0) · Hard (0,85; 1,15; 1,25); desconhecido = Medium
    public static string Label(DifficultyMode mode);          // "Fácil" | "Médio" | "Difícil"
}

public sealed record MissionPayFamily(string Id, string[] Prefixes, double BaseFee, double RatePerMile,
    double RatePerPaxMile, bool PaxAtLeastOne, double RatePerPayloadLb, double Scale);
public sealed class MissionPayTable
{
    public static MissionPayTable LoadDefault();     // Data/mission_pay.json, senão Defaults
    public static MissionPayTable Defaults { get; }  // números de hoje
    public MissionPayFamily Resolve(string subId);   // mesma ordem dos if de hoje; "fallback" no fim
}
```
`mission_pay.json`: `{ "version": 1, "families": [ { "id": "instrucao", "prefixes": ["3.4"], ... "scale": 1.0 }, ... , { "id": "fallback", "prefixes": [], ... } ] }`. A instrução `3.4.1` tem `baseFee` 220 e o resto de `3.4` tem 350: modele como duas famílias (`3.4.1` antes de `3.4`). Ids sugeridos: `instrucao_basica`, `instrucao`, `medevac` (`3.3`, `4.3.1`), `aog` (`4.3.2`, `3.2.1`), `agro_bush` (`4.`), `agro_municipal` (`3.2`), `taxi_aereo` (`3.1`, `2.2`, `1.3`), `carga_expressa` (`2.3`), `regional` (`2.1`), `linha` (`1.1`, `1.2`), `fallback`. Atenção à ordem: `4.3.1`/`4.3.2`/`3.2.1` têm de casar antes de `4.`/`3.2`, exatamente como nos `if` de hoje. Prefixo `"4."` casa com `StartsWith("4.")`; ids exatos (`4.3.1`) com igualdade, como hoje.

## Testes que devem existir e passar
- `DifficultyEconomy_ValoresAprovados`: os três modos com os números acima.
- `CareerStart_EasyUmMilhao`: 1.000.000.
- `CalculateReward_TabelaPadrao_IgualAoCodigoAntigo`: para cada família (um `subId` representativo, pax 0 e 6, payload 500, dist 120, urgência 1,3, tiers 1/4), com `new Random(42)` nos dois lados, o valor é igual ao da fórmula antiga — copie a fórmula antiga para um helper privado do teste.
- `CalculateReward_PayMultiplier_Proporcional`: ×0,85 dá 0,85 × o valor (±0,01).
- `CalculateReward_EscalaDaFamilia`: tabela com `linha.scale = 0,5` → metade do valor para `1.1.x`, igual para `3.1.x`.
- `MissionPayTable_JsonIgualAosPadroes`: o `mission_pay.json` do repositório carregado = `Defaults` (todas as famílias e números).
- `MissionPayTable_ResolveOrdem`: `4.3.1` → medevac, `4.3.2` → aog, `4.1.1` → agro_bush, `3.2.1` → aog, `3.2.2` → agro_municipal, `3.4.1` → instrucao_basica, `3.4.2` → instrucao, `9.9` → fallback.
- `PartFailureEngine_HazardMultiplier_DobraLambda`: mesma peça e `dt`, multiplicador 2 → probabilidade `1 − e^(−2λdt)`.
- Testes existentes continuam passando.

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros (com o app aberto use `-p:OutDir=<pasta temporária>`); `dotnet test src/FlightCareer.Tests` 100 %.
- [ ] **Não rode o `career-stab` completo** (estoura o tempo). Rode só `dotnet run --project src/FlightCareer.Replay -c Release -- career-stab --days 30 --bots 1 --difficulty all --policy equilibrado --fast --out <pasta temporária>` e cole no relatório a linha do Médio no dia 30 (deve bater com a rodada de referência: saldo do Médio/equilibrado no dia 30 com 1 bot; o revisor compara).
- [ ] Nenhuma cobrança nova de custo fora dos pontos listados; nenhuma escala ≠ 1 no JSON.
- [ ] Um commit: `feat(economia): tabela de pagamento em JSON e dificuldade com pagamento, custos e panes (ECON-DIFF)`.
- [ ] Relatório diz o que não foi testado ("não testado no MSFS; tela de confirmação do Hangar não vista").

## Armadilhas
- Arquivos quentes (`FlightMonitor.cs`, `ComponentFlightSession.cs`, `MainViewModel.cs`, `HangarViewModel.cs`): mexa só nas linhas necessárias.
- Contextos longos (`MainViewModel`, `FlightService`): ao ler o piloto para a dificuldade, use a entidade já carregada; não faça `SaveChanges` novo por causa disso.
- `IRandomSource`/`Random` injetados; nada de `new Random()` novo na lógica.
- Sem `catch {}` silencioso.
