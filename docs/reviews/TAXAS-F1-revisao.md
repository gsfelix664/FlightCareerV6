# TAXAS-F1 — Revisão (Claude)

**Branch:** `feat/taxas-aeroporto-f1` · **Commits:** `ca97f4d`, `f28dfa0` · **Data:** 2026-11-16

## Veredito: APROVADO

## Conferido
- `airport_fees.json`: valores da spec, `$` genérico, nota "estimativa — calibrar", `countryOverrides`/`icaoOverrides` vazios. Copiado para a saída da UI pelo glob `Data\*.json` (conferido em `bin/.../Data/airport_fees.json`).
- `GroundServiceCalculator`: LANDING igual à fórmula antiga (teste de regressão); PAX_FEE só com missão e passageiros; HANDLING médio/pesado em tier 1–2 com missão; NAV_TERMINAL médio/pesado tier 1–3; PARKING calculado mas não cobrado (F2); tier 4 sem taxas.
- `FlightService`: cada taxa vira uma `FinancialTransaction` por categoria; `outcome.LandingFee` = soma (comentado); passageiros só das missões concluídas; mensagem "Taxas: $…" quando há mais que o pouso; `CalculateLandingFee` delega ao catálogo.
- `dotnet build` 0 erros; `dotnet test` **577/577** (25 novos).

## Observações
- Falta do `airport_fees.json` lança exceção dentro do fim do voo (cai no `catch` geral do método). Arquivo é copiado pelo build; aceitável.
- Valores são estimativa do revisor; calibrar com voos reais.
