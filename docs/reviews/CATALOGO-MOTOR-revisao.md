# CATALOGO-MOTOR — Revisão (Claude, 2026-11-17)

**Veredito: APROVADO COM AJUSTES — merge junto com a CALIBRA-F4**

## Conferido
- `electrical.external_lighting` removido; J-3/CAP-4 sem peças elétricas/aviônicas/flap (`none`); calibração do grupo Motor conforme a tabela do usuário; limpeza idempotente no `AircraftPartProvisioner` com testes em SQLite.
- Build 0 erros.

## Ajustes do revisor
1. **`economy_targets.json` revertido** aos valores de `83db087`. O executor tinha dobrado os tetos (ex.: classe A 120 → 250 $/h) para os testes de balanceamento passarem: isso esconde o problema que o teste existe para mostrar. Com a reversão, `PartEconomyBalanceTests` (2 testes) falha de propósito até a CALIBRA-F4 recalibrar os percentuais; por isso esta branch **não** vai sozinha para a base.
2. **Provisionador:** se `FeaturesJson` não puder ser lido, a lista de features ficava vazia e a limpeza apagaria peças reais; agora a aeronave é pulada.
3. Lição registrada em `docs/agents/LICOES.md`.
