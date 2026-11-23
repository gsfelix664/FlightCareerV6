# Revisão CATALOGO-MISSOES (Claude, 2026-11-23)

**Veredito: APROVADO** (com 1 ajuste feito pelo revisor).

## Conferido
- Teste de equivalência criado **antes** da refatoração (commit `01d1ce7`) e snapshot `missions_golden.json` **inalterado** no commit da refatoração; compara origem, id (normalizado), destino, pax, carga, recompensa, distância, título, TGL, sobrevoos e `ContractData` em 14 aeroportos. Passa.
- Build 0 erros; 969 testes passam (rodado pelo revisor).
- Consumidores trocados por propriedades do catálogo: `FlightService` (`generator == skydive`), `FlightMonitor` (3 linhas: `survey`/`skydive` e urgência por `board == Medica`, mesmo conjunto de antes: 3.3.x, 4.3.1 e MEDEVAC, que já tinha urgência > 1), `CareerProgressionSimulator` (idem), `MissionDisplayItem` (`vip`; TGL por `generator`, equivalente porque o TGL já tem origem = destino).
- Ajustes por aeródromo viraram dados (`airportAdjustments`) com os mesmos números.
- `career-stab --days 90 --bots 1 --seed 7 --fast`: comparado pelo revisor entre a base (`0f30887`) e a branch — **idêntico** (`stability.csv` sem nenhuma diferença).

## Ajuste feito pelo revisor
1. `AppDbContext.EnsureSchemaUpdatedAsync`: a migração de `SubId` usava `catch {}` silencioso (regra do AGENTS.md › 5) e SQL montado por interpolação. Trocado por comando parametrizado e `Debug.WriteLine` no erro.

## Para a etapa 2 (não bloqueia)
- As cópias por porte **não** foram unificadas (`medevac_dinamico`/`medevac_regional`/`medevac_municipal`, `carga_fragil`/`_regional`/`_municipal`, `fretamento_executivo`/`_regional`, `vip_internacional_curto`/`_regional`). A unificação entra junto com a lista dos 69 tipos (§ 6 do plano), que remapeia todos os ids de qualquer forma.
- Ids `medivac_bush` (grafia) e `diplomatico_militar` serão substituídos pelos ids da lista de 69 tipos.
