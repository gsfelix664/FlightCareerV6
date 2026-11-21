using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Components
{
    /// <summary>Registro de um parâmetro que entrou na faixa vermelha (vai para o logbook técnico).</summary>
    public sealed record ExceedanceEvent(ComponentKind Kind, string Parameter, double Value, double Limit);

    /// <summary>
    /// Desgaste por componente. Uma instância por voo (guarda estado como CHT anterior e tempo de starter).
    ///
    /// perda/hora = natural × (vencido ? overdueMultiplier : 1)
    ///            + Σ amarelo: YellowLossPerHour
    ///            + Σ vermelho: RedLossPerHour × (1 + 5 × excesso relativo)
    /// Pousos aplicam desgaste por ciclo e dano por impacto.
    /// </summary>
    public sealed class WearEngine
    {
        private readonly ComponentCatalog _catalog;
        private readonly EngineClass _engineClass;
        private readonly EngineLimits _limits;
        private readonly AirframeLimits _airframe;

        private double? _previousCht;
        private bool _wasEngineRunning;
        private double _starterSeconds;
        private readonly HashSet<string> _activeRedStressors = new();

        public WearEngine(ComponentCatalog catalog, EngineClass engineClass)
            : this(catalog, engineClass, catalog.LimitsFor(engineClass), catalog.AirframeLimits)
        {
        }

        /// <summary>Usa limites específicos da aeronave (ver ComponentCatalog.ResolveLimits).</summary>
        public WearEngine(ComponentCatalog catalog, EngineClass engineClass, EngineLimits limits, AirframeLimits airframe)
        {
            _catalog = catalog;
            _engineClass = engineClass;
            _limits = limits;
            _airframe = airframe;
        }

        public EngineClass EngineClass => _engineClass;

        private readonly record struct Stress(ComponentKind Kind, string Parameter, StressBand Band, double Value, double Limit, double Excess);

        /// <summary>Aplica o desgaste de um intervalo de telemetria (dtHours em tempo simulado).</summary>
        public IReadOnlyList<ExceedanceEvent> ApplyTick(IList<AircraftComponent> components, SimTelemetry data, double dtHours)
        {
            var events = new List<ExceedanceEvent>();
            if (dtHours <= 0) return events;

            // Partida de motor = um ciclo para motor, seção quente e elétrico
            if (data.IsEngineRunning && !_wasEngineRunning)
            {
                foreach (var c in components.Where(c => c.Kind is ComponentKind.Engine or ComponentKind.HotSection or ComponentKind.Electrical))
                    c.CyclesSinceOverhaul++;
            }
            _wasEngineRunning = data.IsEngineRunning;

            var stresses = EvaluateStresses(data, dtHours);

            foreach (var component in components)
            {
                var def = _catalog.Components.FirstOrDefault(d => d.Kind == component.Kind);
                if (def == null || !def.AppliesTo.Contains(_engineClass)) continue;

                double lossPerHour = 0.0;

                if (!ComponentUsage.IsCycleDriven(component.Kind) && ComponentUsage.IsInUse(component.Kind, data))
                {
                    component.HoursSinceOverhaul += dtHours;
                    bool overdue = component.HoursSinceOverhaul > def.LifeHoursFor(_engineClass);
                    lossPerHour += def.NaturalLossPerHour(_engineClass) * (overdue ? _catalog.OverdueMultiplier : 1.0);
                }

                foreach (var s in stresses.Where(s => s.Kind == component.Kind))
                {
                    lossPerHour += s.Band == StressBand.Red
                        ? def.RedLossPerHour * (1.0 + 5.0 * Math.Max(0.0, s.Excess))
                        : def.YellowLossPerHour;
                }

                double loss = lossPerHour * dtHours;

                // Energia de frenagem desgasta freios (e, em menor grau, pneus) enquanto rola no solo
                if (component.Kind is ComponentKind.Brakes or ComponentKind.Tires)
                {
                    double share = component.Kind == ComponentKind.Brakes ? 1.0 : 0.25;
                    loss += BrakeEnergyLoss(data, dtHours) * share;
                }

                Damage(component, loss);
            }

            // Eventos de borda: só registra quando o parâmetro ENTRA na faixa vermelha
            var currentRed = new HashSet<string>();
            foreach (var s in stresses.Where(s => s.Band == StressBand.Red))
            {
                string key = $"{s.Kind}:{s.Parameter}";
                currentRed.Add(key);
                if (!_activeRedStressors.Contains(key))
                    events.Add(new ExceedanceEvent(s.Kind, s.Parameter, s.Value, s.Limit));
            }
            _activeRedStressors.Clear();
            _activeRedStressors.UnionWith(currentRed);

            return events;
        }

        /// <summary>Aplica o ciclo de pouso: desgaste por ciclo de trem/pneus/freios e dano por impacto.</summary>
        public IReadOnlyList<ExceedanceEvent> ApplyLanding(IList<AircraftComponent> components, double touchdownFpm, double lateralFps, bool wetSurface = false)
        {
            var events = new List<ExceedanceEvent>();
            var l = _catalog.Landing;
            double fpm = Math.Abs(touchdownFpm);
            double lateral = Math.Abs(lateralFps);

            foreach (var component in components.Where(c => ComponentUsage.IsCycleDriven(c.Kind)))
            {
                var def = _catalog.Components.FirstOrDefault(d => d.Kind == component.Kind);
                if (def == null || !def.AppliesTo.Contains(_engineClass)) continue;

                component.CyclesSinceOverhaul++;
                bool overdue = component.CyclesSinceOverhaul > def.LifeCyclesFor(_engineClass);
                double loss = def.NaturalLossPerCycle(_engineClass) * (overdue ? _catalog.OverdueMultiplier : 1.0);
                if (wetSurface && component.Kind is ComponentKind.Tires or ComponentKind.Brakes) loss *= l.WetSurfaceMultiplier;
                Damage(component, loss);
            }

            double hardExcess = Math.Max(0.0, fpm - l.HardLandingFpm) / 100.0;
            double severeExcess = Math.Max(0.0, fpm - l.SevereLandingFpm) / 100.0;
            double lateralExcess = Math.Max(0.0, lateral - l.LateralLimitFps);

            Damage(components, ComponentKind.LandingGear,
                hardExcess * l.GearDamagePer100Fpm + severeExcess * l.GearSevereDamagePer100Fpm + lateralExcess * l.GearLateralDamagePerFps);
            Damage(components, ComponentKind.Tires,
                hardExcess * l.TireDamagePer100Fpm + lateralExcess * l.TireLateralDamagePerFps);
            Damage(components, ComponentKind.Airframe, severeExcess * l.AirframeSevereDamagePer100Fpm);

            if (fpm > l.SevereLandingFpm)
                events.Add(new ExceedanceEvent(ComponentKind.LandingGear, "Razão de toque (FPM)", fpm, l.SevereLandingFpm));
            if (lateral > l.LateralLimitFps * 2.0)
                events.Add(new ExceedanceEvent(ComponentKind.LandingGear, "Carga lateral no toque (fps)", lateral, l.LateralLimitFps * 2.0));

            return events;
        }

        private double BrakeEnergyLoss(SimTelemetry data, double dtHours)
        {
            if (!data.IsOnGround || data.GroundSpeed < 1.0) return 0.0;
            double avgBrake = (data.BrakeLeftPosition + data.BrakeRightPosition) / 2.0;
            double brake = avgBrake > 1.0 ? avgBrake / 32767.0 : avgBrake;
            if (brake < 0.05) return 0.0;

            double speedFactor = Math.Pow(data.GroundSpeed / 60.0, 2);
            double loss = _catalog.Landing.BrakeEnergyLossPerHour * Math.Min(1.0, brake) * speedFactor * dtHours;
            return data.IsWetOrContaminatedSurface ? loss * _catalog.Landing.WetSurfaceMultiplier : loss;
        }

        private List<Stress> EvaluateStresses(SimTelemetry data, double dtHours)
        {
            var list = new List<Stress>();
            var a = _airframe;
            double dtSeconds = dtHours * 3600.0;

            // --- Motor de arranque (ciclo de trabalho) ---
            _starterSeconds = data.StarterActive > 0.5 ? _starterSeconds + dtSeconds : 0.0;
            if (_starterSeconds > _limits.MaxStarterSeconds)
                list.Add(new Stress(ComponentKind.Electrical, "Tempo de starter (s)", StressBand.Yellow, _starterSeconds, _limits.MaxStarterSeconds, 0));

            if (_engineClass == EngineClass.Piston)
                EvaluatePiston(data, dtHours, list);
            else
                EvaluateTurbine(data, list);

            // --- Óleo (comum) ---
            if (data.IsEngineRunning)
            {
                double maxOil = _limits.MaxOilTemp;
                if (data.EngineOilTemp > maxOil)
                {
                    list.Add(new Stress(ComponentKind.Oil, "Temperatura do óleo (°C)", StressBand.Red, data.EngineOilTemp, maxOil, (data.EngineOilTemp - maxOil) / maxOil));
                    list.Add(new Stress(ComponentKind.Engine, "Temperatura do óleo (°C)", StressBand.Yellow, data.EngineOilTemp, maxOil, 0));
                }
                else if (data.EngineOilTemp > maxOil - _limits.OilTempCautionMargin)
                {
                    list.Add(new Stress(ComponentKind.Oil, "Temperatura do óleo (°C)", StressBand.Yellow, data.EngineOilTemp, maxOil, 0));
                }

                if (data.EngineOilTemp < _limits.MinOilTempForHighPower && data.ThrottlePercent > _limits.HighPowerThrottlePercent)
                {
                    list.Add(new Stress(ComponentKind.Oil, "Potência alta com óleo frio", StressBand.Yellow, data.EngineOilTemp, _limits.MinOilTempForHighPower, 0));
                    list.Add(new Stress(ComponentKind.Engine, "Potência alta com óleo frio", StressBand.Yellow, data.EngineOilTemp, _limits.MinOilTempForHighPower, 0));
                }

                bool spunUp = _engineClass == EngineClass.Piston ? data.EngineRPM > 1200.0 : data.EngineN1 > 50.0;
                if (spunUp && data.EngineOilPressure < _limits.MinOilPressure)
                {
                    double excess = (_limits.MinOilPressure - Math.Max(0, data.EngineOilPressure)) / _limits.MinOilPressure;
                    list.Add(new Stress(ComponentKind.Oil, "Pressão do óleo (psi)", StressBand.Red, data.EngineOilPressure, _limits.MinOilPressure, excess));
                    list.Add(new Stress(ComponentKind.Engine, "Pressão do óleo (psi)", StressBand.Red, data.EngineOilPressure, _limits.MinOilPressure, excess));
                }
            }

            // --- Célula ---
            if (!data.IsOnGround)
            {
                double g = data.GForce;
                if (g > a.MaxG)
                    list.Add(new Stress(ComponentKind.Airframe, "Carga G positiva", StressBand.Red, g, a.MaxG, (g - a.MaxG) / a.MaxG));
                else if (g > a.MaxG * a.GCautionFraction)
                    list.Add(new Stress(ComponentKind.Airframe, "Carga G positiva", StressBand.Yellow, g, a.MaxG, 0));

                if (g < a.MinG)
                    list.Add(new Stress(ComponentKind.Airframe, "Carga G negativa", StressBand.Red, g, a.MinG, (a.MinG - g) / Math.Abs(a.MinG)));
                else if (g < a.MinG * a.GCautionFraction)
                    list.Add(new Stress(ComponentKind.Airframe, "Carga G negativa", StressBand.Yellow, g, a.MinG, 0));

                if (data.HasOverspeed)
                    list.Add(new Stress(ComponentKind.Airframe, "Velocidade acima da Vne", StressBand.Red, data.Airspeed, 0, 0.05));

                if (data.StructuralIcePct > a.StructuralIceCautionPct)
                    list.Add(new Stress(ComponentKind.Airframe, "Gelo estrutural (%)", StressBand.Yellow, data.StructuralIcePct, a.StructuralIceCautionPct, 0));
            }

            return list;
        }

        private void EvaluatePiston(SimTelemetry data, double dtHours, List<Stress> list)
        {
            if (!data.IsEngineRunning)
            {
                _previousCht = null;
                return;
            }

            // RPM
            double redline = _limits.RedlineRpm;
            if (data.EngineRPM > redline * (1.0 + _limits.RpmRedMargin))
                list.Add(new Stress(ComponentKind.Engine, "RPM", StressBand.Red, data.EngineRPM, redline, (data.EngineRPM - redline) / redline));
            else if (data.EngineRPM > redline)
                list.Add(new Stress(ComponentKind.Engine, "RPM", StressBand.Yellow, data.EngineRPM, redline, 0));

            // CHT
            if (data.EngineCHT > _limits.MaxCht)
            {
                list.Add(new Stress(ComponentKind.Engine, "CHT (°C)", StressBand.Red, data.EngineCHT, _limits.MaxCht, (data.EngineCHT - _limits.MaxCht) / _limits.MaxCht));
                list.Add(new Stress(ComponentKind.Oil, "CHT (°C)", StressBand.Yellow, data.EngineCHT, _limits.MaxCht, 0));
            }
            else if (data.EngineCHT > _limits.MaxCht - _limits.ChtCautionMargin)
            {
                list.Add(new Stress(ComponentKind.Engine, "CHT (°C)", StressBand.Yellow, data.EngineCHT, _limits.MaxCht, 0));
            }

            // Shock cooling (queda rápida de CHT em voo)
            if (_previousCht.HasValue && !data.IsOnGround)
            {
                double dtMinutes = dtHours * 60.0;
                double coolingRate = (_previousCht.Value - data.EngineCHT) / dtMinutes;
                if (coolingRate > _limits.ShockCoolingRed)
                    list.Add(new Stress(ComponentKind.Engine, "Shock cooling (°C/min)", StressBand.Red, coolingRate, _limits.ShockCoolingRed, (coolingRate - _limits.ShockCoolingRed) / _limits.ShockCoolingRed));
                else if (coolingRate > _limits.ShockCoolingCaution)
                    list.Add(new Stress(ComponentKind.Engine, "Shock cooling (°C/min)", StressBand.Yellow, coolingRate, _limits.ShockCoolingCaution, 0));
            }
            _previousCht = data.EngineCHT;

            // Velas: táxi prolongado com mistura rica e motor frio
            if (data.IsOnGround && data.EngineRPM < 1200.0 && data.MixturePercent > 90.0 && data.EngineCHT < 130.0)
                list.Add(new Stress(ComponentKind.Ignition, "Táxi com mistura rica", StressBand.Yellow, data.MixturePercent, 90.0, 0));
        }

        private void EvaluateTurbine(SimTelemetry data, List<Stress> list)
        {
            // Hot start: ITT acima do limite de partida com starter acionado
            if (data.StarterActive > 0.5 && data.TurboITT > _limits.MaxStartItt)
            {
                list.Add(new Stress(ComponentKind.HotSection, "ITT na partida (°C)", StressBand.Red, data.TurboITT, _limits.MaxStartItt, (data.TurboITT - _limits.MaxStartItt) / _limits.MaxStartItt));
                return;
            }

            if (!data.IsEngineRunning) return;

            if (data.TurboITT > _limits.MaxItt)
            {
                list.Add(new Stress(ComponentKind.HotSection, "ITT (°C)", StressBand.Red, data.TurboITT, _limits.MaxItt, (data.TurboITT - _limits.MaxItt) / _limits.MaxItt));
                list.Add(new Stress(ComponentKind.Engine, "ITT (°C)", StressBand.Yellow, data.TurboITT, _limits.MaxItt, 0));
            }
            else if (data.TurboITT > _limits.MaxItt - _limits.IttCautionMargin)
            {
                list.Add(new Stress(ComponentKind.HotSection, "ITT (°C)", StressBand.Yellow, data.TurboITT, _limits.MaxItt, 0));
            }

            if (_engineClass == EngineClass.Turboprop)
            {
                if (data.TurboTorque > _limits.MaxTorque + _limits.TorqueRedMargin)
                    list.Add(new Stress(ComponentKind.Engine, "Torque (%)", StressBand.Red, data.TurboTorque, _limits.MaxTorque, (data.TurboTorque - _limits.MaxTorque) / _limits.MaxTorque));
                else if (data.TurboTorque > _limits.MaxTorque)
                    list.Add(new Stress(ComponentKind.Engine, "Torque (%)", StressBand.Yellow, data.TurboTorque, _limits.MaxTorque, 0));
            }

            if (data.EngineN1 > _limits.MaxN1)
                list.Add(new Stress(ComponentKind.Engine, "N1 (%)", StressBand.Red, data.EngineN1, _limits.MaxN1, (data.EngineN1 - _limits.MaxN1) / _limits.MaxN1));
        }

        private static void Damage(IList<AircraftComponent> components, ComponentKind kind, double amount)
        {
            if (amount <= 0) return;
            foreach (var c in components.Where(c => c.Kind == kind)) Damage(c, amount);
        }

        private static void Damage(AircraftComponent component, double amount)
        {
            if (amount <= 0) return;
            component.Health = Math.Clamp(component.Health - amount, 0.0, 100.0);
        }
    }
}
