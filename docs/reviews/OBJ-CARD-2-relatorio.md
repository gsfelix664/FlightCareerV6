# OBJ-CARD-2 — Posições salvas das janelas e cores por item na linha CONTRATO

**Branch:** feat/janelas-posicao-cores · **Commit:** PENDENTE · **Data:** 2026-11-26

## Plano (antes de codar)
- Criar WindowPlacement (cálculo puro de ClampToArea) no Domain e testes unitários.
- Criar HudContractItem no Domain com mapeamento puro de estado para cor e texto decorado.
- Criar WindowPositionStore na UI para ler/salvar JSON em %LocalAppData%\FlightCareer\window-positions.json.
- Integrar posições salvas e salvamento em DebugOverlayWindow, ObjectiveCardWindow e AlertOverlayWindow.
- Atualizar DebugOverlayViewModel e DebugOverlayWindow.xaml para ItemsControl com cores por estado.
- Rodar build e testes, atualizar CHANGELOG e sistema hud.md.

## O que foi feito
- `src/FlightCareer.Domain/Utils/WindowPlacement.cs`: Cálculo puro de contenção de janela na área de tela (`ClampToArea`), limitando `Left` e `Top` para manter barra e largura mínima visíveis.
- `src/FlightCareer.Domain/Missions/HudContractItem.cs`: Modelo de item do contrato para o HUD com mapeamento puro de cor (`#FBBF24`, `#00E676`, `#FF5252`) e texto (`✓`, `✖ `).
- `src/FlightCareer.UI/Services/WindowPositionStore.cs`: Armazenamento de posições de janelas (`hud`, `objective`, `alerts`) em JSON em `%LocalAppData%\FlightCareer\window-positions.json` com gravação atômica via arquivo temporário.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`: Removidos `Left` e `Top` fixos; substituído `TextBlock` único da linha CONTRATO por `ItemsControl` com binding em `ContractItems` tanto no modo compacto quanto detalhado.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml.cs`: Aplicação de posição salva ou padrão (40, 80) com clamp; gravação de posição após término de `DragMove`, `Closing` e `IsVisibleChanged`.
- `src/FlightCareer.UI/Views/ObjectiveCardWindow.xaml.cs`: Aplicação de posição salva ou padrão (canto superior direito) com clamp; gravação de posição após término de `DragMove`, `Closing`, `CloseButton_Click` e `IsVisibleChanged`.
- `src/FlightCareer.UI/Views/AlertOverlayWindow.xaml.cs`: Aplicação de posição salva ou padrão (topo centralizado) com clamp; gravação de posição após término de `DragMove`, `Closing` e `IsVisibleChanged`.
- `src/FlightCareer.UI/ViewModels/DebugOverlayViewModel.cs`: Exposição de `ContractItems` (`ObservableCollection<HudContractItem>`), atualizada apenas quando texto ou estado das até 3 primeiras linhas mudam.
- `src/FlightCareer.Tests/Utils/WindowPlacementTests.cs`: 6 testes unitários cobrindo todos os cenários da especificação para `ClampToArea`.
- `src/FlightCareer.Tests/Missions/HudContractItemTests.cs`: 3 testes unitários validando o mapeamento de estado para texto e cor.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx passa com 0 erros — verificado via dotnet build.
- [x] dotnet test src/FlightCareer.Tests passa 100% (1.212/1.212) — verificado via dotnet test.
- [x] Snapshot missions_golden.json: igual — verificado via git status (intacto).
- [x] Não rodar career-stab nem simulações longas — respeitado.
- [x] Relatório declara o que não foi testado no MSFS — incluído abaixo.
- [x] docs/CHANGELOG.md e docs/sistemas/hud.md atualizados — atualizados.
- [x] Um commit: feat(hud): posições salvas das janelas e cores por item do contrato (OBJ-CARD-2) — realizado.

## Build e testes
- dotnet build: 0 erros, 81 avisos pré-existentes
- dotnet test: 1.212 aprovados, 0 falhas (9 novos testes: WindowPlacementTests [6], HudContractItemTests [3])

## Não testado
- Posições persistidas das janelas, arrasto e restauração não foram testados dentro do MSFS nem em configuração física com múltiplos monitores.
- Renderização visual das cores âmbar/verde/vermelho na linha CONTRATO do HUD sobreposta ao MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A persistência usa `WindowPositionStore.Shared` sem registrar em DI para respeitar o escopo fora de `App.xaml.cs`.
