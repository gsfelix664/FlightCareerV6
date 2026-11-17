# Peças do Hangar — lista para revisão

Gerado em 2026-11-17 de `parts_catalog.json` (148 peças em 22 grupos) + 6 lâmpadas da LUZES-F2 (em implementação). Valores marcados como estimativa no catálogo (`lifeSource`).

Legenda: **Vida** = horas / ciclos / meses (o que houver); **Preço** em $ (× = escala com o preço do avião); **MO** = mão de obra (h); **Parada** = downtime (h); **NO-GO** = quebrada bloqueia o voo; **WA** = visível no walkaround; **/mot** = uma por motor.

## Motor (18)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `engine.cylinders` | Cilindros e pistões /mot | todas de piston | 1800 h | 4800 × | 16 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.crankshaft` | Virabrequim e mancais /mot | todas de piston | 2000 h | 6500 × | 24 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.valves` | Válvulas de admissão e escape /mot | todas de piston | 1000 h | 1600 × | 10 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.carburetor` | Carburador aeronáutico /mot | todas de piston, carbureted | 1000 h | 1400 × | 4 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.fuel_injectors` | Bicos injetores e distribuidor /mot | todas de piston, fuelInjected | 1200 h | 1800 × | 5 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.turbocharger` | Turbocompressor, wastegate e controlador de pressão /mot | todas de piston, turbocharged | 1000 h | 4200 × | 8 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.starter` | Motor de partida elétrico /mot | todas de piston | 1500 h | 850 | 2.5 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.coolant_system` | Radiador e arrefecimento líquido /mot | todas de piston, diesel | 1200 h | 1900 × | 6 | 8–36 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.compressor` | Compressor axial / centrífugo da turbina /mot | qualquer de turboprop, jet | 3500 h | 32000 × | 20 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.reduction_gearbox` | Caixa de redução da hélice (RGB) /mot | todas de turboprop | 3500 h | 18000 × | 14 | 8–36 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.fan` | Fan frontal do turbofan /mot | todas de jet | 4000 h · 3000 cic | 45000 × | 16 | 8–36 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.thrust_reverser` | Conjunto do reversor de empuxo /mot | todas de jet | 3000 h · 2500 cic | 22000 × | 10 | 8–36 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.mounts_trusses` | Coxins e berço estrutural de fixação do motor /mot | todas | 10000 h | 8000 × | 14 | 8–24 | não | não | Vibração excessiva do motor e desalinhamento estrutural |
| `engine.cowling_nacelle` | Carenagem e nacele aerodinâmica do motor /mot | todas | 12000 h | 9000 × | 8 | 6–18 | não | não | Trinca em painel de carenagem / arrasto parasita |
| `engine.air_intake_filter` | Filtro e duto de admissão de ar do motor (inlet screen) /mot | todas | 1000 h | 600 | 2 | 1–3 | não | não | Obstrução do fluxo de admissão / perda de potência |
| `engine.pneumatic_starter` | Motor de partida pneumático (ATS - Air Turbine Starter) /mot | todas de jet | 3000 cic | 16000 × | 6 | 3–8 | não | não | Falha de partida pneumática / recusa de acionamento |
| `engine.fadec_eec` | FADEC / Módulo eletrônico de controle do motor (EEC) /mot | todas de fadec | 8000 h | 24000 × | 6 | 4–12 | não | não | Degradação para modo manual / flutuação de empuxo |
| `engine.apu_core` | Módulo gerador de gás e turbina da APU | todas de apu | 4000 h | 35000 × | 16 | 8–24 | não | não | Recusa de partida da APU / perda de potência pneumática auxiliar |

## Óleo (7)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `oil.lubricant` | Óleo lubrificante do motor (pistão) /mot | todas de piston | 50 h · 12 m | 180 | 1.5 | 1–4 | sim | sim | Vazamento ou perda de lubrificação do motor |
| `oil.turbine_oil` | Óleo lubrificante sintético da turbina /mot | qualquer de turboprop, jet | 800 h · 12 m | 350 | 1 | 1–4 | sim | sim | Vazamento ou perda de lubrificação do motor |
| `oil.filter` | Filtro de óleo lubrificante (pistão) /mot | todas de piston | 50 h | 45 | 0.5 | 1–4 | sim | não | Vazamento ou perda de lubrificação do motor |
| `oil.turbine_filter` | Filtro de óleo lubrificante da turbina /mot | qualquer de turboprop, jet | 600 h | 220 | 1 | 1–4 | sim | não | Vazamento ou perda de lubrificação do motor |
| `oil.cooler` | Radiador de óleo /mot | qualquer de piston, turboprop | 2000 h | 1200 × | 3 | 1–4 | não | sim | Vazamento ou perda de lubrificação do motor |
| `oil.pump` | Bomba mecânica de óleo (pistão) /mot | todas de piston | 1800 h | 2200 × | 8 | 1–4 | sim | não | Vazamento ou perda de lubrificação do motor |
| `oil.turbine_pump` | Bomba e módulo de lubrificação da turbina /mot | qualquer de turboprop, jet | 3500 h | 5500 × | 8 | 1–4 | sim | não | Vazamento ou perda de lubrificação do motor |

