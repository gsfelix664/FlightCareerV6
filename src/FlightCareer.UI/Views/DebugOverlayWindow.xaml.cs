using System;
using System.Windows;
using System.Windows.Input;
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }
    }
}
