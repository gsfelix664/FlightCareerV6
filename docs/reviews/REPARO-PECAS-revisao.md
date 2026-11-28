# REPARO-PECAS — Revisão (Claude, 2026-11-19)

**Veredito: APROVADO**

- `PartReplacementPlanner`: troca a preço cheio (base × multiplicador da classe) + mão de obra as peças quebradas, < 80 %, com marca da inspeção ou vida/ciclos/meses ≥ 90 %; parada = maior parada; aplicar zera horas/ciclos, saúde 100 %, data de carreira.
- Hangar: aeronave com peças usa o plano (linha "Trocar N peças", "Em dia" sem peça gasta, "Reparar tudo" soma), recalcula a barra pelo agregado (`SyncComponentsFromParts`, sem reconciliação gratuita no voo seguinte) e avança o relógio; troca de óleo/anual trocam óleo e filtro; revisão de ignição troca as peças de ignição; retífica troca Motor/Seção quente/Óleo. Aeronave sem peças: comportamento antigo.
- Build 0 erros; `dotnet test` 639/639; fuzz `--runs 5` 230/230 (executor).

## Observações
- O plano usa a saúde real da peça (o mecânico descobre ao abrir): o botão de reparo pode revelar uma peça gasta que a faixa do Hangar ainda não mostrava. Comportamento aceitável (a oficina inspeciona antes de trocar).
- Não testado na tela.
