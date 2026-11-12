# FlightCareer V6 — Guia Compartilhado dos Agentes

Fonte de verdade sobre **como trabalhar neste repositório** e **qual é o estado atual de cada sistema**.
Vale para todos os agentes de IA que programam aqui (Gemini e Claude) e para humanos.

- `gemini.md` → regras específicas do Gemini + este guia.
- `CLAUDE.md` → regras específicas do Claude + este guia.
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
| `FlightCareer.Application` | Casos de uso e interfaces | `Services/` (`FlightService`, `MissionGenerator`, `MissionRules`, `HubDataService`, economia, combustível), `Interfaces/` (`IAppDbContext`, `ISimConnectService`...), `DTOs/` |
| `FlightCareer.Infrastructure` | Persistência e dados | `Persistence/AppDbContext`, `Services/` (`DataSeeder`, `MarketSimulationService`, `AircraftComponentProvisioner`...), `CSV/`, `Data/*.json` |
| `FlightCareer.UI` | WPF: Views, ViewModels e serviços ligados ao simulador | `Services/SimConnectService`, `Services/FlightMonitor`, `Services/WearAndFailureService` (legado), `App.xaml.cs` (DI e inicialização) |
| `FlightCareer.Tests` | Testes xUnit | `Components/` (desgaste, falhas, saúde oculta, migração SQLite) |

Dependências: `UI → Infrastructure → Application → Domain`. `Tests` referencia `Domain` e `Infrastructure`.

## 3. Comandos

Execute a partir da raiz do repositório:

```bash
dotnet build src/FlightCareer.slnx
dotnet test src/FlightCareer.Tests
dotnet run --project src/FlightCareer.UI
```

- Use o `dotnet` do PATH. Não fixe caminhos de usuário (`C:\Users\<nome>\...`).
- O banco `flightcareer.db` fica na pasta de saída do `FlightCareer.UI` e está no `.gitignore`.

## 4. Trabalho em dupla (Gemini + Claude)

Os dois agentes editam o mesmo repositório. Para não sobrescrever um ao outro:

1. **Antes de começar:** rode `git status` e `git log --oneline -5`. Se houver alterações não commitadas que você não fez, **não as altere nem as descarte**: pergunte ao usuário.
2. **Uma tarefa = um branch** (`feat/...`, `fix/...`, `refactor/...`). Não commite direto no `main` sem o usuário pedir.
3. **Arquivos quentes** (alto risco de conflito): `WearAndFailureService.cs`, `FlightMonitor.cs`, `SimConnectService.cs`, `SimTelemetry.cs`, `AppDbContext.cs`, `App.xaml.cs`, `MainViewModel.cs`, `BriefingViewModel.cs`. Mexa só no necessário e registre no CHANGELOG.
4. **Commits pequenos**, no padrão `tipo(escopo): descrição em português` (ex.: `fix(simconnect): ...`, `feat(components): ...`). Commit e push só quando o usuário pedir.
5. **Ao terminar:** acrescente uma entrada no fim de `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (Agente)`) e, se mudou arquitetura ou o estado de um sistema, atualize a seção 7 deste arquivo.
6. Não reescreva o trabalho do outro agente sem motivo técnico; se discordar de uma decisão, explique ao usuário em vez de reverter.

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
- **Eventos:** use `SimConnectService.SendEvent(nome, dado)`, que mapeia cada evento uma única vez. Não chame `MapClientEventToSimEvent` de novo com o mesmo ID.
- **Eventos de falha que existem no SDK:** `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`. **Não existem** eventos de alternador, pneu, trem, flap, magneto ou pressurização: esses efeitos são feitos forçando chaves/comandos a cada tick (`EnforceActiveFailures`) ou só no lado do app.
- **Toggles reparam se enviados duas vezes:** antes de enviar, confira o estado pela SimVar (`SetFailure` já faz isso).
- **SimVars escritas pelo app:** `ENG ON FIRE:1` (`SetEngineFire`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`SetOilLeakedPercent`).
- Confirme nomes de eventos e SimVars na documentação do SDK do MSFS 2024 antes de usar. Não invente nomes.

