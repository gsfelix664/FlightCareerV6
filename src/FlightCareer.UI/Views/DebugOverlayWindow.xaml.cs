using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlightCareer.UI.Utils;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Views
{
    public partial class DebugOverlayWindow : Window
    {
        public DebugOverlayWindow()
        {
            InitializeComponent();
            DataContextChanged += DebugOverlayWindow_DataContextChanged;
        }

        private void DebugOverlayWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DebugOverlayViewModel vm)
            {
                vm.RequestClose += () => Close();
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Evitar disparar o arrasto da janela quando o clique vier do mapa tático ou botões
            if (e.OriginalSource is DependencyObject dep)
            {
                if (FindVisualParent<FlightMapControl>(dep) != null) return;
                if (FindVisualParent<Button>(dep) != null) return;
                if (FindVisualParent<TextBox>(dep) != null) return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObj = VisualTreeHelper.GetParent(child);
            if (parentObj == null) return null;
            if (parentObj is T parent) return parent;
            return FindVisualParent<T>(parentObj);
        }
    }
}
