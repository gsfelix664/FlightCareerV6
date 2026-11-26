# Achados do voo de instrução em SDCO (2026-11-25, C152)

Corrigidos em 2026-11-26: ver CHANGELOG (VOO-SDCO-1 a 5). Abaixo, o que ficou.

## Pedidos do usuário (2026-11-26), a especificar
- **Cartão de objetivo** (recomendado pelo Claude, não aprovado em detalhe): janela como a de alertas, abre no início do voo, arrastável e fechável; reabre por botão no HUD e atalho global; amarelo = pendente, verde com ✓ = cumprido. Sem addon dentro do MSFS.
- Salvar posição do HUD, cartão e alertas em `%LocalAppData%\FlightCareer`, trazendo para dentro da tela.
- Cores por item na linha CONTRATO do HUD (hoje tudo âmbar).

## Decisões abertas
- Altitude da área genérica: hoje 2.500–4.500 ft AGL (SDCO: até 6.550 ft no altímetro). Sugestão: 1.500–3.000 ft AGL, como as áreas reais; mostrar também a altitude do altímetro.
- Variedade de destino: na fase 1, ~90% dos tipos voltam à origem. Sugestão: tipo "navegação de instrução" só de ida e mais peso no ferry.

## A investigar
- `piston_rpm_overspeed`: 48% das amostras em voo acima de 2550 RPM, dezenas de eventos com dano exibido 0,00%. Conferir o limite no POH e pensar em histerese.
- `FuelUsageAccumulator`: reinício do contador aos 747 s, logo após uma pausa de ~200 s.
- Pausa do MSFS não é detectada por `isPaused` (pacotes congelados com RPM > 0): conferir efeito em horas, desgaste e duração das excedências.
