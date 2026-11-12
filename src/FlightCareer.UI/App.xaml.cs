using System;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FlightCareer.Application.Interfaces;
using FlightCareer.Application.Services;
using FlightCareer.Infrastructure.Persistence;
using FlightCareer.Infrastructure.Repositories;
using FlightCareer.Infrastructure.Services;
using FlightCareer.Domain.Repositories;
using FlightCareer.Domain.Services;
using FlightCareer.UI.ViewModels;
using FlightCareer.UI.Views;
using FlightCareer.UI.Services;

namespace FlightCareer.UI
{
    public partial class App : System.Windows.Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Forçar cultura en-US para garantir que valores monetários apareçam como $ (USD) mundialmente
            var culture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            // Tratamento global de erros para capturar o 0xc000041d
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Erro de UI não tratado:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = (Exception)args.ExceptionObject;
                MessageBox.Show($"Erro de Domínio não tratado:\n\n{ex.Message}", "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Inicializar GMap.NET
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 1. Mostrar Splash
            var splash = new SplashWindow();
            splash.Show();

            try
            {
                // 2. Inicializar Banco e Carregar CSVs
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();

                    await dbContext.EnsureSchemaUpdatedAsync();

                    // Barras de saúde por componente: cria/migra os componentes das aeronaves do hangar
                    try
                    {
                        var provisioner = scope.ServiceProvider.GetRequiredService<FlightCareer.Infrastructure.Services.AircraftComponentProvisioner>();
                        await provisioner.EnsureComponentsAsync(dbContext);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Componentes] Falha ao provisionar componentes: {ex.Message}");
                    }

                    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
                    if (!seeder.IsSeeded())
                    {
                        await seeder.SeedAsync((status, progress) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                splash.StatusLabel.Text = status;
                                splash.ProgressBar.Value = progress * 100;
                            });
                        });
                    }

                    // Sincronização automática com cenários do MSFS 2024 (Little Navmap DB)
                    var msfsSync = scope.ServiceProvider.GetRequiredService<IMsfsScenerySyncService>();
                    if (await msfsSync.NeedsSyncAsync())
                    {
                        await msfsSync.SyncAirportsAsync((status, progress) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                splash.StatusLabel.Text = status;
                                splash.ProgressBar.Value = progress * 100;
                            });
                        });
                    }
                }

                // 3. Fechar Splash e abrir Login
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                MainWindow = loginWindow;
                splash.Close();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro fatal durante a inicialização:\n\n{ex.Message}\n\n{ex.StackTrace}", "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flightcareer.db");
                if (!System.IO.File.Exists(dbPath) && System.IO.File.Exists("flightcareer.db"))
                {
                    dbPath = System.IO.Path.GetFullPath("flightcareer.db");
                }
                options.UseSqlite($"Data Source={dbPath}");
            });

            // Registrar AppDbContext transient a partir da fábrica para compatibilidade de DI direta
            services.AddTransient<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddTransient<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            services.AddSingleton<IPilotRepository, PilotRepository>();
            services.AddSingleton<IDataSeeder, DataSeeder>();
            services.AddSingleton<IHubDataService, HubDataService>();
            services.AddSingleton<IMsfsScenerySyncService, MsfsScenerySyncService>();
            services.AddSingleton<IMissionGenerator, MissionGenerator>();
            services.AddSingleton<IMarketSimulationService, MarketSimulationService>();
            services.AddSingleton<FlightService>();
            services.AddSingleton(_ => FlightCareer.Domain.Components.ComponentCatalog.LoadDefault());
            services.AddSingleton<FlightCareer.Infrastructure.Services.AircraftComponentProvisioner>(sp =>
                new FlightCareer.Infrastructure.Services.AircraftComponentProvisioner(sp.GetRequiredService<FlightCareer.Domain.Components.ComponentCatalog>()));

            services.AddSingleton<SimConnectService>();
            services.AddSingleton<ISimConnectService>(sp => sp.GetRequiredService<SimConnectService>());
            services.AddSingleton<Services.FlightMonitor>(sp => new Services.FlightMonitor(
                sp.GetRequiredService<SimConnectService>(),
                sp.GetRequiredService<FlightService>(),
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<FlightCareer.Domain.Components.ComponentCatalog>()));
            services.AddSingleton<MetarService>(sp => new MetarService(
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>()));

            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<LoginWindow>(s => new LoginWindow
            {
                DataContext = s.GetRequiredService<LoginViewModel>()
            });

            services.AddTransient<CreatePilotViewModel>();
            services.AddTransient<CreatePilotWindow>(s => new CreatePilotWindow
            {
                DataContext = s.GetRequiredService<CreatePilotViewModel>()
            });

            services.AddSingleton<MarketViewModel>();
            services.AddSingleton<HangarViewModel>();
            services.AddSingleton<FlightLogsViewModel>();
            services.AddSingleton<DebugViewModel>(s => new DebugViewModel(
                s.GetRequiredService<SimConnectService>(),
                s.GetRequiredService<AppDbContext>(),
                s));
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>(s => new MainWindow
            {
                DataContext = s.GetRequiredService<MainViewModel>()
            });

            services.AddTransient<DebugWindow>(s => new DebugWindow
            {
                DataContext = s.GetRequiredService<DebugViewModel>()
            });

            services.AddSingleton<DebugOverlayViewModel>();
            services.AddTransient<DebugOverlayWindow>(s => new DebugOverlayWindow
            {
                DataContext = s.GetRequiredService<DebugOverlayViewModel>()
            });
        }
    }
}

