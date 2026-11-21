# API do SimConnect (MSFS 2024) — o que o FlightCareer pode usar

Criado em 2026-11-16 (Claude). Fontes (SDK 2024, `simconnect/api-reference/`): [SimConnect Actions](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/simconnect-actions/) (página "WIP" na documentação), [Debug](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/debug/debug/), [Events And Data](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/events-and-data/events-and-data/) (inclui a **Flow API**: `SIMCONNECT_FLOW_EVENT`), [Flights](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/flights/flights/), [General](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/general/general/), [Input Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/inputevents/input-events/) (já pesquisada em `eventos-e-input-events.md` § 2).

## 1. Resumo do que mais rende

| Recurso | Uso no FlightCareer |
|---|---|
| **`SimConnect_FlightSave` / `SimConnect_FlightLoad`** | **Continuidade da carreira** (ideia do usuário): ao fim de cada voo (ou ao fechar o app no meio), salvar o estado do voo num `.FLT` da aeronave; no Hangar, botão **"Continuar de onde parou"** carrega o voo com o avião na vaga onde pousou, com combustível, carga e sistemas como ficaram. Ver § 2. |
| **`SimConnect_FlightPlanLoad`** | Carregar no GPS do avião o **plano de voo da missão** (`.PLN` gerado pelo app): origem, destino, alternativa e rota prontos no briefing. |
| **Flow API** (`SimConnect_SubscribeToFlowEvent`) | Eventos de fluxo do MSFS 2024: `TELEPORT_START/DONE`, **`BACK_ON_TRACK_START/DONE`** ("voltar no tempo" depois de um erro/acidente), `SKIP_START/DONE`, `RTC_START/END`, **`REPLAY_START/END`**, `FLIGHT_START/END`, **`PLANE_CRASH`**, `FLT_LOAD/FLT_LOADED`, `BACK_TO_MAIN_MENU`. **Integridade** (teletransporte e Back On Track em voo = invalidar; replay = pausar a leitura), **acidente** (`PLANE_CRASH` substitui o `CRASH FLAG` obsoleto, junto com o evento `Crashed`) e **fluxo** (início/fim de voo do sim, volta ao menu = voo interrompido). |
| **`SimConnect_ExecuteAction`** (SimConnect Actions) | **`FailureAction`** (falha de sistema com `System`, `SystemIndex`, `Behavior` e **`HealthPercent`** — pane nativa com saúde parcial; enums ainda não documentados), **`RefillAction`** (`PercentFuel` por sistema: alternativa ao abastecimento por SimVar), **`DialogAction`/`InstructorDialogAction`** (mensagem com texto e som **dentro do sim** — alerta de pane visível em tela cheia/VR), **`FocusInstrumentAction`/`LookAtInstrumentAction`** (destacar o instrumento da pane no cockpit), `OneShotSoundAction`/`PlaySoundAction` (som), **`RumbleAction`** (vibração do controle no pouso duro/pane), `FadeToColorAction` (tela escura no acidente), `ShowLogbookAction`, `ChangeAssistanceItemAction` (ajustes de assistência — **não mexer sem decisão**: são opções do usuário), `AITakeControlsAction` (não usar). |
| `SimConnect_EnumerateSimObjectsAndLiveries` | Lista os aviões e **pinturas** instalados: casar cada aeronave da frota com o título/pintura do sim (escolha no Hangar, conferência no início do voo). |
| `SimConnect_RequestSystemState` | Pergunta direta ao sim (caminho do avião carregado `AircraftLoaded`, voo carregado, modo diálogo): identificar o avião ao conectar sem esperar o evento. |
| `SimConnect_RequestDataOnSimObjectType` | Objetos num raio do avião (tráfego, veículos): quase colisão, gate ocupado. **Cuidado** com tráfego de IA (suspeita das quedas com `AI *`). |
| `SimConnect_RequestReservedKey` | Atalho de teclado reservado ao app (ex.: marcador do REC, abrir o HUD). |
| `SimConnect_CreateClientData` / `RequestClientData` | Área de dados compartilhada com outro add-on ou módulo WASM (futuro: ler LVars de aviões de terceiros por um módulo próprio). |
| Debug: `SimConnect_GetLastSentPacketID`, `SimConnect_RequestResponseTimes`, `InsertString`/`RetrieveString` | Ligar exceções ao pedido que as causou (já usado no log `NAME_UNRECOGNIZED`); medir a latência da conexão; escrever strings de tamanho variável (ex.: `ATC ID`). |
| Input Events (`EnumerateInputEvents`, `Get/Set/SubscribeInputEvent`, `MapInputEventToClientEvent_EX1`) | Ler/escrever botões dos aviões modernos por nome (`eventos-e-input-events.md` § 2). `MapInputEventToClientEvent` (sem `_EX1`) está **obsoleto por bug**. `EnumerateControllers`: saber se o piloto usa manete física (testes de pane de comando). |

