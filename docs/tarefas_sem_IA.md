**Aqui está a tabela do sistema Motor (18) calibrada para o FlightCareer:**



**Base de dados: Valores reais da aviação (TBOs de fábrica da Lycoming/Continental, limites de LLP de turbinas PT6A/CFM56/LEAP e custos médios de overhaul / reposição em USD).**



**Regra de escala (÷ 4): Aplicada tanto na Vida Útil (horas e ciclos) quanto nos Preços base (mantendo o multiplicador × de porte/classe da aeronave para evitar inflação excessiva no início da carreira com aviões a pistão).**



**Correções técnicas aplicadas: Ajuste de aplicação do filtro de admissão (apenas pistão) e correção dos status de despacho (NO-GO / MEL) para coxins, carenagem, partida pneumática e reversor.**



**idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarengine.cylindersCilindros e pistões /mottodas de piston500 h (2000 h)1.600 × (6.400)168–36simnãoPerda de potência ou falha mecânica de motorengine.crankshaftVirabrequim e mancais /mottodas de piston500 h (2000 h)2.100 × (8.400)248–36simnãoPerda de potência ou falha mecânica de motorengine.valvesVálvulas de admissão e escape /mottodas de piston500 h (2000 h)600 × (2.400)108–36simnãoPerda de potência ou falha mecânica de motorengine.carburetorCarburador aeronáutico /mottodas de piston, carbureted450 h (1800 h)450 × (1.800)48–36simnãoPerda de potência ou falha mecânica de motorengine.fuel\_injectorsBicos injetores e distribuidor /mottodas de piston, fuelInjected500 h (2000 h)650 × (2.600)58–36simnãoPerda de potência ou falha mecânica de motorengine.turbochargerTurbocompressor, wastegate e controladortodas de piston, turbocharged450 h (1800 h)1.400 × (5.600)88–36simnãoPerda de potência ou falha mecânica de motorengine.starterMotor de partida elétrico /mottodas de piston500 h (2000 h)300 (1.200)2.54–12simnãoFalha de partida elétrica / recusa de acionamentoengine.coolant\_systemRadiador e arrefecimento líquido /mottodas de piston, diesel500 h (2000 h)700 × (2.800)68–24simsimSuperaquecimento / perda de potência de motorengine.compressorCompressor axial / centrífugo da turbinaqualquer de turboprop, jet3.750 h (15000 h)45.000 × (180.000)2012–48simnãoPerda de potência ou falha mecânica de motorengine.reduction\_gearboxCaixa de redução da hélice (RGB) /mottodas de turboprop2.500 h (10000 h)22.500 × (90.000)1412–36simnãoPerda de potência ou falha mecânica de motorengine.fanFan frontal do turbofan /mottodas de jet5.000 h · 3.750 cic (20k h · 15k cic)65.000 × (260.000)1612–48simsimVibração severa / perda de potência de motorengine.thrust\_reverserConjunto do reversor de empuxo /mottodas de jet3.000 cic (12000 cic)28.000 × (112.000)108–24nãosimReversor inoperante (MEL: penalidade em pista molhada)engine.mounts\_trussesCoxins e berço estrutural do motor /mottodas3.000 h (12000 h)3.500 × (14.000)148–24simnãoVibração excessiva e risco estrutural de fixaçãoengine.cowling\_nacelleCarenagem e nacele aerodinâmica /mottodas4.000 h (16000 h)4.500 × (18.000)86–18simnãoTrinca em painel de carenagem / arrasto parasitaengine.air\_intake\_filterFiltro de admissão de ar do motor /mottodas de piston125 h (500 h)75 (300)11–2nãonãoObstrução do fluxo de admissão / perda de potênciaengine.pneumatic\_starterMotor de partida pneumático (ATS) /mottodas de jet2.500 cic (10000 cic)6.500 × (26.000)63–8simnãoFalha de partida pneumática / recusa de acionamentoengine.fadec\_eecFADEC / Módulo eletrônico (EEC) /mottodas de fadec6.250 h (25000 h)25.000 × (100.000)64–12simnãoDegradação para modo manual / flutuação de empuxoengine.apu\_coreMódulo gerador de gás e turbina da APUtodas de apu2.500 h (10000 h)30.000 × (120.000)168–24nãonãoFalha na APU (requer GPU externa para partida/elétrica)**



**Notas de Balanceamento para o Código:**

**Piso mínimo aplicado no filtro (engine.air\_intake\_filter): Na vida real, um filtro de ar de Cessna/Piper é trocado a cada 500 h (ou limpo a cada 100 h) e custa entre US$ 150 e US$ 350. Dividido por 4, ficou em 125 h e US$ 75, servindo como um consumível recorrente bem realista.**



**Valores entre parênteses: Mantive a referência real (valor real) ao lado do valor dividido por 4 para facilitar caso você queira documentar a fórmula no JSON/banco de dados do projeto.**



**Multiplicador ×: Como o preço base já foi dividido por 4, certifique-se de que o multiplicador da aeronave (×) no cálculo final não reduza os jatos a valores irreais (ex.: se um jato comercial tiver multiplicador 4x ou 5x, o custo final de um Fan será US$ 260k–325k, o que fica ótimo para a economia do simulador).**



