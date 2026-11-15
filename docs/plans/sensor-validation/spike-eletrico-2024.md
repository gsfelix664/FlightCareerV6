# Spike de Pesquisa — SimVars Elétricas no MSFS 2024

**Data:** 2026-11-15  
**Autor:** Gemini (Executor)  
**Contexto:** Validação de Sensores (F2.1b / F2.4), investigação de telemetria elétrica no Cessna 172SP G1000 e aeronaves com sistema elétrico modular v2.

---

## 1. Documentação Oficial Consultada

As pesquisas e especificações deste documento foram extraídas diretamente da documentação oficial do SDK do Microsoft Flight Simulator 2024 (retail):

1. **Aircraft Electrics Variables:**  
   `https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm`
2. **Aircraft Engine Variables:**  
   `https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Engine_Variables.htm`
3. **Electrical Systems Configuration (`systems.cfg`):**  
   `https://docs.flightsimulator.com/msfs2024/html/5_Content_Config/CFGs/Systems/Electrical.htm`
4. **Electric System Setup Information:**  
   `https://docs.flightsimulator.com/msfs2024/html/5_Content_Config/CFGs/Systems/Electric_System_Setup.htm`
5. **Legacy Electrical System:**  
   `https://docs.flightsimulator.com/msfs2024/html/5_Content_Config/CFGs/Systems/Legacy_Electrical.htm`

---

## 2. Diagnóstico dos Problemas Observados no C172SP G1000

No voo de validação do Cessna 172SP G1000 (`20261115-1534-C172SP_G1000_Passengers.csv`), três sintomas críticos foram constatados na telemetria elétrica:

| Medida | SimVar Utilizada | Valor Observado | Diagnóstico / Causa Raiz |
|---|---|---|---|
| **Master de Aviônicos** | `AVIONICS MASTER SWITCH` | `1` o voo todo | O C172 G1000 utiliza dois barramentos de aviônicos físicos (`AVIONICS BUS 1` e `AVIONICS BUS 2`). A SimVar legada não indexada não reflete as chaves físicas individuais do painel G1000 nem a desenergização do barramento. |
| **Tensão da Bateria** | `ELECTRICAL BATTERY VOLTAGE` | `25,137 V` constante | A SimVar foi consultada **sem índice** (`ELECTRICAL BATTERY VOLTAGE`). O SDK exige índice de bateria (`:1`). Sem índice, o SimConnect devolve a tensão estática nominal de circuito aberto (repouso químico), sem responder à carga ou recarga. |
| **Carga da Bateria** | `ELECTRICAL BATTERY LOAD` | `0` constante | Também consultada **sem índice**. O SDK exige índice de bateria (`:1`). |
| **Tensão do Barramento Principal** | `ELECTRICAL MAIN BUS VOLTAGE` | `0` constante | Consultada **sem índice**. O SDK exige índice de barramento (`:1`, `:2`). |
| **Saída do Alternador** | `ELECTRICAL GENALT BUS VOLTAGE:1`, `AMPS:1`, `LOAD:1` | `0` o voo todo | No sistema modular elétrico v2 do MSFS 2024, o alternador alimenta os barramentos principais (`Main Bus 1/2`), não um barramento isolado legado "GENALT". O estado da conexão do alternador é medido por barramento ou conexão (`ALTERNATOR CONNECTION ON:1`). |
| **Carga Total do Sistema** | `ELECTRICAL TOTAL LOAD AMPS` | `25,52 A` a `51,52 A` | **Funcionou perfeitamente.** Mede com exatidão a corrente total consumida pelo conjunto de barramentos da aeronave. |

---

## 3. Matriz de Candidatos por Medida

### 3.1. Master de Aviônicos (Avionics Master)

| Ordem | SimVar | Unidade | Indexação | Leitura/Escrita | MSFS 2020 | Sistema Modular v2? | Justificativa |
|:---:|---|---|---|:---:|:---:|:---:|---|
| **1º** | `ELECTRICAL AVIONICS BUS VOLTAGE` | `volts` | Não indexado | R | Sim | Sim / Ambos | **Mais robusto:** Mede a tensão real presente no barramento de aviônicos. Se os switches de aviônicos forem desligados, a tensão cai para 0 V, independentemente de como as chaves 3D foram mapeadas no modelo. |
| **2º** | `AVIONICS MASTER SWITCH:1` | `bool` | `:index` (1) | R/W | Sim | Sim / Ambos | Posição da chave de aviônicos 1 (Avionics Bus 1). Documentado explicitamente no SDK como indexado por circuito/chave de aviônicos. |
| **3º** | `AVIONICS MASTER SWITCH:2` | `bool` | `:index` (2) | R/W | Sim | Sim / Ambos | Posição da chave de aviônicos 2 (Avionics Bus 2). |
| **4º** | `ELECTRICAL AVIONICS BUS AMPS` | `amperes` | Não indexado | R | Sim | Sim / Ambos | Corrente consumida no barramento de aviônicos. Vai a 0 A quando desligado. |
| **5º** | `CIRCUIT SWITCH ON:1` / `:2` | `bool` | `:index` | R/W | Sim | Sim | Chave lógica do circuito no sistema modular v2. Útil se a aeronave controlar aviônicos via circuito em vez de chave geral. |
| **6º** | `CIRCUIT ON:1` / `:2` | `bool` | `:index` | R | Sim | Sim | Indica se o circuito está energizado e funcionando. |

