using System.Runtime.InteropServices;

namespace FlightCareer.Domain.Integrity
{
    /// <summary>
    /// Estado de integridade lido do simulador em definição separada.
    /// Contém status de slew e flags de congelamento (altitude, atitude, posição).
    /// A ordem dos campos deve corresponder exatamente à ordem em SimConnectService.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimIntegrityState
    {
        public double IsSlewActive;                     // IS SLEW ACTIVE (Bool)
        public double IsAltitudeFreezeOn;               // IS ALTITUDE FREEZE ON (Bool)
        public double IsAttitudeFreezeOn;               // IS ATTITUDE FREEZE ON (Bool)
        public double IsLatitudeLongitudeFreezeOn;      // IS LATITUDE LONGITUDE FREEZE ON (Bool)
    }
}
