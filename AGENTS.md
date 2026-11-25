# FlightCareer V6 — Guia Compartilhado dos Agentes

Fonte de verdade sobre **como trabalhar neste repositório**; o estado de cada sistema fica em `docs/sistemas/` (índice na § 7).
Vale para todos os agentes de IA que programam aqui (Gemini e Claude) e para humanos.

- `gemini.md` → regras específicas do Gemini + este guia.
- `CLAUDE.md` → regras específicas do Claude + este guia.
- `docs/sistemas/` → estado atual de cada sistema (ler só o do sistema da tarefa).
- `docs/CHANGELOG.md` → histórico do que foi feito (não é a fonte de verdade do estado atual).
- `docs/plans/` → planos de trabalho ainda não concluídos.

Se o que está aqui divergir do código, **o código vence**: corrija este arquivo na mesma tarefa.

---

## 1. O projeto

Carreira de aviação para **Microsoft Flight Simulator 2020/2024**: piloto e companhia, missões procedurais, economia, hangar com aeronaves individuais, manutenção, desgaste/falhas por telemetria e mercado de usados.

- **C# / .NET 10**, **WPF** (MVVM com `CommunityToolkit.Mvvm`), **EF Core + SQLite**, **SimConnect** (MSFS SDK), mapa com **GMap.NET**.
- Interface, comentários, textos de alerta e mensagens de commit em **português**.

## 2. Estrutura da solução (`src/FlightCareer.slnx`)

| Projeto | Responsabilidade | Destaques |
|---|---|---|
| `FlightCareer.Domain` | Entidades, value objects, enums e **lógica pura** (sem EF, sem SimConnect, sem WPF) | `Entities/` (`PilotAircraft`, `AircraftComponent`, `SimTelemetry`...), `Components/` (desgaste e falhas novos), `Services/FlightQualityService` |
| `FlightCareer.Application` | Casos de uso e interfaces | `Services/` (`FlightService`, `MissionGenerator`, `MissionRules`, `HubDataService`, economia, combustível, `TurbineDegradationActuator`), `Interfaces/` (`IAppDbContext`, `ISimConnectService`...), `DTOs/` |
| `FlightCareer.Infrastructure` | Persistência e dados | `Persistence/AppDbContext`, `Services/` (`DataSeeder`, `MarketSimulationService`, `AircraftComponentProvisioner`...), `CSV/`, `Data/*.json` |
| `FlightCareer.UI` | WPF: Views, ViewModels e serviços ligados ao simulador | `Services/SimConnectService`, `Services/FlightMonitor`, `Services/ComponentFlightSession` + `Services/SimFailureActuator` (desgaste e panes no voo), `App.xaml.cs` (DI e inicialização) |
| `FlightCareer.Tests` | Testes xUnit | `Components/` (desgaste, falhas, saúde oculta, migração SQLite) |

Dependências: `UI → Infrastructure → Application → Domain`. `Tests` referencia `Domain` e `Infrastructure`.

## 3. Comandos

Execute a partir da raiz do repositório:

```bash
dotnet build src/FlightCareer.slnx
dotnet test src/FlightCareer.Tests
dotnet run --project src/FlightCareer.UI
dotnet run --project src/FlightCareer.SimVarProbe [--probe-only] [--duration <seg>] [--filter <regex>]
dotnet run --project src/FlightCareer.Replay -- <csv|csv.gz> [--aircraft <modelo>]
dotnet run --project src/FlightCareer.Replay -- fuzz [--runs <n>] [--aircraft <modelo>] [--seed <n>]
dotnet run --project src/FlightCareer.Replay -- career --aircraft "<modelo>,<modelo>" [--bots <n>] [--days <n>] [--hours-per-day <h>] [--seed <n>] [--policy best|random] [--fast] [--out <pasta>]
dotnet run --project src/FlightCareer.Replay -- career-stab [--days <n>] [--bots <n>] [--seed <n>] [--difficulty easy|medium|hard|all] [--policy all|cauteloso|equilibrado|agressivo] [--fast] [--out <pasta>]

# Saída filtrada para economia de quota (use sempre):
dotnet build src/FlightCareer.slnx -v q -nologo 2>&1 | grep -E " error |Erro\(s\)"
dotnet test src/FlightCareer.Tests 2>&1 | grep -E "Com falha!|Aprovado!|^\s+Com falha "
```