**URL da documentação oficial:**  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#AVIONICS_MASTER_SWITCH`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_AVIONICS_BUS_VOLTAGE`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#CIRCUIT_SWITCH_ON`

---

### 3.2. Bateria (Tensão, Carga e Descarga)

| Ordem | SimVar | Unidade | Indexação | Leitura/Escrita | MSFS 2020 | Sistema Modular v2? | Justificativa |
|:---:|---|---|---|:---:|:---:|:---:|---|
| **1º** | `ELECTRICAL BATTERY LOAD:1` | `amperes` | `:index` (1) | R | Sim | Sim / Ambos | **Métrica ideal de descarga:** Valores positivos indicam descarga (bateria alimentando os barramentos); valores negativos indicam carga (recebendo corrente do alternador). Exige índice `:1`. |
| **2º** | `ELECTRICAL BATTERY BUS VOLTAGE` | `volts` | Não indexado | R | Sim | Sim / Ambos | Tensão no barramento da bateria. Deve refletir a tensão de carga (~28 V) com alternador operando e a tensão de descarga (~24 V a 25 V) sem alternador. |
| **3º** | `ELECTRICAL BATTERY VOLTAGE:1` | `volts` | `:index` (1) | R | Sim | Sim / Ambos | Tensão nos terminais da bateria principal com o índice obrigatório `:1`. |
| **4º** | `ELECTRICAL HOT BATTERY BUS VOLTAGE` | `volts` | Não indexado | R | Sim | Sim / Ambos | Tensão no barramento permanente (hot bus) ligado diretamente à bateria, energizado mesmo com a chave master geral desligada. |

**URL da documentação oficial:**  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_BATTERY_LOAD`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_BATTERY_VOLTAGE`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_BATTERY_BUS_VOLTAGE`

---

### 3.3. Alternador e Barramento Principal (Main Bus)

| Ordem | SimVar | Unidade | Indexação | Leitura/Escrita | MSFS 2020 | Sistema Modular v2? | Justificativa |
|:---:|---|---|---|:---:|:---:|:---:|---|
| **1º** | `ELECTRICAL MAIN BUS VOLTAGE:1` | `volts` | `:index` (1) | R | Sim | Sim / Ambos | **Tensão do barramento principal 1:** Deve registrar ~28 V com alternador em funcionamento e cair para ~24 V quando o alternador for desligado. Exige índice de barramento (`:1`). |
| **2º** | `ELECTRICAL MAIN BUS AMPS:1` | `amperes` | `:index` (1) | R | Sim | Sim / Ambos | Corrente total distribuída pelo barramento principal 1. |
| **3º** | `ALTERNATOR CONNECTION ON:1` | `bool` | `:index` (1) | R | Sim | Sim | Estado da conexão do alternador ao barramento. Quando o alternador é desarmado ou desligado via `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`, esta variável vai a 0. |
| **4º** | `ELECTRICAL MAIN BUS VOLTAGE:2` | `volts` | `:index` (2) | R | Sim | Sim / Ambos | Tensão do barramento principal 2 (presente em aeronaves com barramento duplo como o C172). |
| **5º** | `GENERAL ENG GENERATOR ACTIVE:1` | `bool` | `:index` (1) | R/W | Sim | Sim / Ambos | Status do gerador/alternador do motor 1 na tabela de variáveis de motor. |

**URL da documentação oficial:**  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_MAIN_BUS_VOLTAGE`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ALTERNATOR_CONNECTION_ON`  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Engine_Variables.htm#GENERAL_ENG_GENERATOR_ACTIVE`

---

### 3.4. Carga Total do Sistema (Total Load)

| SimVar | Unidade | Indexação | Leitura/Escrita | MSFS 2020 | Sistema Modular v2? | Justificativa |
|---|---|---|---|:---:|:---:|---|
| `ELECTRICAL TOTAL LOAD AMPS` | `amperes` | Não indexado | R | Sim | Sim / Ambos | **Totalmente validada no C172:** Variação consistente entre 25,5 A e 51,5 A no log de validação. Representa a carga instantânea agregada de todo o sistema elétrico. |

