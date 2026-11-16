"""Junta os report.csv do SimVarProbe numa matriz variável × rodada e cruza com o registro e as regras.

Uso: python tools/simvar_probe/build_matrix.py <saida.csv> <resumo.md> <pasta_rodada> [<pasta_rodada> ...]
Só biblioteca padrão. Rodar a partir da raiz do repositório.
"""
import csv, json, sys
from pathlib import Path

DATA = Path("src/FlightCareer.Infrastructure/Data")


def load_report(folder):
    with open(Path(folder) / "report.csv", encoding="utf-8-sig") as f:
        return {r["slot"]: r for r in csv.DictReader(f)}


def short(status):
    return {"AceitaVariando": "varia", "AceitaConstante": "constante", "Rejeitada": "rejeitada",
            "Erro": "erro", "SemResposta": "sem-resposta", "AceitaSemAmostras": "sem-amostras"}.get(status, status)


def final_status(statuses):
    present = [s for s in statuses if s]
    if not present: return "nao-sondada"
    if "varia" in present: return "varia"
    if "rejeitada" in present: return "rejeitada"
    if "constante" in present: return "constante"
    return present[0]


def main(out_csv, out_md, *folders):
    reports = [(Path(f).name, load_report(f)) for f in folders]
    sensors = json.loads((DATA / "sensor_registry.json").read_text(encoding="utf-8"))["sensors"]
    rules = json.loads((DATA / "wear_rules.json").read_text(encoding="utf-8"))["rules"]
    used_by = {}
    for r in rules:
        for k in r.get("sensors", []):
            used_by.setdefault(k, []).append(r["id"])

    slot_to_key = {}
    for s in sensors:
        slot = s["simVar"] + (":1" if s.get("perEngine") else "")
        slot_to_key[slot] = s["key"]

    slots = []
    for _, rep in reports:
        for k in rep:
            if k not in slots: slots.append(k)

    rows = []
    for slot in slots:
        sts = [short(rep[slot]["status"]) if slot in rep else "" for _, rep in reports]
        mins = [float(rep[slot]["min"]) for _, rep in reports if slot in rep and rep[slot]["min"] not in ("", None)]
        maxs = [float(rep[slot]["max"]) for _, rep in reports if slot in rep and rep[slot]["max"] not in ("", None)]
        any_rep = next(rep[slot] for _, rep in reports if slot in rep)
        key = slot_to_key.get(slot, "")
        rows.append({
            "slot": slot, "simvar": any_rep["simvar"], "unit": any_rep["unit"], "category": any_rep["category"],
            **{name: st for (name, _), st in zip(reports, sts)},
            "final": final_status(sts),
            "min": min(mins) if mins else "", "max": max(maxs) if maxs else "",
            "registro": key, "regras": ";".join(used_by.get(key, [])),
        })

    with open(out_csv, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)

    reg_rows = [r for r in rows if r["registro"]]
    missing = [s for s in slot_to_key if s not in {r["slot"] for r in rows}]
    lines = ["# Matriz SimVarProbe — " + ", ".join(n for n, _ in reports), "",
             f"- Slots: {len(rows)}; variam: {sum(r['final']=='varia' for r in rows)}; "
             f"constantes: {sum(r['final']=='constante' for r in rows)}; rejeitadas: {sum(r['final']=='rejeitada' for r in rows)}.",
             f"- Sensores do registro encontrados na sondagem: {len(reg_rows)} de {len(slot_to_key)} "
             f"(fora da lista da sonda: {len(missing)}).", "",
             "## Sensores usados em regras de desgaste que NÃO variaram", "",
             "| Slot | Chave | Status | Regras |", "|---|---|---|---|"]
    for r in reg_rows:
        if r["regras"] and r["final"] != "varia":
            lines.append(f"| `{r['slot']}` | `{r['registro']}` | {r['final']} | {r['regras'].replace(';', ', ')} |")
    lines += ["", "## Sensores usados em regras que variaram (validados neste avião)", ""]
    lines.append(", ".join(f"`{r['registro']}`" for r in reg_rows if r["regras"] and r["final"] == "varia"))
    lines += ["", "## Sensores do registro que não entraram na sondagem", ""]
    lines.append(", ".join(f"`{s}`" for s in missing) or "(nenhum)")
    Path(out_md).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{len(rows)} slots; registro {len(reg_rows)}/{len(slot_to_key)}")


if __name__ == "__main__":
    main(*sys.argv[1:])
