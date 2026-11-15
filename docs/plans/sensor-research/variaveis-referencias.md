# Pesquisa de Variáveis do SDK MSFS 2024 e Projetos de Referência

**Data da pesquisa:** 2026-11-15  
**Autor:** Gemini (Executor) — Tarefa `RESEARCH-SIMVARS`  
**Fontes oficiais:** Documentação Oficial do SDK MSFS 2024 (Retail) — [Programming APIs / SimVars](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/)  
**Repositórios de referência consultados (somente leitura em `C:\PROJETOS\FLIGHTCAREERV6-refs\`):**
- `FsConnect` (c-true, C# / .NET)
- `MobiFlight-Connector` (MobiFlight, C# + WASM)
- `msfs2020-go` (lian, Go)
- `aircraft` (FlyByWire Simulations, Rust / TypeScript / WebAssembly)
- *Nota sobre `fseconomy/fse-simconnect`:* O repositório citado na especificação original não existe no GitHub (clone falhou e não está presente entre as referências).

---

## 1. Tabela de SimVars e Variáveis Candidatas

A tabela abaixo analisa cada uma das variáveis da lista sob investigação, confrontando a documentação oficial do SDK MSFS 2024 com a base de código dos projetos abertos e com o registro interno do FlightCareer (`src/FlightCareer.Infrastructure/Data/sensor_registry.json` e `docs/plans/sensor-inventory.md`).

| Variável | Existe no SDK 2024? (URL) | Obsoleta / Legada? | Unidade | Índice (Nº / Nome) | R/W | Já no Registro FlightCareer? | Evidência em Projeto Aberto | Utilidade para o FlightCareer |
|---|---|---|---|---|---|---|---|---|
| `ENG FAILED:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não (`GENERAL ENG FAILED` coexiste) | `Number` / `Bool` | `:n` (1 a 16) | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:44888`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:423` | Falha crítica de motor; detecção de motor inoperante e perda total. |
| `ENG ON FIRE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `Bool` | `:n` (1 a 16) | R/W (Settable) | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:45616`, `aircraft/fbw-a32nx/src/behavior/src/A32NX_Interior_Fire.xml:56` | Incêndio de motor (atuado pelo app via `SetEngineFire` ou detectado do sim). |
| `GENERAL ENG OIL LEAKED PERCENT:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `Percent` | `:n` (1 a 16) | R/W (Settable) | Sim (`sensor_registry.json`, `sensor-inventory.md` §3) | `FsConnect/data/Aircraft Simulation Variables.html:46904`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:419` | Vazamento progressivo de óleo, retentores e falhas de lubrificação. |
| `GENERAL ENG OIL PRESSURE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `psi` (nativa `psf`) | `:n` (1 a 16) | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §3) | `FsConnect/data/Aircraft Simulation Variables.html:46960`, `aircraft/fbw-a32nx/src/behavior/src/A32NX_Interior_Misc.xml:546` | Bomba de óleo, estresse de mancais/bronzinas, óleo frio na partida. |
| `GENERAL ENG OIL TEMPERATURE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `celsius` (nativa `Rankine`) | `:n` (1 a 16) | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §3) | `FsConnect/data/Aircraft Simulation Variables.html:47016`, `aircraft/fbw-a32nx/src/systems/instruments/src/SD/Pages/Eng/Eng.tsx:327` | Degradação térmica de óleo, superaquecimento e radiador de óleo. |
| `RECIP ENG CYLINDER HEAD TEMPERATURE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `celsius` | `:n` (1 a 16) | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §2) | `FsConnect/data/Aircraft Simulation Variables.html:48528`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:446` | Cilindros, válvulas, shock cooling e sobreaquecimento do motor a pistão. |
| `TURB ENG ITT:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `celsius` (nativa `Rankine`) | `:n` (1 a 16) | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §4) | `FsConnect/data/Aircraft Simulation Variables.html:50319`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:472` | Seção quente de turbina/turboélice, palhetas, estresse por hot start. |
| `ENG EXHAUST GAS TEMPERATURE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não (coexiste com `GENERAL ENG EXHAUST GAS TEMPERATURE:n`) | `celsius` (nativa `Rankine`) | `:n` (1 a 16) | R | Parcial (cadastrada como `GENERAL ENG EXHAUST GAS TEMPERATURE`) | `FsConnect/data/Aircraft Simulation Variables.html:44776` | Válvulas de escape, estresse térmico de exaustão e mistura pobre. |
| `GENERAL ENG DAMAGE PERCENT:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `Percent` | `:n` (1 a 16) | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:46120`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:421` | Detecção de dano catastrófico nativo de motor (>10%). |
| `ELECTRICAL BUS VOLTAGE:name` | Parcial ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)) | Não, mas **sintaxe errada**: SimConnect exige `:index` numérico | `Volts` | Numérico (`:1` a `:N`) | R | Sim (com índice numérico `:1` a `:5`) | Família moderna do New Electrical System | Tensão dos barramentos elétricos, alimentação de aviônicos. |
| `ELECTRICAL BUS AMPS:name` | Parcial ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)) | Não, mas **sintaxe errada**: SimConnect exige `:index` numérico | `Amperes` | Numérico (`:1` a `:N`) | R | Sim (com índice numérico `:1` a `:5`) | Família moderna do New Electrical System | Carga de corrente consumida nos barramentos, detecção de sobrecarga. |
| `LINE BREAKER PULLED:name` | **Não** ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)) | **Inexistente / Inventado** | N/A | N/A | N/A | Não | 0 ocorrências nos projetos abertos | Nenhuma (variável inválida; causa `NAME_UNRECOGNIZED`). |
| `ELECTRICAL BATTERY LOAD:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)) | Não (substitui `ELECTRICAL OLD CHARGIN AMPS`) | `Amperes` (negativo = carga) | `:n` (1 a N) ou sem índice | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §6) | `FsConnect/data/Aircraft Simulation Variables.html:37609`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:586` | Bateria, detecção de descarga profunda, chave master ligada sem alternador. |
| `ELECTRICAL BATTERY VOLTAGE:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)) | Não | `Volts` | `:n` (1 a N) ou sem índice | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §6) | `FsConnect/data/Aircraft Simulation Variables.html:37665`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:587` | Tensão da bateria, saúde da bateria, queda de tensão na partida (crank). |
| `GENERAL ENG MASTER ALTERNATOR:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/)) | Não | `Bool` | `:n` (1 a 16) | R (chave física) | Sim (`sensor_registry.json`, `sensor-inventory.md` §6) | `FsConnect/data/Aircraft Simulation Variables.html:46736`, `aircraft/fbw-a32nx/src/behavior/src/A32NX_Interior_Elec.xml:31` | Monitoramento da posição da chave do alternador. |
| `PARTIAL PANEL AVIONICS` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:18492`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:496` | Pane / apagamento dos displays e instrumentos de aviônicos. |
| `PARTIAL PANEL ELECTRICAL` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:18604`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:498` | Falha elétrica geral do painel de instrumentos. |
| `PARTIAL PANEL PITOT` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:18716`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:500` | Pane de bloqueio do sistema pitot (velocímetro inoperante). |
| `PARTIAL PANEL STATIC` | **Não** ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | **Inexistente no SDK** | N/A | N/A | N/A | **Não** (removido: ver `AGENTS.md` §6) | 0 ocorrências nos projetos abertos | Nenhuma (causou bug real histórico; SimConnect emite exceção e desalinha struct). |
| `PARTIAL PANEL VACUUM` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:18828`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:502` | Pane giroscópica de vácuo (horizonte artificial e giro direcional). |
| `PARTIAL PANEL COMM:n` | Parcial ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não, mas **sintaxe errada**: nome oficial é `PARTIAL PANEL COMM` sem `:n` | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum (`Index: N/A`) | R/W | Não | `FsConnect/data/Aircraft Simulation Variables.html:18548`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:497` | Falha nos rádios de comunicação COM. |
| `PARTIAL PANEL NAV:n` | Parcial ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não, mas **sintaxe errada**: nome oficial é `PARTIAL PANEL NAV` sem `:n` | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum (`Index: N/A`) | R/W | Não | `FsConnect/data/Aircraft Simulation Variables.html:18660`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:499` | Falha nos receptores de navegação NAV/VOR/ILS. |
| `PARTIAL PANEL TRANSPONDER` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Enum` (0=OK, 1=FAIL, 2=BLANK) | Nenhum | R/W | Não | `FsConnect/data/Aircraft Simulation Variables.html:18772`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:501` | Falha do transponder (XPDR inoperante). |
| `HYDRAULIC SYSTEM INTEGRITY:name` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não (sistema modular moderno) | `Percent Over 100` (0=quebrado, 1=novo) | `:name` (Component Name) ou sem índice | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §11) | `FsConnect/data/Aircraft Simulation Variables.html:38449`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:632` | Integridade e saúde de componentes do sistema hidráulico. |
| `HYDRAULIC RESERVOIR PERCENT:name` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Percent Over 100` | `:name` (Reservoir Name) ou `:index` (1-based) | R/W | Sim (`sensor_registry.json`, `sensor-inventory.md` §11) | `FsConnect/data/Aircraft Simulation Variables.html:38393`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:631` | Quantidade de fluido hidráulico no reservatório; vazamento hidráulico. |
| `PNEUMATICS FAN STATUS:name` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não (exclusiva do MSFS 2024 Modular Pneumatics) | `Percent Over 100` | `:name` (Fan Name) ou `:index` | R/W | Não | 0 ocorrências nos projetos abertos legados | Ventilação forçada pneumática / refrigeração de freios / packs em jatos. |
| `GEAR DAMAGE BY SPEED` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/)) | Não | `Bool` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:24023`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:861` | Dano ao trem de pouso causado por ultrapassar $V_{le}$ / $V_{lo}$. |
| `GEAR SPEED EXCEEDED` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/)) | Não | `Bool` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:24654`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:862` | Alerta de sobrevelocidade com trem estendido. |
| `FLAP DAMAGE BY SPEED` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/)) | Não | `Bool` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:13843`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:863` | Dano à estrutura de flaps por exceder a velocidade máxima $V_{fe}$. |
| `FLAP SPEED EXCEEDED` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-control-variables/)) | Não | `Bool` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §5) | `FsConnect/data/Aircraft Simulation Variables.html:13899`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:864` | Alerta de sobrevelocidade operacional com flaps estendidos. |
| `STRUCTURAL ICE PCT` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/)) | Não | `Percent over 100` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §7) | `FsConnect/data/Aircraft Simulation Variables.html:34746`, `aircraft/fbw-a32nx/src/systems/systems-host/systems/FWC/PseudoFWC.ts:4101` | Acúmulo de gelo estrutural em bordo de ataque e célula. |
| `AIRCRAFT STRESS DAMAGE` | **Não** ([SDK](https://docs.flightsimulator.com/msfs2024/retail/)) | **Inexistente como SimVar** (é opção do menu) | N/A | N/A | N/A | Não | 0 ocorrências nos projetos abertos | Nenhuma como SimVar (causa `NAME_UNRECOGNIZED`). Estresse é medido por $G$ ou `WEAR AND TEAR OVERSTRESS DAMAGE RATE`. |
| `G FORCE` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/)) | Não | `GForce` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §10) | `FsConnect/data/Aircraft Simulation Variables.html:17876`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:122` | Estresse estrutural de asa/longarina em manobras e rajadas. |
| `BRAKE LEFT POSITION` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/)) | Não | `Position 32k` ou `Percent over 100` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §9) | `FsConnect/data/Aircraft Simulation Variables.html:12947`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:168` | Posição do pedal de freio esquerdo, trabalho de frenagem e desgaste. |
| `BRAKE RIGHT POSITION` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/)) | Não | `Position 32k` ou `Percent over 100` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §9) | `FsConnect/data/Aircraft Simulation Variables.html:13171`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:169` | Posição do pedal de freio direito, trabalho de frenagem e desgaste. |
| `GROUND VELOCITY` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não | `Knots` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §1) | `FsConnect/data/Aircraft Simulation Variables.html:40769`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:84` | Velocidade no solo para cálculo de energia cinética nos freios e pneus. |
| `CONTACT POINT COMPRESSION:n` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/)) | Não | `Percent` (ou `Position`) | `:n` (0-based) ou `:name` | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §9) | `aircraft/fbw-a32nx/src/wasm/systems/a320_systems_wasm/src/lib.rs:323-325` | Compressão de amortecedores, toque, fim de curso e Weight on Wheels. |
| `VERTICAL SPEED` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-flight-model-variables/)) | Não | `Feet per second` ou `Feet per minute` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §1) | `FsConnect/data/Aircraft Simulation Variables.html:2363` | Razão vertical de afundamento/subida. |
| `SIM ON GROUND` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não | `Bool` | Nenhum | R | Sim (`sensor_registry.json`, `sensor-inventory.md` §1) | `FsConnect/data/Aircraft Simulation Variables.html:42197`, `FsConnect/src/CTrue.FsConnect/FsSimVarFactory.cs:152`, `aircraft/fbw-a32nx/src/base/flybywire-aircraft-a320-neo/SimObjects/AirPlanes/FlyByWire_A320_NEO/model/A320_NEO.xml:557` | Detecção primária de fase de voo (solo vs voo). |
| `COVER ON:0` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:0` (Calços / wheel chocks) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: calços de roda colocados/removidos. |
| `COVER ON:1` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:1` (Capas de motor / cowl plugs) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: capas de admissão/exaustão do motor. |
| `COVER ON:2` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:2` (Capa do tubo de pitot) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: capa de proteção do pitot com flâmula. |
| `COVER ON:3` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:3` (Capas de tomada estática / static ports) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: proteção da tomada estática externa. |
| `COVER ON:4` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:4` (Travas de rotores / helicopter tie-downs) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround em helicópteros e giroplanos. |
| `COVER ON:5` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:5` (Pinos de trem de pouso / gear pins) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: pinos de travamento de solo do trem. |
| `COVER ON:6` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:6` (Amarrações de hélice / propeller ties) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: amarras de hélices em aeronaves a pistão/turbina. |
| `COVER ON:7` | Sim ([SDK](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-miscellaneous-variables/)) | Não (novidade nativa do MSFS 2024) | `Bool` | `:7` (Capa de para-brisa / windshield sun cover) | R/W | Não | Preflight behavior templates do MSFS 2024 | Walkaround: proteção solar do cockpit. |

