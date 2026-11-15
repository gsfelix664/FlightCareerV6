# Revisão FIX-PARTS-SENSORS

**Veredito: APROVADO**

- `PartWearEngine`: sensor global pela chave exata, por motor pela chave `:n`; regra por motor avaliada para cada motor (nº de motores pelo maior índice no frame); estresse e rastreio de excedência/inspeção por `(PartId, EngineIndex)` / `<regra>#<motor>`; peças com `EngineIndex 0` recebem o pior motor, uma vez; `ApplyEvent` sem `ToDictionary` por `PartId` (bimotor não quebra).
- **Bug extra achado pelo executor e aceito:** a reserva da taxa nativa lia `nw-damage-rate-<id>`, mas o sensor chega como `nw-overstress-rate-<id>` (conferido no cabeçalho do CSV do voo do C152 de 2026-11-15); a reserva nativa nunca funcionava. Agora lê o nome certo (o antigo fica como alternativa).
- `PartFlightTracker`: diagnósticos únicos por voo (`_reportedDiagnostics`, zerado em `Begin`/`Reset`), também para os do `PartEventDeriver`.
- Teste de contrato `WearRules_TodoSensorExisteNoLayoutReal_PorPerfil` usa o registro e o `SensorLayout` reais com o nº de motores de cada uma das 46 aeronaves.
- Conferido pelo revisor: build 0 erros; `dotnet test` 309/309; Python 45/45.
- **Não testado no MSFS.** Confirmação com dados reais vem na tarefa REPLAY (voo do C152 de 2026-11-15 como teste).
