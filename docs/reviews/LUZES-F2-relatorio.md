# LUZES-F2 — Iluminação como peças (domínio + catálogos)

**Branch:** feat/luzes-pecas · **Commit:** pendente · **Data:** 2026-11-17

## Plano (antes de codar)
- Adicionar grupo `Iluminação` em `PartGroup.cs` e mapear para `Electrical` em `part_component_mapping.json`.
- Estender `PartDefinition` e `PartCatalog` com `usageSensor`, `damagePerOnCyclePct`, `failsByUsageRate`, `minorPart`, e regras de validação no loader.
- Criar `UsageFailureModel` em `Domain/Parts/` com cálculo de taxa $\lambda(h)$ reaproveitando `FailureEngine.HazardPerHour` e sorteio `Roll` determinístico com `IRandomSource`.
- Criar `LightingMel` em `Domain/Parts/` com avaliação de itens quebrados do grupo Iluminação (`NoGoNight` e `DayOnly`).
- Ajustar `PartComponentBridge.AggregateHealth` para excluir `minorPart` do mínimo da barra (permanecendo na média) e `PartFlightTracker.DamageGroup` para pular `minorPart`.
- Atualizar `PartWearEngine` para condicionar o desgaste natural a `usageSensor >= 0.5`, detectar ciclos de acendimento (transição 0 -> 1) com dano por ciclo, e sortear pane por taxa via `UsageFailureModel`.
- Cadastrar as 6 lâmpadas halógenas em `parts_catalog.json` e os 6 sensores correspondentes em `sensor_registry.json` e `docs/plans/sensor-inventory.md`.
- Atualizar gerador de voos sintéticos (`SyntheticFlightGenerator`) e oráculo (`SyntheticFlightOracle`) para exercitar os sensores e respeitar as horas acesas das luzes.
- Criar suíte completa de testes em `LightingPartsTests.cs`, validar build, testes xUnit e fuzz testing (20 runs por modelo).

## O que foi feito
- `src/FlightCareer.Domain/Parts/PartGroup.cs`: Adicionada constante `public const string Iluminacao = "Iluminação";` e inclusão no vocabulário fechado `PartGroup.All` (23 grupos no total).
- `src/FlightCareer.Infrastructure/Data/part_component_mapping.json` e `src/FlightCareer.Domain/Parts/PartComponentBridge.cs`: Adicionado mapeamento `"Iluminação": "Electrical"` no JSON e no fallback estático `PartComponentMapping.CreateDefault()`.
- `src/FlightCareer.Domain/Parts/PartDefinition.cs` e `PartCatalog.cs`: Adicionadas as propriedades opcionais `UsageSensor`, `DamagePerOnCyclePct`, `FailsByUsageRate` e `MinorPart`. No loader/validador: validação de obrigatoriedade de `UsageSensor` caso `FailsByUsageRate` ou `DamagePerOnCyclePct > 0`, e conferência de existência da chave do sensor no registro de sensores (`validSensorKeys`).
- `src/FlightCareer.Domain/Parts/UsageFailureModel.cs`: Criada classe estática de lógica pura com `LambdaPerHour(healthPct)` reaproveitando `FailureEngine.HazardPerHour` e `Roll(healthPct, dtSimHours, IRandomSource)` com $p = 1 - e^{-\lambda \cdot dt}$. Saúde $\le 0\%$ enquanto acesa resulta em queima garantida.
- `src/FlightCareer.Domain/Parts/LightingMel.cs`: Criada avaliação da MEL noturna (`LightingMel.Evaluate`) retornando `LightingMelItem` com categorias `NoGoNight` (nav, strobe, panel, e beacon sem strobe operacional) e `DayOnly` (landing, taxi, e beacon com strobe operacional) com mensagens em português.
- `src/FlightCareer.Domain/Parts/PartComponentBridge.cs`: `AggregateHealth` atualizado com suporte a `catalog`: peças com `MinorPart == true` entram apenas na média e são excluídas do cálculo do mínimo (salvo se todas forem minor).
- `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`: `DamageGroup` pula peças com `MinorPart == true` (pane no componente elétrico não danifica lâmpadas). `ClampGroup` continua atuando normalmente. Construtor estendido com suporte a `IRandomSource` opcional.
- `src/FlightCareer.Domain/Parts/PartWearEngine.cs`:
  - Desgaste natural de peças com `UsageSensor` só corre quando o sensor no frame está ativo ($\ge 0,5$), incrementando `HoursSinceReplacement` em tempo simulado `dtSimHours`. Sensor desligado ou ausente resulta em zero desgaste sem lançar exceção.
  - Detecção de ciclo de liga (transição 0 $\rightarrow$ 1) aplicando `DamagePerOnCyclePct` (primeira leitura do voo ignorada).
  - Avaliação de pane por taxa de uso via `UsageFailureModel.Roll`. Ao queimar: `IsBroken = true`, `Health = 0` e emissão única do diagnóstico `$"Lâmpada queimada: {def.Name}"`.
