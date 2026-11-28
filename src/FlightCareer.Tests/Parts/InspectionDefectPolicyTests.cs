using FlightCareer.Domain.Parts;
using FlightCareer.Domain.Services;
using Xunit;

namespace FlightCareer.Tests.Parts
{
    public class InspectionDefectPolicyTests
    {
        [Fact]
        public void IsGrounding_TouchdownCodeWithIsGroundingTrue_ReturnsTrue()
        {
            var e = new PartWearInspectionEvent("HARD_LANDING_INSPECTION", "touchdown_hard_landing", "gear", true, "Pouso duro");

            Assert.True(InspectionDefectPolicy.IsGrounding(e));
        }

        [Fact]
        public void IsGrounding_NonTouchdownCodeWithIsGroundingTrue_ReturnsFalse()
        {
            var e = new PartWearInspectionEvent("ENGINE_OVERSPEED_INSPECTION", "piston_rpm_overspeed", "engine", true, "Sobrevelocidade de motor");

            Assert.False(InspectionDefectPolicy.IsGrounding(e));
        }

        [Fact]
        public void IsGrounding_TouchdownCodeWithIsGroundingFalse_ReturnsFalse()
        {
            var e = new PartWearInspectionEvent("HARD_LANDING_INSPECTION", "touchdown_hard_landing", "gear", false, "Pouso duro");

            Assert.False(InspectionDefectPolicy.IsGrounding(e));
        }

        [Fact]
        public void Title_WithDescription_FormatsWithDescription()
        {
            var e = new PartWearInspectionEvent("HARD_LANDING_INSPECTION", "touchdown_hard_landing", "gear", true, "Pouso duro");

            Assert.Equal("Inspeção: Pouso duro", InspectionDefectPolicy.Title(e));
        }

        [Fact]
        public void Title_WithEmptyDescription_FallsBackToDefectCode()
        {
            var e = new PartWearInspectionEvent("HARD_LANDING_INSPECTION", "touchdown_hard_landing", "gear", true, "");

            Assert.Equal("Inspeção: HARD_LANDING_INSPECTION", InspectionDefectPolicy.Title(e));
        }

        [Fact]
        public void DefectCatalog_GetInspectionCode_ReturnsNonGroundingAndCost400()
        {
            var spec = DefectCatalog.Get("FOO_INSPECTION");

            Assert.Equal("FOO_INSPECTION", spec.Code);
            Assert.Equal("Inspeção pendente", spec.Title);
            Assert.False(spec.IsGrounding);
            Assert.Equal(400, spec.BaseRepairCost);
            Assert.Equal(DefectEnforcement.None, spec.Enforcement);
        }

        [Fact]
        public void DefectCatalog_IsInspection_DetectsInspectionSuffix()
        {
            Assert.True(DefectCatalog.IsInspection("HARD_LANDING_INSPECTION"));
            Assert.False(DefectCatalog.IsInspection("ENGINE_FAILURE"));
        }
    }
}
