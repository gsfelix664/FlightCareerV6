"""Monta os bancos de locais usados pelo gerador de missões (Infrastructure/Data/places/).

Fontes abertas (nada inventado):
  - Pontos turísticos: Wikidata (CC0), itens com coordenada, por classe (P31) e popularidade (nº de sitelinks).
  - Escolas de voo, aeroclubes e zonas de paraquedismo: OpenStreetMap (ODbL, atribuição em ATTRIBUTION.md).
  - Aeródromos: src/FlightCareer.Infrastructure/CSV/airports.csv (OurAirports, domínio público).

Uso:
  python tools/places/build_places.py --cache <pasta de downloads> [--download] [--lnm <little_navmap_msfs24.sqlite>]
Sem --download, reaproveita os arquivos já baixados na pasta de cache.
Com --lnm (recomendado), cada aeródromo usa o código que existe no simulador (ex.: L65 e não KL65, OMDU para a
Skydive Dubai) e aeródromos que não existem no simulador ficam de fora.
Zonas de salto curadas à mão: tools/places/dropzones_curated.csv (icao, name, cc, note); se o código não existir
no simulador, usa o aeródromo do simulador mais próximo do mesmo tipo (até 30 NM).
Só biblioteca padrão.
"""
import argparse
import csv
import glob
import json
import math
import os
import re
import sys
import time
import urllib.parse
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
AIRPORTS_CSV = os.path.join(ROOT, "src", "FlightCareer.Infrastructure", "CSV", "airports.csv")
CURATED_DZ_CSV = os.path.join(ROOT, "tools", "places", "dropzones_curated.csv")
CURATED_SCENIC_CSV = os.path.join(ROOT, "tools", "places", "scenic_aerodromes_curated.csv")
HUBS_JSON = os.path.join(ROOT, "src", "FlightCareer.Infrastructure", "Data", "hubs_data.json")
OUT_DIR = os.path.join(ROOT, "src", "FlightCareer.Infrastructure", "Data", "places")
UA = "FlightCareerDataBuilder/1.0"

# Classe Wikidata -> (tipo, sitelinks mínimos no mundo). No Brasil o mínimo é 2.
POI_CLASSES = {
    "Q570116": ("atracao", 5), "Q4989906": ("monumento", 8), "Q8502": ("montanha", 25),
    "Q34038": ("cachoeira", 4), "Q39715": ("farol", 5), "Q23413": ("castelo", 8),
    "Q12280": ("ponte", 10), "Q46169": ("parque_nacional", 5), "Q23442": ("ilha", 12),
    "Q12323": ("barragem", 8), "Q2977": ("catedral", 8), "Q483110": ("estadio", 12),
    "Q23397": ("lago", 15), "Q11303": ("arranha_ceu", 10), "Q8072": ("vulcao", 5),
    "Q44782": ("porto", 8), "Q40080": ("praia", 5), "Q9259": ("patrimonio_unesco", 3),
    "Q16970": ("igreja", 12), "Q33506": ("museu", 15), "Q2319498": ("marco_arquitetonico", 3),
    "Q22698": ("parque", 30), "Q179049": ("reserva_natural", 10), "Q35509": ("caverna", 5),
    "Q185113": ("cabo", 5), "Q207326": ("pico", 5),
}

SCHOOL_NAME = re.compile(r"aero ?clu|flying club|flight school|flight training|aviation academy|flugschule|"
                         r"segelflug|gliding|aéro-club|escola de avia|aeroescola", re.I)
DZ_NAME = re.compile(r"skydiv|paraquedis|parachut|fallschirm|parachutisme|paracaid", re.I)
OSM_SCHOOLS_Q = ('[out:json][timeout:175];(nwr["club"~"^(flying|aviation|aeroclub|gliding)$"];'
                 'nwr["amenity"="flight_school"];nwr["school"~"flight|aviation"];);out center tags;')
OSM_DZ_Q = ('[out:json][timeout:170];(nwr["sport"="parachuting"];nwr["parachuting"];);out center tags;')


def log(msg):
    print(msg, file=sys.stderr)


