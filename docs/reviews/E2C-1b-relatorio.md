# E2C-1b — Variantes da navegação VFR (ida e volta; triângulo com toques e arremetidas)

**Branch:** feat/e2c-1b-vfr-variantes · **Commit:** pendente (neste commit) · **Data:** 2026-11-24

## Plano (antes de codar)
- Estender `MissionProfileDefinition` em `MissionDefinition.cs` adicionando a propriedade opcional `Variant`.
- Estender `MultiStopContractData` em `ContractData.cs` com a propriedade `StopMode` (`"full_stop"` por padrão, `"touch_and_go"`).
- Reutilizar a definição de TGL de `TouchAndGoCounter` (extraindo método/constantes de validação) e implementar suporte a `StopMode = "touch_and_go"` no `MultiStopTracker` (mantendo aceitação de parada completa e tolerâncias de 1,5 NM).
- Atualizar `MissionBoardCore.cs` para repassar o `profile` sorteado ao gerador `CreateVfrTriangleMissionSync`.
- Implementar as variantes `triangle`, `out_and_back` e `triangle_tgl` em `MissionGenerator.CreateVfrTriangleMissionSync`, com filtragem de pernas/distâncias, contrato, ponto de reportagem `P:` e renderização de textos por chave de variante.
- Configurar os 3 perfis da missão `navegacao_vfr` em `mission_catalog.json` e adicionar as variantes de texto em `mission_texts.json`.
- Implementar testes de unidade abrangentes em `MultiStopTrackerTests`, `VfrTriangleTests` e validação com `MissionCompletion`.
- Regravar snapshot `missions_golden.json`, executar `board-report --boards 50`, registrar changelog e documentação.

## O que foi feito
- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`:
  - Adicionada propriedade opcional `public string? Variant { get; set; }` em `MissionProfileDefinition`.
- `src/FlightCareer.Domain/Missions/ContractData.cs`:
  - Adicionada propriedade `public string StopMode { get; set; } = "full_stop";` em `MultiStopContractData` para suportar `"full_stop"` e `"touch_and_go"`.
- `src/FlightCareer.Domain/Missions/TouchAndGoCounter.cs`:
  - Expostas constantes `MinRollDurationSeconds = 1.0`, `MinGroundSpeedForRollKt = 20.0` e `FullStopSpeedThresholdKt = 5.0`.
  - Adicionado método reutilizável `public static bool IsValidTouchAndGo(double rollDurationSeconds, double maxGroundSpeedKt, bool fullStopOccurred)` reutilizado internamente e pelo `MultiStopTracker`.
- `src/FlightCareer.Domain/Missions/MultiStopTracker.cs`:
  - Suporte completo ao modo `StopMode = "touch_and_go"`. Acumula tempo de rolagem no solo (`_rollDurationSeconds`), velocidade máxima no solo e checagem de parada durante o toque.
  - Na decolagem em modo TGL, valida se o toque teve rolagem $\ge 1,0$ s, velocidade máxima $> 20$ kt, sem parada prévia e ocorrido a $\le 1,5$ NM da escala intermediária.
  - Mantém aceitação de parada completa no modo TGL (aluno não é penalizado se parar).
  - Mantém ordem sequencial, verificação de combustível sem reabastecimento indevido e exigência de $\ge 5$ s no ar antes da escala seguinte.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - Repasse do `profile` sorteado por `PickProfile` para `MissionGenerator.CreateVfrTriangleMissionSync`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Implementação das 3 variantes para `navegacao_vfr`:
    - `triangle` (padrão): pernas 25–90 NM, total 80–250 NM, escalas A e B com parada completa (`StopMode = "full_stop"`).
    - `out_and_back`: perna 40–120 NM, total 80–240 NM, escala A com pouso completo (`StopMode = "full_stop"`).
    - `triangle_tgl`: pernas 25–70 NM, total 75–200 NM, escalas A e B com toque e arremetida (`StopMode = "touch_and_go"`).
  - Ponto de reportagem obrigatório `P:` no meio da primeira perna com desvio lateral de 3–8 NM em todas as 3 variantes. Retorno para pouso final na origem e 1 pax (instrutor).
  - Renderização dinâmica do texto usando chave de variante (`navegacao_vfr:out_and_back`, `navegacao_vfr:triangle_tgl`, `navegacao_vfr:triangle`) com fallback transparente em `sub.Id` e nos textos padrão.
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`:
  - Configurados os 3 perfis de `navegacao_vfr` com os pesos correspondentes (50 para `triangle`, 25 para `out_and_back`, 25 para `triangle_tgl`) e faixas de distância da tabela.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`:
  - Adicionadas chaves imersivas `navegacao_vfr:triangle`, `navegacao_vfr:out_and_back` (citando `{stop_a}` e "pouso completo") e `navegacao_vfr:triangle_tgl` (citando `{stop_a}`, `{stop_b}` e "toque e arremetida"), sem `{}` residuais.
- `src/FlightCareer.Tests/Missions/MultiStopTrackerTests.cs`:
  - 4 novos testes unitários para o modo TGL:
    - `ModoTgl_TglEmAeBNaOrdem_CompletaComSucesso`
    - `ModoTgl_TglForaDoRaio1Ponto5Nm_NaoConta`
    - `ModoTgl_ToqueComQuique_NaoConta`
    - `ModoTgl_ParadaCompletaEmA_Conta`
- `src/FlightCareer.Tests/Missions/VfrTriangleTests.cs`:
  - 2 novos testes de integração e contrato:
    - `IdaEVolta_SemEscalaA_FalhaCitandoA` (verificação no `MissionCompletion`)
    - `Gerador_NQuadrosComSementeFixa_Gera3VariantesComInvariantes` (validação procedural das 3 variantes, distâncias, paradas/toques, P: e ausência de chaves de template)
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`:
  - Snapshot regravado e validado.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Documentação atualizada nas seções correspondentes.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [x] Snapshot `missions_golden.json` regravado; análise de diferenças documentada:
  - Total de 280 missões no snapshot golden;
  - 60 registros alterados exclusivamente nos aeroportos onde `navegacao_vfr` e `pinga_pinga` possuem peso no sorteio: `SBRP`, `SBJR`, `SDCO`, `SBDN`, `SBFI`, `SWCA`, `SSKW`;
  - Todos os outros aeroportos (220 registros, incluindo `SBGR`, `SBGL`, `KJFK`, etc.) permaneceram 100% idênticos;
  - Causa das alterações: inclusão do campo `"StopMode": "full_stop"` no contrato tipado `MultiStopContractData` e avanço procedural das sementes de sorteio de perfis da `navegacao_vfr`.
