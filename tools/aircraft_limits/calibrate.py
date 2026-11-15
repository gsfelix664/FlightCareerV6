#!/usr/bin/env python3
"""
calibrate.py — Analisador do modo de calibração de solo para aeronaves (F2.4b-CAL).
Processa gravações de calibração (.csv + -context.json) e determina:
- elecAvionicsCircuitIndex
- elecAvionicsBusAmpsIndex
- elecMainBusIndex
- elecSystemVolts
Reutiliza extração de comandos e superfícies de merge_limits.py.
Com --write, atualiza o JSON da aeronave em Data/aircraft sem nunca sobrescrever POH.
"""

import argparse
import csv
import json
import os
import re
import statistics
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

import merge_limits

STEP_ALIASES = {
    "teste-bateria-avionics-on": "avionics-on",
    "teste-avionics-off": "avionics-off",
    "teste-partida": "engine-start",
    "teste-alternador-off": "alternator-off",
    "teste-alternador-on": "alternator-on",
    "teste-leme": "rudder-full",
}


def normalize_marker(raw_marker):
    if not raw_marker:
        return None
    marker = raw_marker.strip()
    if marker.startswith("cal:"):
        return marker[4:].strip()
    if marker.startswith("cal-skip:"):
        return "skip:" + marker[9:].strip()
    return STEP_ALIASES.get(marker, marker)


def load_calibration_csv(csv_path):
    if not os.path.exists(csv_path):
        raise FileNotFoundError(f"Arquivo CSV não encontrado: {csv_path}")

    rows = []
    with open(csv_path, mode="r", encoding="utf-8-sig") as f:
        reader = csv.DictReader(f)
        for r in reader:
            rows.append(r)

    return rows


def find_marker_indices(rows):
    """Mapeia cada step para o índice da linha onde seu marcador aparece."""
    step_indices = {}
    for idx, row in enumerate(rows):
        m = row.get("marker")
        normalized = normalize_marker(m)
        if normalized:
            step_indices[normalized] = idx
    return step_indices


def safe_float(val, default=None):
    if val is None or val == "":
        return default
    try:
        f = float(val)
        return f if f > -200.0 else default
    except ValueError:
        return default


def get_samples(rows, start_idx, end_idx, col):
    start = max(0, start_idx)
    end = min(len(rows), end_idx)
    if start >= end:
        return []
    vals = []
    for i in range(start, end):
        v = safe_float(rows[i].get(col))
        if v is not None:
            vals.append(v)
    return vals


