using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Domain.Economy
{
    /// <summary>
    /// Capacidade efetiva de transporte de passageiros e carga conforme a configuração da cabine.
    /// </summary>
    public readonly record struct CabinCapacity(int MaxPax, double MaxCargoLbs);

    /// <summary>
    /// Regras econômicas e operacionais de conversão de cabine (passageiro ↔ carga).
    /// Lógica pura compartilhada entre UI, geradores de missão, simulações e testes.
    /// </summary>
    public static class CabinConversion
    {
        public const string ConfigPassenger = "Passageiro";
        public const string ConfigCargo = "Carga (Sem Assentos)";

        // Custos e tempos de indisponibilidade por porte (estimativas de desenvolvimento)
        public const double CostLight = 800.0;
        public const double DowntimeHoursLight = 4.0;

        public const double CostMedium = 3000.0;
        public const double DowntimeHoursMedium = 8.0;

        public const double CostHeavy = 12000.0;
        public const double DowntimeHoursHeavy = 24.0;

        // Fator de assento removido: 77 kg = 170 lb por assento
        public const double SeatWeightLbs = 170.0;

        // Fator limite de carga na configuração passageiro: 25% de CargaMax (bagageiro)
        public const double PassengerBagFactor = 0.25;

        // Fator limite de expansão máxima de carga na configuração carga: 130% de CargaMax
        public const double CargoMaxExpansionFactor = 1.30;

        /// <summary>
        /// Determina se a configuração indicada corresponde a modo Carga.
        /// </summary>
        public static bool IsCargoConfiguration(string? configuration)
        {
            return !string.IsNullOrEmpty(configuration) && configuration.Contains("Carga", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Alterna entre a configuração atual e a configuração de destino.
        /// </summary>
        public static string GetNextConfiguration(string? currentConfiguration)
        {
            return IsCargoConfiguration(currentConfiguration) ? ConfigPassenger : ConfigCargo;
        }

        /// <summary>
        /// Verifica se o conjunto de features da aeronave permite conversão de cabine.
        /// </summary>
        public static bool HasConvertibleCabin(IEnumerable<string>? features)
        {
            return features != null && features.Contains(AircraftFeatures.ConvertibleCabin, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica se a conversão é permitida para a lista de features informada.
        /// </summary>
        public static bool CanConvert(IEnumerable<string>? features)
        {
            return HasConvertibleCabin(features);
        }

        /// <summary>
        /// Retorna o custo de conversão de cabine para o porte da aeronave.
        /// </summary>
        public static double GetCost(AircraftSizeClass sizeClass) => sizeClass switch
        {
            AircraftSizeClass.Light => CostLight,
            AircraftSizeClass.Medium => CostMedium,
            AircraftSizeClass.Heavy => CostHeavy,
            _ => CostLight
        };

        /// <summary>
        /// Retorna o custo de conversão de cabine com base no MTOW (lbs) da aeronave.
        /// </summary>
        public static double GetCost(double mtowLbs)
        {
            return GetCost(AircraftSizeClassifier.Classify(mtowLbs));
        }

        /// <summary>
        /// Retorna o tempo de indisponibilidade (horas no relógio de carreira) para a conversão.
        /// </summary>
        public static double GetDowntimeHours(AircraftSizeClass sizeClass) => sizeClass switch
        {
            AircraftSizeClass.Light => DowntimeHoursLight,
            AircraftSizeClass.Medium => DowntimeHoursMedium,
            AircraftSizeClass.Heavy => DowntimeHoursHeavy,
            _ => DowntimeHoursLight
        };

        /// <summary>
        /// Retorna o tempo de indisponibilidade (horas no relógio de carreira) com base no MTOW.
        /// </summary>
        public static double GetDowntimeHours(double mtowLbs)
        {
            return GetDowntimeHours(AircraftSizeClassifier.Classify(mtowLbs));
        }

        /// <summary>
        /// Calcula a capacidade efetiva (PAX e carga em lbs) para a configuração atual da cabine.
        /// Caso a aeronave não possua cabine conversível (<paramref name="hasConvertibleCabin"/> = false),
        /// aplica-se a regra padrão de fábrica (PAX até MaxPax e carga até MaxPayloadLbs).
        /// </summary>
        public static CabinCapacity GetEffectiveCapacity(int maxPax, double maxPayloadLbs, string? configuration, bool hasConvertibleCabin = true)
        {
            if (!hasConvertibleCabin)
            {
                return new CabinCapacity(maxPax, maxPayloadLbs);
            }

            if (IsCargoConfiguration(configuration))
            {
                // Carga: pax = 0; carga até CargaMax + PaxMax × 170 lb, sem passar de CargaMax × 1,3
                double cargoWithSeatsRemoved = maxPayloadLbs + (maxPax * SeatWeightLbs);
                double cargoCeiling = maxPayloadLbs * CargoMaxExpansionFactor;
                double effectiveCargo = Math.Min(cargoWithSeatsRemoved, cargoCeiling);

                return new CabinCapacity(0, effectiveCargo);
            }
            else
            {
                // Passageiro: pax até PaxMax; carga limitada a 25% de CargaMax (bagagem)
                double effectiveCargo = maxPayloadLbs * PassengerBagFactor;
                return new CabinCapacity(maxPax, effectiveCargo);
            }
        }

        /// <summary>
        /// Valida se uma missão é compatível com a configuração atual da cabine.
        /// Caso a aeronave não possua cabine conversível (<paramref name="hasConvertibleCabin"/> = false),
        /// aplica-se a regra original direta (pax ≤ MaxPax e payload ≤ MaxPayloadLbs).
        /// </summary>
        public static bool IsMissionCompatible(
            string? configuration,
            int maxPax,
            double maxPayloadLbs,
            int missionPax,
            double missionPayloadLbs,
            double luggageWeightLbs = 0.0,
            double cargoWeightLbs = 0.0,
            bool hasConvertibleCabin = true)
        {
            if (!hasConvertibleCabin)
            {
                return missionPax <= maxPax && missionPayloadLbs <= maxPayloadLbs;
            }

            var capacity = GetEffectiveCapacity(maxPax, maxPayloadLbs, configuration, hasConvertibleCabin: true);

            if (IsCargoConfiguration(configuration))
            {
                // Na configuração Carga o avião não recebe/aceita missões com passageiros
                if (missionPax > 0)
                    return false;

                // Não pode exceder a capacidade de carga da configuração
                if (missionPayloadLbs > capacity.MaxCargoLbs)
                    return false;

                return true;
            }
            else
            {
                // Na configuração Passageiro:
                if (missionPax > capacity.MaxPax)
                    return false;

                if (missionPax == 0)
                {
                    // Missão de carga pura acima do limite de bagagem fica fora
                    if (missionPayloadLbs > capacity.MaxCargoLbs)
                        return false;
                }
                else
                {
                    // Missão de passageiros: carga/bagagem adicional não pode exceder o limite de bagageiro
                    double cargoPart = (luggageWeightLbs + cargoWeightLbs) > 0
                        ? (luggageWeightLbs + cargoWeightLbs)
                        : Math.Max(0.0, missionPayloadLbs - (missionPax * SeatWeightLbs));

                    if (cargoPart > capacity.MaxCargoLbs)
                        return false;

                    if (missionPayloadLbs > maxPayloadLbs)
                        return false;
                }

                return true;
            }
        }
    }
}