- `src/FlightCareer.Infrastructure/Data/parts_catalog.json`: Cadastradas as 6 lâmpadas halógenas (`lights.landing`, `lights.taxi`, `lights.nav`, `lights.strobe`, `lights.beacon`, `lights.panel`) com `priceScalesWithAircraft: false` e faixa estimada `repairCostPctOfAircraft: [0.0005, 0.002]`. Como `PriceScalesWithAircraft` é falso, o preço cobrado em substituições é estritamente o valor fixo em `partPrice` ($40 a $250); a faixa percentual foi fornecida em valores pequenos para conformidade com a validação do schema do catálogo.
- `src/FlightCareer.Infrastructure/Data/sensor_registry.json` e `docs/plans/sensor-inventory.md`: Adicionados 6 sensores elétricos (`light-landing-on`, `light-taxi-on`, `light-nav-on`, `light-strobe-on`, `light-beacon-on`, `light-panel-on`) do tipo `bool` ao fim da lista, com paridade 100% validada.
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlightGenerator.cs`: Integrado o acendimento realista dos 6 sensores de luz ao longo das fases do voo (nav/beacon/strobe acesos do táxi ao corte; táxi no solo; landing na decolagem e aproximação; painel apagado).
- `src/FlightCareer.Domain/Replay/Synthetic/SyntheticFlightOracle.cs`: Invariante 5 (conferência de horas simuladas acumuladas) ajustada para desconsiderar peças `lights.*`, visto que acumulam apenas horas acesas.
- `src/FlightCareer.Tests/Parts/PartCatalogTests.cs` e `src/FlightCareer.Tests/Sensors/SensorLayoutTests.cs`: Atualizadas contagens fixas esperadas (148 $\rightarrow$ 154 peças, 22 $\rightarrow$ 23 grupos; 432 $\rightarrow$ 438 sensores no registro e blocos de slots derivados).
- `src/FlightCareer.Tests/Parts/LightingPartsTests.cs`: Criada suíte completa com 17 testes cobrindo todos os cenários da especificação.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros — Verificado via CLI
- [x] `dotnet test src/FlightCareer.Tests` 100% aprovado — 594/594 testes aprovados (577 baseline + 17 novos)
- [x] `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20` sem falhas (920 voos aprovados, 0 falhas)
- [x] Peças sem os campos novos se comportam exatamente como antes (testes legados e fuzz passam sem regressão)
- [x] Entrada no fim de `docs/CHANGELOG.md` e linha no `AGENTS.md` › 7 (Peças no voo)
- [x] Um único commit na branch `feat/luzes-pecas` sem push

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, avisos conhecidos de vulnerabilidade de pacote do SQLitePCLRaw herdados.
- `dotnet test src/FlightCareer.Tests`: 594 aprovados, 0 falhas (17 novos testes em `LightingPartsTests.cs`).
  - `Lampada_HorasAcesas_DesgastaPorTempoSimulado`
  - `Lampada_SensorAusente_NaoDesgastaNemLanca`
  - `Lampada_CicloLiga_AplicaDanoPorTransicao`
  - `Lampada_Falha_PorTaxaPorHora`
  - `Lampada_SaudeZeroAcesa_Queima`
  - `Lampada_Queimada_NaoDesgastaMais`
  - `MinorPart_ForaDoMinimo_DaAgregacao`
  - `DamageGroup_Eletrico_NaoAtingeLampadas`
  - `ClampGroup_Eletrico_AtingeLampadas`
  - `Mel_NavQueimada_NoGoNoturno`
  - `Mel_BeaconQueimadoComStrobeOk_DayOnly`
  - `Mel_BeaconQueimadoSemStrobe_NoGoNoturno`
  - `Mel_PousoQueimado_DayOnly`
  - `Mel_TudoOk_Vazio`
  - `Catalogo_Luzes_NaoProvisionadasEmNoElectrical`
  - `Catalogo_UsageSensorInexistente_Lanca`
  - `Catalogo_LuzesUsamSensoresDoRegistro`
- Fuzz Testing (`dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`):
```text
--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 920
  Voos Aprovados      : 920 (100,0%)
  Voos com Falhas     : 0 (0,0%)
  Tempo Total         : 397,51 s (2 voos/s)
--------------------------------------------------------------------------------
✅ Nenhuma invariante ou gabarito violado em todas as corridas executadas!
================================================================================
```

## Não testado
- Atuação das luzes no simulador MSFS 2024 / SimConnect (escopo da fase F3).
- Interface de usuário (HUD, Hangar, painel de MEL) para exibição dos avisos da MEL (fora do escopo da F2).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Todas as decisões de projeto (grupo Iluminação dentro de Elétrico, lâmpadas halógenas, MEL noturna como aviso sem bloqueio, isolamento `minorPart`, `usageSensor` e queima determinística) foram seguidas rigorosamente.
