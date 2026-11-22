# Spec FASES-2C — Contratos das fases 3 a 7

**Objetivo (1 frase):** criar os tipos de contrato das fases 3–7 do desenho do usuário como subtipos novos do gerador (distância, passageiros, carga, destinos e prazo coerentes com cada fase), sem regra de conclusão nova além do pouso no destino.

## Fatos já levantados (não redescobrir)
- Desenho: `docs/plans/fases-carreira.md` › § 2 e § 3. Decisões: fase limita o **aceite** (não a compra); o pagamento nunca depende de nota nem de pontualidade (só o XP: bônus de prazo já existe no `XpCalculator`, ETE = distância direta / cruzeiro + 15 min).
- Base com FASES-1, FASES-2A e FASES-2B mergeadas. Padrão: subtipos em `MissionRules.cs` (`CategoryConfig`/`SubCategoryConfig` com `RangeRules`, `PaxRules`, `LbsRules`, `Urgency`, `RequiresHardSurface`, `MinRunwayDest`, `MinExpHours/MaxExpHours`), fase por subtipo em `career_phases.json`, família de pagamento em `mission_pay.json` (calibração em `economia-estabilidade.md` › 4.4: famílias novas a escala **igual à da família existente mais parecida**, para não quebrar a curva), M_Tipo em `xp_rules.json`.
- O primeiro dígito do subtipo é o tier do aeroporto de origem (1 hub, 2 regional, 3 municipal, 4 remoto). `Airport` tem `Country`, `Tier`, lat/lon; pistas em `Runways`.
- Missões já existentes por fase (não duplicar): ver a tabela § 3 do plano.

## Contratos novos
| Fase | Contrato | Regras do gerador | Família / M_Tipo |
|---|---|---|---|
| 3 | **Fretamento executivo** | origem tier 1–2 (capital/hub) → destino tier 3–4 do mesmo país sem linha regular; 2–6 pax; 100–400 NM | `taxi_aereo` / 1,25 |
| 3 | **Carga frágil / expressa** | 100–800 lb, 80–400 NM, `Urgency` 1,3, expiração curta (1–3 h) | família nova `carga_fragil` (escala da `aog`) / 1,35 |
| 4 | **MEDEVAC dinâmico** | destino com maior pista ≤ 4.000 ft ou não pavimentada; 1–2 pax (paciente + acompanhante) + 300–600 lb de equipamento; `Urgency` 1,5; expiração 1–2 h; peso maior que os outros na fase 4 | `medevac` / 1,50 |
| 4 | **VIP internacional curto** | destino em **país vizinho** (`Country` diferente, 300–900 NM); 2–8 pax | `taxi_aereo` / 1,25 |
| 6 | **Ponte aérea / troncal** | hub ↔ hub do mesmo país (tier 1 ↔ tier 1), 150–220 pax, 150–700 NM | `linha` / 1,25 |
| 6 | **Cargueiro noturno** | carga 20.000–60.000 lb entre hubs, 300–1.500 NM; texto "voo noturno" (sem regra de horário: não há período do dia confiável) | `linha` / 1,00 (carga) |
| 7 | **Long-haul internacional** | > 2.500 NM entre hubs de países diferentes; 200–400 pax | `linha` / 1,25 |
| 7 | **Fretamento global / carga de alto valor** | > 2.000 NM, carga 30.000–100.000 lb, `Urgency` 1,2 | família nova `carga_global` (escala da `linha`) / 1,35 |

- O **multi-leg sequencial** da fase 5 já existe; só conferir que fica liberado a partir da fase 5 (mapa no `career_phases.json`).
- Use os ids `x.7.n` (ou o próximo livre) e registre todos no `career_phases.json`.

## Fora do escopo
- Regras de conclusão novas, horário real (noite), alternados, ETOPS, fadiga/manutenção antes da travessia (ficam como texto da descrição).

## Testes
- Gerador (sementes fixas): cada contrato respeita distância, pax, carga, tier/país de destino e fase; VIP internacional sempre em outro país; ponte aérea sempre hub ↔ hub do mesmo país; long-haul > 2.500 NM.
- `career_phases.json` cobre todos os subtipos novos (teste que falha se um subtipo do `MissionRules` não tiver fase).
- Pagamento: famílias novas usam a escala da família de referência.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `career-stab --days 90 --bots 1` roda.
- [ ] CHANGELOG e AGENTS.md › 7. Relatório diz "não visto na tela".
- [ ] Um commit: `feat(missoes): contratos das fases 3 a 7 (FASES-2C)`.
