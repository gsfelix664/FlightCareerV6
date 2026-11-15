using System;
using System.IO;
using System.Text;
using FlightCareer.Infrastructure.Replay;
using Xunit;

namespace FlightCareer.Tests.Replay
{
    public class SensorLogReaderTests
    {
        [Fact]
        public void SensorLogReader_LeBomUtcSimRateEMarker()
        {
            // Cria CSV em memória com BOM UTF-8, utc, sim_rate, sensores e marcador com aspas/vírgulas
            var csvSb = new StringBuilder();
            csvSb.Append('\uFEFF'); // BOM
            csvSb.AppendLine("utc,sim_rate,eng-rpm:1,elec-bus-voltage-1,marker");
            csvSb.AppendLine("2026-11-15T12:00:00.0000000Z,1.0,2450.5,28.2,\"marcador, com virgula e \"\"aspas\"\"\"");
            csvSb.AppendLine("2026-11-15T12:00:01.0000000Z,2.0,2500.0,28.0,cal:engine-start");

            using var reader = new StringReader(csvSb.ToString());
            var logData = SensorLogReader.ReadLog(reader);

            Assert.Equal(2, logData.Frames.Count);

            // Frame 1
            var f1 = logData.Frames[0];
            Assert.Equal(new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc), f1.ReceivedUtc);
            Assert.Equal(1.0, f1.SimulationRate);
            Assert.Equal(2450.5, f1.Values["eng-rpm:1"]);
            Assert.Equal(28.2, f1.Values["elec-bus-voltage-1"]);

            // Frame 2
            var f2 = logData.Frames[1];
            Assert.Equal(new DateTime(2026, 11, 15, 12, 0, 1, DateTimeKind.Utc), f2.ReceivedUtc);
            Assert.Equal(2.0, f2.SimulationRate);
            Assert.Equal(2500.0, f2.Values["eng-rpm:1"]);

            // Marcadores
            Assert.Equal(2, logData.Markers.Count);
            Assert.Equal("marcador, com virgula e \"aspas\"", logData.Markers[0].Marker);
            Assert.Equal("cal:engine-start", logData.Markers[1].Marker);
        }

        [Fact]
        public void SensorLogReader_LeToquesDoBlackbox()
        {
            var bbSb = new StringBuilder();
            bbSb.AppendLine("Timestamp;ElapsedSec;Origin;Dest;Latitude;Longitude");
            bbSb.AppendLine("# [EVENTO 10.0s] START");
            bbSb.AppendLine("# [EVENTO 1690.8s] TOQUE: 658 fpm, pico 1.40 G, nariz 4.5°, inclinação 0.2°, GS 65 kt");
            bbSb.AppendLine("# [EVENTO 1705.5s] BOUNCE: TOQUE: 210 fpm, pico 1.15 G, nariz 3.0°, inclinação 0.1°, GS 55 kt");
            bbSb.AppendLine("2026-11-15 18:19:10;1761.0;SBMT;SNEJ;...");

            using var reader = new StringReader(bbSb.ToString());
            var touchdowns = SensorLogReader.ReadTouchdownsFromBlackbox(reader);

            Assert.Equal(2, touchdowns.Count);

            // Toque 1: principal
            var td1 = touchdowns[0];
            Assert.Equal(658.0, td1.Fpm);
            Assert.Equal(1690.8, td1.ElapsedSeconds);
            Assert.Equal(4.5, td1.PitchDeg);
            Assert.Equal(0.2, td1.BankDeg);
            Assert.False(td1.IsBounce);

            // Toque 2: bounce
            var td2 = touchdowns[1];
            Assert.Equal(210.0, td2.Fpm);
            Assert.Equal(1705.5, td2.ElapsedSeconds);
            Assert.Equal(3.0, td2.PitchDeg);
            Assert.Equal(0.1, td2.BankDeg);
            Assert.True(td2.IsBounce);
        }
    }
}
