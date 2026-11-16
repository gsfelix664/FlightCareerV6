# NOTA-PASSAGEIROS — Nota de conforto por tempo simulado e sinal do pitch

**Branch:** fix/nota-passageiros-tempo · **Commit:** <pendente> · **Data:** 2026-11-16

## Plano (antes de codar)
1. Criar `FlightQualityRules` (record em `Domain/Services`) com limites operacionais (G 0,4–1,6, bank 30°, pitch up 20°, pitch down 15°, jerk 2,0 /s, slip 5°, vento vertical 5 ft/s, fração turbulência 0,25) e pesos calibrados para paridade com a escala original.
2. Criar `FlightQualitySample` (record em `Domain/Services`) com atitude, acelerações e opcionais, com fábrica `FromTelemetry` invertendo o sinal de pitch do MSFS (`PitchUpDeg = -data.PlanePitch`).
3. Refatorar `FlightQualityService` para `ProcessSample(sample, isPaxFlight, simSeconds)` acumulando segundos simulados por categoria (GSeconds, BankSeconds, PitchSeconds, JerkSeconds, SlipSeconds), tratando pausa/solo, atenuando por turbulência, ignorando atitude/derrapagem em voos de carga e mantendo métodos/contagens obsoletos para compatibilidade.
4. Integrar cirurgicamente no `FlightMonitor.cs` (~linha 931) passando `qualitySimSeconds = isPaused ? 0.0 : SimTimeStep.ToSimSeconds(dt, data.SimulationRate)`.
5. Criar suíte completa de testes xUnit em `FlightQualityServiceTests.cs` cobrindo todos os critérios da especificação.
6. Atualizar `AGENTS.md` e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.Domain/Services/FlightQualityRules.cs`:
  - Record com valores padrão baseados nos limites de conforto FAA/IATA e comentários de origem.
  - Injetável no construtor de `FlightQualityService` (padrão quando não informado).
- `src/FlightCareer.Domain/Services/FlightQualitySample.cs`:
  - Record com campos `OnGround`, `GForce`, `BankDeg`, `PitchUpDeg`, e opcionais `LoadFactorYDot`, `BetaDeg`, `WindVerticalFps`, `MotionPaused`.
  - Método fábrica `FromTelemetry(SimTelemetry data)` que inverte o pitch (`PitchUpDeg = -data.PlanePitch`, pois no MSFS pitch para cima é negativo).
- `src/FlightCareer.Domain/Services/FlightQualityService.cs`:
  - Método principal `ProcessSample(FlightQualitySample sample, bool isPaxFlight, double simSeconds)` que acumula segundos simulados por categoria fora da faixa de conforto.
  - No solo (`sample.OnGround`) ignora completamente.
  - Pausado (`sample.MotionPaused == true`) ignora tempo e não atualiza extremos Max/Min.
  - Passo nulo/negativo (`simSeconds <= 0` ou `NaN`) não acumula tempo, mas atualiza extremos se não pausado.
  - Voo de carga (`isPaxFlight: false`): bank, pitch e slip não são contabilizados (apenas G e jerk).
  - Turbulência atmosférica (`WindVerticalFps` >= 5 ft/s): atenua tempo de G e solavanco pelo fator 0,25×.
  - Sinal do pitch: desconforto quando nariz para cima > 20° ou nariz para baixo > 15°.
  - Exposição de propriedades somente leitura (`GSeconds`, `BankSeconds`, `PitchSeconds`, `JerkSeconds`, `SlipSeconds`, `IsPaxFlight`).
  - Métodos `ProcessFrame` e contadores `ComfortViolations` / `CargoViolations` mantidos com `[Obsolete]` como projeções derivadas para compatibilidade retroativa.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Chamada na linha ~931 atualizada para calcular `qualitySimSeconds` a partir de `dt`, `isPaused` e `data.SimulationRate` via `SimTimeStep.ToSimSeconds` e invocar `ProcessSample(FlightQualitySample.FromTelemetry(data), hasPax, qualitySimSeconds)`. Apenas 2 linhas modificadas.
- `src/FlightCareer.Tests/Quality/FlightQualityServiceTests.cs`:
  - 12 testes unitários cobrindo todos os cenários da especificação.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Documentação adicionada em AGENTS.md seção 7 e entrada registrada no CHANGELOG.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` todo verde (537 aprovados, 0 falhas).
- [x] Diff do `FlightMonitor.cs` limitado à chamada da qualidade (≤ ~5 linhas; exatamente +2/-1 linhas).
- [x] Relatório diz explicitamente: **não testado no MSFS**; sinal do pitch conferido só pelo comentário/documentação (conferir com captura).
- [x] CHANGELOG `## 2026-11-16 - Nota dos passageiros por tempo simulado (Gemini)`; `AGENTS.md` › 7 (Outros sistemas: qualidade do voo por segundos simulados, pausa, carga, pitch).
- [x] Commit `fix(quality): nota de conforto por tempo simulado e sinal do pitch`; correções em commit novo, sem `--amend`.

## Build e testes
- dotnet build: 0 erros, 15 avisos (vulnerabilidades conhecidas em pacotes de terceiros pré-existentes, nenhum aviso novo).
- dotnet test: 537 aprovados, 0 falhas (12 testes novos em `FlightQualityServiceTests.cs`).
  - `SameManeuver_1HzVs4Hz_SameScore`
  - `SimRate4x_CountsSimulatedSeconds`
  - `Paused_ZeroSeconds_NoPenalty`
  - `MotionPaused_NoPenalty`
  - `OnGround_Ignored`
  - `PitchSign_MsfsNoseUpNegative`
  - `CargoFlight_BankAndPitchDoNotCount_GDoes`
  - `Turbulence_WindVertical_ReducesGPenalty`
  - `NoWindData_FullPenalty`
  - `Jerk_And_Slip_CountOnlyWhenProvided`
  - `Calibration_1Hz_MatchesLegacyScale`
  - `LegacyProcessFrame_WorksAsExpected`
  - `FlightPersistenceTests` continuam verdes.

## Não testado
- **Não testado no MSFS**: execução no simulador real para validação em voo;
- **Sinal do pitch**: conferido pelo comentário em `SimConnectService.cs:75` e documentação oficial do SDK do MSFS (conferir com captura no simulador quando houver voo de validação).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A arquitetura seguiu rigorosamente a especificação do revisor, mantendo as propriedades legadas obsoletas com cálculo derivado para compatibilidade total.
