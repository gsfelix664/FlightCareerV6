# Spec PARADA-REAL — Oficina em tempo real, serviço expresso e prazos reais na tela

**Objetivo (1 frase):** ao fazer um serviço no Hangar, deixar o avião indisponível alguns minutos de relógio real (além do avanço do relógio de carreira que já existe), oferecer serviço expresso, e mostrar os prazos de manutenção da vida real (× 4) explicando o ritmo acelerado.

## Fatos já levantados (não redescobrir)
- Plano aprovado com todas as decisões: `docs/plans/manutencao-parada-real.md` (§ 2, § 3 e § 6). Leia antes.
- Todo serviço pago passa por `HangarViewModel.RunPaidServiceAsync(...)` (`src/FlightCareer.UI/ViewModels/HangarViewModel.cs`, ~linha 201), que recebe `downtimeHours` e hoje só faz `pilot.CareerClockUtc = CareerClock.Advance(...)`. Paradas: `CareerClock` (`Inspection50hDowntimeHours` 2, `Inspection100hDowntimeHours` 8, `Inspection500hDowntimeHours` 6, `OverhaulDowntimeHours` 72, `DefaultRepairDowntimeHours` 2); troca de peças usa a maior `DowntimeHours` do plano (`PartReplacementPlanner`); conversão de cabine 4/8/24 h (`CabinConversion`). O `HangarViewModel` usa `IDbContextFactory` (contexto curto).
- Intervalos comprimidos ÷ 4 em `Domain/Components/MaintenancePricing.cs`: `OilChangeIntervalHours` 12, `AnnualInspectionIntervalHours` 25, `IgnitionRevisionIntervalHours` 125, `PistonTboHours` 500, `TurbineTboHours` 900. Textos fixos na tela: `HangarView.xaml` ~linhas 516/526/536 ("Troca de óleo (12 h)", "Inspeção anual (25 h, obrigatória)", "Revisão de ignição (125 h, magnetos e velas)"). Texto de horas dos grupos: `PartHangarView.GroupLifeInfo` (Domain). Notas de vida das peças: `PartInspectionFlags.LifeNote`.
- Início do voo: `MainViewModel.StartFlight` (~linha 872, **arquivo quente**) e `FlightMonitor.StartMonitoring` (**quente**); briefing: `BriefingViewModel` (**quente**).

## Decisões (do plano, § 6)
1. Minutos reais = `clamp(round(2 × √horasDeJogo), 1, 20)` (ex.: óleo 2 h → 3 min; anual 8 h → 6 min; retífica 72 h → 17 min; 150 h → 20 min).
2. Serviço expresso: **+50 % no preço**, **metade** dos minutos reais e das horas de jogo.
3. Prazo real exibido = valor do jogo **× 4**, sempre (`MaintenancePricing.CompressionFactor = 4.0`).
4. O que falta mostra-se em **horas de voo do jogo**, com o rótulo "de voo": ex. "TBO 2.000 h (vida real) · faltam 280 h de voo".

## Passo a passo
1. **Domain (lógica pura, testável):**
   - `MaintenancePricing.CompressionFactor = 4.0`, `RealHours(double gameHours)`, `RealCycles(double gameCycles)`.
   - `Domain/Components/WorkshopDowntime.cs`: `static int RealMinutesFor(double gameHours)` (regra 1); `static (double gameHours, int realMinutes, double costFactor) Apply(double gameHours, bool express)` (regra 2); `static bool IsInWorkshop(DateTime? untilUtc, DateTime nowUtc)`; `static TimeSpan Remaining(DateTime? untilUtc, DateTime nowUtc)`. Sem `DateTime.Now` dentro: o relógio vem por parâmetro.
