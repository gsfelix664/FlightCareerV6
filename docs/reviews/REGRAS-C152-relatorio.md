# REGRAS-C152 — Regras e sensores do C152 e correções de fuzz

**Branch:** feat/regras-c152 · **Data:** 2026-11-16

## Plano (antes de codar)
- Alinhar o registro de sensores com a matriz de sondagem do C152: pontos de contato individuais por roda (0=nariz, 1=principal esquerdo, 2=principal direito) com `perEngine: false`, novas variáveis de gerador elétrico MSFS 2024 (`ELECTRICAL GENERATOR AMPS/VOLTAGE/ACTIVE`) e pressão dinâmica (`DYNAMIC PRESSURE`).
- Atualizar catálogo de regras (`wear_rules.json`) e peças (`parts_catalog.json`) para usar os novos sensores e desacoplar sensores que não variam no C152 (como `af-min-g-force`).
- Corrigir o suporte a limites negativos no `PartWearEngine` quando `Direction == LimitDirection.Below` (resolvendo a regra `airframe_over_g_negative` com `gLimitNeg = -1.52`).
- Definir `egtMax` padrão para a classe `Piston` (893.52 °C) em `_class_defaults.json`.
- Adequar `PartEventDeriver` (limiares de trem em 0–100%) e `SyntheticFlightGenerator` (escala percentual e duração mínima de abusos contínuos).
- Adicionar suíte completa de testes xUnit cobrindo os 8 cenários exigidos na especificação.

## O que foi feito
1. **Registro de Sensores (`sensor_registry.json`, `sensor-inventory.md`, `SensorRegistryTests.cs`):**
   - Substituídos `gear-contact-point-compression` e `gear-contact-point-is-skidding` (`perEngine: true`) por entradas explícitas por roda com `perEngine: false`: `gear-contact-point-compression-0/-1/-2` e `gear-contact-point-is-skidding-0/-1/-2`.
   - Adicionados sensores de solo por roda `gear-contact-point-on-ground-0/-1/-2` (`CONTACT POINT IS ON GROUND:0..2`, bool).
   - Adicionados sensores de gerador elétrico MSFS 2024 `elec-generator-amps`, `elec-generator-voltage`, `elec-generator-active` com `perEngine: true`. Mantidas as chaves `elec-genalt-*` para compatibilidade com frotas legadas.
   - Adicionado `af-dynamic-pressure` (`DYNAMIC PRESSURE`, unidade `pound-force per square foot`, grupo `airframe`).
   - Mantidas no registro as variáveis que não variaram no C152 (`eng-vibration`, `gear-skidding-factor`, `af-min-g-force`) para continuidade de gravação e prova em outros modelos.
   - Atualizado `SensorRegistryTests` para reconhecer `pound-force per square foot` como unidade válida e mantida paridade 1:1 com `sensor-inventory.md`.

2. **Regras e Catálogo de Peças (`wear_rules.json`, `parts_catalog.json`, `tools/sensor_validation_expectations.json`):**
   - `wear_rules.json` › `airframe_over_g_negative`: simplificado para `sensors: ["af-g-force"]` (removido `af-min-g-force`).
   - `touchdown_hard_landing` e `landing_cycle_structural_stress`: sensores atualizados para `gear-contact-point-compression-1` e `gear-contact-point-compression-2` (rodas principais).
   - `parts_catalog.json`: associados sensores por roda nas peças de pneus e trem, e novas chaves de gerador em `elec.alternator_generator` e `elec.starter_generator`.
   - `tools/sensor_validation_expectations.json`: ajustados blocos 08 e 08b para os novos sensores.

3. **Derivador de Eventos (`PartEventDeriver.cs`):**
   - Corrigida detecção de posição do trem: `isUp = gearPos <= 10.0`, `isDown = gearPos >= 90.0` (removida ambiguidade 0–1).
   - Atualizado fallback de detecção de solo em `TryGetFlightPhase`: ordem prioriza `ctx-sim-on-ground` / `sim-on-ground`, seguido de solo por roda `gear-contact-point-on-ground-*` (qualquer roda = 1), compressão de roda e velocidade de toque.

