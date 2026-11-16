# FUZZ-ALL — Fuzz em todas as aeronaves e ressalvas do oráculo

**Branch:** test/fuzz-all-aeronaves · **Commit:** 6390c7d · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar feature `noFlaps` em `AircraftFeatures` e `_schema.json`, atribuindo aos modelos Piper J-3 Cub e CAP-4 com bump de `dataVersion`.
- Ajustar `SyntheticFlightPlan.Random` para não sortear `FlapOverSpeed` quando o perfil tiver `noFlaps`.
- Ajustar `SyntheticFlightOracle` para ignorar `FlapOverSpeed` em aeronaves `noFlaps` (sem ausência de limite e sem violação) e substituir a tolerância fixa de 0.05 pela comparação escalada pela taxa real da peça em voos com duração reduzida.
- Criar `FuzzRunSummary` no Domain para agregação pura e ordenação por falhas, e implementar `--aircraft all` no CLI `FlightCareer.Replay` com resumo tabular, semente reproduzível por aeronave e campo `reproduce` nos despejos.
- Criar suíte de testes unitários para todas as novas regras e executar o fuzz completo nas 46 aeronaves para levantar diagnósticos.

## O que foi feito
- `src/FlightCareer.Domain/Parts/AircraftFeatures.cs`:
  - Adicionada constante `NoFlaps = "noFlaps"` no grupo "Aerodinâmica e superfícies" e incluída no conjunto `All`.
- `src/FlightCareer.Infrastructure/Data/aircraft/_schema.json`:
  - Adicionado `"noFlaps"` ao `enum` da propriedade `features`.
- `src/FlightCareer.Infrastructure/Data/aircraft/piper-j-3-cub.json`:
  - Incrementado `dataVersion` para 6 e adicionada feature `"noFlaps"`.
- `src/FlightCareer.Infrastructure/Data/aircraft/cap-4.json`:
  - Incrementado `dataVersion` para 5 e adicionada feature `"noFlaps"`.
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlightPlan.cs`:
  - Em `Random`, verifica se `hasFlaps` é falso (presença de `noFlaps`), removendo `FlapOverSpeed` da lista de candidatos a abusos e preservando o determinismo dos modelos com flaps.
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlightOracle.cs`:
  - Ressalva 2: em `CheckMissingLimit`, se a aeronave tiver `noFlaps`, retorna `null` para `FlapOverSpeed`. Em `VerifyGroundTruth`, se o abuso for `FlapOverSpeed` em aeronave `noFlaps`, o gabarito é ignorado sem violações.
  - Ressalva 1: em `VerifyGroundTruth`, quando `abusedHours < controlHours - 1e-5`, calcula `scaledControl = controlPart.HealthLoss * (abusedHours / controlHours)` e valida contra `lossDiff < -ScaledControlToleranceRatio * scaledControl` (`ScaledControlToleranceRatio = 0.01`), eliminando a constante fixa de `0.05`.
- `src/FlightCareer.Domain/Replay/Synthetic/FuzzRunSummary.cs`:
  - Nova classe pura no Domain que agrega resultados por aeronave (`FuzzAircraftSummary`) e consolida totais gerais, ordenando resumos com falhas primeiro.
- `src/FlightCareer.Replay/Program.cs`:
  - Suporte a `--aircraft all` (case-insensitive) executando todos os perfis ordenados por `AircraftId` com 50 corridas padrão por aeronave.
  - Semente determinística por aeronave (`seed = baseSeed + run`), permitindo reprodução direta.
  - Progresso em uma linha por aeronave em `--aircraft all` e a cada 100 voos no modo padrão.
  - Tabela final por aeronave com alinhamento e resumos agregados.
  - Inclusão do campo `reproduce` no `groundtruth.json` com linha de comando para reprodução.
  - Tratamento de erro de I/O de artefatos com `catch (Exception ex)` reportando no `Console.Error`.
  - Atualização do help do CLI com as novas opções.
- `src/FlightCareer.Tests/Replay/Fuzz/SyntheticFlightFuzzAllTests.cs`:
  - 9 novos testes unitários cobrindo todas as novas funcionalidades e invariantes rápidas nas 46 aeronaves.
