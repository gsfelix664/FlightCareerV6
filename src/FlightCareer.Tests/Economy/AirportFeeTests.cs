using System;
using System.IO;
using System.Linq;
using FlightCareer.Domain.Economy;
using FlightCareer.Domain.Entities;
using Xunit;

namespace FlightCareer.Tests.Economy
{
    public class AirportFeeTests
    {
        [Theory]
        [InlineData(1, 1670.0)]
        [InlineData(1, 20000.0)]
        [InlineData(1, 180000.0)]
        [InlineData(2, 1670.0)]
        [InlineData(2, 20000.0)]
        [InlineData(2, 180000.0)]
        [InlineData(3, 1670.0)]
        [InlineData(3, 20000.0)]
        [InlineData(3, 180000.0)]
        [InlineData(4, 1670.0)]
        [InlineData(4, 20000.0)]
        [InlineData(4, 180000.0)]
        public void LandingFee_SameAsLegacyFormula_PerTier(int tier, double mtowLbs)
        {
            double legacyExpected = tier switch
            {
                1 => Math.Round(250.0 + (mtowLbs * 0.035), 2),
                2 => Math.Round(80.0 + (mtowLbs * 0.015), 2),
                3 => Math.Round(25.0 + (mtowLbs * 0.005), 2),
                _ => 0.0
            };

            var context = new FeeContext(tier, "TEST", mtowLbs, 0, false);
            var fees = GroundServiceCalculator.Calculate(context);
            var landing = fees.FirstOrDefault(f => f.Code == "LANDING");
            double actualFee = landing?.Total ?? 0.0;

            Assert.Equal(legacyExpected, actualFee, 2);
        }

        [Theory]
        [InlineData(12499.0, AircraftSizeClass.Light)]
        [InlineData(12500.0, AircraftSizeClass.Medium)]
        [InlineData(60000.0, AircraftSizeClass.Medium)]
        [InlineData(60001.0, AircraftSizeClass.Heavy)]
        public void SizeClass_Boundaries(double mtowLbs, AircraftSizeClass expectedClass)
        {
            var actual = AircraftSizeClassifier.Classify(mtowLbs);
            Assert.Equal(expectedClass, actual);
        }

        [Fact]
        public void PaxFee_PerPassenger_OnlyWithPassengers()
        {
            // Sem passageiros / sem missão: zero
            var ctxNoPax = new FeeContext(1, "TEST", 1670, 0, false);
            var feesNoPax = GroundServiceCalculator.Calculate(ctxNoPax);
            Assert.DoesNotContain(feesNoPax, f => f.Code == "PAX_FEE");

            // Com passageiros mas sem missão: zero
            var ctxNoMission = new FeeContext(1, "TEST", 1670, 5, false);
            var feesNoMission = GroundServiceCalculator.Calculate(ctxNoMission);
            Assert.DoesNotContain(feesNoMission, f => f.Code == "PAX_FEE");

            // Tier 1: 5 pax * $12 = $60
            var ctxT1 = new FeeContext(1, "TEST", 1670, 5, true);
            var feeT1 = GroundServiceCalculator.Calculate(ctxT1).First(f => f.Code == "PAX_FEE");
            Assert.Equal(60.0, feeT1.Total);

            // Tier 2: 5 pax * $6 = $30
            var ctxT2 = new FeeContext(2, "TEST", 1670, 5, true);
            var feeT2 = GroundServiceCalculator.Calculate(ctxT2).First(f => f.Code == "PAX_FEE");
            Assert.Equal(30.0, feeT2.Total);

            // Tier 3: 5 pax * $2 = $10
            var ctxT3 = new FeeContext(3, "TEST", 1670, 5, true);
            var feeT3 = GroundServiceCalculator.Calculate(ctxT3).First(f => f.Code == "PAX_FEE");
            Assert.Equal(10.0, feeT3.Total);

            // Tier 4: sem taxas
            var ctxT4 = new FeeContext(4, "TEST", 1670, 5, true);
            var feesT4 = GroundServiceCalculator.Calculate(ctxT4);
            Assert.Empty(feesT4);
        }

