using System;
using System.Collections.Generic;
using System.Linq;
using FlightCareer.Application.Interfaces;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Missions;

namespace FlightCareer.Application.Services
{
    public readonly struct ProfileCacheKey : IEquatable<ProfileCacheKey>
    {
        public readonly string DefId;
        public readonly MissionProfileDefinition Profile;

        public ProfileCacheKey(string defId, MissionProfileDefinition profile)
        {
            DefId = defId;
            Profile = profile;
        }

        public bool Equals(ProfileCacheKey other) =>
            string.Equals(DefId, other.DefId, StringComparison.OrdinalIgnoreCase) &&
            ReferenceEquals(Profile, other.Profile);

        public override bool Equals(object? obj) => obj is ProfileCacheKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(DefId), Profile);
    }

    public sealed class DestinationChoiceGroup
    {
        private readonly AirportCandidate[] _candidates;
        private readonly double[] _cumulativeWeights;
        private readonly double _totalWeight;

        public DestinationChoiceGroup(
            MissionDefinition def,
            IReadOnlyList<AirportCandidate> validCandidates,
            Func<AirportCandidate, IReadOnlySet<string>> getCandidateTags,
            Func<AirportCandidate, double>? extraMultiplier = null)
        {
            int count = validCandidates.Count;
            _candidates = new AirportCandidate[count];
            for (int i = 0; i < count; i++)
            {
                _candidates[i] = validCandidates[i];
            }

            if (count <= 1)
            {
                _cumulativeWeights = Array.Empty<double>();
                _totalWeight = 0.0;
                return;
            }

            _cumulativeWeights = new double[count];
            double total = 0.0;
            for (int i = 0; i < count; i++)
            {
                double weight = MissionWeighting.DestinationWeight(def, getCandidateTags(_candidates[i]));
                if (extraMultiplier != null)
                {
                    weight *= extraMultiplier(_candidates[i]);
                }
                total += weight;
                _cumulativeWeights[i] = total;
            }
            _totalWeight = total;
        }

        public int Count => _candidates.Length;
        public IReadOnlyList<AirportCandidate> Candidates => _candidates;
        public double TotalWeight => _totalWeight;

        public AirportCandidate? Pick(Random random)
        {
            if (_candidates.Length == 0) return null;
            if (_candidates.Length == 1) return _candidates[0];

            if (_totalWeight <= 0.0)
            {
                return _candidates[random.Next(_candidates.Length)];
            }

            double roll = random.NextDouble() * _totalWeight;
            for (int i = 0; i < _cumulativeWeights.Length; i++)
            {
                if (roll <= _cumulativeWeights[i])
                {
                    return _candidates[i];
                }
            }
            return _candidates[^1];
        }
    }

    /// <summary>
    /// Reaproveita entre quadros o que não consome sorteio. Reutilize a mesma instância só com as mesmas listas de
    /// aeroportos/pistas, catálogo e serviços: a chave interna é a origem (e missão + perfil).
    /// </summary>
    public sealed class BoardCache
    {
        private IReadOnlyList<Airport>? _cachedAirportsRef;
        private IReadOnlyList<Runway>? _cachedRunwaysRef;
        private Dictionary<string, Runway?>? _runwaysByIcao;
        private List<AirportCandidate>? _candidates;
        private List<Airport>? _tier1And2Airports;

        private Airport? _cachedOrigin;
        private double[]? _cachedDistances;
        private IReadOnlySet<string>? _cachedOriginTags;
        private List<(MissionDefinition Mission, double Weight)>? _cachedEligibleMissions;
        private double _cachedTotalOriginWeight;

        private readonly Dictionary<ProfileCacheKey, DestinationChoiceGroup> _destinationGroups = new();
        private readonly Dictionary<string, IReadOnlySet<string>> _candidateTagsWithPistaCurta = new(StringComparer.OrdinalIgnoreCase);

        public BoardCache() { }

        public BoardCache(IReadOnlyList<Airport> allAirports, IReadOnlyList<Runway>? allRunways = null)
        {
            InitializeGlobal(allAirports, allRunways);
        }

        public Airport? CurrentOrigin => _cachedOrigin;

        public void InitializeGlobal(IReadOnlyList<Airport> allAirports, IReadOnlyList<Runway>? allRunways)
        {
            if (ReferenceEquals(_cachedAirportsRef, allAirports) && ReferenceEquals(_cachedRunwaysRef, allRunways) && _candidates != null)
            {
                return;
            }

            _cachedAirportsRef = allAirports;
            _cachedRunwaysRef = allRunways;

            if (allRunways != null && allRunways.Count > 0)
            {
                _runwaysByIcao = allRunways
                    .GroupBy(r => r.AirportIdent, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => !RunwaySurface.IsUnpaved(r.Surface)).ThenByDescending(r => r.LengthFt).FirstOrDefault(),
                        StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _runwaysByIcao = new Dictionary<string, Runway?>(StringComparer.OrdinalIgnoreCase);
            }

            _candidates = allAirports
                .Select(a => new AirportCandidate(a, _runwaysByIcao.GetValueOrDefault(a.ICAO)))
                .ToList();

            _tier1And2Airports = _candidates
                .Where(c => c.Airport.Tier <= 2)
                .Select(c => c.Airport)
                .ToList();

            _candidateTagsWithPistaCurta.Clear();
            _destinationGroups.Clear();
            _cachedOrigin = null;
            _cachedDistances = null;
            _cachedOriginTags = null;
            _cachedEligibleMissions = null;
            _cachedTotalOriginWeight = 0;
        }

        public IReadOnlyList<AirportCandidate> GetOrCreateCandidates(IReadOnlyList<Airport> allAirports, IReadOnlyList<Runway>? allRunways)
        {
            InitializeGlobal(allAirports, allRunways);
            return _candidates!;
        }

        public Runway? GetRunway(string icao) => _runwaysByIcao?.GetValueOrDefault(icao);

        public IReadOnlyList<Airport>? Tier1And2Airports => _tier1And2Airports;

        public void EnsureOrigin(Airport origin)
        {
            if (origin == null) throw new ArgumentNullException(nameof(origin));

            if (_cachedOrigin == null || !string.Equals(_cachedOrigin.ICAO, origin.ICAO, StringComparison.OrdinalIgnoreCase))
            {
                _cachedOrigin = origin;
                _cachedDistances = null;
                _cachedOriginTags = null;
                _cachedEligibleMissions = null;
                _cachedTotalOriginWeight = 0;
                _destinationGroups.Clear(); // Libera memória de destinos da origem anterior e impede vazamento
            }
        }

        public double[] GetOrComputeDistances(Airport origin, IReadOnlyList<AirportCandidate> candidates)
        {
            EnsureOrigin(origin);
            if (_cachedDistances != null && _cachedDistances.Length == candidates.Count)
            {
                return _cachedDistances;
            }

            var distances = new double[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var a = candidates[i].Airport;
                distances[i] = MissionGenerator.CalculateDistance(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude);
            }
            _cachedDistances = distances;
            return distances;
        }

        public IReadOnlySet<string> GetOrComputeOriginTags(Airport origin, IPlacesDataService? placesService)
        {
            EnsureOrigin(origin);
            if (_cachedOriginTags != null)
            {
                return _cachedOriginTags;
            }

            _cachedOriginTags = MissionBoardCore.AugmentOriginTags(origin, AirportProfileCatalog.Shared.TagsFor(origin.ICAO), placesService);
            return _cachedOriginTags;
        }

        public (List<(MissionDefinition Mission, double Weight)> Missions, double TotalWeight) GetOrComputeEligibleMissions(
            Airport origin,
            Runway? originRunway,
            IReadOnlySet<string> originTags,
            MissionCatalog catalog)
        {
            EnsureOrigin(origin);
            if (_cachedEligibleMissions != null)
            {
                return (_cachedEligibleMissions, _cachedTotalOriginWeight);
            }

            (_cachedEligibleMissions, _cachedTotalOriginWeight) =
                MissionBoardCore.ComputeEligibleMissions(origin, originRunway, originTags, catalog);
            return (_cachedEligibleMissions, _cachedTotalOriginWeight);
        }

        public DestinationChoiceGroup GetOrComputeDestinationGroup(
            Airport origin,
            MissionDefinition def,
            MissionProfileDefinition profile,
            IReadOnlyList<AirportCandidate> candidates,
            Func<string, IReadOnlySet<string>> getAirportTags,
            IReadOnlyList<double>? candidateDistances,
            AirlineRouteCatalog? routes = null)
        {
            EnsureOrigin(origin);
            var key = new ProfileCacheKey(def.Id, profile);
            if (_destinationGroups.TryGetValue(key, out var group))
            {
                return group;
            }

            var filtered = MissionBoardCore.FilterCandidates(origin, def, profile, candidates, getAirportTags, candidateDistances);
            var catalog = routes ?? AirlineRouteCatalog.Default;
            group = new DestinationChoiceGroup(
                def,
                filtered,
                c => GetCandidateTagsWithPistaCurta(c, getAirportTags),
                c => MissionWeighting.RealRouteMultiplier(def, catalog, origin.ICAO, c.Airport.ICAO));
            _destinationGroups[key] = group;
            return group;
        }

        public IReadOnlySet<string> GetCandidateTagsWithPistaCurta(AirportCandidate c, Func<string, IReadOnlySet<string>> getAirportTags)
        {
            if (_candidateTagsWithPistaCurta.TryGetValue(c.Airport.ICAO, out var tags))
            {
                return tags;
            }

            tags = MissionBoardCore.DestinationTags(c, getAirportTags);
            _candidateTagsWithPistaCurta[c.Airport.ICAO] = tags;
            return tags;
        }
    }
}
