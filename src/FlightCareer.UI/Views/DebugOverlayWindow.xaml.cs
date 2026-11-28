using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlightCareer.Domain.Utils;
using FlightCareer.UI.Services;
using FlightCareer.UI.Utils;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Views
{
    public partial class DebugOverlayWindow : Window
    {
        public DebugOverlayWindow()
        {
            InitializeComponent();
            ApplySavedOrDefaultPosition();
            DataContextChanged += DebugOverlayWindow_DataContextChanged;
            Closing += (s, e) => SavePosition();
            IsVisibleChanged += (s, e) => { if (!IsVisible) SavePosition(); };
        }

        private void ApplySavedOrDefaultPosition()
        {
            if (WindowPositionStore.Shared.TryGet("hud", out var pos))
            {
                var virtualArea = new WindowRect(
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight);

                double w = !double.IsNaN(Width) && Width > 0 ? Width : 340;
                double h = !double.IsNaN(Height) && Height > 0 ? Height : 200;

                var clamped = WindowPlacement.ClampToArea(new WindowRect(pos.X, pos.Y, w, h), virtualArea);
                Left = clamped.Left;
                Top = clamped.Top;
            }
            else
            {
                Left = 40;
                Top = 80;
            }
        }

        private void SavePosition()
        {
            WindowPositionStore.Shared.Save("hud", Left, Top);
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
                try
                {
                    DragMove();
                    SavePosition();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DebugOverlay] DragMove: {ex.Message}");
                }
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
                try
                {
                    DragMove();
                    SavePosition();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DebugOverlay] DragMove: {ex.Message}");
                }
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
