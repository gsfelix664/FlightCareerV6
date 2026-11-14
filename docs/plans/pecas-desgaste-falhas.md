# Plano: Sensores, peças com saúde oculta, desgaste, revisões e panes

**Status:** aprovado pelo usuário (2026-11-13) · **Revisor/orquestrador:** Claude · **Executor:** Gemini (Antigravity CLI)
**Branch base:** `feat/component-health-system`
**Leitura obrigatória antes de começar:** `AGENTS.md`, `docs/plans/INSTRUCOES-GEMINI.md`, `docs/plans/sensor-inventory.md`.

O executor segue as tarefas **na ordem**, uma por vez, e para nos **portões 🔒**. Se o plano parecer errado ou
incompleto, o executor pergunta (no relatório de revisão); não improvisa.

---

## 0. Regras do jogo (decididas pelo usuário)

1. **Sensor não é peça.** Sensores (SimVars) dizem *como* a aeronave está sendo operada; com isso decidimos *quais peças* sofrem desgaste normal ou desgaste por estresse (overstress).
2. **Cada peça fica salva no Hangar** com saúde **oculta** de 0 a 100%. O piloto nunca vê o número.
3. **Peça trocada** (revisão ou troca avulsa) = 100%, verde, grava a data e as horas da troca.
4. **Desgaste normal** ao voar: a saúde cai devagar. **Abuso** (faixas de estresse dos sensores): cai mais rápido.
5. **Walkaround** antes do voo, grátis: só peças **visíveis**; as com < 40% ganham **exclamação amarela**.
6. **Revisão (50 h, 100 h, 500 h, TBO e revisão geral) só inspeciona:** cobra um valor e entrega a lista de peças com < 40%, que ganham **exclamação amarela**. A revisão **não troca nada sozinha**.
7. **Para liberar uma revisão vencida**, o piloto precisa **trocar todas as peças do pacote** daquela revisão (ex.: 50 h = óleo + filtro). Só então a aeronave é liberada para voo. Trocar as outras peças sinalizadas é opcional.
8. **< 30%**: começam os **break checks** a cada 10 min de voo (tempo simulado), com chance maior quanto menor a saúde.
9. **Break check falhou** → peça **vermelha, BROKEN**. Se for peça que impede o voo, o addon envia a pane ao simulador e a mantém (NO-GO): mesmo que a missão seja iniciada, a aeronave não funciona.
10. **Peça nova:** chance muito baixa de quebrar nas primeiras X horas (mortalidade infantil).
11. Amarelo só existe quando algo foi **descoberto**. Peça a 25% nunca inspecionada continua verde. Isso é intencional.

## 1. Padrões obrigatórios (todas as tarefas)

- **Arquitetura:** regra de negócio em `FlightCareer.Domain` (pura: sem EF, SimConnect, WPF). A UI só orquestra.
- **Calibração em JSON** em `src/FlightCareer.Infrastructure/Data/`. Nenhum número mágico no código.
- **Aleatoriedade injetável:** `IRandomSource`. Nunca `new Random()` na lógica.
- **Tempo simulado** (`SIMULATION RATE`). Chances por intervalo de tempo, nunca por pacote de telemetria.
- **SimConnect:** só nomes do `sensor-inventory.md` (vindos do SDK). Struct e definição na mesma ordem (ou gerados do mesmo registro).
- **Banco:** sem migrations. Tabela/coluna nova = entidade + `CREATE/ALTER ... IF NOT EXISTS` em `EnsureSchemaUpdatedAsync`, com teste em banco antigo. O usuário autorizou começar de banco novo.
- **EF:** `IDbContextFactory<AppDbContext>`, um contexto curto por operação.
- **Testes xUnit** para toda lógica de Domain. Monte Carlo com semente fixa.
- **Sem `catch {}` silencioso.**
- **Não apagar código existente** (`AircraftComponent`, `WearEngine`, `HealthVisibility`, `FailureEngine`) antes do portão da Fase 5. Código novo convive com o antigo.
- **Pronto** = `dotnet build src/FlightCareer.slnx` 0 erros + `dotnet test src/FlightCareer.Tests` verde + critérios de aceite + entrada no `docs/CHANGELOG.md` + relatório de revisão (ver `INSTRUCOES-GEMINI.md`).

