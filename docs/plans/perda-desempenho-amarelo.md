# Plano — Perda de desempenho na faixa amarela (40–20 %)

Criado em 2026-11-20 (Claude). Status: **pesquisa feita, aguardando teste no MSFS (§ 3)**.
Contexto: `docs/plans/economia-estabilidade.md` › 4.1 (peça entre 40 e 20 % perde desempenho de forma proporcional, sem pane).

Fonte: `docs/plans/sensor-research/simvars-msfs2024.csv` (coluna `gravavel`, levantada das páginas do SDK do MSFS 2024: engine, brake-and-landing-gear, miscellaneous). Nas páginas oficiais a coluna "Settable" aparece como ícone. "Gravável" no SDK não garante que o simulador não sobrescreva o valor no frame seguinte: **tudo abaixo precisa do teste do § 3**.

## 1. O que o SDK oferece

| Grupo | Caminho | Gravável? | Comentário |
|---|---|---|---|
| Motor turbina | `TURB ENG N1 LOSS`, `THRUST EFFICIENCY LOSS`, `FUEL EFFICIENCY LOSS`, `ITT COOLING EFFICIENCY LOSS` | sim | **Já implementado** (`TurbineDegradation`). Só falta ligar a curva às faixas novas. |
| Motor pistão | `PROP MAX RPM PERCENT` | sim | Pode limitar a rotação máxima. Não se sabe se é limite ou só leitura do valor atual. **Testar.** |
| Motor pistão | `RECIP ENG MANIFOLD PRESSURE` / `RECIP ENG BRAKE POWER` | sim | Provavelmente recalculados pelo modelo do motor a cada frame. **Testar** com escrita contínua. |
| Motor pistão | `RECIP ENG ALTERNATE AIR POSITION` | sim | Ar alternativo (não filtrado e mais quente) tira ~1 inHg de pressão de admissão na vida real. É uma perda leve e realista para filtro de ar e admissão. **Testar.** |
| Motor pistão | `RECIP ENG CYLINDER HEALTH`, `RECIP ENG NUM CYLINDERS FAILED`, `GENERAL ENG DAMAGE PERCENT` | **não** | Só leitura (do wear and tear nativo). |
| Ignição (pistão) | `RECIP ENG LEFT/RIGHT MAGNETO` | sim | Já usado nas panes. Desligar um magneto dá a queda de RPM real de um magneto ruim, mas **a chave pula no cockpit** (limitação já conhecida). Pode servir para o vermelho; no amarelo é forte demais. |
| Óleo | `GENERAL ENG OIL LEAKED PERCENT:1` | sim | Já escrito pelo app (vazamento). Um vazamento pequeno e lento no amarelo faz o óleo aquecer e a pressão cair. |
| Freios | `BRAKE LEFT/RIGHT POSITION` | sim | Não existe eficiência gravável (`BRAKE * TEMPERATURE EFFECT` e `BRAKE * WEIGHT REMAINING` são só leitura). A única via seria ler o pedal do piloto e regravar a posição reduzida (ex.: × 0,8), disputando com o comando dele. **Testar** se dá sem tremor. |
| Pneus | — | — | **Nenhuma** variável de pneu, aderência ou pressão é gravável (`CONTACT POINT SKIDDING FACTOR` e `GEAR SKIDDING FACTOR` são só leitura). `NOSEWHEEL MAX STEERING ANGLE` é gravável, mas é a direção, não o pneu. |
| Wear and tear nativo | `WEAR AND TEAR *` | não (só `CHECK PROGRESSION`) | O addon não consegue gravar o nível nativo. Confirma a decisão de 2026-11-15. |

## 2. Proposta (depende do teste)

