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

## 0.1 Wear and tear nativo: ligado; o addon calcula tudo e o nativo é só sensor (decisão do usuário, 2026-11-15, versão final)

Substitui a versão de 2026-11-14 ("a taxa nativa convertida substitui o estresse do addon"), descartada depois da F1.4c do C152 (`sensor-validation/calibration-C152.md`): o nativo trata a linha vermelha como uma parede (a 2.700 RPM o C152 perde ~10% por minuto e quebra em ~9 min), muito mais severo que o motor real, em que excesso pequeno pede anotação e inspeção e o dano é acumulativo. O usuário quer o addon **o mais real possível**. Versões anteriores ("nativo como fonte da verdade do desgaste" e "nativo só como fonte de eventos") também ficam descartadas.

1. **O jogador mantém o wear and tear ligado** e o dano por estresse ligado (Assistência › Realismo: "Desativar Danos Causados por Estresse do Motor" e "... da Aeronave" **desmarcadas**). O addon funciona igual com tudo desligado.
2. **O addon calcula toda a %, o mais real possível, para qualquer avião:** MSFS 2024 com ou sem wear and tear, aviões de terceiros e MSFS 2020. Desgaste normal, faixas de estresse, dano por evento e consequências vêm dos **sensores próprios** do addon (RPM, CHT, EGT, temperatura e pressão do óleo, G, razão de toque, frenagem, velocidade com flap/trem, mistura, shock cooling, horas e ciclos) e do JSON, com referência em manual do fabricante, POH e boletins de serviço (ex.: Lycoming SB 369 para excesso de RPM). O addon tem que cobrir sozinho todos os componentes: no C152 o nativo nem responde para flaps, freios, trem e óleo. O **nível** nativo (`WEAR AND TEAR LEVEL`) nunca entra no Hangar; fica só no log.
3. **A taxa de overstress nativa é só sensor; nunca é convertida em % de desgaste.**
   - **O addon é a fonte da verdade.** Exemplo (usuário, 2026-11-15): numa decolagem com manete toda à frente o nativo do C152 pode cair para 80%, mas isso **não** quer dizer que o motor está a 80% no Hangar; o Hangar mostra o que o addon calculou, o mais perto do real (ali, quase nada). Onde o addon conhece o limite do componente para aquele modelo (POH, catálogo, `aircraftOverrides`), **só os sensores do addon** decidem a faixa. A taxa nativa acima do limiar só entra como **reserva**, em componente **Ativo**, quando o addon não tem limite próprio para aquele modelo (ex.: avião de terceiros sem override): aí ela conta como faixa vermelha na regra do addon, com o dano do addon (realista e não linear), nunca com a velocidade do nativo.
   - Fora disso a taxa serve para **log e conferência**: se o nativo acusa um abuso que o addon não viu, é sinal de limite faltando no catálogo (corrigir na F2.2), e vai para o blackbox.
   - Componente em **Observar** só grava no log.
   - A taxa continua sendo reportada com o dano por estresse desativado no sim (nível travado em 1,00, voo 1 da F1.4c): vale como sensor do mesmo jeito, então desativar o dano no sim não escapa do desgaste do addon. Taxa > 0 com o nível parado em exatamente 1,00 = **dano por estresse desativado** (chave anti-trapaça; registrar no blackbox).
   - As SimVars do nativo ficam numa **definição de dados separada** do `SimTelemetry` (nome rejeitado não desloca a telemetria principal), lidas em frequência baixa.
   - Toda excedência (vista pelo addon ou acusada pelo nativo) vai para o **log detalhado** do voo (blackbox): componente, fonte, pico, duração ("excesso de RPM: 2.690 RPM por 62 s, fonte simulador + addon").
