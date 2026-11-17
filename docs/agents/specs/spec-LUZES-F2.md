# Spec LUZES-F2 — Iluminação como peças (domínio + catálogos)

**Objetivo (1 frase):** criar o grupo de peças "Iluminação" (6 lâmpadas halógenas) que desgasta só com a luz acesa, queima por taxa por hora e gera avisos de MEL noturna, tudo em Domain + catálogos JSON, sem atuação no simulador.

Plano de referência: `docs/plans/iluminacao-panes-disjuntor.md` § 5 (leia). Decisões do usuário (2026-11-17), que substituem o § 7 do plano nos itens 1–3:
1. Iluminação = **grupo de peças dentro do componente Elétrico** (barra Electrical existente). Nada de componente novo / 10ª barra.
2. **Tudo halógena** por ora (nenhum campo LED/halógena no perfil).
3. MEL noturna **só avisa** (domínio devolve o aviso; nenhum bloqueio de voo agora).

## Fatos já levantados (não redescubra)
- `Domain/Parts/PartGroup.cs`: constantes + `All`. `Infrastructure/Data/part_component_mapping.json` › `groupMappings` — grupo sem entrada é **ignorado** no `SyncComponentsFromParts` (`PartComponentBridge.cs:132-141`). Agregação da barra: `min*0.7 + avg*0.3` (`PartComponentBridge.cs:87-100`).
- Desgaste natural: `PartWearEngine.cs:478-496`, `normalRatePerHour = 70.0 / def.LifeHours`, aplicado a toda peça em todo tick de voo ativo com `dtSimHours` (tempo simulado). Não há filtro por sensor. `lifeHours: 0` daria divisão por zero: **não use**.
- Peças não têm sorteio de pane por hora; só o `FailureEngine` do componente (`Domain/Components/FailureEngine.cs:81-89`, `λ = 0,02 · 100^((20−h)/20)`, zero acima de 20%). `IsBroken` (`Domain/Entities/AircraftPart.cs:65`) hoje só é ligado por pane nativa (`PartWearEngine.ApplyNativeFailed`).
- `PartFlightTracker.DamageGroup(ComponentKind, amount)` (`:386`) e `ClampGroup` (`:397`) roteiam dano do componente para todas as peças do grupo.
- Provisionamento: `Infrastructure/Services/AircraftPartProvisioner.cs` já adiciona peças do catálogo que faltam em aviões existentes (idempotente). `PartAppliesTo` aceita `all/any/none` com o vocabulário fechado de `Domain/Parts/AircraftFeatures.cs` (`noElectrical` existe; J-3 Cub e CAP-4 o usam).
- SimVars de luz **aceitas no MSFS 2024 (C152, probe 2026-11-16, `AceitaVariando`)**: `LIGHT LANDING ON`, `LIGHT TAXI ON`, `LIGHT NAV ON`, `LIGHT STROBE ON`, `LIGHT BEACON ON`, `LIGHT PANEL ON` (Bool, estado real aceso). Use essas (as sem `ON` são a chave). Nenhuma está hoje em `sensor_registry.json`.
- `sensor_registry.json` tem paridade testada com `docs/plans/sensor-inventory.md` (`Tests/Sensors/SensorRegistryTests.cs`): acrescente nos dois.
- Aleatoriedade: `IRandomSource` (`Domain/Components/ComponentSupport.cs`). Probabilidade sempre `p = 1 − e^(−λ·dt)`.

## Arquivos a mexer
- `Domain/Parts/PartGroup.cs`: `public const string Iluminacao = "Iluminação";` + no `All`.
- `Infrastructure/Data/part_component_mapping.json`: `"Iluminação": "Electrical"`.
- `Domain/Parts/PartDefinition.cs` (+ loader em `PartCatalog.cs`): campos opcionais novos (default = comportamento atual):
  - `usageSensor` (string?, chave do sensor): se definido, o desgaste natural da peça só corre no `dt` em que o valor do sensor no frame for ≥ 0,5. Sensor ausente no frame = sem desgaste (sem exceção). `lifeHours` passa a significar horas acesas.
  - `damagePerOnCyclePct` (double, default 0): dano por transição 0 → 1 do `usageSensor` (ciclo de liga). Ignorar a primeira leitura do voo (sem estado anterior não é ciclo).
  - `failsByUsageRate` (bool, default false): pane por taxa por hora enquanto o `usageSensor` está ativo (ver abaixo).
  - `minorPart` (bool, default false): entra só na **média** da agregação da barra, não no **mínimo**; e **não** recebe dano de `DamageGroup` (pane do componente Elétrico não queima lâmpada). `ClampGroup` continua valendo (dano fora de voo / perda total).
  - Valide no loader: `usageSensor` obrigatório se `failsByUsageRate` ou `damagePerOnCyclePct > 0`.
