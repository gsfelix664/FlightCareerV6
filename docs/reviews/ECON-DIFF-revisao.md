# ECON-DIFF — Revisão (Claude)

**Branch:** `feat/econ-diff` (`28d79ed`, `153b7fa` e ajustes do revisor `687933e`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido
1. Escopo: tabela de pagamento em `mission_pay.json` + `MissionPayTable` (fallback embutido igual aos números antigos, todas as escalas 1,0), `DifficultyEconomy` (Fácil 1,25/0,80/1,00; Médio 1/1/1; Difícil 0,85/1,15/1,25), saldo inicial do Fácil $1 mi, custos × dificuldade em combustível, taxas, serviços do Hangar e caminhão; `HazardMultiplier` em `FailureEngine`/`PartFailureEngine`; simulador de carreira com os três multiplicadores.
2. Ajustes do revisor (rodada 1, feitos pelo Claude em `687933e`): `string.GetHashCode` (aleatório por processo no .NET) trocado por FNV-1a estável (`StableHash`) nas sementes do simulador — sem isso a ECON-STAB não era reprodutível com a mesma semente; tabela de pagamento em cache.
3. Reprodutibilidade e equivalência do Médio: `career-stab --days 30 --bots 1 --difficulty medium --policy equilibrado`, duas vezes na branch e duas vezes na base com o mesmo hash estável aplicado (cópia temporária, não commitada). As 4 execuções deram `stability.csv` idênticos (dia 30: saldo $133.933, patrimônio $180.733, 70,9 h). O `stability.md` só difere na data/duração. Logo, o Médio da ECON-DIFF reproduz exatamente a base.
   - O número do relatório do Gemini ($150.162) é de outra execução (`--difficulty all --fast`, base anterior) e não é comparável.
4. `Pilot.Difficulty` com padrão `Medium`: só afeta entidades criadas sem dificuldade; pilotos existentes guardam o valor no banco.
5. Build da solução 0 erros; `dotnet test` 695/695 na branch e 708/708 após o merge na base.

## Não testado
- Não testado no MSFS; a linha "Dificuldade …: custos ×…" na confirmação do Hangar não foi vista na tela.
- Fácil e Difícil não calibrados ainda (próxima tarefa: calibração do pagamento D + E).
