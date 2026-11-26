# HANGAR-AVISOS — revisão (Claude, 2026-11-26)

**Veredito: APROVADO** (1 rodada). Commit `62fc495` na branch `feat/hangar-avisos`.

- Diff de `Domain` e `UI` lido inteiro; em `MainViewModel` (quente) só a linha do diálogo NO-GO.
- E3: só os textos usam `ApplyCost`; os valores passados a `RunPaidServiceAsync` seguem brutos (sem desconto em dobro).
- E4: botão da pane passa `Defect` ao comando; `Cause` gravada não mudou (`DefectPartLink` continua funcionando).
- E1: a faixa de oficina só aparece quando o cartão de status não é "OFICINA".
- Build 0 erros; 1.267/1.267 testes. `agy-audit`: 84 ações, 0 para conferir.
- **Não conferido com o app aberto** (layout, tooltip, valores nos botões).
- Fora do escopo, mantidos: preço por galão sem o ajuste de dificuldade; itens da confirmação em valor bruto, seguidos da linha de desconto.
