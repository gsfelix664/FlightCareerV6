# Key Events, Input Events e Variáveis no MSFS 2024 SDK

> **Documento gerado na tarefa RESEARCH-SIMVARS-r2**  
> **Objetivo:** Analisar a infraestrutura de eventos de simulação, controle de panes via Key Events, interação com o cockpit via API de Input Events e suporte nativo a LVars/componentes no Microsoft Flight Simulator 2024.

---

## 1. Key Events de Falha e de Sistemas

### 1.1 Confirmação dos Eventos Utilizados no FlightCareer
Todos os eventos de falha e acionamento empregados atualmente no código do FlightCareer foram confrontados diretamente com a documentação oficial do SDK MSFS 2024:

| Key Event (SDK) | Utilizado no FlightCareer | Confirmação no SDK 2024 | Parâmetros | Comportamento Documentado no SDK | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|
| `TOGGLE_ENGINE1_FAILURE` .. `TOGGLE_ENGINE4_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna o estado de falha catastrófica do motor correspondente (desliga combustão, impede religamento até reparo). | [Aircraft Engine Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-engine-events/) |
| `TOGGLE_ELECTRICAL_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a pane geral do sistema elétrico legado, cortando barramentos dependentes. | [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-electrical-events/) |
| `TOGGLE_VACUUM_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a perda de sucção da bomba de vácuo, levando à degradação dos giroscópios analógicos (AI e DG). | [Aircraft System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-system-events/) |
| `TOGGLE_PITOT_BLOCKAGE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna o bloqueio do tubo de pitot (ex.: por gelo ou capa esquecida), congelando o velocímetro. | [Aircraft System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-system-events/) |
| `TOGGLE_STATIC_PORT_BLOCKAGE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna o bloqueio da tomada estática, congelando o altímetro e travando o VSI em zero. | [Aircraft System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-system-events/) |
| `TOGGLE_HYDRAULIC_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a pane de pressão do sistema hidráulico principal. | [Aircraft System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-system-events/) |
| `TOGGLE_TOTAL_BRAKE_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a falha total de frenagem (ambos os pedais sem efeito). | [Aircraft Flight Control Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-flight-control-events/) |
| `TOGGLE_LEFT_BRAKE_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a falha do freio do trem esquerdo (gera guinada em frenagens no solo). | [Aircraft Flight Control Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-flight-control-events/) |
| `TOGGLE_RIGHT_BRAKE_FAILURE` | Sim (`SimFailureActuator`) | **Confirmado** | Nenhum (`0`) | Alterna a falha do freio do trem direito. | [Aircraft Flight Control Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-flight-control-events/) |
| `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` | Sim (`SimConnectService.SetAlternatorConnected`) | **Confirmado** | `(bus, alt)` via `TransmitClientEvent_EX1` | Desconecta fisicamente o barramento especificado do alternador especificado no sistema elétrico modular moderno. Deixa a chave física do cockpit em ON enquanto a bateria descarrega. | [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-electrical-events/) |

### 1.2 Eventos de Falha Adicionais Documentados no SDK que Podem Virar Novas Panes
A documentação oficial do SDK lista eventos com suporte a parâmetros que podem enriquecer o catálogo de discrepâncias e panes persistentes:

1. **`TOGGLE_ALTERNATE_STATIC`**
   * **Parâmetro:** Nenhum (`0`).
   * **Aplicação no FlightCareer:** Pane de static port com recuperação operacional pelo piloto. Se o static port principal estiver bloqueado (`TOGGLE_STATIC_PORT_BLOCKAGE`), o piloto pode puxar a tomada estática alternativa no cockpit para restaurar as indicações do altímetro e velocímetro com leve erro de pressão da cabine.
   * **Referência:** [Aircraft System Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-system-events/)

2. **`EXTINGUISH_ENGINE_FIRE`**
   * **Parâmetro:** Índice do motor (1 a 4).
   * **Aplicação no FlightCareer:** Extinção de incêndio de motor (`ENG ON FIRE`). Permite acionar o disparo de garrafas extintoras em bimotores/turbinas ou verificar se o procedimento de corte de emergência apagou as chamas.
   * **Referência:** [Aircraft Engine Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-engine-events/)

3. **`ELECTRICAL_CIRCUIT_BREAKER_TOGGLE`**
   * **Parâmetro:** Índice do disjuntor / circuito.
   * **Aplicação no FlightCareer:** Desarme de disjuntores específicos por sobrecorrente (ex.: flap motor breaker, turn coordinator breaker, pitot heat breaker). Permite simular panes parciais de circuitos elétricos específicos sem derrubar todo o barramento elétrico.
   * **Referência:** [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-electrical-events/)