---

## 2. Nomes Errados ou Inventados na Lista (com Provas)

A investigação identificou 5 nomes incorretos ou inventados presentes na lista fornecida:

1. **`PARTIAL PANEL STATIC` — INEXISTENTE NO SDK DO MSFS**
   - **A prova:** Não consta na documentação oficial de [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/). Não aparece em nenhum dos 4 repositórios de referência (`FsConnect`, `MobiFlight`, `msfs2020-go`, `aircraft`).
   - **Impacto já comprovado no FlightCareer:** O MSFS rejeita o registro com a exceção `SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED` (código 3). Como o SimConnect descarta o campo que falhou, o pacote transmitido fica menor e todos os campos subsequentes na struct da memória são deslocados, corrompendo a telemetria (ver histórico documentado em `AGENTS.md` §6).
   - **Alternativa correta:** O bloqueio da porta estática no MSFS é atuado pelo evento `TOGGLE_STATIC_PORT_BLOCKAGE` (SDK Event ID) e verificado pela SimVar `ALTERNATE STATIC SOURCE OPEN` (`Bool`).

2. **`AIRCRAFT STRESS DAMAGE` — NÃO É SIMVAR (É OPÇÃO DO MENU)**
   - **A prova:** No MSFS 2024, "Aircraft Stress Damage" é o rótulo de uma opção de assistência no menu do simulador (*Settings > Assistances > Failure and Damage > Aircraft Stress Damage*).
   - **Impacto:** Não existe SimVar com esse nome. Qualquer tentativa de passá-la em `AddToDataDefinition` gera `SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED`.
   - **Alternativa correta:** A taxa de sobreestresse estrutural no MSFS 2024 é monitorada via SimVar `WEAR AND TEAR OVERSTRESS DAMAGE RATE:id` (por componente) ou pela aceleração de carga `G FORCE` / `SEMIBODY LOADFACTOR Y`.

