using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Entities;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.UI.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>
    /// Hangar: frota, barras por componente, reparos, inspeções, walkaround, abastecimento e seguro.
    /// Cada operação usa um contexto novo do banco (dados sempre atuais, sem entidades presas em cache)
    /// e grava os componentes e as colunas antigas sincronizadas (mercado/HUD/FlightService ainda as leem).
    /// </summary>
    public partial class HangarViewModel : ObservableObject
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ComponentCatalog _catalog;
        private readonly IRandomSource _random = new SystemRandomSource();
        private Guid _pilotId;

        [ObservableProperty] private ObservableCollection<PilotAircraftWrapper> _fleet = new();
        [ObservableProperty] private PilotAircraftWrapper? _selectedAircraft;
        [ObservableProperty] private bool _isMonitoring;

        public string FleetCountText => Fleet.Count == 1 ? "1 aeronave" : $"{Fleet.Count} aeronaves";

        public HangarViewModel(IDbContextFactory<AppDbContext> contextFactory, ComponentCatalog catalog)
        {
            _contextFactory = contextFactory;
            _catalog = catalog;
        }

        public async Task InitializeAsync(Guid pilotId)
        {
            _pilotId = pilotId;
            await LoadFleetAsync();
        }

        public async Task LoadFleetAsync(Guid? keepSelection = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var pilot = await context.Pilots
                .Include(p => p.Hangar).ThenInclude(a => a.AircraftType)
                .Include(p => p.Hangar).ThenInclude(a => a.Components)
                .FirstOrDefaultAsync(p => p.Id == _pilotId);
            if (pilot == null) return;

            // Aeronaves novas (compra / criação de piloto) recebem componentes; danos gravados fora do voo
            // (ex.: crash no pouso pelo FlightService) são aplicados às barras.
            bool changed = false;
            foreach (var aircraft in pilot.Hangar)
            {
                var engineClass = _catalog.ResolveEngineClass(aircraft.AircraftType);
                var existing = aircraft.Components.Select(c => c.Kind).ToHashSet();
                var missing = LegacyComponentMigrator.CreateComponents(aircraft, engineClass, _catalog, _random)
                    .Where(c => !existing.Contains(c.Kind)).ToList();
                if (missing.Count > 0)
                {
                    context.AircraftComponents.AddRange(missing);
                    foreach (var m in missing) if (!aircraft.Components.Contains(m)) aircraft.Components.Add(m);
                    changed = true;
                }
                if (LegacyConditionBridge.ReconcileFromLegacy(aircraft, aircraft.Components, _random, _catalog.Visibility) > 0) changed = true;
                LegacyConditionBridge.WriteToLegacy(aircraft, aircraft.Components);
            }
            if (changed || context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();

            var repair = RepairComponentCommand;
            var wrapped = pilot.Hangar
                .OrderBy(a => a.Prefix)
                .Select(a => new PilotAircraftWrapper(a, _catalog, repair) { IsActive = a.Id == pilot.ActiveAircraftId })
                .ToList();

            // Panes persistentes abertas (gravadas pelo voo/vigia em outros contextos)
            var fleetIds = wrapped.Select(w => w.Aircraft.Id).ToList();
            var openDefects = await context.AircraftDefects.AsNoTracking()
                .Where(d => fleetIds.Contains(d.PilotAircraftId) && !d.IsResolved)
                .ToListAsync();
            foreach (var w in wrapped)
                w.SetOpenDefects(openDefects.Where(d => d.PilotAircraftId == w.Aircraft.Id).OrderBy(d => d.DetectedAtUtc));

            Fleet = new ObservableCollection<PilotAircraftWrapper>(wrapped);
            OnPropertyChanged(nameof(FleetCountText));
            var selectId = keepSelection ?? SelectedAircraft?.Aircraft.Id ?? pilot.ActiveAircraftId;
            SelectedAircraft = Fleet.FirstOrDefault(a => a.Aircraft.Id == selectId) ?? Fleet.FirstOrDefault();
        }

        // ------------------------------------------------------------------ Infraestrutura de serviços

        private bool CanOperate(bool allowCrashed = false)
        {
            if (SelectedAircraft == null) return false;
            if (IsMonitoring)
            {
                ThemedDialog.Info("Voo em andamento", "Serviços do hangar não podem ser feitos durante um voo.", DialogKind.Warning);
                return false;
            }
            if (!allowCrashed && SelectedAircraft.IsCrashed)
            {
                ThemedDialog.Info("Perda total", "Esta aeronave foi destruída. Acione o seguro ou descarte-a.", DialogKind.Danger);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Executa um serviço pago: confirma, confere saldo, aplica sobre a aeronave e componentes de um contexto novo,
        /// registra a transação, sincroniza as colunas antigas, salva e recarrega a frota.
        /// </summary>
        private async Task<bool> RunPaidServiceAsync(string title, string message, double cost, string transactionDescription,
            Action<PilotAircraft, List<AircraftComponent>> apply, string category = "Manutenção", DialogKind kind = DialogKind.Info, string? successMessage = null,
            Func<AppDbContext, Task>? applyInContext = null)
        {
            if (SelectedAircraft == null) return false;
            var aircraftId = SelectedAircraft.Aircraft.Id;

            if (!ThemedDialog.Confirm(title, $"{message}\n\nCusto: {cost:C2}", "Autorizar", kind)) return false;

            await using var context = await _contextFactory.CreateDbContextAsync();
            var pilot = await context.Pilots.FindAsync(_pilotId);
            var aircraft = await context.PilotAircrafts
                .Include(a => a.AircraftType)
                .Include(a => a.Components)
                .FirstOrDefaultAsync(a => a.Id == aircraftId);
            if (pilot == null || aircraft == null) return false;

            if (pilot.Balance < cost)
            {
                ThemedDialog.Info("Saldo insuficiente", $"Este serviço custa {cost:C2} e o saldo atual é {pilot.Balance:C2}.", DialogKind.Danger);
                return false;
            }

            pilot.Balance -= cost;
            apply(aircraft, aircraft.Components);
            if (applyInContext != null) await applyInContext(context);
            LegacyConditionBridge.WriteToLegacy(aircraft, aircraft.Components);

            if (cost > 0)
            {
                context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    PilotId = pilot.Id,
                    Date = DateTime.Now,
                    Category = category,
                    Description = $"{transactionDescription}: {aircraft.Prefix}",
                    Amount = cost,
                    Type = TransactionType.Expense
                });
            }

            await context.SaveChangesAsync();
            await LoadFleetAsync(aircraftId);
            await RefreshMainBalanceAsync();

            if (successMessage != null) ThemedDialog.Info(title, successMessage, DialogKind.Success);
            return true;
        }

        private static async Task RefreshMainBalanceAsync()
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel main)
                    await main.RefreshPilotBalanceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Hangar] Falha ao atualizar saldo: {ex.Message}");
            }
        }

        private double Hours(PilotAircraft a) => a.CurrentAirframeHours;
        private AircraftComponent? Find(List<AircraftComponent> list, ComponentKind kind) => list.FirstOrDefault(c => c.Kind == kind);

        private void RepairKind(PilotAircraft aircraft, List<AircraftComponent> components, ComponentKind kind)
        {
            var c = Find(components, kind);
            if (c != null) ComponentMaintenance.Repair(c, Hours(aircraft), _random, _catalog.Visibility);
        }

        /// <summary>Efeitos regulatórios de reparar certos componentes (contadores das inspeções e TBO).</summary>
        private void ApplyRegulatoryReset(PilotAircraft aircraft, ComponentKind kind)
        {
            var engineClass = _catalog.ResolveEngineClass(aircraft.AircraftType);
            switch (kind)
            {
                case ComponentKind.Engine:
                    aircraft.EngineTboRemainingHours = _catalog.Get(ComponentKind.Engine).LifeHoursFor(engineClass);
                    break;
                case ComponentKind.Ignition:
                    aircraft.HoursUntilNext500hInspection = 500.0;
                    break;
            }
            ClearInterdictionIfResolved(aircraft);
        }

        private static void ClearInterdictionIfResolved(PilotAircraft aircraft)
        {
            if (aircraft.IsInterdicted && aircraft.HoursUntilNext100hInspection > 0 && aircraft.EngineTboRemainingHours > 0)
            {
                aircraft.IsInterdicted = false;
                aircraft.InterdictionReason = string.Empty;
            }
        }

        // ------------------------------------------------------------------ Componentes

        [RelayCommand]
        private async Task RepairComponent(ComponentRowViewModel? row)
        {
            if (row == null || !CanOperate() || !row.CanRepair) return;
            var kind = row.Kind;
            string extra = kind == ComponentKind.Engine
                ? "\nO motor volta a 0 h desde o overhaul e o TBO é restaurado."
                : string.Empty;

            await RunPaidServiceAsync(
                row.ServiceName,
                $"{row.ServiceName} em {SelectedAircraft!.Prefix}: {row.Name.ToLowerInvariant()} de {row.RangeText} para 100%.{extra}",
                row.RepairCost,
                row.ServiceName.ToUpperInvariant(),
                (aircraft, components) =>
                {
                    RepairKind(aircraft, components, kind);
                    ApplyRegulatoryReset(aircraft, kind);
                },
                kind: kind == ComponentKind.Engine ? DialogKind.Warning : DialogKind.Info,
                successMessage: $"{row.Name} restaurado(a) a 100%.");
        }

        // ------------------------------------------------------------------ Panes persistentes

        [RelayCommand]
        private async Task RepairDefect(AircraftDefect? defect)
        {
            if (defect == null) return;
            await RepairDefectsAsync(new List<AircraftDefect> { defect });
        }

        [RelayCommand]
        private async Task RepairAllDefects()
        {
            if (SelectedAircraft == null || !SelectedAircraft.HasOpenDefects) return;
            await RepairDefectsAsync(SelectedAircraft.OpenDefects.ToList());
        }

        /// <summary>
        /// Repara panes persistentes: troca/conserto do item. A barra do componente não muda (o desgaste continua),
        /// a pane sai do simulador na próxima leitura do vigia (PersistentFailureService) e o NO-GO é liberado.
        /// </summary>
        private async Task RepairDefectsAsync(List<AircraftDefect> defects)
        {
            if (!CanOperate() || SelectedAircraft == null || defects.Count == 0) return;
            var ids = defects.Select(d => d.Id).ToHashSet();
            double total = defects.Sum(d => d.RepairCost);
            string list = string.Join("\n", defects.Select(d => $"• {d.Title} ({d.OriginLabel}, {d.SeverityLabel}): {d.RepairCost:C0}"));

            await RunPaidServiceAsync(
                defects.Count == 1 ? "Reparar pane" : "Reparar panes",
                $"Oficina em {SelectedAircraft.Prefix}:\n{list}",
                total,
                defects.Count == 1 ? $"REPARO DE PANE: {defects[0].Title}" : $"REPARO DE {defects.Count} PANES",
                (_, _) => { },
                kind: DialogKind.Warning,
                successMessage: defects.Count == 1 ? $"{defects[0].Title}: reparada." : "Panes reparadas.",
                applyInContext: async context =>
                {
                    var rows = await context.AircraftDefects.Where(d => ids.Contains(d.Id)).ToListAsync();
                    foreach (var d in rows)
                    {
                        d.IsResolved = true;
                        d.ResolvedAtUtc = DateTime.UtcNow;
                    }
                });
        }

        [RelayCommand]
        private async Task RepairAll()
        {
            if (!CanOperate() || SelectedAircraft == null || !SelectedAircraft.HasRepairs) return;
            var rows = SelectedAircraft.Components.Where(r => r.CanRepair).ToList();
            string list = string.Join("\n", rows.Select(r => $"• {r.Name}: {r.RangeText} → 100% ({r.RepairCost:C0})"));

            await RunPaidServiceAsync(
                "Reparar tudo",
                $"Serviços em {SelectedAircraft.Prefix}:\n{list}",
                SelectedAircraft.TotalRepairCost,
                "REPARO COMPLETO",
                (aircraft, components) =>
                {
                    foreach (var r in rows)
                    {
                        RepairKind(aircraft, components, r.Kind);
                        ApplyRegulatoryReset(aircraft, r.Kind);
                    }
                },
                successMessage: "Todos os componentes foram restaurados a 100%.");
        }

        [RelayCommand]
        private async Task Walkaround()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            var aircraftId = SelectedAircraft.Aircraft.Id;

            await using var context = await _contextFactory.CreateDbContextAsync();
            var aircraft = await context.PilotAircrafts.Include(a => a.Components).FirstOrDefaultAsync(a => a.Id == aircraftId);
            if (aircraft == null) return;

            int narrowed = ComponentMaintenance.Walkaround(aircraft.Components, _catalog, aircraft.CurrentAirframeHours, _random);
            await context.SaveChangesAsync();
            await LoadFleetAsync(aircraftId);

            ThemedDialog.Info("Walkaround",
                narrowed > 0
                    ? $"Inspeção visual concluída: {narrowed} componente(s) estimados com mais precisão (pneus, freios, trem, óleo e célula). Motor e sistemas só são avaliados nas inspeções de oficina."
                    : "Nada novo a observar: os componentes visíveis por fora já estão com estimativa recente.",
                DialogKind.Success);
        }

        // ------------------------------------------------------------------ Inspeções e TBO

        [RelayCommand]
        private async Task Perform50hInspection()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            await RunPaidServiceAsync(
                "Inspeção de 50 horas",
                "Troca de óleo e filtro, inspeção de limalhas e verificação do motor. O óleo volta a 100% e o motor tem a saúde medida com exatidão.",
                SelectedAircraft.Cost50hInspection,
                "INSPEÇÃO 50H",
                (aircraft, components) =>
                {
                    RepairKind(aircraft, components, ComponentKind.Oil);
                    ComponentMaintenance.Inspect(components.Where(c => c.Kind is ComponentKind.Engine or ComponentKind.Oil), Hours(aircraft), _random, _catalog.Visibility);
                    aircraft.HoursUntilNext50hInspection = 50.0;
                    ClearInterdictionIfResolved(aircraft);
                },
                successMessage: "Inspeção de 50 horas concluída.");
        }

        [RelayCommand]
        private async Task Perform100hInspection()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            await RunPaidServiceAsync(
                "Inspeção de 100 horas",
                "Inspeção regulatória completa: todos os componentes têm a saúde medida com exatidão, o óleo é trocado e os contadores de 100 h e 50 h são zerados. Reparos encontrados são orçados à parte.",
                SelectedAircraft.Cost100hInspection,
                "INSPEÇÃO REGULATÓRIA 100H",
                (aircraft, components) =>
                {
                    RepairKind(aircraft, components, ComponentKind.Oil);
                    ComponentMaintenance.Inspect(components, Hours(aircraft), _random, _catalog.Visibility);
                    aircraft.HoursUntilNext100hInspection = 100.0;
                    aircraft.HoursUntilNext50hInspection = 50.0;
                    ClearInterdictionIfResolved(aircraft);
                },
                successMessage: "Inspeção de 100 horas concluída. Veja as barras: agora todos os valores são exatos.");
        }

        [RelayCommand]
        private async Task Perform500hInspection()
        {
            if (!CanOperate() || SelectedAircraft == null || !SelectedAircraft.IsPiston) return;
            await RunPaidServiceAsync(
                "Revisão de 500 horas",
                "Revisão dos magnetos, platinados e troca de velas. A ignição volta a 100%.",
                SelectedAircraft.Cost500hInspection,
                "REVISÃO 500H / MAGNETOS",
                (aircraft, components) =>
                {
                    RepairKind(aircraft, components, ComponentKind.Ignition);
                    ApplyRegulatoryReset(aircraft, ComponentKind.Ignition);
                },
                successMessage: "Revisão de 500 horas concluída.");
        }

        [RelayCommand]
        private async Task PerformOverhaul()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            await RunPaidServiceAsync(
                "Retífica geral do motor",
                $"Overhaul completo: motor{(SelectedAircraft.IsPiston ? "" : " e seção quente")} a 100%, óleo trocado e TBO restaurado para {SelectedAircraft.EngineLifeHours:N0} h.",
                SelectedAircraft.CostOverhaul,
                "RETÍFICA GERAL / OVERHAUL TBO",
                (aircraft, components) =>
                {
                    RepairKind(aircraft, components, ComponentKind.Engine);
                    RepairKind(aircraft, components, ComponentKind.HotSection);
                    RepairKind(aircraft, components, ComponentKind.Oil);
                    ApplyRegulatoryReset(aircraft, ComponentKind.Engine);
                },
                kind: DialogKind.Warning,
                successMessage: "Overhaul concluído. Motor retificado.");
        }

        [RelayCommand]
        private async Task ToggleConfiguration()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            string current = SelectedAircraft.Configuration;
            string next = current.Contains("Carga") ? "Passageiro" : "Carga (Sem Assentos)";
            await RunPaidServiceAsync(
                "Conversão de cabine",
                $"Converter {SelectedAircraft.Prefix} de '{current}' para '{next}'.",
                SelectedAircraft.CostToggleConfiguration,
                $"CONVERSÃO CABINE ({next})",
                (aircraft, _) => aircraft.Configuration = next,
                successMessage: $"Cabine convertida para: {next}.");
        }

        // ------------------------------------------------------------------ Abastecimento e seguro

        [RelayCommand]
        private async Task Refuel()
        {
            if (!CanOperate() || SelectedAircraft == null || !SelectedAircraft.CanRefuel) return;
            double target = SelectedAircraft.TargetFuel;
            double gallons = SelectedAircraft.RefuelGallons;
            await RunPaidServiceAsync(
                "Abastecimento",
                $"Abastecer {gallons:F1} gal de {SelectedAircraft.FuelType} a {SelectedAircraft.FuelPricePerGallon:C2}/gal.",
                SelectedAircraft.RefuelCost,
                $"ABASTECIMENTO ({gallons:F1} GAL)",
                (aircraft, _) => aircraft.FuelAmount = target,
                category: "Combustível",
                successMessage: $"Tanques com {target:F1} gal.");
        }

        [RelayCommand]
        private async Task BuyInsurance()
        {
            if (!CanOperate() || SelectedAircraft == null) return;
            await RunPaidServiceAsync(
                "Seguro aeronáutico",
                $"Cobertura de 30 dias para {SelectedAircraft.Prefix}.\nFranquia: {SelectedAircraft.InsuranceDeductible:C2}\nCobertura (casco): {SelectedAircraft.InsurancePayout:C2}",
                SelectedAircraft.InsurancePremium,
                "CONTRATAÇÃO SEGURO",
                (aircraft, _) =>
                {
                    var baseDate = aircraft.InsuranceExpirationDate > DateTime.Now ? aircraft.InsuranceExpirationDate!.Value : DateTime.Now;
                    aircraft.InsuranceExpirationDate = baseDate.AddDays(30);
                },
                category: "Seguro",
                successMessage: "Seguro contratado.");
        }

        [RelayCommand]
        private async Task ClaimInsurance()
        {
            if (SelectedAircraft == null || !SelectedAircraft.IsCrashed || !CanOperate(allowCrashed: true)) return;
            if (!SelectedAircraft.IsInsured)
            {
                ThemedDialog.Info("Sem cobertura", "Esta aeronave não estava segurada no momento do acidente.", DialogKind.Danger);
                return;
            }

            double payout = SelectedAircraft.InsurancePayout;
            double deductible = SelectedAircraft.InsuranceDeductible;
            var aircraftId = SelectedAircraft.Aircraft.Id;
            string prefix = SelectedAircraft.Prefix;

            if (!ThemedDialog.Confirm("Acionar seguro",
                    $"Franquia: -{deductible:C2}\nCobertura: +{payout:C2}\n\nA aeronave será removida da frota.", "Acionar", DialogKind.Warning)) return;

            await using var context = await _contextFactory.CreateDbContextAsync();
            var pilot = await context.Pilots.FindAsync(_pilotId);
            if (pilot == null) return;
            if (pilot.Balance < deductible)
            {
                ThemedDialog.Info("Saldo insuficiente", $"Você precisa de {deductible:C2} para pagar a franquia.", DialogKind.Danger);
                return;
            }

            pilot.Balance += payout - deductible;
            context.FinancialTransactions.Add(new FinancialTransaction { Id = Guid.NewGuid(), PilotId = pilot.Id, Date = DateTime.Now, Category = "Seguro", Description = $"PAGTO FRANQUIA: {prefix}", Amount = deductible, Type = TransactionType.Expense });
            context.FinancialTransactions.Add(new FinancialTransaction { Id = Guid.NewGuid(), PilotId = pilot.Id, Date = DateTime.Now, Category = "Seguro", Description = $"RECEBIMENTO SEGURO (PERDA TOTAL): {prefix}", Amount = payout, Type = TransactionType.Income });

            var aircraft = await context.PilotAircrafts.FindAsync(aircraftId);
            if (aircraft != null) context.PilotAircrafts.Remove(aircraft);
            if (pilot.ActiveAircraftId == aircraftId) pilot.ActiveAircraftId = null;

            await context.SaveChangesAsync();
            SelectedAircraft = null;
            await LoadFleetAsync();
            await RefreshMainBalanceAsync();
            ThemedDialog.Info("Seguro processado", $"{payout - deductible:C2} creditados (cobertura menos franquia).", DialogKind.Success);
        }

        [RelayCommand]
        private async Task ScrapAircraft()
        {
            if (SelectedAircraft == null || !SelectedAircraft.IsCrashed || !CanOperate(allowCrashed: true)) return;
            var aircraftId = SelectedAircraft.Aircraft.Id;
            if (!ThemedDialog.Confirm("Descartar aeronave",
                    $"Remover {SelectedAircraft.Prefix} da frota permanentemente? Você não recebe nada por isso.", "Descartar", DialogKind.Danger)) return;

            await using var context = await _contextFactory.CreateDbContextAsync();
            var aircraft = await context.PilotAircrafts.FindAsync(aircraftId);
            if (aircraft != null) context.PilotAircrafts.Remove(aircraft);
            var pilot = await context.Pilots.FindAsync(_pilotId);
            if (pilot != null && pilot.ActiveAircraftId == aircraftId) pilot.ActiveAircraftId = null;
            await context.SaveChangesAsync();

            SelectedAircraft = null;
            await LoadFleetAsync();
        }

        // ------------------------------------------------------------------ Aeronave ativa

        [RelayCommand]
        private async Task SetActive()
        {
            if (SelectedAircraft == null) return;
            if (IsMonitoring)
            {
                ThemedDialog.Info("Voo em andamento", "Não é possível trocar a aeronave ativa durante um voo.", DialogKind.Warning);
                return;
            }

            var aircraftId = SelectedAircraft.Aircraft.Id;
            await using var context = await _contextFactory.CreateDbContextAsync();
            var pilot = await context.Pilots.FindAsync(_pilotId);
            var aircraft = await context.PilotAircrafts.FindAsync(aircraftId);
            if (pilot == null || aircraft == null) return;

            pilot.ActiveAircraftId = aircraftId;
            pilot.CurrentAirportICAO = aircraft.CurrentLocationICAO; // O piloto vai para onde a aeronave está
            await context.SaveChangesAsync();

            foreach (var w in Fleet) w.IsActive = w.Aircraft.Id == aircraftId;

            ThemedDialog.Info("Aeronave ativa",
                $"{SelectedAircraft.Model} ({SelectedAircraft.Prefix}) definida como ativa.\nSua localização agora é {pilot.CurrentAirportICAO}.",
                DialogKind.Success);

            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainVM)
            {
                await mainVM.InitializeForPilotAsync(pilot.Name);
            }
        }
    }
}
