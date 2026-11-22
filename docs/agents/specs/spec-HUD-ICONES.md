# Spec HUD-ICONES — Ponto de partida no mapinha do HUD e saúde por ícone (sem % e sem saúde real)

**Objetivo (1 frase):** no HUD (overlay), mostrar um ponto na partida igual ao ponto verde da chegada e trocar as barras de % de célula/motor/pneus pelos mesmos ícones de estado do Hangar, usando a saúde **conhecida** das peças.

## Fatos já levantados (não redescobrir)
- Pedido do usuário (HANDOFF 2026-11-22): "colocar um ponto para partida no mapinha do HUD, igual tem o ponto verde na chegada" e "ainda aparecem as % no HUD; deve aparecer apenas os ícones".
- HUD: `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml` (mapa ~linhas 168–184, `utils:FlightMapControl` com `DestLat/DestLon/DestIcao/OriginIcao`; saúde ~linhas 370–400: `AirframeCondition`, `EngineCondition`, `TiresCondition` com `StringFormat {0:F0}%` e `ProgressBar`) e `src/FlightCareer.UI/ViewModels/DebugOverlayViewModel.cs` (`RefreshAircraftHealth`, ~linhas 96–101 e 189–191; `OriginIcao`/`DestLat` ~linhas 204–226).
- **Problema maior:** o HUD lê as colunas antigas `PilotAircraft.EngineCondition/AirframeCondition/TiresCondition`, que guardam a **saúde real** (o agregado das peças). Isso vaza a saúde oculta em pleno voo.
- Mapa: `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs` já tem `_originPinMarker`/`CreatePinBadge(..., "#00E676")` para rotas gravadas (~linhas 895–930) e o marcador do destino ao vivo. Não há `OriginLat/OriginLon`.
- Ícones e estados do Hangar (reusar, não duplicar): `Domain/Parts/PartHangarView.DisplayState` / `Summarize` (estados Green, Worn, Yellow, Red, Broken, Uncertain pela saúde **conhecida**, `KnownHealth`/`KnownAtAirframeHours`, com `growthPerHour` de `ComponentCatalog.Visibility`), `UI/ViewModels/HangarIcons.ForDisplayState`, `HangarPalette.ForDisplayState`. Mapeamento peça → grupo: `PartComponentMapping` (o Hangar já agrupa as peças por componente).

## Passo a passo
1. **Ponto de partida:** `FlightMapControl` ganha `OriginLat`/`OriginLon` (DependencyProperty) e desenha, ao vivo, um ponto/pino na partida no mesmo estilo do ponto de chegada (cor distinta, ex. ciano `#00E5FF`, rótulo com o ICAO). `DebugOverlayViewModel` preenche `OriginLat/OriginLon` pelo aeroporto de origem da missão (mesma consulta que já faz para o destino) ou, sem missão, pela posição onde o voo começou. Briefing e diário continuam como estão.
2. **Saúde por ícone:** em `RefreshAircraftHealth`, carregar as peças do avião ativo (contexto curto, `AsNoTracking`) e, para os grupos Célula, Motor e Pneus/Freios (ou os grupos que hoje aparecem), calcular `PartHangarView.Summarize` dos `DisplayState` das peças do grupo → ícone e cor do **pior** estado + texto curto ("Em ordem", "Em uso", "Atenção", "Risco", "Incerta"). Remover as % e as `ProgressBar`. Aviões **sem peças** (`PartComponentBridge.HasParts == false`): mostrar só o ícone pela faixa exibida do componente (`HealthVisibility`), também sem %.
3. Nenhuma leitura de `Health` real nem das colunas antigas para exibição.

## Fora do escopo
- Desgaste, regras, Hangar, outras partes do HUD.

## Testes
- Lógica de agrupamento/resumo do HUD numa classe testável (ex. `HudHealthSummary.Build(parts, catalog, airframeHours, growthPerHour)` em Domain ou Application): peça com `Health` 5 e `KnownHealth` 90 visto há 0 h → grupo "Em ordem" (não vaza a real); grupo com uma peça amarela conhecida → "Atenção"; peça nunca vista → "Incerta".

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %.
- [ ] HUD sem nenhuma % de saúde e sem saúde real; ícones iguais aos do Hangar.
- [ ] Ponto de partida aparece no mapinha ao vivo.
- [ ] CHANGELOG e AGENTS.md › 7. Relatório diz "não visto na tela / não testado no MSFS".
- [ ] Um commit: `feat(hud): ponto de partida no mapa e saude por icone (HUD-ICONES)`.