- `Domain/Parts/PartWearEngine.cs`: filtro do `usageSensor`, ciclos e chamada da pane por uso. Peça `IsBroken` não desgasta nem sorteia.
- **Novo** `Domain/Parts/UsageFailureModel.cs` (lógica pura): `static double LambdaPerHour(double healthPct)` = mesma curva do `FailureEngine` (reaproveite a função se ela for pública/estática; não duplique a constante se puder referenciar) e `static bool Roll(double healthPct, double dtSimHours, IRandomSource rng)`. Saúde ≤ 0 enquanto acesa = queima certa. Ao queimar: `IsBroken = true`, `Health = 0`, diagnóstico para o blackbox no mesmo mecanismo de diagnósticos drenados que o tracker já usa ("Lâmpada queimada: <nome>"), **uma vez**.
- `Domain/Parts/PartComponentBridge.cs`: respeitar `minorPart` na agregação; `DamageGroup` em `PartFlightTracker.cs` pula `minorPart`.
- **Novo** `Domain/Parts/LightingMel.cs`: `static IReadOnlyList<LightingMelItem> Evaluate(IEnumerable<AircraftPart> parts, PartCatalog catalog)`; `record LightingMelItem(string PartId, string Name, LightingMelCategory Category, string Message)`; `enum LightingMelCategory { NoGoNight, DayOnly }`. Só peças do grupo Iluminação com `IsBroken`:
  - `lights.nav`, `lights.strobe`, `lights.panel` → `NoGoNight`.
  - `lights.beacon` → `NoGoNight` se não houver `lights.strobe` instalada e inteira; senão `DayOnly`.
  - `lights.landing`, `lights.taxi` → `DayOnly`.
  - Mensagens em português (ex.: "Luz de navegação queimada: voo noturno não permitido (aviso)").
  - Ninguém consome isso ainda na UI (fora do escopo).
- `Infrastructure/Data/parts_catalog.json`: 6 peças, `group: "Iluminação"`, `appliesTo: { "none": ["noElectrical"] }`, `perEngine: false`, `visibleOnWalkaround: true`, `blocksFlightWhenBroken: false`, `minorPart: true`, `failsByUsageRate: true`, `priceScalesWithAircraft: false`, `calendarLifeMonths: null`, `downtimeHours: [0.5, 1.0]`, `failureEffect: "Lâmpada queimada (luz apagada)"`, `lifeSource: "estimativa — revisar (lâmpada halógena típica)"`, `sensors: [<usageSensor>]`. Copie os demais campos obrigatórios do padrão das outras peças (`repairCostPctOfAircraft` pequeno, ex. `[0.0005, 0.002]` — confira o que o campo significa no código antes de escolher e explique no relatório).

| id | nome | usageSensor | lifeHours | damagePerOnCyclePct | partPrice | laborHours |
|---|---|---|---|---|---|---|
| `lights.landing` | Farol de pouso | `light-landing-on` | 300 | 0.05 | 60 | 0.5 |
| `lights.taxi` | Farol de táxi | `light-taxi-on` | 300 | 0.05 | 60 | 0.5 |
| `lights.nav` | Luzes de navegação | `light-nav-on` | 1000 | 0.02 | 40 | 0.5 |
| `lights.strobe` | Luzes estroboscópicas (anticolisão) | `light-strobe-on` | 1000 | 0.03 | 250 | 1.0 |
| `lights.beacon` | Beacon (anticolisão) | `light-beacon-on` | 1000 | 0.03 | 120 | 0.5 |
| `lights.panel` | Iluminação do painel | `light-panel-on` | 2000 | 0.01 | 50 | 1.0 |

