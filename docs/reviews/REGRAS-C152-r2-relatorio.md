# REGRAS-C152-r2 — Correções da Revisão Rodada 1

**Branch:** feat/regras-c152 · **Data:** 2026-11-16

## Plano (antes de codar)
- Reproduzir e diagnosticar as 14 falhas (17 violações de gabarito) observadas pelo revisor no fuzz de 1000 voos (`fuzz --runs 1000 --seed 777`).
- Classificar cada falha encontrada em Produção × Gerador × Gabarito.
- Restaurar o sorteio incondicional de abusos em `SyntheticFlightPlan` (`VneExceeded`, `FlapOverSpeed`, `BatteryDischarging`, `ElectricalOverload`, `AvionicsMasterOnStart`) e registrar "limite ausente: <chave>" no oráculo e no resumo da CLI em categoria própria segregada.
- Obter o tempo mínimo de acúmulo contínuo no oráculo a partir dos valores reais do catálogo de regras (`wear_rules.json` › primeiro threshold de banda) em vez de tempos fixos.
- Esclarecer e evidenciar tecnicamente no código de produção qual frame real gera as chaves exatas e com `:1` simultaneamente (`PartFlightTracker.cs#L490-L494`).
- Executar e validar o fuzz testing de 1000 voos e a suíte completa xUnit (463 testes).

## O que foi feito
1. **Sorteio Amplo e Categorização de Limites Ausentes (`SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs`, `Program.cs`):**
   - Em `SyntheticFlightPlan.cs`, removidos os bloqueios por perfil para `VneExceeded`, `FlapOverSpeed`, `BatteryDischarging`, `ElectricalOverload` e `AvionicsMasterOnStart`, voltando a sortear todos os abusos em todas as frotas.
   - Em `SyntheticFlightOracle.cs`, implementado `CheckMissingLimit` no `VerifyGroundTruth(..., AircraftLimitProfile? profile, WearRuleCatalog? wearRuleCatalog)`. Quando a aeronave do teste não possui o limite operacional no catálogo (`vne`, `vfe`, `elecMainBusIndex`, `elecAvionicsCircuitIndex`, `alternatorRatedAmps`), retorna `limite ausente: <chave>`.
   - Em `Program.cs`, capturadas as mensagens de limite ausente em categoria própria (`missingLimitsByType`), sem incrementar `failedCount` e sem contar como aprovado (`missingLimitRunsCount++`).
2. **Tempos Reais das Regras no Oráculo (`SyntheticFlightOracle.cs`):**
   - Removidas as constantes mágicas (600 s / 300 s). O oráculo agora lê o primeiro `threshold` configurado em `wear_rules.json` para as regras `battery_deep_discharge_damage` (`BatteryDeepDischargeMinutes` × 60) e `electrical_system_overload_amps` (`ElectricalOverloadMinutes` × 60), usando fallback apenas se o catálogo de regras não for injetado.
3. **Resolução de Conflitos Físicos e Colisão de Abusos no Gerador/Plano:**
   - `PositiveGExceeded` vs `NegativeGExceeded`: escalonados temporalmente para momentos distintos (`0.46 * duration` e `0.54 * duration`), garantindo separação mínima de 3 segundos quando ambos forem sorteados para o mesmo voo (evitando que um sobrescreva o frame do outro no mesmo tick).
   - `BatteryDischarging` vs `ElectricalOverload`: mutuamente exclusivos no mesmo voo em `SyntheticFlightPlan` (descarga profunda exige alternador desligado e queda de tensão < 22V; sobrecarga exige alternador ligado e barramento a ~25.5V sob alta corrente; a ocorrência simultânea em um mesmo barramento anulava o estado elétrico da outra regra).
   - `HardLanding`: momento fixado exatamente no instante do touchdown (`0.90 * duration`), evitando cancelamento prematuro em voo antes de tocar o solo.
   - `DataCancelledMidFlight`: limitado a no máximo 1 por voo, agendado estritamente após o término de todos os abusos físicos planejados.
   - `ThermalShock`: no `SyntheticFlightGenerator.cs`, durante a fase de cruzeiro com choque térmico ativo, preservado o resfriamento contínuo em vez de resetar CHT para 155 °C a cada tick.
   - `EgtHigh`: fórmula no gerador atualizada para `egtMax * (1.0 + Math.Max(0.05, abuse.Intensity))`, garantindo que ultrapasse o `egtMax` do catálogo e atinja a faixa Warning com perda percentual ativa.
   - Compensação de tempo suprimido: em `SyntheticFlightOracle.cs`, quando o voo sob abuso tem duração de simulação menor do que o voo controle (por cancelamento ou buraco de dados `DataFrameGap`), a verificação de perda estritamente maior leva em conta a perda de tempo natural.

## Classificação das Falhas Reproduzidas (14 Falhas da Rodada 1)

