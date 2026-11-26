using System;
using System.Linq;
using System.Reflection;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class DefectTextTests
    {
        [Fact]
        public void DefectText_Friendly_MotorEmVoo()
        {
            var defect = new AircraftDefect
            {
                Code = DefectCatalog.EngineFailure,
                FlightPhase = "VOANDO (PLANANDO / MOTOR CORTADO)"
            };

            var text = DefectText.Friendly(defect);
            Assert.Equal("O motor parou de funcionar em voo.", text);
        }

        [Theory]
        [InlineData("POUSO (733 FPM)", " no pouso.")]
        [InlineData("POUSO BOUNCE (200 FPM)", " no pouso.")]
        [InlineData("SOLO (FORA DE VOO)", " em solo.")]
        [InlineData("PREPARAÇÃO", " em solo.")]
        [InlineData("TÁXI / PÁTIO", " em solo.")]
        [InlineData("FINALIZANDO (5s)", " em solo.")]
        [InlineData("CANCELANDO (5s)", " em solo.")]
        [InlineData("VOO CANCELADO EM SOLO", " em solo.")]
        [InlineData("ACIDENTE", " no acidente.")]
        [InlineData("", ".")]
        [InlineData("   ", ".")]
        [InlineData(null, ".")]
        [InlineData("OUTRA_FASE", ".")]
        public void DefectText_Friendly_CenariosDeFase(string? phase, string expectedSuffix)
        {
            var defect = new AircraftDefect
            {
                Code = DefectCatalog.EngineFailure,
                FlightPhase = phase ?? string.Empty
            };

            var text = DefectText.Friendly(defect);
            Assert.Equal($"O motor parou de funcionar{expectedSuffix}", text);
        }

        [Fact]
        public void DefectText_Friendly_Inspecao()
        {
            var defect = new AircraftDefect
            {
                Code = "HARD_LANDING_INSPECTION",
                FlightPhase = "POUSO"
            };

            var text = DefectText.Friendly(defect);
            Assert.Equal("Inspeção pendente após uma ocorrência no pouso.", text);
        }

        [Fact]
        public void DefectText_Friendly_CodigoDesconhecido()
        {
            var defect = new AircraftDefect
            {
                Code = "UNKNOWN_CODE",
                FlightPhase = "VOANDO"
            };

            var text = DefectText.Friendly(defect);
            Assert.Equal("Defeito registrado em voo.", text);
        }

        [Fact]
        public void DefectText_Friendly_TodosOsCodigos()
        {
            var fields = typeof(DefectCatalog)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            int count = 0;
            foreach (var field in fields)
            {
                string code = (string)field.GetValue(null)!;
                var defect = new AircraftDefect
                {
                    Code = code,
                    FlightPhase = "VOANDO"
                };

                var text = DefectText.Friendly(defect);
                Assert.DoesNotContain("Defeito registrado", text);
                Assert.EndsWith(" em voo.", text);
                count++;
            }

            Assert.True(count >= 25, $"Esperava ao menos 25 códigos no catálogo, encontrou {count}");
        }

        [Fact]
        public void DefectText_Technical_OmiteVazios()
        {
            var fullDefect = new AircraftDefect
            {
                Origin = DefectOrigin.Native,
                FlightPhase = "VOANDO",
                Cause = "WEAR",
                Description = "Desgaste excessivo no motor"
            };

            var fullText = DefectText.Technical(fullDefect);
            Assert.Equal("Origem: SIMULADOR\nFase: VOANDO\nCausa: WEAR\nDesgaste excessivo no motor", fullText);

            var partialDefect = new AircraftDefect
            {
                Origin = DefectOrigin.Addon,
                FlightPhase = "",
                Cause = "   ",
                Description = "Pane injetada"
            };

            var partialText = DefectText.Technical(partialDefect);
            Assert.Equal("Origem: ADDON\nPane injetada", partialText);

            var emptyDefect = new AircraftDefect
            {
                Origin = DefectOrigin.Addon,
                FlightPhase = "",
                Cause = "",
                Description = ""
            };

            var emptyText = DefectText.Technical(emptyDefect);
            Assert.Equal("Origem: ADDON", emptyText);
        }
    }
}