def fetch(url, data=None, headers=None, retries=4):
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, data=data, headers={"User-Agent": UA, **(headers or {})})
            with urllib.request.urlopen(req, timeout=180) as r:
                return r.read()
        except Exception as ex:  # rede instável: registra e tenta de novo
            log(f"  falha ({ex}); nova tentativa em {20 * (attempt + 1)} s")
            time.sleep(20 * (attempt + 1))
    raise RuntimeError(f"não foi possível baixar {url}")


def sparql(query):
    body = urllib.parse.urlencode({"query": query}).encode()
    return fetch("https://query.wikidata.org/sparql", body, {"Accept": "application/sparql-results+json"})


def overpass(query):
    return fetch("https://overpass-api.de/api/interpreter", urllib.parse.urlencode({"data": query}).encode())


def download(cache):
    os.makedirs(os.path.join(cache, "poi"), exist_ok=True)
    for cls, (kind, minimum) in POI_CLASSES.items():
        for scope, filt, mn in (("w", "", minimum), ("br", "wdt:P17 wd:Q155 ;", 2)):
            path = os.path.join(cache, "poi", f"{scope}_{cls}.json")
            if os.path.exists(path):
                continue
            q = (f"SELECT ?i ?pt ?en ?c ?s ?cc WHERE {{ ?i wdt:P31 wd:{cls} ; {filt} wikibase:sitelinks ?s . "
                 f"FILTER(?s >= {mn}) ?i wdt:P625 ?c . OPTIONAL{{?i rdfs:label ?pt FILTER(lang(?pt)='pt')}} "
                 f"OPTIONAL{{?i rdfs:label ?en FILTER(lang(?en)='en')}} OPTIONAL{{?i wdt:P17 ?k . ?k wdt:P297 ?cc}} }}")
            log(f"Wikidata {scope} {kind}")
            open(path, "wb").write(sparql(q))
            time.sleep(2)
    for name, q in (("osm_schools.json", OSM_SCHOOLS_Q), ("osm_dz.json", OSM_DZ_Q)):
        path = os.path.join(cache, name)
        if not os.path.exists(path):
            log(f"OSM {name}")
            open(path, "wb").write(overpass(q))


def nm(lat1, lon1, lat2, lon2):
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dp, dl = p2 - p1, math.radians(lon2 - lon1)
    a = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * 3440.065 * math.asin(math.sqrt(a))


def load_sim_idents(lnm_path):
    """Códigos de aeródromo que existem no simulador (base do Little Navmap), com coordenadas."""
    if not lnm_path:
        return None
    import sqlite3
    con = sqlite3.connect(f"file:{lnm_path}?mode=ro", uri=True)
    try:
        return {row[0].upper(): (row[1], row[2], row[3]) for row in
                con.execute("select ident, laty, lonx, name from airport where ident is not null")}
    finally:
        con.close()


def load_airports(sim=None):
    out = []
    for r in csv.DictReader(open(AIRPORTS_CSV, encoding="utf-8")):
        if r["type"] not in ("small_airport", "medium_airport", "large_airport"):
            continue
        candidates = [c.strip().upper() for c in (r["icao_code"], r["gps_code"], r["ident"], r["local_code"]) if c and c.strip()]
        if sim is not None:
            icao = next((c for c in candidates if c in sim), None)
            if icao is None:
                continue  # não existe no simulador
        else:
            icao = next((c for c in candidates if re.fullmatch(r"[A-Z0-9]{3,4}", c)), None)
            if icao is None:
                continue  # idents tipo "BR-0086" não existem no banco do simulador
        out.append({"icao": icao, "name": r["name"], "lat": float(r["latitude_deg"]), "lon": float(r["longitude_deg"]),
                    "cc": r["iso_country"], "kw": r["keywords"] or "", "type": r["type"],
                    "aliases": set(candidates)})
    return out


class Grid:
    """Índice espacial simples (células de 1°) para achar o aeródromo mais próximo."""

    def __init__(self, airports):
        self.cells = {}
        for a in airports:
            self.cells.setdefault((int(math.floor(a["lat"])), int(math.floor(a["lon"]))), []).append(a)

    def nearest(self, lat, lon, max_nm):
        best, best_d = None, max_nm
        ci, cj = int(math.floor(lat)), int(math.floor(lon))
        for i in (ci - 1, ci, ci + 1):
            for j in (cj - 1, cj, cj + 1):
                for a in self.cells.get((i, j), ()):
                    d = nm(lat, lon, a["lat"], a["lon"])
                    if d <= best_d:
                        best, best_d = a, d
        return best, best_d


