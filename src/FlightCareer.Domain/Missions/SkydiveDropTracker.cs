using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia o cumprimento do contrato de lançamento de paraquedistas (subtipo 3.6.2).
    /// Critério: subir até altitude de salto sobre a zona (<= 2 NM), manter ±200 ft e IAS <= 1.3*Vs1 (ou <= 90 kt)
    /// por >= 30 s contínuos (= salto feito). Na descida e pouso, monitora choque térmico da CHT.
    /// </summary>
    public sealed class SkydiveDropTracker
    {
        private SkydiveContractData? _data;
        public double ChtCoolingRateMax { get; set; } = 28.0;

        public bool HasActiveContract => _data != null;
        public bool DropCompleted { get; private set; }
        public double StableSeconds { get; private set; }
        public bool ThermalShockDetected { get; private set; }
        public double MaxCoolingRateObserved { get; private set; }
        public SkydiveContractData? ContractData => _data;

        public void Configure(SkydiveContractData? data, double chtCoolingRateMax = 28.0)
        {
            _data = data;
            ChtCoolingRateMax = chtCoolingRateMax > 0 ? chtCoolingRateMax : 28.0;
            Reset();
        }

        public void Reset()
        {
            DropCompleted = false;
            StableSeconds = 0.0;
            ThermalShockDetected = false;
            MaxCoolingRateObserved = 0.0;
        }

        public void Update(double lat, double lon, double altitudeMslFt, double iasKt, double chtCoolingRate, double dtSeconds)
        {
            if (_data == null) return;

            if (!DropCompleted)
            {
                double distNM = OverflyTracker.CalculateDistanceNM(lat, lon, _data.DropZoneLatitude, _data.DropZoneLongitude);
                double targetMsl = _data.DropZoneElevationFt + _data.TargetAltitudeAglFt;

                bool inZone = distNM <= 2.0;
                bool altOk = Math.Abs(altitudeMslFt - targetMsl) <= 200.0;
                bool spdOk = iasKt <= _data.MaxDropSpeedKt;

                if (inZone && altOk && spdOk)
                {
                    StableSeconds += dtSeconds;
                    if (StableSeconds >= 30.0)
                    {
                        DropCompleted = true;
                    }
                }
                else
                {
                    StableSeconds = 0.0;
                }
            }
            else
            {
                // Após o salto feito, monitora a descida contra choque térmico
                if (chtCoolingRate > ChtCoolingRateMax)
                {
                    ThermalShockDetected = true;
                    if (chtCoolingRate > MaxCoolingRateObserved)
                    {
                        MaxCoolingRateObserved = chtCoolingRate;
                    }
                }
            }
        }
    }
}
