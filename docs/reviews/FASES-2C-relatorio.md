# FASES-2C — Contratos das fases 3 a 7

**Branch:** feat/fases-2c · **Commit:** 286a013 · **Data:** 2026-11-22

## Plano (antes de codar)
- Adicionar as novas subcategorias de contrato das fases 3 a 7 em `MissionRules.cs` nos Tiers 1, 2 e 3 (`1.7.1`–`1.7.8`, `2.7.1`–`2.7.4`, `3.7.1`–`3.7.2`), com parâmetros físicos, passageiros, carga, distâncias e urgências conforme a especificação.
- Implementar filtros de seleção de destino no `MissionGenerator.cs` (em `CreateMissionFromSubCategory` e `CreateMissionFromSubCategoryInMemory`) para fretamento executivo (mesmo país, tiers 3–4), MEDEVAC dinâmico (pista <= 4.000 ft ou não pavimentada), VIP internacional curto (país vizinho/diferente), ponte aérea (tier 1 ↔ tier 1 mesmo país), cargueiro noturno (hubs tier 1), long-haul internacional (> 2.500 NM entre hubs de países diferentes) e fretamento global (> 2.000 NM).
- Atualizar `career_phases.json` e `CareerPhaseCatalog.cs` com o mapeamento completo dos novos subtipos para suas respectivas fases (3, 4, 6 e 7).
- Atualizar `mission_pay.json` e `MissionPayTable.cs` registrando os novos subtipos nas famílias existentes (`taxi_aereo`, `medevac`, `linha`) e criando as novas famílias `carga_fragil` (escala 0.7 da `aog`) e `carga_global` (escala 0.28 da `linha`).
- Atualizar `xp_rules.json` e `XpRules.cs` adicionando `carga_fragil` (1.35), `carga_global` (1.35) e `1.7.6` (1.00 para cargueiro noturno).
- Criar a suíte de testes `Phase3To7ContractsTests.cs` cobrindo cobertura de fases do catálogo, oráculos do gerador com sementes fixas para os 8 contratos, regras de destino e integridade de escalas de pagamento.

## O que foi feito
- `src/FlightCareer.Application/Services/MissionRules.cs`:
  - Criadas subcategorias nos Tiers 1, 2 e 3 (`CreateTier1CareerCategory`, `CreateTier2CareerCategory`, `CreateTier3CareerCategory`):
    - Fase 3: Fretamento executivo (`1.7.1`, `2.7.1`, `3.7.1`), Carga frágil / expressa (`1.7.2`, `2.7.2`, `3.7.2`).
    - Fase 4: MEDEVAC dinâmico (`1.7.3`, `2.7.3`), VIP internacional curto (`1.7.4`, `2.7.4`).
    - Fase 6: Ponte aérea / troncal (`1.7.5`), Cargueiro noturno (`1.7.6`).
    - Fase 7: Long-haul internacional (`1.7.7`), Fretamento global / carga de alto valor (`1.7.8`).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`:
  - Adicionado suporte a `allRunways` opcional nos métodos de geração para filtros de pista de destino.
  - Implementada filtragem operacional de destinos em `CreateMissionFromSubCategory` e `CreateMissionFromSubCategoryInMemory` conforme as regras de negócio de cada contrato:
    - Fretamento executivo: destino no mesmo país e tier 3–4.
    - MEDEVAC dinâmico: destino com maior pista $\le 4.000$ ft ou pista não pavimentada (`RunwaySurface.IsUnpaved`).
    - VIP internacional curto: destino com país diferente (`Country != origin.Country`).
    - Ponte aérea / troncal: destino hub (tier 1) no mesmo país (`Country == origin.Country`).
    - Cargueiro noturno: destino hub (tier 1).
    - Long-haul internacional: destino hub (tier 1) em país diferente (`Country != origin.Country`).
- `src/FlightCareer.Infrastructure/Data/career_phases.json` e `src/FlightCareer.Domain/Career/CareerPhaseCatalog.cs`:
  - Mapeados todos os novos subtipos `1.7.1`–`1.7.8`, `2.7.1`–`2.7.4`, `3.7.1`–`3.7.2` para suas fases correspondentes (3, 4, 6 e 7) tanto no JSON quanto no fallback embutido seguro de `CareerPhaseCatalog.CreateDefaultCatalog()`.
- `src/FlightCareer.Infrastructure/Data/mission_pay.json` e `src/FlightCareer.Domain/Economy/MissionPayTable.cs`:
  - Adicionadas as novas famílias de pagamento `carga_fragil` (escala 0.7 herdada de `aog`) e `carga_global` (escala 0.28 herdada de `linha`).
  - Mapeados os novos subtipos nas famílias existentes (`taxi_aereo`, `medevac`, `linha`) e nas novas famílias, mantendo paridade e ordem 1:1 entre JSON e `MissionPayTable.Defaults`.
- `src/FlightCareer.Infrastructure/Data/xp_rules.json`, `src/FlightCareer.Domain/Xp/XpRules.cs` e `src/FlightCareer.Domain/Xp/XpCalculator.cs`:
  - Adicionados multiplicadores `carga_fragil: 1.35`, `carga_global: 1.35` e `1.7.6: 1.00` (cargueiro noturno com XP 1,00 para carga ao invés de 1,25 de linha).
  - Exposto método utilitário `XpCalculator.ResolveTypeMultiplier` para resolução pura e desacoplada de multiplicadores por subtipo ou família.
- `src/FlightCareer.Tests/Missions/Phase3To7ContractsTests.cs`:
  - Criados 12 testes unitários e de integração validando catálogo de fases (100% de cobertura de subtipos do `MissionRules`), geração e oráculos com sementes fixas para os 8 contratos, regras geográficas/pistas de destino, escalas de pagamento e persistência no banco.

## Critérios de aceite
- [x] Build 0 erros: verificado com `dotnet build src/FlightCareer.slnx` (0 erros).
- [x] `dotnet test` 100%: 898/898 testes aprovados (100%).
- [x] `career-stab --days 90 --bots 1`: executado com sucesso via CLI.
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `feat(missoes): contratos das fases 3 a 7 (FASES-2C)`.

## Build e testes
- `dotnet build`: 0 erros, 0 avisos novos (77 avisos pré-existentes).
- `dotnet test`: 898 aprovados, 0 falhas (12 testes novos em `Phase3To7ContractsTests.cs`).

## Não testado
- Não visto na tela do app / não testado no MSFS (testado via testes unitários/integração automatizados e simulação headless CLI).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente.
