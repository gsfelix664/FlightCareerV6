#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import csv
import gzip
import json
import os
import shutil
import tempfile
import unittest
import sys
from pathlib import Path

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_REPLAY_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "..", "replay"))
if TOOLS_REPLAY_DIR not in sys.path:
    sys.path.insert(0, TOOLS_REPLAY_DIR)

import make_fixture


class TestMakeFixture(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()

    def tearDown(self):
        shutil.rmtree(self.temp_dir, ignore_errors=True)

    def test_is_column_required_keeps_structural_and_sensors(self):
        prefixes = {"eng-rpm", "elec-circuit-on", "af-g-force"}

        # Estruturais
        self.assertTrue(make_fixture.is_column_required("utc", prefixes))
        self.assertTrue(make_fixture.is_column_required("sim_rate", prefixes))
        self.assertTrue(make_fixture.is_column_required("marker", prefixes))

        # Sensores conhecidos com e sem sufixo
        self.assertTrue(make_fixture.is_column_required("eng-rpm", prefixes))
        self.assertTrue(make_fixture.is_column_required("eng-rpm:1", prefixes))
        self.assertTrue(make_fixture.is_column_required("elec-circuit-on-3", prefixes))
        self.assertTrue(make_fixture.is_column_required("af-g-force", prefixes))

        # Remoção de geográficas
        self.assertFalse(make_fixture.is_column_required("af-latitude", prefixes))
        self.assertFalse(make_fixture.is_column_required("plane-longitude", prefixes))
        self.assertFalse(make_fixture.is_column_required("af-altitude-feet", prefixes))

        # Remoção de colunas não requeridas
        self.assertFalse(make_fixture.is_column_required("unknown-sensor-xyz", prefixes))

    def test_extract_touchdowns_from_blackbox(self):
        blackbox_path = Path(self.temp_dir) / "blackbox.csv"
        content = (
            "# [EVENTO 10.0s] START\n"
            "# [EVENTO 1690.8s] TOQUE: 658 fpm, pico 1.40 G, nariz 4.5°, inclinação 0.2°, GS 65 kt\n"
            "# [EVENTO 1700.0s] BOUNCE: TOQUE: 250 fpm, pico 1.10 G, nariz 3.0°, inclinação 0.0°, GS 50 kt\n"
            "# [EVENTO 1800.0s] FIM\n"
        )
        blackbox_path.write_text(content, encoding="utf-8")

        tds = make_fixture.extract_touchdowns_from_blackbox(blackbox_path)
        self.assertEqual(len(tds), 2)

        self.assertEqual(tds[0]["elapsedSeconds"], 1690.8)
        self.assertEqual(tds[0]["fpm"], 658.0)
        self.assertEqual(tds[0]["pitchDeg"], 4.5)
        self.assertEqual(tds[0]["bankDeg"], 0.2)
        self.assertFalse(tds[0]["isBounce"])

        self.assertEqual(tds[1]["elapsedSeconds"], 1700.0)
        self.assertEqual(tds[1]["fpm"], 250.0)
        self.assertTrue(tds[1]["isBounce"])

    def test_process_fixture_filters_and_compresses(self):
        csv_path = Path(self.temp_dir) / "raw.csv"
        blackbox_path = Path(self.temp_dir) / "bb.csv"

        with open(csv_path, "w", encoding="utf-8", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(["utc", "sim_rate", "eng-rpm:1", "af-latitude", "random-data", "marker"])
            writer.writerow(["2026-11-15T12:00:00Z", "1", "2400", "-23.5", "999", "mark1"])
            writer.writerow(["2026-11-15T12:00:01Z", "1", "2450", "-23.6", "999", ""])

        blackbox_path.write_text("# [EVENTO 5.0s] TOQUE: 180 fpm, pico 1.2 G, nariz 2.0°, inclinação 1.0°, GS 55 kt\n", encoding="utf-8")

        out_dir = Path(self.temp_dir) / "output"
        out_csv, out_td, size = make_fixture.process_fixture(csv_path, blackbox_path, out_dir, "test_flight")

        self.assertTrue(out_csv.exists())
        self.assertTrue(out_td.exists())
        self.assertTrue(size > 0)

        # Confere colunas do CSV descompactado
        with gzip.open(out_csv, "rt", encoding="utf-8") as f:
            reader = csv.reader(f)
            header = next(reader)
            self.assertIn("utc", header)
            self.assertIn("sim_rate", header)
            self.assertIn("eng-rpm:1", header)
            self.assertIn("marker", header)
            self.assertNotIn("af-latitude", header)
            self.assertNotIn("random-data", header)

        # Confere toques gerados
        with open(out_td, "r", encoding="utf-8") as f:
            tds = json.load(f)
            self.assertEqual(len(tds), 1)
            self.assertEqual(tds[0]["fpm"], 180.0)


if __name__ == "__main__":
    unittest.main()
