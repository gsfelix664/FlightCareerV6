#!/usr/bin/env python3
"""Testes unitários para o script de análise de telemetria e validação de sensores (tools/analyze_sensor_log.py)."""

import csv
import json
import os
import sys
import tempfile
import unittest

# Garante que a raiz do repositório está no sys.path
repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
if repo_root not in sys.path:
    sys.path.insert(0, repo_root)

from tools.analyze_sensor_log import (
    classify,
    column_stats,
    compute_durations_and_gaps,
    extract_markers,
    extract_sensor_columns,
    load_expectations,
    load_registry,
    load_rows,
    marker_reactions,
    match_marker_prefix,
    native_wear_summary,
)


class TestAnalyzeSensorLog(unittest.TestCase):

    def setUp(self):
        self.registry_path = os.path.join(repo_root, "src", "FlightCareer.Infrastructure", "Data", "sensor_registry.json")
        self.expectations_path = os.path.join(repo_root, "tools", "sensor_validation_expectations.json")

    def test_constant_column_is_not_supported(self):
        """Coluna com valor constante (ex.: sempre 0) deve ser classificada como 'Não suportado'."""
        rows = [
            {"utc": f"2026-11-14T01:00:{i:02d}.000Z", "sim_rate": "1", "test-col": "0.0", "marker": ""}
            for i in range(20)
        ]
        cols = ["test-col"]
        stats = column_stats(rows, cols)
        res = classify("test-col", stats["test-col"], [], [], {})
        self.assertEqual(res, "Não suportado")

    def test_empty_column_is_not_supported(self):
        """Coluna toda vazia deve ser classificada como 'Não suportado'."""
        rows = [
            {"utc": f"2026-11-14T01:00:{i:02d}.000Z", "sim_rate": "1", "empty-col": "", "marker": ""}
            for i in range(20)
        ]
        cols = ["empty-col"]
        stats = column_stats(rows, cols)
        res = classify("empty-col", stats["empty-col"], [], [], {})
        self.assertEqual(res, "Não suportado")

    def test_varying_column_reacting_at_marker_is_validated(self):
        """Chave esperada em 02-freio-taxi que varia e sobe após o marcador deve ser 'Validado'."""
        rows = []
        # t=0 a t=19: freio em 0
        for i in range(20):
            marker = "02-freio-taxi" if i == 10 else ""
            rows.append({
                "utc": f"2026-11-14T01:00:{i:02d}.000Z",
                "sim_rate": "1",
                "brake-left-position": "0.0" if i <= 10 else f"{0.1 * (i - 10)}",
                "marker": marker,
            })
        # Mais amostras variadas para garantir que % constante < 99%
        for i in range(20, 50):
            rows.append({
                "utc": f"2026-11-14T01:00:{i:02d}.000Z",
                "sim_rate": "1",
                "brake-left-position": f"{((i % 10) + 1) * 0.1:.2f}",
                "marker": "",
            })

        cols = ["brake-left-position"]
        stats = column_stats(rows, cols)
        markers = extract_markers(rows)
        reactions = marker_reactions(rows, cols, markers, window_seconds=10.0, col_stats=stats)

        expectations = {"02-freio-taxi": ["brake-left-position"]}
        res = classify("brake-left-position", stats["brake-left-position"], reactions["brake-left-position"], markers, expectations)

        self.assertIn("02-freio-taxi", reactions["brake-left-position"])
        self.assertEqual(res, "Validado")

    def test_expected_column_not_reacting_is_review(self):
        """Chave esperada em 02-freio-taxi que varia fora da janela mas fica plana na janela do marcador deve ser 'Revisar'."""
        rows = []
        # Marcador em t=10. Janela [-10, +10] vai de t=0 a t=20.
        # Dentro de t=0..20, brake-left-position fica estritamente em 0.0.
        for i in range(25):
            marker = "02-freio-taxi" if i == 10 else ""
            rows.append({
                "utc": f"2026-11-14T01:00:{i:02d}.000Z",
                "sim_rate": "1",
                "brake-left-position": "0.0",
                "marker": marker,
            })
        # Varia bastante fora da janela (t=25..60) de modo que distinct >= 2 e pct_const < 99%
        for i in range(25, 60):
            rows.append({
                "utc": f"2026-11-14T01:00:{i:02d}.000Z",
                "sim_rate": "1",
                "brake-left-position": f"{((i % 7) + 1) * 0.1:.2f}",
                "marker": "",
            })

        cols = ["brake-left-position"]
        stats = column_stats(rows, cols)
        markers = extract_markers(rows)
        reactions = marker_reactions(rows, cols, markers, window_seconds=10.0, col_stats=stats)

        expectations = {"02-freio-taxi": ["brake-left-position"]}
        res = classify("brake-left-position", stats["brake-left-position"], reactions["brake-left-position"], markers, expectations)

        # Não deve ter reagido no marcador
        self.assertNotIn("02-freio-taxi", reactions["brake-left-position"])
        self.assertEqual(res, "Revisar")

    def test_pad_columns_ignored(self):
        """Colunas de preenchimento 'pad-*', 'utc', 'sim_rate' e 'marker' devem ser ignoradas."""
        all_cols = ["utc", "sim_rate", "eng-rpm:1", "pad-1", "pad-2", "marker", "oil-pressure:1"]
        sensor_cols = extract_sensor_columns(all_cols)
        self.assertEqual(sensor_cols, ["eng-rpm:1", "oil-pressure:1"])

    def test_per_engine_suffix_maps_to_registry_key(self):
        """Sufixo de motor ':2' em 'eng-rpm:2' deve mapear para o grupo 'piston' do sensor_registry.json."""
        key_to_group, _ = load_registry(self.registry_path)
        col_name = "eng-rpm:2"
        base_key = col_name.split(":")[0]
        self.assertEqual(base_key, "eng-rpm")
        self.assertEqual(key_to_group.get(base_key), "piston")

    def test_marker_with_comma_parsed(self):
        """Marcador com vírgula (RFC 4180) deve ser lido e interpretado corretamente."""
        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".csv", newline="", encoding="utf-8") as f:
            writer = csv.writer(f)
            writer.writerow(["utc", "sim_rate", "eng-rpm:1", "marker"])
            writer.writerow(["2026-11-14T01:00:00.000Z", "1", "1000", "02-freio-taxi, teste com virgula"])
            f_path = f.name

        try:
            rows, _ = load_rows([f_path])
            markers = extract_markers(rows)
            self.assertEqual(len(markers), 1)
            self.assertEqual(markers[0]["text"], "02-freio-taxi, teste com virgula")
        finally:
            if os.path.exists(f_path):
                os.remove(f_path)

    def test_multiple_files_concatenated_by_utc(self):
        """Múltiplos arquivos CSV devem ser concatenados e ordenados por utc."""
        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".csv", newline="", encoding="utf-8") as f1:
            writer1 = csv.writer(f1)
            writer1.writerow(["utc", "sim_rate", "eng-rpm:1", "marker"])
            writer1.writerow(["2026-11-14T01:00:10.000Z", "1", "1200", ""])
            writer1.writerow(["2026-11-14T01:00:30.000Z", "1", "1500", ""])
            f1_path = f1.name

        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".csv", newline="", encoding="utf-8") as f2:
            writer2 = csv.writer(f2)
            writer2.writerow(["utc", "sim_rate", "eng-rpm:1", "marker"])
            writer2.writerow(["2026-11-14T01:00:00.000Z", "1", "1000", ""])
            writer2.writerow(["2026-11-14T01:00:20.000Z", "1", "1400", ""])
            f2_path = f2.name

        try:
            rows, _ = load_rows([f1_path, f2_path])
            self.assertEqual(len(rows), 4)
            utcs = [r["utc"] for r in rows]
            self.assertEqual(utcs, [
                "2026-11-14T01:00:00.000Z",
                "2026-11-14T01:00:10.000Z",
                "2026-11-14T01:00:20.000Z",
                "2026-11-14T01:00:30.000Z",
            ])
        finally:
            for p in (f1_path, f2_path):
                if os.path.exists(p):
                    os.remove(p)

    def test_expectations_keys_exist_in_registry(self):
        """Toda chave citada em sensor_validation_expectations.json deve existir em sensor_registry.json."""
        _, reg_keys = load_registry(self.registry_path)
        expectations = load_expectations(self.expectations_path)

        self.assertTrue(len(expectations) > 0, "Arquivo de expectativas não pode estar vazio")
        self.assertTrue(len(reg_keys) > 0, "Registro de sensores não pode estar vazio")

        for prefix, keys in expectations.items():
            for k in keys:
                self.assertIn(
                    k,
                    reg_keys,
                    f"Chave '{k}' citada no prefixo de expectativa '{prefix}' não existe em sensor_registry.json",
                )

    def test_missing_registry_exits_with_error(self):
        """Arquivo de registro inexistente deve escrever no stderr e encerrar com código 2."""
        with self.assertRaises(SystemExit) as cm:
            load_registry("caminho/inexistente/sensor_registry.json")
        self.assertEqual(cm.exception.code, 2)

    def test_missing_expectations_exits_with_error(self):
        """Arquivo de expectativas inexistente deve escrever no stderr e encerrar com código 2."""
        with self.assertRaises(SystemExit) as cm:
            load_expectations("caminho/inexistente/sensor_validation_expectations.json")
        self.assertEqual(cm.exception.code, 2)

    def test_gap_between_files_not_counted_in_duration(self):
        """Intervalo entre gravações (dt > max_gap) não deve ser contado na duração coberta e deve registrar quebra."""
        rows = [
            {"utc": "2026-11-14T01:00:00.000Z", "sim_rate": "1", "eng-rpm:1": "1000"},
            {"utc": "2026-11-14T01:00:02.000Z", "sim_rate": "1", "eng-rpm:1": "1000"},
            # Gap de 100 segundos (> max_gap de 5s)
            {"utc": "2026-11-14T01:01:42.000Z", "sim_rate": "1", "eng-rpm:1": "1200"},
            {"utc": "2026-11-14T01:01:45.000Z", "sim_rate": "1", "eng-rpm:1": "1200"},
        ]
        real_dur, sim_dur, gaps = compute_durations_and_gaps(rows, max_gap=5.0)
        # Trecho 1: 2s (00 a 02s). Trecho 2: 3s (102 a 105s). Total coberto: 5.0s.
        self.assertAlmostEqual(real_dur, 5.0, places=2)
        self.assertAlmostEqual(sim_dur, 5.0, places=2)
        self.assertEqual(gaps, 1)

    def test_gap_not_weighting_pct_const(self):
        """Intervalo entre gravações (dt > max_gap) não deve atribuir peso de tempo ao último valor do trecho anterior."""
        rows = [
            # Trecho 1: valor 100.0 por 2 segundos (t=0 a t=2)
            {"utc": "2026-11-14T01:00:00.000Z", "sim_rate": "1", "eng-rpm:1": "100.0"},
            {"utc": "2026-11-14T01:00:01.000Z", "sim_rate": "1", "eng-rpm:1": "100.0"},
            {"utc": "2026-11-14T01:00:02.000Z", "sim_rate": "1", "eng-rpm:1": "100.0"},
            # Gap de 3600 segundos (1 hora)
            # Trecho 2: valor 200.0 por 2 segundos (t=3602 a t=3604)
            {"utc": "2026-11-14T02:00:02.000Z", "sim_rate": "1", "eng-rpm:1": "200.0"},
            {"utc": "2026-11-14T02:00:03.000Z", "sim_rate": "1", "eng-rpm:1": "200.0"},
            {"utc": "2026-11-14T02:00:04.000Z", "sim_rate": "1", "eng-rpm:1": "200.0"},
        ]
        cols = ["eng-rpm:1"]
        stats = column_stats(rows, cols, max_gap=5.0)
        # Com o gap descartado (dt=0), ambos os valores duram 2s cada.
        # pct_const deve ser 50%, e NÃO ~99.9%
        self.assertAlmostEqual(stats["eng-rpm:1"]["pct_const"], 50.0, places=1)

    def test_unknown_marker_has_no_expectation(self):
        """Marcador sem prefixo exato do roteiro (ex.: 'taxi' ou 'outro') não casa com expectativa."""
        expectations = {"02-freio-taxi": ["brake-left-position"]}
        # 'taxi' não tem prefixo '02-freio-taxi'
        prefix_taxi = match_marker_prefix("taxi", expectations)
        self.assertIsNone(prefix_taxi)

        prefix_unknown = match_marker_prefix("marcador-livre", expectations)
        self.assertIsNone(prefix_unknown)

        # Prefixo exato casa normalmente
        prefix_exact = match_marker_prefix("02-freio-taxi", expectations)
        self.assertEqual(prefix_exact, "02-freio-taxi")

    def test_native_wear_delta_counts_only_drops(self):
        """Delta deve somar apenas quedas sucessivas; subida é reinício, move a base e não credita."""
        rows = [
            {"utc": "2026-11-14T01:00:00.000Z", "nw-level-39": "1.00"},
            {"utc": "2026-11-14T01:00:10.000Z", "nw-level-39": "0.98"},
            {"utc": "2026-11-14T01:00:20.000Z", "nw-level-39": "1.00"},  # reinício
            {"utc": "2026-11-14T01:00:30.000Z", "nw-level-39": "0.99"},
        ]
        res = native_wear_summary(rows)
        self.assertTrue(res["has_data"])
        self.assertIn(39, res["components"])
        comp = res["components"][39]
        self.assertAlmostEqual(comp["delta"], 0.03, places=4)
        self.assertEqual(comp["restarts"], 1)
        self.assertAlmostEqual(comp["initial"], 1.00, places=4)
        self.assertAlmostEqual(comp["final"], 0.99, places=4)
        self.assertAlmostEqual(comp["min"], 0.98, places=4)
        self.assertTrue(comp["responded"])
        self.assertFalse(comp["is_idle"])

    def test_native_wear_weak_and_failed_first_time(self):
        """Grava primeiro timestamp e nível de quando nw-is-weak e nw-is-failed viram 1."""
        rows = [
            {"utc": "2026-11-14T01:00:00.000Z", "nw-level-39": "0.50", "nw-is-weak-39": "0", "nw-is-failed-39": "0"},
            {"utc": "2026-11-14T01:00:10.000Z", "nw-level-39": "0.15", "nw-is-weak-39": "1", "nw-is-failed-39": "0"},
            {"utc": "2026-11-14T01:00:20.000Z", "nw-level-39": "0.10", "nw-is-weak-39": "1", "nw-is-failed-39": "0"},
            {"utc": "2026-11-14T01:00:30.000Z", "nw-level-39": "0.05", "nw-is-weak-39": "1", "nw-is-failed-39": "1"},
        ]
        res = native_wear_summary(rows)
        comp = res["components"][39]
        self.assertEqual(comp["weak_utc"], "2026-11-14T01:00:10.000Z")
        self.assertAlmostEqual(comp["weak_level"], 0.15, places=4)
        self.assertEqual(comp["failed_utc"], "2026-11-14T01:00:30.000Z")
        self.assertAlmostEqual(comp["failed_level"], 0.05, places=4)

    def test_native_wear_silent_component_listed_as_idle(self):
        """Componente que não variou de nível e não disparou weak/failed fica marcado como idle."""
        rows = [
            {"utc": "2026-11-14T01:00:00.000Z", "nw-level-0": "1.00", "nw-is-weak-0": "0", "nw-is-failed-0": "0"},
            {"utc": "2026-11-14T01:00:10.000Z", "nw-level-0": "1.00", "nw-is-weak-0": "0", "nw-is-failed-0": "0"},
            {"utc": "2026-11-14T01:00:20.000Z", "nw-level-0": "1.00", "nw-is-weak-0": "0", "nw-is-failed-0": "0"},
        ]
        res = native_wear_summary(rows)
        self.assertTrue(res["has_data"])
        comp = res["components"][0]
        self.assertEqual(comp["delta"], 0.0)
        self.assertEqual(comp["restarts"], 0)
        self.assertIsNone(comp["weak_utc"])
        self.assertIsNone(comp["failed_utc"])
        self.assertFalse(comp["responded"])
        self.assertTrue(comp["is_idle"])



if __name__ == "__main__":
    unittest.main()
