# OBJ-CARD-1 — Cartão de objetivo do voo

**Branch:** feat/cartao-objetivo · **Commit:** 694e8fb · **Data:** 2026-11-26

## Plano (antes de codar)
- Estruturar linhas de progresso no Domain com `ContractObjectiveLine` e `ContractObjectiveState` em `ContractProgressFormatter`, mantendo retrocompatibilidade no `Build` e `FormatHud`.
- Criar `ObjectiveCardBuilder` no Domain para gerar o conteúdo do cartão de objetivos a partir das missões ativas e linhas de rastreador, incluindo "Pouso final em {ICAO}".
- Atualizar `FlightMonitor` para expor `ContractObjectiveLines` alimentado a cada tick e reinicializado em `StartMonitoring`.
- Implementar UI com `ObjectiveCardWindow` (Topmost, WS_EX_NOACTIVATE, DragMove, Hide no ✕), `ObjectiveCardViewModel` (atualização por chave de texto+estado) e `ObjectiveCardService` (singleton com timer de 1s para abertura na decolagem/início e atalho global Ctrl+Alt+O via RegisterHotKey).
- Adicionar botão 🎯 no cabeçalho do `DebugOverlayWindow` chamando o serviço e registrar no `App.xaml.cs`.
- Cobrir com testes unitários em `ContractProgressFormatterTests` e `ObjectiveCardBuilderTests`.

## O que foi feito
- `src/FlightCareer.Domain/Missions/ContractProgressFormatter.cs`: adicionados enum `ContractObjectiveState` e record `ContractObjectiveLine`; métodos `BuildLines` e `FormatHud`; `Build` refatorado para usar `FormatHud(BuildLines(...))`.
- `src/FlightCareer.Domain/Missions/ObjectiveCardBuilder.cs`: criado construtor puro com records `ObjectiveCardMission`, `ObjectiveCardContent` e método estático `Build` que mapeia missões e adiciona os pousos finais únicos.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: adicionada propriedade `ContractObjectiveLines`, limpeza no `StartMonitoring` e atualização no tick de telemetria.
- `src/FlightCareer.UI/ViewModels/ObjectiveCardViewModel.cs`: criado ViewModel com coleções observáveis de missões e objetivos com cores padronizadas (âmbar #FFC400, verde #00E676, vermelho #FF5252), e método `Update` com detecção de mudança por chave composta.
- `src/FlightCareer.UI/Views/ObjectiveCardWindow.xaml` e `.xaml.cs`: janela overlay transparente 320px com `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, arrastável, botão ✕ para esconder.
- `src/FlightCareer.UI/Services/ObjectiveCardService.cs`: singleton com `DispatcherTimer` (1s), transição de abertura automática ao iniciar monitoramento, atalho global Ctrl+Alt+O via `RegisterHotKey` com HwndSource própria e unregister no exit.
- `src/FlightCareer.UI/ViewModels/DebugOverlayViewModel.cs`: recebimento de `ObjectiveCardService?` no construtor e RelayCommand `ShowObjectiveCard`.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`: adicionado botão 🎯 antes do ✕ no cabeçalho do HUD.
- `src/FlightCareer.UI/App.xaml.cs`: registro de `ObjectiveCardService` no container DI e resolução na inicialização após splash/login.
- `src/FlightCareer.Tests/Missions/ContractProgressFormatterTests.cs`: adicionados testes para `BuildLines`, `FormatHud` e cenários de limitação/reabastecimento.
- `src/FlightCareer.Tests/Missions/ObjectiveCardBuilderTests.cs`: criados testes de destinos distintos e lista vazia.
- `docs/sistemas/hud.md`: adicionada nota sobre o cartão de objetivo e atalho Ctrl+Alt+O.
- `docs/CHANGELOG.md`: adicionada entrada da tarefa OBJ-CARD-1.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx passa com 0 erros.
- [x] dotnet test src/FlightCareer.Tests passa 100% (1.203/1.203 testes aprovados, testes antigos do ContractProgressFormatter inalterados).
- [x] Snapshot missions_golden.json inalterado.
- [x] Sem simulações longas (career-stab).
- [x] Declaração explícita no relatório do que não foi testado no simulador.
- [x] Entrada no docs/CHANGELOG.md e docs/sistemas/hud.md atualizado.
- [x] Commit único no formato `feat(hud): cartão de objetivo do voo (OBJ-CARD-1)`.

## Build e testes
- dotnet build: 0 erros, 76 avisos pré-existentes.
- dotnet test: 1.203 aprovados, 0 falhas (8 novos testes: `BuildLines_TglIncompleto_Pending`, `BuildLines_TglCompleto_DoneSemMarcador`, `BuildLines_EscalaReabasteceu_Failed`, `BuildLines_MaisDeTresLinhas_RetornaTodas`, `Build_MaisDeTresLinhas_LimitaATres`, `FormatHud_RecolocaMarcadores`, `ObjectiveCardBuilder_DestinosRepetidos_UmPousoFinalPorDestino`, `ObjectiveCardBuilder_SemMissoes_Vazio`).

## Não testado
- Comportamento visual da janela sobreposta ao Microsoft Flight Simulator (Topmost/transparência).
- Interação do clique e DragMove sem roubar o foco de controle da aeronave no MSFS em tempo real.
- Acionamento do atalho global Ctrl+Alt+O em conjunto com o simulador rodando.
- Botão 🎯 no cabeçalho do HUD clicado em voo.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva encontrada. A especificação cobriu detalhadamente contratos, cores, comportamento de foco e atalhos.
