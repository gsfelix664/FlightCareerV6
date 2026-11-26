# Comandos de ferramentas

Lido sob demanda (`AGENTS.md` › 3). Execute a partir da raiz.

```bash
dotnet run --project src/FlightCareer.SimVarProbe [--probe-only] [--duration <seg>] [--filter <regex>]
dotnet run --project src/FlightCareer.Replay -- <csv|csv.gz> [--aircraft <modelo>]
dotnet run --project src/FlightCareer.Replay -- fuzz [--runs <n>] [--aircraft <modelo>] [--seed <n>]
dotnet run --project src/FlightCareer.Replay -- career --aircraft "<modelo>,<modelo>" [--bots <n>] [--days <n>] [--hours-per-day <h>] [--seed <n>] [--policy best|random] [--fast] [--out <pasta>]
dotnet run --project src/FlightCareer.Replay -- career-stab [--days <n>] [--bots <n>] [--seed <n>] [--difficulty easy|medium|hard|all] [--policy all|cauteloso|equilibrado|agressivo] [--fast] [--out <pasta>]
```
