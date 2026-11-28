# OFICINA-TEMPO-JOGO — Parada da oficina só em tempo de jogo

**Branch:** feat/oficina-tempo-jogo · **Commit:** feat(oficina): parada da oficina só em tempo de jogo (OFICINA-TEMPO-JOGO) · **Data:** 2026-11-28

## Plano (antes de codar)
- Completar a implementação iniciada no commit WIP `8f12df7` seguindo `docs/plans/spec-OFICINA-TEMPO-JOGO.md`.
- Atualizar comentário residual em `HangarViewModel.RunPaidServiceAsync` e substituir os testes legados em `WorkshopDowntimeTests` e `ComponentPersistenceTests`.
- Criar `MissionBoardAgingTests` com SQLite em memória cobrindo os 6 cenários da spec (`AdvanceAsync`).
- Atualizar `docs/CHANGELOG.md`, `docs/sistemas/manutencao-hangar.md`, `docs/sistemas/missoes-quadro.md` e a linha de estado em `docs/plans/manutencao-parada-real.md`.
- Validar build completo da solução, suíte de testes xUnit e grep dos símbolos removidos.

## O que foi feito
- `src/FlightCareer.Domain/Components/WorkshopDowntime.cs` (em `8f12df7`): removidos `MinRealMinutes`, `MaxRealMinutes`, `RealMinutesFor`, `IsInWorkshop`, `Remaining` e `CalculateNewUntilUtc`; mantido `ExpressCostMultiplier = 1.5`, atualizado `Apply(double gameHours, bool express)` para `(double gameHours, double costFactor)` e adicionados `FormatHours` (`pt-BR`) e `CompletionSummary`.
- `src/FlightCareer.Application/Services/MissionBoardAging.cs` (em `8f12df7`): criada classe estática com `AdvanceAsync(IAppDbContext context, double gameHours, DateTime now)` para encurtar `ExpirationTime` de missões `!IsAccepted`, remover as expiradas (`< now`) sem chamar `SaveChanges` e retornar apenas as recém-vencidas.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs` (em `8f12df7` + ajuste de comentário): removidos `realMinutes`, gravação de `WorkshopUntilUtc`/`WorkshopDescription` e `_workshopTimer`/`UpdateWorkshopTimer`/`OnWorkshopTimerTick`; integrados `MissionBoardAging.AdvanceAsync`, `RefreshMainAfterWorkshopAsync` e `WorkshopDowntime.CompletionSummary` em `RunPaidServiceAsync`.
- `src/FlightCareer.UI/Views/ThemedDialog.cs` (em `8f12df7`): `ConfirmService` alterado para receber `normalHours`/`expressHours` e exibir botões formatados com `WorkshopDowntime.FormatHours`.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs` (em `8f12df7`): adicionado `RefreshAfterWorkshopAsync()`, removida a releitura/bloqueio da oficina em `StartFlight` e removidas as atribuições `Workshop*` em `RefreshBriefingAircraftAsync`.
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`, `PilotAircraftWrapper.cs`, `Views/HangarView.xaml`, `Domain/Entities/PilotAircraft.cs` e `Infrastructure/Persistence/AppDbContext.cs` (em `8f12df7`): removidas propriedades, faixa de oficina, status "OFICINA", bloqueios `isInWorkshop` e os dois `ALTER TABLE` de oficina.
- `src/FlightCareer.Tests/Components/WorkshopDowntimeTests.cs`: substituídos os testes antigos pelos 4 testes novos (`Apply_ServicoNormal_MantemHorasECustoPadrao`, `Apply_ServicoExpresso_ReduzMetadeHorasECustaMais50Pct`, `FormatHours_FormataEmPtBr`, `CompletionSummary_ComZeroUmEMultiplosVencidos`), mantendo `MaintenancePricing_RealHours_SempreMultiplicaPorQuatro` e `MaintenancePricing_RealCycles_MultiplicaPorQuatro`.
- `src/FlightCareer.Tests/Missions/MissionBoardAgingTests.cs`: criados os 6 testes com SQLite em memória e releitura em contexto novo (`AdvanceAsync_ContratoComFolga_PerdeValidade`, `AdvanceAsync_ContratoCurto_VenceESai`, `AdvanceAsync_ContratoAceito_NaoMuda`, `AdvanceAsync_JaVencido_SaiSemContar`, `AdvanceAsync_OrigensDiferentes_EnvelhecemJuntas`, `AdvanceAsync_HorasZeroOuNegativas_NadaMuda`).
- `src/FlightCareer.Tests/Components/ComponentPersistenceTests.cs`: removido o bloco "Colunas de oficina (PARADA-REAL)".
- Documentação: atualizados `docs/sistemas/manutencao-hangar.md`, `docs/sistemas/missoes-quadro.md`, `docs/plans/manutencao-parada-real.md` (linha `Estado`) e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.334 aprovados, 0 falhas).
- [x] `git grep -nE "WorkshopUntilUtc|WorkshopDescription|IsInWorkshop|RealMinutes|realMinutes" -- src/ ':!**/bin/**' ':!**/obj/**'` sem resultados.
- [x] `missions_golden.json` não mudou (verificado via `git diff --stat 7123f99`).
- [x] Relatório declara: não testado com o app aberto nem no MSFS.
- [x] `docs/CHANGELOG.md`, `docs/sistemas/manutencao-hangar.md`, `docs/sistemas/missoes-quadro.md` e `docs/plans/manutencao-parada-real.md` atualizados.
- [x] Commit único criado na branch `feat/oficina-tempo-jogo`: `feat(oficina): parada da oficina só em tempo de jogo (OFICINA-TEMPO-JOGO)`.

## Build e testes
- dotnet build (`src/FlightCareer.slnx -v q -nologo`): 0 erros, 81 avisos pré-existentes (nenhum aviso novo nos arquivos alterados).
- dotnet test (`src/FlightCareer.Tests --no-build`): 1.334 aprovados, 0 falhas, 0 ignorados (6 novos em `MissionBoardAgingTests`, 4 novos + 2 mantidos em `WorkshopDowntimeTests`).

## Não testado
- Não testado com o app aberto nem no MSFS (fluxo visual do diálogo de atendimento no Hangar, atualização do quadro após serviço pago na janela principal e conferência no briefing).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. A tarefa foi retomada sobre o commit WIP `8f12df7` e concluída conforme a spec.
