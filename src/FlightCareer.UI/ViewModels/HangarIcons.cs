using System.Collections.Generic;
using System.Windows.Media;
using FlightCareer.Domain.Components;
using FlightCareer.Domain.Parts;

namespace FlightCareer.UI.ViewModels
{
    /// <summary>
    /// Ícones vetoriais (sintaxe de path do SVG, caixa 24×24, traço) de cada grupo de componentes do Hangar
    /// e dos estados de inspeção/saúde das peças (check, exclamação, quebrado, interrogação).
    /// Desenhados para o app; renderizados com <c>Path</c> no tom da saúde do grupo/peça.
    /// </summary>
    public static class HangarIcons
    {
        private static readonly Dictionary<ComponentKind, Geometry> Icons = new()
        {
            // Bloco do motor com cabeçote, coletor e eixo
            [ComponentKind.Engine] = Freeze("M7,8 H17 V18 H7 Z M9,8 V5 H15 V8 M5,11 H7 M5,15 H7 M17,13 H20 M20,10 V16 M10,12 H14 M10,15 H14"),
            // Gota de óleo
            [ComponentKind.Oil] = Freeze("M12,3 C12,3 6,10 6,14.5 A6,6 0 0 0 18,14.5 C18,10 12,3 12,3 Z M9.5,15 A2.5,2.5 0 0 0 12,17.5"),
            // Vela de ignição com faísca
            [ComponentKind.Ignition] = Freeze("M10,2 H14 V5 H10 Z M8.5,5 H15.5 V9 H8.5 Z M10,9 H14 V15 H10 Z M12,15 V17 M12,19 L9,22 M12,19 L15,22 M12,19 V22"),
            // Chama (seção quente da turbina)
            [ComponentKind.HotSection] = Freeze("M12,2.5 C13,6 17.5,8.5 17.5,14 A5.5,5.5 0 0 1 6.5,14 C6.5,10.5 9,9 10,6.5 C10.8,9 12,10 12,10 C13,8 13,5 12,2.5 Z M12,15 A2,2 0 0 0 10,17"),
            // Raio
            [ComponentKind.Electrical] = Freeze("M13,2.5 L5,13.5 H11.5 L10.5,21.5 L19,10 H12.5 Z"),
            // Mostrador com ponteiro
            [ComponentKind.Instruments] = Freeze("M3.5,15 A8.5,8.5 0 0 1 20.5,15 M12,15 L16,9.5 M12,15 m-1.2,0 a1.2,1.2 0 1 0 2.4,0 a1.2,1.2 0 1 0 -2.4,0 M6,15 H7.5 M16.5,15 H18 M12,7.5 V9 M3.5,19 H20.5"),
            // Perna do trem com roda
            [ComponentKind.LandingGear] = Freeze("M12,2 V11 M8,4 H16 M9,4 L12,8 L15,4 M7,16.5 A5,5 0 1 0 17,16.5 A5,5 0 1 0 7,16.5 Z M10.5,16.5 A1.5,1.5 0 1 0 13.5,16.5 A1.5,1.5 0 1 0 10.5,16.5 Z"),
            // Pneu com banda de rodagem
            [ComponentKind.Tires] = Freeze("M3,12 A9,9 0 1 0 21,12 A9,9 0 1 0 3,12 Z M8,12 A4,4 0 1 0 16,12 A4,4 0 1 0 8,12 Z M12,3 V5 M12,19 V21 M3,12 H5 M19,12 H21 M5.6,5.6 L7,7 M17,17 L18.4,18.4 M5.6,18.4 L7,17 M17,7 L18.4,5.6"),
            // Disco de freio com pinça
            [ComponentKind.Brakes] = Freeze("M4,13 A7.5,7.5 0 1 0 19,13 A7.5,7.5 0 1 0 4,13 Z M9.5,13 A2,2 0 1 0 13.5,13 A2,2 0 1 0 9.5,13 Z M14.5,3.5 A9.5,9.5 0 0 1 21,9 L18,10.5 A6.5,6.5 0 0 0 13.5,6.5 Z"),
            // Silhueta do avião (célula)
            [ComponentKind.Airframe] = Freeze("M12,2.5 C12.9,2.5 13.3,3.8 13.3,5 V9.5 L21.5,14 V16 L13.3,13.5 V18.5 L16,20.5 V21.8 L12,20.7 L8,21.8 V20.5 L10.7,18.5 V13.5 L2.5,16 V14 L10.7,9.5 V5 C10.7,3.8 11.1,2.5 12,2.5 Z"),
        };

        // Geometrias de estado de saúde/inspeção (caixa 24×24)
        // Nova: círculo completo com check. Gasta mas boa: só meio círculo (vida consumida) com o mesmo check.
        public static readonly Geometry Check = Freeze("M12,2.5 A9.5,9.5 0 1 1 11.99,2.5 M7.5,12.5 L10.8,15.8 L16.8,9");
        public static readonly Geometry Worn = Freeze("M12,2.5 A9.5,9.5 0 0 0 12,21.5 M7.5,12.5 L10.8,15.8 L16.8,9");
        public static readonly Geometry Warning = Freeze("M12,3 L22,20.5 H2 Z M12,9 V14.5 M12,17.5 V18");
        // Quebrada: círculo partido por uma trinca em zigue-zague (não é o "X" de "proibido").
        public static readonly Geometry Broken = Freeze("M10.5,3 A9,9 0 0 0 9.5,20.6 M13.5,3.4 A9,9 0 0 1 14.5,20.6 M12,2 L10,7.5 L13,11 L10.5,15.5 L12.5,22");
        public static readonly Geometry Question = Freeze("M8.5,8.5 A4,4 0 0 1 15.5,8 C15.5,10.5 12,11.5 12,14 M12,17.5 V18");

        private static readonly Geometry Fallback = Freeze("M5,5 H19 V19 H5 Z");

        public static Geometry For(ComponentKind kind) => Icons.TryGetValue(kind, out var g) ? g : Fallback;

        public static Geometry ForDisplayState(PartDisplayState state) => state switch
        {
            PartDisplayState.Green => Check,
            PartDisplayState.Worn => Worn,
            PartDisplayState.Yellow => Warning,
            PartDisplayState.Red => Broken,
            PartDisplayState.Broken => Broken,
            PartDisplayState.Uncertain => Question,
            _ => Question
        };

        private static Geometry Freeze(string pathData)
        {
            var geometry = Geometry.Parse(pathData);
            geometry.Freeze();
            return geometry;
        }
    }
}
