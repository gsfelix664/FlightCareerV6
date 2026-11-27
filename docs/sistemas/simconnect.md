# SimConnect e MSFS

> Detalhes das armadilhas resumidas no `AGENTS.md` › 6. Leia antes de mexer nesta área; ao mudar o estado, atualize este arquivo.

## SimConnect

- **`SimTelemetry` e `AddToDef` precisam ter a mesma ordem.** O struct é lido por posição de memória: **só acrescente campos no fim**, nos dois lugares, com a mesma unidade.
- **Frequências:** telemetria principal (`SimTelemetry`) 1 s; panes (`SimFailureState`) 6 frames; voo rápido (`SimFastFlightState`, toque/crash/G) 2 frames; sensores genéricos 30 frames. Definições separadas.
- **Eventos:** use `SimConnectService.SendEvent(nome, dado)`, que mapeia cada evento uma única vez. Não chame `MapClientEventToSimEvent` de novo com o mesmo ID.
- **Eventos de falha que existem no SDK:** `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`. **Não existem** eventos diretos de pane de alternador, pneu, trem, flap, magneto ou pressurização: alternador = `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` via `SendEventEx`; flaps = `WriteFlapSurfacePercent`; magnetos pela SimVar; demais `effect: None` (detalhes: `docs/sistemas/desgaste-falhas.md`).
- **Toggles reparam se enviados duas vezes:** antes de enviar, confira o estado pela SimVar (`SetFailure` já faz isso).
- **Key Events observados sem mascarar:** use grupo próprio não mascarável (`GROUPS.ObservedPilotInputs = 3`, `maskable: false`, prioridade `SIMCONNECT_GROUP_PRIORITY_DEFAULT`). Evento mapeado com `GetOrMapEvent` pertence a um só grupo: roteie no `OnRecvEvent` por `e.uGroupID` (`OnObservedEvent` × `OnMaskedEvent`). Observa `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY`.
- **SimVars escritas pelo app:** `ENG ON FIRE:1` (`SetEngineFire`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`SetOilLeakedPercent`).
- Confirme nomes de eventos e SimVars na documentação do SDK do MSFS 2024 antes de usar. Não invente nomes.
- **Escrita de `FUELSYSTEM TANK QUANTITY:n` define o total, a leitura devolve o utilizável:** grave `utilizável + (FUELSYSTEM TANK CAPACITY:n − FUELSYSTEM TANK USABLE CAPACITY:n)` (`FuelTank.ToSimWriteValue`, aplicado em `SimConnectService.SetFuelTanks`). Tanques legados `FUEL TANK * QUANTITY`: leitura e escrita usam o total (com não utilizável). Pendente: distribuição proporcional em `FuelTankWritePlan.Distribute` põe em todos os tanques com carga parcial (detalhes: `docs/sistemas/combustivel.md`).
- **SimVar com nome inválido desloca o struct:** datum rejeitado (`NAME_UNRECOGNIZED`) encurta o pacote e desloca os campos seguintes (o marshaling lê além do fim) (ex.: `PARTIAL PANEL STATIC` **não existe**). O log `[SimConnect EXCEPTION]` mostra nome rejeitado; `TelemetryDefinitionTests` confere contagem.
- **Vigia da ligação (TELEMETRIA-RECARGA):** para detectar sim fechado à força (sem Quit), o temporizador de reconexão (5 s) envia `RequestSystemState(RequestSimState, "Sim")`; 30 s de silêncio (`SimLinkWatchdog`) ou exceção na chamada desconectam para religar a reconexão automática.

## MSFS 2024 nativo

- **Addon é a fonte da verdade:** wear and tear nativo fica ligado, mas a saúde é calculada e mantida pelo addon. O `WEAR AND TEAR LEVEL` nunca entra no Hangar. `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` é só sensor (faixa vermelha), nunca vira % de desgaste. Limites vêm de catálogo fixo por aeronave (`Data/aircraft/`).
- **Panes nativas não ligam SimVars de falha de motor:** `IS FAILED:<id>` (0 → 1) indica pane, mas **não** liga `ENG FAILED:1` nem `GENERAL ENG DAMAGE PERCENT`. O `PersistentFailureService` lê `nw-is-failed-<id>` do `SensorFrameReader` e grava a pane via Tabela 2 (`Domain/Components/NativeWearFailures.cs`). SimVars `WEAR AND TEAR *` são somente leitura, ID 0–49 (`:39` = pistão) (detalhes: `docs/sistemas/desgaste-falhas.md`).
- **Crash no 2024:** `CRASH FLAG` não aparece no 2024; impacto e crash são detectados pelo `TouchdownDetector` do addon.
- **SimVars gravadas pelo addon:** somente `TURB ENG N1 LOSS`, `FUEL EFFICIENCY LOSS`, `ITT COOLING EFFICIENCY LOSS` e `THRUST EFFICIENCY LOSS` por motor (`TurbineDegradationActuator`). Demais são somente leitura.
- **Abastecimento manual (2026-11-21):** o addon **não grava** nos tanques. O piloto abastece no menu do sim a quantidade paga; briefing e `StartMonitoring` conferem a bordo × pago; reabastecimento em voo invalida a missão e cobra consumo real (`GENERAL ENG FUEL USED SINCE START:1..4`) (detalhes: `docs/sistemas/combustivel.md`).