### Banco de dados
- Não há migrations do EF. O banco nasce com `EnsureCreatedAsync()` e evolui por `AppDbContext.EnsureSchemaUpdatedAsync()` (lista de `ALTER TABLE` / `CREATE TABLE IF NOT EXISTS` idempotentes).
- Coluna nova = propriedade na entidade **e** `ALTER TABLE ... ADD COLUMN ... DEFAULT ...` nessa lista. Tabela nova = `DbSet` + `CREATE TABLE IF NOT EXISTS` com os mesmos nomes e tipos que o EF geraria. Há um teste que simula um banco antigo (`ComponentPersistenceTests`).

### MSFS 2024 nativo
- O wear and tear nativo do MSFS ("Aircraft Stress Damage") **não pode ser controlado de fora**. O FlightCareer é a fonte da verdade do desgaste; o jogador deve desligar essa opção no simulador para não ter panes duplicadas.

## 7. Estado atual dos sistemas

### Desgaste e falhas — em migração
- **Em uso no voo (legado):** `FlightCareer.UI/Services/WearAndFailureService.cs`, com colunas em `PilotAircraft` (`EngineCondition`, `OilCondition`, `MagnetosCondition`, `TiresCondition`, `AirframeCondition`, `AvionicsCondition`). Tem break check a cada 60 s (risco por minuto abaixo de 20%), fogo progressivo, vazamento de óleo e as correções de 2026-11-12.
- **Novo (fase 1 concluída, ainda não ligado ao voo):** `FlightCareer.Domain/Components/`
  - 8 componentes por aeronave (tabela `AircraftComponents`): Motor, Óleo, Ignição (pistão) **ou** Seção quente (turbina), Elétrico, Trem, Pneus, Freios, Célula.
  - `WearEngine`: o desgaste natural chega a ~30% no fim da vida útil; 3× com manutenção vencida; faixas verde/amarela/vermelha por parâmetro; pousos; energia de frenagem; registros de excedência.
  - `FailureEngine`: risco zero acima de 20%; abaixo, `λ = 0,02 · 100^((20−h)/20)` falhas/hora; um sorteio por componente; modo de falha escolhido por peso.
  - `HealthVisibility`: o jogador vê uma faixa (±2% a cada 5 h desde a inspeção, máximo ±25%); o walkaround estreita para ±10%; a inspeção completa revela o valor exato.
  - Eventos externos (bird strike, FOD, falha pós-manutenção) ficam **sempre ativos** e independem da saúde — a implementar na fase 2.
- **Regra:** não adicione novas falhas ao `WearAndFailureService`. Lógica nova de desgaste/falhas vai para `Domain/Components` + catálogo JSON, com testes.
- **Próximas fases:**
  2. Ligar `WearEngine`/`FailureEngine` ao `FlightMonitor` no lugar do serviço legado; aplicar os efeitos no simulador; eventos externos; criar componentes ao comprar aeronave ou criar piloto; limites por aeronave no `aircrafts.csv`.
  3. Hangar com as barras, as faixas ocultas, o walkaround e as inspeções.
  4. Discrepâncias persistidas, MEL e manutenção com tempo parado.

### Manutenção
- Inspeções de 50 h (óleo), 100 h (interdita se vencida), 500 h (magnetos) e overhaul (TBO) no `HangarViewModel`. As horas são descontadas no fim do voo pelo `FlightService`.
- TBO: 1.800 h pistão. Turbina: 3.500 h no legado, 3.600 h no mercado e no catálogo novo (inconsistência conhecida, a unificar na fase 2).

### Outros sistemas
- Missões procedurais por Tier e região (`MissionGenerator`, `MissionRules`, `hubs_data.json`); multi-leg; ferry.
- Economia e combustível (`EconomyServices`, `FuelPriceService`); mercado vivo de usados com inspeção pré-compra (`MarketSimulationService`).
- `FlightMonitor`: início/fim automático do voo, touchdown, perda total, pouso de emergência, blackbox (`FlightTelemetryLogger`).
- Plano pendente de detecção de aeronave e W&B: `docs/plans/simconnect-wb-deteccao-aeronave.md`.

## 8. Definição de pronto

Uma tarefa só está concluída quando:

1. `dotnet build src/FlightCareer.slnx` passa com **0 erros**;
2. `dotnet test src/FlightCareer.Tests` passa (e lógica nova de Domain/Application tem teste);
3. o que não pôde ser testado sem o simulador está **dito explicitamente** no relatório ao usuário ("não testado no MSFS");
4. há entrada no `docs/CHANGELOG.md`, e este arquivo foi atualizado se o estado de algum sistema mudou.

Compilar não é o mesmo que funcionar: não declare "aprovado" só porque o build passou.
