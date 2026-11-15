# SPIKE-ELETRICO — SimVars elétricas certas no MSFS 2024

**Branch:** docs/spike-sensores-eletricos · **Commits:** 27503af, 0c2a588 (r2) · **Data:** 2026-11-15

---

## 1. Resumo da Rodada 1
- Investigação inicial das variáveis elétricas no C172SP G1000.
- Adição de 16 candidatos preliminares no registro e no inventário.
- Criação do documento `docs/plans/sensor-validation/spike-eletrico-2024.md` e do checklist na seção 7 de `ROTEIRO.md`.
- Veredito da revisão: **CORRIGIR** (9 itens apontando predominância do sistema modular elétrico v2, URLs oficiais retail e eliminação de variáveis legadas/obsoletas do FSX).

---

## 2. Correções Aplicadas na Rodada 2 (Revisão CORRIGIR — 9 Itens)

| Item da Revisão | O que Foi Feito |
|---|---|
| **1. URLs da documentação oficial retail** | Todas as URLs do documento `spike-eletrico-2024.md` foram substituídas pela documentação retail do MSFS 2024 e testadas via HTTP (todas responderam com status 200 OK):<br>• `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/`<br>• `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-system-variables/`<br>• `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-engine-variables/`<br>• `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/simulation-variables/`<br>• `https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/events-and-data/simconnect_addtodatadefinition/` |
| **2. Tabelas da §3 com coluna v2 / v1.00 / FSX** | As tabelas da seção 3 foram totalmente reconstruídas com a coluna **Sistema SDK (v2+ / v1.00 / FSX)** extraída da documentação. As variáveis modernas v2+ (`ELECTRICAL BUS VOLTAGE`, `ELECTRICAL BUS AMPS`, `CIRCUIT ON`, `ELECTRICAL BATTERY VOLTAGE:1`, `ELECTRICAL BATTERY LOAD:1`) foram promovidas à 1ª prioridade. Variáveis legadas/obsoletas (`ELECTRICAL AVIONICS BUS VOLTAGE`, `ELECTRICAL MAIN BUS VOLTAGE`, `ELECTRICAL BATTERY BUS VOLTAGE`, `ELECTRICAL HOT BATTERY BUS VOLTAGE`) foram rebaixadas para fallback/legado com a justificativa oficial do SDK. |
| **3. Candidatos no registro (máximo 16 datums)** | Os 16 candidatos adicionados na branch foram reconfigurados em `sensor_registry.json` e `sensor-inventory.md` priorizando a arquitetura v2+:<br>• `elec-bus-voltage-1` a `5` (`ELECTRICAL BUS VOLTAGE:1..5`) [5]<br>• `elec-bus-amps-1` a `5` (`ELECTRICAL BUS AMPS:1..5`) [5]<br>• `elec-circuit-on-1` a `3` (`CIRCUIT ON:1..3`) [3]<br>• `elec-battery-voltage-1` (`ELECTRICAL BATTERY VOLTAGE:1`) [1]<br>• `elec-battery-load-1` (`ELECTRICAL BATTERY LOAD:1`) [1]<br>• `elec-main-bus-voltage-1` (`ELECTRICAL MAIN BUS VOLTAGE:1`) [1]<br>Total: exatamente **16 novos datums**. Foram removidos `ELECTRICAL HOT BATTERY BUS VOLTAGE` e `ALTERNATOR CONNECTION ON:1`. Apenas `ELECTRICAL MAIN BUS VOLTAGE:1` foi mantida como controle de compatibilidade com aeronaves legadas v1.00/FSX. |
| **4. Formato por nome no SimConnect (`AddToDataDefinition`)** | Adicionada a seção 3.5 em `spike-eletrico-2024.md` detalhando a pesquisa. A sintaxe `:'<NAME>'_n` é documentada apenas para gauges RPN/XML em `simulation-variables/#using-names-instead-of-indices`. Na documentação da API SimConnect (`SimConnect_AddToDataDefinition`), o parâmetro `DatumName` aceita exclusivamente nomes seguidos de índice numérico (`:index`). O suporte a nomes com aspas é **não documentado** no SimConnect e acarreta risco de exceção `NAME_UNRECOGNIZED` (corrompendo a struct de telemetria). Definido o uso exclusivo de índices numéricos globais. |
| **5. Descoberta de barramentos no voo de teste** | Adicionada a seção 3.6 em `spike-eletrico-2024.md` e atualizada a seção 7 de `ROTEIRO.md`. A topologia dos barramentos é identificada pelo cruzamento temporal com os marcadores de evento: o barramento que cai a 0,0 V / 0,0 A em `teste-avionics-off` é o **Avionics Bus**; os barramentos que transitam de ~28 V para ~24 V em `teste-alternador-off` são os **Main Buses**. |
| **6. `AVIONICS MASTER SWITCH` no 2024** | Localizada na documentação de `aircraft-system-variables`. No v2+, aceita nome ou índice de circuito específico de aviônicos. Como a documentação de elétricos recomenda explicitamente monitorar a tensão do barramento de aviônicos (`ELECTRICAL BUS VOLTAGE`), foram removidas as variáveis indexadas `:1` e `:2` do registro para evitar falsas premissas de mapeamento. |
| **7. Diagnóstico na §2 (Observado vs Hipótese)** | A seção 2 foi reescrita separando categoricamente os **valores observados no CSV** (`20261115-1534-C172SP_G1000_Passengers.csv`) das **hipóteses técnicas** amparadas na documentação oficial v2. Afirmações sem suporte documental (ex.: tensão de circuito aberto sem índice) foram eliminadas. |
| **8. Recomendações da §4 com base no v2+** | Recomendações atualizadas:<br>• `avionics_start_with_avionics_master_on`: monitora a presença de tensão (> 5 V) e corrente no barramento de aviônicos identificado no teste.<br>• `battery_deep_discharge_damage`: monitora `ELECTRICAL BATTERY LOAD:1` (> 2 A em descarga quando fornecedora) e a tensão de barramento da bateria.<br>• `electrical_system_overload_amps`: compara `ELECTRICAL TOTAL LOAD AMPS` com a capacidade nominal do alternador (`RatedAlternatorAmps`). |
| **9. Sincronização, testes e CHANGELOG** | Paridade estrita mantida entre `sensor_registry.json` e `sensor-inventory.md` (testada via `SensorRegistryTests`). Contagens de layout mantidas em 394 sensores (`SensorLayoutTests`). Registro atualizado no `docs/CHANGELOG.md`. |

