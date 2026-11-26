using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Mapeamento de componentes de falha nativa do MSFS (ID 0–49, vide <see cref="FlightCareer.Domain.Components.NativeWearFailureMap"/>)
    /// para peças físicas candidatas do catálogo de peças (<c>parts_catalog.json</c>).
    /// Uma pane nativa afeta uma peça candidata instalada prioritária em vez de danificar todas simultaneamente.
    /// </summary>
    public static class NativeFailurePartMap
    {
        private static readonly Dictionary<int, string[]> Map = BuildMap();

        /// <summary>
        /// Devolve a lista ordenada de <c>PartId</c> candidatas para o componente nativo informado.
        /// </summary>
        public static IReadOnlyList<string> CandidatesFor(int nativeWearId)
        {
            return Map.TryGetValue(nativeWearId, out var candidates) ? candidates : Array.Empty<string>();
        }

        private static Dictionary<int, string[]> BuildMap()
        {
            var d = new Dictionary<int, string[]>();

            // IDs 0–4: FUEL (NativeWearFailureMap: DefectCatalog.FuelSystem)
            var fuel = new[]
            {
                "fuel.engine_pump",
                "fuel.fcu",
                "fuel.aux_pump",
                "fuel.selector_valve",
                "fuel.filter_gascolator"
            };
            for (int i = 0; i <= 4; i++) d[i] = fuel;

            // IDs 5–8: FLAPS, FLAPS CABLE (NativeWearFailureMap: DefectCatalog.FlapDamage, FlapLock)
            var flaps = new[] { "airframe.flaps" };
            for (int i = 5; i <= 8; i++) d[i] = flaps;

            // IDs 9–14, 19, 20: FLIGHT CONTROLS (NativeWearFailureMap: DefectCatalog.FlightControl)
            var flightControls = new[]
            {
                "airframe.flight_controls",
                "empennage.control_cables",
                "wing.ailerons",
                "empennage.elevator",
                "empennage.rudder",
                "empennage.trim_actuator"
            };
            for (int i = 9; i <= 14; i++) d[i] = flightControls;
            d[19] = flightControls;
            d[20] = flightControls;

            // IDs 21–27: HYDRAULIC (NativeWearFailureMap: DefectCatalog.Hydraulic)
            var hydraulic = new[]
            {
                "hydraulic.pump",
                "hydraulic.electric_pump",
                "hydraulic.lines_fittings",
                "hydraulic.accumulators",
                "hydraulic.filters_valves"
            };
            for (int i = 21; i <= 27; i++) d[i] = hydraulic;

            // ID 28: ELECTRICAL ALTERNATOR (NativeWearFailureMap: DefectCatalog.Alternator)
            d[28] = new[]
            {
                "electrical.alternator",
                "electrical.starter_generator",
                "electrical.idg"
            };

            // ID 29: ELECTRICAL BATTERY (NativeWearFailureMap: DefectCatalog.Battery)
            d[29] = new[] { "electrical.battery" };

            // IDs 30–34: ELECTRICAL (NativeWearFailureMap: DefectCatalog.Electrical)
            var electrical = new[]
            {
                "electrical.wiring_harness",
                "electrical.voltage_regulator",
                "electrical.circuit_breakers_relays",
                "electrical.inverter_converter",
                "electrical.tru_transformer_rectifier"
            };
            for (int i = 30; i <= 34; i++) d[i] = electrical;

            // IDs 35, 47: LANDING GEAR (NativeWearFailureMap: DefectCatalog.GearDamage)
            var landingGear = new[]
            {
                "gear.main_struts",
                "gear.nose_strut",
                "gear.tailwheel_assembly",
                "gear.actuator",
                "gear.trunnions_side_stays",
                "gear.wheels_bearings"
            };
            d[35] = landingGear;
            d[47] = landingGear;

            // ID 36: BRAKE (NativeWearFailureMap: DefectCatalog.BrakeFailure)
            d[36] = new[]
            {
                "brakes.pads_discs",
                "brakes.master_cylinders"
            };

            // IDs 37, 38, 48, 49: TIRE / TIRE PRESSURE (NativeWearFailureMap: DefectCatalog.TireBlowout, TireFlat)
            var tires = new[]
            {
                "tires.main",
                "tires.nose"
            };
            d[37] = tires;
            d[38] = tires;
            d[48] = tires;
            d[49] = tires;

            // ID 39: PISTON ENGINE (NativeWearFailureMap: DefectCatalog.EngineFailure)
            d[39] = new[]
            {
                "engine.cylinders",
                "engine.valves",
                "engine.crankshaft"
            };

            // ID 40: TURBINE ENGINE (NativeWearFailureMap: DefectCatalog.EngineFailure)
            d[40] = new[]
            {
                "hotsection.turbine_blades",
                "engine.compressor",
                "hotsection.combustor",
                "engine.fan"
            };

            // ID 41: OIL TANK INTEGRITY (NativeWearFailureMap: DefectCatalog.OilLeak)
            d[41] = new[]
            {
                "oil.cooler",
                "oil.pump",
                "oil.turbine_pump"
            };

            // IDs 43–45: AUTOPILOT (NativeWearFailureMap: DefectCatalog.Autopilot)
            var autopilot = new[] { "avionics.autopilot_servos" };
            d[43] = autopilot;
            d[44] = autopilot;
            d[45] = autopilot;

            // IDs 42 (OIL TANK AMOUNT), 46 (GENERAL) e outros sem mapeamento permanecem vazios.
            return d;
        }
    }
}