## Ignição (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `ignition.magneto_left` | Magneto esquerdo /mot | todas de piston; exceto diesel | 500 h | 1200 | 2.5 | 2–6 | sim | não | Falha de ignição / funcionamento irregular |
| `ignition.magneto_right` | Magneto direito /mot | todas de piston; exceto diesel | 500 h | 1200 | 2.5 | 2–6 | sim | não | Falha de ignição / funcionamento irregular |
| `ignition.spark_plugs` | Velas de ignição (jogo) /mot | todas de piston; exceto diesel | 400 h | 380 | 2 | 2–6 | não | não | Falha de ignição / funcionamento irregular |
| `ignition.glow_plugs` | Velas aquecedoras (glow plugs) /mot | todas de piston, diesel | 600 h | 450 | 2 | 2–6 | sim | não | Falha de ignição / funcionamento irregular |

## Seção quente (6)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `hotsection.combustor` | Câmara de combustão /mot | qualquer de turboprop, jet | 3500 h | 28000 × | 20 | 24–72 | sim | não | Degradação térmica severa / perda de empuxo |
| `hotsection.turbine_blades` | Palhetas da turbina /mot | qualquer de turboprop, jet | 3000 h | 35000 × | 25 | 24–72 | sim | não | Degradação térmica severa / perda de empuxo |
| `hotsection.igniters` | Ignitores da turbina /mot | qualquer de turboprop, jet | 1000 h | 1500 | 2 | 24–72 | sim | não | Degradação térmica severa / perda de empuxo |
| `hotsection.fuel_nozzles` | Bicos injetores de combustível da turbina /mot | qualquer de turboprop, jet | 3000 h | 10000 × | 6 | 4–10 | não | não | Pulverização irregular / ponto quente na câmara de combustão |
| `hotsection.ngv` | Palhetas-guia de bocal da turbina (NGV - Nozzle Guide Vanes) /mot | qualquer de turboprop, jet | 5000 h | 32000 × | 18 | 16–48 | não | não | Erosão térmica / perda de rendimento da turbina |
| `hotsection.ignition_exciters` | Caixas excitadoras de ignição da turbina /mot | qualquer de turboprop, jet | 4000 h | 5000 | 3 | 2–6 | não | não | Ausência de centelha nos ignitores / falha na partida da turbina |

## Hélice (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `propeller.blades` | Pás da hélice /mot | qualquer de fixedPitchProp, constantSpeedProp | 2000 h | 3200 × | 4 | 6–18 | sim | sim | Vibração de hélice ou perda de controle de passo |
| `propeller.governor` | Governador de hélice /mot | todas de constantSpeedProp | 2000 h | 2100 × | 4 | 6–18 | sim | não | Vibração de hélice ou perda de controle de passo |
| `propeller.pitch_feathering` | Mecanismo de passo variável e embandeiramento da hélice /mot | todas de constantSpeedProp | 3000 h | 8500 × | 6 | 4–12 | não | não | Dificuldade de variação de passo ou falha ao embandeirar |
| `propeller.deice_boots` | Botas e anéis de degelo elétrico da hélice /mot | todas de propDeIce | 2500 h · 48 m | 4200 | 4 | 2–6 | não | não | Queima de resistência / acúmulo de gelo nas pás da hélice |

## Elétrico (13)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `electrical.battery` | Bateria principal de bordo | todas | 1200 h · 24 m | 850 | 1.5 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.alternator` | Alternador do motor (pistão) /mot | todas de piston | 1500 h | 1800 | 3 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.starter_generator` | Partida-gerador da turbina (Starter-Generator) /mot | qualquer de turboprop, jet | 2500 h | 6500 × | 4 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.voltage_regulator` | Regulador de tensão (GCU) | todas | 2500 h | 650 × | 2 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.wiring_harness` | Chicote e barramentos elétricos | todas | 6000 h | 2500 × | 12 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.apu_generator` | Geratriz da APU | todas de apu | 3000 h | 12000 × | 8 | 2–8 | não | não | Perda de geração / descarga da bateria |
| `electrical.inverter_converter` | Inversores e conversores estáticos de potência | qualquer de turboprop, jet | 5000 h | 1400 × | 6 | 4–12 | sim | não | Perda do barramento AC essencial / alimentação de instrumentos IFR |
| `electrical.standby_battery` | Bateria auxiliar / standby de emergência | qualquer de glassCockpit, turboprop, jet | 1500 h · 24 m | 1800 | 2 | 1–2 | não | não | Incapacidade de suprir barramento essencial de emergência |
| `electrical.idg` | Gerador de acionamento integrado (IDG / gerador principal) /mot | todas de jet | 3000 h | 22000 × | 8 | 4–12 | não | não | Falha de geração AC principal / desconexão do IDG |
| `electrical.rat` | Turbina de ar de impacto (RAT / gerador de emergência) | todas de rat | 10000 h | 25000 × | 8 | 4–10 | não | não | Falha de desdobramento ou geração hidráulica/elétrica de emergência |
| `electrical.tru_transformer_rectifier` | Unidade transformadora e retificadora (TRU) | qualquer de turboprop, jet | 6000 h | 12000 | 4 | 3–8 | não | não | Perda de conversão AC/DC e isolamento de barramento DC |
| `electrical.external_lighting` | Sistema de iluminação externa (faróis de pouso/táxi, strobes e nav) | todas | 1000 h | 800 | 2 | 1–2 | não | não | Queima de farol de pouso ou strobe impedindo operação noturna |
| `electrical.circuit_breakers_relays` | Painel de disjuntores, contatores e relés de barramento | todas | 10000 h | 2500 | 6 | 4–16 | não | não | Desarme espúrio ou falha de isolamento de barramento elétrico |

