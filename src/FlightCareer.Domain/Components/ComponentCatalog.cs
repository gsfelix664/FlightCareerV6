using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Catálogo de componentes, limites e parâmetros de calibração do sistema de desgaste/falhas.
    /// Carregado de Data/component_catalog.json para permitir calibrar sem recompilar.
    /// </summary>
    public class ComponentCatalog
    {
        public HazardSettings Hazard { get; set; } = new();
        public VisibilitySettings Visibility { get; set; } = new();
        public LandingSettings Landing { get; set; } = new();

        /// <summary>Abaixo deste valor o componente começa a apresentar sintomas (pré-pane).</summary>
        public double SymptomThreshold { get; set; } = 30.0;

        /// <summary>Multiplicador do desgaste natural quando o componente passou da vida útil (manutenção vencida).</summary>
        public double OverdueMultiplier { get; set; } = 3.0;

        public Dictionary<EngineClass, EngineLimits> EngineLimits { get; set; } = new();
        public AirframeLimits AirframeLimits { get; set; } = new();
        public List<ComponentDefinition> Components { get; set; } = new();
        public ExternalEventSettings ExternalEvents { get; set; } = new();
        public CarbIcingSettings CarbIcing { get; set; } = new();

        /// <summary>Ajustes por modelo de aeronave (limites e classe do motor), casados por trecho do nome do modelo.</summary>
        public List<AircraftOverride> AircraftOverrides { get; set; } = new();

        /// <summary>Primeiro override cujo trecho aparece no modelo (sem diferenciar maiúsculas).</summary>
        public AircraftOverride? FindOverride(string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return null;
            return AircraftOverrides.FirstOrDefault(o => o.Match.Any(m => model.Contains(m, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>Classe do motor: override do catálogo, senão a heurística por combustível/categoria.</summary>
        public EngineClass ResolveEngineClass(Entities.AircraftType? type) =>
            FindOverride(type?.Model)?.EngineClass ?? EngineClassResolver.FromAircraftType(type);

        /// <summary>Limites de motor e célula da aeronave: padrão da classe + overrides do modelo.</summary>
        public (EngineLimits Engine, AirframeLimits Airframe) ResolveLimits(string? model, EngineClass engineClass)
        {
            var engine = LimitsFor(engineClass).Clone();
            var airframe = AirframeLimits.Clone();
            var over = FindOverride(model);
            if (over != null)
            {
                LimitOverrides.Apply(engine, over.EngineLimits);
                LimitOverrides.Apply(airframe, over.AirframeLimits);
            }
            return (engine, airframe);
        }

        public ComponentDefinition Get(ComponentKind kind) =>
            Components.FirstOrDefault(c => c.Kind == kind)
            ?? throw new KeyNotFoundException($"Componente {kind} não definido no catálogo.");

        public IEnumerable<ComponentDefinition> For(EngineClass engineClass) =>
            Components.Where(c => c.AppliesTo.Contains(engineClass));

        public EngineLimits LimitsFor(EngineClass engineClass) =>
            EngineLimits.TryGetValue(engineClass, out var limits)
                ? limits
                : throw new KeyNotFoundException($"Limites de motor para {engineClass} não definidos no catálogo.");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static ComponentCatalog FromJson(string json)
        {
            var catalog = JsonSerializer.Deserialize<ComponentCatalog>(json, JsonOptions)
                ?? throw new InvalidDataException("Catálogo de componentes vazio.");
            catalog.Validate();
            return catalog;
        }

        public static ComponentCatalog LoadFromFile(string path) => FromJson(File.ReadAllText(path));

        /// <summary>Procura Data/component_catalog.json a partir do diretório da aplicação.</summary>
        public static ComponentCatalog LoadDefault()
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "component_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "component_catalog.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", "component_catalog.json")
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("component_catalog.json não encontrado.", candidates[0]);
            return LoadFromFile(path);
        }

        private void Validate()
        {
            foreach (var def in Components)
            {
                foreach (var cls in def.AppliesTo)
                {
                    if (def.LifeHoursFor(cls) <= 0 && def.LifeCyclesFor(cls) <= 0)
                        throw new InvalidDataException($"Componente {def.Kind} sem vida útil (horas ou ciclos) para {cls}.");
                    if (!EngineLimits.ContainsKey(cls))
                        throw new InvalidDataException($"Limites de motor ausentes para {cls}.");
                }
                if (def.FailureModes.Count == 0)
                    throw new InvalidDataException($"Componente {def.Kind} sem modos de falha.");
            }

            foreach (var over in AircraftOverrides)
            {
                LimitOverrides.Validate<EngineLimits>(over.EngineLimits);
                LimitOverrides.Validate<AirframeLimits>(over.AirframeLimits);
            }
        }
    }

    public class ComponentDefinition
    {
        public ComponentKind Kind { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<EngineClass> AppliesTo { get; set; } = new();

        /// <summary>Componente crítico entra no cálculo de aeronavegabilidade (pior componente crítico).</summary>
        public bool Critical { get; set; }

        /// <summary>Peso na condição geral (média ponderada).</summary>
        public double Weight { get; set; }

        /// <summary>Vida útil em horas de operação por classe de motor (0 = não é por horas).</summary>
        public Dictionary<EngineClass, double> LifeHours { get; set; } = new();

        /// <summary>Vida útil em ciclos por classe de motor (0 = não é por ciclos).</summary>
        public Dictionary<EngineClass, double> LifeCycles { get; set; } = new();

        /// <summary>Quanto o desgaste natural consome ao longo da vida útil (ex.: 70 = chega a 30% no fim da vida).</summary>
        public double NaturalLossOverLife { get; set; } = 70.0;

        /// <summary>Perda adicional em %/hora enquanto algum parâmetro está na faixa amarela.</summary>
        public double YellowLossPerHour { get; set; }

        /// <summary>Perda adicional em %/hora na faixa vermelha (escala com o quanto o limite foi excedido).</summary>
        public double RedLossPerHour { get; set; }

        /// <summary>Multiplicador da taxa de pane abaixo do limiar (componentes mais robustos &lt; 1).</summary>
        public double HazardScale { get; set; } = 1.0;

        /// <summary>Se o walkaround do pré-voo consegue estimar este componente.</summary>
        public bool WalkaroundVisible { get; set; }

        /// <summary>Nome do serviço que restaura o componente (ex.: "Troca de pneus").</summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>Custo de um reparo completo (0 → 100%) como fração do preço da aeronave nova.</summary>
        public double RepairCostFactor { get; set; }

        /// <summary>Custo mínimo de um reparo completo (0 → 100%), em dólares.</summary>
        public double MinFullRepairCost { get; set; }

        public List<FailureModeDefinition> FailureModes { get; set; } = new();

        public double LifeHoursFor(EngineClass cls) => LifeHours.TryGetValue(cls, out var v) ? v : 0.0;
        public double LifeCyclesFor(EngineClass cls) => LifeCycles.TryGetValue(cls, out var v) ? v : 0.0;

        /// <summary>Desgaste natural em %/hora de operação.</summary>
        public double NaturalLossPerHour(EngineClass cls)
        {
            double life = LifeHoursFor(cls);
            return life > 0 ? NaturalLossOverLife / life : 0.0;
        }

        /// <summary>Desgaste natural em % por ciclo.</summary>
        public double NaturalLossPerCycle(EngineClass cls)
        {
            double life = LifeCyclesFor(cls);
            return life > 0 ? NaturalLossOverLife / life : 0.0;
        }

        public IEnumerable<FailureModeDefinition> ModesFor(EngineClass cls) =>
            FailureModes.Where(m => m.AppliesTo == null || m.AppliesTo.Count == 0 || m.AppliesTo.Contains(cls));
    }

    public class FailureModeDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Weight { get; set; } = 1.0;
        public bool Critical { get; set; }
        public List<EngineClass>? AppliesTo { get; set; }

        /// <summary>Efeito aplicado no simulador quando esta pane acontece.</summary>
        public FailureEffect Effect { get; set; } = FailureEffect.None;

        /// <summary>Parâmetro do efeito (ex.: taxa de vazamento de óleo em %/min).</summary>
        public double EffectValue { get; set; }

        /// <summary>Dano imediato (em %) aplicado quando a pane acontece.</summary>
        public double Damage { get; set; }

        /// <summary>Componente que recebe o dano imediato (padrão: o próprio componente).</summary>
        public ComponentKind? DamageKind { get; set; }
    }

    public class AircraftOverride
    {
        /// <summary>Trechos do nome do modelo (ex.: "C152", "A320").</summary>
        public List<string> Match { get; set; } = new();
        public EngineClass? EngineClass { get; set; }
        public Dictionary<string, double> EngineLimits { get; set; } = new();
        public Dictionary<string, double> AirframeLimits { get; set; } = new();
    }

    /// <summary>Aplica overrides parciais (nome da propriedade → valor) sobre os limites padrão.</summary>
    public static class LimitOverrides
    {
        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;

        public static void Apply<T>(T target, Dictionary<string, double> values) where T : class
        {
            foreach (var (name, value) in values)
                typeof(T).GetProperty(name, Flags)?.SetValue(target, value);
        }

        public static void Validate<T>(Dictionary<string, double> values)
        {
            foreach (var name in values.Keys)
            {
                var prop = typeof(T).GetProperty(name, Flags);
                if (prop == null || prop.PropertyType != typeof(double))
                    throw new InvalidDataException($"Override inválido: {typeof(T).Name} não tem o limite '{name}'.");
            }
        }
    }

    public class ExternalEventSettings
    {
        public BirdStrikeSettings BirdStrike { get; set; } = new();
        public FodSettings Fod { get; set; } = new();
        public PostMaintenanceSettings PostMaintenance { get; set; } = new();
    }

    public class BirdStrikeSettings
    {
        public double MaxAltitudeFt { get; set; } = 3500.0;
        public double MinAirspeedKts { get; set; } = 75.0;
        /// <summary>Eventos/hora a 100 kt na faixa de altitude (escala com (IAS/100)²).</summary>
        public double LambdaPerHourAt100Kts { get; set; } = 0.001;
        public List<ExternalOutcome> Outcomes { get; set; } = new();
    }

    public class FodSettings
    {
        public double MinGroundSpeedKts { get; set; } = 25.0;
        public double MinThrottlePercent { get; set; } = 65.0;
        public double LambdaPerHour { get; set; } = 0.002;
        public double WetMultiplier { get; set; } = 3.0;
        public List<ExternalOutcome> Outcomes { get; set; } = new();
    }

    public class PostMaintenanceSettings
    {
        /// <summary>Janela após o serviço (horas de uso do componente) com risco de falha de montagem.</summary>
        public double WindowHours { get; set; } = 2.0;
        /// <summary>Janela para componentes medidos por ciclos (freios, pneus, trem): pousos desde o serviço.</summary>
        public int WindowCycles { get; set; } = 3;
        public double LambdaPerHour { get; set; } = 0.03;
        public List<ExternalOutcome> Outcomes { get; set; } = new();
    }

    public class ExternalOutcome
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComponentKind Kind { get; set; }
        public double Weight { get; set; } = 1.0;
        public double DamageMin { get; set; }
        public double DamageMax { get; set; }
        public FailureEffect Effect { get; set; } = FailureEffect.None;
        public double EffectValue { get; set; }
        public bool Critical { get; set; }
        public List<EngineClass>? AppliesTo { get; set; }
    }

    public class CarbIcingSettings
    {
        public double MaxCarbTemperature { get; set; } = 0.0;      // °C no venturi
        public double MaxThrottlePercent { get; set; } = 45.0;
        public double MinAmbientTemperature { get; set; } = -5.0;  // Faixa de ar úmido sem precipitação
        public double MaxAmbientTemperature { get; set; } = 25.0;
        public double WarningSeconds { get; set; } = 30.0;         // Aviso de gelo formando
        public double StopSeconds { get; set; } = 90.0;            // Motor apaga
        public double RecoverySeconds { get; set; } = 30.0;        // Carb heat ligado para recuperar
    }

    public class HazardSettings
    {
        /// <summary>Acima deste valor de saúde não há chance de pane por desgaste.</summary>
        public double Threshold { get; set; } = 20.0;

        /// <summary>Falhas/hora exatamente no limiar.</summary>
        public double LambdaAtThreshold { get; set; } = 0.02;

        /// <summary>Falhas/hora com saúde 0%.</summary>
        public double LambdaAtZero { get; set; } = 2.0;
    }

    public class VisibilitySettings
    {
        /// <summary>Meia-largura máxima (em %) da faixa exibida.</summary>
        public double MaxHalfWidth { get; set; } = 25.0;

        /// <summary>Quanto a meia-largura cresce por hora de célula desde a última inspeção.</summary>
        public double GrowthPerHour { get; set; } = 0.4;

        /// <summary>Meia-largura após um walkaround do pré-voo.</summary>
        public double WalkaroundHalfWidth { get; set; } = 10.0;

        /// <summary>Deslocamento máximo do centro da faixa (fração da meia-largura, &lt; 1).</summary>
        public double MaxBias { get; set; } = 0.6;
    }

    public class LandingSettings
    {
        public double HardLandingFpm { get; set; } = 400.0;
        public double SevereLandingFpm { get; set; } = 600.0;
        public double LateralLimitFps { get; set; } = 5.0;

        /// <summary>Dano em % por 100 FPM acima do pouso duro.</summary>
        public double GearDamagePer100Fpm { get; set; } = 1.5;
        public double TireDamagePer100Fpm { get; set; } = 2.0;

        /// <summary>Dano extra em % por 100 FPM acima do pouso severo.</summary>
        public double GearSevereDamagePer100Fpm { get; set; } = 5.0;
        public double AirframeSevereDamagePer100Fpm { get; set; } = 2.0;

        /// <summary>Dano em % por fps de velocidade lateral acima do limite.</summary>
        public double GearLateralDamagePerFps { get; set; } = 1.2;
        public double TireLateralDamagePerFps { get; set; } = 1.5;

        /// <summary>Perda dos freios em %/hora com freio total a 60 kt (escala com freio × (GS/60)²).</summary>
        public double BrakeEnergyLossPerHour { get; set; } = 72.0;

        /// <summary>Multiplicador de desgaste de pneus/freios em pista molhada ou contaminada.</summary>
        public double WetSurfaceMultiplier { get; set; } = 1.8;

        /// <summary>Pouso duro pode trincar o cárter/retentor: chance cresce de 0 em OilLeakMinFpm até OilLeakChanceAt1000Fpm.</summary>
        public double OilLeakMinFpm { get; set; } = 650.0;
        public double OilLeakChanceAt1000Fpm { get; set; } = 0.5;
        public double OilLeakRatePerMin { get; set; } = 3.5;
    }

    public class EngineLimits
    {
        // Pistão
        public double RedlineRpm { get; set; } = 2700.0;
        public double RpmRedMargin { get; set; } = 0.02;      // Vermelho acima de redline × (1 + margem)
        public double MaxCht { get; set; } = 260.0;            // °C
        public double ChtCautionMargin { get; set; } = 20.0;   // Amarelo a partir de MaxCht - margem
        public double ShockCoolingCaution { get; set; } = 28.0; // °C/min de queda de CHT (≈ 50 °F/min)
        public double ShockCoolingRed { get; set; } = 50.0;

        // Óleo (comum)
        public double MaxOilTemp { get; set; } = 118.0;        // °C
        public double OilTempCautionMargin { get; set; } = 8.0;
        public double MinOilPressure { get; set; } = 20.0;     // psi (motor girando acima do idle)
        public double MinOilTempForHighPower { get; set; } = 38.0; // °C (potência alta com óleo frio)
        public double HighPowerThrottlePercent { get; set; } = 75.0;

        // Turbina
        public double MaxItt { get; set; } = 805.0;            // °C
        public double IttCautionMargin { get; set; } = 40.0;
        public double MaxStartItt { get; set; } = 1000.0;      // °C (hot start)
        public double MaxTorque { get; set; } = 100.0;         // %
        public double TorqueRedMargin { get; set; } = 5.0;     // Vermelho acima de MaxTorque + margem
        public double MaxN1 { get; set; } = 101.5;             // %

        // Elétrico
        public double MaxStarterSeconds { get; set; } = 30.0;  // Ciclo de trabalho do motor de arranque

        public EngineLimits Clone() => (EngineLimits)MemberwiseClone();
    }

    public class AirframeLimits
    {
        public double MaxG { get; set; } = 3.8;
        public double MinG { get; set; } = -1.52;
        public double GCautionFraction { get; set; } = 0.85;   // Amarelo a partir de 85% do limite
        public double StructuralIceCautionPct { get; set; } = 10.0;

        public AirframeLimits Clone() => (AirframeLimits)MemberwiseClone();
    }
}
