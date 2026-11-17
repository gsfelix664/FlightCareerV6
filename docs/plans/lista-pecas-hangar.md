# Peças do Hangar — lista para revisão

Gerado em 2026-11-17 de `parts_catalog.json` (153 peças em 22 grupos). Grupo Motor recalibrado pelo usuário em 2026-11-17; demais valores de vida útil marcados como estimativa no catálogo (`lifeSource`).

Legenda: **Vida** = horas / ciclos / meses (o que houver); **Preço** em $ (× = escala com o preço do avião); **MO** = mão de obra (h); **Parada** = downtime (h); **NO-GO** = quebrada bloqueia o voo; **WA** = visível no walkaround; **/mot** = uma por motor.

## Motor (18)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `engine.cylinders` | Cilindros e pistões /mot | todas de piston | 250 h | 1600 × | 8 | 4–14 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.crankshaft` | Virabrequim e mancais /mot | todas de piston | 350 h | 2200 × | 12 | 6–18 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.valves` | Válvulas de admissão e escape /mot | todas de piston | 150 h | 550 × | 5 | 3–10 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.carburetor` | Carburador aeronáutico /mot | todas de piston, carbureted | 180 h | 500 × | 2 | 3–10 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.fuel_injectors` | Bicos injetores e distribuidor /mot | todas de piston, fuelInjected | 200 h | 650 × | 3 | 3–10 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.turbocharger` | Turbocompressor, wastegate e controlador de pressão /mot | todas de piston, turbocharged | 180 h | 1400 × | 4 | 4–12 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.starter` | Motor de partida elétrico /mot | todas de piston | 200 h · 150 cic | 320 | 1.5 | 2–8 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.coolant_system` | Radiador e arrefecimento líquido /mot | todas de piston, diesel | 200 h | 680 × | 3 | 3–10 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.compressor` | Compressor axial / centrífugo da turbina /mot | qualquer de turboprop, jet | 500 h | 11500 × | 10 | 6–18 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.reduction_gearbox` | Caixa de redução da hélice (RGB) /mot | todas de turboprop | 400 h | 6500 × | 7 | 4–14 | sim | não | Perda de potência ou falha mecânica de motor |
| `engine.fan` | Fan frontal do turbofan /mot | todas de jet | 600 h · 400 cic | 16000 × | 8 | 6–18 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.thrust_reverser` | Conjunto do reversor de empuxo /mot | todas de jet | 300 cic | 7800 × | 5 | 4–12 | sim | sim | Perda de potência ou falha mecânica de motor |
| `engine.mounts_trusses` | Coxins e berço estrutural de fixação do motor /mot | todas | 650 h | 2800 × | 6 | 4–12 | não | não | Vibração excessiva do motor e desalinhamento estrutural |
| `engine.cowling_nacelle` | Carenagem e nacele aerodinâmica do motor /mot | todas | 1000 h | 3200 × | 4 | 3–8 | não | não | Trinca em painel de carenagem / arrasto parasita |
| `engine.air_intake_filter` | Filtro e duto de admissão de ar do motor (inlet screen) /mot | todas | 80 h | 220 | 1 | 1–2 | não | não | Obstrução do fluxo de admissão / perda de potência |
| `engine.pneumatic_starter` | Motor de partida pneumático (ATS - Air Turbine Starter) /mot | todas de jet | 250 cic | 5600 × | 3 | 2–5 | não | não | Falha de partida pneumática / recusa de acionamento |
| `engine.fadec_eec` | FADEC / Módulo eletrônico de controle do motor (EEC) /mot | todas de fadec | 800 h | 8500 × | 3 | 2–6 | não | não | Degradação para modo manual / flutuação de empuxo |
| `engine.apu_core` | Módulo gerador de gás e turbina da APU | todas de apu | 450 h | 12000 × | 8 | 4–12 | não | não | Recusa de partida da APU / perda de potência pneumática auxiliar |

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

## Elétrico (12)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `electrical.battery` | Bateria principal de bordo | exceto noElectrical | 1200 h · 24 m | 850 | 1.5 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.alternator` | Alternador do motor (pistão) /mot | todas de piston; exceto noElectrical | 1500 h | 1800 | 3 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.starter_generator` | Partida-gerador da turbina (Starter-Generator) /mot | qualquer de turboprop, jet; exceto noElectrical | 2500 h | 6500 × | 4 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.voltage_regulator` | Regulador de tensão (GCU) | exceto noElectrical | 2500 h | 650 × | 2 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.wiring_harness` | Chicote e barramentos elétricos | exceto noElectrical | 6000 h | 2500 × | 12 | 2–8 | sim | não | Perda de geração / descarga da bateria |
| `electrical.apu_generator` | Geratriz da APU | todas de apu; exceto noElectrical | 3000 h | 12000 × | 8 | 2–8 | não | não | Perda de geração / descarga da bateria |
| `electrical.inverter_converter` | Inversores e conversores estáticos de potência | qualquer de turboprop, jet; exceto noElectrical | 5000 h | 1400 × | 6 | 4–12 | sim | não | Perda do barramento AC essencial / alimentação de instrumentos IFR |
| `electrical.standby_battery` | Bateria auxiliar / standby de emergência | qualquer de glassCockpit, turboprop, jet; exceto noElectrical | 1500 h · 24 m | 1800 | 2 | 1–2 | não | não | Incapacidade de suprir barramento essencial de emergência |
| `electrical.idg` | Gerador de acionamento integrado (IDG / gerador principal) /mot | todas de jet; exceto noElectrical | 3000 h | 22000 × | 8 | 4–12 | não | não | Falha de geração AC principal / desconexão do IDG |
| `electrical.rat` | Turbina de ar de impacto (RAT / gerador de emergência) | todas de rat; exceto noElectrical | 10000 h | 25000 × | 8 | 4–10 | não | não | Falha de desdobramento ou geração hidráulica/elétrica de emergência |
| `electrical.tru_transformer_rectifier` | Unidade transformadora e retificadora (TRU) | qualquer de turboprop, jet; exceto noElectrical | 6000 h | 12000 | 4 | 3–8 | não | não | Perda de conversão AC/DC e isolamento de barramento DC |
| `electrical.circuit_breakers_relays` | Painel de disjuntores, contatores e relés de barramento | exceto noElectrical | 10000 h | 2500 | 6 | 4–16 | não | não | Desarme espúrio ou falha de isolamento de barramento elétrico |

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

## Comandos de voo (0)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|

## Asa (11)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `airframe.wing_structure` | Longarinas e estrutura alar | todas | 12000 h | 15000 × | 40 | 12–48 | sim | sim | Dano estrutural na célula / AOG |
| `airframe.flaps` | Trilhos e mecanismo dos flaps | exceto noFlaps | 3500 h | 1900 × | 6 | 12–48 | não | sim | Dano estrutural na célula / AOG |
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

## Célula (0)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|

## Aviônicos (14)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `avionics.com_nav_radios` | Rádios COM e NAV (VHF / VOR / ILS) | exceto noElectrical | 8000 h | 1600 × | 6 | 4–10 | não | não | Perda de comunicação por voz ATC ou recepção de radiofaróis de aproximação |
| `avionics.transponder` | Transponder Mode S / ADS-B Out | exceto noElectrical | 8000 h | 1200 × | 4 | 3–8 | não | não | Perda de squawk / perda de vigilância e altitude radar secundário ATC |
| `avionics.gps_receiver` | Receptor GNSS / GPS de navegação aérea | exceto noElectrical | 10000 h | 1800 × | 6 | 4–12 | não | não | Perda de fixação de posição por satélite GPS / navegação por waypoints |
| `avionics.fms_computer` | FMS / Computador de gerenciamento de voo | todas de fms; exceto noElectrical | 12000 h | 4500 × | 12 | 8–20 | não | não | Falha de cálculo LNAV/VNAV / tela inoperante da CDU de rota |
| `avionics.autopilot_servos` | Piloto automático e servos mecânicos | todas de autopilot; exceto noElectrical | 6000 h | 2200 × | 8 | 6–16 | não | não | Desengate involuntário do piloto automático ou servo de comando travado |
| `avionics.glass_displays` | Telas integradas Glass Cockpit (PFD/MFD/EICAS) | todas de glassCockpit; exceto noElectrical | 6000 h | 3800 × | 10 | 6–16 | sim | não | Tela preta, flicker intermitente ou perda total de instrumentos primários |
| `avionics.analog_flight_instruments` | Instrumentos analógicos de voo (velocímetro, altímetro, VSI) | exceto glassCockpit | 4000 h | 900 × | 6 | 4–10 | sim | não | Indicação travada ou ponteiro oscilante de velocidade/altitude |
| `avionics.weather_radar` | Radar meteorológico e antena de varredura | todas de weatherRadar; exceto noElectrical | 5000 h | 2600 × | 10 | 6–18 | não | não | Perda de detecção meteorológica de precipitação e células convectivas |
| `avionics.ahrs_irs` | Plataforma inercial e computador de atitude/proa (AHRS / IRS) | qualquer de glassCockpit, autopilot; exceto noElectrical | 8000 h | 15000 | 6 | 3–8 | não | não | Bandeira ATT/HDG no PFD ou perda de alinhamento inercial |
| `avionics.adc` | Computador digital de dados de ar (Air Data Computer - ADC) | qualquer de glassCockpit, turboprop, jet; exceto noElectrical | 7000 h | 12000 | 5 | 2–6 | não | não | Perda de airspeed/altitude no PFD ou dados de ar degradados |
| `avionics.standby_instruments` | Instrumento eletrônico de standby integrado (ISIS / reserva) | todas de glassCockpit; exceto noElectrical | 8000 h | 9000 | 4 | 2–6 | não | não | Apagão da tela do instrumento de reserva integrado |
| `avionics.tcas_egpws` | Sistema de vigilância TCAS / EGPWS / TAWS | qualquer de turboprop, jet; exceto noElectrical | 6000 h | 18000 | 6 | 3–8 | não | não | Inoperância de avisos sonoros e visuais de tráfego e terreno |
| `avionics.flight_recorders_elt` | Gravadores de voo (FDR/CVR) e transmissor localizador (ELT) | exceto noElectrical | 10000 h · 24 m | 6000 | 4 | 2–6 | não | não | Falha de auto-teste do ELT ou perda de gravação de parâmetros de voo |
| `avionics.audio_panel` | Painel de áudio, caixa de controle e intercomunicador | exceto noElectrical | 8000 h | 3500 | 3 | 2–4 | não | não | Falha de transmissão de áudio nos fones ou seleção de canal COM |

## Hidráulico (6)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `hydraulic.pump` | Bomba hidráulica principal | todas de hydraulicGear | 3000 h | 3800 × | 6 | 4–12 | sim | não | Perda de pressão hidráulica |
| `hydraulic.electric_pump` | Bomba hidráulica acionada por motor elétrico (ACMP / Aux) | qualquer de hydraulicGear, turboprop, jet | 4000 h | 14000 × | 6 | 4–10 | não | não | Perda da bomba hidráulica auxiliar de reserva e solo |
| `hydraulic.accumulators` | Acumuladores hidráulicos de pressão e freio de emergência | qualquer de hydraulicGear, jet | 8000 h | 8000 | 4 | 3–8 | não | não | Perda de reserva de pressão para freios de emergência |
| `hydraulic.filters_valves` | Filtros hidráulicos e válvulas de retenção e seletoras | qualquer de hydraulicGear, turboprop, jet | 1500 h | 3000 | 3 | 2–4 | não | não | Filtro hidráulico colmatado / atuação lenta de atuadores |
| `hydraulic.ptu` | Unidade de transferência de potência bidirecional (PTU) | todas de ptu | 5000 h | 20000 × | 8 | 6–14 | não | não | Inoperância de transferência hidráulica entre sistemas verde e amarelo |
| `hydraulic.lines_fittings` | Reservatório, tubulações, mangueiras e conexões hidráulicas | qualquer de hydraulicGear, turboprop, jet | 3500 h · 60 m | 4500 × | 8 | 4–12 | sim | sim | Microvazamento contínuo ou perda de pressão hidráulica |

## Pneumático (4)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `pneumatic.bleed_valves` | Válvulas reguladoras e de corte de sangria de ar (Bleed Valves) /mot | todas de bleedAir | 4500 h | 12000 × | 6 | 4–10 | não | não | Válvula de sangria presa fechada / BLEED TRIP OFF |
| `pneumatic.precooler` | Trocador de calor pré-resfriador do ar de sangria (Precooler) /mot | todas de bleedAir | 6000 h | 15000 × | 8 | 6–14 | não | não | Superaquecimento do duto de sangria / alarme de BLEED OVERHEAT |
| `pneumatic.ducts_leak_detection` | Dutos pneumáticos e loops de detecção de vazamento e calor | todas de bleedAir | 8000 h | 10000 | 6 | 4–12 | não | não | Alarme de vazamento pneumático / BLEED DUCT LEAK |
| `pneumatic.air_cycle_machine` | Máquina de ciclo de ar e unidade de condicionamento (Packs / ACM) | todas de pressurized; qualquer de turboprop, jet | 5000 h | 28000 × | 12 | 8–18 | não | não | Desarme de pack de refrigeração / limitação de teto operacional |

## Degelo e proteção (0)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|

## Cabine de passageiros (0)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|

## Oxigênio e emergência (0)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|

## Iluminação (6)

| id | Peça | Aplica-se a | Vida | Preço | MO | Parada | NO-GO | WA | Efeito ao quebrar |
|---|---|---|---|---|---|---|---|---|---|
| `lights.landing` | Farol de pouso | exceto noElectrical | 300 h | 60 | 0.5 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |
| `lights.taxi` | Farol de táxi | exceto noElectrical | 300 h | 60 | 0.5 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |
| `lights.nav` | Luzes de navegação | exceto noElectrical | 1000 h | 40 | 0.5 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |
| `lights.strobe` | Luzes estroboscópicas (anticolisão) | exceto noElectrical | 1000 h | 250 | 1 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |
| `lights.beacon` | Beacon (anticolisão) | exceto noElectrical | 1000 h | 120 | 0.5 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |
| `lights.panel` | Iluminação do painel | exceto noElectrical | 2000 h | 50 | 1 | 0.5–1 | não | sim | Lâmpada queimada (luz apagada) |

## Peças por modelo de aeronave (46 aeronaves do catálogo)

Contagem de peças únicas (e total físico considerando motores adicionais):

| Arquivo | Features | Peças |
|---|---|---|
| 737-max-8.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 122 (148) |
| 747-8i.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 122 (200) |
| a310.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| a320neo.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| a321.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| a330-200.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| a330-belugaxl.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 116 (142) |
| a400m.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, constantSpeedProp, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, multiEngine, noseWheelSteering, oxygen, pressurized, propDeIce, rat, retractableGear, slats, spoilers, thsTrim, turboprop, weatherRadar, wipers | 117 (201) |
| airbus-a321lr.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| airbus-a330-300.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, ptu, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 123 (149) |
| asobo-tbm-930.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers | 111 (111) |
| atr-42-600.json | antiskid, autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers | 117 (145) |
| atr-72-600.json | antiskid, autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, spoilers, turboprop, weatherRadar, wipers | 117 (145) |
| beechcraft-baron-g58.json | autopilot, constantSpeedProp, deIceBoots, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, weatherRadar | 98 (120) |
| beechcraft-bonanza-g36.json | autopilot, constantSpeedProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 84 (84) |
| beechcraft-bonanza-v35.json | autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 81 (81) |
| beechcraft-bonanza.json | autopilot, constantSpeedProp, fuelInjected, noseWheelSteering, passengerCabin, piston, retractableGear, singleEngine | 81 (81) |
| beechcraft-king-air-250.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 112 (139) |
| beechcraft-king-air-c90gtx.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 112 (139) |
| beechcraft-king-air.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fms, glassCockpit, hydraulicGear, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 112 (139) |
| boeing-747-400-global-supertanker.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, multiEngine, noseWheelSteering, oxygen, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 114 (189) |
| boeing-747-400.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 121 (196) |
| boeing-787-10-dreamliner.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, slats, spoilers, thsTrim, weatherRadar, wipers | 122 (148) |
| c172sp-g1000.json | autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine | 80 (80) |
| c208b.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fixedGear, glassCockpit, noseWheelSteering, oxygen, passengerCabin, propDeIce, singleEngine, turboprop, weatherRadar, wipers | 99 (99) |
| c400-corvalis.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, spoilers, turbocharged | 87 (87) |
| cap-4.json | carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel | 54 (54) |
| cessna-188-agtruck.json | constantSpeedProp, fixedGear, fuelInjected, piston, singleEngine, tailwheel | 73 (73) |
| cessna-207t-stationair.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine, turbocharged | 83 (83) |
| cessna-404-titan.json | autopilot, constantSpeedProp, deIceBoots, fireProtection, fuelInjected, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar, wipers | 99 (124) |
| cessna-408-skycourier.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fireProtection, fixedGear, fms, glassCockpit, iceDetector, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, turboprop, weatherRadar, wipers | 104 (131) |
| cessna-c152.json | carbureted, fixedGear, fixedPitchProp, noseWheelSteering, piston, singleEngine | 73 (73) |
| cessna-citation-cj4.json | antiskid, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, retractableGear, spoilers, weatherRadar, wipers | 116 (142) |
| cessna-citation-longitude.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, thsTrim, weatherRadar, wipers | 121 (147) |
| cirrus-sr22.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, oxygen, passengerCabin, piston, singleEngine | 85 (85) |
| dhc-6-300-twin.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fixedGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, propDeIce, skis, turboprop, wipers | 98 (123) |
| diamond-da40ng.json | autopilot, constantSpeedProp, diesel, fadec, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, passengerCabin, piston, singleEngine, turbocharged | 83 (83) |
| diamond-da62.json | autopilot, constantSpeedProp, deIceBoots, diesel, fadec, fuelInjected, glassCockpit, hydraulicGear, multiEngine, noseWheelSteering, oxygen, passengerCabin, piston, propDeIce, retractableGear, turbocharged, weatherRadar | 99 (122) |
| es30.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, electricHybrid, fireProtection, fms, galley, glassCockpit, iceDetector, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, turboprop, weatherRadar, wipers | 112 (193) |
| flight-design-ct.json | autopilot, fixedGear, fixedPitchProp, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine | 78 (78) |
| nxcub.json | autopilot, constantSpeedProp, fixedGear, fuelInjected, glassCockpit, noseWheelSteering, piston, singleEngine | 80 (80) |
| pc-12-ngx.json | autopilot, bleedAir, constantSpeedProp, deIceBoots, fadec, fireProtection, fms, glassCockpit, hydraulicGear, iceDetector, lavatory, noseWheelSteering, oxygen, passengerCabin, pressurized, propDeIce, retractableGear, singleEngine, turboprop, weatherRadar, wipers | 113 (113) |
| pilatus-pc-24.json | antiskid, apu, autopilot, bleedAir, bleedAntiIce, fadec, fireProtection, fms, galley, glassCockpit, hydraulicGear, iceDetector, jet, lavatory, multiEngine, noseWheelSteering, oxygen, passengerCabin, pressurized, rat, retractableGear, spoilers, weatherRadar, wipers | 121 (147) |
| piper-j-3-cub.json | carbureted, fixedGear, fixedPitchProp, noElectrical, noFlaps, piston, singleEngine, tailwheel | 54 (54) |
| piper-pa28-236-dakota.json | autopilot, carbureted, constantSpeedProp, fixedGear, noseWheelSteering, passengerCabin, piston, singleEngine | 79 (79) |
| robin-dr400.json | carbureted, fixedGear, fixedPitchProp, noseWheelSteering, passengerCabin, piston, singleEngine | 75 (75) |

## Pontos para revisar

- **O Hangar ainda não lista peças:** hoje ele mostra só as 9 barras de componentes; as peças entram na média de cada barra (`min×0,7 + média×0,3`). Uma tela de peças por grupo seria uma tarefa de UI à parte.
- **Vidas e preços** do grupo Motor foram calibrados pelo usuário em 2026-11-17 (`lifeSource: "calibração do usuário 2026-11-17"`); os demais grupos mantêm estimativas (`lifeSource: "estimativa — revisar"`) aguardando calibrações subsequentes do usuário.
