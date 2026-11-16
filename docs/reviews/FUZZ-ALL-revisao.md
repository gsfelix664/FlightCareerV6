# FUZZ-ALL — Revisão (Claude)

**Branch:** `test/fuzz-all-aeronaves` · **Commits:** `6390c7d`, `a5df27e`, `85ebead` (correção do revisor) · **Data:** 2026-11-16

## Veredito: APROVADO (com 1 correção feita pelo revisor)

## Conferido
- Oráculo: `noFlaps` ignora o gabarito de `FlapOverSpeed` e não gera "limite ausente"; avião com flap sem `vfe` continua reportando. Tolerância de duração reduzida agora escala a perda do controle pela fração de horas (folga de 1 %); o 0,05 fixo saiu. Mesma duração: regra estrita de antes, inalterada.
- Plano: `FlapOverSpeed` sai dos candidatos só para `noFlaps`; o sorteio dos demais modelos não muda.
- Dados: `noFlaps` só no J-3 Cub e no CAP-4 (Paulistinha), `dataVersion` incrementado; enum do `_schema.json` atualizado.
- CLI: `--aircraft all`, `--runs` por aeronave (padrão 50), tabela ordenada com falhas primeiro, `catch` vazio trocado por log.
- `dotnet build` 0 erros; `dotnet test` **509/509**; `fuzz --aircraft "Diamond DA40NG" --runs 10` rodado pelo revisor.

## Correção feita pelo revisor (`85ebead`)
1. O campo `reproduce` usava `--seed <seed da corrida> --runs 1`, mas o número de abusos depende do índice da corrida (`run % 5`), então o comando gerava outro voo. Agora: `--seed <semente base> --runs <índice + 1>` (a corrida falha é a última). Os comandos de exemplo do relatório do Gemini têm o mesmo problema: use o `reproduce` dos artefatos.

## Achados do fuzz (não corrigidos nesta tarefa, viram decisões/tarefas)
1. **CHT em motores refrigerados a líquido** (DA40NG, DA62, Flight Design CT): `chtMax` 105–120 °C contra CHT sintética de 155–175 °C. O gerador precisa usar a CHT de cruzeiro pela classe/limite do perfil.
2. **Vfe por dente em aviões grandes** (A330-200, BelugaXL, A400M, ES30): o gerador fixa o dente 2 e usa `vfe:2` com reserva de 85 kt; precisa escolher um dente que exista no perfil.
3. **`egtMax` = 649 °C** (Bonanza G36, CAP-4, DA40NG, Robin DR400) vem da SimVar `MAX EGT` (1.200 °F), que parece ser a escala do instrumento e não um limite do motor. Decisão de dados para o usuário.
4. Limites ausentes mais frequentes: `elecMainBusIndex` (328), `elecAvionicsCircuitIndex` (184), `alternatorRatedAmps` (13) — dependem de captura no simulador ou de padrão de classe.