- Use o `dotnet` do PATH. Não fixe caminhos de usuário (`C:\Users\<nome>\...`).
- O banco `flightcareer.db` fica na pasta de saída do `FlightCareer.UI` e está no `.gitignore`.

## 4. Trabalho em dupla (Gemini + Claude)

Os dois agentes editam o mesmo repositório. Para não sobrescrever um ao outro:

1. **Antes de começar:** rode `git status` e `git log --oneline -5`. Se houver alterações não commitadas que você não fez, **não as altere nem as descarte**: pergunte ao usuário.
2. **Uma tarefa = um branch** (`feat/...`, `fix/...`, `refactor/...`). Não commite direto no `main` sem o usuário pedir.
3. **Arquivos quentes** (alto risco de conflito): `ComponentFlightSession.cs`, `SimFailureActuator.cs`, `component_catalog.json`, `FlightMonitor.cs`, `SimConnectService.cs`, `SimTelemetry.cs`, `AppDbContext.cs`, `App.xaml.cs`, `MainViewModel.cs`, `BriefingViewModel.cs`. Mexa só no necessário e registre no CHANGELOG.
4. **Commits pequenos**, no padrão `tipo(escopo): descrição em português` (ex.: `fix(simconnect): ...`, `feat(components): ...`). Commit e push só quando o usuário pedir.
5. **Ao terminar:** acrescente uma entrada no fim de `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (Agente)`); se mudou o estado de um sistema, atualize `docs/sistemas/<sistema>.md`.
6. Não reescreva o trabalho do outro agente sem motivo técnico; se discordar de uma decisão, explique ao usuário em vez de reverter.
7. **Orquestração:** o Claude especifica, revisa e faz o merge; o Gemini implementa num worktree isolado. Fluxo, checklist e ferramentas em `docs/agents/ORQUESTRACAO.md` e `tools/agents/`.
8. **Máquina nova:** siga `docs/agents/NOVA-MAQUINA.md` (`tools/agents/install-global.sh` instala instruções globais, ferramentas e hooks).
9. **Uma tarefa por sessão** (economia de quota): cada sessão cuida de uma tarefa. Comece por `docs/agents/HANDOFF.md`. Ao terminar: build/testes, CHANGELOG e sistema; reescreva HANDOFF; commit/push; nova sessão. Nunca leia `docs/CHANGELOG.md` inteiro nem arquivos grandes (`missions_golden.json`, catálogos): use `grep`/`tail`. Detalhes em `docs/agents/ORQUESTRACAO.md`.
10. **Economia de quota**: modelo por tipo de sessão (Opus: spec/arquitetura/arquivos quentes; Sonnet: rotina); comandos com saída filtrada (§ 3); diffs com `git diff --stat` primeiro; decisões nos planos (`docs/plans/`); mensagens agrupadas. Detalhes em `docs/agents/ORQUESTRACAO.md`.
11. **Hooks obrigatórios** (uma vez por clone): `git config core.hooksPath "$(pwd)/.githooks"`. Bloqueiam commit/push em `main` e `feat/component-health-system`, mensagem fora do padrão, force push e commit do banco. `FC_ALLOW_PROTECTED=1` libera branches protegidas (só revisor/usuário). Nunca use `--no-verify`.

## 5. Convenções de código

- **Lógica de regra de negócio vai para `Domain`/`Application`**, testável e sem dependências de UI ou simulador. A UI só orquestra.
- **Aleatoriedade injetável:** use `IRandomSource` (`Domain/Components/ComponentSupport.cs`), nunca `new Random()` dentro da lógica, para que os testes sejam determinísticos.
- **Calibração em dados, não em código:** taxas, limites e vidas úteis do sistema de componentes ficam em `Infrastructure/Data/component_catalog.json`.
- **Probabilidades sempre por tempo:** use taxa por hora × `dt` (`p = 1 − e^(−λ·dt)`). Nunca sorteie uma chance fixa "por pacote de telemetria", porque a frequência dos pacotes varia.
- **Tempo de jogo = tempo simulado:** desgaste e horas usam o tempo multiplicado por `SIMULATION RATE`, não o relógio do computador.
- **Sem `catch {}` silencioso** em código novo: registre pelo menos com `Debug.WriteLine`. O `EnsureSchemaUpdatedAsync` é a exceção conhecida (ignora "coluna já existe").
- **MVVM:** `[ObservableProperty]` / `[RelayCommand]` do CommunityToolkit; acesso ao banco a partir de serviços/ViewModels via `IDbContextFactory<AppDbContext>` (contexto curto, um por operação).

