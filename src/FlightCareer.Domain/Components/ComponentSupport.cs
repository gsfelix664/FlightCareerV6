using System;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>Fonte de aleatoriedade injetável (permite testes determinísticos e Monte Carlo com seed).</summary>
    public interface IRandomSource
    {
        /// <summary>Número uniforme em [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;
        public SystemRandomSource() => _random = new Random();
        public SystemRandomSource(int seed) => _random = new Random(seed);
        public double NextDouble() => _random.NextDouble();
    }

    public static class EngineClassResolver
    {
        /// <summary>Classe a partir da SimVar ENGINE TYPE (0=pistão, 1=jato, 5=turboélice).</summary>
        public static EngineClass FromSimEngineType(double engineType) => engineType switch
        {
            1.0 => EngineClass.Jet,
            5.0 => EngineClass.Turboprop,
            _ => EngineClass.Piston
        };

        /// <summary>
        /// Classe a partir do cadastro da aeronave (usado fora do voo, sem telemetria).
        /// Avgas = pistão; Jet A-1 em aviação comercial = jato; Jet A-1 em aviação geral = turboélice,
        /// com exceções conhecidas (pistões diesel e jatos executivos).
        /// </summary>
        public static EngineClass FromAircraftType(AircraftType? type)
        {
            if (type == null) return EngineClass.Piston;

            string model = type.Model ?? string.Empty;
            string fuel = type.FuelType ?? string.Empty;
            string category = type.Type ?? string.Empty;

            if (ContainsAny(model, "DA62", "DA42", "DA40 NG")) return EngineClass.Piston; // Diesel a Jet A-1
            if (ContainsAny(model, "PC-24", "Citation", "Phenom", "HondaJet", "Vision Jet", "Learjet")) return EngineClass.Jet;
            if (!fuel.Contains("Jet", StringComparison.OrdinalIgnoreCase)) return EngineClass.Piston;
            if (category.Contains("Comercial", StringComparison.OrdinalIgnoreCase)) return EngineClass.Jet;
            return EngineClass.Turboprop;
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            foreach (var term in terms)
            {
                if (text.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    public static class ComponentUsage
    {
        /// <summary>
        /// Indica se o componente está em uso (acumulando horas, desgaste natural e exposição a panes).
        /// Motor e sistemas acoplados: motor girando. Célula: em voo. Trem/pneus/freios: rolando no solo.
        /// </summary>
        public static bool IsInUse(ComponentKind kind, SimTelemetry data) => kind switch
        {
            ComponentKind.Engine or ComponentKind.Oil or ComponentKind.Ignition
                or ComponentKind.HotSection or ComponentKind.Electrical => data.IsEngineRunning,
            ComponentKind.Airframe => !data.IsOnGround,
            ComponentKind.LandingGear or ComponentKind.Tires or ComponentKind.Brakes => data.IsOnGround && data.GroundSpeed > 5.0,
            _ => false
        };

        /// <summary>Componentes cuja vida é contada em ciclos de pouso.</summary>
        public static bool IsCycleDriven(ComponentKind kind) =>
            kind is ComponentKind.LandingGear or ComponentKind.Tires or ComponentKind.Brakes;
    }
}
