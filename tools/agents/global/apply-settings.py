"""Mescla as chaves versionadas em ~/.claude/settings.json, sem apagar as demais."""
import json
import os
import shutil
import sys
import time

with open(sys.argv[1], encoding="utf-8") as f:
    src = json.load(f)
dst_path = os.path.join(os.path.expanduser("~"), ".claude", "settings.json")
dst = {}
if os.path.exists(dst_path):
    with open(dst_path, encoding="utf-8-sig") as f:
        dst = json.load(f)
new = dict(dst)
for key, value in src.items():
    new[key] = {**dst.get(key, {}), **value} if isinstance(value, dict) else value
if new == dst:
    print("  ok: ~/.claude/settings.json já atualizado")
else:
    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    if os.path.exists(dst_path):
        shutil.copy(dst_path, dst_path + time.strftime(".bak-%Y%m%d-%H%M%S"))
    with open(dst_path, "w", encoding="utf-8") as f:
        json.dump(new, f, indent=2, ensure_ascii=False)
    print("  ok: ~/.claude/settings.json atualizado")
