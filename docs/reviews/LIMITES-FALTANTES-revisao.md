# LIMITES-FALTANTES — Revisão (Claude)

**Branch:** `feat/limites-faltantes` (`a0f85fb` + ajustes do revisor) · **Data:** 2026-11-22

## Veredito: **APROVADO** (com ajustes do revisor)

## Ajustes do revisor
1. **Dado inventado removido:** o executor criou no C152 um limite `elecAvionicsCircuitIndex = 0` com `source: POH` e citação de página. Dado sem fonte não entra no catálogo; ficou só a feature `noAvionicsMaster`, que já torna a regra "não aplicável".
2. **CAP-4 é acrobático:** Va estimada com n = 6 → 91 kt (era 72 com n = 3,8, o que contaria manobras normais como abuso).

## Conferido
- Detecção em voo do barramento principal (primeiro barramento ≥ 0,5 × tensão com a bateria ligada) e da tensão do sistema (28/14 V); regras elétricas em espera até detectar. Testes em `ElectricalRuleDeriverTests`.
- Va estimada (Vs1·√n, `ClassDefault`, referência com a fórmula) em 15 perfis; valores próximos dos manuais conhecidos (Corvalis 140 × 148; Dakota 107 × 111–124). Nenhum POH/SimVar alterado. 18 perfis sem `vs1` ficaram sem Va (listados no relatório).
- Cobertura: inativas por falta de dado de 4,5 → 2,2 regras/avião.
- Build 0 erros, 789/789 (revisor); fuzz 920/920 (executor).

## Observação
- A mensagem "barramento principal detectado" não aparece no relatório do replay (a detecção ocorre no pré-voo e o aviso único não se repete depois do reinício da ativação). Cosmético; a detecção está coberta pelos testes.

## Não testado
- No MSFS.
