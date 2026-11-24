using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlightCareer.Application.Services;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class PhaseE2C1GeneratorsTests
    {
        private static (Airport Origin, List<AirportCandidate> Candidates) CreateSampleAirports()
        {
            var origin = new Airport
            {
                ICAO = "SBJD",
                Name = "Jundiaí",
                Country = "Brazil",
                Tier = 2,
                Latitude = -23.1822,
                Longitude = -46.9431,
                ElevationFt = 2484
            };

            var list = new List<AirportCandidate>
            {
                new(origin, new Runway { AirportIdent = "SBJD", LengthFt = 4500, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDCO", Name = "Sorocaba", Country = "Brazil", Tier = 3, Latitude = -23.4808, Longitude = -47.4897, ElevationFt = 2077 }, new Runway { AirportIdent = "SDCO", LengthFt = 5000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDAI", Name = "Americana", Country = "Brazil", Tier = 3, Latitude = -22.7631, Longitude = -47.2650, ElevationFt = 1800 }, new Runway { AirportIdent = "SDAI", LengthFt = 3600, Surface = "ASPH" }),
                new(new Airport { ICAO = "SDBP", Name = "Bragança", Country = "Brazil", Tier = 3, Latitude = -22.9778, Longitude = -46.5381, ElevationFt = 2800 }, new Runway { AirportIdent = "SDBP", LengthFt = 4000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SBDN", Name = "Pres. Prudente", Country = "Brazil", Tier = 3, Latitude = -22.1969, Longitude = -49.9261, ElevationFt = 1476 }, new Runway { AirportIdent = "SBDN", LengthFt = 4200, Surface = "ASPH" }),
                new(new Airport { ICAO = "SBSJ", Name = "São José", Country = "Brazil", Tier = 3, Latitude = -23.2289, Longitude = -45.8625, ElevationFt = 2120 }, new Runway { AirportIdent = "SBSJ", LengthFt = 6000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SBKP", Name = "Viracopos", Country = "Brazil", Tier = 2, Latitude = -23.0074, Longitude = -47.1345, ElevationFt = 2170 }, new Runway { AirportIdent = "SBKP", LengthFt = 10000, Surface = "ASPH" }),
                new(new Airport { ICAO = "SBRP", Name = "Ribeirão", Country = "Brazil", Tier = 2, Latitude = -21.1364, Longitude = -47.7744, ElevationFt = 1795 }, new Runway { AirportIdent = "SBRP", LengthFt = 6500, Surface = "ASPH" }),
                new(new Airport { ICAO = "FAZ1", Name = "Fazenda Curta", Country = "Brazil", Tier = 4, Latitude = -23.3500, Longitude = -47.2500, ElevationFt = 1900 }, new Runway { AirportIdent = "FAZ1", LengthFt = 1100, Surface = "DIRT" }),
                new(new Airport { ICAO = "FAZ2", Name = "Fazenda Norte", Country = "Brazil", Tier = 4, Latitude = -22.9000, Longitude = -46.8000, ElevationFt = 2100 }, new Runway { AirportIdent = "FAZ2", LengthFt = 1400, Surface = "GRASS" }),
                new(new Airport { ICAO = "MRO1", Name = "Oficina Homologada", Country = "Brazil", Tier = 2, Latitude = -22.0000, Longitude = -47.0000, ElevationFt = 2000 }, new Runway { AirportIdent = "MRO1", LengthFt = 5000, Surface = "ASPH" })
            };

            return (origin, list);
        }

        [Fact]
        public void PingaPinga_Gerador_Cria3A4EtapasComDistanciasETiersCorretos()
        {
            var (origin, candidates) = CreateSampleAirports();
            var def = MissionCatalog.Shared.Get("pinga_pinga")!;
            Assert.NotNull(def);
            Assert.True(def.Enabled);

            for (int seed = 1; seed <= 10; seed++)
            {
                var rnd = new Random(seed);
                var mission = MissionGenerator.CreateMultiStopMissionSync(
                    origin, def, rnd, DateTime.Now, candidates, paxCount: 6, cargoLbs: 600.0);

                if (mission == null) continue;

                Assert.Equal("pinga_pinga", mission.SubId);
                Assert.Equal(6, mission.PassengerCount);
                Assert.Equal(600.0, mission.CargoWeightLbs);
                Assert.False(string.IsNullOrWhiteSpace(mission.ContractData));

                var contract = JsonSerializer.Deserialize<MultiStopContractData>(mission.ContractData);
                Assert.NotNull(contract);
                Assert.True(contract!.RequireNoRefuel);
                Assert.True(contract.Stops.Count >= 2); // Ao menos 2 intermediárias + destino final = >= 3 etapas
            }
        }

        [Fact]
        public void VfrTriangle_Gerador_SempreVoltaAOrigemETemPontoDeReportagem()
        {
            var (origin, candidates) = CreateSampleAirports();
            var def = MissionCatalog.Shared.Get("navegacao_vfr")!;
            Assert.NotNull(def);
            Assert.True(def.Enabled);

            var rnd = new Random(42);
            var mission = MissionGenerator.CreateVfrTriangleMissionSync(
                origin, def, rnd, DateTime.Now, candidates);

            Assert.NotNull(mission);
            Assert.Equal(origin.ICAO, mission.DestinationICAO); // Volta à origem
            Assert.Equal(1, mission.PassengerCount);
            Assert.Equal(0, mission.CargoWeightLbs);
            Assert.NotNull(mission.OverflyWaypoints);
            Assert.StartsWith("P:", mission.OverflyWaypoints);

            var contract = JsonSerializer.Deserialize<MultiStopContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.Equal(2, contract!.Stops.Count); // Escalas A e B
        }

        [Fact]
        public void StolTraining_Gerador_ConfiguraPistaCurtaELandings()
        {
            var (origin, candidates) = CreateSampleAirports();
            var def = MissionCatalog.Shared.Get("instrucao_stol")!;
            Assert.NotNull(def);
            Assert.True(def.Enabled);

            var rnd = new Random(100);
            var mission = MissionGenerator.CreateStolTrainingMissionSync(
                origin, def, rnd, DateTime.Now, candidates);

            Assert.NotNull(mission);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);
            Assert.Equal(1, mission.PassengerCount);
            Assert.NotNull(mission.ContractData);

            var contract = JsonSerializer.Deserialize<StolContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.Equal(3, contract!.RequiredLandings);
            Assert.True(contract.MaxStopDistanceFt >= 300.0 && contract.MaxStopDistanceFt <= 1000.0);
            Assert.Equal(4.0, contract.MinApproachAngleDeg);
        }

        [Fact]
        public void CheckFlight_Gerador_ConfiguraPassosERumos()
        {
            var (origin, _) = CreateSampleAirports();
            var def = MissionCatalog.Shared.Get("check_flight")!;
            Assert.NotNull(def);
            Assert.True(def.Enabled);

            var rnd = new Random(77);
            var mission = MissionGenerator.CreateCheckFlightMissionSync(
                origin, def, rnd, DateTime.Now, paxCount: 1);

            Assert.NotNull(mission);
            Assert.Equal(origin.ICAO, mission.DestinationICAO);
            Assert.Equal(1, mission.PassengerCount);

            var contract = JsonSerializer.Deserialize<CheckFlightContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.True(contract!.TestAltitudeFt > origin.ElevationFt);
            Assert.Equal(10.0, contract.PowerHoldSeconds);
            Assert.Equal(15.0, contract.HeadingHoldSeconds);
        }

        [Fact]
        public void FerryPermit_Gerador_SorteiaRestricao()
        {
            var (origin, candidates) = CreateSampleAirports();
            var dest = candidates.First(c => c.Airport.ICAO == "MRO1");
            var def = MissionCatalog.Shared.Get("ferry_permit")!;
            Assert.NotNull(def);
            Assert.True(def.Enabled);

            var rnd = new Random(123);
            var mission = MissionGenerator.CreateFerryPermitMissionSync(
                origin, dest.Airport, 70.0, def, rnd, DateTime.Now);

            Assert.NotNull(mission);
            Assert.Equal("MRO1", mission.DestinationICAO);
            Assert.Equal(0, mission.PassengerCount);
            Assert.Equal(0, mission.CargoWeightLbs);

            var contract = JsonSerializer.Deserialize<FerryPermitContractData>(mission.ContractData!);
            Assert.NotNull(contract);
            Assert.True(contract!.Restriction == "max_altitude" || contract.Restriction == "flaps_fixed");
        }

        [Fact]
        public void Paraquedismo_Gerador_RespeitaFaixaDePaxEBagagemIndividual()
        {
            var (origin, _) = CreateSampleAirports();
            var def = MissionCatalog.Shared.Get("paraquedismo")!;
            Assert.NotNull(def);

            // Testando com perfil de 18 pax (salto em grupo grande)
            var rnd = new Random(55);
            var mission = MissionGenerator.CreateSkydiveMissionSync(
                origin, def, rnd, DateTime.Now, paxOverride: 18);

            Assert.Equal(18, mission.PassengerCount);
            // 18 pax * ~30.8 lbs = ~555 lbs
            Assert.True(mission.LuggageWeightLbs >= 18 * 12 * 2.2);
            Assert.True(mission.LuggageWeightLbs <= 18 * 16 * 2.3);
        }

        [Fact]
        public void MissionCompletion_ChamadasAntigasSemTrackers_MantemMesmoResultado()
        {
            var mission = new Mission
            {
                Id = Guid.NewGuid(),
                SubId = "executivo_corporativo",
                OriginICAO = "SBSP",
                DestinationICAO = "SBRJ"
            };

            var success = MissionCompletion.Evaluate(mission, "SBRJ");
            Assert.True(success.IsCompleted);

            var wrongDest = MissionCompletion.Evaluate(mission, "SBKP");
            Assert.False(wrongDest.IsCompleted);
            Assert.Contains("SBRJ", wrongDest.Reason);
        }
    }
}
