# ROTAS-REAIS — Linha aérea prefere rotas reais (OpenFlights)

**Branch:** feat/rotas-reais · **Commit:** 3e5ce78 · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar script `tools/places/build_routes.py` para processar `routes.dat` (OpenFlights) e `airports.csv` (OurAirports), gerando `airline_routes.json` compacto em `Infrastructure/Data/places/`.
- Executar `build_routes.py` e conferir estatísticas de descarte, contagem de pares únicos e tamanho do arquivo (limite <= 1 MB).
- Implementar `AirlineRouteCatalog.cs` em `Domain/Missions/` com singleton lazy `Default`, carregamento de `airline_routes.json` com fallback para `Empty`, contagem de pares simétricos e métodos `Airlines`/`HasRoute`.
- Adicionar `RealRouteMultiplier` em `MissionDestinationRule` (`MissionDefinition.cs`) e implementar `MissionWeighting.RealRouteMultiplier`.
- Estender `DestinationChoiceGroup` com multiplicador extra e atualizar `BoardCache.cs`, `MissionBoardCore.cs` e `MissionGenerator.cs` com repasse de rotas e injeção do marcador `{route_note}`.
- Calibrar multiplicadores de destino em `mission_catalog.json` (6 para ponte/troncal/regional intl/longo curso; 4 para alimentadores/sub-regional/regional essencial) e adicionar `{route_note}` em `mission_texts.json`.
- Atualizar `ATTRIBUTION.md`, `missoes-quadro.md` e `CHANGELOG.md`.
- Criar testes unitários em `AirlineRouteCatalogTests.cs`, regenerar `missions_golden.json` e conferir que subtipos fora da lista permaneceram 100 % idênticos.