---

## FASE 1 — Sensores: ler tudo e validar em voo real

### F1.1 Registro de sensores
- `Domain/Sensors/SensorDefinition.cs`: `record SensorDefinition(string Key, string SimVar, string Unit, string Group, bool PerEngine)`.
- `Infrastructure/Data/sensor_registry.json`: todas as SimVars das seções 2 a 11 do `sensor-inventory.md` (`Key` em kebab-case, ex.: `eng-cht`). Seção 1 em `context_registry.json`.
- Teste: chaves únicas, unidade não vazia, toda SimVar do inventário presente.

### F1.2 Leitor genérico
- `UI/Services/SensorFrameReader.cs`: registra `DEFINITIONS.SensorFrame` em laço sobre o registro; para `PerEngine`, uma entrada por motor (`:1`..`:N`, com `N` lido do contexto).
- **Spike primeiro:** confirmar que o SimConnect gerenciado aceita struct com `[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] double[] Values`. Se não aceitar, gerar o struct a partir do JSON (script) e explicar no relatório.
- SimVar não reconhecida (`NAME_UNRECOGNIZED`): log com o nome (via `dwSendID`/`dwIndex`) e segue sem ela.
- Período `SIM_FRAME` a cada 30 frames. Entrega `SensorFrame` (`IReadOnlyDictionary<string,double>` + horário + sim rate).
- **Não mexer no `SimTelemetry` existente.**

### F1.3 Gravador de validação
- `UI/Services/SensorRecorder.cs`, ligado por chave no overlay de debug: CSV em `%LocalAppData%/FlightCareer/sensor-logs/AAAAMMDD-HHMM-<TITLE>.csv`, uma coluna por chave, mais `marker`.
- Botão "Marcar evento" no overlay (texto livre).
- Arquivo de contexto junto (`...-context.json`) com a seção 1 lida no início.

### F1.4 🔒 Voos de validação (o **usuário** voa; o executor prepara)
Matriz de aeronaves (usar o que existir no MSFS do usuário; os modelos são exemplos):

| # | Tipo | Exemplos | Por quê |
|---|---|---|---|
| A | Pistão carburado, passo fixo, trem fixo | C152, C172 (analógico), Piper Cub | carb heat, magnetos, sem hélice variável |
| B | Pistão injetado, passo variável | Bonanza G36, SR22 | MP, governador, sem carburador |
| C | Pistão turboalimentado | SR22T ou outro com turbo | TIT, wastegate, turbo |
| D | Diesel FADEC (bimotor) | DA40 NG / DA62 | arrefecimento líquido, glow plug, 2 motores |
| E | Bimotor pistão, trem retrátil | Baron G58 | índices `:1`/`:2`, trem retrátil |
| F | Turboélice monomotor | PC-12, TBM 930, Kodiak, Caravan | ITT, torque, beta/feather |
| G | Turboélice bimotor pressurizado | King Air 350 | pressurização, hidráulico/pneumático |
| H | Jato executivo | CJ4, Longitude | N1/N2, reversor, antiskid |
| I | Jato de linha | A320neo, 737 | APU, autobrake, hidráulico completo |

Outros tipos que existem no MSFS 2024 e ficam **fora do escopo por enquanto** (avisar o usuário se quiser incluir): helicópteros (pistão e turbina), planadores, elétricos (Velis Electro), anfíbios/flutuadores e esquis, warbirds com supercharger.

Roteiro por avião (marcar cada item no overlay):
1. Cold & dark → bateria → partida. 2. Taxi com uma frenagem forte. 3. Decolagem em potência máxima, subida.
4. Cruzeiro: mistura/condição normal, depois fora do ideal (pistão: empobrecer além do pico de EGT).
5. Redução brusca de potência em descida rápida (shock cooling). 6. Flap acima da Vfe por 5 s.
7. Curva de 60° (~2 G). 8. Alternador/gerador desligado por 1 min. 9. Trem (se retrátil): ciclo completo.
10. Pouso normal e um pouso firme. 11. Frenagem forte no pouso. 12. Corte no pátio.

