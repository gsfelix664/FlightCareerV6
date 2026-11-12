using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Components;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Garante que toda aeronave do hangar tenha suas barras de componente.
    /// Aeronaves antigas são migradas a partir das colunas legadas (EngineCondition, OilCondition...).
    /// Idempotente: só cria os componentes que estiverem faltando.
    /// </summary>
    public sealed class AircraftComponentProvisioner
    {
        private readonly ComponentCatalog _catalog;
        private readonly IRandomSource _random;

        public AircraftComponentProvisioner(ComponentCatalog catalog, IRandomSource? random = null)
        {
            _catalog = catalog;
            _random = random ?? new SystemRandomSource();
        }

        /// <returns>Quantidade de componentes criados.</returns>
        public async Task<int> EnsureComponentsAsync(IAppDbContext db)
        {
            var aircraft = await db.PilotAircrafts
                .Include(a => a.AircraftType)
                .Include(a => a.Components)
                .ToListAsync();

            int created = 0;
            foreach (var a in aircraft)
            {
                var engineClass = _catalog.ResolveEngineClass(a.AircraftType);
                var existing = a.Components.Select(c => c.Kind).ToHashSet();

                var missing = LegacyComponentMigrator
                    .CreateComponents(a, engineClass, _catalog, _random)
                    .Where(c => !existing.Contains(c.Kind))
                    .ToList();

                if (missing.Count == 0) continue;
                db.AircraftComponents.AddRange(missing);
                created += missing.Count;
            }

            if (created > 0) await db.SaveChangesAsync();
            return created;
        }
    }
}
