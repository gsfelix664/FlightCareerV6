#!/usr/bin/env bash
# Delega uma tarefa de IMPLEMENTAÇÃO ao Gemini (Antigravity CLI) num git worktree isolado.
# Uso: agy-task.sh <ID> <branch> <spec.md> [base]
#   base padrão: branch atual do repositório. Worktree: ../<repo>-gemini (FC_GEMINI_WORKTREE muda).
# O Gemini commita na <branch> (sem push) e escreve o relatório em docs/reviews/<ID>-relatorio.md.
# Saída: resumo curto (commits, arquivos, status, fim da resposta). Log completo em <git-dir>/agent-logs/.
set -euo pipefail
ID=${1:?ID}; BRANCH=${2:?branch}; SPEC=${3:?spec.md}
REPO=$(git rev-parse --show-toplevel)
BASE=${4:-$(git -C "$REPO" symbolic-ref --short HEAD)}
COMMON=$(git -C "$REPO" rev-parse --path-format=absolute --git-common-dir)
WT=${FC_GEMINI_WORKTREE:-$(dirname "$REPO")/$(basename "$REPO")-gemini}
LOGDIR="$COMMON/agent-logs"; mkdir -p "$LOGDIR"; LOG="$LOGDIR/$ID-$(date +%Y%m%d-%H%M%S).log"
[ "$BRANCH" = "$BASE" ] && { echo "branch da tarefa não pode ser a base" >&2; exit 2; }
case "$BRANCH" in main|master) echo "branch protegida" >&2; exit 2;; esac

if [ ! -e "$WT/.git" ]; then
  if git -C "$REPO" show-ref -q --verify "refs/heads/$BRANCH"; then git -C "$REPO" worktree add -q "$WT" "$BRANCH"
  else git -C "$REPO" worktree add -q -b "$BRANCH" "$WT" "$BASE"; fi
else
  [ -n "$(git -C "$WT" status --porcelain)" ] && { echo "worktree $WT com alterações pendentes:" >&2; git -C "$WT" status --short >&2; exit 3; }
  if git -C "$REPO" show-ref -q --verify "refs/heads/$BRANCH"; then git -C "$WT" checkout -q "$BRANCH"
  else git -C "$WT" checkout -q -b "$BRANCH" "$BASE"; fi
fi
START=$(git -C "$WT" rev-parse HEAD)

read -r -d '' PROMPT <<P || true
Você é o EXECUTOR. O Claude é o revisor. Modo não interativo: ninguém responde perguntas.
Tarefa: $ID · Branch: $BRANCH (já ativa; não troque de branch, não faça merge nem push).

1. Leia as instruções do projeto que existirem: AGENTS.md, GEMINI.md/gemini.md, docs/agents/LICOES.md, e o que a spec citar.
2. Se existir docs/reviews/$ID-revisao.md com CORRIGIR, faça SOMENTE as correções pedidas.
3. Investigue só o necessário, implemente só a spec, rode build e testes do projeto.
4. Escreva docs/reviews/$ID-relatorio.md: plano, arquivos alterados, critérios de aceite (como verificou), build/testes, não testado, dúvidas.
5. Commits pequenos no padrão do projeto. Nunca --amend, --no-verify, reset --hard, push.
6. Faltou informação: registre em "Dúvidas" no relatório, commite o que for seguro e PARE.
Responda no fim em até 10 linhas: PRONTO/BLOQUEADO, commits, build, testes, dúvidas.

SPEC:
$(cat "$SPEC")
P

echo "agy: $ID em $WT ($BRANCH, base $BASE). Log: $LOG"
set +e
(cd "$WT" && agy -p "$PROMPT" --dangerously-skip-permissions --print-timeout "${FC_GEMINI_TIMEOUT:-90m}" ${FC_GEMINI_MODEL:+--model "$FC_GEMINI_MODEL"}) >"$LOG" 2>&1
RC=$?; set -e
echo "=== $ID terminou (código $RC)"
git -C "$WT" log --oneline "$START..HEAD"
git -C "$WT" diff --stat "$START..HEAD" | tail -25
git -C "$WT" status --short | head -10
[ -f "$WT/docs/reviews/$ID-relatorio.md" ] && echo "relatório: docs/reviews/$ID-relatorio.md" || echo "SEM relatório"
tail -n 15 "$LOG"
exit $RC
