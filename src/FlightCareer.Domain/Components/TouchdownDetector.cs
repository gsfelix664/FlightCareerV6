using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.Domain.Components
{
    /// <summary>
    /// Amostra da leitura rápida (a cada 2 frames do simulador). Tempo em segundos monotônicos (relógio do app).
    /// <paramref name="NoseUpDeg"/>: positivo = nariz para cima (a SimVar PLANE PITCH DEGREES do MSFS é o contrário).
    /// </summary>
    public sealed record FastFlightSample(
        double TimeSec,
        bool OnGround,
        double VerticalSpeedFpm,
        double BodyVerticalFps,
        double GForce,
        double NoseUpDeg,
        double BankDeg,
        double IndicatedKnots,
        double GroundSpeedKnots,
        double TouchdownNormalFps = double.NaN);

    public enum TouchdownKind { Normal, Crash }

    /// <summary>Contato com o solo medido na leitura rápida: razão de descida antes do contato, pico de G e atitude.</summary>
    public sealed record TouchdownEvent(
        TouchdownKind Kind,
        double TouchdownFpm,
        double PeakG,
        double NoseUpDeg,
        double BankDeg,
        double GroundSpeedKnots,
        double ContactTimeSec,
        string Reason,
        IReadOnlyList<FastFlightSample> Trace,
        double AirborneSec = double.NaN);

    /// <summary>
    /// Detecta cada contato com o solo (ar → solo) e classifica Normal ou Crash pelos limites do catálogo
    /// (<see cref="LandingSettings"/>), independentemente de o simulador acusar o acidente (com "Desativar Danos de
    /// Colisão" marcado, o MSFS não envia Crashed nem CRASH FLAG). Mede a razão de descida no toque prioritariamente
    /// pela SimVar PLANE TOUCHDOWN NORMAL VELOCITY (velocidade normal ao solo no último toque calculada pelo simulador)
    /// e, caso indisponível ou inalterada em relação ao pré-contato, pela VERTICAL SPEED na janela antes do toque
    /// (sem usar VELOCITY BODY Y). O dano de pouso duro continua no WearEngine, que recebe a razão de descida medida aqui.
    /// Lógica pura: o tempo vem das amostras.
    /// </summary>
    public sealed class TouchdownDetector
    {
        private readonly LandingSettings _settings;
        private readonly List<FastFlightSample> _buffer = new();
        private double? _contactTime;
        private double _contactAirborneSec = double.NaN;
        private double _airborneSince = double.NaN;
        private bool _wasOnGround = true;
        private bool _hasSample;

        public TouchdownDetector(LandingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Reset()
        {
            _buffer.Clear();
            _contactTime = null;
            _contactAirborneSec = double.NaN;
            _airborneSince = double.NaN;
            _wasOnGround = true;
            _hasSample = false;
        }

        /// <summary>
        /// Quique na decolagem: contato pouco depois de sair do chão, sem pouso desde a decolagem.
        /// Não conta como pouso (nem ciclo, qualidade ou TGL); só a razão de descida pode causar dano.
        /// Sem o tempo no ar medido, não é quique.
        /// </summary>
        public static bool IsTakeoffBounce(TouchdownEvent ev, bool landedSinceTakeoff, LandingSettings settings)
        {
            if (ev == null || settings == null || landedSinceTakeoff) return false;
            return !double.IsNaN(ev.AirborneSec) && ev.AirborneSec < settings.TakeoffBounceMaxAirborneSec;
        }

        /// <summary>Recebe uma amostra; devolve o evento quando a janela depois do contato fecha.</summary>
        public TouchdownEvent? Update(FastFlightSample s)
        {
            _buffer.Add(s);
            double keepFrom = s.TimeSec - Math.Max(_settings.TraceBeforeSec, _settings.ContactWindowBeforeSec) - _settings.ContactWindowAfterSec - 1.0;
            int drop = _buffer.FindIndex(x => x.TimeSec >= keepFrom);
            if (drop > 0) _buffer.RemoveRange(0, drop);

            if (!_hasSample)
            {
                _hasSample = true;
                _wasOnGround = s.OnGround;
                if (!s.OnGround) _airborneSince = s.TimeSec;
                return null;
            }

            if (!s.OnGround)
            {
                if (_wasOnGround) _airborneSince = s.TimeSec;
            }
            else if (!_wasOnGround && _contactTime == null
                     && !double.IsNaN(_airborneSince) && s.TimeSec - _airborneSince >= _settings.MinAirborneSec)
            {
                _contactTime = s.TimeSec;
                _contactAirborneSec = s.TimeSec - _airborneSince;
            }
            _wasOnGround = s.OnGround;

            if (_contactTime is double t && s.TimeSec - t >= _settings.ContactWindowAfterSec)
            {
                _contactTime = null;
                return Classify(t);
            }
            return null;
        }

        private TouchdownEvent Classify(double contact)
        {
            var before = _buffer.Where(x => x.TimeSec >= contact - _settings.ContactWindowBeforeSec && x.TimeSec <= contact).ToList();
            var around = _buffer.Where(x => x.TimeSec >= contact - 0.2 && x.TimeSec <= contact + _settings.ContactWindowAfterSec).ToList();
            var atContact = _buffer.First(x => x.TimeSec >= contact);

            // (a) vsFpm = maior -VerticalSpeedFpm (positivo = descendo) nas amostras before (sem BodyVerticalFps)
            double vsFpm = 0;
            foreach (var x in before)
            {
                if (x.VerticalSpeedFpm < 0) vsFpm = Math.Max(vsFpm, -x.VerticalSpeedFpm);
            }

            // (b) normalFpm: valor de referência = TouchdownNormalFps da última amostra com TimeSec < contact (se não houver, NaN);
            // entre as amostras de contact até contact + ContactWindowAfterSec, pegar a última com TouchdownNormalFps finito
            // e diferente da referência (|Δ| > 1e-6); se existir, normalFpm = Math.Abs(valor) * 60.
            double? normalFpm = null;
            var beforeContact = _buffer.Where(x => x.TimeSec < contact).ToList();
            double refNormal = beforeContact.Count > 0 ? beforeContact.Last().TouchdownNormalFps : double.NaN;

            var afterContact = _buffer.Where(x => x.TimeSec >= contact && x.TimeSec <= contact + _settings.ContactWindowAfterSec).ToList();
            var changedSample = afterContact.LastOrDefault(x =>
                !double.IsNaN(x.TouchdownNormalFps) &&
                !double.IsInfinity(x.TouchdownNormalFps) &&
                (double.IsNaN(refNormal) || Math.Abs(x.TouchdownNormalFps - refNormal) > 1e-6));

            if (changedSample != null)
            {
                normalFpm = Math.Abs(changedSample.TouchdownNormalFps) * 60.0;
            }

            // (c) fpm = normalFpm se existir; senão vsFpm.
            double fpm = normalFpm ?? vsFpm;

            double peakG = around.Count > 0 ? around.Max(x => x.GForce) : atContact.GForce;

            // Atitude no contato: o pior valor entre o último instante no ar e o contato
            var attitude = before.Count > 0 ? before : new List<FastFlightSample> { atContact };
            double bank = attitude.Max(x => Math.Abs(x.BankDeg));
            double noseDown = attitude.Max(x => -x.NoseUpDeg);
            double noseUp = attitude.Max(x => x.NoseUpDeg);

            var reasons = new List<string>();
            if (fpm >= _settings.CrashFpm) reasons.Add($"razão de descida {fpm:F0} fpm (limite {_settings.CrashFpm:F0})");
            if (bank >= _settings.CrashBankDeg) reasons.Add($"inclinação {bank:F0}° no contato (limite {_settings.CrashBankDeg:F0}°)");
            if (noseDown >= _settings.CrashNoseDownDeg) reasons.Add($"nariz {noseDown:F0}° para baixo no contato (limite {_settings.CrashNoseDownDeg:F0}°)");
            if (noseUp >= _settings.CrashNoseUpDeg) reasons.Add($"nariz {noseUp:F0}° para cima no contato (limite {_settings.CrashNoseUpDeg:F0}°)");
            if (peakG >= _settings.CrashG) reasons.Add($"impacto de {peakG:F1} G (limite {_settings.CrashG:F1})");

            var kind = reasons.Count > 0 ? TouchdownKind.Crash : TouchdownKind.Normal;
            double signedNose = noseDown > noseUp ? -noseDown : noseUp;
            var trace = _buffer.Where(x => x.TimeSec >= contact - _settings.TraceBeforeSec && x.TimeSec <= contact + _settings.ContactWindowAfterSec).ToList();

            return new TouchdownEvent(kind, Math.Round(fpm, 0), Math.Round(peakG, 2), Math.Round(signedNose, 1), Math.Round(bank, 1),
                Math.Round(atContact.GroundSpeedKnots, 1), contact, string.Join("; ", reasons), trace,
                Math.Round(_contactAirborneSec, 2));
        }
    }
}