        [Fact]
        public void Handling_OnlyMediumHeavy_Tier1And2()
        {
            // Light em Tier 1: sem handling
            var ctxLight = new FeeContext(1, "TEST", 10000.0, 0, true);
            var feesLight = GroundServiceCalculator.Calculate(ctxLight);
            Assert.DoesNotContain(feesLight, f => f.Code == "HANDLING");

            // Medium em Tier 1: $250
            var ctxMedT1 = new FeeContext(1, "TEST", 20000.0, 0, true);
            var feeMedT1 = GroundServiceCalculator.Calculate(ctxMedT1).First(f => f.Code == "HANDLING");
            Assert.Equal(250.0, feeMedT1.Total);

            // Medium em Tier 2: $150
            var ctxMedT2 = new FeeContext(2, "TEST", 20000.0, 0, true);
            var feeMedT2 = GroundServiceCalculator.Calculate(ctxMedT2).First(f => f.Code == "HANDLING");
            Assert.Equal(150.0, feeMedT2.Total);

            // Heavy em Tier 1: $1500
            var ctxHvyT1 = new FeeContext(1, "TEST", 100000.0, 0, true);
            var feeHvyT1 = GroundServiceCalculator.Calculate(ctxHvyT1).First(f => f.Code == "HANDLING");
            Assert.Equal(1500.0, feeHvyT1.Total);

            // Heavy em Tier 2: $800
            var ctxHvyT2 = new FeeContext(2, "TEST", 100000.0, 0, true);
            var feeHvyT2 = GroundServiceCalculator.Calculate(ctxHvyT2).First(f => f.Code == "HANDLING");
            Assert.Equal(800.0, feeHvyT2.Total);

            // Tier 3: sem handling
            var ctxMedT3 = new FeeContext(3, "TEST", 20000.0, 0, true);
            var feesMedT3 = GroundServiceCalculator.Calculate(ctxMedT3);
            Assert.DoesNotContain(feesMedT3, f => f.Code == "HANDLING");

            // Sem missão: sem handling
            var ctxNoMission = new FeeContext(1, "TEST", 20000.0, 0, false);
            var feesNoMission = GroundServiceCalculator.Calculate(ctxNoMission);
            Assert.DoesNotContain(feesNoMission, f => f.Code == "HANDLING");
        }

        [Fact]
        public void NavTerminal_PerThousandLb_MediumHeavy()
        {
            // Light: não cobra
            var ctxLight = new FeeContext(1, "TEST", 10000.0, 0, false);
            var feesLight = GroundServiceCalculator.Calculate(ctxLight);
            Assert.DoesNotContain(feesLight, f => f.Code == "NAV_TERMINAL");

            // Medium 20.000 lb em Tier 1: (20000 / 1000) * 3 = $60
            var ctxMed = new FeeContext(1, "TEST", 20000.0, 0, false);
            var feeMed = GroundServiceCalculator.Calculate(ctxMed).First(f => f.Code == "NAV_TERMINAL");
            Assert.Equal(60.0, feeMed.Total);

            // Heavy 150.000 lb em Tier 2: (150000 / 1000) * 3 = $450
            var ctxHvy = new FeeContext(2, "TEST", 150000.0, 0, false);
            var feeHvy = GroundServiceCalculator.Calculate(ctxHvy).First(f => f.Code == "NAV_TERMINAL");
            Assert.Equal(450.0, feeHvy.Total);

            // Tier 4: sem tarifas
            var ctxT4 = new FeeContext(4, "TEST", 150000.0, 0, false);
            Assert.Empty(GroundServiceCalculator.Calculate(ctxT4));
        }

