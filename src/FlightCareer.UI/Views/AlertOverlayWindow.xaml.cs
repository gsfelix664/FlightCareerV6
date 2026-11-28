using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using FlightCareer.Domain.Utils;
using FlightCareer.UI.Services;

namespace FlightCareer.UI.Views
{
    /// <summary>
    /// Overlay de mensagens por cima do simulador (como o HUD): sempre no topo, fora da barra de tarefas e sem
    /// roubar o foco do MSFS (WS_EX_NOACTIVATE), para que clicar no ✕ não tire o controle do avião.
    /// Funciona com o MSFS em janela ou tela cheia sem borda; tela cheia exclusiva cobre qualquer janela.
    /// </summary>
    public partial class AlertOverlayWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public AlertOverlayWindow()
        {
            InitializeComponent();
            ApplySavedOrDefaultPosition();
            SourceInitialized += OnSourceInitialized;
            Closing += (s, e) => SavePosition();
            IsVisibleChanged += (s, e) => { if (!IsVisible) SavePosition(); };
        }

        private void ApplySavedOrDefaultPosition()
        {
            if (WindowPositionStore.Shared.TryGet("alerts", out var pos))
            {
                var virtualArea = new WindowRect(
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight);

                double w = !double.IsNaN(Width) && Width > 0 ? Width : 460;
                double h = !double.IsNaN(Height) && Height > 0 ? Height : 200;

                var clamped = WindowPlacement.ClampToArea(new WindowRect(pos.X, pos.Y, w, h), virtualArea);
                Left = clamped.Left;
                Top = clamped.Top;
            }
            else
            {
                Left = Math.Max(0, (SystemParameters.PrimaryScreenWidth - Width) / 2);
                Top = 40;
            }
        }

        private void SavePosition()
        {
            WindowPositionStore.Shared.Save("alerts", Left, Top);
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
                Debug.WriteLine($"[AlertOverlay] Falha ao aplicar WS_EX_NOACTIVATE: {ex.Message}");
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            try
            {
                DragMove();
                SavePosition();
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[AlertOverlay] DragMove: {ex.Message}");
            }
        }
    }
}
