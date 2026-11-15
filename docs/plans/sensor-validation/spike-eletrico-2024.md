# Spike de Pesquisa — SimVars Elétricas no MSFS 2024

**Data:** 2026-11-15  
**Autor:** Gemini (Executor)  
**Contexto:** Validação de Sensores (F2.1b / F2.4), investigação de telemetria elétrica no Cessna 172SP G1000 e aeronaves com sistema elétrico modular v2.

---

## 1. Documentação Oficial Consultada

Todas as páginas abaixo foram consultadas diretamente na documentação oficial do SDK do Microsoft Flight Simulator 2024 (versão retail) e confirmadas com código HTTP 200:

1. **Aircraft Electrics Variables:**  
   `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/`
2. **Aircraft System Variables:**  
   `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/`
3. **Aircraft Engine Variables:**  
   `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/`
4. **Simulation Variables (Regras Gerais de Indexação e Nomes):**  
   `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/`
5. **SimConnect API Reference (`SimConnect_AddToDataDefinition`):**  
   `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/events-and-data/simconnect_addtodatadefinition/`

---

## 2. Diagnóstico dos Problemas Observados no C172SP G1000

No voo de validação do Cessna 172SP G1000 (`20261115-1534-C172SP_G1000_Passengers.csv`), foram constatadas anomalias em variáveis elétricas críticas. A tabela a seguir distingue com rigor o que foi **observado no CSV** das **hipóteses técnicas** decorrentes da arquitetura elétrica modular v2 do MSFS 2024:

| Medida | SimVar Utilizada | Valor Observado no CSV | Diagnóstico / Hipótese Técnica |
|---|---|---|---|
| **Master de Aviônicos** | `AVIONICS MASTER SWITCH` | `1` constante durante todo o voo (inclusive com aviônicos desligados na partida) | **Observado:** A coluna `elec-avionics-master-switch` registrou `1` do início ao fim.<br>**Hipótese:** No sistema modular v2, a SimVar `AVIONICS MASTER SWITCH` requer nome ou índice de circuito específico de aviônicos (`Circuit name or index`). Quando consultada sem índice, o motor de simulação reporta o estado padrão legado, não refletindo o chaveamento de circuitos no cockpit 3D. |
| **Tensão da Bateria** | `ELECTRICAL BATTERY VOLTAGE` | `25,137 V` constante, sem qualquer flutuação com starter, alternador ou descarga | **Observado:** A coluna `elec-battery-voltage` permaneceu estática em 25,137 V.<br>**Hipótese:** A SimVar foi requisitada sem índice. No sistema v2+, a documentação oficial especifica que `ELECTRICAL BATTERY VOLTAGE` requer nome ou índice da bateria (`Battery name (or index)`) e devolve a tensão **apenas quando a bateria é utilizada como fornecedora** (sendo 0 V quando não fornecedora ou em recarga). No legado FSX/v1.00, exige índice numérico (`:1`). A ausência de índice fez o SimConnect devolver valor fixo do datum em repouso. |
| **Carga da Bateria** | `ELECTRICAL BATTERY LOAD` | `0` constante o voo todo | **Observado:** A coluna `elec-battery-load` registrou 0 A continuamente.<br>**Hipótese:** Consultada sem índice. No sistema v2+, requer nome ou índice da bateria e mede a corrente drenada quando a bateria é fornecedora; no v1.00/FSX requer `:1` (onde negativo indicava recarga). Sem indexação, a variável não atualizou. |
| **Tensão do Barramento Principal** | `ELECTRICAL MAIN BUS VOLTAGE` | `0` constante o voo todo | **Observado:** A coluna `elec-main-bus-voltage` registrou 0 V.<br>**Hipótese:** Consultada sem índice. O SDK classifica `ELECTRICAL MAIN BUS VOLTAGE` como legada (v1.00/FSX), orientando expressamente o uso de `ELECTRICAL BUS VOLTAGE` no sistema modular v2+. |
| **Saída do Alternador** | `ELECTRICAL GENALT BUS VOLTAGE:1`, `AMPS:1`, `LOAD:1` | `0` constante o voo todo | **Observado:** Todas as três colunas registraram 0.<br>**Hipótese:** A documentação oficial do SDK 2024 afirma categoricamente: *"For the modern (v2+) electrical system, use ELECTRICAL BUS AMPS / ELECTRICAL BUS VOLTAGE"*. No sistema v2, o alternador alimenta barramentos elétricos genéricos configurados no `systems.cfg`, inexistindo o conceito fixo de "barramento GENALT" legado. |
| **Carga Total do Sistema** | `ELECTRICAL TOTAL LOAD AMPS` | `25,52 A` a `51,52 A` com variações coerentes | **Observado:** Variação dinâmica imediata conforme acionamento de cargas.<br>**Status:** **Totalmente funcional no MSFS 2024.** |

