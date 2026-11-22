# Spec FASES-2A — Contratos novos da fase 1: voo panorâmico, circuito de TGL e translado básico

**Objetivo (1 frase):** criar os três contratos da fase 1 que o usuário pediu, com regra de conclusão verificada pela telemetria (pontos sobrevoados e número de toques), pagos e com XP pelas regras existentes.

## Fatos já levantados (não redescobrir)
- Desenho: `docs/plans/fases-carreira.md` › § 3 (fase 1) e § 1 (decisões). Fases já implementadas (`CareerPhase`, `career_phases.json` com o mapa subtipo → fase; FASES-1).
- Subtipos: `Application/Services/MissionRules.cs` (`CategoryConfig`/`SubCategoryConfig`: `Id`, `Name`, `Description`, `Weight`, `RangeRules`, `PaxRules`, `LbsRules`, `MinExpHours`, `MaxExpHours`, `Urgency`, `RequiresHardSurface`, `MinRunwayDest`). O treinamento usa `3.4.x` ("Voo de Instrução Local" 3.4.1 já diz "pousos (TGL)", mas hoje é só um destino A→B). O primeiro dígito do subtipo é o tier do aeroporto de origem; a categoria "3.4 Treinamento" aparece nos tiers 2, 3 e 4.
- Pagamento: `MissionGenerator.CalculateReward` + `mission_pay.json` (família por prefixo; `instrucao_basica` = "3.4.1"). XP: `Domain/Xp/XpCalculator` + `xp_rules.json` (M_Tipo por família; ferry 0,80).
- Fim de voo: `FlightService.ProcessFlightFinishAsync` paga a missão quando `mission.DestinationICAO == destinationIcao`. `FlightMonitor` (**arquivo quente**) já conta toques (`_flightTouchdowns`, `_flightLandingsCount`, `HandleTouchdown`/`RegisterLanding`) e a posição (`_lastLat/_lastLon`).
- `Mission.SubId` gravado; `Airports` (lat/lon, ICAO, tier, elevação) no banco.

## Contratos
1. **Voo panorâmico** (subtipo novo `3.5.1`, família nova `panoramico`, fase 1, M_Tipo 1,15 como turismo): origem = destino; 2 ou 3 **pontos de sobrevoo** = aeroportos/aeródromos entre 8 e 30 NM da origem (como "pontos de interesse" da região, já que não há banco de POIs), em ordem de rota razoável; 1–3 passageiros.
   - Conclusão: pousar de volta na origem **e** ter passado a ≤ 2 NM de **todos** os pontos, abaixo de 3.000 ft acima da elevação do ponto. Ponto faltando → missão não concluída (sem pagamento, volta ao mercado com o mesmo valor, como a regra de "descarregar").
2. **Circuito de TGL** (subtipo novo `3.5.2`, família `instrucao_basica`, fase 1, M_Tipo 1,10): origem = destino; exige N toques-e-arremetidas (3 a 6, sorteado) e um pouso final completo na origem; 0–1 passageiro (instrutor).
   - TGL = toque (não quique) seguido de nova decolagem sem parar (GS > 20 kt entre o toque e a decolagem). Conclusão: TGL contados ≥ N e pouso final na origem. Faltando → não concluída (como acima).
3. **Translado básico** (subtipo novo `3.5.3`, família nova `translado` com M_Tipo 0,80 como ferry, fase 1): aeródromo vizinho a < 80 NM, **só piloto e bagagem** (0 passageiros, carga ≤ 50 lb), avião "de manutenção" (só texto). Conclusão: pouso no destino (regra atual).
- Os três entram numa categoria nova "3.5 Aeroclube" nos tiers de origem 2, 3 e 4, com peso parecido ao do "Treinamento". Quadro de missões: mostrar os pontos do panorâmico e o número de TGL na descrição/detalhe.

## Dados e código
- `Mission`: `RequiredTouchAndGoes` (int, 0 = não se aplica) e `OverflyWaypoints` (string, ex. `"SBXX;SDYY;SNZZ"`) → colunas novas em `EnsureSchemaUpdatedAsync` (**quente**, só as duas linhas).
- `mission_pay.json`: famílias `panoramico` (prefixo `3.5.1`; taxa base 300, 6,5/NM do percurso total, 0,10/lb, escala 1,0) e `translado` (prefixo `3.5.3`; base 250, 5,0/NM, escala 1,0); `3.5.2` cai em `instrucao_basica` (incluir o prefixo). `xp_rules.json`: M_Tipo `panoramico` 1,15 e `translado` 0,80. `career_phases.json`: `3.5.x` → fase 1.
- Lógica pura (Domain, testável): `MissionCompletion.Evaluate(mission, landedIcao, visitedWaypoints, touchAndGoCount)` → concluída / não concluída + motivo; `OverflyTracker` (marca pontos visitados pela posição, raio 2 NM, teto 3.000 ft acima da elevação); `TouchAndGoCounter` (sequência toque → GS > 20 kt → decolagem).
- `FlightMonitor` (**quente**, só o necessário): alimentar `OverflyTracker` e `TouchAndGoCounter` com a telemetria e passar o resultado ao `FlightService` (parâmetro opcional novo). `FlightService`: para missões com pontos ou TGL, usar `MissionCompletion` antes de pagar; não concluída → devolver ao mercado (mesmo valor) e linha no resultado do voo dizendo o que faltou.
- Distância do panorâmico para pagamento/ETE: origem → pontos → origem.

## Fora do escopo
- Fotografia aérea, paraquedistas e contratos das fases 2–7 (FASES-2B); banco de POIs reais.

## Testes
- `MissionCompletion`: panorâmico com todos os pontos e pouso na origem → concluída; faltando um → não; pouso em outro aeroporto → não. Circuito: 4 TGL exigidos, 4 feitos + pouso final na origem → concluída; 3 → não.
- `TouchAndGoCounter`: toque + GS 40 kt + decolagem = 1; quique (toque e volta ao ar em < 1 s sem rolar) não conta; toque, parada completa (GS < 5 kt) e nova decolagem → não é TGL (é pouso completo).
- `OverflyTracker`: passar a 1,5 NM e 2.000 ft acima → visitado; a 3 NM → não; a 1 NM mas 5.000 ft acima → não.
- Gerador: panorâmico com 2–3 pontos a 8–30 NM, origem = destino; translado com 0 pax e ≤ 50 lb a < 80 NM; fase 1 nos três.
- Banco antigo ganha as colunas (teste de persistência).

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz `--aircraft all --runs 10` sem regressão.
- [ ] Arquivos quentes só no indicado. CHANGELOG e AGENTS.md › 7. Relatório diz "não testado no MSFS / não visto na tela".
- [ ] Um commit: `feat(missoes): voo panoramico, circuito de tgl e translado basico (FASES-2A)`.
