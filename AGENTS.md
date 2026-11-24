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
7. **Orquestração:** o Claude especifica, revisa e faz o merge; o Gemini implementa num worktree isolado. Fluxo, checklist de revisão e boas práticas em `docs/agents/ORQUESTRACAO.md`; ferramentas em `tools/agents/`.
8. **Máquina nova:** siga `docs/agents/NOVA-MAQUINA.md` (`tools/agents/install-global.sh` instala as instruções globais do Claude, as ferramentas do Gemini e os hooks).
9. **Uma tarefa por sessão** (decisão do usuário, 2026-11-24, para economizar a quota: cada resposta relê a conversa inteira, então sessões longas custam caro). Cada sessão do agente cuida de **uma** tarefa (ou de um pedido curto). Ao terminar: (a) build, testes, `CHANGELOG` e este arquivo conforme a § 8; (b) **reescreva** `docs/agents/HANDOFF.md` com o estado atual (o que foi feito, o que está pendente, o próximo passo recomendado e decisões abertas do usuário), substituindo a passagem anterior; (c) commit e push (o usuário já autorizou o push da passagem); (d) diga ao usuário que a tarefa acabou e que a próxima deve começar numa **sessão nova**. A sessão nova começa lendo `docs/agents/HANDOFF.md`. Se o usuário pedir outra tarefa grande na mesma sessão, lembre esta regra e proponha a sessão nova.
10. **Hooks obrigatórios** (uma vez por clone): `git config core.hooksPath "$(pwd)/.githooks"`. Bloqueiam commit/push em `main` e `feat/component-health-system`, mensagem fora do padrão, `push --force` e commit do banco. `FC_ALLOW_PROTECTED=1` libera as branches protegidas e é só do revisor (merge aprovado) ou do usuário. Nunca use `--no-verify`.

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
- **Frequências:** telemetria principal (`SimTelemetry`) a cada 1 s; panes (`SimFailureState`) a cada 6 frames; leitura rápida de voo (`SimFastFlightState`, toque/crash/G) a cada 2 frames; sensores genéricos a cada 30 frames. Cada uma numa definição separada.
- **Eventos:** use `SimConnectService.SendEvent(nome, dado)`, que mapeia cada evento uma única vez. Não chame `MapClientEventToSimEvent` de novo com o mesmo ID.
- **Eventos de falha que existem no SDK:** `TOGGLE_ENGINE1..4_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`. **Não existem** eventos diretos de pane de alternador, pneu, trem, flap, magneto ou pressurização: alternador usa desconexão do barramento `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` com parâmetros `(bus, alt)` via `SendEventEx` (`TransmitClientEvent_EX1`), deixando a chave física livre em ON com bateria descarregando; flaps usam escrita contínua da superfície (`WriteFlapSurfacePercent`), mantendo a alavanca livre e a superfície travada com leve tremor aerodinâmico; magnetos continuam forçando a chave pela SimVar (limitação do cockpit 3D); os demais atuam no app (`effect: None`).
- **Toggles reparam se enviados duas vezes:** antes de enviar, confira o estado pela SimVar (`SetFailure` já faz isso).
- **Key Events observados sem mascarar:** use grupo próprio não mascarável (`GROUPS.ObservedPilotInputs = 3`, `maskable: false`, prioridade `SIMCONNECT_GROUP_PRIORITY_DEFAULT`). Eventos mapeados com `GetOrMapEvent` pertencem a um único grupo de notificação; roteie no `OnRecvEvent` por `e.uGroupID` (`OnObservedEvent` vs `OnMaskedEvent`). Atualmente observa `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY`.
- **SimVars escritas pelo app:** `ENG ON FIRE:1` (`SetEngineFire`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`SetOilLeakedPercent`).
- Confirme nomes de eventos e SimVars na documentação do SDK do MSFS 2024 antes de usar. Não invente nomes.
- **Escrita de `FUELSYSTEM TANK QUANTITY:n` define o total, a leitura devolve o utilizável** (testado no C152, 2026-11-21: gravar 6,05 → lido 5,30). Grave `utilizável + (FUELSYSTEM TANK CAPACITY:n − FUELSYSTEM TANK USABLE CAPACITY:n)` (`FuelTank.ToSimWriteValue`, aplicado em `SimConnectService.SetFuelTanks`). Tanques legados `FUEL TANK * QUANTITY`: leitura e escrita usam o mesmo valor (total, com o não utilizável; testado no TBM 930: 150,5 → 160,5 → 150,5 gal, 9 gal não utilizáveis), então nada é somado; no legado o combustível do app inclui o não utilizável. Sistema por avião (contexto `ctx-new-fuel-system` dos logs): legado no Bonanza G36, TBM 930, A310, A320neo, A321, A330-200, A330 Beluga e A400M; moderno nos outros 17 gravados. 747-8i (moderno) testado: 8 tanques (índices 1–8, sem parte não utilizável), +500 gal sincronizados com diferença de 1,2 gal. **Pendente:** a distribuição é proporcional à capacidade (`FuelTankWritePlan.Distribute`), então com carga parcial põe combustível também no tanque central e nos de reserva/estabilizador, diferente da ordem real de abastecimento (principais primeiro) e com efeito no CG.
- **SimVar com nome inválido desloca o struct:** o MSFS rejeita o datum (`NAME_UNRECOGNIZED`), o pacote fica menor e todos os campos seguintes chegam na posição errada (e o marshaling lê além do fim do pacote). Já aconteceu com `PARTIAL PANEL STATIC`, que **não existe** (não há SimVar do estado da tomada estática). O log `[SimConnect EXCEPTION]` agora mostra o nome rejeitado; `TelemetryDefinitionTests` confere a contagem de datums × campos.

