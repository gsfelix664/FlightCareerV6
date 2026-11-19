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
