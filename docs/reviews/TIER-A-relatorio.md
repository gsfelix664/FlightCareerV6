# TIER-A — Pista mínima por perfil de missão e tier por movimento

**Branch:** feat/tier-movimento · **Commits:** 14d2788 (missoes), (aeroportos) · **Data:** 2026-11-24

## Plano (antes de codar)
- **Parte A — Pista mínima por perfil:**
  - Adicionar `MinRunwayFt` e `Paved` em `MissionProfileDefinition`.
  - Mapear e preencher `MinRunwayFt` e `Paved` em todos os 110 perfis de `mission_catalog.json` com base na tabela de classes da § 2 de `tiers-por-movimento.md` e na aeronave de referência de `catalogo-missoes.md`.
  - Atualizar `MissionBoardCore.GenerateBoard`, `PickProfile` e `FilterCandidates` para validar a melhor pista da origem e do destino.
  - Atualizar os dois caminhos em `MissionGenerator` (`GenerateMissionsAsync` e `GenerateMissionsInMemory`) para buscar e repassar a melhor pista da origem.
- **Parte B — Tier por movimento:**
  - Criar `AirportTierClassifier` e `AirportTierRules` em `Domain/Airports/` (lógica pura), acompanhado do catálogo de regras `airport_tier_rules.json`.
  - Estender a entidade `Airport` com os campos de estrutura física e pontuação (`IlsEnds`, `Approaches`, `Gates`, `CargoParking`, `HasTower`, `LongestRunwayFt`, `LongestRunwayPaved`, `TierScore`) e garantir migração idempotente em `AppDbContext.EnsureSchemaUpdatedAsync`.
  - Atualizar `DataSeeder`:
    - No fluxo do Little Navmap: ler campos de estrutura de `lnm.airport`, classificar em C# via `AirportTierClassifier` e inserir em lote com transação.
    - No enriquecimento CSV: ler `type` e `scheduled_service`, recalcular tiers dos aeroportos coincidentes por posição.
    - No fluxo só-CSV: carregar pistas de `runways.csv` e classificar com o mesmo motor.
- **Relatórios e Testes:**
  - Implementar subcomando `tier-report` no CLI `FlightCareer.Replay` (`TierReporter.cs`), gerando `tier-report.md` e `tier-report.csv`.
  - Escrever suíte de testes xUnit para `AirportTierClassifier` cobrindo todos os cenários da especificação.
  - Escrever testes xUnit para `MissionBoardCore` (origem curta descarta widebody/ponte aérea; destino respeita pista; todos os perfis têm pista mínima).
  - Atualizar testes legados e regravação do snapshot `missions_golden.json`.

---

## O que foi feito

- `src/FlightCareer.Domain/Missions/MissionDefinition.cs`:
  - Adicionadas as propriedades `int MinRunwayFt` (0 = sem restrição) e `bool Paved` no record `MissionProfileDefinition`.