## O que foi feito
- `tools/places/build_routes.py`: criado script com download automático ou reaproveitamento de cache, mapeamento robusto IATA (3 letras com desempate por tipo de aeroporto large > medium > small) e ICAO (4 letras validados contra aeroportos conhecidos), descarte de codeshares/escalas/origem=destino e serialização compacta de rotas simétricas em `Infrastructure/Data/places/airline_routes.json`.
- `src/FlightCareer.Infrastructure/Data/places/airline_routes.json`: gerado arquivo com 17.223 pares comerciais únicos (330 KB).
- `src/FlightCareer.Domain/Missions/AirlineRouteCatalog.cs`: implementado catálogo imutável em memória com `PairCount` único simétrico, busca insensível a maiúsculas e minúsculas, logging com `Debug.WriteLine` e fallback silencioso para catálogo vazio.
- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`: adicionada propriedade `RealRouteMultiplier` (padrão 0.0) em `MissionDestinationRule`.
- `src/FlightCareer.Domain/Missions/MissionWeighting.cs`: adicionado método `RealRouteMultiplier(MissionDefinition m, AirlineRouteCatalog? routes, string originIcao, string destIcao)` retornando o multiplicador configurado se > 1 e rota real confirmada, ou 1.0 caso contrário.
- `src/FlightCareer.Application/Services/BoardCache.cs`: `DestinationChoiceGroup` estendido com delegate opcional `extraMultiplier` aplicado fora do teto `MaxMultiplier`; `GetOrComputeDestinationGroup` atualizado para integrar `AirlineRouteCatalog`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: repasse opcional de `AirlineRouteCatalog` em `GenerateBoard`, `CreateSingleMission`, `SelectDestination` e `PickDestination`. No caminho padrão de criação de missão, avaliação da flag `isRealRoute = RealRouteMultiplier(...) > 1.0` repassada para `CreateMissionObjectSync`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: parâmetro opcional `bool realRoute = false` adicionado em `CreateMissionObjectSync`, alimentando o marcador `route_note` com `" Rota regular com demanda consolidada."` ou `""`.
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`: adicionado `realRouteMultiplier` = 6 (`ponte_aerea`, `troncal_domestico`, `regional_internacional`, `longo_curso`) e `realRouteMultiplier` = 4 (`feeder_inbound`, `feeder_outbound`, `sub_regional`, `regional_essencial`).
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: marcador `{route_note}` adicionado após o ponto final das descrições dos 8 subtipos de linha aérea.
- `src/FlightCareer.Infrastructure/Data/places/ATTRIBUTION.md`: adicionada linha na tabela para `airline_routes.json` e declaração de licença ODbL do OpenFlights.
- `docs/sistemas/missoes-quadro.md`: documentada a preferência por rotas reais de linha aérea.
- `docs/CHANGELOG.md`: adicionada entrada da tarefa ROTAS-REAIS.
- `src/FlightCareer.Tests/Missions/AirlineRouteCatalogTests.cs`: 11 testes unitários cobrindo simetria de pares, JSON inválido, multiplicadores, equivalência sem multiplicador extra, medição estatística de preferência (5.000 sorteios), dados reais e renderização de notas textuais.
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`: snapshot regenerado e auditado via script no scratchpad.

## Critérios de aceite
- [x] 1. `dotnet build src/FlightCareer.slnx -v q -nologo` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.165/1.165 testes aprovados).
- [x] 2. `missions_golden.json`: conferido no scratchpad que todos os registros fora dos 8 subtipos ficaram 100 % idênticos (0 divergências).
  - Registros mudados nos 8 subtipos:
    - `feeder_inbound`: 1 mudado, 0 com rota real no novo destino (origem SBDN: rota real para SBKP/SBSP; candidatos sofreram reponderação e destino sorteado foi SBCF).
    - `feeder_outbound`: 2 mudados, 2 com rota real no novo destino.
    - `longo_curso`: 2 mudados, 2 com rota real no novo destino.
    - `ponte_aerea`: 0 mudados (destinos mantiveram-se consistentes).
    - `regional_essencial`: 1 mudado, 1 com rota real no novo destino.
    - `regional_internacional`: 0 mudados.
    - `sub_regional`: 3 mudados, 3 com rota real no novo destino.
    - `troncal_domestico`: 0 mudados.
  - Total de registros alterados: 9 (sendo 8 com destino em rota real consolidada).
- [x] 3. `python tools/places/build_routes.py` executado sem `--lnm` (base Little Navmap não existente na máquina).
  - Saída do script:
    - Linhas lidas: 67663
    - Descartadas por codeshare: 14597
    - Descartadas por escalas: 11
    - Descartadas por mesma origem/destino: 1
    - Descartadas por código não mapeado: 1283
    - Pares únicos: 17223
    - Tamanho do arquivo: 330 KB (<= 1 MB)
- [x] 4. `places/ATTRIBUTION.md` e cabeçalho de `build_routes.py` atualizados com aviso ODbL e OpenFlights.
- [x] 5. `docs/sistemas/missoes-quadro.md` e `docs/CHANGELOG.md` atualizados.
- [x] 6. Declarado o que não foi testado no simulador (seção "Não testado").
- [x] 7. Commit único na branch `feat/rotas-reais`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1.165 aprovados, 0 falhas (11 testes novos em `AirlineRouteCatalogTests`):
  - `LoadFromJson_ParesNosDoisSentidos_HasRouteSimetrico`
  - `LoadFromJson_JsonInvalido_CatalogoVazio`
  - `RealRouteMultiplier_SemValorNoCatalogo_Retorna1`
  - `RealRouteMultiplier_RotaReal_RetornaValor`
  - `RealRouteMultiplier_SemRota_Retorna1`
  - `RealRouteMultiplier_CatalogoNulo_Retorna1`
  - `DestinationChoiceGroup_SemMultiplicadorExtra_PesosIguaisAoAtual`
  - `Sorteio_RotaRealMultiplicador6_PreferenciaMedida`
  - `DadosReais_AirlineRoutesJson_CarregaEContemRotasConhecidas`
  - `CreateMissionObjectSync_RotaReal_DescricaoComNota`
  - `CreateMissionObjectSync_SemRota_SemNota`

## Não testado
- Exibição visual de contratos de linha aérea no quadro e verificação em voo real no simulador MSFS 2020/2024.

## Dúvidas e decisões para o revisor
- Nenhuma. Todas as diretrizes da especificação foram cumpridas sem ambiguidades.
