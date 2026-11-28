using System;

namespace FlightCareer.Domain.Utils
{
    public readonly record struct WindowRect(double Left, double Top, double Width, double Height);

    public static class WindowPlacement
    {
        /// <summary>
        /// Garante que pelo menos `minVisible` px (padrão 60) da largura e a barra de título (40 px) fiquem dentro de `area`.
        /// </summary>
        public static WindowRect ClampToArea(WindowRect window, WindowRect area, double minVisible = 60)
        {
            double minLeft = area.Left - window.Width + minVisible;
            double maxLeft = area.Left + area.Width - minVisible;

            double left;
            if (maxLeft < minLeft)
            {
                left = area.Left;
            }
            else
            {
                left = Math.Clamp(window.Left, minLeft, maxLeft);
            }

            double minTop = area.Top;
            double maxTop = area.Top + area.Height - 40;

            double top;
            if (maxTop < minTop)
            {
                top = area.Top;
            }
            else
            {
                top = Math.Clamp(window.Top, minTop, maxTop);
            }

            return new WindowRect(left, top, window.Width, window.Height);
        }
    }
}
