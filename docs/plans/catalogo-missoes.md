# Catálogo de missões em JSON (plano)

Estado: **aguardando a REORG-CATEGORIAS** (uma missão por código) e as decisões do usuário abaixo.

## 1. Objetivo técnico
Cada missão passa a ser um item de catálogo em JSON com id de texto estável (`medevac`, `carga_fragil`...) e declara: código exibido (ex.: 3.3.4), categoria, filtro do quadro, família de pagamento, fase, M_Tipo, regra de destino (`pista_curta`, `outro_pais`, `volta_a_origem`, `hub_de_saude`...), regras de conclusão e pesos por porte. O gerador aplica as regras pelas propriedades, não por `if (sub.Id == ...)`. Prova: `career-stab` com o mesmo resultado de antes.

## 2. Pedido do usuário para o porte 1 (2026-11-23, texto resumido)
**A. Transporte aéreo regular de passageiros**
1. Troncal doméstico (trunk routes): capitais e grandes centros, alta densidade, pontualidade rígida (janela de block time), narrow-body.
2. Longo curso internacional (long-haul / ETOPS): transcontinental, wide-body; combustível para alternados remotos, step climb; margem alta, custo alto.
3. Alimentador de hub (feeder): voos curtos trazendo passageiros de aeroportos regionais para a malha internacional do hub.

**B. Carga aérea pesada e expressa (all-cargo)**
4. Carga geral paletizada (ULD): contêineres entre centros industriais, cargueiros dedicados.
5. Logística expressa / e-commerce (overnight): voos noturnos com horário de corte; penalidade severa por atraso, bônus de pontualidade.
6. Carga crítica / perigosa (DGR e cadeia fria): fármacos refrigerados, peças just-in-time; exige pouso suave (limite baixo de G) e razão de subida/descida controlada.

**C. Aviação executiva de alta gama (FBO / GAT)**
7. Táxi aéreo executivo internacional / intercontinental: CEOs, chefes de estado, celebridades em jatos de longo alcance.
8. Repatriamento médico / UTI aérea de longa distância: entre grandes centros hospitalares, prioridade de ATC (Lifeguard), sem turbulência severa.

**D. Operações técnicas, translados e governo**
9. Translado (ferry / posicionamento): aeronave vazia entre hubs (cobrir AOG) ou entrega de fábrica; valor fixo.
10. Translado para manutenção pesada (Check C / D): aeronave com restrição (trem travado embaixo, limite de altitude, sem pressurização abaixo do FL100) até um MRO grande.
11. Fretamento especial (esportivo / diplomático): delegações, turnês, comitivas fora da grade regular.

## 3. Comparação com o que existe (depois da REORG)
| Pedido | Existe hoje | Falta |
|---|---|---|
| 1 Troncal | 1.1.1 Doméstico / Hub Link (absorveu a Ponte Aérea) | block time só no XP (bônus de prazo); exigência de narrow-body não existe |
| 2 Long-haul | 1.1.3 Intercontinental | ETOPS, alternados e step climb só como texto |
| 3 Feeder | 2.1.1 Alimentação de Hub (sai do porte 2) | não existe no porte 1 |
| 4 ULD | 1.2.1 / 1.2.2 / 1.2.3 (por distância) | — |
| 5 Overnight | absorvido no 1.2.1 | missão própria; horário de corte e penalidade de atraso (contraria a decisão "pagamento não depende de pontualidade") |
| 6 DGR / cadeia fria | 3.2.4 Carga Frágil (sem regra de conclusão) | limite de G no pouso e de razão de subida/descida |
| 7 Executivo intercontinental | 1.3.1 (até 2.500 NM) e 3.1.4 VIP (300–900 NM) | longo alcance intercontinental |
| 8 UTI aérea longa distância | — (MEDEVAC 3.3.4 é 50–350 NM) | missão nova; regra de conforto (turbulência) |
| 9 Ferry | 1.3.3 Translados / Ferry | valor fixo |
| 10 Check C/D com restrição | — | missão nova; regra de voo restrito (altitude máx., trem embaixo) |
| 11 Fretamento especial | 1.3.4 Diplomático e Militar | variantes esportivas |
| (fora da lista) | 1.1.2 Regional Internacional, 1.2.2 Carga Regional Internacional, 1.3.2 Crew & Parts, 3.1.1 Executivo Regional, 3.2.4 Carga Frágil, 3.3.4 MEDEVAC | decidir se ficam no porte 1 |

## 4. Decisões do usuário (2026-11-23)
- As missões que já existem no porte 1 **ficam** (inclusive as que não estão na lista acima).
- Atraso **só pesa no XP** (overnight e block time incluídos); o pagamento continua sem depender de pontualidade.
- Ordem: (1) catálogo JSON como **reorganização técnica pura** (mesmas missões, mesmas chances, `career-stab` igual); (2) depois, numa etapa própria, as missões novas do porte 1 (feeder, overnight, UTI aérea longa distância, Check C/D) e as regras de conclusão novas (G/razão no DGR, turbulência na UTI, voo restrito no Check C/D).

## 5. Nova diretriz do usuário (2026-11-23)
- As missões **deixam de ser amarradas ao porte** (tier). Haverá **uma lista geral de tipos de missão**; cada tipo de aeroporto recebe mais de uns tipos do que de outros (ex.: tier 1 recebe pouca instrução, tier 3 recebe muita), além dos **aeroportos preferenciais** (ex.: aeródromo com dropzone recebe mais paraquedismo, aeroclube mais instrução, aeródromo turístico mais panorâmico).
- Passos: (1) lista geral completa dos tipos de missão (abaixo; o usuário ainda vai mandar mais exemplos); (2) definir, por tipo de aeroporto, a frequência de cada missão e as preferências; (3) implementar no catálogo JSON.
- Os tiers dos aeroportos (OurAirports large/medium/small) **não mudam por enquanto**.

## 6. Lista geral de tipos de missão (rascunho v3, 2026-11-23)
Junta as listas do usuário para os portes 1, 2 e 3 com o que já existe. A lista define **o tipo e as regras**; frequência, destinos, distância, pax e carga por tipo de aeroporto vêm no passo 2.

Legenda de status: **E** = existe (código atual entre parênteses); **P** = parcial (existe parecida; falta o que está na coluna de regras); **N** = nova.
Regras: o que o addon confere pela telemetria para dar o contrato como cumprido (sem regra = só pousar no destino). **HUD:** instruções dinâmicas mostradas durante o voo. Prazo de entrega e pontualidade pesam **só no XP** (decisão § 4). Itens com ⚠SDK dependem de confirmar SimVar/evento na documentação do MSFS 2024.

### A. Instrução e formação
| id | Missão | St. | Regras de conclusão / restrições | HUD |
|---|---|---|---|---|
| instrucao_local | Instrução local (área de instrução + TGL, volta à origem) | E (3.4.1) | passar ≤ 3 NM do centro da área abaixo do teto; N TGL; pouso completo na origem | "Siga para a área (rumo, distância)"; "Na área: manobras"; "TGL x/N" |
| circuito_tgl | Circuito de tráfego (TGL, adaptação de aluno) | E (3.5.2) | 3–6 TGL válidos (toque ≥ 1 s, sem parar, nova decolagem); pouso completo | "TGL x/N"; aviso se o toque não valeu (quique, parou) |
| navegacao_vfr | Navegação VFR (triângulo entre três aeródromos tier 3, ponto de reportagem obrigatório, pouso completo em cada etapa) | P (3.4.2: uma etapa só) | sobrevoar o ponto de reportagem (≤ 2 NM); pouso completo em cada aeródromo do triângulo, na ordem | "Próxima etapa: X (rumo, distância)"; "Reporte em P" |
| instrucao_ifr | Instrução por instrumentos (aproximações RNP/NDB/VOR ou órbitas num aeródromo próximo) | N | N aproximações até o mínimo, com altitude ±100 ft e proa ±5° nos trechos estabilizados; ⚠SDK para conferir procedimento/auxílio sintonizado (sem isso: perfil vertical + alinhamento com a pista) | "Aproximação x/N"; desvio de altitude/proa ao vivo |
| adaptacao_noturna | Adaptação noturna (decolagens e pousos locais à noite) | N | N pousos completos com sol abaixo do horizonte; luzes de navegação e pouso ligadas; ⚠SDK hora local/período do dia e balizamento acionado por rádio (PCL) | "Pouso x/N"; "Ligue as luzes" |
| cheque | Cheque / revalidação (PC, IFR, multimotor) | P (3.4.3: área + 2 TGL) | trechos avaliados com altitude ±100 ft e velocidade ±5 kt; toque na faixa de toque; nota do cheque no resultado | "Tarefa atual: …"; tolerâncias ao vivo |

