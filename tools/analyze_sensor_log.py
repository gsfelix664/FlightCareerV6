#!/usr/bin/env python3
"""Script de análise de logs de telemetria e validação de sensores do FlightCareer.

Transforma arquivos CSV gerados pelo SensorRecorder e metadados de context.json
em um relatório de validação por sensor (Validado / Não suportado / Revisar).
"""

import argparse
import csv
import json
import math
import os
import sys
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional, Set, Tuple


# Raiz padrão do repositório e caminhos de catálogo resolvidos a partir da pasta deste script
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, ".."))
DEFAULT_REGISTRY = os.path.join(
    DEFAULT_REPO_ROOT, "src", "FlightCareer.Infrastructure", "Data", "sensor_registry.json"
)
DEFAULT_EXPECTATIONS = os.path.join(SCRIPT_DIR, "sensor_validation_expectations.json")


def parse_iso_utc(utc_str: str) -> Optional[datetime]:

    """Converte string ISO 8601 UTC em objeto datetime com fuso UTC."""
    if not utc_str:
        return None
    try:
        dt = datetime.fromisoformat(utc_str.strip())
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=timezone.utc)
        return dt
    except Exception:
        return None


def load_context(csv_paths: List[str]) -> Dict[str, Any]:
    """Carrega arquivos de contexto JSON associados aos arquivos CSV informados."""
    context: Dict[str, Any] = {
        "Title": "",
        "EngineCount": 1,
        "SlotCount": 0,
        "RejectedSimVars": [],
        "Strings": {},
        "Numbers": {},
    }

    for csv_path in csv_paths:
        base, _ = os.path.splitext(csv_path)
        context_path = f"{base}-context.json"
        if os.path.isfile(context_path):
            try:
                with open(context_path, "r", encoding="utf-8-sig") as f:
                    data = json.load(f)
                    if data.get("Title"):
                        context["Title"] = data["Title"]
                    if data.get("EngineCount"):
                        context["EngineCount"] = data["EngineCount"]
                    if data.get("SlotCount"):
                        context["SlotCount"] = data["SlotCount"]
                    if data.get("RejectedSimVars"):
                        for rej in data["RejectedSimVars"]:
                            if rej not in context["RejectedSimVars"]:
                                context["RejectedSimVars"].append(rej)
                    if data.get("Strings"):
                        context["Strings"].update(data["Strings"])
                    if data.get("Numbers"):
                        context["Numbers"].update(data["Numbers"])
            except Exception as ex:
                sys.stderr.write(f"Aviso: falha ao ler context.json '{context_path}': {ex}\n")
    return context


def load_rows(csv_paths: List[str]) -> Tuple[List[Dict[str, str]], List[str]]:
    """Lê os arquivos CSV informados, ignora linhas com UTC inválido e concatena em ordem cronológica."""
    raw_rows: List[Tuple[datetime, Dict[str, str]]] = []
    columns_seen: List[str] = []

    for path in csv_paths:
        if not os.path.isfile(path):
            raise FileNotFoundError(f"Arquivo CSV não encontrado: {path}")

        with open(path, "r", newline="", encoding="utf-8-sig") as f:
            reader = csv.DictReader(f)
            if reader.fieldnames:
                for col in reader.fieldnames:
                    if col and col not in columns_seen:
                        columns_seen.append(col)

            for line_no, row in enumerate(reader, start=2):
                utc_raw = row.get("utc")
                if not utc_raw:
                    sys.stderr.write(f"Aviso: linha {line_no} ignorada por 'utc' ausente em '{path}'\n")
                    continue

                dt = parse_iso_utc(utc_raw)
                if dt is None:
                    sys.stderr.write(f"Aviso: linha {line_no} ignorada por 'utc' inválido '{utc_raw}' em '{path}'\n")
                    continue

                raw_rows.append((dt, row))

    # Concatena em ordem de utc
    raw_rows.sort(key=lambda item: item[0])
    ordered_rows = [item[1] for item in raw_rows]
    return ordered_rows, columns_seen