3. **`LINE BREAKER PULLED:name` — INVENTADO**
   - **A prova:** Não existe SimVar `LINE BREAKER PULLED` nem `LINE BREAKER PULLED` no SDK 2024 ([Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/)).
   - **Alternativa correta:** As variáveis documentadas pelo SDK para estados de disjuntor são:
     - `CIRCUIT BREAKER PULLED` (exige `BUS_LOOKUP_INDEX` e índice de circuito)
     - `BUS BREAKER PULLED`
     - `BATTERY BREAKER PULLED`
     - `ALTERNATOR BREAKER PULLED`
     - `EXTERNAL POWER BREAKER PULLED`

4. **`ELECTRICAL BUS VOLTAGE:name` e `ELECTRICAL BUS AMPS:name` — SINTAXE DE ÍNDICE INCORRETA NO SIMCONNECT**
   - **A prova:** Na documentação oficial de [Aircraft Electrics Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/), o SimConnect exige **índice numérico** (1 a N, correspondendo a `[ELECTRICAL.BUS.N]` do `systems.cfg`), como `ELECTRICAL BUS VOLTAGE:1` ou `ELECTRICAL BUS AMPS:1`.
   - **Impacto:** O formato textual com nome (`:name`) é suportado internamente apenas em scripts RPN / Model Behaviors via lookup index (`BUS LOOKUP INDEX`), mas não na API C/C# do SimConnect cliente, onde passar strings alfanuméricas causa erro de parser no simulador.

