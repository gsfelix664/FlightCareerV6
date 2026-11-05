using System.Windows;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Views
{
    public partial class CreatePilotWindow : Window
    {
        public CreatePilotWindow()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is CreatePilotViewModel vm)
                {
                    vm.RequestClose = () => {
                        this.DialogResult = true;
                        this.Close();
                    };
                }
            };
        }
    }
}
