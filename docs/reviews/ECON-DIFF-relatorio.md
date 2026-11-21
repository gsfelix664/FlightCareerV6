# ECON-DIFF — Botões de calibração do pagamento e economia por dificuldade

**Branch:** feat/econ-diff · **Commit:** pendente · **Data:** 2026-11-21

## Plano (antes de codar)
- Criar `DifficultyEconomy.cs` no domínio com multiplicadores (Easy: 1.25/0.80/1.00; Medium: 1.0/1.0/1.0; Hard: 0.85/1.15/1.25) e labels.
- Atualizar `CareerStart.EasyInitialBalance` para $1.000.000 e atualizar teste `CareerProgressionTests`.
- Criar `MissionPayTable.cs` e `mission_pay.json` (11 famílias de missão com scale 1.0 e resolução ordenada com match por prefixo e IDs exatos), integrando em `MissionGenerator.CalculateReward`.
- Aplicar `CostMultiplier` em combustível e taxas (`FlightService`), serviços pagos (`HangarViewModel.RunPaidServiceAsync` com linha informativa) e caminhão de combustível (`FlightMonitor`).
- Adicionar `HazardMultiplier` em `FailureEngine` e `PartFailureEngine` e aplicar na inicialização do voo em `FlightMonitor` / sessões.
- Aplicar multiplicadores de pagamento, custos e panes no `CareerProgressionSimulator` (`career-stab`).
- Implementar a suíte completa de testes xUnit exigida e rodar benchmark `career-stab` de 30 dias.

## O que foi feito
- `src/FlightCareer.Domain/Economy/DifficultyEconomy.cs`: Novo record com multiplicadores de pagamento, custos e falhas para `Easy`, `Medium`, `Hard` e fallback, além dos labels formatados em português ("Fácil", "Médio", "Difícil").
- `src/FlightCareer.Domain/Services/CareerStart.cs`: `EasyInitialBalance` atualizado para $1.000.000,00.
- `src/FlightCareer.Domain/Entities/Pilot.cs`: Dificuldade padrão inicializada explicitamente como `DifficultyMode.Medium` para estabilidade nominal de entidades criadas sem parâmetro explícito.
- `src/FlightCareer.Infrastructure/Data/mission_pay.json`: Catálogo JSON de remuneração com 11 famílias de missão ordenadas (`instrucao_basica`, `instrucao`, `medevac`, `aog`, `agro_bush`, `agro_municipal`, `taxi_aereo`, `carga_expressa`, `regional`, `linha`, `fallback`), todas com `scale: 1.0`.
- `src/FlightCareer.Domain/Economy/MissionPayTable.cs`: Novo gerenciador de regras de tarifação com `Defaults`, carregamento resiliente com fallback embutido idêntico aos números originais e resolução ordenada por IDs exatos (`subId == prefix`) e prefixos (`subId.StartsWith(prefix)`).
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: `CalculateReward` de instância e estático refatorados para ler da tabela de pagamento e aplicar `family.Scale * payMultiplier`. Propriedade `PayMultiplier` exposta na instância e propagada em `GenerateMissionsInMemory` e `CreateMissionObjectSync`.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`: Injeção de `mg.PayMultiplier` com base na dificuldade do piloto antes de `GenerateMissionsAsync`.
- `src/FlightCareer.Application/Services/FlightService.cs`: Aplicação de `CostMultiplier` da dificuldade do piloto sobre o custo de combustível e taxas aeroportuárias (inclusive em pouso de emergência).
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`: Em `RunPaidServiceAsync`, aplicação de `CostMultiplier` ao custo do serviço logo no início e acréscimo da linha informativa `Dificuldade {nome}: custos ×{m:0.00}` na mensagem de autorização caso o multiplicador seja diferente de 1,0.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: Aplicação de `CostMultiplier` em `ProcessFuelTruckDelivery` e configuração de `HazardMultiplier` nas sessões `_failures` e `_parts` no início do voo (`StartMonitoring`), a partir da dificuldade do piloto já recebido no método sem consultas adicionais ao banco.
- `src/FlightCareer.Domain/Components/FailureEngine.cs` e `src/FlightCareer.Domain/Parts/PartFailureEngine.cs`: Propriedade `HazardMultiplier` adicionada, multiplicando a taxa de risco $\lambda$ antes do sorteio da probabilidade exponencial ($p = 1 - e^{-\lambda \cdot dt}$).
- `src/FlightCareer.UI/Services/ComponentFlightSession.cs` e `src/FlightCareer.UI/Services/PartFlightSession.cs`: Propagação do `HazardMultiplier` da sessão para as instâncias de `FailureEngine` e `PartFailureEngine`.
- `src/FlightCareer.Application/Simulation/CareerProgressionSimulator.cs`:
  - Cache de missões isolado por tripla `(Airport, Day, Difficulty)`.
  - Injeção de `diffEcon.PayMultiplier` em `GenerateMissionsInMemory` e voos de instrução locais.
  - Multiplicação de custos operacionais por `diffEcon.CostMultiplier` (seguro mensal, combustível e taxas de voo local, ferry e missão comercial, peças pré-voo, reparos pós-pane).
  - Injeção de `costMultiplier` em `ApplyPolicyMaintenance` (taxa de inspeção anual, peças recomendadas/reveladas, revisão de óleo e peças adicionais, revisão de ignição e retífica TBO).
  - Configuração de `HazardMultiplier` nos motores de falha do bot e na recriação após compra de nova aeronave.
- `src/FlightCareer.Tests/Economy/DifficultyEconomyTests.cs`: Suíte com 8 testes cobrindo todos os requisitos contratuais, equivalência exata com o código antigo, proporcionalidade e precedência de resolução.
- `src/FlightCareer.Tests/Economy/CareerProgressionTests.cs`: Atualização do teste de saldo do Easy ($1.000.000).

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (692/692 testes aprovados).
- [x] Linha do Médio no dia 30 na simulação de 30 dias (`--days 30 --bots 1 --difficulty all --policy equilibrado --fast`):
  `| Medium | equilibrado | 0% | - | $150,162 | $196,962 [$196,962 - $196,962] | A | 2 | - | 80.9 h | 3.13 | $1,232 | $179 | $6 | 85.5% | 12.4 | 32.1 | 1.2 | 0.0 |`
  (Bate rigorosamente com a referência esperada para o Médio/equilibrado com 1 bot).
- [x] Nenhuma cobrança nova de custo fora dos pontos listados; nenhuma escala ≠ 1 no JSON.
- [x] Um commit: `feat(economia): tabela de pagamento em JSON e dificuldade com pagamento, custos e panes (ECON-DIFF)`.
- [x] Relatório diz o que não foi testado ("não testado no MSFS; tela de confirmação do Hangar não vista").

## Build e testes
- dotnet build: 0 erros, 0 avisos novos de compilação.
- dotnet test: 692 aprovados, 0 falhas (novos: 8 testes em `DifficultyEconomyTests.cs`).

## Não testado
- Não testado no MSFS; tela de confirmação do Hangar não vista interativamente.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A entidade `Pilot.Difficulty` foi inicializada com default `DifficultyMode.Medium` para garantir que entidades instanciadas sem especificar dificuldade mantenham multiplicadores 1.0 (comportamento nominal seguro).
