# Spec OFICINA-TEMPO-JOGO — Parada da oficina só em tempo de jogo

> **Retomada:** o commit `8f12df7` da branch já fez parte do §4 (app compila). Complete o que faltar (conferindo o §4 item a item), não refaça. Faltam ao menos os testes (§5) e os docs (§6.5).

## 1. Objetivo
O serviço do Hangar sai pronto na hora (zero espera real); as horas de jogo avançam o relógio de carreira e encurtam a validade dos contratos não aceitos do quadro.

## 2. Fatos já levantados (não redescobrir; linhas aproximadas)
- Decisões: `docs/plans/manutencao-parada-real.md` › 7 e 8 (revogam a espera em minutos reais). Sistemas: `docs/sistemas/manutencao-hangar.md` e `docs/sistemas/missoes-quadro.md`.
- `Domain/Components/WorkshopDowntime.cs`: minutos reais, `Apply`, `IsInWorkshop`, `Remaining`, `CalculateNewUntilUtc`.
- `UI/ViewModels/HangarViewModel.cs`: `RunPaidServiceAsync` (l. 255–405; já avança `pilot.CareerClockUtc` com `CareerClock.Advance`), `RefreshMainBalanceAsync` (l. 408; também usado perto da l. 1150, chamada que NÃO muda), timer `_workshopTimer`/`UpdateWorkshopTimer`/`OnWorkshopTimerTick` (l. 131–190).
- `UI/ViewModels/PilotAircraftWrapper.cs`: status "OFICINA" (l. 254) e bloco "Oficina em tempo real" (l. 354–380).
- `UI/Views/HangarView.xaml` l. 242–249: faixa de oficina.
- `UI/Views/ThemedDialog.cs`: `ServiceOption` e `ConfirmService(title, message, normalCost, normalRealMinutes, expressCost, expressRealMinutes, kind)`.
- `UI/ViewModels/MainViewModel.cs`: bloqueio em `StartFlight` (l. 893–913; a releitura `freshAircraft` só serve à oficina), repasse ao briefing em `RefreshBriefingAircraftAsync` (l. 566–567 e 578–579), `RefreshPilotBalanceAsync` (l. 1114), `InitializeForPilotAsync` (l. 1129; recarrega e repõe o quadro via `_missionGenerator.GenerateMissionsAsync`).
- `UI/ViewModels/BriefingViewModel.cs`: `_workshopUntilUtc`/`_workshopDescription` e os dois `partial void On...Changed` (l. 59–63); `isInWorkshop` em `ValidatePreFlightConditions` (l. 652–705 e 845–850).
- `Domain/Entities/PilotAircraft.cs` l. 43–48: `WorkshopUntilUtc`, `WorkshopDescription`. `Infrastructure/Persistence/AppDbContext.cs` l. 159–160: os dois `ALTER TABLE`.
- Validade dos contratos: `Mission.ExpirationTime` em hora local (`DateTime.Now`). `MissionGenerator.GenerateMissionsAsync` remove os não aceitos vencidos e repõe o quadro do aeroporto. Não mude o gerador.

## 3. Arquivos
Mexer só nos listados acima, mais o novo `FlightCareer.Application/Services/MissionBoardAging.cs`, testes e docs.
**Fora do escopo:** `MissionGenerator`, `MissionBoardCore`, `FlightService`, valores do `CareerClock`, `CareerProgressionSimulator`, preços e horas de cada serviço, coluna "EXPIRAÇÃO" do quadro, catálogos JSON.

