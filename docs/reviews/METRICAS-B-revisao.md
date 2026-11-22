# METRICAS-B — Revisão (Claude)

**Branch:** `feat/metricas-b` (`9463924`, `5cfd072`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. As 8 métricas de sistemas têm produtor no `PartEventDeriver` (gelo, degelo, degelo de hélice, velas de aquecimento, bomba a seco, hidráulico baixo, ciclos de pressurização, descompressão rápida), com o mesmo padrão da METRICAS-A.
2. Bug achado e corrigido pelo executor: a regra `turbine_start_cycle` usava um nome de métrica diferente do evento emitido — partidas de turbina nunca geravam desgaste.
3. `WearRuleMetricCoverageTests` lê o `wear_rules.json` real e falha se alguma regra usar métrica fora de `PartFlightTracker.ProducedMetrics`; nenhuma constante órfã.
4. `docs/plans/cobertura-regras-por-aviao.md` (comando `FlightCareer.Replay coverage`): média de 4,5 regras/avião aplicáveis mas **inativas por falta de dado** — elétricas (índices de barramento/circuito fora do catálogo do jogo, decisão antiga: só no harness de testes) em ~44 aviões; Va ausente em 33, Vno em 19, Vlo em 14, Vle em 10. Vira a tarefa LIMITES-FALTANTES.
5. Build 0 erros, 769/769 (revisor); `agy-audit` 42 ações, 0 para conferir.

## Não testado
- No MSFS (sistemas de gelo, hidráulico e pressurização só em aviões que os têm).
