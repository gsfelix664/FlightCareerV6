# Adendo do usuário (2026-11-15) — Elétrico e Aviônicos (entra na rodada 2 da F2.1b)

Ideia central: elétrico e aviônicos sofrem com calor, umidade, ciclos liga/desliga, picos de tensão e partidas incorretas — servem para punir checklist mal feito.

## Peças (agrupar o que não mudar a jogabilidade)
Formato: peça — gatilho — vida — falha — custo (% do valor) — hangar.
- Baterias principal/standby — ciclos de partida, descarga profunda no solo, tempo — ~1.500 h ou 2 anos — não segura carga, partida só com GPU — 0,08–0,20% — 1–2 h
- Alternadores / geradores / IDG — horas + carga alta (degelo, luzes) — ~3.000 h — GENERATOR FAILURE, corte de cargas — 0,40–0,90% — 4–12 h
- Gerador da APU; RAT (só jato/turboélice grande, feature `rat`)
- TRUs e inversores estáticos — horas + calor no compartimento — ~6.000 h — perda de barramento AC/DC — 0,25–0,55% — 3–8 h
- Chicotes, relés, contatores e disjuntores — vibração, umidade, ciclos de comutação — ~10.000 h — curto intermitente, desarmes — 0,15–0,40% — 4–16 h
- Iluminação externa (pouso/táxi, strobe, beacon, nav) — horas de uso — ~1.000 h — queima (impede voo noturno) — 0,03–0,08% — 1 h
- Telas PFD/MFD/EICAS/ECAM — horas ligadas + partida com aviônicos ligados — ~6.000 h — apagão, modo reversionário — 0,50–1,20% — 2–6 h
- Instrumentos standby/ISIS (feature `glassCockpit` = standby; analógico = principais)
- AHRS/IRS — horas + avião em movimento durante alinhamento — ~8.000 h — perda de atitude/proa — 0,45–1,00% — 3–8 h
- ADC — horas + umidade no pitot/estática — ~7.000 h — erro de velocidade/altitude/VSI — 0,35–0,80% — 2–6 h
- Rádios COM/NAV/ILS (VHF/HF) — horas + ciclos de transmissão — ~5.000 h — perda COM ou VOR/ILS — 0,20–0,50% — 2–4 h
- GPS/FMS (CDU/MCDU, GNSS) — horas ligadas + picos na partida — ~7.000 h — perda RNAV — 0,40–0,95% — 2–6 h
- Transponder/ADS-B — horas — ~6.000 h — sem identificação ATC (impede espaço controlado/RVSM) — 0,15–0,35% — 1–3 h
- Radar meteorológico / TCAS / EGPWS — horas + umidade/impacto no radome — ~6.000 h — 0,35–0,85% — 3–8 h
- Piloto automático e servos — horas engajado + turbulência severa — ~4.500 h — desengate/não segura altitude-proa — 0,30–0,75% — 4–10 h
- FDR/CVR e ELT (ELT acionado por impacto)

## Regras (só com SimVar confirmada no SDK; senão, só desgaste normal)
- Partida com aviônicos ligados: `AVIONICS MASTER SWITCH` = 1 quando `GENERAL ENG COMBUSTION` sobe 0→1 → dano imediato 1–3% nas telas, rádios e GPS/FMS (evento).
- Descarga profunda: `ELECTRICAL BATTERY VOLTAGE` abaixo de ~22 V (24 V) / ~11 V (12 V) sem gerador ou GPU → bateria desgasta 3×.
- Calor no solo: `AMBIENT TEMPERATURE` > 35 °C com aviônicos ligados no solo por muito tempo → telas, TRUs, AHRS/IRS desgastam mais.
- Piloto automático engajado em turbulência severa → servos 2× (definir "turbulência" por variação de G medida, sensor coerente).
- Carga elétrica alta (degelo, luzes) → gerador/alternador desgasta mais, se houver SimVar de carga/amperagem.

## Ideias para fases seguintes (NÃO implementar agora; registrar no plano)
1. **MEL:** falha que não impede VFR diurno (farol, ILS/VOR) deixa voar só VFR diurno (traslado até um hub mais barato); transponder ou PFD principal inoperante = restrição para missões comerciais/IFR até o reparo.
2. **Inspeção calendárica:** transponder, altímetro/ADC e ELT a cada 24 meses (custo fixo mesmo parado).
3. **Cascata de energia:** gerador falhado em voo → consumo da bateria em tempo real; quanto pior a saúde da bateria, menos minutos até apagar telas e rádios no sim.

# Adendo 2 do usuário (2026-11-15) — Hidráulico, Pneumático/Pressurização e Combustível

Ideia central: são as "artérias" — cadeias de causa e efeito (vazamento hidráulico → trem, freios, comandos; falha pneumática → sem pressurização → descida de emergência; bomba/filtro de combustível → queda de potência ou flameout).

