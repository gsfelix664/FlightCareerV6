using System.Runtime.InteropServices;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Struct sequencial para recebimento de strings de identificação via SimConnect (ANSI 256 bytes).
    /// Mapeia TITLE, ATC MODEL e ATC TYPE.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ContextStringData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string AtcModel;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string AtcType;
    }
}