| Grupo | Efeito no amarelo (proporcional de 40 → 20 %) | Se o teste falhar |
|---|---|---|
| Motor turbina | perdas já existentes, escaladas pela faixa | — |
| Motor pistão | perda de potência de até ~10 % (via `PROP MAX RPM PERCENT` ou pressão de admissão, o que funcionar) | ar alternativo aberto (perda fixa e leve) |
| Ignição | sem efeito no sim (o magneto fica para o vermelho e para a pane) | — |
| Óleo | vazamento lento proporcional (`OIL LEAKED PERCENT`) | — |
| Freios | posição do freio limitada a 100 % → ~80 % | sem efeito no sim; só a nota/aviso |
| Pneus, célula, trem, instrumentos | sem efeito no sim: o risco aparece no vermelho (pane) e no aviso do Hangar/HUD | — |

## 3. Roteiro de teste no MSFS 2024 (C152 em solo e em voo, ~30 min)

Ferramenta: **SimVar Watcher** do SDK (permite ler e gravar uma SimVar). Não precisa do app.
Para cada linha: anotar o valor lido antes, gravar, ler de novo depois de 2 s e depois de 10 s, e observar o tacômetro e o MAP.

1. **`PROP MAX RPM PERCENT:1`** — no solo, motor a 2.000 RPM com manete fixa. Gravar 90, depois 80. A RPM cai e fica? O valor volta sozinho?
2. **`RECIP ENG MANIFOLD PRESSURE:1`** (unidade psi) — mesmo cenário. Gravar 1 psi abaixo do lido. Repetir gravando a cada segundo (botão repetidas vezes) e ver se a RPM muda.
3. **`RECIP ENG BRAKE POWER:1`** — igual ao 2, gravando 80 % do valor lido.
4. **`RECIP ENG ALTERNATE AIR POSITION:1`** — gravar 1 (aberto). O MAP cai ~1 inHg? Fica? O C152 tem ar alternativo modelado? (Se não, testar no C172.)
5. **`BRAKE LEFT POSITION`** — parado com motor a 1.500 RPM, pedal esquerdo todo pressionado. Gravar 26.000 (≈ 80 % de 32.767) repetidas vezes. O avião começa a se mover? O valor volta para 32.767 a cada frame?
6. **`GENERAL ENG OIL LEAKED PERCENT:1`** — já testado antes; só confirmar que 5 % aumenta a temperatura do óleo em ~10 min de voo de cruzeiro.

### Resultados — 2026-11-20, C172SP G1000, SimVarProbe `20261120-0822-C172SP_G1000_Passengers`
- **Passo 1 (`PROP MAX RPM PERCENT`):** gravar 90 e 80 não teve efeito (RPM 2.050 e valor 75,9 % antes e depois). A variável é só a RPM atual ÷ RPM máxima (2.050 ÷ 2.700 = 75,9 %), recalculada a cada frame. Provavelmente inútil, **mas a gravação pode não ter chegado ao sim** (ver `DATA_ERROR` abaixo): repetir.
- **Passo 0 (óleo):** `GENERAL ENG OIL LEAKED PERCENT:1` = **100 com o motor sem pane**, caindo devagar com o motor ligado (100 → 99,86 em ~3,5 min) e pressão normal (~11.400 psf ≈ 79 psi). Indica que a variável é o **óleo restante** (100 = cheio), não o vazado. **Suspeita de bug:** `SimFailureActuator` (vazamento sobe de 0 a 100; limpeza grava 0) estaria esvaziando o óleo. Confirmar e corrigir à parte.
- A gravação parou às 11:27:58: passos 2–5 não ficaram registrados (repetir).
- **Gravação pelo Watcher falhou (`SimConnect : DATA_ERROR`)**: na tela, o pedido foi criado com **Is String marcado e Index 0** (`PROP MAX RPM PERCENT` sem `:1`). Para gravar: desmarcar *Is String*, **selecionar a linha já existente com `:1`** na lista da direita, digitar o valor e *Try set value*. Se ainda der `DATA_ERROR`, a variável não aceita escrita externa (anotar). Nota: a confirmação do passo 1 acima veio só do valor não ter mudado; pode ter sido esse erro, então **repetir o passo 1** também.

Anotar os resultados em `docs/plans/sensor-validation/perda-desempenho.md` (ou ditar para o Claude). Depois do teste, o Claude escreve a spec da implementação (efeito no `SimFailureActuator`/`PartFlightSession`, escalado pela faixa).
