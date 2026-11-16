# TURB-DEGRADACAO-r2 — Ajustes da revisão de degradação de turbina

**Branch:** feat/turb-degradacao · **Commit:** bb6dfe9 · **Data:** 2026-11-16

## Plano (antes de codar)
- Suportar múltiplos motores adicionando `NUMBER OF ENGINES` no fim da definição de leitura de conferência e expondo `TurbineEngineCount` em `ISimConnectService` para consumo em `ComponentFlightSession`.
- Corrigir `HandleAircraftReloaded` em `TurbineDegradationActuator` para não gravar durante o evento, apenas marcando motores como não gravados para que o próximo `Apply` valide se o avião é turbina antes de gravar.
- Remover método de debug desnecessário em produção (`SetDebugTurbineLosses`).
- Mover `TurbineDegradationActuator` para `FlightCareer.Application/Services` para permitir cobertura de testes determinísticos em `FlightCareer.Tests` com `ISimConnectService` falso, validando gravação em 2 motores e ciclo de recarga.

## O que foi feito
- `src/FlightCareer.Application/Interfaces/ISimConnectService.cs`: adicionada propriedade `int TurbineEngineCount { get; }`.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: adicionado `public double NumberOfEngines;` no final de `SimTurbineLossesState`, adicionado datum `"NUMBER OF ENGINES"` (unidade `"Number"`) em `TurbineLossesRead`, exposta propriedade `TurbineEngineCount` e removido método sem uso `SetDebugTurbineLosses`.
- `src/FlightCareer.UI/Services/ComponentFlightSession.cs`: atualizada chamada de `_turbineActuator.Apply` para passar `engineCount` derivado de `_sim.TurbineEngineCount` (fallback 1 se 0).
- `src/FlightCareer.Application/Services/TurbineDegradationActuator.cs`: movido de `UI/Services` para `Application/Services` (com namespace atualizado); `HandleAircraftReloaded` agora apenas limpa o estado de gravação (`_lastWritten = None`, `_lastWriteTime = default`, `_hasWritten = false`), sem disparar escrita para o simulador no callback.
- `src/FlightCareer.Tests/Sensors/TelemetryDefinitionTests.cs`: atualizada asserção para 17 campos e 5 datums.
- `src/FlightCareer.Tests/Components/TurbineDegradationTests.cs`: implementado `FakeSimConnectForTurbine : ISimConnectService` e criados testes `Actuator_ComDoisMotores_GravaNosDois` e `Actuator_Recarga_NadaGravadoNoEvento_ProximoApplyTurbinaGrava_PistaoNaoGrava`.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação e histórico atualizados.

## Critérios de aceite
- [x] Número de motores dinâmico via `NUMBER OF ENGINES` e `ISimConnectService.TurbineEngineCount` — verificado via leitura e repasse na sessão.
- [x] Atuador com 2 motores grava nos dois — verificado com teste unitário `Actuator_ComDoisMotores_GravaNosDois`.
- [x] `HandleAircraftReloaded` não grava no callback e delega para o próximo `Apply` — verificado com teste `Actuator_Recarga_NadaGravadoNoEvento_ProximoApplyTurbinaGrava_PistaoNaoGrava`.
- [x] `SetDebugTurbineLosses` removido de produção — verificado via busca e compilação limpa.
- [x] `dotnet build src/FlightCareer.slnx`: 0 erros.
- [x] `dotnet test src/FlightCareer.Tests`: 476/476 aprovados (0 falhas).

## Build e testes
- dotnet build: 0 erros, 15 avisos pré-existentes da solução (vulnerabilidades em pacotes transitivos SQLite/SqlClient).
- dotnet test: 476 aprovados, 0 falhas (2 testes novos cobrindo o atuador).

## Não testado
- A gravação física em tempo real no MSFS 2024 ao vivo.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. As três correções foram implementadas rigorosamente conforme especificado pelo revisor.
