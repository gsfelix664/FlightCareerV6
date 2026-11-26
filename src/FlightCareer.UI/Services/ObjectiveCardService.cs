using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using FlightCareer.Domain.Missions;
using FlightCareer.UI.ViewModels;
using FlightCareer.UI.Views;

namespace FlightCareer.UI.Services
{
    public class ObjectiveCardService
    {
        private readonly FlightMonitor _flightMonitor;
        private readonly ObjectiveCardViewModel _viewModel = new();
        private ObjectiveCardWindow? _window;
        private readonly DispatcherTimer _timer;
        private bool _wasMonitoring;

        private HwndSource? _hotkeySource;
        private const int HOTKEY_ID = 0x9001;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_O = 0x4F;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public ObjectiveCardService(FlightMonitor flightMonitor)
        {
            _flightMonitor = flightMonitor;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            RegisterGlobalHotKey();

            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Exit += (s, e) => Cleanup();
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            bool isMonitoring = _flightMonitor.IsMonitoring;

            // Transição false -> true: abre sozinha
            if (!_wasMonitoring && isMonitoring)
            {
                _wasMonitoring = true;
                Show();
                return;
            }

            // Transição true -> false: esconde a janela
            if (_wasMonitoring && !isMonitoring)
            {
                _wasMonitoring = false;
                Hide();
                return;
            }

            _wasMonitoring = isMonitoring;

            // Enquanto monitorando e visível, atualiza o VM
            if (isMonitoring && _window != null && _window.IsVisible)
            {
                UpdateContent();
            }
        }

        public void Show()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(Show);
                return;
            }

            EnsureWindowCreated();
            UpdateContent();
            _window!.Show();
        }

        public void Hide()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(Hide);
                return;
            }

            _window?.Hide();
        }

        public void Toggle()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(Toggle);
                return;
            }

            if (_window != null && _window.IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void EnsureWindowCreated()
        {
            if (_window == null)
            {
                _window = new ObjectiveCardWindow
                {
                    DataContext = _viewModel
                };
            }
        }

        private void UpdateContent()
        {
            if (!_flightMonitor.IsMonitoring)
            {
                _viewModel.SetEmpty("Nenhum voo em andamento.");
                return;
            }

            var missions = _flightMonitor.ActiveMissions?.ToList();
            var content = ObjectiveCardBuilder.Build(missions, _flightMonitor.ContractObjectiveLines);
            _viewModel.Update(content);
        }

        private void RegisterGlobalHotKey()
        {
            try
            {
                var parameters = new HwndSourceParameters("FlightCareer_ObjectiveCard_Hotkey")
                {
                    WindowStyle = 0,
                    ExtendedWindowStyle = 0,
                    ParentWindow = new IntPtr(-3) // HWND_MESSAGE
                };
                _hotkeySource = new HwndSource(parameters);
                _hotkeySource.AddHook(HwndHook);

                bool registered = RegisterHotKey(
                    _hotkeySource.Handle,
                    HOTKEY_ID,
                    MOD_CONTROL | MOD_ALT | MOD_NOREPEAT,
                    VK_O);

                if (!registered)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[ObjectiveCard] Falha ao registrar atalho global Ctrl+Alt+O (erro {error})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ObjectiveCard] Exceção ao registrar atalho global: {ex.Message}");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                Toggle();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void Cleanup()
        {
            try
            {
                if (_hotkeySource != null)
                {
                    UnregisterHotKey(_hotkeySource.Handle, HOTKEY_ID);
                    _hotkeySource.RemoveHook(HwndHook);
                    _hotkeySource.Dispose();
                    _hotkeySource = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ObjectiveCard] Falha ao desregistrar atalho global: {ex.Message}");
            }
        }
    }
}
