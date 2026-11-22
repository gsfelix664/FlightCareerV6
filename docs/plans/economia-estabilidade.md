# Plano — Estabilidade da economia da carreira (análise da ECON-STAB)

Criado em 2026-11-19 (Claude). Status: **regras de manutenção aplicadas (§ 4.1) e medidas (§ 4.2); aguardando decisões 4 e 5 (pagamento e dificuldade)**.
Fonte: `FlightCareer.Replay career-stab --days 720 --bots 3 --difficulty all --policy all --fast` (27 carreiras, 45 min), com a calibração do usuário (real ÷ 4), preço por classe, reparo por troca de peça, calendário de carreira e cabine já na base. Bots começam como o jogador (saldo da dificuldade + C152), voam ~2,5 h/dia, compram o próximo avião da escada A→J e vendem o anterior.

## 1. Resultado (mediana de 3 bots por grupo)

| Dificuldade / estilo | Dia 30 | Dia 180 | Dia 720 |
|---|---|---|---|
| **Médio, agressivo** (adia manutenção, compra logo) | $179 mil, C152 | $2,9 mi, classe F (1ª compra ~dia 75) | **$15,8 mi**, classe G — 0 falências |
| **Médio, cauteloso** (manutenção em dia, reserva) | $176 mil | $2,0 mi, classe E | **3/3 falidos**, classe F |
| **Médio, equilibrado** | $170 mil | 2/3 falidos | 3/3 falidos |
| **Difícil, agressivo** | $98 mil | $2,8 mi, classe F | **$15,9 mi**, classe G — 0 falências |
| **Difícil, cauteloso / equilibrado** | $94 mil / $74 mil | $1,2 mi / 2 de 3 falidos | **3/3 falidos** |
| **Fácil** (todos) | $1 bi | $1 bi, classe F | $0,9 bi, classe J (avião de linha no 2º ano) |

Outros números: receita/h C152 ~$1.100 → turbohélice F/G $6.000–11.000; margem operacional 60–95 % (meta do usuário 35–55 %); quem segue a manutenção troca **85–100 peças / 100 h**, o agressivo **~9**; panes do agressivo ~0,5–1,5 / 100 h.

## 2. Diagnóstico

1. **Fazer manutenção certa é punido, adiar é premiado.** O agressivo enriquece; quem segue a manutenção quebra. Causas:
   - **Limite do reparo (REPARO-PECAS) alto demais:** troca toda peça abaixo de 80 % de saúde. Como o desgaste natural vai de 100 % a ~30 % ao longo da vida, **80 % = só ~29 % da vida gasta**: o jogador troca peças com 70 % de vida pela frente, ~3,5× mais que o necessário.
   - **Negligência quase não tem consequência:** o risco de pane só começa abaixo de 20 % de saúde (~1,14× a vida); peça com vida vencida mas saúde 25–30 % não dá pane. O agressivo só paga a inspeção anual obrigatória e troca o que quebra.
2. **Depois do upgrade, os bots voam pouco** (0,6–1 h/dia no 2º ano, contra 2,5 no começo) e o **seguro come o caixa**: o seguro é **1 % do valor do avião por mês (12 %/ano)** — um turbohélice de $4 mi paga $40 mil/mês voando ou não. Seguro real de casco em aviação geral costuma ficar em poucos % ao ano (valor de referência, não conferido). Falta investigar se a falta de voo após o upgrade é do jogo (pouca missão compatível com a classe/categoria nova perto) ou do simulador.
3. **Pagamento alto para o custo:** margens de 60–95 %. Com a meta de payback do usuário (C152 em 40–60 h, turbohélice em 100–150 h), o patrimônio **dobra a cada ~50 dias de jogo** — é por construção "ficar rico rápido". As duas metas (payback curto × não enriquecer rápido) são incompatíveis; é preciso escolher uma curva de progressão.
4. **Dificuldade só muda o saldo inicial:** Médio e Difícil convergem em ~2 meses (mesmo resultado no dia 180/720); Fácil com $1 bi é modo livre.
5. A inflação do combustível (×2 em 10 anos) não pesou em 2 anos de jogo.

