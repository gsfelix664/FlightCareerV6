# Spec XP-NOVO — XP por esforço × habilidade + bônus fixos, e nível do piloto

**Objetivo (1 frase):** trocar o cálculo de XP do fim de voo pelo modelo aprovado (`docs/plans/xp-proficiencia.md` › 6), com XP base por minuto no ar, distância e perna, multiplicadores de tipo, realismo e qualidade, bônus fixos de pista, ambiente e prazo, e nível do piloto pela curva 300 × (L − 1)².

## Fatos já levantados (não redescobrir)
- Leia `docs/plans/xp-proficiencia.md` §§ 6.1–6.8 e `docs/plans/fases-carreira.md` § 6 (nível sem limite; fases ficam para a tarefa FASES).
- XP atual: `FlightService.ProcessFlightFinishAsync` (`src/FlightCareer.Application/Services/FlightService.cs`, ~linhas 258–265): `baseXP = minutos + 0,5 × NM`, `+ 20 % × tier`, `× qualityMultiplier`, depois `ProcedurePenalties.ApplyAutoStartPenalty` (−10 %). Voo invalidado já zera o XP (correção de 2026-11-22). Perda total: XP 0 (`ProcessTotalLossAsync`).
- Qualidade: `Domain/Services/FlightQualityService` (nota 0–100 %, `TouchdownFPM`, `ComfortPenaltyPoints`, separação carga × passageiro). O toque vem do `TouchdownDetector` (velocidade normal ao solo do MSFS); com TGL há vários toques.
- Tipo da missão: `Mission.SubId` (gravado desde 2026-11-22; nulo em missões antigas → usar M_Tipo 1,0) → família em `Infrastructure/Data/mission_pay.json` (`MissionPayTable.Resolve`). Pistas: `Runway.LengthFt`, `Runway.Surface`; aeroportos têm elevação; `AircraftType.MinRunwayLength`, `CruiseSpeedKts`. Vento cruzado: `SimTelemetry.CrosswindComponentKnots`.
- SimVars confirmadas no SDK (`docs/plans/sensor-research/simvars-msfs2024.csv`): `SIMULATION RATE`, `REALISM CRASH DETECTION` (Bool), `AUTO COORDINATION` (Bool), `REALISM` (Number, %), `AMBIENT VISIBILITY` (Meters), `AMBIENT IN CLOUD` (Bool), `DENSITY ALTITUDE` (ft), `AMBIENT WIND VELOCITY` (Knots). **Não existem** SimVars de clima em tempo real nem de dano por estresse: esses itens ficam fora. Período do dia (noite): só implemente se achar a variável no SDK (`TIME OF DAY` é variável de ambiente; confirme o nome e o acesso por SimConnect); se não confirmar, deixe o bônus noturno desligado e declare.
- A pausa não é detectada de forma confiável hoje: o item "sem pausa" do realismo **fica fora**.

## Contrato (lógica pura em `Domain/Xp/`, testável)
```csharp
public sealed record XpFlightInputs(
    double AirborneMinutes, double DistanceNm, int LegsCompleted,          // pernas = decolagem + pouso concluídos
    string? MissionFamilyId, bool IsFerryOrNoMission,
    bool UsedTimeAcceleration, bool CrashDetectionOn, bool AutoCoordinationOn, double RealismPct, bool AutoStartUsed,
    double WorstTouchdownFpm, double WorstTouchdownG, int TouchdownCount,
    int VneOrStallInfractions, int GExcessInfractionsWithPax, int LightsOrSpeedInfractions, bool OffRunwayOrExcursion,
    RunwayFacts? Origin, RunwayFacts? Destination, EnvFacts? Landing, double? BlockMinutes, double? EteMinutes, bool IsUrgentMission);
public sealed record RunwayFacts(double AvailableFt, double MinRequiredFt, bool Unpaved, double ElevationFt);
public sealed record EnvFacts(double CrosswindKt, bool IsJet, double GustSpreadKt, double DensityAltitudeFt, bool Night, double VisibilityM, bool InCloud);
public sealed record XpBreakdown(double Base, double MType, double MRealism, double MQuality, double Bonuses, int Total, IReadOnlyList<string> Lines);
public static class XpCalculator { public static XpBreakdown Compute(XpFlightInputs i); }
public static class PilotLevel { public static int ForXp(long xp); public static long XpForLevel(int level); } // 300 × (L − 1)², sem nível máximo
```
- **Base** = 2 × minutos no ar + 0,5 × NM + 50 × pernas.
- **M_Tipo** (§ 6.2): ferry/sem missão 0,80; `carga_expressa`/`fallback` 1,00; `instrucao_basica`/`instrucao` 1,10; `agro_municipal`/`agro_bush` 1,15; `taxi_aereo`/`regional`/`linha` 1,25; `aog` 1,35; `medevac` 1,50; desconhecido 1,00. Valores num JSON novo `Infrastructure/Data/xp_rules.json` (calibração em dados), carregado com fallback embutido igual.
- **M_Realismo** = 1,0 + 0,05 por item: sem aceleração no voo; detecção de colisão ligada; auto-leme desligado; `REALISM` ≥ 100. Partida automática: −0,10. Piso 0,8, teto 1,2.
- **M_Qualidade** = 1,0 + toque (pior toque da perna; com TGL vale o pior): < 120 fpm e < 1,15 G → +0,15; 120–250 fpm e < 1,4 G → +0,05; 251–450 → −0,10; > 450 fpm ou > 2,0 G → −0,30. Infrações: Vne/estol −0,15 cada (estol não conta em missão de instrução); G > 2,2 ou < −0,5 com passageiros −0,15 cada; luzes/velocidade −0,05 cada; fora da pista −0,40 e zera os bônus de pouso (pista e ambiente do destino). Piso 0,5, teto 1,15. Fonte das infrações: o que o `FlightQualityService` e o `FlightMonitor` já medem; o que não for medido hoje entra como 0 (declare quais).
- **Bônus** (§ 6.5): pista curta (disponível entre 1,0 e 1,25 × mínimo) +40; não pavimentada +30; combo (curta + não pavimentada + elevação > 4.000 ft) +30; vento cruzado > 12 kt (monomotor/turboélice) ou > 20 kt (jato) +35; rajadas > 8 kt (diferença entre o vento máximo e mínimo nos últimos 3 min antes do toque) +20; elevação > 4.000 ft ou altitude-densidade > 6.000 ft +25; noite +30 (só se confirmado); IMC (visibilidade < 4.828 m ou em nuvem na aproximação) +50; prazo: ETE = distância / cruzeiro + 15 min, tempo de bloco ≤ 90 % → +30 (+60 urgente), 90–115 % → +15, acima → 0. Pista conta origem **ou** destino (o maior).
- **Total** = round(Base × M_Tipo × M_Realismo × M_Qualidade + Bônus). `Lines`: uma linha por componente, para o resumo do voo.

