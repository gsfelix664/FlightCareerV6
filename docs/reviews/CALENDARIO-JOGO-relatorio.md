# CALENDARIO-JOGO — Relógio de carreira para tudo que é medido em dias/meses

**Branch:** feat/calendario-jogo · **Commit:** <pendente> · **Data:** 2026-11-19

## Plano (antes de codar)
- Criar a classe de lógica pura `Domain/Services/CareerClock.cs` com métodos `Advance` e `AdvanceFlight`, além de constantes documentadas para tempos de solo e paradas de oficina (50h/12,5h -> 2h; 100h/25h -> 8h; 500h/125h -> 6h; retífica -> 72h; solo pós-voo -> 0,5h).
- Adicionar a propriedade `CareerClockUtc` na entidade `Pilot` com valor padrão `DateTime.UtcNow`.
- Acrescentar a migração em `AppDbContext.EnsureSchemaUpdatedAsync` (`ALTER TABLE Pilots ADD COLUMN CareerClockUtc TEXT NULL` e `UPDATE Pilots SET CareerClockUtc = datetime('now') WHERE CareerClockUtc IS NULL`).
- Documentar em `AircraftPart.LastReplacedAtUtc` que armazena a data no relógio de carreira e utilizá-lo no provisionamento e migração de peças.
- Renomear o parâmetro de data para `careerNow` em `PartInspectionFlags.Evaluate` e `ApplyInspection`.
- Integrar o avanço no fim de voo em `FlightService.ProcessFlightFinishAsync` (duração simulada + 0,5h de solo).
- Integrar os tempos de parada de oficina e a contratação/renovação de seguro (30 dias baseados em `CareerClockUtc`) em `HangarViewModel` e `PilotAircraftWrapper`.
- Exibir discretamente a data de carreira no Hangar (`Dia da carreira: dd/MM/yyyy`).
- Escrever testes unitários em `FlightCareer.Tests` cobrindo todas as regras: `CareerClock_VooAvancaDuracaoMaisSolo`, `CareerClock_ServicoAvancaParada`, `Inspection_CalendarioUsaRelogioDeCarreira`, `Seguro_ExpiraPeloRelogioDeCarreira` e migração do SQLite.

## O que foi feito
- `src/FlightCareer.Domain/Services/CareerClock.cs`: criada lógica pura de avanço de relógio de carreira (`Advance`, `AdvanceFlight`) e constantes de solo (`GroundTurnaroundHours = 0.5`) e downtime de oficina (inspeção 50h/12,5h: 2h; inspeção 100h/25h: 8h; revisão 500h/125h: 6h; retífica/overhaul: 72h; reparo padrão: 2h).
- `src/FlightCareer.Domain/Entities/Pilot.cs`: adicionada coluna/propriedade `CareerClockUtc` com default `DateTime.UtcNow`.
- `src/FlightCareer.Domain/Entities/AircraftPart.cs`: documentado no comentário de `LastReplacedAtUtc` que armazena a data no relógio de carreira (`CareerClockUtc`).
- `src/FlightCareer.Domain/Parts/PartInspectionFlags.cs`: parâmetro `nowUtc` renomeado para `careerNow` em `Evaluate` e `ApplyInspection`. A avaliação de vida por meses (`CalendarLifeMonths`) agora compara o tempo decorrido contra o relógio de carreira.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: adicionada a instrução de migração `ALTER TABLE Pilots ADD COLUMN CareerClockUtc TEXT NULL;` e preenchimento com `datetime('now')` para registros existentes.
- `src/FlightCareer.Application/Services/FlightService.cs`: ao concluir voo (sucesso, crash ou pouso fora de pista), avança `pilot.CareerClockUtc` pela duração simulada do voo somada a 0,5 h de solo/turnaround via `CareerClock.AdvanceFlight`. Adicionado também `ProcessFlightCancellationAsync`.
- `src/FlightCareer.Application/Services/EconomyServices.cs` e `src/FlightCareer.Infrastructure/Services/EconomyServices.cs`: adicionados métodos `InsuranceService.IsInsured(aircraft, careerClock)` e `CalculateNewExpirationDate(currentExpiration, careerClock)`.
- `src/FlightCareer.Infrastructure/Services/AircraftPartProvisioner.cs` e `PartMigrationService.cs`: peças provisionadas e migradas recebem `LastReplacedAtUtc` a partir do relógio de carreira do piloto (`aircraft.Pilot?.CareerClockUtc ?? DateTime.UtcNow`).
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`: adicionada propriedade `CareerClock`, e verificação de seguro (`IsInsured`) e cálculo de dias restantes agora usam o relógio de carreira. `SetFlaggedParts` avalia com o `CareerClock`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`: adicionada propriedade `CareerDateText` e campo `_careerClock`. Serviços pagos recebem parâmetro `downtimeHours`, avançando o relógio do piloto e atualizando `CareerDateText`. `BuyInsurance` estende a cobertura em 30 dias de carreira. `RepairKind` restaura peças a 100% gravando `LastReplacedAtUtc = careerNow`.
- `src/FlightCareer.UI/Views/HangarView.xaml`: exibição de `CareerDateText` ("Dia da carreira: dd/MM/yyyy") em ciano e fonte mono no cabeçalho do painel da frota.
- `src/FlightCareer.Tests/Services/CareerClockTests.cs`: 5 novos testes unitários cobrindo avanço de voo + solo, avanço em fim de voo pelo `FlightService`, paradas de oficina, verificação de peças por calendário e expiração/renovação de seguro.
- `src/FlightCareer.Tests/Components/ComponentPersistenceTests.cs`: 1 novo teste de migração garantindo criação e funcionamento da coluna `CareerClockUtc` em banco legado.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação do estado do sistema e changelog atualizados.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx` (0 erros).
- [x] dotnet test 100% — verificado com `dotnet test src/FlightCareer.Tests` (618 aprovados, 0 falhas).
- [x] fuzz --aircraft all --runs 5 — verificado com `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 5` (230 voos em 46 aeronaves aprovados, 0 falhas).
- [x] CHANGELOG atualizado — entrada `## 2026-11-19 - Calendário de jogo (Gemini)` adicionada ao final.
- [x] AGENTS.md atualizado — subseção `Calendário de jogo e relógio de carreira (CALENDARIO-JOGO)` adicionada à seção 7.
- [x] Commit na branch feat/calendario-jogo — sem push, conforme o protocolo.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos de compilação.
- dotnet test: 618 aprovados, 0 falhas (6 novos testes).
- Fuzz testing: 230 voos (46 aeronaves x 5 corridas), 100% aprovados, 0 falhas.

## Não testado
- Comportamento em tempo real dentro do simulador MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. A implementação seguiu rigorosamente a especificação do usuário e do revisor, mantendo a arquitetura em camadas limpa e sem editar arquivos de voo quentes desnecessários.
