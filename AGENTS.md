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

# Saída filtrada para economia de quota (use sempre):
dotnet build src/FlightCareer.slnx -v q -nologo 2>&1 | grep -E " error |Erro\(s\)"
dotnet test src/FlightCareer.Tests 2>&1 | grep -E "Com falha!|Aprovado!|^\s+Com falha "
```

- Sonda de SimVars, replay, fuzz e simulação de carreira: `docs/agents/COMANDOS.md`.
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
9. **Uma tarefa por sessão** (economia de quota): comece por `docs/agents/HANDOFF.md`; ao terminar: build/testes, CHANGELOG e sistema, reescreva o HANDOFF (máx. 40 linhas), commit/push, sessão nova. Nunca leia `docs/CHANGELOG.md` inteiro nem arquivos grandes (`missions_golden.json`, catálogos): use `grep`/`tail`.
10. **Economia de quota**: Opus para spec, arquitetura e arquivos quentes; Sonnet para rotina; saída filtrada (§ 3); `git diff --stat` primeiro; decisões nos planos (`docs/plans/`). Detalhes em `docs/agents/ORQUESTRACAO.md`.
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

Uma linha por armadilha. **Antes de mexer na área, leia o detalhe:** `docs/sistemas/simconnect.md` ou `docs/sistemas/banco.md`.

### SimConnect
- `SimTelemetry` e `AddToDef`: mesma ordem e unidade; campo novo **só no fim**, nos dois.
- Nome de SimVar inválido encurta o pacote e desloca o struct. Confirme nomes de SimVars e eventos no SDK do MSFS 2024; não invente.
- Eventos só por `SimConnectService.SendEvent` (nunca remapear o mesmo ID). Toggle enviado duas vezes repara: confira o estado antes (`SetFailure`).
- Não existem eventos de pane de alternador, pneu, trem, flap, magneto ou pressurização (alternativas em `desgaste-falhas.md`).
- Key Events observados usam grupo próprio não mascarável; roteie por `e.uGroupID`.
- Combustível: a escrita em `FUELSYSTEM TANK QUANTITY` é o total e a leitura é o utilizável (`FuelTank.ToSimWriteValue`).

### Banco de dados
- Sem migrations EF: coluna ou tabela nova vai também em `EnsureSchemaUpdatedAsync` (idempotente).
- Contexto longo guarda cache e sobrescreve dados: código novo usa `IDbContextFactory<AppDbContext>` com contexto curto.

### MSFS 2024 nativo
- O addon é a fonte da verdade da saúde; `WEAR AND TEAR *` é só sensor (somente leitura) e nunca vai ao Hangar.
- Pane nativa não liga `ENG FAILED`; `CRASH FLAG` não aparece (crash pelo `TouchdownDetector`).
- O addon só grava as 4 `* LOSS` de turbina, `ENG ON FIRE:1` e óleo vazado; **não grava** nos tanques (abastecimento manual).

---

## 7. Estado dos sistemas (sob demanda)

Antes de mexer num sistema, leia o arquivo dele em `docs/sistemas/`; ao mudar o estado, atualize o arquivo, não este. Classes-chave de cada um: `docs/sistemas/README.md`.

`desgaste-falhas`, `pecas`, `manutencao-hangar`, `panes-persistentes`, `voo-integridade`, `combustivel`, `economia-dificuldade`, `xp-fases`, `missoes-quadro`, `aeroportos-tiers`, `limites-aeronave`, `sensores`, `hud`, `simconnect`, `banco` (todos `.md`).

Histórico detalhado de cada tarefa: `docs/CHANGELOG.md` (busque pelo ID entre parênteses).

---

## 8. Definição de pronto

Uma tarefa só está concluída quando:

1. `dotnet build src/FlightCareer.slnx` passa com **0 erros**;
2. `dotnet test src/FlightCareer.Tests` passa (e lógica nova de Domain/Application tem teste; desgaste/regras novas exigem teste de replay quando houver log);
3. o que não pôde ser testado sem o simulador está **dito explicitamente** no relatório ao usuário ("não testado no MSFS");
4. há entrada no `docs/CHANGELOG.md`, e o `docs/sistemas/<sistema>.md` foi atualizado se o estado do sistema mudou.

Compilar não é o mesmo que funcionar: não declare "aprovado" só porque o build passou.