- `Infrastructure/Data/sensor_registry.json` + `docs/plans/sensor-inventory.md`: 6 sensores (`light-landing-on` → `LIGHT LANDING ON`, unit `Bool`, …) no grupo/padrão dos sensores elétricos Bool existentes, **acrescentados no fim** da lista.
- Gerador sintético (`Domain/Replay/Synthetic/SyntheticFlightGenerator`): preencher os 6 sensores (nav/beacon/strobe acesos do táxi ao corte; farol de táxi no táxi; farol de pouso na decolagem e aproximação; painel apagado), para o fuzz exercitar o caminho. O oráculo não pode quebrar; se houver invariante de "peça só perde saúde em voo", ela continua valendo.

## Fora do escopo (não mexer)
- `SimFailureActuator.cs`, `SimConnectService.cs`, `FlightMonitor.cs`, `ComponentFlightSession.cs`, `SimTelemetry.cs`, `App.xaml.cs`, Views/ViewModels: **nenhuma** atuação no sim (lâmpada continua acesa no MSFS; isso é F3), nenhuma UI.
- `electricalMap`, novos `FailureEffect`, `DefectCatalog`, disjuntores, flap (fases F3/F4 do plano).
- `component_catalog.json` e o `FailureEngine` (só reaproveitar a curva).

## Testes (em `src/FlightCareer.Tests/Parts/LightingPartsTests.cs` salvo indicação)
- `Lampada_HorasAcesas_DesgastaPorTempoSimulado`: frame com `light-landing-on=1`, 1 h simulada → perda = 70/300 % (±1e-6); com 0 → perda 0.
- `Lampada_SensorAusente_NaoDesgastaNemLanca`.
- `Lampada_CicloLiga_AplicaDanoPorTransicao`: 0→1→0→1 = 2 ciclos; primeira leitura 1 não conta.
- `Lampada_Falha_PorTaxaPorHora`: `IRandomSource` fixo; saúde 50 % → λ=0 nunca queima; saúde 10 % e sorteio abaixo de p → `IsBroken`, `Health=0`, um diagnóstico só; apagada não sorteia.
- `Lampada_SaudeZeroAcesa_Queima`.
- `Lampada_Queimada_NaoDesgastaMais`.
- `MinorPart_ForaDoMinimo_DaAgregacao`: elétrico com bateria 90 e lâmpada 0 → barra = 0,7·90 + 0,3·média(90,0) (respeite os pesos do JSON).
- `DamageGroup_Eletrico_NaoAtingeLampadas`; `ClampGroup_Eletrico_AtingeLampadas`.
- `Mel_NavQueimada_NoGoNoturno`; `Mel_BeaconQueimadoComStrobeOk_DayOnly`; `Mel_BeaconQueimadoSemStrobe_NoGoNoturno`; `Mel_PousoQueimado_DayOnly`; `Mel_TudoOk_Vazio`.
- `Catalogo_Luzes_NaoProvisionadasEmNoElectrical` (J-3/CAP-4 sem `lights.*`; C152 com as 6).
- `Catalogo_UsageSensorInexistente_Lanca` (loader) e `Catalogo_LuzesUsamSensoresDoRegistro` (todo `usageSensor` existe no `sensor_registry.json`).
- Testes existentes continuam passando (paridade sensor-inventory, persistência, fuzz).

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % verde.
- [ ] `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20` sem falhas (cole o resumo).
- [ ] Peças sem os campos novos se comportam exatamente como antes (nenhum teste antigo alterado para passar; se precisar alterar, justifique).
- [ ] Entrada no fim de `docs/CHANGELOG.md` (`## 2026-11-17 - Iluminação como peças (Gemini)`) e linha no `AGENTS.md` › 7 (Peças no voo) descrevendo o grupo Iluminação, `usageSensor`, `minorPart`, `LightingMel` e "sem atuação no sim; não testado no MSFS".
- [ ] Relatório: só fatos conferidos (lição CATALOGO-PADROES / NOTA-PASSAGEIROS: valores são estimativas, sem citar fonte não conferida).
- [ ] Um commit: `feat(parts): grupo Iluminação com desgaste por uso e MEL noturna (LUZES-F2)`.

## Armadilhas
- Diagnóstico lido a cada tick tem que ser drenado (lição F2.4b-CAL).
- Tempo sempre simulado (`dtSimHours`), nunca relógio.
- Nada de `catch {}` silencioso.
- `ObservePreflight` (pré-voo) não aplica desgaste natural: luzes também não desgastam nem queimam no pré-voo (não conte ciclo ali também, para simplificar; diga no relatório).
