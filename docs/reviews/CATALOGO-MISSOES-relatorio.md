# CATALOGO-MISSOES — Catálogo de missões em JSON com IDs de texto estáveis

**Branch:** refactor/catalogo-missoes · **Commit:** (pendente) · **Data:** 2026-11-23

## Plano (antes de codar)
- Extrair as 59 missões do código C# (`MissionRules.cs` e condicionais numéricas espalhadas) para um catálogo central em JSON (`Infrastructure/Data/mission_catalog.json`) com IDs estáveis em snake_case em português, sem alterar nenhuma missão gerada.
- Criar teste de equivalência com snapshot golden cobrindo 14 aeroportos de teste (T1 a T4) antes de mexer no gerador.
- Modelar classes puras em `Domain/Missions/`: `MissionDefinition`, `MissionCatalog` (loader com cache, embedded resource e fallback a arquivo, normalizador bidirecional) e `ProbRange`.
- Refatorar o gerador (`MissionGenerator.cs`) para eliminar todas as checagens por código numérico, chaveando por `generator`, `destination.rule`, `fixedTexts` e interpretando `airportAdjustments` em dados.
- Atualizar todos os consumidores (`MissionPayTable`, `CareerPhaseCatalog`, `XpRules`/`XpCalculator`, `MissionTextCatalog`, `FlightMonitor`, `FlightService`, `CareerProgressionSimulator`, `MissionCompletion`, `MissionBoardCategory`, `MissionDisplayItem`).
- Adicionar migração idempotente de banco em `AppDbContext.EnsureSchemaUpdatedAsync` atualizando `SubId` na tabela `Missions`.
- Validar equivalência estrita, integridade de catálogo, compilação limpa, suíte xUnit 100% verde e estabilidade da carreira idêntica no `career-stab`.

## O que foi feito
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`: catálogo unificado contendo as 59 missões com IDs snake_case, `legacyCodes[]`, categorização em grupos por tier e regras de ajustes por aeroporto (`airportAdjustments`).
- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`: entidade pura contendo o esquema completo da missão (esquema com campos da etapa 1 e campos reservados opcionais para a etapa 2).
- `src/FlightCareer.Domain/Missions/ProbRange.cs`: value object imutável com `ProbRangeJsonConverter` com suporte a deserialização tanto de arrays `[peso, min, max]` quanto objetos.
- `src/FlightCareer.Domain/Missions/MissionCatalog.cs`: loader puro, singleton thread-safe com embedded resource e fallback local, métodos `Get(id)`, `Normalize(idOuCodigo)`, layouts de tiers e lista de ajustes por aeródromo.
- `src/FlightCareer.Application/Services/MissionRules.cs`: refatorado como adaptador fino sobre `MissionCatalog.Shared.Tiers`, preservando os contratos de `TierConfig`, `CategoryConfig` e `SubCategoryConfig`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - `CreateMissionFromSubCategory` e `CreateMissionFromSubCategoryInMemory`: substituídas todas as checagens de `sub.Id == "x.y.z"` por checagens no esquema (`sub.Destination.Rule`, `sub.Generator`, `sub.FixedTexts`).
  - `GetCalibratedCategories`: refatorado para iterar e aplicar as regras dinâmicas de `MissionCatalog.Shared.AirportAdjustments` (importação de categorias, acréscimo de peso, skewing de pesos internos).
  - Métodos auxiliares de geração especial (`CreateLocalInstructionMissionSync`, `CreateCheckrideMissionSync`, `CreateScenicTourMissionSync`, `CreateSurveyMissionSync`, `CreateSkydiveMissionSync`) atualizados para consultar `catalog.Render(sub.Id, ...)` em vez de códigos literais.
