#!/usr/bin/env bash
# Consulta SOMENTE LEITURA ao Gemini (Antigravity CLI) — análise ampla de código sem gastar contexto do Claude.
# Uso: agy-ask.sh "<pergunta, pode citar @caminho/>" [pasta]   (pasta padrão: diretório atual)
# Saída: só a resposta final, cortada em FC_ASK_MAXLINES (padrão 80) linhas.
set -euo pipefail
Q=${1:?pergunta}; DIR=${2:-.}
MAX=${FC_ASK_MAXLINES:-80}
RULES="Responda em português, no máximo $MAX linhas. Seja factual: cite caminho:linha para cada afirmação. Não edite arquivos. Se não encontrar, diga que não encontrou."
cd "$DIR"
timeout "${FC_ASK_TIMEOUT:-600}" agy -p "$Q

$RULES" --mode plan --print-timeout "${FC_ASK_TIMEOUT:-600}s" ${FC_GEMINI_MODEL:+--model "$FC_GEMINI_MODEL"} 2>&1 | head -n "$MAX"
