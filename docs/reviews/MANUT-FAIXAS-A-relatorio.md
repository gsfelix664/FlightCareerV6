# MANUT-FAIXAS-A — Manutenção por faixas de saúde (domínio, voo, seguro e simulador)

**Branch:** feat/manut-faixas-a · **Commit:** 903e923 · **Data:** 2026-11-20

## Plano (antes de codar)
- Criar `PartHealthBands` (Domain) e configurar `partBands` no catálogo JSON + `ComponentCatalog` com `walkaroundHalfWidth = 0.0`.
- Ajustar `PartWearEngine` para aplicar multiplicador de desgaste em saúde baixa (< 40) antes do estresse.
- Expor `FailureEngine.PickMode` e criar `PartFailureEngine` para sorteio de falhas por peça a partir de `HazardPerHour`.
- Adicionar colunas `KnownHealth`, `KnownAtAirframeHours` e `FoundRedAtAnnualAirframeHours` em `AircraftPart` e `AppDbContext`.
- Criar `PartKnowledge` (`Reveal`, `MarkAnnualFindings`), refatorar `PartInspectionFlags` (só por faixas + `LifeNote`) e `PartReplacementPlanner` (`SelectRecommended`, precificação exata de selecionadas).
- Plugar `PartFailureEngine` e `BreakPart` em `PartFlightSession`, `ComponentFlightSession` e `FlightMonitor`.
- Ajustar Hangar, `FlightService`, `FerryFlightViewModel` (remover interdição por TBO; anual só inspeciona sem trocar óleo).
- Atualizar `EconomyServices` (seguro a 2,5%/ano, negação por achado de anual não corrigido) e `CareerProgressionSimulator`.
- Criar e rodar suíte completa de testes xUnit e build da solução.

## O que foi feito
- `Domain/Parts/PartHealthBands.cs`: criado com enum `PartBand` (Green, Yellow, Red), record `PartBandSettings` (padrões 40%, 20%, 2.0x), `Classify` e cálculo não-linear `LowHealthWearMultiplier` (`1.0 + Math.Pow((yellow - h) / yellow, 2) * (max - 1)`).
- `Infrastructure/Data/component_catalog.json` e `Domain/Components/ComponentCatalog.cs`: adicionado bloco `"partBands"` com comentários; `visibility.walkaroundHalfWidth` ajustado para `0.0`.
- `Domain/Parts/PartWearEngine.cs`: multiplicador de desgaste por baixa saúde aplicado à taxa nominal normal e lâmpadas acesas antes de qualquer multiplicador de estresse. Construtor de `PartFlightTracker` atualizado para receber e propagar `bandSettings`.
- `Domain/Components/FailureEngine.cs`: `PickMode` tornado estático e público (`PickMode(ComponentDefinition, EngineClass, IRandomSource)`) para reuso direto sem duplicar lógica nem alterar o comportamento legado.
- `Domain/Parts/PartFailureEngine.cs`: criado motor de falhas por peça no domínio com `HazardPerHour` (0 acima de 20%, subindo até 2,0 em 0% de saúde com escala do catálogo), sorteio de modo via `PickMode`, limite estrito de no máximo uma pane por `ComponentKind` por voo, e isolamento de peças `minorPart` e quebradas.
- `Domain/Entities/AircraftPart.cs` e `Infrastructure/Persistence/AppDbContext.cs`: adicionadas colunas `KnownHealth`, `KnownAtAirframeHours` e `FoundRedAtAnnualAirframeHours` mapeadas no SQLite com migração idempotente em `EnsureSchemaUpdatedAsync` e `CREATE TABLE IF NOT EXISTS AircraftParts`.
- `Domain/Parts/PartKnowledge.cs`: criado com métodos `Reveal` (grava `KnownHealth` e `KnownAtAirframeHours` nas peças do escopo inspecionado) e `MarkAnnualFindings` (registra `FoundRedAtAnnualAirframeHours` em peças quebradas/vermelhas e limpa nas sadias).
- `Domain/Parts/PartInspectionFlags.cs`: `Evaluate` refatorado para sinalizar exclusivamente pela faixa de saúde (`Broken` para quebradas ou < 20%, `Suspect` para < 40%). Criado `LifeNote` para emitir informações de vida/ciclos/calendário sem sinalizar alerta no card nem interditar.
- `Domain/Parts/PartReplacementPlanner.cs`: removido `ReplaceBelowHealthPct`. `Plan(selectedParts)` agora orça estritamente as peças fornecidas. Criado `SelectRecommended(parts, catalog, bands)` que seleciona apenas quebradas e na faixa vermelha para a recomendação automática. `ApplyToPart` atualizado para zerar `FoundRedAtAnnualAirframeHours` e registrar `KnownHealth = 100`, `KnownAtAirframeHours = airframeHours`.
- `UI/Services/PartFlightSession.cs`, `UI/Services/ComponentFlightSession.cs` e `UI/Services/FlightMonitor.cs`: interligados `PartFailureSource` e `PartBreakSink`. Durante o voo com `PartsDriven`, panes saem por peça com identificação nominal da peça e saúde anterior na descrição do alerta, quebrando a peça a 0% e acionando os atuadores do simulador (`ApplyMode`).
- `Application/Services/FlightService.cs` e `UI/ViewModels/FerryFlightViewModel.cs`: eliminada a interdição por TBO vencido (mantendo apenas Inspeção Anual vencida).
- `Application/Services/EconomyServices.cs` e `Infrastructure/Services/EconomyServices.cs`: taxa mensal de seguro ajustada para 2,5%/ano (`0.025 / 12`), piso mínimo de $50/mês. Implementado `ClaimDenied` verificando se a aeronave possui peças com achado de anual não corrigido (`FoundRedAtAnnualAirframeHours != null`).
- `UI/ViewModels/HangarViewModel.cs` e `UI/ViewModels/PilotAircraftWrapper.cs`: `Perform100hInspection` não troca mais o óleo nem zera 12h, executando `Reveal` geral e `MarkAnnualFindings`. Chamadas de `PartKnowledge.Reveal` adicionadas em todas as inspeções (walkaround, 50h, 100h, 500h, overhaul). `ClearInterdictionIfResolved` exige apenas anual em dia e desinterdita aeronaves legadas com interdição de TBO. "Reparar tudo" orça apenas `SelectRecommended`. `ClaimInsurance` bloqueia sinistro caso `ClaimDenied` seja verdadeiro.
- `Application/Simulation/CareerProgressionSimulator.cs`: atualizado para calcular desgaste acelerado por faixas, avaliar panes por peça (`PartFailureEngine`) em voos locais e translados, realizar walkaround a cada voo no modo cauteloso, e alinhar as políticas de manutenção (cauteloso: faixas amarela e vermelha; equilibrado: faixas vermelhas e regulatórias em dia; agressivo: apenas peças quebradas e sem revisões preventivas).