def extract_sensor_columns(all_columns: List[str]) -> List[str]:
    """Filtra colunas do CSV para manter apenas as de sensores (exclui utc, sim_rate, marker e pad-*)."""
    sensor_cols: List[str] = []
    for col in all_columns:
        if col in ("utc", "sim_rate", "marker"):
            continue
        if col.startswith("pad-"):
            continue
        sensor_cols.append(col)
    return sensor_cols


def column_stats(
    rows: List[Dict[str, str]],
    sensor_columns: List[str],
    max_gap: float = 5.0,
) -> Dict[str, Dict[str, Any]]:
    """Calcula estatísticas básicas e porcentagem constante ponderada por tempo para cada coluna."""
    stats: Dict[str, Dict[str, Any]] = {}
    n_rows = len(rows)

    # Pré-calcula timestamps e intervalos dt entre amostras consecutivas
    parsed_timestamps: List[float] = []
    for r in rows:
        dt = parse_iso_utc(r.get("utc", ""))
        parsed_timestamps.append(dt.timestamp() if dt else 0.0)

    dts: List[float] = [0.0] * n_rows
    for i in range(n_rows - 1):
        delta = parsed_timestamps[i + 1] - parsed_timestamps[i]
        if delta > max_gap:
            dts[i] = 0.0
        else:
            dts[i] = max(0.0, delta)

    for col in sensor_columns:
        valid_vals: List[float] = []
        time_by_val: Dict[float, float] = {}
        distinct_vals: Set[float] = set()

        for idx, r in enumerate(rows):
            raw_v = r.get(col, "")
            if raw_v is None or raw_v == "":
                continue
            try:
                v = float(raw_v)
                if math.isnan(v) or math.isinf(v):
                    continue
                valid_vals.append(v)
                # Arredonda levemente para 7 casas decimais para agrupar ruído de precisão float
                v_key = round(v, 7)
                distinct_vals.add(v_key)
                time_by_val[v_key] = time_by_val.get(v_key, 0.0) + dts[idx]
            except ValueError:
                continue

        samples = len(valid_vals)
        if samples == 0:
            stats[col] = {
                "samples": 0,
                "min": None,
                "max": None,
                "mean": None,
                "pct_const": 0.0,
                "distinct": 0,
                "span": 0.0,
            }
            continue

        min_val = min(valid_vals)
        max_val = max(valid_vals)
        mean_val = sum(valid_vals) / samples
        span = max_val - min_val

        total_time = sum(time_by_val.values())
        if total_time > 0.0:
            max_time = max(time_by_val.values())
            pct_const = (max_time / total_time) * 100.0
        else:
            pct_const = 100.0

        stats[col] = {
            "samples": samples,
            "min": min_val,
            "max": max_val,
            "mean": mean_val,
            "pct_const": pct_const,
            "distinct": len(distinct_vals),
            "span": span,
        }

    return stats


def compute_durations_and_gaps(
    rows: List[Dict[str, str]],
    max_gap: float = 5.0,
) -> Tuple[float, float, int]:
    """Calcula duração real coberta, duração simulada e número de quebras (dt > max_gap)."""
    if not rows:
        return 0.0, 0.0, 0

    real_duration = 0.0
    sim_duration = 0.0
    gap_count = 0

    for i in range(len(rows) - 1):
        dt_curr = parse_iso_utc(rows[i].get("utc", ""))
        dt_next = parse_iso_utc(rows[i + 1].get("utc", ""))
        if dt_curr and dt_next:
            delta = dt_next.timestamp() - dt_curr.timestamp()
            if delta > max_gap:
                gap_count += 1
            elif delta > 0.0:
                real_duration += delta
                try:
                    sim_rate = float(rows[i].get("sim_rate", "1"))
                except ValueError:
                    sim_rate = 1.0
                sim_duration += delta * sim_rate

    return real_duration, sim_duration, gap_count



def extract_markers(rows: List[Dict[str, str]]) -> List[Dict[str, Any]]:
    """Extrai todas as linhas que contêm texto na coluna marker."""
    markers: List[Dict[str, Any]] = []
    for idx, r in enumerate(rows):
        marker_text = (r.get("marker") or "").strip()
        if marker_text:
            dt = parse_iso_utc(r.get("utc", ""))
            markers.append({
                "index": idx,
                "text": marker_text,
                "utc": r.get("utc", ""),
                "timestamp": dt.timestamp() if dt else 0.0,
            })
    return markers


