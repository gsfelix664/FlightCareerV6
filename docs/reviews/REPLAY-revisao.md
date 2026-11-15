# Revisão REPLAY

**Veredito: APROVADO**

- `FlightReplayer` (Domain, puro) reutiliza o `PartFlightTracker`; `SensorLogReader` lê `.csv`/`.csv.gz` e toques do blackbox/JSON; CLI `src/FlightCareer.Replay`; fixtures dos três voos reais (< 1 MB, sem posição); 10 testes de replay conferindo comportamento (excedência de RPM no motor 1, horas, toque 658 fpm ±15% do banco, diagnósticos únicos, regras elétricas do C172).
- Mudança fora do escopo aceita: `PartFlightTracker.LastDerivedEvents` (somente leitura, para o replay listar eventos).
- Conferido pelo revisor: build 0 erros; `dotnet test` 319/319; Python 48/48; CLI rodada no voo do C152 (`--aircraft "Cessna C152"`).

## Bugs de lógica revelados pelo replay (não corrigidos aqui; vão para a próxima tarefa)
1. `electrical_system_overload_amps` dispara num voo normal do C152 (faixas de 5, 15 e 30 min): `ELECTRICAL TOTAL LOAD AMPS` mediana **68 A** com alternador de 60 A e barramento estável em 28 V. Carga total do sim não serve sozinha como "sobrecarga".
2. `oil_pressure_low` dispara com o **motor parado** (0 psi antes da partida) e gera inspeção NO-GO de bronzinas. Falta exigir motor em combustão (e um tempo após a partida).
3. `piston_shock_cooling` compara a CHT com `chtMax` em `Below`: dispara com o motor frio (pico 29 °C). Choque térmico é **taxa de queda** da CHT (°C/min), não CHT baixa.
4. Fidelidade do replay: o CSV começa no REC (antes do voo) e o jogo só desgasta depois que o voo é ativado (motor ligado + freio solto por 10 s): 1,137 h no replay × 1,086 h no banco. O replay deve imitar a ativação do `FlightMonitor`.
5. `GENERATOR_OVERHEAT_INSPECTION` sem descrição no `wear_rules.json`.
