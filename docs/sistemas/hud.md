# HUD

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Fornece overlay tático e instrumentos auxiliares por cima da janela do simulador.
- **Onde está:** `UI/ViewModels/DebugOverlayViewModel`, `UI/Controls/FlightMapControl.xaml.cs`, `Domain/Components/HudHealthSummary`, `HangarIcons`, `HangarPalette`.
- **Regras e integridade:**
  - Mini-GPS e mapa ao vivo: renderiza a rota, ponto de partida em destaque (`OriginLat`/`OriginLon`) e destino, com enquadramento tático automático em voos locais ou de circuito.
  - Ocultamento de saúde real: barras numéricas de porcentagem foram removidas. O resumo (`HudHealthSummary`) expõe exclusivamente ícones vetoriais de estado do Hangar (`✓`, `◐`, `⚠`, `✖`, `?`) baseados na saúde conhecida (`KnownHealth`), eliminando o vazamento da saúde física real em voo.
  - Controles de gravação de telemetria (REC/Marcar) e roteiros de calibração integrados. Não testado visualmente no MSFS.
