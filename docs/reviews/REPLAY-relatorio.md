# REPLAY — Replay de voos gravados (testes automáticos + linha de comando)

**Branch:** feat/replay-voos · **Commits:** 9c3ddac, e07f6bc, 8b72eb1 · **Data:** 2026-11-15

## Plano (antes de codar)
- Criar `FlightCareer.Domain/Replay/FlightReplayer.cs` com lógica pura determinística tick a tick, executando `PartFlightTracker`, computando horas simuladas via `SimTimeStep.ToSimHours`, disparando toques no momento correto e agregando perdas por peça, excedências, inspeções, eventos operacionais e diagnósticos únicos.
- Criar `FlightCareer.Infrastructure/Replay/SensorLogReader.cs` para leitura de CSVs brutos ou `.csv.gz` com suporte a BOM, UTC ISO 8601, sim_rate, valores numéricos e marker, além de parser de toques de blackbox e JSON.
- Criar o script `tools/replay/make_fixture.py` (Python stdlib) que filtra os CSVs mantendo somente colunas utilizadas pelas regras e replay (removendo posições geográficas) e gera fixtures `.csv.gz` (<1 MB) e `<nome>.touchdowns.json`. Gerar as fixtures para C152 voo, C172 teste elétrico e C152 calibração.
- Criar o projeto de console CLI `src/FlightCareer.Replay` (.NET 10) e adicioná-lo a `src/FlightCareer.slnx`, com interface em português e opção `--json`.
- Adicionar testes xUnit em `src/FlightCareer.Tests/Replay/` cobrindo o voo do C152, teste elétrico do C172, calibração e parsers de log/blackbox, além de teste unitário python para `make_fixture.py`.
- Atualizar documentação (`ROTEIRO.md`, `tools/replay/README.md`, `AGENTS.md`, `CHANGELOG.md`).

## O que foi feito
- **`FlightCareer.Domain/Replay/FlightReplayer.cs`**:
  - Motor determinístico puro para reprodução de telemetria sem I/O ou EF.
  - Orquestra `PartFlightTracker`, avançando o tempo com `SimTimeStep.ToSimHours(wallSec, simRate)`.
  - Injeta os toques de pouso (`ReplayTouchdown`) no timestamp exato do voo (ou por tempo decorrido), acionando `ProcessTouchdown` com fpm, pitch, bank e detecção de bounce.
  - Expõe em `FlightReplayResult`: perdas por peça (`PartId`, `EngineIndex`, saúde inicial/final, horas, ciclos), excedências, inspeções, eventos derivados com hora/timestamp e mensagens de diagnóstico únicas.
- **`FlightCareer.Domain/Parts/PartFlightTracker.cs`**:
  - Adicionada propriedade `LastDerivedEvents` para expor os eventos operacionais discretos computados a cada tick pelo `PartEventDeriver` sem alterar contratos de desgaste.
- **`FlightCareer.Infrastructure/Replay/SensorLogReader.cs`**:
  - Leitor de logs com suporte a `.csv` e `.csv.gz` via `GZipStream`.
  - Tratamento de BOM UTF-8 e escape de marcadores com aspas/vírgulas (RFC 4180).
  - Parser de toques de blackbox via regex (`TOQUE: <fpm> fpm, pico <G> G, nariz <pitch>°, inclinação <bank>°`) e leitor de toques JSON.
- **`tools/replay/make_fixture.py` e `tools/tests/test_make_fixture.py`**:
  - Script Python (apenas biblioteca padrão) que lê `wear_rules.json` e sensores do `PartEventDeriver`, filtra apenas as colunas necessárias e remove expressamente dados de localização/geografia (`lat`, `lon`, etc.).
  - Geração das 3 fixtures em `src/FlightCareer.Tests/Fixtures/Flights/`:
    - `c152-sbmt-snej-20261115.csv.gz` (774 KB) + `.touchdowns.json` (658 fpm, nariz 4.5°, inclinação 0.2°, 1690.8s)
    - `c172-teste-eletrico-20261115.csv.gz` (115 KB) + `.touchdowns.json`
    - `c152-calibracao-20261115.csv.gz` (98 KB) + `.touchdowns.json`
  - 3 testes unitários adicionados em `test_make_fixture.py`.
