# Plano — Taxas de aeroporto e serviços de solo

Criado em 2026-11-16 (Claude). Status: **proposta**, aguardando decisões do usuário (§ 6).
Origem: análise das SimVars (`docs/plans/sensor-research/variaveis-eletrica.md`, `variaveis-diversos.md`): GPU, APU, estacionamento e clima podem ser detectados no sim e cobrados.

## 1. Objetivo
Cobrar o custo real de operar em cada aeroporto: além da taxa de pouso, estacionamento, serviços de solo e taxas por passageiro, de forma que **jatos grandes e hubs fiquem caros** e aeroportos regionais/pistas rurais continuem baratos. A escolha do aeroporto e do avião passa a pesar na economia.

## 2. O que já existe
- `FlightService.CalculateLandingFee(Airport, mtowLbs)` (`Application/Services/FlightService.cs:370`): taxa de pouso por **tier** (1 hub 250 + 0,035·MTOW lb; 2 regional 80 + 0,015·MTOW; 3 municipal 25 + 0,005·MTOW; 4 rural = 0), lançada como `FinancialTransaction` categoria "Taxa de Pouso" e em `FlightOutcome.LandingFee`.
- `Airport` (`Domain/Entities/Airport.cs`): `ICAO`, `Country`, `ElevationFt`, `Tier`, `IsInSimulator`.
- Combustível com preço por aeroporto (`FuelPriceService`).
- **Problema atual:** valores fixos no código; não há outros serviços.

## 3. Catálogo de taxas e serviços (dados, não código)
Novo `Infrastructure/Data/airport_fees.json`, com valores por **tier** e por **porte da aeronave** (categoria por MTOW), e sobrescrita opcional por país ou por ICAO.

| Código | Serviço | Base de cobrança | Detecção | Obrigatório? |
|---|---|---|---|---|
| `LANDING` | Taxa de pouso | fixo + $/lb MTOW (migrar o cálculo atual) | pouso no destino | sim (tier 1–3) |
| `PARKING` | Estacionamento | por hora acima de uma franquia (ex.: 2 h), por porte | tempo entre o fim do voo e a próxima partida; `PLANE IN PARKING STATE` | sim |
| `GPU` | Energia externa | por minuto conectado | `EXTERNAL POWER ON` | não (opcional, ou obrigatório em jatos sem APU ligada) |
| `PUSHBACK` | Reboque/pushback | fixo por porte | `PUSHBACK STATE` (página de SimVars ainda não analisada) | jatos em gate |
| `DEICE` | Degelo | por aplicação, escala com o porte | antes da decolagem com `AMBIENT TEMPERATURE` ≤ 0 °C e `AMBIENT PRECIP STATE` chuva/neve ou `STRUCTURAL ICE PCT` > 0 | oferecido; recusar aumenta o risco de gelo |
| `HANDLING` | Handling/rampa (bagagem, escada, balizamento) | fixo por porte | pouso com missão de passageiros/carga em tier 1–2 | jatos e turbo-hélices grandes |
| `PAX_FEE` | Taxa de embarque/terminal | por passageiro embarcado | missão | missões de passageiros |
| `CATERING` / `CLEANING` / `LAVATORY` | Serviços de cabine | por voo, por porte | missão de passageiros, duração | jatos (opcional, afeta a nota dos passageiros) |
| `NAV_TERMINAL` | Tarifa de navegação/terminal | por voo IFR, por peso | tipo de missão / plano IFR | IFR |
| `HANGAR_NIGHT` | Pernoite em hangar | por noite, por porte | aeronave parada no destino além de X h | opcional (protege do clima; futuro) |

**Porte da aeronave** (decidir limites): leve (< 12.500 lb), médio (12.500–60.000 lb), pesado (> 60.000 lb). Usar MTOW do perfil (`Data/aircraft/*.json`) ou `MAX TAKEOFF WEIGHT` do sim.

