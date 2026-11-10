using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Services
{
    public record AircraftWeightSpec(double MtowLbs, double EmptyWeightLbs);

    /// <summary>
    /// Especificações oficiais de peso operacional (MTOW e Peso Vazio)
    /// para todas as aeronaves homologadas da frota do FlightCareer V6.
    /// </summary>
    public static class AircraftWeightSpecs
    {
        private static readonly Dictionary<string, AircraftWeightSpec> OfficialSpecs = new(StringComparer.OrdinalIgnoreCase)
        {
            // --- TREINAMENTO / LEVES ---
            ["Cessna C152"] = new(1670, 1100),
            ["C152"] = new(1670, 1100),
            ["Cessna 152"] = new(1670, 1100),
            ["Piper J-3 Cub"] = new(1220, 680),
            ["J-3 Cub"] = new(1220, 680),
            ["CAP-4"] = new(1450, 900),
            ["Flight Design CT"] = new(1320, 720),
            ["Robin DR400"] = new(2205, 1300),
            ["NXCub"] = new(2300, 1216),

            // --- MONOMOTORES E PISTÃO AVANÇADOS ---
            ["C172SP G1000"] = new(2550, 1665),
            ["C172"] = new(2550, 1665),
            ["Cessna 172"] = new(2550, 1665),
            ["Diamond DA40NG"] = new(2888, 1938),
            ["DA40"] = new(2888, 1938),
            ["Piper PA28-236 Dakota"] = new(3000, 1680),
            ["PA28"] = new(3000, 1680),
            ["Beechcraft Bonanza"] = new(3650, 2517),
            ["Beechcraft Bonanza G36"] = new(3650, 2517),
            ["Bonanza"] = new(3650, 2517),
            ["Beechcraft Bonanza V35"] = new(3400, 2075),
            ["Cirrus SR22"] = new(3600, 2270),
            ["SR22"] = new(3600, 2270),
            ["C400 Corvalis"] = new(3600, 2500),
            ["Cessna 207T Stationair"] = new(3800, 2200),
            ["Cessna 188 AGTruck"] = new(4200, 2200),

            // --- BIMOTORES A PISTÃO E UTILITÁRIOS ---
            ["Beechcraft Baron G58"] = new(5500, 3920),
            ["Baron G58"] = new(5500, 3920),
            ["Baron"] = new(5500, 3920),
            ["Diamond DA62"] = new(5071, 3461),
            ["DA62"] = new(5071, 3461),
            ["Cessna 404 Titan"] = new(8400, 4850),

            // --- TURBOÉLICES ---
            ["Asobo TBM 930"] = new(7394, 4629),
            ["TBM 930"] = new(7394, 4629),
            ["C208B"] = new(8750, 4700),
            ["Caravan"] = new(8750, 4700),
            ["Cessna 208B Grand Caravan"] = new(8750, 4700),
            ["PC-12 NGX"] = new(10450, 6800),
            ["PC-12"] = new(10450, 6800),
            ["Beechcraft King Air C90GTx"] = new(10485, 6950),
            ["King Air C90"] = new(10485, 6950),
            ["Beechcraft King Air 250"] = new(12500, 8780),
            ["King Air 250"] = new(12500, 8780),
            ["Beechcraft King Air"] = new(15000, 9955),
            ["King Air 350"] = new(15000, 9955),
            ["DHC-6-300 Twin"] = new(12500, 7415),
            ["Twin Otter"] = new(12500, 7415),
            ["Cessna 408 SkyCourier"] = new(19000, 11000),
            ["SkyCourier"] = new(19000, 11000),
            ["ES30"] = new(26000, 15000),
            ["ATR 42-600"] = new(41005, 24800),
            ["ATR 42"] = new(41005, 24800),
            ["ATR 72-600"] = new(50705, 29320),
            ["ATR 72"] = new(50705, 29320),

            // --- JATOS EXECUTIVOS ---
            ["Cessna Citation CJ4"] = new(17110, 10280),
            ["Citation CJ4"] = new(17110, 10280),
            ["CJ4"] = new(17110, 10280),
            ["Pilatus PC-24"] = new(18300, 11000),
            ["PC-24"] = new(18300, 11000),
            ["Cessna Citation Longitude"] = new(39500, 23389),
            ["Citation Longitude"] = new(39500, 23389),

            // --- LINHA AÉREA COMERCIAL / CARGA PESADA ---
            ["737 Max 8"] = new(181200, 99200),
            ["Boeing 737 Max 8"] = new(181200, 99200),
            ["737"] = new(181200, 99200),
            ["A320neo"] = new(174165, 90400),
            ["Airbus A320neo"] = new(174165, 90400),
            ["A320"] = new(174165, 90400),
            ["A321"] = new(206100, 102400),
            ["Airbus A321"] = new(206100, 102400),
            ["Airbus A321LR"] = new(213850, 104000),
            ["A400M"] = new(310850, 168650),
            ["A310"] = new(330700, 174600),
            ["A330-200"] = new(533500, 264000),
            ["Airbus A330-200"] = new(533500, 264000),
            ["Airbus A330-300"] = new(533500, 274500),
            ["A330-BelugaXL"] = new(500450, 281100),
            ["Boeing 787-10 Dreamliner"] = new(560000, 298700),
            ["Boeing 747-400"] = new(875000, 394100),
            ["Boeing 747-400 Global Supertanker"] = new(870000, 400000),
            ["747-8i"] = new(987000, 485000),
            ["Boeing 747-8"] = new(987000, 485000)
        };

        /// <summary>
        /// Obtém a especificação completa de MTOW e Peso Vazio para o modelo informado.
        /// </summary>
        public static AircraftWeightSpec GetSpec(string? model, AircraftType? aircraftType = null)
        {
            string cleanModel = (model ?? aircraftType?.Model ?? string.Empty).Trim();

            // 1. Busca exata (case-insensitive)
            if (OfficialSpecs.TryGetValue(cleanModel, out var exactSpec))
            {
                return exactSpec;
            }

            // 2. Busca por contenção de tokens conhecidos
            foreach (var kvp in OfficialSpecs)
            {
                if (cleanModel.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Key.IndexOf(cleanModel, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }

            // 3. Fallback inteligente baseado nas propriedades da aeronave cadastrada
            if (aircraftType != null && aircraftType.MaxPayloadLbs > 0)
            {
                bool isHeavy = string.Equals(aircraftType.Type, "Comercial", StringComparison.OrdinalIgnoreCase) || aircraftType.MaxPax >= 20;
                double fuelFuelWeightLbs = aircraftType.FuelTankGallons * (isHeavy ? 6.7 : 6.0);

                if (isHeavy)
                {
                    double estimatedMtow = (aircraftType.MaxPayloadLbs * 2.2) + fuelFuelWeightLbs;
                    double estimatedEmpty = Math.Max(20000, estimatedMtow - aircraftType.MaxPayloadLbs - fuelFuelWeightLbs);
                    return new AircraftWeightSpec(Math.Round(estimatedMtow, 0), Math.Round(estimatedEmpty, 0));
                }
                else
                {
                    double estimatedEmpty = Math.Max(1000, aircraftType.MaxPayloadLbs * 1.3);
                    double estimatedMtow = estimatedEmpty + aircraftType.MaxPayloadLbs + fuelFuelWeightLbs;
                    return new AircraftWeightSpec(Math.Round(estimatedMtow, 0), Math.Round(estimatedEmpty, 0));
                }
            }

            // 4. Default neutro seguro (C172 class)
            return new AircraftWeightSpec(2550, 1665);
        }

        /// <summary>
        /// Retorna o MTOW oficial (em lbs) para o modelo/aeronave fornecida.
        /// </summary>
        public static double GetMtow(string? model, AircraftType? aircraftType = null)
        {
            return GetSpec(model, aircraftType).MtowLbs;
        }

        /// <summary>
        /// Retorna o Peso Vazio oficial (em lbs) para o modelo/aeronave fornecida.
        /// </summary>
        public static double GetEmptyWeight(string? model, AircraftType? aircraftType = null)
        {
            return GetSpec(model, aircraftType).EmptyWeightLbs;
        }
    }
}