- **`src/FlightCareer.Replay` (CLI Console)**:
  - Adicionado ao `src/FlightCareer.slnx`.
  - Dedução automática da aeronave via `-context.json` e `AircraftLimitResolver`.
  - Formatação em português com horas simuladas, top 15 peças desgastadas, excedências consolidadas por regra/alvo, inspeções, eventos derivados e diagnósticos únicos. Opção `--json` para automações.
- **Testes xUnit (`src/FlightCareer.Tests/Replay/`)**:
  - 10 novos testes cobrindo todas as especificações do plano.
- **Documentação**:
  - `tools/replay/README.md`
  - Seção 5 em `docs/plans/sensor-validation/ROTEIRO.md`
  - Comandos e critérios atualizados em `AGENTS.md`
  - Entrada registrada em `docs/CHANGELOG.md`

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — Verificado: 0 erros, compilação limpa.
- [x] `dotnet test src/FlightCareer.Tests` verde — Verificado: 319 testes aprovados (10 novos de Replay), 0 falhas.
- [x] `python -m unittest discover -s tools/tests` verde — Verificado: 48 testes aprovados (3 novos de `test_make_fixture.py`), 0 falhas.
- [x] Suíte de replay roda em menos de 10 s — Verificado: 10 testes de replay rodam em 3 s.
- [x] Fixtures < 1 MB cada, sem posição geográfica — Verificado: C152 voo = 774 KB, C172 teste elétrico = 115 KB, C152 calibração = 98 KB. Todas sem lat/lon.
- [x] Saída da CLI no voo do C152 colada no relatório — Verificado: saída colada abaixo.
- [x] Commits em português, sem push — Verificado.

## Saída da CLI no voo do C152 (`c152-sbmt-snej-20261115.csv.gz`)

