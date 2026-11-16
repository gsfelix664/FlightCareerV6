# Revisão COMBUSTIVEL-REAL — rodada 1

**Veredito: CORRIGIR**

Conferido pelo revisor: build 0 erros; `dotnet test` 486/486. Aprovados: domínio (`FuelUsageAccumulator` com reinício de motor, `FuelIntegrityMonitor`, `FuelTankWritePlan`), leitura em definição separada (sem mexer no `SimTelemetry`), trava de `UNLIMITED FUEL` no início, invalidação em voo sem pagar missão, consumo real no `FlightService` com conferência de 10 %.

## Correções
1. **`_fuelAccumulator` nunca é zerado** (`FlightMonitor.cs`: campo `readonly ... = new()`, sem reset no `StartMonitoring`). No segundo voo da sessão o consumo do primeiro é cobrado de novo. Acrescente `Reset()` no `FuelUsageAccumulator` (zera totais e o último valor de cada motor) e chame no `StartMonitoring`, junto com o `_fuelIntegrity`. Teste: dois voos seguidos no mesmo acumulador após `Reset` → o segundo conta só o seu consumo.
2. **Sincronização sem conferência.** No `StartMonitoring`, `syncSuccess = true` logo depois de `SetFuelTanks`; o laço de 3 tentativas nunca repete e nada confere a leitura. Faça a conferência **depois** de uma leitura nova do sim (o valor muda só no próximo pacote): marque "sincronização pendente" com o alvo; nos ticks seguintes (até ~5 s), compare `FuelTotalGallons` com o alvo (tolerância); se divergir, regrave (máx. 3 vezes); se ainda divergir, alerta "ERRO DE SINCRONIZAÇÃO" e `StopMonitoring`. Só grave com a aeronave **no solo** (`IsOnGround`); com motor ligado tudo bem gravar (já está no solo, antes do voo). A integridade em voo (`FuelIntegrityMonitor`) só começa depois da sincronização confirmada (senão a queda para o valor do app ou a subida aparecem como evento). Teste da lógica pura de "sincronização pendente" (função/classe no Domain com relógio injetável).
3. **Hangar/Refuel gravam no sim mesmo quando a aeronave abastecida não é a carregada no sim.** `HangarViewModel.TryWriteFuelToSimulator` e `RefuelViewModel` gravam o combustível de qualquer aeronave da frota no avião que estiver no sim. Grave só se a aeronave abastecida for a **aeronave ativa** do piloto (a mesma do `FlightMonitor.WatchAircraft`/modo vigia); caso contrário não grave (o `StartMonitoring` sincroniza no próximo voo). Exponha esse id de forma simples (ex.: propriedade no `FlightMonitor` ou serviço já injetado) sem criar dependência circular.

## Fora do escopo (continua)
- `SimTelemetry.cs`, vazamento de combustível, APU, despejo.