4. **Consequência realista das excedências:** conforme a gravidade (faixas no JSON, a partir do fabricante), além do desgaste acumulado, a excedência gera uma **discrepância de inspeção** no Hangar (ex.: "Excesso de RPM — inspeção necessária"), com custo; acima de um limite (ex.: excesso grande ou longo pelo SB do fabricante) é NO-GO até a inspeção. Detalhe na F2.2.
5. **Evento do nativo: só `IS FAILED`.** `WEAR AND TEAR IS FAILED:<id>` 0 → 1 = o jogador viu a pane no sim, então ela vai para o Hangar: discrepância (origem `Native`), peça quebrada (`IsBroken`) a **0%**. `IS WEAK` **não altera a saúde**: só vai para o blackbox. No C152 a pane do wear and tear **não liga `ENG FAILED:1` nem `GENERAL ENG DAMAGE PERCENT`** (F1.4c), então a detecção nativa atual não a pega: é preciso ler `IS FAILED:<id>` na definição separada. Divergência esperada: o nativo é mais severo e pode quebrar uma peça que no Hangar ainda está alta; a pane vale do mesmo jeito.
6. **Panes: vale a que vier primeiro, das duas fontes.**
   - Pane gerada pelo **addon** (break check abaixo de 30%, mortalidade infantil, eventos externos) → discrepância no Hangar e efeito forçado no sim (já existe).
   - Pane gerada pelo **nativo** (`WEAR AND TEAR IS FAILED:<id>`, `ENG FAILED`, fogo, dano de flap/trem por velocidade, freios, `CRASH FLAG`) → também vira discrepância no Hangar, origem `Native`, e persiste entre voos (o `PersistentFailureService` reaplica depois do reinício, quando o MSFS já esqueceu).
   - A mesma peça não gera duas discrepâncias abertas: se já há uma aberta para ela, a segunda fonte só é registrada no blackbox.
   - Pane nativa (`IS FAILED`) marca a peça como quebrada (`IsBroken`), como a pane do addon, e leva a saúde dela a **0%** (decisão do usuário, 2026-11-14; substitui o limite de 15%): peça quebrada = 0%, e só a troca devolve a 100%. `IS WEAK` não gera dano (regra 5). Um evento por peça por voo.
7. **Impacto/pouso duro:** o nativo volta a 100% depois de uma pancada (voo F1.4b: 60,9 ft/s, 11,8 G). O addon detecta pela própria telemetria (pouso duro, perda total do `FlightMonitor`) e aplica dano ou GROUNDED; reiniciar o voo no sim não libera o avião (o `StartMonitoring` bloqueia NO-GO).
8. **Escrita no nativo:** impossível (as SimVars `WEAR AND TEAR *` são só leitura no SDK). O Hangar e o nativo podem divergir; o efeito das panes do addon é forçado pelos meios que já existem (eventos do SDK e watchdog).
9. **Persistência pelo simulador (`.FLT`):** fora do caminho crítico. No voo F1.4b o EFB não salvou o `.flt` (é preciso digitar o nome com a extensão entre aspas); só investigar se virar necessidade.

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
Objetivo (revisado em 2026-11-15, regra 0.1 nova): (1) achar o **limiar** de overstress de cada componente (taxa nativa > limiar = faixa vermelha do addon); (2) saber **quais componentes o nativo acusa** em cada tipo de avião (Ativo / Observar / só addon); (3) registrar como o nativo chega a `IS FAILED` (para o evento de pane); (4) usar os trechos de abuso como **conferência** das faixas do addon (o addon tem que acusar o mesmo abuso pelos próprios sensores). A unidade da taxa foi medida (fração do nível por segundo, C152) só como informação: a taxa não vira % de desgaste.
- **Por que precisa de voo:** o nativo não expõe os limites do CFG nem a fórmula. A taxa só é conhecida por medição: em operação normal ela deve ficar em ~0; no abuso, sobe.
- **Limiar:** por componente, `limiar = max(ruído medido nos trechos normais × 3, 0,0001)`. Ruído = maior taxa vista nos trechos marcados como normais em todos os voos daquele tipo. Se o ruído normal não for ~0 (como pareceu no pneu do C152), o componente fica em Observar até um voo limpo.
- **Unidade (informativa):** integral da taxa no trecho ≈ queda do nível nativo. Confirmada no C152 (razão 0,997). Não gera fator de conversão (regra 0.1 › 3).
- **Conferência das faixas do addon:** em cada trecho de abuso, o addon tem que acusar faixa vermelha pelos próprios sensores no mesmo componente (ex.: C152 acima de 2.600 RPM). Se só o nativo acusar, falta sensor ou override no addon: corrigir a regra na F2.2. Pneus e trem também na **corrida e no pouso normais**, em pista **asfaltada** e em **terra**, separadamente.
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
- **Portão:** o usuário aprova limiares e quais componentes ficam Ativo, Observar ou só addon.

---

## FASE 2 — Peças e desgaste (normal × estresse; overstress nativo como acelerador)

### F2.1 Lista de peças do Hangar
- `Domain/Parts/PartDefinition.cs` (catálogo): `Id` (ex.: `engine.cylinders`), `Name`, `Group` (agrupador da UI: Motor, Óleo, Ignição, Seção quente, Hélice, Elétrico, Instrumentos, Combustível, Trem, Pneus, Freios, Célula, Hidráulico, Pressurização), `AppliesTo` (tipos A–I / classe de motor / condições como "trem retrátil", "carburado"), `LifeHours`, `LifeCycles?`, `VisibleOnWalkaround`, `BlocksFlightWhenBroken`, `PartPrice`, `LaborHours`.
- `Infrastructure/Data/parts_catalog.json`: montado **a partir da tabela de sensores validados** (cada peça cita os sensores que a afetam). O executor propõe a lista; o revisor aprova.
- `Domain/Entities/AircraftPart.cs`: `Id`, `PilotAircraftId`, `PartId`, `Health` (0–100, nunca exibida), `HoursSinceReplacement`, `CyclesSinceReplacement`, `LastReplacedAtUtc`, `LastReplacedAirframeHours`, `Flag` (`None`, `Suspect` = exclamação amarela), `FlaggedBy` (`Walkaround`/`Inspection`), `FlaggedAtUtc?`, `IsBroken`, `BrokenAtUtc?`. Tabela `AircraftParts`, índice único `(PilotAircraftId, PartId)`.
- Provisionamento: aeronave nova recebe as peças que se aplicam ao tipo dela, todas a 100%.
- Teste: IDs únicos; cada tipo A–I tem um conjunto coerente (carburado tem carburador, jato não tem magneto etc.).

