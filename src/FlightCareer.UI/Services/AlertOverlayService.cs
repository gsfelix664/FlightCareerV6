using System;
using System.Diagnostics;
using System.Windows;
using FlightCareer.UI.ViewModels;
using FlightCareer.UI.Views;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Mostra as mensagens do voo no overlay por cima do simulador em vez de caixas modais no app.
    /// A janela é criada na primeira mensagem e escondida quando a fila esvazia. Pode ser chamado de qualquer thread.
    /// </summary>
    public class AlertOverlayService
    {
        private readonly AlertOverlayViewModel _viewModel = new();
        private AlertOverlayWindow? _window;

        public AlertOverlayService()
        {
            _viewModel.HasToastsChanged += hasToasts =>
            {
                if (_window == null) return;
                if (hasToasts) _window.Show();
                else _window.Hide();
            };
        }

        public void Show(string title, string message, AlertKind kind, bool sticky = false)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (_window == null)
                    {
                        _window = new AlertOverlayWindow { DataContext = _viewModel };
                    }
                    _viewModel.Show(title, message, kind, sticky);
                }
                catch (Exception ex)
                {
                    // Sem overlay, a mensagem não pode se perder: volta para a caixa do app
                    Debug.WriteLine($"[AlertOverlay] Falha ao mostrar no overlay: {ex.Message}");
                    MessageBox.Show(message, title, MessageBoxButton.OK,
                        kind == AlertKind.Error ? MessageBoxImage.Error : MessageBoxImage.Information);
                }
            });
        }
    }
}