### B. Linha aérea e regular
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| ponte_aerea | Ponte aérea de alta frequência (dois centros econômicos próximos) | P (absorvida no 1.1.1) | — | — |
| troncal_domestico | Troncal doméstico (capitais e grandes centros; narrow-body) | E (1.1.1, 2.1.2) | block time só no XP | prazo de chegada |
| regional_internacional | Regional internacional (países vizinhos) | E (1.1.2) | — | — |
| longo_curso | Longo curso internacional (long-haul / ETOPS) | E (1.1.3) | ETOPS, alternados e step climb só como texto | — |
| feeder_inbound | Alimentador de hub (interior → hub) | E (2.1.1) | — | — |
| feeder_outbound | Distribuidor de hub (hub → polos regionais) | N | — | — |
| sub_regional | Sub-regional / alimentador leve (cidades pequenas tier 3 ↔ capital estadual) | N | — | — |
| pinga_pinga | Pinga-pinga (várias escalas curtas no mesmo contrato, sem reabastecer): regional de passageiros e **entrega postal / encomenda rural** em 3–4 pistas de fazenda na mesma manhã | P (multi-leg da fase 5) | pouso em cada escala na ordem; combustível não pode subir entre trechos | "Escala x/N: próxima Y" |
| regional_essencial | Voo regional essencial subsidiado e **transporte comunitário / táxi aéreo social** (moradores, professores, trabalhadores entre pistas de aldeia e a cidade com banco e comércio) (comunidades isoladas ↔ capital regional: Amazônia, EAS nos EUA, PSO na Europa, norte do Canadá, Alasca, outback, Papua, ilhas do Pacífico) | N | — | — |

### C. Executiva, charter e turismo
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| executivo_corporativo | Táxi aéreo corporativo (C-level; aeroportos executivos ↔ polos industriais) | E (1.3.1, 3.1.1) | conforto de passageiros no XP | — |
| executivo_intercontinental | Executivo internacional / intercontinental (jatos de longo alcance) | P (1.3.1 até 2.500 NM) | — | — |
| vip_internacional | VIP internacional curto (país vizinho) | E (1.7.4, 2.7.4) | — | — |
| agro_executivo | Táxi aéreo do agronegócio (produtores, agrônomos, investidores; capitais/tier 2 ↔ cidades agrícolas) | E (2.2.1–2.2.3) | — | — |
| familia_alto_padrao | Família de alto padrão | E (3.1.2) | — | — |
| equipes_urgentes | Equipes urgentes | E (3.1.3) | — | — |
| charter_lazer | Fretamento turístico (grupos em feriados/férias, weekend charter de hubs executivos, **transfer para pousada de pesca/lodge** em pista de grama isolada com bagagem pesada) | N | — | — |
| charter_esportivo | Charter esportivo e de turnês (times, shows; madrugada pós-evento) | P (1.3.4, só texto) | — | — |
| transfer_metropolitano | Transfer executivo inter-metropolitano (ponto a ponto curto) | N | — | — |
| panoramico | Voo panorâmico local (sightseeing; volta à origem) e **safári aéreo / fotografia de fauna** (baixo e lento sobre áreas alagadas, rios, savanas e parques pelo mundo, saindo da pista da pousada) | E (3.5.1) | passar ≤ 1,5 NM de cada ponto abaixo de 3.000 ft AGL; **nova:** altitude e velocidade de observação mantidas sobre cada ponto (faixa por avião) | "Próximo ponto: X (rumo, distância)"; "Mantenha N kt / N ft" |
| demo_flight | Voo de demonstração comercial (potencial comprador; decolagem curta e cruzeiro) | N | rolagem de decolagem ≤ referência do avião; cruzeiro estável por N min; pouso na origem | "Demonstre: decolagem curta"; "Cruzeiro estável x/N min" |

### D. Carga
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| carga_paletizada | Carga geral paletizada (ULD; cargueiros dedicados) | E (1.2.1–1.2.3) | — | — |
| carga_overnight | Carga expressa overnight (correios, centros de triagem; cargueiro noturno) | P (2.3.3, 1.7.6) | atraso só no XP | prazo |
| malotes | Malotes bancários e documentos (fim da tarde, interior → centros de compensação) | E (2.3.3) | — | — |
| carga_valores | Transporte de valores (numerário, metais preciosos) | P (2.3.3) | — | — |
| ecommerce | E-commerce e carga fracionada (same-day/next-day; encomendas, eletrônicos e medicamentos no interior) | E (2.3.1) | atraso só no XP | prazo |
| carga_jit | Carga industrial just-in-time / AOG (peças, eletrônicos, maquinário; inclui safra AOG e **harvest rescue**: peça hidráulica/eletrônica pousando na pista de terra da fazenda) | P (1.3.2, 3.2.1, 4.3.2) | atraso só no XP | prazo |
| cadeia_fria | Cadeia fria e perecíveis (frutas, carnes, pescados frescos e produtos regionais de cidades litorâneas/ribeirinhas para hubs) | N | razão de subida/descida dentro de limite; atraso só no XP | razão ao vivo; prazo |
| carga_dgr | Carga perigosa/controlada e farmacêutica (vacinas, medicamentos de alto custo, explosivos para mineração, químicos controlados) | P (1.7.2 Carga Frágil) | G máximo no pouso (baixíssimo nos explosivos); razão de subida/descida; **sem sobrevoo urbano** (aproximação: não passar a menos de N NM de aeroportos tier 1/2 fora da origem/destino; dados de área urbana não existem) | G e razão ao vivo; aviso de área urbana |
| carga_sensivel | Carga leve sensível e viva: insumos biológicos e sementes nobres, genética bovina (botijões de nitrogênio) com veterinário de campo, soltura de fauna silvestre reabilitada em reserva com pista de apoio | N | prazo curto (XP); pouso suave (G) | prazo; G no pouso |
| carga_combi | Carga combi (passageiros + suprimentos; Amazônia e regiões remotas) | N | — | — |
| insumos_agro | Insumos e peças agro | E (2.3.2, 3.2.2, 3.2.3, 4.2.3) | — | — |
| suprimentos_remotos | Abastecimento de rancho e mantimentos (fazenda, vilarejo ribeirinho, pelotão de fronteira, comunidade isolada) e materiais de construção/ferramentas (cimento, telhas, motosserras, geradores, peças de motor de barco) | E (4.1.1) | carga volumosa: ⚠ CG crítico só como texto (W&B não é conferido hoje) | — |
| conexao_pista_curta | Conexão tier 3 → tier 4 (suprimentos ou passageiros para pista de terra/fazenda próxima) | N | destino tier 4 próximo | — |

### E. Saúde
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| orgaos | Transporte de órgãos para transplante (contagem regressiva; atraso e arremetida tiram XP) | P (3.3.3) | prazo rígido (isquemia) e arremetida: **só perda de XP** (decisão 2026-11-23) | contagem regressiva |
| uti_inter_hospitalar | Remoção aeromédica e UTI inter-hospitalar (hospital pequeno → referência tier 2) | E (3.3.1, 3.3.2) | conforto (turbulência/G) no XP | — |
| medevac_remoto | Evacuação aeromédica remota / bush medevac (pista curta de terra ou grama, às vezes com obstáculos; acidente de trabalho, picada de cobra, parto complicado → hospital tier 2/3) | E (1.7.3, 2.7.3, 3.7.2, 4.3.1) | — | — |
| uti_longa_distancia | Repatriamento médico / UTI de longa distância (Lifeguard) | N | evitar turbulência severa (G) | G ao vivo |
| saude_familia | Equipes médicas e vacinas (especialistas para municípios afastados; **ronda médica e odontológica indígena/ribeirinha** com caixas térmicas) — ida e volta no mesmo dia | N | ida e volta no mesmo contrato; na ronda em aldeia: **pouso de volta antes do pôr do sol** (⚠SDK hora/sol) | "Etapa: ida / volta"; tempo até o pôr do sol |

