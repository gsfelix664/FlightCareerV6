# Inventário de sensores do SimConnect (SDK MSFS 2024)

**Fonte:** documentação oficial do SDK MSFS 2024 (retail), páginas de SimVars de aeronave, consultadas em 2026-11-13:
- Motor: https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/
- Elétrico: .../aircraft-electrics-variables/
- Freios e trem: .../aircraft-brake-and-landing-gear-variables/
- Sistemas (instrumentos, hidráulico, pneumático, pressurização, partial panel): .../aircraft-system-variables/
- Diversos (posição, alertas, wear and tear, velocidades): .../aircraft-miscellaneous-variables/
- Combustível: .../aircraft-fuel-variables/
- Modelo de voo (G, velocidades de projeto): .../aircraft-flight-model-variables/
- Comandos (flaps, spoilers, trim): .../aircraft-control-variables/

**Status de todos os itens: CANDIDATO.** Nenhum vira regra de desgaste antes de passar no voo de validação
(plano `pecas-desgaste-falhas.md`, tarefa F1.4). Já confirmados no MSFS 2024 (C152) em sessão anterior estão marcados ✅.

Convenções:
- `:N` = índice do motor (1 a 16). Ler `NUMBER OF ENGINES` e registrar o índice para cada motor existente.
- **Unidade:** peça sempre a unidade escrita aqui na `AddToDataDefinition` (o SimConnect converte). Ex.: temperaturas em `celsius`, pressão de óleo em `psi`.
- **Escrita (W):** só usar escrita onde já foi testada. As demais escritas estão fora do escopo.
- Coluna "Dica de peça" = qual peça o sensor ajuda a avaliar. **Sensor não é peça.**

---

## 1. Identificação e fase do voo (contexto, não desgasta nada)

| SimVar | Unidade | Uso |
|---|---|---|
| `TITLE`, `ATC MODEL`, `ATC TYPE` | string | identificar o avião e escolher limites |
| `ENGINE TYPE` | enum (0 pistão, 1 jato, 2 nenhum, 3 heli turbina, 4 não suportado, 5 turboélice) | classe do motor |
| `NUMBER OF ENGINES` | number | quantos índices ler |
| `IS GEAR RETRACTABLE`, `IS GEAR FLOATS`, `IS GEAR SKIS`, `IS GEAR WHEELS` | bool | quais peças de trem existem |
| `CARB HEAT AVAILABLE` | bool | carburado × injetado |
| `RECIP ENG NUM CYLINDERS:N` | number | nº de cilindros |
| `FLAPS AVAILABLE`, `FLAPS NUM HANDLE POSITIONS` | bool / number | existem flaps |
| `NEW ELECTRICAL SYSTEM`, `NEW FUEL SYSTEM` | bool | qual família de SimVars elétricas/combustível usar |
| `SIM ON GROUND`, `PLANE ALT ABOVE GROUND`, `GROUND VELOCITY`, `AIRSPEED INDICATED` | — | fase do voo |
| `SIMULATION RATE` | number | relógio de desgaste (tempo simulado) |
| `SURFACE TYPE`, `SURFACE CONDITION`, `ON ANY RUNWAY` | enum / bool | pista de terra/grama, molhada |
| `AMBIENT TEMPERATURE`, `AMBIENT PRECIP STATE`, `TOTAL AIR TEMPERATURE` | celsius / mask | multiplicadores (frio, chuva) |

