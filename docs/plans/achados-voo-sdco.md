# Achados do voo de instrução em SDCO (2026-11-25, C152)

Corrigidos em 2026-11-26: ver CHANGELOG (VOO-SDCO-1 a 5). Abaixo, o que ficou.

## Pedidos do usuário (2026-11-26)
- **Cartão de objetivo** (aprovado pelo usuário em 2026-11-26; spec OBJ-CARD-1, branch `feat/cartao-objetivo`): janela como a de alertas, abre no início do voo, arrastável e fechável; reabre pelo botão 🎯 do HUD e pelo atalho global **Ctrl+Alt+O** (escolha do Claude: combinação pouco usada no MSFS; trocar se colidir); amarelo = pendente, verde ✓ = cumprido, vermelho ✖ = falhou; termina com "Pouso final em X". As linhas vêm de `ContractProgressFormatter.BuildLines` (texto + estado), a mesma fonte da linha CONTRATO do HUD.
- **Mapa do quadro com a área de instrução** (feito em 2026-11-26, `feat/mapa-contratos-unificado`): o `FlightMapControl` recebe `ContractMissions` e monta os visuais; quadro, briefing e HUD usam o mesmo caminho.
- Salvar posição do HUD, cartão e alertas em `%LocalAppData%\FlightCareer`, trazendo para dentro da tela.
- Cores por item na linha CONTRATO do HUD (hoje tudo âmbar): usar o estado de `ContractObjectiveLine` (OBJ-CARD-2, junto com as posições).

## Decisões abertas
- Altitude da área genérica: hoje 2.500–4.500 ft AGL (SDCO: até 6.550 ft no altímetro). Sugestão: 1.500–3.000 ft AGL, como as áreas reais; mostrar também a altitude do altímetro.
- Variedade de destino: na fase 1, ~90% dos tipos voltam à origem. Sugestão: tipo "navegação de instrução" só de ida e mais peso no ferry.

## A investigar
- `piston_rpm_overspeed`: 48% das amostras em voo acima de 2550 RPM, dezenas de eventos com dano exibido 0,00%. Conferir o limite no POH e pensar em histerese.
- `FuelUsageAccumulator`: reinício do contador aos 747 s, logo após uma pausa de ~200 s.
- Pausa do MSFS não é detectada por `isPaused` (pacotes congelados com RPM > 0): conferir efeito em horas, desgaste e duração das excedências.

## Voo de conferência (2026-11-26, SDCO, C152 PT-IJG, 21 min reais)
- Contrato cumprido: passou na área de Tatuí, 1 toque e arremetida + pouso final = **2 pousos no Hangar**; status "VOANDO" volta 7 s após a decolagem do toque (regra dos 8 s). Pago R$ 1.438, 306 XP.
- 1,13 h no Hangar para 21 min reais: correto, houve aceleração 16× em ~470 amostras do REC (tempo simulado).
- **Pneus −4,6 % num voo** (toques de 112 e 204 fpm: −1,3 % e −1,4 %; −0,9 % no rolamento lento entre 790 e 792 s): repetiu o caso de 25/11. Investigar `tireDamagePer100Fpm` (2,0) e o dano no táxi/frenagem do toque.
- RPM acima de 2.550 em 210 de 380 amostras em voo (máx. 2.724 a 140 kt em descida): mesmo ponto de `piston_rpm_overspeed` abaixo.