## Integração (arquivos quentes: só o necessário)
- `FlightMonitor`: juntar durante o voo os fatos da `XpFlightInputs` que dependem do simulador (minutos no ar, pernas/toques e pior toque, aceleração usada, vento da aproximação para rajadas, visibilidade/nuvem na aproximação). As SimVars novas (`REALISM CRASH DETECTION`, `AUTO COORDINATION`, `REALISM`, `AMBIENT VISIBILITY`, `AMBIENT IN CLOUD`, `DENSITY ALTITUDE`) entram **no fim** do `SimTelemetry` e do `AddToDef` (mesma ordem e unidade; `TelemetryDefinitionTests` atualizado).
- `FlightService.ProcessFlightFinishAsync`: trocar o cálculo de XP por `XpCalculator.Compute` (receber os fatos num parâmetro novo opcional; sem fatos → comportamento seguro: base × M_Tipo × qualidade da nota atual) e gravar o detalhamento no resumo do voo (`outcome.Message` ou campo novo de linhas).
- `PilotCategory.GetRequiredXP`: novos valores 10.000 / 45.000 / 175.000 / 600.000 (§ 6.6) até a tarefa FASES substituir as categorias.
- `Pilot`: expor `Level` calculado (`PilotLevel.ForXp`), sem coluna nova; mostrar "Nível N" onde hoje aparece o XP (tela principal/perfil).
- `CareerProgressionSimulator`: usar o `XpCalculator` com fatos estimados (qualidade 1,0; realismo 1,0; bônus de pista pelo aeroporto; sem ambiente; prazo "no prazo") para os bots promoverem pelos valores novos.

## Fora do escopo
- As 7 fases (tarefa FASES), contratos novos, pesos por tipo da § 5.2 além da separação carga × passageiro já existente, quadro de missões com "≈ XP" (pode vir depois).

## Testes (`XpCalculatorTests`, `PilotLevelTests`)
- Exemplo do plano: 40 min no ar, 60 NM, 1 perna → base 160; instrução (1,10), realismo 1,0, toque 200 fpm/1,2 G (×1,05), sem bônus → 160 × 1,10 × 1,05 = 184,8 → 185.
- Pior toque vale (TGL: 90, 500 e 150 fpm → −0,30); pista curta de terra a 5.000 ft → +40 + 30 + 30 + 25; fora da pista zera bônus de pouso; pisos e tetos dos multiplicadores; família desconhecida = 1,0; ferry 0,8.
- `PilotLevel`: XP 0 → nível 1; 7.500 → 6; 43.200 → 13; 580.800 → 45; 10 milhões → nível > 50 (sem limite); `XpForLevel(ForXp(x)) ≤ x`.
- Fim de voo (SQLite em memória, padrão de `CareerClockTests`): XP gravado = `XpCalculator`; voo invalidado continua 0.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz `--aircraft all --runs 20` sem regressão.
- [ ] Campos novos só no fim do `SimTelemetry`/`AddToDef`, com teste de contagem.
- [ ] `xp_rules.json` com os multiplicadores e bônus; nenhum número de calibração espalhado no código.
- [ ] CHANGELOG e AGENTS.md › 7. Relatório diz o que ficou fora (noite, se não confirmado; infrações não medidas) e "não testado no MSFS".
- [ ] Um commit: `feat(xp): xp por esforco e habilidade com bonus fixos e nivel do piloto (XP-NOVO)`.