5. **`PARTIAL PANEL COMM:n` e `PARTIAL PANEL NAV:n` — NÃO POSSUEM ÍNDICE**
   - **A prova:** Conforme documentado em [Aircraft System Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/), o campo de índice para essas variáveis é estritamente `Index: N/A`. O nome oficial é `PARTIAL PANEL COMM` e `PARTIAL PANEL NAV`.
   - **Impacto:** Passar `:1` ou `:2` pode ser rejeitado como nome não reconhecido pelo SimConnect.

---

## 3. Como os Projetos Abertos Fazem

### 3.1. Leitura de LVars (MSFS 2020 vs MSFS 2024)
- **No MSFS 2020 (MobiFlight):**
  Como a DLL do SimConnect clássica não possuía suporte nativo à leitura direta de `L:Vars`, o MobiFlight desenvolveu um módulo intermediário em C++ compilado para WebAssembly (`mobiflight-event-module`), instalado na pasta `Community`.
  O cliente C# conecta-se ao WASM através de *Client Data Areas* do SimConnect (`MapClientDataNameToId`, `CreateClientData`, `SetClientData` em `WasmModuleClient.cs:48-55`), enviando comandos textuais como `MF.LVars.List` e recebendo o valor das variáveis que o módulo lê internamente no gauge via `check_named_variable` / `get_named_variable_value`.
