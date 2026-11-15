# F1.4c — Calibração addon × nativo: Cessna C152 (asfalto)

**Data:** 2026-11-15 · **Status:** APROVADO PELO USUÁRIO em 2026-11-15 (itens do portão abaixo; pendências de voo continuam abertas)
**Logs:** `20261115-0600-Cessna_C152.csv` (voo 1, 37 min, 12 trechos) e `20261115-0648-Cessna_C152.csv` (voo 2, 16 min: cruzeiro, RPM 60 s, voo longo até a pane).
Relatório bruto: `py -3 tools/analyze_sensor_log.py --type A --model C152 --calibration <csv>`.

## 0. Condição do simulador (achado novo)
- No voo 1 as opções **"Desativar Danos Causados por Estresse do Motor"** e **"... da Aeronave"** (Assistência › Realismo) estavam **marcadas**. Resultado: a taxa `OVERSTRESS DAMAGE RATE` continua sendo reportada, mas o **nível fica travado em 1,00** (mesmo padrão do achado 9 de `native-wear-C152.md`). O voo 1 serve para limiares e para ver quem responde; não serve para unidade.
- No voo 2 as duas opções foram desmarcadas: o nível passou a cair.
- **Chave anti-trapaça (F1.4b › c):** taxa de overstress > 0 por alguns segundos com o nível parado em exatamente 1,00 ⇒ dano por estresse desativado no sim.

## 1. Motor (ID 39) — confirmado
| Medida | Valor |
|---|---|
| Limiar | taxa > 0 **somente acima de 2.600 RPM** (voo 1: 749 amostras com taxa, RPM mín. 2.601,2; maior RPM com taxa 0 = 2.600,5). Limiar proposto: **0,0001** (o mínimo). O "ruído" de 0,0014 na decolagem do voo 1 não era ruído: o RPM passou de 2.600. |
| Taxa típica | ~0,0013 (2.610 RPM) a ~0,0019 (2.715 RPM) |
| **Unidade** | **fração do nível por segundo — confirmada**: trecho `cal-abuso-rpm-60s` com integral da taxa 0,1846 × queda do nível 0,1852 (razão **0,997**). No voo longo a razão foi 0,907 (o trecho inclui o período depois do weak e a aceleração). Só informativo: pela regra 0.1 revisada (2026-11-15) a taxa **não** vira % de desgaste. |
| Tempo até weak | de 81% a 10% em ~460 s de overstress contínuo (100% → 10% ≈ 9 min a ~2.650–2.700 RPM) |
| `IS WEAK:39` | liga quando o nível chega a **0,0998 (10%)**; `WEAK VALUE` cai de 1,0 para ~0,91 (perda de desempenho de ~9%). |
| Depois do weak | com o RPM abaixo de 2.600 a taxa volta a 0 e o nível para (~7,5%). |
| `IS FAILED:39` | ligou **sem overstress**, com o nível parado em 7,5%, depois de ~54 s reais a 1× + ~52 s reais a **16×** (≈ 14 min simulados depois do weak). O nível foi a 0, `WEAK VALUE` a 0, combustão 0, motor parou. Parece sorteio por tempo depois de weak (break check nativo); 1 amostra só. |
| **`ENG FAILED:1`** | **continuou 0** depois da pane; `GENERAL ENG DAMAGE PERCENT` também 0. |

**Consequência para o código:** a detecção de pane nativa atual (`SimFailureState`: ENG FAILED, ENG ON FIRE, DAMAGE PERCENT ≥ 10%…) **não pega** a pane do wear and tear. O banco não ganhou discrepância nesse voo. A F4 precisa ler `WEAR AND TEAR IS FAILED:<id>` (e `IS WEAK`) em voo, como já previsto na regra 0.1 › 3a.

## 2. Outros componentes
| ID | Componente | Resposta | Proposta |
|---|---|---|---|
| 37 | Pneu | taxa pequena só nos pousos: firme (10,5 ft/s ≈ 630 fpm) pico 0,0022; duro (11,7 ft/s) pico 0,00016. Nível não caiu de forma mensurável (voo 1 sem dano). | Observar; refazer pousos com o dano ligado. |
| 35 | Trem | sem taxa até 11,7 ft/s | Só addon (pouso duro pela telemetria). |
| 36 | Freio | sem taxa (frenagem a 31 kt, freio a ~119 °C) | Só addon. |
| 5–8 | Flaps | **sem taxa** com 114 kt e flap 1 (Vfe 85); `FLAP SPEED EXCEEDED` = 0 | Só addon (velocidade × flap). |
| 9–20 | Comandos | sem taxa a 3,1 G (abaixo do limite de 4,4 G do C152) | Inconclusivo; testar > 4,4 G. |
| 41/42 | Óleo | sem taxa (CHT máx. 231, óleo máx. 31) | Só addon. |

## 3. Desgaste natural nativo (sem overstress)
Com taxa 0, todos os componentes perdem nível devagar no tempo: ~0,0018 em 753 s (≈ 0,9%/h) na maioria; alternador, bateria, óleo, piloto automático e `GENERAL` 3–4× mais rápido. O script marca isso como "divergente" porque a razão fica 0: **não é erro de unidade**, é envelhecimento por tempo, que o addon não usa (regra 0.1 › 2). Ajuste sugerido no script: taxa 0 com queda pequena e contínua = "desgaste natural".

## 4. Pendências do portão F1.4c (C152)
- [x] Usuário aprovou (2026-11-15): limiar 39 = 0,0001; motor 39 **Ativo** (taxa só como reserva/conferência, regra 0.1 › 3); flaps, freios, trem e óleo **só addon**; pneu e comandos em Observar.
- [ ] Repetir pousos firme/duro com dano ligado (pneu 37) e uma puxada > 4,4 G (comandos).
- [ ] Pista de terra (passos 1 e 7).
- [x] ~~Fator do addon ÷ nativo~~: descartado (2026-11-15). O addon segue o real (fabricante/SB), não a velocidade do nativo. Este log serve para conferir que o addon acusa o mesmo abuso (RPM > limite) pelos próprios sensores.
