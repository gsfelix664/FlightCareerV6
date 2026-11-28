# Revisão NAV-IDA (Claude, 2026-11-27)

**Veredito: APROVADO** (rodada 1). Branch `feat/nav-ida`, commit `73cf630`, merge `--no-ff` na base.

## Conferido
- Escopo: só os arquivos da spec; nenhum arquivo quente, UI, banco ou economia.
- `PickProfile` filtra por `MinPilotLevel` só com nível informado; nulo não filtra.
- `VfrOneWayDestinations`: elegibilidade (raio, mesmo país, tier 2–3, pista ≥ 1.800 ft), escada do iniciante (a → b → c), ordenação determinística (distância, ICAO ordinal), peso intermediário 1 + 2·n.
- `CreateVfrOneWayMissionSync`: destino ≠ origem, ponto de reporte no meio da perna, sem `contractData`; despacho em `MissionBoardCore` com `WithPersonLuggage`.
- `triangle_long`: lê `legNm`/`rangeNm` do perfil; `triangle`, `out_and_back` e `triangle_tgl` com os mesmos números de antes.
- Jogo lê `ExperiencePoints` do primeiro piloto; simulador de carreira inclui o nível na chave do cache; `board-report --level`.
- Testes de comportamento (13 novos) com valores esperados e casos de borda. Build 0 erros; 1.314/1.314.
- Golden regenerado: só mudam sorteios pelo tipo e perfil novos (esperado na spec).
- `board-report` SNZG: nível 1 → só ida restrita a 3 destinos (57–62 NM), triangular até 246 NM; nível 3 → ida com 37 destinos até 145 NM, triangular até 299 NM.

## Observações (não bloqueiam)
- Texto `navegacao_vfr_ida:iniciante` diz "com {pax} instrutor a bordo" ("com 1 instrutor"): aceitável.
- Não testado no app nem no MSFS (quadro, briefing, mapa da missão só de ida).