- `src/FlightCareer.Infrastructure/Data/mission_catalog.json`:
  - Preenchidos todos os 110 perfis com suas respectivas restrições físicas de pista (comprimento e pavimentação). Perfis de instrução, voo local e volta à origem receberam 0/não pavimentado; utilitários bush receberam 1.200 ft; bimotores leves 2.500 ft; turboélices regionais 3.600 ft (pavimentada); narrowbody 4.300 ft (pavimentada); jatos de longo alcance 4.900 ft (pavimentada); widebody 8.000 ft (pavimentada).
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`:
  - Novo parâmetro `Runway? originRunway` em `GenerateBoard`.
  - Validação de perfis suportados pela pista da origem em `PickProfile`. Tipos de missão cujos perfis não são comportados pela pista da origem recebem peso zero e são excluídos do sorteio antes de gastar tentativas. Origens sem pista conhecida emitem `Debug.WriteLine` uma vez por geração de quadro.
  - `FilterCandidates` agora exige pista com comprimento $\ge$ `profile.MinRunwayFt` e pavimentação compatível (`RunwaySurface.IsUnpaved`). Candidatos a destino sem pista conhecida são excluídos quando `profile.MinRunwayFt > 0`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - `GenerateMissionsAsync` inclui o ICAO da origem na query de pistas e repassa a melhor pista da origem para `MissionBoardCore.GenerateBoard`.
  - `GenerateMissionsInMemory` resolve a pista de origem de `runwaysByIcao` e repassa para `MissionBoardCore.GenerateBoard`.
- `src/FlightCareer.Domain/Airports/AirportTierRules.cs` e `src/FlightCareer.Domain/Airports/AirportTierClassifier.cs`:
  - Classes puras de domínio implementando pontuação aditiva por etiquetas curadas dominantes, estrutura no simulador (ILS, aproximações, gates, jetways, estacionamento de carga, torre e pista mais longa pavimentada), OurAirports (tipo de aeródromo e serviço regular agendado) e cortes calibrados (Tier 1 $\ge$ 90, Tier 2 $\ge$ 45, Tier 3 $\ge$ 15).
  - Invariante de pista não pavimentada limitando o aeródromo a no máximo Tier 3 (`pavedRequiredForTier1And2`).
  - Etiquetas heurísticas (`isolado`, `fazenda`, etc.) são devidamente ignoradas.
- `src/FlightCareer.Infrastructure/Data/airport_tier_rules.json`:
  - Arquivo de configuração copiado para a pasta de saída contendo os pesos exatos da especificação.
- `src/FlightCareer.Domain/Entities/Airport.cs` e `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - Adicionadas propriedades de persistência dos sinais (`IlsEnds`, `Approaches`, `Gates`, `CargoParking`, `HasTower`, `LongestRunwayFt`, `LongestRunwayPaved`, `TierScore`).
  - Migração de banco idempotente via `ALTER TABLE Airports ADD COLUMN ... DEFAULT 0`.
- `src/FlightCareer.Infrastructure/Services/DataSeeder.cs`:
  - `SeedFromLittleNavmapAsync`: Seleciona dados de estrutura da base LNM anexada (`num_runway_end_ils`, `num_approach`, `num_parking_gate`, `num_jetway`, `num_parking_cargo`, `has_tower_object`, `tower_frequency`, `longest_runway_length`, `longest_runway_surface`, `num_runway_hard`), classifica em C# via `AirportTierClassifier` e insere em lote com transação SQLite.
  - `EnrichFromCsvAsync`: Enriquecimento com cruzamento posicional (`fc_same_place`), associando `type` e `scheduled_service` e recalculando os tiers no banco. Corrigida cláusula SQL de junção para compatibilidade com o alias do SQLite.
  - `SeedAirportsAsync` e `GetRunwayInfoFromCsvAsync`: Fluxo alternativo só-CSV classificando por pistas do `runways.csv` e dados OurAirports.
- `src/FlightCareer.Replay/TierReporter.cs` e `src/FlightCareer.Replay/Program.cs`:
  - Subcomando `tier-report [--lnm <path>] [--out <folder>]` classificando toda a base LNM e OurAirports, gerando `tier-report.md` e `tier-report.csv`.
- `src/FlightCareer.Tests/Airports/AirportTierClassifierTests.cs`:
  - Criados os testes xUnit:
    - `AirportTierClassifier_MegaHub_Tier1`
    - `AirportTierClassifier_PonteAereaPistaCurta_Tier1`
    - `AirportTierClassifier_AeroportoExecutivo_Tier2`
    - `AirportTierClassifier_Fazenda_Tier4`
    - `AirportTierClassifier_SemPavimento_NoMaximoTier3`
    - `AirportTierClassifier_IgnoraEtiquetasHeuristicas`
- `src/FlightCareer.Tests/Missions/MissionRunwayProfileTests.cs`:
  - Criados os testes xUnit:
    - `MissionBoardCore_OrigemPistaCurta_SemPerfilWidebody` (SBMT 5.120 ft sem widebody e sem ponte aérea se pista < 4.300 ft em 2.000 iterações).
    - `MissionBoardCore_DestinoRespeitaPistaDoPerfil` (perfil 4.300 ft pavimentada nunca seleciona pista menor ou de terra).
    - `MissionCatalog_TodosOsPerfisTemPistaMinima` (garante que todos os 110 perfis possuem `MinRunwayFt` preenchido e coerente).