### F. Trabalho aéreo (SAE), agro e vigilância
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| aerolevantamento | Aerofotogrametria e mapeamento (pernas paralelas com altitude e proa constantes) | E (3.6.1) | N pernas: rumo ±15°, altitude ±150 ft, IAS ±10 kt em ≥ 80 % do trecho | "Perna x/N: rumo R, N ft, N kt"; desvio ao vivo |
| paraquedismo | Lançamento de paraquedistas (12–14 mil ft; redução para o lançamento; descida rápida) | E (3.6.2) | subir à altitude de salto a ≤ 2 NM da zona; 30 s estável (±200 ft, IAS ≤ 1,3 Vs1); descida sem choque térmico (aviso) | "Suba para N ft sobre a zona"; "Reduza para N kt e mantenha 30 s — lançamento"; "Paraquedistas fora: desça" |
| pulverizacao | Pulverização / aplicação aérea agrícola (passadas paralelas a 10–30 ft sobre o talhão; Air Tractor, Ipanema, Thrush) | N | N passadas dentro do polígono do talhão, rumo ±10°, 10–30 ft AGL, velocidade de aplicação; só aviões agrícolas | "Passada x/N"; altura AGL ao vivo; "Curva de balão" |
| reboque_planador | Reboque de planadores (até 3.000–4.000 ft e volta ao aeroclube) | N | ⚠SDK planador engatado/liberado (o MSFS 2024 tem reboque nativo); sem isso: subida à altitude de liberação e pouso na origem | "Suba para N ft"; "Planador liberado: retorne" |
| publicidade_aerea | Publicidade aérea (reboque de faixa sobre praias e eventos) | N | ⚠SDK faixa rebocada; trajeto ao longo da orla/evento a baixa velocidade e altura por N min | "Siga a orla"; velocidade/altura ao vivo |
| patrulha_vigilancia | Patrulha e inspeção de área: prevenção de incêndios (reservas, parques, eucalipto), monitoramento de garimpo ilegal e desmatamento, inspeção de propriedade e contagem de gado (pastos, cercas, pivôs, aguadas) | N | cobrir N setores/pontos da área a baixa altura (≤ N ft AGL) e velocidade reduzida; inspeção de propriedade decola e pousa na pista da fazenda | "Setor x/N"; altura/velocidade ao vivo |
| inspecao_linhas | Inspeção de linhas de transmissão, ferrovias e dutos (baixa altura seguindo o traçado) | N | seguir a sequência de pontos do traçado a ≤ 0,5 NM e abaixo de N ft AGL | "Ponto x/N"; desvio lateral |
| monitoramento_trafego | Monitoramento de tráfego e eventos (órbitas sobre rodovias/eventos) | N | permanecer em órbita ≤ R NM do alvo por N min | "Órbita: x/N min" |
| sar | Busca e salvamento visual (creeping line / expanding square em área rural, serrana ou costeira) | N | cumprir o padrão na área; alvo achado ao passar ≤ 0,3 NM dele | "Perna x/N do padrão" |
| calibracao_auxilios | Calibração de auxílios (GEIV: órbitas e aproximações em ILS/VOR/PAPI) | N | ⚠SDK auxílio sintonizado; N aproximações alinhadas e órbitas a raio fixo | "Aproximação x/N"; "Órbita raio R" |

### G. Operações técnicas e translados
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| ferry_posicionamento | Translado / reposicionamento (vazio, empty leg, cobrir AOG) e **extração de aeronave** de pista extremamente curta/alta (hot and high) até um tier 3 asfaltado | E (1.3.3, 3.5.3) | sem pax nem carga; na extração: combustível mínimo na partida (≤ N % ou o necessário + reserva) | combustível a bordo × limite |
| translado_manutencao | Translado para oficina homologada (50 h/100 h, TBO, Check A/C/D, IAM, CVA) | P (2.4.1) | destino = oficina; prazo de horas só no XP | — |
| ferry_permit | Translado com restrição técnica (flap inoperante, trem travado, sem pressurização abaixo de 10.000 ft) | N | altitude máxima; trem embaixo; flaps fixos (conforme a restrição sorteada) | restrição ativa e desvio |
| check_flight | Voo de teste pós-manutenção e de aviônicos/bússola (swing check) | N | roteiro local: subida a N ft, potência/rotação, curvas pelos rumos cardinais, pouso na origem | "Passo x/N do teste" |
| crew_parts | Logística de apoio a aeronaves (tripulação e peças; **field AOG rescue**: mecânico, ferramentas, bateria ou hélice até a pista de terra onde outro avião ficou preso) | E (1.3.2) | — | — |
| translado_piloto_agricola | Transporte de piloto agrícola | E (4.2.2) | — | — |
| pessoas_remotas | Transporte de pessoas para locais remotos: farm shuttle (patrão/gerente), equipe e batedor da fazenda, rodízio de turno de 15/30 dias (canteiros de barragem, ponte, linha de transmissão, mineração), brigadistas florestais com equipamento para pista avançada | E (4.1.2, 4.1.3, 4.2.1) | — | — |

### H. Governo, defesa e humanitário
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| autoridades | Autoridades, comitivas e documentos oficiais (urnas) | E (2.4.2) | pontualidade só no XP | prazo |
| diplomatico | Diplomático e comitivas de estado | E (1.3.4) | — | — |
| militar_can | Correio Aéreo Nacional / logística militar (bases e pelotões de fronteira; equivalentes em outros países) | P (1.3.4) | — | — |
| humanitario | Apoio humanitário e defesa civil (enchentes e secas: água potável, lonas, medicamentos em pistas parcialmente alagadas ou improvisadas) | N | — | — |

### I. Operações remotas, bush e campo
| id | Missão | St. | Regras / restrições | HUD |
|---|---|---|---|---|
| combustivel_tambor | Transporte de combustível em tambor (drum fuel run: gasolina, diesel, AvGas/Jet-A1 para geradores de comunidades remotas ou pontos de reabastecimento avançados) | N | carga perigosa: pouso suave (G); peso alto em pista curta | G no pouso |
| apoio_acampamento | Apoio a acampamentos e expedições: suprimento de prospecção/mineração (sondagem, amostras, peças de bomba, alimentação), expedições científicas e documentários (biólogos, arqueólogos, cinegrafistas com câmeras, drones e baterias), reabastecimento de cache (suprimentos e combustível numa pista isolada para missões futuras) | N | equipamento delicado: pouso suave (G); cache: destino = pista remota sem estrutura | G no pouso |
| instrucao_stol | Instrução bush / STOL (pouso curto, vento de través em terra, aproximação em rampa acentuada sobre obstáculos) | N | N pousos com parada em ≤ N ft após o toque; ângulo de aproximação ≥ N° nos últimos N ft; ao menos 1 pouso com vento de través ≥ N kt (quando houver) | "Pouso curto x/N: parou em N ft"; ângulo ao vivo |
| evacuacao_por_do_sol | Evacuação pré-tempestade / limite de pôr do sol (decolar de pista sem luzes e chegar a aeródromo com balizamento antes do pôr do sol + 30 min) | N | prazo = pôr do sol + 30 min (⚠SDK hora/sol): **só perda de XP**, como órgãos; destino com pista iluminada (`runways.csv` tem `lighted`) | contagem regressiva até o limite |

Resumo v3: 69 tipos — 27 existem, 13 parciais, 29 novos.

## 7. Princípios da lista (usuário, 2026-11-23)
- **O addon é global.** Nenhum tipo de missão é só do Brasil: os exemplos brasileiros são um caso de cada tipo. Ao montar os dados, levantar os equivalentes pelo mundo (ex.: regional essencial acima; CAN ↔ correio militar de outros países; agro ↔ polos agrícolas de cada continente). Confirmar exemplos reais antes de gravar dados (não inventar).
- **A lista define o tipo e as regras; o aeroporto define a frequência.** Cada tipo declara o que é e como se cumpre; depois, por tipo/porte de aeroporto e preferências (dropzone, aeroclube, turístico, hub de saúde, MRO...), definem-se pesos de criação, destinos, distância, passageiros e carga.
- **Sem duplicar por porte:** a mesma missão em portes diferentes muda de frequência e de tamanho, não de tipo.
- **Cada tipo de missão tem no catálogo:**
  1. id estável, nome, grupo, filtro do quadro, fase, família de pagamento, M_Tipo de XP;
  2. **vários textos de RP** (≥ 3 variantes de título e descrição; neutros quanto a país, com marcadores de lugar, cliente, carga etc.; variantes regionais quando fizer sentido);
  3. **dados procedurais quando necessário:** restrições (ex.: altitude máxima, trem embaixo, sem pressurização), regras de conclusão (ex.: G máximo no pouso, razão de subida/descida, área a sobrevoar, TGL), prazo (expiração do contrato e, quando houver, prazo de entrega que pesa só no XP), carga/pax típicos e regra de destino (`volta_a_origem`, `pista_curta`, `outro_pais`, `hub_de_saude`, `mro`...).
- **HUD com instruções dinâmicas:** toda missão com regra de conclusão mostra no HUD o passo atual e as tolerâncias ao vivo (ex.: "Reduza para 80 kt e mantenha 30 s — lançamento", "Passada 3/6 — 18 ft AGL", "TGL 2/4"). As instruções ficam no catálogo (passos da missão), não fixas no código; o HUD só exibe o que o rastreador da missão informa.
- **Decisões 2026-11-23:** (a) **nenhum prazo falha o contrato** — órgãos, evacuação antes do pôr do sol e qualquer outro prazo pesam **só no XP**; (b) **ferry só da própria frota**: não haverá ferry de entrega de aeronave nova/de terceiros por enquanto — os textos de "entrega ao proprietário/novo dono" do 3.5.3, 1.3.3 e 4.2.2 saem no catálogo; (c) **todo item que depende do simulador é conferido na documentação do SDK do MSFS 2024 antes de implementar** (⚠SDK), sem exceção.
- **Tipo × variante de RP:** missões semelhantes são agrupadas num **tipo** (mesmas regras de conclusão), mas cada tipo tem **variantes de RP** com texto, cliente, carga/pax, distância e destino próprios (ex.: `pessoas_remotas` → farm shuttle, rodízio de turno, brigadistas; `apoio_acampamento` → mineração, expedição científica, cache). O quadro deve parecer ter muitas missões diferentes; o agrupamento é só técnico. Cada variante tem ≥ 3 textos.
- **Regras reutilizáveis (modificadores):** regras que valem para vários tipos ficam como blocos combináveis no catálogo, não por tipo: `pouso_suave` (G máximo no toque), `razao_limitada` (subida/descida), `sem_sobrevoo_urbano`, `antes_do_por_do_sol` (⚠SDK), `combustivel_minimo_na_partida`, `passagem_baixa_antes_do_pouso` (**strip inspection pass**: passar a ≤ N ft AGL sobre a pista antes de pousar, usado em destinos tier 4), `ida_e_volta`, `escalas_em_ordem`, `area_a_cobrir`, `pernas_paralelas`, `orbita`, `seguir_tracado`.

