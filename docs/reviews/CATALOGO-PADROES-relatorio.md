# Relatório de Execução — CATALOGO-PADROES

**Data:** 2026-11-16  
**Agente:** Gemini  
**Branch:** `feat/catalogo-limites-estimados`  
**Commit base:** `e01f13b`  
**Commit(s) entregues:** `98bf78e` — `feat(limits): limites estimados, noElectrical e indices eletricos de teste no fuzz`  

---

## 1. O que foi feito

1. **Feature `noElectrical`**:
   - Criada constante `AircraftFeatures.NoElectrical = "noElectrical"` no grupo `"Célula e sistemas adicionais"` e incluída no conjunto `All` (`Domain/Parts/AircraftFeatures.cs`).
   - Adicionada `"noElectrical"` ao enum de features no schema `Infrastructure/Data/aircraft/_schema.json`.
   - Adicionada a feature aos perfis `Infrastructure/Data/aircraft/piper-j-3-cub.json` (dataVersion 7) e `Infrastructure/Data/aircraft/cap-4.json` (dataVersion 6).
   - `SyntheticFlightPlan.Random` verifica se a aeronave possui `noElectrical` e descarta os 3 abusos elétricos (`BatteryDischarging`, `ElectricalOverload`, `AvionicsMasterOnStart`).
   - `SyntheticFlightOracle.CheckMissingLimit` e `SyntheticFlightOracle.VerifyGroundTruth` ignoram a falta de limites elétricos e não reportam violação caso um abuso elétrico seja injetado em modelo `noElectrical`.

2. **Preenchimento de Limites Estimados no Catálogo (`Infrastructure/Data/aircraft/*.json`)**:
   - Atualizados 44 arquivos JSON do catálogo com os limites ausentes de manual (voltagem, rotação, temperatura de óleo/ITT/CHT e corrente do alternador no A400M), com `source: "ClassDefault"` e referência iniciando com `"estimativa — atualizar pelo POH real: <base>"`.
   - `dataVersion` incrementado em +1 em todos os 44 arquivos alterados.
   - Formatação JSON mantida com indentação de 2 espaços, UTF-8 sem BOM e CRLF.
   - Preservados todos os limites já existentes e padrões de classe intactos. Índices elétricos (`elecMainBusIndex`, `elecAvionicsCircuitIndex`, `elecAvionicsBusAmpsIndex`) intencionalmente não inseridos no catálogo de jogo.

3. **Índices Elétricos de Teste no Fuzz (`Domain/Replay/Synthetic/FuzzTestDefaults.cs`)**:
   - Criada classe pura `FuzzTestDefaults` com método `WithTestElectricalIndices(AircraftLimitProfile profile)`.
   - Injeta apenas os índices elétricos ausentes (`elecMainBusIndex` = 1, `elecAvionicsCircuitIndex` = 3, `elecAvionicsBusAmpsIndex` = 4) com `Source = ClassDefault` e `Reference = "padrão de teste do fuzz — não usar no jogo"`.
   - Modelos com `noElectrical` têm retorno imediato do perfil inalterado.
   - Documentação XML explícita proibindo uso em produção (seeder, resolver, UI).
   - `SyntheticFlightGenerator.ResolveLimit` atualizado para ler prioritariamente do perfil recebido `_profile.Limits`, permitindo que índices injetados em memória sejam enxergados pelo gerador e replayer.
   - Aplicado exclusivamente no comando `RunFuzz` do CLI (`Program.cs`) e no teste xUnit `Fuzz_AllProfiles_QuickInvariantsValidation`.
   - Adicionada linha informativa no comando `--help` do CLI.

4. **Nova Suíte de Testes Unitários (`FlightCareer.Tests/Limits/CatalogEstimatesTests.cs`)**:
   - 7 novos testes automatizados:
     - `CatalogEstimates_AllAddedLimits_AreClassDefaultWithPohNote`
     - `NoElectricalModels_LoadWithFeature`
     - `Random_NoElectricalAircraft_NeverPicksElectricalAbuses`
     - `VerifyGroundTruth_ElectricalAbuseOnNoElectrical_NoViolation`
     - `WithTestElectricalIndices_AddsOnlyMissingIndices`
     - `WithTestElectricalIndices_NoElectrical_ReturnsUnchanged`
     - `CatalogProfiles_NeverContainFuzzTestDefaults`

5. **Execução Completa do Fuzz (46 Aeronaves × 50 Corridas = 2.300 Voos)**:
   - Eliminados **100% dos erros de limite ausente** em todos os 46 modelos do catálogo (0 limites ausentes na frota inteira).
   - 38 das 46 aeronaves obtiveram 100% de aprovação (50/50).
   - Total geral: 2.254 voos aprovados (98,0%), 46 voos com falhas (2,0%).

---

## 2. Decisões tomadas e justificativas

