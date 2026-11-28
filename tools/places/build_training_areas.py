"""Monta o banco de áreas de instrução e acrobacia reais do DECEA (Infrastructure/Data/places/training_areas.json).

Fonte:
  - DECEA GeoAISWEB (AIP Brasil), camada WFS ICA:airspace.

Uso:
  python tools/places/build_training_areas.py --cache <pasta de downloads>
Se decea_airspace.json já existir na pasta de cache, reutiliza o download.
"""
import argparse
import json
import math
import os
import re
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "src", "FlightCareer.Infrastructure", "Data", "places")

WFS_URL = (
    "https://geoaisweb.decea.mil.br/geoserver/ows?"
    "service=WFS&version=2.0.0&request=GetFeature&typeNames=ICA:airspace&outputFormat=application/json"
)

EXCLUDE_WORDS = [
    "VOO LIVRE", "PARAPENTE", "PARAMOTOR", "ULTRALEVE", "PARAQUED", "BALONISMO", "PLANADOR",
    "CAMPO DE INSTRU", "MIL ACFT"
]
EXCLUDE_IDENTS = {"SBR801", "SBP818", "SBR841", "SBR842"}

INCL_NAME_REGEX = re.compile(r"INSTRU|AEROCLUBE|ACROBACIA|ACRO\b", re.IGNORECASE)
ACRO_REGEX = re.compile(r"ACROBACIA|ACRO\b", re.IGNORECASE)


def log(msg):
    print(f"[training_areas] {msg}")


def fetch_decea(cache_dir):
    os.makedirs(cache_dir, exist_ok=True)
    cache_file = os.path.join(cache_dir, "decea_airspace.json")
    if os.path.exists(cache_file):
        log(f"Reaproveitando cache existente: {cache_file} ({os.path.getsize(cache_file)} bytes)")
        with open(cache_file, "rb") as f:
            raw = f.read()
    else:
        log(f"Baixando espaços aéreos do DECEA GeoAISWEB...")
        req = urllib.request.Request(WFS_URL, headers={"User-Agent": "FlightCareer/6.0"})
        with urllib.request.urlopen(req, timeout=120) as resp:
            raw = resp.read()
        with open(cache_file, "wb") as f:
            f.write(raw)
        log(f"Salvo em cache: {len(raw)} bytes")

    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        text = raw.decode("latin-1")

    return json.loads(text)


def distance_nm(lat1, lon1, lat2, lon2):
    d_lat = math.radians(lat2 - lat1)
    d_lon = math.radians(lon2 - lon1)
    a = math.sin(d_lat / 2.0) ** 2 + math.cos(math.radians(lat1)) * math.cos(math.radians(lat2)) * math.sin(d_lon / 2.0) ** 2
    c = 2.0 * math.atan2(math.sqrt(a), math.sqrt(1.0 - a))
    return 6371.0 * c / 1.852


def fix_mojibake(s):
    if not s or not isinstance(s, str):
        return s or ""
    if "Ã" in s or "Â" in s:
        try:
            return s.encode("latin-1").decode("utf-8")
        except Exception:
            pass
    return s


