# Revisão E2A-L — remoção da camada antiga de missões

**Veredito: APROVADO** (Claude, 2026-11-24). Branch `refactor/e2a-l-camada-antiga`, commit `9e6fee8`.

## Conferido
- Escopo: `MissionRules.cs` apagado; `CreateMissionFromSubCategory*`, `ResolveG20Hubs`/`ResolveDynamicHubs` e `GetCalibratedCategories` removidos de `MissionGenerator.cs`; `tools/missions/` apagado. Código de produção acrescentado: só a guarda de tier 1–4 (nos dois caminhos, mesmo retorno de antes) e a troca do parâmetro `SubCategoryConfig` por `MissionDefinition` nos geradores especiais.
- `MissionBoardCore.cs` estava fora do escopo, mas a mudança é consequência necessária (o adaptador `ToSubCategoryConfig` dependia do tipo removido) e é mecânica: passa `def` direto.
- Testes reescritos mantêm a intenção: invariantes iterando o `MissionCatalog`; o teste do aeródromo turístico curado passou a testar `AugmentOriginTags` + `MissionWeighting.OriginWeight` (peso ×8); asserções de `SubId` passaram aos ids atuais (`carga_fragil` → `carga_dgr`, `medevac_dinamico` → `medevac_remoto`), coerente com `MissionCatalog.Normalize`.
- `missions_golden.json` não foi alterado e o teste passa: nenhuma missão gerada mudou.
- Build (revisor): 0 erros. `dotnet test`: 1.008/1.008.
- `agy-audit`: 197 ações, 5 para conferir — todas dentro do worktree (remoção prevista na spec e leituras de JSON com Python). Nada fora do projeto.

Não depende do MSFS.
