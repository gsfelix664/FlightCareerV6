# XP-PONTAS — Revisão (Claude)

**Branch:** `feat/xp-pontas` (`71425a4`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. Telemetria (arquivos quentes): `STALL WARNING`, `LIGHT LANDING`, `LIGHT TAXI` no fim do `SimTelemetry` e do `AddToDef`, mesma ordem e unidade; nomes conferidos no SDK (`simvars-msfs2024.csv`).
2. `FlightInfractionTracker` (lógica pura): estol por borda e não conta em instrução; luzes de pouso e táxi desligadas na decolagem e no toque; 250 kt abaixo de 10.000 ft por borda; Vne por borda. `FlightMonitor` só repassa os eventos.
3. `RunwaySurface.IsUnpaved` substitui as duas listas copiadas; pagamento: bônus de +25 % por pista difícil (não pavimentada ou < 2.500 ft) com cache por aeroporto; sem dado de pista, regra antiga (tier 4).
4. ≈ XP no quadro (`XpCalculator.Estimate`); peças do usado com horas/ciclos coerentes com a saúde e o TSMO.
5. Build 0 erros, 850/850 (executor); revisor roda de novo depois do merge com a FASES-2A.

## Observação
- Num circuito de TGL com o farol de pouso desligado, cada toque conta uma infração (−0,05 cada).

## Não testado
- No MSFS e na tela.
