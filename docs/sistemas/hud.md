# HUD

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Fornece overlay tático e instrumentos auxiliares por cima da janela do simulador.
- **Onde está:** `UI/ViewModels/DebugOverlayViewModel`, `UI/Controls/FlightMapControl.xaml.cs`, `Domain/Components/HudHealthSummary`, `HangarIcons`, `HangarPalette`.
- **Regras e integridade:**
  - Mini-GPS e mapa ao vivo: renderiza a rota, ponto de partida em destaque (`OriginLat`/`OriginLon`) e destino, com enquadramento tático automático em voos locais ou de circuito.
  - Ocultamento de saúde real: barras numéricas de porcentagem foram removidas. O resumo (`HudHealthSummary`) mostra só o nome do grupo (CÉLULA, MOTOR, PNEUS) e o ícone vetorial colorido de estado do Hangar, com o texto apenas na dica, (`✓`, `◐`, `⚠`, `✖`, `?`) baseados na saúde conhecida (`KnownHealth`), eliminando o vazamento da saúde física real em voo.
  - O mini-mapa desenha as áreas, pontos e escalas dos contratos ativos (`ContractMarkerBuilder`), como o mapa do briefing. Antes de o monitoramento começar usa os contratos aceitos (`AcceptedMissions`, passados pelo `MainViewModel` ao abrir o HUD), com a posição do avião como partida (2026-11-26, não testado no MSFS).
  - Linha "CONTRATO" (`ContractProgressFormatter`, via `FlightMonitor.ContractProgressText`): andamento dos rastreadores ("TGL 1/2", "Escala 2/4 · próxima X", "Passo 3/3 · rumos ...", faixa de altitude da área abaixo/na faixa/acima). Não testado no MSFS.
  - Controles de gravação de telemetria (REC/Marcar) e roteiros de calibração integrados. Não testado visualmente no MSFS.
