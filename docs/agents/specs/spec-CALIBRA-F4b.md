# Spec CALIBRA-F4b — Calibração do usuário em todas as peças, multiplicador de preço por classe, TBO e textos

**Objetivo (1 frase):** terminar a CALIBRA-F4 aplicando a calibração do usuário (153 peças), trocando o preço por % do valor do avião por **preço base × multiplicador da classe**, com TBO, inspeções, textos do Hangar e dois limites de POH, deixando **todos** os testes verdes sem tocar nas faixas de `economy_targets.json`.

**Limite de tempo:** você tem 1h30. **Não** rode otimizadores nem a simulação de carreira. Os números já vêm prontos. Fuzz: só `--runs 5`.

## Ponto de partida (branch `feat/calibra-f4`, já atualizada com a base pelo revisor)
- Já existe trabalho parcial seu: intervalos de inspeção 12/25/125 h com textos novos, migração SQL das horas salvas (`AppDbContext.EnsureSchemaUpdatedAsync`, bloco "Compressão de tempo ÷ 4"), `CalibraF4Tests.cs`. Revise e mantenha o que estiver certo.
- `Domain/Components/MaintenancePricing.cs` já está com preços ÷ 4 (212,50 / 625 / 362,50 / retífica 5 % mín. 6.250). O teste `MaintenancePricing_MesmosValoresDaUI` (de `CareerSimulatorTests`) ainda espera os valores antigos: atualize-o.
- Falham hoje: `PartEconomyBalanceTests` (2) e `Catalogo_Motor_CalibracaoDoUsuario`.

## O que fazer
1. **Catálogo de peças** (`Infrastructure/Data/parts_catalog.json`): para cada peça de `docs/agents/specs/calibracao-pecas-usuario-2026-11-19.json` › `parts`, copie `lifeHours`, `lifeCycles`, `calendarLifeMonths` (null = remover o campo/null), `partPrice`, `priceScalesWithAircraft`, `laborHours`, `downtimeHours`, `blocksFlightWhenBroken`, `visibleOnWalkaround`, `failureEffect`. `lifeSource` = `"calibração do usuário 2026-11-19 (real ÷ 4)"`. **Não** mude `appliesTo`, exceto `engine.air_intake_filter` → `{ "all": ["piston"] }`. Mantenha `id`, `name`, `group`, `sensors`, `perEngine`, `usageSensor`, `minorPart` etc. Peça só com ciclos (sem `lifeHours`): confirme que o loader e o desgaste aceitam (sem divisão por zero).
2. **Preço por classe** (`Domain/Parts/PartPricing.cs` + quem chama):
   - Mova a classificação `ResolveAircraftClass(model, engineClass, engineCount, features, price)` de `Tests/Parts/PartEconomyBalanceTests.cs` para o Domain (ex.: `Domain/Economy/AircraftEconomyClass.Resolve`, mesmas regras, A–J), e faça o teste usá-la.
   - `CalculatePartPrice`: se `PriceScalesWithAircraft`, preço = `partPrice × multiplicador da classe`; senão `partPrice`. **Multiplicadores** (calculados pelo revisor para todas as classes caberem nas faixas): A 0,75 · B 1,25 · C 1,25 · D 1,25 · E 1,5 · F 1,75 · G 1,75 · H 2,5 · I 3,25 · J 5,5. Guarde-os em `Infrastructure/Data/economy_targets.json` como campo novo `partPriceMultiplier` em cada classe (**não** altere `minCostPerHour`/`maxCostPerHour`).
   - `repairCostPctOfAircraft` e `GetClassPartPriceCap` deixam de ser usados no preço: remova o uso (o campo do JSON pode ficar, documentado como legado).
   - Ajuste `PartEconomySimulator`, `CareerSimulator` e qualquer outro chamador para passar a classe (ou o multiplicador). O teste por classe usa as configurações sintéticas que já existem no teste.
3. **TBO**: pistão **500 h** (era 450 na sua versão parcial), turbina **900 h**; ajuste a migração SQL (`EngineTboRemainingHours > 500`) e onde o TBO é definido (`EngineLifeHours`, mercado, catálogo de componentes, se houver).
4. **Inspeções** (decisão do usuário): troca de óleo **12 h**, inspeção anual **25 h** (interdita se vencida), revisão de ignição **125 h**. Mantém o que você já fez.
5. **Textos do Hangar** alinhados com os novos valores: nenhum texto pode citar 50 h, 100 h, 500 h, 1.800 h, 3.500 h ou 3.600 h como intervalo/TBO (procure em `HangarView.xaml`, `HangarViewModel.cs`, `PilotAircraftWrapper.cs`, `ComponentRowViewModel.cs`, `BriefingViewModel`, mercado). Use os valores das constantes, não números fixos no texto.
6. **Limites de POH** (decisão do usuário, 2026-11-19): `Data/aircraft/diamond-da62.json` `vle` = 156 kt; `Data/aircraft/beechcraft-king-air.json` `vne` = 263 kt; `source: "POH"`, `reference: "usuário 2026-11-19 (AFM; substitui valor anterior)"`; incremente `dataVersion`. Atualize as linhas correspondentes em `tools/aircraft_limits/poh_values.csv`.
7. `docs/plans/lista-pecas-hangar.md`: regenerar com os valores novos (script no seu scratchpad, fora do repo).

## Fora do escopo
- Reparo por troca de peça inteira, calendário de jogo, cabine, pagamento das missões (outras tarefas). Arquivos de voo quentes (`FlightMonitor`, `ComponentFlightSession`, `SimConnectService`, `SimFailureActuator`, `SimTelemetry`).

## Testes
- `dotnet test` 100 % verde, incluindo `PartEconomyBalanceTests` (ambos) **com as faixas originais**.
- Novos: `PartPricing_MultiplicadorPorClasse` (C152 → ×0,75; 737 → ×3,25; peça sem escala → preço base); `Catalogo_CalibracaoDoUsuario` (5 peças de grupos diferentes conferidas contra o JSON do usuário, incluindo NO-GO/WA); `Tbo_Pistao500_Turbina900`; ajuste `Catalogo_Motor_CalibracaoDoUsuario` para os valores do JSON novo.

## Critérios de aceite
- [ ] Build 0 erros; testes 100 %; `fuzz --aircraft all --runs 5` sem falhas (se falhar por vida curta, **não** relaxe o oráculo: descreva).
- [ ] CHANGELOG (`## 2026-11-19 - Calibração do usuário, preço por classe, TBO 500/900 (Gemini)`); `AGENTS.md` › 7 Manutenção (TBO 500/900; inspeções 12/25/125; preço = base × multiplicador da classe).
- [ ] Commit(s) na branch; relatório `docs/reviews/CALIBRA-F4b-relatorio.md` com a tabela custo/h por classe (simulador) e a faixa.