- Documentação atualizada em `AGENTS.md` e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` com 0 erros; `dotnet test src/FlightCareer.Tests` todo verde (509 aprovados, 0 falhas).
- [x] `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50` roda nas 46 aeronaves; tabela e resumos colados abaixo.
- [x] `fuzz --aircraft "Cessna C152" --runs 200` e `fuzz` sem `--aircraft` continuam funcionando como antes.
- [x] Relatório lista cada categoria de falha real encontrada: aeronave, seed, mensagem e hipótese de causa (sem corrigir).
- [x] Entrada no fim de `docs/CHANGELOG.md` e linha no `AGENTS.md` › 7 sobre `--aircraft all` e `noFlaps`.
- [x] Um commit `test(fuzz): fuzz em todas as aeronaves e ressalvas do oráculo`.

## Tabela e Resumos de Execução (`--aircraft all --runs 50`)

```text
--------------------------------------------------------------------------------
Resumo por Aeronave:
AircraftId                       |  voos | aprovados | limite ausente | falhas | principal violação
-----------------------------------------------------------------------------------------------
Diamond DA40NG                   |    50 |        34 |              7 |      9 | Gabarito violado
A330-200                         |    50 |        33 |             11 |      6 | Gabarito violado
A330-BelugaXL                    |    50 |        33 |             11 |      6 | Gabarito violado
A400M                            |    50 |        33 |             11 |      6 | Gabarito violado
ES30                             |    50 |        33 |             11 |      6 | Gabarito violado
Diamond DA62                     |    50 |        35 |              9 |      6 | Gabarito violado
Flight Design CT                 |    50 |        36 |              8 |      6 | Gabarito violado
CAP-4                            |    50 |        38 |              8 |      4 | Gabarito violado
Piper PA28-236 Dakota            |    50 |        38 |              8 |      4 | Gabarito violado
Robin DR400                      |    50 |        38 |              8 |      4 | Gabarito violado
Beechcraft Bonanza G36           |    50 |        39 |              8 |      3 | Gabarito violado
Asobo TBM 930                    |    50 |        37 |             13 |      0 | -
PC-12 NGX                        |    50 |        37 |             13 |      0 | -
737 Max 8                        |    50 |        38 |             12 |      0 | -
747-8i                           |    50 |        38 |             12 |      0 | -
A310                             |    50 |        38 |             12 |      0 | -
A320neo                          |    50 |        38 |             12 |      0 | -
A321                             |    50 |        38 |             12 |      0 | -
Airbus A321LR                    |    50 |        38 |             12 |      0 | -
Airbus A330-300                  |    50 |        38 |             12 |      0 | -
ATR 42-600                       |    50 |        38 |             12 |      0 | -
ATR 72-600                       |    50 |        38 |             12 |      0 | -
Beechcraft King Air              |    50 |        38 |             12 |      0 | -
Beechcraft King Air 250          |    50 |        38 |             12 |      0 | -
Beechcraft King Air C90GTx       |    50 |        38 |             12 |      0 | -
Boeing 747-400                   |    50 |        38 |             12 |      0 | -
Boeing 747-400 Global Supertanke |    50 |        38 |             12 |      0 | -
Boeing 787-10 Dreamliner         |    50 |        38 |             12 |      0 | -
C208B                            |    50 |        38 |             12 |      0 | -
Cessna Citation CJ4              |    50 |        38 |             12 |      0 | -
Cessna Citation Longitude        |    50 |        38 |             12 |      0 | -
Pilatus PC-24                    |    50 |        38 |             12 |      0 | -
Beechcraft Baron G58             |    50 |        39 |             11 |      0 | -
Cessna 404 Titan                 |    50 |        39 |             11 |      0 | -
Cessna 408 SkyCourier            |    50 |        39 |             11 |      0 | -
DHC-6-300 Twin                   |    50 |        39 |             11 |      0 | -
Beechcraft Bonanza               |    50 |        41 |              9 |      0 | -
Beechcraft Bonanza V35           |    50 |        41 |              9 |      0 | -
C400 Corvalis                    |    50 |        41 |              9 |      0 | -
Cessna 188 AGTruck               |    50 |        41 |              9 |      0 | -
Cessna 207T Stationair           |    50 |        41 |              9 |      0 | -
Cirrus SR22                      |    50 |        41 |              9 |      0 | -
NXCub                            |    50 |        41 |              9 |      0 | -
Piper J-3 Cub                    |    50 |        41 |              9 |      0 | -
Cessna C152                      |    50 |        46 |              4 |      0 | -
C172SP G1000                     |    50 |        50 |              0 |      0 | -
--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 2300
  Voos Aprovados      : 1766 (76,8%)
  Voos c/ Limite Aus. : 474 (20,6%)
  Voos com Falhas     : 60 (2,6%)
  Tempo Total         : 1220,80 s (2 voos/s)
--------------------------------------------------------------------------------
Resumo dos Limites Ausentes no Catálogo:
  • limite ausente: elecMainBusIndex: 328 ocorrências
  • limite ausente: elecAvionicsCircuitIndex: 184 ocorrências
  • limite ausente: alternatorRatedAmps: 13 ocorrências

Resumo das Violações Encontradas:
  • Gabarito violado: 84 ocorrências

Artefatos de reprodução salvos em:
  C:\Users\Usuario\AppData\Local\FlightCareer\fuzz-failures
