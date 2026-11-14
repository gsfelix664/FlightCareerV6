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
8. **< 30%**: começam os **break checks** a cada 10 min de voo (tempo simulado), com chance maior quanto menor a saúde. Vale sempre (regra 0.1): as panes do nativo **também** são registradas; a que vier primeiro.
9. **Break check falhou** → peça **vermelha, BROKEN**. Se for peça que impede o voo, o addon envia a pane ao simulador e a mantém (NO-GO): mesmo que a missão seja iniciada, a aeronave não funciona.
10. **Peça nova:** chance muito baixa de quebrar nas primeiras X horas (mortalidade infantil).
11. Amarelo só existe quando algo foi **descoberto**. Peça a 25% nunca inspecionada continua verde. Isso é intencional.

## 0.1 Wear and tear nativo: ligado; o addon calcula o desgaste e o nativo avisa overstress e eventos (decisão do usuário, 2026-11-14, versão final)

Substitui a regra antiga "o jogador deve desligar o desgaste nativo" e as versões anteriores desta seção ("nativo como fonte da verdade do desgaste", descartada depois do voo F1.4b do C152: o nativo zera a cada voo e também depois de um impacto forte, não aceita escrita e, por voo, quase nunca chega a quebrar; e "nativo só como fonte de eventos", trocada por esta).

1. **O jogador mantém o wear and tear ligado** (padrão do jogo). O addon funciona igual com ele desligado.
2. **Quem calcula a % é sempre o addon.** A saúde das peças no Hangar só muda pelo motor de desgaste do addon (sensores próprios: RPM, CHT, EGT, G, razão de toque, frenagem, velocidade com flap/trem, horas e ciclos), acumulada entre voos. O **nível** nativo (`WEAR AND TEAR LEVEL`) nunca entra no Hangar; fica só no log, para calibração (F1.4c).
3. **Taxas: o addon define tudo; o nativo substitui só o overstress quando acusa (decisão do usuário, versão final).** O addon define para qualquer avião a taxa normal (vida útil de carreira, o mais próxima do real) e a de estresse (sensores próprios). Quando `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` está acima do limiar do componente, a **taxa do nativo** convertida (`taxa × nativeRateToLossPerHour`, teto `maxNativeLossPerHour`) **substitui** a taxa de estresse do addon naquele componente. Regras:
   - **uma fonte por vez, nunca a soma:** nativo acima do limiar → taxa do nativo; nativo em 0 (ou abaixo do limiar) e addon vendo abuso por um sensor que o nativo não modela (shock cooling, mistura, CHT…) → taxa do addon; o desgaste normal é sempre do addon;
   - a taxa nativa é usada **sempre que for reportada**, com a opção de wear and tear ligada ou desligada (no voo F1.4b a taxa continuou reportando com a opção desligada); só o nível nativo é ignorado;
   - **as taxas de estresse do addon são calibradas para o nativo** (F1.4c): o mesmo abuso tem que custar parecido em avião Asobo e de terceiros. Motivo: se o nativo está em overstress, em poucos minutos ele quebra a peça no sim (`IS FAILED` → pane no addon); o desgaste do addon tem que acompanhar, para a % no Hangar chegar perto do mínimo junto com a pane, e não continuar alta;
   - o fator começa como **conversão pura de unidade** (mesma velocidade do nativo, sem escala, como o usuário pediu); a F1.4c confirma a unidade (integral da taxa no trecho ≈ queda do nível nativo) e o usuário decide se ajusta. Teto por componente no JSON (`maxNativeLossPerHour`) protege contra valores absurdos ou mudança da fórmula num update do MSFS;
   - componente com status **Ativo** na tabela (`native-wear-mapping.md`) usa o sinal; **Observar** só grava no log;
   - as SimVars do nativo ficam numa **definição de dados separada** do `SimTelemetry` (nome rejeitado não desloca a telemetria principal), lidas em frequência baixa;
   - todo overstress nativo vai para o **log detalhado** do voo (blackbox), com componente, início, fim e duração ("overstress detectado pelo simulador: motor, 42 s"), para o piloto saber o que aconteceu.
   - Avião sem wear and tear ou de terceiros (taxa sempre 0): vale só o addon, com as taxas calibradas.
