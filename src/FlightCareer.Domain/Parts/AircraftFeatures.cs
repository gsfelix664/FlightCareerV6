using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Parts
{
    /// <summary>
    /// Vocabulário fechado e regras de coerência de características das aeronaves.
    /// Decide quais peças do catálogo cada modelo de aeronave possui.
    /// </summary>
    public static class AircraftFeatures
    {
        // Propulsão base (exatamente um)
        public const string Piston = "piston";
        public const string Turboprop = "turboprop";
        public const string Jet = "jet";

        // Variações e tecnologias de motor
        public const string Diesel = "diesel";
        public const string Turbocharged = "turbocharged";
        public const string Carbureted = "carbureted";
        public const string FuelInjected = "fuelInjected";

        // Configuração de hélice
        public const string FixedPitchProp = "fixedPitchProp";
        public const string ConstantSpeedProp = "constantSpeedProp";

        // Quantidade de motores (exatamente um)
        public const string SingleEngine = "singleEngine";
        public const string MultiEngine = "multiEngine";

        // Trem de pouso (exatamente um)
        public const string FixedGear = "fixedGear";
        public const string RetractableGear = "retractableGear";
        public const string Tailwheel = "tailwheel";
        public const string HydraulicGear = "hydraulicGear";

        // Célula e sistemas adicionais
        public const string Pressurized = "pressurized";
        public const string Apu = "apu";
        public const string ElectricHybrid = "electricHybrid";

        // Aviônicos e navegação
        public const string GlassCockpit = "glassCockpit";
        public const string Fms = "fms";
        public const string Autopilot = "autopilot";
        public const string WeatherRadar = "weatherRadar";

        // Aerodinâmica e superfícies
        public const string Slats = "slats";
        public const string Spoilers = "spoilers";
        public const string ThsTrim = "thsTrim";

        // Sistemas de proteção e trem
        public const string DeIceBoots = "deIceBoots";
        public const string BleedAntiIce = "bleedAntiIce";
        public const string Antiskid = "antiskid";
        public const string NoseWheelSteering = "noseWheelSteering";

        // Adendos F2.1b Rodada 2: hidráulico, pneumático, propulsão e aviônicos
        public const string Ptu = "ptu";
        public const string Rat = "rat";
        public const string PropDeIce = "propDeIce";
        public const string Fadec = "fadec";
        public const string BleedAir = "bleedAir";

        // Adendo 4 F2.1b Rodada 3: Cabine de passageiros, oxigênio, fogo e emergência
        public const string PassengerCabin = "passengerCabin";
        public const string Galley = "galley";
        public const string Lavatory = "lavatory";
        public const string Oxygen = "oxygen";
        public const string FireProtection = "fireProtection";
        public const string Floats = "floats";
        public const string Skis = "skis";
        public const string Wipers = "wipers";
        public const string IceDetector = "iceDetector";

        /// <summary>
        /// Todas as características válidas no vocabulário fechado.
        /// </summary>
        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Piston,
            Turboprop,
            Jet,
            Diesel,
            Turbocharged,
            Carbureted,
            FuelInjected,
            FixedPitchProp,
            ConstantSpeedProp,
            SingleEngine,
            MultiEngine,
            FixedGear,
            RetractableGear,
            Tailwheel,
            Pressurized,
            HydraulicGear,
            Apu,
            ElectricHybrid,
            GlassCockpit,
            Fms,
            Autopilot,
            WeatherRadar,
            Slats,
            Spoilers,
            ThsTrim,
            DeIceBoots,
            BleedAntiIce,
            Antiskid,
            NoseWheelSteering,
            Ptu,
            Rat,
            PropDeIce,
            Fadec,
            BleedAir,
            PassengerCabin,
            Galley,
            Lavatory,
            Oxygen,
            FireProtection,
            Floats,
            Skis,
            Wipers,
            IceDetector
        };

        /// <summary>
        /// Valida se um conjunto de características respeita o vocabulário fechado e todas as regras de coerência.
        /// </summary>
        public static void ValidateCoherence(IEnumerable<string>? features, string? aircraftContext = null)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features), $"Lista de features não pode ser nula para aeronave '{aircraftContext ?? "desconhecida"}'.");
            }

            var set = new HashSet<string>(features, StringComparer.OrdinalIgnoreCase);
            string ctx = string.IsNullOrWhiteSpace(aircraftContext) ? "Aeronave" : $"Aeronave '{aircraftContext}'";

            // 1. Todas as features pertencem ao vocabulário fechado
            foreach (var f in set)
            {
                if (!All.Contains(f))
                {
                    throw new InvalidOperationException($"{ctx}: feature '{f}' não pertence ao vocabulário fechado de AircraftFeatures.");
                }
            }

            // 2. Exatamente um de: piston, turboprop, jet
            int engineClassCount = (set.Contains(Piston) ? 1 : 0) +
                                   (set.Contains(Turboprop) ? 1 : 0) +
                                   (set.Contains(Jet) ? 1 : 0);
            if (engineClassCount != 1)
            {
                throw new InvalidOperationException($"{ctx}: deve ter exatamente uma classe de propulsão (piston, turboprop, jet). Encontrado: {engineClassCount}.");
            }

            // 3. Exatamente um de: singleEngine, multiEngine
            int engineCountFlag = (set.Contains(SingleEngine) ? 1 : 0) +
                                  (set.Contains(MultiEngine) ? 1 : 0);
            if (engineCountFlag != 1)
            {
                throw new InvalidOperationException($"{ctx}: deve ter exatamente um de singleEngine ou multiEngine.");
            }

            // 4. Exatamente um de: fixedGear, retractableGear
            int gearFlagCount = (set.Contains(FixedGear) ? 1 : 0) +
                                (set.Contains(RetractableGear) ? 1 : 0);
            if (gearFlagCount != 1)
            {
                throw new InvalidOperationException($"{ctx}: deve ter exatamente um de fixedGear ou retractableGear.");
            }

            // 5. Carbureted e fuelInjected só com piston e nunca os dois simultaneamente
            if (set.Contains(Carbureted) && set.Contains(FuelInjected))
            {
                throw new InvalidOperationException($"{ctx}: não pode ter carbureted e fuelInjected simultaneamente.");
            }
            if (set.Contains(Carbureted) && !set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: carbureted só é válido para motores a pistão.");
            }
            if (set.Contains(FuelInjected) && !set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: fuelInjected só é aplicável a motores a pistão no vocabulário.");
            }
            if (set.Contains(Piston) && !set.Contains(Carbureted) && !set.Contains(FuelInjected))
            {
                throw new InvalidOperationException($"{ctx}: motor a pistão deve ser carbureted ou fuelInjected.");
            }

            // 6. Jet nunca tem hélice
            if (set.Contains(Jet))
            {
                if (set.Contains(FixedPitchProp) || set.Contains(ConstantSpeedProp))
                {
                    throw new InvalidOperationException($"{ctx}: aeronaves a jato (jet) não podem possuir hélice (fixedPitchProp/constantSpeedProp).");
                }
            }

            // 7. Diesel deve ser motor a pistão
            if (set.Contains(Diesel) && !set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: diesel deve ser motor a pistão (piston).");
            }

            // 8. Motores a pistão ou turboélice normalmente possuem hélice (exatamente uma configuração se tiver prop)
            if (set.Contains(FixedPitchProp) && set.Contains(ConstantSpeedProp))
            {
                throw new InvalidOperationException($"{ctx}: não pode ter fixedPitchProp e constantSpeedProp simultaneamente.");
            }
            if ((set.Contains(Piston) || set.Contains(Turboprop)) && !set.Contains(FixedPitchProp) && !set.Contains(ConstantSpeedProp))
            {
                throw new InvalidOperationException($"{ctx}: motor a pistão ou turboélice deve definir fixedPitchProp ou constantSpeedProp.");
            }

            // 9. deIceBoots e bleedAntiIce exclusivos
            if (set.Contains(DeIceBoots) && set.Contains(BleedAntiIce))
            {
                throw new InvalidOperationException($"{ctx}: não pode ter deIceBoots e bleedAntiIce simultaneamente.");
            }

            // 10. slats apenas em jet ou turboprop
            if (set.Contains(Slats) && set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: slats só é aplicável a aeronaves a jato ou turboélice.");
            }

            // 11. thsTrim apenas em jet ou turboprop
            if (set.Contains(ThsTrim) && set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: thsTrim só é aplicável a aeronaves a jato ou turboélice.");
            }

            // 12. tailwheel não possui noseWheelSteering
            if (set.Contains(Tailwheel) && set.Contains(NoseWheelSteering))
            {
                throw new InvalidOperationException($"{ctx}: aeronave tailwheel (bequilha de cauda) não possui noseWheelSteering (direção do trem dianteiro).");
            }

            // 13. ptu apenas em multi-engine jet ou turboprop
            if (set.Contains(Ptu) && (!set.Contains(MultiEngine) || (!set.Contains(Jet) && !set.Contains(Turboprop))))
            {
                throw new InvalidOperationException($"{ctx}: ptu (unidade de transferência de potência hidráulica) só é aplicável a aeronaves multi-motor a jato ou turboélice.");
            }

            // 14. rat apenas em jet ou turboprop
            if (set.Contains(Rat) && (!set.Contains(Jet) && !set.Contains(Turboprop)))
            {
                throw new InvalidOperationException($"{ctx}: rat (turbina de ar de impacto) só é aplicável a aeronaves a jato ou turboélice.");
            }

            // 15. propDeIce não aplicável a jet e requer hélice
            if (set.Contains(PropDeIce) && set.Contains(Jet))
            {
                throw new InvalidOperationException($"{ctx}: propDeIce (degelo de hélice) não é aplicável a aeronaves a jato (jet).");
            }
            if (set.Contains(PropDeIce) && !set.Contains(ConstantSpeedProp) && !set.Contains(FixedPitchProp))
            {
                throw new InvalidOperationException($"{ctx}: propDeIce requer configuração de hélice (constantSpeedProp ou fixedPitchProp).");
            }

            // 16. bleedAir aplicável a jet, turboprop ou aeronave a pistão pressurizada
            if (set.Contains(BleedAir) && set.Contains(Piston) && !set.Contains(Pressurized))
            {
                throw new InvalidOperationException($"{ctx}: bleedAir (sistema pneumático de sangria) só é aplicável a jato, turboélice ou aeronave a pistão pressurizada.");
            }

            // 17. galley e lavatory requerem passengerCabin
            if (set.Contains(Galley) && !set.Contains(PassengerCabin))
            {
                throw new InvalidOperationException($"{ctx}: galley requer passengerCabin (cabine de passageiros).");
            }
            if (set.Contains(Lavatory) && !set.Contains(PassengerCabin))
            {
                throw new InvalidOperationException($"{ctx}: lavatory requer passengerCabin (cabine de passageiros).");
            }

            // 18. floats e skis são mutuamente exclusivos
            if (set.Contains(Floats) && set.Contains(Skis))
            {
                throw new InvalidOperationException($"{ctx}: não pode possuir floats (flutuadores) e skis (esquis) simultaneamente.");
            }

            // 19. iceDetector apenas em jet ou turboprop
            if (set.Contains(IceDetector) && set.Contains(Piston))
            {
                throw new InvalidOperationException($"{ctx}: iceDetector só é aplicável a aeronaves a jato ou turboélice.");
            }
        }
    }
}
