# ECON-SIM — Revisão (Claude, 2026-11-17)

**Veredito: APROVADO (ferramenta de medição; não muda o balanceamento)**

- A execução do Gemini estourou o limite de 1h30 na simulação completa, antes do commit; o revisor commitou o trabalho (`581d56f`), acrescentou CHANGELOG, comando no `AGENTS.md` › 3 e moeda `$` (cultura en-US no comando `career`).
- Build 0 erros; `dotnet test` 612/612 (inclui `CareerSimulatorTests`).
- Execução conferida: C152, 1 bot, 3 dias, `--fast` → 8 voos em 23 s (inclui montar o banco de aeroportos); CSVs e `summary.md` coerentes (receita ~$1.988/h, combustível ~$42/h, taxas ~$76/h).
- Mudanças em código do jogo revisadas no diff: `MissionGenerator` (aleatoriedade/data injetáveis + `OrderBy` estável nas consultas — muda só a ordem das ofertas), `FuelPriceService` (sobrecarga com data), `PilotAircraftWrapper` usa `MaintenancePricing` com os mesmos valores.

## Pendências
1. **Desempenho:** 8 aviões × 5 bots × 90 dias não terminou em ~1 h (nem `--fast` com 3 bots × 60 dias na sessão). Rodar por avião/lotes menores ou otimizar (cache de missões por aeroporto) antes da análise completa.
2. A análise de balanceamento (rebalancear `CalculateReward`) fica para depois da CALIBRA-F4.
3. `CareerSimulator.cs` (1.062 linhas) foi revisado por amostragem, não linha a linha.