3a. **O nativo informa eventos:** quando o wear and tear do MSFS, rodando por conta própria, deixa um componente **fraco** (`WEAR AND TEAR IS WEAK:<id>` 0 → 1) ou **quebrado** (`IS FAILED:<id>` 0 → 1), o addon percebe e aplica **por cima** do desgaste que já fez: fraco = dano de desgaste na peça; quebrado = pane (discrepância). Tabela em `sensor-validation/native-wear-mapping.md`. **Hoje o addon ainda não lê essas flags em voo** (só o `SensorRecorder` grava, com REC ligado): a leitura entra na definição separada da regra 3 e o tratamento na F4.
4. **Panes: vale a que vier primeiro, das duas fontes.**
   - Pane gerada pelo **addon** (break check abaixo de 30%, mortalidade infantil, eventos externos) → discrepância no Hangar e efeito forçado no sim (já existe).
   - Pane gerada pelo **nativo** (`WEAR AND TEAR IS FAILED:<id>`, `ENG FAILED`, fogo, dano de flap/trem por velocidade, freios, `CRASH FLAG`) → também vira discrepância no Hangar, origem `Native`, e persiste entre voos (o `PersistentFailureService` reaplica depois do reinício, quando o MSFS já esqueceu).
   - A mesma peça não gera duas discrepâncias abertas: se já há uma aberta para ela, a segunda fonte só é registrada no blackbox.
   - Pane nativa (`IS FAILED`) marca a peça como quebrada (`IsBroken`), como a pane do addon, e limita a saúde dela ao valor do JSON (`nativeFailedHealthCap`). Peça fraca (`IS WEAK`) recebe o dano do JSON (`nativeWeakHealthCap`). Um evento de cada tipo por peça por voo.
5. **Impacto/pouso duro:** o nativo volta a 100% depois de uma pancada (voo F1.4b: 60,9 ft/s, 11,8 G). O addon detecta pela própria telemetria (pouso duro, perda total do `FlightMonitor`) e aplica dano ou GROUNDED; reiniciar o voo no sim não libera o avião (o `StartMonitoring` bloqueia NO-GO).
6. **Escrita no nativo:** impossível (as SimVars `WEAR AND TEAR *` são só leitura no SDK). O Hangar e o nativo podem divergir; o efeito das panes do addon é forçado pelos meios que já existem (eventos do SDK e watchdog).
7. **Persistência pelo simulador (`.FLT`):** fora do caminho crítico. No voo F1.4b o EFB não salvou o `.flt` (é preciso digitar o nome com a extensão entre aspas); só investigar se virar necessidade.

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

