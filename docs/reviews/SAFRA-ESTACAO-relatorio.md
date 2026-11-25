# SAFRA-ESTACAO — Safra por estação do ano nas missões agro

**Branch:** feat/safra-estacao · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
- Criar `Domain/Missions/AgroSeason.cs` com cálculo puro de mês equivalente austral, fases da safra (Plantio, Colheita, Entressafra), tags `safra:<fase>` e `safra:mes:<m>`, mesclagem de tags em `WithSeasonTags` e nota descritiva `Note`.
- Adicionar `Season` em `MissionDefinition` e aplicar seus multiplicadores em `MissionWeighting.OriginWeight` após o teto de 12 e filtro militar.
- Integrar `seasonDate` em `MissionBoardCore.GenerateBoard`, `MissionGenerator.GenerateMissionsAsync` (lendo `CareerClockUtc` do piloto), `GenerateMissionsInMemory` e `CareerProgressionSimulator`, além de injetar marcador `{safra}` no construtor de missões.
- Atualizar catálogos: `"season"` em `mission_catalog.json`, tags de safra e novo tipo `revisao_entressafra` em `cargo_kinds.json`, e `{safra}` em `mission_texts.json`.
- Implementar suíte de testes unitários `AgroSeasonTests.cs` cobrindo cálculo de safra, pesos, seleção de carga no catálogo real e reflexos no quadro gerado.

## O que foi feito
- `src/FlightCareer.Domain/Missions/AgroSeason.cs`: lógica pura com `SafraPhase` (Plantio, Colheita, Entressafra), cálculo de mês equivalente para hemisfério norte/sul (`SouthernEquivalentMonth`), fases por mês (`PhaseFor`), geração de tags dinâmicas `safra:<fase>` e `safra:mes:<m>` (`TagsFor`), enriquecimento de tags preservando referência se nulo (`WithSeasonTags`) e nota de texto descritivo (`Note`).
- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`: adicionado dicionário `Season` desserializado do catálogo de missões.
- `src/FlightCareer.Domain/Missions/MissionWeighting.cs`: aplicado multiplicadores de `Season` após o teto de 12 e o fator de base militar.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: adicionado parâmetro opcional `seasonDate`, mesclando tags via `AgroSeason.WithSeasonTags` e ignorando cache se tags dinâmicas forem adicionadas.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: `GenerateMissionsAsync` lê `CareerClockUtc` do piloto e repassa como `seasonDate`; `GenerateMissionsInMemory` aceita e repassa `seasonDate`; construtor comum de missões preenche marcador `{safra}`.
- `src/FlightCareer.Application/Simulation/CareerProgressionSimulator.cs`: repassa `seasonDate: simulatedDate` nas chamadas de geração em memória.
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`: adicionado bloco `"season"` para `insumos_agro`, `agro_executivo`, `translado_piloto_agricola` e `pulverizacao`.
- `src/FlightCareer.Infrastructure/Data/cargo_kinds.json`: tags de safra adicionadas às cargas de `insumos_agro` e novo tipo `revisao_entressafra` cadastrado.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: marcador `{safra}` adicionado colado ao ponto final das 3 variantes de `insumos_agro`.
- `src/FlightCareer.Tests/Missions/CargoKindCatalogTests.cs`: atualizado `LoadDefault_EtiquetasExistemNosPerfis` para reconhecer etiquetas dinâmicas com prefixo `safra:`.
- `src/FlightCareer.Tests/Missions/AgroSeasonTests.cs`: 11 testes unitários novos cobrindo regras puras, catálogo real de cargas e geração de quadros.
- `docs/plans/missoes-locais-rp.md`, `docs/sistemas/missoes-quadro.md` e `docs/CHANGELOG.md`: documentação e estado do sistema atualizados.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros (0 erros, 81 avisos pré-existentes).
- [x] `dotnet test src/FlightCareer.Tests` passa 100% (1.154 aprovados, 0 falhas).
- [x] Snapshot `missions_golden.json` idêntico (verificado com `MissionCatalogEquivalenceTests` aprovado sem alteração no snapshot).
- [x] Não rodou `career-stab` nem `career`.
- [x] Relatório gerado e documentação atualizada (`CHANGELOG.md`, `missoes-quadro.md`, `missoes-locais-rp.md`).
- [x] Commit único no formato especificado.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 81 avisos (avisos legados de CS0618/CS8602 pré-existentes na solução).
- `dotnet test src/FlightCareer.Tests`: 1.154 aprovados, 0 falhas (11 testes novos em `AgroSeasonTests`).

## Não testado
- Quadro de missões ao vivo no cliente WPF com o MSFS 2024/2020 em execução (apenas testes automatizados cobrindo geração com datas simuladas e banco em memória).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva; todas as especificações do contrato de safra foram implementadas e verificadas.
