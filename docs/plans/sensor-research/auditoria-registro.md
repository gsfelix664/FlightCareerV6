# Auditoria do Registro de Variáveis do FlightCareer (SDK MSFS 2024) — Rodada 2 Corrigida

> **Documento revisado e corrigido na tarefa RESEARCH-SIMVARS-r3**  
> **Critério de Prova:** Cruzamento estrito entre a documentação oficial do SDK MSFS 2024 e os **CSVs reais** gravados pelo `SensorRecorder` (`%LocalAppData%\FlightCareer\sensor-logs\20261115-1747-Cessna_C152.csv`, `20261115-1733-Cessna_C152.csv`, `20261115-1534-C172SP_G1000_Passengers.csv`, `20261115-1636-C172SP_G1000_Passengers.csv`).  
> **Regra do Simulador:** O `SensorFrameReader` remove automaticamente do layout toda SimVar rejeitada pelo MSFS (`SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED`). Portanto, **coluna presente no CSV = nome aceito pelo simulador**. Somente é classificada como inexistente/rejeitada a variável ausente dos logs por rejeição do SimConnect.

---

## 1. Definições Fixas (`SimConnectService.cs` e `SimTelemetry.cs`)

> [!WARNING]
> Erros em definições fixas têm gravidade crítica: uma SimVar com nome não reconhecido rejeitada pelo MSFS causa `SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED`, diminui o pacote de bytes e desloca todos os campos subsequentes da struct (`AGENTS.md` §6).

| Variável Registrada | Struct / Posição | Unidade no Código | Aceita pelo Sim? | Varia nos Logs? | Documentada? (URL) | Análise / Problema Real | Gravidade | Recomendação |
|---|---|---|---|---|---|---|---|---|
| `TURB ENG CONDITION LEVER POSITION:1` | `SimTelemetry` (pos. 49) | `Percent` | Sim (em turbinas) | Sim | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Documentada como enumerador discreto (0 = Cut-off, 1 = Low Idle, 2 = High Idle) ou percentual em certos modelos. Registrar como `Percent` pode gerar escala 0%, 100%, 200%. | Média | Manter `Enum` / `Number` ou tratar escala no consumidor. |
| `BRAKE LEFT WEIGHT REMAINING` / `BRAKE RIGHT WEIGHT REMAINING` | `SimFailureState` (pos. 10 e 11) | `Percent` | Sim | Sim (em panes) | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) | No SDK oficial a unidade canônica é `Percent Over 100` (0.00 a 1.00). O SimConnect converte para `Percent` (0 a 100). | Baixa | Alinhar unidade para `Percent Over 100` para padronização interna. |
| `RECIP CARBURETOR TEMPERATURE:1` | `SimTelemetry` (pos. 53) | `Celsius` | Sim | Sim | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) | Unidade nativa no motor a pistão é `Rankine`. O SimConnect realiza a conversão em tempo de execução para `Celsius` perfeitamente. | Informativa | Manter `Celsius`. |
| `PARTIAL PANEL VACUUM`, `PITOT`, `ALTIMETER`, `ELECTRICAL` | `SimTelemetry` (pos. 56–58, 62) | `Enum` | Sim | Sim (quando acionado) | [Simulation Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/) | Variáveis legadas do FSX/MSFS 2020 para falhas de instrumentos. Válidas e compatíveis no MSFS 2024 para aeronaves analógicas. | Informativa | Manter para compatibilidade ampla. |

---

## 2. Auditoria de Nomes de SimVars (`sensor_registry.json`)

> [!NOTE]
> **Correção da Rodada 2:** Na rodada 2, `FLAP SPEED EXCEEDED`, `GEAR SPEED EXCEEDED` e `ELECTRICAL BATTERY LEVEL` haviam sido consideradas "inexistentes" porque não constavam na documentação web aberta do SDK. O cruzamento com os CSVs reais revelou que **as três foram aceitas pelo simulador** e estão presentes nos 4 logs sem disparar exceção, sendo que `ELECTRICAL BATTERY LEVEL` varia ativamente.

