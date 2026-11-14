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
        public void TelemetryDefinitions_UseNoKnownInvalidSimVar()
        {
            string source = File.ReadAllText(FindSimConnectService());
            foreach (var name in KnownInvalidSimVars)
            {
                Assert.DoesNotMatch(new Regex($@"Add(ToDef|FailureDef)\(\s*""{Regex.Escape(name)}"""), source);
            }
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
