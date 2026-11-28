# TELEMETRIA-RECARGA — Vigia da ligação com o simulador

**Branch:** fix/telemetria-recarga · **Commit:** pendente · **Data:** 2026-11-27

## Plano (antes de codar)
- Criar `SimLinkWatchdog` em `Domain/Connection` com lógica pura e relógio injetável (`DateTime`).
- Criar testes unitários em `SimLinkWatchdogTests` cobrindo todos os cenários exigidos pela spec.
- Modificar `SimConnectService` mantendo o temporizador de reconexão rodando a cada 5 s para vigiar a ligação via `RequestSystemState("Sim")` e `_linkWatchdog.IsStale(now)`.
- Adicionar `_linkWatchdog.MarkConnected(DateTime.UtcNow)` no `Connect()`, `MarkMessage(DateTime.UtcNow)` no `WndProc`, desarme em `Disconnect()` e tratar exceções de `Dispose()`.
- Validar build da solução com 0 erros e 100% dos testes passando.

## O que foi feito
- `src/FlightCareer.Domain/Connection/SimLinkWatchdog.cs`: classe de lógica pura para monitoramento de silêncio de mensagens SimConnect com timeout configurável (30 s).
- `src/FlightCareer.Tests/Connection/SimLinkWatchdogTests.cs`: suíte de 9 testes unitários xUnit cobrindo construtor com timeout <= 0, estado desarmado, limite de tolerância, silêncio excessivo, reset por nova mensagem, desarme ao desconectar e rearmamento em reconexão.
- `src/FlightCareer.UI/Services/SimConnectService.cs`:
  - Enum `DATA_REQUESTS`: adição de `RequestSimState` no fim do enum.
  - Campos `LinkSilenceTimeout` (30 s) e `_linkWatchdog`.
  - `Initialize`: temporizador de 5 s chama `Connect()` se desconectado, ou `CheckLinkAlive()` se conectado.
  - `Connect`: arma o watchdog em `MarkConnected(DateTime.UtcNow)`. No catch, desarma via `_linkWatchdog.MarkDisconnected()`.
  - `OnRecvOpen`: remoção de `_reconnectTimer?.Stop()` para manter o vigia ativo durante a conexão.
  - `WndProc`: após `ReceiveMessage()` com sucesso, chama `_linkWatchdog.MarkMessage(DateTime.UtcNow)`.
  - Método `CheckLinkAlive()`: invocado via UI dispatcher para checar se `_linkWatchdog.IsStale(now)` ou se `RequestSystemState(RequestSimState, "Sim")` falha, disparando `Disconnect()`.
  - `Disconnect()`: desarma o watchdog e protege `_simConnect.Dispose()` com try/catch para evitar falhas com pipes quebrados.
- `docs/sistemas/simconnect.md`: documentação do vigia da ligação.
- `docs/CHANGELOG.md`: registro da tarefa TELEMETRIA-RECARGA.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.323/1.323 testes aprovados).
- [x] Snapshot `missions_golden.json`: idêntico (não alterado).
- [x] `git diff` do `SimConnectService.cs` contém apenas os passos 1–8 da especificação.
- [x] Declaração explícita de "não testado no MSFS" incluída no relatório.
- [x] `docs/CHANGELOG.md` e `docs/sistemas/simconnect.md` atualizados.
- [x] Commit único no formato `fix(simconnect): reconecta quando o simulador morre sem Quit (TELEMETRIA-RECARGA)`.

## Build e testes
- dotnet build: 0 erros, 81 avisos (nenhum aviso novo).
- dotnet test: 1.323 aprovados, 0 falhas (novos: 9 testes em `SimLinkWatchdogTests`).

## Não testado
- **Não testado no MSFS**: fechar o sim à força com o app aberto; conferir que o status vira "DESCONECTADO" em ≤ 35 s e volta a "CONECTADO" quando o sim reabre; conferir que no menu principal e durante o carregamento de voo o app não fica reconectando em laço — olhar o log `[SimConnect]`.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. Todos os pontos seguiram rigorosamente a especificação do revisor.