## 2. Motor a pistão

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `GENERAL ENG RPM:N` ✅ | rpm | cilindros, virabrequim (over-RPM) |
| `GENERAL ENG MAX REACHED RPM:N` | rpm | over-RPM registrado pelo sim |
| `RECIP ENG MANIFOLD PRESSURE:N` | inHg | cilindros (carga), turbo |
| `RECIP ENG CYLINDER HEAD TEMPERATURE:N` ✅ | celsius | cilindros, válvulas (superaquecimento, shock cooling) |
| `RECIP MAX CHT` | celsius | limite do próprio avião |
| `GENERAL ENG EXHAUST GAS TEMPERATURE:N` | celsius | válvulas de escape, escapamento |
| `MAX EGT` | celsius | limite do avião |
| `RECIP ENG TURBINE INLET TEMPERATURE:N` | celsius | turbocompressor (TIT) |
| `RECIP ENG DETONATING:N` | bool | **cilindros/pistões (detonação)** |
| `RECIP ENG CYLINDER HEALTH:N` | percent_over_100 | saúde nativa dos cilindros (comparar com a nossa) |
| `RECIP ENG NUM CYLINDERS FAILED:N` | number | cilindro perdido (nativo) |
| `RECIP MIXTURE RATIO:N` | ratio | detonação, mistura pobre |
| `GENERAL ENG MIXTURE LEVER POSITION:N` | percent | idem |
| `GENERAL ENG THROTTLE LEVER POSITION:N` | percent | carga, operação brusca |
| `RECIP ENG BRAKE POWER PCT:N` | percent_over_100 | % de potência (carga real) |
| `RECIP ENG FUEL FLOW:N` | pounds per hour | bomba/injetores |
| `GENERAL ENG FUEL PRESSURE:N` | psi | bomba de combustível |
| `RECIP CARBURETOR TEMPERATURE:N` ✅ | celsius | carburador (gelo) |
| `RECIP ENG ALTERNATE AIR POSITION:N` | position | filtro/admissão |
| `RECIP ENG WASTEGATE POSITION:N` | percent | turbo / wastegate |
| `RECIP ENG TURBOCHARGER FAILED:N` | bool | turbocompressor (nativo) |
| `RECIP ENG COWL FLAP POSITION:N` | percent | refrigeração (cowl flap) |
| `RECIP ENG PRIMER:N` | bool | primer |
| `RECIP ENG GLOW PLUG ACTIVE:N` | bool | velas aquecedoras (diesel) |
| `RECIP ENG RADIATOR TEMPERATURE:N`, `RECIP ENG COOLANT RESERVOIR PERCENT:N` | celsius / percent | arrefecimento líquido (diesel/Rotax) |
| `RECIP ENG LEFT MAGNETO:N` / `RIGHT MAGNETO:N` ✅ (W ✅) | bool | magnetos |
| `RECIP ENG STARTER TORQUE:N` | foot pound | motor de partida |
| `GENERAL ENG STARTER:N` ✅, `GENERAL ENG STARTER ACTIVE:N` | bool | motor de partida (tempo engatado) |
| `GENERAL ENG COMBUSTION:N` ✅ | bool | motor funcionando |
| `PROP RPM:N`, `GENERAL ENG PROPELLER LEVER POSITION:N` | rpm / percent | hélice, governador |
| `ENG VIBRATION:N` | number | **hélice/motor desbalanceado** |

## 3. Óleo (todas as classes)

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `GENERAL ENG OIL TEMPERATURE:N` ✅ | celsius | óleo, radiador de óleo |
| `MAX OIL TEMPERATURE` | celsius | limite do avião |
| `GENERAL ENG OIL PRESSURE:N` ✅ | psi | bomba de óleo, bronzinas |
| `ENG OIL QUANTITY:N` | percent_over_100 | nível de óleo |
| `GENERAL ENG OIL LEAKED PERCENT:N` ✅ (W ✅) | percent | retentores, juntas |
| `OIL COOLING FLAPS POSITION:N` | percent_over_100 | radiador de óleo |
| `WARNING OIL PRESSURE` | bool | alerta do painel |

## 4. Turbina (turboélice e jato)

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `TURB ENG N1:N`, `TURB ENG N2:N` | percent | compressor, turbina (sobrevelocidade) |
| `TURB ENG ITT:N` | celsius | seção quente (palhetas, câmara) |
| `TURB MAX ITT` | celsius | limite do avião |
| `TURB ENG MAX TORQUE PERCENT:N` | percent | caixa de redução (turboélice) |
| `TURB ENG FREE TURBINE TORQUE:N`, `ENG TORQUE:N` | foot pound | idem |
| `TURB ENG FUEL FLOW PPH:N` | pounds per hour | FCU / bomba |
| `TURB ENG IS IGNITING:N`, `TURB ENG IGNITION SWITCH EX1:N` | bool / enum | ignitores |
| `TURB ENG MASTER STARTER SWITCH:N` | bool | starter/gerador |
| `TURB ENG CONDITION LEVER POSITION:N` | enum | corte de combustível |
| `TURB ENG INLET TEMPERATURE:N` | celsius | admissão (gelo/FOD) |
| `TURB ENG VIBRATION:N` | number | **rotor desbalanceado** |
| `TURB ENG BLEED AIR:N` | psi | sangria |
| `TURB ENG REVERSE NOZZLE PERCENT:N`, `GENERAL ENG REVERSE THRUST ENGAGED:N` | percent / bool | reversor |
| `PROP BETA:N`, `PROP FEATHERED:N` | radians / bool | hélice (turboélice) |
| `APU PCT RPM`, `APU ON FIRE DETECTED` | percent / bool | APU (jatos) |

## 5. Falhas e danos que o simulador já informa

