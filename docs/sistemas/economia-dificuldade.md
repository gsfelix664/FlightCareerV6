# Economia e dificuldade

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Modela saldo, finanças, tarifas, taxas aeroportuárias, seguro de casco, conversão de cabine e mercado de usados.
- **Onde está:** `Domain/Economy/` (`DifficultyEconomy`, `MissionPayTable`, `AirportFeeCatalog`, `GroundServiceCalculator`, `CabinConversion`), `Application/Services/` (`EconomyServices`, `InsuranceService`), `Infrastructure/Services/MarketSimulationService`, `PartMigrationService`.
- **Regras e integridade:**
  - Modos de dificuldade (`DifficultyEconomy`): Fácil, Médio e Difícil aplicam multiplicadores sobre saldo inicial, receitas de contratos (`PayMultiplier`), despesas operacionais/oficina (`CostMultiplier`) e risco de panes (`FailureMultiplier`/`HazardMultiplier`).
  - Tarifas de missão parametrizadas em `Infrastructure/Data/mission_pay.json`.
  - Taxas de aeroporto e solo (`Infrastructure/Data/airport_fees.json`): cobranças de pouso, embarque de passageiros, rampa e navegação/terminal por porte e tier do aeroporto via `GroundServiceCalculator`.
  - Seguro aeronáutico: prêmio mensal, franquia e indenização por perda total baseados no valor do avião. Sinistro negado (`ClaimDenied`) se voou com discrepância grave não sanada da inspeção anual. Prazos controlados pelo relógio de carreira (`InsuranceService`).
  - Conversão de cabine (`CabinConversion`): permitida apenas em modelos com feature `convertibleCabin` (`AircraftTypes.HasConvertibleCabin`), reconfigurando capacidades de passageiros e carga e filtrando contratos compatíveis (`IsMissionCompatible`). Custo e downtime escalam pelo porte da aeronave.
  - Mercado de usados (`MarketSimulationService`): aeronaves geradas com desgaste proporcional a horas e ciclos; peças de motor limitadas pelo TSMO; inspeção pré-compra reflete na saúde conhecida (`KnownHealth`).
  - Calendário e relógio de carreira (`Pilot.CareerClockUtc` / `CareerClock`): avança exclusivamente por tempo voado simulado e horas de oficina, nunca pelo relógio do computador.
