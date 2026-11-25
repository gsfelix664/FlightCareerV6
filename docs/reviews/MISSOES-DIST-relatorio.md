# MISSOES-DIST — Distâncias de check_flight, paraquedismo e pinga_pinga respeitam o perfil

**Branch:** fix/missoes-distancias · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
1. Atualizar assinaturas e implementações em `MissionGenerator.cs`:
   - `CreateSkydiveMissionSync`: adicionar `double distanceNm` obrigatório após `referenceTime`; remover `estimatedDist = 25.0` e usar `distanceNm` na missão e no texto.
   - `CreateCheckFlightMissionSync`: adicionar `double distanceNm` obrigatório após `referenceTime`; remover `dist = 30.0` e usar `distanceNm` na missão e no texto.
   - `CreateMultiStopMissionSync`: adicionar `MissionProfileDefinition? profile = null` no fim; extrair constantes `MinLegDistanceNm = 25.0`, `MaxLegDistanceNm = 120.0`, `MaxLegFallbackDistanceNm = 160.0`; aplicar filtro de teto `currentDist + d + reserva <= maxTotal` (com reserva `25 * Math.Max(0, 3 - (step + 1))`); retornar `null` se `Math.Round(totalDist, 0) < minTotal`.
2. Em `MissionBoardCore.CreateSingleMission`:
   - Repassar o `distanceNm` já sorteado para `CreateSkydiveMissionSync` e `CreateCheckFlightMissionSync`.
   - Repassar o `profile` sorteado para `CreateMultiStopMissionSync`.
3. Atualizar chamadas existentes nos testes (`MissionGeneratorLocalTests.cs`, `Phase2ContractTests.cs`, `PhaseE2C1GeneratorsTests.cs`).
4. Adicionar os 6 novos testes em `PhaseE2C1GeneratorsTests.cs` cobrindo os cenários descritos na especificação.
5. Executar `dotnet build` e `dotnet test` com saída filtrada.
6. Atualizar snapshot `missions_golden.json`, apurando a contagem de registros alterados por subtipo.
7. Atualizar `docs/sistemas/missoes-quadro.md` e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - `CreateSkydiveMissionSync`: adicionado `double distanceNm` obrigatório após `referenceTime`. Substituído `estimatedDist = 25.0` por `distanceNm` tanto nos valores textuais (`distance_nm`) quanto na chamada a `CreateMissionObjectSync`.
  - `CreateCheckFlightMissionSync`: adicionado `double distanceNm` obrigatório após `referenceTime`. Substituído `dist = 30.0` por `distanceNm` nos valores textuais e em `CreateMissionObjectSync`.
  - `CreateMultiStopMissionSync`: adicionado parâmetro opcional `MissionProfileDefinition? profile = null`. Extraídas constantes de perna (`MinLegDistanceNm = 25.0`, `MaxLegDistanceNm = 120.0`, `MaxLegFallbackDistanceNm = 160.0`). Limites `minTotal` e `maxTotal` extraídos de `profile.RangeNm` (fallback `[100, 400]`). Filtro de candidatos incorpora `currentDist + d + reserva <= maxTotal` onde `reserva = 25.0 * Math.Max(0, 3 - (step + 1))`. Retorna `null` se `Math.Round(totalDist, 0) < minTotal`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - `CreateSingleMission`: repassa `distanceNm` para `CreateSkydiveMissionSync` e `CreateCheckFlightMissionSync`. Repassa `profile` para `CreateMultiStopMissionSync`.
- `src/FlightCareer.Tests/Missions/MissionGeneratorLocalTests.cs` e `src/FlightCareer.Tests/Missions/Phase2ContractTests.cs`:
  - Chamadas atualizadas com parâmetro `distanceNm`.
- `src/FlightCareer.Tests/Missions/PhaseE2C1GeneratorsTests.cs`:
  - Chamadas existentes atualizadas com `distanceNm`.
  - Adicionados 6 testes novos:
    - `CheckFlight_Gerador_UsaDistanciaRecebida`: valida que distância 45.0 é repassada para a missão.
    - `Paraquedismo_Gerador_UsaDistanciaRecebida`: valida que distância 12.0 é repassada para a missão.
    - `CreateSingleMission_CheckFlightEParaquedismo_DistanciaDentroDoPerfil`: valida que múltiplas sementes geram missões com distância dentro das faixas [20, 60] e [5, 15] do catálogo, gerando múltiplos valores distintos em check_flight.
    - `PingaPinga_Gerador_TotalDentroDoPerfil`: com aeroportos em linha permitindo somas > 400 NM, valida sementes 1..200 gerando distâncias sempre entre 100 e 400 NM com ao menos 2 paradas intermediárias.
    - `PingaPinga_Gerador_RotaCurtaDemais_RetornaNulo`: com perfil [300, 400] e candidatos curtos que não atingem 300 NM, retorna `null`.
    - `PingaPinga_Gerador_TetoApertado_RetornaNulo`: com perfil [10, 60] (menor que 3 * 25 NM), retorna `null`.
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`:
  - Snapshot regravado com o novo comportamento.
- `docs/sistemas/missoes-quadro.md`:
  - Registro da regra de distâncias de perfis em geradores especiais.
- `docs/CHANGELOG.md`:
  - Entrada adicionada no fim do histórico.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros (81 avisos pré-existentes, 0 novos)
- [x] `dotnet test src/FlightCareer.Tests` passa 100% (1087 aprovados, 0 falhas)
- [x] Snapshot atualizado e analisado:
  - Registros alterados por subtipo:
    - `check_flight`: 6 registros (distância e recompensa proporcionais ao perfil sorteado)
    - `paraquedismo`: 5 registros (distância e recompensa proporcionais ao perfil sorteado)
    - `pinga_pinga`: 0 registros alterados nas sementes fixas de teste
    - Outros subtipos: 0 alterações
- [x] Não testado no MSFS declarado
- [x] CHANGELOG e docs/sistemas/missoes-quadro.md atualizados
- [x] 1 commit conforme convenção `fix(missions): distâncias de cheque, paraquedismo e pinga-pinga respeitam o perfil (MISSOES-DIST)`

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 81 avisos
- `dotnet test src/FlightCareer.Tests`: 1087 aprovados, 0 falhas (13 aprovados em PhaseE2C1GeneratorsTests, incluindo 6 novos)

## Não testado
- Exibição visual e geração de contratos em voo direto no simulador MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Implementação seguiu estritamente o contrato da especificação.
