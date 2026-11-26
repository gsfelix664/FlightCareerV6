# OBJ-CARD-1 — Revisão (Claude, 2026-11-26)

**Veredito: APROVADO**

- `FlightMonitor.cs` (quente), linha a linha: só as 3 mudanças da spec (propriedade, reset no `StartMonitoring`, `BuildLines` + `FormatHud` no tick).
- `App.xaml.cs` (quente): registro singleton e resolve após o login; nada mais.
- `ContractProgressFormatter`: estados explícitos por ramo; `FormatHud` reproduz o texto antigo (testes antigos intactos). `FailureReason ?? ""` equivale ao texto anterior.
- `ObjectiveCardService`: transições de `IsMonitoring` no timer de 1 s, Hide no fim do voo, atalho em janela de mensagens (`HWND_MESSAGE`) com `MOD_NOREPEAT`, desregistro no `Exit`, falhas com `Debug.WriteLine`.
- `DebugOverlayViewModel`: serviço como último parâmetro opcional; o DI injeta porque está registrado.
- Build 0 erros; testes 1.203/1.203; golden igual.
- Não testado no MSFS: janela, foco, arrasto, Ctrl+Alt+O, botão 🎯.
