# COMBUSTIVEL-REAL — consumo real, abastecimento gravado no sim e travas contra combustível grátis

**Branch:** feat/combustivel-real · **Commits:** bb06ce6, 9abcd18, 7117095, d2658c4, 3d14ca3, b028be6 · **Data:** 2026-11-16

## Plano (antes de codar)
- Criar a camada de domínio pura `FlightCareer.Domain/Fuel/` contendo:
  - `FuelUsageAccumulator`: soma incrementos de `GENERAL ENG FUEL USED SINCE START:n` por motor, lida com reinício de motor (contador cai), ignora NaN e calcula galões com base no peso específico.
  - `FuelIntegrityMonitor`: detecta trapaça de `UNLIMITED FUEL`, abastecimento indevido em voo (quantidade subindo acima da tolerância) e divergência de combustível em solo (para sincronização automática).
  - `FuelTankWritePlan`: distribui proporcionalmente o combustível desejado pela capacidade útil dos tanques (moderno `FUELSYSTEM` e legados), arredondando a 0.01 galão sem passar da capacidade.
- Estender `SimConnectService` e `ISimConnectService`:
  - Adicionar definição separada `FuelStateRead` e struct `SimFuelState` a 1 s (40 doubles: `UNLIMITED FUEL`, consumo por motor `:1..:4`, `NEW FUEL SYSTEM`, tanques 1..8 e tanques legados).
  - Mapear 17 definições de escrita de tanques e expor `SetFuelTanks` e `GetActiveFuelTanks`.
- Atualizar `HangarViewModel.Refuel` e `RefuelViewModel`:
  - Após cobrança e persistência no banco, se a aeronave estiver conectada, no solo e com motores desligados, gravar os tanques no simulador via `FuelTankWritePlan`.
- Atualizar `FlightMonitor`:
  - `StartMonitoring`: bloquear início se `UNLIMITED FUEL` = 1; sincronizar automaticamente no solo se a quantidade no sim divergir do aplicativo além da tolerância (com registro no blackbox).
  - `OnSimDataReceived`: acumular consumo dos motores e verificar integridade em voo (invalida voo se ilimitado ou abastecimento em voo forem detectados).
  - `FinalizeFlightAsync`: passar consumo real do acumulador e estado de invalidação ao `FlightService`.
- Atualizar `FlightService`:
  - Usar consumo real (`realFuelUsed`) para `outcome.FuelUsed` e cobrança financeira, conferindo divergência > 10% com `(initialFuel - finalFuel)`.
  - Se voo invalidado: zerar receita e XP da missão, não concluir missões, mas cobrar despesa de combustível real e taxa de pouso.
- Criar suíte xUnit de domínio e teste de replay com fixture real `c152-sbmt-snej-20261115.csv.gz`.
- Atualizar `AGENTS.md` e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.Domain/Fuel/FuelUsageAccumulator.cs`: acumulador de consumo de combustível com suporte a reinício de motor (contador que zera e recomeça a contar), descarte seguro de NaN/negativos com diagnósticos drenáveis e conversão para galões.
- `src/FlightCareer.Domain/Fuel/FuelIntegrityMonitor.cs`: monitor de integridade com detecção de `UNLIMITED FUEL`, abastecimento em voo com tolerância a ruído de tanques (padrão 0.5 gal ou 1% da capacidade, o maior) e divergência em solo pré-voo.
- `src/FlightCareer.Domain/Fuel/FuelTankWritePlan.cs`: algoritmo de distribuição proporcional pela capacidade útil dos tanques com cálculo de excedente e ajuste fino de centésimos.
- `src/FlightCareer.Domain/Fuel/SimFuelState.cs`: struct sequencial com alinhamento a 1 byte contendo todas as variáveis de combustível necessárias na definição de leitura separada.
- `src/FlightCareer.Application/Interfaces/ISimConnectService.cs`: inclusão de `CurrentFuelState`, `OnFuelStateReceived`, `GetActiveFuelTanks()` e `SetFuelTanks(...)`.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: registro da definição `FuelStateRead` (a 1 s), mapeamento de escrita de 17 tanques (`FUELSYSTEM TANK QUANTITY:1..8` e tanques legados) e implementação dos métodos de acesso e envio.
- `src/FlightCareer.Tests/Components/TurbineDegradationTests.cs`: adaptação do fake `FakeSimConnectForTurbine` para satisfazer o contrato estendido de `ISimConnectService`.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs` e `RefuelViewModel.cs`: injeção de `ISimConnectService` e gravação direta nos tanques do MSFS quando em solo e motores desligados.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: trava contra `UNLIMITED FUEL` no início; sincronização automática de solo antes do voo; acumulação contínua do consumo por motor; detecção de trapaça em voo marcando o voo como invalidado sem cancelá-lo à força; repasse do consumo real ao finalizar o voo.
- `src/FlightCareer.Application/Services/FlightService.cs`: aceitação de `realFuelUsed`, `isFlightInvalidated` e `invalidationReason`; cálculo de `outcome.FuelUsed` baseado no consumo real; conferência de divergência > 10% no blackbox; invalidação de missões mantendo cobrança do combustível real consumido; persistência de `PilotAircraft.FuelAmount` com o valor final do simulador.
- `src/FlightCareer.Tests/Fuel/FuelDomainTests.cs`: 10 testes unitários abrangendo acumulador com dois motores, reinício de motor, descarte de NaN, monitor de integridade (ilimitado, subida em voo, ruído tolerado, divergência em solo), distribuição de capacidade em asas (13/13 gal), tanques legados e fixture de voo `c152-sbmt-snej-20261115.csv.gz`.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação das decisões de gravação de combustível, travas de integridade e consumo por motor.