### F1.4b 🔒 Inventário do wear and tear nativo (antes dos voos da matriz e de qualquer tarefa da Fase 2)
Objetivo: saber **o que o wear and tear conta para o addon** — em que situação cada componente fica fraco ou quebrado e que pane isso vira (regra 0.1). Preparação concluída e primeiro voo do C152 feito (ver `sensor-validation/native-wear-C152.md`).
- **Fato do SDK:** `WEAR AND TEAR LEVEL`, `IS WEAK`, `IS FAILED`, `WEAK VALUE`, `OVERSTRESS FACTOR` e `OVERSTRESS DAMAGE RATE` exigem **ID de componente** (0 FUEL TANK … 39 PISTON ENGINE, 40 TURBINE_ENGINE, 41/42 óleo … 49 TAILWHEEL TIRE PRESSURE; lista completa na página "Aircraft Miscellaneous Variables" do SDK 2024) e, opcional, o índice (ex.: motor 2). São só leitura. Também existem `WEAR AND TEAR EXPOSED PARTS LEVEL` / `LOWEST LEVEL` (sem ID). O `sensor_registry.json` atual lê `WEAR AND TEAR *` **sem ID**: corrigir.
- **Spike:** descobrir a sintaxe que o `AddToDataDefinition` aceita para ID + índice (`WEAR AND TEAR LEVEL:39`? nome? índice do motor?). Testar no MSFS com o C152; o log de `NAME_UNRECOGNIZED` mostra o que for rejeitado.
- Registrar no inventário e no registro um sensor por (variável × ID) aplicável — grupo novo `native-wear` — e gravar com o `SensorRecorder` (o layout já pagina blocos de 256).
- **Voos do usuário (C152 primeiro, depois um avião de cada tipo da matriz que ele tiver):** (a) voo com estresse forte (roteiro F1.4); (b) reiniciar o voo e ver se tudo volta a 100%; (c) o mesmo voo curto com a opção de wear and tear do MSFS **desligada**, para achar um sinal que diga se ela está ligada (chave anti-trapaça); (d) se der: salvar um `.FLT` pelo EFB, recarregar e ver se o desgaste volta.
- Saída: `docs/plans/sensor-validation/native-wear-<modelo>.md` com: componentes que respondem por avião, delta por voo, valor em que `IS WEAK`/`IS FAILED` liga (o usuário observou break check abaixo de ~10%: confirmar), se há perda de desempenho, se reinicia em 100%, como detectar a opção ligada e o resultado do `.FLT`.
- **Portão:** o revisor e o usuário aprovam as tabelas "overstress nativo `<id>` → peça/componente do addon, status e limiar" e "evento nativo `<id>` (fraca/quebrada) → dano ou código de discrepância". Isso alimenta F2.1, F2.2 e F4.2. **Aprovado pelo usuário em 2026-11-14:** `sensor-validation/native-wear-mapping.md` (limiares e fatores provisórios; os definitivos saem da F1.4c). Limiares e fatores definitivos saem da F1.4c.

### F1.4c 🔒 Calibração addon × nativo (o **usuário** voa; o executor prepara o roteiro e o script)
Objetivo: (1) achar o **limiar** de overstress de cada componente; (2) confirmar a **unidade** da taxa nativa e o fator `nativeRateToLossPerHour`; (3) calibrar as taxas do catálogo do addon (aviões **sem** wear and tear) para o mesmo abuso custar parecido com o nativo; (4) decidir quais componentes passam de Observar para Ativo.
- **Por que precisa de voo:** o nativo não expõe os limites do CFG nem a fórmula. A taxa só é conhecida por medição: em operação normal ela deve ficar em ~0; no abuso, sobe.
- **Limiar:** por componente, `limiar = max(ruído medido nos trechos normais × 3, 0,0001)`. Ruído = maior taxa vista nos trechos marcados como normais em todos os voos daquele tipo. Se o ruído normal não for ~0 (como pareceu no pneu do C152), o componente fica em Observar até um voo limpo.
- **Unidade e fator nativo:** em cada trecho de abuso, integrar a taxa nativa no tempo e comparar com a **queda do nível nativo** no mesmo trecho (só quedas; subida = reinício). Se a razão ficar estável (~constante entre trechos), a hipótese "fração do nível por segundo" está confirmada e `nativeRateToLossPerHour = 100 × 3600`. Se não, o componente fica em Observar.
- **Fator do addon (calibrar o addon para o nativo — decisão do usuário):** comparar a perda que o addon calcula só com os próprios sensores com a perda nativa convertida no mesmo trecho de abuso. Ajustar `redLossPerHour` / `yellowLossPerHour` (e, na Fase 2, `multiplierRed` em `wear_rules.json`) para a razão addon ÷ nativo ficar entre 0,8 e 1,25. Pneus e trem também na **corrida e no pouso normais** (decolagem, pouso de ~300 fpm), em pista **asfaltada** e em **terra**, separadamente.
- **Roteiro de voo** (novo capítulo em `sensor-validation/ROTEIRO.md`), com REC ligado e marcadores `cal-normal-<trecho>` / `cal-abuso-<componente>`, C152 primeiro em pista asfaltada:
  1. táxi, decolagem e subida normais; cruzeiro normal 5 min; pouso normal; táxi (base de ruído);
  2. RPM acima da linha vermelha por 30 s, depois 60 s (motor 39);
  3. CHT/temperatura do óleo no limite (subida longa em potência máxima e mistura rica errada) (motor 39, óleo 41);
  4. flap acima da Vfe por 10 s (flaps 5–8);
  5. curva de 60° e manobra até ~3,5 G (comandos 9–20);
  6. frenagem forte em velocidade (freios 36, pneus 37);
  7. pouso firme (~500 fpm) e pouso duro (~700 fpm) (trem 35, pneus 37);
  8. repetir 1 e 7 em pista de terra;
  9. **voo longo de overstress** (C152, motor acima da linha vermelha de forma contínua, 15–20 min ou até a pane, em altitude segura): confirmar se `IS WEAK:39` e `IS FAILED:39` ligam, em que nível, depois de quanto tempo, e se o addon (implementação da Fase 2/F4) registrou o evento fraca e a pane. Marcadores `cal-longo-inicio` / `cal-longo-weak` / `cal-longo-pane`.
  Depois, um avião de cada tipo da matriz que o usuário tiver (prioridade: turboélice, para o 40; bimotor, para o índice do 2º motor; um com freios e hidráulico simulados).
