"""Monta a lista de entrada do SimVarProbe: documentação 2024 ∪ lista interna do SimvarWatcher."""
import csv, re, sys
from pathlib import Path

doc_csv, watcher_dir, out = sys.argv[1:4]
t = Path(watcher_dir, "textsimvars.cs").read_text(encoding="utf-8")
def arr(n):
    i = t.find(n + " = new string[]"); return re.findall(r'"([^"]*)"', t[i:t.find("};", i)])
wunit = dict(zip(arr("Names"), arr("DefaultUnits")))
wunit.pop("NONE", None)

def kind(idx):
    s = idx.lower()
    if not s or s in ("n/a", "-", "none"): return "none"
    if "first" in s and "second" in s: return "multi"
    if "engine" in s: return "engine"
    if "contact point" in s: return "contact"
    if "component id" in s: return "wear"
    if "cover" in s: return "cover"
    return "generic"

rows, seen = [], set()
for r in csv.DictReader(open(doc_csv, encoding="utf-8-sig")):
    n = r["simvar"].strip()
    if n in seen or not re.fullmatch(r"[A-Z0-9 _]+", n): continue
    seen.add(n)
    unit_doc = r["unidade"]
    is_str = unit_doc.lower().startswith("string")
    unit = "" if is_str else (wunit.get(n) or "number")
    rows.append({"simvar": n, "unit": unit, "isString": "true" if is_str else "false",
                 "indexKind": kind(r["indice"]), "source": "doc" + ("+watcher" if n in wunit else ""),
                 "category": r["categoria"], "settable": "true" if r["gravavel"] else "false"})
for n, u in sorted(wunit.items()):
    if n in seen: continue
    seen.add(n)
    rows.append({"simvar": n, "unit": "" if u.lower() == "string" else u,
                 "isString": "true" if u.lower() == "string" else "false",
                 "indexKind": "none", "source": "watcher", "category": "", "settable": ""})
with open(out, "w", newline="", encoding="utf-8") as f:
    w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)
from collections import Counter
print(len(rows), Counter(r["indexKind"] for r in rows), Counter(r["source"] for r in rows), "strings", sum(r["isString"] == "true" for r in rows))
