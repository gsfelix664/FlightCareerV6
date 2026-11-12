using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlightCareer.UI.Views
{
    public enum DialogKind
    {
        Info,
        Success,
        Warning,
        Danger
    }

    /// <summary>
    /// Caixa de diálogo no tema "Avionics Dark" (substitui MessageBox no Hangar).
    /// Confirm devolve true só quando o jogador clica no botão de confirmação.
    /// </summary>
    public static class ThemedDialog
    {
        private static readonly Brush Background = Brush("#141C24");
        private static readonly Brush Border = Brush("#263545");
        private static readonly Brush TextPrimary = Brush("#F0F4F8");
        private static readonly Brush TextSecondary = Brush("#8B9BAE");
        private static readonly Brush ButtonNeutral = Brush("#1F2B37");

        public static bool Confirm(string title, string message, string confirmText = "Confirmar", DialogKind kind = DialogKind.Info)
            => Show(title, message, confirmText, "Cancelar", kind);

        public static void Info(string title, string message, DialogKind kind = DialogKind.Info)
            => Show(title, message, "OK", null, kind);

        private static bool Show(string title, string message, string confirmText, string? cancelText, DialogKind kind)
        {
            Brush accent = kind switch
            {
                DialogKind.Success => Brush("#00E676"),
                DialogKind.Warning => Brush("#FFB300"),
                DialogKind.Danger => Brush("#FF5252"),
                _ => Brush("#00E5FF")
            };

            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current?.MainWindow,
                Title = title
            };
            if (window.Owner == null) window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            bool result = false;

            var titleBlock = new TextBlock
            {
                Text = title.ToUpperInvariant(),
                Foreground = accent,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Inter, Segoe UI"),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                Foreground = TextPrimary,
                FontSize = 13.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                FontFamily = new FontFamily("Inter, Segoe UI"),
                MaxWidth = 460
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
            if (cancelText != null)
            {
                buttons.Children.Add(MakeButton(cancelText, ButtonNeutral, TextSecondary, () => { result = false; window.Close(); }));
            }
            var confirm = MakeButton(confirmText, accent, Brush("#0B0F14"), () => { result = true; window.Close(); });
            confirm.Margin = new Thickness(10, 0, 0, 0);
            buttons.Children.Add(confirm);

            var content = new StackPanel();
            content.Children.Add(titleBlock);
            content.Children.Add(messageBlock);
            content.Children.Add(buttons);

            window.Content = new System.Windows.Controls.Border
            {
                Background = Background,
                BorderBrush = Border,
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24, 20, 24, 20),
                MinWidth = 380,
                Child = new DockPanel
                {
                    Children =
                    {
                        new System.Windows.Controls.Border { Width = 4, Background = accent, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 16, 0) },
                        content
                    }
                }
            };

            window.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape) { result = false; window.Close(); }
                else if (e.Key == Key.Enter) { result = true; window.Close(); }
            };
            window.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) window.DragMove(); };

            window.ShowDialog();
            return result;
        }

        private static Button MakeButton(string text, Brush background, Brush foreground, System.Action onClick)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            border.SetValue(System.Windows.Controls.Border.BackgroundProperty, background);
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(18, 9, 18, 9));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            var button = new Button
            {
                Content = new TextBlock { Text = text, Foreground = foreground, FontWeight = FontWeights.Bold, FontSize = 12.5 },
                Template = template,
                Cursor = Cursors.Hand
            };
            button.Click += (_, _) => onClick();
            return button;
        }

        private static Brush Brush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
