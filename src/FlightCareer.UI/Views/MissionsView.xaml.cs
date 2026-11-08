using System.Windows.Controls;
using System.Windows.Input;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Views
{
    public partial class MissionsView : UserControl
    {
        public MissionsView()
        {
            InitializeComponent();
        }

        private void MissionsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (DataContext is MainViewModel vm && vm.CanAcceptMission)
                {
                    vm.AcceptSelectedMissionsCommand.Execute(null);
                }
            }
        }
    }
}
