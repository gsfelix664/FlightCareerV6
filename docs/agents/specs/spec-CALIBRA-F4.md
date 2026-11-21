# Spec CALIBRA-F4 — Compressão de tempo ÷ 4 em peças, TBO e inspeções

**Objetivo (1 frase):** aplicar o fator de compressão 4 (decisão do usuário, 2026-11-17) a toda vida em horas/ciclos das peças, ao TBO e aos intervalos de inspeção, mantendo o custo de manutenção por hora de cada classe dentro de `economy_targets.json`.

## Decisões do usuário (2026-11-17)
1. Vida das peças: **vida original do catálogo ÷ 4** (horas e ciclos), em **todos** os grupos, inclusive Iluminação. "Original" = valores do `parts_catalog.json` no commit `83db087` (antes da calibração do motor); para o grupo Motor, os preços/mão de obra/parada da CATALOGO-MOTOR **continuam valendo**, só a vida volta a ser original ÷ 4.
2. Calendário (`calendarLifeMonths`) **não** é comprimido (tempo real). Exceção pedida: `oil.lubricant` e `oil.turbine_oil` = 3 meses.
3. Grupo Óleo: preço/mão de obra/parada do usuário: lubricant 130 / 1 h / [1,2]; filter 35 / 0,5 / [1,2]; turbine_oil 240 / 1 / [1,3]; turbine_filter 140 / 1 / [1,3]; cooler 520 / 2 / [2,4]; pump 850 / 4 / [2,4]; turbine_pump 2100 / 5 / [2,4]. Vida = original ÷ 4 (lubricant 12,5 h; filter 12,5; turbine_oil 200; turbine_filter 150; cooler 500; pump 450; turbine_pump 875).
4. Peças com `priceScalesWithAircraft: true`: o custo real vem de `repairCostPctOfAircraft` (% do valor do avião, teto por classe em `PartPricing.GetClassPartPriceCap`). Dividir esses percentuais por 4 e então ajustar por grupo até **todas** as classes do `PartEconomyBalanceTests` ficarem dentro de `[minCostPerHour, maxCostPerHour]`, preferindo ficar perto do meio da faixa. Não mexer em `economy_targets.json` nem no simulador.
5. TBO: pistão 1.800 h → **450 h**; turbina (hoje 3.500 h no legado e 3.600 h no mercado/catálogo, inconsistência conhecida) → **900 h** unificado.
6. Inspeções: 50 h → **12 h** "Troca de óleo"; 100 h → **25 h** "Inspeção anual" (continua interditando se vencida); 500 h → **125 h** "Revisão de ignição". Custos por evento ÷ 4 para manter o custo por hora: 850 → 212,50; 2.500 → 625; 1.450 → 362,50; retífica: 20 % → 5 % do preço de compra, mínimo 25.000 → 6.250. Nomes de propriedades/colunas do código (`HoursUntilNext50hInspection`, `Cost50hInspection`...) **ficam** (sem migração de coluna); mudam constantes, textos da UI e mensagens.

## Ponto de partida
- Esta branch parte de `feat/catalogo-motor` (calibração do motor já aplicada, `external_lighting` removido, J-3/CAP-4 corrigidos, limpeza no provisionador). Nela, `PartEconomyBalanceTests` (2 testes) **falha de propósito**: as metas de `economy_targets.json` foram restauradas aos valores originais. Esta tarefa deve deixá-los verdes **sem tocar em `economy_targets.json`** nem no simulador/teste — ajustando vidas (÷ 4) e `repairCostPctOfAircraft`. Lição: `docs/agents/LICOES.md` (CATALOGO-MOTOR).

## Fatos já levantados
- Custos de inspeção: `UI/ViewModels/PilotAircraftWrapper.cs:150-157` (850 / 2500 / 1450 / overhaul 20 % mín. 25.000, textos "Fazer 50 h").
- Inspeções e TBO no Hangar: `UI/ViewModels/HangarViewModel.cs` (`Perform50hInspection`, `Perform100hInspection`, `Perform500hInspection`, `PerformOverhaul`, `ApplyRegulatoryReset`, ~l.200-220 e 340-410). Horas descontadas no fim do voo: `Application/Services/FlightService.cs`.
- TBO: procure `1800`, `3500`, `3600`, `EngineLifeHours`, `EngineTboRemainingHours`, `TboHours` em Domain/Application/Infrastructure/UI e em `component_catalog.json` (`AGENTS.md` › 7 Manutenção cita a inconsistência).
- Mercado de usados: `Infrastructure/Services/MarketSimulationService.cs` gera horas de motor/célula; horas desde overhaul geradas têm de caber no TBO novo (escale a geração por 4 nas horas comparadas com TBO/inspeção; horas de célula podem ficar, se só forem informativas — diga o que decidiu).
- Balanço: `Tests/Parts/PartEconomyBalanceTests.cs` + `Domain/Parts/PartEconomySimulator.cs`.
- Carreiras existentes no banco: valores salvos maiores que os novos limites (ex.: `EngineTboRemainingHours` 1.200, `HoursUntilNext50hInspection` 40) precisam ser limitados uma vez na inicialização (`min(valor, novo limite)`, idempotente), no mesmo padrão de migração de dados já usado na inicialização (procure onde `PartMigrationService`/`EnsureSchemaUpdatedAsync` rodam). `AircraftPart.HoursSinceReplacement` **não** muda (peça com horas acima da vida nova fica com vida vencida: comportamento desejado).

## Fora do escopo
- Pagamento das missões (`MissionGenerator`), combustível, taxas, seguro. `economy_targets.json`. Calibrações de outros grupos que o usuário ainda vai mandar (só o fator ÷ 4 agora). Arquivos de voo quentes (`FlightMonitor`, `ComponentFlightSession`, `SimConnectService`, `SimFailureActuator`, `SimTelemetry`), a não ser que o TBO/inspeção esteja codificado lá — nesse caso, mudança mínima e explicada.

## Testes
- `PartEconomyBalanceTests` verdes para todas as classes (mostre a tabela custo/h por classe antes × depois no relatório).
- `Catalogo_VidasComprimidasPor4` (compara 5 peças de grupos diferentes com o original ÷ 4; calendário inalterado exceto óleo).
- `Inspecoes_IntervalosComprimidos` (12 / 25 / 125; vencida a de 25 h interdita).
- `Tbo_Pistao450_Turbina900`.
- `Migracao_LimitaHorasSalvas` (SQLite em memória: aeronave com TBO restante 1.200 e 50 h restante 40 → 450 e 12; segunda execução não muda nada).
- Testes antigos que fixavam 50/100/500/1800/3500: ajuste os números e justifique cada um no relatório.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 20` sem falhas (vidas menores podem fazer peças chegarem a 0 em voo longo: se o oráculo falhar, **não** relaxe o oráculo sem explicar o motivo físico).
- [ ] `docs/plans/lista-pecas-hangar.md` regenerado; CHANGELOG (`## 2026-11-17 - Compressão de tempo ÷ 4 (Gemini)`); `AGENTS.md` › 7 (Manutenção: TBO 450/900, inspeções 12/25/125; fator 4 documentado como regra de calibração).
- [ ] Um commit: `feat(economy): compressao de tempo /4 em pecas, TBO e inspecoes (CALIBRA-F4)`.
- [ ] Relatório só com fatos conferidos.