## Instrumentos (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `instruments.vacuum_pump` | Bomba de vácuo mecânica | todas de piston | 1000 h | 750 | 3 | 2–6 | não | não | Leitura errônea ou perda de instrumento |
| `instruments.pitot_tube` | Tubo de pitot com aquecimento | todas | 3000 h | 650 | 2 | 2–6 | sim | sim | Leitura errônea ou perda de instrumento |
| `instruments.static_ports` | Tomadas e linhas estáticas | todas | 4000 h | 350 | 2 | 2–6 | sim | sim | Leitura errônea ou perda de instrumento |
| `instruments.gyro_horizon` | Giroscópio / horizonte de atitude | todas | 2000 h | 2200 × | 3 | 2–6 | sim | não | Leitura errônea ou perda de instrumento |

## Combustível (9)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `fuel.engine_pump` | Bomba mecânica de combustível (pistão) /mot | todas de piston | 1800 h | 1400 | 4 | 3–8 | sim | não | Falha de alimentação / flutuação de pressão |
| `fuel.fcu` | Unidade de controle de combustível (FCU / HMU) /mot | qualquer de turboprop, jet | 3500 h | 8500 × | 8 | 3–8 | sim | não | Falha de alimentação / flutuação de pressão |
| `fuel.aux_pump` | Bomba elétrica auxiliar de combustível | todas | 1500 h | 950 | 2.5 | 3–8 | sim | não | Falha de alimentação / flutuação de pressão |
| `fuel.filter_gascolator` | Filtro e dreno de combustível (gascolator) | todas de piston | 500 h | 220 | 1 | 3–8 | sim | sim | Falha de alimentação / flutuação de pressão |
| `fuel.selector_valve` | Válvula seletora de combustível | todas | 3000 h | 850 | 3 | 3–8 | sim | não | Falha de alimentação / flutuação de pressão |
| `fuel.crossfeed_valve` | Válvula de alimentação cruzada e transferência (Crossfeed Valve) | todas de multiEngine | 6000 h | 5000 | 4 | 3–8 | não | não | Válvula de crossfeed travada na posição aberta ou fechada |
| `fuel.fire_shutoff_valve` | Válvula de corte emergencial de combustível (Fire Shutoff Valve) /mot | todas | 8000 h | 4000 | 4 | 2–6 | não | não | Falha de fechamento em emergência ou alarme falso de isolamento de fogo |
| `fuel.fohe` | Trocador de calor combustível/óleo (FOHE) /mot | qualquer de turboprop, jet | 7000 h | 11000 × | 6 | 6–14 | não | não | Congelamento de micropartículas de água no combustível em baixas temperaturas |
| `fuel.fqis_sensors` | Sensores de quantidade e computador de indicação de combustível (FQIS) | todas | 5000 h | 3500 | 4 | 2–6 | não | não | Indicação errática ou travada no medidor de quantidade nos tanques |

## Trem (10)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `gear.main_struts` | Amortecedores do trem principal | todas | 4000 h · 3000 cic | 2800 × | 6 | 6–16 | sim | sim | Colapso ou travamento de trem de pouso |
| `gear.nose_strut` | Amortecedor e tesoura do trem dianteiro | exceto tailwheel | 3500 h · 3000 cic | 2100 × | 5 | 6–16 | sim | sim | Colapso ou travamento de trem de pouso |
| `gear.tailwheel_assembly` | Conjunto da bequilha de cauda | todas de tailwheel | 3000 h · 2500 cic | 1100 | 3 | 6–16 | sim | sim | Colapso ou travamento de trem de pouso |
| `gear.actuator` | Atuadores e travas do trem retrátil | todas de retractableGear | 3000 h · 2500 cic | 4200 × | 8 | 6–16 | sim | não | Colapso ou travamento de trem de pouso |
| `gear.wheels_bearings` | Rodas e rolamentos do trem de pouso | todas | 2000 h · 2000 cic | 900 × | 4 | 3–6 | sim | sim | Superaquecimento de rolamento ou travamento de roda no pouso |
| `gear.trunnions_side_stays` | Munhões, tesouras de torção e hastes estruturais | todas | 15000 h · 15000 cic | 4800 × | 24 | 24–72 | sim | sim | Colapso estrutural do trem de pouso ou shimmy violento no toque |
| `gear.nosewheel_steering` | Direção do trem dianteiro e amortecedor shimmy | todas de noseWheelSteering | 5000 h | 1100 × | 6 | 4–10 | não | sim | Perda de controle direcional no táxi ou vibração intensa da bequilha |
| `gear.doors_mechanisms` | Portas do trem de pouso e mecanismos | todas de retractableGear | 8000 h · 8000 cic | 1400 × | 8 | 4–12 | não | sim | Porta do trem emperrada aberta causando arrasto parasita excessivo |
| `gear.skis` | Esquis de pouso para neve/gelo | todas de skis | 4000 h · 2500 cic · 60 m | 16000 × | 8 | 4–12 | sim | sim | Trincas na prancha ou travamento da posição do esqui (NO-GO em pistas de neve) |
| `gear.floats` | Flutuadores anfíbios / hidroavião | todas de floats | 6000 h · 3500 cic · 60 m | 28000 × | 12 | 6–18 | sim | sim | Vazamento em compartimento estanque ou falha de trem retrátil anfíbio (NO-GO na água) |

