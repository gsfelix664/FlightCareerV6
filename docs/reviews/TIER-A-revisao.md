# Revisão TIER-A (Claude, 2026-11-24)

**Veredito: CORRIGIR (rodada 1)** — com ajustes já feitos pelo revisor.

## Conferido
- Parte A (pista mínima por perfil): todos os perfis com `minRunwayFt`/`paved` pela classe do plano; origem e destino filtrados; tipo sem perfil compatível sai do sorteio. Correto.
- Parte B (classificador): pontuação conforme o plano; heurísticas ignoradas; `linha_auto`/`carga_auto` não pontuam. Casos de referência certos.
- Build 0 erros; 1.001 testes passam (rodado pelo revisor).

## Ajustes feitos pelo revisor
1. **Tier 1 inchado** (1.013 no mundo, alvo 400–600): a estrutura sozinha chegava a 115 pontos (bases militares e aeroportos regionais russos viravam tier 1 sem etiqueta; Bethel tier 1). Agora: teto da estrutura 60 (`Structure.MaxTotal`), corte do tier 1 em 120, `linha:mega_hub` vale 120 (garante tier 1 sozinho), base militar (`is_military` do simulador, gravado em `Airport.IsMilitary`) sem etiqueta de linha no máximo tier 2 (`Thresholds.MilitaryBestTierWithoutLinha`). Resultado: tier 1 = 457 no mundo (23 no Brasil), tier 2 = 2.663 (6,9 %). `airport_tier_rules.json` ganhou `Version: 2`. Testes `AirportTierReviewTests`.
2. `AirportTierRules.LoadDefault` tinha `catch { }` silencioso (AGENTS.md › 5): agora registra com `Debug.WriteLine`.
3. `tier-report.csv` (38.600 linhas) e `tier-report.md` tinham sido commitados na raiz do repositório: removidos e ignorados no `.gitignore`.

## CORRIGIR (rodada 2, Gemini)
1. **País errado:** `ResolveInitialCountry` devolve a região ICAO (ZS, UI, OM…) quando não conhece o prefixo; o enriquecimento pelo csv não corrige porque o valor tem 2 letras. Isso quebra as regras de destino "mesmo país" / "outro país". Com a posição batendo, o `iso_country` do csv deve sempre prevalecer.
2. **Linha aérea só entre aeroportos de linha:** origem e destino com etiqueta `linha` ou `linha_auto` nos tipos de linha (e `carga:cargueiros`/`linha`/`linha_auto`/`carga_auto` nos perfis de carga a jato).
3. **Recalcular o tier quando a versão das regras mudar**, a partir dos sinais gravados, sem reimportar.

## Rodada 2 (Gemini) — APROVADO (Claude, 2026-11-24)
- País pelo `iso_country` do csv com a posição batendo (`EnrichFromCsvAsync`, `TierReporter`); `requireAnyTag` na origem (missão), no destino e por perfil, com `linha`/`linha_auto` nos tipos de linha e `linha`/`linha_auto`/`carga:cargueiros`/`carga_auto` nos perfis de carga a jato; `DataSeeder.EnsureTiersUpToDateAsync` recalcula o tier pelos sinais gravados quando `AirportTierRules.Version` muda (chamado no início de `SeedAsync`).
- Build 0 erros; 1.004 testes passam (rodado pelo revisor).
- `board-report --boards 30` (revisor): SBMT 0 % de ponte aérea/troncal/regional internacional/longo curso/carga paletizada; SBRJ ponte aérea 27 %, troncal 14 %; SBGR carga paletizada 21 %, longo curso 16 %; PABE alimentador 15 %, regional essencial e medevac remoto ~4 % cada.
- Tiers aprovados pelo usuário (`tier-report`: tier 1 = 457 no mundo, 23 no Brasil).