| Chave (`key`) | SimVar Cadastrada | Grupo | Aceita pelo Sim? | Varia nos Logs? | Documentada? | Diagnóstico Técnico Real | Ação Recomendada |
|---|---|---|---|---|---|---|---|
| `dmg-flap-speed-exceeded` | `FLAP SPEED EXCEEDED` | `native-damage` | **Sim** (presente nos 4 logs: C152 e C172) | Constante (`0` em voo dentro do envelope) | Não documentada na web pública do SDK 2024 | Aceita pelo motor do SimConnect nativamente. Retorna flag quando $V_{FE}$ é violada. | Manter no registro. Pode ser complementada com `FLAP DAMAGE BY SPEED` e limites de POH. |
| `dmg-gear-speed-exceeded` | `GEAR SPEED EXCEEDED` | `native-damage` | **Sim** (presente nos 4 logs: C152 e C172) | Constante (`0` no C152/C172) | Não documentada na web pública do SDK 2024 | Aceita pelo SimConnect. Flag de excedência de velocidade de trem de pouso ($V_{LO}/V_{LE}$). | Manter no registro para aeronaves com trem retrátil. |
| `elec-battery-level` | `ELECTRICAL BATTERY LEVEL` | `electrical` | **Sim** (presente nos 4 logs) | **Sim** (608 valores distintos no C152: 98.6% a 100%) | Não documentada na página `aircraft-electrics-variables` | Variável ativa no sim que reporta o nível de carga da bateria. Altamente funcional no C152 para monitorar descarga e alternador inoperante. | **Manter e utilizar**. Excelente sensor de saúde da bateria. |

---

## 3. Erros de Indexação e Escopo (`sensor_registry.json`)

> [!IMPORTANT]
> O layout dinâmico (`SensorLayout`) expande sensores com `perEngine: true` adicionando `:1`, `:2` correspondentes ao número de motores. Sensores cujo índice no simulador referencia outro subsistema (pontos de contato, circuitos, linhas) sofrem distorção de significado se marcados com `perEngine`.

| Chave (`key`) | SimVar Cadastrada | Índice Atual | Aceita pelo Sim? | Varia nos Logs? | Problema de Indexação | Proposta de Solução no FlightCareer |
|---|---|---|---|---|---|---|
| `gear-contact-point-compression` | `CONTACT POINT COMPRESSION` | `perEngine: true` | Sim (expandida como `:1`) | **Sim** (571 valores distintos no C152: 0.00 a 0.48) | Indexada incorretamente por motor. No simulador, o índice é o **ponto de contato** (`point.N` em `flight_model.cfg`): `0` = trem de nariz/bequilha, `1` = trem principal esquerdo, `2` = trem principal direito. Com `perEngine`, a perna dianteira (`:0`) é completamente ignorada! | **Ler por ponto de contato:** expandir como `gear-contact-compression:0` (nose), `:1` (left main), `:2` (right main), com descoberta de quantidade de contatos via contexto/calibração. |
| `gear-contact-point-is-skidding` | `CONTACT POINT IS SKIDDING` | `perEngine: true` | Sim (expandida como `:1`) | **Sim** (valores 0 e 1 no C152) | Mesma causa: indexada por ponto de contato (`point.N`), não por motor. | Expandir por ponto de contato (`:0`, `:1`, `:2`) para detectar derrapagem e esforço lateral em cada perna. |
| `fuel-system-pump-active` | `FUELSYSTEM PUMP ACTIVE` | `perEngine: true` | Sim (expandida como `:1`) | Sim/Constante | No sistema modular `[FUEL_SYSTEM]`, o índice é o **ID da bomba de combustível** na topologia modular, e não necessariamente o motor. | Resolver índice da bomba no catálogo da aeronave (`AircraftLimitProfile` / calibração). |
| `fuel-system-line-fuel-pressure` | `FUELSYSTEM LINE FUEL PRESSURE` | `perEngine: true` | Sim (expandida como `:1`) | Sim | O índice é o **número da linha de combustível**. Em bimotores com crossfeed, a linha do motor 2 pode ter índice 4 ou 5. | Resolver índice da linha no catálogo da aeronave. |
| `af-wing-flex-pct` | `WING FLEX PCT` | `perEngine: false` (sem índice) | Sim (aceita como escalar ou `:1`) | Sim | No SDK do MSFS, `WING FLEX PCT` aceita índice de asa (`:1` esquerda, `:2` direita). | Cadastrar `WING FLEX PCT:1` e `WING FLEX PCT:2` para asas independentes. |
| `elec-circuit-breaker-pulled` | `CIRCUIT BREAKER PULLED` | `perEngine: false` (sem índice) | Sim | Constante | Requer índice do circuito/disjuntor (`:index`, base 1). Sem índice, consulta o índice padrão. | Mapear disjuntores críticos por aeronave na calibração. |
| `pneu-duct-pressure` | `PNEUMATICS DUCT PRESSURE` | `perEngine: false` | Sim | Sim | No sistema modular pneumático, indexado por pack/duto. | Definir índice do duto principal da aeronave. |
| `pneu-pack-status` | `PNEUMATICS PACK STATUS` | `perEngine: false` | Sim | Sim | Indexado por pack (`:1`, `:2`). | Definir índice do pack. |