## 4. Contrato
### 4.1 `WorkshopDowntime` (mesmo arquivo e namespace; comentários passam a falar em tempo de jogo)
Remover `MinRealMinutes`, `MaxRealMinutes`, `RealMinutesFor`, `IsInWorkshop`, `Remaining`, `CalculateNewUntilUtc`. Fica:
- `public const double ExpressCostMultiplier = 1.5`
- `(double gameHours, double costFactor) Apply(double gameHours, bool express)` — expresso: metade das horas e 1,5; normal: iguais e 1,0.
- `string FormatHours(double hours)` — formato `"0.##"` em cultura pt-BR + `" h"` (72 → "72 h"; 0.5 → "0,5 h"; 0.25 → "0,25 h").
- `string CompletionSummary(double gameHours, DateTime careerClock, int expiredContracts)` — texto exato:
  `"A oficina levou {FormatHours} de carreira. Dia da carreira: {dd/MM/yyyy com InvariantCulture}.\n"` seguido de `"Nenhum contrato do quadro venceu nesse tempo."` | `"1 contrato do quadro venceu nesse tempo."` | `"{n} contratos do quadro venceram nesse tempo."`

### 4.2 `MissionBoardAging` (novo, namespace `FlightCareer.Application.Services`, `public static class`, comentário XML)
`public static async Task<int> AdvanceAsync(IAppDbContext context, double gameHours, DateTime now)`:
- `gameHours <= 0`: não faz nada, devolve 0.
- Carrega (rastreadas, uma única consulta) todas as missões com `!IsAccepted`, de todas as origens.
- Cada uma: `ExpirationTime = ExpirationTime.AddHours(-gameHours)`.
- Remove (`RemoveRange`) as que ficarem com `ExpirationTime < now`.
- Devolve quantas **venceram por causa deste avanço** (antes `>= now`, depois `< now`). As que já estavam vencidas são removidas mas não contadas.
- **Não chama `SaveChanges`**: quem chama salva junto com o serviço.

### 4.3 `HangarViewModel` (só o descrito aqui)
- `RunPaidServiceAsync`: sai `realMinutes` e tudo ligado a ele (inclusive gravar `WorkshopUntilUtc`/`WorkshopDescription` e a linha "⏱ Aeronave na oficina"). A escolha Normal/Expresso e o parâmetro `express` continuam. Mensagem de escolha:
  `"{message}\n\nEscolha o atendimento na oficina:\n• Normal: {FormatHours(normal)} de carreira\n• Expresso (+50% no preço): {FormatHours(expresso)} de carreira\n\nO avião sai pronto na hora. O tempo passa no jogo: contratos do quadro perdem validade e o prazo do seguro corre."`
- Dentro do `if (effectiveDowntimeHours > 0.0)` que avança o relógio: `expiredContracts = await MissionBoardAging.AdvanceAsync(context, effectiveDowntimeHours, DateTime.Now);` (antes do `SaveChangesAsync` único; a checagem de saldo continua antes de tudo isso).
- Depois de `LoadFleetAsync(aircraftId)`: se `effectiveDowntimeHours > 0`, chamar o novo `RefreshMainAfterWorkshopAsync()` (estático, mesmo molde e `try/catch` com `Debug.WriteLine` de `RefreshMainBalanceAsync`, chamando `main.RefreshAfterWorkshopAsync()`); senão `RefreshMainBalanceAsync()`.
- No bloco `successMessage != null`: se houve parada, acrescentar `"\n\n🕒 " + WorkshopDowntime.CompletionSummary(effectiveDowntimeHours, pilot.CareerClockUtc, expiredContracts)`. O acréscimo de "peça(s) com atenção" continua igual.
- Apagar o timer da oficina (`_workshopTimer`, `UpdateWorkshopTimer`, `OnWorkshopTimerTick` e suas chamadas).

### 4.4 `ThemedDialog.ConfirmService`
Assinatura: `(string title, string message, double normalCost, double normalHours, double expressCost, double expressHours, DialogKind kind = DialogKind.Info)`. Botões: `"Normal ({FormatHours}) · {custo:C0}"` e `"⚡ Expresso ({FormatHours}) · {custo:C0}"`.