def match_marker_prefix(marker_text: str, expectations: Dict[str, List[str]]) -> Optional[str]:
    """Associa o texto do marcador ao prefixo de expectativas correspondente pelo prefixo exato do roteiro."""
    norm = marker_text.strip().lower()
    for prefix in expectations:
        if norm.startswith(prefix.lower()):
            return prefix
    return None


def marker_reactions(
    rows: List[Dict[str, str]],
    sensor_columns: List[str],
    markers: List[Dict[str, Any]],
    window_seconds: float,
    col_stats: Dict[str, Dict[str, Any]],
) -> Dict[str, List[str]]:
    """Verifica se cada sensor reagiu na janela antes x depois de cada marcador."""
    reactions_by_col: Dict[str, List[str]] = {col: [] for col in sensor_columns}

    if not markers:
        return reactions_by_col

    # Pré-extrai timestamps e valores válidos por coluna
    timestamps: List[float] = []
    for r in rows:
        dt = parse_iso_utc(r.get("utc", ""))
        timestamps.append(dt.timestamp() if dt else 0.0)

    # Dicionário de (timestamp, valor) por coluna
    col_series: Dict[str, List[Tuple[float, float]]] = {}
    for col in sensor_columns:
        series: List[Tuple[float, float]] = []
        for i, r in enumerate(rows):
            raw = r.get(col, "")
            if raw is not None and raw != "":
                try:
                    val = float(raw)
                    if not (math.isnan(val) or math.isinf(val)):
                        series.append((timestamps[i], val))
                except ValueError:
                    pass
        col_series[col] = series

    for m in markers:
        t_m = m["timestamp"]
        m_text = m["text"]

        for col in sensor_columns:
            span = col_stats.get(col, {}).get("span", 0.0)
            threshold = max(1e-6, 0.01 * span)

            series = col_series[col]
            vals_before = [val for t, val in series if (t_m - window_seconds) <= t < t_m]
            vals_after = [val for t, val in series if t_m <= t <= (t_m + window_seconds)]

            if vals_before and vals_after:
                min_before, max_before = min(vals_before), max(vals_before)
                min_after, max_after = min(vals_after), max(vals_after)

                diff_min = abs(min_after - min_before)
                diff_max = abs(max_after - max_before)

                if diff_min > threshold or diff_max > threshold:
                    reactions_by_col[col].append(m_text)

    return reactions_by_col


def classify(
    column: str,
    stats: Dict[str, Any],
    reactions: List[str],
    markers: List[Dict[str, Any]],
    expectations: Dict[str, List[str]],
) -> str:
    """Classifica a coluna em 'Não suportado', 'Validado' ou 'Revisar' conforme as regras de negócio."""
    if stats["samples"] == 0 or stats["distinct"] <= 1:
        return "Não suportado"

    if stats["pct_const"] >= 99.0:
        return "Revisar"

    base_key = column.split(":")[0]

    # Identifica marcadores presentes no log que tinham essa chave base nas suas expectativas
    expected_markers_in_log: List[str] = []
    for m in markers:
        prefix = match_marker_prefix(m["text"], expectations)
        if prefix and base_key in expectations.get(prefix, []):
            expected_markers_in_log.append(m["text"])

    if expected_markers_in_log:
        # Deve ter reagido em pelo menos um marcador esperado presente no log
        reacted = any(m_text in reactions for m_text in expected_markers_in_log)
        if reacted:
            return "Validado"
        return "Revisar"

    # Se varia e não estava nas expectativas de nenhum marcador presente no log
    return "Validado"


def load_registry(registry_path: str) -> Tuple[Dict[str, str], Set[str]]:
    """Carrega o sensor_registry.json e retorna mapeamento de key -> group e conjunto de chaves.

    Se o arquivo não existir ou o JSON for inválido, emite mensagem no stderr e encerra com código 2.
    """
    if not os.path.isfile(registry_path):
        sys.stderr.write(f"Erro: arquivo de registro de sensores não encontrado: '{registry_path}'\n")
        sys.exit(2)

    key_to_group: Dict[str, str] = {}
    try:
        with open(registry_path, "r", encoding="utf-8-sig") as f:
            data = json.load(f)
            sensors = data.get("sensors", [])
            for s in sensors:
                key_to_group[s["key"]] = s.get("group", "outro")
    except Exception as ex:
        sys.stderr.write(f"Erro: falha ao carregar JSON do registro de sensores '{registry_path}': {ex}\n")
        sys.exit(2)

    return key_to_group, set(key_to_group.keys())


