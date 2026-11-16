# TAXAS-F1 — Catálogo de taxas de aeroporto e calculador (F1 do plano)

**Branch:** feat/taxas-aeroporto-f1 · **Commit:** pendente · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar `src/FlightCareer.Infrastructure/Data/airport_fees.json` com schemaVersion, currency "$", note "estimativa — calibrar", sizeClasses (<12.500 lb light, 12.500-60.000 lb medium, >60.000 lb heavy), LANDING por tier (1: 250 + 0,035/lb, 2: 80 + 0,015/lb, 3: 25 + 0,005/lb, 4: 0), PAX_FEE (t1: 12, t2: 6, t3: 2, t4: 0), HANDLING (medium: 250 t1 / 150 t2; heavy: 1.500 t1 / 800 t2), NAV_TERMINAL ($3 por 1.000 lb MTOW para medium/heavy em t1-3), PARKING (franquia 2 h; t1: 5/40/200, t2: 3/25/120, t3: 1/10/50), countryOverrides e icaoOverrides vazios.
- Vincular `airport_fees.json` no `src/FlightCareer.Tests/FlightCareer.Tests.csproj` para cópia automática na saída de testes.
- Implementar no domínio (`src/FlightCareer.Domain/Economy/`):
  - `AircraftSizeClass` (enum: Light, Medium, Heavy) e classificação `AircraftSizeClassifier.Classify(mtowLbs)`.
  - `FeeLine(string Code, string Description, double Quantity, double UnitPrice, double Total)` e `FeeContext(int airportTier, string icao, double mtowLbs, int passengers, bool hasMission, double parkedGameHours = 0)`.
  - `AirportFeeCatalog` com validação de integridade, carregamento de JSON e `LoadDefault(directory)`.
  - `GroundServiceCalculator.Calculate(FeeContext, AirportFeeCatalog?)` calculando todas as taxas com descrições em português e arredondamento a 2 casas, retornando lista vazia para Tier 4.
- Integrar no `src/FlightCareer.Application/Services/FlightService.cs`:
  - Substituir cálculo inline de taxa de pouso pelo `GroundServiceCalculator.Calculate(feeContext)` com passageiros concluídos e indicador de missões concluídas no destino.
  - Criar `FinancialTransaction` para cada taxa com `Total > 0` com categoria mapeada ("Taxa de Pouso", "Taxa de Embarque", "Handling", "Navegação/Terminal") debitando o saldo do piloto.
  - Atribuir a soma das taxas a `outcome.LandingFee` (comentário de compatibilidade) e atualizar mensagem com `" | Taxas: {total:C2}"` se houver taxas adicionais.
  - Delegar `CalculateLandingFee` para o catálogo preservando a assinatura pública.
- Criar suíte completa de testes unitários em `src/FlightCareer.Tests/Economy/AirportFeeTests.cs` e adicionar teste de persistência em `FlightPersistenceTests.cs`.
- Validar build e suite de testes (`dotnet build` e `dotnet test`).

## O que foi feito
- `src/FlightCareer.Infrastructure/Data/airport_fees.json`: catálogo calibrável com taxas de pouso (tiers 1–4 idêntico à fórmula legada), embarque por passageiro, handling para médio/pesado em tier 1–2, navegação/terminal por 1.000 lb de MTOW em tier 1–3, e estacionamento com 2 h de franquia por tier × porte. Moeda em `$` genérico.
- `src/FlightCareer.Tests/FlightCareer.Tests.csproj`: link e cópia de `airport_fees.json` para o diretório de execução dos testes.
- `src/FlightCareer.Domain/Economy/AircraftSizeClass.cs`: enum `AircraftSizeClass` (`Light`, `Medium`, `Heavy`) e classificador `AircraftSizeClassifier.Classify(mtowLbs)`.
- `src/FlightCareer.Domain/Economy/FeeModels.cs`: record `FeeLine` e record `FeeContext` com propriedades e construtor camelCase conforme especificação.
- `src/FlightCareer.Domain/Economy/AirportFeeCatalog.cs`: deserialização com tratamento de validação e comentários, resolução de caminhos multi-candidatos (`LoadDefault`) e cache `Default`.
- `src/FlightCareer.Domain/Economy/GroundServiceCalculator.cs`: motor determinístico puro de cálculo de taxas de solo e aeroporto; tier 4 retorna lista vazia.
- `src/FlightCareer.Application/Services/FlightService.cs`: cálculo das taxas aeroportuárias via `GroundServiceCalculator.Calculate`, geração de transações individuais no SQLite debitando saldo do piloto, consolidação no `outcome.LandingFee`, sufixo `" | Taxas: {total:C2}"` e delegação compatível de `CalculateLandingFee(airport, mtowLbs)`.
- `src/FlightCareer.Tests/Economy/AirportFeeTests.cs`: 24 novos testes unitários cobrindo todos os cenários e oráculos pedidos.
- `src/FlightCareer.Tests/Components/FlightPersistenceTests.cs`: teste de integração `FlightService_ProcessFlightFinish_CreatesTransactionsPerFeeCategory` validando persistência no SQLite de transações individuais de taxas por categoria e atualização do saldo do piloto.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação do estado da economia e changelog atualizados.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` todo verde (552 + 25 = 577 aprovados).
- [x] Nenhum "R$"/"US$" novo no código ou nos dados (verificado via grep).
- [x] CHANGELOG `## 2026-11-16 - Taxas de aeroporto F1: catálogo e calculador (Gemini)`; `AGENTS.md` › 7 (Outros sistemas: economia).
- [x] Commit `feat(economy): catalogo de taxas de aeroporto e calculador (F1)`; correções em commit novo, sem `--amend`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 12 avisos pré-existentes de pacotes SQLite/SqlClient.
- `dotnet test src/FlightCareer.Tests`: 577 aprovados, 0 falhas (24 novos em `AirportFeeTests` + 1 novo em `FlightPersistenceTests`).

## Não testado
- Telas da UI (Briefing, Extrato, Hangar) e rastreador de telemetria em tempo real no sim (escopos de F2 e F3).

## Dúvidas e decisões para o revisor
- Nenhuma. Implementação seguiu estritamente a especificação e o plano aprovado.
