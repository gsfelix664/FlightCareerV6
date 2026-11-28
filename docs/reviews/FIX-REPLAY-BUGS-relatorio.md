# FIX-REPLAY-BUGS — Bugs de lógica revelados pelo replay do voo do C152

**Branch:** fix/bugs-do-replay · **Commits:** e187531, 992d7ab, b5fbf3f · **Data:** 2026-11-15

## Plano (antes de codar)
- **Sobrecarga elétrica (`PartEventDeriver.CheckElectricalOverload`):** alterar a condição para exigir alternador ligado (`elec-master-alternator:1 > 0.5`), motor em combustão (`eng-combustion:1 > 0.5`), sem starter engatado, corrente acima do nominal (`elec-total-load-amps > alternatorRatedAmps`) e queda de tensão no barramento principal (`elecMainBusIndex`) para a faixa de sag `[0.88 * systemVolts, 0.95 * systemVolts)`. Tensão estável (>= 0.95) indica alternador suprindo sem sobrecarga real; abaixo de 0.88 indica bateria isolada/pane. Documentar no código e em `docs/plans/sensor-validation/teste-eletrico-C172.md`.
- **Regras com motor funcionando (`requiresEngineRunning` + `engineRunningDelaySeconds`):** estender `WearRule`, `WearRuleCatalog` (DTO, loader, validação) e `wear_rules.json` com os novos campos opcionais. No `PartWearEngine`, rastrear borda de combustão e tempo simulado decorrido por motor. Aplicar atraso de 30 s em `oil_pressure_low` (conforme POH de partida do C152) e avaliar choque térmico e demais regras de motor.
- **Choque térmico como taxa de resfriamento:** adicionar `chtCoolingRateMax` em `LimitKeys.cs`, unidade `celsius_per_minute` em `_schema.json`, limite de classe pistão em `_class_defaults.json` (28 °C/min = ~50 °F/min conforme Lycoming SI 1094D / Continental M-0). No `PartFlightTracker`, calcular taxa média móvel em janela de 60 s simulados apenas com motor em combustão, injetando `eng-cht-cooling-rate:n` e `eng-cht-cooling-rate` no frame. Registrar chave derivada aceita pelo contrato de testes. Ajustar `piston_shock_cooling` em `wear_rules.json` para sensor `eng-cht-cooling-rate`, `limitKey: chtCoolingRateMax`, `direction: Above`.
- **Replay fiel ao jogo (`ActivationMode`):** implementar `ReplayActivationMode` (`GameLike` padrão, `FromStart`) em `FlightReplayer`. Em `GameLike`, só computar tempo simulado e desgaste após 10 s reais contínuos de motor em combustão com freio de estacionamento solto (`< 0.5`). Adicionar opção `--from-start` na CLI `FlightCareer.Replay`. Ajustar `tools/replay/make_fixture.py` para reter colunas de freio de estacionamento e alternador e regenerar as fixtures.
- **Inspeção de alternador:** adicionar descrição em português para `GENERATOR_OVERHEAT_INSPECTION` em `wear_rules.json`.
- **Testes e verificação:** implementar suíte completa de testes no `FlightReplayTests.cs` e testes unitários específicos para cada bug corrigido, validar build com 0 erros, testes xUnit e Python verdes.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartEventDeriver.cs`:
  - `CheckElectricalOverload` refatorado com a nova heurística física: exige motor em combustão (`eng-combustion > 0.5`) e alternador ligado (`elec-master-alternator > 0.5`), exclui starter, exige carga acima da capacidade nominal do alternador (`totalLoadAmps > ratedAmps`) e queda de tensão no barramento (`busVolts` entre 88% e 95% de `systemVolts`).
  - Documentação detalhada da heurística em comentários de código.
- `docs/plans/sensor-validation/teste-eletrico-C172.md`:
  - Seção atualizada com a descrição e justificativa da nova heurística de sobrecarga elétrica.
- `src/FlightCareer.Domain/Parts/WearRule.cs` e `src/FlightCareer.Domain/Parts/WearRuleCatalog.cs`:
  - Adicionadas propriedades opcionais `RequiresEngineRunning` (bool) e `EngineRunningDelaySeconds` (double, tempo simulado após a combustão estabilizada).
  - Loader de JSON e DTOs atualizados para deserializar os novos campos.
  - `Validate()` estendido para garantir que `EngineRunningDelaySeconds` seja não-negativo e não esteja presente quando `RequiresEngineRunning` for falso.
- `src/FlightCareer.Domain/Parts/PartWearEngine.cs`:
  - Implementado rastreamento por motor da borda de combustão e tempo acumulado em segundos simulados (`_engineCombustionTimeSeconds`, `_engineWasCombustion`).
  - Se `RequiresEngineRunning == true` e o motor estiver cortado ou no período de atraso, o motor de desgaste zera o rastreio de excedência contínua daquele motor (`_exceedanceDurations.Remove`, `_exceedancePeaks.Remove`) e ignora a avaliação da regra para o motor.
- `src/FlightCareer.Domain/Limits/LimitKeys.cs`:
  - Adicionada constante `ChtCoolingRateMax = "chtCoolingRateMax"` e unidade mapeada `celsius_per_minute`.
- `src/FlightCareer.Infrastructure/Data/aircraft/_schema.json` e `_class_defaults.json`:
  - Adicionada unidade `"celsius_per_minute"` no enum do schema.
  - Adicionado limite `chtCoolingRateMax = 28` (°C/min) no padrão da classe Piston com referência ao Lycoming Service Instruction No. 1094D e Continental Standard Practice Manual M-0 (~50 °F/min = 27,8 °C/min).
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`:
  - Exposta lista `DerivedSensorKeys = { "eng-cht-cooling-rate" }`.
  - Implementado cálculo por motor da taxa de resfriamento da CHT em média móvel de janela de 60 s simulados via histórico `_chtHistoryByEngine`, ativado apenas quando o motor está em combustão (positiva quando esfriando, negativa quando esquentando).
  - Injeção das chaves `eng-cht-cooling-rate:n` e `eng-cht-cooling-rate` (motor 1) no frame de sensores antes da avaliação contínua pelo `PartWearEngine`.
