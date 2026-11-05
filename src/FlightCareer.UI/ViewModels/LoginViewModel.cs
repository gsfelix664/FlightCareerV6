using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using FlightCareer.UI.Views;

namespace FlightCareer.UI.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IPilotRepository _pilotRepository;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<Pilot> _profiles = new();

        [ObservableProperty]
        private Pilot? _selectedProfile;

        public LoginViewModel(IPilotRepository pilotRepository, IServiceProvider serviceProvider)
        {
            _pilotRepository = pilotRepository;
            _serviceProvider = serviceProvider;
            LoadProfilesCommand = new AsyncRelayCommand(LoadProfilesAsync);
            DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync);
            NewPilotCommand = new RelayCommand(OpenCreatePilotWindow);
            LoadSelectedProfileCommand = new AsyncRelayCommand(LoadSelectedProfile);
        }

        public IAsyncRelayCommand LoadProfilesCommand { get; }
        public IAsyncRelayCommand DeleteProfileCommand { get; }
        public IRelayCommand NewPilotCommand { get; }
        public IAsyncRelayCommand LoadSelectedProfileCommand { get; }

        public async Task LoadProfilesAsync()
        {
            var pilots = await _pilotRepository.GetAllAsync();
            Profiles = new ObservableCollection<Pilot>(pilots);
        }

        private void OpenCreatePilotWindow()
        {
            var window = _serviceProvider.GetRequiredService<CreatePilotWindow>();
            if (window.ShowDialog() == true)
            {
                _ = LoadProfilesAsync();
            }
        }

        private async Task LoadSelectedProfile()
        {
            if (SelectedProfile == null) return;

            try
            {
                var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
                await mainVm.InitializeForPilotAsync(SelectedProfile.Name);

                // Como são Singletons, o MainWindow já terá este mesmo mainVm como DataContext
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                var oldWindow = Application.Current.MainWindow;
                
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                
                oldWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar perfil: {ex.Message}\n\n{ex.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteProfileAsync()
        {
            if (SelectedProfile == null) return;
            
            var result = MessageBox.Show($"Deseja apagar o perfil {SelectedProfile.Name}?", "Aviso", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _pilotRepository.DeleteAsync(SelectedProfile);
                await _pilotRepository.SaveChangesAsync();
                await LoadProfilesAsync();
            }
        }
    }
}
