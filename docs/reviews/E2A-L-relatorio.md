# E2A-L — Remoção da camada antiga de missões

**Branch:** refactor/e2a-l-camada-antiga · **Commit:** f061247 · **Data:** 2026-11-24

## Plano (antes de codar)
- Substituir guardas de tier baseadas em `MissionRules.Rules.TryGetValue` por `currentAirport.Tier < 1 || currentAirport.Tier > 4` em `MissionGenerator.cs`.
- Remover código morto em `MissionGenerator.cs`: `CreateMissionFromSubCategory`, `GetDestinationRunwayAsync`, `_runwayCache`, `CreateMissionObject`, `ResolveG20Hubs`, `ResolveDynamicHubs`, `CreateMissionFromSubCategoryInMemory`, sobrecargas de `GetCalibratedCategories`, `AddCategoryWeight` e `PickWeighted`.
- Atualizar assinaturas de geradores síncronos em `MissionGenerator.cs` (`CreateMissionObjectSync`, `CreateLocalInstructionMissionSync`, `CreateCheckrideMissionSync`, `CreateScenicTourMissionSync`, `CreateSurveyMissionSync`, `CreateSkydiveMissionSync`) para aceitar `MissionDefinition` em vez de `SubCategoryConfig`.
- Em `MissionBoardCore.cs`, remover o adaptador `ToSubCategoryConfig` e repassar `def` diretamente aos geradores de `MissionGenerator`.
- Apagar `src/FlightCareer.Application/Services/MissionRules.cs` e a pasta `tools/missions/`.
- Reinscrever testes em `MissionBoardCategoryTests.cs`, `MissionGeneratorLocalTests.cs`, `Phase1ContractsTests.cs`, `Phase2ContractTests.cs`, `Phase3To7ContractsTests.cs` e `CareerProgressionTests.cs` sobre `MissionCatalog.Shared` e `MissionDefinition`.
- Validar compilação com 0 erros, testes 100% verdes (incluindo snapshot `missions_golden.json`) e busca limpa por símbolos removidos.

## O que foi feito
- `src/FlightCareer.Application/Services/MissionRules.cs`: Arquivo apagado por completo (183 linhas).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: 
  - Removidos: `CreateMissionFromSubCategory` (linhas ~280-515), `GetDestinationRunwayAsync` (~517-529), `CreateMissionObject` privado assíncrono (~531-551), `_runwayCache`, `ResolveG20Hubs` (~1253-1283), `ResolveDynamicHubs` (~1285-1306), `CreateMissionFromSubCategoryInMemory` (~1374-1594), sobrecargas de `GetCalibratedCategories` (~1596-1651), `AddCategoryWeight` (~1653-1660) e `PickWeighted` (~1662-1680) (654 linhas removidas no total).
  - Guardas de tier em `GenerateMissionsAsync` e `GenerateMissionsInMemory` substituídas por `currentAirport.Tier < 1 || currentAirport.Tier > 4`.
  - Assinaturas dos geradores síncronos atualizadas para receber `MissionDefinition`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - Removido método privado `ToSubCategoryConfig(MissionDefinition def)`.
  - Chamadas aos geradores síncronos de `MissionGenerator` simplificadas para repassar `def` diretamente sem instanciar adaptador intermediário.
- `tools/missions/`: Pasta removida por completo (`generate_catalog_and_texts.py` e `patch_catalog_v2.py`, 1.231 linhas removidas).
- `AGENTS.md` e `docs/CHANGELOG.md`: Atualizados conforme spec.

### Linhas removidas por arquivo
- `src/FlightCareer.Application/Services/MissionRules.cs`: -183 linhas (arquivo excluído)
- `tools/missions/generate_catalog_and_texts.py`: -1.041 linhas (arquivo excluído)
- `tools/missions/patch_catalog_v2.py`: -190 linhas (arquivo excluído)
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: -654 linhas líquidas
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: -29 linhas líquidas

### Testes reescritos (antes → depois)
- `MissionBoardCategoryTests.cs`:
  - *Antes:* `AllSubIds()` iterava `MissionRules.Rules.Values.SelectMany(t => t.Categories)...`
  - *Depois:* `AllSubIds()` itera `MissionCatalog.Shared.Missions.Values.Where(m => m.Enabled).SelectMany(m => new[] { m.Id }.Concat(m.LegacyCodes)).Distinct()`.
