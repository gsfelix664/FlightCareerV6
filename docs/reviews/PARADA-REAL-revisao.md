# PARADA-REAL — Revisão (Claude)

**Branch:** `feat/parada-real` (`5fcee86`, `270bf93`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. `WorkshopDowntime` (Domain, relógio injetado): minutos reais `clamp(round(2√h), 1, 20)`, expresso (+50 %, metade das horas e dos minutos), parada sobreposta usa a maior. Testes cobrindo piso, teto, expresso e sobreposição.
2. Arquivos quentes linha a linha: `AppDbContext` só as duas colunas (tabela `PilotAircrafts`, plural, conferida no banco real — os `ALTER` antigos no singular são legados e não fazem nada); `MainViewModel.StartFlight` relê o avião com `AsNoTracking` e bloqueia com mensagem; `RefreshBriefingAircraftAsync` repassa a oficina ao briefing; `BriefingViewModel` bloqueia o item da aeronave e o despacho.
3. Hangar: escolha Normal/Expresso com tempos e preços calculados; `DispatcherTimer` só roda com avião na oficina; prazos reais vindos de `MaintenancePricing × CompressionFactor` (nenhum número fixo no XAML); textos "faltam X h de voo".
4. Build 0 erros, 771/771 (rodado pelo revisor); `agy-audit` 132 ações, 0 para conferir.

## Não testado
- Na tela e no MSFS: diálogo Normal/Expresso, contagem regressiva, bloqueio no briefing e liberação com o app fechado no meio.
