# FASES-2B — Contratos da fase 2: levantamento/fotografia aérea e lançamento de paraquedistas

**Branch:** feat/fases-2b · **Commit:** a ser gerado · **Data:** 2026-11-22

## Plano (antes de codar)
- Adicionar coluna `ContractData` (TEXT DEFAULT NULL) em `Mission` e `AppDbContext.EnsureSchemaUpdatedAsync`.
- Criar DTOs `SurveyContractData` e `SkydiveContractData` em `FlightCareer.Domain/Missions/ContractData.cs`.
- Implementar rastreadores de domínio `SurveyLegTracker` e `SkydiveDropTracker` em `FlightCareer.Domain/Missions/`.
- Estender `MissionCompletion.Evaluate` para validar pernas de levantamento (3.6.1) e lançamento estável sobre a zona (3.6.2), gerando aviso de choque térmico sem reprovação.
- Expor `LatestChtCoolingRate` em `PartFlightTracker` e `PartFlightSession` para alimentação do monitor.
- Configurar subtipos 3.6.1 e 3.6.2 em `MissionRules.cs` (categoria 3.6 SAE), `career_phases.json` (fase 2), `mission_pay.json` (famílias aerofoto e paraquedismo com paxAtLeastOne) e `xp_rules.json` (M_Tipo 1.15).
- Atualizar `MissionGenerator` para gerar as duas missões com geometria e parâmetros no `ContractData`.
- Conectar `FlightMonitor` aos rastreadores e repassar ao `FlightService.ProcessFlightFinishAsync`, isentando paraquedistas da taxa de embarque no pouso.
- Criar suíte completa de testes unitários para trackers, completion, regras e persistência, rodar build e fuzz.

## O que foi feito
- `src/FlightCareer.Domain/Missions/ContractData.cs`: criados DTOs `SurveyContractData` (geometria da área retangular, N pernas, targets de altitude e velocidade) e `SkydiveContractData` (coordenadas e elevação da drop zone, altitude de salto AGL e velocidade máxima).
- `src/FlightCareer.Domain/Entities/Mission.cs`: adicionada a propriedade `ContractData` (string? serializada em JSON).
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: adicionada 1 linha em `EnsureSchemaUpdatedAsync` adicionando idempotentemente `ALTER TABLE Missions ADD COLUMN ContractData TEXT DEFAULT NULL;`.
- `src/FlightCareer.Domain/Missions/SurveyLegTracker.cs`: rastreador puro com projeção cartesiana orientada nos eixos $u$ (longitudinal) e $v$ (transversal), validação contínua de tolerâncias (altitude ±150 ft, velocidade ±10 kt, rumo ±15°) durante $\ge 80\%$ do percurso dentro do retângulo e detecção de travessia de borda a borda ($\Delta u \ge 0.75 \times L$).
- `src/FlightCareer.Domain/Missions/SkydiveDropTracker.cs`: rastreador da janela contínua de 30 s sobre a zona ($\le 2$ NM, altitude ±200 ft, IAS $\le 90$ kt) para marcar o salto como feito, e monitoramento de choque térmico da CHT na descida subsequente.
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: estendido `MissionCompletion.Evaluate` para checar pernas de aerofoto (`3.6.1`) e lançamento sobre a zona (`3.6.2`), gerando motivos claros de reprovação ("X de Y pernas", "não estabilizou na altitude de salto") e alertas informativos de choque térmico no `Warning` sem reprovação.
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs` e `src/FlightCareer.UI/Services/PartFlightSession.cs`: exposta a propriedade `LatestChtCoolingRate` calculada pelo filtro móvel de 60 s da CHT.
- `src/FlightCareer.Domain/Economy/MissionPayTable.cs`: adicionado suporte a `BaseFeePerPax` em `MissionPayFamily` e famílias `aerofoto` e `paraquedismo` nos defaults.
- `src/FlightCareer.Domain/Xp/XpRules.cs`: adicionados multiplicadores `aerofoto` (1.15) e `paraquedismo` (1.15) nos padrões embutidos.
- `src/FlightCareer.Infrastructure/Data/career_phases.json`: mapeados `3.6.1` e `3.6.2` para a fase 2 (SAE).
- `src/FlightCareer.Infrastructure/Data/mission_pay.json`: calibradas famílias `aerofoto` (base 600, 6,0/NM, escala 1,0) e `paraquedismo` (base 120 por saltador via `BaseFeePerPax`, 4,0/NM, `paxAtLeastOne`, escala 1,0).
- `src/FlightCareer.Infrastructure/Data/xp_rules.json`: configurado `M_Tipo` de 1,15 para `aerofoto` e `paraquedismo`.
- `src/FlightCareer.Application/Services/MissionRules.cs`: adicionada categoria `3.6` (Serviços Aéreos Especializados - SAE) nos Tiers 2, 3 e 4 com subcategorias `3.6.1` e `3.6.2`.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: implementados `CreateSurveyMissionSync` e `CreateSkydiveMissionSync`, gerando contratos com descrições detalhadas de alvos e parâmetros serializados em `ContractData`. `CalculateReward` atualizado para incorporar `(effectivePax * family.BaseFeePerPax)` à tarifa base.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: inicialização de `_surveyTracker` e `_skydiveTracker` em `StartMonitoring`, atualização no tick de telemetria e repasse das métricas operacionais para `FlightService.ProcessFlightFinishAsync`.
- `src/FlightCareer.Application/Services/FlightService.cs`: chamada de `MissionCompletion.Evaluate` estendida, isenção de paraquedistas da taxa de embarque no pouso (`SubId != "3.6.2"`) e inclusão de avisos de choque térmico no `outcome.Message`.
- `src/FlightCareer.Tests/Missions/Phase2ContractTests.cs`: suíte de 13 testes unitários cobrindo todos os requisitos e casos de borda.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx`
- [x] dotnet test 100% — 886/886 aprovados (0 falhas)
- [x] Fuzz --runs 10 sem regressão — 10/10 voos aprovados (100%), 0 invariantes violadas
- [x] UndeliveredMissionTests continua passando — 2/2 aprovados
- [x] Arquivos quentes só no indicado — `AppDbContext.cs` (1 linha de ALTER TABLE), `FlightMonitor.cs` (wiring dos trackers)
- [x] CHANGELOG e AGENTS.md › 7 atualizados
- [x] Um commit: `feat(missoes): levantamento aereo e lancamento de paraquedistas (FASES-2B)`

## Build e testes
- dotnet build: 0 erros, 30 avisos (apenas avisos de propriedades obsoletas e nullability pré-existentes)
- dotnet test: 886 aprovados, 0 falhas (13 novos testes em `Phase2ContractTests.cs`)
- fuzz testing: 10 corridas com sucesso em 4 modelos de aeronaves (TBM 930, Baron G58, C172SP G1000, C152), 0 falhas

## Não testado
- Comportamento em tempo real dentro do simulador MSFS e visualização na interface gráfica WPF (testado via suíte automatizada de testes de domínio, gerador, migração de banco e fuzzing).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva.
