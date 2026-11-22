# PARADA-REAL — Oficina em tempo real, serviço expresso e prazos reais na tela

**Branch:** feat/parada-real · **Commit:** 5fcee86 · **Data:** 2026-11-22

## Plano (antes de codar)
- Domain: adicionar `CompressionFactor = 4.0`, `RealHours` e `RealCycles` em `MaintenancePricing`; criar `Domain/Components/WorkshopDowntime.cs` com cálculo puro de minutos reais, serviço expresso e checagem de oficina.
- Entidade e banco: adicionar `WorkshopUntilUtc` e `WorkshopDescription` em `PilotAircraft` e migração idempotente em `AppDbContext.EnsureSchemaUpdatedAsync`.
- Textos reais: atualizar `PartHangarView.GroupLifeInfo` e `PartInspectionFlags.LifeNote` para prazos reais (× 4) e restante em horas de voo do jogo com rótulo "de voo".
- Hangar e UI: estender `ThemedDialog` para confirmação Normal/Expresso; atualizar `HangarViewModel.RunPaidServiceAsync` para calcular minutos reais/expresso, gravar prazo de oficina e controlar `DispatcherTimer` de 1 s ativo apenas quando houver avião na oficina; exibir faixa no card e rótulos reais no XAML.
- Bloqueio de voo: bloquear início do voo em `MainViewModel.StartFlight` e checklist em `BriefingViewModel` enquanto aeronave estiver na oficina.
- Testes e validação: criar `WorkshopDowntimeTests`, atualizar testes de texto e persistência, rodar `dotnet build` e `dotnet test`.

## O que foi feito
- `src/FlightCareer.Domain/Components/WorkshopDowntime.cs`:
  - Implementação de `RealMinutesFor(double gameHours)` aplicando `clamp(round(2 × √horasDeJogo), 1, 20)` com piso 1 e teto 20 para horas positivas.
  - Implementação de `Apply(double gameHours, bool express)`: serviço expresso calcula custo × 1,5, metade das horas de jogo e metade dos minutos reais (`RealMinutesFor(gameHours / 2.0)`).
  - Implementação de `IsInWorkshop(DateTime? untilUtc, DateTime nowUtc)` e `Remaining(DateTime? untilUtc, DateTime nowUtc)`.
  - Implementação de `CalculateNewUntilUtc(DateTime? currentUntilUtc, DateTime nowUtc, int realMinutes)` garantindo que novo serviço sobre parada em andamento use a maior data de liberação (não soma).
- `src/FlightCareer.Domain/Components/MaintenancePricing.cs`:
  - Adicionado `CompressionFactor = 4.0`, `RealHours(gameHours)` e `RealCycles(gameCycles)`.
- `src/FlightCareer.Domain/Entities/PilotAircraft.cs`:
  - Adicionadas propriedades `WorkshopUntilUtc` (`DateTime?`) e `WorkshopDescription` (`string?`).
- `src/FlightCareer.Domain/Parts/PartHangarView.cs`:
  - `GroupLifeInfo` atualizado para exibir prazos da vida real multiplicados por 4 (`totalReal`) e restante em horas de voo do jogo ("TBO 2.000 h (vida real) · faltam 280 h de voo", "Troca de óleo a cada 48 h (vida real) · faltam 8 h de voo", "Revisão de ignição a cada 500 h (vida real) · faltam 120 h de voo"), além do formato vencido com rótulo "de voo".