### Banco de dados
- Não há migrations do EF. O banco nasce com `EnsureCreatedAsync()` e evolui por `AppDbContext.EnsureSchemaUpdatedAsync()` (lista de `ALTER TABLE` / `CREATE TABLE IF NOT EXISTS` idempotentes).
- **Contextos de longa duração (bug real já corrigido uma vez):** `MainViewModel`, `PilotRepository`, `FlightService`, `MissionGenerator`, `MarketViewModel`, `MarketSimulationService` e `FerryFlightViewModel` são singletons com um `AppDbContext` próprio para a sessão inteira. O EF devolve a entidade **em cache** numa nova consulta, então dados gravados por outro contexto ficam invisíveis e um `SaveChanges` posterior **sobrescreve** o banco com valores velhos (inspeções e gastos perdidos). Regras: código novo usa `IDbContextFactory<AppDbContext>` com contexto curto por operação; nos contextos longos existentes, chame `_context.ChangeTracker.Clear()` no início de cada operação que lê para alterar; nunca altere uma entidade carregada por outro contexto esperando que o seu `SaveChanges` a grave (copie os valores para a entidade do seu contexto). Teste de regressão: `FlightPersistenceTests`.
- Coluna nova = propriedade na entidade **e** `ALTER TABLE ... ADD COLUMN ... DEFAULT ...` nessa lista. Tabela nova = `DbSet` + `CREATE TABLE IF NOT EXISTS` com os mesmos nomes e tipos que o EF geraria. Há um teste que simula um banco antigo (`ComponentPersistenceTests`).