4. **Motor de Desgaste (`PartWearEngine.cs`):**
   - Linha ~176: aceita limites negativos quando `rule.Direction == LimitDirection.Below`. Rejeita valores zero, NaN, infinito ou negativos com `Direction == Above` (emitindo diagnóstico).
   - `EvaluateActiveBand`: aceita limites negativos com `Direction == Below`, ordenando as faixas por `limit * Threshold` decrescente (unificando regras positivas e negativas com ordenação correta).
   - `excessPct`: corrigido para tratar limites negativos sem inversão: `limitVal != 0.0 ? Math.Abs((measuredVal - limitVal) / limitVal) * 100.0 : 0.0`.
   - `sourceStr`: ajustado para `rule.Direction == Above ? measuredVal >= limitVal : measuredVal <= limitVal`.
   - Resolução de sensores por motor: priorizada a chave `${sensorKey}:1` sobre a chave global desindexada `${sensorKey}` quando ambas estiverem presentes no frame (evitando que métricas com chave global percam o índice do motor na peça alvo).

5. **Padrão de Classe para EGT Máximo (`_class_defaults.json`):**
   - Adicionado `egtMax` = 893.52 °C (`source: ClassDefault`, referência C152 MAX EGT) sob a classe `Piston`.

6. **Gerador Sintético, Replay e Fuzz (`SyntheticFlightGenerator.cs`, `SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs`, `Program.cs`):**
   - Gerador produz trem de pouso em percentual (0–100%) e gera as novas variáveis (`0..2`, gerador, pressão dinâmica).
   - Corrigido clamp de CHT na fase de descida para não sobrescrever choque térmico em andamento.
   - Sorteio procedural de abusos dependentes de limites (Vne, Vfe, sobrecarga elétrica, aviônicos) condicionado à existência dos respectivos limites no perfil da aeronave.
   - Duração dos abusos contínuos e posicionamento de cancelamento prematuro (`DataCancelledMidFlight`) ajustados para não truncar abusos físicos anteriores.
   - Oráculo e executores de fuzz: voo controle sincronizado com a duração real quando houver cancelamento no meio do voo, e supressão de falsos positivos de abusos que foram programados para momento posterior ao encerramento antecipado do voo.

7. **Testes Unitários (`C152RulesTests.cs`, `SensorLayoutTests.cs`):**
   - Implementados os 8 testes específicos em `FlightCareer.Tests/Parts/C152RulesTests.cs`:
     - `OverGNegative_GAbaixoDoLimite_AplicaFaixa`
     - `OverGNegative_GNormal_NaoAplica`
     - `LimiteNegativo_RegraAbove_Ignorada`
     - `C152_EgtMax_PistonClassDefault_AplicaFaixa`
     - `C152_EgtNormal_NaoAplica`
     - `TremPosicao_90pct_DownAndLocked`
     - `TremPosicao_1pct_TransitOuUp`
     - `Paridade_SensorRegistry_SensorInventory`
   - Atualizados os testes de contagem de layout em `SensorLayoutTests.cs` (432 sensores; 413/433/653 slots).

## Critérios de aceite
- [x] Sensores por roda (`0..2`), gerador 2024 e pressão dinâmica cadastrados e testados.
- [x] Regra `airframe_over_g_negative` dispara faixas Caution/Warning para limites negativos (ex: −1,52 no C152).
- [x] Limites negativos com `Above` ignorados com diagnóstico.
- [x] `egtMax` herdado de `_class_defaults.json` › `Piston` para aeronaves sem limite explícito.
- [x] Posição de trem em percentual 0–100% com `isUp <= 10.0` e `isDown >= 90.0`.
- [x] Fuzz testing com voos sintéticos rodando 100/100 corridas com 100% de aprovação e 0 falhas.
- [x] 100% dos testes unitários da solução passando (`dotnet test src/FlightCareer.Tests`).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 463 testes aprovados, 0 falhas.
- `FlightCareer.Replay fuzz --runs 100 --seed 777`: 100/100 voos aprovados (100,0%), 0 falhas.

## Não testado
- Conexão ao vivo no simulador MSFS 2024 (as alterações são puras de dados, regras de domínio e simulação sintética; os sensores foram baseados nas matrizes de sondagem gravadas no C152).

## Dúvidas e decisões para o revisor
- Bug 5 do fuzz (`landingCycles` em 4 peças estruturais vs apenas na regra de pouso): mantido conforme diretriz fora de escopo para decisão posterior do usuário.
- Chaves `elec-genalt-*` mantidas no registro para compatibilidade retroativa, ao lado das novas chaves `elec-generator-*`.
