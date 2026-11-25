# Revisão SAFRA-ESTACAO — Safra por estação nas missões agro

**Veredito: APROVADO** (2026-11-25, Claude). Commit `d7c71ee`, branch `feat/safra-estacao`.

- Escopo: só os arquivos da spec; nenhum arquivo quente; `referenceTime`, prazo, XP e pagamento intactos; `pulverizacao` segue desligada.
- Contrato: `AgroSeason` conforme a spec (hemisfério pelo sinal da latitude, fases 9–12/1–4/5–8, `WithSeasonTags` devolve a mesma instância sem data, `Note` com espaço inicial). `season` aplicado depois do teto de 12 e do fator militar (`MissionWeighting`). Com data de safra o `GenerateBoard` não usa o cache de elegíveis.
- Dados: multiplicadores de `mission_catalog.json`, tags e o tipo novo `revisao_entressafra` de `cargo_kinds.json` e o marcador `{safra}` colado ao ponto final, conforme a spec. `CargoKindCatalogTests` passou a ignorar as etiquetas `safra:*` na validação contra os perfis (correto: são virtuais).
- Build 0 erros; testes 1.154/1.154 (rodados pelo revisor no worktree); snapshot golden igual. `agy-audit`: 0 ações a conferir.
- Não testado: quadro real no app com o relógio de carreira do piloto (precisa abrir o app; não depende do MSFS).