def analyze_avionics_circuit(rows, step_indices):
    """
    Identifica elecAvionicsCircuitIndex:
    O n cujo CIRCUIT ON:n vai 1->0 na janela de avionics-off e 0->1 nas de avionics-on/avionics-on-2,
    e não muda nas outras janelas. Se 0 ou >1 candidatos, não grava e explica.
    """
    if "avionics-off" not in step_indices:
        return None, "Marcador 'cal:avionics-off' não encontrado no log."

    if "avionics-on" not in step_indices:
        return None, "Marcador 'cal:avionics-on' não encontrado no log."

    idx_off = step_indices["avionics-off"]
    idx_on = step_indices["avionics-on"]
    idx_on2 = step_indices.get("avionics-on-2")

    # Mapear colunas de circuito
    sample_row = rows[0] if rows else {}
    circuit_cols = {}
    for col in sample_row.keys():
        m = re.match(r"^(?:elec-circuit-on-|CIRCUIT ON:)(\d+)$", col)
        if m:
            circuit_cols[int(m.group(1))] = col

    if not circuit_cols:
        return None, "Nenhuma coluna de circuito (CIRCUIT ON:n) encontrada no CSV."

    candidates = []
    explanations = {}

    idx_bat = step_indices.get("battery-on")

    for idx in sorted(circuit_cols.keys()):
        col = circuit_cols[idx]

        # Se houver battery-on, o circuito de aviônicos deve estar desligado antes de avionics-on
        if idx_bat is not None:
            bat_samples = get_samples(rows, idx_bat - 8, idx_bat + 1, col)
            if not bat_samples:
                bat_samples = get_samples(rows, idx_bat, idx_bat + 10, col)
            if bat_samples and statistics.median(bat_samples) >= 0.5:
                continue

        # Amostras ligadas em avionics-on
        on_samples_a = get_samples(rows, idx_on - 8, idx_on + 1, col)
        on_samples_b = get_samples(rows, idx_on, idx_on + 12, col)
        on_samples = on_samples_a if on_samples_a else on_samples_b
        if on_samples_a and on_samples_b:
            # Pega a janela com maior presença de 1s
            on_samples = on_samples_a if statistics.median(on_samples_a) >= statistics.median(on_samples_b) else on_samples_b

        # Amostras desligadas em avionics-off
        off_samples_a = get_samples(rows, idx_off - 8, idx_off + 1, col)
        off_samples_b = get_samples(rows, idx_off, idx_off + 12, col)
        off_samples = off_samples_a if off_samples_a else off_samples_b
        if off_samples_a and off_samples_b:
            off_samples = off_samples_a if statistics.median(off_samples_a) <= statistics.median(off_samples_b) else off_samples_b

        if not on_samples or not off_samples:
            continue

        med_on = statistics.median(on_samples)
        med_off = statistics.median(off_samples)

        is_1_to_0 = med_on >= 0.5 and med_off < 0.5

        if not is_1_to_0:
            continue

        # Se houver avionics-on-2, deve voltar para 1
        if idx_on2 is not None:
            on2_a = get_samples(rows, idx_on2 - 8, idx_on2 + 1, col)
            on2_b = get_samples(rows, idx_on2, idx_on2 + 12, col)
            on2_candidates = [statistics.median(s) for s in [on2_a, on2_b] if s]
            if not on2_candidates or max(on2_candidates) < 0.5:
                continue

        candidates.append(idx)
        explanations[idx] = f"Circuito {idx} ({col}): 1 em avionics-on, transiciona para 0 em avionics-off"

    if len(candidates) == 1:
        c = candidates[0]
        return c, explanations[c]
    elif len(candidates) == 0:
        return None, "Nenhum circuito atende aos critérios de chave de aviônicos (1->0 em avionics-off)."
    else:
        return None, f"Circuito ambíguo: múltiplos candidatos encontrados {candidates}."


def analyze_avionics_bus_amps(rows, step_indices):
    """
    Identifica elecAvionicsBusAmpsIndex:
    O ELECTRICAL BUS AMPS:n com a maior queda (>= 3 A) em avionics-off e subida em avionics-on-2.
    """
    if "avionics-off" not in step_indices or "avionics-on" not in step_indices:
        return None, "Passos 'avionics-on' ou 'avionics-off' ausentes."

    idx_off = step_indices["avionics-off"]
    idx_on = step_indices["avionics-on"]
    idx_on2 = step_indices.get("avionics-on-2")

    sample_row = rows[0] if rows else {}
    amps_cols = {}
    for col in sample_row.keys():
        m = re.match(r"^(?:elec-bus-amps-|ELECTRICAL BUS AMPS:)(\d+)$", col)
        if m:
            amps_cols[int(m.group(1))] = col

    if not amps_cols:
        return None, "Nenhuma coluna de barramento de corrente (BUS AMPS:n) encontrada."

    best_idx = None
    max_drop = 0.0
    evidence = ""

    for idx in sorted(amps_cols.keys()):
        col = amps_cols[idx]
        
        # Amostras ligadas (avionics-on)
        on_a = get_samples(rows, idx_on - 8, idx_on + 1, col)
        on_b = get_samples(rows, idx_on, idx_on + 12, col)
        on_candidates = [statistics.median(s) for s in [on_a, on_b] if s]

        # Amostras desligadas (avionics-off)
        off_a = get_samples(rows, idx_off - 8, idx_off + 1, col)
        off_b = get_samples(rows, idx_off, idx_off + 12, col)
        off_candidates = [statistics.median(s) for s in [off_a, off_b] if s]

        if not on_candidates or not off_candidates:
            continue

        med_on = max(on_candidates)
        med_off = min(off_candidates)
        drop = med_on - med_off

        if drop >= 3.0:
            if idx_on2 is not None:
                on2_a = get_samples(rows, idx_on2 - 8, idx_on2 + 1, col)
                on2_b = get_samples(rows, idx_on2, idx_on2 + 12, col)
                on2_candidates = [statistics.median(s) for s in [on2_a, on2_b] if s]
                if on2_candidates:
                    med_on2 = max(on2_candidates)
                    rise = med_on2 - med_off
                    if rise < 3.0:
                        continue

            if drop > max_drop:
                max_drop = drop
                best_idx = idx
                evidence = f"Barramento {idx} ({col}): queda de {drop:.1f} A (de {med_on:.1f} A para {med_off:.1f} A) em avionics-off"

    if best_idx is not None:
        return best_idx, evidence
    else:
        return None, "Nenhum barramento apresentou queda de corrente >= 3.0 A ao desligar aviônicos."


