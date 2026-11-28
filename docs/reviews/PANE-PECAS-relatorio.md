# PANE-PECAS — uma pane quebra uma peça; reparar a pane troca a peça; peça quebrada interdita

**Branch:** fix/pane-pecas · **Commit:** <pendente> · **Data:** 2026-11-26

## Plano (antes de codar)
- Criar `NativeFailurePartMap` mapeando IDs nativos 0–49 para listas ordenadas de PartId candidatas.
- Criar `DefectPartLink` para orçar reparos unificados de panes (incluindo panes associadas a peças quebradas com custo zero na troca da peça) e detectar resolução de panes por peças substituídas.
- Criar `PartAirworthiness` identificando peças quebradas que possuem `BlocksFlightWhenBroken == true`.
- Refatorar `PartFlightTracker.ApplyNativeFailed` para quebrar apenas 1 peça prioritária das candidatas instaladas (desempates: saúde, ordem na tabela, EngineIndex) com fallback antigo caso não haja candidata.
- Atualizar `FlightMonitor.StartMonitoring` para bloquear voo se houver peças quebradas com `BlocksFlightWhenBroken`.
- Atualizar `PilotAircraftWrapper` (status INTERDITADA (PEÇA), DefectsRepairTotal e TotalRepairCost unificados com panes e peças).
- Atualizar `HangarViewModel` para orçar e executar reparos unificados em `RepairDefectsAsync`, `ReplacePart`, `RepairComponent` e `RepairAll` (fluxo único E2).
- Adicionar testes de unidade para os 10 cenários da spec e ajustar testes de integração em `PartFlightIntegrationTests`.

## O que foi feito
- `Domain/Parts/NativeFailurePartMap.cs`: tabela estática ID 0–49 → lista ordenada de peças candidatas, com todas as 47 peças validadas no `parts_catalog.json`.
- `Domain/Parts/DefectPartLink.cs`: orçamentação unificada (`DefectRepairQuote`), vinculação de pane nativa a peças instaladas quebradas (`LinkedBrokenParts`) e identificação de panes resolvidas por troca física de peças (`ResolvedBy`).
- `Domain/Parts/PartAirworthiness.cs`: filtragem de peças instaladas quebradas que possuem `BlocksFlightWhenBroken == true`.
- `Domain/Parts/PartFlightTracker.cs`: `ApplyNativeFailed` quebra exatamente uma peça candidata não quebrada (menor saúde, ordem na tabela, menor motor); fallback de componente mantido para IDs sem candidata ou com todas já quebradas.
- `UI/Services/FlightMonitor.cs`: verificação em `StartMonitoring` de peças quebradas com `BlocksFlightWhenBroken` via contexto curto `_contextFactory`, emitindo alerta de interdição e cancelando início de voo.
- `UI/ViewModels/PilotAircraftWrapper.cs`: status `INTERDITADA (PEÇA)` antes da checagem de panes; `DefectsRepairTotal` e `TotalRepairCost` unificados considerando substituição de peças ligadas e panes avulsas; notificações completas no `SetOpenDefects` e `UpdateReplacementPlans`.
- `UI/ViewModels/HangarViewModel.cs`: `RepairDefectsAsync` substitui peças ligadas a panes e zera cobrança duplicada; `ReplacePart`, `RepairComponent` e `RepairAll` marcam resolvidas as panes ligadas às peças trocadas; `RepairAll` adota fluxo único incluindo panes abertas mesmo sem peças na recomendação.
- `Tests/Parts/NativeFailureAndDefectTests.cs`: 10 testes cobrindo todos os cenários da especificação.
- `Tests/Parts/PartFlightIntegrationTests.cs`: assert atualizado para `broken == 1` no teste de falha nativa.
- `docs/plans/correcoes-voo-forcado-sdco.md`: itens D2, D3, D4 e E2 marcados como concluídos com `(PANE-PECAS)`.
- `docs/sistemas/pecas.md` e `docs/sistemas/panes-persistentes.md`: atualizados com as novas regras de peças quebradas por panes, interdição e fluxo unificado de reparo.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx 0 erros; dotnet test src/FlightCareer.Tests 100% (1.222/1.222 aprovados)
- [x] missions_golden.json inalterado
- [x] Simulações longas ou career-stab não executadas
- [x] Relatório detalhando não testado no MSFS e no app aberto
- [x] docs/CHANGELOG.md, sistemas pecas.md e panes-persistentes.md atualizados; D2, D3, D4 e E2 marcados no plano
- [x] Commit único no formato especificado

## Build e testes
- dotnet build src/FlightCareer.slnx: 0 erros, 0 avisos novos
- dotnet test src/FlightCareer.Tests: 1.222 aprovados, 0 falhas (10 novos testes em `NativeFailureAndDefectTests`)

## Não testado
- Pane nativa real disparada pelo MSFS 2024 e início de voo bloqueado na prática pelo simulador conectado.
- Apresentação visual dos textos e valores atualizados na tela aberta do Hangar em tempo de execução.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva encontrada. A integração seguiu estritamente o contrato da especificação.
