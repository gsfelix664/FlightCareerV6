# Orquestração Claude + Gemini

**Papéis:** o **Claude** planeja, especifica, revisa, aprova e faz o merge. O **Gemini** (Antigravity CLI) implementa.
O objetivo é que o trabalho pesado de código (ler arquivos, tentativa e erro, compilar) aconteça no Gemini
e o Claude gaste tokens só com especificação e revisão.

## Fluxo de uma tarefa

```
Claude: spec da tarefa ──► gemini-task.sh (worktree isolado) ──► Gemini implementa + relatório + commit
   ▲                                                                     │
   │                         CORRIGIR (máx. 2 rodadas)                    ▼
   └──────────── docs/reviews/<ID>-revisao.md ◄── Claude: review-pack.sh + diff dirigido
                                   │
                              APROVADO ──► Claude faz merge na base (FC_ALLOW_PROTECTED=1) ──► push só se o usuário pedir
```

1. **Spec** (Claude): se a tarefa do plano não tiver detalhe suficiente, o Claude escreve uma spec curta no modelo
   `docs/agents/MODELO-SPEC.md` (no scratchpad dele) e passa como 3º argumento.
2. **Execução** (Claude roda, Gemini trabalha):
   ```bash
   tools/agents/gemini-task.sh F1.1 feat/parts-f1 <spec.md>
   ```
   Roda em segundo plano num worktree em `../FLIGHTCAREERV6-gemini`. Log completo em `.git/agent-logs/`
   (o Claude só lê o resumo e o fim do log).
3. **Revisão** (Claude):
   ```bash
   tools/agents/review-pack.sh F1.1 feat/parts-f1
   ```
   Commits, arquivos, arquivos quentes, alertas automáticos, relatório, build e testes, tudo enxuto.
   Depois o Claude lê **só** o diff dos arquivos que importam (e os arquivos quentes inteiros).
4. **Veredito**: `docs/reviews/<ID>-revisao.md` com APROVADO ou CORRIGIR (lista numerada). Erro recorrente vira
   uma linha em `docs/agents/LICOES.md`.
5. **Merge** (Claude, após APROVADO): `FC_ALLOW_PROTECTED=1 git merge --no-ff feat/parts-f1` na base.
   Push no remoto só quando o usuário pedir.

## Travas (hooks em `.githooks/`)

Ative uma vez por clone: `git config core.hooksPath "$(pwd)/.githooks"` (caminho absoluto para valer nos worktrees).

| Hook | Bloqueia |
|---|---|
| `pre-commit` / `pre-merge-commit` | commit em `main` e `feat/component-health-system`; commit do `flightcareer.db` |
| `commit-msg` | mensagem fora de `tipo(escopo): descrição` |
| `pre-push` | push nas branches protegidas, `push --force`, apagar branch remota |

`FC_ALLOW_PROTECTED=1` libera as branches protegidas. Só o Claude (no merge aprovado) ou o usuário usam.
Hooks não substituem revisão: são a rede de segurança para o modo não interativo.

## Boas práticas (vibe coding com agentes)

**Especificação antes do código**
- Tarefa pequena: idealmente < 300 linhas de diff e 1 objetivo. Tarefa grande vira subtarefas (F2.3a, F2.3b).
- Toda tarefa tem **critérios de aceite verificáveis** (teste X passa, método Y existe, campo Z no JSON).
- Diga **o que não fazer** (arquivos fora do escopo, nada de refatorar vizinhos).
- Aponte arquivos e símbolos exatos. Agente que precisa adivinhar inventa.

**Testes como contrato**
- Para lógica de Domain, o Claude pode escrever o nome e o cenário dos testes na spec; o Gemini implementa até passarem.
- Build verde não é aprovação. Teste que só verifica que "não lança exceção" não conta.

**Executor e revisor separados**
- Quem escreve não aprova o próprio código. O `@reviewer` do Gemini é autochecagem; a aprovação é do Claude.
- Revisão por checklist (abaixo), não "olhar geral".

**Contexto enxuto**
- Instruções estáveis ficam em arquivos (`AGENTS.md`, `INSTRUCOES-GEMINI.md`, `LICOES.md`), não repetidas no prompt.
- Um worktree por executor; sessão nova por tarefa (contexto limpo, sem lixo de tarefas antigas).
- Logs longos ficam em arquivo; o revisor lê resumo.

**Limites e escalonamento**
- No máximo **2 rodadas de CORRIGIR** por tarefa. Na 3ª, o Claude corrige direto ou reescreve a spec e pergunta ao usuário.
- Se o Gemini parar com BLOQUEADO, o Claude responde a dúvida na revisão; não deixe o agente "chutar".
- Portões 🔒 e tudo que depende do MSFS: decisão do usuário.

**Histórico reversível**
- Um commit por tarefa, branch por fase, merge `--no-ff`: qualquer tarefa pode ser revertida sozinha.
- Nunca `reset --hard`, `push --force` ou apagar branches em modo automático.

**Aprender com os erros**
- Cada CORRIGIR com causa recorrente vira uma regra curta em `LICOES.md`, que o Gemini lê em toda tarefa.

## Checklist de revisão do Claude

1. Escopo: só o que a tarefa pede? Arquivos fora do escopo?
2. Critérios de aceite: cada um verificado de verdade (teste ou leitura), não só marcado no relatório.
3. Regras de `AGENTS.md` › 5 e 6: `SimTelemetry`/`AddToDef` em ordem e só no fim; eventos/SimVars do SDK; `IRandomSource`;
   probabilidade por tempo e tempo simulado; calibração no JSON; `IDbContextFactory`; schema (`ALTER`/`CREATE IF NOT EXISTS` + teste de banco antigo); sem `catch {}`.
4. Testes novos testam comportamento (valores esperados), incluindo caso de borda.
5. Build 0 erros, testes 100%.
6. CHANGELOG e diário do plano atualizados; "não testado no MSFS" declarado.

## Economia de tokens (Claude)

- Rodar o Gemini em segundo plano e esperar a notificação: não acompanhar o log.
- `review-pack.sh` primeiro; `git diff` só por arquivo; arquivos inteiros só os quentes.
- Revisões de tarefas simples podem ir para um subagente em modelo mais barato; 🔒 e arquitetura ficam com o Claude principal.
- Spec curta e precisa custa menos do que duas rodadas de correção.
