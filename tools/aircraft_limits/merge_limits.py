#!/usr/bin/env python3
"""
merge_limits.py — Consolidação do catálogo de limites operacionais de aeronaves.
Processa logs de captura (.csv + -context.json) e valores de POH (poh_values.csv)
atualizando os JSONs de limites em Data/aircraft aplicando prioridade estrita de fontes.
"""

import argparse
import csv
import json
import os
import re
import statistics
import sys

SOURCE_PRIORITY = {
    "POH": 1,
    "SimVar": 2,
    "Cfg": 3,
    "NativeOnset": 4,
    "ClassDefault": 5
}

SIMVAR_KEY_MAPPING = {
    "af-ref-speed-max-ias": ("vne", "knots", "REFERENCE SPEED MAX IAS"),
    "eng-max-cht": ("chtMax", "celsius", "RECIP MAX CHT"),
    "oil-max-temperature": ("oilTempMax", "celsius", "MAX OIL TEMPERATURE"),
    "eng-max-egt": ("egtMax", "celsius", "MAX EGT"),
    "turb-max-itt": ("ittMax", "celsius", "TURB MAX ITT"),
    "eng-max-rated-rpm:1": ("rpmMaxRated", "rpm", "MAX RATED ENGINE RPM:1"),
    "eng-max-rated-rpm": ("rpmMaxRated", "rpm", "MAX RATED ENGINE RPM"),
    "af-design-speed-vs0": ("vs0", "knots", "DESIGN SPEED VS0"),
    "af-design-speed-vs1": ("vs1", "knots", "DESIGN SPEED VS1"),
    "af-mach-max-operate": ("mmo", "mach", "MACH MAX OPERATE"),
    "af-ref-speed-max-ias-gear-down": ("vle", "knots", "REFERENCE SPEED MAX IAS GEAR DOWN")
}


