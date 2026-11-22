# Spec LIMITES-FALTANTES — Ativar as regras que ficam inativas por falta de dado

**Objetivo (1 frase):** ativar as regras de desgaste que se aplicam mas ficam inativas por falta de parâmetro — detectando em voo o barramento elétrico principal e a tensão do sistema, e completando Va/Vno nos perfis que não têm — sem inventar números.

## Fatos já levantados (não redescobrir)
- Relatório `docs/plans/cobertura-regras-por-aviao.md` (comando `dotnet run --project src/FlightCareer.Replay -- coverage`): média de 4,5 regras/avião inativas:
  - `avionics_start_with_avionics_master_on` (45 aviões): falta `elecAvionicsCircuitIndex`;
  - `electrical_system_overload_amps` (44): falta `alternatorRatedAmps`, `elecMainBusIndex` ou `elecSystemVolts`;
  - `battery_deep_discharge_damage` (44): falta `elecMainBusIndex` ou `elecSystemVolts`;
  - `control_surface_abrupt_above_va` (33): falta `va`; `speedbrake_airspeed_vno_overspeed` (19): falta `vno`; `gear_operation_overspeed_vlo` (14) e `gear_extended_overspeed_vle` (10): faltam `vlo`/`vle`.
- AGENTS.md › 7: os índices elétricos (`elecMainBusIndex`, `elecAvionicsCircuitIndex`, `elecAvionicsBusAmpsIndex`) **não** entram no catálogo do jogo; só no harness de testes (`FuzzTestDefaults.WithTestElectricalIndices`). Por isso as regras elétricas não funcionam em nenhum avião do jogo.
- Voo real do C152 (REC `%LocalAppData%\FlightCareer\sensor-logs\20261122-0822-Cessna_C152.csv`): `elec-bus-voltage-1` e `-2` chegam a 28 V; `elec-bus-voltage-3..5` ficam 0; `ELECTRICAL MAIN BUS VOLTAGE` (legado) fica **0** o voo todo (sistema elétrico novo); `AVIONICS MASTER SWITCH` fica 0 (o C152 **não tem** a chave). O perfil `cessna-c152.json` já tem `elecSystemVolts` 28 e `alternatorRatedAmps` 60 (POH).
- Formato dos limites: `Infrastructure/Data/aircraft/<modelo>.json` › `limits[]` com `key`, `value`, `unit`, `source` (POH, SimVar, Cfg, NativeOnset, ClassDefault), `reference`, `notes`. Padrões por classe em `_class_defaults.json`. Montagem dos perfis: `tools/aircraft_limits/merge_limits.py` + `poh_values.csv` (nunca sobrescrever POH/SimVar).

## Passo a passo
1. **Barramento e tensão detectados em voo** (`Domain/Parts`, onde o `ElectricalConfig` é usado pelo `PartEventDeriver`): quando o perfil não tiver `elecMainBusIndex`, detectar no próprio voo o **primeiro índice k (1..5) com `elec-bus-voltage-k` ≥ 0,5 × tensão do sistema** enquanto `elec-master-battery` = 1, e fixá-lo para o resto do voo. Quando faltar `elecSystemVolts`, usar 28 se a maior tensão vista num barramento com o alternador ligado for ≥ 20 V, senão 14. Enquanto não detectar, a regra fica em espera (sem diagnóstico de "desativada"). Registrar no diagnóstico do replay "barramento principal detectado: k (x V)".
2. **`alternatorRatedAmps` faltando:** padrão da classe em `_class_defaults.json` com `source: ClassDefault` e `reference: "estimativa — atualizar pelo POH: …"`: pistão 60 A por motor, turboélice 250 A, jato 400 A (são estimativas; deixe claro na referência).
3. **`elecAvionicsCircuitIndex`:** não há forma genérica de descobrir; a regra continua inativa onde não houver dado. Para o C152, registre no perfil uma nota de que o avião **não tem** chave de aviônicos (a regra não se aplica) — use a feature/flag que fizer a regra contar como "não aplicável" no relatório, e não como "inativa".
4. **Va faltando:** se houver valor de manual confiável com referência, use-o (`source: POH`). Senão, estimar `Va = Vs1 × √n` com o `vs1` do próprio perfil, `n` = 3,8 (pistão e turboélice até 12.500 lb) ou 2,5 (jato e turboélice de transporte), `source: ClassDefault`, `reference: "estimativa — Va = Vs1·√n (n=…); atualizar pelo POH"`. Sem `vs1` no perfil: não estimar; listar no relatório.
5. **Vno faltando:** em jatos, usar `vmo` do perfil se existir (`source` igual ao do `vmo`, `notes: "Vno = Vmo (jato)"`); nos demais, só com manual. **Vlo/Vle:** só com manual ou cfg do avião (procure em `docs/plans/sensor-research/` e nos `.cfg` que existirem no repositório); sem fonte, listar.
6. Regerar `docs/plans/cobertura-regras-por-aviao.md` e incluir no topo a contagem **antes/depois**. As regras elétricas que passam a funcionar por detecção em voo contam como ativas, com a observação "(detecção em voo)".

## Fora do escopo
- Calibrar faixas das regras; mudar regras; arquivos quentes.

## Testes
- Detecção de barramento: frames com `elec-bus-voltage-1` = 0 e `-2` = 28 e bateria ligada → índice 2; bateria desligada → não detecta; sistema de 14 V detectado.
- Descarga profunda e sobrecarga passam a disparar com perfil **sem** índices (frames sintéticos do C152).
- Estimativa de Va: vs1 40 kt, n 3,8 → 78 kt (registrada como ClassDefault); perfil com Va POH não é alterado.
- `WearRuleMetricCoverageTests` e fuzz all (`--runs 20`) continuam 100 %.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz sem regressão.
- [ ] Nenhum valor POH/SimVar existente alterado; toda estimativa marcada como `ClassDefault` com a fórmula/razão na referência.
- [ ] Relatório de cobertura regenerado com antes/depois e a lista do que continua sem dado.
- [ ] CHANGELOG e AGENTS.md › 7 (a frase sobre índices elétricos "só no harness" muda para "detectados em voo quando ausentes"). Relatório diz "não testado no MSFS".
- [ ] Um commit: `feat(limites): barramento eletrico detectado em voo e va/vno completados (LIMITES-FALTANTES)`.