### 4.5 `MainViewModel` (arquivo quente: leia inteiro; só estas três mudanças)
1. Novo método logo após `RefreshPilotBalanceAsync`, com resumo XML:
   `public async Task RefreshAfterWorkshopAsync()` — se `_flightMonitor.IsMonitoring` ou `PilotName` vazio: `await RefreshPilotBalanceAsync(); return;`; senão `await InitializeForPilotAsync(PilotName);`.
2. `StartFlight`: apagar a releitura `freshAircraft` e o bloco da oficina; as demais validações ficam iguais.
3. `RefreshBriefingAircraftAsync`: apagar as quatro atribuições `Workshop*`.

### 4.6 `BriefingViewModel` (arquivo quente: leia inteiro)
Apagar as duas propriedades, os dois `partial void` e toda a lógica `isInWorkshop` (a condição volta a ser `connectionOk && IsAircraftMatching(...)`; os ramos `else if (isInWorkshop)` saem e os `?:` que dependiam dele ficam só com o outro lado). Nada mais muda.

### 4.7 Wrapper, XAML, entidade e banco
Apagar o status "OFICINA", o bloco de propriedades da oficina e `RefreshWorkshop` do wrapper; a faixa no XAML; as duas propriedades de `PilotAircraft`; os dois `ALTER TABLE` do `AppDbContext` (nada mais nesse arquivo; não tente `DROP COLUMN`).

## 5. Testes
- `Components/WorkshopDowntimeTests.cs`: trocar pelos casos novos — `Apply` normal (72 → 72; 1,0) e expresso (72 → 36; 1,5. 0,5 → 0,25); `FormatHours` (72, 0.5, 0.25, 8); `CompletionSummary` com 0, 1 e 3 vencidos (texto inteiro). **Manter** os dois testes `MaintenancePricing_*`.
- Novo `Missions/MissionBoardAgingTests.cs` com `AppDbContext` SQLite em memória (padrão de `ComponentPersistenceTests`), `now` fixo, `SaveChangesAsync` e releitura num contexto novo:
  - `AdvanceAsync_ContratoComFolga_PerdeValidade`: vale 10 h, avança 2 → fica, validade `now + 8 h`, devolve 0;
  - `AdvanceAsync_ContratoCurto_VenceESai`: vale 1 h, avança 2 → removida, devolve 1;
  - `AdvanceAsync_ContratoAceito_NaoMuda`: aceita que vale 1 h, avança 72 → intacta;
  - `AdvanceAsync_JaVencido_SaiSemContar`: já vencida, avança 2 → removida, devolve 0;
  - `AdvanceAsync_OrigensDiferentes_EnvelhecemJuntas`;
  - `AdvanceAsync_HorasZeroOuNegativas_NadaMuda`.
- `ComponentPersistenceTests.cs`: apagar só o trecho "Colunas de oficina (PARADA-REAL)".

## 6. Critérios de aceite
1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
2. `grep -rnE "WorkshopUntilUtc|WorkshopDescription|IsInWorkshop|RealMinutes|realMinutes" src --include=*.cs --include=*.xaml` sem resultados (fora de `bin/` e `obj/`).
3. `missions_golden.json` **não muda**.
4. Relatório declara: não testado com o app aberto nem no MSFS.
5. `docs/CHANGELOG.md` (`## 2026-11-27 - Parada da oficina em tempo de jogo (OFICINA-TEMPO-JOGO) (Gemini)`); em `docs/sistemas/manutencao-hangar.md` reescrever a linha "Oficina em tempo real" (e o "O que faz", se preciso) para o comportamento novo; em `docs/sistemas/missoes-quadro.md` acrescentar uma linha: a oficina encurta a validade dos contratos não aceitos (`MissionBoardAging`). Em `docs/plans/manutencao-parada-real.md` mude só a linha **Estado** do topo (§§ 7 e 8 implementadas).
6. Um commit: `feat(oficina): parada da oficina só em tempo de jogo (OFICINA-TEMPO-JOGO)`.