## 8. Passo 1 — perfis dos aeroportos (rascunho, 2026-11-23)
Script: `tools/places/airport_profiles.py` (lê `airports.csv`, `runways.csv`, `Data/places/*.json`, `hubs_data.json` e, se houver, a base do Little Navmap para marcar o que existe no MSFS 2024). Mesmo filtro e mesmo tier do `DataSeeder`.

- Banco do jogo: **47.718** aeroportos; **28.071 existem no MSFS 2024** (tier 1: 936 · tier 2: 3.794 · tier 3: 7.067 · tier 4: 16.274). Contagens abaixo = só os que existem no simulador.
- Um aeroporto pode ter vários perfis; **19.932 não têm nenhum** (usam só a frequência base do tier).

| Perfil | Critério | Qtde no MSFS | Por tier (1/2/3/4) | Conferência |
|---|---|---|---|---|
| zona_salto | `dropzones.json` | 158 | 8 / 33 / 56 / 61 | ok (curado + OSM) |
| aeroclube | `training_aerodromes.json` | 407 | 41 / 139 / 131 / 96 | ok |
| turistico_curado | `scenic_aerodromes.json` | 16 | 1 / 8 / 6 / 1 | ok |
| turistico_poi | soma da popularidade dos POIs **naturais/lazer** (ilha, lago, parque nacional, cachoeira, pico, praia, caverna, cabo, montanha, vulcão, reserva, UNESCO, farol, castelo) a ≤ 15 NM; 3 % mais altos dos tier 2–4; nunca em base militar | 671 | — / 202 / 184 / 285 | razoável (Mount Cook, Kerama, Ærø, Portimão, Codrington); Dinamarca e Suíça aparecem muito pela densidade de castelos/ilhas — usar multiplicador moderado |
| hub_cargo, hub_commercialpax, hub_executive, hub_feeder, hub_health, hub_instruction, hub_maintenance, hub_agro | `hubs_data.json` (só **BR** e **GLOBAL**) | 99–232 cada | concentrados nos tier 1–2 | ⚠ dados com ruído: Ubatuba e Santos Dumont como agro, Campo de Marte como saúde/agro; e só BR + lista global curta — revisar/expandir antes de usar como perfil forte |
| fazenda | nome com fazenda, usina, sítio, agropecuária, agrícola, estância, hacienda, finca, granja, farm, ranch, plantation, station, estate (exceto air/naval/research station etc.), só tier 3–4 | 3.151 | — / — / 641 / 2.510 | bom no BR (1.630) e EUA (1.379); fraco fora deles (AU 52, AR 4) — nomes de fazenda em outros países nem sempre têm essas palavras |
| militar | nome com air base, air force, AFB, base aérea, naval air, army airfield, RAF, militar… | 623 | 3 / 401 / 146 / 73 | ok (Grand Forks AFB, RAF Marham, Vidsel, bases na Síria/Ucrânia) |
| militar_misto | militar + voo regular ou "international/regional/municipal" no nome (uso civil e militar) | 49 | 9 / 37 / 3 / — | ok (Bergamo, Sacheon, Karup, Sierra Vista) — missões civis normais + militares |
| isolado | tier 3–4 a ≥ 150 NM do tier 1/2 mais próximo; tier 2 a ≥ 150 NM de outro tier 1/2 | 524 | — / 94 / 118 / 312 | ok (Tefé, São Gabriel da Cachoeira, Fernando de Noronha, Funafuti, Ilha de Páscoa, Kulusuk, Faroé, Alasca, Sibéria, Pacífico) |
| interior_distante | tier 3–4 a 75–150 NM do tier 1/2 mais próximo | 2.439 | — / — / 576 / 1.863 | ok (interior agrícola e sertão) |
| altitude | elevação ≥ 5.000 ft | 1.036 | 34 / 157 / 235 / 610 | ok (Lukla, Santa Fe, Andes, Himalaia, Mongólia) |

Achados para depois:
- **Tier distorcido** (não mexer por ora, decisão § 5): Congonhas, Santos Dumont, Viracopos, Porto Alegre etc. são tier 2; o perfil não corrige isso.
- `hubs_data.json` precisa de revisão e cobertura mundial antes de virar perfil forte (hoje BR + uma lista "GLOBAL").
- Sem dados para: área urbana (sem sobrevoo urbano), mineração/canteiro de obras, hospital real perto do aeroporto (usa `hub_health`), oficina homologada real (usa `hub_maintenance`).

## 9. Dados curados e pesos por aeroporto (2026-11-23)

### 9.1 Listas curadas (enviadas pelo usuário, códigos conferidos no MSFS 2024 pela base do Little Navmap)
| Arquivo (`tools/places/`) | Papel | Linhas | Ajustes feitos na conferência |
|---|---|---|---|
| `scenic_aerodromes_curated.csv` (+ coluna `role`) | turístico (panorâmico / destino) | 104 | SDBZ→SBBZ (Búzios), SBLN→SBLE (Lençóis), SSBR→SSRS (Barreirinhas), SVLR→SVRS (Los Roques), OJAI→OJAQ (Aqaba), SCFA→SCCF (Calama); fora: CYBA e VDSA (não estão no MSFS), CYWH (hidrobase), LSEZ (heliporto) |
| `scenic_pois_curated.csv` | 317 pontos turísticos → aeródromos | 317 | SDBZ→SBBZ, SNTF/SSPG removidos, FMMO→FMMV; fora: FSPP, MMTL, VVVD, WAHI |
| `cargo_hubs_curated.csv` | carga | 208 | SBNT→SBSG, SPIM→SPJC, OKKK→OKBK, FLLS→FLKK; fora: LEZG e Ezhou (não estão no MSFS) |
| `pax_hubs_curated.csv` | linha aérea | 210 | SBNT→SBSG, OKKK→OKBK |
| `exec_hubs_curated.csv` | executiva/FBO | 249 | SSLT→SBSO (Sorriso), SWRD→SBRD; fora: SBNV, CYKZ, heliportos |
| `feeder_hubs_curated.csv` | alimentador | 229 | SBCM→SBJA (Jaguaruna), SBQV→SBVC, SSLT→SBSO, ZUNN→ZGNN |
| `health_hubs_curated.csv` | saúde/aeromédico | 259 | SBNT→SBSG; fora: SBNV, SDBB |
| `training_hubs_curated.csv` | instrução | 258 | SDBB→SDBK (Botucatu), SDBK→SDIO (Itápolis), SDIH→SDIM, SSPG→SSZW, SSCB→SSKS, SSCL→SSKZ, SNCT→SNRU, SIXE→SWUZ (Luziânia); fora: SDNY (no MSFS é Ushuaia), FAPA, NZWG, VOJV, ZUSN, RPUO, WMKM, WARB |
| `mro_hubs_curated.csv` | manutenção/fábrica | 257 | SBSC→SDSC (São Carlos), SDBB→SDBK, SSLT→SBSO; fora: FVRG |
| `agro_hubs_curated.csv` | agro/aviação agrícola | 349 | códigos BR errados trocados pela cidade certa (ex.: SWLV→SBLU Lucas do Rio Verde, SWSO→SJIE Sapezal, SWCI→SBLQ Cristalina, SSMK→SSMJ Maracaju, SSVC→SNEE Vacaria); sem aeródromo no MSFS: Nova Mutum, Campo Novo do Parecis, Querência, Nova Andradina, Sidrolândia, Uruçuí, Guarapuava, Taquara |
| `military_curated.csv` (`role` militar/misto) | militar e uso misto | 373 | SWTP→SBTS (Tiriós), WSTG→WSAT; fora: PHIK, SCRM, LFSX, LFOA, LEZG, LEMO, LEAB, OJMS, HECW |

Gerado: `Data/places/airport_profiles.json` (`tools/places/airport_profiles.py`): 8.752 aeroportos do MSFS com perfis = listas curadas (com subtipos, ex.: `carga:pereciveis`, `saude:orgaos`, `mro:heavy_check`) + heurísticas (fazenda, isolado, interior distante, altitude, turístico por POIs). Os `hub_*` antigos (`hubs_data.json`) ficam só como reserva: as listas curadas têm prioridade.

### 9.2 Como o peso é calculado (proposta para o catálogo)
`peso(missão, aeroporto) = base[missão][tier] × produto dos multiplicadores dos perfis do aeroporto` (limitado a ×12), e só se o aeroporto passar nos requisitos da missão.
- **Escala da base:** — 0 · rara 1 · baixa 3 · média 6 · alta 10.
- **Multiplicadores:** ×2 preferência leve · ×3–4 forte · ×5–8 vocação · ×10 dedicado.
- **Base militar pura (`militar`):** missões civis ×0,2 (exceto SAR, CAN/logística militar, autoridades, diplomático, humanitário, translado para manutenção).
- **Destino:** cada missão sorteia o destino pela regra dela; o perfil do destino multiplica a chance de ser escolhido (coluna "Destino").