---

## 3. Critérios de Aceite
- [x] Todo nome novo tem URL da documentação oficial no doc do spike; nada inventado. — URLs retail oficiais testadas com HTTP 200.
- [x] Registro e inventário em paridade; nenhum sensor existente alterado. — Validado com `SensorRegistryTests` (13/13 testes aprovados).
- [x] Roteiro de teste pronto para o usuário com explicação de descoberta de barramentos e comportamento esperado por coluna. — Atualizado na seção 7 de `ROTEIRO.md`.
- [x] Entrada no fim de `docs/CHANGELOG.md`. — Atualizada com os ajustes da rodada 2.
- [x] Relatório diz explicitamente: "não testado no MSFS" e o que o usuário precisa voar. — Declarado na seção 5.
- [x] Commits em português na branch `docs/spike-sensores-eletricos`. Sem push.

---

## 4. Build e Testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 13 avisos conhecidos pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 264 aprovados, 0 falhas (duração ~1 s).
- `python -m unittest discover -s tools/tests`: 40 aprovados, 0 falhas.

---

## 5. Não Testado
- **Não testado no MSFS:** As novas SimVars modular v2 ainda não foram capturadas em sessão em tempo real do simulador.
- **O que o usuário precisa voar:** Executar o teste de solo no C172SP G1000 conforme a seção 7 de `ROTEIRO.md` (~5 minutos: REC; bateria ON com aviônicos ON 2 min; aviônicos OFF 30 s com Marcar; partida; alternador OFF 60 s com Marcar; alternador ON; leme; REC OFF).

---

## 6. Dúvidas e Decisões para o Revisor
- O uso de índices globais 1 a 5 para barramentos (`ELECTRICAL BUS VOLTAGE:1..5` e `ELECTRICAL BUS AMPS:1..5`) e 1 a 3 para circuitos (`CIRCUIT ON:1..3`) cobre com margem a topologia típica de monomotores leves como o C172SP G1000.
- Com o log que o usuário gravar, identificaremos qual índice corresponde exatamente a Main Bus 1, Main Bus 2, Avionics Bus e Battery Bus, permitindo consolidar os sensores definitivos antes de ativar as regras em F2.4.