def analyze_main_bus_and_system_volts(rows, step_indices):
    """
    Identifica elecMainBusIndex e elecSystemVolts:
    Menor n cujo ELECTRICAL BUS VOLTAGE:n sobe >= 2 V em alternator-on/engine-start e cai em alternator-off.
    elecSystemVolts: mediana desse barramento com alternador ligado, arredondada para 28 ou 14.
    """
    sample_row = rows[0] if rows else {}
    volts_cols = {}
    for col in sample_row.keys():
        m = re.match(r"^(?:elec-bus-voltage-|ELECTRICAL BUS VOLTAGE:)(\d+)$", col)
        if m:
            volts_cols[int(m.group(1))] = col

    if not volts_cols:
        return None, None, "Nenhuma coluna de tensão de barramento (BUS VOLTAGE:n) encontrada."

    has_alt_off = "alternator-off" in step_indices
    has_alt_on = "alternator-on" in step_indices
    has_start = "engine-start" in step_indices

    idx_alt_off = step_indices.get("alternator-off")
    idx_alt_on = step_indices.get("alternator-on")
    idx_start = step_indices.get("engine-start")

    candidates = []

    for idx in sorted(volts_cols.keys()):
        col = volts_cols[idx]

        if has_alt_off and idx_alt_off is not None:
            # Amostras desligadas (alternator-off)
            off_a = get_samples(rows, idx_alt_off - 8, idx_alt_off + 1, col)
            off_b = get_samples(rows, idx_alt_off, idx_alt_off + 15, col)
            off_candidates = [statistics.median(s) for s in [off_a, off_b] if s]
            if not off_candidates:
                continue
            med_off = min(off_candidates)

            # Amostras ligadas (alternator-on ou engine-start)
            on_candidates = []
            if has_alt_on and idx_alt_on is not None:
                on_alt_a = get_samples(rows, idx_alt_on - 8, idx_alt_on + 1, col)
                on_alt_b = get_samples(rows, idx_alt_on, idx_alt_on + 15, col)
                on_candidates.extend([statistics.median(s) for s in [on_alt_a, on_alt_b] if s])
            if has_start and idx_start is not None:
                on_start_a = get_samples(rows, idx_start - 8, idx_start + 1, col)
                on_start_b = get_samples(rows, idx_start, idx_start + 15, col)
                on_candidates.extend([statistics.median(s) for s in [on_start_a, on_start_b] if s])
            if not on_candidates:
                on_fallback = get_samples(rows, idx_alt_off - 20, idx_alt_off - 5, col)
                if on_fallback:
                    on_candidates.append(statistics.median(on_fallback))

            if not on_candidates:
                continue

            med_on = max(on_candidates)
            drop = med_on - med_off

            if drop >= 2.0:
                candidates.append((idx, col, med_on, drop))
        else:
            all_vals = [safe_float(r.get(col)) for r in rows if safe_float(r.get(col)) is not None]
            if all_vals:
                med_all = statistics.median(all_vals)
                if med_all >= 11.0:
                    candidates.append((idx, col, med_all, 0.0))

    if not candidates:
        return None, None, "Nenhum barramento apresentou variação de tensão >= 2.0 V com alternador ON/OFF."

    candidates.sort(key=lambda c: c[0])
    chosen_idx, chosen_col, med_on_volts, drop_val = candidates[0]

    if 25.0 <= med_on_volts <= 30.0:
        system_volts = 28.0
    elif 12.0 <= med_on_volts <= 15.0:
        system_volts = 14.0
    else:
        return chosen_idx, None, f"Barramento {chosen_idx} identificado, mas tensão média ({med_on_volts:.1f} V) fora da faixa nominal de 12-15V ou 25-30V."

    evidence = f"Barramento {chosen_idx} ({chosen_col}): tensão {med_on_volts:.1f} V com ALT ON (queda de {drop_val:.1f} V em alternator-off), sistema nominal {system_volts:.0f} V"
    return chosen_idx, system_volts, evidence


