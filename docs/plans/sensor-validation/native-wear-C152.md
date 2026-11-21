# Wear and tear nativo — Cessna C152 (F1.4b, voo 1)

**Data:** 2026-11-14 · **Logs:** `%LocalAppData%\FlightCareer\sensor-logs\20261114-0813-Cessna_C152*.csv` (um arquivo por recarga do avião) · **Análise:** Claude, com `tools/analyze_sensor_log.py` + leitura direta dos CSVs.

| Trecho | Horário (UTC) | O que foi feito |
|---|---|---|
| `…0813-Cessna_C152.csv` | 11:13–11:16 | voo com estresse forte, wear and tear **ligado** |
| `-3` | 11:16–11:20 | após reiniciar o voo (`nw-reinicio`); `nw-opcao-desligada` às 11:18:51 |
| `-5` | 11:20–11:26 | após nova recarga, wear and tear **desligado** |

## Achados

1. **Sintaxe:** `WEAR AND TEAR LEVEL:39`, `:39:1` e `:'PISTON_ENGINE'_n` foram aceitas (`RejectedSimVars` vazio) e deram valores idênticos. Monomotor: usar `:<id>`. O índice do 2º motor fica para um bimotor.
2. **Componentes que respondem no C152 (26):** motor (39), pneu (37), trem (35), alternador (28), bateria (29), óleo (41/42), piloto automático (43–45), geral (46), comandos, flaps e cabos (5–14, 19, 20), tanque (0), circuito (33), pressão do pneu (38). **Parados em 100%:** bomba/válvula/linhas de combustível, todo o hidráulico, IDG/RAT/barra/retificadores, **freios (36)**, bequilha, turbina.
3. **Overstress do motor:** taxa `OVERSTRESS DAMAGE RATE:39` ≈ 0,0013–0,0020 quando o RPM passou de ~2.600 (acima da linha vermelha); **o motor perdeu 3,3% em ~40 s**. Na partida, táxi e decolagem normais: ~0.
4. **Pneu e trem:** caíram 2,7% e 1,8% na corrida de decolagem.
5. **Envelhecimento por tempo:** ~0,03% uniforme em comandos/flaps/cabos; ~0,1% em alternador, bateria, óleo e piloto automático em 3 min.
6. **`IS WEAK` / `IS FAILED` / `WEAK VALUE`:** nunca mudaram (nível mínimo 96,6%). Limite de quebra do nativo **não confirmado**.
7. **Reinício do voo:** todos os níveis voltam a 1,00.
8. **Impacto:** às 11:14:55, toque de 60,9 ft/s (~3.650 fpm), 11,8 G, 20° de inclinação, em estol; o avião continuou voando e **todos os níveis voltaram a ~1,00 sem recarga**. O nativo "conserta" depois de uma pancada.
9. **Opção desligada (`-5`):** todos os níveis travados em exatamente 1,00 por 6 min, mas a taxa de overstress continuou reportando (motor 0,0015, pneu 0,0093).
10. **`.FLT`:** o EFB não salvou o arquivo (sem bloqueio do Windows: acesso controlado a pastas desligado, nenhum `.flt` no disco). Relatos: digitar o nome com a extensão entre aspas.

## Consequência para o plano
Por 7, 8 e pela falta de escrita, o nível nativo não serve como fonte da verdade do desgaste. Decisão do usuário (plano › 0.1): o addon é o motor de desgaste; o nativo é sensor de estresse (overstress, achado 3 e 9) e de panes (registradas no Hangar, a que vier primeiro).

## A fazer
- Tabela "overstress `<id>` → peça do addon, multiplicador" e "pane nativa → código de discrepância" (portão F1.4b).
- Repetir num bimotor (índice do 2º motor) e num avião com freios simulados (36).
- Descobrir o limite em que `IS WEAK`/`IS FAILED` ligam (precisa de abuso longo num voo, ou não acontece no voo livre).
