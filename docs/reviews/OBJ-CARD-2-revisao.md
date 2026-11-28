# OBJ-CARD-2 — Revisão (Claude, 2026-11-26)

**Veredito: APROVADO**

- `WindowPlacement.ClampToArea`: regras da spec, com proteção para área menor que a janela; 6 testes de borda (incluindo monitor à esquerda).
- `WindowPositionStore`: cache com lock, gravação em temporário + `File.Move(overwrite)`, NaN/Infinito ignorados, falhas com `Debug.WriteLine`.
- HUD, alertas e cartão: posição salva aplicada com clamp na tela virtual; grava após `DragMove`, ao esconder e ao fechar. Os `catch {}` antigos do HUD viraram `Debug.WriteLine`.
- CONTRATO: `ContractItems` (3 primeiros, chave texto+estado), cores por estado; `HasContractProgress` mantido.
- Build 0 erros; testes 1.212/1.212.
- Não testado no MSFS nem com dois monitores.