**URL da documentação oficial:**  
`https://docs.flightsimulator.com/msfs2024/html/6_Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrics_Variables.htm#ELECTRICAL_TOTAL_LOAD_AMPS`

---

## 4. Recomendações para as Três Regras Mudas

### 4.1. `avionics_start_with_avionics_master_on`
- **Comportamento atual:** A regra monitora `elec-avionics-master-switch`, `eng-combustion` e `eng-starter-active`. Como a SimVar antiga permaneceu em `1`, a regra dispara falso-positivo permanente ou não detecta a chave OFF.
- **Recomendação após o teste:**  
  Substituir a checagem por:
  $$\text{Aviônicos Ativos} \iff (\text{ELECTRICAL AVIONICS BUS VOLTAGE} > 5.0\text{ V}) \lor (\text{AVIONICS MASTER SWITCH:1} == 1)$$
  A medição de tensão do barramento de aviônicos (`ELECTRICAL AVIONICS BUS VOLTAGE`) é a mais confiável porque independe de o avião ter 1 ou 2 chaves físicas no cockpit 3D. Se o barramento estiver desenergizado durante o acionamento do starter, não há pico de tensão nos aviônicos.

### 4.2. `battery_deep_discharge_damage`
- **Comportamento atual:** Monitora `elec-battery-voltage`, `elec-battery-load` e `elec-master-battery`. As duas primeiras não variavam por falta de índice.
- **Recomendação após o teste:**  
  Utilizar `elec-battery-load-1` (`ELECTRICAL BATTERY LOAD:1`) e `elec-battery-bus-voltage` (`ELECTRICAL BATTERY BUS VOLTAGE`):
  $$\text{Descarga da Bateria} \iff (\text{ELECTRICAL BATTERY LOAD:1} > 2.0\text{ A}) \land (\text{ELECTRICAL BATTERY BUS VOLTAGE} < 23.5\text{ V})$$
  Quando o alternador está ativo, `ELECTRICAL BATTERY LOAD:1` é negativo (carga) ou zero, e a tensão do barramento fica em ~28 V. Se o alternador estiver desligado com sistemas ligados, `ELECTRICAL BATTERY LOAD:1` torna-se fortemente positivo e a tensão cai progressivamente.

### 4.3. `electrical_system_overload_amps`
- **Comportamento atual:** Monitora `elec-total-load-amps` e `elec-genalt-load`. O `elec-genalt-load` permaneceu em 0.
- **Recomendação após o teste:**  
  Como `ELECTRICAL TOTAL LOAD AMPS` funciona com alta precisão, a sobrecarga deve ser avaliada diretamente comparando `ELECTRICAL TOTAL LOAD AMPS` (ou `ELECTRICAL MAIN BUS AMPS:1`) com a capacidade nominal do alternador da aeronave (extraída do perfil de limites do POH, ex.: 60 A no C172):
  $$\text{Sobrecarga} \iff \text{ELECTRICAL TOTAL LOAD AMPS} > \text{RatedAlternatorAmps} \times 1.10$$
  Dessa forma, a regra não depende de o desenvolvedor do avião ter configurado a curva `curve.N` de percentual de carga de alternador no `systems.cfg`.

---

## 5. Riscos e Compatibilidade

1. **Aeronaves do MSFS 2020 vs 2024:**
   - No MSFS 2020, aeronaves legadas (`Major = 1`) utilizam barramentos globais onde `ELECTRICAL MAIN BUS VOLTAGE` sem índice podia funcionar. No MSFS 2024, a arquitetura modular v2 exige os índices `:1`, `:2` para barramentos específicos.
   - As variáveis em nível de barramento (`ELECTRICAL AVIONICS BUS VOLTAGE`, `ELECTRICAL BATTERY BUS VOLTAGE`, `ELECTRICAL MAIN BUS VOLTAGE:1` e `ELECTRICAL TOTAL LOAD AMPS`) existem em ambas as versões do simulador, garantindo retrocompatibilidade.
2. **Aeronaves Complexas de Terceiros (WASM / Custom):**
   - Jatos e aviões de estudo (Fenix, PMDG) frequentemente implementam lógica elétrica própria em C++/WASM e podem não atualizar todas as SimVars de circuito interno. No entanto, quase todos mantêm a sincronização dos barramentos principais e de aviônicos (`AVIONICS BUS VOLTAGE`, `MAIN BUS VOLTAGE`) para alimentar os instrumentos e aviônicos integrados do simulador.
3. **Limite de 16 Novos Datums no Registro:**
   - A adição de 16 novos datums com `perEngine: false` preserva a arquitetura de blocos de 256 posições do `SensorFrameReader`, sem risco de saturação ou degradação de performance SimConnect.
