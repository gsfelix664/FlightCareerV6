using System;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Forçar cultura en-US para garantir que valores monetários apareçam como $ (USD)
            var culture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
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
            services.AddDbContext<AppDbContext>(options =>
                {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flightcareer.db");
                if (!System.IO.File.Exists(dbPath) && System.IO.File.Exists("flightcareer.db"))
                {
                    dbPath = System.IO.Path.GetFullPath("flightcareer.db");
                }
                options.UseSqlite($"Data Source={dbPath}");
            },
                ServiceLifetime.Singleton); // DbContext como Singleton para WPF

            services.AddSingleton<IPilotRepository, PilotRepository>();
            services.AddSingleton<IDataSeeder, DataSeeder>();
            services.AddSingleton<IMissionGenerator, MissionGenerator>();
            services.AddSingleton<FlightService>();

            services.AddSingleton<SimConnectService>();
            services.AddSingleton<Services.FlightMonitor>();

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
            services.AddSingleton<DebugViewModel>(s => new DebugViewModel(
                s.GetRequiredService<SimConnectService>(),
                s.GetRequiredService<AppDbContext>()));
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>(s => new MainWindow
            {
                DataContext = s.GetRequiredService<MainViewModel>()
            });

            services.AddTransient<DebugWindow>(s => new DebugWindow
            {
                DataContext = s.GetRequiredService<DebugViewModel>()
            });
        }
    }
}

