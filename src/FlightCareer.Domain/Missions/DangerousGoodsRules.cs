using System.Collections.Generic;
using System.Linq;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Domain.Missions
{
    public static class DangerousGoodsRules
    {
        public static DangerousGoodsClass? Resolve(Mission m, CargoKindCatalog? catalog = null)
        {
            if (m == null || string.IsNullOrEmpty(m.CargoKind) || string.IsNullOrEmpty(m.SubId))
            {
                return null;
            }

            var cat = catalog ?? CargoKindCatalog.Default;
            return cat.GetDangerousGoods(m.SubId, m.CargoKind);
        }

        public static string? CheckManifest(IEnumerable<Mission> missions, CargoKindCatalog? catalog = null)
        {
            if (missions == null) return null;

            var list = missions as IList<Mission> ?? missions.ToList();
            int totalPax = list.Sum(m => m.PassengerCount);
            if (totalPax <= 0) return null;

            var cat = catalog ?? CargoKindCatalog.Default;
            foreach (var m in list)
            {
                var dg = Resolve(m, cat);
                if (dg != null && dg.CargoAircraftOnly)
                {
                    return $"A missão '{m.Title}' transporta carga perigosa classe {dg.Code} ({dg.Label}) exclusiva para aeronave cargueira (Cargo Aircraft Only). Não é permitido transportar passageiros ({totalPax} a bordo) no mesmo manifesto.";
                }
            }

            return null;
        }
    }
}
