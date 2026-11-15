# FIX-PARTS-SENSORS — Regras do motor por motor, multimotor e diagnóstico único

**Branch:** fix/parts-sensores-por-motor · **Commit:** 7655f59, c2dc4b8 · **Data:** 2026-11-15

## Plano (antes de codar)
- 1. Resolução de sensores por motor no PartWearEngine: detectar se sensor é perEngine no frame (tentando chave exata; senão chave:n) e detectar engineCount pelo maior índice n presente no frame.
- 2. Avaliação e estresse por motor: avaliar regras perEngine para cada motor 1..engineCount, aplicando estresse nas peças (PartId, n). Para peças (PartId, 0), aplicar o pior estresse entre os motores, sem somar. Rastrear duração, pico, inspeções e excedências por chave "<ruleId>#<n>".
- 3. Suporte multimotor em ApplyTick e ApplyEvent: trocar indexação/dicionários por PartId para (PartId, EngineIndex) e aplicar eventos a todas as peças correspondentes sem lançar ArgumentException.
- 4. Deduplicação de diagnósticos no PartFlightTracker: manter conjunto de diagnósticos já reportados no voo (zerado em Begin/Reset) e citar chave tentada ('eng-rpm' (nem 'eng-rpm:1')).
- 5. Testes unitários e teste de contrato: implementar os testes exigidos na spec (motor único com :1, multimotor, deduplicação de diagnósticos, contrato regras x registro de layout real).

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartWearEngine.cs`:
  - Implementado `DetectEngineCount(SensorFrame frame)` para determinar o número de motores através do maior índice `:<n>` presente nas chaves do frame.
  - Em `ApplyTick`, os sensores das regras contínuas agora são inspecionados para detectar se são globais (chave exata presente no frame) ou por motor (chave presente com índice `:1`).
  - Regras com sensores por motor são avaliadas separadamente para cada motor `1..engineCount` com leituras `chave:n` (fallback para global se aplicável).
  - Estresse acumulado indexado por `(PartId, EngineIndex)`.
  - Para peças compartilhadas (`EngineIndex == 0`), rastreia-se e aplica-se o estresse do pior motor daquela regra uma vez só (sem somar motores).
  - Rastreio de excedência, pico, duração e inspeções utilizam a chave `<ruleId>#<n>` para regras de motor, incluindo o índice do motor em `TargetId` (ex.: `engine.cylinders:1`) e em `Notes` (`Faixa 'warning' atingida (Motor 1)`).
  - Mensagens de sensores ausentes formatadas citando a tentativa com `:1` (`'[sensorKey]' (nem '[sensorKey]:1')`).
  - Suporte à leitura de reserva nativa via chave oficial `nw-overstress-rate-<id>` e fallback legado `nw-damage-rate-<id>`.
  - Em `ApplyEvent`, trocado `parts.ToDictionary(p => p.PartId)` por `GroupBy(p => p.PartId)` para suportar aeronaves multimotor com peças repetidas sem lançar `ArgumentException`, aplicando o evento a todas as peças correspondentes.
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`:
  - Adicionado `_reportedDiagnostics` (`HashSet<string>`) para reter diagnósticos já enviados no voo (zerado em `Begin()` e `Reset()`).
  - `ProcessTick` repassa apenas diagnósticos inéditos ao resultado, eliminando o spam a cada tick no blackbox (`PECAS_DIAG`).
- `src/FlightCareer.Tests/Parts/PartWearEngineTests.cs`:
  - Atualizados os testes legados para refletir frames reais com sufixos `:1` e `EngineIndex = 1`.
  - Adicionados 7 novos testes unitários e de integração cobrindo os cenários requeridos pela especificação.
- `docs/CHANGELOG.md` e `docs/agents/LICOES.md`:
  - Registrada entrada no changelog e lição aprendida sobre resolução dinâmica de sensores `PerEngine`.

## Critérios de aceite
- [x] No frame real do C152 (chaves `:1`), as seis regras do motor/óleo avaliam e geram excedência acima do limite — validado pelo teste `ApplyTick_RpmPorMotor_EngRpm1AcimaDoRedline_DesgastaPecasDoMotor` e atualização dos testes existentes.
- [x] Bimotor não lança exceção e desgasta por motor — validado pelos testes `ApplyTick_Bimotor_SoMotor2EmVermelho_DesgastaSoPecasDoMotor2`, `ApplyTick_Bimotor_ExcedenciaRastreadaPorMotor` e `ApplyEvent_BimotorPecasRepetidas_NaoLancaExcecao_EAplicaEmTodas`.
- [x] Nenhum diagnóstico repetido por tick — validado pelo teste `Tracker_DiagnosticoDeSensorAusente_SaiUmaVezPorVoo`.
- [x] Teste de contrato registro × regras passando, com a lista de regras ajustadas ou pendentes no relatório — validado pelo teste `WearRules_TodoSensorExisteNoLayoutReal_PorPerfil`. Todas as regras contínuas e de evento em `wear_rules.json` têm 100% de correspondência com os sensores cadastrados no `sensor_registry.json` em todas as 46 aeronaves do catálogo.
- [x] Entrada no fim de `docs/CHANGELOG.md` (`## 2026-11-15 - Regras de desgaste por motor e diagnóstico único (Gemini)`); nova linha em `docs/agents/LICOES.md`.
- [x] Relatório diz o que não foi testado no MSFS.
- [x] Commits `fix(parts): ...` / `test(parts): ...` em português. Sem push.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 309 aprovados, 0 falhas (7 novos testes unitários).
- `python -m unittest discover -s tools/tests`: 45 aprovados, 0 falhas.

## Não testado
- Execução de voo com simulador MSFS 2024 ao vivo (não interativo).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. Todas as chaves de sensores declaradas em `wear_rules.json` existem no catálogo de sensores `sensor_registry.json`; o problema decorria puramente do fato de que o `SensorLayout` expande sensores `PerEngine` com sufixo `:1..N` enquanto as regras de desgaste foram declaradas com os identificadores canônicos sem índice. Com a resolução dinâmica implementada, o motor de desgaste resolve transparentemente ambos os casos.
