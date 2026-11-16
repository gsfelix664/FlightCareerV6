using System;
using System.Collections.Generic;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Application.Services;
using Xunit;

namespace FlightCareer.Tests.Components
{
    public class TurbineDegradationTests
    {
        private readonly TurbineDegradationConfig _defaultConfig = new()
        {
            StartHealthPct = 60.0,
            MaxN1LossPct = 5.0,
            MaxFuelEfficiencyLossPct = 10.0,
            MaxIttCoolingLossPct = 15.0,
            MaxThrustEfficiencyLossPct = 8.0,
            WriteToleranceStepPct = 0.5
        };

        [Fact]
        public void Compute_SaudeAcimaDoInicio_SemPerda()
        {
            var losses = TurbineDegradation.Compute(80.0, 90.0, _defaultConfig);
            Assert.Equal(TurbineLosses.None, losses);
        }

        [Fact]
        public void Compute_SaudeZero_PerdaMaxima()
        {
            var losses = TurbineDegradation.Compute(0.0, 0.0, _defaultConfig);
            Assert.Equal(5.0, losses.N1LossPct);
            Assert.Equal(10.0, losses.FuelEfficiencyLossPct);
            Assert.Equal(15.0, losses.IttCoolingLossPct);
            Assert.Equal(8.0, losses.ThrustEfficiencyLossPct);
        }

        [Fact]
        public void Compute_MeioDoCaminho_Linear()
        {
            // StartHealth 60, hot 30 -> f(hot) = 0.5 -> 15 * 0.5 = 7.5
            var losses = TurbineDegradation.Compute(60.0, 30.0, _defaultConfig);
            Assert.Equal(0.0, losses.N1LossPct);
            Assert.Equal(7.5, losses.IttCoolingLossPct);
            Assert.Equal(0.0, losses.ThrustEfficiencyLossPct);
            // Fuel usa o menor (30) -> f = 0.5 -> 10 * 0.5 = 5.0
            Assert.Equal(5.0, losses.FuelEfficiencyLossPct);
        }

        [Fact]
        public void Compute_FuelUsaOMenorDosDois()
        {
            // engine 60 (f=0), hot 0 (f=1.0) -> Fuel usa min(60, 0) = 0 -> FuelEff = 10, N1 = 0
            var losses = TurbineDegradation.Compute(60.0, 0.0, _defaultConfig);
            Assert.Equal(0.0, losses.N1LossPct);
            Assert.Equal(10.0, losses.FuelEfficiencyLossPct);
            Assert.Equal(15.0, losses.IttCoolingLossPct);
            Assert.Equal(0.0, losses.ThrustEfficiencyLossPct);
        }

        [Fact]
        public void Compute_Arredonda_NoPassoDeEscrita()
        {
            var cfg = new TurbineDegradationConfig
            {
                StartHealthPct = 100.0,
                MaxN1LossPct = 10.0,
                MaxFuelEfficiencyLossPct = 10.0,
                MaxIttCoolingLossPct = 10.0,
                MaxThrustEfficiencyLossPct = 10.0,
                WriteToleranceStepPct = 0.5
            };

            // health 73 -> f = 0.27 -> 10 * 0.27 = 2.7 -> múltiplo de 0.5 mais próximo é 2.5
            var losses = TurbineDegradation.Compute(73.0, 73.0, cfg);
            Assert.Equal(2.5, losses.N1LossPct);
            Assert.Equal(2.5, losses.FuelEfficiencyLossPct);

            // health 72 -> f = 0.28 -> 10 * 0.28 = 2.8 -> múltiplo de 0.5 mais próximo é 3.0
            losses = TurbineDegradation.Compute(72.0, 72.0, cfg);
            Assert.Equal(3.0, losses.N1LossPct);
        }

        [Fact]
        public void ShouldWrite_AlvoIgualELeituraIgual_NaoEscreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var simValues = new TurbineLosses(5.0, 10.0, 15.0, 8.0);

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(10),
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.False(write);
        }

