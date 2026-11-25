# BR-PERF — revisão (Claude, 2026-11-25)

**Veredito: APROVADO** (com ajustes feitos pelo revisor)

## Conferido
- Build da solução 0 erros; `dotnet test` 1.120/1.120; `missions_golden.json` inalterado.
- Código antigo × novo, ambos com a leitura do banco em ordem fixa (`--boards 10 --seed 7 --coverage`, Release): 4 CSVs e `.md` idênticos (fora a data); total 64 s → 31 s (quadros ~16 s, cerca de 3×); pico ~415 MB nos dois.
- Auditoria do `agy`: comandos fora do repositório só em pastas temporárias (medição).

## Ajustes do revisor
1. Lógica de elegibilidade e de peso de destino duplicada entre o caminho com e sem cache: unificada.
2. `board-report` não era determinístico entre execuções (ordem do banco por GUID): ordenação fixa.
3. Linha com acentos corrompidos em `docs/sistemas/missoes-quadro.md`.

## Ressalvas
- O relatório do executor cita o commit `9e196ee`, trocado por `73a60b6` com `--amend` (lição já registrada).
- A memória "menos de 100 MB" do relatório do executor não foi medida por ele.
- Vale o tempo medido nesta revisão, não o do relatório do executor.
- Não testado no MSFS (o quadro do jogo passa pelo mesmo código).