## Pneus (2)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `tires.main` | Pneus do trem principal (jogo) | todas | 500 h · 300 cic · 48 m | 480 × | 1.5 | 1–2 | sim | sim | Estouro ou perda de pressão de pneu |
| `tires.nose` | Pneu do trem dianteiro / bequilha | exceto tailwheel | 400 h · 250 cic · 48 m | 220 × | 1 | 1–2 | sim | sim | Estouro ou perda de pressão de pneu |

## Freios (3)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `brakes.pads_discs` | Pastilhas e discos de freio (jogo) | todas | 800 h · 500 cic | 620 × | 2.5 | 2–4 | não | sim | Perda de eficiência de frenagem |
| `brakes.master_cylinders` | Cilindros mestre e linhas de freio | todas | 2500 h · 2000 cic | 850 | 4 | 2–4 | sim | não | Perda de eficiência de frenagem |
| `gear.antiskid_system` | Sistema antiskid e autobrake | todas de antiskid | 4000 h | 1800 × | 6 | 4–8 | não | não | Inoperância do sistema antiskid / perda de frenagem modulada automática |

## Asa (11)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `airframe.wing_structure` | Longarinas e estrutura alar | todas | 12000 h | 15000 × | 40 | 12–48 | sim | sim | Dano estrutural na célula / AOG |
| `airframe.flaps` | Trilhos e mecanismo dos flaps | todas | 3500 h | 1900 × | 6 | 12–48 | não | sim | Dano estrutural na célula / AOG |
| `wing.root_fittings` | Ferragens e pinos de fixação da raiz da asa | todas | 20000 h · 20000 cic | 5200 × | 40 | 36–96 | sim | não | Trinca de fadiga na junção asa-fuselagem / interdição estrutural (AOG) |
| `wing.ribs_skin` | Nervuras e revestimento estrutural alar | todas | 15000 h | 3200 × | 28 | 24–48 | não | sim | Deformação do perfil alar / aumento de arrasto parasita |
| `wing.tanks_sealant` | Tanques integrais de combustível e selantes | todas | 8000 h | 2400 × | 20 | 16–40 | sim | sim | Vazamento de combustível alar através do selante de asa |
| `wing.tips_winglets` | Pontas de asa e winglets | todas | 15000 h | 1200 × | 10 | 8–24 | não | sim | Dano por raspagem de ponta de asa no toque / arrasto induzido |
| `wing.ailerons` | Ailerons e atuadores de rolamento | todas | 10000 h | 2500 × | 16 | 12–30 | sim | sim | Resposta assimétrica ou travamento do controle de rolamento |
| `wing.slats` | Slats de bordo de ataque e mecanismos | todas de slats | 6000 h · 6000 cic | 3400 × | 22 | 16–40 | sim | sim | Assimetria ou travamento de slat de bordo de ataque no pouso |
| `wing.spoilers` | Spoilers e speedbrakes | todas de spoilers | 5000 h · 5000 cic | 2600 × | 14 | 8–24 | não | sim | Falha de acionamento ou spoiler travado estendido em voo |
| `wing.deice_boots` | Botas pneumáticas de degelo da asa | todas de deIceBoots | 2500 h · 48 m | 1600 × | 12 | 12–24 | não | sim | Perfuração ou ressecamento da bota pneumática / acúmulo de gelo alar |
| `wing.bleed_anti_ice` | Válvulas e dutos de antigelo térmico da asa (Bleed Air) | todas de bleedAntiIce | 6000 h | 2800 × | 18 | 16–36 | não | não | Válvula de sangria presa fechada / inoperância do antigelo alar |

## Empenagem (7)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `airframe.flight_controls` | Comandos de voo e cabos de acionamento | todas | 4000 h | 2600 × | 12 | 12–48 | sim | sim | Dano estrutural na célula / AOG |
| `empennage.stabilizers` | Estabilizadores horizontal e vertical | todas | 25000 h | 4500 × | 32 | 48–120 | sim | sim | Dano estrutural nos estabilizadores / interdição de voo (AOG) |
| `empennage.elevator` | Profundor e comandos de arfagem | todas | 12000 h | 2200 × | 14 | 12–36 | sim | sim | Profundor travado ou excursão limitada de arfagem |
| `empennage.rudder` | Leme de direção | todas | 12000 h | 2000 × | 14 | 12–36 | sim | sim | Deformação do leme ou resposta assimétrica de guinada |
| `empennage.control_cables` | Cabos e atuadores de comando da empenagem | todas | 6000 h · 6000 cic | 1200 × | 12 | 6–18 | sim | não | Folga excessiva nos comandos ou resposta lenta da empenagem |
| `empennage.hinges_counterweights` | Dobradiças, rolamentos e contrapesos da cauda | todas | 4000 h | 800 × | 6 | 4–8 | não | sim | Vibração nos comandos de cauda / risco de flutter em alta velocidade |
| `empennage.trim_actuator` | Atuador e fuso do compensador (trim jackscrew) | todas | 3500 h | 950 × | 6 | 4–12 | não | sim | Compensador travado ou movimento descontrolado (trim runaway) |

