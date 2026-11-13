using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Services
{
    /// <summary>
    /// Como a pane é mantida no simulador enquanto estiver aberta.
    /// </summary>
    public enum DefectEnforcement
    {
        /// <summary>Sem efeito no simulador (apenas registro/interdição).</summary>
        None,
        /// <summary>Evento TOGGLE_* nativo, reenviado uma vez a cada carregamento de aeronave.</summary>
        NativeToggle,
        /// <summary>Vigilância contínua de SimVar com correção imediata (+ bloqueio de comandos quando aplicável).</summary>
        Watchdog
    }

    public sealed record DefectSpec(
        string Code,
        string Title,
        bool IsGrounding,
        double BaseRepairCost,
        DefectEnforcement Enforcement,
        string? ToggleEvent = null);

    /// <summary>
    /// Catálogo único de panes persistentes (addon e nativas). As duas origens usam os mesmos códigos,
    /// o que evita registrar duas vezes a mesma pane (ex.: falha de motor injetada pelo addon e lida de volta do sim).
    /// </summary>
    public static class DefectCatalog
    {
        public const string MagnetoLeft = "MAGNETO_LEFT";
        public const string MagnetoRight = "MAGNETO_RIGHT";
        public const string FlapLock = "FLAP_LOCK";
        public const string FlapDamage = "FLAP_DAMAGE";
        public const string Alternator = "ALTERNATOR";
        public const string Vacuum = "VACUUM";
        public const string Pitot = "PITOT";
        public const string StaticPort = "STATIC_PORT";
        public const string EngineFailure = "ENGINE_FAILURE";
        public const string EngineFire = "ENGINE_FIRE";
        public const string EngineDamage = "ENGINE_DAMAGE";
        public const string GearDamage = "GEAR_DAMAGE";
        public const string GearCollapse = "GEAR_COLLAPSE";
        public const string TireBlowout = "TIRE_BLOWOUT";
        public const string BrakesWorn = "BRAKES_WORN";
        public const string Pressurization = "PRESSURIZATION";

        private static readonly Dictionary<string, DefectSpec> Specs = new()
        {
            [MagnetoLeft] = new(MagnetoLeft, "Magneto esquerdo inoperante", true, 1200, DefectEnforcement.Watchdog),
            [MagnetoRight] = new(MagnetoRight, "Magneto direito inoperante", true, 1200, DefectEnforcement.Watchdog),
            [FlapLock] = new(FlapLock, "Flaps travados", false, 1500, DefectEnforcement.Watchdog),
            [FlapDamage] = new(FlapDamage, "Flaps danificados por velocidade", false, 2500, DefectEnforcement.Watchdog),
            [Alternator] = new(Alternator, "Alternador inoperante", true, 900, DefectEnforcement.Watchdog),
            [Vacuum] = new(Vacuum, "Bomba de vácuo inoperante", false, 750, DefectEnforcement.NativeToggle, "TOGGLE_VACUUM_FAILURE"),
            [Pitot] = new(Pitot, "Tubo de pitot obstruído", false, 250, DefectEnforcement.NativeToggle, "TOGGLE_PITOT_BLOCKAGE"),
            [StaticPort] = new(StaticPort, "Tomada estática obstruída", false, 200, DefectEnforcement.NativeToggle, "TOGGLE_STATIC_PORT_BLOCKAGE"),
            [EngineFailure] = new(EngineFailure, "Motor #1 inoperante", true, 8000, DefectEnforcement.NativeToggle, "TOGGLE_ENGINE1_FAILURE"),
            [EngineFire] = new(EngineFire, "Fogo no motor #1", true, 15000, DefectEnforcement.None),
            [EngineDamage] = new(EngineDamage, "Dano interno no motor #1", true, 5000, DefectEnforcement.None),
            [GearDamage] = new(GearDamage, "Trem de pouso danificado por velocidade", true, 4000, DefectEnforcement.None),
            [GearCollapse] = new(GearCollapse, "Colapso de trem de pouso", true, 12000, DefectEnforcement.None),
            [TireBlowout] = new(TireBlowout, "Pneu estourado", true, 350, DefectEnforcement.None),
            [BrakesWorn] = new(BrakesWorn, "Freios gastos abaixo do limite", true, 600, DefectEnforcement.None),
            [Pressurization] = new(Pressurization, "Sistema de pressurização inoperante", true, 3500, DefectEnforcement.None),
        };

        public static DefectSpec Get(string code) =>
            Specs.TryGetValue(code, out var spec) ? spec : new DefectSpec(code, code, true, 1000, DefectEnforcement.None);

        /// <summary>
        /// Custo de reparo escalado pelo valor da aeronave (peças de turbina/jato custam mais que de um C152).
        /// </summary>
        public static double CalculateRepairCost(string code, double aircraftPurchasePrice)
        {
            double multiplier = Math.Clamp(aircraftPurchasePrice / 150000.0, 1.0, 25.0);
            return Math.Round(Get(code).BaseRepairCost * multiplier, 2);
        }
    }
}