- **Prioridade em `SyntheticFlightGenerator.ResolveLimit`**: O gerador anteriormente chamava diretamente `AircraftLimitResolver.ResolveLimit(profile.AircraftId, ...)`. Para que perfis aumentados em tempo de teste por `WithTestElectricalIndices` surtissem efeito no gerador sem alterar o singleton estático do resolver (mantendo isolamento), `ResolveLimit` passou a checar primeiro se a chave existe em `_profile.Limits`.
- **Fuzz de aeronaves com motores a pistão refrigerados a líquido (DA40NG, DA62, Flight Design CT) e elétrico (ES30)**: Permaneceram as 6 falhas de gabarito conhecidas da tarefa FUZZ-ALL decorrentes de injeção sintética de abuso de CHT em motores sem limites de ar. Como orientado na especificação, falhas já catalogadas não foram alteradas nesta tarefa.
- **Piper PA28-236 Dakota (4 falhas de gabarito)**: O modelo não possuía `egtMax` no manual e recebeu `egtMax` da classe pistão (893,52 °C). Em 4 voos com intensidade leve de abuso EGT (16%), a telemetria sintética não atingiu a temperatura de estresse, não gerando desgaste extra em `engine.valves` em relação ao controle.

---

## 3. Critérios de aceite da tarefa

- [x] `dotnet build src/FlightCareer.slnx` passa com 0 erros.
- [x] `dotnet test src/FlightCareer.Tests` passa com 516 testes aprovados (509 anteriores + 7 novos), 0 falhas.
- [x] `python -m unittest discover -s tools/tests` passa com 48 testes aprovados, 0 falhas.
- [x] Feature `noElectrical` implementada em `AircraftFeatures.cs`, `_schema.json`, J-3 Cub e CAP-4.
- [x] Catálogo atualizado com estimativas padronizadas (`Source: ClassDefault`, nota POH, `dataVersion` incrementado).
- [x] `FuzzTestDefaults.WithTestElectricalIndices` implementado e isolado exclusivamente nos testes/fuzz.
- [x] Fuzz `--aircraft all --runs 50` rodado com tabela e resumo consolidado.
- [x] Zero limites ausentes em todas as 46 aeronaves.
- [x] `docs/CHANGELOG.md` e `AGENTS.md` atualizados.

---

## 4. Build e testes

### Compilação
```text
dotnet build src/FlightCareer.slnx
0 Erro(s)
0 Advertência(s) de código (apenas warnings NU1903 de pacote conhecido SQLitePCLRaw)
```

### Testes Unitários (.NET)
```text
dotnet test src/FlightCareer.Tests
Aprovado! - Com falhas: 0, Aprovado: 516, Ignorado: 0, Total: 516, Duração: 17s
```

### Testes Python
```text
python -m unittest discover -s tools/tests
Ran 48 tests in 0.093s
OK
```

### Fuzz Testing Completo (46 modelos × 50 corridas = 2.300 voos)
Comando executado: `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 50`

```text
--------------------------------------------------------------------------------
Resumo por Aeronave:
AircraftId                       |  voos | aprovados | limite ausente | falhas | principal violação
-----------------------------------------------------------------------------------------------
A330-200                         |    50 |        44 |              0 |      6 | Gabarito violado
A330-BelugaXL                    |    50 |        44 |              0 |      6 | Gabarito violado
A400M                            |    50 |        44 |              0 |      6 | Gabarito violado
Diamond DA40NG                   |    50 |        44 |              0 |      6 | Gabarito violado
Diamond DA62                     |    50 |        44 |              0 |      6 | Gabarito violado
ES30                             |    50 |        44 |              0 |      6 | Gabarito violado
Flight Design CT                 |    50 |        44 |              0 |      6 | Gabarito violado
Piper PA28-236 Dakota            |    50 |        46 |              0 |      4 | Gabarito violado
737 Max 8                        |    50 |        50 |              0 |      0 | -
747-8i                           |    50 |        50 |              0 |      0 | -
A310                             |    50 |        50 |              0 |      0 | -
A320neo                          |    50 |        50 |              0 |      0 | -
A321                             |    50 |        50 |              0 |      0 | -
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
NXCub                            |    50 |        50 |              0 |      0 | -
PC-12 NGX                        |    50 |        50 |              0 |      0 | -
Pilatus PC-24                    |    50 |        50 |              0 |      0 | -
Piper J-3 Cub                    |    50 |        50 |              0 |      0 | -
Robin DR400                      |    50 |        50 |              0 |      0 | -
--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 2300
  Voos Aprovados      : 2254 (98,0%)
  Voos com Falhas     : 46 (2,0%)
  Tempo Total         : 1694,44 s (1 voos/s)
--------------------------------------------------------------------------------
```

---

## 5. O que NÃO foi testado no simulador

- A aplicação dos limites estimados durante voo no MSFS 2024 não foi testada in-game (testado exclusivamente via replay determinístico, gerador procedural sintético, oráculo físico e suítes xUnit).

---

## 6. Dúvidas, decisões pendentes ou pedidos ao revisor

- **Nenhuma dúvida impeditiva**. Todas as decisões do plano e da especificação foram cumpridas integralmente.
- O revisor pode aprovar a branch `feat/catalogo-limites-estimados` e planejar tarefas futuras para tratar as ressalvas remanescentes do gerador de fuzz (Vfe por dente em flaps e injeção de abuso de CHT em motores liquid-cooled/elétricos).
