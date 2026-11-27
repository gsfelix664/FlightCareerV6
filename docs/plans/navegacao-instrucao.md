# Navegação de instrução só de ida (NAV-IDA)

**Estado:** decidido, não implementado. Spec detalhada em sessão Opus; implementação pelo Gemini.

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

## A definir na spec
- Peso por tier (proposta: 10 no tier 3; 2 e 4 como a triangular, 3 e 6).
- "Poucos destinos repetidos": critério determinístico por origem.
- Etiquetas de destino para torre, pista curta e relevo; o que fazer sem candidato no raio.
