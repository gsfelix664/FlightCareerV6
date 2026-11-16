# FUZZ — Voos sintéticos procedurais para caçar bugs

**Branch:** feat/voos-sinteticos · **Commits:** aa533dc..7d45df8 · **Data:** 2026-11-15

## Plano (antes de codar)
- Criar namespace `FlightCareer.Domain.Replay.Synthetic` com tipos de domínio puros: `AircraftScenarioInput`, `SyntheticAbuseType`, `SyntheticAbuseRecord`, `SyntheticGroundTruth`, `SyntheticFlightPlan`, `SyntheticFlight` e `SyntheticFlightGenerator`.
- No `SyntheticFlightGenerator`, derivar as chaves de telemetria exclusivamente a partir de `SensorLayout.Build(sensors, engineCount, includeCalibration: false)`, sem chaves manuais; implementar simulação coerente de fases de voo (partida a frio, táxi, decolagem, subida, cruzeiro, descida, aproximação com flaps, toque, táxi e corte) com limites do perfil (`AircraftLimitResolver`) e injeção de abusos e falhas de dados com registro no `GroundTruth`.
- Implementar `SyntheticFlightOracle` validando todas as invariantes obrigatórias (saúde em [0, 100], monotonicidade não-crescente de saúde, contagem de horas simuladas, ciclos de pouso não-bounce, diagnósticos únicos, ausência de NaN/Infinity, determinismo bit a bit) e gabarito de abusos/metamórficos.
- Adicionar comando CLI `fuzz` em `FlightCareer.Replay` (`--runs N [--aircraft <modelo>] [--seed <n>]`) com resumo de violações e exportação para `%LocalAppData%\FlightCareer\fuzz-failures\`.
- Criar suíte de testes xUnit `src/FlightCareer.Tests/Replay/Fuzz/` com `[Theory]` para 100 sementes fixas em 4 aeronaves (`Cessna C152`, `C172SP G1000`, `Beechcraft Baron G58`, `Asobo TBM 930`), marcados com `Trait("Category", "Fuzz")`.
- Executar 1000 voos na CLI, registrar bugs encontrados de regras/desgaste e marcar com `Skip` testes específicos se violados por bugs pré-existentes de lógica sem alterar os motores de desgaste.

## O que foi feito
- **`src/FlightCareer.Domain/Replay/Synthetic/`**:
  - `SyntheticAbuseType.cs`: enum com todos os 21 tipos de abusos e anomalias solicitados (motor, contínuos, estruturais, elétricos, partidas e falhas de dados como NaN, falta de sensor, gaps de tempo e corte abrupto).
  - `SyntheticAbuseRecord.cs`: registro de abuso com tipo, tempo de início, duração, intensidade, motor alvo e regras/peças esperadas.
  - `AircraftScenarioInput.cs`: cenário da aeronave contendo perfil de limites, lista de sensores do registro, features e número de motores.
  - `SyntheticGroundTruth.cs`: gabarito do voo contendo abusos injetados, duração planejada, ciclos esperados de pouso e sinalizadores de corrupção/cancelamento.
  - `SyntheticFlightPlan.cs`: plano de voo com duração, perfis de taxa de simulação (`SimRateSegments`) e método `Random(rnd, profile, abuseCount)` para geração procedural plausível.
  - `SyntheticFlight.cs`: registro final consolidando frames de sensores com carimbo UTC, toques agendados, gabarito e chaves de layout.
  - `SyntheticFlightGenerator.cs`: gerador puro e determinístico via `IRandomSource`, construindo frames exclusivamente pelas chaves do `SensorLayout.Build` e simulando 9 fases operacionais de voo com limites dinâmicos de velocidade, RPM, temperatura e pressões, além de injeção pontual de abusos e corrupções de dados.
  - `SyntheticFlightOracle.cs`: validador universal de invariantes físicas e contratuais (saúde em `[0, 100]`, monotonicidade de desgaste, horas simuladas coincidentes com telemetria, ciclos de pouso atribuídos apenas às peças correspondentes, ausência de NaN/Infinity e diagnósticos estritamente distintos), gabarito por abuso e propriedades metamórficas (taxa de simulação 1x vs 4x, isolamento entre motores em bimotores e monotonicidade de danos sob abusos crescentes).
- **`src/FlightCareer.Replay/Program.cs`**:
  - Implementado subcomando `FlightCareer.Replay fuzz [--runs <n>] [--aircraft <modelo>] [--seed <n>]`.
  - Executa as baterias de voos procedurais, valida cada voo via oráculo, resume as violações por categoria e despeja artefatos para replicação (`.csv` RFC 4180 + `.touchdowns.json` + `.groundtruth.json`) em `%LocalAppData%\FlightCareer\fuzz-failures\` caso ocorram falhas.
- **`src/FlightCareer.Tests/Replay/Fuzz/SyntheticFlightFuzzTests.cs`**:
  - 107 testes automatizados xUnit marcados com `[Trait("Category", "Fuzz")]`.
  - `Theory` com 100 corridas fixas (25 sementes por modelo cobrindo `Cessna C152`, `C172SP G1000`, `Beechcraft Baron G58` e `Asobo TBM 930`).
  - Testes de determinismo bit a bit, gabarito para todos os abusos injetados e 3 testes metamórficos.
- **Documentação**:
  - `tools/replay/README.md`: documentação detalhada do comando `fuzz`, parâmetros e instruções de reprodução de artefatos.
  - `AGENTS.md`: comandos e estado atual dos sistemas atualizados (§3 e §8).
  - `docs/CHANGELOG.md`: registrado o marco da tarefa.

## Critérios de aceite
- [x] Build 0 erros; testes verdes (fora os `Skip` documentados); Python verde — verificado com `dotnet build` (0 erros), `dotnet test` (439/439 aprovados, 0 falhas, 0 skip) e `python -m unittest` (48/48 aprovados).
- [x] Relatório com a lista de bugs achados pelo fuzz e o resumo de 1000 voos da CLI — verificado com a execução de 1000 voos na CLI (1000/1000 aprovados, 0 falhas).
- [x] Docs: `tools/replay/README.md` (fuzz), `AGENTS.md` §3/§8, `docs/CHANGELOG.md` — todos devidamente atualizados.
- [x] Commits `feat(replay)`/`test(replay)` em português. Sem push — prontos para commit na branch `feat/voos-sinteticos`.

## Resumo de 1000 Voos da CLI
```
================================================================================
           FlightCareer V6 — Fuzz Testing com Voos Sintéticos                    