## Hidráulico (6)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `hydraulic.pump` | Bomba hidráulica principal | todas de hydraulicGear | 3000 h | 3800 × | 6 | 4–12 | sim | não | Perda de pressão hidráulica |
| `hydraulic.electric_pump` | Bomba hidráulica acionada por motor elétrico (ACMP / Aux) | qualquer de hydraulicGear, turboprop, jet | 4000 h | 14000 × | 6 | 4–10 | não | não | Perda da bomba hidráulica auxiliar de reserva e solo |
| `hydraulic.accumulators` | Acumuladores hidráulicos de pressão e freio de emergência | qualquer de hydraulicGear, jet | 8000 h | 8000 | 4 | 3–8 | não | não | Perda de reserva de pressão para freios de emergência |
| `hydraulic.filters_valves` | Filtros hidráulicos e válvulas de retenção e seletoras | qualquer de hydraulicGear, turboprop, jet | 1500 h | 3000 | 3 | 2–4 | não | não | Filtro hidráulico colmatado / atuação lenta de atuadores |
| `hydraulic.ptu` | Unidade de transferência de potência bidirecional (PTU) | todas de ptu | 5000 h | 20000 × | 8 | 6–14 | não | não | Inoperância de transferência hidráulica entre sistemas verde e amarelo |
| `hydraulic.lines_fittings` | Reservatório, tubulações, mangueiras e conexões hidráulicas | qualquer de hydraulicGear, turboprop, jet | 3500 h · 60 m | 4500 × | 8 | 4–12 | sim | sim | Microvazamento contínuo ou perda de pressão hidráulica |

## Pressurização (2)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `pressurization.outflow_valve` | Válvula de fluxo (outflow valve) | todas de pressurized | 3500 h | 5500 × | 6 | 6–16 | sim | não | Perda de capacidade de pressurização |
| `pressurization.safety_relief_valves` | Válvulas de segurança de alívio de pressão diferencial | todas de pressurized | 10000 cic | 4000 | 3 | 2–6 | não | não | Vazamento residual de ar comprimido ou risco de sobrepressão de cabine |

## Fuselagem (9)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `fuselage.frames_bulkheads` | Cavernas e armações estruturais da fuselagem | todas | 15000 h · 15000 cic | 5600 × | 48 | 48–120 | sim | não | Deformação permanente de caverna estrutural / interdição mandatória |
| `fuselage.pressure_bulkheads` | Anteparas de pressão dianteira e traseira | todas de pressurized | 20000 h · 20000 cic | 8500 × | 60 | 60–140 | sim | não | Fadiga crítica na antepara / risco de descompressão catastrófica (AOG) |
| `fuselage.longerons_stringers` | Longarinas e reforçadores da fuselagem | todas | 25000 h | 6200 × | 52 | 60–150 | sim | não | Trinca em longarina primária de fuselagem / AOG |
| `fuselage.skin_fairings` | Revestimento e carenagens da fuselagem | todas | 15000 h | 1800 × | 18 | 16–40 | não | sim | Dano na carenagem de fuselagem / aumento de arrasto aerodinâmico |
| `fuselage.doors_seals` | Portas, escotilhas e vedações de cabine | todas | 8000 h · 8000 cic · 60 m | 1500 × | 10 | 8–24 | sim | sim | Vazamento de pressurização ou alerta de porta destravada em voo |
| `fuselage.windshield_windows` | Para-brisas e janelas de cabine | todas | 4000 h | 1300 × | 8 | 6–18 | sim | sim | Trinca no para-brisa ou delaminação impedindo visão de pouso |
| `fuselage.radome` | Radome do nariz e proteção meteorológica | todas de weatherRadar | 5000 h | 950 × | 6 | 4–12 | não | sim | Atenuação de eco radar ou dano de erosão no radome de nariz |
| `fuselage.antennas` | Conjunto de antenas externas e descarregadores estáticos | todas | 6000 h | 600 × | 4 | 2–6 | não | sim | Perda de sensibilidade ou estática na recepção COM/NAV/GPS |
| `fuselage.cargo_doors_latches` | Portas de carga, travas e sensores de fechamento | qualquer de multiEngine, pressurized, turboprop, jet | 8000 cic | 7000 | 6 | 4–12 | não | não | Alerta DOOR OPEN no solo ou falha no mecanismo de travamento de carga |

