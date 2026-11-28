# Revisão MAPA-ESCALAS — APROVADO (com ajuste do revisor)

**Data:** 2026-11-27 · **Revisor:** Claude (Sonnet) · **Branch:** `fix/mapa-escalas` · Item B2 de `docs/plans/correcoes-voo-forcado-sdco.md`.

## Conferido
- `ContractMarkerBuilder`: escalas numeradas, pista STOL, linha origem → reporte → escalas → destino, sem coordenada inventada; marcadores existentes e SAR sem mudança.
- `FlightMapControl`: marcador `Stop`, linha do contrato abaixo das pernas, retorno antecipado considera `Routes`.
- Build 0 erros; 1.283/1.283; golden intacto; `agy-audit` sem ações a conferir.

## Corrigido pelo revisor
- A âncora de origem pela posição do avião ficava travada no primeiro valor: como a latitude chega antes da longitude (e o ICAO antes da posição, ao trocar de aeroporto), a linha podia partir de um ponto errado. Agora a âncora em solo acompanha saltos > 0,05° (táxi não redesenha) e em voo fica a última em solo.
- `RefreshContractAnchors` não faz nada sem contrato (evita reenquadrar o HUD sem missão).

## Observações (não bloqueiam)
- `OverflyWaypoints` é lido duas vezes por missão.

## Não testado
App aberto e MSFS: desenho e enquadramento no briefing, no quadro e no HUD. A lógica de âncora (UI) não tem teste automatizado.