- `src/FlightCareer.Tests/Missions/Golden/missions_golden.json`:
  - Snapshot regravado e testado via `MissionCatalogEquivalenceTests`.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Documentação das regras de pista mínima por perfil e classificação de tier por movimento adicionada na seção 7 de `AGENTS.md` e registrada no `docs/CHANGELOG.md`.

---

## Mapeamento de Classes por Perfil de Missão (110 perfis / 69 missões)

| Tipo de Missão | Perfil | Classe / Aeronave Ref. | MinRunwayFt | Paved |
|---|---|---|---|---|
| `instrucao_local` | P1 | instrução / local | 0 | Não |
| `circuito_tgl` | P1, P2 | instrução / local | 0 | Não |
| `navegacao_vfr` | P1 | instrução / local | 0 | Não |
| `instrucao_ifr` | P1 | instrução / local | 0 | Não |
| `adaptacao_noturna` | P1 | instrução / local | 0 | Não |
| `cheque` | P1 | instrução / local | 0 | Não |
| `instrucao_stol` | P1 | instrução / local | 0 | Não |
| `ponte_aerea` | P1 | narrowbody (A320/B737) | 4.300 | Sim |
| `troncal_domestico` | P1, P2, P3 | narrowbody (A320/B737) | 4.300 | Sim |
| `regional_internacional` | P1, P2, P3 | narrowbody (A320/B737) | 4.300 | Sim |
| `longo_curso` | P1, P2, P3 | widebody (A330/B747/B777) | 8.000 | Sim |
| `feeder_inbound` | P1, P2, P3 | turboélice regional (ATR 72) | 3.600 | Sim |
| `feeder_outbound` | P1, P2, P3 | turboélice regional (ATR 72) | 3.600 | Sim |
| `sub_regional` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `sub_regional` | P2 | turboélice / bimotor leve (King Air/Baron) | 2.500 | Não |
| `pinga_pinga` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `regional_essencial` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `regional_essencial` | P2 | turboélice regional (ATR 72) | 3.600 | Sim |
| `executivo_corporativo` | P1 | turboélice / bimotor leve (King Air) | 2.500 | Não |
| `executivo_corporativo` | P2 | jato executivo leve / médio (CJ4) | 3.400 | Sim |
| `executivo_corporativo` | P3 | jato executivo longo alcance (Longit./Global) | 4.900 | Sim |
| `executivo_intercontinental` | P1 | jato executivo longo alcance | 4.900 | Sim |
| `vip_internacional` | P1 | jato executivo leve / médio (CJ4/Citation) | 3.400 | Sim |
| `agro_executivo` | P1, P2 | turboélice / bimotor leve (King Air/Baron) | 2.500 | Não |
| `familia_alto_padrao` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `equipes_urgentes` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `charter_lazer` | P1 | turboélice / bimotor leve (King Air) | 2.500 | Não |
| `charter_lazer` | P2 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `charter_esportivo` | P1 | turboélice regional (ATR 72) | 3.600 | Sim |
| `charter_esportivo` | P2 | narrowbody (A320/B737) | 4.300 | Sim |
| `transfer_metropolitano` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `panoramico` | P1, P2 | volta à origem / local | 0 | Não |
| `demo_flight` | P1 | volta à origem / local | 0 | Não |
| `carga_paletizada` | P1 | narrowbody cargueiro (B737F) | 4.300 | Sim |
| `carga_paletizada` | P2, P3 | widebody cargueiro (B777F/B747F) | 8.000 | Sim |
| `carga_overnight` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `carga_overnight` | P2 | turboélice regional (ATR 72F) | 3.600 | Sim |
| `carga_overnight` | P3 | narrowbody cargueiro (B737F) | 4.300 | Sim |
| `malotes` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `carga_valores` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `ecommerce` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `ecommerce` | P2 | turboélice regional (ATR 72F) | 3.600 | Sim |
| `ecommerce` | P3 | narrowbody cargueiro (B737F) | 4.300 | Sim |
| `carga_jit` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `carga_jit` | P2 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `cadeia_fria` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `cadeia_fria` | P2 | turboélice regional (ATR 72F) | 3.600 | Sim |
| `cadeia_fria` | P3 | narrowbody cargueiro (B737F) | 4.300 | Sim |
| `carga_dgr` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `carga_dgr` | P2 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `carga_sensivel` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `carga_combi` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `carga_combi` | P2 | turboélice regional (ATR 72) | 3.600 | Sim |
| `insumos_agro` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `suprimentos_remotos` | P1, P2 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `conexao_pista_curta` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `orgaos` | P1 | turboélice / bimotor leve (Baron/King Air) | 2.500 | Não |
| `uti_inter_hospitalar` | P1 | turboélice / bimotor leve (King Air) | 2.500 | Não |
| `medevac_remoto` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `uti_longa_distancia` | P1 | jato executivo leve / médio (CJ4) | 3.400 | Sim |
| `saude_familia` | P1 | monomotor utilitário / bush (Caravan) | 1.200 | Não |
| `aerolevantamento` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `paraquedismo` | P1, P2, P3 | monomotor utilitário / bush | 1.200 | Não |
| `pulverizacao` | P1 | monomotor utilitário / bush (Air Tractor) | 1.200 | Não |
| `reboque_planador` | P1 | instrução / local / volta à origem | 0 | Não |
| `publicidade_aerea` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `patrulha_vigilancia` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `inspecao_linhas` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `monitoramento_trafego`| P1 | monomotor utilitário / bush | 1.200 | Não |
| `sar` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `calibracao_auxilios` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `ferry_posicionamento` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `ferry_posicionamento` | P2 | turboélice / bimotor leve | 2.500 | Não |
| `ferry_posicionamento` | P3 | jato executivo leve / médio | 3.400 | Sim |
| `translado_manutencao`| P1 | monomotor utilitário / bush | 1.200 | Não |
| `ferry_permit` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `check_flight` | P1 | instrução / local / volta à origem | 0 | Não |
| `crew_parts` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `crew_parts` | P2 | monomotor utilitário / bush | 1.200 | Não |
| `translado_piloto_agricola`| P1 | monomotor utilitário / bush | 1.200 | Não |
| `pessoas_remotas` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `pessoas_remotas` | P2 | turboélice / bimotor leve | 2.500 | Não |
| `autoridades` | P1 | turboélice / bimotor leve | 2.500 | Não |
| `diplomatico` | P1 | jato executivo longo alcance | 4.900 | Sim |
| `diplomatico` | P2 | narrowbody | 4.300 | Sim |
| `militar_can` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `militar_can` | P2 | turboélice regional | 3.600 | Sim |
| `militar_can` | P3 | widebody cargueiro | 8.000 | Sim |
| `humanitario` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `humanitario` | P2 | widebody cargueiro | 8.000 | Sim |
| `combustivel_tambor` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `apoio_acampamento` | P1 | monomotor utilitário / bush | 1.200 | Não |
| `evacuacao_por_do_sol`| P1 | monomotor utilitário / bush | 1.200 | Não |