## Critérios de aceite
- [x] Build 0 erros; `dotnet test` 100% (486 testes aprovados, 0 falhas).
- [x] Não inicia com `UNLIMITED FUEL`; voo invalidado com abastecimento em voo; sincronização automática no solo.
- [x] Combustível do app gravado no sim (novo e legado), só no solo com motores desligados.
- [x] Relatório com o que não foi testado no MSFS e roteiro de conferência.
- [x] Commits separados por escopo (domain+testes, simconnect, hangar/refuel, flightmonitor, flightservice, docs).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, avisos conhecidos pré-existentes de pacotes (SQLite / SqlClient).
- `dotnet test src/FlightCareer.Tests`: 486 testes aprovados, 0 falhas (10 testes novos em `FuelDomainTests`).

## Não testado no MSFS
- A gravação física das SimVars `FUELSYSTEM TANK QUANTITY:1..N` e `FUEL TANK * QUANTITY` via SimConnect no MSFS 2024 em tempo de execução real do jogo.
- A reação visual da tela de peso e combustível do MSFS 2024 após a escrita do app no solo.
- O comportamento do piloto abastecendo pelo menu do MSFS 2024 em voo real para conferir a detecção em tempo real e o alerta visual no HUD Overlay.

## Roteiro de testes no MSFS (para o usuário/revisor)
1. **Abastecimento no Hangar**:
   - Abrir o simulador com aeronave estacionada no solo, freio de estacionamento acionado e motores cortados.
   - No app, abrir o Hangar e clicar para abastecer a aeronave ativa.
   - Conferir se no simulador (janela de Peso & Combustível do MSFS) os tanques receberam exatamente o volume comprado proporcionalmente à capacidade útil.
2. **Abastecimento pelo menu no solo (Sincronização automática)**:
   - No solo, antes de iniciar o monitoramento, alterar o combustível no menu do MSFS (para mais ou para menos).
   - Clicar em "Iniciar Monitoramento" no app.
   - Conferir que o app não trava, grava o valor correto do app de volta no sim e grava uma linha no blackbox: `combustível sincronizado: sim X gal → app Y gal`.
3. **Abastecimento pelo menu em voo (Invalidação)**:
   - Decolar e iniciar o voo monitorado.
   - Em voo de cruzeiro, abrir o menu do MSFS e aumentar a quantidade de combustível.
   - Conferir que surge o alerta no HUD Overlay: `Voo invalidado: combustível alterado em voo`.
   - Conferir que o monitoramento continua ativo até o pouso.
   - Ao pousar e finalizar o voo, conferir que a missão não é paga (recompensa 0, XP 0), mas o combustível consumido real pelo motor é cobrado financeiramente no extrato do piloto.
4. **Combustível ilimitado**:
   - Nas opções do MSFS, ativar `Combustível Ilimitado` (Unlimited Fuel).
   - Tentar iniciar o monitoramento no app.
   - Conferir que o monitoramento é bloqueado com a mensagem: "Desligue o combustível ilimitado nas opções do simulador para voar na carreira.".
5. **Reinício de motor em voo**:
   - Em voo de cruzeiro, cortar a mistura/ignição do motor e religá-lo em seguida.
   - Conferir que o consumo acumulado continua crescendo normalmente a partir do ponto anterior, sem subtrair nem zerar o gasto total de combustível.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva encontrada. A integração seguiu fielmente a especificação e as decisões do usuário de 2026-11-16.