| Semente | Aeronave | Falha Observada | Classificação | Causa Raiz e Resolução |
|---|---|---|---|---|
| **899** | Baron G58 | `PositiveGExceeded` não disparou | **Gerador / Plano** | `PositiveGExceeded` e `NegativeGExceeded` agendados para o mesmo instante (`0.50 * duration`); o segundo sobrescreveu `gForce` no frame. Corrigido com escalonamento temporal. |
| **1145** | Cessna C152 | `PositiveGExceeded` não disparou | **Gerador / Plano** | Mesma causa da 899 (colisão de G positivo e negativo no mesmo segundo). |
| **1271** | Baron G58 | `PositiveGExceeded` não disparou | **Gerador / Plano** | Mesma causa da 899 (colisão de G positivo e negativo no mesmo segundo). |
| **1391** | Baron G58 | `PositiveGExceeded` não disparou | **Gerador / Plano** | Mesma causa da 899 (colisão de G positivo e negativo no mesmo segundo). |
| **1550** | C172SP G1000 | `NegativeGExceeded` não disparou | **Gerador / Plano** | Colisão de G: `PositiveGExceeded` veio após `NegativeGExceeded` e sobrescreveu `gForce`. Corrigido com separação temporal. |
| **1664** | TBM 930 | `NegativeGExceeded` não disparou | **Gerador / Plano** | Sorteados 2 cancelamentos no mesmo voo; o primeiro cancelou no instante exato de início do G negativo. Corrigido limitando a 1 cancelamento após abusos. |
| **1139** | Baron G58 | `HardLanding` não disparou | **Gerador / Plano** | Hard landing marcado para `0.85 * duration`, e cancelamento agendado para `0.88 * duration`. Voo cortado no ar antes de `tTouchdown` (`0.90 * duration`). Corrigido alinhando hard landing com o toque. |
| **1235** | Baron G58 | `HardLanding` não disparou | **Gerador / Plano** | Mesma causa da 1139 (cancelamento no ar antes do toque). |
| **1598** | C172SP G1000 | `ThermalShock` não disparou | **Gerador** | Duração do voo estendida por bateria; choque térmico caiu em cruzeiro onde o gerador resetava CHT para 155 °C em todo tick. Corrigido preservando choque térmico em cruzeiro. |
| **1133** | Cessna C152 | `BatteryDischarging` não disparou | **Gerador / Plano** | `BatteryDischarging` e `ElectricalOverload` no mesmo voo; sobrecarga religou o alternador e elevou tensão para 25.5V, anulando a descarga profunda. Corrigido tornando-os mutuamente exclusivos. |
| **1649** | Cessna C152 | `BatteryDischarging` não disparou | **Gerador / Plano** | Mesma causa da 1133 (conflito elétrico antagônico). |
| **1505** | Cessna C152 | `ElectricalOverload` não disparou | **Gerador / Plano** | Conflito elétrico inverso: `BatteryDischarging` desligou o alternador após sobrecarga tentar ativá-lo. Corrigido tornando-os mutuamente exclusivos. |
| **881** | Cessna C152 | Perda nas válvulas menor que no controle | **Gerador + Gabarito** | EGT usava valor fixo 880 °C (abaixo de `egtMax` 893.52 °C, faixa Caution com perda 0) e voo teve `DataFrameGap` de 15s. Corrigido multiplicando por `egtMax` e compensando delta de tempo no oráculo. |
| **895** | Baron G58 | Perda nas válvulas menor que no controle | **Gerador** | EGT usava 880 °C (faixa Caution com perda 0) e `LongStart` no motor 2 atrasou a decolagem do motor 1 em 35s comparado ao controle. Corrigido forçando faixa Warning (`egtMax * (1 + intensity)`). |

*Nota: Nenhuma das falhas foi bug de lógica de produção em `Domain/Parts`.*

## Resposta ao Item 4 — Frame Real com Chaves Exatas e Indexadas (`PartWearEngine`)

Em `src/FlightCareer.Domain/Parts/PartFlightTracker.cs`, método `DecorateSensorFrame` (linhas 490 a 494):
```csharp
dict[$"eng-cht-cooling-rate:{eng}"] = coolingRate;
if (eng == 1)
{
    dict["eng-cht-cooling-rate"] = coolingRate;
}
```
O `PartFlightTracker` adiciona **ambas as chaves no mesmo frame**:
1. `eng-cht-cooling-rate:1` (chave indexada do motor 1);
2. `eng-cht-cooling-rate` (chave global desindexada do motor 1).

Se o `PartWearEngine` priorizar a chave global desindexada `frame.Values.ContainsKey(sensorKey)`, ele assume que a regra é global (`isPerEngineRule = false`), avalia sem índice de motor (`engIdx = null`), gera a excedência em `engine.cylinders` (sem `:1`) e anotação sem "Motor 1". Consequentemente, o oráculo e os sistemas de telemetria falham ao tentar verificar o motor alvo (`targetEngineMatched`).
Por essa razão, a prioridade para `chave:1` (`!DisablePerEngineSensorResolution && frame.Values.ContainsKey($"{sensorKey}:1")`) é estritamente necessária para que qualquer sensor decorado por motor seja avaliado por motor. Mantida a ordem com `chave:1` prioritária.

