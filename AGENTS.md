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
| `FlightCareer.UI` | WPF: Views, ViewModels e serviços ligados ao simulador | `Services/SimConnectService`, `Services/FlightMonitor`, `Services/ComponentFlightSession` + `Services/SimFailureActuator` (desgaste e panes no voo), `App.xaml.cs` (DI e inicialização) |
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
3. **Arquivos quentes** (alto risco de conflito): `ComponentFlightSession.cs`, `SimFailureActuator.cs`, `component_catalog.json`, `FlightMonitor.cs`, `SimConnectService.cs`, `SimTelemetry.cs`, `AppDbContext.cs`, `App.xaml.cs`, `MainViewModel.cs`, `BriefingViewModel.cs`. Mexa só no necessário e registre no CHANGELOG.
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
- **Eventos de falha que existem no SDK:** `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`. **Não existem** eventos de alternador, pneu, trem, flap, magneto ou pressurização: esses efeitos são feitos forçando chaves/comandos a cada tick (`SimFailureActuator`) ou só no lado do app (`effect: None`).
- **Toggles reparam se enviados duas vezes:** antes de enviar, confira o estado pela SimVar (`SetFailure` já faz isso).
- **SimVars escritas pelo app:** `ENG ON FIRE:1` (`SetEngineFire`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`SetOilLeakedPercent`).
- Confirme nomes de eventos e SimVars na documentação do SDK do MSFS 2024 antes de usar. Não invente nomes.

### Banco de dados
- Não há migrations do EF. O banco nasce com `EnsureCreatedAsync()` e evolui por `AppDbContext.EnsureSchemaUpdatedAsync()` (lista de `ALTER TABLE` / `CREATE TABLE IF NOT EXISTS` idempotentes).
- **Contextos de longa duração (bug real já corrigido uma vez):** `MainViewModel`, `PilotRepository`, `FlightService`, `MissionGenerator`, `MarketViewModel`, `MarketSimulationService` e `FerryFlightViewModel` são singletons com um `AppDbContext` próprio para a sessão inteira. O EF devolve a entidade **em cache** numa nova consulta, então dados gravados por outro contexto ficam invisíveis e um `SaveChanges` posterior **sobrescreve** o banco com valores velhos (inspeções e gastos perdidos). Regras: código novo usa `IDbContextFactory<AppDbContext>` com contexto curto por operação; nos contextos longos existentes, chame `_context.ChangeTracker.Clear()` no início de cada operação que lê para alterar; nunca altere uma entidade carregada por outro contexto esperando que o seu `SaveChanges` a grave (copie os valores para a entidade do seu contexto). Teste de regressão: `FlightPersistenceTests`.
- Coluna nova = propriedade na entidade **e** `ALTER TABLE ... ADD COLUMN ... DEFAULT ...` nessa lista. Tabela nova = `DbSet` + `CREATE TABLE IF NOT EXISTS` com os mesmos nomes e tipos que o EF geraria. Há um teste que simula um banco antigo (`ComponentPersistenceTests`).

### MSFS 2024 nativo
- O wear and tear nativo do MSFS ("Aircraft Stress Damage") **não pode ser controlado de fora**. O FlightCareer é a fonte da verdade do desgaste; o jogador deve desligar essa opção no simulador para não ter panes duplicadas.

## 7. Estado atual dos sistemas

### Desgaste e falhas — sistema por componentes (fases 1, 2 e 3 concluídas)
- **Domínio** (`FlightCareer.Domain/Components/`, calibrado em `Infrastructure/Data/component_catalog.json`):
  - 9 componentes por aeronave (tabela `AircraftComponents`): Motor, Óleo, Ignição (pistão) **ou** Seção quente (turbina), Elétrico, Instrumentos (vácuo/pitot/estática), Trem, Pneus, Freios, Célula.
  - `WearEngine`: o desgaste natural chega a ~30% no fim da vida útil; 3× com manutenção vencida; faixas verde/amarela/vermelha por parâmetro, com limites **por modelo** (`aircraftOverrides` no catálogo); pousos; energia de frenagem; registros de excedência.
  - `FailureEngine`: risco zero acima de 20%; abaixo, `λ = 0,02 · 100^((20−h)/20)` falhas/hora; um sorteio por componente; modo de falha escolhido por peso. Cada modo tem `effect` (efeito no MSFS), `damage` e `damageKind`.
  - `ExternalEventEngine` (sempre ativo, independe da saúde): bird strike, FOD, falha pós-manutenção (2 h ou 3 pousos após o serviço). Um de cada tipo por voo.
  - `CarbIcingMonitor`: gelo no carburador **recuperável** (apaga em 90 s sem carb heat; 30 s de carb heat liberam o motor).
  - `HealthVisibility`: faixa exibida (±2% a cada 5 h desde a inspeção, máximo ±25%; walkaround ±10%; inspeção completa = exato). Exibida no Hangar.
  - `LegacyConditionBridge`: ponte com as colunas antigas (ver abaixo).
- **Voo** (`FlightCareer.UI/Services/`): `FlightMonitor` → `ComponentFlightSession` (orquestra o tick) → `SimFailureActuator` (eventos do SDK, fogo progressivo, vazamento de óleo, enforcement de magneto/alternador/flaps). O antigo `WearAndFailureService` foi **removido**.
- **Colunas antigas** de `PilotAircraft` (`EngineCondition`, `OilCondition`, `MagnetosCondition`, `TiresCondition`, `AirframeCondition`, `AvionicsCondition`) continuam existindo porque Hangar, mercado, HUD e `FlightService` ainda as usam. Os componentes são a fonte da verdade:
  - durante o voo, cada coluna recebe a **menor saúde** do seu grupo (ex.: `TiresCondition` = min(pneus, freios));
  - no início do voo, mudanças feitas fora dele nas colunas (reparo/inspeção no Hangar, perda total) são aplicadas aos componentes (`ReconcileFromLegacy`).
- **Regra:** lógica nova de desgaste/falhas vai para `Domain/Components` + catálogo JSON, com testes. Novo efeito no simulador = novo valor em `FailureEffect` + tratamento em `SimFailureActuator`.
- **Removido na fase 2 (substituído por faixas de estresse e panes por componente):** incêndios de partida por backfire / vento de cauda / starter travado e os gatilhos diretos de vazamento de óleo por óleo frio, superaquecimento e over-RPM. O hot start continua como faixa vermelha da seção quente.
- **Hangar (fase 3):** `HangarViewModel` usa `IDbContextFactory` (contexto novo por operação) e opera direto nos componentes via `ComponentMaintenance` (orçamento por componente com `repairCostFactor`/`minFullRepairCost` do catálogo, reparo, inspeções 50 h/100 h/500 h, retífica, walkaround). Status da aeronave por `AircraftStatusEvaluator` (usa a faixa exibida, não o valor real). Confirmações em `Views/ThemedDialog`.
- **Próxima fase (4):** discrepâncias persistidas entre voos, MEL e manutenção com tempo parado. Avaliar aviônicos (disjuntores por aeronave no MSFS 2024).

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
