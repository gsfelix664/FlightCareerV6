# Auditoria do Registro de Variáveis do FlightCareer (SDK MSFS 2024)

> **Documento gerado na tarefa RESEARCH-SIMVARS-r2**  
> **Objetivo:** Auditar todas as variáveis registradas no `src/FlightCareer.Infrastructure/Data/sensor_registry.json` (421 sensores), `context_registry.json` (25 variáveis) e nas definições fixas de `SimConnectService.cs` (`SimTelemetry`, `SimFailureState`, `SimFastFlightState` e definições de escrita).  
> **Critério:** Apresentar **somente os problemas** (nomes inexistentes, variáveis legadas com substituta no 2024, unidades incompatíveis ou suspeitas e erros de indexação/escopo) acompanhados de causa raiz, substituta oficial e URL oficial da documentação do SDK.

---

## 1. Definições Fixas (`SimConnectService.cs` e `SimTelemetry.cs`)

> [!WARNING]
> Erros em definições fixas têm gravidade crítica: uma SimVar com nome não reconhecido rejeitada pelo MSFS causa `SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED`, diminui o pacote de bytes e desloca todos os campos subsequentes da struct (`AGENTS.md` §6).

| Variável Registrada | Struct / Definição | Unidade no Código | Problema Identificado | Gravidade | Substituta / Correção Recomendada | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|---|
| `TURB ENG CONDITION LEVER POSITION:1` | `SimTelemetry` (pos. 49) | `Percent` | O SDK documenta a posição da alavanca de corte/idle de turbina como enumerador discreto de 3 posições (0 = Cut-off, 1 = Low Idle, 2 = High Idle), e não como percentagem contínua. Registrar como `Percent` pode gerar distorção de escala (0%, 100%, 200%) dependendo da aeronave. | Média | Manter `Enum` ou ler via `TURB ENG CONDITION LEVER POSITION:1` com unidade `Enum` / `Number`. | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) |
| `BRAKE LEFT WEIGHT REMAINING` / `BRAKE RIGHT WEIGHT REMAINING` | `SimFailureState` (pos. 10 e 11) | `Percent` | No SDK oficial do MSFS 2024, a unidade canônica documentada é `Percent Over 100` (valor normalizado entre 0.00 e 1.00). O SimConnect aceita `Percent` multiplicando por 100, mas causa inconsistência conceitual com o `sensor_registry.json` que utiliza `percent over 100`. | Baixa | Alinhar unidade para `Percent Over 100` ou tratar escala 0–100 no consumidor. | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) |
| `RECIP CARBURETOR TEMPERATURE:1` | `SimTelemetry` (pos. 53) | `Celsius` | A unidade nativa na documentação do motor a pistão é `Rankine`. O SimConnect realiza a conversão em tempo de execução para `Celsius` sem erro, mas é importante registrar a dependência dessa conversão automática. | Informativa | Manter `Celsius` (conversão suportada pelo SimConnect). | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) |
| `PARTIAL PANEL VACUUM`, `PITOT`, `ALTIMETER`, `ELECTRICAL` | `SimTelemetry` (pos. 56–58, 62) | `Enum` | Variáveis legadas do FSX/MSFS 2020 para falhas parciais de instrumentos. No MSFS 2024, sistemas de falha modernos operam em nível de circuito/linha/componente (`CIRCUIT ON:index`, `WEAR AND TEAR IS FAILED:<id>`). | Baixa | Válidas para compatibilidade com aeronaves clássicas e MSFS 2020. | [Simulation Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/) |

---

## 2. Nomes Inexistentes ou Não Encontrados no SDK (`sensor_registry.json`)

> [!CAUTION]
> Estas variáveis **não existem** no SDK do MSFS 2024. Se requisitadas via `AddToDataDefinition` dinâmica, disparam exceção no SimConnect e falham a leitura do lote correspondente.

| Chave (`key`) | SimVar Cadastrada | Grupo | Causa Raiz do Erro | Substituta Oficial Documentada (MSFS 2024) | Unidade Correta | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|---|
| `dmg-flap-speed-exceeded` | `FLAP SPEED EXCEEDED` | `native-damage` | **Nome inexistente no SDK.** A string refere-se a uma notificação de penalidade da interface do modo Carreira da Asobo, e não a uma SimVar. | `FLAP DAMAGE BY SPEED` (Bool) combinado com `FLAPS CURRENT SPEED LIMITATION` (Knots) e `AIRSPEED INDICATED` (Knots). | `Bool` / `Knots` | [Aircraft Control Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/) |
| `dmg-gear-speed-exceeded` | `GEAR SPEED EXCEEDED` | `native-damage` | **Nome inexistente no SDK.** Trata-se de mensagem de alerta do Career Mode. | `GEAR DAMAGE BY SPEED` (Bool) combinado com `AIRSPEED INDICATED` e limites de $V_{LO}/V_{LE}$. | `Bool` / `Knots` | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) |
| `elec-battery-level` | `ELECTRICAL BATTERY LEVEL` | `electrical` | **Nome inexistente no SDK.** Não existe variável com este nome na categoria `Aircraft Electrics Variables`. | `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT:index` (estimativa de SoC) ou `ELECTRICAL BATTERY LOAD:index` (Amperes). | `Percent` / `Amperes` | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) |

