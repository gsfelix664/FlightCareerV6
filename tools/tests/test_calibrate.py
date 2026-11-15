#!/usr/bin/env python3
import csv
import json
import os
import shutil
import tempfile
import unittest

import sys
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TOOLS_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "..", "aircraft_limits"))
if TOOLS_DIR not in sys.path:
    sys.path.insert(0, TOOLS_DIR)

import calibrate


def generate_synthetic_csv(path, ambiguous_circuits=False, omit_avionics_off=False):
    """Gera um CSV sintético reproduzindo a sequência de calibração."""
    header = [
        "utc", "sim_rate",
        "elec-circuit-on-1", "elec-circuit-on-2", "elec-circuit-on-3",
        "elec-bus-amps-1", "elec-bus-amps-2", "elec-bus-amps-3", "elec-bus-amps-4", "elec-bus-amps-5",
        "elec-bus-voltage-1", "elec-bus-voltage-2", "elec-bus-voltage-3", "elec-bus-voltage-4", "elec-bus-voltage-5",
        "marker"
    ]

    rows = []

    # 1. cold-dark (10 linhas): tudo 0, tensão 0
    for _ in range(10):
        rows.append(["0", "1", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", ""])
    rows[-1][-1] = "cal:cold-dark"

    # 2. battery-on (10 linhas): circuito 1 = 1, circuitos 2 e 3 = 0, amps 4 = 1.5, tensão = 24.0
    c2_bat = "0" if ambiguous_circuits else "1"
    for _ in range(10):
        rows.append(["0", "1", "1", c2_bat, "0", "5", "5", "10", "1.5", "1.5", "24.0", "24.0", "24.0", "24.0", "24.0", ""])
    rows[-1][-1] = "cal:battery-on"

    # 3. avionics-on (15 linhas): circuito 3 vira 1 (e se ambíguo, circuito 2 também), amps 4 sobe para 20.0
    c2_val = "1"
    for _ in range(15):
        rows.append(["0", "1", "1", c2_val, "1", "7", "5", "23", "20.2", "1.6", "23.8", "23.8", "23.8", "23.8", "23.8", ""])
    rows[-1][-1] = "cal:avionics-on"

    # 4. avionics-off (15 linhas): circuito 3 volta para 0 (se ambíguo, circuito 2 também vai para 0), amps 4 cai para 1.6
    c2_off = "0" if ambiguous_circuits else "1"
    for _ in range(15):
        rows.append(["0", "1", "1", c2_off, "0", "7", "5", "23", "1.6", "1.6", "23.9", "23.9", "23.9", "23.9", "23.9", ""])
    if not omit_avionics_off:
        rows[-1][-1] = "cal:avionics-off"

    # 5. engine-start (15 linhas): motor liga, alternador ligado, tensão sobe para 28.0
    c2_rest = "0" if ambiguous_circuits else "1"
    for _ in range(15):
        rows.append(["0", "1", "1", c2_rest, "0", "10", "5", "23", "1.6", "1.6", "28.0", "28.0", "28.0", "28.0", "28.0", ""])
    rows[-1][-1] = "cal:engine-start"

    # 6. alternator-off (15 linhas): alternador desligado, tensão cai para 24.0 (queda de 4.0 V)
    for _ in range(15):
        rows.append(["0", "1", "1", c2_rest, "0", "10", "5", "23", "1.6", "1.6", "24.0", "24.0", "24.0", "24.0", "24.0", ""])
    rows[-1][-1] = "cal:alternator-off"

    # 7. alternator-on (15 linhas): alternador religado, tensão volta para 28.0
    for _ in range(15):
        rows.append(["0", "1", "1", c2_rest, "0", "10", "5", "23", "1.6", "1.6", "28.0", "28.0", "28.0", "28.0", "28.0", ""])
    rows[-1][-1] = "cal:alternator-on"

    with open(path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(header)
        writer.writerows(rows)


class TestCalibrate(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()
        self.csv_path = os.path.join(self.temp_dir, "test_cal.csv")
        self.out_dir = os.path.join(self.temp_dir, "aircraft")
        os.makedirs(self.out_dir, exist_ok=True)

        # Criar JSON sintético da aeronave
        self.aircraft_id = "TEST-AC"
        self.aircraft_json_path = os.path.join(self.out_dir, "test-ac.json")
        self.initial_json = {
            "schemaVersion": 1,
            "dataVersion": 1,
            "id": self.aircraft_id,
            "class": "Piston",
            "engineCount": 1,
            "features": ["piston", "singleEngine"],
            "simMatch": {"title": ["Test Aircraft Title"], "atcModel": ["TEST"]},
            "limits": [
                {
                    "key": "vne",
                    "value": 160.0,
                    "unit": "knots",
                    "source": "POH",
                    "reference": "POH Test",
                    "notes": None
                }
            ]
        }
        with open(self.aircraft_json_path, "w", encoding="utf-8") as f:
            json.dump(self.initial_json, f, indent=2)

    def tearDown(self):
        shutil.rmtree(self.temp_dir)

    def test_calibrate_identifies_circuit3_amps4_bus1_and_28volts(self):
        generate_synthetic_csv(self.csv_path)
        candidates = calibrate.run_calibration(
            self.csv_path,
            aircraft_id=self.aircraft_id,
            out_dir=self.out_dir,
            write=False
        )

        cand_map = {c["key"]: c["value"] for c in candidates}
        self.assertEqual(cand_map.get("elecAvionicsCircuitIndex"), 3.0)
        self.assertEqual(cand_map.get("elecAvionicsBusAmpsIndex"), 4.0)
        self.assertEqual(cand_map.get("elecMainBusIndex"), 1.0)
        self.assertEqual(cand_map.get("elecSystemVolts"), 28.0)

    def test_calibrate_ambiguous_circuits_does_not_record_index(self):
        generate_synthetic_csv(self.csv_path, ambiguous_circuits=True)
        candidates = calibrate.run_calibration(
            self.csv_path,
            aircraft_id=self.aircraft_id,
            out_dir=self.out_dir,
            write=False
        )

        cand_map = {c["key"]: c["value"] for c in candidates}
        self.assertNotIn("elecAvionicsCircuitIndex", cand_map)

    def test_calibrate_missing_avionics_off_does_not_record_index(self):
        generate_synthetic_csv(self.csv_path, omit_avionics_off=True)
        candidates = calibrate.run_calibration(
            self.csv_path,
            aircraft_id=self.aircraft_id,
            out_dir=self.out_dir,
            write=False
        )

        cand_map = {c["key"]: c["value"] for c in candidates}
        self.assertNotIn("elecAvionicsCircuitIndex", cand_map)

    def test_calibrate_without_write_does_not_alter_file(self):
        generate_synthetic_csv(self.csv_path)
        calibrate.run_calibration(
            self.csv_path,
            aircraft_id=self.aircraft_id,
            out_dir=self.out_dir,
            write=False
        )

        with open(self.aircraft_json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data["dataVersion"], 1)
        self.assertEqual(len(data["limits"]), 1)

    def test_calibrate_with_write_does_not_overwrite_poh_and_increments_dataversion(self):
        # Injetar uma tentativa de sobrescrever vne (que está como POH)
        generate_synthetic_csv(self.csv_path)
        calibrate.run_calibration(
            self.csv_path,
            aircraft_id=self.aircraft_id,
            out_dir=self.out_dir,
            write=True
        )

        with open(self.aircraft_json_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data["dataVersion"], 2)
        limits_map = {l["key"]: l for l in data["limits"]}

        # Vne deve continuar POH com valor 160.0
        self.assertEqual(limits_map["vne"]["source"], "POH")
        self.assertEqual(limits_map["vne"]["value"], 160.0)

        # Novos limites SimVar foram inseridos
        self.assertEqual(limits_map["elecAvionicsCircuitIndex"]["value"], 3.0)
        self.assertEqual(limits_map["elecAvionicsCircuitIndex"]["source"], "SimVar")
        self.assertEqual(limits_map["elecMainBusIndex"]["value"], 1.0)
        self.assertEqual(limits_map["elecSystemVolts"]["value"], 28.0)


if __name__ == "__main__":
    unittest.main()
