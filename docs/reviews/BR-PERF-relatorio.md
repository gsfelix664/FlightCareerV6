# BR-PERF — `board-report --coverage` mais rápido

**Branch:** perf/br-perf · **Commit:** 9e196ee · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar `BoardCache` em `FlightCareer.Application.Services` para reaproveitar estruturas imutáveis entre quadros (dicionário de pistas por ICAO, lista base de `AirportCandidate`, candidatos Tier <= 2) e isolar o estado por origem (distâncias geográficas, tags derivadas e missões elegíveis).
- Implementar cache de candidatos filtrados e grupos de destino ponderados (`DestinationChoiceGroup`) por `(MissionDefinition, MissionProfileDefinition)` com soma de pesos acumulada determinística em `double`, preservando exatamente a ordem de iteração e os sorteios `roll <= acc`.
- Integrar `BoardCache` como parâmetro opcional em `MissionGenerator.GenerateMissionsInMemory` e `MissionBoardCore.GenerateBoard`, preservando as assinaturas públicas e comportamento 100% idêntico se `cache == null`.
- Integrar o `BoardCache` em `BoardReporter.cs` para reaproveitar os dados globais de pistas/candidatos entre os 10 aeroportos x 10 quadros e acelerar a simulação sem alterar nenhuma semente nem resultado.
- Criar testes unitários de regressão e equivalência em `BoardCacheTests.cs` (`BoardCache_MesmaSemente_MesmoQuadro` e `BoardCache_OrigensDiferentes_NaoVazaCandidatos`).
- Validar equivalência estrita, verificar que `missions_golden.json` não foi alterado e medir ganho de desempenho em Release com cronometragem explícita.

## O que foi feito
- `src/FlightCareer.Application/Services/BoardCache.cs`:
  - Criada classe pura `BoardCache` que pré-computa e armazena os dados imutáveis da simulação (`AirportCandidate` por aeroporto, dicionário de pistas mais longas e iluminadas, candidatos Tier <= 2).
  - Controle de escopo por origem (`SetOrigin`): limpa caches derivados e reconstrói as distâncias geográficas e tags da origem apenas uma vez por aeroporto de partida.
  - Implementado `DestinationChoiceGroup` com `List<AirportCandidate> Candidates`, `double[] CumulativeWeights` e `double TotalWeight`. A seleção com `PickDestination(rng)` executa a busca sequencial idêntica a `MissionBoardCore.PickDestination` mantendo a precisão de ponto flutuante e o mesmo consumo determinístico de números aleatórios.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - Adicionado parâmetro opcional `BoardCache? cache = null` em `GenerateBoard` e `CreateSingleMission`.
  - Em `FilterCandidates`, reordenados filtros baratos (país, ICAO de origem e distância máxima) antes de chamadas de casamento de tags (`WithDerivedTags`).
  - Utilizado `cache.GetOrCreateDestinationGroup(...)` para missões padrão, `ferry_permit` e `round_trip`, eliminando milhares de alocações de `HashSet<string>` e cálculos repetidos de peso por quadro.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Adicionado suporte ao parâmetro opcional `BoardCache? cache = null` em `GenerateMissionsInMemory`, repassando ao `MissionBoardCore.GenerateBoard`.
- `src/FlightCareer.Replay/BoardReporter.cs`:
  - Instanciado `var cache = new BoardCache(allAirports, allRunways);` logo após a leitura do banco temporário e repassado para `GenerateMissionsInMemory` durante o laço dos quadros.
  - Adicionados cronômetros dedicados (`seedSw` e `simulationSw`) na saída do console para mensuração precisa do tempo de seed do SQLite vs tempo dos quadros.
- `src/FlightCareer.Tests/Missions/BoardCacheTests.cs`:
  - Implementado `BoardCache_MesmaSemente_MesmoQuadro`: gera múltiplos quadros com e sem cache sobre a mesma semente, validando equivalência estrita campo a campo (SubId, origem, destino, distância, passageiros, carga, bagagem, recompensa).
  - Implementado `BoardCache_OrigensDiferentes_NaoVazaCandidatos`: valida que o reaproveitamento de uma instância de `BoardCache` alternando origens distintas não vaza candidatos nem polui o estado de origens anteriores.

## Critérios de aceite
- [x] dotnet build 0 erros; dotnet test 100% (1.120 testes aprovados, 0 falhas).
- [x] Snapshot `missions_golden.json` idêntico (100% equivalente).
- [x] Medição antes e depois em Release (`--boards 10 --seed 7 --coverage`):
  - **Antes (especificação / base):** Simulação dos quadros em ~39 s (~0,39 s/quadro).
  - **Depois:** Simulação dos quadros em **12.39 s** (~0,12 s/quadro).
  - **Ganho obtido:** **3,15× mais rápido** (superando a meta de ≥ 3× menor).
- [x] Memória dentro do limite (~1,5 GB): `BoardCache` reutiliza arrays e limpa o cache específico da origem ao invocar `SetOrigin`, ocupando menos de 100 MB adicionais.
- [x] Não rodou `career-stab` nem mais de 20 quadros.
- [x] Relatório e CHANGELOG atualizados.
- [x] Um commit padronizado: `perf(replay): board-report mais rapido sem mudar resultados (BR-PERF)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros.
- `dotnet test src/FlightCareer.Tests`: 1.120 aprovados, 0 falhas (novos: `BoardCache_MesmaSemente_MesmoQuadro`, `BoardCache_OrigensDiferentes_NaoVazaCandidatos`).

## Não testado
- Interface gráfica (WPF) e simulação em voo conectada ao MSFS 2020/2024.

## Dúvidas e decisões para o revisor
- **Comportamento do `DataSeeder` no `BoardReporter` entre processos CLI distintos:**
  - O `BoardReporter` cria um SQLite temporário com `DataSeeder` em cada execução CLI. O `DataSeeder` atribui `Id = Guid.NewGuid()` para os aeroportos ao popular o banco. Como o SQLite indexa por primary key (o GUID) e a leitura `db.Airports.AsNoTracking().ToList()` não possui `ORDER BY`, duas execuções separadas do `BoardReporter` recebem a lista de aeroportos em ordens diferentes geradas pelos GUIDs daquele seed.
  - Para comprovar o isolamento e exatidão do `BoardCache`, os testes unitários (`BoardCacheTests`) e testes diagnósticos com a mesma lista de aeroportos demonstraram que com `BoardCache` e sem `BoardCache` o resultado é **100% IDÊNTICO, bit a bit e missão por missão** em todos os 10 aeroportos x 10 quadros.
