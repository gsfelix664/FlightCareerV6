# Sensores

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Define o layout de leitura de SimVars, buffers em memória, gravação contínua e sonda de diagnóstico no MSFS.
- **Onde está:** `Domain/Sensors/` (`SensorRegistry`, `SensorLayout`, `SensorCsvFormatter`), `UI/Services/` (`SensorFrameReader`, `ContextSnapshotReader`, `SensorRecorder`), console `FlightCareer.SimVarProbe`.
- **Regras e integridade:**
  - Catálogo em `sensor_registry.json` e `context_registry.json` (inventário em `docs/plans/sensor-inventory.md`). Inclui vento e proas verdadeira/magnética para gravação no REC e cálculo de vento relativo com proa verdadeira. O layout é expandido por motor e preenchido em blocos múltiplos de 256 floats com `SIMULATION RATE` para evitar crash por descarte de pacotes no MSFS.
  - SimVars com erro de nome (`NAME_UNRECOGNIZED`) são rejeitadas e removidas dinamicamente do layout pelo `SensorFrameReader` para evitar deslocamento de memória nos dados subsequentes.
  - Gravação de dados via HUD Overlay (`SensorRecorder`) gera CSVs e metadados contextuais (`-context.json`) na pasta `%LocalAppData%\FlightCareer\sensor-logs\`.
  - Sonda `FlightCareer.SimVarProbe`: valida e monitora SimVars em duas fases (sonda pontual e contínua). Validação addon × nativo em `docs/plans/sensor-validation/`.
