#!/bin/sh
# Branches que só o revisor (Claude) ou o usuário alteram.
# Liberação pontual: FC_ALLOW_PROTECTED=1 (o Gemini NUNCA define esta variável).
PROTECTED_BRANCHES="main feat/component-health-system"

is_protected() {
  for b in $PROTECTED_BRANCHES; do
    [ "$1" = "$b" ] && return 0
  done
  return 1
}