### MSFS 2024 nativo
- **Decisão 2026-11-15 (plano `pecas-desgaste-falhas.md` › 0.1, versão final):** o wear and tear nativo fica **ligado** e o **addon é a fonte da verdade**: calcula e grava a saúde de todas as peças, o mais perto do real (fabricante, POH, boletins), em qualquer avião (com ou sem wear and tear, terceiros, MSFS 2020). O nível nativo (`WEAR AND TEAR LEVEL`) nunca entra no Hangar. A taxa `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` é só **sensor**: nunca vira % de desgaste; serve de reserva (faixa vermelha) só quando o limite do componente é o padrão da classe, e de conferência no log. Os limites por aeronave vêm de um **catálogo fixo** montado no desenvolvimento (plano › F2.0: POH → SimVars de projeto → CFG → início da taxa nativa → padrão da classe; um JSON por aeronave em `Data/aircraft/`, gravado no banco pelo `DataSeeder`); nada é aprendido em tempo de jogo. Excedências geram desgaste acumulado não linear e discrepância de inspeção por gravidade (F2.2). Único evento: `IS FAILED:<id>` 0 → 1 = pane (discrepância, peça a 0%); `IS WEAK` só no blackbox. **A pane do wear and tear não liga `ENG FAILED:1` nem `GENERAL ENG DAMAGE PERCENT`** (C152, F1.4c): por isso o `PersistentFailureService` lê `nw-is-failed-<id>` do `SensorFrameReader` e grava a pane pela Tabela 2 (`Domain/Components/NativeWearFailures.cs`, F4.2c; só em voo monitorado; **testado no MSFS em 2026-11-15**). No MSFS 2024 o `CRASH FLAG` não apareceu nos crashes e o sim reposiciona o avião no ar: o crash é pego pelo `TouchdownDetector` do addon. Com "Desativar Danos Causados por Estresse" marcado no sim, a taxa continua reportada e o nível trava em 1,00. Perda total passa a ser só acidente real ou reparo > 70% do valor de mercado a 100% (plano › F4.2b; ainda não implementado). O nativo zera a cada voo e depois de impactos fortes; as SimVars `WEAR AND TEAR *` são só leitura, com ID 0–49 (`:39` = motor a pistão). Achados: `docs/plans/sensor-validation/native-wear-C152.md` e `calibration-C152.md`; tabela em `native-wear-mapping.md`. **Ainda não implementado** no desgaste/panes (só gravado pelo `SensorRecorder`).
- **Decisão 2026-11-16:** o addon **grava** `TURB ENG N1 LOSS`, `FUEL EFFICIENCY LOSS`, `ITT COOLING EFFICIENCY LOSS` e `THRUST EFFICIENCY LOSS` por motor a partir da saúde de Motor e Seção quente (`TurbineDegradation`); as demais SimVars de desgaste nativo continuam só leitura.
- **Decisão 2026-11-21 (Abastecimento manual; substitui a gravação de 2026-11-16):** o piloto compra o combustível no Hangar e **coloca a mesma quantidade no simulador** (menu ou EFB), distribuindo e cuidando do CG. O addon **não grava** mais nos tanques: o briefing e o `StartMonitoring` conferem (a bordo + não utilizável × pago, tolerância max(1 gal, 2 %)) e bloqueiam se divergir; combustível colocado depois de iniciar o monitoramento invalida o voo. `SetFuelTanks`/`FuelTankWritePlan` ficam para panes futuras (vazamento de combustível). Integridade: `UNLIMITED FUEL` ligado bloqueia o início do monitoramento e, se ativado em voo, invalida o voo; abastecimento pelo menu do sim em voo (`FUEL TOTAL QUANTITY` subindo além da tolerância) invalida o voo sem pagamento da missão, cobrando o combustível consumido real dos motores (`GENERAL ENG FUEL USED SINCE START:1..4`).

## 7. Estado atual dos sistemas

### Desgaste e falhas
- **O que faz:** Simula desgaste natural, sobrecarga e panes em 9 componentes da aeronave (`AircraftComponents`: Motor, Óleo, Ignição ou Seção quente, Elétrico, Instrumentos, Trem, Pneus, Freios e Célula).
- **Onde está:** `FlightCareer.Domain/Components/` (`WearEngine`, `FailureEngine`, `ExternalEventEngine`, `CarbIcingMonitor`, `HealthVisibility`, `LegacyConditionBridge`), `FlightCareer.UI/Services/` (`ComponentFlightSession`, `SimFailureActuator`), `FlightCareer.Application/Services/TurbineDegradationActuator`. O antigo `WearAndFailureService` foi removido.
- **Regras e integridade:**
  - O addon é a fonte da verdade da saúde; SimVars nativas de desgaste do MSFS 2024 são tratadas apenas como sensores ou atuadores de degradação de turbina (`TurbineDegradationActuator`).
  - Calibração de limites, vidas úteis e taxas em `Infrastructure/Data/component_catalog.json`.
  - Em aeronaves sem peças físicas (`PartComponentBridge.HasParts == false`), `WearEngine` e `FailureEngine` continuam operando diretamente.
  - Colunas legadas em `PilotAircraft` recebem o menor valor do grupo durante o voo e aplicam reconciliação fora de voo (`ReconcileFromLegacy`).
  - Novos efeitos no simulador exigem novo enum em `FailureEffect` e tratamento correspondente em `SimFailureActuator`.

