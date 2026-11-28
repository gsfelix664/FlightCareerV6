# Revisão AREAS-INSTRUCAO (2026-11-25, Claude)

**Veredito: APROVADO** (1ª rodada). Branch `feat/areas-instrucao`, commit `c6911f2`.

- Build 0 erros; `dotnet test` 1.095/1.095 (rodado pelo revisor no worktree).
- `training_areas.json`: 27 áreas (11 instrução, 16 acrobacia), nomes com acento corretos (sem U+FFFD), lista bate com a extração do revisor (+ SBR810 Acro Echo).
- Ramo procedural intacto (mesma sequência de `random`); o ramo real só entra com `placesService` e área válida. Snapshot: 48 de 280 registros mudaram, só em SBKP, SBMT e SDCO (origens a ≤ 25 NM de área real); a mudança em outros subtipos dessas origens vem do `Random` compartilhado no lote e é esperada.
- Observações (sem correção): o bloco do ramo real está duplicado entre instrução e cheque (aceitável, pequeno); quando a origem fica dentro da área (ex. SDCO × SBR806, ~1 NM), o texto fala em "setor X a 1 NM" — correto, a área é sobre o aeródromo. Áreas com teto baixo (Itaipu, Solimões, João Monteiro, Antônio Carlos: teto 1.500–2.500 MSL) caem no procedural pela faixa mínima de 1.000 ft.
- Ajuste do revisor: CHANGELOG dizia "folga de 500 ft" no `TryResolveRealAreaBand` (a folga é do rastreador; a função arredonda a múltiplos de 500 ft).
- Não testado no MSFS.
