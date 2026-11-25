# Voo e integridade

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Monitora fases de voo, toque, acidentes, conforto de passageiros, infrações e anti-cheat de slew/congelamento.
- **Onde está:** `UI/Services/FlightMonitor`, `Domain/Components/TouchdownDetector`, `Domain/Integrity/SlewMonitor`, `Domain/Services/FlightQualityService`, `UI/Services/AlertOverlayService`, `CrashPolicy`.
- **Regras e integridade:**
  - Toque e impacto medidos pelo `TouchdownDetector` priorizando `PLANE TOUCHDOWN NORMAL VELOCITY` (reserva `VERTICAL SPEED`, sem `VELOCITY BODY Y`). Crash detectado pelo simulador gera perda total direta (`CrashPolicy`), registrando voo no logbook, avançando carreira e devolvendo contratos ao mercado.
  - Integridade de posição (`SlewMonitor`): slew curto no solo antes da decolagem é tolerado (até 1.000 m acumulados com log); slew no ar, slew pós-decolagem ou qualquer congelamento de atitude/posição invalidam o voo (`FlightMonitor.InvalidateFlight`).
  - Conforto de passageiros (`FlightQualityService`): acumula desconforto em segundos simulados fora dos limites operacionais (`FlightQualityRules`), ignorando solo e pausa. Voos de carga avaliam apenas solavanco e fator G. Vento vertical atenua penalidades.
  - Notificações em voo: utilizar sempre `AlertOverlayService` (janela overlay, topo, sem foco); nunca exibir caixas modais (`MessageBox`) durante o voo monitorado.
  - Pendência: W&B e detecção de aeronave (`docs/plans/simconnect-wb-deteccao-aeronave.md`).
