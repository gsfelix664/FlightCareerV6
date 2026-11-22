# LIMITES-FALTANTES — Ativar as regras que ficam inativas por falta de dado

**Branch:** feat/limites-faltantes · **Commit:** (pendente de hash) · **Data:** 2026-11-22

## Plano (antes de codar)
1. Implementar detecção dinâmica de barramento elétrico principal (`_detectedMainBusIndex`) e tensão do sistema (`_detectedSystemVolts`) em `PartEventDeriver`:
   - Quando `elecMainBusIndex` não estiver configurado, detectar o primeiro índice k (1..5) com `elec-bus-voltage-k >= 0.5 * systemVolts` enquanto `elec-master-battery == 1` e fixar para o resto do voo, emitindo diagnóstico `"barramento principal detectado: k (x V)"`.
   - Quando `elecSystemVolts` não estiver configurado, detectar a maior tensão vista num barramento com alternador ligado: >= 20 V fixa 28 V, senão 14 V.
   - Enquanto não detectar, as regras de descarga profunda e sobrecarga ficam em espera sem emitir diagnóstico de "desativada".
2. Configurar `alternatorRatedAmps` padrão por classe em `_class_defaults.json` com `source: ClassDefault` e referência de estimativa: pistão 60 A por motor, turboélice 250 A, jato 400 A.
3. Adicionar feature `noAvionicsMaster` em `AircraftFeatures`, atualizar `cessna-c152.json` com a feature e nota em `limits` (aeronave sem chave de aviônicos, regra não se aplica), e atualizar `wear_rules.json` para que `avionics_start_with_avionics_master_on` exclua aeronaves com essa feature (`appliesTo.none: ["noElectrical", "noAvionicsMaster"]`).
4. Completar `va` nos 15 perfis que possuem `vs1` usando a fórmula `Va = Vs1 * sqrt(n)` (n=3.8 para pistão/turboélice leve, n=2.5 para jato/transporte), marcados como `ClassDefault`; listar os 18 perfis sem `vs1`.
5. Atualizar `RuleCoverageService` para considerar regras elétricas ativas via detecção em voo (com anotação), gerar o comparativo Antes / Depois no topo e regerar `docs/plans/cobertura-regras-por-aviao.md`.
6. Criar testes unitários para a detecção de barramento, tensão, descarga/sobrecarga com perfil sem índices e estimativa de Va; rodar build, testes xUnit e fuzz all.

## O que foi feito
- `src/FlightCareer.Domain/Parts/AircraftFeatures.cs`:
  - Adicionada constante `NoAvionicsMaster = "noAvionicsMaster"` e registro na coleção estática `All`.
- `src/FlightCareer.Domain/Parts/PartEventDeriver.cs`:
  - Implementada detecção em voo de barramento e tensão do sistema (`UpdateElectricalDetection`, `DetectedMainBusIndex`, `DetectedSystemVolts`).
  - Quando a bateria está ligada e `elecMainBusIndex` não foi fornecido no perfil, seleciona o primeiro barramento `k` (1..5) com tensão >= 50% de `systemVolts` e emite o diagnóstico `"barramento principal detectado: k (x V)"`.
  - Quando `elecSystemVolts` não foi configurado, infere 28 V se algum barramento com alternador atingir >= 20 V, senão 14 V.
  - As regras `battery_deep_discharge_damage` e `electrical_system_overload_amps` usam os valores detectados e permanecem em espera enquanto não houver detecção (sem acusar falso desativado no diagnóstico).
  - A regra `avionics_start_with_avionics_master_on` não é executada se a aeronave tiver a feature `noAvionicsMaster` ou `noElectrical`.
- `src/FlightCareer.Infrastructure/Data/aircraft/_class_defaults.json`:
  - Adicionado `alternatorRatedAmps` nas classes `Piston` (60 A), `Turboprop` (250 A) e `Jet` (400 A) com `source: ClassDefault` e referência formal de estimativa.
- `src/FlightCareer.Infrastructure/Data/aircraft/cessna-c152.json`:
  - Adicionada feature `noAvionicsMaster`, atualizado `dataVersion` para 7 e incluída nota informativa em `limits` (`elecAvionicsCircuitIndex = 0.0`, POH) indicando que o C152 não possui chave master de aviônicos.
- `src/FlightCareer.Infrastructure/Data/aircraft/*.json` (15 modelos):
  - Adicionado limite `va` calculado via `Va = Vs1 * sqrt(n)` com `source: ClassDefault` e referência documentando a fórmula e estimativa nos perfis: `airbus-a310-300`, `airbus-a320neo`, `airbus-a321`, `airbus-a330-200`, `airbus-a330-beluga`, `airbus-a400m`, `beechcraft-bonanza-g36`, `beechcraft-king-air-350i`, `boeing-747-8i`, `cessna-172-skyhawk-g1000`, `cessna-208b-grand-caravan`, `cirrus-sr22`, `diamond-da62`, `flight-design-ctsl`, `heart-aerospace-es-30`.
