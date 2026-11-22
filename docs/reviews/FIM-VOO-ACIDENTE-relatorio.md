# FIM-VOO-ACIDENTE — Combustível cobrado uma vez, acidente no diário e missões devolvidas na perda total

**Branch:** fix/fim-voo-acidente · **Commit:** (a commitar) · **Data:** 2026-11-22

## Plano (antes de codar)
- `FlightService.ProcessFlightFinishAsync`: remover as cobranças de combustível (débito de saldo e transação financeira "Combustível") nos caminhos de pouso fora de pista e normal, mantendo `outcome.FuelUsed` e definindo `outcome.FuelCost = 0`, com comentário citando a decisão de 2026-11-21; garantir preenchimento de `AircraftModel` no `Flight`.
- `FlightService`: implementar `ProcessTotalLossAsync` gravando o `Flight` (origem, destino = crash site ou "ACIDENTE", `AircraftModel`, duração, distância, XP/Revenue 0), somando horas do piloto (`TotalHours`), avançando relógio de carreira (`CareerClock.AdvanceFlight`) e devolvendo missões ativas ao mercado (`IsAccepted = false`, `PilotId = null`, `OriginICAO = originIcao` e distância recalculada), tudo em um único `SaveChangesAsync` com `_context.ChangeTracker.Clear()` prévio.
- `FlightMonitor`: no ramo `TouchdownKind.Crash`, registrar o FPM medido pelo detector via `_qualityService.RecordTouchdownFpm(ev.TouchdownFpm)` antes de disparar `HandleTotalLossAsync`; em `HandleTotalLossAsync`, capturar os dados do voo antes de `StopMonitoring()` e invocar `_flightService.ProcessTotalLossAsync` antes de `OnTotalLoss`.
- `FlightLogEntry`: mapear status `"PERDA_TOTAL_DESTRUIDA"` para `StatusDisplay` "✖ ACIDENTE", `StatusColor` "#FF5252" e `StatusBg` "#2E1214".
- Testes: criar `FlightFinishTotalLossTests` cobrindo fim de voo sem cobrança de combustível, pouso fora de pista sem cobrança e perda total registrando acidente e devolvendo missões; declarar sobre `FlightLogEntry` (projeto UI WPF).

## O que foi feito
- `src/FlightCareer.Application/Services/FlightService.cs`:
  - Removidas as duas cobranças de combustível (`pilot.Balance -= fuelCost...` e a `FinancialTransaction` "Combustível"), tanto no ramo de pouso forçado fora de pista quanto no fluxo de pouso normal, definindo `outcome.FuelCost = 0` e mantendo `outcome.FuelUsed` para o blackbox/informação. Adicionado comentário citando a decisão de 2026-11-21 (AGENTS.md › 6).
  - Preenchimento explícito de `AircraftModel = aircraft.AircraftType?.Model ?? ""` ao criar o `Flight`.
  - Implementado `ProcessTotalLossAsync` executando a persistência atômica da perda total: cria o `Flight` com `Revenue = 0`, `XP = 0`, destino do local do crash ou "ACIDENTE" se vazio, soma `hours` a `pilot.TotalHours`, avança `pilot.CareerClockUtc` com `CareerClock.AdvanceFlight` e redefine as missões de `activeMissions` com `IsAccepted = false`, `PilotId = null`, `OriginICAO = originIcao` e distância recalculada ao destino, mantendo o `RewardMoney` intacto.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - No ramo `ev.Kind == TouchdownKind.Crash` de `HandleTouchdown`, chamada de `_qualityService.RecordTouchdownFpm(ev.TouchdownFpm);` antes de `HandleTotalLossAsync` para que o resumo do blackbox use o valor exato medido pelo detector de impacto.
  - Em `HandleTotalLossAsync`, captura prévia dos dados do voo (piloto, aeronave, origem, aeroporto mais próximo do crash via `GetNearestAirportICAO`, minutos simulados via `_simulatedFlightTimeSeconds / 60`, distância percorrida e cópia das missões ativas) antes de invocar `StopMonitoring()`; após salvar o estado dos componentes e peças, chamada a `_flightService.ProcessTotalLossAsync`.
- `src/FlightCareer.UI/ViewModels/FlightLogEntry.cs`:
  - Mapeado o status `"PERDA_TOTAL_DESTRUIDA"` para `StatusDisplay` "✖ ACIDENTE", `StatusColor` "#FF5252" e `StatusBg` "#2E1214".
- `src/FlightCareer.Tests/Services/FlightFinishTotalLossTests.cs`:
  - Criados 3 testes unitários com SQLite em memória cobrindo:
    1. `Fim_de_voo_nao_cobra_combustivel_consumido`: sem transação de combustível, `outcome.FuelCost == 0`, `outcome.FuelUsed > 0` e saldo final = inicial + receita - taxas.
    2. `Pouso_fora_de_pista_nao_cobra_combustivel`: sem transação de combustível e saldo inalterado.
    3. `Perda_total_registra_voo_de_acidente_e_devolve_missoes`: gravação de `Flight` de acidente com XP/Revenue 0, avanço de horas e relógio de carreira (`AdvanceFlight`), e devolução das missões ao mercado com `IsAccepted = false`, `PilotId = null`, `OriginICAO = SBJD` e `RewardMoney` intacto.
- `src/FlightCareer.Tests/Components/FlightPersistenceTests.cs`:
  - Ajustado o teste `Segundo_voo_ve_inspecao_e_gastos_feitos_entre_os_voos` para refletir que o voo 2 (sem missões ou taxas) não debita mais combustível, preservando `balanceBefore` e conferindo que o gasto de $2.000 do Hangar não é sobrescrito.
- Documentação atualizada em `AGENTS.md` (seção 7) e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (747/747 testes aprovados).
- [x] Combustível cobrado só na compra do Hangar (`ProcessFlightFinishAsync` não debita mais saldo nem cria transações de combustível).
- [x] Perda total: voo no banco (`Flight`), horas e relógio avançados, missões devolvidas ao mercado; diário mapeado com "✖ ACIDENTE".
- [x] Resumo do blackbox do crash usa a medida do detector (`RecordTouchdownFpm` chamado antes de `HandleTotalLossAsync`).
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `fix(voo): combustivel cobrado so na compra, acidente no diario e missoes devolvidas (FIM-VOO-ACIDENTE)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos (vulnerabilidades em pacotes upstream e warnings CS8602 pré-existentes no simulador).
- `dotnet test src/FlightCareer.Tests`: 747 aprovados, 0 falhas (3 testes novos em `FlightFinishTotalLossTests`).
- `FlightLogEntry_acidente`: `FlightLogEntry` reside no projeto `FlightCareer.UI` (WPF), o qual não é referenciado pelo projeto de testes `FlightCareer.Tests` (conforme antecipado na especificação). A lógica da propriedade pura do switch foi inspecionada e verificada diretamente no código.

## Não testado
- Comportamento visual na tela do app / diário e execução no MSFS (requer simulador aberto).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Todos os itens seguiram estritamente a especificação.
