using System;
using System.Collections.Generic;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class HudRouteResolverTests
    {
        private static readonly AirportPosition Sbsj = new("SBSJ", -23.2289, -45.8711);
        private static readonly AirportPosition Snza = new("SNZA", -22.2889, -45.9194);

        private static AirportPosition? Lookup(string icao) => icao.ToUpperInvariant() switch
        {
            "SBSJ" => Sbsj,
            "SNZA" => Snza,
            _ => null
        };

        [Fact]
        public void Resolve_TrocaDeMissaoEntreVoosConsecutivos_AtualizaDestinoMesmoJaPreenchido()
        {
            // Simula voo anterior SBSJ -> SBSJ que deixou OriginLat/DestLat preenchidos com SBSJ
            var activeMissions = new List<Mission>
            {
                new() { OriginICAO = "SBSJ", DestinationICAO = "SNZA", DistanceNM = 57 }
            };

            var result = HudRouteResolver.Resolve(
                isMonitoring: true,
                activeMissions: activeMissions,
                acceptedMissions: null,
                aircraftLat: -23.2236,
                aircraftLon: -45.8645,
                currentOriginIcao: "SBSJ",
                currentOriginLat: Sbsj.Latitude,
                currentOriginLon: Sbsj.Longitude,
                airportLookup: Lookup);

            Assert.Equal("SBSJ", result.OriginIcao);
            Assert.Equal(Sbsj.Latitude, result.OriginLat);
            Assert.Equal(Sbsj.Longitude, result.OriginLon);
            Assert.Equal("SNZA", result.DestIcao);
            Assert.Equal(Snza.Latitude, result.DestLat);
            Assert.Equal(Snza.Longitude, result.DestLon);
            Assert.Contains("SBSJ ➔ SNZA", result.RouteText);
        }

        [Fact]
        public void Resolve_MissaoAceitaAntesDoMonitoramento_UsaAcceptedEIgnoraVooAntigoEncerrado()
        {
            var oldActiveMissions = new List<Mission>
            {
                new() { OriginICAO = "SBSJ", DestinationICAO = "SBSJ", DistanceNM = 20 }
            };
            var acceptedMissions = new List<Mission>
            {
                new() { OriginICAO = "SBSJ", DestinationICAO = "SNZA", DistanceNM = 57 }
            };

            var result = HudRouteResolver.Resolve(
                isMonitoring: false,
                activeMissions: oldActiveMissions,
                acceptedMissions: acceptedMissions,
                aircraftLat: -23.2236,
                aircraftLon: -45.8645,
                currentOriginIcao: "SBSJ",
                currentOriginLat: Sbsj.Latitude,
                currentOriginLon: Sbsj.Longitude,
                airportLookup: Lookup);

            Assert.Equal("SBSJ", result.OriginIcao);
            Assert.Equal("SNZA", result.DestIcao);
            Assert.Equal(Snza.Latitude, result.DestLat);
            Assert.Equal(Snza.Longitude, result.DestLon);
        }

        [Fact]
        public void Resolve_VooMonitoradoLivre_ZeraDestinoEPreservaPartida()
        {
            var result = HudRouteResolver.Resolve(
                isMonitoring: true,
                activeMissions: new List<Mission>(),
                acceptedMissions: null,
                aircraftLat: -23.20,
                aircraftLon: -45.85,
                currentOriginIcao: "PARTIDA",
                currentOriginLat: -23.22,
                currentOriginLon: -45.87,
                airportLookup: Lookup);

            Assert.Equal("VOO LOCAL / CIRCUITO", result.RouteText);
            Assert.Equal("PARTIDA", result.OriginIcao);
            Assert.Equal(-23.22, result.OriginLat);
            Assert.Equal(-45.87, result.OriginLon);
            Assert.Equal(string.Empty, result.DestIcao);
            Assert.Equal(0, result.DestLat);
            Assert.Equal(0, result.DestLon);
        }

        [Fact]
        public void Resolve_EmSoloSemContratosAceitos_ZeraOrigemEDestino()
        {
            var result = HudRouteResolver.Resolve(
                isMonitoring: false,
                activeMissions: new List<Mission>(),
                acceptedMissions: new List<Mission>(),
                aircraftLat: -23.22,
                aircraftLon: -45.87,
                currentOriginIcao: "SBSJ",
                currentOriginLat: Sbsj.Latitude,
                currentOriginLon: Sbsj.Longitude,
                airportLookup: Lookup);

            Assert.Equal("EM SOLO", result.RouteText);
            Assert.Equal(string.Empty, result.OriginIcao);
            Assert.Equal(0, result.OriginLat);
            Assert.Equal(0, result.OriginLon);
            Assert.Equal(string.Empty, result.DestIcao);
            Assert.Equal(0, result.DestLat);
            Assert.Equal(0, result.DestLon);
        }

        [Fact]
        public void Resolve_OrigemAusenteNoLookup_UsaPosicaoDaAeronaveEResolveDestino()
        {
            var acceptedMissions = new List<Mission>
            {
                new() { OriginICAO = "XXXX", DestinationICAO = "SNZA", DistanceNM = 57 }
            };

            var result = HudRouteResolver.Resolve(
                isMonitoring: false,
                activeMissions: null,
                acceptedMissions: acceptedMissions,
                aircraftLat: -23.2236,
                aircraftLon: -45.8645,
                currentOriginIcao: string.Empty,
                currentOriginLat: 0,
                currentOriginLon: 0,
                airportLookup: Lookup);

            Assert.Equal("XXXX", result.OriginIcao);
            Assert.Equal(-23.2236, result.OriginLat);
            Assert.Equal(-45.8645, result.OriginLon);
            Assert.Equal("SNZA", result.DestIcao);
            Assert.Equal(Snza.Latitude, result.DestLat);
            Assert.Equal(Snza.Longitude, result.DestLon);
        }

        [Fact]
        public void Resolve_DestinoAusenteNoLookup_ZeraCoordenadasDeDestino()
        {
            var activeMissions = new List<Mission>
            {
                new() { OriginICAO = "SBSJ", DestinationICAO = "ZZZZ", DistanceNM = 50 }
            };

            var result = HudRouteResolver.Resolve(
                isMonitoring: true,
                activeMissions: activeMissions,
                acceptedMissions: null,
                aircraftLat: -23.2236,
                aircraftLon: -45.8645,
                currentOriginIcao: "SBSJ",
                currentOriginLat: Sbsj.Latitude,
                currentOriginLon: Sbsj.Longitude,
                airportLookup: Lookup);

            Assert.Equal("SBSJ", result.OriginIcao);
            Assert.Equal("ZZZZ", result.DestIcao);
            Assert.Equal(0, result.DestLat);
            Assert.Equal(0, result.DestLon);
        }
    }
}
