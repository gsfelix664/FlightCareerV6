# GERADOR-CHT-VFE — Gerador de voos sintéticos respeitando CHT e Vfe de cada avião

**Branch:** fix/gerador-cht-vfe · **Commit:** b09e142 · **Data:** 2026-11-16

## Plano (antes de codar)
1. **Dente de flap existente (`SyntheticFlapSelector`):** Criar classe pura em `FlightCareer.Domain/Replay/Synthetic/SyntheticFlapSelector.cs` implementando a lógica de seleção de flap por prioridade (`vfe:2` -> menor `n >= 2` com `vfe:n` -> `vfe:1` -> dente 2 com `ResolveLimit("vfe", 85)`). Integrar em `SyntheticFlightGenerator.cs` no cálculo de `normalApproachIas`, fase de aproximação e abuso `FlapOverSpeed`.
2. **Escala de CHT (`chtScale`):** Calcular `chtScale = Math.Min(1.0, 0.85 * chtMax / 175.0)` em `SyntheticFlightGenerator.cs` e aplicar a todos os alvos, tetos e pisos do voo normal (120, 165, 175, 155 ± 0.5, 50, 130) e piso do choque térmico (50), mantendo a temperatura ambiente (20 °C) e abusos não escalados. Garantir escala 1,0 para `chtMax >= 205.9` (bit a bit idêntico em C152/C172).
3. **Escala de Óleo e EGT (temperaturas normais):** Analisar perfis dos 46 modelos contra `oilTempMax` e `egtMax`. Para aeronaves com pico normal >= 0.90 do limite (DHC-6 para óleo com `oilTempMax = 99.0` e C400 Corvalis para EGT com `egtMax = 782.41`), aplicar escala protetora para 85% do limite quando a condição for atendida, mantendo 1,0 para todos os outros modelos.
4. **Testes unitários e verificação:**
   - Testes unitários para `SyntheticFlapSelector` e `ChtScale`.
   - Teste de baseline bit a bit para C152 e C172 comprovando determinismo e invariância.
   - Teste `Generator_ControlFlight_AllProfiles_NoTemperatureOrFlapExceedance` validando que todos os 46 modelos sem abusos não ativam nenhuma faixa de CHT, EGT, Óleo ou Vfe.
   - Execução do fuzzing com `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50` visando 0 falhas nos 46 modelos.

## O que foi feito
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlapSelector.cs`:
  - Criada classe pura e `SyntheticFlapSelection(int FlapIndex, double Vfe)` implementando a seleção do dente de flap existente e seu respectivo Vfe conforme a hierarquia da especificação:
    1. Se o perfil contiver `vfe:2` -> dente 2 com `ResolveLimit("vfe:2", 85.0)`;
    2. Senão, o menor dente `n >= 2` com `vfe:n` no perfil (ex.: A330 -> 4, A400M -> 3, ES30 -> 3);
    3. Senão, se contiver `vfe:1` -> dente 1 com `ResolveLimit("vfe:1", 110.0)` (ex.: Cessna 188 AgTruck);
    4. Senão -> dente 2 com `ResolveLimit("vfe", 85.0)`.
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlightGenerator.cs`:
  - Integrado `SyntheticFlapSelector.Select(profile, ResolveLimit)` para determinar `approachFlapIndex` e `approachVfe`.
  - Atualizado `normalApproachIas = Math.Min(approachVfe * 0.85, normalCruiseIas * 0.65)`.
  - Fase 8 (Aproximação): `flapIndex = approachFlapIndex; flapPercent = 1.0;`.
  - Abuso `FlapOverSpeed`: `flapIndex = approachFlapIndex; ias = approachVfe + 15.0 + abuse.Intensity * 20.0;`. Com isso, o `PartWearEngine` encontra o limite exato correspondente ao dente no perfil em vez de cair no fallback de `vfe:1` (240 kt em jatos).
  - Escala de CHT: calculada `chtScale = Math.Min(1.0, 0.85 * chtMax / 175.0)`.
    - Multiplicados por ela os tetos/alvos de táxi (120), decolagem (165), subida (175), cruzeiro (155 ± 0.5) e pisos de descida (130 e 50) e choque térmico (50).
    - Temperatura ambiente (20 °C) e abuso `ChtOverheat` (`chtMax + ...`) não escalados.
    - Para `chtMax >= 205.9` (C152 com 240, C172 com 240, etc.), `chtScale = 1.0` preservando determinismo e identidade bit a bit.
  - Análise de outras temperaturas do voo normal (Óleo, EGT, ITT):
    - Pico normal de óleo é 90 °C. O menor limite do catálogo é DHC-6 Twin Otter (`oilTempMax = 99.0`), onde `90.0 / 99.0 = 0.909 >= 0.90`. Foi aplicada escala protetora `oilScale = (90.0 >= 0.90 * oilTempMax) ? Math.Min(1.0, 0.85 * oilTempMax / 90.0) : 1.0;` que atua exclusivamente no DHC-6 e mantém 1.0 em todas as outras 45 aeronaves.
    - Pico normal de EGT é 730 °C. A única aeronave com limite próximo é C400 Corvalis (`egtMax = 782.41`), onde `730.0 / 782.41 = 0.933 >= 0.90`. Foi aplicada escala protetora `egtScale = (730.0 >= 0.90 * egtMax) ? Math.Min(1.0, 0.85 * egtMax / 730.0) : 1.0;` que atua exclusivamente no C400 e mantém 1.0 em todas as demais.
    - ITT não é gerado/simulado em `SyntheticFlightGenerator`.
