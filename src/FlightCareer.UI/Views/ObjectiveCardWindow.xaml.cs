using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace FlightCareer.UI.Views
{
    public partial class ObjectiveCardWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public ObjectiveCardWindow()
        {
            InitializeComponent();
            Left = Math.Max(0, SystemParameters.PrimaryScreenWidth - Width - 40);
            Top = 120;
            SourceInitialized += OnSourceInitialized;
            Closing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
            };
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
                SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ObjectiveCard] Falha ao aplicar WS_EX_NOACTIVATE: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[ObjectiveCard] DragMove: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
