# METRICAS-B — Métricas de sistemas e garantia de que nenhuma regra fica sem cálculo

**Branch:** feat/metricas-b · **Commit:** pendente · **Data:** 2026-11-22

## Plano (antes de codar)
- Implementar no `PartEventDeriver` o cálculo das 8 métricas de sistemas restantes (`icingExposureMinutes`, `deiceCycles`, `propDeiceCycles`, `glowPlugCycles`, `dryRunPumpMinutes`, `lowHydraulicPressureMinutes`, `pressurizationCycles`, `rapidDecompressionDeltaPsf`) conforme o contrato da especificação, com reset completo no método `Reset()`.
- Expor `ProducedMetrics` em `PartEventDeriver` e `PartFlightTracker`.
- Atualizar a regra `turbine_start_cycle` no `wear_rules.json` para usar `turbineStartCycle`, sincronizando o catálogo com o evento emitido pelo Deriver.
- Criar a suíte `WearRuleMetricCoverageTests` garantindo que nenhuma regra de evento use métrica sem produtor e que nenhuma regra contínua tenha sensores desconhecidos.
- Implementar testes unitários para cada uma das 8 métricas verificando os cruzamentos de faixas, comportamento com sensores zerados e proteção de features ausentes.
- Implementar o comando `coverage` no `FlightCareer.Replay` e gerar o relatório detalhado `docs/plans/cobertura-regras-por-aviao.md` cobrindo todos os 46 modelos de aeronaves.
- Executar build completo da solução, rodar todos os testes xUnit e o fuzz test dos 46 modelos para garantir 100% de estabilidade.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartEventDeriver.cs`:
  - Adicionado conjunto estático `ProducedMetrics` expondo as 24 métricas operacionais derivadas e produzidas pela classe.
  - Implementado cálculo das 8 métricas de sistemas:
    - `icingExposureMinutes`: tempo acumulado no ar com `inst-structural-ice-pct` $\ge 5\%$ ou `inst-pitot-ice-pct` $\ge 10\%$, emitindo em 5, 15 e 30 minutos com reinício após 10 min sem gelo.
    - `deiceCycles`: contagem de acionamentos de `inst-structural-deice-switch` (0→1) para aeronaves com `deIceBoots`, emitindo em 2, 6 e 12 ciclos.
    - `propDeiceCycles`: contagem de acionamentos de `inst-prop-deice-switch` (0→1), emitindo em 2, 6 e 12 ciclos.
    - `glowPlugCycles`: contagem de partidas de motor em aeronaves `diesel`, emitindo em 1, 3 e 5 ciclos.
    - `dryRunPumpMinutes`: tempo acumulado com bomba de combustível ligada em tanque $< 0,5$ gal ou no solo sem combustão, emitindo em 2, 5 e 10 minutos com reinício após 1 min fora.
    - `lowHydraulicPressureMinutes`: tempo acumulado com pressão hidráulica abaixo de 50% do pico após armar com pressão $\ge 500$ psi e motor em combustão, emitindo em 3, 8 e 15 minutos.
    - `pressurizationCycles`: contagem de ciclos de pressurização em aeronaves `pressurized` (diferencial subindo acima de 2 psi e retornando abaixo de 0,5 psi), emitindo em 1, 2 e 4 ciclos.
    - `rapidDecompressionDeltaPsf`: queda brusca do diferencial $\ge 150$ psf/s em janela de 1 s simulado em aeronaves `pressurized`, emitindo o pico após estabilização ($< 20$ psf/s por 5 s).
  - Atualizado método `Reset()` para limpar todos os novos acumuladores, filas e conjuntos de thresholds emitidos.
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`:
  - Exposto conjunto estático `ProducedMetrics` consolidando `PartEventDeriver.ProducedMetrics` com as métricas de toque rápido (`touchdownFpm`, `touchdownPitchDegrees`, `touchdownBankDegrees`, `touchdownLateralFps`) e ciclo de pouso (`landingCycles`), totalizando exatamente as 29 métricas do vocabulário fechado `WearRuleMetrics.All`.
- `src/FlightCareer.Infrastructure/Data/wear_rules.json`:
  - Regra `turbine_start_cycle` atualizada para `"metric": "turbineStartCycle"` (anteriormente continha `"engineStartCycle"`, o que impedia o casamento com o evento emitido para turbinas pelo derivador).
- `src/FlightCareer.Infrastructure/Services/RuleCoverageService.cs`:
  - Novo serviço para avaliar a aplicabilidade das 47 regras de desgaste para cada um dos 46 perfis de aeronave do catálogo, identificando regras ativas, inativas por ausência de limites/parâmetros e regras não aplicáveis por feature.
- `src/FlightCareer.Replay/Program.cs`:
  - Adicionado subcomando `coverage [--out <caminho>]` para geração sob demanda do relatório de cobertura por avião.
- `docs/plans/cobertura-regras-por-aviao.md`:
  - Relatório consolidado cobrindo todos os 46 modelos do catálogo com resumo por classe no topo e detalhamento minucioso por modelo.
- `src/FlightCareer.Tests/Parts/WearRuleMetricCoverageTests.cs`:
  - Nova suíte xUnit contendo testes estáticos de cobertura de métricas e regras contínuas, 8 testes funcionais das novas métricas de sistemas e teste de geração de relatório.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Atualizados com a documentação do sistema implementado.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx`.
- [x] `dotnet test` 100 % aprovados — 769 testes aprovados (0 falhas).
- [x] Nenhuma métrica usada por regra sem produtor (teste `EveryEventRule_HasProducedMetric`).
- [x] Nenhuma constante órfã em `WearRuleMetrics` (teste `AllWearRuleMetricsConstants_AreProducedAndNoneOrphan`).
- [x] Todas as regras contínuas com sensores e limites válidos (teste `EveryContinuousRule_HasKnownSensorsAndValidLimitKey`).
- [x] Um teste por métrica cobrindo cruzamentos corretos, sensores parados em 0 e proteções de features ausentes.
- [x] `docs/plans/cobertura-regras-por-aviao.md` gerado com os 46 modelos de aeronaves.
- [x] Nenhuma mudança em arquivo quente.
- [x] Fuzz all 100 % — 46 modelos testados.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 19 avisos pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 769 aprovados, 0 falhas (12 testes novos em `WearRuleMetricCoverageTests`).
- `FlightCareer.Replay fuzz --aircraft all --runs 50 --seed 1`: 100% de aprovação (2.300 voos sintéticos nos 46 modelos, 0 falhas).

## Não testado
- Atuação e comportamento dos sensores em voo interativo dentro do Microsoft Flight Simulator 2024 / 2020 (a lógica foi validada via testes unitários determinísticos, dados reais de replay e fuzz testing procedural).

## Dúvidas e decisões para o revisor
- **Sincronização de `turbine_start_cycle`**: a regra no catálogo continha `"metric": "engineStartCycle"`, enquanto `PartEventDeriver` já emitia `turbineStartCycle` para turbinas e jatos desde a F2.4. A regra foi atualizada para `"metric": "turbineStartCycle"`, eliminando a discrepância e assegurando que `engineStartCycle` permaneça para pistões e `turbineStartCycle` para turbinas, ambas com produtor e sem órfãs.
