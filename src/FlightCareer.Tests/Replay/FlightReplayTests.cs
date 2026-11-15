using System;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Replay;
using FlightCareer.Infrastructure.Replay;
using FlightCareer.Infrastructure.Services;
using Xunit;

namespace FlightCareer.Tests.Replay
{
    public class FlightReplayTests
    {
        private static string GetFixturePath(string relativePath)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "Flights", relativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "Flights", relativePath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "Flights", relativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Tests", "Fixtures", "Flights", relativePath)
            };

            var match = candidates.FirstOrDefault(File.Exists);
            if (match != null) return Path.GetFullPath(match);

            throw new FileNotFoundException($"Fixture '{relativePath}' não encontrada nos caminhos buscados:\n" + string.Join("\n", candidates));
        }

        private static (PartCatalog PartsCatalog, WearRuleCatalog WearRuleCatalog, AircraftLimitResolver Resolver, AircraftLimitProfile Profile) LoadCatalogAndProfile(string aircraftId)
        {
            var partsCatalog = PartCatalog.LoadDefault();
            var wearRuleCatalog = WearRuleCatalog.LoadDefault();
            var loader = new AircraftLimitCatalogLoader();
            var resolver = loader.CreateResolver();
            var profile = resolver.FindById(aircraftId)
                ?? resolver.FindProfile(aircraftId, aircraftId)
                ?? throw new InvalidOperationException($"Perfil da aeronave '{aircraftId}' não encontrado no catálogo.");

            return (partsCatalog, wearRuleCatalog, resolver, profile);
        }

        [Fact]
        public void Replay_C152Voo_RpmAcimaDoRedline_GeraExcedenciaNoMotor1()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-sbmt-snej-20261115.csv.gz"));
            var touchdowns = SensorLogReader.ReadTouchdownsFromJson(GetFixturePath("c152-sbmt-snej-20261115.touchdowns.json"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, touchdowns, profile, profile.Features, profile.EngineCount);

            // Confere que houve excedência da regra piston_rpm_overspeed
            var rpmExceedances = result.Exceedances
                .Where(e => e.RuleId.Equals("piston_rpm_overspeed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(rpmExceedances);
            // Deve conter alvo no motor 1 (:1)
            Assert.Contains(rpmExceedances, e => e.TargetId.Contains(":1") || e.Notes.Contains("Motor 1"));
            // Redline do C152 é 2550 RPM; pico no voo passou de 2600 RPM
            Assert.True(rpmExceedances.Max(e => e.PeakValue) >= 2550.0);
        }

        [Fact]
        public void Replay_C152Voo_HorasSimuladas_1_086()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-sbmt-snej-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            // O voo monitorado durou 1,086 h simuladas no simulador; o CSV inclui taxi/cold-dark pré-voo e corte totalizando ~1.137 h
            Assert.InRange(result.TotalSimHours, 1.05, 1.15);
        }

        [Fact]
        public void Replay_C152Voo_Toque658_DanificaPneusEAmortecedores_EUmCiclo()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-sbmt-snej-20261115.csv.gz"));
            var touchdowns = SensorLogReader.ReadTouchdownsFromJson(GetFixturePath("c152-sbmt-snej-20261115.touchdowns.json"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, touchdowns, profile, profile.Features, profile.EngineCount);

            // Valores reais gravados no banco pelo voo: pneus -3.69% / -3.65%, amortecedores -2.52%
            var mainTires = result.Parts.First(p => p.PartId.Equals("tires.main", StringComparison.OrdinalIgnoreCase));
            var noseTires = result.Parts.First(p => p.PartId.Equals("tires.nose", StringComparison.OrdinalIgnoreCase));
            var mainStruts = result.Parts.First(p => p.PartId.Equals("gear.main_struts", StringComparison.OrdinalIgnoreCase));
            var noseStrut = result.Parts.First(p => p.PartId.Equals("gear.nose_strut", StringComparison.OrdinalIgnoreCase));

            // Perdas dentro de ±15% das gravadas no banco
            Assert.InRange(mainTires.HealthLoss, 3.65 * 0.85, 3.65 * 1.15);
            Assert.InRange(noseTires.HealthLoss, 3.69 * 0.85, 3.69 * 1.15);
            Assert.InRange(mainStruts.HealthLoss, 2.52 * 0.85, 2.52 * 1.15);
            Assert.InRange(noseStrut.HealthLoss, 2.52 * 0.85, 2.52 * 1.15);

            // CyclesSinceReplacement = 1 nas peças associadas ao impacto do pouso
            var wheelBearings = result.Parts.First(p => p.PartId.Equals("gear.wheels_bearings", StringComparison.OrdinalIgnoreCase));
            var trunnions = result.Parts.First(p => p.PartId.Equals("gear.trunnions_side_stays", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(1, wheelBearings.CyclesSinceReplacement);
            Assert.Equal(1, trunnions.CyclesSinceReplacement);
        }

        [Fact]
        public void Replay_C152Voo_DiagnosticosNaoRepetem()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-sbmt-snej-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            // Cada diagnóstico deve aparecer no máximo 1 vez
            var duplicates = result.Diagnostics
                .GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void Replay_C152Voo_ComRegrasDoMotor_DesgastaMaisQueSemElas()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-sbmt-snej-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            var valves = result.Parts.First(p => p.PartId.Equals("engine.valves", StringComparison.OrdinalIgnoreCase));
            var cylinders = result.Parts.First(p => p.PartId.Equals("engine.cylinders", StringComparison.OrdinalIgnoreCase));

            // Taxa natural pura: 70.0 / LifeHours (1000h para válvulas = 0.07%/h; 1800h para cilindros = 0.0389%/h)
            double naturalValvesLoss = (70.0 / 1000.0) * result.TotalSimHours;
            double naturalCylindersLoss = (70.0 / 1800.0) * result.TotalSimHours;

            // Por causa das sobrevelocidades e estresse, perda é estritamente maior
            Assert.True(valves.HealthLoss > naturalValvesLoss * 2.0);
            Assert.True(cylinders.HealthLoss > naturalCylindersLoss * 2.0);
        }

        [Fact]
        public void Replay_C172TesteEletrico_PartidaComAvionicsDesligado_NaoEmiteSpike()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("C172SP G1000");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c172-teste-eletrico-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            // Partida às 19:40:31 UTC ocorreu com aviônicos desligados (elec-circuit-on-3 = 0)
            bool spikeEmitted = result.DerivedEvents.Any(e =>
                e.MetricKey.Equals(WearRuleMetrics.AvionicsMasterStartSpikes, StringComparison.OrdinalIgnoreCase));

            Assert.False(spikeEmitted);
        }

        [Fact]
        public void Replay_C172TesteEletrico_SemDescargaProfunda()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("C172SP G1000");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c172-teste-eletrico-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            // Tensão esteve sempre em ~24V bateria e ~28V alternador, nunca atingindo níveis de descarga profunda (<21.25V / 22V)
            bool deepDischargeEmitted = result.DerivedEvents.Any(e =>
                e.MetricKey.Equals(WearRuleMetrics.BatteryDeepDischargeMinutes, StringComparison.OrdinalIgnoreCase));

            Assert.False(deepDischargeEmitted);
        }

        [Fact]
        public void Replay_C152Calibracao_Executa_SemExcecao()
        {
            var (partsCatalog, wearRuleCatalog, resolver, profile) = LoadCatalogAndProfile("Cessna C152");
            var frames = SensorLogReader.ReadFrames(GetFixturePath("c152-calibracao-20261115.csv.gz"));

            var replayer = new FlightReplayer(partsCatalog, wearRuleCatalog, resolver);
            var result = replayer.Run(frames, null, profile, profile.Features, profile.EngineCount);

            Assert.NotNull(result);
            Assert.True(result.TotalTicks > 0);
            Assert.NotEmpty(result.Parts);
        }
    }
}
