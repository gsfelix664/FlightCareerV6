using System.Collections.Generic;

namespace FlightCareer.Infrastructure.Services
{
    public struct DestinationProfile
    {
        public List<string> DomesticHubs { get; set; }
        public List<string> RegionalIntl { get; set; }
        public List<string> LongHaul { get; set; }
        public List<string> ExecutiveHubs { get; set; }
        public List<string> MaintenanceHubs { get; set; }
        public List<string> FeederHubs { get; set; }
        public List<string> RegionalPoints { get; set; }
        public List<string> AgroPoints { get; set; }
        public List<string> AgroTechPoints { get; set; }
        public List<string> InvestPoints { get; set; }
        public List<string> CargoHubs { get; set; }
        public List<string> EcommercePoints { get; set; }
        public List<string> AgroPartsPoints { get; set; }
        public List<string> GovPoints { get; set; }
        public List<string> HealthHubs { get; set; }
        public List<string> LeisurePoints { get; set; }
        public List<string> InstructionHubs { get; set; }
    }

    public static class G20Destinations
    {
        public static readonly Dictionary<string, DestinationProfile> Profiles = new Dictionary<string, DestinationProfile>
        {
            { "BR", new DestinationProfile 
            {
                DomesticHubs = new List<string> { 
                    "SBGR", "SBSP", "SBBR", "SBKP", "SBCF", "SBRJ", "SBGL", "SBRF", "SBSV", "SBPA", 
                    "SBCT", "SBFZ", "SBMO", "SBSG", "SBEG", "SBBE", "SBFL", "SBVT", "SBGO", "SBCY", 
                    "SBCG", "SBPS", "SBJP", "SBAR", "SBTE", "SBSL", "SBNF" 
                },
                RegionalIntl = new List<string> { 
                    "SAEZ", "SABE", "SCEL", "SUMU", "SPIM", "SKBO", "SGAS", "SLLP", "SLVR", "SACO", 
                    "SULS", "SEQM", "SPJC", "MPTO", "SKCG", "SEGU", "SAME", "SGES", "SPZO", "SKCL" 
                },
                LongHaul     = new List<string> { 
                    "KMIA", "KJFK", "KMCO", "KDFW", "KIAH", "KATL", "KORD", "CYYZ", "EGLL", "LFPG", 
                    "LPPT", "LPPR", "LEMD", "EDDF", "EHAM", "EBBR", "LSZH", "LIRF", "LTFM", "OMDB", 
                    "OTHH", "FACT", "FAOR", "HAAB", "FNLU", "VHHH", "MDPC", "KMEM", "KSDF", "PANC", "ELLX" 
                },
                ExecutiveHubs = new List<string> { 
                    "SBSP", "SBMT", "SBJH", "SBJD", "SBRJ", "SBJR", "SBBH", "SBBR", "SBCT", "SBCF", 
                    "SBRP", "SBGO", "SBSI", "SBMG", "SBLO", "SBML", "SBSJ", "SBKP" 
                },
                MaintenanceHubs = new List<string> { 
                    "SBSJ", "SBKP", "SBGL", "SBCF", "SBJD", "SBMG", "SDSC", "SDCO", "SBGR", "SBBH", 
                    "SBGO", "SBMK", "SBZM" 
                },
                FeederHubs = new List<string> { 
                    "SBGR", "SBKP", "SBBR", "SBCF", "SBPA", "SBRF", "SBGL", "SBSV", "SBCT", "SBFL", 
                    "SBFZ", "SBEG", "SBBE", "SBGO", "SBNT", "SBPS", "SBMO", "SBVT", "SBUL", "SBSL", 
                    "SBPJ", "SBJP", "SBMG", "SBNF" 
                },
                RegionalPoints = new List<string> { 
                    "SBRP", "SBLO", "SBJV", "SBUL", "SBUR", "SBMK", "SBZM", "SBMG", "SBPS", "SBIL", 
                    "SBCG", "SBCY", "SBFI", "SBSR", "SBSI", "SBCH", "SBPF", "SBNM", "SBAE", "SBDN", 
                    "SBAU", "SBML", "SBAQ", "SBPK", "SBSM", "SBUG", "SBCX", "SBCA", "SBSO", "SWRD", 
                    "SWRV", "SBJT", "SBIZ", "SBMA", "SBEK", "SBHT", "SBCJ", "SBPL", "SBJU", "SBKG", 
                    "SBVC", "SNBR", "SBME", "SBCB", "SBJA" 
                },
                AgroPoints = new List<string> { 
                    "SDTF", "SDJA", "SDAG", "SDEH", "SDUB", "SNMN", "SNPD", "SBAX", "SSCP", "SWTS", 
                    "SNFE", "SSKW", "SWRO", "SWJW", "SWRD", "SD8N", "SD8F", "SD7U", "SD7S", "SD7O", 
                    "SD7L", "SD7J", "SD7G", "SD6V", "SD6U", "SISW", "SDOL", "SWAF", "SWHE", "SNLJ", 
                    "SIZS", "SNLK", "SWAH", "SWQH", "SWNF", "SJHU", "SWZA", "SJXB", "SWXB", "SWVJ", 
                    "SIFW", "SJWI", "SJRJ", "SD4W", "SSHK", "SJWV", "SWGT", "SWGW", "SWHS", "SWIM", 
                    "SJRR", "SWFQ", "SIZM", "SWGU", "SJFL", "SNHG", "SJQZ", "SJPP", "SWKM", "SJIE", 
                    "SJKA", "SIUB", "SJSS", "SIZU", "SDWC" 
                },
                AgroTechPoints = new List<string> { 
                    "SBBG", "SDVH", "SSOK", "SBLO", "SBSJ", "SWNB", "SWRD", "SBDO", "SILC", "SDNM", 
                    "SBPP", "SWGI", "SBJD", "SWSI", "SBCT", "SBPJ", "SNBS", "SNGW", "SBRP", "SDAI", 
                    "SDSC", "SBUL", "SBGO", "SBKP", "SBPA", "SBFL", "SBMG", "SBCA", "SBPF", "SBSO", 
                    "SDVY", "SBBH", "SBML", "SBAE", "SBDN", "SBSR" 
                },
                InvestPoints = new List<string> { 
                    "SBSP", "SDCO", "SBRP", "SBAO", "SBUL", "SBSR", "SBDN", "SBVG", "SBTU", "SBLE", 
                    "SBCY", "SWSI", "SWRD", "SWMT", "SWRV", "SBGO", "SWIT", "SBCG", "SBDO", "SBCA", 
                    "SNBS", "SILC", "SDNM", "SBLO", "SNGW", "SBSO", "SWLC", "SWNB", "SWJW", "SWPY", 
                    "SWCP", "SNBR", "SBSI", "SBTG", "SBPJ", "SBAI", "SWGU", "SBVH", "SBJI", "SBMG", 
                    "SBTD", "SBGU", "SBPF", "SBSM", "SBUG", "SBER", "SBAU", "SBAE", "SBPR", "SNAM" 
                },
                CargoHubs = new List<string> { 
                    "SBGR", "SBKP", "SBEG", "SBGL", "SBBR", "SBRF", "SBSP", "SBFZ", "SBCF", "SBSV", 
                    "SBCT", "SBPA", "SBBE", "SBPL", "SBCB", "SBSJ", "SBVT", "SBJV", "SBPV", "SBBV", 
                    "SBFI", "SBGO", "SBMG", "SBNF" 
                },
                EcommercePoints = new List<string> { 
                    "SBKP", "SBGR", "SBSP", "SBBR", "SBCT", "SBPA", "SBRF", "SBSV", "SBFZ", "SBEG", 
                    "SBBE", "SBGL", "SBBH", "SBCF", "SBJD", "SBUL", "SBGO", "SBMG", "SBLO", "SBVT", 
                    "SBJV", "SBFL", "SBPS", "SBNF", "SBSR", "SBDN", "SBAU", "SBML", "SDCO", "SBBI" 
                },
                AgroPartsPoints = new List<string> { 
                    "SWRV", "SSCM", "SDZJ", "SWVE", "SBCT", "SBRP", "SBSR", "SBLO", "SBAO", "SBSJ", 
                    "SDAI", "SWNB", "SWSI", "SBDO", "SILC", "SBKP", "SBJD", "SBAQ", "SBCX", "SBPA", 
                    "SBUL", "SBGO", "SBSO", "SWPY", "SSPT", "SBML", "SBAE", "SBDN", "SBAU", "SBPF" 
                },
                GovPoints = new List<string> { 
                    "SBBR", "SBSP", "SBRJ", "SBCF", "SBBH", "SBPA", "SBCT", "SBSV", "SBRF", "SBGO", 
                    "SBCG", "SBCY", "SBEG", "SBBE", "SBFL", "SBVT", "SBFZ", "SBMO", "SBNT", "SBSG", 
                    "SBJP", "SBAR", "SBTE", "SBSL", "SBPJ", "SBRB", "SBMQ", "SBPV", "SBBV" 
                },
                HealthHubs = new List<string> { 
                    "SBGR", "SBSP", "SBRJ", "SBGL", "SBBR", "SBCF", "SBPA", "SBCT", "SBRF", "SBSV", 
                    "SBGO", "SBEG", "SBBE", "SBFL", "SBKP", "SBMT", "SBBH", "SBFZ", "SBVT", "SBSL", 
                    "SBMO", "SBAR", "SBSG", "SBNT", "SBUL", "SBSM", "SBSI", "SBRP" 
                },
                LeisurePoints = new List<string> { 
                    "SBPS", "SBSV", "SBFL", "SBNF", "SBRJ", "SBMO", "SBFZ", "SBJP", "SBNT", "SBBZ", 
                    "SBPC", "SBMT", "SBJH", "SBFN", "SBFI", "SBDB", "SBJE", "SBTR", "SBIL", "SBLE", 
                    "SBCN", "SDCO", "SDPY" 
                },
                InstructionHubs = new List<string> { 
                    "SBMT", "SBJD", "SBJR", "SBBI", "SBBH", "SBLO", "SBML", "SBSI", "SBAU", "SBBU", 
                    "SBRP", "SBGO", "SBCF" 
                }
            }},
            { "US", new DestinationProfile 
            {
                DomesticHubs = new List<string> { 
                    "KATL", "KORD", "KLAX", "KDFW", "KJFK", "KDEN", "KSFO", "KMCO", "KLAS", "KPHX", 
                    "KBOS", "KCLT", "KMIA", "KIAH", "KEWR", "KFLL", "KSEA", "KMSP", "KDTW", "KPHL", 
                    "KSLP", "KBWI", "KSLC", "KSAN", "KMDW", "KTPA", "KPDX", "KSTL", "KHOU" 
                },
                RegionalIntl = new List<string> { 
                    "CYYZ", "CYVR", "CYUL", "CYQB", "CYXE", "CYQR", "CYYJ", "MMMX", "MMUN", "MMGL", 
                    "MMSD", "MMPV", "MMTL", "MMMY", "MMTJ", "TJSJ", "MDPC", "MDSD", "MKJS", "MKJP", 
                    "MYNN", "MTPP", "TNCM", "TFFF", "TAPA", "TLPC", "TGPY", "TKPK", "TXKF", "MROC", 
                    "MGGT", "MNMG", "MZBZ", "MSLP", "MHLM", "MHTG", "MHLC", "MPTO" 
                },
                LongHaul     = new List<string> { 
                    "EGLL", "LFPG", "EHAM", "EDDF", "EDDM", "LSZH", "EIDW", "LEMD", "LGAV", "LIPZ", 
                    "LFMN", "EGPH", "LEBL", "LIRN", "LPFR", "BIKF", "RJTT", "RJAA", "RKSI", "RCTP", 
                    "VHHH", "ZBAA", "ZSAM", "OMDB", "OTHH", "OBBI", "OMAA", "LLBG", "GOOY", "FACT", 
                    "FAOR", "YSSY", "NZAA", "SBGR", "SBGL", "SAEZ", "SCEL", "SKBO", "KMEM", "KSDF", "PANC" 
                },
                ExecutiveHubs = new List<string> { 
                    "KTEB", "KVNY", "KPWK", "KHPN", "KDAL", "KBUR", "KSNA", "KAPA", "KPBI", "KIAD", 
                    "KLAS", "KOPF", "KSDL", "KHOU", "KPDK", "KAPF", "KAUS", "KFLL", "KMDW", "KBNA", 
                    "KSLC", "KBED", "KDVT", "KMYF", "KPRC", "KBJC", "KLGB", "KHIO", "KFFZ", "KCHD" 
                },
                MaintenanceHubs = new List<string> { 
                    "KROW", "KVCV", "KSBD", "KGYR", "KMCI", "KAFW", "KTUL", "KOKC", "KATL", "KSFB", 
                    "KLAL", "KAVQ", "KFLL", "KBCT", "KOPF", "KSRQ", "KINT", "KOSC", "KMOB", "KAMA" 
                },
                FeederHubs = new List<string> { 
                    "KATL", "KORD", "KDFW", "KDEN", "KCLT", "KDTW", "KMSP", "KPHL", "KPHX", "KSLC", 
                    "KSEA", "KLAX", "KSFO", "KMIA", "KJFK", "KLGA", "KEWR", "KIAH", "KBOS", "KIAD", 
                    "KDCA", "KLAS", "KMCO", "KPDX", "KSAN", "KTPA", "KSTL", "KMDW", "KCVG", "KIND" 
                },
                RegionalPoints = new List<string> { 
                    "KSNA", "KBUR", "KIND", "KCVG", "KPIT", "KSDF", "KMEM", "KBNA", "KRSW", "KPBI", 
                    "KCMH", "KRDU", "KAUS", "KSAT", "KSMF", "KSJC", "KMKE", "KBUF", "KJAX", "KCHS", 
                    "KSAV", "KGRR", "KDSM", "KOMA", "KRNO", "KBOI", "KTUS", "KGEG", "KABQ", "KOKC" 
                },
                AgroPoints = new List<string> { 
                    "KDSM", "KOMA", "KICT", "KLNK", "KFSD", "KGFK", "KFAR", "KBIS", "KAMA", "KLBB", 
                    "KOKC", "KLAW", "KGLD", "KGCK", "KDDC", "KHYS", "KRSL", "KSLN" 
                },
                AgroTechPoints = new List<string> { 
                    "KSTL", "KDSM", "KCMH", "KRDU", "KSFO", "KSJC", "KBOS", "KORD", "KLAX", "KMSP", 
                    "KIND", "KCID", "KMLI", "KDBQ", "KSUX" 
                },
                InvestPoints = new List<string> { 
                    "KJFK", "KORD", "KDFW", "KHOU", "KIAH", "KCLT", "KATL", "KSFO", "KDEN", "KSTP", 
                    "KMSP", "KSTL", "KOKC", "KOMA", "KDSM" 
                },
                CargoHubs = new List<string> { 
                    "KMEM", "KSDF", "KCVG", "PANC", "KMIA", "KORD", "KLAX", "KDFW", "KJFK", "KEWR", 
                    "KIND", "KONT", "KAFW", "KSBD", "KPHL", "KATL" 
                },
                EcommercePoints = new List<string> { 
                    "KCVG", "KAFW", "KSBD", "KONT", "KORD", "KATL", "KDFW", "KJFK", "KMCO", "KMIA", 
                    "KPHX", "KSEA", "KMSP", "KDTW", "KPHL", "KCLT" 
                },
                AgroPartsPoints = new List<string> { 
                    "KMLI", "KDBQ", "KCID", "KALO", "KORD", "KDSM", "KIND", "KSDF", "KSTL", "KOMA" 
                },
                GovPoints = new List<string> { 
                    "KIAD", "KDCA", "KBWI", "KALB", "KATL", "KAUG", "KAUS", "KBTR", "KBIS", 
                    "KBOI", "KBOS", "KCXP", "KCRW", "KCYS", "KCAE", "KCMH", "KCON", "KDEN", "KDSM", 
                    "KDOV", "KFFT", "KMDT", "KBDL", "KHLN", "PHNL", "KIND", "KJAN", "KJEF", "PAJN", 
                    "KLAN", "KLNK", "KLIT", "KMSN", "KMXF", "KMPV", "KBNA", "KOKC", "KOLM", "KPHX", 
                    "KPIR", "KPVD", "KRDU", "KRIC", "KSMF", "KSLE", "KSLC", "KSAF", "KSPI", "KTLH", 
                    "KFOE", "KTTN" 
                },
                HealthHubs = new List<string> { 
                    "KRST", "KCLE", "KHOU", "KBOS", "KJFK", "KLAX", "KORD", "KATL", "KPHX", "KSEA", 
                    "KMSP", "KDEN", "KSFO", "KDFW" 
                },
                LeisurePoints = new List<string> { 
                    "KMCO", "KLAS", "KMIA", "KFLL", "KSFO", "KLAX", "KSEA", "KBOS", "KBZN", "KPDX", 
                    "KASE", "KEGE", "KGUC", "KJAC", "KEYW", "KMYR", "KACK", "KMVY", "KPVC" 
                }
            }},
            { "GB", new DestinationProfile 
            {
                DomesticHubs = new List<string> { 
                    "EGLL", "EGKK", "EGCC", "EGSS", "EGGW", "EGPH", "EGBB", "EGPF", "EGGD", "EGAA", 
                    "EGNT", "EGNX", "EGGP", "EGNM", "EGLC", "EGHI", "EGPD", "EGAC", "EGFF", "EGMC" 
                },
                RegionalIntl = new List<string> { 
                    "LFPG", "EHAM", "EDDF", "LEMD", "LIRF", "EIDW", "EBBR", "LSZH", "LSGG", "EKCH", 
                    "EDDM", "LPPT", "LEBL", "LIPZ", "LFMN", "LGAV", "ESSA", "ENGM", "EFHK", "LOWW" 
                },
                LongHaul     = new List<string> { 
                    "KJFK", "KLAX", "KMIA", "KORD", "KSFO", "CYYZ", "OMDB", "OTHH", "VHHH", "WSSS", 
                    "RJTT", "FACT", "FAOR", "SBGR", "SBGL", "SAEZ", "YSSY", "NZAA", "ELLX", "KMEM" 
                },
                ExecutiveHubs = new List<string> { 
                    "EGLF", "EGLC", "EGKB", "EGTK", "EGWU", "EGGW", "EGSS", "EGCC", "EGPH", "EGBB", 
                    "EGGD", "EGHI" 
                },
                MaintenanceHubs = new List<string> { 
                    "EGPK", "EGHL", "EGBP", "EGTK", "EGKB", "EGSS", "EGGD", "EGMC", "EGSH", "EGAA" 
                },
                FeederHubs = new List<string> { 
                    "EGLL", "EGKK", "EGCC", "EGBB", "EGPH", "EGPF", "EGGD", "EGNT", "EGAA", "EGNX" 
                },
                RegionalPoints = new List<string> { 
                    "EGNT", "EGAA", "EGNV", "EGGP", "EGFF", "EGNM", "EGHI", "EGPD", "EGAC", "EGTE", 
                    "EGHH", "EGHQ", "EGSH", "EGPE", "EGPC", "EGPA", "EGPL", "EGPO", "EGJJ", "EGJB" 
                },
                AgroPoints = new List<string> { 
                    "EGCL", "EGSV", "EGNW", "EGXW", "EGHR", "EGTR", "EGLP", "EGLS", "EGHA", "EGBN" 
                },
                AgroTechPoints = new List<string> { 
                    "EGSC", "EGTK", "EGSH", "EGLF", "EGBB", "EGCC", "EGPH" 
                },
                InvestPoints = new List<string> { 
                    "EGLC", "EGLL", "EGPH", "EGCC", "EGBB" 
                },
                CargoHubs = new List<string> { 
                    "EGNX", "EGSS", "EGLL", "EGPK", "EGCC", "EGAA", "EGSS" 
                },
                EcommercePoints = new List<string> { 
                    "EGNX", "EGCC", "EGBB", "EGSS", "EGAA", "EGGD", "EGPH" 
                },
                AgroPartsPoints = new List<string> { 
                    "EGBB", "EGCC", "EGNX", "EGNM", "EGSC", "EGPH" 
                },
                GovPoints = new List<string> { 
                    "EGLL", "EGLC", "EGWU", "EGPH", "EGFF", "EGAC", "EGAA" 
                },
                HealthHubs = new List<string> { 
                    "EGLL", "EGKK", "EGLC", "EGCC", "EGBB", "EGPF", "EGPH", "EGAA" 
                },
                LeisurePoints = new List<string> { 
                    "EGHQ", "EGJJ", "EGJB", "EGNS", "EGPE", "EGPC", "EGPU", "EGPR", "EGHH", "EGKA" 
                }
            }},
            { "FR", new DestinationProfile 
            {
                DomesticHubs = new List<string> { 
                    "LFPG", "LFPO", "LFMN", "LFLL", "LFML", "LFBZ", "LFBD", "LFRS", "LFST", "LFQQ", 
                    "LFKJ", "LFKB", "LFKF", "LFSB", "LFRB", "LFMT", "LFOB" 
                },
                RegionalIntl = new List<string> { 
                    "EGLL", "EGKK", "EHAM", "EDDF", "LEMD", "LIRF", "EBBR", "LSZH", "LPPT", "LEBL", 
                    "EDDM", "LSGG", "EKCH", "LIPZ", "LGAV", "ESSA", "LOWW", "DTTA", "GMAD", "GMMN" 
                },
                LongHaul     = new List<string> { 
                    "KJFK", "KEWR", "KMIA", "KLAX", "KORD", "CYUL", "CYYZ", "SBGR", "SBGL", "SAEZ", 
                    "SCEL", "SKBO", "OMDB", "OTHH", "RJTT", "VHHH", "WSSS", "FAOR", "FLLF", "SOCA", 
                    "TFFF", "TTPP", "HAAB", "HKJK" 
                },
                ExecutiveHubs = new List<string> { 
                    "LFPB", "LFMN", "LFMD", "LFTH", "LFKF", "LFLP", "LFPV", "LFBD", "LFML", "LFLL" 
                },
                MaintenanceHubs = new List<string> { 
                    "LFBO", "LFCH", "LFBX", "LFML", "LFMT", "LFLX", "LFOK", "LFLS", "LFPB", "LFPG" 
                },
                FeederHubs = new List<string> { 
                    "LFPG", "LFPO", "LFMN", "LFLL", "LFML", "LFBO", "LFRS", "LFST", "LFBD" 
                },
                RegionalPoints = new List<string> { 
                    "LFBO", "LFML", "LFRS", "LFSB", "LFQQ", "LFRN", "LFRB", "LFMT", "LFST", "LFRG", 
                    "LFBE", "LFBZ", "LFMK", "LFLC", "LFOH", "LFRH", "LFOT", "LFOA" 
                },
                AgroPoints = new List<string> { 
                    "LFOI", "LFOE", "LFOK", "LFAY", "LFAG", "LFAL", "LFAM", "LFAP", "LFAV", "LFAW" 
                },
                AgroTechPoints = new List<string> { 
                    "LFBO", "LFLL", "LFMT", "LFST", "LFBD", "LFPO" 
                },
                InvestPoints = new List<string> { 
                    "LFPG", "LFPO", "LFPB", "LFLL", "LFMN", "LFML" 
                },
                CargoHubs = new List<string> { 
                    "LFPG", "LFLL", "LFML", "LFPO", "LFOK", "LFBD" 
                },
                EcommercePoints = new List<string> { 
                    "LFPG", "LFLL", "LFML", "LFPO", "LFQQ", "LFST", "LFBD" 
                },
                AgroPartsPoints = new List<string> { 
                    "LFBO", "LFLL", "LFQQ", "LFST", "LFBD", "LFRS" 
                },
                GovPoints = new List<string> { 
                    "LFPG", "LFPO", "LFPB", "LFLL", "LFML", "LFBO", "LFBD", "LFRS", "LFST", "LFQQ" 
                },
                HealthHubs = new List<string> { 
                    "LFPG", "LFPO", "LFLL", "LFQQ", "LFBO", "LFST", "LFML", "LFBD", "LFRS" 
                },
                LeisurePoints = new List<string> { 
                    "LFMN", "LFMD", "LFTH", "LFKJ", "LFKB", "LFKF", "LFBZ", "LFBE", "LFRG", "LFAT", 
                    "LFRN", "LFMT", "LFLB", "LFLP" 
                }
            }},
            { "PT", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "LPPT", "LPPR", "LPFR", "LPMA", "LPHR", "LPLA" },
                RegionalIntl = new List<string> { "LEMD", "LEBL", "LFPG", "EGLL", "EDDF", "LIRF", "LPPS" },
                LongHaul     = new List<string> { "SBGR", "SBGL", "KMIA", "EWR", "VHHH", "FAOR" },
                ExecutiveHubs = new List<string> { "LPCS", "LPPT", "LPPR", "LPFR" },
                MaintenanceHubs = new List<string> { "LPBJ", "LPOV", "LPPT" },
                FeederHubs = new List<string> { "LPPT", "LPPR" },
                RegionalPoints = new List<string> { "LPFR", "LPMA", "LPPD" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "AR", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "SABE", "SAEZ", "SACO", "SAME", "SAZS", "SANT" },
                RegionalIntl = new List<string> { "SBGR", "SCEL", "SUMU", "SGAS", "SPIM", "SBGL", "SBCT" },
                LongHaul     = new List<string> { "KMIA", "LEMD", "LIRF", "KJFK", "EGLL", "LFPG" },
                ExecutiveHubs = new List<string> { "SADP", "SADF", "SABE", "SACO" },
                MaintenanceHubs = new List<string> { "SAEZ", "SACO", "SARC" },
                FeederHubs = new List<string> { "SABE", "SAEZ", "SACO" },
                RegionalPoints = new List<string> { "SAZS", "SANT", "SAZY", "SAVC" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "CA", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "CYYZ", "CYVR", "CYUL", "CYYC", "CYOW", "CYHZ", "CYEG" },
                RegionalIntl = new List<string> { "KJFK", "KORD", "KLAX", "MMUN", "MWCR", "KMIA", "KBOS", "KSFO" },
                LongHaul     = new List<string> { "EGLL", "LFPG", "VHHH", "RJTT", "EDDF", "ZSPD" },
                ExecutiveHubs = new List<string> { "CYTZ", "CYHU", "CYBW", "CYKZ" },
                MaintenanceHubs = new List<string> { "CYMX", "CYXU", "CYQM" },
                FeederHubs = new List<string> { "CYYZ", "CYVR", "CYUL", "CYYC" },
                RegionalPoints = new List<string> { "CYQB", "CYXE", "CYQR", "CYYJ" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "MX", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "MMMX", "MMUN", "MMGL", "MMMY", "MMTJ", "MMCUN" },
                RegionalIntl = new List<string> { "KLAX", "KMIA", "KIAH", "MPTO", "SKBO", "MSLP", "MGGT" },
                LongHaul     = new List<string> { "LEMD", "LFPG", "RJTT", "SBGR", "EGLL", "EDDF" },
                ExecutiveHubs = new List<string> { "MMTO", "MMAN", "MMHO", "MMCZ" },
                MaintenanceHubs = new List<string> { "MMGL", "MMMY", "MMMX" },
                FeederHubs = new List<string> { "MMMX", "MMGL", "MMMY" },
                RegionalPoints = new List<string> { "MMCE", "MMCZ", "MMPS", "MMVA" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "DE", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "EDDF", "EDDM", "EDDB", "EDDH", "EDDL", "EDDS", "EDDN" },
                RegionalIntl = new List<string> { "EGLL", "LFPG", "LIRF", "LTFM", "UUEE", "LOWW", "LSZH", "EBBR", "EHAM" },
                LongHaul     = new List<string> { "KJFK", "ZSPD", "SBGR", "FAOR", "RJTT", "WSSS", "KLAX", "OMDB", "ELLX" },
                ExecutiveHubs = new List<string> { "EDDB", "EDDM", "EDDS", "EDKB", "EDVK" },
                MaintenanceHubs = new List<string> { "EDDP", "EDDC", "EDDW" },
                FeederHubs = new List<string> { "EDDF", "EDDM", "EDDL", "EDDH" },
                RegionalPoints = new List<string> { "EDDC", "EDDN", "EDDW", "EDLP" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "IT", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "LIRF", "LIMC", "LIPZ", "LIRN", "LIME", "LIPE", "LICJ" },
                RegionalIntl = new List<string> { "LFPG", "EDDF", "EGLL", "LEMD", "LTFM", "LSZH", "EBBR", "LOWW" },
                LongHaul     = new List<string> { "KJFK", "SBGR", "RJTT", "OMDB", "FAOR", "EZEA" },
                ExecutiveHubs = new List<string> { "LIRA", "LIML", "LIPH", "LIRN" },
                MaintenanceHubs = new List<string> { "LIPX", "LIPQ", "LIRF" },
                FeederHubs = new List<string> { "LIRF", "LIMC", "LIRN" },
                RegionalPoints = new List<string> { "LIPZ", "LIRP", "LIBR", "LICJ" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "RU", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "UUEE", "ULLI", "UNNT", "URSS", "UUDD", "UUWW", "UWWW" },
                RegionalIntl = new List<string> { "LTFM", "OMDB", "ZBAA", "EDDF", "EVRA", "UAAA", "UKBB" },
                LongHaul     = new List<string> { "MUHA", "VTBS", "VIGG", "HKJK", "WSSS", "RJTT" },
                ExecutiveHubs = new List<string> { "UUWW", "UUDD", "ULLI", "URSS" },
                MaintenanceHubs = new List<string> { "UUBW", "UNNT", "UUEE" },
                FeederHubs = new List<string> { "UUEE", "UUDD", "ULLI" },
                RegionalPoints = new List<string> { "UWWW", "USSS", "URMM", "UWKD" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "CN", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "ZBAA", "ZSPD", "ZGGG", "ZUUU", "ZSSS", "ZUCK", "ZHHH" },
                RegionalIntl = new List<string> { "RKSI", "RJTT", "VHHH", "WSSS", "VTBS", "RCTP", "RPLL", "WMKK" },
                LongHaul     = new List<string> { "KLAX", "KJFK", "EGLL", "EDDF", "YSSY", "SBGR", "LSZH", "LFPG" },
                ExecutiveHubs = new List<string> { "ZSSS", "ZBAD", "ZGSN", "ZGOW" },
                MaintenanceHubs = new List<string> { "ZBTJ", "ZSAM", "ZJSY" },
                FeederHubs = new List<string> { "ZBAA", "ZSPD", "ZGGG" },
                RegionalPoints = new List<string> { "ZUCK", "ZHHH", "ZSAM", "ZBTJ" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "JP", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "RJTT", "RJAA", "RJBB", "RJCC", "RJGG", "RJFF", "RJOO" },
                RegionalIntl = new List<string> { "RKSI", "ZSPD", "RCTP", "VHHH", "PGUM", "RPLL", "ZGGG" },
                LongHaul     = new List<string> { "KLAX", "KJFK", "EGLL", "LFPG", "YSSY", "EDDF", "SBGR", "VABB" },
                ExecutiveHubs = new List<string> { "RJTI", "RJOY", "RJNK", "RJTF" },
                MaintenanceHubs = new List<string> { "RJAA", "RJBB", "RJCC" },
                FeederHubs = new List<string> { "RJTT", "RJOO", "RJFF" },
                RegionalPoints = new List<string> { "RJSN", "RJSK", "RJOT", "RJOK" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "KR", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "RKSI", "RKPC", "RKPK", "RKSS" },
                RegionalIntl = new List<string> { "RJTT", "ZBAA", "VHHH", "RCTP", "VVTS", "WSSS", "RJBB" },
                LongHaul     = new List<string> { "KLAX", "KJFK", "EGLL", "EDDF", "LFPG", "YSSY" },
                ExecutiveHubs = new List<string> { "RKSS", "RKPC", "RKJJ" },
                MaintenanceHubs = new List<string> { "RKSI", "RKPK" },
                FeederHubs = new List<string> { "RKSI", "RKSS", "RKPK" },
                RegionalPoints = new List<string> { "RKNY", "RKNW", "RKTH" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "IN", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "VIDP", "VABB", "VOBL", "VOMM", "VECC", "VOHS", "VAAH" },
                RegionalIntl = new List<string> { "OMDB", "WSSS", "VTBS", "VCBI", "VNKT", "OTHH", "VABB" },
                LongHaul     = new List<string> { "EGLL", "KJFK", "YSSY", "CYYZ", "LFPG", "EDDF", "FAOR" },
                ExecutiveHubs = new List<string> { "VAJJ", "VIDP", "VABB", "VOBL" },
                MaintenanceHubs = new List<string> { "VABP", "VOHY", "VIJP" },
                FeederHubs = new List<string> { "VIDP", "VABB", "VOBL" },
                RegionalPoints = new List<string> { "VOCI", "VOGO", "VOPB", "VIJP" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "ID", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "WIII", "WADD", "WARR", "WALL", "WAAA", "WIPP" },
                RegionalIntl = new List<string> { "WSSS", "WMKK", "YBBN", "RPLL", "VTBS", "VHHH", "YSSY" },
                LongHaul     = new List<string> { "EHAM", "OMDB", "RJTT", "LTFM", "EGLL", "LFPG" },
                ExecutiveHubs = new List<string> { "WIHH", "WADD", "WARS" },
                MaintenanceHubs = new List<string> { "WABD", "WIII" },
                FeederHubs = new List<string> { "WIII", "WADD" },
                RegionalPoints = new List<string> { "WABB", "WARR", "WAAA" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "SA", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "OEJN", "OERK", "OEDF", "OEMA", "OEAH", "OETB" },
                RegionalIntl = new List<string> { "OMDB", "HECA", "LTFM", "OBBI", "OKBK", "OTHH", "OMAA" },
                LongHaul     = new List<string> { "EGLL", "KJFK", "LFPG", "WIII", "RPLL", "EDDF", "VABB" },
                ExecutiveHubs = new List<string> { "OERY", "OEJN", "OERK" },
                MaintenanceHubs = new List<string> { "OEMA", "OEDF" },
                FeederHubs = new List<string> { "OEJN", "OERK" },
                RegionalPoints = new List<string> { "OEAH", "OETB", "OEDM" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "TR", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "LTFM", "LTFJ", "LTAI", "LTAC", "LTBJ", "LTAF", "LTCG" },
                RegionalIntl = new List<string> { "EDDF", "UUEE", "OMDB", "HECA", "LIRF", "LGAV", "EHAM", "EBBR" },
                LongHaul     = new List<string> { "KJFK", "KLAX", "RJTT", "SBGR", "FACT", "VHHH", "WSSS" },
                ExecutiveHubs = new List<string> { "LTBA", "LTFJ", "LTAC", "LTBS" },
                MaintenanceHubs = new List<string> { "LTBW", "LTAH", "LTFM" },
                FeederHubs = new List<string> { "LTFM", "LTAC" },
                RegionalPoints = new List<string> { "LTBJ", "LTAI", "LTAF" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "AU", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "YSSY", "YMML", "YBBN", "YPPH", "YPAD", "YBCS", "YSCB" },
                RegionalIntl = new List<string> { "NZAA", "WSSS", "WADD", "NFFN", "RPLL", "AYPY", "NFTF" },
                LongHaul     = new List<string> { "KLAX", "EGLL", "OMDB", "RJTT", "KJFK", "VHHH", "SBGR" },
                ExecutiveHubs = new List<string> { "YSSY", "YMEN", "YBAF", "YJTJ" },
                MaintenanceHubs = new List<string> { "YBTL", "YAMB", "YPJT" },
                FeederHubs = new List<string> { "YSSY", "YMML", "YBBN" },
                RegionalPoints = new List<string> { "YBCS", "YPAD", "YPPH", "YMHB" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "ZA", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "FAOR", "FACT", "FALE", "FAPE", "FABL" },
                RegionalIntl = new List<string> { "HKJK", "FLLF", "FIMP", "FVHA", "FNLU", "FQMA", "FBSK" },
                LongHaul     = new List<string> { "EGLL", "KJFK", "YSSY", "SBGR", "EDDF", "LFPG", "RJTT" },
                ExecutiveHubs = new List<string> { "FALA", "FAOR", "FACT", "FALE" },
                MaintenanceHubs = new List<string> { "FALA", "FAOR" },
                FeederHubs = new List<string> { "FAOR", "FACT" },
                RegionalPoints = new List<string> { "FAPE", "FABL", "FAKM" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }},
            { "ES", new DestinationProfile 
            {
                DomesticHubs = new List<string> { "LEMD", "LEBL", "LEPA", "LEAG", "LEMG", "LEVC", "LEBB" },
                RegionalIntl = new List<string> { "LFPG", "EGLL", "LPPT", "LIRF", "EDDF", "GMAD" },
                LongHaul     = new List<string> { "KJFK", "SBGR", "SAEZ", "MMMX", "SKBO" },
                ExecutiveHubs = new List<string> { "LETO", "LELL", "LECU", "LEVC" },
                MaintenanceHubs = new List<string> { "LEZL", "LEGT", "LEMD" },
                FeederHubs = new List<string> { "LEMD", "LEBL" },
                RegionalPoints = new List<string> { "LEPA", "LEAG", "LEMG", "LEVC" },
                AgroPoints = new List<string>(),
                AgroTechPoints = new List<string>(),
                InvestPoints = new List<string>(),
                CargoHubs = new List<string>(),
                EcommercePoints = new List<string>(),
                AgroPartsPoints = new List<string>(),
                GovPoints = new List<string>(),
                HealthHubs = new List<string>(),
                LeisurePoints = new List<string>()
            }}
        };

        public static DestinationProfile? GetProfile(string countryCode)
        {
            if (Profiles.TryGetValue(countryCode, out var profile))
                return profile;
            
            return null;
        }
    }
}