### Peças
- **O que faz:** Modela o ciclo de vida, estresse operacional contínuo e quebra de peças físicas individuais instaladas.
- **Onde está:** `Domain/Parts/` (`PartWearEngine`, `PartFlightTracker`, `PartEventDeriver`, `PartComponentBridge`, `ElectricalConfig`), `Infrastructure/Services/PartStateWriter`, `UI/Services/PartFlightSession`, `AircraftPartProvisioner`.
- **Regras e integridade:**
  - Em aeronaves com peças (`PartComponentBridge.HasParts`), o desgaste em voo é calculado por peça pelo `PartWearEngine` a partir do `SensorFrame`, do toque e de eventos operacionais derivados (`PartEventDeriver`, 29 métricas unificadas em `PartFlightTracker.ProducedMetrics`). O `WearEngine` legado fica inativo (`PartsDriven = true`).
  - As 9 barras de componentes e colunas legadas tornam-se agregadas da saúde das peças (`SyncComponentsFromParts` / `AggregateHealth`).
  - Danos de falhas e eventos são aplicados via `DamageGroup`/`ClampGroup`. Reconciliação no início do voo (`ReconcilePartsFromComponents`) detecta reparos externos (eleva saúde e limpa `IsBroken`) ou avarias fora de voo.
  - Peças com vida em ciclos (pneus, freios, rodas) avançam em pousos reais (`PartWearEngine.ApplyLandingCycle`), ignorando quiques.
  - Lâmpadas no grupo `Iluminação` (`PartGroup.Iluminacao`) são `minorPart` (não recebem dano de grupo) com restrições operacionais da MEL noturna (`LightingMel`). Sem atuação direta no sim; não testado no MSFS.
  - Na preparação pré-voo (`ObservePreflight`), eventos operacionais são computados sem aplicar desgaste natural contínuo, salvando no cancelamento apenas se houver alterações (`HasPreflightChanges`).
  - Provisionamento e limpeza (`AircraftPartProvisioner`) adiciona peças faltantes a 100% e remove peças obsoletas por features do modelo (`noElectrical`, `noFlaps`).
  - Persistência em lote via `PartStateWriter.CopyToDbAsync` nos 4 momentos do voo (a cada 60 s, cancelamento, perda total e conclusão).
  - Testes e ferramentas de verificação: `FlightReplayer`, `SyntheticFlightGenerator`, `SyntheticFlightOracle` e subcomandos `fuzz` e `coverage` no `FlightCareer.Replay`. Calibração em `component_catalog.json` e `wear_rules.json`.

### Manutenção e Hangar
- **O que faz:** Gerencia revisões periódicas, inspeção anual, retífica de motor (TBO), substituição de peças e oficina com tempo de parada.
- **Onde está:** `UI/ViewModels/HangarViewModel`, `Domain/Components/WorkshopDowntime`, `Domain/Parts/PartReplacementPlanner`, `PartPricing`, `PartInspectionFlags`, `ComponentMaintenance`.
- **Regras e integridade:**
  - Acesso ao banco via `IDbContextFactory<AppDbContext>` com contexto curto por operação.
  - Inspeções com fator de compressão regulatória 4× (`MaintenancePricing.CompressionFactor = 4.0`): 12 h (troca de óleo), 25 h (inspeção anual obrigatória, única que interdita a aeronave se vencida e marca `FoundRedAtAnnualAirframeHours`), 125 h (ignição para pistão) e TBO (500 h pistão / 900 h turbina; não interdita). Horas descontadas pelo `FlightService`.
  - Oficina em tempo real (`WorkshopDowntime`, `PilotAircraft.WorkshopUntilUtc`): serviços pagos geram indisponibilidade em minutos reais de relógio; serviço expresso reduz pela metade o tempo com acréscimo de custo. Bloqueia início de voo em `MainViewModel.StartFlight` e `BriefingViewModel`. Libera automaticamente ao fim do prazo.
  - Preços de peças físicas (`PartPricing`): base escala por classe econômica da aeronave (Classes A a J) somada à mão de obra, substituindo o percentual fixo sobre o valor do avião.
  - Lista de peças no Hangar (`PartHangarView`, `PartRowViewModel`): expander por componente exibindo ícones de estado (`✓`, `◐`, `⚠`, `✖`, `?`) sem percentuais numéricos para aeronaves com peças. "Reparar tudo" seleciona recomendadas (quebradas e vermelhas via `SelectRecommended`), permitindo troca individual (`Plan(selectedParts)`). Troca zera horas/ciclos, limpa anotações e sincroniza a barra (`SyncComponentsFromParts`).
  - Walkaround exibe saúde exata. Vida vencida é informativa (`LifeNote`), sem interdição automática.