2. **Entidade e banco:** `PilotAircraft.WorkshopUntilUtc` (`DateTime?`) e `WorkshopDescription` (`string?`). `AppDbContext.EnsureSchemaUpdatedAsync` (**quente**: só acrescentar as duas linhas `ALTER TABLE PilotAircrafts ADD COLUMN ... DEFAULT NULL`). `ComponentPersistenceTests` (banco antigo) continua passando.
3. **Hangar (`HangarViewModel` / `PilotAircraftWrapper` / `HangarView.xaml`):**
   - `RunPaidServiceAsync` ganha `bool express`. Com parada > 0: aplica a regra 2 no preço e nas horas, avança o relógio de carreira (como hoje) e grava `WorkshopUntilUtc = max(atual, agora + minutos reais)` (serviço sobre parada em andamento usa a **maior**, não soma) e `WorkshopDescription`.
   - Confirmação do serviço com duas opções: "Normal (X min)" e "Expresso (+50 %, Y min)". Pode ser um diálogo com dois botões (`ThemedDialog`) ou um `CheckBox` "Expresso" no card do serviço — escolha o que encaixar melhor no padrão da tela e descreva.
   - Enquanto `IsInWorkshop`: faixa no card do avião "Na oficina — libera em mm:ss (serviço)", atualizada por um `DispatcherTimer` de 1 s **só enquanto houver avião na oficina**; outros serviços continuam permitidos.
   - Textos com prazo real: botões/rótulos "Troca de óleo — a cada 48 h na vida real", "Inspeção anual — a cada 100 h na vida real (obrigatória)", "Revisão de ignição — a cada 500 h na vida real (magnetos e velas)", retífica "TBO 2.000 h (vida real)"; legenda fixa: "Os prazos de manutenção são os da vida real; o jogo conta as horas em ritmo acelerado (×4) para caber na carreira." Valores sempre calculados de `MaintenancePricing` × `CompressionFactor` (não escrever números fixos no XAML).
   - `PartHangarView.GroupLifeInfo`: "TBO 2.000 h (vida real) · faltam 280 h de voo"; "Troca de óleo a cada 48 h (vida real) · faltam 8 h de voo"; "Revisão de ignição a cada 500 h (vida real) · faltam 120 h de voo"; vencido: "… vencida há X h de voo". Ajuste os testes de `PartDisplayStateTests` que conferem esses textos.
   - `PartInspectionFlags.LifeNote`: vida em horas/ciclos exibida × 4 com "(vida real)"; o restante em horas de voo/ciclos do jogo.
4. **Bloqueio do voo (arquivos quentes — só o necessário):**
   - `MainViewModel.StartFlight`: antes de `StartMonitoring`, se `IsInWorkshop` → mensagem "A aeronave {prefixo} está na oficina ({descrição}). Libera em mm:ss." e `return`.
   - `BriefingViewModel`: o item da aeronave aparece como bloqueado ("Na oficina — libera em mm:ss") quando for o caso (siga o padrão dos outros itens do checklist).
   - Releia o avião do banco (contexto curto ou `AsNoTracking`) para não pegar valor em cache (AGENTS.md › 6).
5. **Simulador de carreira:** não muda (usa horas de jogo; minutos reais não entram). Conferir que o expresso não é usado pelos bots.

## Fora do escopo
- Calibração de preços/paradas; peças; XP; qualquer outra parte do `FlightMonitor`/`MainViewModel`/`BriefingViewModel` além do bloqueio.

## Testes
- `WorkshopDowntimeTests`: 2 h → 3 min; 8 h → 6 min; 72 h → 17 min; 150 h → 20 min (teto); 0,1 h → 1 min (piso); expresso 72 h → 36 h de jogo, 12 min reais, custo × 1,5; `IsInWorkshop`/`Remaining` com relógio injetado; serviço sobre parada usa a maior.
- `MaintenancePricing.RealHours`: 12 → 48, 25 → 100, 125 → 500, 500 → 2.000, 900 → 3.600. Decisão do usuário: **sempre × 4** — a troca de óleo aparece como **48 h** (não 50).
- Textos de `GroupLifeInfo` e `LifeNote` com prazo real e restante "de voo".
- Persistência: banco antigo ganha as colunas.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %.
- [ ] Avião na oficina não inicia voo (mensagem) e aparece bloqueado no briefing; libera sozinho ao fim do tempo, mesmo com o app fechado no meio.
- [ ] Expresso cobra +50 % e reduz tempos à metade.
- [ ] Nenhum número de prazo fixo no XAML; todos vêm do Domain × 4.
- [ ] CHANGELOG e AGENTS.md › 7 (Manutenção). Relatório diz "não visto na tela / não testado no MSFS".
- [ ] Um commit: `feat(hangar): oficina em tempo real, servico expresso e prazos reais (PARADA-REAL)`.

## Armadilhas
- Arquivos quentes: `MainViewModel`, `BriefingViewModel`, `AppDbContext` — mexer só no indicado.
- `DispatcherTimer` parado quando nenhum avião estiver na oficina (não deixar timer rodando à toa).
- Sem `catch {}` silencioso.
