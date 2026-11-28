# Revisão MISSOES-LOCAIS — APROVADO (com ajustes do revisor)

Revisor: Claude, 2026-11-22. Diff lido nos arquivos quentes (`FlightMonitor` bloco de sobrevoo, `BriefingViewModel`, `App.xaml.cs`, `BriefingView.xaml`) e no domínio (`OverflyWaypointCodec`, `OverflyTracker`, `MissionCompletion`), gerador (3.4.1, 3.4.3, 3.5.1 e fallbacks nos dois caminhos), `PlacesDataService` (carga lazy com lock, busca por célula) e amostra de `mission_texts.json` (59 subtipos, 3 variantes cada). Build 0 erros; `dotnet test` 926/926. Auditoria: um script Python temporário criado e apagado dentro do worktree; nada fora dele.

Ajustes feitos pelo revisor:
1. Nomes de pontos turísticos com `;`, `@` ou `,` quebrariam o formato `P:` → `OverflyWaypointCodec.SanitizeName` aplicado no gerador e no `Format`, com teste.
2. Texto do panorâmico dizia "até 2 NM" com raio real de 1,5 NM → corrigido.

Observações (não bloqueiam): o 3.4.3 manteve `RangeRules` 20–40 NM, mas a distância usada é a do circuito (área + volta). Mapa e marcadores **não vistos na tela**; nada testado no MSFS.