## 2. Continuar de onde parou (`FlightSave`/`FlightLoad`)
- **Como funcionaria:** o app guarda um `.FLT` por aeronave da frota (pasta do app em `%LocalAppData%\FlightCareer\flights\<aeronave>.FLT`). Ao terminar o voo na vaga (ou ao fechar o app/sim no meio do voo), `FlightSave`. No Hangar, "Continuar de onde parou" chama `FlightLoad` com o arquivo daquela aeronave.
- **Ganhos:** o avião fica **no aeroporto onde pousou** (carreira contínua, sem teletransporte entre voos), com o combustível e a carga que sobraram; voo interrompido pode ser retomado no ar.
- **Cuidados (testar no MSFS 2024):**
  1. O `.FLT` é texto: conferir o que ele guarda (posição, combustível, carga, chaves, falhas nativas, clima, hora) e o que o app precisa sobrepor (combustível e panes continuam vindo do app: a sincronização da COMBUSTIVEL-REAL e o `PersistentFailureService` reaplicam depois do `FLT_LOADED`).
  2. `FlightLoad` a partir de um app externo com o MSFS 2024 já num voo: confirmar que funciona e usar o evento de fluxo `FLT_LOADED` para saber quando terminou.
  3. Integridade: o arquivo é editável; o app guarda um hash e confere no carregamento (ou regrava posição/combustível pelos próprios valores).
  4. Clima e hora: decidir se o "continuar" usa o clima/hora reais ou os salvos.
- **Decisão do usuário:** salvar automaticamente ao fim de todo voo? Retomar voo interrompido no ar?

## 3. Decisões para o usuário
1. "Continuar de onde parou" com `FlightSave`/`FlightLoad` (§ 2).
2. Plano de voo da missão carregado no GPS (`FlightPlanLoad`).
3. Flow API: teleporte e Back On Track em voo invalidam o voo; `PLANE_CRASH` como sinal de acidente.
4. Alertas dentro do sim (`DialogAction` com texto e som, destaque do instrumento, vibração do controle) junto com o HUD.
5. `FailureAction` com `HealthPercent`: testar como alternativa às panes por `TOGGLE_*` (enums não documentados).

**Decididas pelo usuário (2026-11-19, `docs/tarefas_sem_IA.md`):**
1. **`FlightSave`/`FlightLoad`:** só no solo (parado, freio de estacionamento, motores cortados); depois do load o app reaplica combustível, payload e panes do banco.
2. **`FlightPlanLoad`:** padrão no briefing, com opção "Carregar rota no GPS do simulador" (marcada); **só origem e destino da perna ativa**, sem rota completa.
3. **Flow API:** teleporte e Back On Track após o off-block ou em voo invalidam como o slew; `PLANE_CRASH` encerra como acidente (aeronave AOG, seguro/resgate), com ~10 s de carência no carregamento.
4. **Alertas no sim (modular por nível de realismo):** vibração do controle em voo (toque duro, estol, overspeed, pneu); destaque de instrumento só no modo assistido; `DialogAction` só no solo (em voo, só som curto).
5. **`FailureAction` com `HealthPercent`:** testar como camada complementar (`IFailureDriver`, estratégia por sistema registrada no JSON da aeronave), com `TOGGLE_*`/`FUELSYSTEM_*` como fallback.

## 4. Próximos passos
1. Testes no MSFS: `FlightSave` ao fim de um voo + `FlightLoad` pelo app; `SubscribeToFlowEvent` (teleporte pelo mapa, Back On Track, replay, crash); `ExecuteAction` com `DialogAction` e `FailureAction`; `FlightPlanLoad` com um `.PLN` simples.
2. Spec para o Gemini da Flow API (integridade e acidente) — encaixa na SLEW-INTEGRIDADE e na troca do `CRASH FLAG`.