## 3. Proposta (para decidir)

| # | Mudança | Efeito esperado |
|---|---|---|
| A | Reparo troca só o que a inspeção marca: **vida ≥ 90 %** (h/ciclos/meses), **saúde < 40 %** ou quebrada (o mesmo critério da exclamação amarela/vermelha). | Cauteloso deixa de trocar peça "nova"; custo de manutenção cai ~3×. |
| B | **Risco de pane também pela vida vencida**: acima de 100 % da vida, λ cresce com o excesso (ex.: λ = 0,02 × 4^((h_vida − 1)/0,25) por hora). Inspeção anual vencida já interdita; TBO vencido também deve interditar (confirmar). | Adiar manutenção passa a ter risco real. |
| C | **Seguro**: de 1 %/mês para ~2 %/ano (≈ 0,17 %/mês), mantendo a franquia; avião parado sem voar no mês paga 50 %. | Fim da falência por custo fixo depois do upgrade. |
| D | **Curva de progressão** como meta (no lugar do payback curto): Médio — 1º upgrade em ~60–90 dias de jogo, turbohélice (F) em ~1 ano, jato/linha só no 3º ano; Difícil ~1,5× mais lento; patrimônio do agressivo ≤ 20× o inicial no 1º ano. Ajusta-se a taxa base e a tarifa por milha das missões por classe até a ECON-STAB cair nessa curva. | Progressão controlada, sem enriquecer rápido. |
| E | **Dificuldade mexe na economia**, não só no saldo: Fácil $1 mi (não $1 bi), pagamento ×1,25, custos ×0,8; Médio ×1; Difícil pagamento ×0,85, custos ×1,15, panes ×1,25. | As três dificuldades viram curvas diferentes. |
| F | Investigar a queda de voo depois do upgrade (missões compatíveis por classe/categoria e alcance do reposicionamento). | Separar defeito do simulador de falta de missão no jogo. |

Ordem sugerida: A + B + C (regras) → rodar ECON-STAB → F → D + E (pagamento e dificuldade) → ECON-STAB de novo até a curva de D.

## 4. Decisões do usuário
1. Aprova A (critério de troca = critério da inspeção)?
2. Aprova B (pane por vida vencida) e TBO vencido interditando?
3. Seguro (C): taxa anual e desconto de avião parado.
4. Curva de progressão (D): os marcos do Médio e quanto mais lento é o Difícil.
5. Dificuldade (E): saldo do Fácil e multiplicadores.

## 4.1 Decisões do usuário (2026-11-20) — manutenção por faixas (substitui A e B)
- **Faixas de saúde da peça** (walkaround, inspeção e revisão): verde 100–40 %, amarelo 40–20 % (perda de desempenho proporcional, sem pane), vermelho 20–0 % (sorteio de pane crescendo até 0 %; curva atual do `FailureEngine`, λ = 0,02 · 100^((20−h)/20) por hora, λ = 2/h em 0 %).
- **Desgaste acelerado pela saúde, não pelo vencimento:** abaixo de 40 % o desgaste natural é multiplicado por uma curva suave (ex.: 1 + 1 × ((40 − h)/40)², 2× em 0 %; máximo a calibrar). O 3× por manutenção vencida não volta.
- **Visibilidade:** walkaround grátis mostra a saúde **exata** das peças visíveis; inspeção paga e revisões mostram a exata das demais do grupo; peça não vista mostra o último valor conhecido e há quantas horas.
- **Reparo:** sai o limite automático de 80 %. Revisões trocam o conjunto delas como na vida real (óleo: óleo e filtro; ignição: velas e magnetos; TBO: motor) e revelam as demais; pneus e freios não têm revisão. Troca individual a preço cheio, por escolha do piloto.
- **Vencimentos:** óleo, ignição e TBO vencidos **não interditam** (o risco vem da saúde); inspeção anual vencida continua interditando (só inspeciona, não troca).
- **Seguro:** pago por mês, ~2–3 %/ano do valor do avião (calibrar na ECON-STAB); cobre revisão vencida; **não cobre** pane de peça que a última inspeção anual achou em vermelho (< 20 %) ou quebrada e que não foi trocada.
- Perda de desempenho no amarelo depende do que o SDK permite atuar (motor a pistão, pneu, freio): pesquisa e teste no MSFS à parte.

