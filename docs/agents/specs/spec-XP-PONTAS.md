# Spec XP-PONTAS — XP estimado no quadro, infrações que faltam, pista difícil no pagamento e peças coerentes no usado

**Objetivo (1 frase):** fechar as pontas do XP e da economia que não dependem do simulador: mostrar "≈ XP" no quadro de missões, medir as infrações de estol, luzes e 250 kt, dar o bônus de pista difícil pelo tipo de pista (não pelo tier) e provisionar as peças do avião usado com horas/ciclos coerentes com a saúde.

## Fatos já levantados (não redescobrir)
- XP: `Domain/Xp/XpCalculator`, `XpFlightInputs` (campos `VneOrStallInfractions`, `LightsOrSpeedInfractions` hoje alimentados com 0 para estol/luzes/velocidade), `xp_rules.json`. Plano: `docs/plans/xp-proficiencia.md` › 6.4 (infrações: estol fora de instrução −0,15; luzes de pouso/táxi esquecidas ou > 250 KIAS abaixo de 10.000 ft −0,05 cada) e › 3 ("Quadro de missões: mostra ≈ XP com a mesma fórmula, minutos no ar pela distância e velocidade de cruzeiro, qualidade 1,0, bônus da pista de destino").
- Quadro: `UI/ViewModels/MissionDisplayItem.cs` (recebe missão, aeroportos de origem/destino, avião ativo e fase).
- Pagamento: `MissionGenerator.CalculateReward` aplica `bushBonus = destTier == 4 ? 1,25 : 1,00` (~linha 770). Achado do plano (§ 4.3): o bônus deve vir da **pista** (não pavimentada ou curta), não do tier. `Runway` tem `LengthFt` e `Surface`; o `FlightMonitor` já classifica superfície dura (`ASPH`, `CON`, `CONC`, `HARD`, `BIT`, `PEM`, `ASP`) para o XP — **reuse essa regra num único lugar do Domain** (ex. `RunwaySurface.IsUnpaved(string)`) e troque as duas cópias.
- Telemetria: `SimTelemetry`/`AddToDef` (**quentes**; só acrescentar no fim, mesma ordem e unidade; `TelemetryDefinitionTests`). Confirmar no SDK (`docs/plans/sensor-research/simvars-msfs2024.csv`) os nomes `STALL WARNING` (Bool) e `LIGHT LANDING` (Bool) / `LIGHT TAXI`.
- Peças do usado: `MarketSimulationService` (TSMO ≤ TBO já é coerente) chama `PartMigrationService.ProvisionPurchasedAircraft`, que cria peças com saúde ~50 % mas `HoursSinceReplacement`/`CyclesSinceReplacement` = 0 (voo real: PP-UZJ com 7.221 h de célula e peças a ~50 % com 0 h).
- Mantém a calibração do pagamento (`economia-estabilidade.md` › 4.4): o bônus de pista continua +25 %, só muda **quem** recebe.

## Passo a passo
1. **≈ XP no quadro:** função pura `XpCalculator.Estimate(...)` (ou `XpEstimator`) com minutos no ar = distância / cruzeiro × 60, 1 perna (circuito/TGL: N + 1), M_Tipo da família, realismo e qualidade 1,0, bônus de pista da origem/destino, prazo "no prazo" (+15). `MissionDisplayItem` exibe "≈ N XP" ao lado do pagamento.
2. **Infrações** (`FlightMonitor`, **quente**, só o necessário; campos novos no fim do `SimTelemetry`):
   - estol: borda de subida de `STALL WARNING` no ar → +1, **exceto** em missão de instrução (`instrucao_basica`, `instrucao`);
   - luzes: farol de pouso **e** de táxi desligados no instante da decolagem → +1; idem no toque final → +1;
   - velocidade: borda de subida de IAS > 250 kt com altitude < 10.000 ft → +1.
   - Somar em `VneOrStallInfractions` (estol) e `LightsOrSpeedInfractions` (luzes, 250 kt); linhas no resumo do XP.
3. **Pista difícil no pagamento:** `CalculateReward` recebe a pista do destino (ou um booleano "pista difícil" = não pavimentada **ou** maior pista < 2.500 ft); sem dado de pista, cai no tier 4 como hoje. Ajuste o gerador para carregar as pistas dos destinos (sem consulta por missão: cache por aeroporto).
4. **Usado coerente:** no provisionamento de compra, para cada peça com vida em horas: `HoursSinceReplacement = clamp((100 − saúde) / 30 × vidaHoras, 0, horas de célula)` (o desgaste natural chega a ~30 % no fim da vida, `AGENTS.md` › 7); vida em ciclos: idem com ciclos (limitado ao número de pousos estimado). Peças do motor coerentes com o TSMO (nunca mais horas que o TSMO). `KnownHealth` = saúde (o comprador fez a inspeção pré-compra).

## Fora do escopo
- Calibração dos valores; contratos novos; arquivos quentes além do indicado.

## Testes
- `Estimate`: 60 NM a 100 kt, instrução → ≈ (2 × 36 + 30 + 50) × 1,10 + 15 = 182.
- Infrações (lógica pura, ex. `FlightInfractionTracker`): bordas de estol (não conta em instrução), luzes na decolagem/toque, 250 kt abaixo de 10.000 ft (só borda).
- `RunwaySurface.IsUnpaved`: "ASPHALT" → falso; "GRASS"/"DIRT"/"GRAVEL" → verdadeiro.
- Pagamento: destino tier 2 com pista de grama → +25 %; destino tier 4 com asfalto longo → sem bônus; sem dado de pista → regra antiga.
- Usado: peça a 55 % com vida 2.000 h → 3.000 h? Não: limitado pelas horas de célula e pelo TSMO no motor; teste com 7.221 h de célula, motor TSMO 0,8 h → peças do motor ≤ 0,8 h.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz `--runs 10` sem regressão.
- [ ] CHANGELOG e AGENTS.md › 7. Relatório diz "não testado no MSFS".
- [ ] Um commit: `feat(xp): xp estimado no quadro, infracoes, pista dificil no pagamento e usado coerente (XP-PONTAS)`.
