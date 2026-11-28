using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Fornece limites elétricos padrão de teste exclusivamente para o harness de fuzz testing e suítes de teste de voos sintéticos.
    /// <para>
    /// <b>ATENÇÃO:</b> Esta classe é utilizada <b>EXCLUSIVAMENTE</b> no <c>RunFuzz</c> do CLI e nos testes unitários de fuzz.
    /// <b>NUNCA</b> deve ser utilizada no <c>AircraftLimitSeeder</c>, no <c>AircraftLimitResolver</c>, em serviços de jogo ou na UI,
    /// pois os índices reais dos barramentos dependem do <c>systems.cfg</c> específico de cada aeronave e índices incorretos em voo
    /// gerariam desgaste elétrico espúrio.
    /// </para>
    /// </summary>
    public static class FuzzTestDefaults
    {
        public const string TestReference = "padrão de teste do fuzz — não usar no jogo";

        /// <summary>
        /// Devolve uma cópia do perfil de limites da aeronave acrescentando apenas as chaves de índices elétricos
        /// que estiverem ausentes ('elecMainBusIndex' = 1, 'elecAvionicsCircuitIndex' = 3, 'elecAvionicsBusAmpsIndex' = 4,
        /// calibrados a partir do C172SP G1000), com origem ClassDefault e referência de teste.
        /// Caso a aeronave possua a característica 'noElectrical', devolve o perfil inalterado.
        /// </summary>
        /// <param name="profile">Perfil da aeronave a ser verificado.</param>
        /// <returns>Perfil copiado com os índices elétricos de teste injetados ou o perfil original se não aplicável.</returns>
        public static AircraftLimitProfile WithTestElectricalIndices(AircraftLimitProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            // Aeronaves sem sistema elétrico (ex.: Piper J-3 Cub, CAP-4) não recebem índices elétricos de teste
            if (profile.Features != null &&
                profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
            {
                return profile;
            }

            var existingLimits = profile.Limits ?? Array.Empty<AircraftLimit>();

            bool hasMainBus = existingLimits.Any(l => string.Equals(l.Key, LimitKeys.ElecMainBusIndex, StringComparison.OrdinalIgnoreCase));
            bool hasAvionicsCircuit = existingLimits.Any(l => string.Equals(l.Key, LimitKeys.ElecAvionicsCircuitIndex, StringComparison.OrdinalIgnoreCase));
            bool hasAvionicsBusAmps = existingLimits.Any(l => string.Equals(l.Key, LimitKeys.ElecAvionicsBusAmpsIndex, StringComparison.OrdinalIgnoreCase));
            bool hasNoAvionicsMaster = profile.Features != null &&
                profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoAvionicsMaster, StringComparison.OrdinalIgnoreCase));

            if (hasMainBus && (hasAvionicsCircuit || hasNoAvionicsMaster) && hasAvionicsBusAmps)
            {
                return profile;
            }

            var newLimits = new List<AircraftLimit>(existingLimits);

            if (!hasMainBus)
            {
                newLimits.Add(new AircraftLimit(
                    Key: LimitKeys.ElecMainBusIndex,
                    Value: 1.0,
                    Unit: LimitKeys.UnitOf(LimitKeys.ElecMainBusIndex),
                    Source: LimitSource.ClassDefault,
                    Reference: TestReference,
                    Notes: null
                ));
            }

            if (!hasAvionicsCircuit && !hasNoAvionicsMaster)
            {
                newLimits.Add(new AircraftLimit(
                    Key: LimitKeys.ElecAvionicsCircuitIndex,
                    Value: 3.0,
                    Unit: LimitKeys.UnitOf(LimitKeys.ElecAvionicsCircuitIndex),
                    Source: LimitSource.ClassDefault,
                    Reference: TestReference,
                    Notes: null
                ));
            }

            if (!hasAvionicsBusAmps)
            {
                newLimits.Add(new AircraftLimit(
                    Key: LimitKeys.ElecAvionicsBusAmpsIndex,
                    Value: 4.0,
                    Unit: LimitKeys.UnitOf(LimitKeys.ElecAvionicsBusAmpsIndex),
                    Source: LimitSource.ClassDefault,
                    Reference: TestReference,
                    Notes: null
                ));
            }

            return profile with { Limits = newLimits };
        }
    }
}
