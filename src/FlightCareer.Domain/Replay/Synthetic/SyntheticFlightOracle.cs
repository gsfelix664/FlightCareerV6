using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Limits;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Domain.Replay.Synthetic
{
    /// <summary>
    /// Oráculo para validação de invariantes, gabarito e propriedades metamórficas de voos sintéticos.
    /// </summary>
    public static class SyntheticFlightOracle
    {
        /// <summary>
        /// Incremento mínimo estritamente positivo esperado de dano em relação ao controle para voos de mesma duração.
        /// </summary>
        private const double MinStrictLossDifference = 1e-6;

        /// <summary>
        /// Folga de 1% para acomodar a não linearidade do desgaste natural ao comparar voos de durações desiguais.
        /// </summary>
        private const double ScaledControlToleranceRatio = 0.01;

        /// <summary>
        /// Valida todas as invariantes universais que qualquer voo (com ou sem abusos) deve obedecer.
        /// Retorna a lista de violações encontradas (vazia se 100% aprovado).
        /// </summary>
        public static IReadOnlyList<string> VerifyInvariants(SyntheticFlight flight, FlightReplayResult result)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var violations = new List<string>();

            // 1. Sem NaN ou Infinity em nenhum resultado
            if (double.IsNaN(result.TotalSimHours) || double.IsInfinity(result.TotalSimHours))
            {
                violations.Add($"Invariante violada: TotalSimHours é inválido ({result.TotalSimHours}).");
            }

            foreach (var part in result.Parts)
            {
                if (double.IsNaN(part.FinalHealth) || double.IsInfinity(part.FinalHealth))
                {
                    violations.Add($"Invariante violada: Peça '{part.PartId}:{part.EngineIndex}' tem FinalHealth inválido ({part.FinalHealth}).");
                }
                if (double.IsNaN(part.InitialHealth) || double.IsInfinity(part.InitialHealth))
                {
                    violations.Add($"Invariante violada: Peça '{part.PartId}:{part.EngineIndex}' tem InitialHealth inválido ({part.InitialHealth}).");
                }
                if (double.IsNaN(part.HoursSinceReplacement) || double.IsInfinity(part.HoursSinceReplacement))
                {
                    violations.Add($"Invariante violada: Peça '{part.PartId}:{part.EngineIndex}' tem HoursSinceReplacement inválido ({part.HoursSinceReplacement}).");
                }

                // 2. Saúde sempre em [0, 100]
                if (part.FinalHealth < 0.0 || part.FinalHealth > 100.0)
                {
                    violations.Add($"Invariante violada: Peça '{part.PartId}:{part.EngineIndex}' com saúde fora de [0, 100]: {part.FinalHealth:F2}%.");
                }

                // 3. Saúde nunca sobe durante o voo (não-crescente)
                if (part.FinalHealth > part.InitialHealth + 1e-6)
                {
                    violations.Add($"Invariante violada: Peça '{part.PartId}:{part.EngineIndex}' aumentou saúde durante o voo (inicial: {part.InitialHealth:F2}%, final: {part.FinalHealth:F2}%).");
                }
            }

            foreach (var ex in result.Exceedances)
            {
                if (double.IsNaN(ex.PeakValue) || double.IsInfinity(ex.PeakValue))
                {
                    violations.Add($"Invariante violada: Excedência '{ex.RuleId}' tem PeakValue inválido ({ex.PeakValue}).");
                }
                if (double.IsNaN(ex.DurationSeconds) || double.IsInfinity(ex.DurationSeconds))
                {
                    violations.Add($"Invariante violada: Excedência '{ex.RuleId}' tem DurationSeconds inválido ({ex.DurationSeconds}).");
                }
            }

            // 4. Nenhum diagnóstico repetido
            var duplicateDiagnostics = result.Diagnostics
                .GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateDiagnostics.Count > 0)
            {
                violations.Add($"Invariante violada: Diagnósticos repetidos encontrados: {string.Join(" | ", duplicateDiagnostics)}.");
            }

            // 5. Horas simuladas acumuladas nas peças
            // Todas as peças instaladas (não quebradas) devem registrar as horas simuladas do voo (±1% ou ±0.003h)
            if (result.TotalSimHours > 0.01)
            {
                foreach (var part in result.Parts)
                {
                    if (part.IsBroken) continue;

                    double diff = Math.Abs(part.HoursSinceReplacement - result.TotalSimHours);
                    double allowedTolerance = Math.Max(0.003, result.TotalSimHours * 0.01);
                    if (diff > allowedTolerance)
                    {
                        violations.Add($"Invariante violada: Horas da peça '{part.PartId}:{part.EngineIndex}' ({part.HoursSinceReplacement:F4}h) diferem das horas simuladas do voo ({result.TotalSimHours:F4}h, diff={diff:F4}h).");
                        break;
                    }
                }
            }

            // 6. Ciclos de pouso nas peças estruturais e de trem associadas à regra de pouso
            if (!flight.GroundTruth.IsCancelled && flight.GroundTruth.ExpectedLandingCycles > 0)
            {
                var landingParts = result.Parts.Where(p =>
                    p.PartId.Equals("gear.wheels_bearings", StringComparison.OrdinalIgnoreCase) ||
                    p.PartId.Equals("gear.trunnions_side_stays", StringComparison.OrdinalIgnoreCase) ||
                    p.PartId.Equals("fuselage.frames_bulkheads", StringComparison.OrdinalIgnoreCase) ||
                    p.PartId.Equals("wing.root_fittings", StringComparison.OrdinalIgnoreCase)).ToList();

                if (landingParts.Count > 0)
                {
                    foreach (var part in landingParts)
                    {
                        if (part.CyclesSinceReplacement != flight.GroundTruth.ExpectedLandingCycles)
                        {
                            violations.Add($"Invariante violada: Peça '{part.PartId}' tem {part.CyclesSinceReplacement} ciclos, esperado {flight.GroundTruth.ExpectedLandingCycles}.");
                            break;
                        }
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// Valida se o gabarito do abuso injetado foi respeitado comparando com o voo-controle.
        /// </summary>
        public static IReadOnlyList<string> VerifyGroundTruth(
            FlightReplayResult controlResult,
            FlightReplayResult abusedResult,
            SyntheticAbuseRecord abuse,
            AircraftLimitProfile? profile = null,
            WearRuleCatalog? wearRuleCatalog = null)
        {
            if (controlResult == null) throw new ArgumentNullException(nameof(controlResult));
            if (abusedResult == null) throw new ArgumentNullException(nameof(abusedResult));
            if (abuse == null) throw new ArgumentNullException(nameof(abuse));

            var violations = new List<string>();

            // Se o abuso for FlapOverSpeed em aeronave sem flaps (noFlaps), abuso é inaplicável: gabarito ignorado sem violação
            if (abuse.Type == SyntheticAbuseType.FlapOverSpeed &&
                profile?.Features != null &&
                profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoFlaps, StringComparison.OrdinalIgnoreCase)))
            {
                return violations;
            }

            // Se o abuso for elétrico em aeronave sem sistema elétrico (noElectrical), abuso é inaplicável: gabarito ignorado sem violação
            if ((abuse.Type == SyntheticAbuseType.BatteryDischarging ||
                 abuse.Type == SyntheticAbuseType.ElectricalOverload ||
                 abuse.Type == SyntheticAbuseType.AvionicsMasterOnStart) &&
                profile?.Features != null &&
                profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
            {
                return violations;
            }

            // Se o perfil da aeronave foi fornecido, verifica se o limite operacional está presente no catálogo
            if (profile != null)
            {
                string? missingKey = CheckMissingLimit(abuse.Type, profile);
                if (missingKey != null)
                {
                    return new[] { $"limite ausente: {missingKey}" };
                }
            }

            // Se o abuso não chegou a acontecer porque o voo encerrou antes do seu início
            double simDuration = abusedResult.TotalSimHours * 3600.0;
            if (abuse.StartSeconds >= simDuration - 1.0)
            {
                return violations;
            }

            // Se o abuso for de acumulador contínuo em tempo, verifica se coube no voo para acumular o threshold mínimo
            if (abuse.Type == SyntheticAbuseType.BatteryDischarging)
            {
                double minDischargeMinutes = wearRuleCatalog?.Rules
                    .FirstOrDefault(r => r.Metric == WearRuleMetrics.BatteryDeepDischargeMinutes)?
                    .Bands.Min(b => b.Threshold) ?? 10.0;
                double minRequiredSeconds = minDischargeMinutes * 60.0;

                double activeSimSeconds = Math.Min(abuse.DurationSeconds, Math.Max(0.0, simDuration - abuse.StartSeconds));
                if (activeSimSeconds < minRequiredSeconds)
                {
                    // Descarga profunda exige threshold mínimo acumulado; não esperar evento
                    return violations;
                }
            }
            else if (abuse.Type == SyntheticAbuseType.ElectricalOverload)
            {
                double minOverloadMinutes = wearRuleCatalog?.Rules
                    .FirstOrDefault(r => r.Metric == WearRuleMetrics.ElectricalOverloadMinutes)?
                    .Bands.Min(b => b.Threshold) ?? 5.0;
                double minRequiredSeconds = minOverloadMinutes * 60.0;

                double activeSimSeconds = Math.Min(abuse.DurationSeconds, Math.Max(0.0, simDuration - abuse.StartSeconds));
                if (activeSimSeconds < minRequiredSeconds)
                {
                    // Sobrecarga elétrica exige threshold mínimo acumulado; não esperar evento
                    return violations;
                }
            }

            // 1. Se o abuso especificou uma regra esperada, ela deve ter sido acionada
            if (!string.IsNullOrWhiteSpace(abuse.TargetRuleId))
            {
                bool ruleTriggered = abusedResult.Exceedances.Any(e => e.RuleId.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase)) ||
                                     abusedResult.DerivedEvents.Any(e => e.MetricKey.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase) ||
                                                                         IsEventMetricMatchingRule(abuse.TargetRuleId, e.MetricKey)) ||
                                     abusedResult.Inspections.Any(i => i.RuleId.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase));

                if (!ruleTriggered)
                {
                    violations.Add($"Gabarito violado: Abuso '{abuse.Type}' não disparou a regra esperada '{abuse.TargetRuleId}'.");
                }
                else if (abuse.EngineIndex > 0)
                {
                    // Confere motor correto
                    bool targetEngineMatched = abusedResult.Exceedances.Any(e =>
                        e.RuleId.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase) &&
                        (e.TargetId.Contains($":{abuse.EngineIndex}") || e.Notes.Contains($"Motor {abuse.EngineIndex}")));

                    if (!targetEngineMatched)
                    {
                        bool derivedEngineMatched = abusedResult.DerivedEvents.Any(e =>
                            (e.MetricKey.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase) ||
                             IsEventMetricMatchingRule(abuse.TargetRuleId, e.MetricKey)) &&
                            e.EngineIndex == abuse.EngineIndex);

                        if (!derivedEngineMatched)
                        {
                            bool inspectionMatched = abusedResult.Inspections.Any(i =>
                                i.RuleId.Equals(abuse.TargetRuleId, StringComparison.OrdinalIgnoreCase) &&
                                (i.TargetPartId.Contains($":{abuse.EngineIndex}") || i.Description.Contains($"Motor {abuse.EngineIndex}")));

                            if (!inspectionMatched)
                            {
                                violations.Add($"Gabarito violado: Abuso '{abuse.Type}' no motor {abuse.EngineIndex} não registrou o evento/excedência para o motor correto.");
                            }
                        }
                    }
                }
            }

            // 2. A perda na peça alvo deve ser estritamente maior que no voo controle quando a regra tem efeito
            if (!string.IsNullOrWhiteSpace(abuse.TargetPartId))
            {
                var controlPart = controlResult.Parts.FirstOrDefault(p =>
                    p.PartId.Equals(abuse.TargetPartId, StringComparison.OrdinalIgnoreCase) &&
                    p.EngineIndex == abuse.EngineIndex)
                    ?? controlResult.Parts.FirstOrDefault(p =>
                    p.PartId.Equals(abuse.TargetPartId, StringComparison.OrdinalIgnoreCase) &&
                    p.EngineIndex == 0);

                var abusedPart = abusedResult.Parts.FirstOrDefault(p =>
                    p.PartId.Equals(abuse.TargetPartId, StringComparison.OrdinalIgnoreCase) &&
                    p.EngineIndex == abuse.EngineIndex)
                    ?? abusedResult.Parts.FirstOrDefault(p =>
                    p.PartId.Equals(abuse.TargetPartId, StringComparison.OrdinalIgnoreCase) &&
                    p.EngineIndex == 0);

                if (controlPart != null && abusedPart != null)
                {
                    double controlHours = controlResult.TotalSimHours;
                    double abusedHours = abusedResult.TotalSimHours;

                    // Se o voo com abuso teve duração de simulação menor que o controle (ex: cancelamento prematuro ou gap),
                    // compara contra a perda do controle escalada pela fração de tempo percorrida
                    if (abusedHours < controlHours - 1e-5)
                    {
                        double scaledControl = controlHours > 0.0
                            ? controlPart.HealthLoss * (abusedHours / controlHours)
                            : controlPart.HealthLoss;
                        double lossDiff = abusedPart.HealthLoss - scaledControl;

                        if (lossDiff < -ScaledControlToleranceRatio * scaledControl)
                        {
                            violations.Add($"Gabarito violado: Perda na peça '{abuse.TargetPartId}' com abuso ({abusedPart.HealthLoss:F4}%) foi inferior ao controle escalado ({scaledControl:F4}%, diff={lossDiff:F6}%) em comparação escalada por duração menor ({abusedHours:F2}h vs {controlHours:F2}h).");
                        }
                    }
                    else
                    {
                        double lossDiff = abusedPart.HealthLoss - controlPart.HealthLoss;
                        if (lossDiff < MinStrictLossDifference)
                        {
                            violations.Add($"Gabarito violado: Perda na peça '{abuse.TargetPartId}' com abuso ({abusedPart.HealthLoss:F4}%) não foi estritamente maior que no voo controle ({controlPart.HealthLoss:F4}%, diff={lossDiff:F6}%).");
                        }
                    }
                }
            }

            return violations;
        }

        private static string? CheckMissingLimit(SyntheticAbuseType type, AircraftLimitProfile profile)
        {
            switch (type)
            {
                case SyntheticAbuseType.VneExceeded:
                    if (!profile.Limits.Any(l => string.Equals(l.Key, LimitKeys.Vne, StringComparison.OrdinalIgnoreCase)))
                        return LimitKeys.Vne;
                    break;

                case SyntheticAbuseType.FlapOverSpeed:
                    if (profile.Features != null && profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoFlaps, StringComparison.OrdinalIgnoreCase)))
                        return null;
                    if (!profile.Limits.Any(l => l.Key.StartsWith("vfe", StringComparison.OrdinalIgnoreCase)))
                        return "vfe";
                    break;

                case SyntheticAbuseType.BatteryDischarging:
                    if (profile.Features != null && profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
                        return null;
                    if (!profile.Limits.Any(l => string.Equals(l.Key, LimitKeys.ElecMainBusIndex, StringComparison.OrdinalIgnoreCase)))
                        return LimitKeys.ElecMainBusIndex;
                    break;

                case SyntheticAbuseType.ElectricalOverload:
                    if (profile.Features != null && profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
                        return null;
                    if (!profile.Limits.Any(l => string.Equals(l.Key, LimitKeys.AlternatorRatedAmps, StringComparison.OrdinalIgnoreCase)))
                        return LimitKeys.AlternatorRatedAmps;
                    if (!profile.Limits.Any(l => string.Equals(l.Key, LimitKeys.ElecMainBusIndex, StringComparison.OrdinalIgnoreCase)))
                        return LimitKeys.ElecMainBusIndex;
                    break;

                case SyntheticAbuseType.AvionicsMasterOnStart:
                    if (profile.Features != null && profile.Features.Any(f => string.Equals(f, AircraftFeatures.NoElectrical, StringComparison.OrdinalIgnoreCase)))
                        return null;
                    if (!profile.Limits.Any(l => string.Equals(l.Key, LimitKeys.ElecAvionicsCircuitIndex, StringComparison.OrdinalIgnoreCase)))
                        return LimitKeys.ElecAvionicsCircuitIndex;
                    break;
            }

            return null;
        }

        private static bool IsEventMetricMatchingRule(string ruleId, string metricKey)
        {
            if (string.Equals(ruleId, "engine_starter_duty_cycle", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "starterSeconds", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(ruleId, "avionics_start_with_avionics_master_on", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "avionicsMasterStartSpikes", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(ruleId, "battery_deep_discharge_damage", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "batteryDeepDischargeMinutes", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(ruleId, "electrical_system_overload_amps", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "electricalOverloadMinutes", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(ruleId, "gear_cycle_stress", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "gearCycles", StringComparison.OrdinalIgnoreCase)) return true;

            if (string.Equals(ruleId, "touchdown_hard_landing", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(metricKey, "touchdownFpm", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        /// <summary>
        /// Validação metamórfica: mesmo voo a 1x e a 4x de sim rate com o mesmo tempo simulado deve gerar perda semelhante (±2%).
        /// </summary>
        public static IReadOnlyList<string> VerifyMetamorphicSimRate(FlightReplayResult result1x, FlightReplayResult result4x)
        {
            var violations = new List<string>();

            double hoursDiff = Math.Abs(result1x.TotalSimHours - result4x.TotalSimHours);
            if (hoursDiff > Math.Max(0.01, result1x.TotalSimHours * 0.02))
            {
                violations.Add($"Metamórfico violado: Horas simuladas divergem entre 1x e 4x (1x={result1x.TotalSimHours:F4}h, 4x={result4x.TotalSimHours:F4}h, diff={hoursDiff:F4}h).");
            }

            double totalLoss1x = result1x.Parts.Sum(p => p.HealthLoss);
            double totalLoss4x = result4x.Parts.Sum(p => p.HealthLoss);
            double lossDiff = Math.Abs(totalLoss1x - totalLoss4x);

            if (lossDiff > Math.Max(0.10, totalLoss1x * 0.05))
            {
                violations.Add($"Metamórfico violado: Perda de saúde total diverge entre 1x e 4x (1x={totalLoss1x:F3}%, 4x={totalLoss4x:F3}%, diff={lossDiff:F3}%).");
            }

            return violations;
        }

        /// <summary>
        /// Validação metamórfica: em bimotor, abuso exclusivo no motor 2 deve deixar o motor 1 com desgaste idêntico ao controle.
        /// </summary>
        public static IReadOnlyList<string> VerifyMetamorphicTwinEngine(FlightReplayResult controlResult, FlightReplayResult engine2AbusedResult)
        {
            var violations = new List<string>();

            var eng1PartsControl = controlResult.Parts.Where(p => p.EngineIndex == 1).ToList();
            var eng1PartsAbused = engine2AbusedResult.Parts.Where(p => p.EngineIndex == 1).ToList();

            foreach (var pCtrl in eng1PartsControl)
            {
                var pAbused = eng1PartsAbused.FirstOrDefault(p => p.PartId.Equals(pCtrl.PartId, StringComparison.OrdinalIgnoreCase));
                if (pAbused != null)
                {
                    double diff = Math.Abs(pCtrl.HealthLoss - pAbused.HealthLoss);
                    if (diff > 0.01)
                    {
                        violations.Add($"Metamórfico bimotor violado: Peça '{pCtrl.PartId}' do motor 1 sofreu alteração com abuso no motor 2 (ctrl={pCtrl.HealthLoss:F3}%, abused={pAbused.HealthLoss:F3}%, diff={diff:F3}%).");
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// Validação metamórfica de monotonicidade: abuso mais intenso deve produzir perda maior ou igual à de menor intensidade.
        /// </summary>
        public static IReadOnlyList<string> VerifyMetamorphicMonotonicity(
            FlightReplayResult lowAbuseResult,
            FlightReplayResult highAbuseResult,
            string targetPartId,
            int engineIndex = 1)
        {
            var violations = new List<string>();

            var lowPart = lowAbuseResult.Parts.FirstOrDefault(p =>
                p.PartId.Equals(targetPartId, StringComparison.OrdinalIgnoreCase) &&
                (p.EngineIndex == engineIndex || p.EngineIndex == 0));

            var highPart = highAbuseResult.Parts.FirstOrDefault(p =>
                p.PartId.Equals(targetPartId, StringComparison.OrdinalIgnoreCase) &&
                (p.EngineIndex == engineIndex || p.EngineIndex == 0));

            if (lowPart != null && highPart != null)
            {
                if (highPart.HealthLoss < lowPart.HealthLoss - 1e-6)
                {
                    violations.Add($"Monotonicidade violada: Peça '{targetPartId}' teve menor perda sob abuso mais intenso (baixo={lowPart.HealthLoss:F3}%, alto={highPart.HealthLoss:F3}%).");
                }
            }

            return violations;
        }
    }
}
