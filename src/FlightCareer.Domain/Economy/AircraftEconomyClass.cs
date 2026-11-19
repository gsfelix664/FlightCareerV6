using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Economy
{
    /// <summary>
    /// Classificação econômica de aeronaves (Classes A a J) e mapeamento de multiplicadores de preço de peças (CALIBRA-F4b).
    /// </summary>
    public static class AircraftEconomyClass
    {
        /// <summary>
        /// Multiplicador de peças escaláveis por classe de aeronave para calibrar custo/hora de manutenção dentro das metas econômicas.
        /// A 0,75 · B 1,25 · C 1,25 · D 1,25 · E 1,5 · F 1,75 · G 1,75 · H 2,5 · I 3,25 · J 5,5
        /// </summary>
        public static double GetPartPriceMultiplier(string classKey)
        {
            if (string.IsNullOrWhiteSpace(classKey)) return 1.0;
            return classKey.Trim().ToUpperInvariant() switch
            {
                "A" => 0.75,
                "B" => 1.25,
                "C" => 1.25,
                "D" => 1.25,
                "E" => 1.5,
                "F" => 1.75,
                "G" => 1.75,
                "H" => 2.5,
                "I" => 3.25,
                "J" => 5.5,
                _ => 1.0
            };
        }

        /// <summary>
        /// Determina a classe econômica da aeronave (A a J) com base em motorização, features, contagem de motores e preço.
        /// </summary>
        public static string Resolve(string modelName, string engineClass, int engineCount, IReadOnlyCollection<string> features, double price)
        {
            modelName ??= string.Empty;
            engineClass ??= string.Empty;
            features ??= Array.Empty<string>();

            if (string.Equals(engineClass, "Piston", StringComparison.OrdinalIgnoreCase))
            {
                if (engineCount > 1) return "E"; // Bimotor pistão (Baron G58, C404 Titan, DA62)
                if (ContainsFeature(features, "diesel")) return "D"; // Diesel FADEC monomotor (DA40 NG)
                if (ContainsFeature(features, "turbocharged")) return "C"; // Pistão turbo (SR22T, C400 Corvalis, C207T)
                if (ContainsFeature(features, "variablePitchProp") || ContainsFeature(features, "fuelInjected")) return "B"; // Pistão complexo
                return "A"; // Pistão leve (C152, J-3 Cub, CAP-4, DR400, Dakota)
            }

            if (string.Equals(engineClass, "Turboprop", StringComparison.OrdinalIgnoreCase))
            {
                if (engineCount == 1) return "F"; // Turboélice mono (TBM, PC-12, C208B)
                if (price >= 100_000_000.0) return "J"; // Cargueiro militar pesado quadrimotor (A400M)
                return "G"; // Turboélice bi/multimotor regional (King Air, ATR, DHC-6, SkyCourier, ES30)
            }

            if (string.Equals(engineClass, "Jet", StringComparison.OrdinalIgnoreCase))
            {
                // Jato executivo (CJ4, Longitude, PC-24)
                bool isBizJet = modelName.Contains("Citation", StringComparison.OrdinalIgnoreCase) ||
                                modelName.Contains("PC-24", StringComparison.OrdinalIgnoreCase) ||
                                (price < 50_000_000.0 && !modelName.Contains("A310", StringComparison.OrdinalIgnoreCase));

                if (isBizJet) return "H";

                // Widebody moderno / transporte pesado (> 200M ou 4 motores ou modelos específicos)
                bool isWidebody = engineCount >= 4 ||
                                  price >= 200_000_000.0 ||
                                  modelName.Contains("747", StringComparison.OrdinalIgnoreCase) ||
                                  modelName.Contains("A330", StringComparison.OrdinalIgnoreCase) ||
                                  modelName.Contains("787", StringComparison.OrdinalIgnoreCase);

                if (isWidebody) return "J";

                // Linha aérea narrowbody (A320, A321, 737, A310)
                return "I";
            }

            return "A";
        }

        private static bool ContainsFeature(IReadOnlyCollection<string> features, string target)
        {
            foreach (var f in features)
            {
                if (string.Equals(f, target, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