---

## 4. Análise de Unidades e Escalas (`sensor_registry.json`)

| Chave (`key`) | SimVar | Unidade no Registro | Unidade Canônica SDK | Aceita pelo Sim? | Valores Reais no CSV | Impacto Técnico | Status |
|---|---|---|---|---|---|---|---|
| `elec-battery-estimated-capacity-pct` | `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT` | `percent over 100` | `Percent` | Sim | 0.986 a 1.000 | O SimConnect entrega valores entre 0.00 e 1.00 com a unidade `percent over 100`. A conversão funciona perfeitamente sem distorção. | **Informativo (Válido)** |
| `pneu-pack-status` | `PNEUMATICS PACK STATUS` | `bool` | `percent over 100` | Sim | 0.0 ou 1.0 | O SDK documenta como vazão normalizada (0.00 a 1.00). Como booleano retorna 0 ou 1. | **Informativo (Válido)** |
| `gear-skidding-factor` | `GEAR SKIDDING FACTOR` | `percent` | `Percent Over 100` | Sim | 0 a 100 | Em `percent`, o valor vem na escala 0–100. Em `percent over 100`, viria 0.0–1.0. | **Informativo (Padronizar)** |
| `eng-max-cht`, `eng-max-egt`, `oil-max-temperature` | `RECIP MAX CHT`, etc. | `celsius` | `Rankine` | Sim | Valores em °C coerentes | A API do SimConnect realiza a conversão de `Rankine` para `celsius` automaticamente. | **Informativo (Válido)** |

---

## 5. Variáveis Clássicas vs Modernas no MSFS 2024

| Chave (`key`) | SimVar Cadastrada | Aceita pelo Sim? | Varia nos Logs? | Documentada? (URL) | Status no SDK 2024 | Análise Técnica | Recomendação |
|---|---|---|---|---|---|---|---|
| `elec-total-load-amps` | `ELECTRICAL TOTAL LOAD AMPS` | **Sim** | **Sim** (C152: 0–46 A; C172: 24–175 A) | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) | **Válida e Documentada** (não está obsoleta) | Reporta a carga total instantânea exigida de todo o sistema elétrico. Varia dinamicamente ao ligar aviônicos, faróis, pitot heat e partida. | **Manter**. Essencial para diagnóstico de sobrecarga elétrica geral. Complementar com `ELECTRICAL BUS AMPS:index`. |
| `hyd-pressure`, `hyd-reservoir-percent` | `HYDRAULIC PRESSURE`, `HYDRAULIC RESERVOIR PERCENT` | Sim | Sim (em hidráulicos) | [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) | Válida para sistema clássico | Mede pressão e reservatório de sistema hidráulico geral. Em aeronaves complexas modernas coexiste com circuitos modulares. | Manter para suporte a jatos e turboélices clássicos. |
| `af-airspeed-barber-pole` | `AIRSPEED BARBER POLE` | Sim | Sim (varia com altitude/pressão) | [Aircraft Flight Model Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/) | Válida | Velocidade máxima operacional indicada pelo ponteiro listrado (barber pole), dinâmica com Mach. | Manter. Válida e útil. |

---

## 6. Proposta de Arquitetura para Leitura de Contact Points (`CONTACT POINT COMPRESSION / IS SKIDDING`)

