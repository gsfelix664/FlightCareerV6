# Navegação de instrução só de ida (NAV-IDA)

**Estado:** implementado e aprovado (2026-11-27, `docs/reviews/NAV-IDA-revisao.md`). Não testado no app nem no MSFS.

## Problema
Fase 1 em SNZG (tier 3): ~88% dos contratos aceitáveis voltam à origem (instrução local 10, TGL 10, navegação VFR 10, cheque 6, panorâmico 6; translados só de ida 3 + 3). Origem: `achados-voo-sdco.md` › Decisões abertas.

## Regras trazidas pelo usuário (2026-11-27)
Texto colado pelo usuário sobre treinamento/ANAC (**não conferido contra o RBAC 61 vigente**: conferir na spec):
- **Primeiras navegações (duplo comando):** aeródromos a 50–100 NM, pouso fácil, pouco tráfego ou infraestrutura básica de abastecimento; repetir um ou dois destinos conhecidos.
- **Intermediárias e solo:** variar destinos: controlados (torre/APP), espaços aéreos mais complexos, pistas curtas, relevo diferente.
- **Longa (PP):** voo solo de no mínimo 150 NM com dois pousos completos em aeródromos diferentes (rota triangular ou multi-pernas).

## Decisões do usuário (2026-11-27)
1. Criar **navegação VFR só de ida** com **peso 10**.
2. Etapa escolhida **pelo nível**: níveis 1–2 só iniciante (50–100 NM, poucos destinos próximos repetidos); níveis 3–5 entram os intermediários.
3. Longa: **variante na navegação VFR triangular** (`navegacao_vfr`), mínimo 150 NM e dois aeródromos diferentes com pouso completo; sem tipo novo e sem virar requisito de promoção.
4. Peso dos translados: **não** foi pedido mudar.

## Fatos para a spec
- `mission_catalog.json` › `navegacao_vfr`: `baseByTier` {1:0, 2:3, 3:10, 4:6}; perfis `triangle` 80–250, `out_and_back` 80–240, `triangle_tgl` 75–200 NM; destinos tier 3, mesmo país; gerador `vfr_triangle` (`MissionGenerator.CreateVfrTriangleMissionSync`).
- O sorteio (`MissionBoardCore`) não recebe o nível do piloto hoje: passar o dado (jogo, bots/simulador de carreira e `board-report`) e definir o que acontece com o quadro salvo quando o nível muda.
- Pesos: `MissionWeighting.OriginWeight`; novo tipo muda `missions_golden.json` (justificar).

## Revisão e escolhas da spec (Claude, 2026-11-27)
Plano conferido contra o código: fatos corretos, com duas ressalvas.
- RBAC 61.81 (PP-A), pela orientação da ANAC: 10 h solo, 5 h de navegação solo e ao menos uma navegação solo de 270 km (150 NM) com pouso completo em dois aeródromos diferentes. Bate com a decisão 3. O texto consolidado vigente não foi lido.
- `CreateVfrTriangleMissionSync` ignora `rangeNm` do perfil (pernas e totais fixos no código); a variante longa lê `rangeNm`/`legNm` do perfil.
- Tipo `navegacao_vfr_ida` (gerador `vfr_one_way`), peso por tier {1:0, 2:3, 3:10, 4:6}; perfis `iniciante` (60, 50–100 NM, tier 3) e `intermediaria` (40, 50–150 NM, tiers 2–3, a partir do nível 3).
- Nível no sorteio: campo `minPilotLevel` no perfil; nível nulo não filtra. Quadro salvo não é invalidado quando o nível muda.
- Iniciante: lista fixa dos 3 mais próximos por origem (distância, depois ICAO), preferindo sem torre e pista pavimentada ≥ 2.500 ft; sem candidato, relaxa (pavimentada ≥ 2.500 ft, depois qualquer). Intermediária: peso 1 + 2 por característica (torre; pista ≤ 3.000 ft ou não pavimentada; desnível ≥ 1.000 ft da origem). Pista mínima 1.800 ft. Sem candidato: a missão não é criada.
- Longa: perfil `triangle_long` (peso 20, 150–300 NM, pernas 40–130, pouso completo), também a partir do nível 3 (solo). Ajustável no catálogo.
- Spec: `NAV-IDA` delegada ao Gemini em 2026-11-27.

## A definir na spec (resolvido acima)
- Peso por tier (proposta: 10 no tier 3; 2 e 4 como a triangular, 3 e 6).
- "Poucos destinos repetidos": critério determinístico por origem.
- Etiquetas de destino para torre, pista curta e relevo; o que fazer sem candidato no raio.