## Aviônicos (14)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `avionics.com_nav_radios` | Rádios COM e NAV (VHF / VOR / ILS) | todas | 8000 h | 1600 × | 6 | 4–10 | não | não | Perda de comunicação por voz ATC ou recepção de radiofaróis de aproximação |
| `avionics.transponder` | Transponder Mode S / ADS-B Out | todas | 8000 h | 1200 × | 4 | 3–8 | não | não | Perda de squawk / perda de vigilância e altitude radar secundário ATC |
| `avionics.gps_receiver` | Receptor GNSS / GPS de navegação aérea | todas | 10000 h | 1800 × | 6 | 4–12 | não | não | Perda de fixação de posição por satélite GPS / navegação por waypoints |
| `avionics.fms_computer` | FMS / Computador de gerenciamento de voo | todas de fms | 12000 h | 4500 × | 12 | 8–20 | não | não | Falha de cálculo LNAV/VNAV / tela inoperante da CDU de rota |
| `avionics.autopilot_servos` | Piloto automático e servos mecânicos | todas de autopilot | 6000 h | 2200 × | 8 | 6–16 | não | não | Desengate involuntário do piloto automático ou servo de comando travado |
| `avionics.glass_displays` | Telas integradas Glass Cockpit (PFD/MFD/EICAS) | todas de glassCockpit | 6000 h | 3800 × | 10 | 6–16 | sim | não | Tela preta, flicker intermitente ou perda total de instrumentos primários |
| `avionics.analog_flight_instruments` | Instrumentos analógicos de voo (velocímetro, altímetro, VSI) | exceto glassCockpit | 4000 h | 900 × | 6 | 4–10 | sim | não | Indicação travada ou ponteiro oscilante de velocidade/altitude |
| `avionics.weather_radar` | Radar meteorológico e antena de varredura | todas de weatherRadar | 5000 h | 2600 × | 10 | 6–18 | não | não | Perda de detecção meteorológica de precipitação e células convectivas |
| `avionics.ahrs_irs` | Plataforma inercial e computador de atitude/proa (AHRS / IRS) | qualquer de glassCockpit, autopilot | 8000 h | 15000 | 6 | 3–8 | não | não | Bandeira ATT/HDG no PFD ou perda de alinhamento inercial |
| `avionics.adc` | Computador digital de dados de ar (Air Data Computer - ADC) | qualquer de glassCockpit, turboprop, jet | 7000 h | 12000 | 5 | 2–6 | não | não | Perda de airspeed/altitude no PFD ou dados de ar degradados |
| `avionics.standby_instruments` | Instrumento eletrônico de standby integrado (ISIS / reserva) | todas de glassCockpit | 8000 h | 9000 | 4 | 2–6 | não | não | Apagão da tela do instrumento de reserva integrado |
| `avionics.tcas_egpws` | Sistema de vigilância TCAS / EGPWS / TAWS | qualquer de turboprop, jet | 6000 h | 18000 | 6 | 3–8 | não | não | Inoperância de avisos sonoros e visuais de tráfego e terreno |
| `avionics.flight_recorders_elt` | Gravadores de voo (FDR/CVR) e transmissor localizador (ELT) | todas | 10000 h · 24 m | 6000 | 4 | 2–6 | não | não | Falha de auto-teste do ELT ou perda de gravação de parâmetros de voo |
| `avionics.audio_panel` | Painel de áudio, caixa de controle e intercomunicador | todas | 8000 h | 3500 | 3 | 2–4 | não | não | Falha de transmissão de áudio nos fones ou seleção de canal COM |

## Pneumático (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `pneumatic.bleed_valves` | Válvulas reguladoras e de corte de sangria de ar (Bleed Valves) /mot | todas de bleedAir | 4500 h | 12000 × | 6 | 4–10 | não | não | Válvula de sangria presa fechada / BLEED TRIP OFF |
| `pneumatic.precooler` | Trocador de calor pré-resfriador do ar de sangria (Precooler) /mot | todas de bleedAir | 6000 h | 15000 × | 8 | 6–14 | não | não | Superaquecimento do duto de sangria / alarme de BLEED OVERHEAT |
| `pneumatic.ducts_leak_detection` | Dutos pneumáticos e loops de detecção de vazamento e calor | todas de bleedAir | 8000 h | 10000 | 6 | 4–12 | não | não | Alarme de vazamento pneumático / BLEED DUCT LEAK |
| `pneumatic.air_cycle_machine` | Máquina de ciclo de ar e unidade de condicionamento (Packs / ACM) | todas de pressurized; qualquer de turboprop, jet | 5000 h | 28000 × | 12 | 8–18 | não | não | Desarme de pack de refrigeração / limitação de teto operacional |

## Cabine (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `cabin.passenger_seats` | Assentos de passageiros | todas de passengerCabin | 12000 h | 8500 × | 8 | 4–16 | não | não | Desgaste de estofamento, quebra de reclinação de assentos, redução de conforto/satisfação de passageiros |
| `cabin.galley` | Galley / Copa de bordo | todas de galley | 10000 h | 15000 × | 12 | 6–18 | não | não | Falha elétrica de aquecedores/fornos ou travamento de portas da copa |
| `cabin.lavatory` | Sistema de lavatório | todas de lavatory | 8000 h | 12000 × | 10 | 4–12 | não | não | Entupimento ou vazamento de lavatório; restrição para missões longas de passageiros (MEL de cabine) |
| `cabin.in_flight_entertainment` | Sistema de entretenimento a bordo (IFE) | todas de passengerCabin, jet | 6000 h | 18000 × | 8 | 4–12 | não | não | Telas de assento inoperantes ou congeladas; degradação da experiência de bordo |

