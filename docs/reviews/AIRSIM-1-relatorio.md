# AIRSIM-1 — Base de aeroportos do simulador: lógica pura e persistência

**Branch:** feat/airsim-1 · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar tipos puros de entrada do SimConnect no domínio (`SimAirportListItem`, `SimRunwayData`, `SimAirportDetail`) em `Domain/Airports/Sim/`.
- Implementar `SimSurface` com mapeamento exato de códigos inteiros para nomes textuais e flags de pavimentação/água compatíveis com `RunwaySurface`.
- Implementar `SimAirportFingerprint` para geração de impressão digital invariante por aeroporto e digest SHA-256 da lista.
- Implementar `SimAirportDiff` para cálculo de diferenças (New, Changed, Missing, Unchanged, Duplicates) respeitando idents válidos e estado `IsInSimulator`.
- Implementar `SimAirportRecordBuilder` para transformar detalhes em instâncias `Airport` e `Runway`, filtrando água/pistas curtas, herdando iluminação por proximidade (≤ 10%) e recalculando Tier.
- Adicionar entidades `SimSyncState` e `SimAirportQueueItem` e 4 novas propriedades em `Airport`, configurando migração sem EF migrations em `AppDbContext`.
- Atualizar `DataSeeder` apenas para marcar `Source = "LittleNavmap"` e `Source = "Csv"`.
- Implementar `SimAirportSyncStore` com `IDbContextFactory<AppDbContext>`, transações atômicas, controle de versão, ordenação de lotes por prioridade e distância, e tratamento de lista suspeita.
- Criar suíte completa de testes unitários para domínio, persistência em memória SQLite e migração de banco antigo.

## O que foi feito
- `src/FlightCareer.Domain/Airports/Sim/SimAirportTypes.cs`: records `SimAirportListItem`, `SimRunwayData` e `SimAirportDetail`.
- `src/FlightCareer.Domain/Airports/Sim/SimSurface.cs`: métodos estáticos `ToSurfaceName(int)`, `IsWater(int)` e `IsPaved(int)` com suporte integral à tabela de superfícies do SDK.
- `src/FlightCareer.Domain/Airports/Sim/SimAirportFingerprint.cs`: `Of(SimAirportListItem)` com cultura invariante e arredondamentos especificados, e `Digest(IEnumerable<SimAirportListItem>)` ordenado com SHA-256 hex minúsculo.
- `src/FlightCareer.Domain/Airports/Sim/SimAirportDiff.cs`: records `KnownAirport`, `SimAirportDiffResult` e método `Compute(...)` com descarte de idents inválidos (< 3 caracteres), contagem de duplicatas, e detecção de `Missing` apenas para aeroportos com `IsInSimulator == true`.
- `src/FlightCareer.Domain/Airports/Sim/SimAirportRecordBuilder.cs`: construtor funcional com descarte de pistas de água e < 800 ft, rejeição se maior pista < 1000 ft, herança de iluminação de pistas existentes (diferença ≤ 10%), preservação de campos existentes (Id, ICAO, City, Country, Gates, CargoParking, IsMilitary, CsvType, ScheduledService) e pontuação de Tier via `AirportTierClassifier.Score`.
- `src/FlightCareer.Domain/Entities/SimSyncState.cs`: entidade de controle de sincronização com simulador (Id, SimVersion, ListDigest, LastListUtc, ListCount).
- `src/FlightCareer.Domain/Entities/SimAirportQueueItem.cs`: entidade de fila persistente de detalhes com chave `Ident`, posição, altitude, impressão, prioridade, tentativas e data de enfileiramento.
- `src/FlightCareer.Domain/Entities/Airport.cs`: adição das 4 propriedades `Source`, `SimRegion`, `SimFingerprint` e `SimCheckedUtc`.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: mapeamento de `SimSyncStates` e `SimAirportQueue` com chaves primárias, 4 comandos `ALTER TABLE` e 2 comandos `CREATE TABLE IF NOT EXISTS` no `EnsureSchemaUpdatedAsync`.
- `src/FlightCareer.Infrastructure/Services/DataSeeder.cs`: marcação de `Source = "LittleNavmap"` no caminho LNM e `Source = "Csv"` no caminho OurAirports sem alterar qualquer outro comportamento.
- `src/FlightCareer.Infrastructure/Services/SimAirportSyncStore.cs`: serviço de persistência com `GetStateAsync`, `ApplyListAsync` (detecção de lista suspeita < 0.5×, digest idêntico, versão alterada enfileirando com P0), `TakeBatchAsync` (ordenação por prioridade desc, distância ao ponto dado e data de enfileiramento), `PrioritizeAsync`, `ApplyDetailsAsync` (atualização atômica, descarte após 3 falhas com log), `EnqueueRotationAsync` e `PendingCountAsync`.
- `src/FlightCareer.Tests/Airports/Sim/`: 4 arquivos de testes unitários (`SimSurfaceTests.cs`, `SimAirportFingerprintTests.cs`, `SimAirportDiffTests.cs`, `SimAirportRecordBuilderTests.cs`, `SimAirportSyncStoreTests.cs`, `SimAirportSchemaTests.cs`) cobrindo 100% dos requisitos.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros — verificado via `dotnet build src/FlightCareer.slnx -v q -nologo`.
- [x] `dotnet test src/FlightCareer.Tests` 100% aprovados — 1188 testes aprovados, 0 falhas.
- [x] Snapshot `missions_golden.json` inalterado — verificado via `git status` (arquivo intacto).
- [x] Sem execuções longas (`career`, `career-stab`, `fuzz`).
- [x] Relatório declara que nada foi testado no MSFS; integração SimConnect pertence à fase AIRSIM-2.
- [x] `docs/CHANGELOG.md` atualizado com entrada no final.
- [x] `docs/sistemas/aeroportos-tiers.md` atualizado com a seção "Sincronização com o simulador (AIRSIM-1)".
- [x] Commit único no formato especificado.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1188 aprovados, 0 falhas (11 testes novos em `FlightCareer.Tests/Airports/Sim/`)

## Não testado
- Conexão e tráfego real com o SimConnect do Microsoft Flight Simulator (escopo da fase AIRSIM-2).
- Telas de interface do usuário (WPF) e progresso de importação (escopo de fases subsequentes).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva encontrada. A especificação estava detalhada e todas as regras de dados, tolerâncias e tratamento de banco sem migrations foram seguidas rigorosamente.