## Critérios de aceite
- [x] Faixas de saúde [100, 40] verde, [40, 20) amarela com desgaste até 2x, [20, 0] vermelha com risco de pane — verificado em `PartHealthBandsAndFailuresTests.Bands_Classify_Limites` e `LowHealthWear_Curva`.
- [x] Desgaste acelerado progressivo aplicado no `PartWearEngine` — verificado em `PartWear_PecaAbaixoDe40_GastaMaisRapido`.
- [x] Taxa de pane por peça zera acima de 20% e atinge 2,0/h em 0% — verificado em `PartFailure_AcimaDe20_NuncaFalha` e `PartFailure_Em0_TaxaDe2PorHora`.
- [x] No máximo uma pane por componente por voo — verificado em `PartFailure_UmaPorComponentePorVoo`.
- [x] Peças minorPart e quebradas ignoradas no sorteio de falhas — verificado em `PartFailure_IgnoraMinorEQuebradas`.
- [x] Panes utilizam a saúde da peça individual e não o agregado da barra — verificado em `PartFailure_UsaPecaNaoBarra`.
- [x] Planner não troca peças verdes nem amarelas na recomendação automática — verificado em `Planner_NaoTrocaVerdeNemAmarelaSozinho`.
- [x] Planner orça estritamente as peças selecionadas — verificado em `Planner_Plan_PrecificaSoAsSelecionadas`.
- [x] Flags de inspeção avaliadas exclusivamente pela saúde — verificado em `Flags_SoPelaSaude` e `PartInspectionFlagsTests`.
- [x] Inspeção anual não troca óleo, marca achados de peças vermelhas e zera após substituição — verificado em `AnnualInspection_NaoTrocaOleo_MarcaAchados`.
- [x] Seguro com prêmio de 2,5% ao ano e piso de $50 — verificado em `Insurance_Premio_2v5PorCentoAoAno`.
- [x] Sinistro de seguro negado com achado da anual não corrigido — verificado em `Insurance_ClaimNegado_ComAchadoDaAnualNaoCorrigido`.
- [x] TBO vencido não interdita aeronave; Anual vencida interdita — verificado em `FlightService_TboVencido_NaoInterdita` e `FlightService_AnualVencida_Interdita`.
- [x] Banco antigo recebe novas colunas de `AircraftParts` de forma idempotente — verificado em `ComponentPersistenceTests.Banco_antigo_recebe_novas_colunas_de_aircraft_parts`.
- [x] Política agressiva do simulador de carreira registra taxa de panes/100h maior que cauteloso na mesma semente — verificado em `CareerProgressionTests.Agressivo_PulaRevisoes_TemMaisPanesQueCauteloso`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 673 aprovados, 0 falhas (18 novos testes adicionados e suíte 100% verde).

## Não testado
- Comportamento visual na tela XAML do Hangar (o layout completo dos cards e botões por peça será implementado na tarefa MANUT-FAIXAS-B).
- Voo no simulador Microsoft Flight Simulator 2024 conectado ao SimConnect.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida em aberto. Todas as decisões de `docs/plans/economia-estabilidade.md` › 4.1 e as especificações de MANUT-FAIXAS-A foram rigorosamente atendidas.