def osm_elements(path):
    if not os.path.exists(path):
        log(f"AVISO: {path} ausente")
        return []
    for e in json.load(open(path, encoding="utf-8")).get("elements", []):
        lat = e.get("lat", e.get("center", {}).get("lat"))
        lon = e.get("lon", e.get("center", {}).get("lon"))
        if lat is not None and lon is not None:
            yield e.get("tags", {}), lat, lon, f"osm:{e['type']}/{e['id']}"


def build_pois(cache):
    pois = {}
    for path in glob.glob(os.path.join(cache, "poi", "*.json")):
        cls = os.path.basename(path).split("_", 1)[1][:-5]
        if cls not in POI_CLASSES:
            continue
        kind = POI_CLASSES[cls][0]
        try:
            rows = json.load(open(path, encoding="utf-8"))["results"]["bindings"]
        except (ValueError, KeyError) as ex:
            log(f"AVISO: {path} inválido ({ex})")
            continue
        is_br = os.path.basename(path).startswith("br_")
        for b in rows:
            qid = b["i"]["value"].rsplit("/", 1)[-1]
            m = re.match(r"Point\(([-\d.eE]+) ([-\d.eE]+)\)", b["c"]["value"])
            name = (b.get("pt") or b.get("en") or {}).get("value")
            if not m or not name or re.fullmatch(r"Q\d+", name):
                continue
            cc = b.get("cc", {}).get("value") or ("BR" if is_br else "")
            pop = int(b["s"]["value"])
            cur = pois.get(qid)
            if cur is None or pop > cur["p"]:
                pois[qid] = {"id": qid, "n": name, "lat": round(float(m.group(2)), 5),
                             "lon": round(float(m.group(1)), 5), "k": kind, "cc": cc, "p": pop}
    return sorted(pois.values(), key=lambda x: (x["cc"], -x["p"], x["id"]))


def build_training(airports, grid, cache):
    hubs = json.load(open(HUBS_JSON, encoding="utf-8-sig"))["HubProfiles"]
    by_icao = {a["icao"]: a for a in airports}
    found = {}

    def add(a, source, detail):
        e = found.setdefault(a["icao"], {"icao": a["icao"], "name": a["name"], "cc": a["cc"], "sources": []})
        if source not in e["sources"]:
            e["sources"].append(source)
        if detail and "school" not in e:
            e["school"] = detail

    for prof in hubs.values():
        for icao in prof.get("InstructionHubs", []):
            if icao in by_icao:
                add(by_icao[icao], "hubs_data", None)
    for a in airports:
        if SCHOOL_NAME.search(a["name"]) or SCHOOL_NAME.search(a["kw"]):
            add(a, "ourairports-nome", None)
    for tags, lat, lon, _ in osm_elements(os.path.join(cache, "osm_schools.json")):
        a, _d = grid.nearest(lat, lon, 3.0)
        if a:
            add(a, "osm", tags.get("name"))
    return sorted(found.values(), key=lambda x: (x["cc"], x["icao"]))


def raw_airport(code):
    """Linha do OurAirports com este código (mesmo que não exista no simulador), para achar um substituto."""
    for r in csv.DictReader(open(AIRPORTS_CSV, encoding="utf-8")):
        if code in {c.strip().upper() for c in (r["icao_code"], r["gps_code"], r["ident"], r["local_code"]) if c}:
            return {"lat": float(r["latitude_deg"]), "lon": float(r["longitude_deg"]), "type": r["type"]}
    return None


def nearest_same_type(airports, lat, lon, kind, max_nm=30.0):
    best, best_d = None, max_nm
    for a in airports:
        if a["type"] == kind:
            d = nm(lat, lon, a["lat"], a["lon"])
            if d <= best_d:
                best, best_d = a, d
    return best