### Panes persistentes
- **O que faz:** Mantém e gerencia discrepâncias e panes mecânicas não resolvidas entre voos consecutivos.
- **Onde está:** `Domain/Entities/AircraftDefect`, `Domain/Services/DefectCatalog`, `UI/Services/PersistentFailureService`, tabela `AircraftDefects`.
- **Regras e integridade:**
  - Panes com efeito no simulador ou críticas sem efeito geram discrepância (`DefectCatalog.CodeFor`), gravadas pelo `FlightMonitor` antes do alerta. Panes nativas do MSFS são detectadas por borda em `SimFailureState`.
  - Gravidade: NO-GO (`IsGrounding`, bloqueia `StartMonitoring` com status "GROUNDED (PANE)") ou DIFERIDA (permite voar com restrição).
  - Atuação contínua e reaplicação: `Watchdog` e `NativeToggle` reaplicam efeitos ao recarregar a aeronave. Comandos do piloto para flaps e alternador não são mascarados.
  - Modo vigia (`FlightMonitor.WatchAircraft`): mantém falhas ativas fora do voo e monitora reparos no banco a cada 3 s para liberar no simulador.
  - Reparo no card "Panes em aberto" no Hangar (`RepairDefect`/`RepairAllDefects`). O reparo da discrepância não restaura a saúde do componente ou peça associada. Não testado no MSFS após integração com peças.

### Voo e integridade
- **O que faz:** Monitora fases de voo, toque, acidentes, conforto de passageiros, infrações e anti-cheat de slew/congelamento.
- **Onde está:** `UI/Services/FlightMonitor`, `Domain/Components/TouchdownDetector`, `Domain/Integrity/SlewMonitor`, `Domain/Services/FlightQualityService`, `UI/Services/AlertOverlayService`, `CrashPolicy`.
- **Regras e integridade:**
  - Toque e impacto medidos pelo `TouchdownDetector` priorizando `PLANE TOUCHDOWN NORMAL VELOCITY` (reserva `VERTICAL SPEED`, sem `VELOCITY BODY Y`). Crash detectado pelo simulador gera perda total direta (`CrashPolicy`), registrando voo no logbook, avançando carreira e devolvendo contratos ao mercado.
  - Integridade de posição (`SlewMonitor`): slew curto no solo antes da decolagem é tolerado (até 1.000 m acumulados com log); slew no ar, slew pós-decolagem ou qualquer congelamento de atitude/posição invalidam o voo (`FlightMonitor.InvalidateFlight`).
  - Conforto de passageiros (`FlightQualityService`): acumula desconforto em segundos simulados fora dos limites operacionais (`FlightQualityRules`), ignorando solo e pausa. Voos de carga avaliam apenas solavanco e fator G. Vento vertical atenua penalidades.
  - Notificações em voo: utilizar sempre `AlertOverlayService` (janela overlay, topo, sem foco); nunca exibir caixas modais (`MessageBox`) durante o voo monitorado.
  - Pendência: W&B e detecção de aeronave (`docs/plans/simconnect-wb-deteccao-aeronave.md`).

### Combustível
- **O que faz:** Controla a compra, verificação, consumo e reabastecimento de combustível no simulador.
- **Onde está:** `Domain/Fuel/FuelUsageAccumulator`, `Domain/Fuel/FuelIntegrityMonitor`, `Application/Services/FuelPriceService`, `FlightMonitor`.
- **Regras e integridade:**
  - Abastecimento manual (Decisão 2026-11-21): combustível é comprado no Hangar e inserido manualmente pelo piloto no MSFS. O addon não grava quantidade nos tanques. O briefing e o `StartMonitoring` conferem a quantidade a bordo contra a paga (tolerância máxima de 1 gal ou 2%) e bloqueiam a partida se divergir.
  - Integridade: uso de `UNLIMITED FUEL` bloqueia o início do monitoramento e reabastecimento em voo invalida a missão sem pagamento, cobrando o combustível consumido real dos motores (`GENERAL ENG FUEL USED SINCE START:1..4`).
  - Consumo apurado pelo acumulador dos motores e conferido contra a variação dos tanques (divergências > 10% registradas no blackbox).
  - Caminhão de combustível do sim (`REQUEST_FUEL_KEY` / `FuelTruckDelivery`): observado no solo em modo vigia; ao estabilizar a subida, cobra o valor pelo preço local do aeroporto (`FuelPriceService`), atualiza `PilotAircraft.FuelAmount` e consolida antes de iniciar o monitoramento.

