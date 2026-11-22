# HUD-ICONES — Ponto de partida no mapinha do HUD e saúde por ícone (sem % e sem saúde real)

**Branch:** feat/hud-icones · **Commit:** <hash> · **Data:** 2026-11-22

## Plano (antes de codar)
- Investigar `FlightMapControl.xaml.cs`, `DebugOverlayViewModel.cs`, `DebugOverlayWindow.xaml` e o sistema de exibição de saúde de peças (`PartHangarView`, `HangarIcons`, `HangarPalette`).
- Adicionar propriedades de dependência `OriginLat` e `OriginLon` ao `FlightMapControl` para desenhar o ponto de partida ao vivo (marcador circular ciano neon `#00E5FF` com tooltip ICAO de partida) no mesmo estilo do ponto de chegada e integrá-lo ao enquadramento tático.
- Criar a classe de domínio puro `HudHealthSummary` em `FlightCareer.Domain/Parts/HudHealthSummary.cs` para agregar a saúde conhecida das peças por grupo (Célula, Motor, Pneus/Freios) via `PartHangarView.Summarize` e `PartDisplayInfo` sem jamais ler ou vazar a saúde real oculta da aeronave, com fallback `BuildFromComponents` para aeronaves legadas sem peças.
- Atualizar `DebugOverlayViewModel` para remover a leitura das colunas legadas `AirframeCondition`, `EngineCondition` e `TiresCondition`, preencher `OriginLat`/`OriginLon` pela origem da missão ativa ou pela posição GPS onde o voo começou, e expor status com ícone e texto curto de cada grupo.
- Atualizar `DebugOverlayWindow.xaml` para ligar `OriginLat`/`OriginLon` no `FlightMapControl`, remover as barras de progresso e porcentagens numéricas do Grupo 5, e renderizar os ícones vetoriais de estado do Hangar (`HangarIcons.ForDisplayState`) com texto curto ("Em ordem", "Em uso", "Atenção", "Risco", "Incerta") na cor da paleta do Hangar.
- Criar suíte de testes xUnit em `FlightCareer.Tests/Parts/HudHealthSummaryTests.cs` e verificar 100% de aprovação no build e testes.

## O que foi feito
- `src/FlightCareer.Domain/Parts/HudHealthSummary.cs`:
  - Novo record de domínio puro `HudGroupHealth(GroupName, State, StatusText)`.
  - Método `HudHealthSummary.Build(parts, partCatalog, airframeHours, growthPerHour, bands, mapping)`: mapeia peças instaladas para Célula, Motor e Pneus, calcula `PartDisplayInfo` a partir da saúde conhecida (`KnownHealth` / `KnownAtAirframeHours`), avalia incerteza com base nas horas voadas e obtém o pior estado com `PartHangarView.Summarize`.
  - Método `HudHealthSummary.BuildFromComponents(components, airframeHours, visibility)`: fallback para aeronaves sem peças baseado na faixa de inspeção exibida (`HealthVisibility`).
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`:
  - Adicionadas DependencyProperties `OriginLatProperty` e `OriginLonProperty` com callback `OnOriginChanged`.
  - No método `UpdateDestinationRoute()`, desenha marcador circular ciano neon (`#00E5FF`, 9×9 px com borda preta) no ponto de partida com tooltip `$"ORIGEM: {OriginIcao}"`.
  - Ajustado o cálculo do retângulo de enquadramento suave (fly-to / pan-zoom) para incluir as coordenadas de origem quando presentes.
  - No método `UpdateMultiLegRoute()`, limpa o marcador de origem simples quando em modo de rotas com múltiplas pernas.
- `src/FlightCareer.UI/ViewModels/DebugOverlayViewModel.cs`:
  - Injeção de dependências suportando `IDbContextFactory<AppDbContext>`, `ComponentCatalog` e `PartCatalog` com fallbacks seguros para testes.
  - Novas propriedades observáveis de navegação `OriginLat` e `OriginLon`.
  - Método `FindAirport` com contexto curto e `AsNoTracking` para busca rápida das coordenadas dos aeródromos de origem e destino da missão.
  - No loop de telemetria, em missão ativa preenche `OriginLat`/`OriginLon`; em voo local sem missão, registra a posição GPS inicial onde o voo começou; em solo sem missão, reseta para 0.
  - Removida completamente a leitura de `AirframeCondition`, `EngineCondition` e `TiresCondition` da aeronave no loop de telemetria e no refresh de saúde.
  - `RefreshAircraftHealth`: consulta peças ou componentes do avião ativo via contexto curto com `AsNoTracking` e invoca `HudHealthSummary.Build` (ou `BuildFromComponents`).
  - Novas propriedades observáveis de status: `AirframeStatusText`, `AirframeStatusBrush`, `AirframeStatusColor`, `AirframeStatusIconGeometry`, e equivalentes para `Engine` e `Tires`.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`:
  - Ligados `OriginLat="{Binding OriginLat}"` e `OriginLon="{Binding OriginLon}"` ao `FlightMapControl`.
  - No Grupo 5 ("INTEGRIDADE DA AERONAVE & DESGASTE"): removidos `{0:F0}%` e `ProgressBar`. Adicionados `Viewbox` com `Path` desenhando o ícone vetorial (`HangarIcons.ForDisplayState`) e `TextBlock` com o texto curto de estado formatado na cor do status.
- `src/FlightCareer.Tests/Parts/HudHealthSummaryTests.cs`:
  - 7 novos testes unitários validando: não vazamento de saúde real (peça com saúde real 5 e saúde conhecida 90 reporta "Em ordem" verde), detecção de faixa amarela ("Atenção"), peça nunca vista ("Incerta"), peça em desgaste normal ("Em uso"), peça quebrada ("Quebrada"), prevalência do pior estado e fallback por componentes.
- Atualizados `docs/CHANGELOG.md` e `AGENTS.md` (seção 7).

## Critérios de aceite
- [x] Build 0 erros; `dotnet test` 100 % (834/834 testes aprovados).
- [x] HUD sem nenhuma % de saúde e sem saúde real; ícones iguais aos do Hangar.
- [x] Ponto de partida aparece no mapinha ao vivo.
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `feat(hud): ponto de partida no mapa e saude por icone (HUD-ICONES)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos conhecidos (vulnerabilidades NuGet upstream do SQLitePCLRaw e avisos de nulabilidade legados de outros módulos).
- `dotnet test src/FlightCareer.Tests`: 834 aprovados, 0 falhas (7 novos testes em `HudHealthSummaryTests`).

## Não testado
- Não visto na tela / não testado no MSFS com voo em tempo real (depende de simulador aberto e renderização gráfica de UI interativa do WPF).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. A implementação seguiu integralmente a especificação do revisor.
