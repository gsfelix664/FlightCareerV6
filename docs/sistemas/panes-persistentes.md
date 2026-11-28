# Panes persistentes

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Mantém e gerencia discrepâncias e panes mecânicas não resolvidas entre voos consecutivos.
- **Onde está:** `Domain/Entities/AircraftDefect`, `Domain/Services/DefectCatalog`, `UI/Services/PersistentFailureService`, tabela `AircraftDefects`.
- **Regras e integridade:**
  - Panes com efeito no simulador ou críticas sem efeito geram discrepância (`DefectCatalog.CodeFor`), gravadas pelo `FlightMonitor` antes do alerta. Panes nativas do MSFS são detectadas por borda em `SimFailureState`.
  - Gravidade: NO-GO (`IsGrounding`, bloqueia `StartMonitoring` com status "GROUNDED (PANE)") ou DIFERIDA (permite voar com restrição).
  - Atuação contínua e reaplicação: `Watchdog` e `NativeToggle` reaplicam efeitos ao recarregar a aeronave. Comandos do piloto para flaps e alternador não são mascarados.
  - Modo vigia (`FlightMonitor.WatchAircraft`): mantém falhas ativas fora do voo e monitora reparos no banco a cada 3 s para liberar no simulador.
  - Reparo no card "Panes em aberto" no Hangar (`RepairDefect`/`RepairAllDefects`) e no fluxo unificado de "Reparar tudo": orçado via `DefectPartLink`. Panes associadas a peças quebradas são resolvidas com a substituição física da peça (custo zero na pane); trocar a peça no Hangar resolve as panes vinculadas; panes sem peça cobram seu `RepairCost`. Peças quebradas que bloqueiam voo interditam a aeronave antes mesmo das panes NO-GO. Não testado no MSFS após integração com peças.
