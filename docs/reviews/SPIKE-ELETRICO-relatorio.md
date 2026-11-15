# SPIKE-ELETRICO — SimVars elétricas certas no MSFS 2024

**Branch:** docs/spike-sensores-eletricos · **Commit:** 8faa027 (e commit seguinte de docs) · **Data:** 2026-11-15

## Plano (antes de codar)
- Pesquisar na documentação oficial do SDK MSFS 2024 (`Aircraft Electrics Variables`, `Aircraft Engine Variables` e `systems.cfg [ELECTRICAL]`) as SimVars corretas para master de aviônicos, tensão/carga da bateria e alternador/gerador.
- Identificar as causas-raiz pelas quais `AVIONICS MASTER SWITCH`, `ELECTRICAL BATTERY VOLTAGE` e `ELECTRICAL GENALT BUS *` apresentaram valores estáticos ou zerados no C172 G1000.
- Documentar em `docs/plans/sensor-validation/spike-eletrico-2024.md` a matriz de candidatos, hipóteses fundamentadas, recomendações para as 3 regras mudas e riscos de compatibilidade.
- Adicionar 16 novos sensores candidatos em `sensor_registry.json` e `sensor-inventory.md`, mantendo paridade testada e sem alterar os sensores existentes.
- Incluir no `ROTEIRO.md` o checklist objetivo do teste elétrico e leme no C172 G1000 com o comportamento esperado por coluna.

## O que foi feito
- `docs/plans/sensor-validation/spike-eletrico-2024.md`: documento completo do spike com todas as URLs oficiais do SDK MSFS 2024, diagnóstico do C172 G1000, matriz de candidatos em ordem de preferência, recomendações para as regras `avionics_start_with_avionics_master_on`, `battery_deep_discharge_damage` e `electrical_system_overload_amps`, além de análise de riscos (MSFS 2020 vs 2024 e aeronaves de terceiros).
- `src/FlightCareer.Infrastructure/Data/sensor_registry.json`: adicionados 16 novos candidatos elétricos (`elec-avionics-master-switch-1/2`, `elec-avionics-bus-voltage`, `elec-avionics-bus-amps`, `elec-circuit-switch-on-1/2`, `elec-circuit-on-1/2`, `elec-battery-voltage-1`, `elec-battery-load-1`, `elec-battery-bus-voltage`, `elec-hot-battery-bus-voltage`, `elec-main-bus-voltage-1/2`, `elec-main-bus-amps-1`, `elec-alternator-connection-on-1`).
- `docs/plans/sensor-inventory.md`: adicionadas as 16 SimVars correspondentes na seção 6 (Elétrico), mantendo a paridade estrita testada pelo teste automatizado `SensorRegistryTests`.
- `src/FlightCareer.Tests/Sensors/SensorLayoutTests.cs`: atualizadas as contagens de sensores globais e slots de blocos (378 -> 394 sensores no registro, slots para 1 e 4 motores).
- `docs/plans/sensor-validation/ROTEIRO.md`: adicionada a seção 7 com o passo a passo objetivo para o usuário voar o teste elétrico e comandos no C172 G1000 (~5 min em solo/pátio) e tabela de valores esperados por coluna.
- `docs/CHANGELOG.md`: registrada entrada cronológica no fim do arquivo.

## Critérios de aceite
- [x] Todo nome novo tem URL da documentação oficial no doc do spike; nada inventado. — Confirmado no arquivo `spike-eletrico-2024.md`.
- [x] Registro e inventário em paridade; nenhum sensor existente alterado. — Verificado com `SensorRegistryTests` (13/13 aprovados).
- [x] Roteiro de teste pronto para o usuário, com o resultado esperado por coluna. — Adicionado na seção 7 de `ROTEIRO.md`.
- [x] Entrada no fim de `docs/CHANGELOG.md` (`## 2026-11-15 - Spike sensores elétricos MSFS 2024 (Gemini)`). — Adicionada.
- [x] Relatório diz explicitamente: "não testado no MSFS" e o que o usuário precisa voar. — Declarado abaixo.
- [x] Commits `docs(sensors): ...` / `feat(sensors): ...` em português, na branch da tarefa. Sem push. — Realizado.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 13 avisos conhecidos pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 264 aprovados, 0 falhas (tempo ~1 s).
- `python -m unittest discover -s tools/tests`: 40 aprovados, 0 falhas.

## Não testado
- **Não testado no MSFS:** Como se trata de um spike de pesquisa estático e preparação de telemetria, as SimVars novas ainda não foram lidas pelo SimConnect em sessão ao vivo do MSFS 2024.
- **O que o usuário precisa voar:** Executar o teste em solo no C172SP G1000 conforme o checklist da seção 7 de `ROTEIRO.md` (REC; bateria ON motor parado 2 min com aviônicos ON; aviônicos OFF 30 s com Marcar; partida; alternador OFF 60 s com Marcar; alternador ON; mexer pedais do leme; REC OFF).

## Dúvidas e decisões para o revisor
- Os sensores foram adicionados como candidatos experimentais em `sensor_registry.json`. Nenhuma regra de desgaste em `wear_rules.json` ou lógica em `Domain/Parts/` foi modificada, preservando isolamento completo com as tarefas em andamento da F2.4.
- Quando o usuário gravar o CSV do teste do C172 G1000, analisaremos quais dos candidatos responderam com maior precisão e limparemos os candidatos redundantes antes de ligar as 3 regras.
