# HUD em modo tablet (plano futuro)

> Ideia do usuário, 2026-11-25. Ainda sem spec nem decisões de implementação. Estado atual do HUD: `docs/sistemas/hud.md`.

## Pedido do usuário
- O HUD overlay ganha um terceiro modo, **estilo tablet, na horizontal**, além do compacto e do expandido.
- Mostra **mais informações** que o modo expandido e tem um **mapa maior à direita**.
- Pode exibir as **informações da tela de briefing** (missão, rota, carga, combustível pago, contrato).
- O usuário pode **iniciar o voo a partir do HUD**, sem voltar à janela principal.
- **Acesso rápido** a:
  - abastecimento;
  - conferência do seguro;
  - conferência das peças (estado da aeronave).

## A decidir quando virar tarefa
- Layout: o que vai à esquerda (painéis/abas) e o tamanho do mapa; troca entre os modos.
- Iniciar o voo pelo HUD: reaproveitar a mesma conferência do briefing (combustível a bordo × pago, `StartMonitoring`) sem duplicar regra na UI.
- Acessos rápidos: só leitura ou com ações (ex.: pagar abastecimento, ativar seguro) e de onde vêm os dados (serviços existentes do Hangar e do briefing).
- Uso com o simulador em tela cheia (foco, teclado, janela sempre visível).
