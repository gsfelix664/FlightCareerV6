# HANGAR-ICONES-PECAS — Estado das peças por ícone (sem % visível) e horas corretas por grupo

**Branch:** feat/hangar-icones-pecas · **Commit:** (a gerar) · **Data:** 2026-11-22

## Plano (antes de codar)
- No `FlightCareer.Domain/Parts/PartHangarView.cs`:
  - Implementar enum `PartDisplayState { Green, Yellow, Red, Broken, Uncertain }`.
  - Implementar record `PartDisplayInfo(PartDisplayState State, PartKnownBand LastKnownBand, double? HoursSinceSeen)`.
  - Implementar `PartHangarView.DisplayState(AircraftPart part, double airframeHours, double growthPerHour, PartBandSettings? bands = null)`.
  - Implementar `PartHangarView.Summarize(IEnumerable<PartDisplayInfo> items)`.
  - Implementar `PartHangarView.GroupLifeText(ComponentKind kind, PilotAircraft aircraft, EngineClass engineClass)`.
  - Atualizar `PartHangarRow` com `PartDisplayInfo Display`.
- No `FlightCareer.UI/ViewModels/HangarIcons.cs`:
  - Adicionar as 4 geometrias vetoriais 24x24 (Check, Exclamation, Broken, Question) e método auxiliar para obter a geometria do `PartDisplayState`.
- No `FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`:
  - Atualizar `PartRowViewModel` para expor o estado de exibição, texto de status, geometria do ícone e pincel.
  - Atualizar `ComponentRowViewModel` com propriedades do grupo (`GroupDisplayState`, `GroupIconGeometry`, `GroupStateBrush`, `GroupCountsSummary`, `IsGroupPartsDriven`, `GroupLifeText`, etc.).
- No `FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Adicionar suporte a aeronave com peças nas caixas do topo: Aeronavegabilidade (pior estado dos grupos críticos + texto) e Condição Geral (resumo de contagem das peças) e expor flags para alternar a exibição entre aeronave com peças vs clássica.
- No `FlightCareer.UI/Views/HangarView.xaml`:
  - Atualizar caixas do topo, cabeçalhos de grupos e linhas de peças para mostrar ícones e contagens sem % visível em aeronaves com peças.
  - Atualizar legenda para explicar os 4 ícones e estados.
- No `FlightCareer.Tests/Parts/PartDisplayStateTests.cs`:
  - Implementar todos os testes de unidade solicitados na especificação e para `GroupLifeText`.
- Compilar, rodar testes, revisar diff e commitar.

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartHangarView.cs`:
  - Criados o enum `PartDisplayState` e o record `PartDisplayInfo`.
  - Adicionado campo `PartDisplayInfo Display` ao `PartHangarRow`.
  - Implementado `PartHangarView.DisplayState(...)` calculando o estado a partir de `KnownHealth`, `KnownAtAirframeHours` e `IsBroken`, aplicando a expansão por incerteza (`growthPerHour × horasDesdeVista`) contra o limite inferior da faixa (40 % verde, 20 % amarelo), mantendo peças vermelhas/quebradas imunes a incerteza e nunca vazando `Health`.
  - Implementado `PartHangarView.Summarize(...)` retornando o pior estado na prioridade solicitada (Broken > Red > Yellow > Uncertain > Green) e o dicionário de contagens por estado.
  - Implementado `PartHangarView.GroupLifeText(...)` e `GroupLifeInfo(...)` formatando horas restantes de TBO, troca de óleo (12 h) e revisão de ignição (125 h), indicando atrasos com aviso "vencida há X h" e deixando os demais grupos sem texto no cabeçalho.
- `src/FlightCareer.UI/ViewModels/HangarIcons.cs`:
  - Adicionadas geometrias vetoriais `Check`, `Warning`, `Broken` e `Question` (SVG path 24×24 com strokes padronizados).
  - Adicionado helper `ForDisplayState(PartDisplayState)`.
- `src/FlightCareer.UI/ViewModels/ComponentRowViewModel.cs`:
  - `PartRowViewModel` atualizado para expor `Display`, `DisplayState`, `StateIconGeometry`, `StateBrush` e `StateTooltip`, sem exibir % na tela.
  - `HangarPalette` enriquecida com `ForDisplayState`.
  - `ComponentRowViewModel` atualizado para expor o estado consolidado do grupo (`GroupWorstState`, `GroupStateIconGeometry`, `GroupStateBrush`, `GroupCountsSummary`), integrando o texto de horas de manutenção de aeronaves com peças via `PartHangarView.GroupLifeText`.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Caixas do topo atualizadas: quando `HasPartsDriven` for verdadeiro, `AirworthinessText` exibe a descrição do pior estado dos grupos críticos ("Em ordem", "Atenção", "Risco de pane", "Peça quebrada", "Incerta — faça uma inspeção") e `AirworthinessBrush` assume a cor correspondente; `ConditionText` exibe o resumo compacto de peças (`✓ n · ⚠ n · ✖ n · ? n`).
- `src/FlightCareer.UI/Views/HangarView.xaml`:
  - Substituição das porcentagens numéricas visíveis por ícones de estado nas peças e contagens resumidas no cabeçalho dos grupos quando a aeronave possui peças.
  - Caixas do topo exibem ícone e status textual na Aeronavegabilidade e contagem na Condição Geral para aeronaves com peças.
  - Adicionada legenda para os 4 ícones e mantida a legenda e exibição clássica para aeronaves sem peças (`HasPartsDriven == false`).
- `src/FlightCareer.Tests/Parts/PartDisplayStateTests.cs`:
  - Criada nova suíte de testes com 17 testes cobrindo todas as situações pedidas na especificação.
- `docs/CHANGELOG.md` e `AGENTS.md`:
  - Registrado o histórico e o estado atualizado do Hangar.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx 0 erros; dotnet test src/FlightCareer.Tests 100 %.
- [x] Nenhuma % visível no Hangar de uma aeronave com peças (peças, grupos, caixas do topo, tooltips).
- [x] Ícones vetoriais (Geometry/Path), não emoji; cores da HangarPalette.
- [x] Estado calculado só com a saúde conhecida (teste Nao_usa_a_saude_real).
- [x] Texto de horas dos grupos Motor/Óleo/Ignição vem dos campos de manutenção da aeronave; demais grupos sem texto.
- [x] Aeronave sem peças: tela igual à de hoje.
- [x] Entrada no fim do docs/CHANGELOG.md e atualização do AGENTS.md › 7 (Hangar). Relatório diz "não visto na tela / não testado no MSFS".
- [x] Um commit: feat(hangar): estado das pecas por icone e horas de manutencao por grupo (HANGAR-ICONES-PECAS).

## Build e testes
- dotnet build: 0 erros, 19 avisos pré-existentes.
- dotnet test: 729 aprovados, 0 falhas (17 novos testes em `PartDisplayStateTests`).

## Não testado
- Interface gráfica WPF não vista em execução na tela e comportamento não testado em voo no MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma.
