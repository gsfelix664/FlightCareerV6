# Limites por aeronave

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Fornece perfis de limites operacionais de motor, elétrica, arrefecimento e velocidades para monitoramento e estresse de peças.
- **Onde está:** `Domain/Limits/AircraftLimitResolver`, `Infrastructure/Data/aircraft/` (JSON por modelo + `_schema.json` e `_class_defaults.json`), tabelas `AircraftLimitProfiles` e `AircraftLimits`, `AircraftLimitSeeder`.
- **Regras e integridade:**
  - Resolução por precedência estrita: POH → SimVar → Cfg → NativeOnset → ClassDefault.
  - Limites semeados no banco na inicialização pelo `AircraftLimitSeeder` com upsert por versão (`dataVersion`).
  - Features do modelo definem exceções de regras: `noElectrical` (isentos de elétrica), `noFlaps` (isentos de Vfe/over-speed), `noAvionicsMaster` e `convertibleCabin`.
  - Calibração e importação executadas em tempo de desenvolvimento via ferramentas Python (`tools/aircraft_limits/calibrate.py`, `merge_limits.py`, `aircraft_limits-v4.json`). HUD suporta modo guiado de calibração em solo (`CalibrationScript`).
