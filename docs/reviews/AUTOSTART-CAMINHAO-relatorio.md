# AUTOSTART-CAMINHAO — Auto-start tira XP e caminhão de combustível do sim é cobrado

**Branch:** feat/autostart-caminhao · **Commit:** <pendente> · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar `Domain/Services/ProcedurePenalties.cs` com `AutoStartXpPenaltyFraction = 0.10` e função pura `ApplyAutoStartPenalty(xp, autoStartUsed)`.
- Criar `Domain/Fuel/FuelTruckDelivery.cs` com lógica pura, status (`Idle`, `Armed`, `Completed`, `Expired`), relógio injetável, tolerância de subida, detecção de estabilidade por 10 s, expiração por decolagem ou timeout de 10 min, descida ignorada e `ForceComplete`.
- Atualizar `SimConnectService`: adicionar `GROUPS.ObservedPilotInputs = 3`, registrar `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY` como não-mascaráveis ao conectar; expor `OnObservedEvent` e rotear no `OnRecvEvent` por `e.uGroupID`.
- Atualizar `FlightMonitor`: assinar `OnObservedEvent`, registrar uso de auto-start em voo monitorado com log no blackbox; gerenciar entrega de caminhão fora de voo no solo via telemetria com cobrança no banco (contexto curto), atualização do `FuelAmount` e `pilot.Balance`, registro de `FinancialTransaction`, alerta no HUD e blackbox; fechar entrega via `ForceComplete` em `StartMonitoring` antes da sincronização; passar `autoStartUsed` para `ProcessFlightFinishAsync`.
- Atualizar `FlightService.ProcessFlightFinishAsync`: adicionar `bool autoStartUsed = false` no fim dos parâmetros, aplicar penalidade no `totalXP` e adicionar sufixo explicativo na mensagem quando usado.
- Implementar suíte completa de testes xUnit e verificar regressão.

## O que foi feito
- `src/FlightCareer.Domain/Services/ProcedurePenalties.cs`: criada classe pura com constante `AutoStartXpPenaltyFraction = 0.10` e método `ApplyAutoStartPenalty(xp, autoStartUsed)` que reduz 10% do XP (arredondado para baixo, não-negativo).
- `src/FlightCareer.Domain/Fuel/FuelTruckDelivery.cs`: criada máquina de estados com relógio injetável para entrega de combustível via caminhão do simulador no solo (`Idle`, `Armed`, `Completed`, `Expired`). Lida com janela de estabilidade de 10 s, descidas ignoradas, cancelamento por decolagem, timeout de 10 min sem abastecimento e finalização forçada com `ForceComplete`.
- `src/FlightCareer.Application/Services/FlightService.cs`: adicionado parâmetro opcional `bool autoStartUsed = false` no fim de `ProcessFlightFinishAsync`, aplicando `ProcedurePenalties.ApplyAutoStartPenalty` sobre `totalXP` antes de salvar e concatenando o sufixo `" | Partida automática usada (−10% XP)"` à mensagem final.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: adicionado `GROUPS.ObservedPilotInputs = 3`, evento público `OnObservedEvent`, mapeamento não mascarável de `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY` no `ApplyObservedEvents()`, e roteamento em `OnRecvEvent` por `e.uGroupID`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: assinado `OnObservedEvent` e `OnDataReceived` para controle do caminhão e auto-start; adicionado método `ProcessFuelTruckDelivery` usando contexto curto (`_contextFactory.CreateDbContext()`), consultando preço pelo `FuelPriceService.GetCurrentPrice(fuelType, airportTier)`, lançando `FinancialTransaction` de Expense ("Combustível"), atualizando `FuelAmount` e `pilot.Balance`, emitindo alerta HUD e blackbox; no `StartMonitoring`, se houver entrega armada, executa `ForceComplete` e cobra antes de sincronizar o combustível; repassa `autoStartUsed` no `ProcessFlightFinishAsync`.
- `src/FlightCareer.Tests/Fuel/FuelTruckDeliveryTests.cs`: 8 novos testes cobrindo todos os cenários da especificação (subida 20 gal + estabilidade 10 s, ruído abaixo da tolerância, decolagem expirando entrega, timeout de 10 min, descida ignorada, `ForceComplete` com e sem subida, relógio injetável e sobrecargas).
- `src/FlightCareer.Tests/Components/ProcedurePenaltiesTests.cs`: 5 novos testes cobrindo 100 → 90, 0 → 0, sem auto-start inalterado, 7 → 6 e XP negativo.
- `src/FlightCareer.Tests/Components/FlightPersistenceTests.cs`: teste de integração `FlightService_AutoStartUsed_ReducesXpAndMentionsInMessage` validando persistência do XP penalizado no `Flight.XP`, `pilot.ExperiencePoints` e sufixo da mensagem.
- `AGENTS.md`: atualizado na seção 6 com a diretriz sobre Key Events observados sem mascaramento (grupo próprio `ObservedPilotInputs`) e na seção 7 documentando as regras de auto-start e caminhão de combustível.
- `docs/CHANGELOG.md`: adicionada entrada detalhada para a tarefa sob a data 2026-11-16.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — verificado via CLI (compilação com êxito, 0 erros).
- [x] `dotnet test src/FlightCareer.Tests` todo verde (538 + 14 novos = 552 aprovados, 0 falhas).
- [x] `TelemetryDefinitionTests` verde — 5/5 testes aprovados (nenhuma SimVar nova ou modificada em `SimTelemetry`).
- [x] Diff de `SimConnectService.cs` e `FlightMonitor.cs` restrito ao descrito; nenhum `catch {}` silencioso.
- [x] Relatório declara explicitamente: **não testado no MSFS** (o disparo de `ENGINE_AUTO_START` e `REQUEST_FUEL_KEY` para o cliente SimConnect ao pressionar Ctrl+E ou chamar o caminhão só pode ser verificado no simulador).
- [x] CHANGELOG atualizado com `## 2026-11-16 - Auto-start tira XP e caminhão de combustível cobrado (Gemini)`; `AGENTS.md` atualizado nas seções 6 e 7.
- [x] Commit no padrão `feat(career): auto-start reduz XP e caminhao de combustivel do sim e cobrado`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos de compilação.
- `dotnet test src/FlightCareer.Tests`: 552 aprovados, 0 falhas (14 novos testes: 8 em `FuelTruckDeliveryTests`, 5 em `ProcedurePenaltiesTests`, 1 em `FlightPersistenceTests`).
- `TelemetryDefinitionTests`: 5 aprovados, 0 falhas.

## Não testado
- **Não testado no MSFS**: o envio real dos eventos `ENGINE_AUTO_START` e `REQUEST_FUEL_KEY` pelo MSFS para a sessão SimConnect do aplicativo em resposta ao atalho de teclado Ctrl+E e à solicitação de serviço de solo no simulador só pode ser verificado com o MSFS 2024 em execução.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A ambiguidade de namespace entre `FlightCareer.Application.Services.FuelPriceService` e `FlightCareer.Infrastructure.Services.FuelPriceService` no `FlightMonitor` foi resolvida qualificando explicitamente a referência para o serviço de Application conforme orientado na especificação.