- [x] `board-report --boards 50`: executado; linhas de frequência e faixas da `navegacao_vfr` coladas abaixo:
  - Frequência por aeroporto:
    `| navegacao_vfr | 0.0% | 0.9% | 0.9% | 0.1% | 6.3% | 4.1% | 12.9% | 2.2% | 0.1% | 2.1% |`
    (com forte presença em aeródromos de instrução/escolas: Jacarepaguá 12.9%, Sorocaba 6.3%, Boituva 4.1%, Fazenda Portela 2.2%, Anápolis 2.1%).
  - Faixas operacionais:
    `| navegacao_vfr | 1/1.0/1 · (1) | 0/0/0 · (1) | 9/18/26 · (9) | 83/167/248 · (139) |`
    (Pax: 1 fixo; Carga: 0 lb; Bagagem: 9 a 26 lb; Distância total: 83 a 248 NM em 139 amostras, cobrindo o envelope total 75–250 NM).
- [x] `docs/CHANGELOG.md` e `AGENTS.md` atualizados.
- [x] Um único commit com a mensagem padronizada `feat(missoes): navegacao VFR com ida e volta e triangulo com toques (E2C-1b)`. Sem push.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 1.057 aprovados, 0 falhas (100% verde). Novos:
  - `MultiStopTrackerTests.ModoTgl_TglEmAeBNaOrdem_CompletaComSucesso`
  - `MultiStopTrackerTests.ModoTgl_TglForaDoRaio1Ponto5Nm_NaoConta`
  - `MultiStopTrackerTests.ModoTgl_ToqueComQuique_NaoConta`
  - `MultiStopTrackerTests.ModoTgl_ParadaCompletaEmA_Conta`
  - `VfrTriangleTests.IdaEVolta_SemEscalaA_FalhaCitandoA`
  - `VfrTriangleTests.Gerador_NQuadrosComSementeFixa_Gera3VariantesComInvariantes`

## Não testado
- Atuação ao vivo dentro do Microsoft Flight Simulator (não testado no MSFS) / não visto na tela do WPF durante o voo.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida em aberto. Todas as especificações técnicas, tolerâncias de rolagem/toque e geração procedural foram cumpridas rigorosamente.