Script `tools/analyze_sensor_log.py` (Python + pandas): para cada coluna, mín, máx, média, % constante, se variou nos marcadores.
Saída: `docs/plans/sensor-validation/<tipo>-<modelo>.md`, com tabela **Validado / Inválido / Não suportado**.
**Portão:** o revisor aprova. Sensor não validado não entra nas fases seguintes.

---

## FASE 2 — Peças e desgaste (normal × overstress)

### F2.1 Lista de peças do Hangar
- `Domain/Parts/PartDefinition.cs` (catálogo): `Id` (ex.: `engine.cylinders`), `Name`, `Group` (agrupador da UI: Motor, Óleo, Ignição, Seção quente, Hélice, Elétrico, Instrumentos, Combustível, Trem, Pneus, Freios, Célula, Hidráulico, Pressurização), `AppliesTo` (tipos A–I / classe de motor / condições como "trem retrátil", "carburado"), `LifeHours`, `LifeCycles?`, `VisibleOnWalkaround`, `BlocksFlightWhenBroken`, `PartPrice`, `LaborHours`.
- `Infrastructure/Data/parts_catalog.json`: montado **a partir da tabela de sensores validados** (cada peça cita os sensores que a afetam). O executor propõe a lista; o revisor aprova.
- `Domain/Entities/AircraftPart.cs`: `Id`, `PilotAircraftId`, `PartId`, `Health` (0–100, nunca exibida), `HoursSinceReplacement`, `CyclesSinceReplacement`, `LastReplacedAtUtc`, `LastReplacedAirframeHours`, `Flag` (`None`, `Suspect` = exclamação amarela), `FlaggedBy` (`Walkaround`/`Inspection`), `FlaggedAtUtc?`, `IsBroken`, `BrokenAtUtc?`. Tabela `AircraftParts`, índice único `(PilotAircraftId, PartId)`.
- Provisionamento: aeronave nova recebe as peças que se aplicam ao tipo dela, todas a 100%.
- Teste: IDs únicos; cada tipo A–I tem um conjunto coerente (carburado tem carburador, jato não tem magneto etc.).

### F2.2 🔒 Mapa sensor → estresse → peça
- `Infrastructure/Data/wear_rules.json`. Cada regra: `id`, `sensors`, faixas verde/amarela/vermelha (limites por tipo e por modelo, reaproveitando `aircraftOverrides`), `parts` com `multiplierYellow` / `multiplierRed`, ou `damagePerEvent` (toque, partida, ciclo de trem, ciclo de pressurização).
- O executor propõe a tabela com justificativa (manual do fabricante / POH quando houver); o revisor aprova antes do código.

### F2.3 Motor de desgaste por peça
- `Domain/Parts/PartWearEngine.cs`: `ApplyTick(parts, SensorFrame, dtSimHours)` e `ApplyEvent(...)`.
- Normal: `100% → ~30%` ao longo da `LifeHours` (ou `LifeCycles`). Estresse: `normal × maior multiplicador ativo` + dano por evento.
- Excedências → blackbox (sem janela modal em voo).
- Testes: 1 h toda verde = `70/LifeHours`; CHT vermelha 10 min desgasta cilindros/válvulas ≥ 3× e não toca nos pneus; pouso de 700 fpm danifica amortecedores/pneus conforme JSON; sim rate 4× = 4× por segundo real.

### F2.4 Integração no voo
- `UI/Services/PartFlightSession.cs` consome o `SensorFrame`, aplica o desgaste e salva as peças (a cada 60 s, no fim, cancelamento e perda total), no mesmo padrão do `ComponentFlightSession`.

---

## FASE 3 — Walkaround, revisões e troca de peças

