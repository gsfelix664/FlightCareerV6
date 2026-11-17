# Spec ECON-SIM — Simulador de carreira com bots (relatório de balanceamento)

**Objetivo (1 frase):** criar um simulador determinístico em que vários bots, cada um com um avião, aceitam missões reais do `MissionGenerator`, voam, pagam combustível, taxas de aeroporto, seguro e manutenção pelas regras do jogo durante N dias, e gerar um relatório que permita rebalancear o pagamento das missões.

**Importante:** esta tarefa **não muda o balanceamento**. Só mede. Nenhuma constante de pagamento, preço, vida útil ou taxa pode ser alterada (exceção: extrair constantes para reuso, sem mudar valor).

## Fatos já levantados
- Pagamento: `Application/Services/MissionGenerator.cs` `CalculateReward` (~l.520-621): `(baseFee + distância·ratePerMile + payload·ratePerPayloadLb) · urgência · originFactor(tier) · bushBonus · marketSpread(0,88–1,14)`; tarifas por subcategoria (l.534-591). Construtor `MissionGenerator(IAppDbContext, IHubDataService?)`, `GenerateMissionsAsync(Airport)`. Usa aleatoriedade interna (confira; para determinismo, aceite um `IRandomSource`/semente opcional no construtor sem mudar o comportamento padrão).
- Custos do voo: `Application/Services/FlightService.cs` ~l.316-384 (combustível `fuelUsed · FuelPriceService.GetCurrentPrice(tipo, tier)` — Avgas 6,80, Jet A-1 5,40 base, inflação ×2 a cada 10 anos desde uma data, variação diária por `DateTime.Today`; taxas pelo `Domain/Economy/GroundServiceCalculator.Calculate(FeeContext)` no destino: pouso, embarque, handling, navegação; estacionamento calculado mas hoje com 0 h).
- Seguro: `EconomyServices.CalculateMonthlyPremium(basePrice)` (30 dias, contratado no Hangar).
- Manutenção no Hangar (`UI/ViewModels/HangarViewModel.cs` + `PilotAircraftWrapper.cs:150-157`): inspeções 50 h $850 (motor+óleo), 100 h $2.500 (tudo, interdita se vencida), 500 h $1.450 (ignição, pistão), retífica 20 % do preço (mín. 25.000) no TBO; reparo por componente `ComponentMaintenance.RepairCost(def, comp, preço)`; panes persistentes (`AircraftDefect`, `DefectCatalog`, custo escalado pelo preço). Os valores de inspeção estão na **UI**: extraia para uma classe pura em Application ou Domain (ex.: `Domain/Components/MaintenancePricing.cs`) **com os mesmos valores**, e faça `PilotAircraftWrapper` usá-la (mudança mínima).
- Desgaste por voo: `Domain/Replay/FlightReplayer` + `Domain/Replay/Synthetic/SyntheticFlightGenerator` (voo procedural acoplado ao layout real, por aeronave; usado no `fuzz`), `PartFlightTracker`, `PartComponentBridge.SyncComponentsFromParts`. Panes: `FailureEngine` (componente) e queima de lâmpada (peças). Custo/h teórico: `PartEconomySimulator`.
- Dados: `Infrastructure/CSV/aircrafts.csv` (preço, cruzeiro, consumo, pax, carga, pista), `hubs_data.json`, `DataSeeder` (Replay já referencia Infrastructure). CLI existente: `src/FlightCareer.Replay` (subcomando `fuzz`).