- `src/FlightCareer.Domain/Parts/PartInspectionFlags.cs`:
  - `LifeNote` atualizado para horas e ciclos com prazo real (× 4) e restante ou vencimento com rótulo "de voo" ("Vida 400 h (vida real) · faltam 10 h de voo", "Ciclos 400 (vida real) · faltam 10 ciclos de voo").
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`:
  - Inclusão dos comandos idempotentes em `EnsureSchemaUpdatedAsync` adicionando as colunas `WorkshopUntilUtc` e `WorkshopDescription` na tabela `PilotAircrafts`.
- `src/FlightCareer.UI/Views/ThemedDialog.cs`:
  - Novo método `ConfirmService` exibindo diálogo temático com botões `Cancelar`, `Normal (X min) · $XXX` e `⚡ Expresso (Y min) · $YYY` (+50% custo).
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`:
  - Propriedades de oficina `IsInWorkshop`, `WorkshopRemaining`, `WorkshopRemainingFormatted`, `WorkshopBannerText` e método `RefreshWorkshop()`.
  - Ajuste de `Status` para exibir estado `OFICINA` ("Em manutenção (...). Libera em mm:ss") enquanto `IsInWorkshop` estiver ativo.
  - Propriedades com rótulos calculados de prazos reais para o XAML: `OilChangeLabel`, `AnnualInspectionLabel`, `IgnitionRevisionLabel` e `TboLabel`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`:
  - `RunPaidServiceAsync` suporta parada em tempo real e escolha Normal/Expresso, gravando `WorkshopUntilUtc` e `WorkshopDescription`.
  - `DispatcherTimer` de 1 s em `UpdateWorkshopTimer()` e `OnWorkshopTimerTick()`, ativo unicamente enquanto houver avião na oficina na frota.
- `src/FlightCareer.UI/Views/HangarView.xaml`:
  - Faixa visual de oficina no card da aeronave ("⏱ Na oficina — libera em mm:ss (serviço)").
  - Rótulos de inspeções vinculados aos labels do Domain/Wrapper (zero números fixos de prazo no XAML).
  - Legenda informativa fixa: "Os prazos de manutenção são os da vida real; o jogo conta as horas em ritmo acelerado (×4) para caber na carreira."
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`:
  - Em `StartFlight`, releitura fresca da aeronave ativa do banco (`AsNoTracking`) e bloqueio com alerta modal se estiver na oficina ("A aeronave {prefixo} está na oficina ({descrição}). Libera em mm:ss.").
  - Repasse de `WorkshopUntilUtc` e `WorkshopDescription` para o `_briefingViewModel`.
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`:
  - Em `ValidatePreFlightConditions()`, se a aeronave ativa estiver na oficina, o item 0 (MODELO DA AERONAVE) é marcado como bloqueado/divergente ("Na oficina — libera em mm:ss"), o badge mostra "NA OFICINA" e o despacho de voo é impedido.
- Testes:
  - Criação de `WorkshopDowntimeTests.cs` cobrindo todas as conversões de minutos (piso 1 min, teto 20 min, 2h, 8h, 72h, 150h), serviço expresso, relógio injetado e cálculo de maior prazo sobre parada em andamento.
  - Atualização dos testes existentes em `PartDisplayStateTests.cs`, `PartInspectionFlagsTests.cs`, `PartHealthBandsAndFailuresTests.cs` e asserção de colunas em banco antigo em `ComponentPersistenceTests.cs`.
- Documentação:
  - Atualização da seção 7 (Manutenção) em `AGENTS.md`.
  - Nova entrada no fim de `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] Build 0 erros; `dotnet test` 100 %.
- [x] Avião na oficina não inicia voo (mensagem) e aparece bloqueado no briefing; libera sozinho ao fim do tempo, mesmo com o app fechado no meio.
- [x] Expresso cobra +50 % e reduz tempos à metade.
- [x] Nenhum número de prazo fixo no XAML; todos vêm do Domain × 4.
- [x] CHANGELOG e AGENTS.md › 7 (Manutenção). Relatório diz "não visto na tela / não testado no MSFS".
- [x] Um commit: `feat(hangar): oficina em tempo real, servico expresso e prazos reais (PARADA-REAL)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 warnings conhecidos (vulnerabilidades em pacotes transitivos SQLitePCLRaw/SqlClient herdados do projeto).
- `dotnet test src/FlightCareer.Tests`: 770 aprovados, 0 falhas, 0 pulados (novos: 6 testes em `WorkshopDowntimeTests`, asserções estendidas em `ComponentPersistenceTests`).

## Não testado
- Não visto na tela / não testado no MSFS (execução com MSFS 2024 conectado e renderização WPF interativa).

## Dúvidas e decisões para o revisor
- Nenhuma. Todas as decisões seguiram a spec e o plano aprovado `manutencao-parada-real.md`.
