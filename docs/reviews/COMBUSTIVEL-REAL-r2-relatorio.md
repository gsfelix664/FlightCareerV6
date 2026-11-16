# COMBUSTIVEL-REAL-r2 — correções da revisão rodada 1

**Branch:** feat/combustivel-real · **Commit:** 4efb8c0 · **Data:** 2026-11-16

## Plano (antes de codar)
- Item 1: Acrescentar método `Reset()` em `FuelUsageAccumulator` (limpando dicionários acumulados, últimos valores lidos por motor e diagnósticos) e invocá-lo em `FlightMonitor.StartMonitoring` junto ao reset da integridade. Criar teste demonstrando dois voos seguidos no mesmo acumulador após `Reset`.
- Item 2: Criar classe pura no domínio `FuelSyncCoordinator` (`FlightCareer.Domain/Fuel/FuelSyncCoordinator.cs`) com relógio injetável (`Func<DateTime>`), janela de ~5 s por tentativa e máximo de 3 tentativas para gerenciar a sincronização pendente com tolerância e conferência assíncrona após novas leituras do sim. Integrar ao `FlightMonitor` para que a integridade só inicie após a confirmação da sincronização e que gravações só ocorram no solo. Criar testes unitários para a lógica pura.
- Item 3: Expor `WatchedAircraftId` em `FlightMonitor` e injetar `FlightMonitor` (opcional) em `HangarViewModel` e `RefuelViewModel`, restringindo a escrita de combustível no simulador apenas para a aeronave ativa/vigiada do piloto (`aircraft.Id == _flightMonitor.WatchedAircraftId`).

## O que foi feito
- `src/FlightCareer.Domain/Fuel/FuelUsageAccumulator.cs`: adicionado método `Reset()` que zera `_accumulatedByEngine`, `_lastSeenByEngine` e `_diagnostics`.
- `src/FlightCareer.Domain/Fuel/FuelSyncCoordinator.cs`: nova classe pura de domínio que gerencia o ciclo de vida da sincronização (`Idle`, `Pending`, `Confirmed`, `Failed`), controla o número de tentativas (máx 3), relógio injetável para timeout de ~5 s, sinalização de necessidade de escrita (`NeedsSimWrite`) e guarda de solo (`isOnGround`).
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Exposta a propriedade `WatchedAircraftId => _aircraft?.Id ?? _watchedAircraft?.Id`.
  - No `StartMonitoring`: chamada a `_fuelAccumulator.Reset()` e inicialização do `_fuelSync.Start(...)`. Se divergir no solo, despacha a 1ª escrita e entra em modo pendente; `_fuelIntegrity` só é criado se a sincronização já estiver confirmada de imediato.
  - No `OnSimDataReceived`: enquanto `_fuelSync.Status == FuelSyncStatus.Pending`, avalia a conferência com nova leitura do sim a cada pacote. Se convergir dentro da tolerância, transiciona para `Confirmed`, registra no log e instancia `_fuelIntegrity`. Se passar 5 s sem convergir, regrava no sim até 3 vezes; se esgotar ou sair do solo, alerta "ERRO DE SINCRONIZAÇÃO" e chama `StopMonitoring`. Impede a ativação do voo durante a sincronização pendente.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`: construtor atualizado para receber `FlightMonitor? flightMonitor = null` via DI; método `TryWriteFuelToSimulator` agora valida `_flightMonitor != null && _flightMonitor.WatchedAircraftId == aircraft.Id` antes de gravar no sim.
- `src/FlightCareer.UI/ViewModels/RefuelViewModel.cs`: construtor atualizado para receber `FlightMonitor? flightMonitor = null`; gravação no sim agora confere se a aeronave abastecida é a vigiada ativa no simulador.
- `src/FlightCareer.Tests/Fuel/FuelDomainTests.cs`: 6 novos testes cobrindo reset do acumulador entre dois voos e todos os caminhos do `FuelSyncCoordinator` (confirmação imediata, falha imediata no ar, convergência assíncrona no 2º pacote, timeout de 5 s com reenvio até 3 vezes e falha final, e decolagem durante sincronização pendente).

## Critérios de aceite
- [x] `_fuelAccumulator.Reset()` implementado e chamado no `StartMonitoring` junto ao `_fuelIntegrity`.
- [x] Teste de dois voos seguidos no mesmo acumulador após `Reset` comprovando que o 2º voo cobra somente seu próprio consumo.
- [x] Sincronização pendente com conferência assíncrona após pacote novo do sim, janela de ~5 s, limite de 3 tentativas, exigência de solo e alerta com `StopMonitoring` se divergir.
- [x] `FuelIntegrityMonitor` só inicia após a sincronização estar confirmada.
- [x] Classe de lógica pura `FuelSyncCoordinator` com relógio injetável testada no Domain.
- [x] `HangarViewModel.TryWriteFuelToSimulator` e `RefuelViewModel` gravam no sim somente se a aeronave for a ativa/vigiada (`WatchedAircraftId`).
- [x] `dotnet build`: 0 erros.
- [x] `dotnet test`: 492 aprovados, 0 falhas (486 anteriores + 6 novos).

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos pré-existentes de vulnerabilidades de pacotes herdados (SQLitePCLRaw / System.Data.SqlClient).
- `dotnet test src/FlightCareer.Tests`: 492 aprovados, 0 falhas.
  - Novos testes:
    - `Accumulator_Reset_DoisVoosSeguidos_ContaApenasSegundoVoo`
    - `SyncCoordinator_ValorInicialDentroDaTolerancia_ConfirmaImediatamente`
    - `SyncCoordinator_DivergenciaForaDoSolo_FalhaImediatamente`
    - `SyncCoordinator_DivergenciaNoSolo_EscreveEConfirmaNoProximoTick`
    - `SyncCoordinator_Timeout5s_DisparaNovaTentativaAteTresVezes_E_Falha`
    - `SyncCoordinator_DecolaDuranteSincronizacaoPendente_Falha`

## Não testado no MSFS
- A conferência visual em tempo real no MSFS 2024 de uma sincronização que exija 2 ou 3 tentativas de reenvio por latência de rede SimConnect.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida ou divergência. Todas as 3 correções foram implementadas exatamente de acordo com as especificações da revisão da rodada 1.
