using System;
using System.Collections.Generic;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class PartDisplayStateTests
    {
        private readonly PartBandSettings _bands = new(YellowBelowPct: 40.0, RedBelowPct: 20.0);
        private const double GrowthPerHour = 0.4;

        [Fact]
        public void Nunca_vista_e_incerta()
        {
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 80.0,
                KnownHealth = null,
                KnownAtAirframeHours = null,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 50.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Uncertain, info.State);
            Assert.Equal(PartKnownBand.Unknown, info.LastKnownBand);
            Assert.Null(info.HoursSinceSeen);
        }

        [Fact]
        public void Verde_recem_vista_e_verde()
        {
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 80.0,
                KnownHealth = 80.0,
                KnownAtAirframeHours = 50.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 50.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Green, info.State);
            Assert.Equal(PartKnownBand.Green, info.LastKnownBand);
            Assert.Equal(0.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Verde_alta_continua_verde_por_muito_tempo()
        {
            // known 80, visto há 50 h: 80 - 0.4*50 = 80 - 20 = 60 >= 40 -> Green
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 80.0,
                KnownHealth = 80.0,
                KnownAtAirframeHours = 50.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Green, info.State);
            Assert.Equal(PartKnownBand.Green, info.LastKnownBand);
            Assert.Equal(50.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Verde_perto_do_limite_vira_incerta()
        {
            // known 43, visto há 10 h: 43 - 0.4*10 = 43 - 4 = 39 < 40 -> Uncertain, LastKnownBand Green, HoursSinceSeen 10
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 43.0,
                KnownHealth = 43.0,
                KnownAtAirframeHours = 90.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Uncertain, info.State);
            Assert.Equal(PartKnownBand.Green, info.LastKnownBand);
            Assert.Equal(10.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Verde_perto_do_limite_ainda_verde()
        {
            // known 43, visto há 5 h: 43 - 0.4*5 = 41 >= 40 -> não incerta; abaixo de 80 -> Worn (gasta mas boa)
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 43.0,
                KnownHealth = 43.0,
                KnownAtAirframeHours = 95.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Worn, info.State);
            Assert.Equal(PartKnownBand.Green, info.LastKnownBand);
            Assert.Equal(5.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Amarela_vira_incerta_quando_pode_estar_vermelha()
        {
            // known 25, visto há 15 h: 25 - 0.4*15 = 25 - 6 = 19 < 20 -> Uncertain, LastKnownBand Yellow
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 25.0,
                KnownHealth = 25.0,
                KnownAtAirframeHours = 85.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Uncertain, info.State);
            Assert.Equal(PartKnownBand.Yellow, info.LastKnownBand);
            Assert.Equal(15.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Amarela_recente_e_amarela()
        {
            // known 30, visto há 1 h: 30 - 0.4*1 = 29.6 >= 20 -> Yellow
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 30.0,
                KnownHealth = 30.0,
                KnownAtAirframeHours = 99.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Yellow, info.State);
            Assert.Equal(PartKnownBand.Yellow, info.LastKnownBand);
            Assert.Equal(1.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Vermelha_nunca_vira_incerta()
        {
            // known 10, visto há 100 h -> Red
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 10.0,
                KnownHealth = 10.0,
                KnownAtAirframeHours = 0.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Red, info.State);
            Assert.Equal(PartKnownBand.Red, info.LastKnownBand);
            Assert.Equal(100.0, info.HoursSinceSeen!.Value, 3);
        }

        [Fact]
        public void Quebrada_e_quebrada_mesmo_sem_inspecao()
        {
            // IsBroken true, KnownHealth null -> Broken
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 0.0,
                KnownHealth = null,
                KnownAtAirframeHours = null,
                IsBroken = true
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Broken, info.State);
            Assert.Equal(PartKnownBand.Broken, info.LastKnownBand);
        }

        [Fact]
        public void Nao_usa_a_saude_real()
        {
            // Health 5, KnownHealth 90 visto há 0 h -> Green
            var part = new AircraftPart
            {
                Id = Guid.NewGuid(),
                PartId = "engine.cylinders",
                Health = 5.0,
                KnownHealth = 90.0,
                KnownAtAirframeHours = 100.0,
                IsBroken = false
            };

            var info = PartHangarView.DisplayState(part, airframeHours: 100.0, growthPerHour: GrowthPerHour, _bands);

            Assert.Equal(PartDisplayState.Green, info.State);
            Assert.Equal(PartKnownBand.Green, info.LastKnownBand);
        }

        [Fact]
        public void Resumo_pior_estado_e_contagem()
        {
            var items = new[]
            {
                new PartDisplayInfo(PartDisplayState.Green, PartKnownBand.Green, 0),
                new PartDisplayInfo(PartDisplayState.Green, PartKnownBand.Green, 1),
                new PartDisplayInfo(PartDisplayState.Yellow, PartKnownBand.Yellow, 2),
                new PartDisplayInfo(PartDisplayState.Uncertain, PartKnownBand.Green, 15)
            };

            var (worst, counts) = PartHangarView.Summarize(items);

            Assert.Equal(PartDisplayState.Yellow, worst);
            Assert.Equal(2, counts[PartDisplayState.Green]);
            Assert.Equal(1, counts[PartDisplayState.Yellow]);
            Assert.Equal(1, counts[PartDisplayState.Uncertain]);
            Assert.Equal(0, counts[PartDisplayState.Red]);
            Assert.Equal(0, counts[PartDisplayState.Broken]);
        }

        [Fact]
        public void Resumo_quebrada_e_pior_que_vermelha()
        {
            var items = new[]
            {
                new PartDisplayInfo(PartDisplayState.Red, PartKnownBand.Red, 10),
                new PartDisplayInfo(PartDisplayState.Broken, PartKnownBand.Broken, 5),
                new PartDisplayInfo(PartDisplayState.Green, PartKnownBand.Green, 0)
            };

            var (worst, counts) = PartHangarView.Summarize(items);

            Assert.Equal(PartDisplayState.Broken, worst);
            Assert.Equal(1, counts[PartDisplayState.Broken]);
            Assert.Equal(1, counts[PartDisplayState.Red]);
            Assert.Equal(1, counts[PartDisplayState.Green]);
        }

        [Fact]
        public void GroupLifeText_OleoComHorasRestantes()
        {
            var aircraft = new PilotAircraft
            {
                HoursUntilNext50hInspection = 7.5
            };

            var text = PartHangarView.GroupLifeText(ComponentKind.Oil, aircraft, EngineClass.Piston);

            Assert.Equal("Troca de óleo em 8 h (de 12)", text);
        }

        [Fact]
        public void GroupLifeText_IgnicaoVencida()
        {
            var aircraft = new PilotAircraft
            {
                HoursUntilNext500hInspection = -3.0
            };

            var text = PartHangarView.GroupLifeText(ComponentKind.Ignition, aircraft, EngineClass.Piston);

            Assert.Equal("Revisão de ignição vencida há 3 h", text);
        }

        [Fact]
        public void GroupLifeText_PneusEVazio()
        {
            var aircraft = new PilotAircraft();

            var text = PartHangarView.GroupLifeText(ComponentKind.Tires, aircraft, EngineClass.Piston);

            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void GroupLifeText_MotorTboRestante()
        {
            var aircraft = new PilotAircraft
            {
                EngineTboRemainingHours = 450.2
            };

            var text = PartHangarView.GroupLifeText(ComponentKind.Engine, aircraft, EngineClass.Piston);

            Assert.Equal("TBO: 450 h restantes (de 500)", text);
        }

        [Fact]
        public void GroupLifeText_TurbinaSecaoQuenteSemTexto()
        {
            var aircraft = new PilotAircraft
            {
                EngineTboRemainingHours = 800.0
            };

            var text = PartHangarView.GroupLifeText(ComponentKind.HotSection, aircraft, EngineClass.Turboprop);

            Assert.Equal(string.Empty, text);
        }

        private static AircraftPart Vista(double known, double knownAt) => new()
        {
            Id = Guid.NewGuid(),
            PartId = "engine.cylinders",
            Health = known,
            KnownHealth = known,
            KnownAtAirframeHours = knownAt,
        };

        [Theory]
        [InlineData(80.0, 0.0, PartDisplayState.Green)]   // limite: 80 conta como nova
        [InlineData(79.9, 0.0, PartDisplayState.Worn)]
        [InlineData(60.0, 0.0, PartDisplayState.Worn)]
        [InlineData(40.0, 0.0, PartDisplayState.Worn)]    // 40 ainda é verde (não amarelo)
        [InlineData(39.9, 0.0, PartDisplayState.Yellow)]
        [InlineData(82.0, 10.0, PartDisplayState.Green)]  // a marca de 80 não gera "?"
        [InlineData(60.0, 60.0, PartDisplayState.Uncertain)] // 60 - 24 = 36 < 40
        public void Faixa_gasta_mas_boa_entre_80_e_40(double known, double horasDesdeVista, PartDisplayState esperado)
        {
            var info = PartHangarView.DisplayState(Vista(known, 100.0), 100.0 + horasDesdeVista, GrowthPerHour, _bands);
            Assert.Equal(esperado, info.State);
        }

        [Fact]
        public void Resumo_gasta_e_pior_que_nova_e_melhor_que_incerta()
        {
            var (worst1, counts) = PartHangarView.Summarize(new[]
            {
                new PartDisplayInfo(PartDisplayState.Green, PartKnownBand.Green, 0),
                new PartDisplayInfo(PartDisplayState.Worn, PartKnownBand.Green, 0),
            });
            Assert.Equal(PartDisplayState.Worn, worst1);
            Assert.Equal(1, counts[PartDisplayState.Worn]);

            var (worst2, _) = PartHangarView.Summarize(new[]
            {
                new PartDisplayInfo(PartDisplayState.Worn, PartKnownBand.Green, 0),
                new PartDisplayInfo(PartDisplayState.Uncertain, PartKnownBand.Green, 30),
            });
            Assert.Equal(PartDisplayState.Uncertain, worst2);
        }
    }
}
