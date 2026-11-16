using System;
using System.IO;

namespace FlightCareer.SimVarProbe
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var config = new ProbeSessionConfiguration();

            // Resolução do caminho padrão da lista
            string defaultListPath = Path.Combine(AppContext.BaseDirectory, "Data", "simvar_probe_list.csv");
            if (!File.Exists(defaultListPath))
            {
                // Fallback para árvore de desenvolvimento
                string devPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FlightCareer.Infrastructure", "Data", "simvar_probe_list.csv"));
                if (File.Exists(devPath))
                {
                    defaultListPath = devPath;
                }
            }
            config.ListPath = defaultListPath;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase))
                {
                    ShowHelp();
                    return 0;
                }

                if (string.Equals(arg, "--list", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    config.ListPath = args[++i];
                }
                else if (string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    config.OutputDirectory = args[++i];
                }
                else if (string.Equals(arg, "--duration", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int dur) && dur > 0)
                    {
                        config.DurationSeconds = dur;
                    }
                }
                else if (string.Equals(arg, "--max-engine", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val >= 1)
                    {
                        config.MaxEngine = val;
                    }
                }
                else if (string.Equals(arg, "--max-contact", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val >= 0)
                    {
                        config.MaxContact = val;
                    }
                }
                else if (string.Equals(arg, "--max-generic", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val >= 0)
                    {
                        config.MaxGeneric = val;
                    }
                }
                else if (string.Equals(arg, "--filter", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    config.Filter = args[++i];
                }
                else if (string.Equals(arg, "--probe-only", StringComparison.OrdinalIgnoreCase))
                {
                    config.ProbeOnly = true;
                }
                else if (string.Equals(arg, "--probe-timeout", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val > 0)
                    {
                        config.ProbeTimeoutSeconds = val;
                    }
                }
                else if (string.Equals(arg, "--block-size", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val > 0)
                    {
                        config.BlockSize = val;
                    }
                }
                else if (string.Equals(arg, "--interval-frames", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int val) && val > 0)
                    {
                        config.IntervalFrames = val;
                    }
                }
            }

            using var session = new ProbeSession(config);

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[SimVarProbe] Interrupção por Ctrl+C recebida. Encerrando de forma segura...");
                session.Stop();
            };

            return session.Run();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("FlightCareer SimVarProbe — Ferramenta de sondagem e gravação de SimVars");
            Console.WriteLine("Uso: FlightCareer.SimVarProbe [opções]");
            Console.WriteLine();
            Console.WriteLine("Opções:");
            Console.WriteLine("  --list <caminho>          Caminho para o CSV de entrada de SimVars.");
            Console.WriteLine("                            (Padrão: Data/simvar_probe_list.csv)");
            Console.WriteLine("  --out <pasta>             Diretório raiz para gravação dos logs.");
            Console.WriteLine("                            (Padrão: %LocalAppData%\\FlightCareer\\probe-logs\\)");
            Console.WriteLine("  --duration <segundos>     Duração máxima da gravação na Fase B.");
            Console.WriteLine("                            (Opcional; padrão: grava até 'q'+Enter ou Ctrl+C)");
            Console.WriteLine("  --max-engine <número>     Contagem máxima de motores para expansão :1..N (Padrão: 4)");
            Console.WriteLine("  --max-contact <número>    Contagem máxima de pontos de contato :0..N (Padrão: 5)");
            Console.WriteLine("  --max-generic <número>    Contagem máxima de índices genéricos :0..N (Padrão: 4)");
            Console.WriteLine("  --filter <regex>          Filtro regex opcional aplicado aos nomes das SimVars.");
            Console.WriteLine("  --probe-only              Executa apenas a Fase A (validação de nomes) e sai.");
            Console.WriteLine("  --probe-timeout <seg>     Tempo limite total para a sondagem na Fase A (Padrão: 15 s)");
            Console.WriteLine("  --block-size <número>     Tamanho máximo de cada bloco numérico (Padrão: 200)");
            Console.WriteLine("  --interval-frames <N>     Usa SIM_FRAME com intervalo de N frames em vez de 1 segundo.");
            Console.WriteLine("  --help, -h                Mostra esta mensagem de ajuda e encerra.");
            Console.WriteLine();
            Console.WriteLine("Comandos interativos durante a gravação (Fase B):");
            Console.WriteLine("  m                         Registra uma marca numerada no log CSV.");
            Console.WriteLine("  m <comentário>            Registra uma marca numerada com comentário textual.");
            Console.WriteLine("  q                         Encerra a gravação e salva todos os relatórios.");
        }
    }
}
