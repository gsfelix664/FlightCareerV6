using System;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Limits
{
    public class ElectricalLimitsTests
    {
        [Fact]
        public void C172SP_G1000_CarregaLimitesEletricos()
        {
            var loader = new AircraftLimitCatalogLoader();
            var profiles = loader.LoadProfiles();

            var c172 = profiles.FirstOrDefault(p => string.Equals(p.AircraftId, "C172SP G1000", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(c172);

            var limits = c172.Limits.ToDictionary(l => l.Key);

            Assert.True(limits.ContainsKey(LimitKeys.ElecSystemVolts));
            Assert.Equal(28.0, limits[LimitKeys.ElecSystemVolts].Value);
            Assert.Equal(LimitSource.SimVar, limits[LimitKeys.ElecSystemVolts].Source);
            Assert.Equal("volts", limits[LimitKeys.ElecSystemVolts].Unit);

            Assert.True(limits.ContainsKey(LimitKeys.ElecAvionicsCircuitIndex));
            Assert.Equal(3.0, limits[LimitKeys.ElecAvionicsCircuitIndex].Value);
            Assert.Equal(LimitSource.SimVar, limits[LimitKeys.ElecAvionicsCircuitIndex].Source);
            Assert.Equal("index", limits[LimitKeys.ElecAvionicsCircuitIndex].Unit);

            Assert.True(limits.ContainsKey(LimitKeys.ElecAvionicsBusAmpsIndex));
            Assert.Equal(4.0, limits[LimitKeys.ElecAvionicsBusAmpsIndex].Value);
            Assert.Equal(LimitSource.SimVar, limits[LimitKeys.ElecAvionicsBusAmpsIndex].Source);
            Assert.Equal("index", limits[LimitKeys.ElecAvionicsBusAmpsIndex].Unit);

            Assert.True(limits.ContainsKey(LimitKeys.ElecMainBusIndex));
            Assert.Equal(1.0, limits[LimitKeys.ElecMainBusIndex].Value);
            Assert.Equal(LimitSource.SimVar, limits[LimitKeys.ElecMainBusIndex].Source);
            Assert.Equal("index", limits[LimitKeys.ElecMainBusIndex].Unit);

            Assert.True(limits.ContainsKey(LimitKeys.AlternatorRatedAmps));
            Assert.Equal(60.0, limits[LimitKeys.AlternatorRatedAmps].Value);
            Assert.Equal(LimitSource.POH, limits[LimitKeys.AlternatorRatedAmps].Source);
            Assert.Equal("amperes", limits[LimitKeys.AlternatorRatedAmps].Unit);
        }
    }
}
