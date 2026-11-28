# Spec AGENTS-ENXUTO — enxugar a seção 7 do AGENTS.md

## 1. Objetivo
Reduzir `AGENTS.md` › 7 ("Estado atual dos sistemas", linhas 101–336, ~62 KB) para **no máximo 22 KB**, mantendo só o **estado atual** de cada sistema. O `AGENTS.md` é relido em toda chamada de todo agente: cada KB cortado economiza quota. O histórico detalhado já está no `docs/CHANGELOG.md`.

## 2. Regras de corte
- Fica, para cada sistema: o que ele faz (1–2 linhas); **onde está** (arquivos/classes principais, com caminho); **regras que um agente precisa saber para não quebrar** (contratos, fonte da verdade, o que é calibrado em JSON e em qual arquivo); decisões do usuário que ainda valem (com a data); pendências e "não testado no MSFS".
- Sai: números de calibração que estão nos JSON (taxas, pesos, faixas, tabelas de pagamento, valores por classe — cite o arquivo JSON em vez dos valores); descrição passo a passo de algoritmos (cite a classe); IDs de tarefa repetidos em vários parágrafos (mantenha o ID uma vez, entre parênteses, para achar no CHANGELOG); fórmulas que estão no código; listas de subtipos/IDs que estão no catálogo; texto duplicado entre blocos.
- **Não corte nada da § 6** ("Regras técnicas que já causaram bugs") nem das §§ 1–5 e 8. Só a § 7.
- Não invente nem mude fatos: só resuma o que está escrito. Se dois trechos se contradizem, mantenha o mais recente (o bloco que vem depois ou que cita data maior) e liste a contradição no relatório.
- Agrupe por sistema com subtítulos `###` curtos (ex.: Desgaste e falhas; Peças; Manutenção e Hangar; Panes persistentes; Voo e integridade; Combustível; Economia e dificuldade; XP e fases; Missões e quadro; Aeroportos e tiers; Limites por aeronave; Sensores; HUD). Blocos hoje separados por tarefa (FASES-2A, FASES-2B, FASES-2C, MISSOES-LOCAIS, E2A, TIER-A, E2C-1...) viram um só bloco do sistema.
- Termine a § 7 com uma linha: "Histórico detalhado de cada tarefa: `docs/CHANGELOG.md` (busque pelo ID entre parênteses)."
- Português, mesmo tom do arquivo.

## 3. Arquivos
- Mexer: só `AGENTS.md` (§ 7) e `docs/CHANGELOG.md` (entrada nova no fim).
- **Fora do escopo:** código, testes, qualquer outro documento.

## 4. Critérios de aceite
1. `AGENTS.md` › 7 ≤ 22 KB (medir com `awk '/^## 7\./{f=1} /^## 8\./{f=0} f' AGENTS.md | wc -c`); §§ 1–6 e 8 byte a byte iguais (conferir com `git diff`).
2. Relatório com: tamanho antes/depois; lista dos sistemas cobertos; lista de **todos** os arquivos/classes citados antes que não são mais citados (para o revisor conferir que nada importante sumiu); contradições encontradas.
3. Entrada no `docs/CHANGELOG.md`: `## 2026-11-24 - AGENTS.md › 7 enxuto (AGENTS-ENXUTO) (Gemini)`.
4. Não rodar build nem testes (só documentação).
5. Um commit: `docs(agents): enxuga o estado dos sistemas no AGENTS (AGENTS-ENXUTO)`.
