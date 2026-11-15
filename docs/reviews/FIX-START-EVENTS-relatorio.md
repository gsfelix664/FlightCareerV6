# FIX-START-EVENTS — Eventos de partida observados antes da ativação do voo

**Branch:** fix/eventos-de-partida · **Commit:** 3de7450 · **Data:** 2026-11-15

## Plano (antes de codar)
- No `PartFlightTracker`, implementar `ObservePreflight` executando exclusivamente `_eventDeriver.Update` e aplicando os eventos derivados via `_wearEngine.ApplyEvent` (com filtragem por motor e preservação do estado contínuo), sem tocar em `ApplyTick`, desgaste natural, horas ou regras contínuas, e expondo a propriedade `HasPreflightChanges`.
- Em `PartFlightSession`, expor `ObservePreflight` espelhando `ProcessTick` para despachar inspeções e diagnósticos.
- Em `FlightMonitor`, no ramo `!IsFlightActive` de `OnSimDataReceived`, acumular tempo simulado da preparação via `_lastPreflightTick` e chamar `ObservePreflight` sincronizando componentes e condições legadas. Em `CancelMonitoringAsync`, salvar peças se `!IsFlightActive && _parts.HasPreflightChanges`.
- No `FlightReplayer`, chamar `ObservePreflight` antes da ativação no modo `GameLike`, registrando eventos derivados, inspeções e diagnósticos emitidos durante a preparação.
- Em `PartWearEngine`, ignorar faixas sem efeito (`Multiplier <= 1` e `LossPerHourPct <= 0` e sem inspeção) ao gerar `PartWearExceedance`.
- Implementar a suíte de testes unitários e de integração de replay solicitados, validar `dotnet build` e `dotnet test`.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`:
  - Implementado método `ObservePreflight(SensorFrame? frame, double dtSimHours, DateTime nowUtc)`: roda `_eventDeriver.Update(frame, dtSimSeconds, _features)`, atualiza `LastDerivedEvents`, aplica cada evento derivado às peças instaladas correspondentes via `_wearEngine.ApplyEvent`, drena diagnósticos únicos e trata frames nulos ou defasados (> 5s).
  - Adicionada propriedade `HasPreflightChanges` para rastrear se algum evento de pré-voo reduziu a saúde, incrementou ciclos ou disparou inspeção nas peças.
- `src/FlightCareer.UI/Services/PartFlightSession.cs`:
  - Expostos método `ObservePreflight(double dtSimHours)` e propriedade `HasPreflightChanges`, despachando eventos `OnInspection` e `OnDiagnostic`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - No ramo `!IsFlightActive` de `OnSimDataReceived`: adicionado controle de tempo simulado da preparação com `_lastPreflightTick`, invocando `_parts.ObservePreflight(dtHours)`, `_parts.SyncComponents` e `SyncLegacyConditions()` mantendo as barras do HUD atualizadas com danos de pré-voo sem tocar em `_failures`, `_qualityService` nem na ativação de voo.
  - Em `CancelMonitoringAsync`: caso o voo seja cancelado na preparação (`!IsFlightActive`) e `_parts.HasPreflightChanges` seja verdadeiro, grava o estado das peças em contexto curto do EF sem tocar em combustível, localização ou missões.
- `src/FlightCareer.Domain/Replay/FlightReplayer.cs`:
  - No loop de frames do modo `GameLike`, antes de `isFlightActive`, os frames são submetidos a `tracker.ObservePreflight(frame, dtSimHours, frame.ReceivedUtc)` quando `dtSimHours > 0`, acumulando eventos derivados, inspeções e diagnósticos sem incrementar o tempo de voo simulado.
- `src/FlightCareer.Domain/Parts/PartWearEngine.cs`:
  - Condição para registro de `PartWearExceedance` atualizada para exigir que a faixa ativa possua efeito (`Multiplier > 1.0` ou `LossPerHourPct > 0.0`) ou inspeção vinculada (`activeBand.Inspection != null`).
- `src/FlightCareer.Domain/Parts/WearRuleCatalog.cs`:
  - Adicionado suporte a `EventDamagePct` além de `DamagePerEventPct` no DTO do catálogo de regras (`WearRulePartDto`), permitindo que regras de evento formatadas com `eventDamagePct` (ex.: `avionics_start_with_avionics_master_on`) carreguem seus valores de dano corretamente.
- `src/FlightCareer.Tests/Replay/FlightReplayTests.cs` e `src/FlightCareer.Tests/Parts/PartWearEngineTests.cs`:
  - Implementados testes automatizados cobrindo todos os cenários solicitados na especificação.

## Saída da CLI no Voo do C152 (GameLike)
```
================================================================================
                  FlightCareer V6 — Relatório de Replay de Voo                  