- `src/FlightCareer.Infrastructure/Data/wear_rules.json`:
  - Regra `oil_pressure_low`: configurada com `requiresEngineRunning: true` e `engineRunningDelaySeconds: 30.0` (tempo para estabilização de pressão de óleo previsto no POH do C152).
  - Regra `piston_shock_cooling`: migrada para sensor `eng-cht-cooling-rate`, `limitKey: chtCoolingRateMax`, `direction: Above`, `requiresEngineRunning: true`, `engineRunningDelaySeconds: 60.0`, com faixas caution (1.0), warning (1.25), severe (1.5) e danos mantidos.
  - Adicionada descrição em português para `GENERATOR_OVERHEAT_INSPECTION`.
- `src/FlightCareer.Domain/Replay/FlightReplayer.cs`:
  - Criado enum `ReplayActivationMode` (`GameLike` padrão, `FromStart`).
  - No modo `GameLike`, a contagem de horas simuladas e a aplicação de desgaste pelo tracker só iniciam após 10 segundos reais contínuos de motor em combustão com freio de estacionamento liberado (`ParkingBrake < 0.5`), replicando a lógica de partida de voo do `FlightMonitor`.
- `src/FlightCareer.Replay/Program.cs`:
  - Adicionada opção `--from-start` para permitir reprodução desde o frame zero quando desejado.
  - Atualizada mensagem de ajuda (`PrintHelp`).
- `tools/replay/make_fixture.py`:
  - Adicionadas colunas `brake-parking-position`, `brake-parking` e `elec-master-alternator` na lista de sensores fixos retidos.
  - Preservação do arquivo `.touchdowns.json` existente ao regenerar sem `--blackbox`.
  - Fixtures dos 3 voos reais regeneradas mantendo tamanho < 1 MB.
