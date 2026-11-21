# Revisão MANUT-FAIXAS-B (Claude) — 2026-11-21

Branch `feat/manut-faixas-b`, commit `c144d49`. Spec: `docs/agents/specs/spec-MANUT-FAIXAS-B.md`.

## Conferido
- `PartHangarView` (Domain): faixa pela saúde **conhecida**, quebrada vence o conhecido, "nunca inspecionada" / "visto agora" / "visto há X h", nota de vida, ordem Broken → Red → Yellow → Unknown → Green. Testes cobrem todos os cenários da spec (11 novos).
- `SelectRecommendedKnown` e o motivo do `Plan` não leem `part.Health`; o Hangar usa só o conhecido. `SelectRecommended` antigo mantido (simulador).
- `HangarViewModel` (quente, diff inteiro): só `ReplacePart` novo e a remoção do `ApplyRegulatoryReset` nos caminhos com peças de `RepairComponent` e `RepairAll` — trocar peça avulsa não devolve mais TBO nem a revisão de 125 h. Caminho legado intacto; retífica e revisão de ignição continuam fazendo o reset.
- `HangarPalette.ForHealth` 40/20. XAML: `Expander` fechado; todos os `StaticResource` usados existem.
- Build 0 erros; `dotnet test` 684/684 (rodado pelo revisor). `agy-audit`: 82 ações, 0 para conferir.

## Veredito: **APROVADO**

## Não testado
- Não visto na tela (aparência do `Expander` no tema escuro, alinhamento do botão do card agora no topo).
- Não testado no MSFS (não depende do simulador).