### Diagnóstico
Atualmente, `gear-contact-point-compression` e `gear-contact-point-is-skidding` estão registradas com `perEngine: true`. O `SensorLayout` as expande como `:1` (e `:2` se bimotor).
Nos aviões triciclo:
- **Ponto 0:** Trem de Nariz (Nose Gear).
- **Ponto 1:** Trem Principal Esquerdo (Left Main Gear).
- **Ponto 2:** Trem Principal Direito (Right Main Gear).

Como resultado, o código atual lê apenas o trem principal esquerdo (`:1`), ignorando totalmente o impacto no trem de nariz (`:0`) — que é o ponto mais vulnerável a pouso duro tipo "three-point" ou colapso de garfo — e o trem direito (`:2`) em monomotores.

### Solução Proposta para o FlightCareer
1. **Desacoplar de `perEngine`:** No `sensor_registry.json`, substituir as entradas por sensores explícitos por ponto de contato do trem de pouso:
   - `gear-contact-point-0-compression` (`CONTACT POINT COMPRESSION:0`, unidade `percent over 100`, Nose Gear).
   - `gear-contact-point-1-compression` (`CONTACT POINT COMPRESSION:1`, Left Main Gear).
   - `gear-contact-point-2-compression` (`CONTACT POINT COMPRESSION:2`, Right Main Gear).
   - `gear-contact-point-0-is-skidding` (`CONTACT POINT IS SKIDDING:0`, Bool).
   - `gear-contact-point-1-is-skidding` (`CONTACT POINT IS SKIDDING:1`, Bool).
   - `gear-contact-point-2-is-skidding` (`CONTACT POINT IS SKIDDING:2`, Bool).
2. **Mapeamento Direto para as Peças:**
   - Contato 0 → Alimenta `landinggear.nose_strut` e `tires.nose_tire`.
   - Contato 1 → Alimenta `landinggear.main_struts` (esquerdo), `tires.main_left_tire` e `brakes.discs_linings_left`.
   - Contato 2 → Alimenta `landinggear.main_struts` (direito), `tires.main_right_tire` e `brakes.discs_linings_right`.
3. **Detecção de Tailstrike:**
   - Adicionar `gear-contact-point-tail-compression` (`CONTACT POINT COMPRESSION:N` onde N é o scrape point traseiro, tipicamente ponto 3 ou 4) ou ler `TAILHOOK POSITION` / ângulo de arfagem no toque para dano na antepara traseira (`fuselage.pressure_bulkheads`).

---

## 7. Implementação e Alinhamento na Tarefa REGRAS-C152 (2026-11-16)

Conforme comprovado pela matriz de sondagem do Cessna 152 (`docs/plans/sensor-validation/probe-runs/matriz-C152.md`), as seguintes correções foram integradas:
1. **Contact Points por Ponto de Contato Fixo (`perEngine: false`):**
   - `gear-contact-point-compression-0`, `-1`, `-2` (`CONTACT POINT COMPRESSION:0..2`)
   - `gear-contact-point-is-skidding-0`, `-1`, `-2` (`CONTACT POINT IS SKIDDING:0..2`)
   - `gear-contact-point-on-ground-0`, `-1`, `-2` (`CONTACT POINT IS ON GROUND:0..2`)
   - Substituto para `CONTACT POINT SKIDDING FACTOR:0..5` que se manteve constante 0 no C152.
2. **Geradores Elétricos MSFS 2024 (`perEngine: true`):**
   - `elec-generator-amps`, `elec-generator-voltage`, `elec-generator-active` ativos no MSFS 2024.
   - As chaves `elec-genalt-*` foram mantidas no registro para compatibilidade com aeronaves legadas.
3. **Pressão Dinâmica (`perEngine: false`):**
   - `af-dynamic-pressure` (`DYNAMIC PRESSURE`, unidade `pound-force per square foot`).
4. **Sensores Mortos no C152 Mantidos no Registro:**
   - `eng-vibration`, `gear-skidding-factor` e `af-min-g-force` continuam no registro (podem operar em outros modelos e o gravador continua coletando dados), mas as regras de desgaste foram desacopladas deles (ex.: `airframe_over_g_negative` usa exclusivamente `af-g-force`).