---

## 3. Erros de Indexação e Escopo (`sensor_registry.json`)

> [!IMPORTANT]
> O gerador de layout dinâmico (`SensorLayout`) expande variáveis com `perEngine: true` anexando `:1`, `:2`, etc. Variáveis que requerem índices de outro subsistema (pontos de contato, circuitos, linhas de combustível) sofrem distorção de significado se marcadas com `perEngine`. Do mesmo modo, variáveis que exigem índice sofrem rejeição se cadastradas sem índice.

| Chave (`key`) | SimVar Cadastrada | Índice Atual | Problema de Indexação | Correção Necessária no MSFS 2024 | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|
| `gear-contact-point-compression` | `CONTACT POINT COMPRESSION` | `perEngine: true` | O índice desta SimVar representa o **número do ponto de contato** (`point.N` em `flight_model.cfg`), e **não** o índice do motor. Usar `perEngine` gera leitura do contato 1 e 2 apenas se houver 2 motores, ignorando o trem de nariz (contato 0) ou associando erradamente motor a trem de pouso. | `CONTACT POINT COMPRESSION:index` (onde index = 0, 1, 2 para nose, left, right gear) ou nome do ponto `:'Wheel_Front'_n`. | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) |
| `gear-contact-point-is-skidding` | `CONTACT POINT IS SKIDDING` | `perEngine: true` | Mesma causa: indexado por ponto de contato (`point.N`), não por motor. | `CONTACT POINT IS SKIDDING:index` ou `:'Wheel_Main_Left'_n`. | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) |
| `fuel-system-pump-active` | `FUELSYSTEM PUMP ACTIVE` | `perEngine: true` | No sistema modular de combustível (`[FUEL_SYSTEM]`), o índice representa o **ID da bomba de combustível** no esquema modular (bomba 1, bomba 2, etc.), e não necessariamente o motor. | `FUELSYSTEM PUMP ACTIVE:index` com índice mapeado do catálogo da aeronave. | [Aircraft Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) |
| `fuel-system-line-fuel-pressure` | `FUELSYSTEM LINE FUEL PRESSURE` | `perEngine: true` | O índice representa o **número da linha de combustível**, não o motor. Em bimotores com linhas cruzadas, o índice da linha do motor direito pode ser 4 ou 5. | `FUELSYSTEM LINE FUEL PRESSURE:index` com resolução por catálogo. | [Aircraft Fuel Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-fuel-variables/) |
| `af-wing-flex-pct` | `WING FLEX PCT` | `perEngine: false` (sem índice) | No SDK do MSFS, `WING FLEX PCT` **exige obrigatoriamente** o índice da asa: `:1` para a asa esquerda e `:2` para a asa direita. Consultar sem índice retorna 0.0 ou rejeição. | Definir dois sensores: `WING FLEX PCT:1` (asa esquerda) e `WING FLEX PCT:2` (asa direita). | [Aircraft Flight Model Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/) |
| `elec-circuit-breaker-pulled` | `CIRCUIT BREAKER PULLED` | `perEngine: false` (sem índice) | Exige o índice do disjuntor/circuito (`CIRCUIT BREAKER PULLED:index`, base 1). Sem índice, consulta o índice 0 (geralmente inexistente). | `CIRCUIT BREAKER PULLED:index` indexado por disjuntor do sistema. | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) |
| `elec-battery-estimated-capacity-pct` | `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT` | `perEngine: false` (sem índice) | Em aeronaves com mais de uma bateria (ex.: Main e Standby no G1000), exige índice de bateria (`:index`). | `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT:1` e `:2`. | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) |
| `pneu-duct-pressure` | `PNEUMATICS DUCT PRESSURE` | `perEngine: false` (sem índice) | No sistema pneumático modular do MSFS 2024, exige o identificador do Pack ou duto (`:index` ou `:'Pack_1'_n`). | `PNEUMATICS DUCT PRESSURE:index` ou nome do componente. | [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) |
| `pneu-pack-status` | `PNEUMATICS PACK STATUS` | `perEngine: false` (sem índice) | Exige o identificador do Pack (`:index` ou `:'Pack_1'_n`). | `PNEUMATICS PACK STATUS:index` ou `:'Pack_1'_n`. | [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) |

---