- `src/FlightCareer.Infrastructure/Data/wear_rules.json`:
  - Regra `avionics_start_with_avionics_master_on` atualizada para excluir aeronaves com feature `noAvionicsMaster` (`appliesTo.none: ["noElectrical", "noAvionicsMaster"]`).
- `src/FlightCareer.Domain/Replay/Synthetic/FuzzTestDefaults.cs`, `SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs`:
  - Fuzz e oráculo sintético atualizados para respeitar `noAvionicsMaster` (não injetando abuso de partida com aviônicos no C152) e reconhecer a detecção dinâmica de barramento (sem falso positivo de ausência de limite).
- `src/FlightCareer.Infrastructure/Services/RuleCoverageService.cs`:
  - Incluída anotação `(detecção em voo)` para as regras de sobrecarga e descarga profunda quando a aeronave possui gerador/bateria sem índices estáticos no perfil.
  - Implementada geração automática da Seção 0 "Comparativo Antes / Depois (LIMITES-FALTANTES)" no relatório markdown.
- `docs/plans/cobertura-regras-por-aviao.md`:
  - Regenerado com comando CLI `dotnet run --project src/FlightCareer.Replay -- coverage`. Mostra evolução das médias: ativas de 30,3 para 32,6; inativas de 4,5 para 2,2; C152 reduziu regras inativas de 3 para 0.
- `src/FlightCareer.Tests/Parts/ElectricalRuleDeriverTests.cs`:
  - Adicionados 5 novos testes cobrindo detecção do barramento com tensão, bateria desligada mantendo pendência, detecção de sistema 14 V, disparo de descarga profunda sem índices e disparo de sobrecarga sem índices.
- `src/FlightCareer.Tests/Limits/CatalogTests.cs`:
  - Adicionado teste garantindo cálculo determinístico de `va` a partir de `vs1` e preservação dos limites POH do Cessna C152.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Atualizadas as notas técnicas de documentação conforme solicitado.

## Critérios de aceite
- [x] Build 0 erros; `dotnet test` 100 %; fuzz sem regressão — Verificado com `dotnet build` (0 erros), 789 testes xUnit aprovados e 920 voos no fuzz all (46 modelos x 20 runs) sem falhas.
- [x] Nenhum valor POH/SimVar existente alterado; toda estimativa marcada como `ClassDefault` com a fórmula/razão na referência — Verificado via diff em todos os perfis JSON e teste `CatalogTests`.
- [x] Relatório de cobertura regenerado com antes/depois e a lista do que continua sem dado — Verificado no topo de `docs/plans/cobertura-regras-por-aviao.md` e na seção de dúvidas abaixo.
- [x] CHANGELOG e AGENTS.md › 7 atualizados (frase sobre índices elétricos atualizada para "detectados em voo quando ausentes"). Relatório diz "não testado no MSFS".
- [x] Um commit: `feat(limites): barramento eletrico detectado em voo e va/vno completados (LIMITES-FALTANTES)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 19 avisos pré-existentes.
- `dotnet test src/FlightCareer.Tests`: 789 aprovados, 0 falhas (novos: 5 testes em `ElectricalRuleDeriverTests`, 1 teste em `CatalogTests`).
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`: 920/920 voos aprovados (46 modelos, 0 falhas, 0 limites ausentes).

## Não testado
- Não testado no MSFS (comportamento de detecção de barramento em voo interativo dentro do simulador MSFS 2024 / SimConnect).

## Dúvidas e decisões para o revisor
1. **Perfis sem `vs1` mantidos sem `va`:** Conforme a especificação (passo 4: "Sem vs1 no perfil: não estimar; listar no relatório"), os seguintes 18 perfis não possuem `vs1` no catálogo e foram mantidos sem `va`:
   - `at-802`, `boeing-f-a-18e-super-hornet`, `cabri-g2`, `curtiss-jn-4d-jenny`, `dg-aviation-ls8-18`, `douglas-dc-3`, `extra-330lt`, `grumman-g-21a-goose`, `hughes-h-4-hercules`, `jmb-vl-3`, `nemeth-designs-aerolite-103`, `pipistrel-virus-sw-121`, `pitts-special-s1s`, `pitts-special-s2s`, `robinson-r66`, `ryan-nyp-spirit-of-st-louis`, `volocopter-volocity`, `wright-flyer`.
2. **Vno em jatos e Vlo/Vle:** Os jatos existentes no catálogo (`airbus-a310-300`, `airbus-a320neo`, `airbus-a321`, `airbus-a330-200`, `airbus-a330-beluga`, `boeing-747-8i`, etc.) não possuem a chave `vmo` cadastrada no arquivo JSON (possuem `vne` com o valor numérico em nós correspondente a Vmo, e `mmo`). Conforme a orientação da spec ("se existir... nos demais, só com manual"), nenhum valor foi inventado e as regras de speedbrake acima de Vno continuam inativas apenas onde o manual não trouxe Vno/Vmo explicitamente.