---

## Resultados do `tier-report`

### Tabela 1: Contagem e % por Tier (Mundo e Brasil)
Base total: 38.598 aeródromos analisados.

| Tier | Mundo (Qtd) | Mundo (%) | Alvo Mundo | Brasil (Qtd) | Brasil (%) |
|---|---|---|---|---|---|
| **Tier 1** | 1,013 | 2.6% | ≈ 400–600 | 36 | 1.1% |
| **Tier 2** | 2,107 | 5.5% | ≈ 8–10 % | 74 | 2.3% |
| **Tier 3** | 13,671 | 35.4% | ≈ 25 % | 951 | 29.2% |
| **Tier 4** | 21,807 | 56.5% | resto (≈ 60 %) | 2,198 | 67.4% |

> **Nota sobre o alvo:** No mundo, Tier 1 atingiu 1.013 aeroportos (2,6%) e Tier 2 atingiu 2.107 (5,5%). No Brasil, a distribuição ficou muito próxima do real: 36 aeroportos em Tier 1 (1,1%), 74 em Tier 2 (2,3%), 951 em Tier 3 (29,2%) e 2.198 em Tier 4 (67,4%).

### Tabela 2: Casos de Referência do Plano

| ICAO | Nome | Tier Novo | Tier Antigo | Score Total | Tag Dominante (Pts) | Estrutura (ILS/App/Gates/Cgo/Twr/Rw) | OurAirports (Tipo/Sched) |
|---|---|---|---|---|---|---|---|
| **SBGR** | AITA de Guarulhos | **1** | 1 | **260** | linha:mega_hub (100) | 115 (ILS:32, App:20, Gt:40, Cgo:0, Twr:8, Rw:15) | 45 (Typ:30, Sched:15) |
| **SBKP** | AITA de Viracopos | **1** | 1 | **191** | linha:domestico (70) | 91 (ILS:8, App:20, Gt:40, Cgo:0, Twr:8, Rw:15) | 30 (Typ:15, Sched:15) |
| **SBSP** | AITA de Congonhas | **1** | 2 | **152** | linha:domestico (70) | 52 (ILS:16, App:20, Gt:0, Cgo:0, Twr:8, Rw:8) | 30 (Typ:15, Sched:15) |
| **SBRJ** | Aeroporto Santos Dumont | **1** | 3 | **147** | linha:domestico (70) | 47 (ILS:0, App:20, Gt:16, Cgo:0, Twr:8, Rw:3) | 30 (Typ:15, Sched:15) |
| **KJFK** | AITA JFK | **1** | 1 | **260** | linha:mega_hub (100) | 115 (ILS:32, App:20, Gt:40, Cgo:0, Twr:8, Rw:15) | 45 (Typ:30, Sched:15) |
| **EGLL** | Aeroporto de Heathrow | **1** | 1 | **260** | linha:mega_hub (100) | 115 (ILS:32, App:20, Gt:40, Cgo:0, Twr:8, Rw:15) | 45 (Typ:30, Sched:15) |
| **SBMT** | Aeroporto Campo de Marte | **2** | 2 | **56** | executivo (25) | 16 (ILS:0, App:0, Gt:0, Cgo:0, Twr:8, Rw:8) | 15 (Typ:15, Sched:0) |
| **SDCO** | Aeroporto de Sorocaba | **2** | 2 | **58** | executivo (25) | 18 (ILS:0, App:2, Gt:0, Cgo:0, Twr:8, Rw:8) | 15 (Typ:0, Sched:15) |
| **SDAO** | Aeroporto da Fazenda Portela | **4** | 3 | **0** | nenhuma (0) | 0 (ILS:0, App:0, Gt:0, Cgo:0, Twr:0, Rw:0) | 0 (Typ:0, Sched:0) |
| **PABE** | Aeroporto de Bethel | **1** | 2 | **99** | alimentador (35) | 34 (ILS:8, App:10, Gt:0, Cgo:0, Twr:8, Rw:8) | 30 (Typ:15, Sched:15) |

