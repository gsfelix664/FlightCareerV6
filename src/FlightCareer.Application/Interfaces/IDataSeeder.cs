using System;
using System.Threading.Tasks;

namespace FlightCareer.Application.Interfaces
{
    public interface IDataSeeder
    {
        Task SeedAsync(Action<string, double>? progressCallback = null);
        bool IsSeeded();
    }
}
