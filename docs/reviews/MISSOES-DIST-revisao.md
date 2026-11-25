# MISSOES-DIST — Revisão (Claude, 2026-11-25)

**Veredito: APROVADO** (commit `4d29340`)

- Escopo conforme a spec; catálogo, tabela de pagamento, rastreadores e UI intocados.
- `check_flight` e paraquedismo usam a distância já sorteada do perfil, sem sorteio novo.
- `pinga_pinga`: teto acumulado com reserva de 25 NM por etapa que falta até 3; total abaixo do mínimo retorna nulo.
- Build 0 erros; `dotnet test` 1.087/1.087 (rodados pelo revisor).
- Snapshot: só `DistanceNm` e `Reward` mudaram, em 11 registros (6 cheque, 5 paraquedismo; contagem por subtipo do relatório). Recompensa do paraquedismo cai 2–11 %; a do cheque acompanha a distância (30 → 24–50 NM).
- `agy-audit.sh`: 2 comandos marcados, ambos cópia/remoção temporária do snapshot e script de comparação no scratch do Gemini; worktree limpo.

## Não verificado
- MSFS.
- Frequência com que o `pinga_pinga` deixa de ser gerado em dados reais pelo mínimo de 100 NM (`board-report` não rodado).
