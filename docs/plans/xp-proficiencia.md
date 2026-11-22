# Plano — XP por proficiência e dificuldade

Criado em 2026-11-21 (Claude), depois do voo de SNEJ (C152, pista de terra a 5.130 ft entre montanhas, 17,7 min, 23 XP). Status: **aprovado pelo usuário em 2026-11-21 (§ 5.1); implementação pendente**.

## 1. Como é hoje (`FlightService.ProcessFlightFinishAsync`, ~linha 258)
```
base  = minutos de voo (inclui táxi) + 0,5 × milhas percorridas
bônus = base × 0,2 × tier do destino        (tier 1 hub = +20 % … tier 4 remoto = +80 %)
XP    = (base + bônus) × nota de qualidade (0–100 %)   −10 % com partida automática
```
- O XP **não define nível**: o título (Student/Private/Commercial/ATP) vem só das horas (`Pilot.UpdateRank`). O XP é um dos três requisitos para subir de **categoria** (`PilotCategory.GetRequiredXP`): 2.500 / 12.000 / 45.000 / 150.000, junto com 40 / 150 / 500 / 1.500 h e o custo.
- Problemas: (1) o quadro de missões mostra outro número (`Mission.ExperienceGain` = distância × 0,5 × urgência; num voo local ≈ 5 XP); (2) nada premia a dificuldade real (pista de terra, curta, aeródromo alto, vento cruzado, noite, tempo ruim); (3) o táxi conta como voo; (4) o tier premia o aeroporto pelo porte, não pela dificuldade; (5) XP/h ≈ 90–110 voando bem, quase igual à exigência (62–100 XP/h): o XP repete as horas.

## 2. Objetivo
O XP mede **proficiência**: voar bem e em condições difíceis sobe mais rápido. Horas continuam medindo experiência bruta. Para subir de categoria, as horas não bastam: é preciso voar bem e enfrentar condições mais exigentes conforme a categoria.

## 3. Proposta
```
XP = minutos no ar × 1,0 × fator de qualidade × (1 + soma dos bônus de dificuldade) − penalidades
```
- **Minutos no ar** (decolagem → toque final), não o tempo de bloco: táxi não é proficiência. Voo de instrução local segue o mesmo cálculo.
- **Fator de qualidade** (0,5 a 1,2), a partir da nota atual: nota 100 → 1,2; 80 → 1,0; 60 → 0,8; ≤ 30 → 0,5. Toque de 60–250 fpm soma +0,1 (pouso "manteiga").
- **Bônus de dificuldade** (somados, teto +100 %), avaliados na decolagem e no pouso:

| Condição | Bônus | Dado |
|---|---|---|
| Pista não pavimentada (terra, grama, cascalho) | +15 % cada operação | `Runways.Surface` |
| Pista curta: < 2.500 ft (pistão) / < 4.500 ft (turbina) | +15 % | `Runways.LengthFt` |
| Aeródromo alto: > 3.000 ft +10 %; > 6.000 ft +20 % | até +20 % | `Airports.ElevationFt` |
| Vento cruzado no pouso > 8 kt +10 %; > 15 kt +20 % | até +20 % | `VELOCITY BODY X` no toque (já medido) / vento ambiente |
| Pouso noturno | +15 % | conferir no SDK (`E:LOCAL TIME` / `TIME OF DAY`) |
| Visibilidade baixa / IMC | +20 % | conferir no SDK (`AMBIENT VISIBILITY`) |

- **Penalidades**: pouso duro > 500 fpm −20 %; excedência com dano (faixa de alerta) −5 % cada, até −25 %; partida automática −10 % (já existe); voo invalidado = 0 XP.
- **Quadro de missões**: mostra "≈ XP" com a mesma fórmula, estimando os minutos no ar pela distância e velocidade de cruzeiro e com fator de qualidade 1,0, já com os bônus da pista de destino.

### Calibração
Um voo normal e bem feito rende ~60 XP/h; um voo difícil e bem feito, 90–120 XP/h. Com as exigências atuais (62,5 / 80 / 90 / 100 XP por hora exigida), a categoria 2 sai voando bem, e as categorias 3–5 exigem voar bem **e** enfrentar condições difíceis. Rodar a ECON-STAB depois para conferir que os bots não travam na categoria (bots usam nota fixa; o simulador precisa estimar os bônus pelo aeroporto).

### Exemplo: o voo de SNEJ de 2026-11-21
7,4 min no ar (de 17,7 min de bloco) × fator 0,64 (nota 64 %) × (1 + 0,15 terra na decolagem + 0,15 terra no pouso + 0,10 elevação 5.130 ft) × 0,8 (pouso duro, 645 fpm) ≈ **5 XP**. Com um pouso de 200 fpm (nota ~90 %, fator ~1,2): 7,4 × 1,2 × 1,4 ≈ **12 XP**. Hoje: 23 XP, porque conta o táxi. O mesmo voo em asfalto ao nível do mar, pouso bom: 7,4 × 1,2 ≈ 9 XP. Ou seja, a proposta **reduz** o XP de voos curtos e locais e aumenta o peso da dificuldade; para não ficar mais lento no geral, a taxa base pode subir (ex.: 1,5 XP/min) — decisão 3.