## O que construir
1. **Núcleo puro** `Application/Simulation/CareerSimulator.cs` (ou Domain, se não precisar de Application) — sem UI, sem SimConnect:
   - Entrada: lista de aeronaves (modelo do `aircrafts.csv`), bots por aeronave, dias, horas de voo por dia (padrão 2), semente, hub inicial (padrão: um aeroporto Tier 2 do hub da aeronave; configurável).
   - Banco: SQLite temporário semeado pelo `DataSeeder` (aeroportos, tipos). Um contexto por operação.
   - **Dia do bot:** enquanto horas voadas no dia < limite e o avião estiver disponível: gera missões no aeroporto atual (`GenerateMissionsAsync`), filtra as compatíveis com a aeronave pelas mesmas regras do jogo (alcance, carga, pista, tier — use `MissionRules`/o filtro que a UI usa; documente qual), escolhe pela política (`best` = maior receita por hora estimada; `random` = sorteio entre compatíveis; padrão `best`) e voa.
   - **Voo:** duração = distância / cruzeiro + 0,25 h (táxi/subida); desgaste rodando `SyntheticFlightGenerator` + `FlightReplayer` com a duração da missão (modo rápido `--fast`: só desgaste natural pelo `PartEconomySimulator`/`PartWearEngine` sem frames — diga a diferença medida entre os modos em 1 avião); combustível = consumo do csv × duração × preço (`FuelPriceService` com **data fixa** injetável para determinismo; se precisar, acrescente sobrecarga com data, sem mudar a assinatura atual); taxas pelo `GroundServiceCalculator` no destino (inclua estacionamento **estimado** de pernoite numa coluna separada, sem somar ao lucro principal); horas e ciclos somados como no `FlightService`.
   - **Panes:** sortear pelo `FailureEngine`/peças com o `IRandomSource` semeado; pane com discrepância NO-GO = avião parado até o reparo; registrar.
   - **Política de manutenção do bot** (configurável): faz cada inspeção quando vence; repara componente com barra < 60 % (ou peça quebrada/marcada em vermelho pela `PartInspectionFlags`); retífica no TBO; paga os preços do jogo (`MaintenancePricing`, `ComponentMaintenance.RepairCost`, reparo de discrepância). Tempo parado: `downtimeHours` das peças/serviços consome horas do dia (24 h de calendário = 1 dia); avião parado não voa.
   - **Seguro:** contrata e renova a cada 30 dias.
   - Saldo inicial generoso para não falir; registrar o saldo, mas não parar a simulação se ficar negativo (marcar "faliu no dia X").
2. **CLI** `dotnet run --project src/FlightCareer.Replay -- career --aircraft "Cessna C152,C172SP G1000,Beechcraft Baron G58,C208B,Asobo TBM 930,Beechcraft King Air,Cessna Citation CJ4,737 Max 8" --bots 5 --days 90 --hours-per-day 2 --seed 1 [--policy best|random] [--fast] [--out <pasta>]`. Padrão de saída: `%LocalAppData%/FlightCareer/career-sim/<data-hora>/` (nunca dentro do repositório).
3. **Saídas:**
   - `flights.csv`: bot, avião, dia, origem, destino, tier, subcategoria, distância, horas, pax/carga, receita, combustível, cada taxa, estacionamento estimado, pane (sim/qual).
   - `maintenance.csv`: bot, avião, dia, horas da célula, serviço (inspeção 50/100/500, reparo de componente X, discrepância, retífica), custo, parada (h).
   - `daily.csv`: bot, dia, horas voadas, receita, custos por categoria, saldo, disponível/parado.
   - `summary.md` (português): por modelo (média dos bots + mín/máx) — horas voadas, voos, **receita/h**, combustível/h, taxas/h, manutenção/h (inspeções, reparos, discrepâncias, retífica), seguro/h, **margem/h** e margem %, horas para pagar o avião, manutenções por 100 h, intervalo médio entre manutenções (h e dias), panes por 100 h, dias parado, % do custo por categoria; mistura de subcategorias aceitas; comparação com `economy_targets.json` (manutenção/h dentro da faixa?). Tabela final com uma linha por modelo.
4. Determinismo: mesma semente → mesmos números (teste).

## Fora do escopo
- Mudar qualquer valor de economia/pagamento/vida/taxa. Arquivos de voo quentes (`FlightMonitor`, `ComponentFlightSession`, `SimConnectService`, `SimFailureActuator`, `SimTelemetry`), `AppDbContext` (só usar), `parts_catalog.json`.

## Testes
- `CareerSim_MesmaSemente_MesmoResultado` (2 bots, 5 dias, `--fast`).
- `CareerSim_InspecaoVencida_BotFazInspecao` (avião com 100 h vencendo → linha de inspeção com o preço do jogo).
- `CareerSim_TaxasDoDestino_IgualGroundServiceCalculator` (1 voo controlado).
- `CareerSim_AviaoParado_NaoVoa` (pane NO-GO → dias parados até o reparo).
- `MaintenancePricing_MesmosValoresDaUI` (850 / 2.500 / 1.450 / 20 % mín. 25.000).

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 5` sem falhas.
- [ ] Rodar a simulação completa do item 2 (5 bots × 8 aviões × 90 dias, modo completo; se passar de ~30 min, rode `--fast` e diga quanto levou cada um) e **copiar o `summary.md` para `docs/reviews/ECON-SIM-resultado.md`** (só o resumo, não os CSVs).
- [ ] CHANGELOG (`## 2026-11-17 - Simulador de carreira com bots (Gemini)`); `AGENTS.md` › 3 (comando `career`).
- [ ] Um commit: `feat(economy): simulador de carreira com bots para balanceamento (ECON-SIM)`.
- [ ] Relatório só com fatos medidos; hipóteses marcadas como hipótese.
