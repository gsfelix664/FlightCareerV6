using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using FlightCareer.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>
    /// Aeronave do hangar para a UI: identificação, barras por componente (com saúde oculta),
    /// status, inspeções, abastecimento e seguro.
    /// </summary>
    public partial class PilotAircraftWrapper : ObservableObject
    {
        private readonly ComponentCatalog _catalog;
        private readonly PartCatalog _partCatalog;
        private readonly string? _classKey;
        private readonly IRelayCommand<PartRowViewModel>? _replacePartCommand;
        private readonly DifficultyEconomy _economy;

        public PilotAircraft Aircraft { get; }
        public EngineClass EngineClass { get; }
        public ObservableCollection<ComponentRowViewModel> Components { get; } = new();

        private DateTime? _careerClock;
        public DateTime CareerClock
        {
            get => _careerClock ?? Aircraft.Pilot?.CareerClockUtc ?? DateTime.UtcNow;
            set
            {
                if (_careerClock != value)
                {
                    _careerClock = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsInsured));
                    OnPropertyChanged(nameof(InsuranceStatusText));
                    OnPropertyChanged(nameof(InsuranceBrush));
                    OnPropertyChanged(nameof(InsuranceExpiryText));
                    OnPropertyChanged(nameof(InsuranceButtonText));
                    OnPropertyChanged(nameof(InsurancePremiumPrice));
                    UpdateReplacementPlans();
                }
            }
        }

        public PilotAircraftWrapper(
            PilotAircraft aircraft,
            ComponentCatalog catalog,
            IRelayCommand<ComponentRowViewModel> repairCommand,
            DateTime? careerClock = null,
            PartCatalog? partCatalog = null,
            string? classKey = null,
            bool hasConvertibleCabin = false,
            IRelayCommand<PartRowViewModel>? replacePartCommand = null,
            DifficultyMode difficulty = DifficultyMode.Medium)
        {
            Aircraft = aircraft;
            _catalog = catalog;
            _careerClock = careerClock;
            _partCatalog = partCatalog ?? PartCatalog.LoadDefault();
            _classKey = classKey;
            _replacePartCommand = replacePartCommand;
            HasConvertibleCabin = (aircraft.AircraftType?.HasConvertibleCabin == true) || hasConvertibleCabin;
            EngineClass = catalog.ResolveEngineClass(aircraft.AircraftType);
            _economy = DifficultyEconomy.For(difficulty);

            var order = catalog.Components.Select(d => d.Kind).ToList();
            foreach (var c in aircraft.Components.OrderBy(c => order.IndexOf(c.Kind)))
            {
                var def = catalog.Components.FirstOrDefault(d => d.Kind == c.Kind);
                if (def == null || !def.AppliesTo.Contains(EngineClass)) continue;
                Components.Add(new ComponentRowViewModel(c, def, catalog, EngineClass, PurchasePrice, aircraft.CurrentAirframeHours, repairCommand, aircraft, _economy));
            }

            _targetFuel = aircraft.FuelAmount;
            UpdateReplacementPlans();
        }

        // --- Identificação ---
        public string Prefix => Aircraft.Prefix;
        public string Model => Aircraft.AircraftType?.Model ?? "N/A";
        public double PurchasePrice => Aircraft.AircraftType?.PurchasePrice ?? 0;
        public double MTOW => AircraftWeightSpecs.GetMtow(Aircraft.AircraftType?.Model, Aircraft.AircraftType);
        public double FuelCapacity => Aircraft.AircraftType?.FuelTankGallons ?? 0;
        public string FuelType => Aircraft.AircraftType?.FuelType ?? "AVGAS";
        public int MaxPax => Aircraft.AircraftType?.MaxPax ?? 0;
        public int MaxPayloadLbs => Aircraft.AircraftType?.MaxPayloadLbs ?? 0;
        public int CruiseSpeedKts => Aircraft.AircraftType?.CruiseSpeedKts ?? 0;
        public int RangeNM => Aircraft.AircraftType?.RangeNM ?? 0;
        public string Location => string.IsNullOrEmpty(Aircraft.CurrentLocationICAO) ? "—" : Aircraft.CurrentLocationICAO;
        public double AirframeHours => Aircraft.CurrentAirframeHours;
        public int LandingsCount => Aircraft.LandingsCount;
        public bool IsPiston => EngineClass == EngineClass.Piston;
        public string EngineClassText => EngineClass switch
        {
            EngineClass.Piston => "PISTÃO",
            EngineClass.Turboprop => "TURBOÉLICE",
            _ => "JATO"
        };
        public string Configuration => string.IsNullOrEmpty(Aircraft.Configuration) ? "Passageiro" : Aircraft.Configuration;
        public bool IsCargoConfig => CabinConversion.IsCargoConfiguration(Configuration);
        public bool HasConvertibleCabin { get; }
        public CabinCapacity EffectiveCapacity => CabinConversion.GetEffectiveCapacity(MaxPax, MaxPayloadLbs, Configuration, HasConvertibleCabin);
        public string ConfigurationDisplayText => !HasConvertibleCabin
            ? $"Passageiro: até {EffectiveCapacity.MaxPax} PAX e {EffectiveCapacity.MaxCargoLbs:N0} lb"
            : (IsCargoConfig
                ? $"Carga: até {EffectiveCapacity.MaxCargoLbs:N0} lb"
                : $"Passageiro: até {EffectiveCapacity.MaxPax} PAX e {EffectiveCapacity.MaxCargoLbs:N0} lb bagagem");

        [ObservableProperty] private bool _isActive;

        // --- Saúde geral e status ---
        public bool HasPartsDriven => PartComponentBridge.HasParts(Aircraft);

        private AircraftHealthSummary Summary =>
            AircraftHealthSummary.Compute(Components.Select(r => r.Component), _catalog, EngineClass, Aircraft.CurrentAirframeHours);

        public string AirworthinessText
        {
            get
            {
                if (HasPartsDriven)
                {
                    return PartsAirworthinessState switch
                    {
                        PartDisplayState.Broken => "Peça quebrada",
                        PartDisplayState.Red => "Risco de pane",
                        PartDisplayState.Yellow => "Atenção",
                        PartDisplayState.Green => "Em ordem",
                        PartDisplayState.Worn => "Em ordem",
                        PartDisplayState.Uncertain => "Incerta — faça uma inspeção",
                        _ => "Em ordem"
                    };
                }
                return Summary.DisplayedAirworthiness.ToString();
            }
        }

        public string ConditionText
        {
            get
            {
                if (HasPartsDriven)
                {
                    return PartsConditionSummaryText;
                }
                return Summary.DisplayedCondition.ToString();
            }
        }

        public double AirworthinessMid => Summary.DisplayedAirworthiness.Mid;

        public SolidColorBrush AirworthinessBrush
        {
            get
            {
                if (HasPartsDriven)
                {
                    return PartsAirworthinessBrush;
                }
                return HangarPalette.ForHealth(AirworthinessMid);
            }
        }

        public GridLength AirworthinessFill => new(Math.Max(0, AirworthinessMid), GridUnitType.Star);
        public GridLength AirworthinessEmpty => new(Math.Max(0, 100 - AirworthinessMid), GridUnitType.Star);

        // --- Suporte a peças no topo ---
        private PartDisplayState _partsAirworthinessState = PartDisplayState.Green;
        public PartDisplayState PartsAirworthinessState => _partsAirworthinessState;

        public Geometry PartsAirworthinessIconGeometry => HangarIcons.ForDisplayState(_partsAirworthinessState);

        public SolidColorBrush PartsAirworthinessBrush => HangarPalette.ForDisplayState(_partsAirworthinessState);

        private string _partsConditionSummaryText = string.Empty;
        public string PartsConditionSummaryText => _partsConditionSummaryText;

        private void RecalculateTopBoxes()
        {
            if (!HasPartsDriven)
            {
                _partsAirworthinessState = PartDisplayState.Green;
                _partsConditionSummaryText = string.Empty;
                return;
            }

            var allPartDisplays = Components
                .SelectMany(c => c.Parts)
                .Select(p => p.Row.Display)
                .ToList();

            var (_, overallCounts) = PartHangarView.Summarize(allPartDisplays);

            _partsConditionSummaryText = HangarPalette.CountsText(overallCounts);

            var criticalDisplays = Components
                .Where(c => c.IsCritical)
                .SelectMany(c => c.Parts)
                .Select(p => p.Row.Display)
                .ToList();

            var (worstCritical, _) = PartHangarView.Summarize(criticalDisplays);
            _partsAirworthinessState = worstCritical;

            OnPropertyChanged(nameof(PartsAirworthinessState));
            OnPropertyChanged(nameof(PartsAirworthinessIconGeometry));
            OnPropertyChanged(nameof(PartsAirworthinessBrush));
            OnPropertyChanged(nameof(PartsConditionSummaryText));
            OnPropertyChanged(nameof(AirworthinessText));
            OnPropertyChanged(nameof(ConditionText));
            OnPropertyChanged(nameof(AirworthinessBrush));
        }

        private AircraftStatus Status
        {
            get
            {
                var status = AircraftStatusEvaluator.Evaluate(Aircraft, Components.Select(r => r.Component).ToList(), _catalog, EngineClass);
                if (status.Level is AircraftStatusLevel.Destroyed or AircraftStatusLevel.Interdicted) return status;

                // Peças quebradas que bloqueiam voo: interdita antes da checagem de panes
                if (PartComponentBridge.HasParts(Aircraft))
                {
                    var groundingParts = PartAirworthiness.GroundingParts(Aircraft.Parts, _partCatalog);
                    if (groundingParts.Count > 0)
                    {
                        int engineCount = Aircraft.Parts.Select(p => p.EngineIndex).DefaultIfEmpty(0).Max();
                        string nomes = string.Join(", ", groundingParts.Select(p =>
                        {
                            var def = _partCatalog.Get(p.PartId);
                            return def != null ? PartHangarView.DisplayName(p, def, engineCount) : p.PartId;
                        }));
                        return new AircraftStatus(AircraftStatusLevel.Interdicted, "INTERDITADA (PEÇA)",
                            $"Peça quebrada: {nomes}. Troque a peça no Hangar.");
                    }
                }

                // Panes persistentes abertas: NO-GO interdita; diferida rebaixa "pronta" para atenção
                var grounding = OpenDefects.Where(d => d.IsGrounding).ToList();
                if (grounding.Count > 0)
                    return new AircraftStatus(AircraftStatusLevel.Interdicted, "GROUNDED (PANE)",
                        "Item inoperante: " + string.Join(", ", grounding.Select(d => d.Title)) + ". Repare a pane na oficina.");

                if (IsInWorkshop)
                    return new AircraftStatus(AircraftStatusLevel.Attention, "OFICINA",
                        $"Em manutenção ({Aircraft.WorkshopDescription ?? "serviço"}). Libera em {WorkshopRemainingFormatted}.");

                if (OpenDefects.Count > 0 && status.Level is AircraftStatusLevel.Ready or AircraftStatusLevel.MaintenanceDue)
                    return new AircraftStatus(AircraftStatusLevel.Attention, "PANE DIFERIDA",
                        "Voo permitido com: " + string.Join(", ", OpenDefects.Select(d => d.Title)) + ".");
                return status;
            }
        }

        // --- Panes persistentes (discrepâncias abertas, addon ou simulador) ---
        public ObservableCollection<AircraftDefect> OpenDefects { get; } = new();
        public ObservableCollection<DefectRowViewModel> DefectRows { get; } = new();
        public bool HasOpenDefects => OpenDefects.Count > 0;
        public bool HasGroundingDefects => OpenDefects.Any(d => d.IsGrounding);
        public double DefectsRepairTotal
        {
            get
            {
                if (OpenDefects.Count == 0) return 0.0;
                if (!PartComponentBridge.HasParts(Aircraft))
                {
                    return OpenDefects.Sum(d => d.RepairCost);
                }

                var quote = DefectPartLink.Quote(OpenDefects, Aircraft.Parts);
                double partsCost = 0.0;
                if (quote.PartsToReplace.Count > 0)
                {
                    var plan = PartReplacementPlanner.Plan(quote.PartsToReplace, _partCatalog, _classKey, CareerClock, bands: _catalog.PartBands);
                    partsCost = plan.TotalCost;
                }
                return Math.Round(partsCost + quote.DefectsCost, 2);
            }
        }
        public string RepairAllDefectsText => $"Reparar todas as panes · {_economy.ApplyCost(DefectsRepairTotal):C0}";

        public void SetOpenDefects(IEnumerable<AircraftDefect> defects)
        {
            OpenDefects.Clear();
            DefectRows.Clear();
            foreach (var d in defects)
            {
                OpenDefects.Add(d);
                DefectRows.Add(new DefectRowViewModel(d, _economy));
            }
            OnPropertyChanged(nameof(HasOpenDefects));
            OnPropertyChanged(nameof(HasGroundingDefects));
            OnPropertyChanged(nameof(DefectsRepairTotal));
            OnPropertyChanged(nameof(RepairAllDefectsText));
            OnPropertyChanged(nameof(TotalRepairCost));
            OnPropertyChanged(nameof(HasRepairs));
            OnPropertyChanged(nameof(RepairAllText));
            OnPropertyChanged(nameof(StatusLevel));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDetail));
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(ShowWorkshopBanner));
        }

        public AircraftStatusLevel StatusLevel => Status.Level;
        public string StatusText => Status.Text;
        public string StatusDetail => Status.Detail;
        public SolidColorBrush StatusBrush => HangarPalette.ForStatus(Status.Level);
        public bool IsCrashed => Status.Level == AircraftStatusLevel.Destroyed;
        public bool CanService => !IsCrashed;

        public double TotalRepairCost
        {
            get
            {
                if (!PartComponentBridge.HasParts(Aircraft))
                {
                    return Components.Sum(c => c.RepairCost);
                }

                var componentPlanItems = Components
                    .Where(c => c.CanRepair && c.ReplacementPlan != null)
                    .SelectMany(c => c.ReplacementPlan!.Items)
                    .ToList();
                double componentCost = componentPlanItems.Sum(i => i.PartPrice + i.LaborCost);

                var quote = DefectPartLink.Quote(OpenDefects, Aircraft.Parts);
                var plannedPartIds = componentPlanItems.Select(i => i.Part.Id).ToHashSet();
                var extraParts = quote.PartsToReplace.Where(p => !plannedPartIds.Contains(p.Id)).ToList();

                double extraPlanCost = 0.0;
                if (extraParts.Count > 0)
                {
                    var extraPlan = PartReplacementPlanner.Plan(extraParts, _partCatalog, _classKey, CareerClock, bands: _catalog.PartBands);
                    extraPlanCost = extraPlan.TotalCost;
                }

                return Math.Round(componentCost + extraPlanCost + quote.DefectsCost, 2);
            }
        }
        public bool HasRepairs => TotalRepairCost > 0;
        public string RepairAllText => HasRepairs ? $"Reparar tudo · {_economy.ApplyCost(TotalRepairCost):C0}" : "Nada a reparar";

        // --- Oficina em tempo real (PARADA-REAL) ---
        public bool IsInWorkshop => WorkshopDowntime.IsInWorkshop(Aircraft.WorkshopUntilUtc, DateTime.UtcNow);
        public bool ShowWorkshopBanner => IsInWorkshop && Status.Text != "OFICINA";
        public TimeSpan WorkshopRemaining => WorkshopDowntime.Remaining(Aircraft.WorkshopUntilUtc, DateTime.UtcNow);
        public string WorkshopRemainingFormatted
        {
            get
            {
                var rem = WorkshopRemaining;
                return $"{(int)rem.TotalMinutes:D2}:{rem.Seconds:D2}";
            }
        }
        public string WorkshopBannerText => $"Na oficina — libera em {WorkshopRemainingFormatted} ({Aircraft.WorkshopDescription ?? "serviço"})";

        public void RefreshWorkshop()
        {
            OnPropertyChanged(nameof(IsInWorkshop));
            OnPropertyChanged(nameof(ShowWorkshopBanner));
            OnPropertyChanged(nameof(WorkshopRemaining));
            OnPropertyChanged(nameof(WorkshopRemainingFormatted));
            OnPropertyChanged(nameof(WorkshopBannerText));
            OnPropertyChanged(nameof(StatusLevel));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDetail));
            OnPropertyChanged(nameof(StatusBrush));
        }

        // --- Prazos reais (× 4) ---
        public string OilChangeLabel => $"Troca de óleo — a cada {MaintenancePricing.RealHours(MaintenancePricing.OilChangeIntervalHours):F0} h na vida real";
        public string AnnualInspectionLabel => $"Inspeção anual — a cada {MaintenancePricing.RealHours(MaintenancePricing.AnnualInspectionIntervalHours):F0} h na vida real (obrigatória)";
        public string IgnitionRevisionLabel => $"Revisão de ignição — a cada {MaintenancePricing.RealHours(MaintenancePricing.IgnitionRevisionIntervalHours):F0} h na vida real (magnetos e velas)";
        public string TboLabel => $"TBO {MaintenancePricing.RealHours(MaintenancePricing.TboHoursFor(EngineClass)):N0} h (vida real)";

        // --- Inspeções e TBO ---
        public double EngineTboRemainingHours => Math.Round(Aircraft.EngineTboRemainingHours, 1);
        public double HoursUntilNext50hInspection => Math.Round(Aircraft.HoursUntilNext50hInspection, 1);
        public double HoursUntilNext100hInspection => Math.Round(Aircraft.HoursUntilNext100hInspection, 1);
        public double HoursUntilNext500hInspection => Math.Round(Aircraft.HoursUntilNext500hInspection, 1);
        public bool IsInterdicted => Aircraft.IsInterdicted;
        public string InterdictionReason => Aircraft.InterdictionReason;

        public SolidColorBrush Brush50h => DueBrush(HoursUntilNext50hInspection, 1.2);
        public SolidColorBrush Brush100h => DueBrush(HoursUntilNext100hInspection, 2.5);
        public SolidColorBrush Brush500h => DueBrush(HoursUntilNext500hInspection, 12.5);
        public SolidColorBrush BrushTbo => DueBrush(EngineTboRemainingHours, 12.5);

        private static SolidColorBrush DueBrush(double remaining, double warn) =>
            remaining <= 0 ? HangarPalette.Red : remaining <= warn ? HangarPalette.Amber : HangarPalette.Green;

        public double EngineLifeHours => _catalog.Get(ComponentKind.Engine).LifeHoursFor(EngineClass);

        public double Cost50hInspection => MaintenancePricing.Cost50hInspection;
        public double Cost100hInspection => MaintenancePricing.Cost100hInspection;
        public double Cost500hInspection => MaintenancePricing.Cost500hInspection;
        public double CostOverhaul => MaintenancePricing.CalculateOverhaulCost(PurchasePrice);
        public double CostToggleConfiguration => CabinConversion.GetCost(MTOW);
        public double DowntimeToggleConfiguration => CabinConversion.GetDowntimeHours(MTOW);
        public string TargetConfigurationName => IsCargoConfig ? "Passageiro" : "Carga";

        public string Text50h => $"Troca de óleo · {_economy.ApplyCost(Cost50hInspection):C0}";
        public string Text100h => $"Inspeção anual · {_economy.ApplyCost(Cost100hInspection):C0}";
        public string Text500h => $"Revisão de ignição · {_economy.ApplyCost(Cost500hInspection):C0}";
        public string TextOverhaul => $"Retífica · {_economy.ApplyCost(CostOverhaul):C0}";
        public string TextToggleConfiguration => $"Converter para {TargetConfigurationName} · {_economy.ApplyCost(CostToggleConfiguration):C0} · {DowntimeToggleConfiguration:F0} h";
        public string OverhaulTooltip => $"Restaura o motor como novo e o TBO para {EngineLifeHours:N0} h";

        // --- Abastecimento ---
        [ObservableProperty] private double _targetFuel;
        public double FuelAmount => Aircraft.FuelAmount;
        public double FuelPricePerGallon => FuelPriceService.GetCurrentPrice(FuelType);
        public double RefuelGallons => Math.Max(0, TargetFuel - Aircraft.FuelAmount);
        public double RefuelCost => RefuelGallons * FuelPricePerGallon;
        public bool CanRefuel => RefuelCost > 0.01 && !IsCrashed;
        public string RefuelButtonText => CanRefuel ? $"Abastecer {RefuelGallons:F1} gal · {_economy.ApplyCost(RefuelCost):C0}" : "Ajuste o volume para abastecer";

        partial void OnTargetFuelChanged(double value)
        {
            if (value < Aircraft.FuelAmount) { TargetFuel = Aircraft.FuelAmount; return; }
            OnPropertyChanged(nameof(RefuelGallons));
            OnPropertyChanged(nameof(RefuelCost));
            OnPropertyChanged(nameof(CanRefuel));
            OnPropertyChanged(nameof(RefuelButtonText));
        }

        // --- Seguro ---
        public double InsurancePremium => InsuranceService.CalculateMonthlyPremium(PurchasePrice);
        public double InsurancePremiumPrice => _economy.ApplyCost(InsurancePremium);
        public double InsuranceDeductible => InsuranceService.CalculateDeductible(PurchasePrice);
        public double InsurancePayout => InsuranceService.CalculatePayout(PurchasePrice);
        public bool IsInsured => InsuranceService.IsInsured(Aircraft, CareerClock);
        public string InsuranceStatusText => IsInsured ? "ATIVO" : "INATIVO";
        public SolidColorBrush InsuranceBrush => IsInsured ? HangarPalette.Green : HangarPalette.Red;
        public string InsuranceExpiryText => IsInsured
            ? $"vence em {Aircraft.InsuranceExpirationDate:dd/MM/yyyy} ({(Aircraft.InsuranceExpirationDate!.Value - CareerClock).TotalDays:F0} dias)"
            : "sem cobertura";
        public string InsuranceButtonText => IsInsured ? $"Renovar +30 dias · {_economy.ApplyCost(InsurancePremium):C0}" : $"Contratar 30 dias · {_economy.ApplyCost(InsurancePremium):C0}";

        /// <summary>Atualiza as peças com atenção visíveis em cada card de componente.</summary>
        public void SetFlaggedParts(IEnumerable<AircraftPart> parts, PartCatalog partCatalog)
        {
            var partsList = parts?.ToList() ?? new List<AircraftPart>();
            int maxEngineCount = partsList.Select(p => p.EngineIndex).DefaultIfEmpty(0).Max();
            var mapping = PartComponentMapping.CreateDefault();

            var flaggedByKind = new Dictionary<ComponentKind, List<FlaggedPartViewModel>>();

            foreach (var part in partsList)
            {
                if (!PartInspectionFlags.IsVisible(part, Aircraft.CurrentAirframeHours))
                    continue;

                var def = partCatalog.Get(part.PartId);
                if (def == null) continue;

                var kind = mapping.ResolveComponentKind(def.Group);
                if (!kind.HasValue) continue;

                string name = PartHangarView.DisplayName(part, def, maxEngineCount);

                var eval = PartInspectionFlags.Evaluate(part, def, CareerClock, _catalog.PartBands);
                string reason = !string.IsNullOrEmpty(eval.Reason)
                    ? eval.Reason
                    : (part.Flag == PartFlag.Broken ? "Quebrada" : "Atenção recomendada");

                var item = new FlaggedPartViewModel(name, part.Flag, reason);

                if (!flaggedByKind.TryGetValue(kind.Value, out var list))
                {
                    list = new List<FlaggedPartViewModel>();
                    flaggedByKind[kind.Value] = list;
                }
                list.Add(item);
            }

            foreach (var row in Components)
            {
                if (flaggedByKind.TryGetValue(row.Kind, out var list))
                {
                    row.SetFlaggedParts(list);
                }
                else
                {
                    row.SetFlaggedParts(Array.Empty<FlaggedPartViewModel>());
                }
            }

            UpdateReplacementPlans();
        }

        /// <summary>
        /// Atualiza os planos de substituição física de peças para cada linha de componente.
        /// </summary>
        public void UpdateReplacementPlans()
        {
            bool hasParts = PartComponentBridge.HasParts(Aircraft);
            if (!hasParts)
            {
                foreach (var row in Components)
                {
                    row.SetReplacementPlan(null);
                    row.SetParts(Array.Empty<PartRowViewModel>());
                }
                RecalculateTopBoxes();
                return;
            }

            var mapping = PartComponentMapping.CreateDefault();
            var partsByKind = new Dictionary<ComponentKind, List<AircraftPart>>();
            foreach (var part in Aircraft.Parts)
            {
                var def = _partCatalog.Get(part.PartId);
                if (def == null) continue;
                var kind = mapping.ResolveComponentKind(def.Group);
                if (kind.HasValue)
                {
                    if (!partsByKind.TryGetValue(kind.Value, out var list))
                    {
                        list = new List<AircraftPart>();
                        partsByKind[kind.Value] = list;
                    }
                    list.Add(part);
                }
            }

            int engineCount = Aircraft.Parts.Select(p => p.EngineIndex).DefaultIfEmpty(0).Max();
            double growthPerHour = _catalog.Visibility?.GrowthPerHour ?? 0.4;

            foreach (var row in Components)
            {
                if (partsByKind.TryGetValue(row.Kind, out var groupParts))
                {
                    var recommended = PartReplacementPlanner.SelectRecommendedKnown(groupParts, _catalog.PartBands);
                    var plan = PartReplacementPlanner.Plan(recommended, _partCatalog, _classKey, CareerClock, bands: _catalog.PartBands);
                    row.SetReplacementPlan(plan);

                    var hangarRows = PartHangarView.BuildList(groupParts, _partCatalog, engineCount, Aircraft.CurrentAirframeHours, CareerClock, _catalog.PartBands, growthPerHour);
                    var partRowViewModels = new List<PartRowViewModel>();
                    foreach (var hRow in hangarRows)
                    {
                        var singlePlan = PartReplacementPlanner.Plan(new[] { hRow.Part }, _partCatalog, _classKey, CareerClock, bands: _catalog.PartBands);
                        string replaceText = $"Trocar · {_economy.ApplyCost(singlePlan.TotalCost):C0}";
                        partRowViewModels.Add(new PartRowViewModel(hRow, replaceText, _replacePartCommand));
                    }
                    row.SetParts(partRowViewModels);
                }
                else
                {
                    row.SetReplacementPlan(new PartReplacementPlan(Array.Empty<PartReplacementItem>(), 0.0, 0.0));
                    row.SetParts(Array.Empty<PartRowViewModel>());
                }
            }

            RecalculateTopBoxes();

            OnPropertyChanged(nameof(TotalRepairCost));
            OnPropertyChanged(nameof(HasRepairs));
            OnPropertyChanged(nameof(RepairAllText));
        }

        /// <summary>Recalcula tudo após um serviço ou recarga.</summary>
        public void Refresh()
        {
            UpdateReplacementPlans();
            foreach (var row in Components) row.Refresh(Aircraft.CurrentAirframeHours, Aircraft);
            if (TargetFuel < Aircraft.FuelAmount) TargetFuel = Aircraft.FuelAmount;
            RecalculateTopBoxes();
            OnPropertyChanged(string.Empty);
        }
    }
}