def add_curated_dropzones(found, airports, sim=None):
    """Lista curada pelo usuário: tem prioridade sobre o OSM (nome e nota).
    Código ausente do simulador -> aeródromo do simulador mais próximo do mesmo tipo (até 30 NM)."""
    if not os.path.exists(CURATED_DZ_CSV):
        return
    by_alias = {}
    for a in airports:
        for alias in a["aliases"] | {a["icao"]}:
            by_alias.setdefault(alias, a)
    for row in csv.DictReader(open(CURATED_DZ_CSV, encoding="utf-8")):
        code = row["icao"].strip().upper()
        a = by_alias.get(code)
        substitute_for = None
        if a is None and sim is not None and code in sim:
            lat, lon, name = sim[code]
            a = {"icao": code, "name": name, "lat": lat, "lon": lon, "cc": row["cc"], "type": "", "aliases": {code}}
        if a is None:
            raw = raw_airport(code)
            a = nearest_same_type(airports, raw["lat"], raw["lon"], raw["type"]) if raw else None
            if a is None:
                log(f"AVISO: {code} ({row['name']}) sem aeródromo no simulador a até 30 NM; ignorado")
                continue
            substitute_for = code
            log(f"  {code} não existe no simulador: usando {a['icao']} ({a['name']})")
        elif a["icao"] != code:
            log(f"  {code} -> {a['icao']} (código do simulador)")
        e = {"icao": a["icao"], "airport": a["name"], "cc": row["cc"] or a["cc"], "name": row["name"],
             "lat": round(a["lat"], 5), "lon": round(a["lon"], 5), "sources": ["curado"], "note": row["note"]}
        if substitute_for:
            e["substituteFor"] = substitute_for
        old = found.get(a["icao"])
        if old:
            e["sources"] += [src for src in old["sources"] if src != "curado"]
        found[a["icao"]] = e


def build_dropzones(airports, grid, cache, sim=None):
    found = {}
    for tags, lat, lon, ref in osm_elements(os.path.join(cache, "osm_dz.json")):
        a, d = grid.nearest(lat, lon, 3.0)
        if not a:
            continue  # sem aeródromo próximo não dá para gerar a decolagem
        e = found.setdefault(a["icao"], {"icao": a["icao"], "airport": a["name"], "cc": a["cc"],
                                         "name": tags.get("name") or a["name"], "lat": round(lat, 5),
                                         "lon": round(lon, 5), "sources": []})
        if "osm" not in e["sources"]:
            e["sources"].append("osm")
    for a in airports:
        if DZ_NAME.search(a["name"]) or DZ_NAME.search(a["kw"]):
            e = found.setdefault(a["icao"], {"icao": a["icao"], "airport": a["name"], "cc": a["cc"], "name": a["name"],
                                             "lat": round(a["lat"], 5), "lon": round(a["lon"], 5), "sources": []})
            if "ourairports-nome" not in e["sources"]:
                e["sources"].append("ourairports-nome")
    add_curated_dropzones(found, airports, sim)
    return sorted(found.values(), key=lambda x: (x["cc"], x["icao"]))



def _norm(text):
    import unicodedata
    t = unicodedata.normalize("NFKD", text or "").encode("ascii", "ignore").decode().lower()
    return re.sub(r"[^a-z0-9 ]", " ", t).split()


def _wikidata_lookup(name, cache):
    """Busca o nome no Wikidata (pt e en) e devolve [(id, rótulo, lat, lon)] dos resultados com coordenada."""
    path = os.path.join(cache, "scenic_lookup.json")
    memo = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
    if name not in memo:
        found = []
        for lang in ("pt", "en"):
            url = ("https://www.wikidata.org/w/api.php?action=wbsearchentities&format=json&limit=7&language="
                   f"{lang}&search={urllib.parse.quote(name)}")
            ids = [x["id"] for x in json.loads(fetch(url)).get("search", [])]
            if not ids:
                continue
            ent = json.loads(fetch("https://www.wikidata.org/w/api.php?action=wbgetentities&format=json&props=claims|labels"
                                   f"&languages=pt|en&ids={'|'.join(ids)}"))["entities"]
            for qid, e in ent.items():
                for c in e.get("claims", {}).get("P625", []):
                    v = c.get("mainsnak", {}).get("datavalue", {}).get("value")
                    if v:
                        label = (e.get("labels", {}).get("pt") or e.get("labels", {}).get("en") or {}).get("value", name)
                        found.append([qid, label, v["latitude"], v["longitude"]])
                        break
            time.sleep(1)
        memo[name] = found
        json.dump(memo, open(path, "w", encoding="utf-8"), ensure_ascii=False)
    return memo[name]


