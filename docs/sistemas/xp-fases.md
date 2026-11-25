# XP e fases

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Calcula experiência ganha nos voos, níveis do piloto e progressão pelas 7 fases da carreira.
- **Onde está:** `Domain/Xp/XpCalculator.cs`, `Domain/Xp/PilotLevel.cs`, `Domain/Entities/CareerPhase.cs`, `Domain/Career/CareerPhaseCatalog.cs`, `CareerProgressionSimulator.cs`.
- **Regras e integridade:**
  - Fórmula desacoplada de XP: `XpCalculator.Compute` combina esforço base (tempo no ar, distância, pernas), multiplicador do tipo de missão, multiplicador de realismo (auxílios e partida automática), qualidade operacional (suavidade do pouso e infrações monitoradas por `FlightInfractionTracker`) e bônus fixos de ambiente/pista. Estimativa exibida no quadro via `XpCalculator.Estimate`.
  - Prazo do bônus no quadro (2026-11-25): `XpDeadline.Compute` usa a mesma regra do fim do voo (distância direta ÷ cruzeiro + 15 min; ≤ 90 % e ≤ 115 %) e o pré-despacho mostra "Prazo: +30/+60 XP com voo até …" (vermelho em missão urgente ou do quadro `Medica`); não aparece em voo local.
  - Partida automática (`ENGINE_AUTO_START` / Ctrl+E) aplica penalidade de 10% no XP total. Voos invalidados ou acidentes com perda total zeram o XP ganho.
  - Níveis do piloto (`PilotLevel`): progressão quadrática contínua sem nível máximo baseada em XP acumulado.
  - Fases de carreira (Fases 1 a 7, substituindo as 5 categorias antigas): restringem o aceite e início de missões (não a compra de aeronaves). Subir de fase exige nível, horas de voo e taxa de homologação em `Infrastructure/Data/career_phases.json`.
  - Parâmetros e multiplicadores calibrados em `Infrastructure/Data/xp_rules.json` e `career_phases.json`.
  - Telemetria de realismo adicionada em `SimTelemetry` (crash detection, auto-coordenação, realismo, visibilidade, nuvem e altitude-densidade).