================================================================================
Arquivo de Entrada : c152-sbmt-snej-20261115.csv.gz
Aeronave           : Cessna C152 (Classe: Piston, Motores: 1)
Features Técnicas  : carbureted, fixedGear, fixedPitchProp, noseWheelSteering, piston, singleEngine
Quadros Processados: 4.666 frames (0 marcadores)
Tempo Real Gravado : 00:31:38
Tempo Simulado     : 1,1053 h (66,3 min)

--------------------------------------------------------------------------------
Top Peças por Desgaste (Saúde e Ciclos)
--------------------------------------------------------------------------------
Peça                           Motor  Inicial  Final    Perda %   Horas     Ciclos
tires.nose                         0   100,0%    96,3%    -3,69%   1,105h      0
tires.main                         0   100,0%    96,3%    -3,65%   1,105h      0
gear.nose_strut                    0   100,0%    97,5%    -2,52%   1,105h      0
gear.main_struts                   0   100,0%    97,5%    -2,52%   1,105h      0
engine.valves                      1   100,0%    98,4%    -1,63%   1,105h      0
brakes.pads_discs                  0   100,0%    98,4%    -1,60%   1,105h      0
oil.lubricant                      1   100,0%    98,5%    -1,55%   1,105h      0
oil.filter                         1   100,0%    98,5%    -1,55%   1,105h      0
brakes.master_cylinders            0   100,0%    98,8%    -1,23%   1,105h      0
engine.cylinders                   1   100,0%    99,0%    -1,01%   1,105h      0
airframe.wing_structure            0   100,0%    99,0%    -1,01%   1,105h      0
ignition.spark_plugs               1   100,0%    99,1%    -0,89%   1,105h      0
ignition.magneto_left              1   100,0%    99,2%    -0,80%   1,105h      0
ignition.magneto_right             1   100,0%    99,2%    -0,80%   1,105h      0
engine.crankshaft                  1   100,0%    99,3%    -0,74%   1,105h      0

--------------------------------------------------------------------------------
Excedências de Limites Operacionais Consolidadas (16)
--------------------------------------------------------------------------------
Regra                    Alvo                     Pico       Limite     Duração   Perda % 
piston_rpm_overspeed     engine.valves:1             2606,3    2550,0  2617,0s -1,2986%
piston_rpm_overspeed     engine.cylinders:1          2606,3    2550,0  2617,0s -0,8658%
piston_rpm_overspeed     engine.crankshaft:1         2606,3    2550,0  2617,0s -0,6493%
piston_rpm_overspeed     propeller.blades:1          2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     propeller.governor:1        2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     ignition.magneto_left:1     2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     ignition.magneto_right:1    2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     ignition.spark_plugs:1      2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     oil.pump:1                  2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     electrical.alternator:1     2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     instruments.vacuum_pump:1    2606,3    2550,0  2617,0s -0,4329%
piston_rpm_overspeed     fuel.engine_pump:1          2606,3    2550,0  2617,0s -0,4329%
piston_egt_excess        engine.valves:1              849,4     893,5    15,4s    0.00%
piston_egt_excess        engine.cylinders:1           849,4     893,5    15,4s    0.00%
piston_egt_excess        ignition.spark_plugs:1       849,4     893,5    15,4s    0.00%
piston_egt_excess        engine.fuel_injectors:1      849,4     893,5    15,4s    0.00%