- `src/FlightCareer.Domain/Economy/MissionPayTable.cs` e `mission_pay.json`: removidos prefixos numéricos e adaptada resolução para buscar `def.PayFamily` via `MissionCatalog.Shared.Get(subId)`.
- `src/FlightCareer.Domain/Career/CareerPhaseCatalog.cs` e `career_phases.json`: removido `subIdToPhase` estático; `ResolvePhase` busca `def.Phase` do catálogo; dicionário público preenchido com todos os IDs e legados.
- `src/FlightCareer.Domain/Xp/XpRules.cs` e `XpCalculator.cs`: atualizada chave de multiplicador para `"cargueiro_noturno"` em `xp_rules.json`; `XpCalculator` resolve `def.XpType` e `def.PayFamily` via catálogo.
- `src/FlightCareer.Domain/Missions/MissionTextCatalog.cs` e `mission_texts.json`: re-chaveado para os novos IDs de texto (incluindo `voo_panoramico-turistico`); `Render` normaliza códigos legados automaticamente caso requisitado.
- `src/FlightCareer.Domain/Missions/MissionBoardCategory.cs`: consulta `MissionCatalog.Shared.Get(id)?.Board`.
- `src/FlightCareer.UI/ViewModels/MissionDisplayItem.cs`: `IsVip` e `isCircuitOrTgl` consultam o catálogo.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: linhas 674/683 consultam `def.Generator`; linha 1765 consulta `board == MissionBoardKind.Medica`.
- `src/FlightCareer.Application/Services/FlightService.cs`: linha 315 consulta `def.Generator != "skydive"`.
- `src/FlightCareer.Application/Simulation/CareerProgressionSimulator.cs`: linha 676 consulta `board == MissionBoardKind.Medica`.
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: linhas 68 e 93 consultam `def.Generator`.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: instrução idempotente em `EnsureSchemaUpdatedAsync` atualizando `SubId` na tabela `Missions` com base em `MissionCatalog.Shared.LegacyCodes`.
- `src/FlightCareer.Tests/Missions/MissionCatalogEquivalenceTests.cs`: teste de equivalência com snapshot golden (`missions_golden.json`) de 270 missões em 14 aeródromos (T1 a T4) validado com 100% de correspondência.
- `src/FlightCareer.Tests/Missions/MissionCatalogTests.cs`: suíte de integridade validando:
  1. Todos os 59 `legacyCodes` aparecem exatamente uma vez.
  2. Todo id tem fase, família de pagamento existente, texto com $\ge 3$ variantes, e board válido.
  3. Comportamento do normalizador `MissionCatalog.Shared.Normalize`.
  4. Ausência total de códigos numéricos de missão em arquivos `.cs` de produção (`Domain`, `Application`, `Infrastructure`, `UI`).
  5. Migração de schema em banco SQLite antigo com códigos numéricos para IDs novos.

## Critérios de aceite
- [x] Teste de equivalência antes/depois: snapshot golden de 270 missões em 14 aeródromos 100% idêntico.
- [x] Catálogo JSON centralizado em `Infrastructure/Data/mission_catalog.json` e embutido como recurso em `FlightCareer.Domain`.
- [x] IDs de texto estáveis em snake_case em português para todas as 59 missões.
- [x] Remoção de todos os códigos numéricos de missão fora de `legacyCodes` e testes de compatibilidade.
- [x] Migração de banco SQLite idempotente em `AppDbContext.EnsureSchemaUpdatedAsync`.
- [x] `career-stab` (`--days 90 --bots 1 --seed 7 --fast`) idêntico à baseline nos 9 cenários.
- [x] Build limpo com 0 erros (`dotnet build src/FlightCareer.slnx`).
- [x] Suíte de testes 100% verde (`dotnet test src/FlightCareer.Tests` com 969 aprovados, 0 falhas).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, avisos conhecidos de nullable/obsolete.
- `dotnet test src/FlightCareer.Tests`: 969 aprovados, 0 falhas (tempo: 59 s).
  - Novos testes: `MissionCatalogEquivalenceTests`, `MissionCatalogTests` (5 testes de integridade e migração).
- `dotnet run --project src/FlightCareer.Replay -- career-stab --days 90 --bots 1 --seed 7 --fast`:
  - Easy/agressivo: Saldo $1,003,799 | Patrimônio $3,453,799 | 219.0 h | Margem 89.6%
  - Easy/cauteloso: Saldo $876,211 | Patrimônio $1,296,211 | 222.1 h | Margem 89.0%
  - Easy/equilibrado: Saldo $1,105,920 | Patrimônio $3,555,920 | 225.8 h | Margem 92.6%
  - Hard/agressivo: Saldo $147,369 | Patrimônio $194,169 | 210.8 h | Margem 84.2%
  - Hard/cauteloso: Saldo $116,388 | Patrimônio $163,188 | 203.4 h | Margem 80.2%
  - Hard/equilibrado: Saldo $136,475 | Patrimônio $183,275 | 207.2 h | Margem 84.0%
  - Medium/agressivo: Saldo $35,738 | Patrimônio $372,698 | 206.4 h | Margem 90.1%
  - Medium/cauteloso: Saldo $58,031 | Patrimônio $394,991 | 216.3 h | Margem 89.0%
  - Medium/equilibrado: Saldo $287,830 | Patrimônio $334,630 | 202.9 h | Margem 88.2%
  - Todos os valores exatamente idênticos aos anteriores.

## Não testado
- Renderização visual em tempo real no simulador MSFS 2024 / execução ao vivo da UI durante voo conectado.

## Dúvidas e decisões para o revisor
- Todas as decisões técnicas e regras de normalização foram mantidas estritamente de acordo com a especificação técnica do revisor. Os campos opcionais para a etapa 2 (`rules[]`, `hudSteps[]`, `variants[]`, `baseByTier{}`, `originProfiles{}`, `destinationProfiles{}`) foram mapeados em `MissionDefinition` para suportar expansões futuras sem retrabalho de esquema.