| SimVar | Unidade | Uso |
|---|---|---|
| `ENG FAILED:N` ✅ | bool | motor falhado |
| `ENG ON FIRE:N` ✅ (W ✅), `GENERAL ENG FIRE DETECTED:N` | bool | fogo |
| `GENERAL ENG DAMAGE PERCENT:N` ✅ | percent | dano nativo do motor |
| `FLAP DAMAGE BY SPEED` ✅, `FLAP SPEED EXCEEDED` | bool | flaps |
| `GEAR DAMAGE BY SPEED` ✅, `GEAR SPEED EXCEEDED` | bool | trem |
| `CRASH FLAG` ✅ | enum | acidente |
| `PARTIAL PANEL VACUUM/PITOT/STATIC/ELECTRICAL/ENGINE/AIRSPEED/ALTIMETER/ATTITUDE/HEADING/...` | enum | estado de panes do menu do sim |
| `WEAR AND TEAR LEVEL`, `WEAR AND TEAR IS WEAK`, `WEAR AND TEAR IS FAILED`, `WEAR AND TEAR OVERSTRESS FACTOR` | percent_over_100 / bool | **desgaste nativo do MSFS 2024** (só para detectar se o jogador deixou ligado e avisar) |

## 6. Elétrico

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `GENERAL ENG MASTER ALTERNATOR:N` ✅ (só leitura) | bool | chave do alternador |
| `ELECTRICAL GENALT BUS VOLTAGE:N`, `ELECTRICAL GENALT BUS AMPS:N` | volts / amperes | alternador/gerador |
| `ELECTRICAL GENALT LOAD:N` | percent | **sobrecarga do alternador** |
| `ELECTRICAL MAIN BUS VOLTAGE` | volts | regulador de voltagem |
| `ELECTRICAL BATTERY VOLTAGE`, `ELECTRICAL BATTERY LOAD`, `ELECTRICAL BATTERY LEVEL` | volts / amperes / percent | bateria (descarga profunda) |
| `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT` | percent_over_100 | bateria |
| `ELECTRICAL MASTER BATTERY` | bool | uso da bateria |
| `ELECTRICAL TOTAL LOAD AMPS` | amperes | carga elétrica |
| `EXTERNAL POWER ON` | bool | GPU (poupa bateria/starter) |
| `WARNING VOLTAGE` | bool | alerta do painel |
| `BREAKER <nome>`, `CIRCUIT BREAKER PULLED` | bool | disjuntores (fase futura: aviônicos) |

## 7. Vácuo, pitot, estática e gelo

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `SUCTION PRESSURE` ✅ | inHg | bomba de vácuo |
| `WARNING VACUUM` | bool | alerta |
| `PITOT HEAT`, `PITOT HEAT SWITCH`, `PITOT ICE PCT` | bool / enum / percent | sistema pitot |
| `ALTERNATE STATIC SOURCE OPEN` | bool | sistema estático |
| `STRUCTURAL ICE PCT`, `STRUCTURAL DEICE SWITCH`, `PROP DEICE SWITCH`, `WINDSHIELD DEICE SWITCH` | percent / bool | gelo na célula/hélice |
| `ENG ANTI ICE:N` / `GENERAL ENG ANTI ICE POSITION:N` | bool | carb heat / anti-gelo do motor |

## 8. Combustível

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `FUEL TOTAL QUANTITY`, `FUEL TOTAL QUANTITY WEIGHT` | gallons / pounds | — (já usado) |
| `GENERAL ENG FUEL PUMP SWITCH:N`, `GENERAL ENG FUEL PUMP ON:N` | bool | bomba elétrica (horas de uso) |
| `FUELSYSTEM PUMP ACTIVE:N`, `FUELSYSTEM ENGINE PRESSURE:N`, `FUELSYSTEM LINE FUEL PRESSURE:N` | bool / kPa | bombas e linhas (só aviões com `NEW FUEL SYSTEM`) |
| `WARNING FUEL` | bool | alerta |
| `RECIP ENG FUEL AVAILABLE:N`, `TURB ENG FUEL AVAILABLE:N` | bool | falta de combustível (não é pane de peça) |

