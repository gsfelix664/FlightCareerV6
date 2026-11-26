# HUD

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Fornece overlay tático e instrumentos auxiliares por cima da janela do simulador.
- **Onde está:** `UI/ViewModels/DebugOverlayViewModel`, `UI/Controls/FlightMapControl.xaml.cs`, `Domain/Components/HudHealthSummary`, `HangarIcons`, `HangarPalette`.
- **Regras e integridade:**
  - Mini-GPS e mapa ao vivo: renderiza a rota, ponto de partida em destaque (`OriginLat`/`OriginLon`) e destino, com enquadramento tático automático em voos locais ou de circuito.
  - Ocultamento de saúde real: barras numéricas de porcentagem foram removidas. O resumo (`HudHealthSummary`) mostra só o nome do grupo (CÉLULA, MOTOR, PNEUS) e o ícone vetorial colorido de estado do Hangar, com o texto apenas na dica, (`✓`, `◐`, `⚠`, `✖`, `?`) baseados na saúde conhecida (`KnownHealth`), eliminando o vazamento da saúde física real em voo.
  - O mini-mapa desenha as áreas, pontos e escalas dos contratos ativos (`ContractMarkerBuilder`), como o mapa do briefing. Antes de o monitoramento começar usa os contratos aceitos (`AcceptedMissions`, passados pelo `MainViewModel` ao abrir o HUD), com a posição do avião como partida (2026-11-26, não testado no MSFS).
  - Linha "CONTRATO" (`ContractProgressFormatter`, `HudContractItem`, via `FlightMonitor.ContractObjectiveLines` / `ContractProgressText`): andamento dos rastreadores com cores por item e estado (âmbar `#FBBF24` pendente, verde `#00E676` cumprido com " ✓", vermelho `#FF5252` falhou com "✖ "). Não testado no MSFS.
  - Cartão de objetivo do voo (`ObjectiveCardWindow`, `ObjectiveCardService`): janela overlay transparente e arrastável com os objetivos das missões ativas (pendente ○ âmbar, cumprido ✓ verde, falhou ✖ vermelho) e atalho global Ctrl+Alt+O, reabrível também pelo botão 🎯 no cabeçalho do HUD (2026-11-26, não testado no MSFS).
  - Posições salvas de janelas: HUD, cartão de objetivo e alertas persistem suas coordenadas em `%LocalAppData%\FlightCareer\window-positions.json` (`WindowPositionStore.Shared`), com garantia de visibilidade na tela virtual (`WindowPlacement.ClampToArea`) ao abrir (2026-11-26, não testado no MSFS).
  - Controles de gravação de telemetria (REC/Marcar) e roteiros de calibração integrados. Não testado visualmente no MSFS.