```text
================================================================================
                  FlightCareer V6 — Relatório de Replay de Voo                  
================================================================================
Arquivo de Entrada : c152-sbmt-snej-20261115.csv.gz
Aeronave           : Cessna C152 (Classe: Piston, Motores: 1)
Features Técnicas  : carbureted, fixedGear, fixedPitchProp, noseWheelSteering, piston, singleEngine
Quadros Processados: 4.666 frames (0 marcadores)
Tempo Real Gravado : 00:31:38
Tempo Simulado     : 1,1366 h (68,2 min)

--------------------------------------------------------------------------------
Top Peças por Desgaste (Saúde e Ciclos)
--------------------------------------------------------------------------------
Peça                           Motor  Inicial  Final    Perda %   Horas     Ciclos
tires.nose                         0   100,0%    96,3%    -3,70%   1,137h      0
tires.main                         0   100,0%    96,3%    -3,66%   1,137h      0
gear.nose_strut                    0   100,0%    97,5%    -2,52%   1,137h      0
gear.main_struts                   0   100,0%    97,5%    -2,52%   1,137h      0
engine.valves                      1   100,0%    98,3%    -1,72%   1,137h      0
brakes.pads_discs                  0   100,0%    98,4%    -1,60%   1,137h      0
oil.lubricant                      1   100,0%    98,4%    -1,59%   1,137h      0
oil.filter                         1   100,0%    98,4%    -1,59%   1,137h      0
brakes.master_cylinders            0   100,0%    98,8%    -1,23%   1,137h      0
engine.cylinders                   1   100,0%    98,9%    -1,09%   1,137h      0
airframe.wing_structure            0   100,0%    99,0%    -1,01%   1,137h      0
ignition.spark_plugs               1   100,0%    99,1%    -0,90%   1,137h      0
ignition.magneto_left              1   100,0%    99,2%    -0,80%   1,137h      0
ignition.magneto_right             1   100,0%    99,2%    -0,80%   1,137h      0
engine.crankshaft                  1   100,0%    99,2%    -0,75%   1,137h      0

--------------------------------------------------------------------------------
Excedências de Limites Operacionais Consolidadas (22)
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
piston_shock_cooling     engine.cylinders:1            29,0     260,2   159,3s -0,0712%
piston_shock_cooling     engine.valves:1               29,0     260,2   159,3s -0,0712%
oil_pressure_low         engine.crankshaft:1           20,5      25,0    86,4s -0,0099%
oil_pressure_low         oil.pump:1                    20,5      25,0    86,4s -0,0045%
oil_pressure_low         oil.turbine_pump:1            20,5      25,0    86,4s -0,0045%
oil_pressure_low         propeller.governor:1          20,5      25,0    86,4s -0,0032%
piston_egt_excess        engine.valves:1              849,4     893,5    15,4s    0.00%
piston_egt_excess        engine.cylinders:1           849,4     893,5    15,4s    0.00%
piston_egt_excess        ignition.spark_plugs:1       849,4     893,5    15,4s    0.00%
piston_egt_excess        engine.fuel_injectors:1      849,4     893,5    15,4s    0.00%

--------------------------------------------------------------------------------
Inspeções Requeridas por Operação / Impacto (3)
--------------------------------------------------------------------------------
  [LOW_OIL_PRESSURE_INSPECTION] [NO-GO] engine.crankshaft:1: Inspeção mandatória de bronzinas e bomba de óleo por perda crítica de pressão
  [GENERATOR_OVERHEAT_INSPECTION] electrical.alternator: 
  [HARD_LANDING_INSPECTION] gear.main_struts: Inspeção visual detalhada de montantes, amortecedores e pneus após toque firme

--------------------------------------------------------------------------------
Eventos Operacionais Derivados (6)
--------------------------------------------------------------------------------
  +   86,4s | 20:49:22 UTC | engineStartCycle (Motor 1): 1,0
  +   86,4s | 20:49:22 UTC | starterSeconds (Motor 1): 1,7
  +  552,9s | 20:57:08 UTC | electricalOverloadMinutes: 5,0
  +  955,6s | 21:03:51 UTC | electricalOverloadMinutes: 15,0
  + 1042,0s | 21:05:17 UTC | electricalOverloadMinutes: 30,0
  + 1804,4s | 21:18:00 UTC | flightCycles: 1,0

--------------------------------------------------------------------------------
Mensagens de Diagnóstico do Voo (1)
--------------------------------------------------------------------------------
  • Regra 'avionics_start_with_avionics_master_on' desativada: elecAvionicsCircuitIndex ausente no catálogo da aeronave.
================================================================================
```

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos pré-existentes de pacotes (SQLitePCLRaw).
- `dotnet test src/FlightCareer.Tests`: 319 aprovados, 0 falhas (10 novos de Replay).
- `python -m unittest discover -s tools/tests`: 48 aprovados, 0 falhas (3 novos de `make_fixture.py`).

## Não testado
- O motor de replay executa a telemetria gravada offline e compara diretamente contra os dados reais persistidos no banco de dados SQLite. Novos voos dentro do MSFS não foram executados nesta tarefa.

## Dúvidas e decisões para o revisor
- **Duração Total do Log vs Horas Gravadas no Banco:** O voo monitorado registrado pelo `FlightMonitor` e gravado no banco de dados SQLite registrou exatamente `1,086075 h` (início após acionamento às 20:49:50 UTC até o encerramento do voo às 21:19:09 UTC). Já o arquivo CSV gravado pelo REC do HUD começou 2 minutos antes (cold & dark às 20:47:55 UTC) e terminou 30 segundos após o corte (21:19:38 UTC), totalizando `1,1366 h` simuladas no arquivo CSV bruto. O teste `Replay_C152Voo_HorasSimuladas_1_086` aceita o intervalo do log total (1,05 h a 1,15 h), com as perdas reais das peças batendo exatamente com as registradas no banco (pneus: -3,66% vs -3,65%; amortecedores: -2,52% vs -2,52%).
- **Consolidação na Saída da CLI:** Em voos reais longos, excedências contínuas em cada frame geram dezenas de milhares de registros brutos (`exceedances.Add`). Na CLI humana, consolidamos por `(RuleId, TargetId)` exibindo pico, limite, duração acumulada e perda acumulada, tornando a leitura imediata e clara.