### 9.3 Tabela de pesos (T1/T2/T3/T4 = base por tier)
| Missão | T1 | T2 | T3 | T4 | Origem preferida | Destino |
|---|---|---|---|---|---|---|
| instrucao_local | rara | baixa | alta | média | instrucao ×4, aeroclube ×4 | volta à origem |
| circuito_tgl | rara | baixa | alta | média | instrucao ×4, aeroclube ×4 | volta à origem |
| navegacao_vfr | — | baixa | alta | média | instrucao ×3, aeroclube ×3 | triângulo de tier 3 a 30–80 NM |
| instrucao_ifr | rara | média | média | — | instrucao:ifr ×5, instrucao:academia_linha ×3, instrucao ×2 | aeródromo próximo pavimentado (tier 1–3) |
| adaptacao_noturna | rara | baixa | média | — | instrucao ×3, aeroclube ×3 | volta à origem (pista iluminada) |
| cheque | rara | baixa | média | rara | instrucao ×3, instrucao:academia_linha ×3 | volta à origem |
| ponte_aerea | alta | média | — | — | linha:ponte_aerea ×8, linha:alta_frequencia ×4 | outro ponte_aerea/alta_frequencia do mesmo país ≤ 300 NM ×8 |
| troncal_domestico | alta | média | — | — | linha ×3, linha:domestico ×2 | linha do mesmo país ×4 |
| regional_internacional | média | baixa | — | — | linha:internacional ×3 | tier 1–2 de país vizinho, linha ×3 |
| longo_curso | média | — | — | — | linha:mega_hub ×4, linha:internacional ×3 | linha:mega_hub de outro país/continente ×5 |
| feeder_inbound | rara | alta | média | — | alimentador ×5 | linha (hub) ≤ 500 NM ×5 |
| feeder_outbound | média | baixa | — | — | linha ×3 | alimentador ≤ 500 NM ×5 |
| sub_regional | — | média | alta | baixa | alimentador ×2, interior_distante ×2 | capital/tier 2 alimentador ×4 |
| pinga_pinga | — | média | média | baixa | alimentador ×3, isolado ×2 | escalas tier 2–4 em sequência |
| regional_essencial | — | baixa | média | média | isolado ×8, alimentador:amazonia ×4, alimentador:remoto ×4 | capital regional (tier 2 linha/alimentador) ×4 |
| executivo_corporativo | média | alta | média | rara | executivo ×4, executivo:corporativo ×2 | executivo/tier 1–2 ×3 |
| executivo_intercontinental | baixa | rara | — | — | executivo:dedicado ×4, executivo ×2 | executivo de outro continente ×4 |
| vip_internacional | baixa | baixa | — | — | executivo ×3 | país vizinho: executivo:lazer ×3, tier 1–2 |
| agro_executivo | — | média | alta | média | agro:executivo_rural ×4, agro ×3, fazenda ×3 | agro/fazenda ×4 |
| familia_alto_padrao | rara | média | média | baixa | executivo:lazer ×3 | executivo:lazer ×3, turístico ×3 |
| equipes_urgentes | baixa | média | média | baixa | mro ×2, carga:industrial ×2 | qualquer |
| charter_lazer | média | média | baixa | rara | linha:turistico ×3, executivo:lazer ×3 | turístico (curado ×4, POI ×2), executivo:lazer ×3 |
| charter_esportivo | média | média | — | — | linha ×2 | tier 1–2 do mesmo país |
| transfer_metropolitano | baixa | média | baixa | — | executivo:urbano ×4, executivo:dedicado ×2 | executivo ≤ 150 NM ×4 |
| panoramico | rara | baixa | média | baixa | turístico curado (base) ×8, turistico_poi ×3 | volta à origem |
| demo_flight | rara | baixa | baixa | — | mro:fabrica ×8, mro:executivo ×3, executivo:dedicado ×2 | volta à origem |
| carga_paletizada | alta | média | — | — | carga ×3, carga:cargueiros ×3 | carga ×4 |
| carga_overnight | média | média | baixa | — | carga:integrador ×5, carga:noturno ×4, carga:expressa ×3, carga:postal ×3 | carga/alimentador ×3 |
| malotes | — | média | média | baixa | carga:postal ×2, interior_distante ×2 | capital/tier 2 ×3 |
| carga_valores | baixa | média | baixa | — | carga:alto_valor ×4 | carga/linha ×3 |
| ecommerce | média | média | baixa | — | carga:ecommerce ×5, carga:integrador ×3 | alimentador/tier 2–3 ×3 |
| carga_jit | média | média | média | baixa | carga:industrial ×3, carga:automotivo ×3, carga:eletronicos ×3, agro ×2 | carga:industrial ×3; variante safra: agro/fazenda ×5 |
| cadeia_fria | média | média | baixa | — | carga:pereciveis ×6, agro:pereciveis ×5, agro:pescado ×4 | carga (exportação) ×4 |
| carga_dgr | baixa | média | baixa | — | carga:farma ×5, carga:mineracao ×3, saude ×2 | carga:farma/hospital ×3; explosivos: carga:mineracao ×4 |
| carga_sensivel | rara | média | média | média | agro:genetica ×5, agro:sementes ×4, agro ×2 | fazenda/agro ×4 |
| carga_combi | — | baixa | média | média | isolado ×6, alimentador:amazonia ×4 | isolado tier 3–4 ×5 |
| insumos_agro | — | média | alta | alta | agro ×4, fazenda ×4 | fazenda/agro ×5 |
| suprimentos_remotos | — | baixa | média | alta | isolado ×4, fazenda ×3, interior_distante ×2 | fazenda/isolado tier 3–4 ×5 |
| conexao_pista_curta | — | — | média | média | fazenda ×2, interior_distante ×2 | tier 4 ≤ 50 NM |
| orgaos | média | média | baixa | — | saude:orgaos ×6, saude:hospital_referencia ×3 | saude:orgaos/hospital_referencia ×6 |
| uti_inter_hospitalar | baixa | média | média | baixa | saude ×3 | saude:hospital_referencia ×5 |
| medevac_remoto | — | baixa | média | alta | saude:medevac_remoto ×4, isolado ×4, fazenda ×2 | pista curta/não pavimentada; base de saúde ×3 |
| uti_longa_distancia | baixa | baixa | — | — | saude:repatriacao ×6, saude:uti_aerea ×3 | saude:hospital_referencia de outro país ×5 |
| saude_familia | — | média | média | baixa | saude ×3, alimentador ×2 | isolado tier 3–4 ×4 (ida e volta) |
| aerolevantamento | rara | baixa | média | baixa | — | volta à origem |
| paraquedismo | rara | rara | baixa | rara | zona_salto ×10 | volta à origem |
| pulverizacao | — | — | média | alta | agro:pulverizacao ×6, fazenda ×4, agro ×3 (só avião agrícola) | volta à origem |
| reboque_planador | — | rara | baixa | baixa | instrucao:planador ×10, aeroclube ×3 | volta à origem |
| publicidade_aerea | — | baixa | baixa | — | turistico_poi ×4, turístico curado ×3 | volta à origem |
| patrulha_vigilancia | — | baixa | média | média | isolado ×3, fazenda ×3, agro ×2, militar:fronteira ×2 | volta à origem |
| inspecao_linhas | — | baixa | média | baixa | interior_distante ×2 | traçado e volta |
| monitoramento_trafego | baixa | média | baixa | — | executivo:urbano ×2 | volta à origem |
| sar | rara | baixa | baixa | baixa | militar:sar ×5, saude:resgate ×3 | volta à origem |
| calibracao_auxilios | rara | média | baixa | — | — | aeródromo com ILS/VOR (tier 1–2) |
| ferry_posicionamento | baixa | baixa | baixa | rara | mro ×2 | linha/executivo ×2 |
| translado_manutencao | baixa | média | baixa | rara | — | mro ×6 (heavy_check para jatos, geral para leves, agricola para agrícolas, helicoptero) |
| ferry_permit | rara | baixa | baixa | — | — | mro ×6 |
| check_flight | rara | baixa | baixa | — | mro ×6 | volta à origem |
| crew_parts | média | baixa | baixa | — | mro ×3, linha ×2 | linha/mro ×3 |
| translado_piloto_agricola | — | baixa | média | média | agro:pulverizacao ×4, fazenda ×3 | agro/fazenda ×4 |
| pessoas_remotas | — | média | média | alta | fazenda ×4, isolado ×3, agro ×2 | fazenda/isolado ×4 |
| autoridades | baixa | média | baixa | rara | militar:vip ×4, executivo:governo ×3 | tier 1–2 do mesmo país |
| diplomatico | média | baixa | — | — | militar:vip ×4, executivo:governo ×3 | capital (tier 1) de outro país ×3 |
| militar_can | — | média | média | baixa | militar ×6, militar_misto ×3, militar:fronteira ×4 | militar:fronteira ×5, isolado ×3 |
| humanitario | rara | baixa | média | média | saude:humanitario ×4, militar:transporte ×2 | isolado ×4 |
| combustivel_tambor | — | baixa | média | alta | isolado ×3, alimentador:remoto ×3 | isolado/fazenda tier 4 ×4 |
| apoio_acampamento | — | baixa | média | alta | carga:mineracao ×3, isolado ×4 | isolado tier 4 ×5 |
| instrucao_stol | — | rara | baixa | média | aeroclube ×3, instrucao:bush ×4, altitude ×2 | pista curta tier 4 ≤ 30 NM |
| evacuacao_por_do_sol | — | — | baixa | média | isolado ×3 | tier 2–3 com pista iluminada |

