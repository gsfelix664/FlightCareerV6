# Revisão FIX-START-EVENTS

**Veredito: APROVADO**

- `FlightMonitor.cs` lido linha a linha: na fase PREPARAÇÃO, `ObservePreflight` com relógio próprio (`_lastPreflightTick`, tempo simulado por `SimTimeStep`, zerado em `StartMonitoring`) só enquanto `!IsFlightActive`; lógica de ativação intacta. `CancelMonitoringAsync` salva só as peças se houve evento na preparação e o voo não foi ativado.
- `PartFlightTracker.ObservePreflight`: só eventos derivados (sem desgaste natural, horas ou regras contínuas), mesmo deriver do voo (sem contagem dupla da borda do starter).
- Conferido pelo revisor: build 0 erros; `dotnet test` 332/332; Python 48/48; replay `GameLike` do C152 com `engineStartCycle` 1 e `starterSeconds` 1,7 s, horas 1,105 h.
- `piston_egt_excess` com "perda 0%": a faixa de cautela dobra o desgaste natural (multiplicador 2,0) e não tem perda extra por hora; o relatório mostra só a perda extra. Aceito; melhorar a exibição numa tarefa futura.
- **Não testado no MSFS** (preparação e cancelamento no `FlightMonitor`).
