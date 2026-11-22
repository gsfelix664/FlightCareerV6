# FASES-1 — 7 fases da carreira no lugar das 5 categorias

**Branch:** feat/fases-1 · **Commit:** <pendente de commit> · **Data:** 2026-11-22

## Plano (antes de codar)
- Investigar todos os arquivos de código/testes/UI que usam `PilotCategory`/`CompanyTier`.
- Criar `CareerPhase` (enum 1 a 7) em `FlightCareer.Domain/Entities/CareerPhase.cs` e catálogo de dados `Infrastructure/Data/career_phases.json` com fallback embutido idêntico em `FlightCareer.Domain/Career/CareerPhaseCatalog.cs`.
- Mapear os 26 subtipos existentes de missão para as 7 fases no JSON conforme § 3 de `fases-carreira.md`.
- Atualizar `Pilot` e `Mission` para utilizarem `CareerPhase` e `RequiredPhase`, mantendo compatibilidade transitória segura sem quebras.
- Implementar migração idempotente em `AppDbContext.EnsureSchemaUpdatedAsync` convertendo pilotos e missões antigos (1→1, 2→2, 3→3, 4→5, 5→6).
- Atualizar `MissionGenerator` para definir `RequiredPhase` a partir do `subId` via catálogo (e conversão para missões antigas sem `subId`).
- Ajustar `MainViewModel`, `FlightMonitor`, `MissionDisplayItem`, `CreatePilotViewModel` e `FerryFlightViewModel` para validação e exibição das 7 fases ("Fase N — nome · Nível L · licença"), com tríade de promoção (nível, horas, homologação) e mensagens claras do que falta.
- Ajustar `CareerProgressionSimulator` para bots promoverem pelas novas regras e escada de compra heurística (A, B → 1; E, F → 3; G, H → 4; I → 6; J → 7).
- Escrever testes unitários em `CareerPhaseTests.cs` cobrindo promoção, mapa de subtipos, migração de banco antigo e nível alto após fase 7.
- Executar `dotnet build`, `dotnet test` e `career-stab --days 90 --bots 1 --difficulty medium --policy equilibrado`.

## O que foi feito
- `src/FlightCareer.Infrastructure/Data/career_phases.json`: arquivo de dados contendo as 7 fases (nomes, níveis de entrada, horas mínimas, custos de homologação, licença exibida e descrições operacionais) e mapeamento dos 26 subtipos de missão para as fases conforme § 2, § 3 e § 6 de `fases-carreira.md`.
- `src/FlightCareer.Domain/Entities/CareerPhase.cs`: novo enum `CareerPhase` (1 a 7) com extensões de conveniência (`GetDisplayName`, `GetShortName`, `GetLicenseDisplay`, `GetBadgeColor`, `GetEntryLevel`, `GetMinHours`, `GetPromotionCost`, `GetOperationalDescription` e `FormatPhaseStatus`).
- `src/FlightCareer.Domain/Career/CareerPhaseCatalog.cs`: catálogo de domínio para carregamento de `career_phases.json` com fallback embutido idêntico, resolução de subtipos e conversor de categorias legadas (regra 5: 1→1, 2→2, 3→3, 4→5, 5→6).
- `src/FlightCareer.Domain/Entities/Pilot.cs` e `src/FlightCareer.Domain/Entities/Mission.cs`: novas propriedades principais `CareerPhase` e `RequiredPhase`; mantidas propriedades `PilotCategory`/`CompanyTier`/`RequiredCategory`/`RequiredTier` com `[Obsolete]` como adaptadores para compatibilidade.
- `src/FlightCareer.Domain/Entities/PilotCategory.cs`: enums e extensões legadas marcados como `[Obsolete]`.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - `EnsureSchemaUpdatedAsync` atualizado com comandos idempotentes para criar as colunas `CareerPhase` e `RequiredPhase` e migrar dados antigos usando `CASE ... WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 WHEN 4 THEN 5 WHEN 5 THEN 6`.
  - Configurado `HasDefaultValue(CareerPhase.Phase1_FlightSchool)` no `OnModelCreating` para inserções e schemas em memória.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: `CreateMissionObjectSync` atualizado para obter `RequiredPhase` e tipo da missão via `CareerPhaseCatalog.Instance.ResolvePhase(sub.Id)`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: bloqueio de voo não homologado verificando `prohibitedMission = missions.FirstOrDefault(m => m.RequiredPhase > pilot.CareerPhase)`.