## Hidráulico (aviões médios/grandes; nos leves, freios e atuador de trem)
- Bombas acionadas pelo motor (EDP, por motor) — horas de motor + superaquecimento — ~6.000 h — pressão baixa, perda de redundância — 0,45–1,10% — 6–16 h
- Bombas elétricas (ACMP) — uso contínuo (ex.: esquecida ligada no solo) — ~4.000 h — queima, atuação lenta — 0,30–0,75% — 4–10 h
- Reservatórios e acumuladores — ciclos + perda de nitrogênio — ~8.000 h — sem freio de emergência — 0,20–0,50% — 3–8 h
- Tubulações, mangueiras e O-rings — vibração, ciclos térmicos, ressecamento parado — ~3.500 h — vazamento contínuo — 0,15–0,40% — 4–12 h
- Filtros e válvulas seletoras — horas + contaminação — ~1.500 h — travamento; contaminação acelera 3× as bombas — 0,08–0,20% — 2–4 h
- PTU (feature `ptu`: A320/737) — uso prolongado com motor/bomba inoperante — ~5.000 h — 0,35–0,80% — 6–14 h

## Pneumático, ar-condicionado e pressurização (turboélice/jato; pistão pressurizado usa ar do turbo)
- Válvulas de sangria (motor e APU, por motor) — ciclos de potência + calor — ~4.500 h — BLEED TRIP OFF — 0,30–0,70% — 4–10 h
- Pré-resfriadores — alta potência + tempo quente + sujeira — ~6.000 h — superaquecimento do bleed — 0,35–0,85% — 6–14 h
- Dutos pneumáticos e loops de superaquecimento — vibração + expansão térmica — ~8.000 h — BLEED LEAK — 0,25–0,60% — 4–12 h
- Packs (ACM) — horas + poeira/umidade — ~5.000 h — PACK TRIP, limita o teto — 0,50–1,20% — 8–18 h
- Válvulas outflow + controlador (CPC) — ciclos de pressurização + descidas muito rápidas — ~6.000 ciclos — cabine sobe, descida para 10.000 ft — 0,30–0,75% — 4–12 h
- Válvulas de alívio (positiva/negativa) — tempo + ciclos de ΔP máximo — ~10.000 ciclos — vazamento lento — 0,15–0,35% — 2–6 h
(Já existem `pressurization.outflow_valve` e `pressurization.door_seals`: unifique.)

## Combustível
- Bombas elétricas (boost/aux) — horas ligadas + rodar a seco (tanque vazio) — ~3.500 h — sem pressão em altitude — 0,20–0,50% — 3–8 h
- Bombas mecânicas do motor (por motor) — horas + combustível contaminado — ~5.000 h — flutuação ou flameout — 0,40–0,95% — 4–12 h
- Seletoras e crossfeed — ciclos + tempo parado — ~6.000 h — travada — 0,15–0,40% — 3–8 h
- Válvulas de corte de fogo (por motor)
- Filtros e drenos — horas + abastecimento em aeroporto remoto — ~500 h — entupimento, perda de potência máxima — 0,04–0,10% — 1–2 h
- Trocador de calor combustível/óleo (FOHE, turbinas) — operação longa com OAT < −30 °C — ~7.000 h — gelo no combustível — 0,30–0,70% — 6–14 h
- Sensores de quantidade e fluxo (FQIS) — horas + umidade — ~5.000 h — leitura errada — 0,12–0,30% — 2–6 h
(Já existem `fuel.engine_pump`, `fuel.aux_pump`, `fuel.selector_valve`, `fuel.filter_gascolator`, `fuel.fcu`: unifique.)

## Regras possíveis (só com SimVar confirmada no SDK)
- Bomba hidráulica elétrica ligada no solo por muito tempo (chave da bomba + no solo); pressão hidráulica baixa (se houver SimVar de pressão por sistema).
- Descidas muito rápidas da cabine (razão de variação de `PRESSURIZATION CABIN ALTITUDE`) → outflow/CPC; ΔP perto do máximo → válvulas de alívio.
- Bomba de combustível ligada com tanque vazio (chave da bomba + quantidade no tanque ≈ 0) → bomba elétrica.
- OAT < −30 °C por muito tempo (turbina) → FOHE.
- Abastecimento em aeroporto remoto → filtros (fica para a F2.3/economia: depende do aeroporto, não de sensor).

## Ideia para fases seguintes (registrar no plano)
4. **Cadeias de causa e efeito:** vazamento hidráulico afeta trem, freios e comandos; filtro de hidráulico contaminado acelera 3× as bombas; falha pneumática impede pressurizar e obriga descida; bomba/filtro de combustível causa perda de potência ou flameout.

