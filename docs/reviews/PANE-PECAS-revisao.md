# PANE-PECAS — revisão (Claude, 2026-11-26)

**Veredito: APROVADO** (1ª rodada).

- Escopo: só os arquivos da spec; `FlightMonitor.cs` (quente) com o bloco único em `StartMonitoring`, contexto curto.
- `ApplyNativeFailed`: quebra uma candidata (saúde, ordem da tabela, motor); fallback ignora quebradas.
- `DefectPartLink`/`PartAirworthiness`: conforme o contrato; panes do addon seguem com `RepairCost`.
- Hangar: reparar pane troca as peças ligadas (preço = peças + panes sem peça); trocar peça resolve a pane coberta; "Reparar tudo" inclui todas as panes. `PartReplacementPlanner.Plan` é aditivo, então o valor do botão (`TotalRepairCost`) bate com o cobrado (antes da dificuldade, tarefa 7).
- Testes: 10 novos com valores esperados; build 0 erros, 1.222/1.222. `agy-audit`: 0 ações a conferir.
- Nits (não bloqueiam): `engineCount` no alerta do `FlightMonitor` vem só das peças quebradas (nome pode sair sem "motor N" em bimotor); "Reparar tudo" passou a diálogo `Warning`.
- Não testado no MSFS: pane nativa real e bloqueio do início de voo; textos/preços do Hangar sem o app aberto.