def load_expectations(expectations_path: str) -> Dict[str, List[str]]:
    """Carrega o sensor_validation_expectations.json.

    Se o arquivo não existir ou o JSON for inválido, emite mensagem no stderr e encerra com código 2.
    """
    if not os.path.isfile(expectations_path):
        sys.stderr.write(f"Erro: arquivo de expectativas não encontrado: '{expectations_path}'\n")
        sys.exit(2)

    try:
        with open(expectations_path, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    except Exception as ex:
        sys.stderr.write(f"Erro: falha ao carregar JSON de expectativas '{expectations_path}': {ex}\n")
        sys.exit(2)


def format_num(val: Optional[float]) -> str:
    """Formata valor numérico para relatório em Markdown."""
    if val is None:
        return "-"
    if abs(val) < 1e-5 and val != 0.0:
        return f"{val:.4e}"
    if abs(val) >= 10000:
        return f"{val:.1f}"
    if abs(val) < 0.01 and val != 0.0:
        return f"{val:.4f}"
    return f"{val:.2f}"


def render_report(
    ac_type: str,
    ac_model: str,
    csv_paths: List[str],
    context: Dict[str, Any],
    sensor_columns: List[str],
    stats: Dict[str, Dict[str, Any]],
    reactions: Dict[str, List[str]],
    classifications: Dict[str, str],
    markers: List[Dict[str, Any]],
    expectations: Dict[str, List[str]],
    key_to_group: Dict[str, str],
    unmapped_columns: List[str],
    real_duration_sec: float,
    sim_duration_sec: float,
    gap_count: int = 0,
    max_gap_sec: float = 5.0,
) -> str:
    """Gera o relatório completo em formato Markdown."""
    counts = {"Validado": 0, "Revisar": 0, "Não suportado": 0}
    for c in classifications.values():
        counts[c] = counts.get(c, 0) + 1

    total_cols = len(sensor_columns)

    # Formatação de durações
    real_m, real_s = divmod(int(real_duration_sec), 60)
    real_h, real_m = divmod(real_m, 60)
    real_dur_str = f"{real_h:02d}:{real_m:02d}:{real_s:02d} ({real_duration_sec:.1f} s)"

    sim_m, sim_s = divmod(int(sim_duration_sec), 60)
    sim_h, sim_m = divmod(sim_m, 60)
    sim_dur_str = f"{sim_h:02d}:{sim_m:02d}:{sim_s:02d} ({sim_duration_sec:.1f} s)"

    strings = context.get("Strings", {})
    ctx_title = strings.get("ctx-title") or context.get("Title") or "-"
    ctx_atc_model = strings.get("ctx-atc-model") or "-"
    ctx_atc_type = strings.get("ctx-atc-type") or "-"
    engine_count = context.get("EngineCount", 1)

    rejected_simvars = context.get("RejectedSimVars", [])
    files_list = ", ".join(f"`{os.path.basename(p)}`" for p in csv_paths)

    lines: List[str] = []
    lines.append(f"# Relatório de Validação de Sensores — Tipo {ac_type} ({ac_model})")
    lines.append("")
    lines.append("> [!NOTE]")
    lines.append("> As classificações deste relatório são **sugestões automáticas** geradas pelo script de análise com base em regras heurísticas.")
    lines.append("> A decisão final sobre a validação de cada sensor cabe ao **revisor humano**.")
    lines.append("")
    lines.append("## Metadados do Voo")
    lines.append("")
    lines.append(f"- **Tipo de Aeronave (Matriz):** {ac_type}")
    lines.append(f"- **Modelo:** {ac_model}")
    lines.append(f"- **Título (Contexto):** {ctx_title}")
    lines.append(f"- **ATC Model / Type:** {ctx_atc_model} / {ctx_atc_type}")
    lines.append(f"- **Número de Motores:** {engine_count}")
    lines.append(f"- **Arquivos Analisados:** {files_list}")
    lines.append(f"- **Duração Coberta:** {real_dur_str}")
    lines.append(f"- **Duração Simulada (Σ dt × sim_rate):** {sim_dur_str}")
    lines.append(f"- **Quebras de Gravação (intervalo > {max_gap_sec:.1f}s):** {gap_count}")
    if rejected_simvars:
        lines.append(f"- **SimVars Rejeitadas no SimConnect:** {', '.join(f'`{r}`' for r in rejected_simvars)}")
    else:
        lines.append("- **SimVars Rejeitadas no SimConnect:** Nenhuma")
    lines.append("")

    lines.append("## Resumo das Sugestões")
    lines.append("")
    lines.append(f"- **Total de Sensores Analisados:** {total_cols}")
    lines.append(f"- **Validado:** {counts.get('Validado', 0)} ({counts.get('Validado', 0) / max(1, total_cols) * 100:.1f}%)")
    lines.append(f"- **Revisar:** {counts.get('Revisar', 0)} ({counts.get('Revisar', 0) / max(1, total_cols) * 100:.1f}%)")
    lines.append(f"- **Não suportado:** {counts.get('Não suportado', 0)} ({counts.get('Não suportado', 0) / max(1, total_cols) * 100:.1f}%)")
    lines.append("")

    lines.append("## Marcadores Registrados")
    lines.append("")
    if markers:
        lines.append("| Marcador | UTC | Esperados que reagiram / esperados |")
        lines.append("|---|---|---|")
        for m in markers:
            m_text = m["text"]
            m_utc = m["utc"]
            prefix = match_marker_prefix(m_text, expectations)
            if prefix and prefix in expectations:
                expected_keys = expectations[prefix]
                reacted_cnt = 0
                for ek in expected_keys:
                    # Chave esperada reagiu se qualquer coluna com essa chave base reagiu
                    if any(
                        col.split(":")[0] == ek and m_text in reactions.get(col, [])
                        for col in sensor_columns
                    ):
                        reacted_cnt += 1
                ratio_str = f"{reacted_cnt} / {len(expected_keys)}"
            else:
                ratio_str = "fora do roteiro (sem expectativa)"
            lines.append(f"| `{m_text}` | `{m_utc}` | {ratio_str} |")
    else:
        lines.append("*Nenhum marcador registrado no log.*")
    lines.append("")

    # Agrupamento das colunas por grupo
    group_order = [
        "piston",
        "turbine",
        "oil",
        "electrical",
        "fuel",
        "gear",
        "hydraulic",
        "instruments",
        "airframe",
        "native-damage",
        "outro",
    ]

    cols_by_group: Dict[str, List[str]] = {}
    for col in sensor_columns:
        base_k = col.split(":")[0]
        grp = key_to_group.get(base_k, "outro")
        cols_by_group.setdefault(grp, []).append(col)

    lines.append("## Tabela de Sensores por Grupo")
    lines.append("")

    for grp in group_order:
        cols_in_grp = cols_by_group.get(grp, [])
        if not cols_in_grp:
            continue

        lines.append(f"### Grupo: {grp}")
        lines.append("")
        lines.append("| Chave | Grupo | Amostras | Mín | Máx | Média | % const. | Reagiu em | Sugestão |")
        lines.append("|---|---|---|---|---|---|---|---|---|")

        for col in sorted(cols_in_grp):
            st = stats[col]
            sug = classifications[col]
            react_list = reactions.get(col, [])
            react_str = ", ".join(f"`{r}`" for r in react_list) if react_list else "-"

            min_s = format_num(st["min"])
            max_s = format_num(st["max"])
            mean_s = format_num(st["mean"])
            pct_s = f"{st['pct_const']:.1f}%"
            samples_s = str(st["samples"])

            lines.append(f"| `{col}` | {grp} | {samples_s} | {min_s} | {max_s} | {mean_s} | {pct_s} | {react_str} | **{sug}** |")
        lines.append("")

    if unmapped_columns:
        lines.append("## Colunas do CSV Fora do Registro")
        lines.append("")
        for uc in sorted(unmapped_columns):
            lines.append(f"- `{uc}`")
        lines.append("")

    return "\n".join(lines)


def main() -> None:
    try:
        if hasattr(sys.stdout, "reconfigure"):
            sys.stdout.reconfigure(encoding="utf-8")
        if hasattr(sys.stderr, "reconfigure"):
            sys.stderr.reconfigure(encoding="utf-8")
    except Exception as ex:
        sys.stderr.write(f"Aviso: falha ao reconfigurar encoding para UTF-8: {ex}\n")

    parser = argparse.ArgumentParser(
        description="Analisa logs CSV do SensorRecorder e gera relatório de validação por sensor."
    )
    parser.add_argument("--type", required=True, help="Tipo de aeronave na matriz A–I (ex.: A)")
    parser.add_argument("--model", required=True, help="Modelo da aeronave (ex.: C152)")
    parser.add_argument("csv_files", nargs="+", help="Um ou mais arquivos CSV de log de sensores")
    parser.add_argument(
        "--out",
        help="Caminho do arquivo Markdown de saída (padrão: docs/plans/sensor-validation/<type>-<model>.md)",
    )
    parser.add_argument(
        "--window",
        type=float,
        default=30.0,
        help="Janela em segundos antes x depois dos marcadores para detecção de reação (padrão: 30)",
    )
    parser.add_argument(
        "--max-gap",
        type=float,
        default=5.0,
        help="Intervalo máximo em segundos entre amostras consecutivas antes de considerar quebra na gravação (padrão: 5.0)",
    )
    parser.add_argument(
        "--registry",
        default=DEFAULT_REGISTRY,
        help="Caminho do sensor_registry.json",
    )
    parser.add_argument(
        "--expectations",
        default=DEFAULT_EXPECTATIONS,
        help="Caminho do sensor_validation_expectations.json",
    )

    args = parser.parse_args()

    # Valida caminhos dos arquivos
    for p in args.csv_files:
        if not os.path.isfile(p):
            sys.stderr.write(f"Erro: arquivo CSV não encontrado: {p}\n")
            sys.exit(1)

    out_path = args.out
    if not out_path:
        out_path = os.path.join("docs", "plans", "sensor-validation", f"{args.type}-{args.model}.md")

    # Carrega dados
    try:
        rows, all_columns = load_rows(args.csv_files)
    except Exception as ex:
        sys.stderr.write(f"Erro ao processar arquivos CSV: {ex}\n")
        sys.exit(1)

    if not rows:
        sys.stderr.write("Erro: nenhum registro válido encontrado nos arquivos CSV informados.\n")
        sys.exit(1)

    context = load_context(args.csv_files)
    key_to_group, reg_keys = load_registry(args.registry)
    expectations = load_expectations(args.expectations)

    sensor_cols = extract_sensor_columns(all_columns)
    unmapped_cols = [c for c in sensor_cols if c.split(":")[0] not in reg_keys]

    # Cálculos
    stats = column_stats(rows, sensor_cols, max_gap=args.max_gap)
    markers = extract_markers(rows)
    reactions = marker_reactions(rows, sensor_cols, markers, args.window, stats)

    classifications: Dict[str, str] = {}
    for col in sensor_cols:
        classifications[col] = classify(
            col, stats[col], reactions.get(col, []), markers, expectations
        )

    # Duração coberta (real) e simulada considerando quebras (dt > max-gap)
    real_duration, sim_duration, gap_count = compute_durations_and_gaps(rows, args.max_gap)

    report_content = render_report(
        ac_type=args.type,
        ac_model=args.model,
        csv_paths=args.csv_files,
        context=context,
        sensor_columns=sensor_cols,
        stats=stats,
        reactions=reactions,
        classifications=classifications,
        markers=markers,
        expectations=expectations,
        key_to_group=key_to_group,
        unmapped_columns=unmapped_cols,
        real_duration_sec=real_duration,
        sim_duration_sec=sim_duration,
        gap_count=gap_count,
        max_gap_sec=args.max_gap,
    )

    # Grava relatório
    out_dir = os.path.dirname(out_path)
    if out_dir:
        os.makedirs(out_dir, exist_ok=True)

    with open(out_path, "w", encoding="utf-8") as f:
        f.write(report_content)

    print(f"Relatório gerado com sucesso em: {out_path}")


if __name__ == "__main__":
    main()
