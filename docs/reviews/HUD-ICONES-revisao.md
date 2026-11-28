# HUD-ICONES — Revisão (Claude)

**Branch:** `feat/hud-icones` (`e62ca77`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. O HUD não lê mais as colunas antigas (`EngineCondition`/`AirframeCondition`/`TiresCondition` = saúde real): as peças são carregadas com `AsNoTracking` e resumidas por `HudHealthSummary` com `PartHangarView.DisplayState` (saúde **conhecida**); aviões sem peças usam a faixa exibida do componente (`HealthVisibility`). Nenhuma % nem `ProgressBar` de saúde no XAML.
2. Mapa: `OriginLat/OriginLon` com o ponto de partida ao vivo (aeroporto de origem da missão ou posição inicial do voo).
3. Build 0 erros, 834/834 (revisor); `agy-audit` 128 ações, 0 para conferir.

## Não testado
- Na tela e no MSFS.
