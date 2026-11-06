using System.Windows;

namespace FlightCareer.UI.Views
{
    public partial class FerryFlightWindow : Window
    {
        public FerryFlightWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