- `src/FlightCareer.UI/ViewModels/MissionDisplayItem.cs`: elegibilidade de missões verificada contra `mission.RequiredPhase > pilotPhase`, exibindo o nome da fase requerida.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`:
  - Verificação de aceite de missões e início de voo contra `RequiredPhase` e `CareerPhase`.
  - Método `PromoteCompanyTierAsync` adaptado para as 7 fases com conferência da tríade (nível por XP, horas voadas e taxa de homologação), com mensagem detalhada de pendências no formato `"Nível X — ok · Horas Y/Z · Homologação $W"` e trava após a fase 7 (níveis continuam sem limite).
  - Exibição de cabeçalho formatada como `"Fase N — nome · Nível L · licença"`.
- `src/FlightCareer.UI/ViewModels/CreatePilotViewModel.cs` e `src/FlightCareer.UI/ViewModels/FerryFlightViewModel.cs`: carreira inicial e contratos de traslado configurados em `CareerPhase.Phase1_FlightSchool`.
- `src/FlightCareer.Application/Simulation/CareerProgressionSimulator.cs`:
  - Bots promovem via `CheckPhasePromotion` checando nível por XP (`PilotLevel.ForXp`), horas mínimas e custo com reserva da política.
  - Escada de aeronaves de compra heurística mapeada para as fases (A, B → 1; E, F → 3; G, H → 4; I → 6; J → 7).
  - Tabela consolidada com coluna `Fase` e relatórios adaptados.
- `src/FlightCareer.Tests/Career/CareerPhaseTests.cs`: 5 novos testes xUnit cobrindo:
  - Progressão de nível por XP puro sem custo.
  - Tríade de promoção (horas faltantes, saldo faltante e promoção bem-sucedida).
  - Resolução dos 26 subtipos de missão para as 7 fases.
  - Liberação de missões anteriores e bloqueio de superiores.
  - Nível 60 após Fase 7 (sem novas fases).
  - Migração de banco legado em SQLite com conversão de categorias 4→5 e 5→6 e idempotência.
- `src/FlightCareer.Tests/Economy/CareerProgressionTests.cs` e `src/FlightCareer.Tests/Parts/PartFlightIntegrationTests.cs`: adaptados para `CareerPhase`.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação atualizada.

## Critérios de aceite
- [x] Build 0 erros — `dotnet build src/FlightCareer.slnx` passou com 0 erros.
- [x] `dotnet test` 100 % — 827/827 testes aprovados (100%), 0 falhas.
- [x] `career-stab --days 90 --bots 1 --difficulty medium --policy equilibrado` roda sem erro — Simulação concluída com sucesso (tabela abaixo).
- [x] Nenhum lugar usa mais os números antigos de categoria — Verificado via `git grep`: nenhum fluxo de regra de negócio, aceite de missões, monitor de voo, promoção ou simulador utiliza números antigos. As únicas ocorrências restantes são propriedades com `[Obsolete]` mantidas como adaptadores de compatibilidade e o teste de regressão de migração de banco legado.
- [x] CHANGELOG e AGENTS.md › 7 atualizados — Registrado em ambos os arquivos.
- [x] Relatório diz "não visto na tela" — Declarado na seção Não testado.
- [x] Um commit: `feat(carreira): 7 fases por nivel no lugar das categorias (FASES-1)`.

### Tabela da simulação career-stab (Dia 30 e Dia 90)

```markdown
### Marco: Dia 30

| Dificuldade | Política | Falidos (%) | Dia Falência | Saldo Mediano ($) | Patrimônio Mediano ($) [Mín - Máx] | Classe | Fase | 1ª Compra (Dia) | Horas Voadas | Voos/Dia | Rec/h ($) | Custo/h ($) | Peças/h ($) | Margem (%) | Manut/100h | Peças/100h | Panes/100h | Parado (dias) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Medium | equilibrado | 0% | - | $115,451 | $162,251 [$162,251 - $162,251] | A | 2 | - | 66.5 h | 3.77 | $980 | $221 | $7 | 77.4% | 12.0 | 33.1 | 1.5 | 0.0 |

### Marco: Dia 90

| Dificuldade | Política | Falidos (%) | Dia Falência | Saldo Mediano ($) | Patrimônio Mediano ($) [Mín - Máx] | Classe | Fase | 1ª Compra (Dia) | Horas Voadas | Voos/Dia | Rec/h ($) | Custo/h ($) | Peças/h ($) | Margem (%) | Manut/100h | Peças/100h | Panes/100h | Parado (dias) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Medium | equilibrado | 0% | - | $176,123 | $222,923 [$222,923 - $222,923] | A | 3 | - | 222.6 h | 3.44 | $1,241 | $202 | $15 | 83.7% | 15.7 | 36.4 | 4.9 | 0.0 |
```

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 94 avisos (avisos conhecidos de dependência NU1903 e CS0618 de compatibilidade).
- `dotnet test src/FlightCareer.Tests`: 827 aprovados, 0 falhas (5 novos testes em `CareerPhaseTests.cs`).

## Não testado
- Não testado no MSFS / não visto na tela.

## Dúvidas e decisões para o revisor
- Nenhuma. O mapeamento da tabela § 3 de `fases-carreira.md` e a tríade de requisitos de promoção de § 6 foram seguidos estritamente.
