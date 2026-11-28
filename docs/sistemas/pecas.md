# Peças

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Modela o ciclo de vida, estresse operacional contínuo e quebra de peças físicas individuais instaladas.
- **Onde está:** `Domain/Parts/` (`PartWearEngine`, `PartFlightTracker`, `PartEventDeriver`, `PartComponentBridge`, `ElectricalConfig`, `NativeFailurePartMap`, `DefectPartLink`, `PartAirworthiness`), `Infrastructure/Services/PartStateWriter`, `UI/Services/PartFlightSession`, `AircraftPartProvisioner`.
- **Regras e integridade:**
  - Em aeronaves com peças (`PartComponentBridge.HasParts`), o desgaste em voo é calculado por peça pelo `PartWearEngine` a partir do `SensorFrame`, do toque e de eventos operacionais derivados (`PartEventDeriver`, 29 métricas unificadas em `PartFlightTracker.ProducedMetrics`). O `WearEngine` legado fica inativo (`PartsDriven = true`).
  - As 9 barras de componentes e colunas legadas tornam-se agregadas da saúde das peças (`SyncComponentsFromParts` / `AggregateHealth`).
  - Danos de falhas e eventos são aplicados via `DamageGroup`/`ClampGroup`. Panes nativas do simulador quebram exatamente uma peça prioritária via `NativeFailurePartMap` (menor saúde, ordem na tabela, menor motor).
  - Peças quebradas com `BlocksFlightWhenBroken == true` interditam obrigatoriamente a aeronave (`PartAirworthiness`), bloqueando o voo no Hangar (status "INTERDITADA (PEÇA)") e no `FlightMonitor.StartMonitoring`.
  - Quebra de trem e pneus no pouso (`LandingBreakModel`, 2026-11-26): chance calibrada em JSON por razão de descida (600/850/900 fpm), inclinação de asa, terreno fora de pista e desgaste; sorteia no máximo 1 peça de trem e 1 de pneu por impacto, priorizando montante principal antes do nariz/bequilha. Peça quebrada interdita imediatamente sem duplicar pane. Inspeções operacionais viram panes no Hangar (`InspectionDefectPolicy`), interditando em toque severo e diferidas para outras regras. Não testado no MSFS.
  - Reconciliação no início do voo (`ReconcilePartsFromComponents`) detecta reparos externos (eleva saúde e limpa `IsBroken`) ou avarias fora de voo.
  - Peças com vida em ciclos (pneus, freios, rodas) avançam em pousos reais (`PartWearEngine.ApplyLandingCycle`), ignorando quiques.
  - Lâmpadas no grupo `Iluminação` (`PartGroup.Iluminacao`) são `minorPart` (não recebem dano de grupo) com restrições operacionais da MEL noturna (`LightingMel`). Sem atuação direta no sim; não testado no MSFS.
  - Na preparação pré-voo (`ObservePreflight`), eventos operacionais são computados sem aplicar desgaste natural contínuo, salvando no cancelamento apenas se houver alterações (`HasPreflightChanges`).
  - Provisionamento e limpeza (`AircraftPartProvisioner`) adiciona peças faltantes a 100% e remove peças obsoletas por features do modelo (`noElectrical`, `noFlaps`).
  - Persistência em lote via `PartStateWriter.CopyToDbAsync` nos 4 momentos do voo (a cada 60 s, cancelamento, perda total e conclusão).
  - Testes e ferramentas de verificação: `FlightReplayer`, `SyntheticFlightGenerator`, `SyntheticFlightOracle` e subcomandos `fuzz` e `coverage` no `FlightCareer.Replay`. Calibração em `component_catalog.json` e `wear_rules.json`.
