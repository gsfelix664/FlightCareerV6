using FlightCareer.Domain.Missions;
using Xunit;

namespace FlightCareer.Tests.Missions
{
    public class OverflyTrackerTests
    {
        [Fact]
        public void Update_CustomRadius_TriggersOnlyWithinRadius()
        {
            var tracker = new OverflyTracker();
            // Point at (0, 0), elev 1000 ft, radius 1.5 NM, maxAgl 3000 ft
            var pt = new OverflyPoint("WP1", 0.0, 0.0, 1000.0, RadiusNm: 1.5, MaxAglFt: 3000.0);
            tracker.ConfigureWaypoints(new[] { pt });

            // 1.8 NM away (lat = 1.8 / 60.0 ~ 0.03 deg)
            double lat18 = 1.8 / 60.0;
            tracker.Update(lat18, 0.0, 2000.0);
            Assert.False(tracker.IsPointVisited("WP1"));

            // 1.4 NM away (lat = 1.4 / 60.0 ~ 0.0233 deg)
            double lat14 = 1.4 / 60.0;
            tracker.Update(lat14, 0.0, 2000.0);
            Assert.True(tracker.IsPointVisited("WP1"));
        }

        [Fact]
        public void Update_CustomMaxAgl_TriggersOnlyBelowMaxAltitude()
        {
            var tracker = new OverflyTracker();
            // Point at (0, 0), elev 2000 ft, radius 3.0 NM, maxAgl 6000 ft (max alt = 8000 ft)
            var pt = new OverflyPoint("AREA_N", 0.0, 0.0, 2000.0, RadiusNm: 3.0, MaxAglFt: 6000.0);
            tracker.ConfigureWaypoints(new[] { pt });

            // Close horizontally (0.5 NM), but at 8500 ft (above 8000 ft limit)
            tracker.Update(0.005, 0.0, 8500.0);
            Assert.False(tracker.IsPointVisited("AREA_N"));

            // Close horizontally (0.5 NM), at 7500 ft (below 8000 ft limit)
            tracker.Update(0.005, 0.0, 7500.0);
            Assert.True(tracker.IsPointVisited("AREA_N"));
        }

        [Fact]
        public void OverflyTracker_ComFaixa_ForaAbaixo_NaoConta()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA1", 0.0, 0.0, ElevationFt: 100.0, RadiusNm: 2.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 0.0, 2499.0);
            Assert.False(tracker.IsPointVisited("AREA1"));

            tracker.Update(0.0, 0.0, 2500.0);
            Assert.True(tracker.IsPointVisited("AREA1"));
        }

        [Fact]
        public void OverflyTracker_ComFaixa_ForaAcima_NaoConta()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA1", 0.0, 0.0, ElevationFt: 100.0, RadiusNm: 2.0, MaxAglFt: 3500.0, MinAglFt: 2500.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 0.0, 3701.0);
            Assert.False(tracker.IsPointVisited("AREA1"));

            tracker.Update(0.0, 0.0, 3700.0);
            Assert.True(tracker.IsPointVisited("AREA1"));
        }

        [Fact]
        public void OverflyTracker_SemMinimo_RegraAntiga()
        {
            var tracker = new OverflyTracker();
            var pt = new OverflyPoint("AREA_OLD", 0.0, 0.0, ElevationFt: 500.0, RadiusNm: 2.0, MaxAglFt: 3000.0, MinAglFt: 0.0);
            tracker.ConfigureWaypoints(new[] { pt });

            tracker.Update(0.0, 0.0, 500.0);
            Assert.True(tracker.IsPointVisited("AREA_OLD"));
        }
    }
}