---

## 3. Matriz de Candidatos por Medida

### 3.1. Aviônicos e Chave Master

No sistema modular v2, a documentação oficial do SDK MSFS 2024 instrui o uso da tensão do barramento de aviônicos (`ELECTRICAL BUS VOLTAGE`) em vez de variáveis legadas de chaveamento:

| Ordem | SimVar | Unidade | Sistema SDK (v2+ / v1.00 / FSX) | Indexação Documentada | Justificativa / Documentação Oficial |
|:---:|---|---|---|---|---|
| **1º** | `ELECTRICAL BUS VOLTAGE:<n>` | `volts` | **v2+** | `Bus name (or index)` (índice global) | **Recomendada pelo SDK para o sistema v2+:** Mede a tensão real presente no barramento de aviônicos. Se os interruptores de aviônicos forem desligados, a tensão cai para 0 V. |
| **2º** | `ELECTRICAL BUS AMPS:<n>` | `amperes` | **v2+** | `Bus name (or index)` (índice global) | Corrente consumida no barramento de aviônicos. Vai a 0 A com aviônicos desligados. |
| **3º** | `CIRCUIT ON:<n>` | `bool` | **v2+** / v1.00 | `Circuit name (or index)` (índice global) | Informa se o circuito específico está energizado e funcionando (`True`/`False`). Não disponível no FSX. |
| **Fallback** | `AVIONICS MASTER SWITCH` | `bool` | v2+ / v1.00 / FSX | `Circuit name or index` (v2+) / `Circuit index` (v1/FSX) | Chave master de aviônicos. No v2+ aceita nome ou índice de circuito de aviônicos; sem índice ou com índice genérico não reflete a arquitetura de múltiplos barramentos do G1000. |
| **Legada** | `ELECTRICAL AVIONICS BUS VOLTAGE` | `volts` | FSX apenas | N/A | **Legada:** A documentação declara: *"This is a legacy SimVar, only used by the FSX electrical system. For the v2+ system, use ELECTRICAL BUS VOLTAGE"*. |
| **Legada** | `ELECTRICAL AVIONICS BUS AMPS` | `amperes` | FSX apenas | N/A | **Legada:** Mesma restrição; apenas FSX. |

---

### 3.2. Bateria (Tensão, Carga e Descarga)