Pendente de validação: ferramenta que gera mil quadros em ~10 aeroportos reais e mostra a porcentagem de cada missão, para o usuário conferir antes da simulação de carreira.

### 9.4 Verificação pedida pelo usuário (2026-11-23) — depois de cada etapa
Ferramenta de conferência (subcomando do `FlightCareer.Replay`, ex.: `board-report`) que gera ~1.000 quadros por aeroporto real (tier 1: SBGR, KMEM; tier 2: SBKP, SBTF; tier 3: SDCO, SDOI, SBJR; tier 4: fazenda no MT, PABE; militar: SBAN) e mostra:
1. **tipos de missão por tier**: porcentagem de cada tipo; tier 1 deve concentrar linha aérea, carga pesada, executiva; tier 3/4 instrução, agro, remotas;
2. **destinos preferenciais por tipo**: porcentagem dos destinos com o perfil esperado (ex.: órgãos → hospital de referência; translado para manutenção → MRO; carga perecível → hub de carga; regional essencial → capital regional);
3. **pax e carga por missão**: mínimo, média e máximo sorteados por tipo, conferindo que respeitam as faixas da missão e variam aleatoriamente (não repetem o mesmo valor).
O usuário confere o relatório antes da simulação de carreira.

### 9.5 Cobertura de destinos (usuário, 2026-11-23): todo aeroporto precisa ter chance de receber voo
Achados no gerador atual (`MissionGenerator.CreateMissionFromSubCategory`, caminho do jogo):
1. **Preferencial exclusivo:** se houver ao menos um destino preferencial dentro da faixa de distância, o sorteio é feito **só entre os preferenciais** (`preferredCandidates.Any() ? preferred : todos`). Os vizinhos não preferenciais nunca recebem essa missão.
2. **Corte alfabético:** os candidatos são lidos do banco numa faixa de latitude, `OrderBy(ICAO).Take(500)`. Em regiões densas (EUA, Europa, Brasil) só os 500 primeiros em ordem alfabética entram; os demais nunca são destino.
Regra para a etapa 2:
- Preferência por **peso**, nunca exclusiva: destino preferencial ×k (tabela § 9.3), não preferencial ×1; nenhum destino válido fica com peso 0 por não ser preferencial.
- Candidatos por área real (latitude **e** longitude dentro do raio), sem corte alfabético; se precisar limitar, amostra aleatória com a semente do gerador.
- Teste/relatório de cobertura (`board-report --coverage`): para todos os aeroportos do MSFS, quantas missões cada um recebe como destino numa simulação ampla; listar os que ficam com 0 e o motivo (nenhuma missão permite pela pista/tier/distância — aceitável — ou foram excluídos pelo sorteio — erro).

### 9.6 Bagagem por pessoa realista (usuário, 2026-11-23)
Hoje (`MissionGenerator`, os dois caminhos): bagagem = nº de pax × um único sorteio de 0–50 lb (o mesmo valor para todos), igual em qualquer missão; o campo `LuggageWeightKg` de cada subtipo não é usado; sem pax a bagagem é 0, e no translado básico é zerada.
Regra para a etapa 2 (faixas por pessoa no catálogo, sorteio **individual** por pessoa, valores em kg convertidos para lb):
| Tipo de missão | Bagagem por pessoa | Tripulação/apoio a bordo (sempre, mesmo sem pax) |
|---|---|---|
| Instrução, TGL, navegação, STOL, cheque | mochila 2–6 kg | instrutor/examinador + aluno: 2–6 kg cada |
| Panorâmico, safári, voo de demonstração | 0–5 kg (câmera, bolsa) | — |
| Paraquedismo | equipamento 12–16 kg por saltador | — |
| Táxi aéreo, executiva, VIP, autoridades | 8–23 kg | — |
| Linha doméstica, regional, feeder, sub-regional | 5–23 kg (média ~12) | — |
| Longo curso, regional internacional | 15–32 kg | — |
| Charter de lazer, pousada de pesca/lodge | 15–30 kg (tralha de pesca, esqui) | — |
| Rodízio de turno, brigadistas, equipes, pessoas remotas | 15–30 kg (15–30 dias / equipamento) | — |
| Regional essencial, combi, táxi social | 5–25 kg | — |
| Saúde (UTI, medevac, órgãos, ronda) | acompanhante 3–10 kg | equipe médica 3–8 kg cada (o equipamento médico continua como carga) |
| Ferry, translado, check flight, ferry permit | — | piloto 3–10 kg |
| Carga pura (sem pax) | — | — |
- O total de bagagem conta no peso da missão e no limite da aeronave, como hoje.
- O relatório do gerador (§ 9.4) mostra mínimo, média e máximo da bagagem por tipo.

### 9.7 Revisão das faixas de pax, carga e distância (usuário, 2026-11-23) — antes dos testes do gerador
Para cada um dos 69 tipos, revisar e calibrar as faixas sorteadas (`paxRules`, `lbsRules`, `rangeRules`) contra a operação real:
- **Aeronave típica** da missão (ex.: instrução C152/C172; táxi aéreo Baron/King Air/Citation; regional ATR/E-Jet; linha A320/737; longo curso 777/A350; carga pesada 747F/777F; agrícola Air Tractor/Ipanema) e a capacidade/payload dela, para que a faixa caiba em quem faz essa missão no mundo real.
- **Distâncias reais** por tipo (ex.: instrução local 5–20 NM; panorâmico 10–60 NM de percurso; ponte aérea 150–300 NM; regional 100–500 NM; longo curso 2.500–7.500 NM; ferry de fazenda 20–150 NM), com exemplos de rotas reais.
- **Carga** coerente com o tipo (malotes leves, ULD pesados, tambores de combustível ~180 kg cada, insumos agro em lotes, órgão em caixa térmica leve).
- **Pax** coerente (instrução 1–2, cheque 1 examinador, paraquedismo pela capacidade do avião de salto, rodízio de turno pela equipe, linha pela cabine).
- O limite do avião continua filtrando no quadro (missão que não cabe aparece como acima do peso); a faixa do tipo não deve depender do avião do jogador.
Entrega: tabela de faixas por tipo neste plano (com a aeronave de referência e a fonte), revisada pelo usuário; depois o catálogo é preenchido e o relatório § 9.4 confere.

#### 9.7.1 Tabela de faixas proposta (v1, Claude, 2026-11-23) — aguardando aprovação do usuário
**Como a faixa é sorteada (mudança técnica):** hoje distância, pax e carga são sorteados **separados**, então sai um "linha" de 130 pax com 1.600 NM ou um cargueiro de 120.000 lb com 200 NM. Proposta: cada tipo tem 1 a 3 **perfis** (bandas com peso); o gerador sorteia o perfil e, **dentro dele**, distância, pax e carga. O perfil grande pesa mais em aeroporto grande (tier 1–2 ou etiqueta do tipo) e o pequeno em aeroporto pequeno; a missão não depende do avião do jogador (o quadro continua marcando o que não cabe).
**Bagagem por pessoa:** a da tabela § 9.6, sorteada por pessoa (não repetida aqui). "Carga" abaixo é a carga paga, sem bagagem.
**Aeronave de referência (decisão do usuário, 2026-11-23):** só aviões do `aircrafts.csv` do projeto (PaxMax / CargaMax); nenhum perfil depende de avião fora do jogo. **Aprovada pelo usuário em 2026-11-23** com essa restrição. Distâncias: ortodrômica de rotas reais citadas.

Formato dos perfis: `peso% · distância NM · pax · carga lb`.

**A. Instrução** (aluno = pax; o jogador é o instrutor)
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| instrucao_local | C152 (2 / 530), C172 (4 / 878), DA40 | 100 · área a 5–15 NM (percurso 2×+10 = 20–40) · 1 · 0 | igual a hoje; tirar o perfil de 0 pax (sempre há aluno) |
| circuito_tgl | C152, C172 | 80 · 10–25 · 1 · 0 ; 20 · 10–25 · 0 · 0 (solo supervisionado do instrutor em checagem) | igual a hoje com 80/20 em vez de 50/50 |
| navegacao_vfr | C172, PA-28, DA40 | 100 · triângulo de 80–250 (etapas 25–90) · 1 · 0 | hoje 50–300 numa etapa só; triângulos de aeroclube típicos de 2–3 h |
| instrucao_ifr | C172 G1000, DA62, Baron | 100 · aeródromo a 20–80 (ida e volta 40–160) · 1 · 0 | 2–3 aproximações por sessão |
| adaptacao_noturna | C172, PA-28 | 100 · local 10–30 · 1 · 0 | 5 pousos noturnos (exigência de experiência recente) |
| cheque | C172, Baron, King Air | 100 · local 20–60 · 1 (examinador) · 0 | igual a hoje |
| instrucao_stol | NXCub (2 / 1.000), Cub, C207 | 100 · pista curta a 10–40 · 1 · 0 | — |

