#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Ferramenta para extrair fixtures enxutas a partir de logs reais de telemetria (.csv).
Filtra colunas mantendo apenas as requeridas pelas regras de desgaste e pelo PartEventDeriver,
remove coordenadas geográficas e comprime para .csv.gz com alvo < 1 MB.
Também extrai toques do blackbox gerando <nome>.touchdowns.json.
"""

import argparse
import csv
import gzip
import json
import os
import re
import sys
from pathlib import Path

# Garante saída UTF-8 no Windows
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def load_required_sensor_prefixes(repo_root: Path) -> set:
    """Carrega os prefixos/nomes de sensores usados em wear_rules.json e PartEventDeriver."""
    sensors = set()

    # Sensores fixos operacionais do PartEventDeriver e Replay
    fixed_sensors = {
        "eng-combustion",
        "turb-is-igniting",
        "eng-starter",
        "eng-starter-active",
        "turb-master-starter-switch",
        "elec-circuit-on",
        "elec-main-bus-voltage",
        "elec-bus-voltage",
        "elec-genalt-bus-amps",
        "elec-bus-amps",
        "gear-handle-position",
        "gear-position",
        "gear-total-pct-extended",
        "af-airspeed-indicated",
        "af-airspeed-true",
        "af-g-force",
        "dmg-eng-failed",
        "dmg-crash-flag",
        "nw-is-failed",
    }
    sensors.update(fixed_sensors)

    # Carrega wear_rules.json
    wear_rules_path = repo_root / "src" / "FlightCareer.Infrastructure" / "Data" / "wear_rules.json"
    if wear_rules_path.exists():
        try:
            with open(wear_rules_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                for rule in data.get("rules", []):
                    for s in rule.get("sensors", []):
                        sensors.add(s.strip())
        except Exception as e:
            print(f"[Aviso] Falha ao ler wear_rules.json: {e}", file=sys.stderr)

    return sensors


def is_column_required(col_name: str, required_sensor_prefixes: set) -> bool:
    """Verifica se a coluna deve ser mantida na fixture."""
    col = col_name.strip()
    if not col:
        return False

    # Remove eventual BOM UTF-8 do nome da coluna
    col = col.lstrip("\ufeff")
    col_lower = col.lower()

    # Sempre mantém colunas estruturais
    if col_lower in ("utc", "sim_rate", "marker"):
        return True

    # Remove qualquer coluna explicitamente geográfica / posição
    geo_keywords = ("lat", "lon", "latitude", "longitude", "altitude-feet", "plane-alt")
    if any(k in col_lower for k in geo_keywords):
        return False

    # Extrai o prefixo base da chave (remove :1, :2, -1, -2, etc.)
    base_prefix = re.split(r"[:\-]\d+$", col)[0]

    if col in required_sensor_prefixes or base_prefix in required_sensor_prefixes:
        return True

    # Checa casamento por prefixo
    for req in required_sensor_prefixes:
        if col.startswith(req):
            return True

    return False


def extract_touchdowns_from_blackbox(blackbox_path: Path) -> list:
    """Lê linhas de TOQUE do arquivo de blackbox."""
    touchdowns = []
    if not blackbox_path or not blackbox_path.exists():
        return touchdowns

    with open(blackbox_path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            if "TOQUE:" not in line:
                continue

            fpm_m = re.search(r"TOQUE:\s*([0-9\.\-]+)\s*fpm", line, re.IGNORECASE)
            if not fpm_m:
                continue

            try:
                fpm = float(fpm_m.group(1))
            except ValueError:
                continue

            time_m = re.search(r"\[EVENTO\s+([0-9\.]+)\s*s\]", line, re.IGNORECASE)
            elapsed_sec = float(time_m.group(1)) if time_m else None

            pitch_m = re.search(r"nariz\s*([0-9\.\-]+)°", line, re.IGNORECASE)
            pitch = float(pitch_m.group(1)) if pitch_m else None

            bank_m = re.search(r"inclina[çc][ãa]o\s*([0-9\.\-]+)°", line, re.IGNORECASE)
            bank = float(bank_m.group(1)) if bank_m else None

            is_bounce = "BOUNCE" in line.upper()

            touchdowns.append({
                "elapsedSeconds": elapsed_sec,
                "fpm": fpm,
                "pitchDeg": pitch,
                "bankDeg": bank,
                "lateralFps": None,
                "isBounce": is_bounce
            })

    return touchdowns


def process_fixture(csv_path: Path, blackbox_path: Path | None, out_dir: Path, fixture_name: str | None = None) -> tuple[Path, Path, int]:
    """Cria a fixture .csv.gz e o arquivo .touchdowns.json."""
    if not csv_path.exists():
        raise FileNotFoundError(f"Arquivo CSV não encontrado: {csv_path}")

    # Determina o nome base da fixture
    if not fixture_name:
        name = csv_path.name
        if name.endswith(".csv"):
            name = name[:-4]
        # Sanitiza nome
        fixture_name = re.sub(r"[^a-zA-Z0-9_\-]", "_", name).lower()

    out_dir.mkdir(parents=True, exist_ok=True)
    out_csv_gz = out_dir / f"{fixture_name}.csv.gz"
    out_td_json = out_dir / f"{fixture_name}.touchdowns.json"

    # Encontra raiz do repositório a partir deste script
    repo_root = Path(__file__).resolve().parent.parent.parent
    prefixes = load_required_sensor_prefixes(repo_root)

    # Abre o CSV de entrada
    is_gz = str(csv_path).endswith(".gz")
    open_func = gzip.open if is_gz else open

    with open_func(csv_path, "rt", encoding="utf-8", errors="replace") as in_f:
        reader = csv.reader(in_f)
        header = next(reader, None)
        if not header:
            raise ValueError(f"CSV vazio: {csv_path}")

        # Identifica colunas a manter
        keep_indices = []
        new_header = []

        for idx, col in enumerate(header):
            clean_col = col.strip().lstrip("\ufeff")
            if is_column_required(clean_col, prefixes):
                keep_indices.append(idx)
                new_header.append(clean_col)

        # Garante que marker seja a última coluna se existir
        if "marker" in new_header:
            m_idx = new_header.index("marker")
            if m_idx != len(new_header) - 1:
                # Reordena marker para o fim
                orig_keep_m = keep_indices.pop(m_idx)
                orig_h_m = new_header.pop(m_idx)
                keep_indices.append(orig_keep_m)
                new_header.append(orig_h_m)

        # Grava .csv.gz de saída
        with gzip.open(out_csv_gz, "wt", encoding="utf-8", newline="") as out_f:
            writer = csv.writer(out_f)
            writer.writerow(new_header)

            for row in reader:
                filtered_row = [row[i] if i < len(row) else "" for i in keep_indices]
                writer.writerow(filtered_row)

    file_size = out_csv_gz.stat().st_size

    # Extrai toques
    touchdowns = []
    if blackbox_path:
        touchdowns = extract_touchdowns_from_blackbox(blackbox_path)

    with open(out_td_json, "w", encoding="utf-8") as td_f:
        json.dump(touchdowns, td_f, indent=2, ensure_ascii=False)

    return out_csv_gz, out_td_json, file_size


def main():
    parser = argparse.ArgumentParser(description="Gera fixtures compactas (.csv.gz e .touchdowns.json) a partir de logs reais.")
    parser.add_argument("csv", help="Caminho do arquivo CSV de entrada (.csv ou .csv.gz)")
    parser.add_argument("--blackbox", help="Caminho do log blackbox correspondente")
    parser.add_argument("--out", required=True, help="Diretório de saída")
    parser.add_argument("--name", help="Nome base para a fixture de saída")

    args = parser.parse_args()

    csv_path = Path(args.csv)
    blackbox_path = Path(args.blackbox) if args.blackbox else None
    out_dir = Path(args.out)

    out_csv, out_json, size_bytes = process_fixture(csv_path, blackbox_path, out_dir, args.name)

    size_kb = size_bytes / 1024.0
    size_mb = size_kb / 1024.0
    status = "OK" if size_mb < 1.0 else "AVISO: > 1MB"

    print(f"Fixture gerada:")
    print(f"  CSV.GZ: {out_csv} ({size_kb:.1f} KB / {size_mb:.2f} MB) [{status}]")
    print(f"  Toques: {out_json}")


if __name__ == "__main__":
    main()
