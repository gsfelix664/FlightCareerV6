using System.Windows;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                try
                {
                    if (DataContext is LoginViewModel vm)
                    {
                        await vm.LoadProfilesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar perfis: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }
    }
}
