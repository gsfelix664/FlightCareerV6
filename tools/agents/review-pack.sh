#!/usr/bin/env bash
# Pacote compacto para o revisor: commits, arquivos, relatório, build e testes (saída enxuta).
# Uso: tools/agents/review-pack.sh <ID> <branch> [base]
#   --no-build   pula build e testes
set -euo pipefail
ID=${1:?ID}; BRANCH=${2:?branch}; BASE=${3:-feat/component-health-system}
NOBUILD=0; for a in "$@"; do [ "$a" = "--no-build" ] && NOBUILD=1; done
REPO=$(git rev-parse --show-toplevel)
WT=${FC_GEMINI_WORKTREE:-$(dirname "$REPO")/$(basename "$REPO")-gemini}
[ -d "$WT" ] || WT=$REPO
MB=$(git -C "$WT" merge-base "$BASE" "$BRANCH")

echo "=== Commits ($BASE..$BRANCH)"; git -C "$WT" log --oneline "$MB..$BRANCH"
echo; echo "=== Arquivos"; git -C "$WT" diff --stat "$MB..$BRANCH" | tail -40
HOT='ComponentFlightSession.cs|SimFailureActuator.cs|component_catalog.json|FlightMonitor.cs|SimConnectService.cs|SimTelemetry.cs|AppDbContext.cs|App.xaml.cs|MainViewModel.cs|BriefingViewModel.cs'
HITS=$(git -C "$WT" diff --name-only "$MB..$BRANCH" | grep -E "$HOT" || true)
[ -n "$HITS" ] && { echo; echo "=== ARQUIVOS QUENTES (ler inteiros)"; echo "$HITS"; }
echo; echo "=== Alertas automáticos (linhas adicionadas)"
git -C "$WT" diff -U0 "$MB..$BRANCH" -- '*.cs' | grep -nE '^\+.*(new Random\(|catch\s*(\(\w*\))?\s*\{\s*\}|Thread\.Sleep|\.Result\b|async void|MapClientEventToSimEvent)' || echo "(nenhum)"
REL="$WT/docs/reviews/$ID-relatorio.md"
echo; echo "=== Relatório"; [ -f "$REL" ] && cat "$REL" || echo "SEM RELATÓRIO"
if [ $NOBUILD = 0 ]; then
  echo; echo "=== Build"
  (cd "$WT" && dotnet build src/FlightCareer.slnx -nologo -v q -clp:NoSummary 2>&1 | grep -E 'error|Build succeeded|Compilação|erro' | tail -15) || true
  echo "=== Testes"
  (cd "$WT" && dotnet test src/FlightCareer.Tests --nologo -v q 2>&1 | grep -E 'Failed|Falha|Passed!|Aprovado|Total|error' | tail -15) || true
fi
echo; echo "Diff completo: git -C \"$WT\" diff $MB..$BRANCH -- <arquivo>"