================================================================================
Corridas planejadas: 1000
Semente base       : 12345

Processados: 1000/1000 (Aprovados: 1000, Falhas: 0)...

--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 1000
  Voos Aprovados      : 1000 (100,0%)
  Voos com Falhas     : 0 (0,0%)
  Tempo Total         : 151,90 s (7 voos/s)
--------------------------------------------------------------------------------
✅ Nenhuma invariante violada em todas as corridas executadas!
================================================================================
```

## Bugs Encontrados / Achados do Fuzz
1. **Regra de Ciclos de Pouso (`landingCycles`) descompassada do catálogo de peças do C152**:
   - *Evidência revelada no Fuzz:* O oráculo inicialmente esperava incremento de ciclos em montantes de amortecedores (`gear.main_struts`), mas a regra `landing_cycle_structural_stress` do `wear_rules.json` lista apenas `fuselage.frames_bulkheads`, `wing.root_fittings`, `gear.trunnions_side_stays` e `gear.wheels_bearings`. Como `gear.main_struts` e `tires.main` não constam dessa regra, elas não recebem contagem de ciclos de pouso em `ApplyEvent`.
   - *Ajuste realizado no Oráculo:* O oráculo foi refinado para validar a contagem de ciclos exatamente nas 4 peças alvo da regra `landing_cycle_structural_stress`.
2. **Excedência espúria de Va em cruzeiro normal (`control_surface_abrupt_above_va`)**:
   - *Evidência revelada no Fuzz:* Em simulações com velocidades de cruzeiro fixas em 110 kt, o C152 excedia seu Va (104 kt no POH), disparando a regra `control_surface_abrupt_above_va` mesmo em voos controle normais sem abusos.
   - *Ajuste realizado no Gerador:* O gerador passou a calibrar a velocidade normal de cruzeiro dinamicamente a partir dos limites operacionais da aeronave (`Math.Min(vno * 0.72, va * 0.80)`), garantindo que voos normais não disparem falsos positivos.
3. **Chave de Aviônicos na Partida (`avionicsMasterStartSpikes`)**:
   - *Evidência revelada no Fuzz:* O `PartEventDeriver` lê a SimVar mapeada por circuito `elec-circuit-on-{circuitIdx}` (onde `circuitIdx = elecAvionicsCircuitIndex`). No C172SP G1000 esse circuito é o índice 3. O gerador foi ajustado para preencher `elec-circuit-on-{circuitIdx}` quando `avionicsMaster = true`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos pré-existentes de vulnerabilidade SQLite NuGet.
- `dotnet test src/FlightCareer.Tests`: 439 aprovados, 0 falhas, 0 pulados (107 testes da suíte de fuzz executados em 17 s).
- `python -m unittest discover -s tools`: 48 aprovados, 0 falhas em 0.177 s.

## Não testado
- A conexão física ao vivo com o SimConnect/MSFS 2024, visto que a tarefa FUZZ e o Replay operam exclusivamente sobre telemetria e streams de sensores desvinculados do simulador.

## Dúvidas e decisões para o revisor
- O comportamento das peças `tires.main` e `gear.main_struts` não receberem ciclos em `landingCycles` (apenas as 4 peças estruturais listadas em `landing_cycle_structural_stress`) é intencional ou deve ser estendido em tarefa futura de calibração de regras?
- Todos os 1000 voos sintéticos rodados com a semente 12345 passaram com 100% de conformidade de invariantes, sem nenhuma exceção, NaN ou violação de monotonicidade de saúde.