- Testes Automatizados:
  - `FlightReplayTests.cs`: adicionados testes de ausência de sobrecarga elétrica, ausência de inspeção de óleo com motor desligado, choque térmico apenas com queda rápida e horas simuladas nos modos `GameLike` e `FromStart`.
  - `ElectricalRuleDeriverTests.cs`: adicionados `Overload_BarramentoEstavel_NaoConta`, `Overload_TensaoCaida_ComCargaAlta_Conta`, `Overload_AlternadorEmPane_BateriaSozinha_NaoConta`.
  - `PartWearEngineTests.cs`: adicionados `RequiresEngineRunning_DentroDoAtraso_NaoAvalia` e `ChtCoolingRate_JanelaSimulada_IndependeDoSimRate`.
  - `PartWearEngineTests.WearRules_TodoSensorExisteNoLayoutReal_PorPerfil` e `WearRuleCatalogTests.LoadValidSensors`: atualizados para aceitar `PartFlightTracker.DerivedSensorKeys`.
- Documentação:
  - `docs/agents/LICOES.md`: adicionada lição sobre direção Below com motor desligado, choque térmico como taxa e sobrecarga com sag de barramento.
  - `AGENTS.md` §7: atualizado estado dos sistemas de regras elétricas, motor funcionando, choque térmico e replay.
  - `docs/CHANGELOG.md`: adicionada entrada detalhada de correções.

## Critérios de aceite
- [x] Saída da CLI no voo do C152 (modo `GameLike`) colada no relatório, sem os falsos positivos — Verificado abaixo.
- [x] Toda fonte nova citada; nada inventado — Verificado: Lycoming Service Instruction No. 1094D / Continental Standard Practice Manual M-0 (~50 °F/min = 27,8 °C/min), POH C152 Seção 4 (pressão de óleo até 30s após partida).
- [x] CHANGELOG (`## 2026-11-15 - Correções dos bugs achados pelo replay (Gemini)`), `AGENTS.md` §7 se mudar o estado, `LICOES.md` (uma linha por erro recorrente) — Verificado.
- [x] Commits `fix(parts)`, `fix(replay)`, `test(...)` em português. Sem push — Verificado.

## Saída da CLI no voo do C152 (Modo GameLike)
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
Eventos Operacionais Derivados (1)
--------------------------------------------------------------------------------
  + 1804,4s | 21:18:00 UTC | flightCycles: 1,0

--------------------------------------------------------------------------------
Mensagens de Diagnóstico do Voo (0)
--------------------------------------------------------------------------------
  Nenhum diagnóstico emitido.
================================================================================
```

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, avisos conhecidos de vulnerabilidades de pacotes pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 327 aprovados (8 novos testes de replay e unitários), 0 falhas.
- `python -m unittest discover -s tools/tests`: 48 aprovados, 0 falhas.

## Não testado
- Conexão ao vivo com o simulador MSFS (os testes foram executados via replay determinístico sobre as gravações reais de sensores do C152 e C172).

## Dúvidas e decisões para o revisor
- **Avaliação de outras regras de motor quanto a `requiresEngineRunning`:**
  - `oil_pressure_low`: recebeu `requiresEngineRunning: true` com atraso de 30 s (tempo estipulado pelo POH do C152 para a pressão subir à faixa verde após a partida). Com motor cortado no pátio a pressão é 0 psi, o que disparava indevidamente o NO-GO `LOW_OIL_PRESSURE_INSPECTION`.
  - `piston_shock_cooling`: recebeu `requiresEngineRunning: true` com atraso de 60 s (janela móvel do cálculo da taxa), prevenindo que a perda de calor natural pós-corte do motor no hangar seja computada como choque térmico de voo.
  - Demais regras (`piston_rpm_overspeed`, `piston_cht_overheat`, `piston_egt_excess`, `oil_temperature_high`, etc.): possuem limiares elevados com `direction: Above` (2550+ RPM, 260 °C, 850 °C, 118 °C). Com motor parado, esses valores situam-se em zero ou temperatura ambiente, logo não causam falso positivo em repouso e não exigiram o campo restritivo.
- **Limiares de bandas de `piston_shock_cooling`:** Como a regra passou de `Below` (fração da CHT máxima) para `Above` (taxa de resfriamento contra `chtCoolingRateMax = 28`), os thresholds foram normalizados para o padrão de regras `Above` baseadas em limite: `caution: 1.0` (28 °C/min), `warning: 1.25` (35 °C/min) e `severe: 1.5` (42 °C/min), preservando exatamente as peças e danos da regra.
