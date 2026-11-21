using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Parts;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class CabinConversionTests
    {
        [Fact]
        public void Cabin_SemFeature_NaoConverte()
        {
            // 1. Feature ausente em lista genérica
            var semFeature = new[] { AircraftFeatures.Piston, AircraftFeatures.SingleEngine };
            Assert.False(CabinConversion.HasConvertibleCabin(semFeature));
            Assert.False(CabinConversion.CanConvert(semFeature));

            var comFeature = new[] { AircraftFeatures.Turboprop, AircraftFeatures.ConvertibleCabin };
            Assert.True(CabinConversion.HasConvertibleCabin(comFeature));
            Assert.True(CabinConversion.CanConvert(comFeature));

            // Null ou vazio
            Assert.False(CabinConversion.HasConvertibleCabin(null));
            Assert.False(CabinConversion.HasConvertibleCabin(Array.Empty<string>()));

            // 2. Perfis reais do catálogo por arquivo JSON
            var loader = new AircraftLimitCatalogLoader();
            var dir = loader.ResolveAircraftDataDirectory();

            var arquivosComCabineConversivel = new[]
            {
                "c208b.json",
                "pc-12-ngx.json",
                "beechcraft-king-air.json",
                "beechcraft-king-air-c90gtx.json",
                "beechcraft-king-air-250.json",
                "dhc-6-300-twin.json",
                "atr-42-600.json",
                "atr-72-600.json",
                "cessna-408-skycourier.json",
                "cessna-404-titan.json",
                "cessna-207t-stationair.json"
            };

            foreach (var arquivo in arquivosComCabineConversivel)
            {
                var caminho = Path.Combine(dir, arquivo);
                Assert.True(File.Exists(caminho), $"Arquivo de perfil '{arquivo}' deve existir.");
                var json = File.ReadAllText(caminho);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var features = doc.RootElement.GetProperty("features").EnumerateArray().Select(e => e.GetString()!).ToList();
                Assert.True(CabinConversion.HasConvertibleCabin(features), $"Arquivo '{arquivo}' deve possuir feature 'convertibleCabin'.");
            }

            var arquivosSemCabineConversivel = new[]
            {
                "cessna-c152.json",
                "c172sp-g1000.json",
                "737-max-8.json",
                "a320neo.json",
                "piper-j-3-cub.json"
            };

            foreach (var arquivo in arquivosSemCabineConversivel)
            {
                var caminho = Path.Combine(dir, arquivo);
                Assert.True(File.Exists(caminho), $"Arquivo de perfil '{arquivo}' deve existir.");
                var json = File.ReadAllText(caminho);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var features = doc.RootElement.GetProperty("features").EnumerateArray().Select(e => e.GetString()!).ToList();
                Assert.False(CabinConversion.HasConvertibleCabin(features), $"Arquivo '{arquivo}' NÃO deve possuir feature 'convertibleCabin'.");
            }
        }

        [Fact]
        public void Cabin_CustoEParadaPorPorte()
        {
            // Leve: MTOW < 12.500 lb (ex: C208B MTOW 8.750 lb)
            Assert.Equal(800.0, CabinConversion.GetCost(AircraftSizeClass.Light));
            Assert.Equal(4.0, CabinConversion.GetDowntimeHours(AircraftSizeClass.Light));
            Assert.Equal(800.0, CabinConversion.GetCost(8750.0));
            Assert.Equal(4.0, CabinConversion.GetDowntimeHours(8750.0));

            // Médio: 12.500 <= MTOW <= 60.000 lb (ex: ATR-72 MTOW 50.700 lb)
            Assert.Equal(3000.0, CabinConversion.GetCost(AircraftSizeClass.Medium));
            Assert.Equal(8.0, CabinConversion.GetDowntimeHours(AircraftSizeClass.Medium));
            Assert.Equal(3000.0, CabinConversion.GetCost(50700.0));
            Assert.Equal(8.0, CabinConversion.GetDowntimeHours(50700.0));

            // Pesado: MTOW > 60.000 lb (ex: B737 MTOW 175.000 lb)
            Assert.Equal(12000.0, CabinConversion.GetCost(AircraftSizeClass.Heavy));
            Assert.Equal(24.0, CabinConversion.GetDowntimeHours(AircraftSizeClass.Heavy));
            Assert.Equal(12000.0, CabinConversion.GetCost(175000.0));
            Assert.Equal(24.0, CabinConversion.GetDowntimeHours(175000.0));
        }

        [Fact]
        public void Cabin_CapacidadePassageiro()
        {
            // C208B: PaxMax = 12, CargaMax = 3690 lbs
            var cap1 = CabinConversion.GetEffectiveCapacity(12, 3690.0, "Passageiro");
            Assert.Equal(12, cap1.MaxPax);
            Assert.Equal(922.5, cap1.MaxCargoLbs); // 25% de 3690

            // ATR-72: PaxMax = 70, CargaMax = 16000 lbs
            var cap2 = CabinConversion.GetEffectiveCapacity(70, 16000.0, "Passageiro");
            Assert.Equal(70, cap2.MaxPax);
            Assert.Equal(4000.0, cap2.MaxCargoLbs); // 25% de 16000
        }

        [Fact]
        public void Cabin_CapacidadeCarga()
        {
            // Caso 1: Expansão limitada a 130% de CargaMax
            // C208B: PaxMax = 12, CargaMax = 3690 lbs
            // 3690 + 12 * 170 = 3690 + 2040 = 5730 lbs > 1.3 * 3690 (4797 lbs)
            var cap1 = CabinConversion.GetEffectiveCapacity(12, 3690.0, "Carga (Sem Assentos)");
            Assert.Equal(0, cap1.MaxPax);
            Assert.Equal(4797.0, cap1.MaxCargoLbs);

            // Caso 2: Expansão sem atingir o teto de 130%
            // PaxMax = 2, CargaMax = 5000 lbs
            // 5000 + 2 * 170 = 5340 lbs < 1.3 * 5000 (6500 lbs)
            var cap2 = CabinConversion.GetEffectiveCapacity(2, 5000.0, "Carga (Sem Assentos)");
            Assert.Equal(0, cap2.MaxPax);
            Assert.Equal(5340.0, cap2.MaxCargoLbs);
        }

        [Fact]
        public void Missions_ConfigCarga_SemPassageiros()
        {
            // C208B na configuração Carga: capacidade 4797 lbs, 0 PAX
            const string config = "Carga (Sem Assentos)";
            const int maxPax = 12;
            const double maxPayload = 3690.0;

            // Missão com passageiro: NÃO compatível
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 1, missionPayloadLbs: 500));
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 10, missionPayloadLbs: 2000));

            // Missão de carga pura dentro da capacidade (<= 4797 lbs): compatível
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 1000));
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 4797));

            // Missão de carga pura acima da capacidade (> 4797 lbs): NÃO compatível
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 4800));
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 6000));
        }

        [Fact]
        public void Missions_ConfigPassageiro_CargaLimitada()
        {
            // C208B na configuração Passageiro: capacidade 12 PAX, carga bagagem até 922.5 lbs
            const string config = "Passageiro";
            const int maxPax = 12;
            const double maxPayload = 3690.0;

            // Missão de carga pura acima do limite de bagagem (> 922.5 lbs): FICA FORA
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 950));
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 2000));

            // Missão de carga pura dentro do limite de bagagem (<= 922.5 lbs): permitida
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 500));
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 922.5));

            // Missão de passageiros dentro do limite de PAX e carga: permitida
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 8, missionPayloadLbs: 8 * 170 + 400, luggageWeightLbs: 400));

            // Missão de passageiros excedendo o número de assentos (> 12 PAX): NÃO permitida
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 15, missionPayloadLbs: 2500));

            // Missão de passageiros com bagagem/carga excedendo limite do bagageiro (> 922.5 lbs): NÃO permitida
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 8, missionPayloadLbs: 8 * 170 + 1200, luggageWeightLbs: 1200));
        }

        [Fact]
        public void Missions_AviaoNaoConversivel_RegraAntiga()
        {
            // Cessna C152: MaxPax = 2, MaxPayloadLbs = 530, não conversível
            const string config = "Passageiro";
            const int maxPax = 2;
            const double maxPayload = 530.0;
            const bool hasConvertible = false;

            // Capacidade efetiva sem conversível deve manter regra antiga (pax até MaxPax, carga até MaxPayloadLbs)
            var cap = CabinConversion.GetEffectiveCapacity(maxPax, maxPayload, config, hasConvertibleCabin: hasConvertible);
            Assert.Equal(2, cap.MaxPax);
            Assert.Equal(530.0, cap.MaxCargoLbs);

            // Carga pura de 472 lb (acima de 25% de 530 = 132.5 lb): DEVE ser aceita na aeronave não conversível
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 472.0, hasConvertibleCabin: hasConvertible));

            // 1 PAX + bagagem até o payload (ex: 170 lb pax + 302 lb bagagem = 472 lb payload total): DEVE ser aceito
            Assert.True(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 1, missionPayloadLbs: 472.0, luggageWeightLbs: 302.0, hasConvertibleCabin: hasConvertible));

            // Carga pura excedendo o payload total (> 530 lb): NÃO aceito
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 0, missionPayloadLbs: 550.0, hasConvertibleCabin: hasConvertible));

            // Passageiros acima de MaxPax (> 2 PAX): NÃO aceito
            Assert.False(CabinConversion.IsMissionCompatible(config, maxPax, maxPayload, missionPax: 3, missionPayloadLbs: 510.0, hasConvertibleCabin: hasConvertible));
        }
    }
}