# Adendo 3 do usuário (2026-11-15) — Grupo motopropulsor (motores, hélices, APU)

Separar peças comuns (acessórios) das específicas de cada tipo de motor. Unificar com o que já existe no catálogo (cilindros, virabrequim, válvulas, carburador, injetores, turbo, partida, óleo, magnetos, velas, compressor, câmara, palhetas, ignitores, RGB, fan, reversor, FCU, hélice, governador, APU).

## Peças novas ou a ajustar
- **Comuns:** sistema de lubrificação (bombas, filtros, radiador, linhas); partida / partida-gerador / partida pneumática (ATS em jato); **coxins e berço do motor** (novo, por motor); **carenagem/nacele** (nova, por motor) e reversores.
- **Pistão:** cilindros + pistões + anéis + válvulas; virabrequim + bielas + comando; magnetos + cabos + velas; carburador ou servo de injeção + bicos; turbo + **wastegate**.
- **Turbina:** seção fria (fan, LPC, HPC) — vulnerável a FOD, areia, bird strike; câmara + **bicos injetores**; seção quente (HPT/LPT, **NGV**); **FADEC**/FCU/HMU; RGB (turboélice); ignitores + **excitadores**.
- **Hélice e APU:** pás + cubo; governador + **passo/embandeiramento**; **degelo da hélice** (botas/slip rings, feature `propDeIce`); APU (turbina, gerador, caixa de acessórios).

## Vida, gatilho e falha (do usuário)
- Bloco pistão (virabrequim/bielas) — horas + over-RPM + óleo baixo — ~2.000 h (TBO) — perda severa de potência, vibração, travamento
- Cilindros e válvulas — CHT alta + choque térmico + mistura muito pobre — ~1.200 h — perda de compressão (−15 a −25% de potência), consumo de óleo
- Magnetos, cabos e velas — horas + operação rica no solo (carbonização) — ~500 h — queda de RPM no teste de magnetos, falha de ignição
- Seção fria (fan/compressores) — horas + pista de terra/areia + bird strike — ~8.000 h — compressor stall, consumo maior
- Seção quente (HPT/LPT) — ciclos térmicos + ITT/EGT alta + hot start — ~6.000 h — EGT alta constante, perda de empuxo, falha catastrófica
- RGB (turboélice) — horas + over-torque + troca brusca de manete — ~5.000 h — limalha (chip detector), perda de tração
- FADEC/FCU/injetores — horas + combustível contaminado — ~5.000 h — flutuação de RPM/N1, flameout
- Lubrificação (bombas/cooler) — horas + partida com óleo frio em alta rotação — ~3.000 h — pressão baixa ou superaquecimento
- Partida / partida-gerador — ciclos de partida + tentativas longas (> 30 s) — ~2.500 ciclos — não gira o motor (NO-GO)
- Pás e cubo da hélice — horas + pedras/terra (nicks) — ~2.000 h (TBO) — vibração, perda de tração
- Governador — ciclos de variação de passo + óleo contaminado — ~2.500 h — prop overspeed ou falha ao embandeirar
- APU — ciclos de partida + horas no solo — ~4.000 h — não parte, perde bleed ou gerador

## Regras possíveis (só com SimVar confirmada)
- **Potência alta com óleo frio:** `GENERAL ENG OIL TEMPERATURE` abaixo do mínimo + potência de decolagem logo após a partida → 3× em cilindros/anéis (pistão) ou rolamentos (turbina) nos primeiros 5 min.
- **Partida longa:** motor de arranque > 30 s contínuos → partida (já existe `engine_starter_duty_cycle`: ajustar).
- **Mistura rica no solo** (pistão, tempo em marcha lenta com mistura cheia) → velas (carbonização).
- **Troca brusca de manete** (turboélice, variação rápida de torque) → RGB.
- **Pista de terra/areia** (tipo de superfície no contexto + motor em potência no solo) → seção fria, pás da hélice (nicks).
- **Hot start:** ITT/EGT acima do limite de partida durante a partida (turbina) → seção quente.

## Ideias para fases seguintes (registrar no plano)
5. **TBO comercial vs. privado:** depois do TBO o jogador pode voar privado (chance crescente de falha), mas missões comerciais/fretamento exigem motor dentro do TBO.
6. **Top overhaul vs. major overhaul:** o top troca só cilindros e válvulas (bem mais barato) e recupera parte da confiabilidade; o major é a revisão geral do bloco.
7. **Vazamento de óleo em cascata:** lubrificação ou cilindros abaixo de 30% → injetar aos poucos `GENERAL ENG OIL LEAKED PERCENT` (já gravável pelo addon via `SetOilLeakedPercent`): pressão cai e temperatura sobe nos instrumentos do avião, forçando reduzir potência ou cortar o motor antes de fundir (perda total do motor = custo cheio).