4. **`TOGGLE_ELECT_FUEL_PUMP1` .. `TOGGLE_ELECT_FUEL_PUMP4` / `FUEL_PUMP`**
   * **Parâmetro:** Índice do motor ou da bomba.
   * **Aplicação no FlightCareer:** Pane na bomba elétrica auxiliar de combustível (fuel pump inoperante na partida ou em decolagem).
   * **Referência:** [Aircraft Fuel Events](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/key-events/aircraft-fuel-events/)

---

## 2. API de Input Events (MSFS 2024)

### 2.1 Visão Geral e Funcionamento da API
A API de **Input Events** (eventos de entrada / interações do cockpit) introduzida formalmente no MSFS 2024 permite que clientes SimConnect interajam diretamente com as chaves, botões, manetes e seletores do cockpit 3D, espelhando os chamados `B:vars` (Behaviors) e eventos de template de animação.

A API é composta por quatro funções principais na biblioteca `Microsoft.FlightSimulator.SimConnect`:

```cpp
// 1. Enumera os Input Events registrados para o SimObject atual
HRESULT SimConnect_EnumerateInputEvents(
    HANDLE hSimConnect,
    SIMCONNECT_DATA_REQUEST_ID RequestID
);

// 2. Consulta o valor atual de um Input Event específico pelo Hash UINT64
HRESULT SimConnect_GetInputEvent(
    HANDLE hSimConnect,
    SIMCONNECT_DATA_REQUEST_ID RequestID,
    UINT64 Hash
);

// 3. Define o valor de um Input Event (aciona a chave no cockpit 3D)
HRESULT SimConnect_SetInputEvent(
    HANDLE hSimConnect,
    UINT64 Hash,
    DWORD cbUnitSize,
    void* pValue
);

// 4. Assina notificações automáticas de mudança de estado de um Input Event
HRESULT SimConnect_SubscribeInputEvent(
    HANDLE hSimConnect,
    UINT64 Hash
);
```

### 2.2 O que a API de Input Events Resolve para o FlightCareer
1. **Chave de Aviônicos no G1000 e Glass Cockpits:**  
   Em aviônicos baseados em HTML/JS (como o G1000 NXi padrão do C172), a chave física de aviônicos no painel aciona internamente um Input Event do template de Model Behavior (ex.: `B:ELECTRICAL_Avionics_Bus_1_Toggle` ou `B:AVIONICS_BUS_1_Switch`), enquanto a SimVar legada `AVIONICS MASTER SWITCH` muitas vezes permanece em 1 ou não reflete o estado elétrico da chave. Com `SimConnect_SubscribeInputEvent`, o FlightCareer pode escutar o evento real da chave e detectar fielmente se a partida foi dada com os aviônicos ligados (regra `avionicsMasterStartSpikes`).
2. **Eliminação do Mascaramento de Chaves:**  
   Atualmente, quando uma pane ocorre, algumas chaves precisam ser forçadas a cada frame via SimVar (como magnetos), o que pode fazer a chave tremer visualmente no cockpit 3D. A API de Input Events permite atuar sobre a mecânica da chave ou travar o estado do circuito no próprio sistema de behaviors.
3. **Leitura de Disjuntores (Circuit Breakers):**  
   Muitas aeronaves modernas no MSFS 2024 modelam disjuntores push-pull interativos cujos estados são expostos como Input Events individuais, permitindo que inspeções pré-voo detectem disjuntores desarmados no walkaround de cabine.

### 2.3 Limitações e Desafios da API de Input Events
* **Identificação por Hash Dinâmico (UINT64):** Os Input Events não possuem identificadores estáticos globais fixos. Cada aeronave carrega seus próprios templates durante o carregamento do modelo. É obrigatório chamar `SimConnect_EnumerateInputEvents` no evento `AircraftLoaded`, mapear os nomes para os hashes `UINT64` recebidos no callback `SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS`, e só então usar `GetInputEvent` ou `SubscribeInputEvent`.
* **Falta de Nomenclatura Padronizada:** Diferentes fabricantes e desenvolvedores adotam convenções distintas de nomes (ex.: `B:CIRCUIT_BREAKER_NAV_1` em uma aeronave vs `B:CB_NAV1_PULLED` em outra).
* **Sobrecarga de Eventos Assíncronos:** Assinar muitos Input Events simultaneamente pode gerar sobrecarga no pipeline de despacho de mensagens do SimConnect. Deve-se assinar apenas os switches estritamente necessários para as regras de monitoramento.

