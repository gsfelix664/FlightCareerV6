# LUZES-F2 — Revisão (Claude, 2026-11-17)

**Veredito: APROVADO** (com 2 ajustes feitos pelo revisor)

Branch `feat/luzes-pecas` (`517953f`, `01f0e0d` Gemini; ajustes do revisor no commit seguinte).

## Conferido
- Build 0 erros; `dotnet test` 594/594 (17 novos em `LightingPartsTests`); `fuzz --aircraft all --runs 3` 138/138 depois dos ajustes (Gemini: `--runs 20` 920/920).
- `PartWearEngine`: `usageSensor` filtra desgaste natural (70/lifeHours por hora acesa, tempo simulado), ciclo 0→1 só com leitura anterior (primeira leitura não conta; estado zerado em `ResetExceedanceTracking`), sorteio `p = 1 − e^(−λ·dt)` pela curva do `FailureEngine.HazardPerHour` (reaproveitada, sem duplicar constante), queima = `IsBroken`, saúde 0, um diagnóstico; peça quebrada não desgasta.
- `minorPart`: fora do mínimo da agregação (só média) e fora do `DamageGroup`; `ClampGroup` mantido.
- Catálogo: 6 peças com `none: [noElectrical]` (J-3/CAP-4 sem lâmpadas), não bloqueiam voo, visíveis no walkaround; 6 sensores `LIGHT * ON` no fim do registro + inventário (nomes aceitos no probe do C152).
- Oráculo do fuzz ignora `lights.*` na invariante de horas (horas acesas ≠ horas de voo): justificado.
- Fora do escopo respeitado: nenhum arquivo quente, nenhuma UI, nenhuma atuação no sim.

## Ajustes do revisor
1. `FlightReplayer` não injetava `IRandomSource` → replay/fuzz cairiam em `SystemRandomSource()` sem semente (sorteio da lâmpada não reproduzível). Agora passa `SystemRandomSource(RandomSeed)` (propriedade, padrão 0).
2. Mensagem da MEL com concordância errada ("Farol de pouso queimada") → "<nome>: lâmpada queimada — …".

## Pendências (não bloqueiam)
- `PartFlightSession` (UI) usa o `SystemRandomSource()` padrão em produção: aceitável (jogo), mas fica registrado.
- `electrical.external_lighting` (item antigo de iluminação, 1.000 h, $ 800) continua no catálogo e duplica as lâmpadas: decisão do usuário (remover ou virar "fiação e chaves da iluminação").
- `LightingMel` ainda não é consumido pela UI; lâmpada queimada continua acesa no MSFS (fase F3). **Não testado no MSFS.**