        [Fact]
        public void ShouldWrite_LeituraZeroComAlvoMaiorQueZero_Escreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var simValues = TurbineLosses.None; // Sim zerou

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(10),
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.True(write);
        }

        [Fact]
        public void ShouldWrite_MenosDe5Segundos_NaoEscreve()
        {
            var target = new TurbineLosses(5.0, 10.0, 15.0, 8.0);
            var lastWritten = TurbineLosses.None;
            var simValues = TurbineLosses.None;

            bool write = TurbineDegradation.ShouldWrite(
                target,
                lastWritten,
                simValues,
                TimeSpan.FromSeconds(3), // menos de 5 s
                0.5,
                TimeSpan.FromSeconds(5));

            Assert.False(write);
        }

        [Fact]
        public void Catalog_BlocoTurbineDegradation_CarregaValores()
        {
            var catalog = ComponentCatalog.LoadDefault();
            Assert.NotNull(catalog.TurbineDegradation);
            Assert.Equal(60.0, catalog.TurbineDegradation.StartHealthPct);
            Assert.Equal(5.0, catalog.TurbineDegradation.MaxN1LossPct);
            Assert.Equal(10.0, catalog.TurbineDegradation.MaxFuelEfficiencyLossPct);
            Assert.Equal(15.0, catalog.TurbineDegradation.MaxIttCoolingLossPct);
            Assert.Equal(8.0, catalog.TurbineDegradation.MaxThrustEfficiencyLossPct);
            Assert.Equal(0.5, catalog.TurbineDegradation.WriteToleranceStepPct);
            Assert.Equal("estimativa — calibrar no MSFS", catalog.TurbineDegradation.Source);
        }

        [Fact]
        public void Catalog_SemBloco_Desligado()
        {
            string jsonWithoutBlock = @"{
                ""hazard"": { ""threshold"": 20.0, ""lambdaAtThreshold"": 0.02, ""lambdaAtZero"": 2.0 },
                ""visibility"": { ""maxHalfWidth"": 25.0, ""growthPerHour"": 0.4, ""walkaroundHalfWidth"": 10.0, ""maxBias"": 0.6 },
                ""landing"": { ""hardLandingFpm"": 400.0, ""severeLandingFpm"": 600.0, ""lateralLimitFps"": 5.0, ""gearDamagePer100Fpm"": 1.5, ""tireDamagePer100Fpm"": 2.0, ""gearSevereDamagePer100Fpm"": 5.0, ""airframeSevereDamagePer100Fpm"": 2.0, ""gearLateralDamagePerFps"": 1.2, ""tireLateralDamagePerFps"": 1.5, ""brakeEnergyLossPerHour"": 72.0, ""wetSurfaceMultiplier"": 1.8, ""oilLeakMinFpm"": 650.0, ""oilLeakChanceAt1000Fpm"": 0.5, ""oilLeakRatePerMin"": 3.5, ""crashFpm"": 1500.0, ""crashBankDeg"": 30.0, ""crashNoseDownDeg"": 15.0, ""crashNoseUpDeg"": 30.0, ""crashG"": 6.0, ""contactWindowBeforeSec"": 1.0, ""contactWindowAfterSec"": 0.5, ""minAirborneSec"": 0.5, ""traceBeforeSec"": 2.0 },
                ""engineLimits"": {
                    ""Piston"": { ""chtRedLine"": 240, ""chtYellowMin"": 220, ""oilTempRedLine"": 115, ""oilTempYellowMin"": 105, ""oilPressureMinRedLine"": 20, ""oilPressureYellowMin"": 30, ""oilPressureMaxRedLine"": 100, ""oilPressureYellowMax"": 90, ""rpmRedLine"": 2700, ""rpmYellowMin"": 2600 }
                },
                ""airframeLimits"": { ""flapsMaxSpeed"": 100, ""gearMaxSpeed"": 140, ""vne"": 160, ""maxPositiveG"": 3.8, ""maxNegativeG"": -1.5 },
                ""components"": [
                    {
                        ""kind"": ""Engine"",
                        ""name"": ""Motor"",
                        ""appliesTo"": [ ""Piston"" ],
                        ""critical"": true,
                        ""weight"": 30,
                        ""lifeHours"": { ""Piston"": 2000 },
                        ""failureModes"": [
                            { ""id"": ""engine.failure"", ""name"": ""Falha"", ""weight"": 100, ""critical"": true, ""effect"": ""EngineFailure"" }
                        ]
                    }
                ]
            }";

            var catalog = ComponentCatalog.FromJson(jsonWithoutBlock);
            Assert.Null(catalog.TurbineDegradation);

            // Compute com config nula deve resultar em TurbineLosses.None
            var losses = TurbineDegradation.Compute(10.0, 10.0, catalog.TurbineDegradation);
            Assert.Equal(TurbineLosses.None, losses);
        }

        [Fact]
        public void Actuator_ComDoisMotores_GravaNosDois()
        {
            var fakeSim = new FakeSimConnectForTurbine();
            var actuator = new TurbineDegradationActuator(fakeSim, _defaultConfig);

            var telemetry = new SimTelemetry { EngineType = 1.0 }; // Turbina
            var engine = new AircraftComponent { Health = 0.0 };
            var hotSection = new AircraftComponent { Health = 0.0 };

            actuator.Apply(telemetry, engine, hotSection, engineCount: 2);

            Assert.Equal(2, fakeSim.WrittenLosses.Count);
            Assert.True(fakeSim.WrittenLosses.ContainsKey(1));
            Assert.True(fakeSim.WrittenLosses.ContainsKey(2));
            Assert.Equal(5.0, fakeSim.WrittenLosses[1].N1LossPct);
            Assert.Equal(5.0, fakeSim.WrittenLosses[2].N1LossPct);
        }

        [Fact]
        public void Actuator_Recarga_NadaGravadoNoEvento_ProximoApplyTurbinaGrava_PistaoNaoGrava()
        {
            var fakeSim = new FakeSimConnectForTurbine();
            DateTime currentTime = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var actuator = new TurbineDegradationActuator(fakeSim, _defaultConfig, () => currentTime);

            var turbineTelemetry = new SimTelemetry { EngineType = 5.0 }; // Turboprop
            var pistonTelemetry = new SimTelemetry { EngineType = 0.0 };  // Piston
            var engine = new AircraftComponent { Health = 0.0 };
            var hotSection = new AircraftComponent { Health = 0.0 };

            // 1. Aplica perdas iniciais em turbina
            actuator.Apply(turbineTelemetry, engine, hotSection, 1);
            Assert.Equal(1, fakeSim.SetTurbineLossesCallCount);
            fakeSim.SimLosses.Clear(); // Simula que o MSFS zerou SimVars na recarga

            // 2. Evento de recarga da aeronave disparado
            fakeSim.TriggerAircraftReloaded();

            // Nada deve ter sido gravado durante o evento
            Assert.Equal(1, fakeSim.SetTurbineLossesCallCount);

            // 3. Próximo Apply com aeronave a pistão -> não grava
            currentTime = currentTime.AddSeconds(10);
            actuator.Apply(pistonTelemetry, engine, hotSection, 1);
            Assert.Equal(1, fakeSim.SetTurbineLossesCallCount);

            // 4. Próximo Apply com aeronave a turbina -> grava
            currentTime = currentTime.AddSeconds(10);
            actuator.Apply(turbineTelemetry, engine, hotSection, 1);
            Assert.Equal(2, fakeSim.SetTurbineLossesCallCount);
            Assert.Equal(5.0, fakeSim.WrittenLosses[1].N1LossPct);
        }
    }

    public class FakeSimConnectForTurbine : ISimConnectService
    {
        public event EventHandler? OnConnected { add { } remove { } }
        public event EventHandler? OnDisconnected { add { } remove { } }
        public event EventHandler<SimTelemetry>? OnDataReceived { add { } remove { } }
        public event EventHandler? OnCrashEvent { add { } remove { } }
        public event Action? OnAircraftReloaded;

        public bool IsCrashed { get; set; }
        public bool DebugMode { get; set; }
        public bool IsConnected { get; set; } = true;
        public SimTelemetry CurrentData { get; set; }
        public int TurbineEngineCount { get; set; } = 1;

        public readonly Dictionary<int, TurbineLosses> WrittenLosses = new();
        public readonly Dictionary<int, TurbineLosses> SimLosses = new();
        public int SetTurbineLossesCallCount { get; set; }

        public void TriggerAircraftReloaded() => OnAircraftReloaded?.Invoke();

        public void ResetCrashState() { }
        public void SetDebugData(SimTelemetry data) { CurrentData = data; }
        public void Initialize(IntPtr windowHandle) { }
        public void Connect() { }
        public void Disconnect() { }
        public void SetFailure(string eventName) { }
        public void SendEvent(string eventName, uint data = 0) { }
        public void ClearSimFailures() { }
        public void SetEngineFire(bool isOnFire) { }
        public void SetOilLeakedPercent(double percent) { }
        public void SetAlternatorConnected(int alternatorIndex, bool connected) { }
        public void WriteFlapSurfacePercent(double fraction) { }

        public void SetTurbineLosses(int engineIndex, TurbineLosses losses)
        {
            SetTurbineLossesCallCount++;
            WrittenLosses[engineIndex] = losses;
            SimLosses[engineIndex] = losses;
        }

        public TurbineLosses GetTurbineLosses(int engineIndex)
        {
            return SimLosses.TryGetValue(engineIndex, out var val) ? val : TurbineLosses.None;
        }

        public event Action<FlightCareer.Domain.Fuel.SimFuelState>? OnFuelStateReceived { add { } remove { } }
        public FlightCareer.Domain.Fuel.SimFuelState CurrentFuelState { get; set; }
        public event Action<FlightCareer.Domain.Integrity.SimIntegrityState>? OnIntegrityStateReceived { add { } remove { } }
        public FlightCareer.Domain.Integrity.SimIntegrityState CurrentIntegrityState { get; set; }
        public readonly List<(string SimVar, double Gallons)> WrittenFuelTanks = new();
        public IReadOnlyList<FlightCareer.Domain.Fuel.FuelTank> ActiveFuelTanks { get; set; } = Array.Empty<FlightCareer.Domain.Fuel.FuelTank>();
        public IReadOnlyList<FlightCareer.Domain.Fuel.FuelTank> GetActiveFuelTanks() => ActiveFuelTanks;
        public void SetFuelTanks(IReadOnlyList<(string simVar, double gallons)> tanks)
        {
            if (tanks != null) WrittenFuelTanks.AddRange(tanks);
        }
    }
}