## Oxigênio (3)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `oxygen.crew_cylinder` | Cilindro de oxigênio da tripulação | todas de oxygen | 15000 h · 60 m | 3500 | 4 | 2–6 | sim | não | Pressão de oxigênio de tripulação insuficiente; impede decolagem de voo em alta altitude (NO-GO) |
| `oxygen.passenger_system` | Sistema de oxigênio dos passageiros | todas de oxygen, passengerCabin | 12000 h · 60 m | 9000 × | 8 | 4–12 | sim | não | Falha de pressurização das linhas de passageiros ou geradores vencidos; limita altitude de despacho |
| `oxygen.masks_regulators` | Máscaras e reguladores de oxigênio | todas de oxygen | 8000 h · 36 m | 2200 | 3 | 2–4 | sim | sim | Vazamento nas máscaras tipo quick-donning ou travamento do regulador de demanda (NO-GO) |

## Proteção contra Fogo (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `fire.engine_detection` | Sistema de detecção de fogo do motor /mot | todas de fireProtection | 10000 h | 4200 × | 6 | 4–10 | sim | não | Falha do laço sensor de fogo (FIRE LOOP FAULT) ou alarme falso de fogo no motor (NO-GO) |
| `fire.engine_extinguisher_bottles` | Garrafas extintoras de fogo do motor /mot | todas de fireProtection | 15000 h · 60 m | 6500 × | 4 | 3–8 | sim | não | Garrafa extintora descarregada ou cápsula detonadora vencida; aeronave impedida de voar (NO-GO) |
| `fire.apu_extinguisher` | Detecção e extinção de fogo da APU | todas de apu, fireProtection | 12000 h · 60 m | 5000 × | 4 | 2–6 | não | não | APU FIRE DISCH inoperante; APU inoperante para partida e geração de solo/voo |
| `fire.cabin_extinguishers` | Extintores portáteis de cabine e cockpit | todas | 20000 h · 24 m | 450 | 1 | 1–2 | sim | sim | Extintor de bordo descarregado ou vencido (NO-GO) |

## Emergência (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `emergency.evacuation_slides` | Escorregadeiras de evacuação de emergência | todas de passengerCabin, jet | 15000 h · 36 m | 22000 × | 10 | 6–16 | sim | sim | SLIDE DISARMED permanente ou garrafa descarregada (NO-GO para voos comerciais) |
| `emergency.life_vests_rafts` | Coletes salva-vidas e botes de emergência | todas de passengerCabin | 12000 h · 36 m | 6000 × | 4 | 2–6 | não | não | Coletes vencidos ou ausentes; restrição para operações sobre a água (ETOPS/marítimo) |
| `emergency.windshield_wipers` | Limpadores de para-brisa eletromecânicos | todas de wipers | 4000 h · 12 m | 1800 | 2 | 1–3 | não | sim | Palheta rasgada ou motor travado; visibilidade reduzida sob chuva forte no pouso |
| `emergency.ice_detector` | Sonda detectora de gelo óptico/vibratório | todas de iceDetector | 6000 h | 4800 × | 3 | 2–5 | não | sim | Falha do aviso ICE DETECTED; acionamento de degelo depende de observação visual manual |

## Iluminação (6, LUZES-F2 — em implementação)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito |
|---|---|---|---|---|---|---|---|---|---|
| `lights.landing` | Farol de pouso | exceto noElectrical | 300 h acesas | 60 | 0.5 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |
| `lights.taxi` | Farol de táxi | exceto noElectrical | 300 h acesas | 60 | 0.5 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |
| `lights.nav` | Luzes de navegação | exceto noElectrical | 1000 h acesas | 40 | 0.5 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |
| `lights.strobe` | Strobes (anticolisão) | exceto noElectrical | 1000 h acesas | 250 | 1 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |
| `lights.beacon` | Beacon (anticolisão) | exceto noElectrical | 1000 h acesas | 120 | 0.5 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |
| `lights.panel` | Iluminação do painel | exceto noElectrical | 2000 h acesas | 50 | 1 | 0,5–1 | não | sim | Lâmpada queimada (aviso MEL noturno) |

## Peças por avião (catálogo atual, sem as lâmpadas)