def _ensure_utf8_output():
    """O console padrão do Windows (cp1252) não codifica os símbolos do relatório: força UTF-8."""
    for stream in (sys.stdout, sys.stderr):
        try:
            if (getattr(stream, "encoding", "") or "").lower().replace("-", "") != "utf8":
                stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError) as ex:
            print(f"[calibrate] não foi possível ajustar a codificação da saída: {ex}", file=sys.__stderr__)


def run_calibration(csv_path, context_path=None, aircraft_id=None, out_dir=None, write=False):
    _ensure_utf8_output()
    if not out_dir:
        out_dir = os.path.join(SCRIPT_DIR, "..", "..", "src", "FlightCareer.Infrastructure", "Data", "aircraft")
    out_dir = os.path.abspath(out_dir)

    csv_name = os.path.basename(csv_path)

    # Carregar contexto
    if not context_path:
        cand_ctx = csv_path[:-4] + "-context.json"
        if os.path.exists(cand_ctx):
            context_path = cand_ctx

    context = {}
    if context_path and os.path.exists(context_path):
        with open(context_path, "r", encoding="utf-8") as f:
            context = json.load(f)

    # Identificar aeronave
    map_path = os.path.join(SCRIPT_DIR, "title_map.csv")
    title_map = merge_limits.load_title_map(map_path)

    sim_title = context.get("Title", "").strip()
    if not aircraft_id:
        if sim_title in title_map:
            aircraft_id = title_map[sim_title]
        elif sim_title:
            aircraft_id = sim_title

    rows = load_calibration_csv(csv_path)
    step_indices = find_marker_indices(rows)

    # 1. Análises elétricas
    circuit_idx, circuit_ev = analyze_avionics_circuit(rows, step_indices)
    amps_idx, amps_ev = analyze_avionics_bus_amps(rows, step_indices)
    main_bus_idx, system_volts, bus_ev = analyze_main_bus_and_system_volts(rows, step_indices)

    candidates = []

    if circuit_idx is not None:
        candidates.append({
            "key": "elecAvionicsCircuitIndex",
            "value": float(circuit_idx),
            "unit": "index",
            "source": "SimVar",
            "reference": f"{csv_name} / cal:avionics-off",
            "notes": None
        })

    if amps_idx is not None:
        candidates.append({
            "key": "elecAvionicsBusAmpsIndex",
            "value": float(amps_idx),
            "unit": "index",
            "source": "SimVar",
            "reference": f"{csv_name} / cal:avionics-off",
            "notes": None
        })

    if main_bus_idx is not None:
        candidates.append({
            "key": "elecMainBusIndex",
            "value": float(main_bus_idx),
            "unit": "index",
            "source": "SimVar",
            "reference": f"{csv_name} / cal:alternator-off",
            "notes": None
        })

    if system_volts is not None:
        candidates.append({
            "key": "elecSystemVolts",
            "value": float(system_volts),
            "unit": "volts",
            "source": "SimVar",
            "reference": f"{csv_name} / cal:alternator-on",
            "notes": None
        })

    # 2. Outros limites (comandos, dentes de flap, etc. extraídos pelo merge_limits)
    other_candidates = merge_limits.extract_simvar_candidates_from_csv(csv_path, context)
    all_candidates = candidates + other_candidates

    # Relatório Markdown
    print(f"\n# Relatório de Calibração: {aircraft_id or 'Aeronave Desconhecida'}")
    print(f"**Arquivo:** `{csv_name}`")
    if sim_title:
        print(f"**Título MSFS:** `{sim_title}`")
    print(f"**Passos detectados:** {', '.join(step_indices.keys()) or 'nenhum'}\n")

    print("## Parâmetros Elétricos Identificados")
    print("| Parâmetro | Chave | Valor | Unidade | Evidência |")
    print("|---|---|---|---|---|")
    print(f"| Chave Aviônicos | `elecAvionicsCircuitIndex` | {circuit_idx if circuit_idx is not None else 'NÃO ENCONTRADO'} | index | {circuit_ev} |")
    print(f"| Carga Aviônicos | `elecAvionicsBusAmpsIndex` | {amps_idx if amps_idx is not None else 'NÃO ENCONTRADO'} | index | {amps_ev} |")
    print(f"| Barramento Principal | `elecMainBusIndex` | {main_bus_idx if main_bus_idx is not None else 'NÃO ENCONTRADO'} | index | {bus_ev} |")
    print(f"| Tensão Nominal | `elecSystemVolts` | {f'{system_volts:.0f} V' if system_volts is not None else 'NÃO ENCONTRADO'} | volts | {bus_ev} |")

    # Alerta se algum índice estiver fora da faixa do voo normal
    if circuit_idx is not None and circuit_idx > 10:
        print(f"\n> [!WARNING] O circuito de aviônicos identificado ({circuit_idx}) está fora da faixa de voo normal (1..10). A regra ficará desligada até ampliar a faixa no registro de sensores.")
    if amps_idx is not None and amps_idx > 5:
        print(f"\n> [!WARNING] O barramento de corrente ({amps_idx}) está fora da faixa de voo normal (1..5).")
    if main_bus_idx is not None and main_bus_idx > 5:
        print(f"\n> [!WARNING] O barramento de tensão ({main_bus_idx}) está fora da faixa de voo normal (1..5).")

    if other_candidates:
        print("\n## Outros Limites Operacionais Extraídos")
        print("| Chave | Valor | Unidade | Referência |")
        print("|---|---|---|---|")
        for oc in other_candidates:
            print(f"| `{oc['key']}` | {oc['value']} | {oc['unit']} | {oc['reference']} |")

    print("\n## Parâmetros que dependem de POH (o simulador não informa)")
    print("- `alternatorRatedAmps`: Capacidade nominal do alternador em ampères (ex.: 60 A no C172S). Necessário para a regra `electrical_system_overload_amps`.")
    print("- `vno`, `va`, `gLimitPos`, `gLimitNeg`: Limites estruturais do manual de operação.")

    # Localizar arquivo JSON da aeronave
    json_path = None
    if aircraft_id:
        json_path = merge_limits.find_json_file_for_aircraft(out_dir, aircraft_id)

    if not json_path:
        print(f"\n⚠️ Arquivo de perfil JSON para a aeronave '{aircraft_id}' não encontrado em `{out_dir}`.")
        return all_candidates

    with open(json_path, "r", encoding="utf-8") as f:
        aircraft_data = json.load(f)

    existing_limits = aircraft_data.get("limits", [])
    limits_before = json.loads(json.dumps(existing_limits))

    changed = merge_limits.apply_limit_candidates(existing_limits, all_candidates)

    if changed:
        print(f"\n## Alterações Propostas para `{os.path.basename(json_path)}`")
        print("| Chave | Antes | Proposto | Fonte Proposta |")
        print("|---|---|---|---|")

        before_dict = {l["key"]: l for l in limits_before}
        for l in existing_limits:
            k = l["key"]
            bef = before_dict.get(k)
            if bef != l:
                bef_str = f"{bef['value']} {bef['unit']} ({bef['source']})" if bef else "*(novo)*"
                now_str = f"{l['value']} {l['unit']}"
                print(f"| `{k}` | {bef_str} | {now_str} | {l['source']} |")

        if write:
            aircraft_data["limits"] = existing_limits
            aircraft_data["dataVersion"] = aircraft_data.get("dataVersion", 1) + 1
            with open(json_path, "w", encoding="utf-8") as f:
                json.dump(aircraft_data, f, indent=2, ensure_ascii=False)
                f.write("\n")
            print(f"\n✅ Alterações gravadas com sucesso em `{os.path.basename(json_path)}` (dataVersion -> {aircraft_data['dataVersion']}).")
        else:
            print(f"\nℹ️ Modo somente-leitura. Use `--write` para gravar as alterações em `{os.path.basename(json_path)}`.")
    else:
        print(f"\nNenhuma alteração necessária em `{os.path.basename(json_path)}` (todos os limites válidos já estão presentes ou protegidos por POH).")

    return all_candidates


def main():
    parser = argparse.ArgumentParser(description="Calibra parâmetros elétricos e limites de aeronave a partir de gravação de solo.")
    parser.add_argument("csv", help="Caminho do arquivo CSV de calibração.")
    parser.add_argument("--context", help="Caminho do arquivo -context.json (opcional).", default=None)
    parser.add_argument("--aircraft", help="Identificador da aeronave (opcional).", default=None)
    parser.add_argument("--out-dir", help="Pasta com os JSONs de limites de aeronaves.", default=None)
    parser.add_argument("--write", action="store_true", help="Grava os limites identificados no JSON da aeronave sem sobrescrever POH.")

    args = parser.parse_args()

    try:
        run_calibration(args.csv, args.context, args.aircraft, args.out_dir, args.write)
    except Exception as ex:
        print(f"\n❌ Erro na calibração: {ex}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
