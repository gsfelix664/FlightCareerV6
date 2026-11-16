# Revisão TURB-DEGRADACAO — rodada 1

**Veredito: CORRIGIR**

Conferido pelo revisor: build 0 erros; `dotnet test` 474/474. Aprovados: `TurbineDegradation` (cálculo, arredondamento, `ShouldWrite`), bloco no catálogo, definições de escrita mapeadas uma vez, leitura de conferência em definição separada (sem mexer no `SimTelemetry`), `Reset` só do que o addon gravou, unidade 0–100 conferida na documentação.

## Correções
1. **Número de motores fixo em 1** (`ComponentFlightSession.cs`, chamada `_turbineActuator.Apply(..., 1)`): em bimotores e quadrimotores (King Air, ATR, jatos) só o motor 1 recebe a perda. Acrescente `NUMBER OF ENGINES` (unidade `Number`) **no fim** da definição `TurbineLossesRead` (campo novo no fim do `SimTurbineLossesState`; atualize o teste de contagem) e exponha `TurbineEngineCount` no `ISimConnectService`; a sessão passa esse valor (fallback 1 se 0). Teste: atuador com 2 motores grava nos dois.
2. **`HandleAircraftReloaded` escreve direto no callback do SimConnect.** No `AircraftLoaded` o avião pode ser outro (pistão) ou ainda não estar pronto. Não escreva ali: só marque os motores como "não gravados" (`_lastWritten` = `null`/`None` e `_lastWriteTime` = `default`) para que o próximo `Apply` regrave **depois** de conferir que é turbina. Teste com relógio e sim falsos: recarga → nada gravado no evento; próximo `Apply` em turbina grava; em pistão não grava.
3. **`SetDebugTurbineLosses` público no `SimConnectService` de produção** (sem uso): remover. Testes usam um `ISimConnectService` falso.

## Fora do escopo (continua)
- `SimTelemetry.cs`, `FlightMonitor.cs`, `PersistentFailureService.cs`.

---

# Revisão TURB-DEGRADACAO — rodada 2

**Veredito: APROVADO**

Conferido pelo revisor: build 0 erros; `dotnet test` 476/476.
1. `NUMBER OF ENGINES` no fim da definição `TurbineLossesRead`, exposto como `TurbineEngineCount`; a sessão passa o valor (fallback 1). Teste com 2 motores.
2. `HandleAircraftReloaded` só marca os motores como não gravados; o próximo `Apply` regrava depois de conferir turbina. Teste do ciclo de recarga.
3. `SetDebugTurbineLosses` removido.

Ressalva: o atuador ficou em `Application/Services` (depende só de `ISimConnectService`, ok). Não testado no MSFS: toda a escrita; roteiro no relatório da rodada 1.