def load_title_map(map_path):
    title_to_aircraft = {}
    if not map_path or not os.path.exists(map_path):
        return title_to_aircraft

    with open(map_path, mode="r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            title = row.get("simTitle", "").strip()
            aircraft_id = row.get("aircraftId", "").strip()
            if title and aircraft_id:
                title_to_aircraft[title] = aircraft_id
    return title_to_aircraft


def load_poh_values(poh_path):
    poh_by_aircraft = {}
    if not poh_path or not os.path.exists(poh_path):
        return poh_by_aircraft

    with open(poh_path, mode="r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            aircraft_id = row.get("aircraftId", "").strip()
            key = row.get("key", "").strip()
            val_str = row.get("value", "").strip()
            unit = row.get("unit", "").strip()
            ref = row.get("reference", "").strip()
            notes = row.get("notes", "").strip() or None

            if not aircraft_id or not key or not val_str:
                continue

            try:
                val = float(val_str)
            except ValueError:
                continue

            if aircraft_id not in poh_by_aircraft:
                poh_by_aircraft[aircraft_id] = []

            poh_by_aircraft[aircraft_id].append({
                "key": key,
                "value": val,
                "unit": unit,
                "source": "POH",
                "reference": ref,
                "notes": notes
            })

    return poh_by_aircraft


def extract_atc_model_pattern(raw_text):
    if not raw_text:
        return None
    match = re.search(r"AC_MODEL\s+(.*?)\.0\.text", raw_text)
    if match:
        return match.group(1).strip()
    return raw_text.strip()


def find_json_file_for_aircraft(out_dir, aircraft_id):
    if not os.path.exists(out_dir):
        return None

    for fname in os.listdir(out_dir):
        if not fname.endswith(".json") or fname.startswith("_"):
            continue
        fpath = os.path.join(out_dir, fname)
        try:
            with open(fpath, "r", encoding="utf-8") as f:
                data = json.load(f)
                if data.get("id") == aircraft_id:
                    return fpath
        except (json.JSONDecodeError, OSError):
            continue

    return None


def extract_simvar_candidates_from_csv(csv_path, context):
    candidates = []
    if not os.path.exists(csv_path):
        return candidates

    numbers = context.get("Numbers", {})
    is_gear_retractable = numbers.get("ctx-is-gear-retractable", context.get("ctx-is-gear-retractable", 0))
    gear_is_fixed = float(is_gear_retractable) <= 0.0

    columns_data = {}
    flaps_by_index = {}
    native_onset_rpms = []

    with open(csv_path, mode="r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            for col, val_str in row.items():
                if not val_str:
                    continue
                try:
                    val = float(val_str)
                except ValueError:
                    continue

                if col not in columns_data:
                    columns_data[col] = []
                columns_data[col].append(val)

            # Flaps Vfe por dente
            flaps_lim = row.get("af-flaps-current-speed-limitation")
            flaps_idx = row.get("af-flaps-handle-index")
            if flaps_lim is not None and flaps_idx is not None:
                try:
                    f_speed = float(flaps_lim)
                    f_idx = int(round(float(flaps_idx)))
                    if f_idx >= 1 and f_speed > 0:
                        if f_idx not in flaps_by_index:
                            flaps_by_index[f_idx] = []
                        flaps_by_index[f_idx].append(f_speed)
                except ValueError:
                    pass

            # NativeOnset RPM
            rpm_str = row.get("eng-rpm:1") or row.get("eng-rpm")
            rate_str = row.get("nw-overstress-rate-39") or row.get("nw-overstress-rate-40")
            if rpm_str and rate_str:
                try:
                    rpm_val = float(rpm_str)
                    rate_val = float(rate_str)
                    if rate_val > 0.0 and rpm_val > 0.0:
                        native_onset_rpms.append(rpm_val)
                except ValueError:
                    pass

    csv_name = os.path.basename(csv_path)

    # Mediana das colunas mapeadas
    for col, (key, unit, ref_name) in SIMVAR_KEY_MAPPING.items():
        if key == "vle" and gear_is_fixed:
            continue

        values = columns_data.get(col, [])
        # Filtrar valores <= 0 e descartar -273.15 (qualquer valor <= -200)
        valid_values = [v for v in values if v > 0.0 and v > -200.0]
        if not valid_values:
            continue

        med = statistics.median(valid_values)
        candidates.append({
            "key": key,
            "value": round(med, 2) if unit != "mach" else round(med, 3),
            "unit": unit,
            "source": "SimVar",
            "reference": f"{ref_name} ({csv_name})",
            "notes": None
        })

    # Flaps Vfe por dente
    for dente in sorted(flaps_by_index.keys()):
        speeds = flaps_by_index[dente]
        valid_speeds = [s for s in speeds if s > 0.0]
        if valid_speeds:
            med_speed = statistics.median(valid_speeds)
            candidates.append({
                "key": f"vfe:{dente}",
                "value": round(med_speed, 1),
                "unit": "knots",
                "source": "SimVar",
                "reference": f"FLAPS CURRENT SPEED LIMITATION, dente {dente} ({csv_name})",
                "notes": None
            })

    # NativeOnset
    if native_onset_rpms:
        min_onset_rpm = min(native_onset_rpms)
        candidates.append({
            "key": "rpmNativeOnset",
            "value": round(min_onset_rpm, 1),
            "unit": "rpm",
            "source": "NativeOnset",
            "reference": f"OVERSTRESS RATE início ({csv_name})",
            "notes": None
        })

    return candidates


def apply_limit_candidates(existing_limits, new_candidates):
    limits_by_key = {l["key"]: l for l in existing_limits}
    changed = False

    for candidate in new_candidates:
        key = candidate["key"]
        cand_source = candidate["source"]
        cand_prio = SOURCE_PRIORITY.get(cand_source, 99)

        if key not in limits_by_key:
            existing_limits.append(candidate)
            limits_by_key[key] = candidate
            changed = True
        else:
            current = limits_by_key[key]
            current_source = current.get("source", "ClassDefault")
            current_prio = SOURCE_PRIORITY.get(current_source, 99)

            # Regra: POH nunca é sobrescrito
            if current_source == "POH":
                continue

            # Se a nova fonte tiver prioridade melhor ou igual
            if cand_prio <= current_prio:
                if (current.get("value") != candidate.get("value") or
                    current.get("source") != candidate.get("source") or
                    current.get("unit") != candidate.get("unit")):
                    current["value"] = candidate["value"]
                    current["source"] = candidate["source"]
                    current["unit"] = candidate["unit"]
                    current["reference"] = candidate["reference"]
                    current["notes"] = candidate["notes"]
                    changed = True

    return changed


def process_merging(captures_dir, poh_path, map_path, out_dir, dry_run=False):
    title_map = load_title_map(map_path)
    poh_data = load_poh_values(poh_path)
    unmapped_titles = set()

    # Mapear aeronaves afetadas
    aircraft_updates = {}

    # 1. Processar capturas se pasta existir
    if captures_dir and os.path.exists(captures_dir):
        files = os.listdir(captures_dir)
        context_files = [f for f in files if f.endswith("-context.json")]

        for ctx_file in sorted(context_files):
            ctx_path = os.path.join(captures_dir, ctx_file)
            csv_path = ctx_path[:-len("-context.json")] + ".csv"

            try:
                with open(ctx_path, "r", encoding="utf-8") as f:
                    context = json.load(f)
            except (json.JSONDecodeError, OSError):
                continue

            title = context.get("Title", "").strip()
            if not title:
                continue

            aircraft_id = title_map.get(title)
            if not aircraft_id:
                unmapped_titles.add(title)
                continue

            if aircraft_id not in aircraft_updates:
                aircraft_updates[aircraft_id] = {
                    "simMatch_titles": set(),
                    "simMatch_atc": set(),
                    "candidates": []
                }

            aircraft_updates[aircraft_id]["simMatch_titles"].add(title)

            # ATC Model
            strings = context.get("Strings", {})
            raw_atc = strings.get("ctx-atc-model", context.get("ctx-atc-model", ""))
            atc_code = extract_atc_model_pattern(raw_atc)
            if atc_code:
                aircraft_updates[aircraft_id]["simMatch_atc"].add(atc_code)

            # Candidatos do CSV
            if os.path.exists(csv_path):
                cands = extract_simvar_candidates_from_csv(csv_path, context)
                aircraft_updates[aircraft_id]["candidates"].extend(cands)

    # 2. Adicionar candidatos POH
    for aircraft_id, poh_list in poh_data.items():
        if aircraft_id not in aircraft_updates:
            aircraft_updates[aircraft_id] = {
                "simMatch_titles": set(),
                "simMatch_atc": set(),
                "candidates": []
            }
        aircraft_updates[aircraft_id]["candidates"].extend(poh_list)

    # 3. Aplicar nos arquivos JSON de cada aeronave
    for aircraft_id, update_info in aircraft_updates.items():
        json_file = find_json_file_for_aircraft(out_dir, aircraft_id)
        if not json_file:
            continue

        with open(json_file, "r", encoding="utf-8") as f:
            data = json.load(f)

        changed = False

        # Atualizar simMatch
        sim_match = data.setdefault("simMatch", {})
        curr_titles = sim_match.setdefault("title", [])
        curr_atc = sim_match.setdefault("atcModel", [])

        for t in update_info["simMatch_titles"]:
            if t not in curr_titles:
                curr_titles.append(t)
                changed = True

        for a in update_info["simMatch_atc"]:
            if a not in curr_atc:
                curr_atc.append(a)
                changed = True

        # Aplicar candidatos a limites
        limits = data.setdefault("limits", [])
        if apply_limit_candidates(limits, update_info["candidates"]):
            changed = True

        if changed:
            data["dataVersion"] = data.get("dataVersion", 1) + 1
            if dry_run:
                print(f"[DRY-RUN] Alterações para '{aircraft_id}' em {os.path.basename(json_file)}: dataVersion -> {data['dataVersion']}")
            else:
                with open(json_file, "w", encoding="utf-8") as f:
                    json.dump(data, f, indent=2, ensure_ascii=False)
                    f.write("\n")
                print(f"Atualizado '{aircraft_id}' em {os.path.basename(json_file)}: dataVersion -> {data['dataVersion']}")

    if unmapped_titles:
        for t in sorted(unmapped_titles):
            print(f"sem mapa: {t}")


def main():
    parser = argparse.ArgumentParser(description="Mescla e atualiza limites operacionais de aeronaves.")
    parser.add_argument("--captures", help="Diretório contendo capturas de sensores (.csv e -context.json)")
    parser.add_argument("--poh", required=True, help="Arquivo CSV com limites de POH (poh_values.csv)")
    parser.add_argument("--map", required=True, help="Arquivo CSV mapeando título do sim para aircraftId")
    parser.add_argument("--out", required=True, help="Diretório de saída dos JSONs de aeronaves")
    parser.add_argument("--dry-run", action="store_true", help="Executa sem salvar alterações nos arquivos")

    args = parser.parse_args()
    process_merging(args.captures, args.poh, args.map, args.out, dry_run=args.dry_run)


if __name__ == "__main__":
    main()