## 4. Incompatibilidade ou Divergência de Unidades (`sensor_registry.json`)

| Chave (`key`) | SimVar | Unidade no Registro | Unidade Canônica no SDK | Impacto Técnico | Correção Recomendada | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|---|
| `pneu-pack-status` | `PNEUMATICS PACK STATUS` | `bool` | `percent over 100` | O SDK define esta variável como a vazão/fluxo de ar normal do pack em formato percentual fracionário (0.00 a 1.00), e não como booleano ligado/desligado. | Alterar unidade para `percent over 100`. | [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) |
| `elec-battery-estimated-capacity-pct` | `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT` | `percent over 100` | `Percent` | O SDK documenta a capacidade relativa de bateria em escala de 0 a 100 (`Percent`). Pedir como `percent over 100` força o SimConnect a dividir por 100 ou falhar a conversão dependendo da precisão. | Alinhar unidade para `Percent`. | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) |
| `gear-skidding-factor` | `GEAR SKIDDING FACTOR` | `percent` | `Percent Over 100` | O SDK documenta o fator de derrapagem como valor entre 0.00 e 1.00 (`Percent Over 100`). Pedir como `percent` faz o SimConnect devolver valor multiplicado por 100 (0 a 100). | Alinhar unidade para `percent over 100` para manter consistência com cálculos físicos de atrito. | [Aircraft Brake and Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) |
| `eng-max-cht`, `eng-max-egt`, `oil-max-temperature`, `turb-max-itt` | `RECIP MAX CHT`, `MAX EGT`, `MAX OIL TEMPERATURE`, `TURB MAX ITT` | `celsius` | `Rankine` | A unidade nativa especificada na documentação do SDK para todas as constantes de aquecimento do `engines.cfg` é `Rankine`. O SimConnect converte automaticamente para `celsius`. | Documentar que a conversão é realizada pela API SimConnect. | [Aircraft Engine Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/) |

---

## 5. Variáveis Legadas (FSX / MSFS v1) com Substitutas Modernas no MSFS 2024

| Chave (`key`) | SimVar Cadastrada | Status no MSFS 2024 | Limitação da Versão Legada | Substituta Moderna no MSFS 2024 | Vantagem da Substituta no 2024 | Referência Oficial SDK MSFS 2024 |
|---|---|---|---|---|---|---|
| `elec-total-load-amps` | `ELECTRICAL TOTAL LOAD AMPS` | Legada (FSX) | Reflete apenas a soma simplificada do modelo elétrico antigo. Em aeronaves com sistema modular (`systems.cfg - [ELECTRICAL]`), retorna valores estáticos ou zerados. | `ELECTRICAL BUS AMPS:index` ou `ELECTRICAL GENALT BUS AMPS:index` | Mede o fluxo de corrente real em cada barramento e alternador individualmente. | [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/) |
| `hyd-pressure`, `hyd-reservoir-percent` | `HYDRAULIC PRESSURE`, `HYDRAULIC RESERVOIR PERCENT` | Legada | Medição de sistema hidráulico clássico único e centralizado. | `HYDRAULIC SYSTEM INTEGRITY:index` e circuitos modulares | Permite acompanhar a degradação de linhas hidráulicas independentes. | [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/) |
| `af-airspeed-barber-pole` | `AIRSPEED BARBER POLE` | Legada | Valor visual do velocímetro analógico. | `REFERENCE SPEED MAX IAS` e `MACH MAX OPERATE` | Representa diretamente as velocidades de projeto aerodinâmico ($V_{MO}/M_{MO}$) do modelo de voo. | [Aircraft Flight Model Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/) |

---

## 6. Context Registry (`context_registry.json`)

| Chave (`key`) | SimVar | Unidade | Análise no SDK 2024 | Observação Técnica |
|---|---|---|---|---|
| `ctx-new-electrical-system` | `NEW ELECTRICAL SYSTEM` | `bool` | Válida (SDK MSFS 2020/2024). | Retorna `TRUE` se a aeronave utiliza a arquitetura modular moderna de `systems.cfg`. Fundamental para condicionar se regras elétricas avançadas devem rodar. |
| `ctx-new-fuel-system` | `NEW FUEL SYSTEM` | `bool` | Válida (SDK MSFS 2020/2024). | Retorna `TRUE` se a aeronave utiliza `[FUEL_SYSTEM]`. |
| `ctx-simulation-rate` | `SIMULATION RATE` | `number` | Válida (Environment Variables). | Retorna a taxa de aceleração temporal (0.0625 a 128.0). |
| `ctx-surface-condition` | `SURFACE CONDITION` | `enum` | Divergência de tipo com `SimTelemetry` (`Number`). | No SDK é um enumerador de condição da pista (Seca, Molhada, Neve, etc.). Ambos são lidos como double no C# sem impacto. |
