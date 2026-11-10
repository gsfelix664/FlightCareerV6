using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlightCareer.Domain.Entities;

namespace FlightCareer.Infrastructure.Services
{
    /// <summary>
    /// Serviço de normalização, higienização e saneamento de dados aeronáuticos (ICAO, Municípios e Países ISO).
    /// Corrige anomalias provenientes de bases de simuladores (MSFS / Little Navmap) onde códigos de FIR (ex: SB, SS, K4)
    /// foram gravados no lugar de países ISO ou municípios ficaram em branco.
    /// </summary>
    public static class AviationDataNormalizer
    {
        /// <summary>
        /// Prefixos ICAO oficiais do Brasil (Anexo 10 da OACI).
        /// Todos os aeródromos iniciados com estes prefixos pertencem obrigatoriamente ao território brasileiro (ISO: BR).
        /// </summary>
        private static readonly HashSet<string> BrazilianIcaoPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "SB", // Principais aeródromos IFR, controlados e internacionais
            "SD", // Aeródromos nos estados de SP e RJ
            "SN", // Aeródromos nos estados de MG e ES
            "SS", // Aeródromos na Região Sul (PR, SC, RS)
            "SW", // Aeródromos na Região Centro-Oeste e Norte (MT, MS, RO, AC, AM)
            "SI", // Aeródromos em GO, DF e TO
            "SJ"  // Aeródromos no Norte e Nordeste (PA, AP, MA, PI, CE, RN, PB, PE, AL, SE)
        };

