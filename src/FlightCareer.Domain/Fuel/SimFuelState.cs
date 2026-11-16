using System.Runtime.InteropServices;

namespace FlightCareer.Domain.Fuel
{
    /// <summary>
    /// Estado de leitura de combustível do simulador (definição separada).
    /// Contém UNLIMITED FUEL, consumo por motor e capacidades/quantidades dos tanques
    /// (sistema moderno FUELSYSTEM e tanques legados).
    /// Atenção: a ordem dos campos deve corresponder exatamente à ordem em SimConnectService.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimFuelState
    {
        public double UnlimitedFuel;                        // UNLIMITED FUEL (Bool)
        public double FuelUsedSinceStart1;                  // GENERAL ENG FUEL USED SINCE START:1 (Pounds)
        public double FuelUsedSinceStart2;                  // GENERAL ENG FUEL USED SINCE START:2 (Pounds)
        public double FuelUsedSinceStart3;                  // GENERAL ENG FUEL USED SINCE START:3 (Pounds)
        public double FuelUsedSinceStart4;                  // GENERAL ENG FUEL USED SINCE START:4 (Pounds)
        public double NewFuelSystem;                        // NEW FUEL SYSTEM (Bool)

        // Tanques sistema moderno FUELSYSTEM (1..8)
        public double FuelSystemTankUsableCapacity1;        // FUELSYSTEM TANK USABLE CAPACITY:1 (Gallons)
        public double FuelSystemTankQuantity1;              // FUELSYSTEM TANK QUANTITY:1 (Gallons)
        public double FuelSystemTankUsableCapacity2;        // FUELSYSTEM TANK USABLE CAPACITY:2 (Gallons)
        public double FuelSystemTankQuantity2;              // FUELSYSTEM TANK QUANTITY:2 (Gallons)
        public double FuelSystemTankUsableCapacity3;        // FUELSYSTEM TANK USABLE CAPACITY:3 (Gallons)
        public double FuelSystemTankQuantity3;              // FUELSYSTEM TANK QUANTITY:3 (Gallons)
        public double FuelSystemTankUsableCapacity4;        // FUELSYSTEM TANK USABLE CAPACITY:4 (Gallons)
        public double FuelSystemTankQuantity4;              // FUELSYSTEM TANK QUANTITY:4 (Gallons)
        public double FuelSystemTankUsableCapacity5;        // FUELSYSTEM TANK USABLE CAPACITY:5 (Gallons)
        public double FuelSystemTankQuantity5;              // FUELSYSTEM TANK QUANTITY:5 (Gallons)
        public double FuelSystemTankUsableCapacity6;        // FUELSYSTEM TANK USABLE CAPACITY:6 (Gallons)
        public double FuelSystemTankQuantity6;              // FUELSYSTEM TANK QUANTITY:6 (Gallons)
        public double FuelSystemTankUsableCapacity7;        // FUELSYSTEM TANK USABLE CAPACITY:7 (Gallons)
        public double FuelSystemTankQuantity7;              // FUELSYSTEM TANK QUANTITY:7 (Gallons)
        public double FuelSystemTankUsableCapacity8;        // FUELSYSTEM TANK USABLE CAPACITY:8 (Gallons)
        public double FuelSystemTankQuantity8;              // FUELSYSTEM TANK QUANTITY:8 (Gallons)

        // Tanques sistema legado
        public double TankLeftMainCapacity;                 // FUEL TANK LEFT MAIN CAPACITY (Gallons)
        public double TankLeftMainQuantity;                 // FUEL TANK LEFT MAIN QUANTITY (Gallons)
        public double TankRightMainCapacity;                // FUEL TANK RIGHT MAIN CAPACITY (Gallons)
        public double TankRightMainQuantity;                // FUEL TANK RIGHT MAIN QUANTITY (Gallons)
        public double TankLeftAuxCapacity;                  // FUEL TANK LEFT AUX CAPACITY (Gallons)
        public double TankLeftAuxQuantity;                  // FUEL TANK LEFT AUX QUANTITY (Gallons)
        public double TankRightAuxCapacity;                 // FUEL TANK RIGHT AUX CAPACITY (Gallons)
        public double TankRightAuxQuantity;                 // FUEL TANK RIGHT AUX QUANTITY (Gallons)
        public double TankLeftTipCapacity;                  // FUEL TANK LEFT TIP CAPACITY (Gallons)
        public double TankLeftTipQuantity;                  // FUEL TANK LEFT TIP QUANTITY (Gallons)
        public double TankRightTipCapacity;                 // FUEL TANK RIGHT TIP CAPACITY (Gallons)
        public double TankRightTipQuantity;                 // FUEL TANK RIGHT TIP QUANTITY (Gallons)
        public double TankCenterCapacity;                   // FUEL TANK CENTER CAPACITY (Gallons)
        public double TankCenterQuantity;                   // FUEL TANK CENTER QUANTITY (Gallons)
        public double TankCenter2Capacity;                  // FUEL TANK CENTER2 CAPACITY (Gallons)
        public double TankCenter2Quantity;                  // FUEL TANK CENTER2 QUANTITY (Gallons)
        public double TankCenter3Capacity;                  // FUEL TANK CENTER3 CAPACITY (Gallons)
        public double TankCenter3Quantity;                  // FUEL TANK CENTER3 QUANTITY (Gallons)
    }
}
