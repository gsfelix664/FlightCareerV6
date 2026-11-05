using System;
using System.Threading.Tasks;

namespace FlightCareer.Domain.Services
{
    public interface IDataSeeder
    {
        Task SeedAsync(Action<string, double>? progressCallback = null);
        bool IsSeeded();
    }
}