## 4.2 Rodada com as regras de faixas (2026-11-21, após MANUT-FAIXAS-A)
Mesmo comando (`--days 720 --bots 3 --seed 1 --difficulty all --policy all --fast`, 22 min). Relatório completo: `docs/plans/econ-stab/2026-11-21-faixas.md`.

| Médio, mediana | Dia 180 | Dia 720 | Panes/100 h | Peças/100 h |
|---|---|---|---|---|
| Agressivo | $3,0 mi, classe F | **$19,8 mi**, classe H | 1,0 | 9 |
| Cauteloso | $2,6 mi, classe F | **$19,0 mi**, classe H | 0,0 | 49 |
| Equilibrado | $2,9 mi, classe F | **$19,1 mi**, classe H | 2,3 | 47 |

1. **Fim da falência:** 0 falidos em todas as 27 carreiras (antes: cauteloso e equilibrado 3/3 falidos no Médio e no Difícil). O seguro caiu para ~$160/h de ~$1.300/h de custo.
2. **Manutenção certa deixou de ser punida**, mas também quase não pesa: troca de peças + manutenção = ~$200–500/h contra receita ~$13.000/h. O cauteloso tem 0 panes e o agressivo 1–2 por 100 h; a diferença de patrimônio é de ~4 %.
3. **A utilização não cai mais depois do upgrade** (~2,6 h/dia nos 2 anos): a queda da rodada anterior vinha das carreiras quebradas pelo custo fixo; o item F fica sem objeto.
4. **O problema que sobra é o pagamento:** margem ~90 % no Médio/Difícil; patrimônio ~15× no dia 180 e ~100× em 2 anos. Médio e Difícil continuam iguais depois de ~3 meses.
5. **Calibrar `lowHealthWearMaxMultiplier` agora não vale a pena:** com margem de 90 % nenhum custo de manutenção muda o resultado. Rever depois de D + E. O simulador também não modela perda total (o risco real do agressivo).
6. Fácil (J no 2º ano) é modo livre; o cauteloso lá gasta ~$5.000/h em manutenção geral nos jatos (conferir o preço de revisão/retífica da classe J junto com D).

**Próximo:** decisões 4 (curva D) e 5 (dificuldade E) do § 4 → recalibrar `MissionGenerator.CalculateReward` por classe → ECON-STAB de novo.

## 4.3 Decisões do usuário (2026-11-21) — pagamento e dificuldade
- **D aprovada como proposta:** Médio — 1º upgrade em ~60–90 dias de jogo, turboélice (classe F) em ~1 ano, jato/linha (H, I, J) só no 3º ano; patrimônio do agressivo ≤ 20× o inicial no 1º ano. Difícil ~1,5× mais lento.
- **E aprovada como proposta:** Fácil $1 mi, pagamento ×1,25, custos ×0,8; Médio ×1; Difícil pagamento ×0,85, custos ×1,15, panes ×1,25.
- Execução: ECON-DIFF (Gemini) cria os botões sem mudar o Médio (`mission_pay.json` com `scale` por família de missão + `DifficultyEconomy`); depois o revisor calibra as `scale` com a ECON-STAB até a curva de D.

## 5. Como reproduzir
```bash
dotnet run --project src/FlightCareer.Replay -- career-stab --days 720 --bots 3 --seed 1 --difficulty all --policy all --fast
```
(~45 min; relatório em `%LocalAppData%/FlightCareer/career-sim/`.)