- **No MSFS 2024 (Suporte Nativo):**
  A documentação oficial do SDK MSFS 2024 ([SimConnect_AddToDataDefinition](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect-api/simconnect-api-reference/simconnect_addtodatadefinition/)) confirma oficialmente que **é possível ler e escrever LVars diretamente via SimConnect**, sem necessidade de qualquer módulo WASM:
  ```cpp
  SimConnect_AddToDataDefinition(hSimConnect, DefineID, "L:VARIABLE_NAME", "number", SIMCONNECT_DATATYPE_FLOAT64);
  ```
  - **Regras do SDK 2024 para LVars via SimConnect:**
    1. O tipo de dado é estritamente `SIMCONNECT_DATATYPE_FLOAT64`.
    2. A unidade especificada deve ser `"number"` (ou compatível com a conversão).
    3. O escopo é compartilhado globalmente entre clientes da sessão.

### 3.2. Toque e Pouso (Weight on Wheels / Contact Points)
- No repositório `aircraft` (FlyByWire A320), o toque no solo e o estado de *Weight on Wheels* (WoW) são orquestrados pela unidade LGCIU (*Landing Gear Control and Interface Unit*, `LgciuWeightOnWheels`).
- Para alimentar a física e a lógica dos computadores de voo (FAC, SEC, FWC), o FBW lê diretamente a compressão dos amortecedores por ponto de contato:
  `CONTACT POINT COMPRESSION` nos índices 0 (bequilha), 1 (trem principal esquerdo) e 2 (trem principal direito), conforme registrado em `fbw-a32nx/src/wasm/systems/a320_systems_wasm/src/lib.rs:323-325`.
