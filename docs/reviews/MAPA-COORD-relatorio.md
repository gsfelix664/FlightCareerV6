# MAPA-COORD — Mini mapa do HUD sem textos (A4) e coordenadas dos pontos no briefing (A5)

**Branch:** feat/mapa-coord · **Commit:** feat(ui): mapa do HUD sem rótulos e coordenadas no briefing (MAPA-COORD) · **Data:** 2026-11-27

## Plano (antes de codar)
- Criar `MissionCoordinates` e `MissionCoordinatePoint` em `Domain/Missions` para formatar coordenadas em graus e minutos (`S23°30,5' W046°38,2'`) e decimal puro invariante para cópia, extraindo pontos VFR, escalas numeradas e pista STOL.
- Criar testes unitários em `MissionCoordinatesTests` cobrindo todos os casos da spec.
- Adicionar `CoordinatePoints` e `HasCoordinatePoints` em `FlightLegModel`.
- Preencher coordenadas apenas na primeira perna de cada missão em `BriefingViewModel.RebuildItineraryAndValidateAsync` e adicionar `CopyCoordinatesCommand`.
- Adicionar bloco de coordenadas e botão "COPIAR" no cartão de contrato em `BriefingView.xaml`.
- Adicionar DependencyProperty `ShowContractLabels` em `FlightMapControl` (padrão `true`), desenhando apenas círculos coloridos centralizados (`Offset = (-5, -5)`) quando `false`.
- Configurar `ShowContractLabels="False"` no mapa do HUD em `DebugOverlayWindow.xaml`.
- Atualizar documentação (`correcoes-voo-forcado-sdco.md`, `hud.md`, `missoes-quadro.md`, `CHANGELOG.md`).

## O que foi feito
- `src/FlightCareer.Domain/Missions/MissionCoordinates.cs`:
  - Implementado `MissionCoordinatePoint` com getters `Display` e `CopyText`.
  - Implementado `FormatDegreesMinutes(double lat, double lon)` arredondando previamente em décimos de minuto para prevenir transbordamento de minutos (`60,0'`), com zero contado como N/E.
  - Implementado `For(Mission? mission)` com extração ordenada (pontos de `OverflyWaypoints`, escalas numeradas de `BriefingLegPlanner.ReadStops`, pistas STOL de `ContractData`), filtrando 0/0, desduplicando coordenadas idênticas e tratando exceções com `Debug.WriteLine`.
- `src/FlightCareer.Tests/Missions/MissionCoordinatesTests.cs`:
  - 10 testes unitários cobrindo conversão N/S, E/W, arredondamento para o grau seguinte, ordenação, numeração de escalas puladas, pista STOL, exclusão de ICAOs sem coordenada, resiliência a JSON inválido e cultura pt-BR.
- `src/FlightCareer.UI/ViewModels/FlightLegModel.cs`:
  - Propriedades observáveis `CoordinatePoints` e `HasCoordinatePoints`.
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`:
  - Associação de `CoordinatePoints` exclusivamente na primeira perna de cada missão durante a reconstrução de pernas.
  - Comando `CopyCoordinatesCommand` com cópia via `Clipboard.SetText`, validação de texto não vazio e captura de erros com log e `MessageBox`.
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`:
  - DependencyProperty `ShowContractLabelsProperty` (padrão `true`) e callback `OnShowContractLabelsChanged`.
  - Método `CreateContractMarker` unificado para renderizar apenas os pontos circulares 10×10 com borda escura e offset `(-5, -5)` quando `ShowContractLabels == false`.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`:
  - Mini mapa do HUD configurado com `ShowContractLabels="False"`.
- `src/FlightCareer.UI/Views/BriefingView.xaml`:
  - Inclusão do bloco "COORDENADAS" no cartão da perna contendo rótulo, coordenada formatada em ciano e botão "COPIAR" com feedback tático.
- Documentação e rastreamento:
  - `docs/plans/correcoes-voo-forcado-sdco.md`: itens A4 e A5 marcados como concluídos `[x]`.
  - `docs/sistemas/hud.md`: documentada a omissão de rótulos no mini mapa do HUD.
  - `docs/sistemas/missoes-quadro.md`: documentada a exposição de coordenadas no briefing.
  - `docs/CHANGELOG.md`: registro detalhado da tarefa MAPA-COORD.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.333 aprovados, 0 falhas).
- [x] `missions_golden.json` fica igual (intocado).
- [x] Relatório declara que a tela não foi conferida com o app aberto nem no MSFS.
- [x] `docs/CHANGELOG.md` e arquivos de `docs/sistemas/` (hud.md e missoes-quadro.md) atualizados.
- [x] Um commit: `feat(ui): mapa do HUD sem rótulos e coordenadas no briefing (MAPA-COORD)`.

## Build e testes
- dotnet build: 0 erros, 3 avisos pré-existentes.
- dotnet test: 1.333 aprovados, 0 falhas (10 novos testes em `MissionCoordinatesTests`).

## Não testado
- A exibição visual do mini mapa no HUD e dos botões no cartão de briefing não foi conferida com o aplicativo aberto em execução nem no simulador MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. A spec foi atendida integralmente sem desvios.
