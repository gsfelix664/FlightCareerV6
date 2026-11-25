"""Monta o banco de rotas aéreas regulares (Infrastructure/Data/places/airline_routes.json).

Fontes abertas:
  - Rotas comerciais: OpenFlights routes.dat (2014-06), ODbL (https://openflights.org/data).
  - Códigos e aeródromos: src/FlightCareer.Infrastructure/CSV/airports.csv (OurAirports, domínio público).

Uso:
  python tools/places/build_routes.py --cache <pasta de downloads> [--download] [--lnm <little_navmap_msfs24.sqlite>]

Sem --download, reaproveita o routes.dat já baixado na pasta de cache.
Com --lnm (opcional), aeródromos são restritos aos existentes no simulador.
Só biblioteca padrão.
"""
import argparse
import csv
import json
import os
import re
import sys
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

from build_places import load_airports, load_sim_idents, AIRPORTS_CSV, OUT_DIR

OPENFLIGHTS_ROUTES_URL = "https://raw.githubusercontent.com/jpatokal/openflights/master/data/routes.dat"
UA = "FlightCareerDataBuilder/1.0"


def log(msg):
    print(msg, file=sys.stderr)


def download_routes(cache_dir):
    os.makedirs(cache_dir, exist_ok=True)
    target = os.path.join(cache_dir, "routes.dat")
    log(f"Baixando OpenFlights routes.dat para {target}...")
    req = urllib.request.Request(OPENFLIGHTS_ROUTES_URL, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=180) as resp:
        data = resp.read()
    with open(target, "wb") as f:
        f.write(data)
    log(f"Download concluído ({len(data)} bytes).")


def build_iata_mapping(valid_icaos, sim):
    iata_to_icao = {}
    iata_rank = {}
    type_rank = {"large_airport": 3, "medium_airport": 2, "small_airport": 1}

    for r in csv.DictReader(open(AIRPORTS_CSV, encoding="utf-8")):
        t = r["type"]
        if t not in type_rank:
            continue
        iata = (r["iata_code"] or "").strip().upper()
        if len(iata) != 3:
            continue

        candidates = [c.strip().upper() for c in (r["icao_code"], r["gps_code"], r["ident"], r["local_code"]) if c and c.strip()]
        if sim is not None:
            icao = next((c for c in candidates if c in sim), None)
        else:
            icao = next((c for c in candidates if re.fullmatch(r"[A-Z0-9]{3,4}", c)), None)

        if not icao or icao not in valid_icaos:
            continue

        rank = type_rank[t]
        if iata not in iata_to_icao or rank > iata_rank[iata]:
            iata_to_icao[iata] = icao
            iata_rank[iata] = rank

    return iata_to_icao


def main():
    parser = argparse.ArgumentParser(description="Gera airline_routes.json a partir do OpenFlights routes.dat.")
    parser.add_argument("--cache", required=True, help="Pasta de cache para armazenar routes.dat")
    parser.add_argument("--download", action="store_true", help="Baixa routes.dat da web se especificado")
    parser.add_argument("--lnm", help="Caminho do banco SQLite do Little Navmap (opcional)")
    args = parser.parse_args()

    routes_path = os.path.join(args.cache, "routes.dat")
    if args.download or not os.path.exists(routes_path):
        download_routes(args.cache)

    sim = load_sim_idents(args.lnm)
    if sim is None:
        log("AVISO: sem --lnm, os códigos não são conferidos com o banco do simulador.")

    airports = load_airports(sim)
    valid_icaos = {a["icao"] for a in airports}
    iata_to_icao = build_iata_mapping(valid_icaos, sim)

    def map_code(code):
        c = (code or "").strip().upper()
        if len(c) == 3:
            return iata_to_icao.get(c)
        if len(c) == 4 and c in valid_icaos:
            return c
        return None

    read_count = 0
    disc_codeshare = 0
    disc_stops = 0
    disc_same = 0
    disc_unmapped = 0

    pair_airlines = {}

    with open(routes_path, "r", encoding="utf-8", errors="replace") as f:
        reader = csv.reader(f)
        for row in reader:
            read_count += 1
            if len(row) < 9:
                disc_unmapped += 1
                continue

            airline, airline_id, src, src_id, dst, dst_id, codeshare, stops, equip = [x.strip() for x in row[:9]]

            if codeshare == "Y":
                disc_codeshare += 1
                continue

            if stops != "0":
                disc_stops += 1
                continue

            src_icao = map_code(src)
            dst_icao = map_code(dst)

            if not src_icao or not dst_icao:
                disc_unmapped += 1
                continue

            if src_icao == dst_icao:
                disc_same += 1
                continue

            pair = (min(src_icao, dst_icao), max(src_icao, dst_icao))
            al = airline_id if airline_id != r"\N" and airline_id != "" else airline
            pair_airlines.setdefault(pair, set()).add(al)

    routes = {}
    for (a, b), als in sorted(pair_airlines.items()):
        n = len(als)
        routes.setdefault(a, {})[b] = n
        routes.setdefault(b, {})[a] = n

    routes_sorted = {k: {dst: count for dst, count in sorted(v.items())} for k, v in sorted(routes.items())}

    doc = {
        "version": 1,
        "generatedBy": "tools/places/build_routes.py",
        "note": "OpenFlights routes.dat (2014-06), ODbL. n = nº de companhias distintas na rota (sem codeshare, sem escalas).",
        "routes": routes_sorted
    }

    os.makedirs(OUT_DIR, exist_ok=True)
    out_file = os.path.join(OUT_DIR, "airline_routes.json")
    with open(out_file, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
        f.write("\n")

    size_kb = os.path.getsize(out_file) // 1024

    print(f"Linhas lidas: {read_count}")
    print(f"Descartadas por codeshare: {disc_codeshare}")
    print(f"Descartadas por escalas: {disc_stops}")
    print(f"Descartadas por mesma origem/destino: {disc_same}")
    print(f"Descartadas por código não mapeado: {disc_unmapped}")
    print(f"Pares únicos: {len(pair_airlines)}")
    print(f"Tamanho do arquivo: {size_kb} KB ({out_file})")


if __name__ == "__main__":
    main()