- A compressão varia proporcionalmente à carga estática e dinâmica absorvida pelo trem.

### 3.3. Física de Freios e Hidráulica (Modelagem FlyByWire)
O FlyByWire modela freios e hidráulica em Rust/WASM com física profunda de primeiros princípios (`fbw-common/src/wasm/systems/systems/src/hydraulic/brake.rs`):
- **Trabalho e Calor de Frenagem:**
  O aumento de temperatura é computado pela energia dissipada pelo atrito:
  $$\text{Força} = \text{Pressão do Atuador Hidráulico} \times \text{Área do Pistão}$$
  $$\text{Energia (Joule)} = \text{Força} \times \text{Distância Percorrida pelo Disco} (\text{Velocidade da Roda} \times \Delta t)$$
  $$\Delta \text{Temperatura} = \frac{\text{Energia}}{\text{Capacidade Térmica do Disco de Carbono (1420 J/kg}\cdot\text{K)}}$$
- **Arrefecimento Realista:**
  Modela a perda de calor por radiação térmica (lei de Stefan-Boltzmann com emissividade 0.71) e convecção aerodinâmica (o coeficiente de convecção dobra quando o trem está baixado e exposto ao vento relativo, e quadruplica quando o ventilador de freio `BrakeFan` está ligado).
- **Modelo de Hidráulica:**
  Em `a320_systems/src/hydraulic/mod.rs`, os circuitos Verde, Azul e Amarelo possuem reservatórios modelados em litros reais (ex.: 23 L nominal, 18 L utilizável), bombas mecânicas ligadas ao motor (EDP), bombas elétricas, PTU de transferência mecânica de pressão sem mistura de óleo, e válvulas com detecção de vazamento (`leak_measurement_valve`).

### 3.4. Armadilhas Documentadas nos Projetos Abertos
1. **Ruído residual em rodas paradas:**
   Em `brake.rs:71` do FBW há o aviso explícito:
   `// For some reason the wheels do rotate a bit even when the plane is not moving.`
   As variáveis `WHEEL RPM` e de velocidade angular de roda possuem jitter/ruído no MSFS mesmo com a aeronave imóvel com freio de estacionamento acionado. A verificação de movimento precisa filtrar rotações inferiores a $0.1\text{ RPM}$.
2. **`GEAR TOTAL PCT EXTENDED` obsoleta no MSFS 2024:**
   O SDK 2024 marca `GEAR TOTAL PCT EXTENDED` como obsoleta e avisa que ela retorna permanentemente `0` em modelos modernos. O status do trem deve ser consultado via `GEAR ANIMATION POSITION:index` ou pela compressão dos contact points.
3. **Flap handle positions e contagem do zero:**
   `FLAPS NUM HANDLE POSITIONS` conta unicamente os dentes estendidos (ex.: retorna 3 para o C152 com posições 10°, 20°, 30°), não contabilizando o dente zero (recolhido).

---

## 4. Ferramentas de Conferência ao Vivo

Para que o usuário ou desenvolvedor possa testar e validar o comportamento de variáveis candidatas sem necessidade de modificar código ou recompilar o FlightCareer, o ambiente oficial do MSFS disponibiliza duas ferramentas fundamentais: o **SimConnect Inspector** (integrado no Developer Mode) e o **SimVarWatcher** (aplicativo utilitário fornecido como amostra do SDK).

### 4.1. SimConnect Inspector (Developer Mode do MSFS 2024)
O SimConnect Inspector é a ferramenta diagnóstica nativa embutida no simulador, essencial para inspecionar clientes conectados e diagnosticar erros de protocolo em tempo real.

- **Como acessar:**
  1. No menu do MSFS, acesse *General Options > Developers* e ative **Developer Mode** (*On*).
  2. Na barra superior do DevMode, clique no menu **Tools** e selecione **SimConnect Inspector**.
