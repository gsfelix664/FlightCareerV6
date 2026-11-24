# Spec E2A-L — remover a camada antiga de missões

## 1. Objetivo
Apagar o código morto da geração antiga de missões (ids antigos, `MissionRules` por tier, `CreateMissionFromSubCategory*`, `GetCalibratedCategories`, `ResolveG20Hubs`/`ResolveDynamicHubs`) e reescrever os testes que dependem dele sobre o núcleo novo (`MissionBoardCore` + `MissionCatalog`), **sem mudar nenhuma missão gerada**.

## 2. Fatos já levantados (não redescobrir)
- Caminho vivo do jogo: `MissionGenerator.GenerateMissionsAsync` → `MissionBoardCore.GenerateBoard` (linhas ~137–242). Caminho vivo dos bots/relatórios: `MissionGenerator.GenerateMissionsInMemory` (~linha 1322) → `MissionBoardCore`.
- Nos dois caminhos, `MissionRules.Rules.TryGetValue(currentAirport.Tier, out var tierConfig)` (linhas 138 e 1322) só serve de guarda "tier 1–4"; `tierConfig` não é usado depois. Trocar por uma guarda explícita de tier 1–4 com o mesmo comportamento (tier fora de 1–4 → retorna as existentes / lista vazia, como hoje).
- Código morto em `src/FlightCareer.Application/Services/MissionGenerator.cs` (confirme com Grep que não há outro chamador antes de apagar):
  - `CreateMissionFromSubCategory` (~280) e `CreateMissionFromSubCategoryInMemory` (~1374), com os helpers que só eles usam;
  - `ResolveG20Hubs` (~1253) e `ResolveDynamicHubs` (~1285);
  - as três sobrecargas de `GetCalibratedCategories` (~1596–1610).
- `src/FlightCareer.Application/Services/MissionRules.cs` (183 linhas): `TierConfig`, `CategoryConfig`, `SubCategoryConfig` e `MissionRules`. Monta categorias por tier e **duplica** as missões com ids antigos (`LegacyCodes`) e categorias falsas `aeroclube` e `contratos_carreira_t{n}` só para testes antigos. Apagar o arquivo inteiro se nada vivo depender dele; se algum tipo for usado por código vivo (ex.: `ProbRange`, que vem de `Domain.Missions`), mantenha só o necessário.
- Testes que usam a camada antiga (26 ocorrências):
  - `src/FlightCareer.Tests/Missions/MissionBoardCategoryTests.cs:12`
  - `src/FlightCareer.Tests/Missions/MissionGeneratorLocalTests.cs:34, 343–367` (`GetCalibratedCategories_CuratedScenicAerodrome_ImportsCategory35AndSkewsWeights`)
  - `src/FlightCareer.Tests/Missions/Phase1ContractsTests.cs:228`
  - `src/FlightCareer.Tests/Missions/Phase3To7ContractsTests.cs:27, 131, 172, 192`
- `MissionCatalog.Normalize`/`legacyIds` continuam (ids antigos gravados em logbook e textos). **Não** remova a resolução de ids legados do catálogo.
- Scripts `tools/missions/generate_catalog_and_texts.py` e `tools/missions/patch_catalog_v2.py`: foram usados uma vez para gerar o catálogo; não são chamados por nada. Remover a pasta `tools/missions/`.

## 3. Arquivos
- Mexer: `MissionGenerator.cs`, `MissionRules.cs` (apagar), os 4 arquivos de teste acima, `tools/missions/` (apagar).
- **Fora do escopo:** `MissionBoardCore`, `MissionWeighting`, `mission_catalog.json`, `mission_texts.json`, `missions_golden.json`, `FlightMonitor`, `FlightService`, qualquer arquivo quente. Não mude pesos, faixas nem a ordem de consumo do `Random` no caminho vivo.

## 4. Reescrita dos testes (mesma intenção, sobre o núcleo novo)
Para cada teste que depende da camada antiga, preserve a **intenção** usando `MissionCatalog.Shared` e/ou `MissionGenerator.GenerateMissionsInMemory` / `MissionBoardCore.GenerateBoard`:
- `MissionBoardCategoryTests`: em vez de iterar `MissionRules.Rules`, iterar `MissionCatalog.Shared.Missions.Values` (habilitadas) e conferir o mesmo invariante (todo id tem categoria/board válida).
- `MissionGeneratorLocalTests:34`: mesmo invariante sobre o catálogo.
- `GetCalibratedCategories_CuratedScenicAerodrome_...`: reescrever como teste de quadro: origem aeródromo turístico curado gera `panoramico` com peso maior que num aeródromo comum de mesmo tier (N quadros com semente fixa, comparar frequência), ou testar `MissionWeighting.OriginWeight` com as etiquetas aumentadas (`MissionBoardCore.AugmentOriginTags`). Use o que exercitar a regra real hoje.
- `Phase1ContractsTests:228` e `Phase3To7ContractsTests` (27, 131, 172, 192): pegar a definição por `MissionCatalog.Shared.Get("<id novo>")` (ex.: `fretamento_executivo`, `carga_fragil`, `medevac_dinamico` → use `Normalize` para achar o id atual) e manter as asserções sobre faixas/regras.
- Se um teste antigo só testava a estrutura removida (sem regra de negócio por trás), apague-o e liste no relatório com o motivo.

## 5. Critérios de aceite
1. `Grep` por `MissionRules|TierConfig|CategoryConfig|SubCategoryConfig|GetCalibratedCategories|CreateMissionFromSubCategory|ResolveG20Hubs|ResolveDynamicHubs` em `src/` retorna 0 resultados (exceto comentários de CHANGELOG/docs).
2. `dotnet build src/FlightCareer.slnx` com 0 erros.
3. `dotnet test src/FlightCareer.Tests` 100 % aprovado; o teste do snapshot `missions_golden.json` passa **sem regravar o snapshot** (prova de que nenhuma missão mudou).
4. Relatório lista: linhas removidas por arquivo, testes reescritos (antes → depois) e testes apagados com motivo.
5. Entrada no fim de `docs/CHANGELOG.md`: `## 2026-11-24 - Remoção da camada antiga de missões (E2A-L) (Gemini)`; em `AGENTS.md` › 7, no bloco "Catálogo de 69 tipos ... (E2A)", trocar a linha "Pendente (E2A-L): ..." por "Camada antiga removida (E2A-L)".
6. Um commit: `refactor(missoes): remove a camada antiga de missoes (E2A-L)`.