**B. Linha aérea**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| ponte_aerea | A320neo (195), 737 MAX 8 (210) | 100 · 150–400 · 120–195 · 0–6.000 (belly) | SBSP–SBRJ 197, JFK–BOS 160, MAD–BCN 260, HND–ITM 250, MEL–SYD 380 |
| troncal_domestico | A320neo, 737 MAX 8, A321 (244) | 45 · 250–700 · 120–190 · 0–8.000 ; 40 · 700–1.400 · 140–210 · 0–10.000 ; 15 · 1.400–2.400 · 160–230 · 0–10.000 | GRU–REC 1.140, GRU–MAO 1.450, JFK–LAX 2.150; hoje teto 1.600 |
| regional_internacional | A320neo, A321LR, 737 MAX | 40 · 400–1.000 · 120–190 · 0–8.000 ; 45 · 1.000–1.900 · 140–220 · 0–10.000 ; 15 · 1.900–3.000 · 160–220 · 0–8.000 | GRU–EZE 900, GRU–SCL 1.400, LHR–FCO 780, MIA–BOG 1.320 |
| longo_curso | 787-10 (330), A330-300 (300), 747-400 (416) | 35 · 3.000–4.500 · 220–320 · 5.000–30.000 ; 50 · 4.500–6.500 · 250–380 · 5.000–35.000 ; 15 · 6.500–7.700 · 250–400 · 5.000–25.000 | GRU–LIS 4.300, GRU–LHR 5.100, GRU–DXB 6.600; teto pelo alcance do 747-8i (7.730) |
| feeder_inbound / feeder_outbound | ES30 (30), ATR 42 (48), ATR 72 (72) | 40 · 80–250 · 20–48 · 0–2.000 ; 45 · 150–400 · 40–72 · 0–3.000 ; 15 · 250–550 · 60–72 · 0–3.000 | hoje 45–160 pax: o perfil 111–160 (A320) sai do feeder |
| sub_regional | C208B (14), Twin Otter (20), SkyCourier (19), ES30 (30) | 60 · 50–200 · 4–12 · 0–800 ; 40 · 100–300 · 10–19 · 0–1.000 | Cape Air (Cessna 402, 9 pax), Azul Conecta (C208), Twin Otter nas ilhas |
| pinga_pinga | C208B, Twin Otter, SkyCourier | 100 · 3–4 etapas de 25–120 (total 100–400) · 2–14 · 100–1.500 (malote/encomenda) | rotas postais do Alasca e Azul Conecta |
| regional_essencial | C208B, Twin Otter, ATR 42 | 70 · 50–300 · 4–14 · 200–1.500 ; 30 · 100–350 · 20–48 · 500–3.000 | EAS nos EUA (9–30 assentos), Widerøe no norte da Noruega |

**C. Executiva, charter e turismo**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| executivo_corporativo | Baron (6), King Air 250 (9), CJ4 (10), PC-24 (10), Longitude (12) | 45 · 100–400 · 1–4 · 0 ; 40 · 300–1.200 · 2–7 · 0 ; 15 · 1.000–2.000 · 4–10 · 0 | hoje 1–25 pax (25 não cabe em jato executivo): teto 12 |
| executivo_intercontinental | Longitude (12 / 3.500 NM), PC-24 (2.000 NM) | 100 · 1.800–3.400 · 2–12 · 0 | TEB–LTN 3.000, LTN–DXB 3.000; o teto é o alcance do Longitude |
| vip_internacional | CJ4, PC-24, Longitude | 100 · 300–1.200 · 2–8 · 0 | hoje 300–900 |
| agro_executivo | Baron, King Air C90 (8), PC-12 (10) | 60 · 80–300 · 1–4 · 0–200 ; 40 · 250–600 · 3–8 · 0–200 | Sorriso, Rio Verde, LEM a partir de capitais |
| familia_alto_padrao | Bonanza G36 (6), PC-12, King Air | 100 · 100–800 · 2–7 · 0 | hoje 2–8 |
| equipes_urgentes | Caravan, PC-12, King Air | 100 · 50–500 · 2–6 · 50–400 (equipamento) | — |
| charter_lazer | Caravan, PC-12, King Air; pousada: C207T (7), NXCub | 60 · 100–800 · 4–9 · 0 (fim de semana) ; 40 · 40–300 · 2–8 · 0 (lodge, pista isolada) | pesca no Pantanal/Amazônia, lodges do Alasca e Canadá |
| charter_esportivo | A320neo, 737 MAX 8; banda ou time pequeno: ATR 72 | 60 · 200–1.200 · 30–70 · 0–4.000 (equipamento) ; 40 · 300–1.500 · 60–150 · 0–6.000 | time de futebol com comissão ~50–70 pessoas |
| transfer_metropolitano | Baron, King Air, CJ4 | 100 · 50–250 · 1–6 · 0 | SBMT–SBJR 190, TEB–ACK 170 |
| panoramico | C172, C208, Twin Otter (safári) | 70 · percurso 15–60 · 1–3 · 0 ; 30 · percurso 30–80 · 4–12 · 0 (turismo em grupo) | Grand Canyon (Twin Otter), Fiordes; hoje 1–3 |
| demo_flight | qualquer (o avião do jogador) | 100 · local 20–60 · 1–3 (comprador + assessor) · 0 | — |

**D. Carga**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| carga_paletizada | 737 MAX 8, A321, A330-300, 787-10, 747-400 (148.412, corrigido), A330 BelugaXL | 40 · 300–1.600 · 0 · 20.000–45.000 ; 40 · 600–2.600 · 0 · 45.000–100.000 ; 20 · 3.500–7.200 · 0 · 100.000–148.000 (faixas finais depois da correção do CargaMax dos jatos) | VCP–MAO, MIA–BOG, LEJ–HKG; perfil pesado só de aeroporto `carga:cargueiros` |
| carga_overnight | C208B (3.305), SkyCourier, ATR 72 (16.500), 737 MAX 8 | 45 · 100–400 · 0 · 1.000–3.300 ; 35 · 150–500 · 0 · 6.000–16.000 ; 20 · 300–1.500 · 0 · 20.000–45.000 | FedEx Feeder (Caravan), ASL ATR; hoje 20.000–60.000 só |
| malotes | Baron, Caravan | 100 · 50–350 · 0–1 · 50–600 | hoje 50–1.000 e 50–600 NM |
| carga_valores | King Air, Caravan, PC-12 | 100 · 100–600 · 1–2 (escolta) · 200–2.000 | 1 milhão em notas de 100 ≈ 10 kg; ouro em barras de 12,4 kg |
| ecommerce | C208B, ATR 72, 737 MAX 8 | 50 · 40–300 · 0 · 500–3.300 ; 35 · 100–500 · 0 · 4.000–15.000 ; 15 · 300–1.200 · 0 · 20.000–45.000 | hoje até 12.000 |
| carga_jit | PC-12, King Air, Caravan, CJ4 | 70 · 50–800 · 0–2 (mecânico) · 50–1.500 ; 30 · harvest rescue 20–150 · 0–1 · 50–500 | — |
| cadeia_fria | Caravan, ATR 72, 737 MAX 8 | 40 · 80–400 · 0 · 800–3.300 ; 40 · 150–600 · 0 · 6.000–16.000 ; 20 · 400–1.500 · 0 · 20.000–45.000 | pescado de Manaus/Belém, salmão do sul do Chile, frutas do Vale do São Francisco |
| carga_dgr | Caravan, King Air, Twin Otter | 70 · 100–800 · 0 · 100–2.000 (farmacêutica) ; 30 · 50–300 · 0 · 500–3.000 (explosivo de mineração) | — |
| carga_sensivel | C207, Caravan, Baron | 100 · 50–400 · 0–1 (veterinário/biólogo) · 50–500 | botijão de sêmen ~30–60 lb |
| carga_combi | C208B, Twin Otter, ATR 42 | 70 · 50–400 · 2–9 · 500–2.000 ; 30 · 100–400 · 15–30 · 3.000–6.000 | ATR combi no norte do Canadá |
| insumos_agro | Caravan, C207, C404 Titan (3.500) | 100 · 30–400 · 0–1 · 200–3.000 | hoje 50–3.500 |
| suprimentos_remotos | C207, Caravan, Twin Otter, NXCub | 60 · 15–150 · 0–1 · 300–1.500 ; 40 · 30–250 · 0–1 · 1.500–3.300 | hoje 50–1.500 |
| conexao_pista_curta | C207, Caravan | 100 · 15–80 · 0–5 · 0–1.200 | — |

