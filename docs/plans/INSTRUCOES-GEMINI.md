# Instruções para o Gemini (Antigravity CLI) — execução do plano de peças

Você é o **executor** do plano `docs/plans/pecas-desgaste-falhas.md`. O **Claude** é o revisor e orquestrador.
Normalmente o Claude te chama em modo não interativo (`tools/agents/gemini-task.sh`), num worktree isolado
(`../FLIGHTCAREERV6-gemini`) com a branch da tarefa já ativa. O fluxo completo está em `docs/agents/ORQUESTRACAO.md`.
Siga este arquivo à risca.

## 1. Antes de começar (toda sessão)

1. Leia por completo: `AGENTS.md`, `gemini.md`, `docs/agents/LICOES.md`, `docs/plans/pecas-desgaste-falhas.md`, `docs/plans/sensor-inventory.md`.
2. Rode `git status` e `git log --oneline -5`. Se houver alterações que não são suas, **pare**: em modo interativo pergunte ao usuário; em modo não interativo registre no relatório e encerre.
3. Veja no "Diário de execução" do plano qual é a **próxima tarefa**. Leia em `docs/reviews/` a última revisão do Claude: se ela pediu correções, a próxima tarefa é **corrigir**, não avançar.

## 2. Branch e commits

- Uma branch por fase, criada a partir de `feat/component-health-system`: `feat/parts-f1`, `feat/parts-f2`, `feat/parts-f3`, `feat/parts-f4`, `feat/parts-f5`.
- **Um commit por tarefa**: `feat(parts): F1.1 registro de sensores` (tipo, escopo `parts` ou `sensors`, ID da tarefa, descrição em português). Correções pedidas na revisão: `fix(parts): F1.1 ajustes da revisão`.
- Você **pode** commitar na branch da fase. Push só em modo interativo e se o usuário pedir; quando o Claude te chama, **não faça push**.
- Você **não pode**: commitar ou fazer push no `main` ou na `feat/component-health-system`, fazer merge, trocar de branch, `push --force`, `reset --hard`, apagar branches, apagar o banco do usuário ou arquivos que não criou.
- Hooks do git (`.githooks/`) bloqueiam branch protegida, mensagem fora do padrão e force push. Se um hook bloquear, **corrija a causa**: nunca use `--no-verify` e nunca defina `FC_ALLOW_PROTECTED`.
- Tarefa grande (> ~300 linhas de diff): divida em commits por subtarefa e avise no relatório.

## 3. Ciclo de cada tarefa

1. **Investigar:** leia os arquivos citados na tarefa e os vizinhos. Confirme nomes de classes, métodos e SimVars (só use SimVars do `sensor-inventory.md`).
2. **Planejar:** escreva no relatório (passo 5) em 3 a 6 linhas o que vai fazer, antes de codar.
3. **Implementar** só o que a tarefa pede. Nada de refatorar o que não está na tarefa.
4. **Verificar (`@reviewer`):**
   ```bash
   dotnet build src/FlightCareer.slnx
   dotnet test src/FlightCareer.Tests
   ```
   Os dois precisam passar. Confira cada critério de aceite da tarefa.
5. **Relatório de revisão:** crie `docs/reviews/<ID>-relatorio.md` usando o modelo da seção 4.
6. **Commit e push** da branch da fase. Acrescente uma linha no "Diário de execução" do plano e uma entrada no fim do `docs/CHANGELOG.md` (`## AAAA-MM-DD - <ID> <título> (Gemini)`).
7. **Pare.** Diga ao usuário: *"Tarefa <ID> pronta para revisão do Claude: docs/reviews/<ID>-relatorio.md, branch <nome>, commit <hash>."*
   - Tarefas com 🔒: **não comece a próxima** até existir `docs/reviews/<ID>-revisao.md` com **APROVADO**.
   - Tarefas sem 🔒: pode seguir para a próxima tarefa da mesma fase, mas cada uma tem seu relatório.

## 4. Modelo do relatório (`docs/reviews/<ID>-relatorio.md`)

```markdown
# <ID> — <título da tarefa>

**Branch:** feat/parts-fX · **Commit:** <hash curto> · **Data:** AAAA-MM-DD

## Plano (antes de codar)
- ...

## O que foi feito
- arquivo: o que mudou e por quê

## Critérios de aceite
- [x] critério 1 — como verifiquei
- [ ] critério 2 — por que não foi possível

## Build e testes
- dotnet build: 0 erros, N avisos novos
- dotnet test: X aprovados, 0 falhas (novos: lista de testes)

## Não testado
- o que depende do MSFS ou da tela do app

## Dúvidas e decisões para o revisor
- ...
```

## 5. Como o Claude responde

O Claude grava `docs/reviews/<ID>-revisao.md` com:
- **APROVADO** — pode seguir; ou
- **CORRIGIR** — lista numerada de correções. Faça **somente** essas correções, atualize o mesmo relatório com uma seção "Correções", commit `fix(parts): <ID> ajustes da revisão`, e pare de novo.

Se você discordar de uma correção, escreva o motivo técnico na seção "Dúvidas" e pare. Não ignore.

## 6. Tarefas que dependem do usuário no simulador

Em F1.4 e F4.3 quem voa é o usuário. Sua parte: preparar o gravador, o roteiro (copie do plano para `docs/plans/sensor-validation/ROTEIRO.md` em formato de checklist) e o script de análise. Depois do voo, o usuário te passa os CSVs; você roda o script, gera `docs/plans/sensor-validation/<tipo>-<modelo>.md` e para para revisão.

## 7. Regras que mais derrubam tarefas (leia de novo antes de cada commit)

- Lógica de regra em `FlightCareer.Domain`, com teste. A UI só chama.
- Números de calibração no JSON, não no código.
- `IRandomSource`, nunca `new Random()` na lógica.
- Tempo simulado (`SIMULATION RATE`), chance por intervalo de tempo.
- `IDbContextFactory<AppDbContext>`, contexto curto. Tabela nova: `DbSet` + `CREATE TABLE IF NOT EXISTS` + teste em banco antigo.
- Nunca `catch {}` vazio.
- Nunca apague nem reescreva código antigo do sistema de componentes antes da Fase 5.
- Na dúvida, **pergunte no relatório e pare**. Errar parado é barato; errar avançando é caro.

## 8. Prompt para iniciar uma sessão no Antigravity

Cole isto no Gemini ao abrir a sessão:

> Leia `docs/plans/INSTRUCOES-GEMINI.md` e siga-o. Execute a próxima tarefa pendente do plano `docs/plans/pecas-desgaste-falhas.md` (veja o Diário de execução e as revisões em `docs/reviews/`). Ao terminar a tarefa, gere o relatório e pare.
