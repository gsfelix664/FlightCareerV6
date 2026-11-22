# Spec METRICAS-B — Métricas de sistemas e garantia de que nenhuma regra fica sem cálculo

**Objetivo (1 frase):** calcular as 8 métricas de sistemas que as regras usam sem produtor, criar um teste que falha se qualquer regra usar métrica sem cálculo, e gerar o relatório de cobertura das regras para os 46 aviões do catálogo.

## Fatos já levantados (não redescobrir)
- Base: branch da METRICAS-A (já mergeada na base quando esta tarefa rodar). Leia `docs/agents/specs/spec-METRICAS-A.md` e o código dela: o padrão (emissão em cruzamento de threshold, `ReportDiagnosticOnce`, tempo simulado, reset) deve ser o mesmo.
- Métricas sem produtor desta tarefa e suas regras em `Infrastructure/Data/wear_rules.json`:
  | Métrica | Regra | Sensores da regra | `appliesTo` | Faixas |
  |---|---|---|---|---|
  | `icingExposureMinutes` | `structural_icing_exposure` | `inst-structural-ice-pct`, `inst-structural-deice-switch`, `inst-pitot-ice-pct` | — | 5 / 15 / 30 |
  | `deiceCycles` | `structural_deice_boot_cycles` | `inst-structural-deice-switch`, `inst-structural-ice-pct` | `deIceBoots` | 2 / 6 / 12 |
  | `propDeiceCycles` | `propeller_deice_boot_cycles` | `inst-prop-deice-switch` | — | 2 / 6 / 12 |
  | `glowPlugCycles` | `diesel_glow_plug_cycle` | `elec-master-battery`, `eng-starter-active` | `diesel` | 1 / 3 / 5 |
  | `dryRunPumpMinutes` | `fuel_pump_dry_run_empty_tank` | `fuel-pump-on`, `fuel-pump-switch`, `fuel-total-quantity` | — | 2 / 5 / 10 |
  | `lowHydraulicPressureMinutes` | `hydraulic_low_pressure_stress` | `hyd-pressure`, `hyd-pump-pressure`, `hyd-reservoir-percent` | — | 3 / 8 / 15 |
  | `pressurizationCycles` | `pressurization_cycle_stress` | `af-pressurization-pressure-differential`, `af-pressurization-cabin-altitude` | `pressurized` | 1 / 2 / 4 |
  | `rapidDecompressionDeltaPsf` | `cabin_rapid_decompression_stress` | idem | `pressurized` | 150 / 300 / 500 |
- No C152 (sem esses sistemas) esses sensores ficam constantes em 0: nenhuma regra pode disparar com sensor parado em 0.

## Contrato por métrica (no `PartEventDeriver`, estado reiniciado em `Reset()`)
1. `icingExposureMinutes`: no ar, gelo estrutural ≥ 5 % **ou** gelo no pitot ≥ 10 % → minutos acumulados; emitir em 5, 15, 30; reinício após 10 min sem gelo.
2. `deiceCycles` / `propDeiceCycles`: contar transições desligado → ligado da chave correspondente no voo; emitir em 2, 6, 12.
3. `glowPlugCycles` (diesel): contar as partidas do motor (mesmo gatilho do `engineStartCycle`) no voo; emitir em 1, 3, 5.
4. `dryRunPumpMinutes`: bomba ligada (`fuel-pump-on` ou chave) **e** (combustível total < 0,5 gal **ou** motor sem combustão no solo) → minutos; emitir em 2, 5, 10; reinício após 1 min fora.
5. `lowHydraulicPressureMinutes`: só arma depois de ver pressão ≥ 500 psi no voo (aviões sem hidráulico nunca armam); depois, com motor em combustão e pressão < 50 % da maior pressão vista → minutos; emitir em 3, 8, 15.
6. `pressurizationCycles`: um ciclo = diferencial sobe acima de 2 psi e depois volta abaixo de 0,5 psi; emitir em 1, 2, 4 (1 ciclo por voo normal = desgaste normal pequeno, previsto pela regra).
7. `rapidDecompressionDeltaPsf`: queda do diferencial em psf/s (psi × 144) numa janela de 1 s simulado; emitir o pico quando ≥ 150 (uma vez por evento; rearma quando a queda < 20 psf/s por 5 s).

## Garantia de cobertura (o mais importante)
- `WearRuleMetricCoverageTests`: carrega o `wear_rules.json` real e falha se **qualquer** `metric` usada por uma regra não estiver na lista de métricas produzidas. A lista vem do código, não de cópia manual: exponha `PartEventDeriver.ProducedMetrics` (e o equivalente do `PartFlightTracker` / toque / ciclos de pouso, ex. `PartFlightTracker.ProducedMetrics`) como conjuntos estáticos e use-os no teste. Regras `Continuous` com `limitKey`/sensor também entram: o sensor ou a métrica derivada que elas leem precisa existir no registro ou ser produzida.
- Remover de `WearRuleMetrics` constantes que não são usadas nem por regra nem por código (ou justificar no relatório).
- **Relatório de cobertura por avião** em `docs/plans/cobertura-regras-por-aviao.md`, gerado por um teste ou comando do `FlightCareer.Replay` (ex.: `coverage`): para cada um dos 46 perfis de `Infrastructure/Data/aircraft/*.json` (features + limites), a lista de regras que **se aplicam**, as que **não se aplicam** (por feature) e as que **se aplicariam mas ficam inativas** (limite ou parâmetro faltando, ex. `elecAvionicsCircuitIndex`), com o motivo. Resumo no topo: total por classe (pistão, turboélice, jato). Se gerado por teste, o teste só escreve o arquivo quando uma variável de ambiente pedir; o conteúdo do repositório é a saída commitada.

## Fora do escopo
- Arquivos quentes; calibração das faixas existentes; métricas da METRICAS-A.

## Testes
- Um teste por métrica (cruzamentos certos; sensor parado em 0 não dispara; feature ausente não dispara).
- `WearRuleMetricCoverageTests` (acima).
- Fuzz: `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50 --seed 1` continua 100 % (sem relaxar invariantes).

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz all 100 %.
- [ ] Nenhuma métrica usada por regra sem produtor (teste), nenhuma constante órfã.
- [ ] `docs/plans/cobertura-regras-por-aviao.md` com os 46 aviões.
- [ ] Nenhuma mudança em arquivo quente. CHANGELOG e AGENTS.md › 7. Relatório diz "não testado no MSFS".
- [ ] Um commit: `feat(desgaste): metricas de sistemas e cobertura das regras por aviao (METRICAS-B)`.
