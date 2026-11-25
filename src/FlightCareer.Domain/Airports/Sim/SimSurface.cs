namespace FlightCareer.Domain.Airports.Sim
{
    public static class SimSurface
    {
        public static string ToSurfaceName(int code) => code switch
        {
            0 => "Concrete",
            1 or 3 or 5 or 6 => "Grass",
            7 => "Turf",
            4 => "Asphalt",
            8 => "Snow",
            9 => "Ice",
            12 => "Dirt",
            13 => "Coral",
            14 => "Gravel",
            15 => "Oil Treated",
            16 => "Mat",
            17 => "Bituminous",
            18 => "Brick",
            19 => "Macadam",
            20 => "Planks",
            21 => "Sand",
            22 => "Shale",
            23 => "Tarmac",
            2 or (>= 26 and <= 31) => "Water",
            _ => "Other"
        };

        public static bool IsWater(int code) => code == 2 || (code >= 26 && code <= 31);

        public static bool IsPaved(int code) => code is 0 or 4 or 17 or 18 or 19 or 23;
    }
}
