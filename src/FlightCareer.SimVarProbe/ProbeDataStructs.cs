using System.Runtime.InteropServices;

namespace FlightCareer.SimVarProbe
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ProbeBlockData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public double[] Values;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProbeSingleFloatData
    {
        public double Value;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ProbeStringData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Value;
    }
}
