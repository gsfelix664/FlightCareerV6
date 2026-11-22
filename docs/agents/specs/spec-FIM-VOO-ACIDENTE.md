# Spec FIM-VOO-ACIDENTE — Combustível cobrado uma vez, acidente no diário e missões canceladas na perda total

**Objetivo (1 frase):** parar de cobrar o combustível duas vezes, registrar a perda total como voo de acidente (diário e banco) com as missões devolvidas ao mercado, e fazer o resumo do blackbox usar a razão de descida medida pelo detector.

## Fatos já levantados (não redescobrir)
- **Cobrança dupla (bug):** desde a decisão de 2026-11-21 (AGENTS.md › 6, abastecimento manual) o piloto **paga** o combustível na compra no Hangar (`HangarViewModel.Refuel` → `RunPaidServiceAsync`, já com `DifficultyEconomy.CostMultiplier`). Mesmo assim `FlightService.ProcessFlightFinishAsync` (`src/FlightCareer.Application/Services/FlightService.cs`) cobra de novo o consumido: caminho de pouso fora de pista (~linhas 206–226, `fuelCostOff`) e caminho normal (~linhas 314–334, `fuelCost`), com transação "Combustível consumido". Voo real de 2026-11-22: 3,3 gal comprados ($22,40) + 2,7 gal "consumidos" ($18,05).
- **Perda total:** `FlightMonitor.HandleTotalLossAsync` (`src/FlightCareer.UI/Services/FlightMonitor.cs`, ~linhas 818–865) zera componentes/peças, grava condições 0 e chama `StopFlightLog("PERDA_TOTAL_DESTRUIDA", fpm, 0)`, mas **não** registra voo no banco, não soma horas do piloto nem avança o relógio de carreira, e as missões aceitas continuam aceitas (no voo real ficou "Navegação (Cross-Country)" SBBP → SBJR aceita num avião destruído). É chamada no crash do detector (`HandleTouchdown`, ramo `TouchdownKind.Crash`, ~linha 1218–1229) e em outros dois pontos (~805 e ~1086, crash do simulador).
- **Resumo do blackbox:** em `HandleTotalLossAsync`, `fpm = _qualityService.TouchdownFPM > 0 ? ... : |_lastData.VerticalSpeed|`. No ramo de crash do `HandleTouchdown` o valor do detector (`ev.TouchdownFpm`) não é registrado antes, então o resumo saiu "Touchdown FPM: 4285" (VS da última linha de 1 s) enquanto o evento dizia 5752 fpm (velocidade normal ao solo, igual ao simulador).
- **Diário:** a tela de logs (`FlightLogsViewModel` + `UI/ViewModels/FlightLogEntry.cs`) lê os CSV do blackbox; `FlightLogEntry.StatusDisplay/StatusColor/StatusBg` (~linhas 72–93) só conhecem `VOO_CONCLUIDO`, `CANCELADO_PELO_PILOTO` e `RESTART`; o crash aparece cru como "PERDA_TOTAL_DESTRUIDA" na cor padrão.

## Passo a passo
1. `FlightService.ProcessFlightFinishAsync`: **remover** as duas cobranças de combustível (débito no saldo e a `FinancialTransaction` "Combustível consumido"), nos dois caminhos. Manter o cálculo do consumido para informação: `outcome.FuelUsed` continua; `outcome.FuelCost = 0`. Não mexer no desconto do combustível do avião (o que atualiza `FuelAmount` continua como está). Comentário curto citando a decisão de 2026-11-21. **Não** mexer no `CareerProgressionSimulator` (lá não há compra no Hangar; a cobrança por voo é a única).
2. `FlightService`: novo método público
   ```csharp
   Task ProcessTotalLossAsync(Guid pilotId, Guid aircraftId, string originIcao, string crashSiteIcao,
       double flightDurationMinutes, double distanceNM, List<Mission> activeMissions)
   ```
   que, num único `SaveChangesAsync` do contexto do serviço (siga o padrão do `ProcessFlightFinishAsync`, incluindo `ChangeTracker.Clear()` no início se ele fizer):
   - grava um `Flight` (Origin = `originIcao`, Destination = `crashSiteIcao` ou `"ACIDENTE"` se vazio, `DurationHours` = minutos/60, `DistanceNM`, `Revenue` 0, `XP` 0, `AircraftModel` como no fim de voo normal);
   - soma as horas ao piloto (`TotalHours`) e avança `CareerClockUtc` com `CareerClock.AdvanceFlight`;
   - **não** cobra combustível nem taxas, **não** dá XP nem receita;
   - para cada missão de `activeMissions` (buscar a entidade pelo `Id` no contexto): `IsAccepted = false`, `PilotId = null`, `OriginICAO = originIcao`, distância recalculada de `originIcao` ao destino como o método já faz para missões não entregues; o `RewardMoney` **não muda**.
