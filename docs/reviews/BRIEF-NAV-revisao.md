# Revisão BRIEF-NAV — APROVADO

**Data:** 2026-11-27 · **Revisor:** Claude (Opus) · **Branch:** `fix/brief-nav` (`257ad39`) · Itens A3, B1, B3 de `docs/plans/correcoes-voo-forcado-sdco.md`.

## Conferido
- `BriefingLegPlanner` (Domain): TGL só com `RequiredTouchAndGoes > 0`; missão com escalas não entra no grupo local e se expande em `ContractStop` (com `StopMode` e coordenada do `StopPoint`) + `Navigation` final; hops repetidos descartados (cobre `round_trip`, cuja última escala é o destino). Encadeamento antigo preservado (agora sem diferenciar maiúsculas, inofensivo). JSON inválido ou de outro contrato → sem escalas, com `Debug.WriteLine`.
- `BriefingViewModel` (quente, linha a linha): diff só em `RebuildItineraryAndValidateAsync`; texto do circuito só no TGL; "(LOCAL)" no voo local; escala sem pagamento, pagamento na última perna da missão; coordenada do destino cai no `StopPoint` quando o aeroporto não está no banco.
- `FlightMapControl`: circuito âmbar só com `IsTglOrLocal`; voo local só com marcador "BASE"; enquadramento único (`ApplyCombinedFraming`) somando rota e contrato, com o `catch {}` antigo trocado por log. Os retornos antecipados também reenquadram (depois de `UpdateDestinationRoute`) incluindo o contrato: aceitável.
- 10 testes novos com valores esperados; build 0 erros; 1.277/1.277 (rodado pelo `review-pack.sh`). Golden intacto. `agy-audit`: nada fora do worktree.

## Observações (não bloqueiam)
- `ReadStops`/`ReadStopMode` desserializam o mesmo JSON mais de uma vez por missão: custo desprezível no briefing.
- Perna que sai de uma escala ausente do banco usa distância de fallback (DistanceNM da missão ou 40 NM).

## Não testado
App aberto e MSFS: texto das pernas, sumário, circuito e enquadramento do mapa no briefing; HUD (mesmo construtor de contrato) segue a conferir no MSFS.
