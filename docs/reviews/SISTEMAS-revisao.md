# Revisão SISTEMAS — instruções sob demanda

**Veredito: APROVADO** (com ajustes feitos pelo revisor no próprio branch). Revisor: Claude, 2026-11-25.

## Conferido
- `AGENTS.md`: 39.707 → 14.951 bytes (meta ≤ 15.000). §§ 1–8 mantêm a numeração; § 7 virou índice de 13 sistemas.
- `docs/sistemas/`: 13 arquivos + README. Cobertura mecânica (`comm -23` das linhas do AGENTS original × AGENTS novo + `docs/sistemas/*.md` + `ORQUESTRACAO.md`): os 13 blocos da § 7 e os textos longos da § 6 (MSFS nativo, combustível, eventos de falha) e da § 4.9/4.10 estão literais no destino. Só faltaram cabeçalhos e linhas encurtadas no lugar (§ 4 itens 5, 7, 8, 11; § 6 Frequências, Key Events, SimVar inválida, Banco de dados), conferidas uma a uma.
- Referências ajustadas: `CLAUDE.md`, `gemini.md`, `.antigravity/rules.md`, `MODELO-SPEC.md`, `prompt-executor.md`, `INSTRUCOES-GEMINI.md`. Nenhuma "› 7" antiga restante nesses arquivos.

## Ajustes feitos pelo revisor (fatos perdidos ao encurtar)
1. Key Events: voltaram a prioridade `SIMCONNECT_GROUP_PRIORITY_DEFAULT` e o roteamento `OnObservedEvent` × `OnMaskedEvent` (evento pertence a um só grupo).
2. Banco: a regra "nunca altere entidade de outro contexto esperando que o seu `SaveChanges` a grave (copie os valores)" estava truncada sem sentido; restaurada. Voltou "com os nomes e tipos que o EF geraria" e "teste de banco antigo".
3. SimVar inválida: voltou "o marshaling lê além do fim".
4. Para caber em 15 KB: a 3ª coluna do índice ficou com 2 itens por sistema; "(o sim reposiciona o avião no ar)" saiu do AGENTS (está literal em `desgaste-falhas.md`).
