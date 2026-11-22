# Spec FASES-1 — 7 fases da carreira no lugar das 5 categorias

**Objetivo (1 frase):** substituir as 5 categorias (`PilotCategory`) por 7 fases ligadas ao nível do piloto, com a fase limitando o aceite de missões (não a compra de avião) e a promoção exigindo nível, homologação e horas.

## Fatos já levantados (não redescobrir)
- Desenho e decisões do usuário: `docs/plans/fases-carreira.md` (§§ 1, 2, 3, 6 e a regra de níveis sem limite depois da fase 7) e `docs/plans/xp-proficiencia.md` › 6.8. Leia os dois.
- Base: branch com a XP-NOVO mergeada (`Domain/Xp/PilotLevel`: XP do nível L = 300 × (L − 1)², sem nível máximo; `Pilot.Level`).
- Categorias hoje: `Domain/Entities/PilotCategory.cs` (enum 1–5 gravado como inteiro; nomes, `GetPromotionCost`, `GetRequiredHours`, `GetRequiredXP`), `CompanyTier` (alias por cast). Usada em 11 arquivos: `MissionGenerator` (decide `RequiredCategory` pela carga/pax, ~linhas 320–340), `CareerProgressionSimulator`, `Mission`, `Pilot`, `AppDbContext`, `FlightMonitor`, `CreatePilotViewModel`, `FerryFlightViewModel`, `MainViewModel` (aceite, início de voo, promoção ~linha 937), `MissionDisplayItem`.
- Tipos de missão: `MissionRules.cs` (subtipos "1.1.1"… — o primeiro dígito é o tier do aeroporto de origem, não a fase). `Mission.SubId` gravado desde 2026-11-22 (nulo em missões antigas).

## Regras (decisões do usuário)
1. Fases 1–7 (nome, faixa de níveis, licença exibida) da tabela § 2 de `fases-carreira.md`.
2. **Subir de nível**: só XP. **Mudar de fase**: nível ≥ nível de entrada **e** homologação paga **e** horas mínimas (§ 6: fase 2 → L6, 40 h, $35 mil; 3 → L13, 150 h, $120 mil; 4 → L21, 300 h, $300 mil; 5 → L29, 500 h, $500 mil; 6 → L37, 1.000 h, $1,2 mi; 7 → L45, 1.500 h, $2 mi). Valores num JSON novo `Infrastructure/Data/career_phases.json` (nomes, níveis, horas, custo, licença).
3. A fase **não** limita compra de avião; limita **aceite e início** de missão.
4. Missão exige a fase pelo **subtipo** (tabela § 3 de `fases-carreira.md`), também no `career_phases.json` (mapa `subId` → fase). Missões de fases anteriores continuam liberadas (decisão provisória do revisor, como sugerido no plano; deixe num único ponto fácil de trocar).
5. Missão antiga sem `SubId`: fase pela categoria antiga convertida (1→1, 2→2, 3→3, 4→5, 5→6).
6. Depois da fase 7, níveis continuam subindo sem novas fases.

## Passo a passo
- Novo enum `CareerPhase` (1–7) em `Domain/Entities` (pode reaproveitar `PilotCategory` renomeando valores; escolha a forma com menos risco e descreva). Conversão do banco em `EnsureSchemaUpdatedAsync` (**quente**, só o necessário): pilotos e missões gravados como categoria 1–5 viram fase pela regra 5 (use uma coluna nova `Phase` + conversão idempotente, ou conversão no lugar com marca de versão — o teste de banco antigo precisa passar).
- `MissionGenerator`: `RequiredPhase` pelo subtipo; mantém os filtros de capacidade do avião.
- `MainViewModel` (**quente**): aceite e início de voo checam a fase; promoção usa as três exigências; mensagens dizem o que falta ("Nível 6 — ok · Horas 32/40 · Homologação $35.000").
- Telas: onde hoje aparece "Categoria N", mostrar "Fase N — nome · Nível L · licença". Quadro de missões: bloqueio pela fase com o nome da fase exigida.
- `CreatePilotViewModel`: carreira nova começa na fase 1, nível 1. `FerryFlightViewModel`: traslado é fase 1.
- `FlightMonitor`: só ajustar o que usar a categoria (provavelmente tipo de missão para a nota); nada mais.
- `CareerProgressionSimulator`: bots promovem pelas regras novas; escada de aviões mapeada para a fase das missões que o avião consegue fazer (A, B → 1; E, F → 3; G, H → 4; I → 6; J → 7) só como heurística de compra do bot.

## Fora do escopo
- Contratos novos (voo panorâmico, TGL, translado básico, foto aérea, paraquedistas etc.) — tarefa FASES-2.
- Calibração dos valores (fica com o revisor na ECON-STAB).

## Testes
- Promoção: nível 6 com 39 h → bloqueia (horas); com 40 h sem saldo → bloqueia (homologação); com tudo → fase 2. Nível sobe de 1 a 5 sem pagar nada.
- Mapa subtipo → fase (amostras de cada fase da tabela § 3); missão de fase anterior liberada; de fase superior bloqueada.
- Migração: banco antigo com piloto categoria 4 → fase 5; missão categoria 5 sem SubId → fase 6.
- Nível 60 com fase 7: continua fase 7.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `career-stab --days 90 --bots 1 --difficulty medium --policy equilibrado` roda sem erro (colar a tabela no relatório).
- [ ] Nenhum lugar usa mais os números antigos de categoria (busque `PilotCategory`/`CompanyTier` e explique os que sobrarem).
- [ ] CHANGELOG e AGENTS.md › 7 (Missões e progressão). Relatório diz "não visto na tela".
- [ ] Um commit: `feat(carreira): 7 fases por nivel no lugar das categorias (FASES-1)`.