**Referência Oficial do SDK:**  
- [Input Events API Reference](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/inputevents/input-events/)  
- [SimConnect_EnumerateInputEvents](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/inputevents/input-events/#simconnect_enumerateinputevents)  
- [SimConnect_SubscribeInputEvent](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/inputevents/input-events/#simconnect_subscribeinputevent)

---

## 3. Leitura e Escrita de LVars e Escopos pelo SimConnect no MSFS 2024

### 3.1 Suporte Nativo a LVars sem Módulo WASM
Uma das maiores evoluções do SimConnect no MSFS 2024 é a possibilidade de registrar e manipular variáveis locais (`L:`) diretamente através da função canônica `SimConnect_AddToDataDefinition`, sem a necessidade de instalar módulos WebAssembly (WASM) intermediários ou servidores externos.

* **Sintaxe de Registro:**
  ```csharp
  // Exemplo em C# gerenciado (Managed SimConnect SDK)
  _simConnect.AddToDataDefinition(
      DEFINITIONS.CustomLocalData, 
      "L:IS_AVATAR", 
      "number", 
      SIMCONNECT_DATATYPE.FLOAT64, 
      0.0f, 
      SimConnect.SIMCONNECT_UNUSED
  );
  ```
* **Regras Oficiais Documentadas:**
  1. O nome da variável deve incluir o prefixo `L:` (ex.: `"L:AIRCRAFT_SYSTEM_STATE"`).
  2. O tipo de dado canônico no SimConnect é `SIMCONNECT_DATATYPE.FLOAT64`.
  3. A unidade especificada (`"number"`, `"bool"`, etc.) converte a escala do dado.
  4. O escopo das LVars é compartilhado entre os instrumentos e sistemas do modelo da aeronave ativa.

### 3.2 Suporte a Escopos no Vars API (`A:`, `E:`, `L:`, `I:`, `O:`, `Z:`)
O SDK do MSFS 2024 unificou o acesso a múltiplos escopos de variáveis internas:
* **`A:` (Aircraft Simulation Variables):** O conjunto tradicional de SimVars do simulador (ex.: `(A:AIRSPEED INDICATED, knots)`).
* **`E:` (Environment Variables):** Variáveis globais de ambiente (ex.: `SIMULATION RATE`).
* **`L:` (Local Variables):** Variáveis internas da aeronave e gauges.
* **`I:` (Input Variables):** Variáveis ligadas a inputs e interatores de controle.
* **`O:` (Output Variables):** Variáveis ligadas a saídas mapeadas de componentes.
* **`Z:` (Local Macro/Gauge Variables):** Variáveis com escopo isolado de um display ou template XML.

### 3.3 Variáveis Indexadas por Nome de Componente (`:'ComponentName'_n`)
No MSFS 2024, sistemas modulares (elétrica, pneumática, combustível, contact points) frequentemente possuem estruturas com nomes semânticos atribuídos nos arquivos de configuração (`systems.cfg`, `flight_model.cfg`). Para evitar fragilidades com índices numéricos estáticos que mudam quando mods são combinados, o SDK estabelece a sintaxe oficial de índice nomeado:

$$\text{SIMVAR\_NAME} : \text{'ComponentName'\_n}$$

* **Exemplos Oficiais do SDK:**
  * Ponto de contato: `CONTACT POINT IS SKIDDING:'Wheel_Front'_n`
  * Barramento elétrico modular: `ELECTRICAL BUS VOLTAGE:'Bus_1'_n`
  * Duto de ar condicionado / pack: `PNEUMATICS DUCT PRESSURE:'Pack_1'_n`
* **Vantagem para o FlightCareer:**  
  Permite que o `AircraftLimitCatalogLoader` configure limites elétricos e pneumáticos associados diretamente ao nome semântico do barramento da aeronave (ex.: `'Avionics_Bus_1'`), eliminando o risco de leitura de barramentos trocados por desalinhamento de índice numérico.

**Referência Oficial do SDK:**  
- [SimConnect_AddToDataDefinition](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/events-and-data/events-and-data/#simconnect_addtodatadefinition)  
- [Simulation Variables - Variable Scopes](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/)  
- [Named Component Indexing Guidelines](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/#named_indices)