| Ordem | SimVar | Unidade | Sistema SDK (v2+ / v1.00 / FSX) | Indexação Documentada | Justificativa / Documentação Oficial |
|:---:|---|---|---|---|---|
| **1º** | `ELECTRICAL BATTERY LOAD:1` | `amperes` | **v2+** / v1.00 / FSX | `Battery name (or index)` (v2+) / `:index` (v1/FSX) | **Comportamento no v2+:** O SDK documenta: *"this is the load drawn from the Battery when it is used as a supplier, and will not return the supplied tension when charging, or when the battery is not a supplier"*. Portanto, com motor parado ou alternador desligado, valores positivos indicam a corrente exata de descarga da bateria. |
| **2º** | `ELECTRICAL BATTERY VOLTAGE:1` | `volts` | **v2+** / v1.00 / FSX | `Battery name (or index)` (v2+) / `:index` (v1/FSX) | **Comportamento no v2+:** Tensão nos terminais da bateria quando ela é fornecedora. O SDK documenta que se a bateria não for fornecedora (ex.: alternador alimentando o sistema), a variável reporta 0 V. |
| **3º** | `ELECTRICAL BUS VOLTAGE:<n>` | `volts` | **v2+** | `Bus name (or index)` (índice global) | Tensão no barramento da bateria (Battery Bus). Permite acompanhar a tensão contínua do sistema (~24 V com bateria, ~28 V em recarga pelo alternador). |
| **Fallback** | `ELECTRICAL BATTERY BUS VOLTAGE` | `volts` | v1.00+ / FSX | `Battery index` (v1.00) / N/A (FSX) | **Legada:** O SDK declara: *"This is a legacy SimVar, only used by the FSX and v1.00 electrical systems. For the v2+ system (and preferrably for the v1.00 system as well), use ELECTRICAL BUS VOLTAGE"*. |
| **Obsoleta** | `ELECTRICAL HOT BATTERY BUS VOLTAGE` | `volts` | FSX apenas | `Battery index` | **Obsoleta:** O SDK declara: *"This is only valid for the FSX electrical system"*. Incompatível com o sistema v2+. |

---

### 3.3. Alternador e Barramentos Principais (Main Bus)

| Ordem | SimVar | Unidade | Sistema SDK (v2+ / v1.00 / FSX) | Indexação Documentada | Justificativa / Documentação Oficial |
|:---:|---|---|---|---|---|
| **1º** | `ELECTRICAL BUS VOLTAGE:<n>` | `volts` | **v2+** | `Bus name (or index)` (índice global) | **Padrão v2+:** Tensão dos barramentos principais. Varia de ~24 V (bateria) para ~28 V quando o alternador está ativo. |
| **2º** | `ELECTRICAL BUS AMPS:<n>` | `amperes` | **v2+** | `Bus name (or index)` (índice global) | Carga distribuída através do barramento indexado. |
| **3º** | `CIRCUIT ON:<n>` | `bool` | **v2+** / v1.00 | `Circuit name (or index)` (índice global) | Estado de funcionamento do circuito indexado. |
| **Controle** | `ELECTRICAL MAIN BUS VOLTAGE:1` | `volts` | v1.00 / FSX | `Bus index` (v1.00) / N/A (FSX) | **Legada:** O SDK orienta o uso de `ELECTRICAL BUS VOLTAGE`. Mantida como datum único de controle/conferência para testar resposta em aviões legados. |
| **Legada** | `ELECTRICAL MAIN BUS AMPS:1` | `amperes` | v1.00 / FSX | `Bus index` (v1.00) / N/A (FSX) | **Legada:** Substituída por `ELECTRICAL BUS AMPS`. |
| **Não suportada** | `ALTERNATOR CONNECTION ON:1` | `bool` | v2+ / v1.00 | `Generator name` (v2+) / `Alternator index` + `Bus index` (v1.00) | **Formato inválido com `:1`:** No v2+ requer o nome do gerador (string); no v1.00 requer dois parâmetros (`Alternator index` e `Bus index`). O índice numérico único `:1` não é documentado e não funciona. |
| **Motor** | `GENERAL ENG GENERATOR ACTIVE:1` | `bool` | v2+ / v1.00 / FSX | `:index` (motor 1) | Status do alternador na tabela de engine variables. |

---

### 3.4. Carga Total do Sistema (Total Load)

| SimVar | Unidade | Sistema SDK | Indexação | Justificativa / Documentação Oficial |
|---|---|---|---|---|
| `ELECTRICAL TOTAL LOAD AMPS` | `amperes` | v2+ / v1.00 / FSX | Não indexado | **Validada no C172:** Mede a corrente instantânea agregada de todo o sistema elétrico. Respondeu com alta fidelidade no voo de validação (25,5 A a 51,5 A). |

---

## 3.5. Análise de Indexação no SimConnect: Nome vs Índice Numérico