def build_training_areas(geojson_data):
    features = geojson_data.get("features", [])
    areas = []

    for feat in features:
        props = feat.get("properties", {})
        typ = (props.get("typ") or "").strip().upper()
        ident = (props.get("ident") or "").strip().upper()
        nam = fix_mojibake(props.get("nam") or "").strip()
        txtrmk = fix_mojibake(props.get("txtrmk") or "").strip()

        if typ not in ("R", "D"):
            continue
        if ident in EXCLUDE_IDENTS:
            continue
        if props.get("part") or re.search(r"^[A-Z]{3}\d+[A-Z]+$", ident):
            continue

        combined = f"{nam} {txtrmk}".upper()
        if any(ex in combined for ex in EXCLUDE_WORDS):
            continue
        if "MIL ACFT" in txtrmk.upper():
            continue
        if "CAMPO DE INSTRUÇÃO" in nam.upper() or "CAMPO DE INSTRUCAO" in nam.upper():
            continue

        civ_training = "CIV ACFT TRAINING" in txtrmk.upper() or "INSTRUCTIONAL FLIGHTS" in txtrmk.upper()
        matches_name = bool(INCL_NAME_REGEX.search(nam))
        if not (civ_training or matches_name):
            continue

        kind = "acrobacia" if ACRO_REGEX.search(nam) else "instrucao"

        # Geometria e centroide simples dos vértices do anel externo
        geom = feat.get("geometry") or {}
        gtype = geom.get("type")
        coords = geom.get("coordinates") or []
        if not coords:
            continue

        if gtype == "Polygon":
            ring = coords[0]
        elif gtype == "MultiPolygon":
            ring = coords[0][0]
        else:
            continue

        if not ring:
            continue

        lats = [pt[1] for pt in ring]
        lons = [pt[0] for pt in ring]
        c_lat = sum(lats) / len(lats)
        c_lon = sum(lons) / len(lons)
        max_d = max(distance_nm(c_lat, c_lon, lat, lon) for lat, lon in zip(lats, lons))
        radius_nm = max(0.5, round(max_d, 1))

        # Altitudes (piso e teto)
        p_fl = props.get("lowerlimi1")
        p_fl_ref = props.get("codedistv1")
        t_lim = props.get("upperlimit")
        t_uni = props.get("uplimituni")
        t_ref = props.get("codedistve")

        floor_ft = int(round(float(p_fl))) if p_fl is not None else 0
        floor_ref = "SFC" if p_fl_ref == "SFC" else "MSL"

        if t_uni == "FL":
            ceil_ft = int(round(float(t_lim) * 100)) if t_lim is not None else 0
            ceil_ref = "MSL"
        else:
            ceil_ft = int(round(float(t_lim))) if t_lim is not None else 0
            ceil_ref = "MSL" if t_ref in ("MSL", "STD") else ("SFC" if t_ref == "SFC" else "MSL")

        # Nome final
        clean_name = nam.rstrip(".").strip()
        if clean_name == "CAPIVARI":
            clean_name = "Capivari"

        areas.append({
            "id": ident,
            "name": clean_name,
            "kind": kind,
            "lat": round(c_lat, 5),
            "lon": round(c_lon, 5),
            "radiusNm": radius_nm,
            "floorFt": floor_ft,
            "floorRef": floor_ref,
            "ceilFt": ceil_ft,
            "ceilRef": ceil_ref
        })

    areas.sort(key=lambda a: a["id"])
    return areas


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cache", required=True, help="Pasta de cache para decea_airspace.json")
    args = ap.parse_args()

    geojson_data = fetch_decea(args.cache)
    areas = build_training_areas(geojson_data)

    os.makedirs(OUT_DIR, exist_ok=True)
    out_file = os.path.join(OUT_DIR, "training_areas.json")
    doc = {
        "version": 1,
        "generatedBy": "tools/places/build_training_areas.py",
        "note": "DECEA (AIP Brasil) WFS GeoAISWEB ICA:airspace",
        "areas": areas
    }

    with open(out_file, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
        f.write("\n")

    log(f"training_areas.json gravado: {len(areas)} áreas ({os.path.getsize(out_file)} bytes)")
    inst = [a for a in areas if a["kind"] == "instrucao"]
    acro = [a for a in areas if a["kind"] == "acrobacia"]
    log(f"Total: {len(inst)} instrução, {len(acro)} acrobacia")
    for a in areas:
        log(f"  {a['id']} ({a['kind']}): {a['name']} | centro=({a['lat']},{a['lon']}) r={a['radiusNm']}NM | {a['floorFt']} {a['floorRef']} -> {a['ceilFt']} {a['ceilRef']}")


if __name__ == "__main__":
    main()