- **Script:** `tools/analyze_sensor_log.py` ganha `calibration_report`: por componente e trecho marcado, taxa máxima e média, duração em overstress, queda do nível nativo, perda do addon recalculada com o catálogo atual e razão addon ÷ nativo; sugere limiar e fator. Testes em `tools/tests` com logs sintéticos (só biblioteca padrão).
- **Saída:** `sensor-validation/calibration-<modelo>.md` e a tabela de status/limiar atualizada em `native-wear-mapping.md`.
- **Portão:** o usuário aprova limiares, fatores e quais componentes ficam Ativo.

---

## FASE 2 — Peças e desgaste (normal × estresse; overstress nativo como acelerador)

### F2.1 Lista de peças do Hangar
- `Domain/Parts/PartDefinition.cs` (catálogo): `Id` (ex.: `engine.cylinders`), `Name`, `Group` (agrupador da UI: Motor, Óleo, Ignição, Seção quente, Hélice, Elétrico, Instrumentos, Combustível, Trem, Pneus, Freios, Célula, Hidráulico, Pressurização), `AppliesTo` (tipos A–I / classe de motor / condições como "trem retrátil", "carburado"), `LifeHours`, `LifeCycles?`, `VisibleOnWalkaround`, `BlocksFlightWhenBroken`, `PartPrice`, `LaborHours`.
- `Infrastructure/Data/parts_catalog.json`: montado **a partir da tabela de sensores validados** (cada peça cita os sensores que a afetam). O executor propõe a lista; o revisor aprova.
- `Domain/Entities/AircraftPart.cs`: `Id`, `PilotAircraftId`, `PartId`, `Health` (0–100, nunca exibida), `HoursSinceReplacement`, `CyclesSinceReplacement`, `LastReplacedAtUtc`, `LastReplacedAirframeHours`, `Flag` (`None`, `Suspect` = exclamação amarela), `FlaggedBy` (`Walkaround`/`Inspection`), `FlaggedAtUtc?`, `IsBroken`, `BrokenAtUtc?`. Tabela `AircraftParts`, índice único `(PilotAircraftId, PartId)`.
- Provisionamento: aeronave nova recebe as peças que se aplicam ao tipo dela, todas a 100%.
- Teste: IDs únicos; cada tipo A–I tem um conjunto coerente (carburado tem carburador, jato não tem magneto etc.).

