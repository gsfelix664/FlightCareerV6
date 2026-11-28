# Spike — Falhar o Sistema sem Mexer no Comando do Piloto (MSFS 2024)

**Estado (conferido em 2026-11-25):** pesquisa concluída; alternador e flap tratados na F4.2e (2026-11-15). O texto descreve o addon antes dela. Estado atual: `docs/sistemas/desgaste-falhas.md`.

**Data:** 2026-11-15  
**Autor:** Gemini (Executor) — Revisão: Claude  
**Objetivo:** Descobrir, na documentação oficial do SDK do MSFS 2024, como aplicar cada pane do addon sem mover chaves ou alavancas no painel (chave do alternador permanece ON, alavanca do flap se move, mas o sistema não responde) e entregar a análise comparativa, a recomendação técnica por pane e um roteiro prático de teste em voo (C152).

---

## 1. O Problema Atual (Estado do Addon)

Hoje o addon aplica três panes forçando comandos ou chaves no cockpit:
- **Alternador (`DefectCatalog.Alternator`):** Dispara `ALTERNATOR_OFF` + mascara eventos de ligar do piloto (`ALTERNATOR_ON`, `ALTERNATOR_SET`, etc.). No cockpit, a chave física aparece desligada (OFF).
- **Flaps travados (`DefectCatalog.FlapLock` / `FlapDamage`):** Escreve `FLAPS HANDLE INDEX` + mascara eventos `FLAPS_*` do piloto via grupo de notificação mascarável. No cockpit, a alavanca do flap fica fisicamente travada na posição da pane.
- **Magnetos (`DefectCatalog.MagnetoLeft` / `MagnetoRight`):** Escreve `RECIP ENG LEFT/RIGHT MAGNETO:1` para 0 (ou dispara `MAGNETO1_RIGHT` / `MAGNETO1_LEFT`). No cockpit, a chave de ignição pula visualmente de posição (de BOTH para R ou L).

As demais panes com efeito no sim (`ENGINE_FAILURE`, `ENGINE_FIRE`, `OIL_LEAK`, `VACUUM`, `PITOT`, `STATIC_PORT`, `ELECTRICAL`, `BRAKE_FAILURE`, `HYDRAULIC`) já utilizam eventos nativos do SDK (`TOGGLE_*_FAILURE`) ou SimVars físicas diretas (`ENG ON FIRE:1`, `GENERAL ENG OIL LEAKED PERCENT:1`) e não manipulam manetes nem chaves do piloto.

---

## 2. Pesquisa Técnica no SDK Oficial do MSFS 2024

### 2.1 Flaps: Superfície vs. Alavanca e Danos por Velocidade

#### Perguntas Investigadas:
1. *As SimVars de superfície (`TRAILING EDGE FLAPS LEFT/RIGHT PERCENT`, `ANGLE`) são graváveis ("Settable")?*
2. *Existe key event ou SimVar de flap "jammed" / "failed" (ex.: `FLAP DAMAGE BY SPEED`, `FLAPS AVAILABLE`)?*
3. *Dá para manter a superfície parada com a alavanca livre?*

#### Achados na Documentação Oficial:
- **`TRAILING EDGE FLAPS LEFT PERCENT` / `TRAILING EDGE FLAPS RIGHT PERCENT`:**
  - **Tipo / Unidade:** `Percent Over 100` (valores de `0.0` a `1.0`).
  - **Settable:** **Y** (Sim, gravável via SimConnect).
  - A própria documentação de `TRAILING EDGE FLAPS LEFT ANGLE` instrui: *"Use TRAILING_EDGE_FLAPS_LEFT_PERCENT to set a value"*.
  - **URL:** [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Control_Variables.htm)
- **`FLAP DAMAGE BY SPEED`:**
  - **Tipo / Unidade:** `Bool`.
  - **Settable:** **N** (Read-Only). Retorna `True` quando a aeronave excede a velocidade estrutural dos flaps e o simulador registra dano. Não pode ser forçada externamente via SimConnect.
  - **URL:** [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Control_Variables.htm)
- **`FLAPS AVAILABLE`:**
  - **Tipo / Unidade:** `Bool`.
  - **Settable:** **N** (Read-Only). Informa apenas se o modelo de voo possui flaps configurados.
  - **URL:** [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Control_Variables.htm)
