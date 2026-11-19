# ECON-STAB — Revisão (Claude, 2026-11-19)

**Veredito: CORRIGIR**

Ajustes já feitos pelo revisor (commit desta revisão): base integrada (calibração ÷ 4, preço por classe, reparo por troca de peça, calendário); intervalos e TBO como constantes (`MaintenancePricing.OilChangeIntervalHours` 12, `AnnualInspectionIntervalHours` 25, `IgnitionRevisionIntervalHours` 125, `TboHoursFor(EngineClass)` 500/900) usadas pelo simulador, `CareerStart`, `CreatePilotViewModel` e `HangarViewModel` (antes o simulador fixava 1.800 h e 50/100/500 h).

## Corrigir
1. **Desgaste das peças inexistente.** `CareerProgressionSimulator` cria componentes a 100 % que nunca mudam: não há desgaste, nem troca de peça, e o `FailureEngine` (risco zero acima de 20 %) nunca dispara. A manutenção medida fica quase só inspeções. Implemente no modo `--fast`:
   - Peças por bot provisionadas pelo catálogo (`PartCatalog.PartsFor(features do perfil da aeronave)`, uma instância por motor quando `perEngine`).
   - A cada voo: desgaste natural `70/lifeHours × horas × 1,15` (mesmo fator do `PartEconomySimulator`) e `HoursSinceReplacement += horas`; peças só por ciclos: `+1` ciclo por voo e `70/lifeCycles` por ciclo; lâmpadas (`usageSensor`): fração acesa nav/strobe/beacon 100 %, pouso/táxi 15 %, painel 0 %. Sem estresse/abusos.
   - Barra dos componentes = `PartComponentBridge.AggregateHealth` das peças do grupo **antes** de `FailureEngine.Evaluate`; o dano da pane vai para as peças do grupo (como o `DamageGroup`).
2. **Manutenção pelas regras do Hangar:** troca de peças com `PartReplacementPlanner.Plan(peças, catálogo, classe de AircraftEconomyClass, relógio)` + `Apply`, preço cheio e parada do plano. Política: `cauteloso` e `equilibrado` aplicam o plano completo (todos os grupos) na inspeção anual e trocam óleo/filtro na troca de óleo, como o Hangar; `agressivo` só troca o que está quebrado ou é NO-GO quando o avião fica parado. Peça NO-GO quebrada sem dinheiro para trocar = falência (regra atual).
3. **Relatório:** acrescente "Troca de peças" no custo/h e no detalhamento, e "peças trocadas por 100 h".
4. Mantenha o limite de 1h30: rode só a amostra (`--days 60 --bots 2 --difficulty medium --policy all --fast`) e informe o tempo.

## Rodada 2 — CORRIGIR (Claude, 2026-11-19) — última rodada

Bateria completa (`career-stab --days 720 --bots 5 --difficulty all --policy all --fast`, 46 s) mostra que o simulador não é válido:
1. **Bots param de voar:** `horas_voadas` = 16,1 h no dia 30, 180 **e** 720 em todos os bots (o saldo só cai pelo seguro mensal, por isso o "seguro/h" sobe de $37 para $892 e a margem fica negativa). Descubra por que não há mais voos depois dos primeiros dias (cache de missões por `(ICAO, dia)`, aeroporto sem missões compatíveis, missão aceita que nunca é concluída, relógio/data, avião parado sem nunca liberar…) e corrija. Se o aeroporto atual não tiver missão compatível, o bot deve fazer um voo de reposicionamento (ferry, sem receita, pagando combustível e taxas) para o hub mais próximo com missões, como um jogador faria.
2. **Todos os bots iguais:** mesma semente para os 5 bots e para as 3 dificuldades — use semente derivada de (semente base, dificuldade, política, índice do bot).
3. **Nenhuma compra nem no Fácil ($1 bi):** a lógica de compra não dispara; confira a regra de reserva, a escada de classes e a categoria exigida. Se a categoria do piloto bloquear a compra, isso tem de aparecer no relatório (motivo de não comprar).
4. **Sanidade no relatório:** acrescente horas voadas por marco e voos por dia na tabela; alerta automático "bot parado" se um bot sobrevivente voar < 50 % das horas/dia configuradas.
5. Teste novo: `Progression_BotVoaHorasPorDiaAoLongoDoTempo` (1 bot, Médio, 60 dias: horas voadas ≥ 0,8 × 60 × 2 h) e `Progression_BotsComSementesDiferentes`.
Rode a amostra `--days 60 --bots 2 --difficulty medium --policy all --fast` e mostre as horas voadas no relatório.
