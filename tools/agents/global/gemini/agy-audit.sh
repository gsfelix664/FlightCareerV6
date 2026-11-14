#!/usr/bin/env bash
# Auditoria da última execução do Antigravity CLI: lista ações FORA da pasta permitida.
# Uso: agy-audit.sh <pasta-permitida> [n-ultimas-sessoes=1]
set -euo pipefail
ALLOW=${1:?pasta}; N=${2:-1}
for d in $(ls -t ~/.gemini/antigravity-cli/brain | head -n "$N"); do
  f=~/.gemini/antigravity-cli/brain/$d/.system_generated/logs/transcript.jsonl
  [ -f "$f" ] || continue
  py -3 -I - "$f" "$ALLOW" <<'PY'
import json,sys,re
f,allow=sys.argv[1],sys.argv[2].replace('/',chr(92)).lower()
n=0;sus=0
risky=re.compile(r'(reg(\.exe)? (add|delete)|Set-ItemProperty|Remove-Item|rm -rf|format |Set-ExecutionPolicy|winget|choco|npm i(nstall)? -g|Invoke-WebRequest|curl |git push|--force|\.ssh|credential|token|password)',re.I)
for line in open(f,encoding='utf-8'):
    d=json.loads(line)
    for t in d.get('tool_calls') or []:
        n+=1; a=t.get('args',{})
        v=str(a.get('CommandLine') or a.get('TargetFile') or a.get('Path') or a.get('AbsolutePath') or '')
        low=v.replace(chr(92)*2,chr(92)).lower()
        paths=[m.group(0) for m in re.finditer('[a-z]:'+re.escape(chr(92))+'[^";|]*',low)]
        outside=[p for p in paths if not p.startswith(allow) and 'antigravity-cli' not in p]
        if outside or risky.search(v):
            sus+=1; print('ATENCAO', t['name'], v[:160])
print(f'{f.split("brain")[1][1:9]}: {n} acoes, {sus} para conferir')
PY
done