### Economia e dificuldade
- **O que faz:** Modela saldo, finanças, tarifas, taxas aeroportuárias, seguro de casco, conversão de cabine e mercado de usados.
- **Onde está:** `Domain/Economy/` (`DifficultyEconomy`, `MissionPayTable`, `AirportFeeCatalog`, `GroundServiceCalculator`, `CabinConversion`), `Application/Services/` (`EconomyServices`, `InsuranceService`), `Infrastructure/Services/MarketSimulationService`, `PartMigrationService`.
- **Regras e integridade:**
  - Modos de dificuldade (`DifficultyEconomy`): Fácil, Médio e Difícil aplicam multiplicadores sobre saldo inicial, receitas de contratos (`PayMultiplier`), despesas operacionais/oficina (`CostMultiplier`) e risco de panes (`FailureMultiplier`/`HazardMultiplier`).
  - Tarifas de missão parametrizadas em `Infrastructure/Data/mission_pay.json`.
  - Taxas de aeroporto e solo (`Infrastructure/Data/airport_fees.json`): cobranças de pouso, embarque de passageiros, rampa e navegação/terminal por porte e tier do aeroporto via `GroundServiceCalculator`.
  - Seguro aeronáutico: prêmio mensal, franquia e indenização por perda total baseados no valor do avião. Sinistro negado (`ClaimDenied`) se voou com discrepância grave não sanada da inspeção anual. Prazos controlados pelo relógio de carreira (`InsuranceService`).
  - Conversão de cabine (`CabinConversion`): permitida apenas em modelos com feature `convertibleCabin` (`AircraftTypes.HasConvertibleCabin`), reconfigurando capacidades de passageiros e carga e filtrando contratos compatíveis (`IsMissionCompatible`). Custo e downtime escalam pelo porte da aeronave.
  - Mercado de usados (`MarketSimulationService`): aeronaves geradas com desgaste proporcional a horas e ciclos; peças de motor limitadas pelo TSMO; inspeção pré-compra reflete na saúde conhecida (`KnownHealth`).
  - Calendário e relógio de carreira (`Pilot.CareerClockUtc` / `CareerClock`): avança exclusivamente por tempo voado simulado e horas de oficina, nunca pelo relógio do computador.

### XP e fases
- **O que faz:** Calcula experiência ganha nos voos, níveis do piloto e progressão pelas 7 fases da carreira.
- **Onde está:** `Domain/Xp/XpCalculator.cs`, `Domain/Xp/PilotLevel.cs`, `Domain/Entities/CareerPhase.cs`, `Domain/Career/CareerPhaseCatalog.cs`, `CareerProgressionSimulator.cs`.
- **Regras e integridade:**
  - Fórmula desacoplada de XP: `XpCalculator.Compute` combina esforço base (tempo no ar, distância, pernas), multiplicador do tipo de missão, multiplicador de realismo (auxílios e partida automática), qualidade operacional (suavidade do pouso e infrações monitoradas por `FlightInfractionTracker`) e bônus fixos de ambiente/pista. Estimativa exibida no quadro via `XpCalculator.Estimate`.
  - Partida automática (`ENGINE_AUTO_START` / Ctrl+E) aplica penalidade de 10% no XP total. Voos invalidados ou acidentes com perda total zeram o XP ganho.
  - Níveis do piloto (`PilotLevel`): progressão quadrática contínua sem nível máximo baseada em XP acumulado.
  - Fases de carreira (Fases 1 a 7, substituindo as 5 categorias antigas): restringem o aceite e início de missões (não a compra de aeronaves). Subir de fase exige nível, horas de voo e taxa de homologação em `Infrastructure/Data/career_phases.json`.
  - Parâmetros e multiplicadores calibrados em `Infrastructure/Data/xp_rules.json` e `career_phases.json`.
  - Telemetria de realismo adicionada em `SimTelemetry` (crash detection, auto-coordenação, realismo, visibilidade, nuvem e altitude-densidade).

