# CALENDARIO-JOGO — Revisão (Claude, 2026-11-19)

**Veredito: APROVADO (com observações)**

- Build 0 erros; `dotnet test` 618/618; fuzz `--runs 5` 230/230 (executor).
- `CareerClock` puro (voo + 0,5 h de solo; paradas de oficina), `Pilot.CareerClockUtc` com `ALTER TABLE` + preenchimento único na migração, seguro e vida em meses (`PartInspectionFlags`) contra o relógio de carreira, data da carreira no Hangar.

## Observações
1. `FlightService.ProcessFlightCancellationAsync` existe mas **ninguém chama** (o cancelamento sai pelo `FlightMonitor`, arquivo quente). Cancelamento de voo ativo ainda não avança o relógio — ligar numa tarefa futura.
2. `RepairKind` passou a zerar todas as peças do grupo (saúde 100, horas/ciclos 0) cobrando o preço proporcional antigo: fora do escopo; a REPARO-PECAS substitui essa lógica pela troca das peças gastas a preço cheio.
3. Não testado na tela nem no MSFS.
