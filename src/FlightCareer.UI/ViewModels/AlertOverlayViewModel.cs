using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlightCareer.UI.ViewModels
{
    public enum AlertKind { Info, Warning, Error }

    /// <summary>Uma mensagem do addon mostrada no overlay por cima do simulador.</summary>
    public partial class AlertToastViewModel : ObservableObject
    {
        public AlertToastViewModel(string title, string message, AlertKind kind, bool sticky = false)
        {
            _sticky = sticky;
            Title = title;
            Message = message;
            Kind = kind;
            Time = DateTime.Now.ToString("HH:mm:ss");
        }

        public string Title { get; }
        public string Message { get; }
        public AlertKind Kind { get; }
        public string Time { get; }
        private readonly bool _sticky;
        public bool IsSticky => _sticky || Kind == AlertKind.Error;

        public SolidColorBrush AccentBrush => Kind switch
        {
            AlertKind.Error => Freeze(0xFF, 0x52, 0x52),
            AlertKind.Warning => Freeze(0xFF, 0xB3, 0x00),
            _ => Freeze(0x00, 0xE5, 0xFF)
        };

        public string Icon => Kind switch
        {
            AlertKind.Error => "!",
            AlertKind.Warning => "!",
            _ => "i"
        };

        private static SolidColorBrush Freeze(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>
    /// Fila de mensagens do overlay: avisos somem sozinhos; erros ficam até o piloto fechar.
    /// Guarda no máximo <see cref="MaxVisible"/> mensagens (as mais antigas saem primeiro).
    /// </summary>
    public partial class AlertOverlayViewModel : ObservableObject
    {
        public const int MaxVisible = 5;
        public static readonly TimeSpan InfoDuration = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan WarningDuration = TimeSpan.FromSeconds(20);

        public ObservableCollection<AlertToastViewModel> Toasts { get; } = new();

        /// <summary>Disparado quando a fila passa de vazia para não vazia (true) ou o contrário (false).</summary>
        public event Action<bool>? HasToastsChanged;

        public void Show(string title, string message, AlertKind kind, bool sticky = false)
        {
            var toast = new AlertToastViewModel(title, message, kind, sticky);
            bool wasEmpty = Toasts.Count == 0;
            Toasts.Add(toast);
            while (Toasts.Count > MaxVisible) Toasts.RemoveAt(0);
            if (wasEmpty) HasToastsChanged?.Invoke(true);

            if (!toast.IsSticky)
            {
                var timer = new DispatcherTimer { Interval = kind == AlertKind.Warning ? WarningDuration : InfoDuration };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    Dismiss(toast);
                };
                timer.Start();
            }
        }

        [RelayCommand]
        private void Dismiss(AlertToastViewModel? toast)
        {
            if (toast == null || !Toasts.Remove(toast)) return;
            if (Toasts.Count == 0) HasToastsChanged?.Invoke(false);
        }

        [RelayCommand]
        private void DismissAll()
        {
            if (Toasts.Count == 0) return;
            Toasts.Clear();
            HasToastsChanged?.Invoke(false);
        }
    }
}
