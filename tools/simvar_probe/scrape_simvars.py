"""Baixa as tabelas de SimVars da documentação do SDK do MSFS 2024 e gera CSV + .simvars."""
import csv, json, re, sys, urllib.request
from html.parser import HTMLParser
from pathlib import Path

BASE = "https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/"
PAGES = [
    ("Câmera", "camera-variables/"),
    ("Serviços", "services-variables/"),
    ("Diversos", "miscellaneous-variables/"),
    ("Helicóptero", "helicopter-variables/"),
    ("Balão/dirigível", "balloon-and-airship-variables/"),
    ("Piloto automático", "aircraft-simvars/aircraft-autopilot-and-assistant-variables/"),
    ("Freios e trem", "aircraft-simvars/aircraft-brake-and-landing-gear-variables/"),
    ("Controles", "aircraft-simvars/aircraft-control-variables/"),
    ("Elétrico", "aircraft-simvars/aircraft-electrics-variables/"),
    ("Motor", "aircraft-simvars/aircraft-engine-variables/"),
    ("Modelo de voo", "aircraft-simvars/aircraft-flight-model-variables/"),
    ("Combustível", "aircraft-simvars/aircraft-fuel-variables/"),
    ("Aeronave diversos", "aircraft-simvars/aircraft-miscellaneous-variables/"),
    ("Rádio e navegação", "aircraft-simvars/aircraft-radio-navigation-variables/"),
    ("Sistemas", "aircraft-simvars/aircraft-system-variables/"),
]


class P(HTMLParser):
    def __init__(self):
        super().__init__()
        self.rows, self.section = [], ""
        self.in_h = False; self.htxt = ""
        self.depth_table = 0; self.row = None; self.cell = None; self.cell_html = ""
        self.header = None

    def handle_starttag(self, tag, a):
        if tag in ("h2", "h3", "h4") and not self.depth_table:
            self.in_h = True; self.htxt = ""
        if tag == "table": self.depth_table += 1; self.header = None
        if self.depth_table == 1:
            if tag == "tr": self.row = []
            elif tag in ("td", "th") and self.row is not None: self.cell = ""; self.cell_html = ""
            elif self.cell is not None:
                cls = dict(a).get("class", "") or ""
                self.cell_html += f"<{tag} {cls}>"
                if tag in ("br", "span") and "smallfont-list" in cls: self.cell += " "
        if tag == "br" and self.cell is not None: self.cell += " "

    def handle_endtag(self, tag):
        if tag in ("h2", "h3", "h4") and self.in_h:
            self.in_h = False; self.section = re.sub(r"\s+", " ", self.htxt).strip().rstrip("#").strip()
        if self.depth_table == 1:
            if tag in ("td", "th") and self.cell is not None and self.row is not None:
                self.row.append((re.sub(r"\s+", " ", self.cell).strip(), self.cell_html)); self.cell = None
            elif tag == "tr" and self.row is not None:
                texts = [c[0] for c in self.row]
                if self.header is None: self.header = texts
                elif self.header and self.header[0].lower().startswith("simulation variable") and len(self.row) >= 4:
                    self.rows.append((self.section, self.header, self.row))
                self.row = None
        if tag == "table": self.depth_table -= 1

    def handle_data(self, d):
        if self.in_h: self.htxt += d
        if self.cell is not None: self.cell += d


def main(out_dir, watcher_dir, registry):
    out = Path(out_dir)
    wnames = set(re.findall(r'"([^"]+)"', Path(watcher_dir, "textsimvars.cs").read_text(encoding="utf-8")))
    reg = json.loads(Path(registry).read_text(encoding="utf-8"))
    regnames = {s["simVar"].split(":")[0].upper() for s in reg.get("sensors", []) if s.get("simVar")}
    recs = []
    for cat, path in PAGES:
        html = urllib.request.urlopen(BASE + path).read().decode("utf-8")
        p = P(); p.feed(html)
        for sec, hdr, row in p.rows:
            col = {h.lower(): i for i, h in enumerate(hdr)}
            def g(k):
                i = next((v for kk, v in col.items() if kk.startswith(k)), None)
                return row[i] if i is not None and i < len(row) else ("", "")
            name = g("simulation variable")[0]
            settable_html = g("settable")[1]
            name = re.sub(r"\(:?[^)]*\)", "", name)
            alias = ""
            if "alias" in name:
                name, alias = [x.strip() for x in name.split("alias", 1)]
            sec2 = "" if sec.lower().startswith("on this page") else sec
            for n in [x.strip() for x in re.split(r"\s{2,}|,", name) if x.strip()]:
                base = re.sub(r":.*$", "", n).strip()
                recs.append({
                    "categoria": cat, "secao": sec2, "simvar": n, "alias": alias,
                    "indice": (g("index")[0] if "index" in " ".join(col) else "").replace("💡︎", "").replace("💡", "").strip(),
                    "unidade": g("units")[0], "gravavel": "sim" if ("checkmark" in settable_html and "checkmark_circle_red" not in settable_html) else "",
                    "no_watcher": "sim" if base in wnames else "",
                    "no_registro": "sim" if base.upper() in regnames else "",
                    "descricao": g("description")[0][:300],
                    "url": BASE + path,
                })
    csv_path = out / "simvars-msfs2024.csv"
    with open(csv_path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(recs[0].keys())); w.writeheader(); w.writerows(recs)
    print(len(recs), "linhas;", len({r['simvar'] for r in recs}), "nomes únicos")
    print("gravaveis:", sum(1 for r in recs if r["gravavel"]),
          "| no watcher:", sum(1 for r in recs if r["no_watcher"]),
          "| fora do watcher:", sum(1 for r in recs if not r["no_watcher"]),
          "| no registro:", sum(1 for r in recs if r["no_registro"]))
    for c, _ in PAGES: print(f"  {c}: {sum(1 for r in recs if r['categoria']==c)}")
    missing = sorted(regnames - {re.sub(r':.*$','',r['simvar']).upper() for r in recs})
    print("registro sem doc:", missing)


if __name__ == "__main__":
    main(*sys.argv[1:4])
