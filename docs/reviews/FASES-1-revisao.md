# FASES-1 — Revisão (Claude)

**Branch:** `feat/fases-1` (`0ef8202`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. `CareerPhase` (1–7) e `CareerPhaseCatalog` + `career_phases.json` (nomes, níveis de entrada 6/13/21/29/37/45, horas 40/150/300/500/1.000/1.500, homologação $35 mil … $2 mi, mapa subtipo → fase). `PilotCategory` e `CompanyTier` ficam como alias obsoleto.
2. Migração (`AppDbContext`, linha a linha): colunas `Pilots.CareerPhase` e `Missions.RequiredPhase` + `UPDATE` 1→1, 2→2, 3→3, 4→5, 5→6, idempotente (só converte quem está na fase 1). A carreira atual do usuário (categoria 1) vira fase 1.
3. `MainViewModel` (linha a linha): aceite e início de voo checam a fase; promoção exige nível, horas e homologação, com o resumo do que falta; fase 7 avisa que os níveis seguem sem novas fases. `FlightMonitor`: só a checagem da fase.
4. Simulador: bots promovem pelas regras novas. Build 0 erros, 827/827 (executor); revisor roda de novo após o merge na base atual.

## Não testado
- Na tela (badge da fase, quadro de missões com o bloqueio pela fase, diálogo de promoção) e no MSFS.
