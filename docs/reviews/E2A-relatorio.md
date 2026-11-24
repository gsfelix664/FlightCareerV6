# E2A — Catálogo de 69 tipos, pesos por perfil de aeroporto e gerador unificado

**Branch:** feat/catalogo-69-tipos · **Commit:** <pendente> · **Data:** 2026-11-24

## Plano (antes de codar)
1. **Modelos de dados e catálogos:**
   - Atualizar `MissionDefinition.cs` para a versão 2 com `baseByTier`, `originProfiles`, `destination` (`DestinationDefinition`), `profiles` (`List<MissionProfileDefinition>`), `luggagePerPersonKg`, `crew` (`MissionCrewDefinition`), `civilAtMilitaryBase`, `legacyIds`, `enabled`.
   - Criar `AirportProfileCatalog.cs` em `Domain/Missions/` com carregamento de `airport_profiles.json`, cache de tags, casamento de tags (com e sem prefixo `:`) e construtor para injeção em testes.
   - Criar `MissionWeighting.cs` puro em `Domain/Missions/` com `OriginWeight` (base do tier × produto dos multiplicadores que casam limitados a ×12, base militar ×0,2 exceto missões com `civilAtMilitaryBase: false`, 0 se desabilitada) e `DestinationWeight` (mínimo 1.0, limitado a ×12).
   - Atualizar `MissionCatalog.cs` para carregar `mission_catalog.json` v2 com os 69 tipos e indexar `Missions` e `LegacyCodes`.
2. **Dados JSON:**
   - Criar `mission_catalog.json` com os 69 tipos especificados no § 6 e tabelas § 9.2, § 9.3, § 9.6 e § 9.7.1, com os 17 tipos desabilitados e `bigAirport` nas bandas pesadas.
   - Atualizar `mission_texts.json` re-chaveando para os novos IDs, fundindo todas as variantes dos IDs legados mapeados e criando variantes simples com marcadores para os tipos novos.
   - Atualizar `AppDbContext.EnsureSchemaUpdatedAsync` removendo a migração de `SubId`s legados.
3. **Núcleo de geração unificado:**
   - Criar `MissionBoardCore.cs` puro em `Application/Services/`: sorteio unificado em um nível por peso de origem, seleção correlacionada de perfil (ponderação `bigAirport`), sorteio de distância/pax/carga, bagagem individual por passageiro e crew, seleção ponderada de destinos válidos (nunca exclusivos) por `DestinationWeight` e suporte aos geradores especiais existentes.
   - Atualizar `MissionGenerator.cs`: tanto `GenerateMissionsAsync` quanto `GenerateMissionsInMemory` delegam a geração de quadro ao `MissionBoardCore`. O caminho do jogo substitui a consulta limitada por latitude e ordem alfabética por busca em caixa delimitadora de raio com amostragem aleatória se exceder 3.000 candidatos.
4. **Relatório CLI (`board-report`):**
   - Implementar `board-report` no `FlightCareer.Replay` com suporte a `--boards`, `--seed`, `--airports`, `--coverage` e `--out`, gerando `board-report.md` e CSV com as 4 tabelas solicitadas.
   - Executar a fazenda do MT selecionada (`SBOK`).
5. **Testes e validação:**
   - Escrever os testes requeridos em `src/FlightCareer.Tests/Missions/` cobrindo catálogo, casamento de tags, pesos de origem/destino, destino não exclusivo, correlação de perfil, bagagem por pessoa, paridade jogo vs bots, concentração de tier 1 e snapshot atualizado.
   - Executar `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests`.
   - Executar `board-report` e `career-stab`.
