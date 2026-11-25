# HUD-PROGRESSO — Progresso do contrato no HUD

**Branch:** feat/hud-progresso · **Commit:** (registrado no commit único da tarefa) · **Data:** 2026-11-25

## Plano (antes de codar)
- Expandir `OverflyTracker` com `OverflyAltitudeState`, propriedades `Targets`, `LastLatitude/LastLongitude/LastAltitudeFt` e método puro `EvaluateAltitude`.
- Implementar `ContractProgressFormatter` em `FlightCareer.Domain.Missions` com suporte aos 8 rastreadores de contrato, cultura pt-BR e limite de 3 linhas.
- Integrar `FlightMonitor.ContractProgressText` nos 3 pontos estritos solicitados (propriedade, reset no StartMonitoring e cálculo no OnSimDataReceived).
- Integrar `DebugOverlayViewModel` e XAML da janela do HUD nos modos compacto e expandido com visibilidade dinâmica.
- Cobrir com 10 testes unitários em `ContractProgressFormatterTests` e validar build e suíte de testes.

## O que foi feito
- `src/FlightCareer.Domain/Missions/OverflyTracker.cs`:
  - Adicionado enum `OverflyAltitudeState { InBand, Below, Above }`.
  - Adicionadas propriedades públicas `Targets`, `LastLatitude`, `LastLongitude` e `LastAltitudeFt`.
  - Adicionado método estático `EvaluateAltitude(OverflyPoint point, double altitudeFt)` que avalia `Below`, `Above` ou `InBand` respeitando tolerância de 500 ft (quando `MinAglFt > 0`) ou teto nominal (quando `MinAglFt == 0`).
  - Atualizado `Update` para gravar coordenadas/altitude e usar `EvaluateAltitude(point, currentAltitudeFt) == OverflyAltitudeState.InBand`.
  - Atualizados `Reset` e `ConfigureWaypoints` para limpar coordenadas/altitude registradas.
- `src/FlightCareer.Domain/Missions/ContractProgressFormatter.cs`:
  - Criada classe pura `ContractProgressFormatter` e record `ContractProgressInputs` para formatação tática de progresso dos 8 tipos de contratos em cultura `pt-BR`.
  - Suporte a TGL, sobrevoo com identificação de ponto mais próximo e faixa/teto, escalas com violações ou próxima parada, voo de teste passo a passo e cardinais, pousos curtos STOL, pernas de levantamento, lançamento de paraquedismo e restrições de translado ferry.
  - Junção com quebras de linha `\n` limitando a no máximo 3 linhas.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Adicionada propriedade pública `ContractProgressText { get; private set; } = string.Empty;`.
  - Em `StartMonitoring`, resetada `ContractProgressText = string.Empty;`.
  - Em `OnSimDataReceived`, cálculo síncrono de `ContractProgressText` via `ContractProgressFormatter.Build` após atualização dos rastreadores.
  - Nenhuma outra linha ou ordem de chamada alterada (3 pontos estritos).
- `src/FlightCareer.UI/ViewModels/DebugOverlayViewModel.cs`:
  - Adicionadas propriedades observáveis `ContractProgressText` e `HasContractProgress`.
  - Em `OnSimDataReceived` (dentro do Dispatcher), atualização síncrona lendo `FlightMonitor.ContractProgressText` e ativando `HasContractProgress`.
- `src/FlightCareer.UI/Views/DebugOverlayWindow.xaml`:
  - Modo compacto: `TextBlock` com `Text="{Binding ContractProgressText}"`, `FontSize="10"`, `FontWeight="SemiBold"`, `Foreground="#FBBF24"`, `TextWrapping="Wrap"`, visibilidade ligada a `HasContractProgress`.
  - Modo expandido: `Border` idêntico ao da rota contendo `DockPanel` com `TextBlock "CONTRATO: "` (`FontSize="10.5"`, `FontWeight="Bold"`, `Foreground="#64748B"`, `VerticalAlignment="Top"`) e `TextBlock` do texto (`FontSize="11"`, `FontWeight="Bold"`, `Foreground="#FBBF24"`, `TextWrapping="Wrap"`), com visibilidade ligada a `HasContractProgress`.
- `src/FlightCareer.Tests/Missions/ContractProgressFormatterTests.cs`:
  - Criados 10 testes unitários cobrindo todos os cenários solicitados (vazio, TGL, sobrevoo abaixo/na faixa/acima, tolerâncias de altitude, escalas, voo de teste, restrição de translado e limite de 3 linhas).

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx passa com 0 erros (0 erros, 81 avisos pré-existentes).
- [x] dotnet test src/FlightCareer.Tests passa (1081 aprovados, 0 falhas).
- [x] git diff de FlightMonitor.cs toca apenas os 3 pontos permitidos (conferido via git diff).
- [x] sem catch {} novo em código adicionado.
- [x] Commit único na branch feat/hud-progresso.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos (81 avisos pré-existentes de obsolescência/nulidade da solução)
- dotnet test: 1081 aprovados, 0 falhas (10 novos testes em `ContractProgressFormatterTests`)

## Não testado
- Renderização visual in-game com a janela transparente sobreposta ao Microsoft Flight Simulator em tempo real.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. As regras e especificações do contrato foram seguidas estritamente.