## 6. Regras técnicas que já causaram bugs

### SimConnect
- **`SimTelemetry` e `AddToDef` precisam ter a mesma ordem.** O struct é lido por posição de memória: **só acrescente campos no fim**, nos dois lugares, com a mesma unidade.
- **Frequências:** telemetria principal (`SimTelemetry`) 1 s; panes (`SimFailureState`) 6 frames; voo rápido (`SimFastFlightState`, toque/crash/G) 2 frames; sensores genéricos 30 frames. Definições separadas.
- **Eventos:** use `SimConnectService.SendEvent(nome, dado)`, que mapeia cada evento uma única vez. Não chame `MapClientEventToSimEvent` de novo com o mesmo ID.
- **Eventos de falha que existem no SDK:** `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`. **Não existem** eventos diretos de pane de alternador, pneu, trem, flap, magneto ou pressurização: alternador = `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` via `SendEventEx`; flaps = `WriteFlapSurfacePercent`; magnetos pela SimVar; demais `effect: None` (detalhes: `docs/sistemas/desgaste-falhas.md`).
- **Toggles reparam se enviados duas vezes:** antes de enviar, confira o estado pela SimVar (`SetFailure` já faz isso).
- **Key Events observados sem mascarar:** use grupo próprio não mascarável (`GROUPS.ObservedPilotInputs = 3`, `maskable: false`, roteamento por `e.uGroupID` em `OnRecvEvent`). Observa `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY`.
- **SimVars escritas pelo app:** `ENG ON FIRE:1` (`SetEngineFire`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`SetOilLeakedPercent`).
- Confirme nomes de eventos e SimVars na documentação do SDK do MSFS 2024 antes de usar. Não invente nomes.
- **Escrita de `FUELSYSTEM TANK QUANTITY:n` define o total, a leitura devolve o utilizável:** grave `utilizável + (FUELSYSTEM TANK CAPACITY:n − FUELSYSTEM TANK USABLE CAPACITY:n)` (`FuelTank.ToSimWriteValue`, aplicado em `SimConnectService.SetFuelTanks`). Tanques legados `FUEL TANK * QUANTITY`: leitura e escrita usam o total (com não utilizável). Pendente: distribuição proporcional em `FuelTankWritePlan.Distribute` põe em todos os tanques com carga parcial (detalhes: `docs/sistemas/combustivel.md`).
- **SimVar com nome inválido desloca o struct:** datum rejeitado (`NAME_UNRECOGNIZED`) encurta o pacote e corrompe campos seguintes (ex.: `PARTIAL PANEL STATIC` **não existe**). O log `[SimConnect EXCEPTION]` mostra nome rejeitado; `TelemetryDefinitionTests` confere contagem.

### Banco de dados
- **Sem migrations EF:** banco nasce com `EnsureCreatedAsync()` e evolui por `AppDbContext.EnsureSchemaUpdatedAsync()` (`ALTER TABLE` / `CREATE TABLE IF NOT EXISTS` idempotentes). Coluna nova = propriedade na entidade + `ALTER TABLE ... ADD COLUMN ... DEFAULT ...`. Tabela nova = `DbSet` + `CREATE TABLE IF NOT EXISTS` (teste: `ComponentPersistenceTests`).
- **Contextos de longa duração (bug de cache/sobrescrita):** singletons (`MainViewModel`, `PilotRepository`, `FlightService`, `MissionGenerator`, `MarketViewModel`, `MarketSimulationService`, `FerryFlightViewModel`) com `AppDbContext` próprio guardam entidades em cache e um `SaveChanges` posterior sobrescreve dados de outros contextos. Código novo usa `IDbContextFactory<AppDbContext>` com contexto curto; nos contextos longos, chame `_context.ChangeTracker.Clear()` antes de ler para alterar; nunca salve em outro contexto entidade de outro. Teste: `FlightPersistenceTests`.

