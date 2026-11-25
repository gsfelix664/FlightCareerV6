# Panes persistentes

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Mantém e gerencia discrepâncias e panes mecânicas não resolvidas entre voos consecutivos.
- **Onde está:** `Domain/Entities/AircraftDefect`, `Domain/Services/DefectCatalog`, `UI/Services/PersistentFailureService`, tabela `AircraftDefects`.
- **Regras e integridade:**
  - Panes com efeito no simulador ou críticas sem efeito geram discrepância (`DefectCatalog.CodeFor`), gravadas pelo `FlightMonitor` antes do alerta. Panes nativas do MSFS são detectadas por borda em `SimFailureState`.
  - Gravidade: NO-GO (`IsGrounding`, bloqueia `StartMonitoring` com status "GROUNDED (PANE)") ou DIFERIDA (permite voar com restrição).
  - Atuação contínua e reaplicação: `Watchdog` e `NativeToggle` reaplicam efeitos ao recarregar a aeronave. Comandos do piloto para flaps e alternador não são mascarados.
  - Modo vigia (`FlightMonitor.WatchAircraft`): mantém falhas ativas fora do voo e monitora reparos no banco a cada 3 s para liberar no simulador.
  - Reparo no card "Panes em aberto" no Hangar (`RepairDefect`/`RepairAllDefects`). O reparo da discrepância não restaura a saúde do componente ou peça associada. Não testado no MSFS após integração com peças.