- `MissionGeneratorLocalTests.cs`:
  - *Antes:* `GetSubConfig(id)` percorria `MissionRules.Rules.Values` procurando categorias legadas e retornando `SubCategoryConfig`.
  - *Depois:* `GetSubConfig(id)` consulta `MissionCatalog.Shared.Get(id)` com fallback nos `LegacyCodes` e retorna `MissionDefinition`.
  - *Antes:* `GetCalibratedCategories_CuratedScenicAerodrome_ImportsCategory35AndSkewsWeights` chamava `MissionGenerator.GetCalibratedCategories` e conferia peso de categoria aeroclube.
  - *Depois:* `CuratedScenicAerodrome_RecebeTagTuristicoCuradoBase_EAumentaPesoDePanoramico` valida a regra viva atual: aeródromo com turismo curado em base de passeio recebe a tag `turistico_curado:base` via `MissionBoardCore.AugmentOriginTags`, aumentando em 8x o peso de `panoramico` via `MissionWeighting.OriginWeight`.
- `Phase1ContractsTests.cs`:
  - *Antes:* Acessava `MissionRules.Rules[2].Categories.FirstOrDefault(c => c.Id == "aeroclube")` para obter subcategorias.
  - *Depois:* Assegura diretamente que as missões `panoramico`, `circuito_tgl` e `ferry_posicionamento` existem no `MissionCatalog.Shared`.
- `Phase2ContractTests.cs`:
  - *Antes:* Instanciava `SubCategoryConfig` para `subSurvey` e `subSkydive`.
  - *Depois:* Instancia `MissionDefinition` com os mesmos parâmetros para validação dos dados de contrato.
- `Phase3To7ContractsTests.cs`:
  - *Antes:* `CareerPhases_TodosSubtiposDeMissionRulesPossuemFaseDefinida` iterava `MissionRules.Rules`.
  - *Depois:* `CareerPhases_TodosSubtiposDoCatalogoPossuemFaseDefinida` itera `MissionCatalog.Shared.Missions.Values` (incluindo `LegacyCodes`) e valida que todos os IDs possuem fase regulatória válida (1 a 7).
  - *Antes:* Buscava `sub` navegando em `MissionRules.Rules[1].Categories.First(c => c.Id == "contratos_carreira_t1").SubCategories...`
  - *Depois:* Obtém a definição diretamente via `MissionCatalog.Shared.Get("fretamento_executivo")!`, `carga_fragil` e `medevac_dinamico`, assertando os IDs normalizados correspondentes (`carga_dgr`, `medevac_remoto`).
- `CareerProgressionTests.cs`:
  - *Antes:* Instanciava `SubCategoryConfig` para `subCat1`, `subCat2`, `subCat5`.
  - *Depois:* Instancia `MissionDefinition` com perfis correspondentes.

### Testes apagados com motivo
- Nenhum teste de comportamento/regra foi apagado; apenas testes que afirmavam a existência das estruturas internas mortas (`TierConfig`/`CategoryConfig`) foram reescritos sobre `MissionCatalog.Shared` mantendo a intenção original.

## Critérios de aceite
- [x] Grep por `MissionRules|TierConfig|CategoryConfig|SubCategoryConfig|GetCalibratedCategories|CreateMissionFromSubCategory|ResolveG20Hubs|ResolveDynamicHubs` em `src/` retorna 0 resultados.
- [x] `dotnet build src/FlightCareer.slnx` com 0 erros.
- [x] `dotnet test src/FlightCareer.Tests` 100 % aprovado (1.008/1.008 aprovados). Snapshot `missions_golden.json` passa sem ser regravado.
- [x] Linhas removidas por arquivo, testes reescritos (antes → depois) e testes apagados documentados no relatório.
- [x] Entrada no fim de `docs/CHANGELOG.md` e atualização em `AGENTS.md` › 7.
- [x] Commit: `refactor(missoes): remove a camada antiga de missoes (E2A-L)`.

## Build e testes
- dotnet build: 0 erros, 3 avisos pré-existentes em testes
- dotnet test: 1.008 aprovados, 0 falhas, 0 ignorados
- Snapshot `missions_golden.json`: intacto e aprovado em `MissionCatalogEquivalenceTests`

## Não testado
- Interface gráfica (WPF) e voo ao vivo no simulador MSFS (mudança 100% de refatoração de backend e testes).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida ou pendência. Refatoração limpa e determinística.
