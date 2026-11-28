# TIER-A2 — país certo, linha aérea entre aeroportos de linha e recálculo do tier

**Branch:** feat/tier-movimento · **Commit:** 846640b · **Data:** 2026-11-24

## Plano (antes de codar)
- Item 1 (País): no `DataSeeder.EnrichFromCsvAsync` e no `TierReporter.cs`, sempre que a posição bater com o CSV (`fc_same_place` / `IsSameAirportPosition`), atribuir `Country = iso_country` do CSV.
- Item 2 (Linha aérea entre aeroportos de linha): adicionar `RequireAnyTag` em `MissionDefinition`, `MissionDestinationRule` e `MissionProfileDefinition`. Filtrar peso 0 na origem em `MissionWeighting`/`GenerateBoard`, filtrar perfis compatíveis na origem em `GenerateBoard`/`PickProfile`, e filtrar destinos candidatos em `FilterCandidates`. Atualizar `mission_catalog.json` conforme especificado.
- Item 3 (Recálculo do tier): adicionar colunas `CsvType`, `ScheduledService` e `TierRulesVersion` em `Airport` e `AppDbContext`. No início de `DataSeeder.SeedAsync`, verificar se há aeroportos com `TierRulesVersion != rules.Version` e recalcular todos em lote numa transação com log `Debug.WriteLine`. Atualizar seeds para gravar versão 2.
- Validação: criar testes unitários para os 3 itens, regravar snapshot `missions_golden.json`, rodar `tier-report` e `board-report` apontando para diretório temporário fora do repositório, atualizar `docs/CHANGELOG.md` e `AGENTS.md`.

## O que foi feito
- `src/FlightCareer.Domain/Entities/Airport.cs` e `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - Adicionadas propriedades e migrações idempotentes para `CsvType` (TEXT), `ScheduledService` (INTEGER DEFAULT 0) e `TierRulesVersion` (INTEGER DEFAULT 0).
- `src/FlightCareer.Infrastructure/Services/DataSeeder.cs`:
  - `EnrichFromCsvAsync`: sempre que a posição confere até 10 NM (`fc_same_place`), `Country` recebe `iso_country` do CSV incondicionalmente, além de persistir `CsvType`, `ScheduledService` e `TierRulesVersion = rules.Version`.
  - `SeedFromLittleNavmapAsync` e `SeedAirportsAsync`: persistência de `CsvType`, `ScheduledService` e `TierRulesVersion = rules.Version`.
  - `EnsureTiersUpToDateAsync`: novo método chamado no início de `SeedAsync` (antes de qualquer `return`). Se houver qualquer aeroporto com `TierRulesVersion != AirportTierRules.Default.Version`, recalcula todos a partir dos sinais estruturais gravados e etiquetas curadas do catálogo em uma transação SQLite em lote, emitindo `Debug.WriteLine`.
- `src/FlightCareer.Replay/TierReporter.cs`:
  - `CsvItem` atualizado com `IsoCountry`; quando `IsSameAirportPosition` é verdadeiro, substitui o país de saída pelo `IsoCountry` do CSV.
- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`, `MissionWeighting.cs` e `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - Adicionado `RequireAnyTag` em `MissionDefinition`, `MissionDestinationRule` e `MissionProfileDefinition`.
  - `MissionWeighting.OriginWeight`: missões com `RequireAnyTag` na origem recebem peso 0 caso a origem não possua ao menos uma etiqueta compatível.
  - `MissionBoardCore.GenerateBoard` e `PickProfile`: perfis com `RequireAnyTag` só são elegíveis/sorteados se a origem possuir uma das etiquetas exigidas. Se nenhum perfil couber na origem, a missão é excluída.
  - `MissionBoardCore.FilterCandidates`: destinos candidatos são descartados caso não possuam as etiquetas exigidas por `destination.RequireAnyTag` ou pelo perfil sorteado (`profile.RequireAnyTag`).
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`:
  - `ponte_aerea`, `troncal_domestico`, `regional_internacional`, `longo_curso`: `requireAnyTag: ["linha", "linha_auto"]` na origem e no destino.
  - `feeder_inbound`: `requireAnyTag: ["linha", "linha_auto"]` no destino.
  - `feeder_outbound`: `requireAnyTag: ["linha", "linha_auto"]` na origem.
  - Perfis $\ge 4.300$ ft de `carga_paletizada`, `carga_overnight`, `ecommerce` e `cadeia_fria`: `requireAnyTag: ["linha", "linha_auto", "carga:cargueiros", "carga_auto"]`.
- `src/FlightCareer.Tests/`:
  - `AirportTierRecalculationAndCountryTests.cs`: testes dedicados de país pelo CSV (ZSPD chinês virando CN na mesma posição e SBOK a > 10 NM preservando Amontada) e recálculo de Tier em SQLite em memória (KJFK versão 1 virando versão 2 com Tier 1).
  - `MissionRunwayProfileTests.cs`: teste `MissionBoardCore_LinhaAerea_ExigeEtiquetaLinhaNaOrigemEDestino` conferindo que SBMT gera 0% de ponte aérea / troncal em 2.000 sorteios, destino sem linha nunca é escolhido e `sub_regional` continua saindo de aeroporto sem etiqueta.
  - `MissionCatalogEquivalenceTests.cs`: snapshot `missions_golden.json` regravado e validado.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação atualizada conforme as convenções do repositório.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` passa. — Verificado com 1.004/1.004 testes aprovados.
- [x] `dotnet run --project src/FlightCareer.Replay -- tier-report --out <pasta fora do repositório>` roda; a coluna País mostra códigos ISO (BR, CN, RU…). — Verificado em `$env:TEMP\fc_tier_test` (ZSPD → CN, UWUU/UWUK → RU, etc.).
- [x] `dotnet run --project src/FlightCareer.Replay -- board-report --boards 50 --seed 7 --airports SBMT,SBRJ,SBGR,PABE --out <pasta fora do repositório>`: no relatório, % de `ponte_aerea` e `troncal_domestico` em cada um. — Verificado em `$env:TEMP\fc_board_test`: SBMT tem 0.0% de `ponte_aerea` e 0.0% de `troncal_domestico`; SBRJ tem 27.4% e 13.5%; SBGR tem 2.3% e 6.7%; PABE tem 2.0% e 1.9%.
- [x] Nada de saída de relatório dentro do repositório; nenhum `catch {}` silencioso. — Todas as saídas apontadas para pastas temporárias fora do repositório; nenhum `catch {}` vazio adicionado.
- [x] Commit: `fix(missoes): pais pelo csv, linha aerea entre aeroportos de linha e recalculo do tier (TIER-A rodada 2)`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 1.004 aprovados, 0 falhas (novos: `Pais_CsvMesmaPosicao_SubstituiPais_EPosicaoDivergenteNaoSubstitui`, `RecalculoTier_VersaoRegrasMudou_RecalculaTodos`, `MissionBoardCore_LinhaAerea_ExigeEtiquetaLinhaNaOrigemEDestino`).

## Não testado
- Resultados dependentes de Little Navmap local ou MSFS na tela do usuário.

## Dúvidas e decisões para o revisor
- Nenhuma. Tudo implementado estritamente conforme a especificação TIER-A (rodada 2).
