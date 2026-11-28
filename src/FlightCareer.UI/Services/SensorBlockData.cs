using System.Runtime.InteropServices;

namespace FlightCareer.UI.Services
{
    /// <summary>
    /// Struct sequencial para recebimento de blocos de até 256 doubles via SimConnect.
    /// Tamanho marshalado = 2048 bytes (256 * 8).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorBlockData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public double[] Values;
    }
}