- **Abas principais e diagnósticos:**
  - **Proxies:** Lista todos os clientes SimConnect ativos (ex.: `FlightCareer.UI`, drivers de hardware, utilitários).
  - **Exceptions (A mais crítica):** Exibe qualquer exceção disparada pelo cliente SimConnect em tempo real. Se uma SimVar com nome incorreto for registrada, a exceção `SIMCONNECT_EXCEPTION_NAME_UNRECOGNIZED` (código 3) aparecerá imediatamente nesta aba, identificando a variável rejeitada antes que ela corrompa structs.
  - **Data Definition:** Exibe a lista completa de datums registrados por cada definição (`DEFINITIONS`), permitindo verificar o tamanho total em bytes e a ordem exata dos campos na struct.
  - **Frames:** Rastreia as transmissões frame a frame, indicando a taxa de entrega de pacotes e eventuais descartes de telemetria.
  - **Statistics:** Exibe métricas de largura de banda, contagem de mensagens e latência da conexão IPC.

### 4.2. SimVarWatcher (SDK Sample Project)
O SimVarWatcher é um cliente SimConnect executável independente desenvolvido pela própria Asobo/Microsoft em C# (localizado em `[MSFS_SDK]\Samples\VisualStudio\SimvarWatcher\`). Ele permite consultar ou escrever qualquer SimVar em tempo de execução sem abrir o código-fonte de add-ons.

- **Principais funcionalidades:**
  - **Seleção de Objeto:** Permite escolher o objeto alvo (`User Aircraft`, tráfego IA ou SimObject específico).
  - **Busca e Teste de Variáveis:** Digite o nome da SimVar com seu respectivo índice (ex.: `COVER ON:2`, `CONTACT POINT COMPRESSION:1`, `TURB ENG ITT:1`).
  - **Seleção Flexível de Unidades:** Permite testar diferentes unidades aceitas (ex.: `celsius` vs `Rankine`, `Percent` vs `Percent Over 100`) para verificar a conversão automática do simulador.
  - **Watchlist em Tempo Real:** Atualiza continuamente o valor na tela com taxa configurável.

### 4.3. Passo a Passo Rápido: Como o Usuário Confere uma Variável sem o FlightCareer
1. **Iniciar o simulador** e carregar o voo com a aeronave de interesse no pátio (motores desligados / *cold & dark*).
2. **Abrir o SimVarWatcher:** Execute o executável compilado do SimVarWatcher (`Simvars.exe`).
3. **Conectar ao Simulador:** Clique no botão **Connect**. O status no rodapé mudará para *Connected to Flight Simulator*.
4. **Adicionar a Variável:**
   - No campo **SimVar**, insira o nome exato (ex.: `COVER ON:0` ou `ELECTRICAL BUS VOLTAGE:1`).
   - No campo **Unit**, selecione a unidade documentada (ex.: `Bool` ou `Volts`).
   - Clique em **Add to Watchlist**.
5. **Acionar o Sistema no Simulador:** Execute a ação física no cockpit ou no solo (ex.: ligar a Master Switch da bateria, puxar o freio de estacionamento, remover a flâmula do pitot).
6. **Validar a Resposta:** Observe a coluna *Value* no SimVarWatcher. Se o valor variar de forma síncrona com a ação sem disparar exceção no SimConnect Inspector, a variável está validada para inclusão no `sensor_registry.json`.

---

## 5. Candidatas para o Próximo Teste de Solo

Abaixo estão listadas as **20 variáveis candidatas prioritárias**, selecionadas pelo seu alto valor para o sistema de inspeção pré-voo, desgastes operacionais e falhas do FlightCareer, ordenadas por valor decrescente e com o passo de teste que as valida em solo:

1. `COVER ON:0` (Calços de roda / Chocks)
   - *Passo do teste:* No MSFS 2024, em modo pré-voo no pátio, colocar os calços de roda na bequilha/trens → `COVER ON:0` = 1; remover os calços durante a inspeção pré-voo → `COVER ON:0` = 0.
2. `COVER ON:1` (Capas de motor / Cowl plugs)
   - *Passo do teste:* Na inspeção externa, remover as capas de entrada de ar do motor → `COVER ON:1` transita de 1 para 0.
3. `COVER ON:2` (Capa do tubo de pitot)
   - *Passo do teste:* No walkaround, remover a capa com flâmula do pitot → `COVER ON:2` transita de 1 para 0.
4. `COVER ON:3` (Capas das tomadas estáticas / Static ports)
   - *Passo do teste:* No walkaround externo, remover as proteções das portas estáticas → `COVER ON:3` transita de 1 para 0.
