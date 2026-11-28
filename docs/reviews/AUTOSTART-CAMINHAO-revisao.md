# AUTOSTART-CAMINHAO — Revisão (Claude)

**Branch:** `feat/autostart-caminhao` · **Commits:** `9c67ae3`, `0ec90b4` + ajustes do revisor · **Data:** 2026-11-16

## Veredito: APROVADO (com 3 ajustes feitos pelo revisor)

## Conferido (arquivos quentes lidos linha a linha no diff)
- `SimConnectService`: grupo `ObservedPilotInputs = 3` não mascarável com `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN`, `REQUEST_FUEL_KEY`, aplicado ao conectar; `OnRecvEvent` roteia por `uGroupID` (observado → `OnObservedEvent`; resto → `OnMaskedEvent` como antes). Eventos mapeados pelo `GetOrMapEvent` (uma vez).
- `FlightMonitor`: auto-start só com monitoramento ativo, uma vez por voo, blackbox; caminhão armado só fora do voo, no solo, com aeronave vigiada; `ForceComplete` antes da sincronização no `StartMonitoring`; `autoStartUsed` passado ao `FlightService`.
- `FlightService`: parâmetro opcional no fim; `ProcedurePenalties.ApplyAutoStartPenalty` (−10 %, arredonda para baixo) antes de somar/gravar o XP; mensagem "Partida automática usada (−10% XP)".
- `FuelTruckDelivery`: estabilidade de 10 s, decolagem/10 min expiram, descida ignorada, `ForceComplete`; relógio injetável.
- `dotnet build` 0 erros; `dotnet test` **552/552** (14 novos).

## Ajustes feitos pelo revisor
1. **Contexto longo (AGENTS.md › 6):** a cobrança grava saldo e combustível num contexto novo, mas o `MainViewModel` guarda piloto e aeronave num contexto longo; um `SaveChanges` posterior regravaria os valores antigos. Novo evento `FlightMonitor.OnFuelTruckCharged` → `MainViewModel.RefreshAfterFuelTruckAsync` (recarrega saldo; recarrega a aeronave só fora do voo monitorado). No `StartMonitoring` a aeronave recebida (a do contexto longo) recebe o novo combustível direto.
2. **Base da cobrança:** cobrava a subida medida no sim desde o pedido; se o sim não estava sincronizado com o app, o piloto ganhava ou pagava combustível a mais. Agora cobra `combustível final do sim − combustível do app` e grava o valor do sim (limitado à capacidade) — "cobra e aceita".
3. `ProcessFuelTruckDelivery` devolve o novo combustível (antes o `StartMonitoring` só atualizava se a aeronave fosse a vigiada).

## Limitações / não testado
- **Não testado no MSFS:** se Ctrl+E e o caminhão chegam como Key Events a um cliente SimConnect; ordem do evento × subida do combustível.
- Caminhão pedido **depois** de iniciar o monitoramento (fase de preparação) não é cobrado: a sincronização/integridade atual trata a subida (comportamento anterior). Avaliar depois do teste no sim.
- A cobrança vive na UI (sem teste unitário); a lógica de entrega e a penalidade de XP têm testes.
