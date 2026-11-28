# Spec FASES-2B — Contratos da fase 2: levantamento/fotografia aérea e lançamento de paraquedistas

**Objetivo (1 frase):** criar os dois contratos de trabalho aéreo da fase 2 com conclusão verificada pela telemetria — pernas paralelas com altitude e velocidade constantes sobre uma área, e subida até a altitude de salto, estabilização na zona e descida sem choque térmico.

## Fatos já levantados (não redescobrir)
- Desenho: `docs/plans/fases-carreira.md` › § 3 (fase 2). Padrão a seguir: FASES-2A já implementada — `Domain/Missions/` (`MissionCompletion`, `OverflyTracker`, `TouchAndGoCounter`), `Mission.OverflyWaypoints`/`RequiredTouchAndGoes`, `FlightMonitor` alimenta os rastreadores e passa o resultado ao `FlightService.ProcessFlightFinishAsync` (parâmetros opcionais), que só devolve ao mercado o contrato **que pousou no destino sem cumprir** (regra da revisão FASES-2A; não quebrar o multi-leg).
- Subtipos em `MissionRules.cs`; categoria "3.5 Aeroclube" criada na FASES-2A; fase por subtipo em `career_phases.json`; pagamento por família em `mission_pay.json` (calibração em `economia-estabilidade.md` › 4.4 — famílias novas a escala 1,0); M_Tipo em `xp_rules.json`.
- Choque térmico já é medido: `PartFlightTracker` calcula `eng-cht-cooling-rate` (°C/min, janela de 60 s) contra `chtCoolingRateMax` (28 °C/min, pistão) — reuse o mesmo cálculo/limite.

## Contratos (fase 2)
1. **Levantamento / fotografia aérea** (subtipo `3.6.1`, família nova `aerofoto`, M_Tipo 1,15): área retangular de 2–4 NM de lado centrada entre 5 e 25 NM da origem; altitude-alvo (2.000–6.000 ft MSL, sorteada e acima do terreno + 1.000 ft) e velocidade-alvo (cruzeiro do avião × 0,75, arredondada); **N pernas paralelas** (3–5) cruzando a área de lado a lado, orientação sorteada. Origem = destino (volta ao aeroporto).
   - Conclusão: completar as N pernas — cada perna conta se o avião cruzar a área de uma borda à oposta com rumo dentro de ±15° da orientação, **altitude ±150 ft** e **velocidade ±10 kt** do alvo durante ≥ 80 % do trecho dentro da área; e pousar na origem.
2. **Lançamento de paraquedistas** (subtipo `3.6.2`, família nova `paraquedismo`, M_Tipo 1,15): zona de salto = o próprio aeródromo de origem; altitude de salto 10.000–12.000 ft AGL (sorteada); 4–10 passageiros (paraquedistas) limitado ao avião.
   - Conclusão: subir até ≥ altitude de salto − 200 ft a ≤ 2 NM da zona; manter ±200 ft e IAS ≤ 1,3 × Vs1 (ou ≤ 90 kt se não houver Vs1) por ≥ 30 s sobre a zona (= "salto feito"); descer e pousar na origem **sem** choque térmico (taxa de resfriamento da CHT abaixo do limite da classe durante toda a descida) — se houver choque térmico, o contrato é concluído e pago, mas a linha do resultado avisa e o desgaste vem da regra já existente (não duplicar dano).
   - Paraquedistas **não** contam como passageiros no pouso (taxa de embarque) depois do salto.

## Dados e código
- `Mission`: campos para a geometria — use uma coluna `ContractData` (texto JSON com os parâmetros: centro, lados, orientação, N pernas, altitude/velocidade-alvo; ou altitude de salto) em vez de muitas colunas; coluna nova em `EnsureSchemaUpdatedAsync` (**quente**, só uma linha).
- Lógica pura: `SurveyLegTracker`, `SkydiveDropTracker` em `Domain/Missions/`, e `MissionCompletion` estendido para os dois tipos (motivo claro quando faltar algo: "2 de 4 pernas", "não estabilizou na altitude de salto").
- `FlightMonitor` (**quente**): alimentar os rastreadores com posição, altitude, rumo, IAS e a taxa de resfriamento da CHT; repassar ao `FlightService`.
- Quadro de missões: descrição com os alvos (ex.: "4 pernas a 3.500 ft e 85 kt, rumo 040°"; "salto a 11.000 ft AGL").
- `career_phases.json`: `3.6.x` → fase 2. `mission_pay.json`: `aerofoto` (base 600, 6,0/NM do percurso estimado) e `paraquedismo` (base 120 por paraquedista + 4,0/NM, `paxAtLeastOne`), escala 1,0.

## Fora do escopo
- Contratos das fases 3–7 (FASES-2C); calibração.

## Testes
- `SurveyLegTracker`: perna reta dentro da tolerância → conta; fora de altitude por > 20 % do trecho → não conta; rumo fora de ±15° → não conta; N pernas exigidas.
- `SkydiveDropTracker`: subida, 30 s estável na zona → salto feito; 20 s → não; fora da zona → não.
- `MissionCompletion` para os dois tipos (concluído/não concluído com motivo); choque térmico só gera aviso.
- Gerador: área 5–25 NM, alvos coerentes com o avião, fase 2; banco antigo ganha `ContractData`.
- Regressão: `UndeliveredMissionTests` continua passando.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; fuzz `--runs 10` sem regressão.
- [ ] Arquivos quentes só no indicado. CHANGELOG e AGENTS.md › 7. Relatório diz "não testado no MSFS / não visto na tela".
- [ ] Um commit: `feat(missoes): levantamento aereo e lancamento de paraquedistas (FASES-2B)`.