        [Fact]
        public void Parking_FreeFirst2Hours_ThenHourly_BySize()
        {
            // 2 horas ou menos: gratuito
            var ctx2h = new FeeContext(1, "TEST", 1670.0, 0, false, parkedGameHours: 2.0);
            var fees2h = GroundServiceCalculator.Calculate(ctx2h);
            Assert.DoesNotContain(fees2h, f => f.Code == "PARKING");

            // 3.5 horas (1.5 h faturada) Light em Tier 1: 1.5 * $5 = $7.50
            var ctxLight = new FeeContext(1, "TEST", 1670.0, 0, false, parkedGameHours: 3.5);
            var feeLight = GroundServiceCalculator.Calculate(ctxLight).First(f => f.Code == "PARKING");
            Assert.Equal(7.50, feeLight.Total);

            // 4 horas (2 h faturadas) Medium em Tier 1: 2 * $40 = $80
            var ctxMedT1 = new FeeContext(1, "TEST", 25000.0, 0, false, parkedGameHours: 4.0);
            var feeMedT1 = GroundServiceCalculator.Calculate(ctxMedT1).First(f => f.Code == "PARKING");
            Assert.Equal(80.0, feeMedT1.Total);

            // 4 horas (2 h faturadas) Heavy em Tier 1: 2 * $200 = $400
            var ctxHvyT1 = new FeeContext(1, "TEST", 80000.0, 0, false, parkedGameHours: 4.0);
            var feeHvyT1 = GroundServiceCalculator.Calculate(ctxHvyT1).First(f => f.Code == "PARKING");
            Assert.Equal(400.0, feeHvyT1.Total);

            // 4 horas Tier 2 Medium: 2 * $25 = $50
            var ctxMedT2 = new FeeContext(2, "TEST", 25000.0, 0, false, parkedGameHours: 4.0);
            var feeMedT2 = GroundServiceCalculator.Calculate(ctxMedT2).First(f => f.Code == "PARKING");
            Assert.Equal(50.0, feeMedT2.Total);

            // 4 horas Tier 3 Heavy: 2 * $50 = $100
            var ctxHvyT3 = new FeeContext(3, "TEST", 80000.0, 0, false, parkedGameHours: 4.0);
            var feeHvyT3 = GroundServiceCalculator.Calculate(ctxHvyT3).First(f => f.Code == "PARKING");
            Assert.Equal(100.0, feeHvyT3.Total);
        }

        [Fact]
        public void Tier4_NoFees()
        {
            var ctx = new FeeContext(4, "TEST", 180000.0, 150, true, parkedGameHours: 10.0);
            var fees = GroundServiceCalculator.Calculate(ctx);
            Assert.Empty(fees);
        }

        [Fact]
        public void C152_Hub_TotalAbout300()
        {
            // C152 MTOW 1.670 lb, Tier 1, 2 passageiros em missão:
            // Landing: 250 + (1670 * 0.035) = 250 + 58.45 = 308.45
            // Pax: 2 * 12 = 24.00
            // Total: 332.45
            var ctx = new FeeContext(1, "SBGR", 1670.0, 2, true, parkedGameHours: 0.0);
            var fees = GroundServiceCalculator.Calculate(ctx);

            double total = fees.Sum(f => f.Total);
            Assert.Equal(332.45, total, 2);
            Assert.True(total >= 300.0 && total <= 350.0, "Total esperado em torno de $300 a $335.");
        }

        [Fact]
        public void Jet_Hub_Expensive()
        {
            // Jato comercial MTOW 174.200 lb, Tier 1, 150 passageiros em missão:
            // Landing: 250 + (174200 * 0.035) = 250 + 6097.00 = $6,347.00
            // Pax: 150 * 12 = $1,800.00
            // Handling (Heavy, Tier 1): $1,500.00
            // Nav/Terminal: (174200 / 1000) * 3 = 174.2 * 3 = $522.60
            // Total calculado: $10,169.60
            var ctx = new FeeContext(1, "SBGR", 174200.0, 150, true, parkedGameHours: 0.0);
            var fees = GroundServiceCalculator.Calculate(ctx);

            Assert.Equal(4, fees.Count);
            Assert.Equal(6347.00, fees.First(f => f.Code == "LANDING").Total);
            Assert.Equal(1800.00, fees.First(f => f.Code == "PAX_FEE").Total);
            Assert.Equal(1500.00, fees.First(f => f.Code == "HANDLING").Total);
            Assert.Equal(522.60, fees.First(f => f.Code == "NAV_TERMINAL").Total);

            double grandTotal = fees.Sum(f => f.Total);
            Assert.Equal(10169.60, grandTotal, 2);
        }

        [Fact]
        public void Catalog_LoadsAndValidates()
        {
            // Carrega default sem erros
            var defaultCatalog = AirportFeeCatalog.LoadDefault();
            Assert.NotNull(defaultCatalog);
            Assert.Equal(1, defaultCatalog.SchemaVersion);
            Assert.Equal("$", defaultCatalog.Currency);

            // JSON vazio ou inválido lança exceção clara
            Assert.Throws<ArgumentException>(() => AirportFeeCatalog.LoadFromJson(string.Empty));
            Assert.Throws<InvalidOperationException>(() => AirportFeeCatalog.LoadFromJson("{ invalid json }"));
            Assert.Throws<InvalidOperationException>(() => AirportFeeCatalog.LoadFromJson("{\"schemaVersion\": 1}"));
        }
    }
}