## 4. Arquitetura
- **Domain/Application** (lógica pura, testável):
  - `AirportFeeCatalog` (carrega o JSON, valida, resolve tier × porte × país/ICAO).
  - `GroundServiceCalculator.Calculate(contexto) → IReadOnlyList<FeeLine>`: recebe aeroporto, perfil da aeronave, missão (pax, carga, IFR) e **uso medido** (minutos de GPU, horas paradas, degelo, pushback). Sem SimConnect.
  - `FeeLine`: código, descrição em português, quantidade, valor unitário, total.
- **UI/Services**: um rastreador fino (`GroundServiceTracker`) alimentado pelo `FlightMonitor`/telemetria acumula tempo de GPU, estado de pushback e condições de degelo **por tempo simulado** (regra `AGENTS.md` › 5), e entrega o uso ao `FlightService` no fim do voo.
- **FlightService**: troca `CalculateLandingFee` pelo calculador; cada `FeeLine` vira uma `FinancialTransaction` (categoria = serviço) e o total entra no `FlightOutcome` (substitui/estende `LandingFee`).
- **Briefing**: estimativa das taxas do destino antes de aceitar a missão (o piloto escolhe aeroporto mais barato); **Finanças**: extrato por categoria.
- **Banco**: sem tabela nova na fase 1 (transações existentes). Estacionamento entre voos precisa guardar o horário de chegada da aeronave (coluna nova em `PilotAircraft`, via `EnsureSchemaUpdatedAsync`).

## 5. Fases
1. **F1 — Catálogo e calculador**: JSON + `AirportFeeCatalog` + `GroundServiceCalculator` com `LANDING` (mesmo valor de hoje, teste de regressão), `PARKING`, `PAX_FEE`, `HANDLING`, `NAV_TERMINAL`. Só dados da missão; nada do sim. Testes de cálculo por tier × porte.
2. **F2 — Serviços medidos no sim**: `GPU`, `DEICE`, `PUSHBACK` com o rastreador; testes de replay com fixtures (GPU ligada/desligada, frio com chuva). Depende do Watcher/MSFS confirmar `EXTERNAL POWER ON` e `PUSHBACK STATE`.
3. **F3 — Interface**: estimativa no briefing, extrato nas finanças, recibo no fim do voo.
4. **F4 — Efeitos de jogo**: recusar degelo aumenta risco de gelo; catering/limpeza na nota dos passageiros; pernoite em hangar.

## 6. Decisões do usuário
1. **Decidido (2026-11-16): moeda genérica `$`, base dólar, sem escrever "US$"** — igual ao resto do app (o `App.xaml.cs` força a cultura en-US, então `:C2` sai como `$1,234.56`). Valores do `airport_fees.json` em dólar. Escala sugerida: C152 num hub ≈ $300 no total; jato grande ≈ $15.000–30.000 (pouso + estacionamento + passageiros + handling). A taxa de pouso atual fica igual (teste de regressão).
2. Porte — **recomendação (aguarda confirmação):** leve < 12.500 lb, médio 12.500–60.000 lb, pesado > 60.000 lb; taxa de pouso continua linear no MTOW (o porte serve para estacionamento, handling e serviços).
3. Obrigatórios — **recomendação (aguarda confirmação):** pouso, estacionamento acima da franquia e taxa de embarque (missões de passageiros); handling obrigatório para jatos e turbo-hélices grandes em tier 1–2; GPU, catering, degelo e hangar opcionais (fases F2–F4).
4. Sobrescrita por país — **recomendação (aguarda confirmação):** depois da F1, com o campo já previsto no JSON.
5. Estacionamento — **recomendação (aguarda confirmação):** só tempo de jogo (parado no solo na sessão, acima de 2 h de franquia); app fechado não conta.

## 7. Testes previstos
- `LandingFee_MesmoValorDoCalculoAtual_PorTier` (regressão).
- `Parking_FranquiaEDepoisPorHora_PorPorte`.
- `PaxFee_PorPassageiro_SoEmMissaoDePassageiros`.
- `Tier4_SemTaxas` (pista rural).
- `Gpu_MinutosPorTempoSimulado` (replay; usa `SIMULATION RATE`).
- `Deice_SoComFrioEPrecipitacao`.