        /// <summary>
        /// Códigos FIR ou regionais conhecidos que colidem ou são inseridos incorretamente como país.
        /// </summary>
        private static readonly HashSet<string> KnownFirCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "SB", "SD", "SN", "SS", "SW", "SI", "SJ", "K1", "K2", "K3", "K4", "K5", "K6", "K7", "KZ", "C1", "C2", "C3", "CZ"
        };

        /// <summary>
        /// Mapeamento de prefixos ICAO de 2 letras para código de país ISO 3166-1 alpha-2.
        /// </summary>
        private static readonly Dictionary<string, string> TwoLetterIcaoToIsoCountry = new(StringComparer.OrdinalIgnoreCase)
        {
            // Brasil
            { "SB", "BR" }, { "SD", "BR" }, { "SN", "BR" }, { "SS", "BR" }, { "SW", "BR" }, { "SI", "BR" }, { "SJ", "BR" },

            // América do Sul
            { "SA", "AR" }, // Argentina
            { "SC", "CL" }, // Chile
            { "SE", "EC" }, // Equador
            { "SF", "FK" }, // Ilhas Malvinas
            { "SG", "PY" }, // Paraguai
            { "SK", "CO" }, // Colômbia
            { "SL", "BO" }, // Bolívia
            { "SM", "SR" }, // Suriname
            { "SO", "GF" }, // Guiana Francesa
            { "SP", "PE" }, // Peru
            { "SU", "UY" }, // Uruguai
            { "SV", "VE" }, // Venezuela
            { "SY", "GY" }, // Guiana

            // América do Norte & Caribe
            { "MM", "MX" }, // México
            { "PA", "US" }, // Alasca (EUA)
            { "PF", "US" }, // Alasca (EUA)
            { "PO", "US" }, // Alasca (EUA)
            { "PP", "US" }, // Alasca (EUA)
            { "PH", "US" }, // Havaí (EUA)
            { "TJ", "PR" }, // Porto Rico (EUA)
            { "TI", "VI" }, // Ilhas Virgens Americanas (EUA)
            { "MB", "TC" }, // Ilhas Turcas e Caicos
            { "MD", "DO" }, // República Dominicana
            { "MG", "GT" }, // Guatemala
            { "MH", "HN" }, // Honduras
            { "MK", "JM" }, // Jamaica
            { "MN", "NI" }, // Nicarágua
            { "MP", "PA" }, // Panamá
            { "MR", "CR" }, // Costa Rica
            { "MS", "SV" }, // El Salvador
            { "MT", "HT" }, // Haiti
            { "MU", "CU" }, // Cuba
            { "MW", "KY" }, // Ilhas Cayman
            { "MY", "BS" }, // Bahamas
            { "MZ", "BZ" }, // Belize
            { "TX", "BM" }, // Bermudas

            // Europa
            { "EG", "GB" }, // Reino Unido
            { "LF", "FR" }, // França
            { "ED", "DE" }, // Alemanha civil
            { "ET", "DE" }, // Alemanha militar
            { "LE", "ES" }, // Espanha
            { "LP", "PT" }, // Portugal
            { "LI", "IT" }, // Itália
            { "EH", "NL" }, // Holanda
            { "EB", "BE" }, // Bélgica
            { "EL", "LU" }, // Luxemburgo
            { "LS", "CH" }, // Suíça
            { "LO", "AT" }, // Áustria
            { "EK", "DK" }, // Dinamarca
            { "EN", "NO" }, // Noruega
            { "ES", "SE" }, // Suécia
            { "EF", "FI" }, // Finlândia
            { "EI", "IE" }, // Irlanda
            { "LG", "GR" }, // Grécia
            { "EP", "PL" }, // Polônia
            { "LK", "CZ" }, // República Tcheca
            { "LZ", "SK" }, // Eslováquia
            { "LH", "HU" }, // Hungria
            { "LR", "RO" }, // Romênia
            { "LB", "BG" }, // Bulgária
            { "LT", "TR" }, // Turquia
            { "LC", "CY" }, // Chipre
            { "LM", "MT" }, // Malta
            { "LA", "AL" }, // Albânia
            { "LD", "HR" }, // Croácia
            { "LJ", "SI" }, // Eslovênia
            { "LQ", "BA" }, // Bósnia e Herzegovina
            { "LY", "RS" }, // Sérvia / Montenegro
            { "LW", "MK" }, // Macedônia do Norte
            { "EE", "EE" }, // Estônia
            { "EV", "LV" }, // Letônia
            { "EY", "LT" }, // Lituânia
            { "UK", "UA" }, // Ucrânia
            { "UM", "BY" }, // Bielorrússia
            { "BI", "IS" }, // Islândia

            // Ásia, Oceania, Oriente Médio & África
            { "RJ", "JP" }, // Japão
            { "RO", "JP" }, // Japão (Okinawa)
            { "RK", "KR" }, // Coreia do Sul
            { "VH", "HK" }, // Hong Kong
            { "NZ", "NZ" }, // Nova Zelândia
            { "VT", "TH" }, // Tailândia
            { "VV", "VN" }, // Vietnã
            { "VD", "KH" }, // Camboja
            { "VL", "LA" }, // Laos
            { "VY", "MM" }, // Mianmar
            { "WM", "MY" }, // Malásia Peninsular
            { "WB", "MY" }, // Malásia Oriental
            { "WS", "SG" }, // Cingapura
            { "WI", "ID" }, // Indonésia (Jacarta)
            { "WA", "ID" }, // Indonésia (Ujung Pandang)
            { "WR", "ID" }, // Indonésia (Bali)
            { "WQ", "ID" }, // Indonésia
            { "RP", "PH" }, // Filipinas
            { "VO", "IN" }, // Índia
            { "VE", "IN" }, // Índia
            { "VI", "IN" }, // Índia
            { "VA", "IN" }, // Índia
            { "OP", "PK" }, // Paquistão
            { "VC", "LK" }, // Sri Lanka
            { "VR", "MV" }, // Maldivas
            { "VG", "BD" }, // Bangladesh
            { "VN", "NP" }, // Nepal
            { "OM", "AE" }, // Emirados Árabes Unidos
            { "OE", "SA" }, // Arábia Saudita
            { "OB", "BH" }, // Bahrein
            { "OT", "QA" }, // Catar
            { "OO", "OM" }, // Omã
            { "OY", "YE" }, // Iêmen
            { "OJ", "JO" }, // Jordânia
            { "LL", "IL" }, // Israel
            { "OS", "SY" }, // Síria
            { "OL", "LB" }, // Líbano
            { "OR", "IQ" }, // Iraque
            { "OI", "IR" }, // Irã
            { "HE", "EG" }, // Egito
            { "FA", "ZA" }  // África do Sul
        };

        /// <summary>
        /// Mapeamentos manuais canônicos para aeródromos conhecidos do simulador com dados incompletos.
        /// </summary>
        private static readonly Dictionary<string, (string City, string Country)> KnownAirportOverrides =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "SBWM", ("Ribeirão Branco", "BR") }
        };

        private static readonly Regex BrazilianStateSuffixRegex = new(
            @"[\s,\-\/]+(AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ParenthesesRegex = new(
            @"\s*\([^)]*\)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AviationPrefixRegex = new(
            @"^(?:aeroporto|aer[oó]dromo|campo\s+de\s+avia[cç][aã]o|base\s+a[eé]rea|pista(?:\s+de\s+pouso)?)\s+(?:internacional|regional|municipal|estadual|nacional|federal|comunit[aá]rio|executivo|militar|p[uú]blico|privado)?\s*(?:de|do|da|dos|das|d')?\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StandaloneAdjectivePrefixRegex = new(
            @"^(?:internacional|regional|municipal|estadual|nacional|federal|executivo)\s+(?:de|do|da|dos|das|d')?\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AviationSuffixRegex = new(
            @"\s+(?:international|intl|regional|municipal|executive|memorial|national|county|metropolitan|metro|estadual|internacional)?\s*(?:airport|airfield|airstrip|field|arp|aeroporto|aer[oó]dromo)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StandaloneAdjectiveSuffixRegex = new(
            @"\s+(?:municipal|regional|internacional|estadual|nacional|federal)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MultipleSpacesRegex = new(
            @"\s{2,}",
            RegexOptions.Compiled);

        /// <summary>
        /// Verifica se o código ICAO pertence ao espaço aéreo e território brasileiro.
        /// </summary>
        public static bool IsBrazilianIcao(string? icao)
        {
            if (string.IsNullOrWhiteSpace(icao) || icao.Length < 2)
                return false;

            return BrazilianIcaoPrefixes.Contains(icao.Substring(0, 2));
        }

        /// <summary>
        /// Verifica se uma sequência representa um código de FIR regional em vez de país ISO legítimo.
        /// </summary>
        public static bool IsFirCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            string c = code.Trim().ToUpperInvariant();
            if (c.Length == 2 && char.IsDigit(c[1]))
                return true;

            return KnownFirCodes.Contains(c);
        }

        /// <summary>
        /// Resolve o código ISO 3166-1 alpha-2 do país a partir do código ICAO do aeródromo e país atual.
        /// Garante que prefixos brasileiros sejam sempre associados a "BR" e nunca a códigos FIR como "SB".
        /// </summary>
        public static string ResolveCountryFromIcao(string? icao, string? currentCountry = null)
        {
            if (string.IsNullOrWhiteSpace(icao))
                return SanitizeCountry(currentCountry);

            string cleanIcao = icao.Trim().ToUpperInvariant();

            // 1. Aeródromos do Brasil têm prioridade absoluta
            if (IsBrazilianIcao(cleanIcao))
                return "BR";

            // 2. Prefixos de 2 letras
            if (cleanIcao.Length >= 2)
            {
                string p2 = cleanIcao.Substring(0, 2);
                if (TwoLetterIcaoToIsoCountry.TryGetValue(p2, out var iso2))
                    return iso2;
            }

            // 3. Prefixos de 1 letra (Estados Unidos, Canadá, Austrália, China, Rússia)
            if (cleanIcao.Length == 4)
            {
                char p1 = cleanIcao[0];
                switch (p1)
                {
                    case 'K':
                        return "US"; // EUA Continental (48 estados)
                    case 'C':
                        return "CA"; // Canadá
                    case 'Y':
                        return "AU"; // Austrália
                    case 'Z':
                        return "CN"; // China
                    case 'U':
                    case 'X':
                        return "RU"; // Rússia / CEI
                }
            }

            return SanitizeCountry(currentCountry);
        }

        private static string SanitizeCountry(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return string.Empty;

            string c = country.Trim().ToUpperInvariant();
            // Se for FIR ou código com dígitos, descarta
            if (IsFirCode(c))
                return string.Empty;

            return c.Length == 2 ? c : string.Empty;
        }

        /// <summary>
        /// Tenta extrair e higienizar o nome canônico do município a partir do nome formal do aeroporto.
        /// Aplica regras canônicas de aviação (ex: "Aeroporto de Ribeirão Branco" -> "Ribeirão Branco").
        /// </summary>
        /// <param name="airportName">Nome registrado do aeroporto.</param>
        /// <param name="city">Nome do município extraído com sucesso.</param>
        /// <returns>True se foi possível deduzir um município válido; caso contrário, False.</returns>
        public static bool TryExtractCityFromName(string? airportName, out string city)
        {
            city = string.Empty;
            if (string.IsNullOrWhiteSpace(airportName))
                return false;

            string text = airportName.Trim();

            // Se for idêntico a ICAO/IATA de até 4 caracteres alfanuméricos, não há município
            if (text.Length <= 4 && text.All(char.IsLetterOrDigit))
                return false;

            // 1. Remover anotações entre parênteses no final: "Alexânia (SWXX)", "Ribeirão Branco (Privado)"
            text = ParenthesesRegex.Replace(text, "").Trim();

            // 2. Remover sufixo de estado com vírgula ou hífen: "Alexânia, GO", "Ribeirão Branco - SP"
            text = BrazilianStateSuffixRegex.Replace(text, "").Trim();

            // 3. Remover prefixos aeronáuticos canônicos:
            // "Aeroporto Municipal de Alexânia" -> "Alexânia"
            // "Aeroporto de Ribeirão Branco" -> "Ribeirão Branco"
            // "Aeródromo Fazenda Chapadão" -> "Fazenda Chapadão"
            text = AviationPrefixRegex.Replace(text, "").Trim();
            text = StandaloneAdjectivePrefixRegex.Replace(text, "").Trim();

            // 4. Se houver barra separadora (ex: "Campinas / Viracopos" ou "Ribeirão Branco / SP")
            if (text.Contains('/'))
            {
                var parts = text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    if (parts[1].Length <= 3 && parts[1].All(char.IsLetter))
                    {
                        text = parts[0];
                    }
                    else if (!Regex.IsMatch(parts[0], @"\b(aeroporto|aer[oó]dromo|airport|field)\b", RegexOptions.IgnoreCase))
                    {
                        text = parts[0];
                    }
                    else
                    {
                        text = parts[1];
                    }
                }
            }

            // 5. Se houver hífen separador de nome de homenagem ou complemento:
            // "Sorocaba - Bertram Luiz Leupolz" -> "Sorocaba"
            // "Guarulhos - Gov. André Franco Montoro" -> "Guarulhos"
            if (text.Contains('-'))
            {
                var parts = text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    if (parts[1].Length <= 3 && parts[1].All(char.IsLetter))
                    {
                        text = parts[0];
                    }
                    else if (parts[0].Length >= 3)
                    {
                        text = parts[0];
                    }
                }
            }

            // 6. Remover sufixos aeronáuticos canônicos em inglês/português:
            // "Ribeirão Branco Airport" -> "Ribeirão Branco"
            // "Alexânia Municipal Airport" -> "Alexânia"
            // "Orlando Executive Airport" -> "Orlando"
            text = AviationSuffixRegex.Replace(text, "").Trim();
            text = StandaloneAdjectiveSuffixRegex.Replace(text, "").Trim();

            // 7. Normalizar espaços múltiplos
            text = MultipleSpacesRegex.Replace(text, " ").Trim();

            // 8. Validação final
            if (text.Length >= 2 && !Regex.IsMatch(text, @"^[0-9\W]+$"))
            {
                city = CapitalizeTitle(text);
                return true;
            }

            city = string.Empty;
            return false;
        }

        /// <summary>
        /// Aplica normalização completa a uma entidade <see cref="Airport"/>, saneando País, Município e Nome.
        /// </summary>
        /// <param name="airport">Entidade aeroportuária a ser normalizada.</param>
        /// <returns>A própria entidade para encadeamento fluente.</returns>
        public static Airport NormalizeAirport(Airport airport)
        {
            if (airport == null)
                throw new ArgumentNullException(nameof(airport));

            // 1. Verificação de overrides manuais canônicos (ex: SBWM)
            if (!string.IsNullOrEmpty(airport.ICAO) && KnownAirportOverrides.TryGetValue(airport.ICAO, out var ovr))
            {
                if (string.IsNullOrWhiteSpace(airport.City) || IsFirCode(airport.City) || airport.City.Equals(airport.Country, StringComparison.OrdinalIgnoreCase))
                {
                    airport.City = ovr.City;
                }
                airport.Country = ovr.Country;
            }
            else
            {
                // 2. Normalização do País (Country)
                airport.Country = ResolveCountryFromIcao(airport.ICAO, airport.Country);

                // 3. Normalização do Município (City)
                bool isCityEmptyOrInvalid = string.IsNullOrWhiteSpace(airport.City)
                    || IsFirCode(airport.City)
                    || (airport.City.Length <= 2 && airport.City.Equals(airport.Country, StringComparison.OrdinalIgnoreCase));

                if (isCityEmptyOrInvalid)
                {
                    if (TryExtractCityFromName(airport.Name, out var extractedCity))
                    {
                        airport.City = extractedCity;
                    }
                }
                else
                {
                    airport.City = CapitalizeTitle(airport.City.Trim());
                }
            }

            // 4. Normalização do Nome do Aeroporto
            if (!string.IsNullOrWhiteSpace(airport.Name))
            {
                airport.Name = MultipleSpacesRegex.Replace(airport.Name.Trim(), " ");
            }

            return airport;
        }

        /// <summary>
        /// Formata adequadamente um título em maiúsculas e minúsculas com preservação de conectivos em português.
        /// </summary>
        public static string CapitalizeTitle(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            bool isAllUpper = input.All(c => !char.IsLetter(c) || char.IsUpper(c));
            bool isAllLower = input.All(c => !char.IsLetter(c) || char.IsLower(c));
            if (!isAllUpper && !isAllLower)
                return input;

            var lowerConnectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "de", "do", "da", "dos", "das", "d'", "e", "em", "no", "na", "nos", "nas", "of", "the", "and"
            };

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToLowerInvariant();
                if (i > 0 && lowerConnectors.Contains(w))
                {
                    words[i] = w;
                }
                else if (w.Length > 0)
                {
                    words[i] = char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w.Substring(1) : "");
                }
            }
            return string.Join(" ", words);
        }
    }
}
