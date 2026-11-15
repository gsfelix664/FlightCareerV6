using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Sensors;
using Xunit;

namespace FlightCareer.Tests.Sensors
{
    public class CalibrationScriptTests
    {
        private static CalibrationScript LoadRealScript(IReadOnlySet<string>? features = null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "calibration_script.json");
            return CalibrationScript.LoadFromFile(path, features);
        }

        [Fact]
        public void CalibrationScript_FiltraFlapsPorFeature()
        {
            // Aeronave sem flaps ("noFlaps")
            var featuresNoFlap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "piston", "singleEngine", "noFlaps" };
            var scriptNoFlap = LoadRealScript(featuresNoFlap);

            Assert.DoesNotContain(scriptNoFlap.ActiveSteps, s => s.Id == "flaps-each-detent");

            // Aeronave normal com flaps
            var featuresWithFlap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "piston", "singleEngine" };
            var scriptWithFlap = LoadRealScript(featuresWithFlap);

            Assert.Contains(scriptWithFlap.ActiveSteps, s => s.Id == "flaps-each-detent");
        }

        [Fact]
        public void CalibrationScript_ProntoAntesDoTempoMinimo_NaoAvanca()
        {
            var script = LoadRealScript();
            // Primeiro passo: cold-dark (10 s)
            Assert.Equal("cold-dark", script.CurrentStep?.Id);
            Assert.Equal(10.0, script.TimeRemaining);

            // Simular 3 segundos
            script.Tick(3.0);
            Assert.Equal(7.0, script.TimeRemaining);

            // TryAdvance deve falhar pois faltam 7 s
            var advanced = script.TryAdvance(out var nextStep);
            Assert.False(advanced);
            Assert.Equal("cold-dark", nextStep?.Id);
            Assert.Equal("cold-dark", script.CurrentStep?.Id);
        }

        [Fact]
        public void CalibrationScript_AposTempoMinimo_Avanca()
        {
            var script = LoadRealScript();
            Assert.Equal("cold-dark", script.CurrentStep?.Id);

            // Simular 10 segundos
            script.Tick(10.0);
            Assert.Equal(0.0, script.TimeRemaining);

            var advanced = script.TryAdvance(out var nextStep);
            Assert.True(advanced);
            Assert.NotNull(nextStep);
            Assert.Equal("battery-on", nextStep.Id);

            // Segundo passo deve ser battery-on
            Assert.Equal("battery-on", script.CurrentStep?.Id);
        }

        [Fact]
        public void CalibrationScript_Pular_AvancaMesmoAntesDoTempo()
        {
            var script = LoadRealScript();
            Assert.Equal("cold-dark", script.CurrentStep?.Id);

            // Pula com 0 segundos passados
            var nextStep = script.Skip();
            Assert.NotNull(nextStep);
            Assert.Equal("battery-on", nextStep.Id);

            Assert.Equal("battery-on", script.CurrentStep?.Id);
        }

        [Fact]
        public void CalibrationScript_PercorrerTodos_Conclui()
        {
            var script = LoadRealScript();

            while (script.CurrentStep != null && script.CurrentStep.Id != "end")
            {
                script.Skip();
            }

            Assert.True(script.IsCompleted);
            Assert.Equal("end", script.CurrentStep?.Id);
        }
    }
}