**aqui está a análise e a tabela corrigida para o sistema de Óleo (7), seguindo a regra de base real da aviação dividida por 4 (com aplicação de piso mínimo para consumíveis de ciclo curto, para evitar que o jogador troque óleo a cada 2 voos no Cessna).Auditoria Técnica \& Ajustes Aplicados:Consumíveis de Pistão (oil.lubricant e oil.filter): Na vida real, troca-se óleo e filtro de pistão (AeroShell W100 / Tempest) a cada 50 h (ou 25 h se não tiver filtro) ou 4 a 6 meses (não 12 m). Dividindo 50 h por 4 daria 12,5 h — o que tornaria o jogo cansativo. Aplicamos um piso mínimo de 25 h · 3 m e ajustamos o preço real (8 a 12 quarts de óleo \~US$ 120 ÷ 4 = US$ 30; filtro de óleo \~US$ 40 ÷ 4 = US$ 15).Óleo Sintético e Filtro de Turbina (oil.turbine\_oil e oil.turbine\_filter): Na aviação real (Mobil Jet Oil II / Eastman 2380), motores a turbina operam em sistema fechado (on-condition) e só completam o nível; o filtro de óleo da turbina e inspeção de detector de limalha (chip detector) ocorrem nas inspeções de 600 h a 1.000 h (ou troca anual). Divididos por 4, ficam em 250 h · 6 m e 200 h.Radiador de Óleo (oil.cooler): Estava marcado como NO-GO: não com efeito de vazamento de óleo. Na realidade, se o radiador de óleo vazar ou rachar, o motor perde pressão de óleo em minutos — é NO-GO: sim absoluto. Além disso, jatos (turbofans) também possuem radiador de óleo (Fuel-Cooled Oil Cooler - FCOC), mas mantive restrito a piston, turboprop conforme sua arquitetura atual, ajustando o custo base real (\~US$ 1.800 a US$ 3.500).Tempo de Parada (Hangar): Trocar bomba de óleo ou radiador de óleo leva mais do que 1–4 h (exige drenagem, desmontagem de acessórios e teste de pressão/vazamento em ponto fixo), ajustado para 4–12 h.Tabela Corrigida — Sistema de Óleo (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebraroil.lubricantÓleo lubrificante do motor (pistão) /mottodas de piston25 h · 3 m (50 h · 6 m — piso)35 (140)1.51–2simsimDegradação térmica / perda de lubrificaçãooil.turbine\_oilÓleo lubrificante sintético da turbina /motqualquer de turboprop, jet250 h · 6 m (1000 h · 24 m)110 (440)11–3simsimSuperaquecimento de mancais / perda de lubrificaçãooil.filterFiltro de óleo lubrificante (pistão) /mottodas de piston25 h (50 h — piso)15 (45)0.51–2simnãoBypass de impurezas / desgaste acelerado do motoroil.turbine\_filterFiltro de óleo lubrificante da turbina /motqualquer de turboprop, jet200 h (800 h)85 (340)11–3simnãoObstrução de fluxo / queda de pressão de óleooil.coolerRadiador de óleo /motqualquer de piston, turboprop1.250 h (5000 h)650 × (2.600)44–12simsimVazamento externo ou superaquecimento do óleooil.pumpBomba mecânica de óleo (pistão) /mottodas de piston500 h (2000 h)550 × (2.200)84–12simnãoPerda total de pressão de óleo do motoroil.turbine\_pumpBomba e módulo de lubrificação da turbina /motqualquer de turboprop, jet3.750 h (15000 h)4.500 × (18.000)106–16simnãoPerda de pressão de óleo e falha de rolamentos**



**Detalhe de Economia no Jogo:**

**Trocar óleo + filtro de um monomotor a pistão (ex: Cessna 172) agora custará US$ 50 em peças (+ MO), contra os US$ 225 anteriores, refletindo fielmente a realidade de operação da aviação geral sem punir o jogador no início da carreira.**



**Já o módulo de lubrificação de turbina (oil.turbine\_pump) subiu de preço base para US$ 4.500 × (contra US$ 5.500 ÷ 4 = US$ 1.375 que seria muito barato para uma turbina PT6/CFM56), acompanhando a durabilidade ampliada de 3.750 h.**



**essa tabela de Ignição (4) tem uma base muito boa, mas precisa de ajustes importantes nos preços das velas (especialmente se o avião usar velas de irídio ou de eletrodo massivo), na vida útil real dos magnetos e em um detalhe regulatório crítico sobre despacho (NO-GO).**



**Auditoria Técnica \& Ajustes Aplicados:**

**Magnetos (magneto\_left / magneto\_right):**



**Vida útil real: Na aviação real (Slick / Bendix), magnetos passam por inspeção interna obrigatória a cada 500 h (troca de platinado, condensador e lubrificação) e revisão geral (overhaul) junto com o motor em 1.800–2.000 h. Como o jogo trata a "Vida" como ciclo até troca/revisão completa, usamos a base real de 1.000 h (média entre inspeção profunda e overhaul) → dividido por 4 = 250 h.**



**Preço real: Um magneto novo ou factory rebuilt custa entre US$ 1.100 e US$ 1.500 cada. Dividido por 4 = US$ 325 cada.**



**Velas de Ignição (ignition.spark\_plugs):**



**Vida útil real: Velas aeronáuticas (Champion / Tempest) duram de 300 h a 500 h (eletrodo massivo) até 1.000 h+ (irídio/platina), sendo limpas e calibradas a cada 100 h. Usando 500 h como vida útil média real → dividido por 4 = 125 h.**



**Preço real: Um jogo de 8 a 12 velas aeronáuticas custa entre US$ 400 (eletrodo massivo, \~US$ 45/cada) e US$ 1.200 (irídio, \~US$ 110/cada). Usando a média de US$ 500 → dividido por 4 = US$ 125 (com multiplicador × opcional caso você queira diferenciar 4, 6 ou 8 cilindros, ou manter fixo em US$ 140).**



**Erro de NO-GO (spark\_plugs como não): Na aviação real, se o jogo de velas falhar (queda de RPM acima do limite no run-up antes da decolagem — normalmente > 125–175 RPM de queda ou > 50 RPM de diferença entre magnetos), a aeronave está terminantemente proibida de decolar (NO-GO: sim) por risco de perda de potência na decolagem ou detonação.**



**Velas Aquecedoras Diesel (ignition.glow\_plugs):**



**Vida útil real: Em motores AeroDiesel (Austro Engine AE300 / Thielert Centurion — como nos Diamond DA40 NG / DA42), as glow plugs duram cerca de 600 h a 800 h (inspecionadas a cada 300 h). Dividindo 600 h por 4 = 150 h.**



**Preço real: Um conjunto de 4 velas aquecedoras aeronáuticas custa em média US$ 320 → dividido por 4 = US$ 80.**



**Tabela Corrigida — Sistema de Ignição (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarignition.magneto\_leftMagneto esquerdo /mottodas de piston; exceto diesel250 h (1000 h)325 (1.300)2.52–6simnãoQueda de RPM / perda de redundância de igniçãoignition.magneto\_rightMagneto direito /mottodas de piston; exceto diesel250 h (1000 h)325 (1.300)2.52–6simnãoQueda de RPM / perda de redundância de igniçãoignition.spark\_plugsVelas de ignição (jogo) /mottodas de piston; exceto diesel125 h (500 h)140 (560)21–4simnãoFalha de ignição / funcionamento irregular (rough running)ignition.glow\_plugsVelas aquecedoras (glow plugs) /mottodas de piston, diesel150 h (600 h)80 (320)21–4simnãoDificuldade ou recusa de partida a frio (diesel)**







**Auditoria Técnica \& Ajustes Aplicados:**

**Câmara de Combustão (hotsection.combustor) e Palhetas da Turbina (hotsection.turbine\_blades):**



**Vida útil real: Em turboélices (PT6A) a inspeção de seção quente (HSI) ocorre por volta de 1.800–2.000 h, e em jatos comerciais (CFM56/LEAP) o módulo de turbina de alta pressão (HPT) dura entre 10.000 e 15.000 h (ou ciclos). Usando uma média equilibrada para turbinas em geral (8.000 h para câmara e 6.000 h para palhetas HPT/LPT) → dividido por 4 = 2.000 h e 1.500 h.**



**Preço real: Um conjunto de palhetas de turbina (turbine blades — que são dezenas de lâminas monocristalinas refrigeradas a ar) custa facilmente entre US$ 180.000 e US$ 400.000+ na vida real; uma câmara de combustão (combustor liner) custa entre US$ 120.000 e US$ 250.000. Aplicando ÷ 4, os preços base sobem de 28k/35k para 40.000 × e 60.000 ×.**



**Ignitores da Turbina (hotsection.igniters):**



**Vida útil e Preço: Velas ignitoras de turbina (Champion / Unison) são itens consumíveis trocados normalmente a cada 1.500 h a 2.000 h (dividido por 4 = 450 h) e custam entre US$ 600 e US$ 1.200 o par → dividido por 4 = US$ 250.**



**Parada e Efeito: A parada estava em 24–72 h com MO de apenas 2 h (incongruente!). Trocar ignitores é rápido, leva 2–4 h de parada. Além disso, o efeito de falha de um ignitor não é "degradação térmica severa", mas sim dificuldade de partida ou falha na reignição em voo (chuva/gelo).**



**NO-GO: Como turbinas têm 2 ignitores redundantes, via MEL muitas aeronaves podem despachar com 1 ignitor inoperante (desde que não haja condições de gelo/chuva forte na rota), ou exigir troca se ambos falharem. Mantive como não (MEL) ou sim caso represente o par inteiro falhando — deixei sim considerando a falha do conjunto, mas ajustei o efeito para falha de partida.**



**Bicos Injetores (hotsection.fuel\_nozzles) e NGV (hotsection.ngv):**



**NO-GO: Ambos estavam como NO-GO: não. Na aviação real, se os bicos injetores pulverizarem combustível de forma irregular (streaking), eles derretem as palhetas da turbina em minutos (hot start / overtemp). E as NGVs (Nozzle Guide Vanes) são o componente mais quente e crítico logo na saída da câmara; se estiverem trincadas/erodidas fora de limite, o motor é reprovado na boroscopia (NO-GO: sim).**



**Vida útil real dos Bicos: Em turbinas PT6/TPE331, os bicos injetores são retirados para limpeza e teste de fluxo a cada 400 h a 1.000 h (pois o coqueamento de querosene entope os orifícios). Usando 1.200 h como média real → dividido por 4 = 300 h.**



**Caixas Excitadoras (hotsection.ignition\_exciters):**



**Vida útil e NO-GO: São componentes eletrônicos selados de longa duração (\~10.000 h reais → 2.500 h no jogo). Como cada motor tem duas caixas excitadoras (A e B), se uma falhar, a aeronave geralmente pode voar via MEL (NO-GO: não, conforme sua tabela original — que está correta para falha simples!).**



**idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarhotsection.combustorCâmara de combustão /motqualquer de turboprop, jet2.000 h (8000 h)40.000 × (160.000)2024–72simnãoTrinca térmica / perda de empuxo e risco de fogohotsection.turbine\_bladesPalhetas da turbina /motqualquer de turboprop, jet1.500 h (6000 h)60.000 × (240.000)2524–72simnãoDegradação térmica severa / perda de rendimento e vibraçãohotsection.ignitersIgnitores da turbina (par) /motqualquer de turboprop, jet450 h (1800 h)250 (1.000)22–4simnãoFalha de centelha / recusa de partida da turbinahotsection.fuel\_nozzlesBicos injetores de combustível da turbina /motqualquer de turboprop, jet300 h (1200 h)4.500 × (18.000)64–10simnãoPulverização irregular / ponto quente (hot spot) na câmarahotsection.ngvPalhetas-guia de bocal (NGV) /motqualquer de turboprop, jet1.500 h (6000 h)35.000 × (140.000)1816–48simnãoErosão térmica / queda de EGT margin e rendimentohotsection.ignition\_excitersCaixas excitadoras de ignição da turbina /motqualquer de turboprop, jet2.500 h (10000 h)1.500 (6.000)32–6nãonãoPerda de redundância de ignição / falha de partida**



**Impacto no Balanceamento do FlightCareer:Os bicos injetores (300 h) e os ignitores (450 h) passam a funcionar como manutenções intermediárias periódicas para quem opera turboélices e jatos — exigindo que o jogador reserve caixa antes de chegar na revisão pesada das palhetas (1.500 h) e da câmara (2.000 h), exatamente como ocorre na aviação executiva e comercial real (inspeção de bicos vs. HSI completo).**



**Os principais ajustes aqui são o limite de calendário (meses) das hélices de passo variável — que na vida real têm TBO obrigatório por tempo (anos) mesmo se o avião voar pouco —, os preços reais (especialmente para hélices constant speed e composto/turboélice) e um ajuste no critério NO-GO do cubo/embandeiramento.Auditoria Técnica \& Ajustes Aplicados:Pás da Hélice (propeller.blades):Vida útil real: Hélices de passo fixo (McCauley / Sensenich) seguem o TBO do motor (2.000 h, sem limite rígido de calendário na aviação privada). Já hélices de passo variável (Hartzell / McCauley) têm TBO de 2.000 a 2.400 h ou 6 anos (72 meses). Usando a base real de 2.000 h · 72 m → dividido por 4 = 500 h · 18 m.Preço real: Uma hélice bipá de passo fixo custa \~US$ 4.500–6.000, enquanto uma tripá constant speed custa US$ 12.000–18.000 (e de turboélice passa de US$ 45.000). Usando base real de US$ 8.000 × → dividido por 4 = 2.000 × (com o multiplicador × ajustando bem do Cessna 172 ao King Air/ATR).Efeito: Uma pá de passo fixo não tem "perda de controle de passo", então ajustei o efeito para vibração severa, perda de tração ou trinca/delaminação.Governador de Hélice (propeller.governor):Vida útil real: Na aviação real (Woodward / McCauley / Jihostroj), o governador passa por overhaul junto com o motor/hélice em 2.000 h → dividido por 4 = 500 h.Preço real: Um governador revisado ou novo custa entre US$ 2.200 e US$ 3.600 (e até US$ 6.000+ em turboélices com overspeed governor). Usando base real de US$ 2.800 × → dividido por 4 = 700 ×.Mecanismo de Passo Variável e Embandeiramento (propeller.pitch\_feathering):Vida útil real: O cubo (hub), pistão hidráulico, contrapesos e molas de embandeiramento têm o mesmo TBO das pás: 2.000 a 2.400 h · 72 m (não duram mais que as pás porque são revisados juntos no prop overhaul). Dividido por 4 = 550 h · 18 m.Preço real: Revisão geral (overhaul) ou troca do conjunto do cubo/atuador custa entre US$ 6.000 e US$ 16.000 (chegando a US$ 35.000 em turboélices). Usando base real de US$ 10.000 × → dividido por 4 = 2.500 ×.NO-GO: Estava marcado como não. Na vida real, se o mecanismo de controle de passo falhar ou vazar óleo/nitrogênio pelo cubo, a aeronave sofre disparo de hélice (overspeed) ou não consegue embandeirar um motor em pane — é NO-GO: sim.Botas e Anéis de Degelo Elétrico (propeller.deice\_boots):Vida útil real: Botas de borracha coladas no bordo de ataque das pás sofrem erosão por chuva e pedras; duram em média 1.500 a 2.000 h · 48 meses (4 anos). Dividido por 4 = 450 h · 12 m.Preço real: O kit de botas de degelo + anéis coletores (slip rings) e escovas (brush block) custa entre US$ 2.400 e US$ 4.800 → dividido por 4 = 800.NO-GO e Efeito: Estava sem descrição de efeito no final da linha. Adicionei o efeito (Falha de aquecimento / acúmulo assimétrico de gelo nas pás). Via MEL, o avião pode voar com degelo de hélice inoperante desde que não entre em condições de gelo (VFR/IMC sem gelo), então NO-GO: não está perfeito!Tabela Corrigida — Sistema de Hélice (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarpropeller.bladesPás da hélice /motqualquer de fixedPitchProp, constantSpeedProp500 h · 18 m (2000 h · 72 m)2.000 × (8.000)46–18simsimVibração severa de hélice / perda de traçãopropeller.governorGovernador de hélice /mottodas de constantSpeedProp500 h (2000 h)700 × (2.800)44–12simnãoDisparo de RPM (overspeed) / oscilação de passopropeller.pitch\_featheringMecanismo de passo variável e embandeiramento /mottodas de constantSpeedProp550 h · 18 m (2200 h · 72 m)2.500 × (10.000)66–18simnãoPerda de controle de passo ou falha ao embandeirarpropeller.deice\_bootsBotas e anéis de degelo elétrico da hélice /mottodas de propDeIce450 h · 12 m (1800 h · 48 m)800 (3.200)42–6nãonãoFalha de degelo / vibração por acúmulo assimétrico de gelo**



**Os principais ajustes aqui envolvem:Diferenciação entre Starter-Generator (Turboélice/Jato leve) e IDG (Jatos comerciais): Um Starter-Generator tem escovas de carvão que desgastam rápido (\~1.000 h reais), enquanto um IDG (Integrated Drive Generator refrigerado a óleo) dura 15.000–20.000 h reais e custa uma fortuna (US$ 120.000+ na vida real).Critérios NO-GO (MEL): Na aviação real, um bimotor/jato com 1 IDG ou 1 TRU inoperante pode despachar via MEL (usando o gerador da APU no lugar do IDG, por exemplo), enquanto uma RAT (Ram Air Turbine) inoperante é NO-GO: sim na quase totalidade dos manuais de despacho ETOPS/IFR comerciais.Baterias (Lead-Acid vs. Ni-Cd / Lítio): Baterias aeronáuticas reais (Gill / Concorde / Saft) duram de 24 a 36 meses (ou \~1.200–1.500 h). Dividindo por 4, aplicamos 350 h · 6 m para a principal e 400 h · 6 m para a standby (com preços reais de baterias seladas/Ni-Cd).Completar a última linha (electrical.circuit\_breakers\_relays): Estava faltando a coluna WA e o Efeito ao quebrar.Tabela Corrigida — Sistema Elétrico (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarelectrical.batteryBateria principal de bordotodas350 h · 6 m (1400 h · 24 m)220 × (880)1.51–3simnãoPerda de energia de partida e reserva DCelectrical.alternatorAlternador do motor (pistão) /mottodas de piston500 h (2000 h)350 (1.400)32–6simnãoPerda de geração / descarga da bateria em vooelectrical.starter\_generatorPartida-gerador da turbina (Starter-Generator) /motqualquer de turboprop, jet300 h (1200 h)2.200 × (8.800)42–8simnãoFalha na partida ou perda de geração DC do motorelectrical.voltage\_regulatorRegulador de tensão / GCUtodas1.250 h (5000 h)450 × (1.800)22–6simnãoSobretensão (overvoltage) ou desarme do geradorelectrical.wiring\_harnessChicote e barramentos elétricostodas5.000 h (20000 h)1.800 × (7.200)128–24simnãoCurto-circuito / falha intermitente de barramentoelectrical.apu\_generatorGeratriz da APUtodas de apu2.500 h (10000 h)7.500 × (30.000)84–12nãonãoSem geração elétrica da APU (requer GPU externa)electrical.inverter\_converterInversores e conversores estáticosqualquer de turboprop, jet2.000 h (8000 h)1.500 × (6.000)64–12simnãoPerda do barramento AC essencial / instrumentos IFRelectrical.standby\_batteryBateria auxiliar / standby de emergênciaqualquer de glassCockpit, turboprop, jet400 h · 6 m (1600 h · 24 m)380 (1.520)21–2simnãoIncapacidade de suprir barramento essencial em paneelectrical.idgGerador de acionamento integrado (IDG) /mottodas de jet4.000 h (16000 h)30.000 × (120.000)84–12nãonãoFalha de geração AC principal / desconexão do IDGelectrical.ratTurbina de ar de impacto (RAT)todas de rat6.250 h (25000 h)22.000 × (88.000)86–16simnãoFalha de desdobramento ou geração de emergênciaelectrical.tru\_transformer\_rectifierUnidade transformadora e retificadora (TRU)qualquer de turboprop, jet3.750 h (15000 h)3.500 (14.000)43–8nãonãoPerda de conversão AC/DC e isolamento de barramentoelectrical.external\_lightingSistema de iluminação externa (pouso/strobes/nav)todas375 h (1500 h)250 (1.000)21–2nãonãoLuzes externas inoperantes (restrição para voo noturno)electrical.circuit\_breakers\_relaysPainel de disjuntores, contatores e reléstodas4.500 h (18000 h)1.200 × (4.800)64–16simnãoFalha de comutação / desarme de barramentos principais**



**idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarelectrical.circuit\_breakers\_relaysPainel de disjuntores, contatores e relés de barramentotodas4.500 h (18000 h)1.200 × (4.800)64–16nãonãoDesarme espúrio ou falha de isolamento de barramento elétrico**



**essa tabela de Instrumentos (4) é clássica da aviação geral (steam gauges / sistema Pitot-Estático e Vácuo). A estrutura está ótima, mas vale uma atenção especial na aplicação (já que aviões modernos Glass Cockpit ou jatos não usam bomba de vácuo nem giroscópio mecânico tradicional — usam AHRS/ADIRU, que costumam ficar na categoria Aviônicos) e na vida útil real das bombas de vácuo secas (Rapco/Tempest), que são famosas na aviação geral por terem aletas de grafite que quebram relativamente cedo.**



**Auditoria Técnica \& Ajustes Aplicados:**

**Bomba de Vácuo Mecânica (instruments.vacuum\_pump):**



**Vida útil real: As bombas de vácuo secas (Rapco / Tempest) têm palhetas de carbono que desgastam continuamente; na vida real duram entre 500 h e 800 h (muitas oficinas recomendam troca preventiva a cada 500–600 h porque a falha é súbita). Usando 600 h como média real → dividido por 4 = 150 h.**



**Preço real: Uma bomba de vácuo nova custa entre US$ 550 e US$ 900 (média US$ 700) → dividido por 4 = US$ 175.**



**NO-GO: Como não está bem calibrado para voo VFR diurno (via MEL/KOEL de Cessna 172/Piper, você pode decolar VFR dia sem bomba de vácuo colocando etiqueta "INOP", embora seja proibida para IFR).**



**Tubo de Pitot com Aquecimento (instruments.pitot\_tube):**



**Vida útil real: O tubo metálico dura a vida da aeronave, mas a resistência elétrica interna do aquecimento (Pitot Heat) queima com o uso (especialmente se esquecida ligada no solo). Dura em média 3.000 h a 4.000 h → dividido por 4 = 900 h.**



**Preço real: Um tubo de Pitot aquecido de aviação geral (Aero Instruments / Dynon / Piper) custa cerca de US$ 600–900, enquanto em turboélices e jatos (Rosemount / Goodrich SmartProbe) custa de US$ 3.500 a US$ 12.000+. Usando base real de US$ 1.200 × → dividido por 4 = 300 × (o multiplicador × ajusta perfeitamente do Cessna ao A320!).**



**Tomadas e Linhas Estáticas (instruments.static\_ports):**



**Vida útil real: As tomadas estáticas na fuselagem e as mangueiras internas (com drenos de umidade) duram cerca de 8.000 h antes de precisarem de substituição de vedações/linhas no teste bienal de Pitot-Estático (FAR 91.411) → dividido por 4 = 2.000 h.**



**Preço real: Kit de tomadas estáticas (aquecidas em jatos/turboélices ou simples em pistão) + linhas e drenos custa em média US$ 800 × → dividido por 4 = 200 ×.**



**Giroscópio / Horizonte de Atitude (instruments.gyro\_horizon):**



**Aplica-se a: Estava em todas. Na vida real, um giroscópio mecânico (a vácuo ou elétrico) só existe em aviões analógicos ou como instrumento standby (como o Mid-Continent Lifesaver ou L3 ESI-500 em jatos/glass cockpit). Mantive todas (funcionando como horizonte principal nos analógicos e horizonte standby nos modernos!).**



**Vida útil real: Um horizonte artificial mecânico (Sigma-Tek / RCA) dura cerca de 1.500 h a 2.500 h antes de os rolamentos do rotor perderem precisão ou precessarem → usando 2.000 h reais, dividido por 4 = 500 h.**



**Preço real: Um horizonte mecânico revisado custa \~US$ 1.600 (e um standby elétrico certificado custa US$ 4.500–8.000). Usando base real de US$ 2.400 × → dividido por 4 = 600 ×.**



**Tabela Corrigida — Sistema de Instrumentos (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarinstruments.vacuum\_pumpBomba de vácuo mecânicatodas de piston150 h (600 h)175 (700)32–6nãonãoPerda de sucção / tombamento dos giros a vácuo (AI e DG)instruments.pitot\_tubeTubo de pitot com aquecimentotodas900 h (3600 h)300 × (1.200)22–6simsimFalha do aquecimento ou obstrução / leitura errônea de IASinstruments.static\_portsTomadas e linhas estáticastodas2.000 h (8000 h)200 × (800)22–6simsimErro de altitude (altímetro), VSI e velocidade indicadainstruments.gyro\_horizonGiroscópio / horizonte de atitudetodas500 h (2000 h)600 × (2.400)32–6simnãoPrecessão excessiva ou tombamento do horizonte artificial**





**essa tabela de Combustível (9) está bem estruturada — especialmente pela inclusão do FOHE (Fuel-Oil Heat Exchanger) e da Fire Shutoff Valve, que dão muita profundidade operacional para o FlightCareer.Os principais ajustes aqui envolvem o custo real de componentes críticos de turbina (como a FCU/HMU, que custa dezenas de milhares de dólares na vida real), o ajuste regulatório de despacho (NO-GO) para a válvula de corte de fogo e o FOHE, e o escalonamento (×) para itens que variam drasticamente de um Cessna 152 para um Boeing 747/A330 (como o FQIS e as bombas auxiliares).Auditoria Técnica \& Ajustes Aplicados:Bomba Mecânica de Combustível (fuel.engine\_pump):Vida útil e Preço: Na aviação geral (Romec / Weldon / AC), a bomba mecânica acionada pelo motor tem o mesmo TBO do motor (2.000 h → dividido por 4 = 500 h) e custa entre US$ 900 e US$ 1.600 (média US$ 1.200 → dividido por 4 = US$ 300).Unidade de Controle de Combustível (fuel.fcu - FCU / HMU):Vida útil real: Uma FCU hidromecânica (PT6A) ou HMU (Hydromechanical Unit, CFM56/LEAP) dura entre 6.000 h e 12.000 h antes de revisão geral (média 8.000 h → dividido por 4 = 2.000 h).Preço real: É um dos componentes mais caros fora do núcleo da turbina — custa entre US$ 25.000 (turboélice) e US$ 120.000+ (jato comercial). Usando base real de US$ 32.000 × → dividido por 4 = 8.000 × (o valor de 8.500 × da sua tabela original já estava praticamente cravado no valor dividido por 4!).Bomba Elétrica Auxiliar / Boost Pump (fuel.aux\_pump):Vida útil e Preço: Em aviões leves (Weldon / Dukes) dura \~2.000 h e custa \~US$ 1.000; em jatos comerciais (Fuel Boost Pumps imersas nos tanques) dura \~10.000 h e custa \~US$ 8.000+. Usando média ponderada com multiplicador: 800 h (3.200 h real) e 350 × (1.400 real).Filtro e Dreno / Gascolator (fuel.filter\_gascolator):Vida útil e Preço: Na vida real, o elemento filtrante do gascolator (ou filtro de linha) é limpo/trocado a cada 100 h (inspeção anual/100h) e custa entre US$ 40 e US$ 120. Aplicando piso mínimo de jogo: 100 h (400 h real) e 30 (120 real).Válvula de Corte Emergencial de Combustível (fuel.fire\_shutoff\_valve):NO-GO: Estava marcada como NO-GO: não. Na aviação real, se a Firewall Shutoff Valve (pistão) ou Engine LP/HP Fire Shutoff Valve (jato/turboélice) estiver inoperante, a aeronave não pode decolar (NO-GO: sim) por risco catastrófico em caso de incêndio no motor.Trocador de Calor Combustível/Óleo (fuel.fohe):NO-GO: Estava marcado como não. O FOHE aquece o querosene frio (evitando obstrução por gelo nos filtros de combustível, como no famoso acidente do BA 038) e resfria o óleo do motor simultaneamente. Se ele vazar (misturando óleo e combustível) ou entupir, é NO-GO: sim absoluto.Sensores de Quantidade e Computador (fuel.fqis\_sensors):Aplica-se a e Preço: Em aviões leves a pistão, a indicação usa boias resistivas simples (\~US$ 400 o conjunto); em jatos/turboélices usa sondas capacitivas + computador FQIS (\~US$ 18.000+). Adicionando o multiplicador × em 850 × (3.400 real), o custo escala perfeitamente entre um Cessna 152 e um A330!NO-GO: Na aviação real, é obrigatório ter indicação confiável da quantidade de combustível de cada tanque antes da decolagem (FAR 91.205 / MEL: se um canal do FQIS falhar em jato, às vezes permite dripstick manual com restrição, mas em geral falha no FQIS é NO-GO: sim).Tabela Corrigida — Sistema de Combustível (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarfuel.engine\_pumpBomba mecânica de combustível (pistão) /mottodas de piston500 h (2000 h)300 (1.200)43–8simnãoQueda de pressão de combustível / apagamento do motorfuel.fcuUnidade de controle de combustível (FCU / HMU) /motqualquer de turboprop, jet2.000 h (8000 h)8.000 × (32.000)86–18simnãoFlutuação de N1/Ng, hot start ou falha de alimentaçãofuel.aux\_pumpBomba elétrica auxiliar / boost pumptodas800 h (3200 h)350 × (1.400)2.52–6simnãoFalha de pressão auxiliar / cavitação em altitudefuel.filter\_gascolatorFiltro e dreno de combustível (gascolator)todas de piston100 h (400 h)30 (120)11–2simsimObstrução do fluxo de combustível / presença de águafuel.selector\_valveVálvula seletora de combustíveltodas1.500 h (6000 h)300 × (1.200)33–8simnãoTravamento na troca de tanques ou entrada falsa de arfuel.crossfeed\_valveVálvula de alimentação cruzada (Crossfeed)todas de multiEngine2.500 h (10000 h)1.400 × (5.600)43–8nãonãoVálvula travada aberta ou fechada (impede balanceamento)fuel.fire\_shutoff\_valveVálvula de corte emergencial de fogo /mottodas3.000 h (12000 h)1.100 × (4.400)43–8simnãoFalha de isolamento de combustível em emergência de fogofuel.foheTrocador de calor combustível/óleo (FOHE) /motqualquer de turboprop, jet3.000 h (12000 h)3.500 × (14.000)66–14simnãoCongelamento de combustível em altitude / contaminação óleo-fuelfuel.fqis\_sensorsSensores de quantidade e computador (FQIS)todas2.000 h (8000 h)850 × (3.400)42–6simnãoIndicação errática ou travada de combustível nos tanques**







**essa tabela de Trem (10) — cobrindo desde bequilha e trem triciclo até esquis (NXCub/PC-6) e flutuadores anfíbios (ICON A5 / Caravan Amphibian) — está com uma divisão conceitual excelente.**



**Os pontos que exigem calibração aqui são:**



**Unidade de desgaste do Trem de Pouso: Na aviação real, trem de pouso não se desgasta em voo de cruzeiro (horas), mas sim em ciclos (pousos/decolagens) e tempo calendárico (vedações O-ring / retentores de nitrogênio e fluido hidráulico ressecam com os anos). Por isso, os limites de ciclos devem ser o gargalo principal!**



**Preços reais de amortecedores e estruturas (Munhões): Em um jato comercial ou executivo, um conjunto de amortecedores principais (oleo struts) ou munhões (trunnions) custa dezenas a centenas de milhares de dólares (overhaul de trem de A320/737 passa de US$ 300k–500k). Ajustamos a base para escalar bem com o multiplicador ×.**



**Rolamentos de roda (gear.wheels\_bearings): Na vida real, rolamentos de roda são inspecionados, limpos e engraxados a cada troca de pneu (\~200 a 400 pousos) e substituídos por volta de 1.000 a 1.500 ciclos (dividido por 4 = 350 cic).**



**Direção do Trem Dianteiro (gear.nosewheel\_steering): Estava marcada como NO-GO: não e medida apenas em horas. Na aviação real, se o sistema hidráulico/mecânico de Nose Wheel Steering (NWS) falhar ou o shimmy damper estourar, a aeronave está proibida de decolar (NO-GO: sim), e seu desgaste ocorre nos ciclos de táxi/pouso.**



**Tabela Corrigida — Trem de Pouso (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrargear.main\_strutsAmortecedores do trem principaltodas2.500 cic · 30 m (10k cic · 120 m)2.200 × (8.800)66–16simsimVazamento de fluido/nitrogênio ou colapso no pousogear.nose\_strutAmortecedor e tesoura do trem dianteiroexceto tailwheel2.000 cic · 30 m (8k cic · 120 m)1.600 × (6.400)56–16simsimShimmy violento ou colapso do trem dianteirogear.tailwheel\_assemblyConjunto da bequilha de caudatodas de tailwheel750 cic (3000 cic)350 (1.400)34–10simsimDestravamento / shimmy de bequilha e perda de controle no sologear.actuatorAtuadores e travas do trem retrátiltodas de retractableGear2.500 cic (10000 cic)3.500 × (14.000)86–16simnãoFalha de recolhimento/baixamento ou travamento (downlock)gear.wheels\_bearingsRodas e rolamentos do trem de pousotodas350 cic (1400 cic)400 × (1.600)42–6simsimSuperaquecimento de rolamento ou travamento de roda no pousogear.trunnions\_side\_staysMunhões, tesouras e hastes estruturaistodas5.000 cic · 36 m (20k cic · 144 m)6.500 × (26.000)2424–72simsimTrinca por fadiga / colapso estrutural do trem de pousogear.nosewheel\_steeringDireção do trem dianteiro e shimmy dampertodas de noseWheelSteering1.500 cic (6000 cic)950 × (3.800)64–10simsimPerda de controle direcional no táxi ou vibração intensagear.doors\_mechanismsPortas do trem de pouso e mecanismostodas de retractableGear3.000 cic (12000 cic)1.100 × (4.400)84–12nãosimPorta do trem emperrada aberta causando arrasto parasitagear.skisEsquis de pouso para neve/gelotodas de skis600 cic · 15 m (2400 cic · 60 m)3.500 × (14.000)84–12simsimTrincas na prancha ou travamento do atuador hidráulico do esquigear.floatsFlutuadores anfíbios / hidroaviãotodas de floats1.000 cic · 18 m (4000 cic · 72 m)8.500 × (34.000)128–24simsimVazamento em compartimento estanque ou falha do trem anfíbio**





**Isso explica perfeitamente por que não estavam na tabela do Trem.**



**Na aviação real, tanto pneus quanto freios se desgastam exclusivamente por ciclos (pousos) — um Boeing 777 voando 14 horas de Guarulhos a Dubai gasta exatamente a mesma borracha e carbono no pouso que um Cessna 172 voando 30 minutos no circuito. Além disso, os pneus têm limite rígido de tempo calendárico (validade da borracha).**



**Auditoria Técnica \& Ajustes Aplicados:**

**Pneus (tires.main e tires.nose):**



**Vida útil real:**



**Na aviação geral (Goodyear Flight Custom III / Michelin Air), pneus duram entre 200 e 350 pousos (dependendo da pista e frenagem); em jatos comerciais duram entre 250 e 400 pousos (antes de irem para recauchutagem). Usando 300 ciclos (principal) e 240 ciclos (nariz, que gira mais rápido por ter diâmetro menor) + 48 meses de validade da borracha → dividido por 4 = 75 cic · 12 m e 60 cic · 12 m.**



**Aplica-se a (tires.nose): Estava exceto tailwheel, mas o nome diz "Pneu do trem dianteiro / bequilha". Se mudar para todas, ele cobre tanto o pneu do nariz (triciclo) quanto o pneu da bequilha (tailwheel) sem precisar criar um terceiro ID!**



**Preço real: Um par de pneus de Cessna 172 custa \~US$ 500–700 (US$ 150 ÷ 4), enquanto um jogo de pneus principais de jato comercial custa US$ 6.000–12.000+. Usando 180 × (720 real) para o jogo principal e 90 × (360 real) para o dianteiro/bequilha, o multiplicador escala perfeitamente.**



**Pastilhas e Discos de Freio (brakes.pads\_discs):**



**Vida útil real: Pastilhas orgânicas/metálicas (Cleveland / Parker) em aviões leves duram \~400–600 ciclos (discos duram o dobro); freios de carbono em jatos duram \~1.200–2.000 ciclos. Usando 600 ciclos como base média → dividido por 4 = 150 cic.**



**Preço real: Jogo de pastilhas + discos de aço (aviação geral) custa \~US$ 800–1.200, enquanto um conjunto de freios de carbono de jato custa US$ 25.000–50.000+ por roda. Usando 350 × (1.400 real).**



**NO-GO: Estava marcado como NO-GO: não. Na aviação real, se os freios estiverem abaixo do limite mínimo de espessura (wear indicator pin rente ou gasto) ou com perda de eficiência de frenagem, a aeronave está terminantemente proibida de decolar (NO-GO: sim).**



**Cilindros Mestre e Linhas de Freio (brakes.master\_cylinders):**



**Vida útil real: Cilindros mestre (pedais) e mangueiras hidráulicas de freio têm troca obrigatória de reparos/mangueiras (Aeroquip) a cada 5 a 7 anos (60–84 meses) ou \~3.000 ciclos → dividido por 4 = 750 cic · 18 m.**



**Preço real: Em aviões leves custa \~US$ 900, em jatos/turboélices com válvulas dosadoras (brake metering valves) custa US$ 4.000+. Adicionando o multiplicador ×: 280 × (1.120 real).**



**Sistema Antiskid e Autobrake (gear.antiskid\_system):**



**Vida útil real: Sensores de velocidade de roda (wheel speed transducers) e válvulas servo-controladoras de antiskid duram cerca de 6.000 ciclos → dividido por 4 = 1.500 cic.**



**Preço real: Um computador/válvula antiskid (Hydro-Aire / Crane) custa entre US$ 12.000 e US$ 35.000 na vida real → usando base real de US$ 16.000 × dividido por 4 = 4.000 ×.**



**NO-GO: Como não está 100% correto! Pela MEL de jatos comerciais e executivos, é permitido despachar com o Antiskid ou Autobrake inoperante aplicando penalidades rigorosas de peso máximo de pouso e proibição de pista molhada/contaminada.**



**Tabela Corrigida — Pneus e Freios (Real ÷ 4)Pneus (2)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrartires.mainPneus do trem principal (jogo)todas75 cic · 12 m (300 cic · 48 m)180 × (720)1.51–2simsimEstouro, desgaste no limite (lona exposta) ou perda de pressãotires.nosePneu do trem dianteiro / bequilhatodas60 cic · 12 m (240 cic · 48 m)90 × (360)11–2simsimEstouro, shimmy ou perda de pressão no pneu direcionalFreios (3)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarbrakes.pads\_discsPastilhas e discos de freio (jogo)todas150 cic (600 cic)350 × (1.400)2.52–4simsimDesgaste além do limite / perda de eficiência de frenagembrakes.master\_cylindersCilindros mestre e linhas de freiotodas750 cic · 18 m (3000 cic · 72 m)280 × (1.120)42–4simnãoVazamento de fluido hidráulico / falha de frenagem nos pedaisgear.antiskid\_systemSistema antiskid e autobraketodas de antiskid1.500 cic (6000 cic)4.000 × (16.000)64–8nãonãoInoperância do antiskid/autobrake (MEL: restrição de pista)**



**essa tabela da Asa (11) está muito bem detalhada — cobrindo desde botas pneumáticas (King Air / Baron / Caravan) até slats, spoilers e antigelo térmico por bleed air (jatos executivos e comerciais).**



**Os principais ajustes aqui envolvem:**



**Unidade de desgaste de superfícies hipersustentadoras (flaps, slats, spoilers): Na vida real, flaps e slats não se desgastam voando em cruzeiro (horas), mas sim nos ciclos de acionamento (decolagem e pouso). Além disso, flap travado ou com falha assimétrica (airframe.flaps) na aviação real é NO-GO: sim (ninguém pode decolar com flap inoperante fora de voos de traslado especiais sem passageiros).**



**Estruturas primárias (wing\_structure, root\_fittings, ribs\_skin): Estruturas de asa em alumínio ou fibra de carbono têm vida de projeto de 30.000 h a 60.000 h / ciclos na vida real (ex.: C-Check / D-Check e boletins de fadiga na raiz da asa). Dividindo por 4, ficam entre 7.500 h e 12.500 h, com custos reais ajustados para o tamanho de uma reparação estrutural alar.**



**Tanques integrais (Wet Wing) e selantes (wing.tanks\_sealant): O selante de polissulfeto (PRC) dentro das asas resseca e trinca principalmente pelo tempo (anos) e ciclos de pressurização/flexão da asa (a cada 10–12 anos / 120–144 meses ou 12.000 h). Dividido por 4 = 3.000 h · 30 m.**



**Pontas de asa e winglets (wing.tips\_winglets): Um winglet de jato moderno ou ponta de asa de fibra custa de US$ 15.000 a US$ 150.000+ na vida real; ajustamos o custo base para escalar melhor com ×.**



**Tabela Corrigida — Asa e Superfícies Alares (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarairframe.wing\_structureLongarinas e estrutura alartodas10.000 h (40000 h)8.500 × (34.000)4024–72simsimFadiga na longarina / interdição estrutural (AOG)airframe.flapsTrilhos e mecanismo dos flapstodas2.000 cic (8000 cic)1.200 × (4.800)88–24simsimAssimetria ou travamento de flap na decolagem/pousowing.root\_fittingsFerragens e pinos de fixação da raiz da asatodas12.500 h · 12.500 cic (50k h · 50k cic)4.500 × (18.000)4036–96simnãoTrinca de fadiga na junção asa-fuselagem / AOGwing.ribs\_skinNervuras e revestimento estrutural alartodas8.750 h (35000 h)2.500 × (10.000)2824–48simsimDeformação do perfil alar / aumento de arrasto parasitawing.tanks\_sealantTanques integrais (wet wing) e selantestodas3.000 h · 30 m (12k h · 120 m)1.800 × (7.200)2016–40simsimVazamento de combustível alar através do selantewing.tips\_wingletsPontas de asa e wingletstodas7.500 h (30000 h)1.500 × (6.000)108–24nãosimDano na ponta de asa / aumento de arrasto induzidowing.aileronsAilerons e atuadores de rolamentotodas4.500 h (18000 h)1.800 × (7.200)1612–30simsimResposta assimétrica ou travamento do controle lateralwing.slatsSlats de bordo de ataque e mecanismostodas de slats2.500 cic (10000 cic)3.200 × (12.800)2216–40simsimAssimetria ou travamento de slat no bordo de ataquewing.spoilersSpoilers e speedbrakestodas de spoilers2.000 cic (8000 cic)2.200 × (8.800)148–24nãosimSpoiler travado ou falha de atuação (MEL: penalidade de pouso)wing.deice\_bootsBotas pneumáticas de degelo da asatodas de deIceBoots600 h · 12 m (2400 h · 48 m)1.400 × (5.600)1212–24nãosimPerfuração ou ressecamento da bota (restrição para gelo)wing.bleed\_anti\_iceVálvulas e dutos de antigelo térmico (Bleed Air)todas de bleedAntiIce2.500 h (10000 h)2.600 × (10.400)1816–36nãonãoVálvula de sangria travada / inoperância do antigelo alar**







**essa tabela da Empenagem (7) está bem dividida — separando a estrutura fixa (estabilizadores), as superfícies móveis (profundor e leme), a transmissão (cabos/polias/atuadores), os pontos de articulação (hinges/counterweights) e o fuso do compensador (trim jackscrew).**



**Aqui estão os ajustes técnicos e de calibração (Real ÷ 4) para deixá-la fiel à engenharia aeronáutica:**



**Auditoria Técnica \& Ajustes Aplicados:**

**Redundância Conceitual entre airframe.flight\_controls e empennage.control\_cables:**



**Na tabela original, airframe.flight\_controls ("Comandos de voo e cabos de acionamento") ficou dentro da Empenagem e tem sobreposição com empennage.control\_cables ("Cabos e atuadores de comando da empenagem"). Para fazer sentido mecânico sem redundância, ajustamos airframe.flight\_controls para representar a coluna de comando, pedais, polias e quadrantes primários da cabine/fuselagem, enquanto empennage.control\_cables representa os cabos, tensores e PCUs (atuadores hidráulicos de cauda).**



**Critérios NO-GO Críticos (hinges\_counterweights e trim\_actuator):**



**Dobradiças, rolamentos e contrapesos (empennage.hinges\_counterweights): Estava marcado como NO-GO: não com o efeito "risco de flutter em alta velocidade". Na aviação real, folga nos rolamentos de dobradiça ou dano nos contrapesos de balanceamento aerodinâmico da cauda causa flutter destrutivo em segundos — é NO-GO: sim absoluto (verificado inclusive no Walkaround).**



**Atuador e fuso do compensador (empennage.trim\_actuator): Estava como NO-GO: não. Se o trim jackscrew (como o famoso sistema do MD-80/737/Cessna) estiver desgastado ou com o compensador travado, nenhuma aeronave é autorizada a decolar (NO-GO: sim), pois o trim é primário para o controle de arfagem e segurança de decolagem.**



**Vida Útil Real (Fadiga Estrutural vs. Componentes Mecânicos):**



**Estabilizadores (empennage.stabilizers): Estrutura primária de cauda dura toda a vida útil econômica da célula (40.000 h a 60.000 h reais) → dividido por 4 = 12.500 h.**



**Profundor e Leme (elevator / rudder): Superfícies de controle de cauda duram cerca de 24.000 h antes de precisarem de reconstrução/re-skinning por fadiga acústica ou corrosão → dividido por 4 = 6.000 h.**



**Fuso do Compensador (trim\_actuator): Fusos de esferas (ballscrews) ou rosca trapezoidal de trim têm inspeção de folga (end-play check) a cada \~600–1.000 h e vida útil média de 6.000 h → dividido por 4 = 1.500 h.**



**Tabela Corrigida — Empenagem (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarairframe.flight\_controlsColuna de comando, pedais e polias primáriastodas3.000 h (12000 h)1.400 × (5.600)128–24simsimFolga ou travamento nos comandos primários de cabineempennage.stabilizersEstabilizadores horizontal e verticaltodas12.500 h (50000 h)4.000 × (16.000)3248–120simsimTrinca estrutural nos estabilizadores / interdição (AOG)empennage.elevatorProfundor e comandos de arfagemtodas6.000 h (24000 h)1.800 × (7.200)1412–36simsimProfundor travado ou excursão limitada de arfagemempennage.rudderLeme de direçãotodas6.000 h (24000 h)1.600 × (6.400)1412–36simsimDeformação do leme ou resposta assimétrica de guinadaempennage.control\_cablesCabos, tensores e atuadores da empenagemtodas2.500 h · 2.500 cic (10k h · 10k cic)950 × (3.800)126–18simnãoPerda de tensão nos cabos / resposta lenta da caudaempennage.hinges\_counterweightsDobradiças, rolamentos e contrapesos da caudatodas1.500 h (6000 h)550 × (2.200)64–8simsimFolga excessiva nos mancais / risco de flutter em vooempennage.trim\_actuatorAtuador e fuso do compensador (trim jackscrew)todas1.500 h (6000 h)1.200 × (4.800)64–12simsimCompensador travado ou movimento descontrolado (runaway)**



**Detalhe de Economia \& Gameplay:**

**Com hinges\_counterweights (1.500 h) e trim\_actuator (1.500 h) tendo desgaste mais rápido e flag WA: sim (Walkaround), o jogador ganha um motivo real para fazer a inspeção pré-voo na cauda (checar folga do profundor/leme e posição do trim), enquanto os estabilizadores (12.500 h) funcionam como ativo estrutural de longo prazo da aeronave.**







**essa tabela do sistema Hidráulico (6) está muito bem montada — com destaque para a inclusão da PTU (Power Transfer Unit, icônica no A320/737) e das bombas elétricas auxiliares (ACMP).Os principais ajustes aqui envolvem:Unidade e Vida Útil Real:Bombas Hidráulicas (hydraulic.pump - EDP e hydraulic.electric\_pump - ACMP): Na aviação real (Parker / Eaton / Vickers), uma bomba hidráulica acionada pelo motor (Engine Driven Pump - EDP) roda continuamente durante todo o voo e dura entre 6.000 h e 10.000 h antes de revisão geral (média 8.000 h → dividido por 4 = 2.000 h). Já a bomba elétrica (ACMP) roda sob demanda ou em solo/decolagem/pouso, durando cerca de 10.000 h reais (dividido por 4 = 2.500 h).PTU (hydraulic.ptu): Só entra em ação quando há diferencial de pressão > 500 PSI entre os sistemas (ex.: durante o táxi monomotor ou falha de uma EDP), então sua vida útil real é altíssima (15.000 h → dividido por 4 = 3.750 h).Filtros Hidráulicos (hydraulic.filters\_valves): Na aviação real, os cartuchos dos filtros de pressão e retorno (case drain filters) têm indicadores pop-out e são trocados a cada 1.000 h a 1.500 h (média 1.200 h → dividido por 4 = 300 h) para evitar contaminação do fluido Skydrol/MIL-PRF-5606.Mangueiras e Vedações (hydraulic.lines\_fittings): Mangueiras flexíveis de teflon/borracha e O-rings têm validade calendárica rígida na aviação (5 a 8 anos / 60–96 meses ou 8.000 h) → dividido por 4 = 2.000 h · 18 m.Preços Reais (Escalonamento do Cessna 210/Bonanza até o A320/737):Uma bomba hidráulica de trem retrátil de pistão (Power Pack Cessna/Piper) custa \~US$ 2.500, enquanto uma EDP de Airbus/Boeing custa US$ 35.000–65.000, e uma PTU custa US$ 60.000–110.000. Ajustando as bases para escalar bem com o multiplicador ×, e adicionando × nos acumuladores e filtros (já que um filtro hidráulico de Cessna custa US$ 60 e um kit de filtros de A320 custa US$ 2.500).Critérios NO-GO (MEL):Acumuladores (hydraulic.accumulators): Estava como NO-GO: não. Na aviação real, se o acumulador do freio de emergência/estacionamento (Brake Accumulator) estiver sem pré-carga de nitrogênio ou furado, a aeronave está terminantemente proibida de decolar (NO-GO: sim) por ficar sem freio de estacionamento e sem frenagem de emergência em caso de pane elétrica/hidráulica!ACMP e PTU como NO-GO: não: Estão 100% fiéis à MEL real! Tanto o Airbus A320 quanto o Boeing 737 podem despachar via MEL com 1 bomba elétrica (ACMP) inoperante ou com a PTU inoperante (aplicando restrições operacionais como proibição de táxi monomotor).Tabela Corrigida — Sistema Hidráulico (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarhydraulic.pumpBomba hidráulica principal (EDP / Power Pack)todas de hydraulicGear2.000 h (8000 h)2.500 × (10.000)64–12simnãoPerda de pressão no sistema hidráulico principalhydraulic.electric\_pumpBomba hidráulica elétrica auxiliar (ACMP)qualquer de hydraulicGear, turboprop, jet2.500 h (10000 h)4.500 × (18.000)64–10nãonãoPerda da bomba hidráulica auxiliar de reserva e solohydraulic.accumulatorsAcumuladores hidráulicos de pressão e freioqualquer de hydraulicGear, jet2.500 h · 24 m (10k h · 96 m)1.200 × (4.800)43–8simnãoPerda de reserva de pressão para freios de emergência/parkinghydraulic.filters\_valvesFiltros hidráulicos e válvulas seletorasqualquer de hydraulicGear, turboprop, jet300 h (1200 h)250 × (1.000)32–4nãonãoFiltro hidráulico colmatado (bypass) / atuação lentahydraulic.ptuUnidade de transferência de potência (PTU)todas de ptu3.750 h (15000 h)12.000 × (48.000)86–14nãonãoInoperância de transferência entre sistemas hidráulicoshydraulic.lines\_fittingsReservatório, tubulações e mangueirasqualquer de hydraulicGear, turboprop, jet2.000 h · 18 m (8k h · 72 m)1.400 × (5.600)84–12simsimVazamento externo de fluido hidráulico / perda de pressão**



**as tabelas de Pressurização (2) e Fuselagem (9) fecham a parte estrutural da aeronave com bastante precisão. O principal ajuste técnico aqui envolve alinhar a unidade de desgaste ao esforço físico real (anteparas de pressão e portas desgastam por ciclos de pressurização, não por horas de cruzeiro), corrigir três status críticos de NO-GO (válvula de alívio de pressão e travas de porta de carga) e calibrar o custo real dos para-brisas aquecidos e estruturais.Auditoria Técnica \& Ajustes Aplicados:Pressurização (pressurization.safety\_relief\_valves):NO-GO: Estava marcado como não com o efeito "risco de sobrepressão de cabine". Na aviação real, as válvulas de alívio positivo e negativo são itens obrigatórios de segurança estrutural para evitar a ruptura da fuselagem em altitude — se uma falhar, é NO-GO: sim.Vida útil e Preço: A outflow valve já estava quase exata (3.500 h e 5.500 × batem com \~14.000 h e US$ 22k reais). Ajustamos a safety relief valve para 5.000 cic (20.000 cic real) e 2.200 × (8.800 real) para escalar conforme o porte da cabine.Anteparas de Pressão (fuselage.pressure\_bulkheads):Unidade de desgaste: Assim como o trem de pouso, a antepara traseira (rear pressure bulkhead) sofre fadiga pelo ciclo de inflar e desinflar a cabine a cada voo (limite real de projeto entre 45.000 e 60.000 ciclos, famoso pelos casos JAL 123 e Aloha 243). Removemos as horas e calibramos em 12.500 cic (50.000 cic real), aumentando o custo base para 15.000 × (60.000 real) devido à complexidade extrema de reparo estrutural na cauda.Para-brisas e Janelas (fuselage.windshield\_windows):Custo real: Em aviões leves, um para-brisa acrílico custa \~US$ 2.000; porém, em turboélices e jatos pressurizados, um para-brisa laminado multicamada com aquecimento elétrico anti-gelo/anti-embaçamento custa entre US$ 25.000 e US$ 65.000 cada painel (e costuma delaminar ou dar arco elétrico a cada \~8.000 h). Ajustamos para 2.000 h (8.000 h real) e 2.200 × (8.800 real).Portas de Carga e Travas (fuselage.cargo\_doors\_latches):NO-GO: Estava como NO-GO: não com efeito "falha no mecanismo de travamento de carga". Na aviação real (especialmente após os acidentes históricos com portas de carga do DC-10 e Boeing 747), qualquer falha nos ganchos de travamento ou sensores de proximidade da porta de carga é NO-GO: sim absoluto. Também adicionamos o multiplicador × em 2.500 × (10.000 real).Vedações de Portas (fuselage.doors\_seals):Borrachas de vedação (e selos infláveis de jatos executivos/comerciais) ressecam pelo tempo e desgaste mecânico de abrir/fechar porta: calibrado em 2.000 cic · 15 m (8.000 cic · 60 m real).Tabelas Corrigidas — Pressurização e Fuselagem (Real ÷ 4)Pressurização (2)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarpressurization.outflow\_valveVálvula de fluxo (outflow valve)todas de pressurized3.750 h · 2.500 cic (15k h · 10k cic)5.500 × (22.000)66–16simnãoPerda de controle ou capacidade de pressurizaçãopressurization.safety\_relief\_valvesVálvulas de alívio de pressão diferencialtodas de pressurized5.000 cic (20000 cic)2.200 × (8.800)32–6simnãoVazamento de cabine ou risco de sobrepressão estruturalFuselagem (9)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarfuselage.frames\_bulkheadsCavernas e armações estruturais da fuselagemtodas11.250 h · 11.250 cic (45k h · 45k cic)9.000 × (36.000)4848–120simnãoDeformação permanente de caverna / interdição mandatóriafuselage.pressure\_bulkheadsAnteparas de pressão dianteira e traseiratodas de pressurized12.500 cic (50000 cic)15.000 × (60.000)6060–140simnãoFadiga crítica na antepara / risco de descompressão (AOG)fuselage.longerons\_stringersLongarinas e reforçadores da fuselagemtodas12.500 h (50000 h)8.500 × (34.000)5260–150simnãoTrinca em longarina primária de fuselagem / AOGfuselage.skin\_fairingsRevestimento e carenagens da fuselagemtodas7.500 h (30000 h)2.500 × (10.000)1816–40nãosimDano na carenagem / aumento de arrasto aerodinâmicofuselage.doors\_sealsPortas, escotilhas e vedações de cabinetodas2.000 cic · 15 m (8k cic · 60 m)1.800 × (7.200)108–24simsimVazamento de pressurização ou alerta de porta destravadafuselage.windshield\_windowsPara-brisas e janelas de cabinetodas2.000 h (8000 h)2.200 × (8.800)86–18simsimTrinca no para-brisa, arco elétrico ou delaminaçãofuselage.radomeRadome do nariz e proteção meteorológicatodas de weatherRadar3.000 h (12000 h)2.500 × (10.000)64–12nãosimAtenuação de eco radar ou dano de erosão no radomefuselage.antennasAntenas externas e descarregadores estáticostodas3.750 h (15000 h)750 × (3.000)42–6nãosimPerda de sensibilidade ou estática na recepção COM/NAV/GPSfuselage.cargo\_doors\_latchesPortas de carga, travas e sensores de fechamentoqualquer de multiEngine, pressurized, turboprop, jet3.000 cic (12000 cic)2.500 × (10.000)64–12simnãoAlerta DOOR OPEN ou falha no travamento da porta de carga**



**essa tabela de Aviônicos (14) fecha praticamente todo o ecossistema da aeronave! Na aviação real, aviônicos de estado sólido (solid-state) têm altíssima confiabilidade (MTBF entre 15.000 e 25.000 horas), exceto componentes com partes móveis (como servos de piloto automático) ou baterias internas (como a bateria do ELT).**



**Os principais ajustes aqui foram:**



**Escalonamento de Preço (×) nos últimos 6 itens: Na sua tabela original, itens como ahrs\_irs (US$ 15.000), adc (US$ 12.000) e flight\_recorders\_elt (US$ 6.000) estavam com preço fixo sem ×. Se ficassem assim, um Cessna 172 G1000 pagaria preço de jato executivo, enquanto um Airbus A320 pagaria uma pechincha por um ADIRU (que custa \~US$ 90.000 na vida real). Adicionando × com bases calibradas, o custo escala proporcionalmente entre aviação leve e comercial!**



**Erros críticos de NO-GO (ahrs\_irs, adc, standby\_instruments e elt):**



**AHRS / IRS e ADC: Estavam como NO-GO: não. Na aviação real, se o AHRS ou o ADC falharem, o piloto perde horizonte artificial, proa, altitude e velocidade no PFD — a aeronave está terminantemente proibida de decolar (NO-GO: sim).**



**Instrumento Standby (standby\_instruments): Em qualquer aeronave Glass Cockpit (C172 G1000, TBM 930, CJ4, A320), o instrumento de reserva (ISIS / standby) é item obrigatório de despacho (NO-GO: sim).**



**ELT / Gravadores (flight\_recorders\_elt): Pela regulamentação aeronáutica (RBAC 91 / FAR 91.207), voar com ELT inoperante é proibido (NO-GO: sim, exceto voo de traslado para oficina).**



**Piloto Automático, Radar Meteorológico e TCAS/EGPWS como NO-GO: não: Estão 100% fiéis à MEL real! Via MEL, um avião pode despachar sem piloto automático (voando na mão / fora do espaço aéreo RVSM), sem radar meteorológico (em condições VMC/sem formação convectiva) ou temporariamente com restrições de TCAS.**



**Tabela Corrigida — Aviônicos (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebraravionics.com\_nav\_radiosRádios COM e NAV (VHF / VOR / ILS)todas4.500 h (18000 h)1.500 × (6.000)64–10nãonãoPerda de um canal COM ou recepção VOR/ILS (MEL: restrição IFR)avionics.transponderTransponder Mode S / ADS-B Outtodas5.000 h (20000 h)1.200 × (4.800)43–8nãonãoPerda de squawk / vigilância radar (restrição em espaço aéreo controlado)avionics.gps\_receiverReceptor GNSS / GPS de navegação aéreatodas5.000 h (20000 h)1.600 × (6.400)64–12nãonãoPerda de posição por satélite GPS / navegação RNP/RNAVavionics.fms\_computerFMS / Computador de gerenciamento de vootodas de fms6.250 h (25000 h)6.500 × (26.000)128–20nãonãoFalha de cálculo LNAV/VNAV / tela inoperante da CDUavionics.autopilot\_servosPiloto automático e servos mecânicostodas de autopilot2.500 h (10000 h)2.000 × (8.000)86–16nãonãoDesengate involuntário do AP ou servo travado (fora de RVSM)avionics.glass\_displaysTelas integradas Glass Cockpit (PFD/MFD/EICAS)todas de glassCockpit3.500 h (14000 h)3.200 × (12.800)106–16simnãoTela apagada, flicker ou perda de instrumentos primáriosavionics.analog\_flight\_instrumentsInstrumentos analógicos (velocímetro, altímetro, VSI)exceto glassCockpit2.250 h (9000 h)600 × (2.400)64–10simnãoIndicação travada ou cápsula aneroide danificadaavionics.weather\_radarRadar meteorológico e antena de varreduratodas de weatherRadar2.500 h (10000 h)3.500 × (14.000)106–18nãonãoPerda de detecção meteorológica (proibido entrar em IMC convectivo)avionics.ahrs\_irsPlataforma inercial e atitude/proa (AHRS / IRS)qualquer de glassCockpit, autopilot4.000 h (16000 h)2.500 × (10.000)63–8simnãoBandeira ATT/HDG no PFD ou perda de alinhamento inercialavionics.adcComputador digital de dados de ar (ADC)qualquer de glassCockpit, turboprop, jet4.000 h (16000 h)2.000 × (8.000)52–6simnãoPerda de airspeed/altitude no PFD ou dados de ar degradadosavionics.standby\_instrumentsInstrumento eletrônico de standby (ISIS / reserva)todas de glassCockpit4.000 h (16000 h)1.800 × (7.200)42–6simnãoApagão da tela do instrumento de reserva integradoavionics.tcas\_egpwsSistema de vigilância TCAS / EGPWS / TAWSqualquer de turboprop, jet4.000 h (16000 h)3.500 × (14.000)63–8nãonãoInoperância de avisos sonoros/visuais de tráfego e terrenoavionics.flight\_recorders\_eltGravadores de voo (FDR/CVR) e ELTtodas4.000 h · 12 m (16k h · 48 m)450 × (1.800)42–6simnãoBateria do ELT vencida / falha de gravação de parâmetrosavionics.audio\_panelPainel de áudio e intercomunicadortodas4.500 h (18000 h)500 × (2.000)32–4nãonãoFalha de intercom ou seleção de canal secundário COM**



**essas duas tabelas — Pneumático (4) e Cabine (4) — estão excelentes no aspecto operacional e regulatório! Os critérios de NO-GO (não) estão 100% fiéis à MEL real da aviação comercial e executiva (despacho com 1 Pack inoperante limitado ao FL250/FL310, despacho com 1 Bleed Valve isolada via crossbleed/APU, e lavatório/IFE inoperantes afetando apenas o conforto ou rotas longas).Os ajustes aqui foram focados em calibrar a vida útil real (MTBF/overhaul) — já que máquinas de ciclo de ar (ACM) e válvulas de sangria trabalham sob altíssima temperatura/rotação — e escalonar melhor os preços base (Real ÷ 4) para que o multiplicador × funcione bem tanto em um King Air / CJ4 quanto em um A330 / 747.Auditoria Técnica \& Ajustes Aplicados:Sistema Pneumático (bleed\_valves, precooler, ducts\_leak\_detection, air\_cycle\_machine):Válvulas de Sangria (pneumatic.bleed\_valves): Na aviação real, válvulas PRSOV e HP Bleed Valves sofrem fadiga térmica severa e passam por revisão geral entre 8.000 h e 12.000 h (média 10.000 h → dividido por 4 = 2.500 h). O custo real varia de \~US$ 10.000 (turboélice) a US$ 55.000+ (jato comercial), calibrado em 5.000 × (20.000 real).Precooler (pneumatic.precooler): Trocadores de calor ar-ar trincam por ciclagem térmica em torno de 14.000 h reais (dividido por 4 = 3.500 h), com custo real médio de US$ 32.000 × → dividido por 4 = 8.000 ×.Dutos e Loops de Detecção (pneumatic.ducts\_leak\_detection): Adicionamos o multiplicador × no preço (3.500 × / 14.000 real), pois o comprimento dos dutos e sensores overheat loop (Kidde/Fenwal) de um bimotor executivo é uma fração da malha pneumática de um A330 ou Boeing 747.Packs / ACM (pneumatic.air\_cycle\_machine): Turbinas de ciclo de ar giram acima de 40.000 RPM em mancais pneumáticos (air bearings) e têm overhaul real em torno de 12.000 h (dividido por 4 = 3.000 h), custando entre US$ 45.000 e US$ 130.000+ → base calibrada em 12.000 × (48.000 real).Cabine de Passageiros (passenger\_seats, galley, lavatory, in\_flight\_entertainment):Assentos (cabin.passenger\_seats): Na aviação real, estofamentos, espumas retardantes de chama, cintos e mecanismos de reclinação são reformados nos C-Checks pesados a cada 16.000 h ou 5 anos (60 meses) → dividido por 4 = 4.000 h · 15 m.Galley e Lavatório (cabin.galley e cabin.lavatory): Fornos de convecção, cafeteiras aeronáuticas e bombas de vácuo dos toaletes exigem revisão a cada 10.000–14.000 h reais (dividido por 4 = 3.500 h e 2.500 h), com custos reais ajustados para escalar via × entre jatos executivos e airliners.IFE (cabin.in\_flight\_entertainment): Telas individuais e servidores de bordo têm ciclo de vida mais curto que a estrutura do avião (\~10.000 h reais → dividido por 4 = 2.500 h).Tabelas Corrigidas — Pneumático e Cabine (Real ÷ 4)Pneumático (4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarpneumatic.bleed\_valvesVálvulas reguladoras e de corte de sangria /mottodas de bleedAir2.500 h (10000 h)5.000 × (20.000)64–10nãonãoVálvula de sangria presa fechada / BLEED TRIP OFFpneumatic.precoolerTrocador de calor pré-resfriador (Precooler) /mottodas de bleedAir3.500 h (14000 h)8.000 × (32.000)86–14nãonãoSuperaquecimento do duto de sangria / BLEED OVERHEATpneumatic.ducts\_leak\_detectionDutos pneumáticos e loops de detecção de calortodas de bleedAir3.500 h (14000 h)3.500 × (14.000)64–12nãonãoAlarme de vazamento pneumático / BLEED DUCT LEAKpneumatic.air\_cycle\_machineMáquina de ciclo de ar (Packs / ACM)todas de pressurized; qualquer de turboprop, jet3.000 h (12000 h)12.000 × (48.000)128–18nãonãoDesarme de pack de refrigeração / limitação de teto operacionalCabine (4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarcabin.passenger\_seatsAssentos de passageirostodas de passengerCabin4.000 h · 15 m (16k h · 60 m)4.000 × (16.000)84–16nãonãoDesgaste de estofamento, falha de reclinação e perda de confortocabin.galleyGalley / Copa de bordotodas de galley3.500 h (14000 h)6.000 × (24.000)126–18nãonãoFalha elétrica de fornos/aquecedores ou travamento da copacabin.lavatorySistema de lavatóriotodas de lavatory2.500 h (10000 h)4.500 × (18.000)104–12nãonãoEntupimento ou vazamento (restrição para voos longos)cabin.in\_flight\_entertainmentSistema de entretenimento a bordo (IFE)todas de passengerCabin, jet2.500 h (10000 h)7.000 × (28.000)84–12nãonãoTelas de assento inoperantes / degradação da experiência**



**essa tabela de Oxigênio (3) está muito bem alinhada com a regulamentação real (RBAC 91/121 e FAR 91.211 / 121.327) — inclusive os status de NO-GO (sim) e o check de pré-voo (WA: sim nas máscaras quick-donning, que o piloto testa no cockpit prep).Na aviação real, o sistema de oxigênio tem uma característica particular: o desgaste é quase 100% calendárico (meses), e não por horas de voo — cilindros vencem pelo teste hidrostático obrigatório (3 a 5 anos) e geradores químicos de passageiros vencem pela validade do composto (10 a 15 anos), mesmo que o avião fique parado no hangar.Auditoria Técnica \& Ajustes Aplicados:Cilindro de Oxigênio da Tripulação (oxygen.crew\_cylinder):Vida útil real: Cilindros aeronáuticos (Scott / Zodiac — padrão DOT 3AA de aço ou DOT 3HT composto/Kevlar) exigem teste hidrostático obrigatório a cada 36 a 60 meses (3 a 5 anos), independentemente das horas voadas. Usando 60 meses reais → dividido por 4 = 15 m (removemos as horas para refletir o vencimento calendárico real, ou você pode manter 3.750 h · 15 m se o seu código exigir horas em todos os itens).Preço real: Adicionamos o multiplicador × em 900 × (3.600 real), pois um cilindro pequeno de Bonanza/TBM custa \~US$ 2.200, enquanto um cilindro de alta capacidade de jato comercial custa US$ 7.000+.Sistema de Oxigênio dos Passageiros (oxygen.passenger\_system):Vida útil real: Nos jatos comerciais (A320 / 737 / A330), os geradores químicos das máscaras (PSU Chemical Oxygen Generators) têm validade fixa de 120 a 180 meses (10 a 15 anos). Usando 120 meses reais (e 12.000 h) → dividido por 4 = 3.000 h · 30 m.Preço real: Trocar todo o lote de geradores químicos de um airliner (mais o descarte controlado de material perigoso) custa entre US$ 35.000 e US$ 75.000, enquanto em um turboélice/jato executivo (sistema gasoso central) custa \~US$ 10.000. Calibrado em 4.000 × (16.000 real).Máscaras e Reguladores (oxygen.masks\_regulators):Vida útil real: Máscaras quick-donning (Eros / Zodiac) passam por revisão de diafragma, válvulas de demanda e teste de estanqueidade a cada 72 meses (6 anos) ou 6.000 h → dividido por 4 = 1.500 h · 18 m.Preço real: Estava fixo em US$ 2.200, mas na vida real um único par de máscaras quick-donning de cabine de comando custa entre US$ 7.000 e US$ 11.000 (enquanto máscaras simples de pistão custam \~US$ 800). Adicionando o multiplicador × em 1.000 × (4.000 real), o preço escala perfeitamente entre classes.Tabela Corrigida — Sistema de Oxigênio (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebraroxygen.crew\_cylinderCilindro de oxigênio da tripulaçãotodas de oxygen15 m (60 m — calendárico)900 × (3.600)42–6simnãoPressão insuficiente ou teste hidrostático vencido (NO-GO)oxygen.passenger\_systemSistema de oxigênio dos passageirostodas de oxygen, passengerCabin3.000 h · 30 m (12k h · 120 m)4.000 × (16.000)84–12simnãoGeradores químicos vencidos ou falha nas linhas de passageirosoxygen.masks\_regulatorsMáscaras e reguladores de oxigêniotodas de oxygen1.500 h · 18 m (6k h · 72 m)1.000 × (4.000)32–4simsimVazamento nas máscaras quick-donning ou falha no regulador**







**essas duas tabelas — Proteção contra Fogo (4) e Emergência (4) — encerram o catálogo com chave de ouro! A lógica regulatória de NO-GO / MEL já veio impecável de fábrica na sua estrutura original (extintor de APU inoperante travando apenas a APU, coletes restringindo apenas trechos marítimos, limpadores e detector de gelo despacháveis via MEL).**



**Os ajustes aqui foram apenas na calibração de vida útil e custos reais (÷ 4), especialmente porque garrafas extintoras (Halon + cartuchos pirotécnicos squibs), escorregadeiras de evacuação (escape slides) e coletes/botes são itens governados por validade calendárica rigorosa na aviação real, além de terem custos elevados de recertificação.**



**Auditoria Técnica \& Ajustes Aplicados:**

**Proteção contra Fogo (fire.\*):**



**Laços Detectores de Fogo (fire.engine\_detection): Sensores pneumáticos/contínuos (Kidde / Fenwal / Meggitt) ficam expostos a altíssima temperatura e vibração na nacele do motor, durando em média 12.000 h reais (dividido por 4 = 3.000 h), com custo real médio de US$ 14.000 × → dividido por 4 = 3.500 ×.**



**Garrafas Extintoras do Motor (fire.engine\_extinguisher\_bottles): Na aviação real, os cartuchos pirotécnicos (squibs) têm validade fixa de 5 a 6 anos (60–72 meses) e os cilindros passam por teste hidrostático a cada 60 meses (ou 15.000 h). Dividindo 15.000 h · 60 m por 4 = 3.750 h · 15 m. Uma garrafa de Halon 1301 com squibs novos custa em média US$ 18.000 × na vida real → dividido por 4 = 4.500 ×.**



**Extinção de Fogo da APU (fire.apu\_extinguisher): Mesma regra calendárica dos squibs (12.000 h · 60 m ÷ 4 = 3.000 h · 15 m), com custo real base de US$ 14.000 × → dividido por 4 = 3.500 ×.**



**Extintores Portáteis (fire.cabin\_extinguishers): Inspeção anual obrigatória (12 m) e recarga/teste hidrostático a cada 60 a 72 meses (ou troca em \~24–48 m em frotas comerciais). Mantendo a base de 48 m (4 anos) e 16.000 h → dividido por 4 = 4.000 h · 12 m, e preço real de US$ 400 (Halotron aeronáutico) → dividido por 4 = 100 (com × opcional ou fixo em 120).**



**Emergência (emergency.\*):**



**Escorregadeiras de Evacuação (emergency.evacuation\_slides): Na aviação real (Air Cruisers / Goodrich), os slides são removidos obrigatoriamente a cada 36 meses (3 anos) para desdobramento, inspeção de tecido/costuras e recarga da garrafa de nitrogênio/CO₂. Dividindo 15.000 h · 36 m por 4 = 3.750 h · 9 m, com custo real médio de US$ 48.000 × (jogo completo de portas) → dividido por 4 = 12.000 ×.**



**Coletes Salva-Vidas e Botes (emergency.life\_vests\_rafts): Coletes têm validade fixa de 60 a 120 meses (5 a 10 anos) e botes (life rafts) são recertificados a cada 36 meses. Dividindo 12.000 h · 36 m por 4 = 3.000 h · 9 m, com custo real base de US$ 12.000 × → dividido por 4 = 3.000 ×.**



**Limpadores de Para-Brisa (emergency.windshield\_wipers): Palhetas aeronáuticas ressecam em 12–24 meses (\~1.000 h reais de uso/exposição UV → dividido por 4 = 1.000 h · 6 m), e um motor/braço completo custa \~US$ 2.400 → dividido por 4 = 600 ×.**



**Sonda Detectora de Gelo (emergency.ice\_detector): Sondas vibratórias ultrassônicas (Rosemount / Collins) duram cerca de 10.000 h reais (dividido por 4 = 2.500 h) e custam entre US$ 12.000 e US$ 25.000 → base calibrada em 4.000 × (16.000 real).**



**Tabelas Corrigidas — Proteção contra Fogo e Emergência (Real ÷ 4)Proteção contra Fogo (4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarfire.engine\_detectionSistema de detecção de fogo do motor /mottodas de fireProtection3.000 h (12000 h)3.500 × (14.000)64–10simnãoFalha do laço sensor (FIRE LOOP FAULT) ou alarme falso (NO-GO)fire.engine\_extinguisher\_bottlesGarrafas extintoras de fogo do motor /mottodas de fireProtection3.750 h · 15 m (15k h · 60 m)4.500 × (18.000)43–8simnãoGarrafa descarregada ou squib pirotécnico vencido (NO-GO)fire.apu\_extinguisherDetecção e extinção de fogo da APUtodas de apu, fireProtection3.000 h · 15 m (12k h · 60 m)3.500 × (14.000)42–6nãonãoAPU FIRE DISCH inoperante; APU bloqueada para usofire.cabin\_extinguishersExtintores portáteis de cabine e cockpittodas4.000 h · 12 m (16k h · 48 m)120 (480)11–2simsimExtintor portátil descarregado ou com validade vencida (NO-GO)Emergência (4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebraremergency.evacuation\_slidesEscorregadeiras de evacuação de emergênciatodas de passengerCabin, jet3.750 h · 9 m (15k h · 36 m)12.000 × (48.000)106–16simsimSLIDE DISARMED permanente ou garrafa descarregada (NO-GO)emergency.life\_vests\_raftsColetes salva-vidas e botes de emergênciatodas de passengerCabin3.000 h · 9 m (12k h · 36 m)3.000 × (12.000)42–6nãonãoColetes/botes vencidos; restrição para rotas sobre a águaemergency.windshield\_wipersLimpadores de para-brisa eletromecânicostodas de wipers1.000 h · 6 m (4k h · 24 m)600 × (2.400)21–3nãosimPalheta rasgada ou motor travado (restrição sob chuva forte)emergency.ice\_detectorSonda detectora de gelo óptico/vibratóriotodas de iceDetector2.500 h (10000 h)4.000 × (16.000)32–5nãosimFalha do aviso ICE DETECTED; exige observação visual de gelo**



**Na aviação real, lâmpadas incandescentes/halógenas (como as clássicas GE 4509/4596) e conjuntos de strobe têm vida útil medida exatamente pelo tempo em que o filamento ou capacitor fica energizado, e a vibração do motor/pouso acelera a queima dos faróis de pouso e táxi.A estrutura regulatória (NO-GO: não, gerando restrição de MEL noturno) e o check no Walkaround (WA: sim) estão 100% fiéis à aviação real (FAR 91.205 / RBAC 91). Os ajustes aqui são apenas para calibrar a vida útil e os custos médios reais (ponderando entre lâmpadas halógenas de aviação geral e conjuntos LED/xenon modernos) na escala Real ÷ 4:Auditoria Técnica \& Ajustes Aplicados:Faróis de Pouso e Táxi (lights.landing e lights.taxi):Vida útil real: Lâmpadas halógenas aeronáuticas tradicionais (GE 4509 / PAR36 / PAR46) duram apenas 25 a 100 horas acesas por conta do calor intenso e vibração da asa/capô, enquanto faróis modernos em LED (Whelen Parmetheus / AeroLEDs) duram 5.000+ horas. Na sua tabela original, 300 h acesas já está muito próximo do ideal para o simulador (equivalente a \~1.200 h reais de conjunto misto). Ajustamos para 250 h acesas (1.000 h real) para dar um ritmo realista de manutenção para quem voa muito à noite ou esquece o farol de pouso ligado em cruzeiro!Preço real: Uma lâmpada halógena custa \~US$ 40–80, enquanto um bloco óptico selado/LED custa US$ 350–1.200+. Calibrado em 65 × (260 real) — adicionando o multiplicador × para escalar bem de um Cessna 152 até um Boeing 737/A320 (que possui faróis retráteis duplos).Luzes de Navegação (lights.nav):Vida útil e Preço: Lâmpadas de ponta de asa (vermelha/verde/cauda) duram em média 1.600 h acesas reais → dividido por 4 = 400 h acesas, com custo médio de US$ 180 × para o jogo → dividido por 4 = 45 ×.Strobes e Beacon Anticolisão (lights.strobe e lights.beacon):Vida útil e Preço: Sistemas strobe de descarga capacitiva (tubos de xenon + fonte power supply Whelen/Grimes) e beacons rotativos/LED duram entre 2.000 h e 3.000 h acesas reais → dividido por 4 = 600 h acesas (strobes) e 750 h acesas (beacon).Um conjunto de strobes aeronáuticos custa entre US$ 600 (pistão) e US$ 3.500+ (jato), e um beacon custa entre US$ 400 e US$ 1.800. Calibrados em 220 × (880 real) e 140 × (560 real).Iluminação do Painel (lights.panel):Vida útil e Preço: Lâmpadas de instrumentos (post lights / flood lights / retroiluminação) duram em média 4.000 h acesas reais → dividido por 4 = 1.000 h acesas, com custo médio de US$ 240 × → dividido por 4 = 60 ×.Ajuste de WA (Walkaround) e Efeito: O painel interno é verificado no cockpit prep (não no walkaround externo), e o efeito específico é a perda de visibilidade dos instrumentos em voo noturno/IMC.Tabela Corrigida — Sistema de Iluminação (Real ÷ 4)idPeçaAplica-se aVida (Real ÷ 4)Preço Base (Real ÷ 4)MO (h)Parada (h)NO-GOWAEfeito ao quebrarlights.landingFarol de pousoexceto noElectrical250 h acesas (1000 h)65 × (260)0.50,5–1nãosimFarol de pouso queimado (MEL: proibido pouso noturno sem farol)lights.taxiFarol de táxiexceto noElectrical250 h acesas (1000 h)65 × (260)0.50,5–1nãosimFarol de táxi queimado (MEL: restrição de táxi noturno)lights.navLuzes de navegaçãoexceto noElectrical400 h acesas (1600 h)45 × (180)0.50,5–1nãosimLuz de navegação queimada (MEL: proibido voo noturno)lights.strobeStrobes (anticolisão)exceto noElectrical600 h acesas (2400 h)220 × (880)10,5–1nãosimStrobe inoperante (MEL: restrição noturna / espaço aéreo)lights.beaconBeacon (anticolisão)exceto noElectrical750 h acesas (3000 h)140 × (560)0.50,5–1nãosimBeacon queimado (MEL: proibido acionamento/voo noturno)lights.panelIluminação do painelexceto noElectrical1.000 h acesas (4000 h)60 × (240)10,5–1nãonãoFalha na retroiluminação dos instrumentos (MEL: restrição IFR/noturna)**



**Dica para o Código da LUZES-F2:Redundância com electrical.external\_lighting: Note que na tabela do sistema Elétrico (revisada anteriormente), havia o item genérico electrical.external\_lighting (Sistema de iluminação externa). Como você agora criou a categoria dedicada Iluminação (6) com rastreamento individual por LVar/SimVar (h acesas), lembre-se de remover electrical.external\_lighting da categoria Elétrico para não cobrar manutenção em duplicidade do jogador!**



**essa matriz de 45 aeronaves mostra que a arquitetura de feature tags do FlightCareer está escalando com muita coerência — indo de 66 peças em um monomotor clássico (Piper J-3 Cub / CAP-4 Paulistinha) até 195–196 peças (contando os motores multiplicados /mot) em um quadrimotor pesado como o Boeing 747-8i ou o Airbus A400M.**



**Fazendo uma auditoria cruzada entre as tags de cada avião e as regras Aplica-se a das tabelas anteriores, encontrei dois pontos importantes de código/regra e três detalhes técnicos reais nos JSONs das aeronaves:**



**1. O Filtro noElectrical (CAP-4 e Piper J-3 Cub)**

**Note que cap-4.json e piper-j-3-cub.json possuem a tag noElectrical (sem sistema elétrico de bordo — partida na mão na hélice, sem bateria, sem rádio fixo e sem luzes), mas ainda assim receberam 66 peças cada.**



**Isso acontece porque nas tabelas anteriores (antes de você implementar a regra exceto noElectrical na tabela de Iluminação), vários itens elétricos estavam marcados como todas. Para que o J-3 Cub e o Paulistinha não cobrem manutenção de eletrônica que não existe neles, vale adicionar exceto noElectrical nos seguintes IDs:**



**Elétrico: electrical.battery, electrical.voltage\_regulator, electrical.wiring\_harness, electrical.circuit\_breakers\_relays**



**Combustível / Instrumentos / Fuselagem: fuel.aux\_pump (bomba elétrica), instruments.pitot\_tube (tubo aquecido), fuselage.antennas**



**Aviônicos: avionics.com\_nav\_radios, avionics.transponder, avionics.gps\_receiver, avionics.audio\_panel, avionics.flight\_recorders\_elt (o J-3 Cub e CAP-4 originais nem ELT fixo possuíam, ou usam PLB portátil).**



**(Com esse ajuste, o CAP-4 e o Piper J-3 Cub caem de 66 para \~54 peças puramente mecânicas, deixando a manutenção inicial extremamente fiel à realidade!)**



**Anomalias Técnicas Reais nas Tags das Aeronavesboeing-787-10-dreamliner.json com bleedAir e bleedAntiIce:Na aviação real, o Boeing 787 Dreamliner é famoso justamente por ter uma arquitetura bleedless (sem sangria de ar dos motores). Ele não possui bleed\_valves, precooler nem bleed\_anti\_ice — a pressurização/ar-condicionado usa compressores elétricos dedicados (Cabin Air Compressors) e o antigelo das asas é eletrotérmico (mantas aquecidas por eletricidade).Sugestão: Remova bleedAir e bleedAntiIce do boeing-787-10-dreamliner.json (ele passará de 117 para 113 peças, destacando tecnologicamente o 787 dos demais jatos!).cessna-citation-longitude.json com rat:   O Citation Longitude não possui RAT (Ram Air Turbine) — a redundância elétrica de emergência dele é feita por 4 baterias dedicadas + APU certificada para uso em voo (ao contrário do Pilatus PC-24, que realmente tem uma RAT instalada na fuselagem traseira).Sugestão: Remova rat do cessna-citation-longitude.json.Trem dianteiro livre (Free-Castering) marcado como noseWheelSteering:Nos modelos cirrus-sr22.json, diamond-da40ng.json, diamond-da62.json e flight-design-ct.json, a roda do nariz na vida real não tem mecanismo de direção ligado aos pedais (free-castering nosewheel — o piloto curva no solo usando freio diferencial).Como a peça gear.nosewheel\_steering também engloba o amortecedor shimmy (shimmy damper), manter a tag funciona bem no jogo para simular desgaste do shimmy damper, mas se você quiser purismo total, pode removê-la desses 4 modelos.**



**Acertos Técnicos Notáveis no Seu CatálogoVale destacar vários detalhes muito bem mapeados nos seus JSONs que mostram cuidado com a aviação real:c400-corvalis.json com spoilers: Corretíssimo — o Cessna 400 / Columbia 400 realmente possui speedbrakes elétricos no extradorso das asas.nxcub.json com noseWheelSteering (e sem tailwheel): Corretíssimo — o CubCrafters NXCub é exatamente a versão triciclo do XCub.diamond-da40ng.json e diamond-da62.json com diesel + fuelInjected: Perfeito — os motores Austro Engine AE300/AE330 são ciclo Diesel com injeção eletrônica common-rail, então ambos os filtros se aplicam sem conflito.Cargueiros especializados (a330-belugaxl.json e boeing-747-400-global-supertanker.json): Remoção limpa de passengerCabin, galley e lavatory, reduzindo corretamente a contagem de peças.noFlaps no cap-4.json e piper-j-3-cub.json: Só certifique-se de que na tabela da Asa, a linha airframe.flaps esteja configurada como exceto noFlaps (atualmente estava como todas), para que o J-3 Cub e o Paulistinha não tenham trilhos de flap para manter!**



**Esse relatório cruzando as flags (noElectrical, noFlaps) com as regras do catálogo pegou exatamente os pontos de atrito antes de irem para produção.**



**Aqui estão as recomendações técnicas e arquiteturais para resolver cada um desses 4 pontos de forma limpa no código:**



**1. Ajuste das Regras para noElectrical e noFlaps (Atenção a um detalhe!)**

**Nos achados automáticos do cap-4.json e piper-j-3-cub.json, a maioria dos itens realmente deve receber exceto noElectrical, com uma exceção importante:**



**MANTENHA avionics.analog\_flight\_instruments no CAP-4 e J-3 Cub:**



**Velocímetro (ASI), Altímetro e VSI são instrumentos puramente mecânicos / cápsulas aneroides movidos a pressão de ar (Pitot-Estático) — eles não usam eletricidade e existem no painel do Paulistinha e do Piper J-3 Cub!**



**Se você aplicar exceto noElectrical nele, o CAP-4 e o J-3 ficarão sem nenhum instrumento de voo no catálogo.**



**Onde aplicar exceto noElectrical:**



**electrical.battery, electrical.alternator, electrical.voltage\_regulator, electrical.wiring\_harness, electrical.circuit\_breakers\_relays**



**avionics.com\_nav\_radios, avionics.transponder, avionics.gps\_receiver, avionics.flight\_recorders\_elt, avionics.audio\_panel**



**(Verifique também fuel.aux\_pump e fuselage.antennas — se estiverem como todas, também devem receber exceto noElectrical).**



**Onde aplicar exceto noFlaps:**



**Apenas em airframe.flaps (já elimina o erro no CAP-4 e no J-3 Cub).**



**2. Sobreposição com LUZES-F2 (electrical.external\_lighting vs lights.\*)**

**Entre as duas opções levantadas, a melhor decisão para evitar redundância e manter a limpeza da tabela é remover electrical.external\_lighting:**



**Por quê: Você já possui electrical.wiring\_harness (chicote elétrico geral) e electrical.circuit\_breakers\_relays (painel de disjuntores e relés) cobrindo a parte elétrica de fiação e chaves.**



**Como as 6 peças da LUZES-F2 (landing, taxi, nav, strobe, beacon, panel) já possuem exceto noElectrical e contam horas acesas individualmente, remover electrical.external\_lighting elimina 100% da dupla contagem.**



**3. Impacto da Fórmula do Hangar (min × 0,7 + média × 0,3)**

**A sua fórmula atual para as 9 barras do Hangar (min × 0,7 + média × 0,3) funciona muito bem com os novos valores calibrados (Real ÷ 4), especialmente por causa dos consumíveis de giro rápido:**



**Como itens como oil.lubricant (25 h), tires.main (75 cic) e lights.landing (250 h) chegam perto de 0% bem antes da estrutura (12.500 h), o peso de 0,7 no componente mais desgastado (min) garante que a barra do grupo no Hangar caia visualmente e alerte o jogador mesmo quando os outros 15 itens do motor/trem ainda estão em 95%.**



**Dica rápida para a futura tarefa de UI (Drill-down de peças): Quando você for criar a tela de peças por grupo no Blazor, basta ordenar a lista por HealthPercent ASC (mais desgastadas no topo) e destacar com badge vermelho os itens com NO-GO: true abaixo de 15–20%, assim o jogador sabe exatamente qual peça puxou a barra do grupo para baixo.**



**4. Metadado lifeSource no Catálogo**

**Para diferenciar no seu JSON/banco o que já foi auditado do que ainda está pendente de revisão, você pode atualizar o campo nas 22 categorias calibradas de "estimativa — revisar" para:**



**JSON**

**"lifeSource": "real\_div\_4\_calibrado"**

**Assim, qualquer script de auditoria futura no seu projeto consegue filtrar instantaneamente se alguma peça nova adicionada ao catálogo ainda está com "estimativa — revisar".**











Conferência do POH (aircraft\_limits-v4) e Variantes do Simulador

Vle do Diamond DA62 (201 kt vs 156 kt):



Correto no POH/AFM do DA62: Vle = 156 KIAS (velocidade máxima com o trem baixado). Já Vlo (operação/extensão e recolhimento) também é 156 KIAS, e Vne é 205 KIAS. Portanto, o arquivo (156 kt) está certo e o catálogo (201 kt) estava errado.



Vne / Vmo do King Air 350 (245 kt vs 263 kt):



Correto no AFM do Beechcraft King Air 350: Vmo = 263 KIAS (do nível do mar até 21.000 ft; acima disso é Mmo = 0.58). Portanto, o arquivo (263 kt) está certo e o catálogo (245 kt) estava subestimado (provavelmente confundido com o King Air C90, que tem Vmo = 226 kt, ou King Air 200, que tem Vmo = 259 kt).



CAP 10 × CAP-4: NÃO são o mesmo avião! O Mudry CAP 10 (nativo do MSFS) é um avião acrobático francês moderno (180 hp, asa baixa, hélice de passo variável, sistema elétrico completo e flaps). Já o CAP-4 Paulistinha (addon brasileiro) é um treinador clássico dos anos 1940 (65 hp, asa alta, sem sistema elétrico noElectrical e sem flaps noFlaps). Mantenha os dois separados!







&#x20;Decisões Pendentes (1 linha cada, prontas para colar no doc)Vlo: Usar sempre a menor entre extensão ($V\_{LO\\text{ ext}}$) e recolhimento ($V\_{LO\\text{ ret}}$), pois é o limite estrutural mais restritivo para acionar a alavanca do trem sem danificar portas e atuadores.Auto-leme e outras ajudas: Aplicar -10% de XP por assistência ativa (acumulável até um teto máximo de -30%, mantendo a mesma regra do auto-start).Calendário de jogo (vida em meses): Usar tempo simulado acumulado de carreira (dias avançam conforme a duração dos voos realizados e tempos de parada no hangar), nunca data real do relógio do Windows (para não punir o jogador que fica semanas sem abrir o simulador).





Referência de Margem \& Custo Real por Hora (Para o Rebalanceamento de Missões)Para calibrar o retorno financeiro do FlightCareer, estas são as referências reais de mercado e metas equilibradas de payback (horas de voo para pagar o avião):ClasseExemplosCusto Operacional Real/h (Combustível + Reserva Manutenção)Tarifa Charter / Aluguel Real/hMeta de Payback no SimuladorMargem Líquida Alvo / HoraPistão LeveC152, C172, J-3 CubUS$ 90 – US$ 140/hUS$ 160 – US$ 220/h40 – 60 h de voo\~35% a 45%Pistão Complexo / BimotorBonanza, Baron G58, DA62US$ 250 – US$ 450/hUS$ 450 – US$ 750/h70 – 100 h de voo\~35% a 45%Turboélice Utilitário / ExecC208B, TBM 930, King Air 350US$ 750 – US$ 1.400/hUS$ 1.500 – US$ 2.800/h100 – 150 h de voo\~40% a 50%Jato ExecutivoCitation CJ4, PC-24, LongitudeUS$ 1.800 – US$ 3.200/hUS$ 3.500 – US$ 6.500/h150 – 220 h de voo\~40% a 50%Comercial / AirlinerATR-72, A320neo, 737 MAX, 787US$ 3.500 – US$ 9.500/hUS$ 7.000 – US$ 18.000/h250 – 400 h de voo\~45% a 55%









Integridade e Anti-Cheat (BACK\_TO\_FLY, SIM\_RESET, RELOAD\_USER\_AIRCRAFT, REPAIR\_AND\_REFUEL)

Decisão recomendada: Sim, invalida o contrato ativo e reaplica o estado persistido do banco (SQLite) — com uma trava de grace period no carregamento inicial.



Regra de estado:



Em voo (SIM ON GROUND == 0) ou com motor ligado após o início do bloco (Off-Block): Qualquer disparo de SIM\_RESET, SITUATION\_RESET, RELOAD\_USER\_AIRCRAFT ou REPAIR\_AND\_REFUEL invalida o voo na hora (status ABORTED / INVALIDATED), aplica multa de quebra de contrato/telemetria e força a reinjeção imediata do desgaste, combustível e panes salvas no SQLite.



Tratamento do BACK\_TO\_FLY (Sair do menu ESC): No MSFS 2024, abrir o menu ESC e voltar dispara BACK\_TO\_FLY. Não invalide o voo por ele sozinho. Use-o apenas como gatilho de auditoria: ao receber BACK\_TO\_FLY, compare PLANE LATITUDE/LONGITUDE, PLANE ALTITUDE, FUEL TOTAL QUANTITY e estado das panes com o último tick válido antes do pause. Se houve teleporte, ganho de altitude/combustível ou limpeza de pane via menu/DevMode, aí sim invalide.



Exceção de Spawn: Ignore RELOAD\_USER\_AIRCRAFT e SITUATION\_RESET nos primeiros 10–15 segundos da sessão enquanto o simulador está posicionando a aeronave no pátio (Cold \& Dark).







Serviços de Solo (GPU, Catering, Baggage, Jetway, Stairs, Pushback)

Decisão recomendada: Modelo Híbrido (Pré-compra no Dispatch + Auditoria por Evento do Sim).



Como funciona na prática:



O app chama o que foi comprado: No Dispatch/Briefing do FlightCareer, o piloto contrata o pacote de solo da escala (ex.: GPU + Pushback + Bagagem). Quando o avião para no gate (SIM ON GROUND == 1, parking brake setado e motores cortados), o seu C# dispara os eventos correspondentes (TOGGLE\_EXTERNAL\_POWER, REQUEST\_LUGGAGE, TOGGLE\_JETWAY, etc.) para dar vida à operação sem o piloto precisar brigar com o ATC nativo. (ASSIM, PELO ADDON ELE COMPRA OS SERVICOS, E DEPOIS O SIMULADOR ATIVA OS SERVICOS E SUAS CORRESPONDENDES ANIMACOES CASO TIVER, O USUARIO PRECISAR USAR O ATC).



Cobra o que o piloto pedir por fora: Se o piloto acionar um serviço extra direto pelo menu do MSFS ou toolbar (ex.: chamou REQUEST\_CATERING ou TOGGLE\_PUSHBACK sem ter incluído no plano), o SimConnect captura o evento e lança como taxa avulsa (com acréscimo de \~20% de taxa de conveniência de rampa) no fechamento financeiro do voo.



Regra de isenção: Pushback só é cobrado em aeronaves acima de um determinado MTOW (ex.: > 5.700 kg / bimotores pesados e jatos). Para um Cessna 152/172, o pushback é gratuito (empurrado manualmente na barra).



Alarme Nativo (MASTER\_CAUTION\_SET / MASTER\_WARNING\_SET)

Decisão recomendada: Sim, sempre sincronizado com a severidade da pane do addon.



Mapeamento ideal:



Pane Amarela / Degradação (Warning / Degraded): Dispara MASTER\_CAUTION\_SET (1) quando o componente cruza o limiar de falha parcial (ex.: alternador caindo, filtro obstruído, queda de pressão hidráulica leve).



Pane Vermelha / Crítica (Critical / Failure): Dispara MASTER\_WARNING\_SET (1) em panes graves (fogo, falha de motor, despressurização, perda total de barramento).



Cuidado técnico no SimConnect: Em aeronaves complexas (PMDG, Fenix, A2A, Black Square), o WASM próprio do avião sobrescreve os eventos nativos de Master Caution/Warning a cada frame. Para aeronaves padrão/G1000/G3000 do MSFS 2024 funciona perfeitamente; para third-party de estudo, deixe uma flag por modelo no seu JSON de aeronaves (supports\_native\_master\_alarms: bool) para evitar conflito de áudio infinito ou pisca-pisca no painel.



Fogo e Extintor (EXTINGUISH\_ENGINE\_FIRE)

Decisão recomendada: Sim, totalmente acoplado ao estágio do incêndio progressivo do addon.



Lógica por estágio (State Machine):



Estágio 1 — Incipiente / Superaquecimento Severo (0 a 20s): Cortar a mistura/combustível + acionar EXTINGUISH\_ENGINE\_FIRE (com índice do motor correto) extingue 100% do fogo imediatamente. O motor fica inoperante (AOG), mas salva a fuselagem e evita perda total.



Estágio 2 — Fogo Estabelecido (20 a 45s): O primeiro disparo do extintor reduz o fogo para o Estágio 1 (contido, mas ainda fumegando/quente). Exige o segundo disparo (garrafa B) ou corte completo de combustível + ar de sangria (bleed air) para apagar de vez.



Estágio 3 — Fora de Controle (> 45s): O extintor nativo não dá conta sozinho; apenas reduz temporariamente a taxa de dano estrutural enquanto o piloto precisa fazer descida de emergência e pouso forçado antes da falha estrutural catastrófica.



Persistência: Cada disparo de EXTINGUISH\_ENGINE\_FIRE descarrega a garrafa no banco de dados — se o piloto não recarregar o extintor na oficina, no próximo voo o botão não terá efeito.



Capas/Calços no Pernoite e TOOLS\_QUICK\_PREFLIGHT (Walkaround Pulado)Decisão recomendada: Sim, com impacto direto na taxa de desgaste ambiental e na detecção pré-voo.Efeito das Capas e Calços (Tie-downs / Pitot Covers / Chocks):Se colocados antes de encerrar a sessão: Zera a penalidade de desgaste ambiental durante o tempo offline (chuva, vento forte, maresia em aeroportos costeiros e entrada de sujeira/insetos no tubo de Pitot).Se esquecidos no pernoite: Aumenta a probabilidade de falha latente no próximo voo (ex.: tubo de Pitot obstruído gerando leitura errada de IAS na decolagem, bateria descarregada por esquecer master/aviônicos ou dano por rajada de vento em aeronaves leves).Efeito do TOOLS\_QUICK\_PREFLIGHT (Pular o Walkaround do MSFS 2024):Pular o walkaround não aplica multa direta, mas remove o bônus de inspeção visual pré-voo.Na prática: componentes que já entraram na faixa de desgaste crítico (ex.: < 15% de vida útil em pneus, freios, hélice ou vazamento de óleo) continuam ocultos na tela de briefing até falharem em voo. Se o piloto fizer o walkaround completo sem usar o TOOLS\_QUICK\_PREFLIGHT, o FlightCareer revela esses itens desgastados antes da partida, permitindo trocar a peça no hangar antes de decolar.







1\. Slew em Voo (SLEW\_ON / SLEW\_SET / SLEW\_TOGGLE)

Status: Confirmado e travado conforme decisão de 16/11.



Implementação: O SimConnect apenas escuta os eventos de Slew para registrar o timestamp e as coordenadas no log de auditoria do voo, disparando imediatamente a regra SLEW-INTEGRIDADE (invalidação do contrato ativo) se ocorrer fora do estado permitido em solo, sem tentar forçar SLEW\_OFF contra a vontade do usuário.



2\. Compensador: Trim Inoperante (\*\_TRIM\_DISABLED\_SET) e Trim Disparado (Runaway)

Decisão recomendada: Implementar ambos — são panes de altíssimo valor prático e realismo.



Como separar os dois comportamentos:



Trim Inoperante (Falha Passiva): Dispara ELEVATOR\_TRIM\_DISABLED\_SET (1) — e AILERON\_TRIM\_DISABLED\_SET / RUDDER\_TRIM\_DISABLED\_SET nos modelos equipados. O compensador congela na posição em que estava, obrigando você a manter pressão física no manche ou gerenciar o pitch pela potência até o pouso.



Trim Disparado / Runaway (Falha Ativa): O addon incrementa continuamente a ELEVATOR TRIM POSITION em direção ao batente (nariz em cima ou embaixo, \~0,5° a 1,0° por segundo) enquanto o circuito elétrico estiver energizado. O disparo só para quando o piloto corta o sistema acionando ELEVATOR\_TRIM\_DISABLED\_SET (1), desligando o Pitch Trim Switch / disjuntor correspondente, ou cortando o barramento aviônico/master.



Trava de Realismo no JSON da Aeronave: Habilite o Trim Runaway apenas em aeronaves que possuem compensador elétrico/servo de piloto automático (ex.: Baron, King Air, TBM, Citation, airliners). Aeronaves com compensador 100% mecânico a cabo/manivela (como C152/C172 básico) podem sofrer apenas Trim Inoperante (cabo emperrado), nunca Runaway.



3\. Auto-Leme (AUTO COORDINATION / AUTORUDDER\_TOGGLE)

Decisão recomendada: Sim, aplicar exatamente a mesma regra do Auto-Start (reduz XP / bônus de realismo, mas não invalida o voo).



Justificativa: Muitos pilotos voam com manche/joystick sem pedais físicos de leme e dependem do auto-leme para manter o eixo da pista no taxi e na corrida de decolagem/pouso. Invalidar o contrato por isso penalizaria o hardware do jogador.



Regra de pontuação: Monitore a SimVar AUTO COORDINATION (bool). Se permanecer ativa em qualquer momento entre a decolagem e o pouso, o voo segue válido e paga o frete integralmente, mas perde a tag/multiplicador de Assistências Desligadas (Full Realism) no cálculo final de XP.



4\. Superfície Travada (Jammed Flight Controls)Decisão recomendada: Seguir a recomendação de NÃO fazer travamento total de superfícies primárias (profundor, aileron, leme) — e, se quiser representar desgaste de cabos/atuadores, usar apenas a versão leve (curso limitado) ou focar em flaps.Por que evitar o travamento total: Sem force feedback real no manche, sobreescrever os eixos primários via SimConnect a cada frame gera briga com o input do joystick (jitter violento) e torna o pouso fisicamente impossível na maioria das situações, parecendo bug de controle em vez de pane.Alternativa segura (Versão Leve):Curso Limitado (Control Restriction): Em vez de travar em 0°, aplique um teto (clamp) limitando a deflexão máxima a ±55%–65% do curso normal. A aeronave continua perfeitamente pousável, mas exige curvas mais abertas e flare antecipado.Flap Travado / Assimétrico: Muito mais seguro e divertido no simulador — travar os flaps em 0° (pousar clean em pista mais longa) ou travar em posição intermediária (limita a $V\_{FE}$ no cruzeiro) testa o julgamento do piloto sem sabotar os controles primários.





1\. REPAIR\_AND\_REFUEL em Voo (Invalidação e Reaplicação)

Status: Confirmado e travado (recomendado).



Fluxo exato de execução:



Em voo (SIM ON GROUND == 0) ou após o Off-Block: Ao capturar REPAIR\_AND\_REFUEL, o FlightCareer invalida imediatamente o voo ativo (status INVALIDATED), aplica a penalidade de quebra de integridade/telemetria e reinjeta na sequência o combustível salvo em PilotAircraft.FuelAmount junto com todas as panes ativas persistidas no SQLite (neutralizando o efeito de "cura mágica" e tanque cheio).



No solo antes do voo: Se acionado sem REQUEST\_FUEL\_KEY, o voo ainda não iniciou (não invalida contrato), mas a rotina COMBUSTIVEL-REAL devolve o combustível ao valor do banco e reaplica as panes pendentes de hangar.



Caminhão de Combustível (REQUEST\_FUEL\_KEY) e Sincronização COMBUSTIVEL-REAL

Status: Decidido e travado (16/11/2026).



Ponto de atenção na implementação (Evitar Race Condition):



Como o caminhão de combustível do MSFS abastece gradualmente ao longo de vários segundos após REQUEST\_FUEL\_KEY, abra uma janela de abastecimento autorizado (IsFuelTruckAuthorized = true) no momento do evento enquanto SIM ON GROUND == 1 e motores desligados.



Enquanto FUEL TOTAL QUANTITY estiver subindo dentro dessa janela, suspenda temporariamente o rollback da COMBUSTIVEL-REAL.



Quando a quantidade estabilizar por \~3 segundos (ou o caminhão desconectar), calcule delta = QuantidadeFinal - QuantidadeInicial, registre a transação "Combustível (caminhão do sim)" via FuelPriceService, atualize PilotAircraft.FuelAmount no SQLite e reative a trava da COMBUSTIVEL-REAL.



Qualquer subida de combustível fora dessa janela (ex.: slider da barra Weight \& Balance no solo ou em voo) continua sendo bloqueada/revertida no solo e invalidando o voo se ocorrer no ar.



Panes de Válvula, Bomba e Linha via FUELSYSTEM\_\*

Decisão recomendada: Sim — atuar diretamente nas válvulas e bombas internas via FUELSYSTEM\_\* sem forçar a posição visual da seletora na cabine.



Por que essa abordagem é superior:



Sem guerra com o cockpit: Forçar FUEL\_SELECTOR\_SET para OFF move a alavanca física na cabine; se o piloto clicar nela de volta, cria um loop de disputa com o SimConnect. Atuando na camada interna (FUELSYSTEM\_\*), a alavanca permanece na posição escolhida pelo piloto, mas o fluxo real é interrompido — simulando fielmente uma bomba queimada, válvula solenoide travada fechada, linha obstruída ou vapor lock.



Mapeamento por componente:



Falha de Bomba (Fuel Pump): Dispara FUELSYSTEM\_PUMP\_OFF / FUELSYSTEM\_PUMP\_SET no índice da bomba afetada. A pressão cai e obriga o piloto a ligar a bomba elétrica auxiliar (Boost Pump) ou abrir o Crossfeed.



Falha de Válvula / Linha Obstruída (Fuel Valve / Line): Dispara FUELSYSTEM\_VALVE\_CLOSE / FUELSYSTEM\_VALVE\_SET na válvula da linha correspondente. O motor começa a falhar por falta de combustível (fuel starvation) mesmo com o seletor apontando para o tanque, forçando o piloto a trocar de tanque ou usar crossfeed.







Continuar de onde parou" com FlightSave / FlightLoad (§ 2)

Decisão recomendada: Aprovar com escopo restrito ao solo (Pernoite / Pátio) e reinjeção obrigatória do SQLite após o carregamento.



Como evitar armadilhas no MSFS 2024:



Salvar apenas no corte: Dispare SimConnect\_FlightSave somente quando a aeronave estiver parada (SIM ON GROUND == 1), freio de estacionamento acionado e motores cortados (Shutdown). Salvar estado em voo gera transientes de altitude/trim e abre brecha para save scumming antes de pousos difíceis.



Sincronização pós-load: O arquivo .flt restaura posição exata no pátio, clima e horário, mas não garante o estado interno de aviônicos complexos nem do seu banco. Assim que o evento de conclusão do FlightLoad disparar, o FlightCareer deve reaplicar automaticamente o combustível (PilotAircraft.FuelAmount), payload e panes persistidas no SQLite.



Plano de Voo Carregado no GPS (FlightPlanLoad)

Decisão recomendada: Aprovar como padrão no Dispatch, com uma chave opcional no Briefing para aeronaves com FMC próprio.



Fluxo ideal:



Ao iniciar o voo no Briefing, o FlightCareer gera o arquivo .pln temporário com a rota e as pernas da missão e chama SimConnect\_FlightPlanLoad, carregando os waypoints automaticamente no G1000, G3000, GNS 530/430 e aviônicos padrão.



Exceção prática: Inclua um toggle "Carregar rota no GPS do simulador" (marcado por padrão). Aeronaves de estudo que usam FMC externo/próprio (como Fenix, PMDG ou iniBuilds) ignoram o .pln nativo ou podem sofrer conflito se o plano for injetado à força. (APENAS AERODROMO DE ORIGEM E DE DESTINO DA LEG ATIVA, NADA DE ROTAS PERSONALIZADAS, OU PROCEDIMENTOS DAS CARTAS, ISSO FICARA A CARGO DO PILOTO).

3\. Flow API: Teleporte, Back On Track e PLANE\_CRASH

Decisão recomendada: Aprovar integralmente — fecha a blindagem anti-cheat e o ciclo de sinistro.



Regras de execução:



Teleporte e Back On Track: O recurso Back On Track do MSFS 2024 (que reposiciona o avião na pista após saída de pista ou acidente) deve ter o mesmo tratamento do Slew: se ocorrer após o início do bloco (Off-Block) ou em voo, invalida o contrato na hora e impede o registro do pouso.



PLANE\_CRASH: Trate como gatilho oficial de Acidente / Sinistro, encerrando o voo como CRASHED, colocando a matrícula em estado AOG (Aircraft on Ground) e acionando a franquia de seguro ou custo de resgate na oficina. Mantenha apenas o grace period de \~10 segundos no carregamento inicial da sessão para evitar falsos positivos de colisão de cenário no spawn.



4\. Alertas Dentro do Sim (DialogAction, Destaque de Instrumento e Vibração) junto com o HUD

Decisão recomendada: Aprovar em modo modular (configurável por nível de realismo), separando uso em solo e em voo.



Divisão recomendada para não poluir a cabine:



Vibração do controle (Rumble): Excelente para manter ativo em voo — dá retorno tátil imediato em toque duro (hard landing), stall, overspeed ou estouro de pneu sem tirar os olhos da pista.



Destaque de instrumento (Instrument Highlight): Muito útil no modo Assistido / Treinamento (aponta o disjuntor, seletora ou mostrador afetado por uma pane), mas deve poder ser desligado por pilotos que preferem diagnóstico 100% raiz.



DialogAction (caixas de texto/áudio nativas): Restrinja os popups visuais ao solo (instruções de rampa, walkaround e encerramento). Em voo, evite abrir caixas de diálogo nativas que disputam espaço ou foco com o HUD externo do FlightCareer — prefira apenas alertas sonoros curtos.



FailureAction com HealthPercent vs. Eventos TOGGLE\_\*

Decisão recomendada: Aprovar o teste como camada complementar em um Strategy Pattern, mantendo os TOGGLE\_\* e FUELSYSTEM\_\* como fallback.



Por que não substituir tudo de uma vez:



O FailureAction com HealthPercent (0–100%) resolve bem a lacuna dos enums não documentados de subsistemas novos do MSFS 2024 e permite degradação gradual nativa.



Porém, aeronaves portadas do MSFS 2020 ou modelos de terceiros nem sempre expõem todos os nós de saúde para o HealthPercent. A abordagem mais segura é criar uma interface IFailureDriver no C# e registrar no JSON de especificações de cada aeronave qual método (HealthPercent ou LegacyToggle) controla cada sistema com 100% de resposta comprovada.





Abrir o app e olhar o Hangar: dotnet run --project src/FlightCareer.UI. Faça uma inspeção e veja se os ícones amarelo/vermelho aparecem bem nos cards. É a única parte de hoje que ninguém viu na tela.





(ALINHAR OS NOMES DE TEXTOS NO HANGAR COM AS CALIBRACOES DE HORAS DOS GRUPOS DE COMPONENTES, EXEMPLO NO HANGAR ESTA 1800 HORAS DE REVISAO PARA MOTOR).



AJUSTAR EM TODO O CODIGO DA UI, O USO DO R$ PARA APENAS $.



VERFICAR OS CUSTOS DAS PEÇAS ESTAO SENDO PAGO PROPORCIONAL A PORCENTAGEM, NA VIDA REAL POR EX, NAO TEM COMO TROCAR APENAS 5% DO FREIO, OU TROCA 100% OU NAO, PAGANDO O PREÇO INTEGRAL DA PEÇA. EX. NO HANGAR, UM FREIO QUE ESTA 98% DE INTEGRIDADE, O PILOTO PAGA APENAS $9 PARA DEIXAR 100%. O IDEAL ERA PARA O VALOR COMPLETO DA PEÇA CERTO? AJUSTE ISSO E VERIFIQUE SE FAZ SENTIDO O QUE FALEI, E AJUSTE EM TODAS AS PEÇAS.



REMOVER A FUNCAO DE CONFIGURACAO DA CABINA, OU MELHORAR A MECANICA DE GAMEPLAY.





Em casa (com o MSFS)

Testes rápidos no simulador, já listados no handoff:

Ctrl+E e o pedido do caminhão de combustível chegam ao app?

Sinal do pitch numa captura subindo e descendo.

Pausar pelo menu durante a nota dos passageiros.

Mapa elétrico (Developer Mode › Electrical System Debug) do C152, C172, Baron e TBM: anote os índices dos circuitos do motor do flap, aquecimento do pitot, GPS, piloto automático, transponder e luzes. É a fase F1 do plano de iluminação e só você consegue fazer.

