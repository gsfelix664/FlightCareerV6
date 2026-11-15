using System;
using System.Collections.Generic;

namespace FlightCareer.Domain.Parts
{
    public enum WearRuleKind
    {
        Continuous,
        Event
    }

    public enum LimitDirection
    {
        Above,
        Below
    }

    /// <summary>
    /// Efeito de desgaste em uma peça específica dentro de uma faixa da regra.
    /// Multiplier: fator multiplicador sobre o desgaste normal por hora.
    /// LossPerHourPct: perda direta de saúde em %/h (adicional ao multiplicador).
    /// DamagePerEventPct: dano direto em % sofrido no evento.
    /// </summary>
    public sealed record WearRulePart(
        string PartId,
        double Multiplier = 1.0,
        double LossPerHourPct = 0.0,
        double DamagePerEventPct = 0.0);

    /// <summary>
    /// Discrepância de inspeção gerada caso a operação atinja condição crítica ou gravidade prolongada.
    /// DefectCode: código da discrepância em maiúsculas com underscore (ex.: ENGINE_OVERSPEED_INSPECTION).
    /// Grounding: se interdita a aeronave até inspeção no Hangar.
    /// MinSeconds: tempo mínimo de exposição contínua na faixa para disparar a inspeção (opcional).
    /// MinExcessPct: excesso percentual mínimo acima do limiar para disparar inspeção (opcional).
    /// </summary>
    public sealed record WearRuleInspection(
        string DefectCode,
        bool Grounding,
        double? MinSeconds = null,
        double? MinExcessPct = null,
        string Description = "");

    /// <summary>
    /// Sensor nativo de sobretensão/estresse (overstress) do MSFS 2024.
    /// Usado exclusivamente como reserva (fallback) quando o limite da aeronave for ClassDefault (Regra 0.1 › 3).
    /// </summary>
    public sealed record NativeOverstress(
        int ComponentId,
        double Threshold);

    /// <summary>
    /// Faixa de severidade da regra (ex.: caution, warning, severe).
    /// Threshold: valor relativo ao limite (ex.: 0.97 = 97%, 1.00 = 100%, 1.08 = 108%) para regras contínuas,
    /// ou valor escalar da métrica (ex.: 600 fpm) para regras de evento.
    /// </summary>
    public sealed record WearRuleBand(
        string Name,
        double Threshold,
        IReadOnlyList<WearRulePart> Parts,
        WearRuleInspection? Inspection = null);

    /// <summary>
    /// Regra operacional de desgaste por sensor/condição ou evento.
    /// </summary>
    public sealed class WearRule
    {
        public string Id { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public WearRuleKind Kind { get; init; } = WearRuleKind.Continuous;
        public PartAppliesTo AppliesTo { get; init; } = new();
        public IReadOnlyList<string> Sensors { get; init; } = Array.Empty<string>();
        /// <summary>
        /// Chave de limite operacional (do catálogo LimitKeys, ex.: rpmRedline, ittMax, vle, vlo,
        /// ou convenções dinâmicas como 'vfe:current' para o dente de flap em uso).
        /// </summary>
        public string? LimitKey { get; init; }
        public LimitDirection Direction { get; init; } = LimitDirection.Above;
        public string? Metric { get; init; }
        public IReadOnlyList<WearRuleBand> Bands { get; init; } = Array.Empty<WearRuleBand>();
        public WearRuleInspection? Inspection { get; init; }
        public NativeOverstress? NativeOverstress { get; init; }
        public string Source { get; init; } = string.Empty;
        public bool RequiresEngineRunning { get; init; } = false;
        public double EngineRunningDelaySeconds { get; init; } = 0.0;

        public WearRule() { }

        public WearRule(
            string id,
            string description,
            WearRuleKind kind,
            PartAppliesTo appliesTo,
            IReadOnlyList<string> sensors,
            string? limitKey,
            LimitDirection direction,
            string? metric,
            IReadOnlyList<WearRuleBand> bands,
            WearRuleInspection? inspection,
            NativeOverstress? nativeOverstress,
            string source,
            bool requiresEngineRunning = false,
            double engineRunningDelaySeconds = 0.0)
        {
            Id = id;
            Description = description;
            Kind = kind;
            AppliesTo = appliesTo ?? new PartAppliesTo();
            Sensors = sensors ?? Array.Empty<string>();
            LimitKey = limitKey;
            Direction = direction;
            Metric = metric;
            Bands = bands ?? Array.Empty<WearRuleBand>();
            Inspection = inspection;
            NativeOverstress = nativeOverstress;
            Source = source;
            RequiresEngineRunning = requiresEngineRunning;
            EngineRunningDelaySeconds = engineRunningDelaySeconds;
        }
    }
}
