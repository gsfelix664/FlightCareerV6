# LIMPA-MAPA — Limpeza do mapa e texto de reporte VFR

**Branch:** feat/limpa-mapa · **Commit:** <pendente> · **Data:** 2026-11-26

## Plano (antes de codar)
- Remover waypoints manuais do mapa (A1):
  - Em `FlightMapControl.xaml`, remover o bloco do overlay de waypoints (`TxtDistance`, "WAYPOINTS ATIVOS"), o botão LIMPAR (`BtnClear_Click`) e o recurso `BooleanToVisibilityConverter` (`BoolToVisConverter`).
  - Em `FlightMapControl.xaml.cs`, remover campos `_waypoints`, `_waypointRoute`, propriedades `ShowWaypointsOverlayProperty` e `ShowWaypointsOverlay`, e o método `BtnClear_Click`.
  - Em `FlightMapControl.xaml.cs`, renomear `CreateWaypointCircle` para `CreateRoutePointCircle` (mantendo implementação) e atualizar a região para `#region Aeroportos e Trajetória Gravada`.
  - Em `DebugOverlayWindow.xaml`, remover o atributo `ShowWaypointsOverlay="False"`.
- Corrigir terminologia de reporte VFR (A2):
  - Em `mission_texts.json`, alterar as 4 ocorrências de "ponto de reportagem" para "ponto de reporte".
  - Em `MissionGenerator.cs`, atualizar comentários, descrições de fallback e o identificador gravado `"Ponto de Reportagem VFR"` para `"Ponto de Reporte VFR"`.
  - Em `VfrTriangleTests.cs` e `PhaseE2C1GeneratorsTests.cs`, atualizar literais, asserções, comentários e nomes de métodos de teste.
  - Em `missions_golden.json`, substituir pontualmente apenas o literal `"Ponto de Reportagem VFR"` por `"Ponto de Reporte VFR"` preservando rigorosamente quebras de linha CRLF e codificação.
- Validação:
  - Garantir ausência de referências via `git grep`.
  - Compilar e rodar 100% dos testes da solução.
  - Atualizar `docs/plans/correcoes-voo-forcado-sdco.md` (marcar A1 e A2) e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml`: removidos recursos de conversão (`BoolToVisConverter`), painel overlay de waypoints manuais e botão LIMPAR. O botão centralizar/recenter 🎯 foi mantido intacto.
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`: removidos campos mortos `_waypoints`, `_waypointRoute`, dependency property `ShowWaypointsOverlayProperty`, property wrapper `ShowWaypointsOverlay` e handler `BtnClear_Click`. `CreateWaypointCircle` renomeado para `CreateRoutePointCircle` em todas as suas chamadas e região renomeada para `#region Aeroportos e Trajetória Gravada`.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`: removido o atributo obsoleto `ShowWaypointsOverlay="False"` na tag `FlightMapControl`.
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: substituídas as 4 ocorrências de "ponto de reportagem" por "ponto de reporte" em `navegacao_vfr`, `navegacao_vfr:triangle`, `navegacao_vfr:out_and_back` e `navegacao_vfr:triangle_tgl`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: atualizados comentários, textos de fallback e o ponto gerado em `OverflyWaypointCodec.FormatCoordinatePoint` para `"Ponto de Reporte VFR"`.
- `src/FlightCareer.Tests/Missions/PhaseE2C1GeneratorsTests.cs`: renomeado teste `VfrTriangle_Gerador_SempreVoltaAOrigemETemPontoDeReportagem` para `VfrTriangle_Gerador_SempreVoltaAOrigemETemPontoDeReporte`.
- `src/FlightCareer.Tests/Missions/VfrTriangleTests.cs`: renomeados testes e atualizados literais/comentários de `"Ponto de Reportagem VFR"` para `"Ponto de Reporte VFR"`.
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`: substituição pontual binária de 4 ocorrências de `"Ponto de Reportagem VFR"` para `"Ponto de Reporte VFR"`, mantendo CRLF e codificação UTF-8 originais intactos.
- `docs/plans/correcoes-voo-forcado-sdco.md`: marcados como concluídos os itens A1 e A2.
- `docs/CHANGELOG.md`: adicionada entrada de changelog para a tarefa LIMPA-MAPA.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % — Verificado via terminal (0 erros de compilação; 1.245 testes aprovados).
- [x] `git grep -nE "_waypoints|_waypointRoute|BtnClear_Click|ShowWaypointsOverlay|CreateWaypointCircle|TxtDistance|WAYPOINTS ATIVOS" -- src` não retorna nada — Verificado (0 ocorrências).
- [x] `git grep -niE "reportagem" -- src` não retorna nada — Verificado (0 ocorrências).
- [x] `git diff --stat` do golden mostra exatamente 4 linhas alteradas — Verificado (`1 file changed, 4 insertions(+), 4 deletions(-)`).
- [x] Relatório declara o que não foi testado no app/MSFS — Declarado na seção Não testado.
- [x] `docs/CHANGELOG.md` (`## 2026-11-26 - Limpeza do mapa e texto de reporte VFR (LIMPA-MAPA) (Gemini)`); no plano, marcar `[x]` em A1 e A2 — Concluído.
- [x] Um commit: `chore(mapa): remove waypoints manuais e corrige texto de reporte VFR (LIMPA-MAPA)` — Preparado para commit único na branch.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 1.245 aprovados, 0 falhas (100% de sucesso).

## Não testado
- Interface gráfica WPF do mapa em execução interativa (não testada visualmente em runtime).
- Integração e comportamento visual do mapa/HUD durante voo conectado ao MSFS 2024.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. Todas as instruções da especificação e regras de aceite foram estritamente cumpridas.
