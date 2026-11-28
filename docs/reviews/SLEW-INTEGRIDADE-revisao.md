# Revisão SLEW-INTEGRIDADE — rodada 1

**Veredito: APROVADO** (com ressalva a confirmar no MSFS)

Conferido pelo revisor: build 0 erros; `dotnet test` 500/500. `SlewMonitor` puro e testado (solo dentro/acima de 1.000 m, no ar, depois da decolagem, inativo não soma, congelamentos); leitura `IS SLEW ACTIVE` + 3 congelamentos em definição separada (sem escrita); `InvalidateFlight(motivo)` único, reaproveitado pelo combustível; blackbox a cada uso no solo.

## Ressalva (testar no MSFS antes de considerar fechado)
- O slew no solo depende de `SIM ON GROUND` continuar 1 enquanto o slew está ativo. Se o MSFS reportar 0 durante o slew (avião "solto" do chão), um reposicionamento legítimo na vaga **invalidaria o voo**. Roteiro: iniciar o monitoramento, ligar o slew no pátio, mover ~100 m e conferir o blackbox ("slew no solo: X m") e que o voo não foi invalidado. Se falhar: antes da decolagem, julgar o slew só pela distância (e por altura acima do solo, que hoje não está no `SimTelemetry`).
