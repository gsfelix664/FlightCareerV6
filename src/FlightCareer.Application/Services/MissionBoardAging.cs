using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightCareer.Application.Services
{
    /// <summary>
    /// Envelhecimento do quadro de missões por avanço do relógio de carreira (OFICINA-TEMPO-JOGO).
    /// Encurta a validade de contratos não aceitos e remove os que expirarem.
    /// </summary>
    public static class MissionBoardAging
    {
        /// <summary>
        /// Avança o relógio sobre todos os contratos não aceitos do quadro, encurtando a validade
        /// pelas horas de jogo informadas. Remove contratos que expirarem (sem chamar SaveChanges).
        /// Devolve quantos contratos venceram exclusivamente por causa deste avanço.
        /// </summary>
        public static async Task<int> AdvanceAsync(IAppDbContext context, double gameHours, DateTime now)
        {
            if (gameHours <= 0.0)
            {
                return 0;
            }

            var unaccepted = await context.Missions
                .Where(m => !m.IsAccepted)
                .ToListAsync();

            if (unaccepted.Count == 0)
            {
                return 0;
            }

            var toRemove = new List<Mission>();
            int newlyExpiredCount = 0;

            foreach (var mission in unaccepted)
            {
                bool wasValid = mission.ExpirationTime >= now;
                mission.ExpirationTime = mission.ExpirationTime.AddHours(-gameHours);
                bool isNowExpired = mission.ExpirationTime < now;

                if (isNowExpired)
                {
                    toRemove.Add(mission);
                    if (wasValid)
                    {
                        newlyExpiredCount++;
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                context.Missions.RemoveRange(toRemove);
            }

            return newlyExpiredCount;
        }
    }
}