================================================================================
```

## Categorias de Falhas Reais Encontradas (Sem Corrigir)

Todas as falhas observadas foram de **gabarito violado** (nenhuma invariante física universal de integridade, NaN ou monotonicidade foi violada). As falhas se dividem em três causas raiz nos perfis/gerador:

### 1. Motores refrigerados a líquido com CHT máxima reduzida (Diamond DA40NG, DA62, Flight Design CT)
- **Aeronaves e sementes de exemplo**:
  - `Diamond DA40NG` (Seed 46, 49): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "Diamond DA40NG" --seed 46 --runs 1`
  - `Diamond DA62` (Seed 43, 46): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "Diamond DA62" --seed 43 --runs 1`
  - `Flight Design CT` (Seed 46, 49): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "Flight Design CT" --seed 46 --runs 1`
- **Mensagem**: `Gabarito violado: Perda na peça 'engine.cylinders' com abuso (1,2762%) não foi estritamente maior que no voo controle (2,0269%, diff=-0,750660%).`
- **Hipótese da causa**:
  - Essas aeronaves utilizam motores com refrigeração líquida (Austro Engine Diesel no DA40/DA62 com `chtMax = 105.0 °C`; Rotax 912S no Flight Design CT com `chtMax = 120.0 °C`).
  - O `SyntheticFlightGenerator` simula voos a pistão padrão com CHT de cruzeiro entre 155 °C e 175 °C (típica de motores a pistão Lycoming refrigerados a ar).
  - Em consequência, o voo-controle de referência passa o voo inteiro em sobretemperatura severa contínua de CHT (>105 °C / >120 °C), acumulando danos massivos no controle antes de qualquer abuso.

### 2. FlapOverSpeed com índice fixo em jatos e turboélices pesados (A330-200, BelugaXL, A400M, ES30)
- **Aeronaves e sementes de exemplo**:
  - `A330-200` (Seed 46, 50): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "A330-200" --seed 46 --runs 1`
  - `A330-BelugaXL` (Seed 46, 50)
  - `A400M` (Seed 46, 50)
  - `ES30` (Seed 46, 50)
- **Mensagem**:
  - `Gabarito violado: Abuso 'FlapOverSpeed' não disparou a regra esperada 'flap_overspeed_vfe'.`
  - `Gabarito violado: Perda na peça 'airframe.flaps' com abuso (0,0028%) não foi estritamente maior que no voo controle (0,0028%, diff=0,000000%).`
- **Hipótese da causa**:
  - No catálogo, essas aeronaves possuem limites discretos por dente (`vfe:1` e `vfe:4` no A330-200), sem limite genérico `vfe` nem `vfe:2`.
  - O `SyntheticFlightGenerator` injeta `FlapOverSpeed` fixando `flapIndex = 2` e calculando `ias = vfe2 + 15 + ...` usando fallback fixo `vfe2 = 85 kt`.
  - Como 85 kt é muito abaixo da velocidade real do jato e a aeronave não possui `vfe:2` cadastrado, a regra de sobrevelocidade de flap não dispara.

### 3. Excesso contínuo de EGT no voo controle por limite `egtMax` baixo (Bonanza G36, CAP-4, Piper Dakota, Robin DR400)
- **Aeronaves e sementes de exemplo**:
  - `Beechcraft Bonanza G36` (Seed 46): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "Beechcraft Bonanza G36" --seed 46 --runs 1`
  - `CAP-4` (Seed 68): `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft "CAP-4" --seed 68 --runs 1`
  - `Piper PA28-236 Dakota` (Seed 46)
  - `Robin DR400` (Seed 46)
- **Mensagem**: `Gabarito violado: Perda na peça 'engine.valves' com abuso (1,3008%) não foi estritamente maior que no voo controle (1,3008%, diff=0,000000%).`
- **Hipótese da causa**:
  - Esses modelos possuem `egtMax = 649.07 °C` no catálogo.
  - O gerador sintético simula o voo normal com EGT constante de 710 °C a 730 °C.
  - Assim, o voo controle normal já opera continuamente em excedência máxima de EGT (`piston_egt_excess`), saturando a banda superior de desgaste da peça antes da injeção do abuso.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, avisos conhecidos de dependência de pacotes (SQLitePCLRaw / System.Data.SqlClient).
- `dotnet test src/FlightCareer.Tests`: 509 aprovados, 0 falhas (9 novos testes unitários adicionados).

## Não testado
- Comportamento visual na interface WPF / telas de voo ao vivo do simulador (tarefa focada exclusivamente em CLI / Domain / Replay).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida técnica impeditiva. As falhas encontradas em outras regras/perfis foram catalogadas com hipóteses de causa conforme solicitado, sem afrouxamento do oráculo e sem desvio de escopo.
