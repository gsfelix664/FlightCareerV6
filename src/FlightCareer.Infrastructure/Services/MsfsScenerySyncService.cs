using FlightCareer.Domain.Entities;
using FlightCareer.Application.DTOs;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Utils;
using FlightCareer.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.Infrastructure.Services
{
    public class MsfsScenerySyncService : IMsfsScenerySyncService
    {
        private readonly AppDbContext _context;

        public MsfsScenerySyncService(AppDbContext context)
        {
            _context = context;
        }

        public string? LocateMsfsDatabasePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var lnmDir = Path.Combine(appData, "ABarthel", "little_navmap_db");

            if (!Directory.Exists(lnmDir))
                return null;

            // Prioridade 1: MSFS 2024
            var msfs24 = Path.Combine(lnmDir, "little_navmap_msfs24.sqlite");
            if (File.Exists(msfs24)) return msfs24;

            // Prioridade 2: MSFS 2020 / Padrão
            var msfs = Path.Combine(lnmDir, "little_navmap_msfs.sqlite");
            if (File.Exists(msfs)) return msfs;

            // Prioridade 3: Qualquer SQLite com msfs no nome
            var files = Directory.GetFiles(lnmDir, "*msfs*.sqlite");
            return files.FirstOrDefault();
        }

        public async Task<bool> NeedsSyncAsync()
        {
            try
            {
                // Se nenhum aeroporto estiver com IsInSimulator == true, necessita sincronizar
                return !await _context.Airports.AnyAsync(a => a.IsInSimulator);
            }
            catch
            {
                return true;
            }
        }

        private class MsfsAirportRecord
        {
            public string Ident { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        public async Task<MsfsSyncResult> SyncAirportsAsync(Action<string, double>? progressCallback = null)
        {
            var result = new MsfsSyncResult();
            var dbPath = LocateMsfsDatabasePath();

            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                result.Success = false;
                result.Message = "Banco de dados do simulador (Little Navmap SQLite) não encontrado em %APPDATA%\\ABarthel\\little_navmap_db\\.";
                return result;
            }

            progressCallback?.Invoke("Conectando ao banco de cenários do MSFS 2024...", 0.05);

            var msfsAirports = new List<MsfsAirportRecord>();
            var msfsByIdent = new Dictionary<string, MsfsAirportRecord>(StringComparer.OrdinalIgnoreCase);
            var spatialGrid = new Dictionary<(int y, int x), List<MsfsAirportRecord>>();

            // 1. Carregar aeródromos do simulador
            try
            {
                using (var simConn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;"))
                {
                    await simConn.OpenAsync();
                    using var cmd = simConn.CreateCommand();
                    cmd.CommandText = "SELECT ident, name, city, country, laty, lonx FROM airport;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var ident = reader.GetString(0);
                        var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        var city = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        var country = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        var lat = reader.GetDouble(4);
                        var lon = reader.GetDouble(5);

                        var record = new MsfsAirportRecord
                        {
                            Ident = ident.Trim().ToUpperInvariant(),
                            Name = name,
                            City = city,
                            Country = country,
                            Lat = lat,
                            Lon = lon
                        };

                        msfsAirports.Add(record);
                        if (!msfsByIdent.ContainsKey(record.Ident))
                        {
                            msfsByIdent[record.Ident] = record;
                        }

                        // Grid espacial com células de 0.5 grau (~30 NM)
                        var cell = ((int)(lat * 2.0), (int)(lon * 2.0));
                        if (!spatialGrid.TryGetValue(cell, out var cellList))
                        {
                            cellList = new List<MsfsAirportRecord>();
                            spatialGrid[cell] = cellList;
                        }
                        cellList.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Erro ao ler dados de cenários do MSFS: {ex.Message}";
                return result;
            }

            result.TotalMsfsAirports = msfsAirports.Count;
            progressCallback?.Invoke($"Carregados {msfsAirports.Count:N0} aeródromos do MSFS. Correlacionando com base DECEA/CSV...", 0.25);

            // 2. Carregar aeroportos locais
            var localAirports = await _context.Airports.ToListAsync();
            int total = localAirports.Count;
            int exactCount = 0;
            int divergenceCount = 0;
            int nonSimCount = 0;

            var updates = new List<(Guid Id, bool InSim, string? MsfsIdent)>(total);

            for (int i = 0; i < total; i++)
            {
                var apt = localAirports[i];
                var icao = apt.ICAO.Trim().ToUpperInvariant();

                bool matched = false;
                string? matchedIdent = null;

                // Teste 1: ICAO Idêntico e Proximidade Geoespacial <= 5 NM
                if (msfsByIdent.TryGetValue(icao, out var exact))
                {
                    double dist = LocationUtils.CalculateDistance(apt.Latitude, apt.Longitude, exact.Lat, exact.Lon);
                    if (dist <= 5.0)
                    {
                        matched = true;
                        matchedIdent = icao;
                        exactCount++;
                    }
                }

                // Teste 2: Se não casou por ICAO ou foi colisão/reaproveitamento (dist > 5 NM), busca no grid espacial <= 3 NM
                if (!matched)
                {
                    int cellY = (int)(apt.Latitude * 2.0);
                    int cellX = (int)(apt.Longitude * 2.0);
                    MsfsAirportRecord? bestCandidate = null;
                    double bestDist = 99999.0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var cell = (cellY + dy, cellX + dx);
                            if (spatialGrid.TryGetValue(cell, out var candidates))
                            {
                                foreach (var cand in candidates)
                                {
                                    double d = LocationUtils.CalculateDistance(apt.Latitude, apt.Longitude, cand.Lat, cand.Lon);
                                    if (d < bestDist)
                                    {
                                        bestDist = d;
                                        bestCandidate = cand;
                                    }
                                }
                            }
                        }
                    }

                    if (bestCandidate != null && bestDist <= 3.0)
                    {
                        matched = true;
                        matchedIdent = bestCandidate.Ident;
                        divergenceCount++;
                    }
                    else
                    {
                        nonSimCount++;
                    }
                }

                updates.Add((apt.Id, matched, matchedIdent));

                if (i % 2500 == 0)
                {
                    double prog = 0.25 + (0.45 * (i / (double)total));
                    progressCallback?.Invoke($"Processando correspondência ({i}/{total})...", prog);
                }
            }

            progressCallback?.Invoke("Gravando persistência atômica no banco de dados...", 0.75);

            // 3. Atualizar em lote no banco SQLite
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var conn = (SqliteConnection)_context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE Airports SET IsInSimulator = @InSim, MsfsIdent = @MsfsIdent WHERE Id = UPPER(@Id);";

                var paramInSim = updateCmd.CreateParameter();
                paramInSim.ParameterName = "@InSim";
                updateCmd.Parameters.Add(paramInSim);

                var paramMsfsIdent = updateCmd.CreateParameter();
                paramMsfsIdent.ParameterName = "@MsfsIdent";
                updateCmd.Parameters.Add(paramMsfsIdent);

                var paramId = updateCmd.CreateParameter();
                paramId.ParameterName = "@Id";
                updateCmd.Parameters.Add(paramId);

                for (int i = 0; i < updates.Count; i++)
                {
                    var u = updates[i];
                    paramInSim.Value = u.InSim ? 1 : 0;
                    paramMsfsIdent.Value = (object?)u.MsfsIdent ?? DBNull.Value;
                    paramId.Value = u.Id.ToString().ToUpperInvariant();
                    await updateCmd.ExecuteNonQueryAsync();

                    if (i % 5000 == 0)
                    {
                        double prog = 0.75 + (0.20 * (i / (double)updates.Count));
                        progressCallback?.Invoke($"Salvando registros ({i}/{updates.Count})...", prog);
                    }
                }

                // Sincronizar também missões existentes que tenham aeroportos de destino com divergência
                using var missionUpdateCmd = conn.CreateCommand();
                missionUpdateCmd.CommandText = @"
                    UPDATE Missions 
                    SET DestinationMsfsIdent = (SELECT MsfsIdent FROM Airports WHERE Airports.ICAO = Missions.DestinationICAO LIMIT 1),
                        OriginMsfsIdent = (SELECT MsfsIdent FROM Airports WHERE Airports.ICAO = Missions.OriginICAO LIMIT 1)
                    WHERE OriginMsfsIdent IS NULL OR OriginMsfsIdent = '' OR DestinationMsfsIdent IS NULL OR DestinationMsfsIdent = '';";
                await missionUpdateCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }

            result.Success = true;
            result.TotalValidatedInSim = exactCount + divergenceCount;
            result.DivergencesIdentified = divergenceCount;
            result.NonSimulatorExcluded = nonSimCount;
            result.Message = $"Sincronização Concluída: {result.TotalValidatedInSim:N0} aeródromos validados no MSFS 2024 ({divergenceCount:N0} divergências identificadas e {nonSimCount:N0} excluídos da geração).";

            progressCallback?.Invoke(result.Message, 1.0);
            return result;
        }
    }
}
