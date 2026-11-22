# HANGAR-ICONES-PECAS — Revisão (Claude)

**Branch:** `feat/hangar-icones-pecas` · **Data:** 2026-11-22

## Veredito: **APROVADO** (com ajustes do revisor commitados na branch)

## Conferido
1. Estado calculado só com a saúde conhecida (`KnownHealth`/`KnownAtAirframeHours`/`IsBroken`); teste `Nao_usa_a_saude_real`.
2. Regra do "?": `known − growthPerHour × horas < limite inferior da faixa conhecida`; `growthPerHour` lido de `ComponentCatalog.Visibility` no `PilotAircraftWrapper`. Vermelho e quebrado nunca viram "?".
3. Texto de horas dos grupos Motor/Óleo/Ignição vem de `EngineTboRemainingHours`, `HoursUntilNext50hInspection`, `HoursUntilNext500hInspection` e das constantes de `MaintenancePricing`; demais grupos sem texto. Aeronaves sem peças sem mudança (bindings com `InverseBooleanToVisibilityConverter`, que existe em `App.xaml`).
4. `agy-audit`: 109 ações, 0 para conferir.

## Ajustes do revisor (rodada única, feitos pelo Claude)
1. Novo estado `Worn` (80–40 %, `GoodAbovePct = 80`), decisão do usuário nesta sessão; a marca de 80 % não gera "?".
2. Ícone de quebrado: círculo trincado em vez de "X"; check verde dentro de círculo; gasta = meio círculo + check.
3. Contagem: vermelho e quebrado num só "✖" (antes saíam dois "✖"); lógica única em `HangarPalette.CountsText`.
4. Tooltips e legenda sem números de %.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros. `dotnet test`: 737/737.

## Não testado
- Não visto na tela (ícones, tamanhos e cores). Não depende do MSFS.