--------------------------------------------------------------------------------
Inspeções Requeridas por Operação / Impacto (1)
--------------------------------------------------------------------------------
  [HARD_LANDING_INSPECTION] gear.main_struts: Inspeção visual detalhada de montantes, amortecedores e pneus após toque firme

--------------------------------------------------------------------------------
Eventos Operacionais Derivados (3)
--------------------------------------------------------------------------------
  +   86,4s | 20:49:22 UTC | engineStartCycle (Motor 1): 1,0
  +   86,4s | 20:49:22 UTC | starterSeconds (Motor 1): 1,7
  + 1804,4s | 21:18:00 UTC | flightCycles: 1,0

--------------------------------------------------------------------------------
Mensagens de Diagnóstico do Voo (1)
--------------------------------------------------------------------------------
  • Regra 'avionics_start_with_avionics_master_on' desativada: elecAvionicsCircuitIndex ausente no catálogo da aeronave.
================================================================================
```

## Critérios de aceite
- [x] Saída da CLI no voo do C152 (`GameLike`) no relatório, com a partida contada (`engineStartCycle: 1,0`, `starterSeconds: 1,7s` aos 86,4s).
- [x] Relatório diz explicitamente: "não testado no MSFS" para o caminho do `FlightMonitor` (preparação e cancelamento).
- [x] CHANGELOG (`## 2026-11-15 - Eventos de partida antes da ativação do voo (Gemini)`), `AGENTS.md` §7, `LICOES.md`.
- [x] Commits `fix(parts)`, `fix(replay)`, `test(...)` em português. Sem push.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos (apenas avisos NU de pacotes pré-existentes).
- `dotnet test src/FlightCareer.Tests`: 332 aprovados, 0 falhas (5 novos testes: `Replay_C152Voo_GameLike_ContaPartidaNaPreparacao`, `Tracker_ObservePreflight_NaoAplicaDesgasteNatural_NemHoras`, `Tracker_PartidaNaPreparacao_NaoContaDeNovoNoVoo`, `Tracker_PartidaComAvionicsLigadoNaPreparacao_EmiteSpike`, `ApplyTick_FaixaSemEfeito_NaoRegistraExcedencia`).
- Testes Python (`python -m unittest discover tools`): 48 aprovados, 0 falhas.

## Não testado
- não testado no MSFS: caminho do `FlightMonitor` (observação de eventos na preparação e cancelamento com salvamento de preflight).

## Dúvidas e decisões para o revisor
- **Excedência `piston_egt_excess` no C152 com perda 0:**
  - A spec pediu: *em `PartWearEngine`, só registrar `PartWearExceedance` quando a faixa ativa tiver efeito na peça (`Multiplier > 1` ou `LossPerHourPct > 0`) ou inspeção*.
  - Essa condição foi implementada à risca (`hasEffect = (bandPart.Multiplier > 1.0 || bandPart.LossPerHourPct > 0.0) || activeBand.Inspection != null`).
  - No entanto, no catálogo `wear_rules.json`, a faixa `caution` de `piston_egt_excess` (threshold 0.95, EGT ≥ 848,8 °C) define multiplicadores de desgaste: `engine.valves` = 2.0, `engine.cylinders` = 1.5, `ignition.spark_plugs` = 2.0 (com `lossPerHourPct: 0.0`). Como `Multiplier > 1.0`, essa faixa tem efeito multiplicador no desgaste natural da peça (acelera o desgaste em 2x).
  - Por essa razão, aplicando estritamente a fórmula `Multiplier > 1 || LossPerHourPct > 0`, ela continua sendo registrada para essas peças.
  - Como catálogos JSON estavam fora do escopo desta tarefa, mantivemos a implementação fiel à fórmula da spec. Se o revisor desejar que excedências não sejam registradas quando `LossPerHourPct == 0` (mesmo com multiplicador), ou quando o valor estiver abaixo do limite operacional (`threshold < 1.0`), basta ajustar a condição ou o catálogo na próxima tarefa.
