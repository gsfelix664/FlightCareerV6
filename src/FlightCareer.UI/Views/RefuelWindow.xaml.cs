using System.Windows;

namespace FlightCareer.UI.Views
{
    public partial class RefuelWindow : Window
    {
        public RefuelWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}