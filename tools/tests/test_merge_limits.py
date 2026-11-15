import csv
import io
import json
import os
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "aircraft_limits")))
import merge_limits


class TestMergeLimits(unittest.TestCase):
    def setUp(self):
        self.test_dir = tempfile.mkdtemp()
        self.captures_dir = os.path.join(self.test_dir, "captures")
        self.out_dir = os.path.join(self.test_dir, "aircraft")
        os.makedirs(self.captures_dir)
        os.makedirs(self.out_dir)

        self.poh_file = os.path.join(self.test_dir, "poh_values.csv")
        self.map_file = os.path.join(self.test_dir, "title_map.csv")

        with open(self.map_file, "w", encoding="utf-8", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(["simTitle", "aircraftId"])
            writer.writerow(["Cessna Skyhawk G1000", "C172SP G1000"])

        with open(self.poh_file, "w", encoding="utf-8", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(["aircraftId", "key", "value", "unit", "reference"])
            writer.writerow(["C172SP G1000", "rpmRedline", "2700", "rpm", "POH C172"])

        # JSON base da aeronave
        self.aircraft_json = os.path.join(self.out_dir, "c172sp-g1000.json")
        with open(self.aircraft_json, "w", encoding="utf-8") as f:
            json.dump({
                "schemaVersion": 1,
                "dataVersion": 1,
                "id": "C172SP G1000",
                "class": "Piston",
                "simMatch": {"title": [], "atcModel": []},
                "limits": []
            }, f, indent=2)

    def tearDown(self):
        shutil.rmtree(self.test_dir)

    def _create_synthetic_capture(self, context_data, csv_rows):
        base_name = "20261115-1200-TestCapture"
        ctx_path = os.path.join(self.captures_dir, f"{base_name}-context.json")
        csv_path = os.path.join(self.captures_dir, f"{base_name}.csv")

        with open(ctx_path, "w", encoding="utf-8") as f:
            json.dump(context_data, f, indent=2)

        if csv_rows:
            with open(csv_path, "w", encoding="utf-8", newline="") as f:
                writer = csv.DictWriter(f, fieldnames=list(csv_rows[0].keys()))
                writer.writeheader()
                for r in csv_rows:
                    writer.writerow(r)

    def test_vfe_per_notch_and_recoil_ignored(self):
        ctx = {
            "Title": "Cessna Skyhawk G1000",
            "Strings": {"ctx-atc-model": "ATCCOM.AC_MODEL C172.0.text"},
            "Numbers": {"ctx-is-gear-retractable": 0}
        }
        rows = [
            {"af-flaps-handle-index": "0", "af-flaps-current-speed-limitation": "-1"},
            {"af-flaps-handle-index": "1", "af-flaps-current-speed-limitation": "110"},
            {"af-flaps-handle-index": "1", "af-flaps-current-speed-limitation": "110"},
            {"af-flaps-handle-index": "2", "af-flaps-current-speed-limitation": "85"},
            {"af-flaps-handle-index": "2", "af-flaps-current-speed-limitation": "85"}
        ]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        limits = {l["key"]: l["value"] for l in data["limits"]}
        self.assertIn("vfe:1", limits)
        self.assertIn("vfe:2", limits)
        self.assertNotIn("vfe:0", limits)
        self.assertEqual(limits["vfe:1"], 110.0)
        self.assertEqual(limits["vfe:2"], 85.0)

    def test_itt_negative_273_ignored(self):
        ctx = {"Title": "Cessna Skyhawk G1000"}
        rows = [
            {"turb-max-itt": "-273.15"},
            {"turb-max-itt": "-273.15"}
        ]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        limits = {l["key"]: l["value"] for l in data["limits"]}
        self.assertNotIn("ittMax", limits)

    def test_vle_ignored_when_gear_is_fixed(self):
        ctx = {
            "Title": "Cessna Skyhawk G1000",
            "Numbers": {"ctx-is-gear-retractable": 0}
        }
        rows = [{"af-ref-speed-max-ias-gear-down": "160"}]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        limits = {l["key"]: l["value"] for l in data["limits"]}
        self.assertNotIn("vle", limits)

    def test_native_onset_picks_min_rpm_with_positive_rate(self):
        ctx = {"Title": "Cessna Skyhawk G1000"}
        rows = [
            {"eng-rpm:1": "2500", "nw-overstress-rate-39": "0.0"},
            {"eng-rpm:1": "2620", "nw-overstress-rate-39": "0.005"},
            {"eng-rpm:1": "2600", "nw-overstress-rate-39": "0.001"},
            {"eng-rpm:1": "2700", "nw-overstress-rate-39": "0.010"}
        ]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        limits = {l["key"]: l["value"] for l in data["limits"]}
        self.assertIn("rpmNativeOnset", limits)
        self.assertEqual(limits["rpmNativeOnset"], 2600.0)

    def test_poh_is_never_overwritten(self):
        # Configurar aeronave com POH existente
        with open(self.aircraft_json, "w", encoding="utf-8") as f:
            json.dump({
                "schemaVersion": 1,
                "dataVersion": 2,
                "id": "C172SP G1000",
                "class": "Piston",
                "simMatch": {"title": [], "atcModel": []},
                "limits": [
                    {
                        "key": "vne",
                        "value": 163.0,
                        "unit": "knots",
                        "source": "POH",
                        "reference": "POH original inviolável"
                    }
                ]
            }, f, indent=2)

        # Tentativa de injetar SimVar com valor diferente
        ctx = {"Title": "Cessna Skyhawk G1000"}
        rows = [{"af-ref-speed-max-ias": "170"}]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        vne = next(l for l in data["limits"] if l["key"] == "vne")
        self.assertEqual(vne["source"], "POH")
        self.assertEqual(vne["value"], 163.0)
        self.assertEqual(vne["reference"], "POH original inviolável")

    def test_unmapped_title_listed(self):
        ctx = {"Title": "Titulo Desconhecido Sem Mapa 123"}
        self._create_synthetic_capture(ctx, [{"af-ref-speed-max-ias": "150"}])

        captured_stdout = io.StringIO()
        old_stdout = sys.stdout
        try:
            sys.stdout = captured_stdout
            merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)
        finally:
            sys.stdout = old_stdout

        output = captured_stdout.getvalue()
        self.assertIn("sem mapa: Titulo Desconhecido Sem Mapa 123", output)

    def test_dry_run_does_not_save_to_disk(self):
        ctx = {"Title": "Cessna Skyhawk G1000"}
        rows = [{"af-ref-speed-max-ias": "163"}]
        self._create_synthetic_capture(ctx, rows)

        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=True)

        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data = json.load(f)

        self.assertEqual(data["dataVersion"], 1)
        self.assertEqual(len(data["limits"]), 0)

    def test_data_version_increments_only_when_changes_occur(self):
        ctx = {"Title": "Cessna Skyhawk G1000"}
        rows = [{"af-ref-speed-max-ias": "163"}]
        self._create_synthetic_capture(ctx, rows)

        # 1ª execução com alterações
        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)
        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data1 = json.load(f)
        self.assertEqual(data1["dataVersion"], 2)

        # 2ª execução com exatamente os mesmos dados
        merge_limits.process_merging(self.captures_dir, self.poh_file, self.map_file, self.out_dir, dry_run=False)
        with open(self.aircraft_json, "r", encoding="utf-8") as f:
            data2 = json.load(f)
        self.assertEqual(data2["dataVersion"], 2)


if __name__ == "__main__":
    unittest.main()