---

## Comparativo no Quadro de Missões de SBMT (`board-report --boards 50 --seed 7`)

Em SBMT (Campo de Marte, pista de 5.120 ft asfaltada):

| Tipo de Missão | Antes (Regra Antiga) | Depois (TIER-A) | Comportamento Observado |
|---|---|---|---|
| `longo_curso` | Presente / Comum | **0.0%** (0 missões) | **Totalmente eliminado** de SBMT, pois todos os perfis exigem pista $\ge$ 8.000 ft. |
| `ponte_aerea` | Presente | **1.3%** | Apenas o perfil narrowbody (4.300 ft) é gerado para destinos compatíveis ($\ge$ 4.300 ft pavimentada). |
| `carga_paletizada` | Presente | **1.0%** | Perfis pesados widebody (8.000 ft) foram eliminados; apenas o perfil 1 de narrowbody cargueiro pôde ser gerado. |

---

## Critérios de aceite

- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` passa (997 testes aprovados).
- [x] `dotnet run --project src/FlightCareer.Replay -- tier-report` roda e gera tabelas 1 e 2 no relatório.
- [x] `dotnet run --project src/FlightCareer.Replay -- board-report --boards 50 --seed 7` roda; comparativo de SBMT antes × depois reportado.
- [x] Nenhum `catch {}` silencioso; aleatoriedade apenas por fonte injetada.
- [x] Não rodados `board-report --coverage` nem `career-stab` lentos.
- [x] Entrada no fim do `docs/CHANGELOG.md` e atualização da seção 7 de `AGENTS.md`.
- [x] Commits divididos em `feat(missoes): pista minima por perfil (TIER-A)` e `feat(aeroportos): tier por movimento (TIER-A)`.

---

## Build e testes

- `dotnet build src/FlightCareer.slnx`: **0 erros**, 0 warnings novos (apenas 3 avisos conhecidos pré-existentes na suíte de testes de components).
- `dotnet test src/FlightCareer.Tests`: **997 aprovados**, 0 falhas.
  - Novos testes aprovados:
    - `AirportTierClassifierTests`:
      - `AirportTierClassifier_MegaHub_Tier1`
      - `AirportTierClassifier_PonteAereaPistaCurta_Tier1`
      - `AirportTierClassifier_AeroportoExecutivo_Tier2`
      - `AirportTierClassifier_Fazenda_Tier4`
      - `AirportTierClassifier_SemPavimento_NoMaximoTier3`
      - `AirportTierClassifier_IgnoraEtiquetasHeuristicas`
    - `MissionRunwayProfileTests`:
      - `MissionBoardCore_OrigemPistaCurta_SemPerfilWidebody`
      - `MissionBoardCore_DestinoRespeitaPistaDoPerfil`
      - `MissionCatalog_TodosOsPerfisTemPistaMinima`
    - Snapshot regravado: `MissionCatalogEquivalenceTests.GoldenSnapshot_MatchesCurrentGeneration`.

---

## Não testado

- Não foi executado voo no Microsoft Flight Simulator (não aplicável a esta tarefa de geração procedural e classificação de dados).
- Telas de UI do Hangar/Missões não foram abertas visualmente (apenas testadas as camadas de Domínio, Aplicação e Infraestrutura/DB).

---

## Dúvidas e decisões para o revisor

1. **Classificação de pistas no Little Navmap (`num_runway_hard` vs `longest_runway_surface`):**
   - Notou-se que grandes aeroportos internacionais no MSFS (ex.: KJFK) reportam `longest_runway_surface = "UNKNOWN"` na tabela do LNM, embora possuam `num_runway_hard > 0`. Para evitar que KJFK e similares fossem marcados incorretamente como pistas de terra (o que ativaria o teto de Tier 3), a regra adotada em C# considera pavimentada qualquer pista com superfície reconhecida (`A`, `C`, `B`, `T`) OU quando `num_runway_hard > 0` e a superfície não for explicitamente grama/terra (`G`, `D`, `Grass`, etc.).
2. **Proporção mundial de Tier 1 e 2:**
   - A contagem de Tier 1 no mundo resultou em 1.013 aeroportos (2,6%), ligeiramente acima do teto de referência de 400–600 aeródromos previsto na estimativa inicial. Isso ocorre porque o simulador possui bases militares e aeroportos regionais de médio porte com procedimentos IFR avançados, ILS e torre. Como orientado na especificação, não alteramos os pesos nem os limites de corte para que o revisor avalie se deseja ajustar o corte de Tier 1 (por exemplo, subindo de 90 para 100 ou 110) ou se a distribuição atual está adequada.
