# Revisão AGENTS-ENXUTO — § 7 do AGENTS.md enxuta

**Veredito: APROVADO com ajuste do revisor** (Claude, 2026-11-24). Branch `docs/agents-enxuto`, commits `dc9ed14` + `d665469`.

- § 7: 62.053 → 20.669 bytes (≤ 22 KB). §§ 1–6 e 8 byte a byte iguais (conferido com `cmp`).
- Conferido por amostragem que continuam citados: rastreadores da E2C-1, `linha_auto:hub`, `PersistentFailureService`, `CareerClock`, `XpCalculator`, caminhão de combustível, abastecimento manual (decisão 2026-11-21), `calibrate.py`/`merge_limits.py`, `airport_profiles.py`, "não testado no MSFS".
- **Ajuste:** a contradição 1 do relatório estava errada — `hubs_data.json` não foi absorvido: continua em `Infrastructure/Data/` e é lido pelo `HubDataService`. Menção restaurada no bloco de missões.
- Valores de calibração (ex.: `destination.maxMultiplier` 150 da saúde) saíram de propósito: estão no `mission_catalog.json`.