A documentação geral de Simulation Variables do MSFS 2024 (`simulation-variables/#using-names-instead-of-indices`) introduz a seguinte convenção para referenciar componentes por nome no sistema modular:
```text
<SIMVAR>:'<NAME>'_n
Exemplo em RPN: (A:HYDRAULIC VALVE ACTIVE:'VALVE_GEAR_LEFT_1'_n, Bool)
```

No entanto, ao analisar a documentação da API C/C++ do SimConnect para a função `SimConnect_AddToDataDefinition` (`simconnect_addtodatadefinition/`), verifica-se:
1. O parâmetro `DatumName` é documentado exclusivamente com suporte a nomes de SimVar seguidos opcionalmente de índice numérico após dois pontos:
   > *"If an index is required then it should be appended to the variable name following a colon, see the example for DEFINITION_2 below. Indexes are numbered from 1 (not zero)."*
2. Em nenhum ponto da documentação da API SimConnect há especificação ou exemplo de suporte à sintaxe de nomes entre aspas simples (`:'Bus_1'_n`).
3. Conforme alertado na seção 6 de `AGENTS.md`, requisições com strings não reconhecidas geram a exceção `NAME_UNRECOGNIZED` no SimConnect, o que encurta o pacote binário de dados e corrompe o alinhamento de memória de todos os campos subsequentes na struct.

**Conclusão:** O formato por nome é **não documentado** na API SimConnect. Por rigor técnico e segurança contra corrupção de telemetria, **devem ser utilizados estritamente índices numéricos globais (`:1`, `:2`, etc.)**.

---

## 3.6. Metodologia de Descoberta dos Índices de Barramento no Voo de Teste

Como o sistema elétrico v2 indexa os barramentos de forma global (`Bus 1`, `Bus 2`, ... no `systems.cfg` da aeronave), é necessário mapear qual índice corresponde a cada função física no C172SP G1000. O roteiro de teste em solo (seção 7 de `ROTEIRO.md`) foi desenhado com ações isoladas no tempo para identificar essa correspondência:

1. **Passo Bateria ON com Aviônicos ON (`teste-bateria-avionics-on`):**
   - Os barramentos energizados pela bateria apresentarão tensão de repouso (~24 V a 25 V).
   - Os barramentos de aviônicos e barramentos com carga ativa apresentarão amperagem positiva em `elec-bus-amps-<n>`.
2. **Passo Aviônicos OFF (`teste-avionics-off`):**
   - Ao desligar os switches de aviônicos no cockpit, a tensão cai para 0,0 V e a corrente cessa (0,0 A).
   - **Identificação:** O barramento cujo `elec-bus-voltage-<n>` cair a 0 V e `elec-bus-amps-<n>` zerar durante esta janela temporal exata é o **Barramento de Aviônicos** (Avionics Bus).
3. **Passo Partida e Alternador ON (`teste-partida`):**
   - A rotação do motor aciona o alternador. A tensão nos barramentos alimentados sobe para ~27,5 V a 28,5 V.
4. **Passo Alternador OFF (`teste-alternador-off`):**
   - Ao desligar a chave `ALT`, cessa a geração. A tensão cai de ~28 V de volta para a tensão de descarga da bateria (~24 V).
   - **Identificação:** Os barramentos que transitam de 28 V para 24 V são os **Barramentos Principais** (Main Bus 1 / Main Bus 2).
5. **Passo Alternador ON (`teste-alternador-on`):**
   - O retorno imediato para ~28 V e pico temporário de recarga confirmam o vínculo com o alternador.

Esse cruzamento entre os marcadores de evento e as colunas varridas (`elec-bus-voltage-1..5` e `elec-bus-amps-1..5`) permite mapear a topologia elétrica exata do C172SP G1000.

---

## 4. Recomendações Atualizadas para as Três Regras Mudas

