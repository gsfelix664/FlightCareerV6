# NAV-IDA — Navegação VFR só de ida, por nível do piloto

**Branch:** feat/nav-ida · **Data:** 2026-11-27

## Plano (antes de codar)
- Estender `MissionProfileDefinition` com campos opcionais `MinPilotLevel`, `LegNm` e `RepeatPool`.
- Adicionar o contrato `navegacao_vfr_ida` e a variante `triangle_long` em `navegacao_vfr` no `mission_catalog.json` e seus modelos em `mission_texts.json`.
- Criar o serviço estático puro `VfrOneWayDestinations` para filtrar e ordenar deterministamente a lista de destinos iniciantes (pool de 3) e ponderar intermediários por características desafiadoras (torre, pista curta/não pavimentada, desnível de relevo).
- Integrar filtragem por nível do piloto em `MissionBoardCore.PickProfile`, repassando `pilotLevel` em `GenerateBoard`, `CreateSingleMission`, `GenerateMissionsInMemory` e na rotação do jogo em `MissionGenerator`.
- Implementar gerador `CreateVfrOneWayMissionSync` e ramificação de `triangle_long` em `CreateVfrTriangleMissionSync`.
- Adicionar opção `--level` em `BoardReporter` e chave de cache quádrupla no `CareerProgressionSimulator`.
- Criar suíte de testes unitários `VfrOneWayTests`, ajustar testes de contagem/listas e regenerar snapshot `missions_golden.json`.

## O que foi feito
- `MissionDefinition.cs`: Adicionados `MinPilotLevel`, `LegNm` e `RepeatPool` a `MissionProfileDefinition`.
- `VfrOneWayDestinations.cs`: Lógica de elegibilidade (raio, tier 2/3, pista mínima 1.800 ft), escada determinística de iniciante (`BeginnerPool`, 3 aeródromos tier 3 mais próximos com pista pavimentada ≥ 2.500 ft e sem torre, relaxando para pavimentada e depois qualquer) e cálculo de peso intermediário (`IntermediateWeight`, 1 + 2×n por torre, pista curta/não pavimentada e desnível ≥ 1.000 ft).
- `mission_catalog.json`: Inserido `navegacao_vfr_ida` (gerador `vfr_one_way`, tiers 2–3, base 10 no tier 3, variantes iniciante e intermediária) e variante `triangle_long` em `navegacao_vfr` (150–300 NM, pernas 40–130 NM, nível mínimo 3).
- `mission_texts.json`: Adicionados modelos textuais para `navegacao_vfr:triangle_long`, `navegacao_vfr_ida`, `navegacao_vfr_ida:iniciante` e `navegacao_vfr_ida:intermediaria`.
- `MissionBoardCore.cs`: Parâmetro `pilotLevel` propagado; `PickProfile` descarta perfis com `MinPilotLevel > pilotLevel`; despacho para `CreateVfrOneWayMissionSync` com `WithPersonLuggage`.
- `MissionGenerator.cs`: Implementado `CreateVfrOneWayMissionSync` (ponto de reporte a 3–8 NM, pouso no destino); adicionada variante `triangle_long` em `CreateVfrTriangleMissionSync`; leitura de XP/nível no jogo; propagação em `GenerateMissionsInMemory`.
- `CareerProgressionSimulator.cs`: Inclusão do nível do piloto na chave do `missionCache` e propagação para as chamadas de geração em memória.
- `BoardReporter.cs` / `Program.cs`: Adicionado argumento `--level <n>` com exibição no cabeçalho e ajuda de console.
- `VfrOneWayTests.cs`: 13 testes cobrindo descarte por nível, nível nulo, pool iniciante determinístico, relaxamento de escada, cálculo de pesos, geração de contratos, validação de quadro e variante triangular longa.
- `MissionCatalog69Tests.cs`, `MissionRunwayProfileTests.cs`, `VfrTriangleTests.cs`: Ajustados para 70 tipos no catálogo, permissão de pista zero para `navegacao_vfr_ida` e suporte ao quarto perfil `triangle_long`.
- `missions_golden.json`: Regenerado após confirmação de que apenas os sorteios variaram em decorrência da adição dos novos perfis.

## Critérios de aceite
- [x] 1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.314/1.314 testes aprovados).
- [x] 2. Snapshot `missions_golden.json` regenerado. Diff stat:
  ```
  src/FlightCareer.Tests/Missions/Golden/missions_golden.json | 2584 ++++++++++----------
  1 file changed, 1292 insertions(+), 1292 deletions(-)
  ```
- [x] 3. Linhas de `board-report` para SNZG com `--level 1` e `--level 3`:
  - **Level 1:**
    ```
    | `navegacao_vfr` | 2.7% |
    | `navegacao_vfr_ida` | 2.7% |
    | `navegacao_vfr` | 1/1.0/1 · (1) | 0/0/0 · (1) | 9/17/26 · (9) | 80/166/246 · (75) |
    | `navegacao_vfr_ida` | 1/1.0/1 · (1) | 0/0/0 · (1) | 9/18/26 · (9) | 57/60/62 · (3) |
    ```
    *(Nota: `navegacao_vfr_ida` concentrada estritamente nos 3 destinos próximos do pool; `navegacao_vfr` sem a variante longa de 150+ NM).*
  - **Level 3:**
    ```
    | `navegacao_vfr` | 2.8% |
    | `navegacao_vfr_ida` | 2.6% |
    | `navegacao_vfr` | 1/1.0/1 · (1) | 0/0/0 · (1) | 9/17/26 · (9) | 80/177/299 · (92) |
    | `navegacao_vfr_ida` | 1/1.0/1 · (1) | 0/0/0 · (1) | 9/18/26 · (9) | 57/82/145 · (37) |
    ```
    *(Nota: `navegacao_vfr_ida` expandida para 37 destinos com variantes intermediárias até 145 NM; `navegacao_vfr` alcançando 299 NM com a variante `triangle_long`).*
- [x] 4. Declaração de não testado: Não testado visualmente na interface WPF nem em voo real no simulador MSFS.
- [x] 5. Documentação atualizada em `docs/CHANGELOG.md`, `docs/sistemas/missoes-quadro.md` e `docs/plans/navegacao-instrucao.md`.
- [x] 6. Commit único preparado para `feat(missoes): navegação VFR só de ida por nível (NAV-IDA)`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1.314 aprovados, 0 falhas (13 testes novos em `VfrOneWayTests`)

## Não testado
- Renderização visual das pernas no mapa do briefing e HUD no aplicativo em execução.
- Voo e cumprimento de reporte visual no MSFS 2020/2024.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida em aberto; todos os pontos da especificação seguiram as decisões já alinhadas no plano `docs/plans/navegacao-instrucao.md`.
