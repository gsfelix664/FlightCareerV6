# CALIBRA-F4b (+ CATALOGO-MOTOR e CALIBRA-F4) — Revisão (Claude, 2026-11-19)

**Veredito: APROVADO**

- Catálogo conferido por script contra `docs/agents/specs/calibracao-pecas-usuario-2026-11-19.json`: **0 divergências** em 153 peças (vida h/ciclos/meses, preço, ×, mão de obra, parada, NO-GO, WA); `engine.air_intake_filter` só pistão.
- Preço = base × multiplicador da classe (`AircraftEconomyClass`, A 0,75 … J 5,5); `economy_targets.json` só ganhou `partPriceMultiplier`, faixas intactas; `PartEconomyBalanceTests` verdes com as faixas originais.
- TBO 500/900 h; inspeções 12/25/125 h com textos "Troca de óleo / Inspeção anual / Revisão de ignição"; migração das horas salvas; DA62 Vle 156 e King Air 350 Vne 263 (POH do usuário).
- Base integrada pelo revisor (calendário de carreira): conflitos em `AppDbContext`, `HangarViewModel`, CHANGELOG e LICOES resolvidos; paradas das inspeções do `CareerClock` renomeadas para os novos intervalos.
- Build 0 erros; `dotnet test` 630/630; `fuzz --aircraft all --runs 3` sem falhas.

## Observações
1. Multiplicadores duplicados: fixos em `AircraftEconomyClass.GetPartPriceMultiplier` **e** em `economy_targets.json`. Regra do projeto: calibração em dados — o código deve ler do JSON (próxima tarefa que mexer em preço).
2. `repairCostPctOfAircraft` e `GetClassPartPriceCap` ficaram legados (`[Obsolete]`).
3. Não testado na tela nem no MSFS.