### 4.1. `avionics_start_with_avionics_master_on`
- **Diagnóstico atual:** A regra monitora `elec-avionics-master-switch`, `eng-combustion` e `eng-starter-active`. Como a SimVar legada permaneceu em `1`, a regra gera falso-positivo ou falha de detecção.
- **Recomendação v2+:**  
  Substituir a checagem pela presença real de tensão e consumo no barramento de aviônicos (cujo índice global $k$ será determinado no teste do C172):
  $$\text{Aviônicos Ativos} \iff (\text{ELECTRICAL BUS VOLTAGE:}k > 5.0\text{ V}) \lor (\text{ELECTRICAL BUS AMPS:}k > 0.5\text{ A})$$
  Se durante o engrazamento do starter (`eng-starter-active == 1`) a tensão no barramento de aviônicos for superior a 5 V, o surto de partida atinge os aviônicos, caracterizando a infração.

### 4.2. `battery_deep_discharge_damage`
- **Diagnóstico atual:** Monitora `elec-battery-voltage` e `elec-battery-load`, que permaneceram estáticas por ausência de índice.
- **Recomendação v2+:**  
  No sistema v2+, a documentação estabelece que `ELECTRICAL BATTERY LOAD:1` reporta a corrente extraída da bateria quando ela atua como fornecedora. Combinando com a tensão de barramento ou tensão da bateria:
  $$\text{Descarga Profunda} \iff (\text{ELECTRICAL BATTERY LOAD:1} > 2.0\text{ A}) \land (\text{ELECTRICAL BUS VOLTAGE:battery} < 23.0\text{ V})$$
  Quando o alternador está ativo, a bateria não é fornecedora (`LOAD:1 == 0` ou não fornece corrente) e a tensão fica em ~28 V. Com o alternador desligado e aviônicos/luzes ligados, `ELECTRICAL BATTERY LOAD:1` torna-se fortemente positiva e a tensão do barramento sofre decaimento progressivo.

### 4.3. `electrical_system_overload_amps`
- **Diagnóstico atual:** Monitora `elec-total-load-amps` e `elec-genalt-load`. O `elec-genalt-load` permaneceu em 0.
- **Recomendação v2+:**  
  Como `ELECTRICAL TOTAL LOAD AMPS` funciona com alta precisão no MSFS 2024, a sobrecarga deve ser avaliada diretamente comparando a corrente total consumida com a capacidade nominal de projeto do alternador da aeronave (`RatedAlternatorAmps`, extraída do catálogo de limites/POH, ex.: 60 A no C172):
  $$\text{Sobrecarga} \iff \text{ELECTRICAL TOTAL LOAD AMPS} > \text{RatedAlternatorAmps} \times 1.10$$
  Isso elimina a dependência de curvas internas de alternador no arquivo `.cfg` de terceiros.

---

## 5. Riscos e Compatibilidade

1. **Aeronaves Modernas (v2+) vs Legadas (v1.00 / FSX):**
   - Aeronaves nativas do MSFS 2024 utilizam a arquitetura modular v2, exigindo `ELECTRICAL BUS VOLTAGE:<n>` e `ELECTRICAL BUS AMPS:<n>`.
   - Aeronaves legadas (MSFS 2020 / FSX) mapeiam os barramentos para o Main Bus clássico. A inclusão de `ELECTRICAL MAIN BUS VOLTAGE:1` no registro serve de controle e fallback para modelos antigos.
2. **Aeronaves Complexas de Terceiros (WASM / Custom):**
   - Aeronaves de alto detalhamento (Fenix, PMDG, FlyByWire) implementam simulação elétrica em código C++/WASM próprio, mas sincronizam barramentos elétricos no simulador para alimentar gauges e displays. A leitura de `ELECTRICAL BUS VOLTAGE` nos barramentos 1 a 5 cobre tanto a arquitetura padrão quanto customizada.
3. **Orçamento de Novos Datums:**
   - Foram adicionados exatamente 16 novos datums escalares com `perEngine: false`, mantendo o alinhamento estrito dos blocos de 256 posições do `SensorFrameReader` e garantindo paridade com `sensor-inventory.md`.