### Missões e quadro
- **O que faz:** Gera contratos no quadro de missões, valida requisitos operacionais em voo e apura a conclusão de contratos.
- **Onde está:** `Application/Services/MissionBoardCore` (geração compartilhada com bots), `Application/Services/MissionGenerator`, `Application/Services/PlacesDataService.cs`, `Domain/Missions/` (`MissionCatalog`, `MissionCompletion`, `OverflyTracker`, `TouchAndGoCounter`, `SurveyLegTracker`, `SkydiveDropTracker`, `MultiStopTracker`, `StolLandingTracker`, `CheckFlightTracker`, `FerryRestrictionMonitor`, `ContractMarkerBuilder`, `OverflyWaypointCodec`), `Infrastructure/Data/mission_catalog.json`, `Infrastructure/Data/mission_texts.json`.
- **Regras e integridade:**
  - Catálogo v2 (`mission_catalog.json`): 69 tipos em snake_case com perfis de capacidade física (`MinRunwayFt`, `Paved`), tags de aeroporto (`RequireAnyTag`, `originProfiles`), pesos e tripulação. `MissionRules.cs` opera como adaptador sobre o catálogo.
  - Pista mínima e pavimento: a melhor pista da origem e destinos elegíveis filtra previamente os perfis que a pista comporta; contratos inviáveis têm peso zerado na origem.
  - Bases de dados geográficos em `Data/places/` (`tourist_pois.json`, `training_aerodromes.json`, `dropzones.json`, `scenic_aerodromes.json`) integradas via `PlacesDataService`.
  - Tipos com regras e rastreadores dedicados (`Domain/Missions/`):
    - Voo panorâmico e translado básico: sobrevoo de waypoints (`OverflyTracker`) usando formato de coordenadas `P:<nome>@<lat>,<lon>` (`OverflyWaypointCodec`) e POIs reais.
    - Circuitos de instrução: máquina de estados de toques e arremetidas (`TouchAndGoCounter`).
    - SAE e aerofotogrametria (`SurveyLegTracker`): varredura em pernas paralelas sobre área orientada dentro de tolerâncias de rumo, velocidade e altitude.
    - Lançamento de paraquedistas (`SkydiveDropTracker`): subida e estabilização de 30 s sobre a zona de salto (`dropzones.json`) e monitoramento de choque térmico na descida.
    - Multi-etapas (`MultiStopTracker`): escalas intermediárias com pouso completo ou toque e arremetida (`pinga_pinga`, `navegacao_vfr`), sem reabastecimento não autorizado.
    - Pouso STOL (`StolLandingTracker`): aproximação íngreme e parada curta em pista remota (`instrucao_stol`).
    - Voo de cheque técnico (`CheckFlightTracker`): perfil de manobras e potência retornando à origem (`check_flight`).
    - Translado de manutenção restrito (`FerryRestrictionMonitor`): limites de teto e flaps (`ferry_permit`).
  - Conclusão avaliada puramente em `MissionCompletion.Evaluate` recebendo `MissionTrackerResults`. Contratos não cumpridos retornam ao mercado sem penalidade financeira.
  - Textos e briefing: `mission_texts.json` fornece variantes narrativas com marcadores dinâmicos. `ContractMarkerBuilder` projeta marcadores e polígonos no mapa tático do briefing (`FlightMapControl.xaml.cs`). Não testado visualmente no simulador.

### Aeroportos e tiers
- **O que faz:** Classifica aeroportos em tiers (1 a 4), enriquece dados de infraestrutura, resolve fronteiras e gerencia perfis de tráfego.
- **Onde está:** `Domain/Airports/` (`AirportTierClassifier`, `AirportTierRules`), `Domain/Missions/AirportProfileCatalog`, `Domain/Geo/CountryLocator`, `Infrastructure/Data/airport_tier_rules.json`, `Data/places/airport_profiles.json`, `DataSeeder`.
- **Regras e integridade:**
  - Classificação por pontuação combinada (`AirportTierClassifier`): cortes baseados em infraestrutura real (Little Navmap, OurAirports e etiquetas curadas). Pistas não pavimentadas têm teto de Tier 3.
  - Recálculo automático em lote no `DataSeeder.EnsureTiersUpToDateAsync` acionado por mudança de versão da regra (`TierRulesVersion`).
  - Etiquetas e vocações de aeroporto (`AirportProfileCatalog`): tags de malha aérea (`linha`, `linha_auto:hub`, `carga`, `hospital`, etc.) determinam a geração e os destinos de voos de linha regular e missões especiais. Jatos de linha exigem hubs conectados.
  - Resolução geográfica de países (`CountryLocator`): mapeia coordenadas geográficas contra fronteiras vetoriais (`country_borders.json`) com aproximação por aeroportos vizinhos (`AirportSpatialGrid`) para áreas costeiras e ilhas.
  - Relatório e auditoria via comando `tier-report` no CLI `FlightCareer.Replay`. Não testado no MSFS.