## 4. Achados do mesmo voo (para corrigir junto ou antes)
1. **Peças de vida em ciclos não envelhecem no jogo.** O `PartWearEngine` só tem desgaste natural por horas; pneus (75 ciclos), freios (150) e rodas (350) só se gastam por eventos, e `CyclesSinceReplacement` só sobe nas peças da regra `landing_cycle_structural_stress`. O simulador de carreira (`CareerProgressionSimulator` ~linha 1024) gasta 70 %/vida por pouso e conta os ciclos: a ECON-STAB fica mais cara que o jogo. Corrigir no jogo: a cada pouso (sem bounce), +1 ciclo e desgaste natural de 70/vida (× multiplicador da faixa amarela) em toda peça com `lifeCycles`.
2. **Avião usado do mercado com dados incoerentes**: PP-UZJ veio com TBO restante de 1.791 h (TBO do pistão é 500 h) e pneus/freios/trem com ~50 % de saúde e 0 h/0 ciclos desde a troca. Gerador do mercado (`MarketSimulationService`) precisa limitar o TBO à vida atual e gerar horas/ciclos coerentes com a saúde.
3. **Pagamento não premia pista difícil**: o bônus bush (+25 %) só vale para destino tier 4; SNEJ (terra, 5.130 ft, montanha) é tier 3. Avaliar junto com a calibração do pagamento (ECON-DIFF/D): usar `Runways.Surface`/comprimento em vez do tier.

## 5. Decisões do usuário
1. Aprova contar só os minutos no ar (sem táxi)?
2. Aprova a tabela de bônus e penalidades (valores e teto de +100 %)?
3. Taxa base (1,0 ou 1,5 XP por minuto no ar) e exigências de XP por categoria (2.500 / 12.000 / 45.000 / 150.000): manter ou ajustar?
4. Os achados 1–3 entram antes do XP?

## 5.1 Decisões do usuário (2026-11-21)
- Plano aprovado como proposto: XP por minuto no ar (sem táxi), fator de qualidade 0,5–1,2, bônus de dificuldade e penalidades da § 3, teto de +100 %, e o quadro de missões com a mesma estimativa.
- Taxa base (1,0 ou 1,5 XP/min) e exigências por categoria: o revisor calibra na ECON-STAB para que os bots de política cautelosa/equilibrada sigam a curva de progressão de economia-estabilidade.md › 4.3 (D), mantendo 2.500 / 12.000 / 45.000 / 150.000 se a curva fechar.
- Achados § 4: o 1 (ciclos de pouso) foi corrigido em 2026-11-21 (`PartWearEngine.ApplyLandingCycle`); 2 (avião usado incoerente) e 3 (bônus de pista difícil no pagamento) entram antes ou junto da implementação do XP.

## 5.2 Pedido do usuário (2026-11-22) — critérios da nota por tipo de missão (implementação futura)
A nota de qualidade (que vira o fator 0,5–1,2 do XP) passa a avaliar **o que importa em cada tipo de voo**, em vez de uma regra única. Ponto de partida no código: `Domain/Services/FlightQualityService` já separa carga × passageiro (na carga só contam G e solavanco) e tem as regras injetáveis em `FlightQualityRules`; a ideia é ter um conjunto de regras e pesos **por família de missão**, usando as mesmas famílias/prefixos de `mission_pay.json` (`instrucao_basica`, `instrucao`, `taxi_aereo`, `regional`, `linha`, `carga_expressa`, `aog`, `medevac`, `agro_bush`, `agro_municipal`).

| Tipo de voo | O que mais pesa na nota | O que pesa pouco ou nada |
|---|---|---|
| Instrução (3.4, 3.4.1) | pousos (razão de descida no toque, quiques), **toque-e-arremetida (TGL)**, força G, ângulos (bank/pitch) e velocidades dentro do permitido (Vfe, Vno, Vne, excedências de motor) | tempo de chegada |
| Passageiros / táxi aéreo / regional / linha | conforto: força G, solavanco, ângulo e razão de subida/descida, bank, pouso suave | — |
| Carga / carga expressa / AOG | força G e solavanco (carga solta), limites estruturais; pontualidade na expressa e no AOG | bank e pitch (já ignorados hoje) |
| Médico (medevac) | **velocidade/tempo de chegada** (pontualidade contra o tempo estimado), sem exceder limites, conforto moderado (paciente a bordo) | — |
| Agro / bush | precisão em pista curta/terra, controle no toque, limites | conforto |

O que ainda não é medido e precisa de sensor/regra nova: contagem de TGL (toque sem parada seguido de decolagem), aproximação estabilizada, tempo de chegada contra o estimado da missão (o prazo `ExpirationTime` existe, o tempo estimado de voo não). Calibrar os pesos com voos gravados (replay) antes de ligar. **Decisão do usuário (2026-11-22): os critérios por tipo só afetam o XP; o pagamento da missão nunca depende da nota nem da pontualidade.** Pendente: os pesos de cada tipo.