### MSFS 2024 nativo
- **Addon é a fonte da verdade:** wear and tear nativo fica ligado, mas a saúde é calculada e mantida pelo addon. O `WEAR AND TEAR LEVEL` nunca entra no Hangar. `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` é só sensor (faixa vermelha), nunca vira % de desgaste. Limites vêm de catálogo fixo por aeronave (`Data/aircraft/`).
- **Panes nativas não ligam SimVars de falha de motor:** `IS FAILED:<id>` (0 → 1) indica pane, mas **não** liga `ENG FAILED:1` nem `GENERAL ENG DAMAGE PERCENT`. O `PersistentFailureService` lê `nw-is-failed-<id>` do `SensorFrameReader` e grava a pane via Tabela 2 (`Domain/Components/NativeWearFailures.cs`). SimVars `WEAR AND TEAR *` são somente leitura, ID 0–49 (`:39` = pistão) (detalhes: `docs/sistemas/desgaste-falhas.md`).
- **Crash no 2024:** `CRASH FLAG` não aparece no 2024 (o sim reposiciona o avião no ar); impacto e crash são detectados pelo `TouchdownDetector` do addon.
- **SimVars gravadas pelo addon:** somente `TURB ENG N1 LOSS`, `FUEL EFFICIENCY LOSS`, `ITT COOLING EFFICIENCY LOSS` e `THRUST EFFICIENCY LOSS` por motor (`TurbineDegradationActuator`). Demais são somente leitura.
- **Abastecimento manual (2026-11-21):** o addon **não grava** nos tanques. O piloto abastece no menu do sim a quantidade paga; briefing e `StartMonitoring` conferem a bordo × pago; reabastecimento em voo invalida a missão e cobra consumo real (`GENERAL ENG FUEL USED SINCE START:1..4`) (detalhes: `docs/sistemas/combustivel.md`).

## 7. Estado dos sistemas (sob demanda)

Antes de mexer num sistema, leia o arquivo dele em `docs/sistemas/`; ao mudar o estado, atualize o arquivo, não este.

| Sistema | Arquivo | Classes/pastas-chave |
|---|---|---|
| Desgaste e falhas | `desgaste-falhas.md` | `Domain/Components/`, `ComponentFlightSession`, `SimFailureActuator` |
| Peças | `pecas.md` | `Domain/Parts/`, `PartFlightSession`, `PartStateWriter` |
| Manutenção e Hangar | `manutencao-hangar.md` | `HangarViewModel`, `WorkshopDowntime`, `PartPricing` |
| Panes persistentes | `panes-persistentes.md` | `AircraftDefect`, `DefectCatalog`, `PersistentFailureService` |
| Voo e integridade | `voo-integridade.md` | `FlightMonitor`, `TouchdownDetector`, `SlewMonitor` |
| Combustível | `combustivel.md` | `FuelUsageAccumulator`, `FuelIntegrityMonitor`, `FuelPriceService` |
| Economia e dificuldade | `economia-dificuldade.md` | `Domain/Economy/`, `EconomyServices`, `MarketSimulationService` |
| XP e fases | `xp-fases.md` | `XpCalculator`, `PilotLevel`, `CareerPhaseCatalog` |
| Missões e quadro | `missoes-quadro.md` | `MissionBoardCore`, `Domain/Missions/`, `PlacesDataService` |
| Aeroportos e tiers | `aeroportos-tiers.md` | `AirportTierClassifier`, `AirportProfileCatalog`, `CountryLocator` |
| Limites por aeronave | `limites-aeronave.md` | `AircraftLimitResolver`, `Data/aircraft/`, `AircraftLimitSeeder` |
| Sensores | `sensores.md` | `Domain/Sensors/`, `SensorFrameReader`, `FlightCareer.SimVarProbe` |
| HUD | `hud.md` | `DebugOverlayViewModel`, `FlightMapControl`, `HudHealthSummary` |

Histórico detalhado de cada tarefa: `docs/CHANGELOG.md` (busque pelo ID entre parênteses).

## 8. Definição de pronto

Uma tarefa só está concluída quando:

1. `dotnet build src/FlightCareer.slnx` passa com **0 erros**;
2. `dotnet test src/FlightCareer.Tests` passa (e lógica nova de Domain/Application tem teste; desgaste/regras novas exigem teste de replay quando houver log);
3. o que não pôde ser testado sem o simulador está **dito explicitamente** no relatório ao usuário ("não testado no MSFS");
4. há entrada no `docs/CHANGELOG.md`, e o `docs/sistemas/<sistema>.md` foi atualizado se o estado do sistema mudou.

Compilar não é o mesmo que funcionar: não declare "aprovado" só porque o build passou.
