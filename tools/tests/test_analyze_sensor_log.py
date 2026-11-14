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
    extract_markers,
    extract_sensor_columns,
    load_expectations,
    load_registry,
    load_rows,
    marker_reactions,
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


if __name__ == "__main__":
    unittest.main()