- **Key Events de Flap:**
  - Não existem eventos como `TOGGLE_FLAP_FAILURE` ou `FLAPS_JAMMED`. Apenas eventos de acionamento (`FLAPS_SET`, `FLAPS_UP`, `FLAPS_DOWN`, `FLAPS_INCR`, `FLAPS_DECR`, `AXIS_FLAPS_SET`).
  - **URL:** [Aircraft Flight Control Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Flight_Control_Events.htm)
- **Circuito elétrico do motor de flaps (`CIRCUIT_FLAP_MOTOR`):**
  - No `systems.cfg` (`[ELECTRICAL]`), aeronaves com acionamento elétrico de flap (como o Cessna 152) possuem um circuito do tipo `CIRCUIT_FLAP_MOTOR`.
  - Se o disjuntor do flap abrir ou o circuito for desativado via evento elétrico (`ELECTRICAL_CIRCUIT_TOGGLE` ou `ELECTRICAL_CIRCUIT_BREAKER_TOGGLE`), o motor elétrico não recebe alimentação.
  - **URL:** [systems.cfg - Electrical](https://docs.flightsimulator.com/msfs2024/retail/Content_Configuration/SimObjects/Aircraft_SimObjects/Systems_Config/Electrical_System.htm)

#### Viabilidade de manter a superfície parada com alavanca livre:
- **Abordagem A (Forçar `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT`):** Se a alavanca for deixada livre para o piloto mover (`FLAPS HANDLE INDEX` muda), a cada frame o motor interno de física de voo (`flight_model.cfg`) recalcula o deslocamento da superfície em direção à alavanca. Se o SimConnect escrever o percentual fixo a cada frame/tick, ocorre uma disputa de sobrescrita entre o solver interno do sim e a API externa. Conforme documentado no DevSupport, isso gera instabilidade aerodinâmica e trepidação visual (*jitter*) nas superfícies 3D.
- **Abordagem B (Cortar o circuito `CIRCUIT_FLAP_MOTOR`):** Em aviões com flaps puramente elétricos ligados ao sistema elétrico moderno do MSFS (v2), desligar o circuito do motor do flap faz com que a alavanca se mova no painel, mas o motor não acione as superfícies. Porém, isso possui duas limitações severas:
  1. Só mantém a posição travada se a pane ocorrer exatamente quando o flap atingir aquela posição em voo; se a aeronave for recarregada ou reiniciar o voo, ela inicializa com flaps a 0° e o motor cortado a deixará presa em 0°, sem como restaurar a posição travada intermediária (ex.: 10° ou 20°).
  2. Não se aplica a aeronaves com flaps manuais por alavanca de catraca mecânica (ex.: Piper Cherokee) ou sistemas puramente hidráulicos de aeronaves complexas.

---

### 2.2 Alternador e Sistema Elétrico: Disjuntores e Desconexão de Barramento

#### Perguntas Investigadas:
1. *Existem key events para abrir disjuntor ou circuito (ex.: `ELECTRICAL_CIRCUIT_TOGGLE`, `BREAKER_*`, `ELECTRICAL_BUS_TO_*`)?*
2. *Existem SimVars para ler/forçar (`CIRCUIT SWITCH ON:n`, `ELECTRICAL GENALT ...:n`, `CIRCUIT ...`)?*
3. *Qual faria o alternador parar de gerar (amperagem 0, bateria descarregando) com a chave física ON?*
4. *Funciona no C152 / Asobo? Como descobrir o índice por SimConnect?*

#### Achados na Documentação Oficial:
- **`ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`:**
  - **Parâmetros:** Requer 2 argumentos: `[0] = Source bus index` (ex.: 1), `[1] = Target alternator index` (ex.: 1).
  - **Descrição:** Alterna a conexão elétrica entre o barramento especificado e o alternador especificado.
  - **URL:** [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Electrical_Events.htm)
- **`ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE`:**
  - **Parâmetros:** Requer 2 argumentos: `[0] = Source bus index`, `[1] = Target alternator index`.
  - **Descrição:** Alterna o estado do disjuntor do alternador ligado ao barramento.
  - **URL:** [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Electrical_Events.htm)
- **`ELECTRICAL_CIRCUIT_TOGGLE`:**
  - **Parâmetros:** 1 argumento: `[0] = Circuit index`.
  - **Descrição:** Alterna o estado de ativação do circuito especificado no `systems.cfg`.
  - **URL:** [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Electrical_Events.htm)
- **`ELECTRICAL_CIRCUIT_BREAKER_TOGGLE`:**
  - **Parâmetros:** 2 argumentos: `[0] = Source bus index`, `[1] = Target circuit index`.
  - **Descrição:** Alterna o disjuntor do circuito ligado ao barramento.
  - **URL:** [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Electrical_Events.htm)
- **SimVars de monitoramento do Alternador:**
  - **`ELECTRICAL GENALT BUS AMPS:index`** (Amperes): Corrente fornecida pelo alternador ao barramento. Com a conexão aberta, vai a `0.0`.
  - **`ELECTRICAL GENALT LOAD:index`** (Percent): Percentual de carga consumida do alternador. Vai a `0.0`.
  - **`ELECTRICAL BATTERY BUS AMPS`** (Amperes): Amperagem da bateria. Quando o alternador para de gerar com equipamentos ligados, a corrente fica negativa (bateria descarregando).
  - **`ALTERNATOR CONNECTION ON:index`** (Bool): Requer `BUS LOOKUP INDEX` para leitura via RPN/WASM.
  - **`CIRCUIT SWITCH ON:index`** (Bool): Status da chave do circuito (Read-Only).
  - **URL:** [Aircraft Electrical Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Electrical_Variables.htm)
- **Estrutura no C152 / `systems.cfg` Asobo:**
  - No C152, o alternador é `alternator.1`, ligado a `bus.1`.
  - No modelo elétrico do C152, o alternador é uma fonte de potência (`[ELECTRICAL]` › `alternator.1 = Connections:bus.1...`). Não é uma carga indexada em `circuit.N`.
  - Se a conexão entre `bus.1` e `alternator.1` for desligada via `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`:
    1. A chave **Master Alternator** no cockpit permanece física e visualmente ligada em **ON** (`GENERAL ENG MASTER ALTERNATOR:1` permanece `1.0`).
    2. A amperagem do alternador cai para **0 A**.
    3. O amperímetro do painel (que mede a corrente da bateria) deflete para a esquerda (descarga contínua da bateria).
    4. A bateria se esgota gradualmente até apagar aviônicos e instrumentos elétricos.
- **Questão técnica do SimConnect (2 parâmetros):**
  - A chamada padrão `SimConnect_TransmitClientEvent` (usada pelo wrapper clássico gerenciado de .NET) aceita apenas um inteiro `DWORD dwData`.
  - Para eventos do SDK que exigem múltiplos parâmetros (como `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` que exige `bus_index` e `alt_index`), o SDK oficial do MSFS 2024 disponibiliza a API **`SimConnect_TransmitClientEvent_EX1`**, que aceita até 5 parâmetros (`dwData0` a `dwData4`).
  - No C152 e na maioria dos monomotores da aviação geral, os índices são `bus = 1` e `alt = 1`.

---

### 2.3 Magnetos: O Dilema da Animação do Cockpit

#### Perguntas Investigadas:
1. *Existe alguma forma de "magneto morto" sem mexer na chave?*
2. *Existe falha nativa por índice ou SimVar de magneto "failed"?*
3. *Existe circuito elétrico de magneto no `systems.cfg`?*

#### Achados na Documentação Oficial:
- **`RECIP ENG LEFT MAGNETO:index` / `RECIP ENG RIGHT MAGNETO:index`:**
  - **Tipo / Unidade:** `Bool`.
  - **Settable:** **Y** (Gravável via SimConnect).
  - **URL:** [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Engine_Variables.htm)
- **Key Events de Magneto:**
  - `MAGNETO1_OFF`, `MAGNETO1_RIGHT`, `MAGNETO1_LEFT`, `MAGNETO1_BOTH`, `MAGNETO1_START`, `MAGNETO1_SET`.
  - **URL:** [Aircraft Engine Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Engine_Events.htm)
- **Não existe variável de pane para magnetos:**
  - Não existe `RECIP ENG MAGNETO FAILED`, `MAGNETO DAMAGE` ou `TOGGLE_MAGNETO_FAILURE`.
  - Magnetos são geradores autônomos de ignição acionados mecanicamente pelo motor. Por definição aeronáutica e no MSFS, eles **não pertencem** à rede de barramentos do `systems.cfg` (`[ELECTRICAL]`); logo, não possuem circuitos ou disjuntores para trip.
- **Acoplamento rígido no Modelo 3D / Behaviors do Cockpit:**
  - No C152 e em aviões convencionais do simulador, a animação da chave de ignição é definida em XML (Model Behaviors) como uma leitura combinada direta de `(A:RECIP ENG LEFT MAGNETO:1, Bool)` e `(A:RECIP ENG RIGHT MAGNETO:1, Bool)`:
    - Se L=1 e R=1 ➔ posição 3 (BOTH).
    - Se L=0 e R=1 ➔ posição 2 (R).
    - Se L=1 e R=0 ➔ posição 1 (L).
    - Se L=0 e R=0 ➔ posição 0 (OFF).
  - **Conclusão:** É **fisicamente impossível** no modelo do MSFS 2024 desligar a centelha de um magneto via SimConnect sem que a chave visual pule para a posição do magneto sobrevivente. Qualquer tentativa externa de forçar a chave em BOTH enquanto um magneto gera faísca nula não encontra suporte nas SimVars de motor do simulador.

---

### 2.4 Sistema de Failures do MSFS 2024: O Que Mudou no SDK 2024?

#### Perguntas Investigadas:
1. *O SDK 2024 tem API ou sistema novo de falhas programáveis (Failure Manager, `FAILURE_*` events)?*
2. *O que dá para disparar por SimConnect e o que é restrito ao menu interno / `.FLT`?*
3. *Os componentes do Wear and Tear nativo são escrevíveis via SimConnect?*

#### Achados na Documentação Oficial:
- **Ausência de nova API de Failure Manager no SimConnect:**
  - Não existe no MSFS 2024 uma nova interface SimConnect dedicada a agendamento de falhas ou injeção de panes.
  - O agendamento nativo do simulador (falha após tempo/altitude) é configurado estaticamente em arquivos de voo (`.FLT`, seções `[SystemFailureN.i]`) ou via UI do menu de desenvolvedor.
- **SimVars de Wear and Tear Nativo (`WEAR AND TEAR *`):**
  - Variáveis como `WEAR AND TEAR LEVEL:<id>`, `WEAR AND TEAR IS FAILED:<id>`, `WEAR AND TEAR IS WEAK:<id>` e `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` são **estritamente Read-Only** (`Settable: N`).
  - O addon não pode escrever nelas para simular que uma peça nativa quebrou.
  - **URL:** [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_System_Variables.htm)
- **Eventos de Pane suportados por SimConnect:**
  - O catálogo de falhas acionáveis por SimConnect permanece o conjunto legado de Key Events do tipo toggle:
    - `TOGGLE_ENGINE1..4_FAILURE`
    - `TOGGLE_ELECTRICAL_FAILURE`
    - `TOGGLE_VACUUM_FAILURE`
    - `TOGGLE_PITOT_BLOCKAGE`
    - `TOGGLE_STATIC_PORT_BLOCKAGE`
    - `TOGGLE_HYDRAULIC_FAILURE`
    - `TOGGLE_LEFT_BRAKE_FAILURE`, `TOGGLE_RIGHT_BRAKE_FAILURE`, `TOGGLE_TOTAL_BRAKE_FAILURE`
    - SimVars graváveis de efeito direto: `ENG ON FIRE:1` e `GENERAL ENG OIL LEAKED PERCENT:1`.
  - **URL:** [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm)

---

## 3. Matriz Comparativa por Pane do DefectCatalog

Tabela consolidada com todas as panes do `DefectCatalog` que possuem efeito ou enforcement no simulador:

| Código da Pane (`DefectCatalog`) | Como é Hoje no Addon | Opção sem Mexer no Comando | Documentação Oficial (URL) | Limitações Técnicas | Grau de Confiança |
|---|---|---|---|---|---|
| **`ALTERNATOR`** | `ALTERNATOR_OFF` + mascara comandos do piloto. Chave fica OFF. | `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` (bus: 1, alt: 1) ou `ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE`. | [Aircraft Electrical Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Electrical_Events.htm) | Exige SimConnect com múltiplos parâmetros (`_EX1`). Conexão restaura se a aeronave recarregar (necessário vigia após reload). Funciona se o avião usar `[ELECTRICAL]` modular. | **Documentado Oficialmente** (comprovado no SDK) |
| **`FLAP_LOCK` / `FLAP_DAMAGE`** | Escreve `FLAPS HANDLE INDEX` + mascara eventos de alavanca. Alavanca fica travada. | **Opção 1:** Escrever continuamente `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT`.<br>**Opção 2:** Cortar `CIRCUIT_FLAP_MOTOR` via `ELECTRICAL_CIRCUIT_TOGGLE`. | [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Control_Variables.htm)<br>[Electrical System cfg](https://docs.flightsimulator.com/msfs2024/retail/Content_Configuration/SimObjects/Aircraft_SimObjects/Systems_Config/Electrical_System.htm) | **Opção 1:** Briga com o flight model interno, causando trepidação (*jitter*) visual e instabilidade aerodinâmica.<br>**Opção 2:** Não permite manter o flap travado em posições intermediárias após recarregar aeronave; inútil em flaps manuais ou hidráulicos. | **Opção 1: Documentado** (mas com limitação conhecida de jitter em fóruns).<br>**Opção 2: Documentado** (viabilidade restrita). |
| **`MAGNETO_LEFT` / `MAGNETO_RIGHT`** | Força `RECIP ENG LEFT/RIGHT MAGNETO:1` para 0. Chave pula para R ou L. | **Nenhuma opção viável.** | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Engine_Variables.htm) | O modelo 3D do cockpit amarra a chave visual diretamente às SimVars de magneto. Não há SimVar de pane independente nem circuito elétrico de magneto. | **Não Encontrado / Inexistente no SDK** |
| **`ENGINE_FAILURE`** | `TOGGLE_ENGINE1_FAILURE`. Motor apaga. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Nenhuma. Manete de mistura e throttle permanecem onde o piloto as deixou. | **Documentado Oficialmente** |
| **`ENGINE_FIRE`** | Escreve `ENG ON FIRE:1`. Chamas visuais no motor. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Nenhuma. | **Documentado Oficialmente** |
| **`OIL_LEAK`** | Escreve `GENERAL ENG OIL LEAKED PERCENT:1`. Pressão cai progressivamente. | Já é a opção ideal sem mexer no comando. | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/SimVars/Aircraft_SimVars/Aircraft_Engine_Variables.htm) | Nenhuma. | **Documentado Oficialmente** |
| **`VACUUM`** | `TOGGLE_VACUUM_FAILURE`. Sucção cai a zero. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Não há alavanca de vácuo; instrumentos giroscópicos tombam. | **Documentado Oficialmente** |
| **`PITOT`** | `TOGGLE_PITOT_BLOCKAGE`. Velocímetro trava/comporta-se como altímetro. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Não altera o switch do aquecimento de pitot. | **Documentado Oficialmente** |
| **`STATIC_PORT`** | `TOGGLE_STATIC_PORT_BLOCKAGE`. Altímetro e VSI congelam. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Não altera comandos. | **Documentado Oficialmente** |
| **`ELECTRICAL`** | `TOGGLE_ELECTRICAL_FAILURE`. Sistema elétrico apaga. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Desliga o barramento principal sem mexer na chave master da bateria. | **Documentado Oficialmente** |
| **`BRAKE_FAILURE`** | `TOGGLE_LEFT/RIGHT/TOTAL_BRAKE_FAILURE`. Frenagem falha. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Pedais de freio continuam se movendo; apenas o efeito na roda cessa. | **Documentado Oficialmente** |
| **`HYDRAULIC`** | `TOGGLE_HYDRAULIC_FAILURE`. Pressão hidráulica cai. | Já é a opção ideal sem mexer no comando. | [Aircraft Miscellaneous Events](https://docs.flightsimulator.com/msfs2024/retail/Programming_APIs/Event_IDs/Aircraft_Miscellaneous_Events.htm) | Afeta apenas aeronaves com hidráulica ativa configurada. | **Documentado Oficialmente** |

---

## 4. Recomendações Técnicas

1. **Alternador (Candidato Primário a Mudança Futura):**
   - **Recomendação:** Migrar de `ALTERNATOR_OFF` para `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`.
   - **Ganhos:** Experiência de voo realista: a chave do alternador permanece ligada em ON, o alternador gera 0 Amps, o amperímetro mostra descarga imediata da bateria, e a pane exige que o piloto perceba a falha pelos instrumentos do painel.
   - **Pré-requisito:** Implementar suporte a chamadas de múltiplos parâmetros (`SimConnect_TransmitClientEvent_EX1`) no `SimConnectService`.

2. **Flaps (Recomendação: Manter Implementação Atual):**
   - **Recomendação:** **Manter o comportamento atual** (alavanca travada na posição da pane + correção espaçada com política `FlapEnforcementPolicy`).
   - **Justificativa:** Forçar `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT` enquanto a alavanca do piloto se move gera conflito direto com o motor de física do MSFS e resulta em trepidação gráfica das superfícies. Já o corte de circuito de motor de flap (`CIRCUIT_FLAP_MOTOR`) é inconsistente após recarregar a aeronave e não funciona em flaps não elétricos. Além disso, a decisão recente do projeto (CHANGELOG 2026-11-15) optou por manter o som do flap como indicativo sonoro perceptível de pane ao piloto.

3. **Magnetos (Recomendação: Manter Implementação Atual):**
   - **Recomendação:** **Manter o comportamento atual** (`WriteMagneto` forçando 0 no magneto danificado).
   - **Justificativa:** Não existe SimVar ou evento desacoplado da animação 3D da chave no MSFS 2024. A chave pulando para a posição do magneto sobrevivente é uma limitação estrutural da engine do simulador.

4. **Demais Panes:**
   - Já operam 100% livres de intervenção nos comandos do piloto, preservando a imersão.

---

## 5. Roteiro de Teste em Voo (Cessna 152 no MSFS 2024)

Roteiro de verificação para o usuário validar as alternativas no C152 (motor ligado no solo ou em voo reto nivelado a 2.500 ft):

### Teste 1: Alternador Desconectado vs. Chave ON
- **Objetivo:** Confirmar se desconectar o alternador mantém a chave em ON e descarrega a bateria.
- **Preparação:**
  - C152 com motor em 2.000 RPM.
  - Chaves de Bateria e Alternador ligadas (ON). Luzes e rádio ligados para gerar consumo.
  - Amperímetro no centro (neutro/ligeira carga).
- **Ação:**
  - Disparar o evento `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` com parâmetros `bus = 1` e `alternator = 1`.
- **O que observar no cockpit:**
  - [ ] A chave vermelha **Master Alternator** permanece física e visualmente na posição **LIGADA (ON)**.
  - [ ] O ponteiro do **amperímetro** do C152 deflete imediatamente para a **esquerda** (indicando que a bateria está fornecendo toda a corrente).
  - [ ] Na telemetria do SimConnect: `ELECTRICAL GENALT BUS AMPS:1` cai para `0.0` e `ELECTRICAL BATTERY BUS AMPS` fica negativo.
  - [ ] Após alguns minutos com rádio/luzes ligados, a tensão cai progressivamente até os aviônicos apagarem.

### Teste 2: Escrita Direta em Trailing Edge Flaps
- **Objetivo:** Avaliar o comportamento visual e aerodinâmico ao forçar percentual de flap com alavanca em posição diferente.
- **Preparação:**
  - C152 em voo nivelado a 75 kt com flaps recolhidos (0°). Alavanca em UP.
- **Ação:**
  - Enviar continuamente via SimConnect escrita de `TRAILING EDGE FLAPS LEFT PERCENT` = `0.5` (50% de extensão) e `TRAILING EDGE FLAPS RIGHT PERCENT` = `0.5`, sem enviar `FLAPS_SET` e sem mascarar os comandos do piloto.
  - Mover a alavanca do flap no cockpit para a posição 20° ou 30°.
- **O que observar no cockpit e na visão externa:**
  - [ ] Na visão externa: verificar se há trepidação (*jitter*) visual nas superfícies das asas enquanto o simulador tenta estender para 30° e o SimConnect tenta manter 50%.
  - [ ] No painel: a alavanca se move livremente.
  - [ ] Na aerodinâmica: verificar se há oscilação na velocidade vertical ou se o arrasto estabiliza.

### Teste 3: Chave de Magneto (Confirmação do Vínculo Visual)
- **Objetivo:** Confirmar que desligar o magneto força a chave de ignição no cockpit.
- **Preparação:**
  - C152 com motor em 1.800 RPM em solo, chave de ignição em **BOTH**.
- **Ação:**
  - Enviar escrita de `RECIP ENG LEFT MAGNETO:1` = `0.0` (ou evento `MAGNETO1_RIGHT`).
- **O que observar no cockpit:**
  - [ ] O conta-giros (RPM) apresenta queda de aproximadamente 50 a 100 RPM (comportamento correto da perda de um magneto).
  - [ ] A chave física de ignição no painel gira visualmente da posição **BOTH** para **R**.
  - [ ] Tentativa de colocar a chave em BOTH via clique do mouse faz com que ela retorne para R se o SimConnect continuar forçando o magneto esquerdo desligado.
