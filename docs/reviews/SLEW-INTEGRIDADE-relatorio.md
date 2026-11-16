# SLEW-INTEGRIDADE — Slew no solo permitido, voo invalidado no ar e detecção de congelamento

**Branch:** feat/slew-integridade · **Commit:** 88a0c95 · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar `FlightCareer.Domain/Integrity/SimIntegrityState.cs` e `FlightCareer.Domain/Integrity/SlewMonitor.cs` (lógica pura de slew e freeze com distâncias por Haversine).
- Estender `LocationUtils` com `CalculateDistanceMeters(lat1, lon1, lat2, lon2)`.
- Adicionar leitura das SimVars de integridade (`IS SLEW ACTIVE`, `IS ALTITUDE FREEZE ON`, `IS ATTITUDE FREEZE ON`, `IS LATITUDE LONGITUDE FREEZE ON`) em definição separada em `SimConnectService` e contrato em `ISimConnectService`.
- No `FlightMonitor`, extrair `InvalidateFlight(string motivo)` reutilizado por combustível e integridade, alimentar o `SlewMonitor` a cada tick e registrar usos permitidos no blackbox.
- Criar suíte de testes xUnit `SlewMonitorTests` cobrindo todos os cenários solicitados de slew e congelamentos, além de validar definições de telemetria.

## O que foi feito
- `src/FlightCareer.Domain/Integrity/SimIntegrityState.cs`: struct sequencial com `Pack = 1` para leitura via SimConnect de `IS SLEW ACTIVE`, `IS ALTITUDE FREEZE ON`, `IS ATTITUDE FREEZE ON` e `IS LATITUDE LONGITUDE FREEZE ON`.
- `src/FlightCareer.Domain/Integrity/SlewMonitor.cs`: lógica pura de domínio para monitoramento de slew e congelamentos. Acumula distância percorrida no solo via Haversine enquanto slew está ativo, permitindo reposicionamentos até `maxGroundSlewMeters` (1.000 m padrão). Invalida o voo se slew for acionado no ar, após decolagem, ou exceder o limite no solo. Avalia também congelamentos (tolerados no solo antes da decolagem, invalidam o voo no ar ou após decolagem).
- `src/FlightCareer.Domain/Utils/LocationUtils.cs`: adicionado método `CalculateDistanceMeters` convertendo distância náutica para metros exatos.
- `src/FlightCareer.Application/Interfaces/ISimConnectService.cs`: exposta propriedade `CurrentIntegrityState` e evento `OnIntegrityStateReceived`.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: mapeamento da definição `DEFINITIONS.IntegrityStateRead` e request a cada 1 s `DATA_REQUESTS.RequestIntegrityState`. Nenhuma escrita em `IS SLEW ACTIVE`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Extraído método comum `InvalidateFlight(string reason)` reutilizado para combustível ("combustível alterado em voo"), slew ("slew") e congelamento ("congelamento").
  - Instanciação de `SlewMonitor` no `StartMonitoring` e atualização contínua em `OnSimDataReceived`.
  - Ao concluir uso permitido no solo, registra no blackbox via tag `SLEW` o evento `"slew no solo: X m"`.
- `src/FlightCareer.Tests/Integrity/SlewMonitorTests.cs`: 7 testes cobrindo slew permitido no solo, 2 usos somados excedendo limite, slew no ar, slew no solo após decolagem, táxi sem slew (não conta) e congelamentos (em voo e no solo pré-decolagem).
- `src/FlightCareer.Tests/Sensors/TelemetryDefinitionTests.cs`: teste de paridade entre datums de `IntegrityStateRead` e campos de `SimIntegrityState`.
- `src/FlightCareer.Tests/Components/TurbineDegradationTests.cs`: atualizado mock `FakeSimConnectForTurbine` para satisfazer `ISimConnectService`.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação atualizada com as novas regras de integridade do voo.

## Critérios de aceite
- [x] Build 0 erros; testes 100 %.
- [x] Nenhuma escrita em `IS SLEW ACTIVE` (apenas leitura em `IntegrityStateRead`).
- [x] Relatório com roteiro no MSFS: slew curto na vaga (permite), slew longo no solo (invalida), slew no ar (invalida).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 500 aprovados, 0 falhas (8 novos testes: 7 em `SlewMonitorTests` e 1 em `TelemetryDefinitionTests`).

## Não testado
- Conexão em tempo real com MSFS 2024 em execução física (comportamento de hardware).

## Roteiro de verificação no MSFS 2024 (usuário)
1. **Cenário 1 — Slew curto na vaga (Permitido):**
   - Carregar aeronave em qualquer aeroporto, iniciar monitoramento no app.
   - Na vaga (no solo antes de decolar), pressionar `Y` (modo slew do MSFS), mover a aeronave 30 a 50 metros para simular desengate ou correção de vaga.
   - Pressionar `Y` novamente para sair do slew.
   - *Resultado esperado:* O voo continua normalmente. No log do blackbox é registrada a linha `slew no solo: X m`.
2. **Cenário 2 — Slew longo no solo (Invalida):**
   - No solo antes da decolagem, ativar o slew (`Y`) e transladar a aeronave por mais de 1.000 m (ex.: cruzar de uma ponta a outra do aeroporto ou mudar de aeroporto pelo solo).
   - *Resultado esperado:* O HUD exibe "VOO INVALIDADO: Voo invalidado: slew", e o voo é marcado como invalidado sem pagamento.
3. **Cenário 3 — Slew no ar (Invalida):**
   - Decolar normalmente. Uma vez no ar (`SIM ON GROUND = 0`), pressionar `Y`.
   - *Resultado esperado:* O HUD exibe imediatamente "VOO INVALIDADO: Voo invalidado: slew" e registra no blackbox.
4. **Cenário 4 — Slew após pouso (Invalida):**
   - Após pousar na pista de destino, enquanto ainda no solo antes de cortar os motores, pressionar `Y`.
   - *Resultado esperado:* O voo é invalidado por uso de slew após decolagem.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida ou divergência encontrada.