## Lista de Aeronaves sem Limites no Catálogo (Item 2)

Total de aeronaves cadastradas no catálogo: 46.

1. **Sem `vne` (26/46):**
   737 Max 8, 747-8i, A310, A330-200, A330-BelugaXL, Airbus A321LR, Airbus A330-300, ATR 42-600, ATR 72-600, Beechcraft Baron G58, Beechcraft Bonanza V35, Beechcraft Bonanza, Beechcraft King Air 250, Beechcraft King Air C90GTx, Boeing 747-400 Global Supertanker, Boeing 747-400, Boeing 787-10 Dreamliner, C208B, Cessna 188 AGTruck, Cessna 207T Stationair, Cessna 404 Titan, Cessna 408 SkyCourier, Cessna Citation Longitude, Cirrus SR22, Pilatus PC-24, Piper J-3 Cub.

2. **Sem `vfe` (22/46):**
   Airbus A321LR, Airbus A330-300, ATR 42-600, ATR 72-600, Beechcraft Baron G58, Beechcraft Bonanza V35, Beechcraft Bonanza, Beechcraft King Air 250, Beechcraft King Air C90GTx, Boeing 747-400 Global Supertanker, Boeing 747-400, Boeing 787-10 Dreamliner, C208B, CAP-4, Cessna 188 AGTruck, Cessna 207T Stationair, Cessna 404 Titan, Cessna 408 SkyCourier, Cessna Citation Longitude, Cirrus SR22, Pilatus PC-24, Piper J-3 Cub.

3. **Sem `elecMainBusIndex` (44/46):**
   *Apenas Cessna C152 e C172SP G1000 possuem.* Todas as outras 44 aeronaves estão sem o parâmetro.

4. **Sem `elecAvionicsCircuitIndex` (45/46):**
   *Apenas C172SP G1000 possui.* Todas as outras 45 aeronaves estão sem o parâmetro.

5. **Sem `alternatorRatedAmps` (44/46):**
   *Apenas Cessna C152 e C172SP G1000 possuem.* Todas as outras 44 aeronaves estão sem o parâmetro.

## Resumo do Fuzz de 1000 Voos Final

Comando executado: `dotnet run --project src/FlightCareer.Replay -- fuzz --runs 1000 --seed 777`

```
================================================================================
           FlightCareer V6 — Fuzz Testing com Voos Sintéticos                    
================================================================================
Corridas planejadas: 1000
Semente base       : 777
Modo               : Completo (Invariantes + Gabarito com Voo-Controle)

--------------------------------------------------------------------------------
Resultado do Fuzz Testing:
  Total de Voos       : 1000
  Voos Aprovados      : 847 (84,7%)
  Voos c/ Limite Aus. : 153 (15,3%)
  Voos com Falhas     : 0 (0,0%)
  Tempo Total         : 491,71 s (2 voos/s)
--------------------------------------------------------------------------------
Resumo dos Limites Ausentes no Catálogo:
  • limite ausente: elecAvionicsCircuitIndex: 58 ocorrências
  • limite ausente: alternatorRatedAmps: 45 ocorrências
  • limite ausente: elecMainBusIndex: 32 ocorrências
  • limite ausente: vfe: 22 ocorrências
  • limite ausente: vne: 14 ocorrências

✅ Nenhuma invariante ou gabarito violado em todas as corridas executadas!
================================================================================
```

## Critérios de aceite
- [x] Fuzz de 1000 voos executado com 0 falhas e 0 violações de gabarito.
- [x] 14 falhas da rodada 1 reproduzidas e classificadas (Produção × Gerador × Gabarito).
- [x] Abusos condicionados a limites voltaram a ser sorteados em todas as aeronaves.
- [x] Limites ausentes registrados em categoria própria na CLI e no relatório.
- [x] Levantamento completo das aeronaves sem limites do catálogo incluído no relatório.
- [x] Tempos mínimos de acumulador contínuo obtidos dinamicamente de `wear_rules.json` sem números mágicos.
- [x] Frame real com chaves duplicadas identificado e evidenciado no relatório (`PartFlightTracker.cs`).
- [x] 100% dos testes unitários passando (`dotnet test src/FlightCareer.Tests`: 463/463).
- [x] `dotnet build src/FlightCareer.slnx` com 0 erros.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 463 aprovados, 0 falhas.
- `FlightCareer.Replay fuzz --runs 1000 --seed 777`: 847 aprovados (100% dos válidos), 153 limites ausentes segregados, 0 falhas.

## Não testado
- Conexão ao vivo com o simulador MSFS 2024 em execução (os testes cobrem telemetria sintética e catálogo estático).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente.
