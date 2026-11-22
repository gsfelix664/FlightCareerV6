# METRICAS-A — Revisão (Claude)

**Branch:** `feat/metricas-a` (`c743de4` + ajustes do revisor) · **Data:** 2026-11-22

## Veredito: **APROVADO** (com ajustes do revisor, rodada única)

## Achados e ajustes feitos pelo revisor
1. **Três regras nunca disparavam no jogo:** frenagem, curva no táxi e derrapagem procuravam `ground-velocity`/`ctx-ground-velocity`, mas o registro não gravava a velocidade no solo a cada quadro. O relatório deu o replay como sucesso sem ler os diagnósticos ("velocidade de solo ausente"). Ajuste: sensor `ground-velocity` (`GROUND VELOCITY`, knots) no fim do `sensor_registry.json`; reserva pela velocidade verdadeira para logs antigos (só frenagem e derrapagem).
2. **Falso abuso na corrida de decolagem:** a curva no táxi acompanhava a maior velocidade até o fim da ocorrência (30 kt na aceleração para decolar). Ajuste: velocidade só conta com a bequilha virada (≥ 30 %), potência de decolagem encerra/ignora (`IsTakeoffPower`: RPM ≥ 80 % da linha vermelha, manete ≥ 70 % ou torque ≥ 70 %), e a curva exige GS real (a velocidade verdadeira inclui o vento).
3. Testes de layout atualizados (+1 sensor); replay do C152 de 2026-11-15: +0,2 % no pneu do nariz por 2 s de derrapagem legítima no toque de 658 fpm (pouso suave não marca derrapagem — conferido no voo de 2026-11-22).

## Conferido
- Energia de frenagem: soma Δ(v²/2) só quando a velocidade cai, sem freio de estacionamento, referência 1,3 × Vso. A calibrar com voos: frenagem forte logo no toque do C152 (~45 kt) dá ~110 % (faixa de alerta).
- `TURB ENG MAX TORQUE PERCENT` é o torque atual em % do nominal (SDK), uso correto.
- Replay do voo com abusos de 2026-11-22: partida com 3 luzes (bateria −1,1 %, motor de partida −0,9 %) e curva no táxi a 21,6 kt detectadas; a freada foi a ~10 kt (5 % da referência, sem evento, correto) e as derrapagens < 1 s.
- Build 0 erros, 754/754; fuzz all 2.300/2.300 (com o sensor novo, antes do ajuste 2, que só reduz eventos).

## Não testado
- No MSFS ao vivo. Conferir no próximo voo de abusos: curva no táxi, frenagem forte no pouso e derrapagem longa.