**E. Saúde** (pax = paciente + acompanhante; equipe médica entra como tripulação de apoio, com bagagem 3–8 kg)
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| orgaos | CJ4, King Air, PC-12 | 100 · 50–700 · 2–3 (equipe de captação) · 20–60 (caixa térmica) | coração: isquemia fria ~4–6 h → teto ~700 NM em jato; hoje 100–600 sem carga |
| uti_inter_hospitalar | King Air, PC-12, CJ4 | 100 · 80–500 · 1–2 (paciente + acompanhante) · 150–400 (maca, monitor, O₂) | hoje 3–4 pax sem carga |
| medevac_remoto | Caravan, PC-12, C207 | 100 · 30–300 · 1–2 · 100–300 | hoje 1–2 / 300–600 lb (pesado para C207) |
| uti_longa_distancia | CJ4, PC-24, Longitude | 100 · 800–3.000 · 1–2 · 200–400 | repatriação EUA–Brasil, Caribe–EUA |
| saude_familia | Caravan, King Air, Twin Otter | 100 · 50–250 (ida) · 3–8 (equipe) · 100–600 (vacinas, odonto) | ronda indígena/ribeirinha |

**F. Trabalho aéreo, agro e vigilância**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| aerolevantamento | C207T, C208, King Air | 100 · percurso 15–80 · 0–1 (operador) · 50–200 (câmera) | igual a hoje com operador opcional |
| paraquedismo | C172 (4), C207T (7), C208B (14), Twin Otter (20) | 35 · zona a 0–5 · 3–6 · 0 ; 50 · 0–5 · 8–14 · 0 ; 15 · 0–5 · 15–20 · 0 | o equipamento de 12–16 kg passa a ser a bagagem por saltador; o perfil pesa conforme a DZ |
| pulverizacao | Cessna 188 AGTruck (1.100) | 100 · talhão a 2–15 · 0 · 600–1.100 | só aviões agrícolas (hoje só o AGTruck no csv) |
| reboque_planador | NXCub, C172, Robin DR400 | 100 · local 5–15 · 0 · 0 | — |
| publicidade_aerea | Cub, C172, NXCub | 100 · percurso 20–80 · 0 · 30–60 (faixa) | — |
| patrulha_vigilancia | C172, C208, Bonanza | 100 · percurso 30–200 · 1–2 (observador) · 0 | — |
| inspecao_linhas | C172, C208 | 100 · percurso 40–200 · 1 (inspetor) · 0 | — |
| monitoramento_trafego | C172, Bonanza | 100 · percurso 10–40 · 1–2 (repórter, cinegrafista) · 0–50 | — |
| sar | C208, King Air, PC-12 | 100 · percurso 40–250 · 1–3 (observadores) · 0 | — |
| calibracao_auxilios | King Air, CJ4 | 100 · 20–200 · 1–2 (técnico) · 300–800 (console) | — |

**G. Operações técnicas e translados** (piloto com bagagem 3–10 kg)
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| ferry_posicionamento | avião da frota | 55 · 20–150 · 0 · 0 ; 35 · 150–600 · 0 · 0 ; 10 · 600–1.500 · 0 · 0 | hoje 30–500 com faixas sem continuidade (61–179 nunca saía) |
| translado_manutencao | avião da frota | 100 · 20–400 · 0 · 0 | destino = oficina |
| ferry_permit | avião da frota | 100 · 20–300 · 0 · 0 | — |
| check_flight | avião da frota | 100 · local 20–60 · 0–1 (mecânico) · 0 | — |
| crew_parts | PC-12, King Air, CJ4, Caravan | 60 · 50–800 · 1–6 (tripulação/mecânicos) · 50–600 ; 40 · 30–200 · 1–2 · 100–1.000 (field AOG: hélice, bateria) | hoje 4–18 pax sem carga |
| translado_piloto_agricola | C172, Bonanza, Baron | 100 · 30–200 · 1–2 · 0 | igual a hoje |
| pessoas_remotas | C207, Caravan, Twin Otter, SkyCourier | 60 · 15–150 · 1–6 · 0–300 ; 40 · 30–300 · 6–19 (turno, brigada) · 0–1.000 | hoje dividido em 3 tipos com 1–9 pax |

**H. Governo, defesa e humanitário**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| autoridades | King Air, CJ4, PC-12 | 100 · 80–600 · 2–8 · 0–600 (urnas) | hoje 2–15 |
| diplomatico | Longitude, A320neo / A321 (versão VIP) | 60 · 300–3.000 · 4–12 · 0 ; 40 · 1.000–6.000 · 20–60 · 0–3.000 | — |
| militar_can | Caravan, SkyCourier, ATR 72, A400M | 40 · 100–600 · 0–9 · 500–3.000 ; 40 · 200–1.200 · 5–30 · 3.000–15.000 ; 20 · 300–2.000 · 10–60 · 15.000–60.000 | rotas do CAN para a fronteira |
| humanitario | Caravan, Twin Otter, A400M | 70 · 50–400 · 0–4 · 1.000–4.500 ; 30 · 200–1.500 · 0–20 · 10.000–60.000 | — |

**I. Remotas, bush e campo**
| id | Referência | Perfis | Exemplo real / observação |
|---|---|---|---|
| combustivel_tambor | Caravan, Twin Otter, C207 | 100 · 20–200 · 0 · 2–10 tambores de 200 L (≈ 360 lb de AvGas ou 390 lb de Jet A/diesel cada, com o tambor) | Caravan leva ~8 tambores; C207 ~3–4 |
| apoio_acampamento | C207, Caravan, NXCub | 100 · 30–300 · 0–4 · 200–2.000 | — |
| evacuacao_por_do_sol | C207, Caravan, Bonanza | 100 · 30–250 · 1–6 · 0–300 | — |

**Achados na faixa atual (corrigidos na tabela):** executiva até 25 pax; feeder até 160 pax (A320); cargueiro noturno sem perfil leve (Caravan nunca recebia); UTI sem a carga do equipamento médico; medevac bush com 300–600 lb (pesado para C207); `ferry` com buraco de 61–179 NM; carga e distância sorteadas separadas (cargueiro de 120.000 lb a 200 NM).
**Correção de dados (2026-11-23):** o `CargaMax(lbs)` do 747-400 (versão passageiros) estava em 475.000 lb, que é a carga útil (MTOW − peso vazio, carga + combustível), não a carga paga. Corrigido para **148.412 lb** (MZFW 542.500 − OEW ~394.088, dados do usuário). Outros jatos do csv têm o mesmo tipo de erro e estão em conferência com fonte (ver § 9.7.2).

#### 9.7.2 Carga paga máxima dos jatos no `aircrafts.csv` (corrigida em 2026-11-24, aprovada pelo usuário)
Vários jatos tinham em `CargaMax(lbs)` um valor próximo da carga útil (carga + combustível), não da carga paga máxima estrutural (MZFW − OEW). Com o valor errado, o quadro e os bots aceitavam missões que não decolam com combustível. Valores aplicados:
| Avião | Antes | Agora (lb) | Fonte | Confiança |
|---|---|---|---|---|
| Boeing 747-400 | 475.000 | 148.412 | usuário: MZFW 542.500 − OEW ~394.088 | alta |
| 747-8i | 502.000 | 157.500 | Boeing (StartupBoeing 747-8I): MZFW 651.000 − OEW 493.500 | alta |
| A400M | 120.000 | 81.600 | Airbus/OCCAR: 37 t | alta |
| Citation Longitude | 16.000 | 3.200 | Textron (página atual do Longitude) | alta |
| PC-24 | 6.800 | 3.100 | Pilatus (ficha do PC-24, versão 2023) | alta |
| SkyCourier | 8.000 | 5.500 | Textron (cartão 2025, versão passageiros; cargueiro 6.000) | alta |
| 787-10 | 260.000 | 126.300 | MZFW 425.000 − OEW 298.700 (Aerocorner) | média |
| A321 (neo) | 90.000 | 56.200 | 25,5 t (Aerocorner/aircraftinvestigation); soma bate com o MZFW 75,6 t da Airbus | média |
| A321LR | 107.000 | 51.800 | MZFW 75,6 t − OEW ~52,1 t (tanques extras) = 23,5 t | média |
| A320neo | 76.400 | 46.300 | MZFW 65,3 t (Airbus) − OEW 44,3 t (aircraftinvestigation) = 21 t | média |
| 737 MAX 8 | 80.000 | 46.000 | ~20,9 t (Aerocorner); MZFW 145.400 da Boeing | média |
| A330-300 | 263.000 | 114.000 | MZFW 175 t (Airbus) − OEW ~123,3 t = 51,7 t | média |
| A330-200 | 269.000 | 109.100 | MZFW 170 t − OEW 120,5 t = 49,5 t | média |
| A310 | 70.000 | mantido | MZFW 113–114 t (Airbus); 31,8 t coerente | média |
| A330 BelugaXL, 747 Supertanker | 113.000 / 150.000 | mantidos | BelugaXL 51 t de carga (Airbus); Supertanker = carga de água/retardante | — |
- SkyCourier tem cabine conversível: na configuração carga o app permite até min(CargaMax + 19 × 170, 1,3 × CargaMax) = 7.150 lb, acima dos 6.000 do cargueiro real. Ajustar a regra de conversão fica para depois (não bloqueia).
