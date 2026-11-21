#!/usr/bin/env bash
# Configura uma máquina nova para a divisão Claude (revisor) + Gemini/Antigravity (executor).
# Uso (Git Bash, na raiz do repositório): tools/agents/install-global.sh
#   - instala ~/.claude/CLAUDE.md (instruções globais, valem para todos os projetos)
#   - instala ~/.claude/tools/gemini/ (agy-ask, agy-task, agy-audit)
#   - ativa os hooks de git deste repositório (core.hooksPath)
#   - confere se o agy está instalado e respondendo
# Arquivos existentes são guardados com sufixo .bak-<data> antes de serem substituídos.
set -euo pipefail
REPO=$(git rev-parse --show-toplevel)
SRC="$REPO/tools/agents/global"
STAMP=$(date +%Y%m%d-%H%M%S)

backup() { [ -e "$1" ] && ! cmp -s "$1" "$2" && cp "$1" "$1.bak-$STAMP" && echo "  backup: $1.bak-$STAMP" || true; }

echo "1) Instruções globais do Claude"
mkdir -p ~/.claude/tools/gemini
backup ~/.claude/CLAUDE.md "$SRC/CLAUDE.md"
cp "$SRC/CLAUDE.md" ~/.claude/CLAUDE.md
for f in "$SRC"/gemini/*.sh; do
  backup ~/.claude/tools/gemini/"$(basename "$f")" "$f"
  cp "$f" ~/.claude/tools/gemini/
done
chmod +x ~/.claude/tools/gemini/*.sh
echo "  ok: ~/.claude/CLAUDE.md e ~/.claude/tools/gemini/"

echo "2) Hooks de git deste repositório"
git -C "$REPO" config core.hooksPath "$REPO/.githooks"
echo "  ok: core.hooksPath = $(git -C "$REPO" config core.hooksPath)"

echo "3) Antigravity CLI (agy)"
if command -v agy >/dev/null 2>&1; then
  if timeout 120 agy -p "Responda apenas: OK" --mode plan --print-timeout 100s 2>/dev/null | grep -q OK; then
    echo "  ok: agy responde"
  else
    echo "  ATENÇÃO: agy instalado mas não respondeu. Abra 'agy' uma vez para fazer login e rode este script de novo."
  fi
else
  echo "  ATENÇÃO: agy não encontrado no PATH. Instale o Antigravity (inclui o CLI 'agy'), faça login e rode de novo."
fi

echo "4) Python (usado pela auditoria e pelos scripts de revisão)"
command -v py >/dev/null 2>&1 && echo "  ok: $(py -3 --version)" || echo "  ATENÇÃO: 'py' não encontrado (instale o Python 3 para Windows)."

echo "Pronto. Abra o Claude Code na pasta do projeto: ele lê ~/.claude/CLAUDE.md, CLAUDE.md e AGENTS.md."
