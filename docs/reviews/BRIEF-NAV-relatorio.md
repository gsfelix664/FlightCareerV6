# BRIEF-NAV — Briefing: navegação deixa de ser TGL e mapa enquadra rota + contrato

**Branch:** fix/brief-nav · **Commit:** (commit único da tarefa) · **Data:** 2026-11-27

## Plano (antes de codar)
- Criar `BriefingLegPlanner` em `FlightCareer.Domain/Missions/` com lógica pura de extração de escalas (`ReadStops`), identificação de TGL/Local/Escala/Navegação e expansão de hops sem duplicatas consecutivas.
- Implementar suite de testes de unidade em `FlightCareer.Tests/Missions/BriefingLegPlannerTests.cs` cobrindo todos os 10 cenários da especificação.
- Adicionar propriedade `IsLocal` e ajustar `RouteDisplay` em `FlightLegModel`.
- Integrar `BriefingLegPlanner` em `BriefingViewModel.RebuildItineraryAndValidateAsync`, eliminando circuito TGL em navegações com escalas e distinguindo `Local` de `TouchAndGo`.
- Em `FlightMapControl`, unificar o enquadramento de rota e contrato com `_routeFramingPoints` e `_contractFramingPoints` via `ApplyCombinedFraming()`, desenhando circuito âmbar apenas para TGL real e marcador de base para voo local.

## O que foi feito
- Criado `src/FlightCareer.Domain/Missions/BriefingLegPlanner.cs`:
  - `ReadStops`: desserialização resiliente de `MultiStopContractData` em JSON (sem exceção, fallback para lista vazia com log).
  - `Plan`: segregação de grupo local na origem (`TouchAndGo` se `RequiredTouchAndGoes > 0`, senão `Local`) e encadeamento sequencial de missões. Missões com escalas expandem cada trecho intermediário como `ContractStop` (preservando `StopMode` e coordenadas `ToLat`/`ToLon`) e perna final como `Navigation` (`IsFinalLegOfMission = true`), descartando hops consecutivos redundantes.
- Criado `src/FlightCareer.Tests/Missions/BriefingLegPlannerTests.cs`:
  - 10 testes unitários cobrindo todos os cenários exigidos pela especificação (triângulo full stop, triângulo TGL, TGL isolado, voo local sem TGL, última escala igual ao destino, multi-stop com destino diferente, JSON inválido/nulo, missões encadeadas em ordem inversa, TGL prioritário antes de travessia e repasse de latitude/longitude de escalas).
- Atualizado `src/FlightCareer.UI/ViewModels/FlightLegModel.cs`:
  - Adicionada propriedade `IsLocal` e atualização de `RouteDisplay` ("(TGL)" se `IsTglOrLocal`, "(LOCAL)" se `IsLocal`, ou sem sufixo).
- Atualizado `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`:
  - Em `RebuildItineraryAndValidateAsync`, substituída a lógica antiga de montagem de pernas pela chamada ao `BriefingLegPlanner`.
  - Tratamento de ações para cada tipo de perna (`TouchAndGo`, `Local`, `ContractStop` e `Navigation`), cálculo de distâncias considerando coordenadas de escalas e atribuição de remuneração exclusivamente à perna final. A string "Circuito de Tráfego: Toque e Arremetida" ficou restrita ao caso `TouchAndGo`.
  - Busca da primeira perna não-local/não-TGL atualizada para `!l.IsTglOrLocal && !l.IsLocal`.
- Atualizado `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`:
  - Circuito âmbar restrito a instruções TGL reais (`IsTglOrLocal`).
  - Pernas locais sem TGL (`OriginICAO == DestinationICAO`) plotam apenas marcador de base (`#00E676`) se ainda não plotado, sem desenhar circuito.
  - Implementado `ApplyCombinedFraming()` unificando os pontos coletados da rota (`_routeFramingPoints`) e dos contratos (`_contractFramingPoints`) sob margem de 30% e span mínimo de 0.06 graus, eliminando os antigos blocos duplicados de zoom e blocos `catch` vazios.
- Documentação e rastreabilidade:
  - Atualizado `docs/sistemas/missoes-quadro.md` com menção ao `BriefingLegPlanner` e enquadramento combinado.
  - Marcados os itens A3, B1 e B3 em `docs/plans/correcoes-voo-forcado-sdco.md`.
  - Registrada entrada no fim de `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] critério 1 — `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.277/1.277 aprovados).
- [x] critério 2 — `missions_golden.json` intacto (sem modificações).
- [x] critério 3 — Em `BriefingViewModel.cs` o diff ficou restrito a `RebuildItineraryAndValidateAsync`; a string "Circuito de Tráfego: Toque e Arremetida" só é usada no caso TouchAndGo.
- [x] critério 4 — Relatório declara: não testado com o app aberto nem no MSFS (briefing e enquadramento do mapa).
- [x] critério 5 — `docs/CHANGELOG.md` atualizado e itens A3, B1, B3 marcados em `docs/plans/correcoes-voo-forcado-sdco.md`; `docs/sistemas/missoes-quadro.md` atualizado.
- [x] critério 6 — Um commit no padrão `fix(briefing): navegação com escalas deixa de ser TGL e mapa enquadra rota e contrato (BRIEF-NAV)`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1.277 aprovados, 0 falhas (10 novos testes em `BriefingLegPlannerTests`)

## Não testado
- Não testado com o app aberto nem no MSFS (interface gráfica do briefing e enquadramento visual do mapa GMap.NET).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. Todas as regras de negócio de escalas, TGL, local e enquadramento foram implementadas estritamente de acordo com a especificação técnica.
