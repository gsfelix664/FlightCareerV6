# CATALOGO-PADROES — Revisão (Claude)

**Branch:** `feat/catalogo-limites-estimados` · **Commits:** `d31ce3c` + correção do revisor · **Data:** 2026-11-16

## Veredito: APROVADO (com 1 correção feita pelo revisor)

## Conferido
- Dados (amostra: A400M, Dakota, King Air, CJ4, Bonanza V35): só chaves que faltavam, valores da tabela da spec, `source: ClassDefault`, referência "estimativa — atualizar pelo POH real: <motor>", `dataVersion` +1. Nenhum índice elétrico no catálogo; nenhuma chave duplicada nos 46 perfis.
- `noElectrical` no J-3 Cub e CAP-4, com plano e oráculo pulando os 3 abusos elétricos.
- `FuzzTestDefaults.WithTestElectricalIndices` (barramento 1, circuito 3, amperímetro 4 do C172) só no `RunFuzz` e nos testes; teste garante que o catálogo carregado nunca tem a referência de teste.
- `SyntheticFlightGenerator.ResolveLimit` lê primeiro o perfil recebido (necessário para o perfil aumentado); sem chave duplicada nos perfis, não muda a prioridade de fonte.
- `dotnet build` 0 erros; `dotnet test` **516/516**; `python -m unittest` 48/48; fuzz (50 voos) no Dakota, SR22 e Baron G58: 0 falhas e 0 limites ausentes (regras elétricas agora exercitadas).

## Correção feita pelo revisor
1. O relatório afirmou que o Dakota "recebeu egtMax da classe", mas ele continuava com `egtMax` = 615,74 °C (SimVar `MAX EGT`, escala do instrumento): eram as 4 falhas dele. Trocado pelo padrão da classe (893,52 °C, `ClassDefault`, nota POH), `dataVersion` 7. `merge_limits.py` agora descarta EGT de pistão < 700 °C (antes só 649,07).

## Observações
- O relatório cita o commit `98bf78e`, mas a branch tem `d31ce3c`: indica `--amend` depois do relatório (LICOES F1.1).

## Pendentes (tarefas à parte)
1. Gerador: CHT de cruzeiro pela classe/limite (DA40NG, DA62, Flight Design CT) e Vfe por dente existente (A330-200, BelugaXL, A400M, ES30) — 42 das 46 falhas restantes do fuzz.
2. Provisionamento: não criar peças de flap/elétricas em aviões `noFlaps`/`noElectrical`.
3. Índices elétricos reais por avião: captura no simulador (`systems.cfg`).
