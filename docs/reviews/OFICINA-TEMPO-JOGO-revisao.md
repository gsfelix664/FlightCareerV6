# Revisão OFICINA-TEMPO-JOGO — APROVADO (1ª rodada, 2026-11-28, Gemini)

Commits revisados: `8f12df7` + `0ae6ad9` (`feat/oficina-tempo-jogo`).

- Escopo conforme `docs/plans/spec-OFICINA-TEMPO-JOGO.md`; nenhum arquivo fora do escopo tocado e `missions_golden.json` inalterado.
- Arquivos quentes lidos linha a linha:
  - `MainViewModel.cs`: apenas `RefreshAfterWorkshopAsync()`, remoção da releitura `freshAircraft`/bloqueio de oficina em `StartFlight` e remoção das 4 atribuições `Workshop*` em `RefreshBriefingAircraftAsync`.
  - `BriefingViewModel.cs`: removidas as duas `[ObservableProperty]`, os dois `partial void` e toda a lógica `isInWorkshop` em `ValidatePreFlightConditions`; condição `aircraftOk` restaurada.
  - `AppDbContext.cs`: removidos apenas os dois `ALTER TABLE` de oficina em `EnsureSchemaUpdatedAsync` (sem `DROP COLUMN`).
- `WorkshopDowntime` (`Apply`, `FormatHours` pt-BR, `CompletionSummary`), `MissionBoardAging.AdvanceAsync` (sem `SaveChanges`, conta apenas recém-vencidos `!IsAccepted`), `HangarViewModel.RunPaidServiceAsync` e `ThemedDialog.ConfirmService` batem com o contrato § 4.
- `git grep` dos símbolos removidos (`WorkshopUntilUtc`, `WorkshopDescription`, `IsInWorkshop`, `RealMinutes`, `realMinutes`): 0 ocorrências em `src/`.
- Build 0 erros; 1.334/1.334 testes aprovados (4 novos + 2 mantidos em `WorkshopDowntimeTests`; 6 novos em `MissionBoardAgingTests` com SQLite em memória e releitura em novo contexto).
- Docs atualizados (`CHANGELOG.md`, `manutencao-hangar.md`, `missoes-quadro.md`, `manutencao-parada-real.md`).
- Observação menor: na linha 3 de `OFICINA-TEMPO-JOGO-relatorio.md`, o campo `Commit:` registra a mensagem em vez do hash curto (`0ae6ad9`).
- **Não conferido** com o app aberto nem no MSFS (diálogo de atendimento no Hangar, resumo de conclusão, reposição do quadro na janela principal e conferência no briefing).
