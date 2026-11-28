# POUSO-DANO — Revisão (Claude, 2026-11-26)

**Veredito: APROVADO** (1 ajuste feito pelo revisor).

Escopo: C1, D1, D5, D6, D10, D11 e B4 do plano `docs/plans/correcoes-voo-forcado-sdco.md`. Implementação do Gemini (`bb4d09b`), ajuste do revisor `a14c120`.

## Conferido
- `LandingBreakModel` segue a curva e os multiplicadores da spec; no máximo 1 trem e 1 pneu por toque; pula peça quebrada; aleatoriedade via `IRandomSource`.
- `PartFlightSession`: passa a receber `ComponentCatalog` e `_random` do `FlightMonitor` (antes usava o padrão). `ctx-on-any-runway` ausente = em pista.
- Quebra só marca a peça (`BreakPart`); sem pane extra, sem cobrança em dobro. Interdição por `PartAirworthiness` (peças de trem/pneu têm `blocksFlightWhenBroken`).
- D1: dano de terreno via `Damage()` depois do `StopMonitoring`; os componentes e a sessão de peças continuam carregados nesse ponto (conferido `ComponentFlightSession.Reset` e o comentário do `StopMonitoring`). `FlightService` pula o -35/-20 antigo com `terrainDamageApplied`.
- B4/D10: missões removidas, XP × 0,5, relógio avança. D6: `Grounding ?? IsGrounding`. D5: `Record(..., isGrounding)`; só 5 códigos de toque interditam.
- D11: `StopFlightLog` depois de saber se foi fora de pista; `RecordPeakG` com lock.
- Build 0 erros; 1.245 testes aprovados. Auditoria do agy: 0 ações para conferir.

## Ajuste do revisor
1. O `return` antecipado (`_pilot`/`_aircraft` nulos) passou a acontecer antes do `StopFlightLog` movido: blackbox ficava aberto. Fecha agora nesse caminho.

## Ressalvas (não bloqueiam)
- `FlightServiceOffFieldLandingTests` confere `XP > 0`, não a metade exata.
- Não testado no MSFS: `ON ANY RUNWAY` real no toque, alerta de peça quebrada, inspeções no Hangar, blackbox "POUSO_FORCADO".
- Inspeções agora viram pane (antes só log): voos com excedências passam a deixar itens "Inspeção pendente" no Hangar (custo base 400). As de falso positivo (D7, D9) ficam diferidas até a tarefa 9.
