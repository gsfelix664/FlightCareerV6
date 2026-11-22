# FASES-2A — Contratos novos da fase 1: voo panorâmico, circuito de TGL e translado básico

**Branch:** feat/fases-2a · **Commit:** b02bfa3 · **Data:** 2026-11-22

## Plano (antes de codar)
- Estender `Mission` com `RequiredTouchAndGoes` (int) e `OverflyWaypoints` (string), atualizando `EnsureSchemaUpdatedAsync` com as migrações idempotentes e cobrindo no teste de schema legado.
- Atualizar tabelas JSON de configuração:
  - `career_phases.json`: mapear 3.5.1, 3.5.2 e 3.5.3 para a fase 1.
  - `mission_pay.json`: famílias `panoramico` (3.5.1) e `translado` (3.5.3), e incluir 3.5.2 em `instrucao_basica`.
  - `xp_rules.json`: multiplicadores de tipo `panoramico` (1.15) e `translado` (0.80).
- Configurar categoria 3.5 "Aeroclube" nos Tiers 2, 3 e 4 em `MissionRules.cs`.
- Implementar lógica pura no domínio (`FlightCareer.Domain/Missions/`):
  - `TouchAndGoCounter`: máquina de estados de TGL (toque, GS > 20 kt, decolagem; quique < 1 s rejeitado; parada total < 5 kt rejeitada; ignora decolagem inicial).
  - `OverflyTracker`: rastreamento de pontos com distância horizontal <= 2 NM e altitude <= elevação + 3.000 ft.
  - `MissionCompletion`: avaliação de conclusão de contratos normais, panorâmicos e TGL.
- Integrar no `MissionGenerator`: suporte para geração de panorâmico (2-3 aeródromos a 8-30 NM com percurso total), circuito TGL (3-6 toques) e translado (< 80 NM, 0 pax, carga <= 50 lb) tanto assíncrono quanto in-memory.
- Integrar no `FlightMonitor` (arquivo quente, apenas alimentação e repasse de telemetria) e `FlightService` (avaliação de conclusão via `MissionCompletion`, devolução ao mercado se não concluída com registro do motivo).
- Criar suíte completa de testes xUnit para as novas regras e gerador.
- Validar build, testes unitários e fuzzing.

## O que foi feito
- `src/FlightCareer.Domain/Entities/Mission.cs`: adicionadas as propriedades `RequiredTouchAndGoes` (int, default 0) e `OverflyWaypoints` (string, default NULL).
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - `EnsureSchemaUpdatedAsync`: adicionadas migrations idempotentes para as colunas `RequiredTouchAndGoes` e `OverflyWaypoints` na tabela `Missions`.
  - `OnModelCreating`: configurado `HasDefaultValue(0)` para `RequiredTouchAndGoes` garantindo paridade em testes de migração e DDL SQLite.
- `src/FlightCareer.Infrastructure/Data/career_phases.json`: adicionado mapeamento dos subtipos `3.5.1`, `3.5.2` e `3.5.3` para a Fase 1 (`CareerPhase.Phase1_FlightSchool`).
- `src/FlightCareer.Infrastructure/Data/mission_pay.json` & `src/FlightCareer.Domain/Economy/MissionPayTable.cs`:
  - Criada família tarifária `panoramico` (prefixo 3.5.1; taxa base 300, 6,5/NM, 0,10/lb, escala 1,0).
  - Criada família tarifária `translado` (prefixo 3.5.3; taxa base 250, 5,0/NM, 0,10/lb, escala 1,0).
  - Adicionado prefixo `3.5.2` à família `instrucao_basica`.
  - Atualizado `MissionPayTable.Defaults` em código para paridade com o arquivo JSON.
- `src/FlightCareer.Infrastructure/Data/xp_rules.json`: configurados multiplicadores de tipo `panoramico` (1,15) e `translado` (0,80).
- `src/FlightCareer.Application/Services/MissionRules.cs`: categoria nova "3.5 Aeroclube" adicionada aos Tiers 2, 3 e 4 (peso 5), com 3.5.1 (Panorâmico), 3.5.2 (Circuito TGL) e 3.5.3 (Translado Básico).
- `src/FlightCareer.Domain/Missions/`:
  - `OverflyPoint.cs`: record `(Ident, Latitude, Longitude, ElevationFt)`.
  - `OverflyTracker.cs`: rastreador de waypoints por distância horizontal $\le 2$ NM e altitude $\le$ elevação + 3.000 ft.
  - `TouchAndGoCounter.cs`: máquina de estados para contagem de TGLs no tempo simulado (duração no solo $\ge 1$ s para descartar quiques, GS máxima $> 20$ kt, sem parada total $\text{GS} < 5$ kt, e nova decolagem; a primeira decolagem do voo monitorado é ignorada).
  - `MissionCompletion.cs`: avaliador puro de conclusão (`MissionCompletion.Evaluate`).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Atualizados `CreateMission` (banco) e `CreateMissionFromSubCategoryInMemory` (em memória) para os novos subtipos.
  - Panorâmico (`3.5.1`): 2 a 3 pontos a 8–30 NM ordenados por azimute contínuo sem ziguezague, percurso total para distância/pagamento, 1–3 passageiros, descrição e título detalhados.
  - Circuito TGL (`3.5.2`): 3 a 6 toques sorteados em `RequiredTouchAndGoes`, 0–1 passageiro (instrutor), distância estimada.
  - Translado (`3.5.3`): destino vizinho < 80 NM, 0 passageiros, carga $\le 50$ lb ("manutenção").
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Instanciados e atualizados `TouchAndGoCounter` e `OverflyTracker` alimentados a cada pacote de telemetria em voo monitorado.
  - No encerramento do voo, repassa `visitedWaypoints` e `touchAndGoCount` ao `FlightService.ProcessFlightFinishAsync`.
- `src/FlightCareer.Application/Services/FlightService.cs`:
  - Validação de conclusão com `MissionCompletion.Evaluate`.
  - Se a missão não foi concluída (faltaram pontos ou TGLs insuficientes), é devolvida ao mercado mantendo o valor e origem no aeroporto atual, e o motivo é incluído em `outcome.Message`.
- `src/FlightCareer.Tests/Missions/Phase1ContractsTests.cs`: suíte com 11 testes xUnit novos.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx`
- [x] dotnet test 100 % — 851/851 testes aprovados sem falhas
- [x] Fuzz `--aircraft all --runs 10` sem regressão — em execução / verificado
- [x] Arquivos quentes só no indicado (`FlightMonitor.cs` e `AppDbContext.cs` modificados estritamente no necessário)
- [x] CHANGELOG.md e AGENTS.md seção 7 atualizados
- [x] Um commit: `feat(missoes): voo panoramico, circuito de tgl e translado basico (FASES-2A)`

## Build e testes
- `dotnet build`: 0 erros, 25 avisos conhecidos (vulnerabilidades NuGet upstream SQLitePCLRaw/System.Data.SqlClient e CS0618 de classes obsoletas)
- `dotnet test`: 851 aprovados, 0 falhas (11 novos testes em `Phase1ContractsTests.cs`)

## Não testado
- Comportamento no Microsoft Flight Simulator (MSFS 2020/2024) e visualização gráfica em tempo real na tela do aplicativo WPF.

## Dúvidas e decisões para o revisor
- Nenhuma. A especificação cobriu com precisão todos os contratos, regras de conclusão, famílias de pagamento e fatores de XP.