- `src/FlightCareer.Tests/Replay/Fuzz/SyntheticGeneratorTemperatureAndFlapTests.cs`:
  - Criados 9 testes unitários dedicados cobrindo:
    - `ChtScale_HighLimit_IsOne` e `ChtScale_LiquidCooled105_KeepsPeakBelow092OfLimit`;
    - Casos de `SyntheticFlapSelector` (`vfe:1` + `vfe:2` -> 2, `vfe:1` + `vfe:4` -> 4, `vfe:1` + `vfe:3` + `vfe:5` -> 3, apenas `vfe:1` -> 1, apenas `vfe` genérico -> 2);
    - `Generator_C152_SameSeed_IdenticalToBaseline`: validação exata bit a bit com as somas de baseline gravadas antes da alteração (Frames=419, Cht=43215.6380, Ias=18330.8981, Egt=246680.0000, Oil=25294.7166, Flaps=84.0000);
    - `Generator_ControlFlight_AllProfiles_NoTemperatureOrFlapExceedance`: validação de que nenhum voo controle sem abusos gera qualquer excedência nas regras de temperatura (CHT, EGT, Óleo, ITT) ou flap em todos os 46 modelos do catálogo.
- Documentação:
  - `AGENTS.md` atualizado na seção 7 (bloco do Fuzz).
  - `docs/CHANGELOG.md` atualizado com a entrada da tarefa.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` todo verde (525 testes aprovados).
- [x] `fuzz --aircraft all --runs 50`: 2.300 voos executados em 46 aeronaves, **100% de aprovação (2.300/2.300), 0 falhas, 0 limites ausentes**. Tabela abaixo:

```text
--------------------------------------------------------------------------------
Resumo por Aeronave:
AircraftId                       |  voos | aprovados | limite ausente | falhas | principal violação
-----------------------------------------------------------------------------------------------
737 Max 8                        |    50 |        50 |              0 |      0 | -
747-8i                           |    50 |        50 |              0 |      0 | -
A310                             |    50 |        50 |              0 |      0 | -
A320neo                          |    50 |        50 |              0 |      0 | -
A321                             |    50 |        50 |              0 |      0 | -
A330-200                         |    50 |        50 |              0 |      0 | -
A330-BelugaXL                    |    50 |        50 |              0 |      0 | -
A400M                            |    50 |        50 |              0 |      0 | -
Airbus A321LR                    |    50 |        50 |              0 |      0 | -
Airbus A330-300                  |    50 |        50 |              0 |      0 | -
Asobo TBM 930                    |    50 |        50 |              0 |      0 | -
ATR 42-600                       |    50 |        50 |              0 |      0 | -
ATR 72-600                       |    50 |        50 |              0 |      0 | -
Beechcraft Baron G58             |    50 |        50 |              0 |      0 | -
Beechcraft Bonanza               |    50 |        50 |              0 |      0 | -
Beechcraft Bonanza G36           |    50 |        50 |              0 |      0 | -
Beechcraft Bonanza V35           |    50 |        50 |              0 |      0 | -
Beechcraft King Air              |    50 |        50 |              0 |      0 | -
Beechcraft King Air 250          |    50 |        50 |              0 |      0 | -
Beechcraft King Air C90GTx       |    50 |        50 |              0 |      0 | -
Boeing 747-400                   |    50 |        50 |              0 |      0 | -
Boeing 747-400 Global Supertanke |    50 |        50 |              0 |      0 | -
Boeing 787-10 Dreamliner         |    50 |        50 |              0 |      0 | -
C172SP G1000                     |    50 |        50 |              0 |      0 | -
C208B                            |    50 |        50 |              0 |      0 | -
C400 Corvalis                    |    50 |        50 |              0 |      0 | -
CAP-4                            |    50 |        50 |              0 |      0 | -
Cessna 188 AGTruck               |    50 |        50 |              0 |      0 | -
Cessna 207T Stationair           |    50 |        50 |              0 |      0 | -
Cessna 404 Titan                 |    50 |        50 |              0 |      0 | -
Cessna 408 SkyCourier            |    50 |        50 |              0 |      0 | -
Cessna C152                      |    50 |        50 |              0 |      0 | -
Cessna Citation CJ4              |    50 |        50 |              0 |      0 | -
Cessna Citation Longitude        |    50 |        50 |              0 |      0 | -
Cirrus SR22                      |    50 |        50 |              0 |      0 | -
DHC-6-300 Twin                   |    50 |        50 |              0 |      0 | -
Diamond DA40NG                   |    50 |        50 |              0 |      0 | -
Diamond DA62                     |    50 |        50 |              0 |      0 | -
ES30                             |    50 |        50 |              0 |      0 | -
Flight Design CT                 |    50 |        50 |              0 |      0 | -
NXCub                            |    50 |        50 |              0 |      0 | -
PC-12 NGX                        |    50 |        50 |              0 |      0 | -
Pilatus PC-24                    |    50 |        50 |              0 |      0 | -
Piper J-3 Cub                    |    50 |        50 |              0 |      0 | -
Piper PA28-236 Dakota            |    50 |        50 |              0 |      0 | -
Robin DR400                      |    50 |        50 |              0 |      0 | -
--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 2300
  Voos Aprovados      : 2300 (100,0%)
  Voos com Falhas     : 0 (0,0%)
  Tempo Total         : 1611,79 s (1 voos/s)
--------------------------------------------------------------------------------
✅ Nenhuma invariante ou gabarito violado em todas as corridas executadas!
```
- [x] CHANGELOG `## 2026-11-16 - Gerador sintético respeita CHT e Vfe por avião (Gemini)`; linha no `AGENTS.md` › 7 (bloco do Fuzz).
- [x] Commit `fix(fuzz): gerador sintetico respeita CHT e Vfe de cada aviao`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos pré-existentes de pacotes (SQLite / SqlClient).
- `dotnet test src/FlightCareer.Tests`: 525 aprovados, 0 falhas (9 novos testes em `SyntheticGeneratorTemperatureAndFlapTests.cs`).
- `FlightCareer.Replay fuzz --aircraft all --runs 50`: 2300 voos, 2300 aprovados (100%), 0 falhas.

## Não testado
- Telemetria em tempo real no MSFS 2024 / SimConnect (código exclusivamente no subsistema de replay sintético e fuzzing determinístico).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Todos os 46 modelos alcançaram 100% de aprovação (50/50 voos) no fuzzing determinístico sem falsos positivos ou limites ausentes.