### F2.2 🔒 Mapa sensor → estresse → peça
- **Regra 0.1 › 3:** uma regra pode ter `nativeOverstress: <id>` (só componentes Ativo na tabela, com `nativeThreshold`), usado **só como reserva** quando o addon não tem limite próprio para aquele modelo: então taxa nativa acima do limiar = faixa vermelha da regra. Com limite próprio, só os sensores do addon decidem. Nunca converte a taxa em %; o nível nativo nunca é usado.
- `Infrastructure/Data/wear_rules.json`. Cada regra: `id`, `sensors`, faixas verde/amarela/vermelha (limites por tipo e por modelo, reaproveitando `aircraftOverrides`), `parts` com `multiplierYellow` / `multiplierRed`, ou `damagePerEvent` (toque, partida, ciclo de trem, ciclo de pressurização).
- O executor propõe a tabela com justificativa (manual do fabricante / POH quando houver); o revisor aprova antes do código.
- **Excedências realistas (regra 0.1 › 4):** cada regra de excesso tem faixas de gravidade por **quanto** passou e **por quanto tempo** (não linear: pouco acima custa pouco; muito acima custa muito), com desgaste acumulado e, por faixa, `inspectionDefect` (código da discrepância de inspeção, custo, NO-GO ou não). Fonte: boletins e manuais do fabricante (motor a pistão: Lycoming SB 369 e equivalente Continental; turbina: limites de ITT/N1/torque do manual). O executor pesquisa e cita a fonte de cada número; o usuário aprova.

### F2.3 Motor de desgaste por peça
- Evento nativo (regra 0.1 › 5): `ApplyNativeFailed(part)` leva a saúde a 0% + `IsBroken` (só baixa, nunca sobe; um por peça por voo). `IS WEAK` não altera a saúde (só blackbox). Testes: peça a 80% com quebrada fica em 0%; segundo evento no mesmo voo é ignorado; `IS WEAK` e nível nativo não alteram nada.
- Overstress nativo (regra 0.1 › 3): componente Ativo **sem limite próprio do addon para o modelo** e taxa nativa > limiar → faixa vermelha (multiplicador e dano da regra do addon); nos demais casos, só as faixas dos sensores do addon. Testes: componente com limite próprio, RPM do addon na faixa verde e nativo acima do limiar → continua verde (só blackbox de conferência); componente sem limite próprio e nativo acima do limiar → vermelha; addon e nativo os dois em vermelho → um multiplicador só; nativo abaixo do limiar ou componente em Observar → só o addon; taxa reportada com o dano desativado no sim (nível travado em 1,00) → conta como vermelha e grava "dano por estresse desativado" no blackbox; avião sem wear and tear (taxa 0) → só o addon; a velocidade do desgaste nunca depende do valor da taxa; cada trecho de excedência gera uma linha no blackbox com componente, fonte (simulador/addon), pico, duração e perda.
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
- `Domain/Parts/BreakCheckEngine.cs`: a cada 10 min simulados com `Health < 30`, um sorteio por peça com **curva linear (decisão do usuário, 2026-11-14)**: `p = clamp((30 − h) / 30, 0, 1)` (remap linear inverso). Cada 1% de saúde abaixo de 30% soma ~3,33% de chance por sorteio.

  | Saúde | Chance por sorteio (10 min simulados) |
  |---|---|
  | ≥ 30% | 0% (zona segura) |
  | 29% | 3,33% |
  | 25% | 16,67% |
  | 15% | 50,00% |
  | 5% | 83,33% |
  | 0% | 100% (quebra garantida) |

  No JSON: `breakCheck.thresholdHealth = 30`, `breakCheck.intervalSimMinutes = 10`, `breakCheck.maxChance = 1.0` (override por peça permitido). O intervalo é contado em **tempo simulado acumulado** (`SimTimeStep`); o sorteio acontece cada vez que o acumulado passa de 10 min, nunca por pacote de telemetria. O acumulado zera no início de cada voo.