### F3.1 Walkaround (grátis)
- `Domain/Parts/InspectionService.Walkaround(parts)`: peças `VisibleOnWalkaround` com `Health < 40` → `Flag = Suspect`, `FlaggedBy = Walkaround`.
- Teste: invisível a 10% não é marcada; visível a 39% é; a 41% não é.

### F3.2 Revisões só inspecionam
- `Infrastructure/Data/maintenance_programs.json`: para cada revisão (50 h, 100 h, 500 h, TBO, geral): `price` (com fator do valor da aeronave), `inspects` (peças ou "todas") e `replaceToRelease` (pacote obrigatório).
- `InspectionService.RunProgram(program, parts)`: cobra; marca `Suspect` nas inspecionadas com < 40%; devolve o relatório. **Não troca nada.**
- Revisão vencida fica **pendente** até todas as peças de `replaceToRelease` serem trocadas **depois** da inspeção; então o contador da revisão é zerado e a aeronave é liberada.
- Testes: inspeção não altera saúde; revisão vencida continua bloqueando até o pacote completo; trocar só parte do pacote não libera.

### F3.3 Troca de peças
- Trocar peça: `Health = 100`, zera horas/ciclos, grava data/horas, limpa `Flag`/`IsBroken`, resolve `AircraftDefect` ligado à peça.
- Preço = peça + mão de obra × fator do valor da aeronave (JSON).
- **Tempo parado:** em tempo real, limitado a ~24 h, com opção expressa paga (JSON). Aeronave indisponível até a liberação.
- Testes de custo, tempo parado e liberação.

### F3.4 🔒 Revisão da Fase 3

---

## FASE 4 — Panes

### F4.1 Break check
- `Domain/Parts/BreakCheckEngine.cs`: a cada 10 min simulados com `Health < 30`: `p = pMax · ((30 − h) / 30)^k` (JSON, global com override por peça).
- Mortalidade infantil: `HoursSinceReplacement < infantHours` → `infantChancePer10Min`.
- Teste Monte Carlo (100 mil, semente fixa): 29%, 20%, 10%, 0% dentro de ±10% da fórmula; 31% = zero.

### F4.2 Efeito da quebra
- Cada peça tem `BrokenEffect` (`FailureEffect`). Quebra → `SimFailureActuator` aplica o efeito e `PersistentFailureService.Record` grava a discrepância (NO-GO se `BlocksFlightWhenBroken`). Os dois já existem.
- Peça sem efeito no sim que impede o voo: o revisor escolhe o efeito equivalente (ex.: motor → `TOGGLE_ENGINE1_FAILURE`).

### F4.3 🔒 Teste no MSFS (usuário)
- Comando de debug para quebrar à força: magneto, alternador, bomba de vácuo, motor. Reiniciar o voo no sim → a pane volta. Trocar no Hangar → a pane sai.

---

## FASE 5 — Interface

- Hangar por grupo → peças. Cada peça: verde / amarelo ! / vermelho BROKEN, data e horas da última troca, botão "Trocar · $".
- Tela da revisão: lista de achados, pacote obrigatório destacado, seleção do que trocar.
- Nenhum número de saúde em nenhuma tela. Remover a "faixa estimada" atual.
- Checklist pré-voo sugerindo o walkaround.
- 🔒 Capturas de tela aprovadas pelo usuário. Depois disso o revisor decide o que aposentar do sistema antigo.

---

## Riscos conhecidos
1. Nem todo avião simula tudo (bateria, sucção, temperatura de freio). Por isso a validação é por tipo.
2. O desgaste nativo do MSFS 2024 deve ficar desligado; o app pode ler `WEAR AND TEAR LEVEL` para avisar o jogador.
3. Os índices `:N` mudam por avião; nunca assumir 1 motor.

## Diário de execução
(O executor acrescenta aqui uma linha por tarefa: ID, data, status, link do relatório de revisão.)
- F1.1 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.1-relatorio.md) · branch feat/parts-f1
- F1.2 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.2-relatorio.md) · branch feat/parts-f1
- F1.3 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.3-relatorio.md) · branch feat/parts-f1