### Limites por aeronave
- **O que faz:** Fornece perfis de limites operacionais de motor, elétrica, arrefecimento e velocidades para monitoramento e estresse de peças.
- **Onde está:** `Domain/Limits/AircraftLimitResolver`, `Infrastructure/Data/aircraft/` (JSON por modelo + `_schema.json` e `_class_defaults.json`), tabelas `AircraftLimitProfiles` e `AircraftLimits`, `AircraftLimitSeeder`.
- **Regras e integridade:**
  - Resolução por precedência estrita: POH → SimVar → Cfg → NativeOnset → ClassDefault.
  - Limites semeados no banco na inicialização pelo `AircraftLimitSeeder` com upsert por versão (`dataVersion`).
  - Features do modelo definem exceções de regras: `noElectrical` (isentos de elétrica), `noFlaps` (isentos de Vfe/over-speed), `noAvionicsMaster` e `convertibleCabin`.
  - Calibração e importação executadas em tempo de desenvolvimento via ferramentas Python (`tools/aircraft_limits/calibrate.py`, `merge_limits.py`, `aircraft_limits-v4.json`). HUD suporta modo guiado de calibração em solo (`CalibrationScript`).

### Sensores
- **O que faz:** Define o layout de leitura de SimVars, buffers em memória, gravação contínua e sonda de diagnóstico no MSFS.
- **Onde está:** `Domain/Sensors/` (`SensorRegistry`, `SensorLayout`, `SensorCsvFormatter`), `UI/Services/` (`SensorFrameReader`, `ContextSnapshotReader`, `SensorRecorder`), console `FlightCareer.SimVarProbe`.
- **Regras e integridade:**
  - Catálogo em `sensor_registry.json` e `context_registry.json` (inventário em `docs/plans/sensor-inventory.md`). O layout é expandido por motor e preenchido em blocos múltiplos de 256 floats com `SIMULATION RATE` para evitar crash por descarte de pacotes no MSFS.
  - SimVars com erro de nome (`NAME_UNRECOGNIZED`) são rejeitadas e removidas dinamicamente do layout pelo `SensorFrameReader` para evitar deslocamento de memória nos dados subsequentes.
  - Gravação de dados via HUD Overlay (`SensorRecorder`) gera CSVs e metadados contextuais (`-context.json`) na pasta `%LocalAppData%\FlightCareer\sensor-logs\`.
  - Sonda `FlightCareer.SimVarProbe`: valida e monitora SimVars em duas fases (sonda pontual e contínua). Validação addon × nativo em `docs/plans/sensor-validation/`.

### HUD
- **O que faz:** Fornece overlay tático e instrumentos auxiliares por cima da janela do simulador.
- **Onde está:** `UI/ViewModels/DebugOverlayViewModel`, `UI/Controls/FlightMapControl.xaml.cs`, `Domain/Components/HudHealthSummary`, `HangarIcons`, `HangarPalette`.
- **Regras e integridade:**
  - Mini-GPS e mapa ao vivo: renderiza a rota, ponto de partida em destaque (`OriginLat`/`OriginLon`) e destino, com enquadramento tático automático em voos locais ou de circuito.
  - Ocultamento de saúde real: barras numéricas de porcentagem foram removidas. O resumo (`HudHealthSummary`) expõe exclusivamente ícones vetoriais de estado do Hangar (`✓`, `◐`, `⚠`, `✖`, `?`) baseados na saúde conhecida (`KnownHealth`), eliminando o vazamento da saúde física real em voo.
  - Controles de gravação de telemetria (REC/Marcar) e roteiros de calibração integrados. Não testado visualmente no MSFS.

Histórico detalhado de cada tarefa: `docs/CHANGELOG.md` (busque pelo ID entre parênteses).

## 8. Definição de pronto

Uma tarefa só está concluída quando:

1. `dotnet build src/FlightCareer.slnx` passa com **0 erros**;
2. `dotnet test src/FlightCareer.Tests` passa (e lógica nova de Domain/Application tem teste; desgaste/regras novas exigem teste de replay quando houver log);
3. o que não pôde ser testado sem o simulador está **dito explicitamente** no relatório ao usuário ("não testado no MSFS");
4. há entrada no `docs/CHANGELOG.md`, e este arquivo foi atualizado se o estado de algum sistema mudou.

Compilar não é o mesmo que funcionar: não declare "aprovado" só porque o build passou.