3. `FlightMonitor` (**arquivo quente** — mexer só nisto):
   - no ramo `TouchdownKind.Crash` de `HandleTouchdown`, antes de `_ = HandleTotalLossAsync();`, chamar `_qualityService.RecordTouchdownFpm(ev.TouchdownFpm);` para o resumo usar a medida do detector;
   - em `HandleTotalLossAsync`, depois de parar o log e antes de `OnTotalLoss`, chamar `ProcessTotalLossAsync` com: piloto e avião do voo, origem do voo, aeroporto mais próximo (`GetNearestAirportICAO` com a última posição; já existe), minutos simulados (`_simulatedFlightTimeSeconds / 60`), distância percorrida (`_distanceTraveled`, se é o que o fim de voo normal usa) e as missões ativas do voo. Use o mesmo acesso ao `FlightService` que o fim de voo normal (~linha 1460) usa. Capturar esses valores **antes** de `StopMonitoring()` (que zera o estado). Exceção registrada com `Debug.WriteLine`, sem `catch {}` vazio.
4. `FlightLogEntry`: `PERDA_TOTAL_DESTRUIDA` → `StatusDisplay` "✖ ACIDENTE", `StatusColor` "#FF5252", `StatusBg` "#2E1214".

## Fora do escopo
- `CareerProgressionSimulator`, seguro, cálculo de perda total, mercado, HUD, demais caminhos do `FlightMonitor`.

## Testes (xUnit, padrão de `Tests/Services/CareerClockTests.cs` com SQLite em memória)
- `Fim_de_voo_nao_cobra_combustivel_consumido`: voo normal com consumo (initialFuel 20, finalFuel 15) e uma missão entregue → nenhuma `FinancialTransaction` de categoria "Combustível"; saldo final = saldo inicial + receita − taxas; `outcome.FuelCost` = 0 e `outcome.FuelUsed` > 0.
- `Pouso_fora_de_pista_nao_cobra_combustivel`: `isOffFieldLanding: true` → nenhuma transação "Combustível".
- `Perda_total_registra_voo_de_acidente_e_devolve_missoes`: duas missões aceitas (origem SBJD) → depois de `ProcessTotalLossAsync(..., originIcao "SBJD", crashSiteIcao "SBBP", 13 min, ...)`: um `Flight` com Revenue 0 e XP 0 e destino SBBP; `TotalHours` +13/60; relógio avançado em 13 min + 0,5 h; as duas missões com `IsAccepted` false, `PilotId` null, `OriginICAO` SBJD e `RewardMoney` igual ao de antes; nenhuma transação financeira nova.
- `FlightLogEntry_acidente`: `Status = "PERDA_TOTAL_DESTRUIDA"` → "✖ ACIDENTE" e cor "#FF5252" (se a classe for testável sem WPF; senão, declarar no relatório).

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (ajuste testes existentes que esperavam a cobrança de combustível e explique no relatório).
- [ ] Combustível cobrado só na compra do Hangar.
- [ ] Perda total: voo no banco, horas e relógio, missões devolvidas; diário mostra "✖ ACIDENTE".
- [ ] Resumo do blackbox do crash usa a medida do detector.
- [ ] CHANGELOG e AGENTS.md › 7 (Economia e combustível: "cobrança … pelo consumo real" muda para "pago na compra no Hangar; o consumo medido só desconta dos tanques e vai para o blackbox"; FlightMonitor/perda total). Relatório diz "não testado no MSFS".
- [ ] Um commit: `fix(voo): combustivel cobrado so na compra, acidente no diario e missoes devolvidas (FIM-VOO-ACIDENTE)`.

## Armadilhas
- Contextos longos (AGENTS.md › 6): o `FlightService` usa contexto de longa duração; limpe o rastreamento antes de ler para alterar, como o método de fim de voo faz.
- `HandleTotalLossAsync` roda em `async void`/fire-and-forget: capture os dados do voo antes de `StopMonitoring()`.
