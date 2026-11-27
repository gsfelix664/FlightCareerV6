# Revisão MAPA-COORD — APROVADO (1ª rodada, 2026-11-27, Claude)

Commit revisado: `6021faa` (`feat/mapa-coord`).

- Escopo conforme a spec; `BriefingViewModel` (quente) lido no diff: só a lista de coordenadas na primeira perna de cada missão e o comando `CopyCoordinates`.
- Comando ligado por `AncestorType=UserControl`: o `BriefingView` recebe `DataContext="{Binding Briefing}"` em `MainWindow.xaml`.
- `ShowContractLabels` só é `False` no mapa do HUD; os demais mapas ficam como estavam.
- Build ok; 1.333/1.333 testes; `missions_golden.json` sem mudança.
- Observação: o CHANGELOG fala em "feedback" no botão COPIAR; só há o realce ao passar o mouse (sem aviso de sucesso, como pedia a spec).
- **Não conferido** com o app aberto nem no MSFS (alinhamento dos pontos no HUD e largura do bloco no cartão da perna).