## 4.4 Calibração do pagamento (2026-11-22, revisor) — `mission_pay.json`
Medição por classe × tipo de missão (1 ano, Médio, escalas 1,0): o C152 fatura $1.019/h em instrução, $2.226/h em carga especializada e $3.882/h em táxi aéreo (~40 % da receita dele); da classe B em diante quase tudo é **táxi aéreo/charter (passageiros)**, $8–15 mil/h — é isso que acelerava a carreira depois da 1ª troca.

Escalas finais: `instrucao_basica`/`instrucao` **1,5**; `medevac`, `aog`, `agro_bush`, `agro_municipal`, `carga_expressa`, `fallback` **0,7**; `taxi_aereo`, `regional`, `linha` **0,28**.

| Rodada (3 anos, 3 bots/estilo) | 1ª troca (Médio) | Dia 365 (Médio) | Dia 730 (Médio) | Difícil |
|---|---|---|---|---|
| Escalas 1,0 (antes) | 71–88 | classe G, $8,3 mi agressivo | G | 1ª troca 95–151, G no 1º ano |
| **Final** | **79–96** | **F** (turboélice), $4,5–4,8 mi | **H** (jato, início do 3º ano) | 1ª troca 105–137, F em 1 ano, G no 3º, sem jato em 3 anos |

- Meta D cumprida nos marcos de avião (1ª troca no limite de 90 dias para o cauteloso).
- **Conflito na meta D:** "patrimônio do agressivo ≤ 20× no 1º ano" ($2 mi) é incompatível com "turboélice em ~1 ano": o próprio C208 vale mais de $2 mi. Priorizados os marcos de avião; o patrimônio fica ~45× no dia 365. **Decisão do usuário pendente.**
- Margem ainda ~88–93 % no 1º ano (custo pequeno perto da receita); um bot agressivo faliu no 2º ano nas rodadas finais (risco real de quem compra no limite).
- Simulador: marcos 30/60/90/180/365/540/730/1095.
- **Revalidar** depois da FASES-1 (a promoção passa a ser por nível/horas/homologação e o XP mudou — XP-NOVO —, o que pode mudar quando os bots trocam de avião).

## 4.5 Calibração com XP, fases e contratos das fases 3–7 (2026-11-22, revisor)
Depois de XP-NOVO, FASES-1/2A/2B/2C a progressão ficou lenta (t5/t6: 1ª compra no dia 112–206, nenhum jato em 3 anos). Escalas finais em `mission_pay.json`: `instrucao_basica`/`instrucao` **2,0**; `taxi_aereo`/`regional`/`linha`/`carga_global` **0,35**; demais como na § 4.4 (famílias das fases 1–2 a 1,0; agro/medevac/aog/carga 0,7).

Rodada t8 (`career-stab`, 3 anos, 3 bots por política, `--fast`):

| Dificuldade | 1ª compra (dia) | Dia 365 | Dia 730 | Dia 1095 | Falências |
|---|---|---|---|---|---|
| Médio (3 políticas) | 55–113 | classe F, ~$3,5–3,8 mi | G, ~$8,1–8,7 mi | G, ~$16,3–16,5 mi | 0 |
| Difícil (3 políticas) | 128–214 | E, ~$1,8–2,2 mi | F, ~$4,3–4,4 mi | H, ~$9,9–10,3 mi | 0 |

- A quebra dos agressivos da t7 (2 de 3 no Médio) não se repetiu na t8.
- **Pendências para o usuário:** (1) patrimônio no 3º ano ≈ 16 mi no Médio, acima da meta ≤ 20× do valor da frota prevista quando a meta foi escrita; reduzir exige baixar as escalas de linha/regional, o que atrasa o turboélice de ~1 ano. (2) No Médio os bots ficam na classe G do dia 540 ao 1095 (o Difícil chega a H no dia 1095): provável trava de fase 6 (nível/horas/homologação) nas políticas dos bots — investigar no simulador antes de mexer em escala.
