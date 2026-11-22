using System;

namespace FlightCareer.Domain.Missions
{
    /// <summary>
    /// Rastreia o cumprimento de pernas paralelas em missões de levantamento / fotografia aérea (subtipo 3.6.1).
    /// Critério: cruzar a área retangular de borda a borda com rumo dentro de ±15° da orientação,
    /// altitude ±150 ft e velocidade ±10 kt do alvo durante >= 80% do trecho dentro da área.
    /// </summary>
    public sealed class SurveyLegTracker
    {
        private SurveyContractData? _data;
        private bool _inArea;
        private int _samplesCount;
        private int _validSamplesCount;
        private double _minU;
        private double _maxU;

        public int CompletedLegs { get; private set; }
        public int RequiredLegs => _data?.RequiredLegs ?? 0;
        public bool HasActiveContract => _data != null && _data.RequiredLegs > 0;
        public bool IsCompleted => HasActiveContract && CompletedLegs >= RequiredLegs;
        public SurveyContractData? ContractData => _data;

        public void Configure(SurveyContractData? data)
        {
            _data = data;
            Reset();
        }

        public void Reset()
        {
            CompletedLegs = 0;
            _inArea = false;
            _samplesCount = 0;
            _validSamplesCount = 0;
            _minU = 0.0;
            _maxU = 0.0;
        }

        public void Update(double lat, double lon, double altitudeFt, double iasKt, double headingDeg)
        {
            if (_data == null || _data.RequiredLegs <= 0) return;

            // Projeção local em relação ao centro (em milhas náuticas)
            double dNorth = (lat - _data.CenterLatitude) * 60.0;
            double cosLat = Math.Cos(_data.CenterLatitude * Math.PI / 180.0);
            double dEast = (lon - _data.CenterLongitude) * 60.0 * cosLat;

            // Rotação para os eixos da área orientada (u = ao longo da perna, v = transversal)
            double rad = _data.OrientationDeg * Math.PI / 180.0;
            double u = (dNorth * Math.Cos(rad)) + (dEast * Math.Sin(rad));
            double v = (-dNorth * Math.Sin(rad)) + (dEast * Math.Cos(rad));

            double halfL = _data.LengthNM / 2.0;
            double halfW = _data.WidthNM / 2.0;

            bool inside = Math.Abs(u) <= halfL && Math.Abs(v) <= halfW;

            // Checagem de tolerâncias na amostra atual
            bool altOk = Math.Abs(altitudeFt - _data.TargetAltitudeFt) <= 150.0;
            bool spdOk = Math.Abs(iasKt - _data.TargetSpeedKt) <= 10.0;
            bool hdgOk = AngleDiff(headingDeg, _data.OrientationDeg) <= 15.0;
            bool sampleValid = altOk && spdOk && hdgOk;

            if (inside)
            {
                if (!_inArea)
                {
                    _inArea = true;
                    _samplesCount = 0;
                    _validSamplesCount = 0;
                    _minU = u;
                    _maxU = u;
                }

                _samplesCount++;
                if (sampleValid)
                {
                    _validSamplesCount++;
                }

                if (u < _minU) _minU = u;
                if (u > _maxU) _maxU = u;
            }
            else if (_inArea)
            {
                // Saiu da área: avaliar se a perna cruzou de borda a borda e atendeu aos 80% de qualidade
                _inArea = false;
                double spanU = _maxU - _minU;
                bool crossedBorderToBorder = spanU >= (_data.LengthNM * 0.75);
                double validRatio = _samplesCount > 0 ? (double)_validSamplesCount / _samplesCount : 0.0;
                bool qualityOk = validRatio >= 0.80;

                if (crossedBorderToBorder && qualityOk)
                {
                    CompletedLegs++;
                }

                _samplesCount = 0;
                _validSamplesCount = 0;
            }
        }

        public static double AngleDiff(double a, double b)
        {
            double diff = Math.Abs(a - b) % 360.0;
            return diff > 180.0 ? 360.0 - diff : diff;
        }
    }
}
