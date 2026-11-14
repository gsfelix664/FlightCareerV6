#!/usr/bin/env bash
# Roda o Gemini (Antigravity CLI) numa tarefa, isolado num git worktree.
# Uso: tools/agents/gemini-task.sh <ID> <branch> [arquivo-spec.md]
# Ex.: tools/agents/gemini-task.sh F1.1 feat/parts-f1 /caminho/spec-F1.1.md
# Variáveis: FC_BASE (branch base, padrão feat/component-health-system),
#            FC_GEMINI_WORKTREE (pasta do worktree), FC_GEMINI_MODEL, FC_GEMINI_TIMEOUT (padrão 90m),
#            FC_GEMINI_CLI (agy | gemini; padrão agy).
set -euo pipefail

ID=${1:?informe o ID da tarefa (ex.: F1.1)}
BRANCH=${2:?informe a branch (ex.: feat/parts-f1)}
SPEC_FILE=${3:-}

REPO=$(git rev-parse --show-toplevel)
COMMON=$(cd "$REPO" && git rev-parse --path-format=absolute --git-common-dir)
BASE=${FC_BASE:-feat/component-health-system}
WT=${FC_GEMINI_WORKTREE:-$(dirname "$REPO")/$(basename "$REPO")-gemini}
CLI=${FC_GEMINI_CLI:-agy}
TIMEOUT=${FC_GEMINI_TIMEOUT:-90m}
LOGDIR="$COMMON/agent-logs"
mkdir -p "$LOGDIR"
LOG="$LOGDIR/$ID-$(date +%Y%m%d-%H%M%S).log"

case "$BRANCH" in
  main|feat/component-health-system) echo "Branch protegida: use uma branch de tarefa." >&2; exit 2 ;;
esac

# 1. Worktree isolado (o checkout principal fica livre para o usuário e o revisor).
if [ ! -d "$WT/.git" ] && [ ! -f "$WT/.git" ]; then
  if git -C "$REPO" show-ref --verify --quiet "refs/heads/$BRANCH"; then
    git -C "$REPO" worktree add "$WT" "$BRANCH"
  elif git -C "$REPO" show-ref --verify --quiet "refs/remotes/origin/$BRANCH"; then
    git -C "$REPO" worktree add -b "$BRANCH" "$WT" "origin/$BRANCH"
  else
    git -C "$REPO" worktree add -b "$BRANCH" "$WT" "$BASE"
  fi
else
  if [ -n "$(git -C "$WT" status --porcelain)" ]; then
    echo "O worktree $WT tem alterações não commitadas. Revise antes de rodar outra tarefa." >&2
    git -C "$WT" status --short >&2
    exit 3
  fi
  if git -C "$REPO" show-ref --verify --quiet "refs/heads/$BRANCH"; then
    git -C "$WT" checkout -q "$BRANCH"
  else
    git -C "$WT" checkout -q -b "$BRANCH" "$BASE"
  fi
fi

START=$(git -C "$WT" rev-parse HEAD)

# 2. Prompt a partir do modelo.
SPEC="(nenhuma)"
[ -n "$SPEC_FILE" ] && SPEC=$(cat "$SPEC_FILE")
TEMPLATE=$(cat "$REPO/tools/agents/prompt-executor.md")
PROMPT=${TEMPLATE//\{\{ID\}\}/$ID}
PROMPT=${PROMPT//\{\{BRANCH\}\}/$BRANCH}
PROMPT=${PROMPT//\{\{SPEC\}\}/$SPEC}

# 3. Execução não interativa. Os hooks do git (core.hooksPath) barram branch protegida, push e force.
echo "Executando $ID em $WT (branch $BRANCH) com $CLI. Log: $LOG"
set +e
if [ "$CLI" = "gemini" ]; then
  (cd "$WT" && gemini -p "$PROMPT" --approval-mode yolo ${FC_GEMINI_MODEL:+-m "$FC_GEMINI_MODEL"}) >"$LOG" 2>&1
else
  (cd "$WT" && agy -p "$PROMPT" --dangerously-skip-permissions --print-timeout "$TIMEOUT" \
      ${FC_GEMINI_MODEL:+--model "$FC_GEMINI_MODEL"}) >"$LOG" 2>&1
fi
RC=$?
set -e

# 4. Resumo curto para o revisor (não despeja o log inteiro).
echo "=== $ID terminou (código $RC) ==="
echo "--- commits novos"
git -C "$WT" log --oneline "$START..HEAD" || true
echo "--- arquivos"
git -C "$WT" diff --stat "$START..HEAD" | tail -25 || true
echo "--- não commitado"
git -C "$WT" status --short | head -20
if [ -f "$WT/docs/reviews/$ID-relatorio.md" ]; then echo "--- relatório: docs/reviews/$ID-relatorio.md"; else echo "--- SEM relatório"; fi
echo "--- fim do log ($LOG)"
tail -n 15 "$LOG"
exit $RC
