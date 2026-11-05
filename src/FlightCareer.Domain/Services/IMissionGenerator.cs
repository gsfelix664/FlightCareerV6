using FlightCareer.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightCareer.Domain.Services
{
    public interface IMissionGenerator
    {
        Task<IEnumerable<Mission>> GenerateMissionsAsync(Airport currentAirport);
    }
}