## 9. Trem, pneus e freios

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `PLANE TOUCHDOWN NORMAL VELOCITY` | feet per second | amortecedores, estrutura do trem, pneus |
| `PLANE TOUCHDOWN BANK DEGREES`, `PLANE TOUCHDOWN PITCH DEGREES` | degrees | pouso em uma roda / tail strike |
| `CONTACT POINT COMPRESSION:N` | position | **amortecedor (fim de curso)** |
| `CONTACT POINT IS SKIDDING:N`, `GEAR IS SKIDDING`, `GEAR SKIDDING FACTOR` | bool / percent | **pneus (derrapagem, frenagem travada)** |
| `WHEEL RPM`, `LEFT WHEEL RPM`, `RIGHT WHEEL RPM` | rpm | pneus (spin-up no toque) |
| `GEAR POSITION`, `GEAR HANDLE POSITION`, `GEAR TOTAL PCT EXTENDED` | percent | atuador do trem (ciclos) |
| `GEAR HYDRAULIC PRESSURE` | psf | hidráulico do trem |
| `GEAR WARNING` | enum | — |
| `BRAKE LEFT POSITION`, `BRAKE RIGHT POSITION` | position (0–32K) | pastilhas, discos |
| `BRAKE LEFT TEMPERATURE`, `BRAKE RIGHT TEMPERATURE` | kelvin | **discos/pastilhas (superaquecimento)** |
| `BRAKE LEFT WEIGHT REMAINING`, `BRAKE RIGHT WEIGHT REMAINING` | percent_over_100 | desgaste nativo das pastilhas |
| `BRAKE PARKING POSITION` | bool | — |
| `ANTISKID BRAKES ACTIVE`, `AUTOBRAKES ACTIVE` | bool | jatos |
| `NOSEWHEEL MAX STEERING ANGLE`, `GEAR STEER ANGLE PCT` | radians / percent | direção da bequilha |

## 10. Célula, comandos e estrutura

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `G FORCE` ✅, `MAX G FORCE`, `MIN G FORCE` | gforce | estrutura (asa, longarina) |
| `SEMIBODY LOADFACTOR Y` | number | estrutura |
| `WING FLEX PCT` | percent_over_100 | estrutura (flexão de asa) |
| `AIRSPEED INDICATED` ✅, `AIRSPEED TRUE`, `AIRSPEED MACH` | knots / mach | estrutura (Vne/Mmo) |
| `OVERSPEED WARNING` ✅, `AIRSPEED BARBER POLE` | bool / knots | idem |
| `REFERENCE SPEED MAX IAS`, `REFERENCE SPEED MAX IAS GEAR DOWN`, `MACH MAX OPERATE` | knots / mach | limites do próprio avião |
| `DESIGN SPEED VS0`, `DESIGN SPEED VS1`, `DESIGN SPEED VC` | kias / feet per second | limites do avião |
| `STALL WARNING` | bool | — |
| `FLAPS HANDLE INDEX` ✅ (W ✅), `FLAPS HANDLE PERCENT`, `FLAPS CURRENT SPEED LIMITATION` | number / percent / knots | flaps (Vfe por posição) |
| `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT` | percent_over_100 | assimetria de flap |
| `SPOILERS HANDLE POSITION` | percent | spoilers |
| `ELEVATOR TRIM POSITION`, `AILERON TRIM PCT`, `RUDDER TRIM PCT` | radians / float | trim (servos) |
| `ACCELERATION BODY X/Y/Z` | feet per second squared | cargas laterais/turbulência |
| `PRESSURIZATION CABIN ALTITUDE`, `PRESSURIZATION PRESSURE DIFFERENTIAL` | feet / psf | **estrutura pressurizada (ciclos de pressurização)** |

## 11. Hidráulico e pneumático (aviões que tiverem)

| SimVar | Unidade | Dica de peça |
|---|---|---|
| `HYDRAULIC PRESSURE`, `HYDRAULIC PUMP PRESSURE`, `HYDRAULIC RESERVOIR PERCENT` | psf / psi / percent | bomba hidráulica, reservatório |
| `HYDRAULIC SYSTEM INTEGRITY` | percent_over_100 | integridade nativa |
| `ENG HYDRAULIC PRESSURE:N`, `ENG HYDRAULIC QUANTITY:N` | psf / percent | bomba acionada pelo motor |
| `PNEUMATICS ENGINE BLEED AIR PRESSURE:N`, `PNEUMATICS DUCT PRESSURE` | psi | sangria, dutos |
| `PNEUMATICS PACK STATUS` | bool | packs de ar condicionado |

---

## Regras de leitura (para o executor)

1. Ler em **duas definições separadas**: a de contexto (seção 1, uma vez por voo e no `AircraftLoaded`) e a de sensores (seções 2 a 11, ~1–2 Hz).
2. Se `AddToDataDefinition` de uma SimVar gerar `SIMCONNECT_EXCEPTION.NAME_UNRECOGNIZED`, registrar no log com o nome e **seguir sem ela** (o `dwIndex` da exceção diz qual foi). Não derrubar a conexão.
3. Variável que fica **constante em 0** durante todo o roteiro de validação = "não suportada nesta aeronave".
4. Itens ✅ já foram vistos funcionando no C152; mesmo assim entram no voo de validação para os outros aviões.