### F2.2 🔒 Mapa sensor → estresse → peça
- **Regra 0.1.3:** além dos sensores próprios, uma regra pode ter `nativeOverstress: <id>` (só componentes Ativo na tabela, com limiar, `nativeRateToLossPerHour` e `maxNativeLossPerHour`). Taxa nativa acima do limiar **substitui** o estresse dos sensores do addon naquele componente; abaixo do limiar, vale o estresse do addon. O nível nativo nunca é usado.
- `Infrastructure/Data/wear_rules.json`. Cada regra: `id`, `sensors`, faixas verde/amarela/vermelha (limites por tipo e por modelo, reaproveitando `aircraftOverrides`), `parts` com `multiplierYellow` / `multiplierRed`, ou `damagePerEvent` (toque, partida, ciclo de trem, ciclo de pressurização).
- O executor propõe a tabela com justificativa (manual do fabricante / POH quando houver); o revisor aprova antes do código.

### F2.3 Motor de desgaste por peça
- Eventos nativos (regra 0.1.3a): `ApplyNativeWeak(part)` limita a saúde a `nativeWeakHealthCap` e `ApplyNativeFailed(part)` a `nativeFailedHealthCap` + `IsBroken` (só baixam, nunca sobem; um de cada por peça por voo). Testes: peça a 80% fica no limite; peça já abaixo não muda; segundo evento no mesmo voo é ignorado; nível nativo não altera nada.
- Overstress nativo (regra 0.1.3): com taxa nativa > limiar, estresse da peça = `min(taxa × nativeRateToLossPerHour, maxNativeLossPerHour)`; senão, estresse dos sensores do addon. Desgaste normal do addon sempre somado. Testes: taxa 0,0015 por 40 s com fator 360.000 = −6% (≈ 540 %/h × 0,0111 h); RPM vermelho do addon (66 %/h) com nativo convertido 540 %/h → usa 540; addon vermelho (66 %/h) com nativo convertido **menor** que 66 (acima do limiar) → usa o nativo; nativo abaixo do limiar ou componente em Observar → addon; taxa acima do teto → teto; taxa reportada com a opção desligada (nível travado em 1,00) → usa a taxa; avião sem wear and tear (taxa 0) → addon; cada trecho de overstress gera uma linha no blackbox com componente, fonte (simulador/addon), duração e perda.
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
- Vale para todas as peças (regra 0.1). Pane nativa da mesma peça (`IS FAILED` / pane do sim) também quebra a peça: a que vier primeiro.
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
2. O wear and tear nativo fica **ligado** (regra 0.1). Riscos: índice do 2º motor por SimConnect não confirmado (`:39:1` foi aceito mas não distingue no monomotor); aviões sem wear and tear ficam só com os sensores do addon.
3. Os índices `:N` mudam por avião; nunca assumir 1 motor.

## Diário de execução
(O executor acrescenta aqui uma linha por tarefa: ID, data, status, link do relatório de revisão.)
- F1.1 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.1-relatorio.md) · branch feat/parts-f1
- F1.2 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.2-relatorio.md) · branch feat/parts-f1
- F1.2b · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.2b-relatorio.md) · branch feat/parts-f1
- F1.3 · 2026-11-14 · PRONTO · [Relatório](docs/reviews/F1.3-relatorio.md) · branch feat/parts-f1
- F1.4 (preparação) · 2026-11-14 · PRONTO PARA VOO · [Relatório](docs/reviews/F1.4-relatorio.md) · branch feat/parts-f1
- F1.4b (preparação) · 2026-11-14 · PRONTO PARA VOO · [Relatório](docs/reviews/F1.4b-relatorio.md) · branch feat/parts-f1
- F1.4b (tabelas do portão) · 2026-11-14 · PROPOSTA, AGUARDA APROVAÇÃO · [Tabelas](docs/plans/sensor-validation/native-wear-mapping.md) · branch docs/f1.4b-tabelas-nativo
- F1.4c (preparação) · 2026-11-14 · PRONTO PARA VOO · [Relatório](docs/reviews/F1.4c-relatorio.md) · branch feat/calibracao-f14c
- F1.4b (portão) · 2026-11-14 · APROVADO PELO USUÁRIO · [Tabelas](docs/plans/sensor-validation/native-wear-mapping.md)