5. `L:IS_AVATAR` (Modo Avatar de Walkaround)
   - *Passo do teste:* Sair da cabine para realizar a inspeção externa caminhando ao redor do avião → `L:IS_AVATAR` vai para 1; retornar ao assento do piloto → `L:IS_AVATAR` vai para 0.
6. `CONTACT POINT COMPRESSION:0` (Compressão da bequilha / nose gear)
   - *Passo do teste:* No solo, aplicar frenagem forte durante o táxi a 10 kt → compressão da bequilha sobe significativamente devido à transferência de peso à frente.
7. `CONTACT POINT COMPRESSION:1` (Compressão do trem principal esquerdo)
   - *Passo do teste:* No táxi, efetuar curva fechada para a esquerda com pedal e freio diferencial esquerdo → amortecedor esquerdo sofre compressão sob a força centrífuga/peso.
8. `CONTACT POINT COMPRESSION:2` (Compressão do trem principal direito)
   - *Passo do teste:* No táxi, efetuar curva fechada para a direita → compressão no ponto 2 sobe.
9. `BRAKE LEFT POSITION` (Posição do freio esquerdo)
   - *Passo do teste:* Pressionar o pedal esquerdo de freio a fundo no solo → valor sobe de 0 a 32767 (ou de 0.0 a 1.0).
10. `BRAKE RIGHT POSITION` (Posição do freio direito)
    - *Passo do teste:* Pressionar o pedal direito de freio a fundo no solo → valor sobe de 0 a 32767.
11. `CIRCUIT BREAKER PULLED:1` (Disjuntores)
    - *Passo do teste:* Em aeronave com cockpit de disjuntores operáveis (ex.: C172 G1000), puxar o disjuntor do Pitot Heat ou Avionics → valor muda para 1 (true).
12. `ELECTRICAL BUS VOLTAGE:1` (Tensão do barramento principal)
    - *Passo do teste:* Com motor desligado, ligar a Master Switch da bateria → tensão sobe de ~0V para ~24V-25V; dar a partida e ligar o alternador → sobe para ~28V.
13. `ELECTRICAL BUS AMPS:1` (Corrente do barramento)
    - *Passo do teste:* Ligar Taxi Lights, Landing Lights e Pitot Heat simultaneamente → corrente do barramento 1 sobe em degraus mensuráveis.
14. `ELECTRICAL BATTERY LOAD:1` (Carga/descarga da bateria)
    - *Passo do teste:* Ligar todos os consumidores com motor desligado → leitura negativa acentuada (descarga); acelerar motor com alternador em ON → transita para positivo/neutro (recarga).
15. `ELECTRICAL BATTERY VOLTAGE:1` (Tensão da bateria)
    - *Passo do teste:* Monitorar a tensão no instante do acionamento do starter elétrico (*cranking*) → observar queda transitória da tensão sob o torque de partida.
16. `PNEUMATICS FAN STATUS:1` (Ventilação pneumática / ECS)
    - *Passo do teste:* Em aeronave compatível com o sistema modular pneumático do MSFS 2024, acionar o interruptor de ventilação/fan no painel → variável vai de 0.0 para 1.0.
17. `HYDRAULIC RESERVOIR PERCENT:1` (Nível do reservatório hidráulico)
    - *Passo do teste:* Em aeronave com sistema hidráulico, acionar flaps ou comandos de voo no solo e monitorar o nível de fluido.
18. `HYDRAULIC SYSTEM INTEGRITY:1` (Integridade hidráulica)
    - *Passo do teste:* Monitorar a integridade nativa (1.0 = íntegro) no solo em aeronave com subsistema hidráulico ativo.
19. `STRUCTURAL ICE PCT` (Gelo estrutural)
    - *Passo do teste:* No menu de clima do MSFS, configurar temperatura de -5°C com precipitação máxima e congelamento → taxa de gelo sobe de 0 a 100%.
20. `PARTIAL PANEL PITOT` (Falha do velocímetro/pitot)
    - *Passo do teste:* No menu de panes do MSFS, acionar a falha de pitot → a variável muda para 1 (`FAIL`), comprovando a detecção da pane de velocímetro.
