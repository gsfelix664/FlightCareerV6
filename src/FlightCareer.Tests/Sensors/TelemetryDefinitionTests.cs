using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Sensors
{
    /// <summary>
    /// O SimTelemetry é lido por posição de memória: cada AddToDef no SimConnectService precisa de um campo
    /// double na mesma posição. Um datum que o simulador rejeita (nome fora do SDK) desloca todos os campos seguintes.
    /// </summary>
    public class TelemetryDefinitionTests
    {
        // Nomes confirmados como inexistentes no SDK MSFS 2024 (rejeitados em voo real: NAME_UNRECOGNIZED).
        private static readonly string[] KnownInvalidSimVars = { "PARTIAL PANEL STATIC" };

        [Fact]
        public void AircraftData_DatumCount_MatchesSimTelemetryDoubleFields()
        {
            var datums = ReadAircraftDataDatums();
            int doubleFields = typeof(SimTelemetry)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Count(f => f.FieldType == typeof(double));

            Assert.Equal(doubleFields, datums.Length);
        }

        [Fact]
        public void FailureState_DatumCount_MatchesSimFailureStateFields()
        {
            string source = File.ReadAllText(FindSimConnectService());
            var datums = Regex.Matches(source, @"\bAddFailureDef\(\s*""([^""]+)""")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            var match = Regex.Match(source, @"struct\s+SimFailureState\s*\{([^}]+)\}");
            Assert.True(match.Success);
            var fields = Regex.Matches(match.Groups[1].Value, @"public\s+double\s+(\w+)\s*;")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            Assert.Equal(fields.Length, datums.Length);
        }

        [Fact]
        public void TurbineLossesState_DatumCount_MatchesSimTurbineLossesStateFields()
        {
            string source = File.ReadAllText(FindSimConnectService());
            var datums = Regex.Matches(source, @"\bAddTurbineLossesDef\(\s*\$?[""]([^""]+)[""]")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            var match = Regex.Match(source, @"struct\s+SimTurbineLossesState\s*\{([^}]+)\}");
            Assert.True(match.Success);
            var fields = Regex.Matches(match.Groups[1].Value, @"public\s+double\s+(\w+)\s*;")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            Assert.Equal(17, fields.Length);
            // 4 chamadas de AddTurbineLossesDef no loop cobrindo os 4 motores + 1 para NUMBER OF ENGINES
            Assert.Equal(5, datums.Length);
        }

        [Fact]
        public void IntegrityState_DatumCount_MatchesSimIntegrityStateFields()
        {
            string source = File.ReadAllText(FindSimConnectService());
            var datums = Regex.Matches(source, @"\bAddIntegrityDef\(\s*""([^""]+)""")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            int doubleFields = typeof(FlightCareer.Domain.Integrity.SimIntegrityState)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Count(f => f.FieldType == typeof(double));

            Assert.Equal(4, doubleFields);
            Assert.Equal(doubleFields, datums.Length);
        }

        [Fact]
        public void FastFlightState_DatumCount_MatchesSimFastFlightStateFields()
        {
            string source = File.ReadAllText(FindSimConnectService());
            var datums = Regex.Matches(source, @"\bAddFastDef\(\s*""([^""]+)""")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            var match = Regex.Match(source, @"struct\s+SimFastFlightState\s*\{([^}]+)\}");
            Assert.True(match.Success);
            var fields = Regex.Matches(match.Groups[1].Value, @"public\s+double\s+(\w+)\s*;")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            Assert.Equal(9, fields.Length);
            Assert.Equal(fields.Length, datums.Length);
        }

        [Fact]
        public void TelemetryDefinitions_UseNoKnownInvalidSimVar()
        {
            string source = File.ReadAllText(FindSimConnectService());
            foreach (var name in KnownInvalidSimVars)
            {
                Assert.DoesNotMatch(new Regex($@"Add(ToDef|FailureDef)\(\s*""{Regex.Escape(name)}"""), source);
            }
        }

        [Fact]
        public void SimTelemetry_Vento_UsaProaVerdadeira()
        {
            var telemetry = new SimTelemetry
            {
                AmbientWindDirection = 90.0,
                AmbientWindVelocity = 10.0,
                PlaneHeadingTrue = 90.0,
                PlaneHeading = 112.0 // Magnética com declinação
            };

            Assert.True(Math.Abs(telemetry.CrosswindComponentKnots) < 0.001);
            Assert.True(Math.Abs(telemetry.TailwindComponentKnots - (-10.0)) < 0.001);
        }

        [Fact]
        public void SimTelemetry_Vento_Traves90Graus()
        {
            var telemetry = new SimTelemetry
            {
                AmbientWindDirection = 180.0,
                AmbientWindVelocity = 10.0,
                PlaneHeadingTrue = 90.0,
                PlaneHeading = 70.0
            };

            Assert.True(Math.Abs(telemetry.CrosswindComponentKnots - 10.0) < 0.001);
            Assert.True(Math.Abs(telemetry.TailwindComponentKnots) < 0.001);
        }

        private static string[] ReadAircraftDataDatums()
        {
            string source = File.ReadAllText(FindSimConnectService());
            return Regex.Matches(source, @"\bAddToDef\(\s*""([^""]+)""")
                .Select(m => m.Groups[1].Value)
                .ToArray();
        }

        private static string FindSimConnectService()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "FlightCareer.UI", "Services", "SimConnectService.cs");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException("SimConnectService.cs não encontrado subindo a partir de AppContext.BaseDirectory.");
        }
    }
}