| Avião | Features | Peças (linhas, com /mot) |
|---|---|---|
| 737-max-8.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 117 (143) |
| 747-8i.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 117 (195) |
| a310.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| a320neo.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| a321.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| a330-200.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| a330-belugaxl.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 111 (137) |
| a400m.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, constantSpeedProp, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, multiEngine, noseWheelSteering, oxygen, pressurized, propDeIce, rat, retractableGear, slats, spoilers, thsTrim, turboprop, weatherRadar, wipers | 112 (196) |
| airbus-a321lr.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| airbus-a330-300.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 118 (144) |
| asobo-tbm-930.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers | 106 (106) |
| atr-42-600.json | antiskid, autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers | 112 (140) |
| atr-72-600.json | antiskid, autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers | 112 (140) |
| beechcraft-baron-g58.json | autopilot, constantSpeedProp, deIceBoots, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, weatherRadar | 93 (115) |
| beechcraft-bonanza-g36.json | autopilot, constantSpeedProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 79 (79) |
| beechcraft-bonanza-v35.json | autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 76 (76) |
| beechcraft-bonanza.json | autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 76 (76) |
| beechcraft-king-air-250.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 107 (134) |
| beechcraft-king-air-c90gtx.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 107 (134) |
| beechcraft-king-air.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 107 (134) |
| boeing-747-400-global-supertanker.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 109 (184) |
| boeing-747-400.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 116 (191) |
| boeing-787-10-dreamliner.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 117 (143) |
| c172sp-g1000.json | autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine | 75 (75) |
| c208b.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fixedGear, glassCockpit, noseWheelSteering, oxygen, passengerCabin, propDeIce, singleEngine, turboprop, weatherRadar, wipers | 94 (94) |
| c400-corvalis.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, spoilers, turbocharged | 82 (82) |
| cap-4.json | carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel | 66 (66) |
| cessna-188-agtruck.json | constantSpeedProp, fixedGear, fuelInjected, piston, singleEngine, tailwheel | 68 (68) |
| cessna-207t-stationair.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, turbocharged | 78 (78) |
| cessna-404-titan.json | autopilot, constantSpeedProp, deIceBoots, fireProtection, fuelInjected, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar, wipers | 94 (119) |
| cessna-408-skycourier.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fixedGear, fms, glassCockpit, iceDetector, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, turboprop, weatherRadar, wipers | 99 (126) |
| cessna-c152.json | carbureted, fixedGear, fixedPitchProp, noseWheelSteering, piston, singleEngine | 68 (68) |
| cessna-citation-cj4.json | antiskid, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, retractableGear, spoilers, weatherRadar, wipers | 111 (137) |
| cessna-citation-longitude.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, thsTrim, weatherRadar, wipers | 116 (142) |
| cirrus-sr22.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine | 80 (80) |
| dhc-6-300-twin.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fixedGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, skis, turboprop, wipers | 93 (118) |
| diamond-da40ng.json | autopilot, constantSpeedProp, diesel, fadec, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine, turbocharged | 78 (78) |
| diamond-da62.json | autopilot, constantSpeedProp, deIceBoots, diesel, fadec, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar | 94 (117) |
| es30.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, electricHybrid, fireProtection, fms, galley, glassCockpit, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 107 (188) |
| flight-design-ct.json | autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine | 73 (73) |
| nxcub.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine | 75 (75) |
| pc-12-ngx.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, lavatory, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers | 108 (108) |
| pilatus-pc-24.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, weatherRadar, wipers | 116 (142) |
| piper-j-3-cub.json | carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel | 66 (66) |
| piper-pa28-236-dakota.json | autopilot, carbureted, constantSpeedProp, fixedGear, noseWheelSteering, passengerCabin, piston, singleEngine | 74 (74) |
| robin-dr400.json | carbureted, fixedGear, fixedPitchProp, noseWheelSteering, passengerCabin, piston, singleEngine | 70 (70) |

## Pontos para revisar

Achados automáticos (peças que não deveriam existir no avião):

- **cap-4.json** (`noElectrical`) recebe: `electrical.battery`, `electrical.alternator`, `electrical.voltage_regulator`, `electrical.wiring_harness`, `avionics.com_nav_radios`, `avionics.transponder`, `avionics.gps_receiver`, `avionics.analog_flight_instruments`, `avionics.flight_recorders_elt`, `avionics.audio_panel`, `electrical.external_lighting`, `electrical.circuit_breakers_relays`
- **cap-4.json** (`noFlaps`) recebe: `airframe.flaps`
- **piper-j-3-cub.json** (`noElectrical`) recebe: `electrical.battery`, `electrical.alternator`, `electrical.voltage_regulator`, `electrical.wiring_harness`, `avionics.com_nav_radios`, `avionics.transponder`, `avionics.gps_receiver`, `avionics.analog_flight_instruments`, `avionics.flight_recorders_elt`, `avionics.audio_panel`, `electrical.external_lighting`, `electrical.circuit_breakers_relays`
- **piper-j-3-cub.json** (`noFlaps`) recebe: `airframe.flaps`

Outros pontos:

- **Sobreposição com a LUZES-F2:** já existe `electrical.external_lighting` (pouso/táxi/strobe/nav num item só, 1.000 h, $ 800, todas as aeronaves). Com as 6 lâmpadas novas a iluminação ficaria contada duas vezes. Opções: remover o item antigo ou mantê-lo como "fiação e chaves da iluminação".
- **O Hangar ainda não lista peças:** hoje ele mostra só as 9 barras de componentes; as peças entram na média de cada barra (`min×0,7 + média×0,3`). Uma tela de peças por grupo seria uma tarefa de UI à parte.
- **Vidas e preços** são estimativas do catálogo (`lifeSource: "estimativa — revisar"`), salvo onde houver referência.
