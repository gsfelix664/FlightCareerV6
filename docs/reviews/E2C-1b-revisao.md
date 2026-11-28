# Revisão E2C-1b — variantes da navegação VFR

**Veredito: APROVADO** (Claude, 2026-11-24). Branch `feat/e2c-1b-vfr-variantes`, commit `73ca841`.

## Conferido
- Catálogo: `navegacao_vfr` com 3 perfis `variant` (`triangle` 50, `out_and_back` 25, `triangle_tgl` 25); `MissionProfileDefinition.Variant` e `MultiStopContractData.StopMode` (padrão `full_stop`, `pinga_pinga` inalterado).
- `TouchAndGoCounter`: só extração das constantes e de `IsValidTouchAndGo` (mesma regra: rolagem ≥ 1 s, GS máx > 20 kt, sem parada < 5 kt); circuito de TGL inalterado.
- `MultiStopTracker` modo `touch_and_go`: escala conta com TGL válido a ≤ 1,5 NM, ordem preservada (escala posterior tocada antes = violação), parada completa também conta; modo padrão com os testes antigos passando sem mudança.
- `MissionBoardCore`: só passa o `profile` ao gerador. Testes do gerador conferem as 3 variantes, faixas de etapa/total, volta à origem e `P:`.
- Snapshot: 60/280 registros mudaram (aeroportos com peso de instrução, efeito do sorteio de perfil).
- Build (revisor): 0 erros. `dotnet test`: 1.057/1.057.
- `agy-audit`: 87 ações, 1 para conferir (apagar e regravar o snapshot, previsto). Nada fora do worktree.

Não testado no MSFS (toques nas escalas, parada em ida e volta).