def build_scenic(airports, pois, cache, sim=None, max_nm=75.0):
    """Aeródromos turísticos curados; cada atração vira um ponto com coordenada real (POI do banco ou Wikidata),
    aceito só se estiver a até 75 NM do aeródromo (rotas de flightseeing reais chegam a ~60 NM, ex.: Talkeetna–Denali). Atração sem coordenada confiável fica de fora (logada)."""
    if not os.path.exists(CURATED_SCENIC_CSV):
        return []
    by_alias = {}
    for a in airports:
        for alias in a["aliases"] | {a["icao"]}:
            by_alias.setdefault(alias, a)
    out = []
    for row in csv.DictReader(open(CURATED_SCENIC_CSV, encoding="utf-8")):
        code = row["icao"].strip().upper()
        a = by_alias.get(code)
        if a is None and sim is not None and code in sim:
            lat, lon, nm_ = sim[code]
            a = {"icao": code, "name": nm_, "lat": lat, "lon": lon}
        if a is None:
            log(f"AVISO: {code} não existe no simulador; ignorado")
            continue
        points = []
        for attr in [x.strip() for x in row["attractions"].split("|") if x.strip()]:
            words = set(_norm(attr))
            best = None
            for p in pois:
                d = nm(a["lat"], a["lon"], p["lat"], p["lon"])
                if d <= max_nm and words and words <= set(_norm(p["n"])):
                    if best is None or p["p"] > best[1]["p"]:
                        best = (d, p)
            if best:
                p = best[1]
                points.append({"n": attr, "lat": p["lat"], "lon": p["lon"], "src": p["id"]})
                continue
            cands = [(nm(a["lat"], a["lon"], la, lo), q, lab, la, lo) for q, lab, la, lo in _wikidata_lookup(attr, cache)]
            cands = [c for c in cands if c[0] <= max_nm]
            if cands:
                d, q, lab, la, lo = min(cands)
                points.append({"n": attr, "lat": round(la, 5), "lon": round(lo, 5), "src": q})
                if d > 40:
                    log(f"  {code}: '{attr}' a {d:.0f} NM ({lab})")
            else:
                log(f"  {code}: '{attr}' sem coordenada a até {max_nm:.0f} NM; ignorado")
        out.append({"icao": a["icao"], "name": row["name"], "cc": row["cc"], "note": row["note"], "points": points})
    return out

def write(name, key, items, note):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    doc = {"version": 1, "generatedBy": "tools/places/build_places.py", "note": note, key: items}
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
        f.write("\n")
    log(f"{name}: {len(items)} itens, {os.path.getsize(path) // 1024} KB")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cache", required=True)
    ap.add_argument("--download", action="store_true")
    ap.add_argument("--lnm", help="base little_navmap_msfs24.sqlite para conferir os códigos do simulador")
    args = ap.parse_args()
    if args.download:
        download(args.cache)
    sim = load_sim_idents(args.lnm)
    if sim is None:
        log("AVISO: sem --lnm, os códigos não são conferidos com o simulador")
    airports = load_airports(sim)
    grid = Grid(airports)
    pois = build_pois(args.cache)
    write("tourist_pois.json", "pois", pois,
          "Wikidata (CC0). k = tipo, cc = país ISO, p = nº de sitelinks (popularidade).")
    write("training_aerodromes.json", "aerodromes", build_training(airports, grid, args.cache),
          "hubs_data (curado) + nome do aeródromo no OurAirports + escolas/aeroclubes do OpenStreetMap (ODbL) a ≤ 3 NM.")
    write("dropzones.json", "dropzones", build_dropzones(airports, grid, args.cache, sim),
          "OpenStreetMap (ODbL) sport=parachuting a ≤ 3 NM de um aeródromo + nome do aeródromo no OurAirports. "
          "Altitude de salto não vem da fonte: regra do gerador por classe da aeronave.")
    write("scenic_aerodromes.json", "aerodromes", build_scenic(airports, pois, args.cache, sim),
          "Lista curada (tools/places/scenic_aerodromes_curated.csv); coordenadas das atrações do Wikidata (CC0), "
          "aceitas só a até 75 NM do aeródromo.")


if __name__ == "__main__":
    main()
