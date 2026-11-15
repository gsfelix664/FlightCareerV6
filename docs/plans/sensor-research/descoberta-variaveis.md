# Descoberta de Novas Variáveis do MSFS 2024 para o Sistema de Peças e Regras

> **Documento gerado na tarefa RESEARCH-SIMVARS-r3**  
> **Objetivo:** Identificar variáveis **documentadas e verdadeiras** do SDK MSFS 2024 (SimVars `A:`, Key Events, Input Events, Estados de Pré-Voo) que ainda **não** constam nas definições fixas (`SimTelemetry.cs`) nem nos registros do FlightCareer (`sensor_registry.json`, `context_registry.json`), partindo das necessidades físicas de cada peça do `parts_catalog.json`.  
> **Repositórios de Referência Cruzada:** `C:\PROJETOS\FLIGHTCAREERV6-refs\` (`MobiFlight-Connector`, `aircraft` FlyByWire, `FsConnect`, `msfs2020-go`).

---

## 1. Tabela de Descoberta por Grupo de Peças e Regras

| Grupo / Peça | Variável Oficial Nova | Unidade | Índice / Escopo | R / W | SDK MSFS 2024 (URL Oficial) | MSFS 2020? | Evidência em Projeto Aberto | Como a Usaríamos no FlightCareer (Regra / Melhoria) | Valor | Como Confirmar em Teste Curto (Passo Operacional) |
|---|---|---|---|---|---|---|---|---|---|---|
| **Walkaround / Capas** (`fuselage.skin_fairings`, `instruments.pitot_tube`, `instruments.static_ports`) | `COVER ON:0` (Chocks) | `Bool` | `:0` (Calços) | R | [Misc Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) | Não (2024 nativo) | `aircraft\fbw-a380x\...\interactive-parts.xml:558` | Detecta decolagem ou aceleração de motor com calços colocados (dano no trem de nariz e hélice). | **Alto** | Colocar calços no EFB/Walkaround no pátio e acelerar motor. |
| **Walkaround / Capas** (`instruments.pitot_tube`) | `COVER ON:2` (Pitot) | `Bool` | `:2` (Pitot) | R | [Misc Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) | Não (2024 nativo) | SDK Preflight audio / sound.xml | Decolagem com capa de pitot colocada gera indicação zero de velocímetro (pane operacional gravíssima). | **Alto** | Iniciar voo sem retirar capa do pitot na inspeção externa. |
| **Walkaround / Capas** (`instruments.static_ports`) | `COVER ON:3` (Estática) | `Bool` | `:3` (Tomada Estática) | R | [Misc Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) | Não (2024 nativo) | SDK Preflight audio / sound.xml | Decolagem com tomada estática coberta bloqueia altímetro e VSI (altitude congelada). | **Alto** | Iniciar com capa estática e conferir se altímetro congela. |
| **Walkaround / Capas** (`propeller.blades`, `engine.starter`) | `COVER ON:6` (Hélice) | `Bool` | `:6` (Amarração da Hélice) | R | [Misc Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) | Não (2024 nativo) | SDK Preflight audio / sound.xml | Dar partida com a hélice amarrada queima imediatamente o motor de partida e sobrecarrega o virabrequim. | **Alto** | Deixar amarração na hélice e acionar a chave de ignição START. |
| **Pneus / Rodas** (`tires.nose_tire`, `tires.main_left_tire`, `tires.main_right_tire`) | `LEFT WHEEL RPM`, `RIGHT WHEEL RPM`, `CENTER WHEEL RPM` | `RPM` | Escalar por roda | R | [Brake & Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:72` | Mede o spin-up imediato no toque (queima de borracha) e detecta pneu travado arrastando (`RPM == 0` com velocidade de solo > 0 = flat spot severo). | **Alto** | Tocar o solo freando tudo e observar RPM das rodas ir a zero com ground speed alta. |
| **Trem de Pouso** (`landinggear.nose_strut`, `landinggear.main_struts`) | `CONTACT POINT COMPRESSION:0`, `:1`, `:2` | `Percent Over 100` | `:0` nariz, `:1` esq, `:2` dir | R | [Brake & Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | Sim | `aircraft\fbw-a380x\...\A380_Gear_Nose_Behavior.xml:41` | Mede a compressão física independente de cada amortecedor no toque (pouso monoperma ou impacto no trem de nariz gera dano localizado). | **Alto** | Pousar em "three-point" duro ou pousar com asa inclinada tocando só uma roda. |
| **Trem de Pouso** (`landinggear.nose_strut`, `tires.nose_tire`) | `CONTACT POINT IS SKIDDING:0`, `:1`, `:2` | `Bool` | `:0` nariz, `:1` esq, `:2` dir | R | [Brake & Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | Sim | SDK Contact Point Variables | Detecta carga lateral severa e derrapagem em curvas rápidas no solo ou pouso com vento de través sem alinhar a aeronave. | **Alto** | Fazer curva fechada em alta velocidade na pista ou tocar caranguejado. |
| **Freios** (`brakes.discs_linings_left`, `brakes.discs_linings_right`, `brakes.parking_brake_valve`) | `BRAKE PARKING POSITION` | `Bool` | Escalar | R | [Brake & Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:161` | Detecta táxi ou decolagem com freio de estacionamento acionado (superaquecimento e destruição das pastilhas em 60 s). | **Alto** | Puxar freio de mão e acelerar motor acima de 1.800 RPM em rolagem. |
| **Freios** (`brakes.discs_linings_left`, `brakes.discs_linings_right`) | `BRAKE LEFT POSITION`, `BRAKE RIGHT POSITION` | `Percent Over 100` | Escalar por pedal | R | [Brake & Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:159-160` | Calcula a energia de frenagem contínua dissipada por pedal em táxi longo ou rejeição de decolagem. | **Médio** | Pressionar os pedais de freio durante a rolagem de solo. |
| **Instrumentos / Vácuo** (`instruments.vacuum_pump`, `instruments.attitude_indicator`) | `SUCTION PRESSURE` | `Inches of Mercury` (inHg) | Escalar | R | [Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:388` | Mede a pressão real fornecida pela bomba de vácuo mecânica; queda < 4.5 inHg causa desorientação e tombamento do horizonte artificial. | **Alto** | Reduzir para marcha lenta em voo ou desligar motor e observar manômetro de sucção cair a zero. |
| **Óleo** (`oil.lubricant`, `oil.pump`) | `ENG OIL QUANTITY:index` | `Percent Over 100` | `:index` por motor | R | [Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:389` | Acompanha a redução gradual de nível de óleo por queima operacional ou perda acelerada por vazamento de junta. | **Alto** | Simular vazamento de óleo via `SetOilLeakedPercent` e conferir queda do nível. |
| **Hélice** (`propeller.blades`, `propeller.pitch_feathering`) | `PROP BETA:index` | `Degrees` | `:index` por motor | R | [Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:361` | Ângulo real de passo das pás; detecta reversão em voo proibida ou abuso de passo mínimo/máximo com sobretorque. | **Alto** | Mover alavanca de hélice de fina para grossa e reversor em turboélice. |
| **Hélice** (`propeller.pitch_feathering`, `propeller.governor`) | `PROP FEATHERED:index` | `Bool` | `:index` por motor | R | [Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:363` | Confirma o embandeiramento da hélice após corte de motor em emergência para evitar arrasto parasita severo. | **Médio** | Puxar manete de hélice toda para trás (posição feather) com motor parado. |
| **Combustível** (`fuel.selector_valve`, `fuel.tanks`) | `FUEL TANK SELECTOR:index` | `Enum` | `:index` por motor | R | [Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:450` | Detecta chaveamento contínuo da seletora de combustível e esvaziamento assimétrico de asa gerando desbalanceamento lateral. | **Médio** | Alternar a seletora entre Left, Right, Both e Off. |
| **Combustível** (`fuel.lines_fittings`, `fuel.tanks`) | `FUEL WEIGHT PER GALLON` | `Pounds` (lbs/gal) | Escalar | R | [Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) | Sim | `aircraft\fbw-a32nx\...\A32NX_Refuel.ts:21` | Distingue Avgas (aprox. 6.0 lbs/gal) de Jet-A (aprox. 6.7 lbs/gal); abastecer combustível trocado gera pane de detonação ou corte imediato. | **Alto** | Abastecer aeronave a pistão com Jet-A no menu de peso do sim. |
| **Combustível** (`fuel.lines_fittings`) | `FUEL DUMP SWITCH` | `Bool` | Escalar | R | [Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:498` | Registra o uso da válvula de alijamento de combustível em emergência (custo de combustível e desgaste da válvula). | **Baixo** | Acionar a chave de combustível dump em aeronaves pesadas. |
| **Superfícies de Controle** (`wings.flaps`) | `TRAILING EDGE FLAPS LEFT PERCENT`, `RIGHT PERCENT` | `Percent Over 100` | Escalar | R | [Control Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/) | Sim | `aircraft\fbw-a32nx\...\sound.xml:73` | Detecta assimetria de flap real (uma asa com flap estendido e outra recolhida), gerando rolamento violento. | **Alto** | Atuar comando de flaps e conferir deflexão física de cada lado. |
| **Superfícies de Controle** (`wings.ailerons`, `tail.elevator`, `tail.rudder`) | `ELEVATOR POSITION`, `RUDDER POSITION` | `Position` (-1.0 a +1.0) | Escalar | R | [Control Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/) | Sim | `aircraft\fbw-a32nx\...\sound.xml:1736-1741` | Acúmulo de ciclos e fadiga mecânica nas articulações, cabos de comando e rolamentos de cauda. | **Médio** | Defletir totalmente manche e pedais de leme no teste de comandos pré-voo. |
| **Fuselagem / Portas** (`fuselage.doors_seals`, `fuselage.cargo_doors_latches`) | `CANOPY OPEN`, `EXIT OPEN:1` | `Percent Over 100` | `:1` por porta | R | [Misc Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:280-286` | Detecta portas ou capota abertas em voo acima de 40 nós (dano estrutural na dobradiça e despressurização instantânea). | **Alto** | Abrir a porta do piloto ou capota com motor acelerado na decolagem. |
| **Elétrico / Luzes** (`electrical.lighting_exterior`) | `LIGHT BEACON ON`, `LIGHT STROBE ON`, `LIGHT LANDING ON`, `LIGHT TAXI ON`, `LIGHT NAV ON` | `Bool` | Escalar por tipo | R | [Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) | Sim | `aircraft\fbw-a32nx\...\A32NX_Lights.xml:33` | Monitora horas de queima das lâmpadas e superaquecimento do farol de pouso aceso parado no solo prolongado. | **Médio** | Ligar cada chave de luz individualmente no overhead/painel. |
| **Elétrico / Bateria** (`electrical.battery`) | `EXTERNAL POWER AVAILABLE:1`, `EXTERNAL POWER ON:1` | `Bool` | `:1` por conexão | R | [Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) | Sim | `aircraft\fbw-a32nx\...\A32NX_Interior_Elec.xml:14` | Evita descarga profunda de bateria durante ground handling com aviônicos ligados se houver GPU conectada. | **Médio** | Conectar a GPU no pátio e acionar a chave EXT PWR. |
| **Pressurização** (`pressurization.outflow_valve`) | `PRESSURIZATION CABIN ALTITUDE RATE` | `Feet per minute` | Escalar | R | [System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:887` | Detecta taxa de despressurização brusca (> 1.500 ft/min) que causa barotrauma em passageiros e aciona máscaras de O₂. | **Alto** | Voar pressurizado e atuar a válvula de alívio rápido (dump). |
| **Cabine / Segurança** (`cabin.passenger_seats`) | `CABIN SEATBELTS ALERT SWITCH` | `Bool` | Escalar | R | [System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) | Sim | `aircraft\fbw-a380x\...\FwsCore.ts:4557` | Penalidade de satisfação/segurança de passageiros se voar em turbulência severa com aviso de cinto desligado. | **Médio** | Acionar a chave SEATBELTS no painel da cabine. |
| **Proteção contra Fogo** (`fire.engine_extinguisher_bottles`) | `FIRE BOTTLE SWITCH:index`, `FIRE BOTTLE DISCHARGED:index` | `Bool` | `:index` por garrafa | R | [System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) | Sim | `FsConnect\...\FsSimVarFactory.cs:890-891` | Registra a descarga de garrafa de halon no motor (apaga fogo, mas exige recarga cara no Hangar antes do próximo voo). | **Alto** | Puxar o punho de fogo e acionar o botão DISCH do extintor de motor. |
| **Ambiente / Pista** (`tires.*`, `engine.air_intake_filter`, `propeller.blades`) | `SURFACE TYPE` | `Enum` (0=Concrete, 1=Grass, 4=Asphalt, 7=Gravel, 14=Sand...) | Escalar | R | [Simulation Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/) | Sim | `aircraft\fbw-a32nx\...\sound.xml:1886` | Pistas de terra, grama ou cascalho aceleram o desgaste de pneus, rolamentos, hélice (FOD de pedras) e filtro de ar em 3× a 5×. | **Alto** | Táxi ou decolagem a partir de pista de cascalho ou grama não pavimentada. |
| **Ambiente / Clima** (`tires.*`, `brakes.*`) | `SURFACE CONDITION` | `Enum` (0=Normal, 1=Wet, 2=Icy, 3=Snow) | Escalar | R | [Simulation Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/) | Sim | Context Registry do FlightCareer | Pista molhada ou com gelo aumenta o risco de aquaplanagem e derrapagem lateral nos pousos e frenagens. | **Alto** | Pousar com chuva intensa em pista molhada (Wet) ou com neve (Snow). |
| **Ambiente / Atmosfera** (`engine.*`, `brakes.*`) | `AMBIENT DENSITY` | `Slugs per cubic foot` | Escalar | R | [Environment Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/environment-variables/) | Sim | `aircraft\fbw-a32nx\...\sound.xml:746` | Ar rarefeito em aeroportos de grande altitude reduz o resfriamento de freios e cilindros do motor. | **Médio** | Operar em aeroporto de alta montanha (ex.: La Paz - SLLP). |
| **Degelo / Célula** (`emergency.ice_detector`, `wings.spars_ribs`) | `TOTAL WEIGHT ICE` | `Pounds` (lbs) | Escalar | R | [Environment Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/environment-variables/) | Sim | SDK Environment Variables | Mede o peso adicional da carga de gelo na aeronave, reduzindo sustentação e exigindo potência excessiva. | **Alto** | Voar em nuvem com temperatura de 0°C a -10°C sem ligar os sistemas de degelo. |

---

## 2. Top 25 para o Próximo Teste de Solo e Voo Curto (~15 Minutos)

As 25 variáveis de maior retorno técnico imediato para a integridade física de peças e regras operacionais foram agrupadas abaixo em um roteiro linear e executável de solo e voo curto (C152 ou C172):

### Lista das Top 25
1. `COVER ON:0` (Calços)
2. `COVER ON:2` (Capa de Pitot)
3. `COVER ON:6` (Amarração da Hélice)
4. `CANOPY OPEN` ou `EXIT OPEN:1` (Porta da Cabine)
5. `SUCTION PRESSURE` (Vácuo dos Instrumentos)
6. `ENG OIL QUANTITY:1` (Nível de Óleo do Cárter)
7. `RECIP ENG PRIMER:1` (Primer de Partida)
8. `BRAKE PARKING POSITION` (Freio de Mão)
9. `BRAKE LEFT POSITION` (Pedal de Freio Esquerdo)
10. `BRAKE RIGHT POSITION` (Pedal de Freio Direito)
11. `CENTER WHEEL RPM` (Rotação da Roda Dianteira)
12. `LEFT WHEEL RPM` (Rotação da Roda Esquerda)
13. `RIGHT WHEEL RPM` (Rotação da Roda Direita)
14. `CONTACT POINT COMPRESSION:0` (Amortecedor do Nariz)
15. `CONTACT POINT COMPRESSION:1` (Amortecedor Principal Esquerdo)
16. `CONTACT POINT COMPRESSION:2` (Amortecedor Principal Direito)
17. `CONTACT POINT IS SKIDDING:0` (Derrapagem do Nariz)
18. `CONTACT POINT IS SKIDDING:1` (Derrapagem Trem Esquerdo)
19. `TRAILING EDGE FLAPS LEFT PERCENT` (Flap Esquerdo Físico)
20. `TRAILING EDGE FLAPS RIGHT PERCENT` (Flap Direito Físico)
21. `FUEL TANK SELECTOR:1` (Seletora de Combustível)
22. `LIGHT BEACON ON` (Luz de Beacon)
23. `LIGHT LANDING ON` (Farol de Pouso)
24. `SURFACE TYPE` (Tipo de Superfície da Pista)
25. `TOTAL WEIGHT ICE` (Massa de Gelo Acumulada)

---

### Roteiro de Teste (~15 minutos)

```
[00:00 - Pátio Desligado]
Passo 1: Iniciar no pátio Cold & Dark. Ativar REC no FlightCareer HUD.
Passo 2: Com calços e capas colocados, verificar COVER ON:0 = 1, COVER ON:2 = 1, COVER ON:6 = 1.
Passo 3: Abrir a porta da cabine (CANOPY OPEN ou EXIT OPEN:1 = 1).
Passo 4: Retirar capas e calços na inspeção pré-voo (COVER ON:0..6 -> 0). Fechar a porta.

[03:00 - Procedimento de Partida]
Passo 5: Acionar primer 3 vezes (RECIP ENG PRIMER:1 deve registrar pulsos).
Passo 6: Ligar Beacon (LIGHT BEACON ON = 1). Verificar nível de óleo (ENG OIL QUANTITY:1).
Passo 7: Dar partida no motor. Observar SUCTION PRESSURE subir de 0 para a faixa verde (4.5 a 5.2 inHg).

[06:00 - Teste de Freios e Superfícies no Solo]
Passo 8: Com freio de estacionamento puxado (BRAKE PARKING POSITION = 1), acelerar brevemente para 1.500 RPM.
Passo 9: Soltar freio de mão. Pressionar pedal esquerdo e direito alternados (BRAKE LEFT/RIGHT POSITION).
Passo 10: Iniciar táxi. Verificar WHEEL RPM subindo nas três rodas. Fazer curva aberta para checar ausência de derrapagem (SKIDDING = 0).
Passo 11: Alternar seletora de combustível (FUEL TANK SELECTOR:1) entre Left, Right e Both.
Passo 12: Estender flaps para o primeiro dente (TRAILING EDGE FLAPS LEFT/RIGHT PERCENT devem marcar ~0.20 a 0.33 simétricos).

[10:00 - Corrida e Toque Curto]
Passo 13: Alinhar na pista. Conferir SURFACE TYPE (asfalto ou concreto). Ligar farol de pouso (LIGHT LANDING ON = 1).
Passo 14: Iniciar corrida de decolagem. Ao rodar a aeronave, CONTACT POINT COMPRESSION:0..2 desce a 0.00.
Passo 15: Fazer um circuito de tráfego rápido (touch-and-go).
Passo 16: No toque: registrar o pico de CONTACT POINT COMPRESSION em cada perna e o salto de WHEEL RPM de 0 para a velocidade de pista.
Passo 17: Frear com firmeza no retorno ao pátio e encerrar a gravação com botão Marcar/Parar no HUD.
```

---

## 3. Variáveis Descartadas ou Esclarecidas

| Variável Candidata | Status Oficial | Motivo do Descarte ou Esclarecimento Técnico |
|---|---|---|
| `BRAKE TEMPERATURE` | **Não existe como SimVar nativa no SDK** | Não existe variável de temperatura de freio nativa na categoria `Aircraft Brake and Landing Gear Variables` do MSFS 2024. Projetos complexos (FBW A320, Fenix) implementam modelo termodinâmico próprio em C++/WASM e gravam em LVars (ex.: `L:A32NX_BRAKE_TEMPERATURE_1`). No FlightCareer, a dissipação térmica e desgaste dos freios é calculada pela física de absorção de energia cinética ($E = \frac{1}{2} m \Delta v^2$). |
| `PARTIAL PANEL STATIC` | **Inexistente / Rejeitada** | Já comprovada na rodada 1 e 2: rejeitada pelo SimConnect (`NAME_UNRECOGNIZED`). Não há SimVar de pane para tomada estática analógica legada. |
| `AIRCRAFT STRESS DAMAGE` | **Inexistente** | Inexistente como SimVar. No MSFS 2024, o estresse nativo é medido exclusivamente pela taxa contínua `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>`. |
| `ELECTRICAL LINE BREAKER PULLED` | **Nome Errado** | O nome correto e documentado no SDK 2024 é `CIRCUIT BREAKER PULLED:index`. |
| `ELECTRICAL BUS VOLTAGE:name` | **Sintaxe Inválida no SimConnect** | `ELECTRICAL BUS VOLTAGE` aceita apenas índice numérico (`:1`, `:2`), e não strings nominais. |
| `TOTAL WEIGHT ON WHEELS` | **Legada / Redundante** | Variável clássica de modelo simplificado do FSX. No MSFS 2024 o peso nas rodas é melhor aferido pela soma de `CONTACT POINT COMPRESSION:index`. |
| `IS TAIL DRAGGER` | **Substituída por Contexto** | Variável descritiva estática dispensável, pois a configuração do trem de pouso já é identificada no seed da aeronave (`isGearTaildragger`). |