- Mortalidade infantil: `HoursSinceReplacement < infantHours` → `infantChancePer10Min`.
- **Peça já quebrada (`IsBroken`) não sorteia** break check até ser trocada (sem pane repetida nem ruído no blackbox). Teste: peça `IsBroken` a 0% por 1 h simulada → nenhum sorteio.
- Testes: fórmula exata nos pontos da tabela (30 → 0; 29 → 0,0333; 25 → 0,1667; 15 → 0,5; 5 → 0,8333; 0 → 1; 31 e 100 → 0; saúde negativa → 1); Monte Carlo (100 mil, semente fixa) a 29%, 20%, 10% dentro de ±10% da fórmula; 0% quebra em todos os sorteios; 9 min simulados não sorteiam, 10 min sorteiam uma vez, 25 min sorteiam duas; a 16× o número de sorteios por hora simulada é o mesmo que a 1×.

### F4.2 Efeito da quebra
- Cada peça tem `BrokenEffect` (`FailureEffect`). Quebra → `SimFailureActuator` aplica o efeito e `PersistentFailureService.Record` grava a discrepância (NO-GO se `BlocksFlightWhenBroken`). Os dois já existem.
- Peça sem efeito no sim que impede o voo: o revisor escolhe o efeito equivalente (ex.: motor → `TOGGLE_ENGINE1_FAILURE`).

### F4.2b Perda total (decisão do usuário, 2026-11-14)
- **Saúde 0% não significa avião destruído.** Hoje o `AircraftStatusEvaluator` (`ComponentMaintenance.cs`, motor e célula em 0% → "PERDA TOTAL") e o `FlightMonitor` (motor, pneus e célula em 0% → `HandleTotalLossAsync`) deduzem perda total da saúde: com a regra de pane nativa a 0%, duas panes virariam perda total por engano. Substituir por:
  1. **Acidente real** (crash confirmado pelo simulador, caminho que já existe em `ComponentFlightSession`): perda total direta, estado explícito na aeronave (`IsTotalLoss` / `TotalLossReason`), sem conta.
  2. **Perda total econômica:** `custo do reparo > totalLoss.economicThreshold × valor de referência` (JSON, `economicThreshold = 0.70`).
     - **Custo do reparo** = soma do reparo de todos os componentes/peças até 100% (`ComponentMaintenance.RepairCost`, na Fase 2 preço de troca das peças) + panes abertas (`DefectCatalog.CalculateRepairCost`).
     - **Valor de referência** = **valor de mercado do avião com todos os componentes a 100%** (ano e horas considerados; `MarketSimulationService.CalculateBaseMarketValue` com as condições a 100%), ou seja, o valor antes do dano. Nunca o valor já derrubado pelo dano (conta circular).
     - Avaliada no fim do voo e a cada mudança de saúde/pane no Hangar. Ao passar do limite: status "PERDA TOTAL (ECONÔMICA)", oferta de seguro ou descarte (fluxo do Hangar que já existe). O jogador pode reparar mesmo assim, se quiser pagar.
- **Calibração:** com os `repairCostFactor` atuais o reparo de um pistão inteiro a 0% soma ~0,285 do preço de compra, abaixo de 70% do valor de mercado: a perda econômica nunca dispararia. Recalibrar os fatores (referência: custos reais de retífica, célula, trem) para que só casos graves passem de 70% (fogo, colapso de trem com dano estrutural, motor + célula destruídos). Feito na implementação, aprovado pelo usuário.
- Lógica pura em `Domain` (`TotalLossEvaluator.Evaluate(repairCost, referenceValue, catalog)`), com testes: reparo 69% → não; 71% → sim; valor de referência ignora o dano (avião com motor a 0% usa o valor a 100%); crash → perda total mesmo com reparo barato; motor e célula a 0% sem crash e reparo < 70% → **não** é perda total.

### F4.2c Pane do wear and tear nativo (regra 0.1 › 5)
- Ler `WEAR AND TEAR IS FAILED:<id>` (componentes do avião, IDs da tabela) numa definição separada do `SimTelemetry`, em frequência baixa, e tratar a borda 0 → 1 como pane nativa: `PersistentFailureService.Record` (origem `Native`, código pelo componente) + `ApplyNativeFailed` na peça. Motivo: no C152 a pane do wear and tear não liga `ENG FAILED:1` nem `GENERAL ENG DAMAGE PERCENT` (F1.4c), e a detecção atual não registra nada.
- Testes da lógica de borda (Domain): 0 → 1 registra uma vez; 1 → 1 não repete; reinício do voo (1 → 0) não repara a discrepância.

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
- F1.4c (C152 asfalto) · 2026-11-15 · PROPOSTA, AGUARDA APROVAÇÃO · [Calibração](docs/plans/sensor-validation/calibration-C152.md) · branch docs/f1.4c-calibracao-c152
