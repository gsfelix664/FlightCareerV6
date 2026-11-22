# Plano — As 7 fases da carreira (substituem as 5 categorias)

**Estado:** desenho do usuário (2026-11-22), registrado para implementar junto com o XP novo (`xp-proficiencia.md` › 6) e depois da calibração do pagamento. Nada implementado.

## 1. Decisões do usuário (2026-11-22)
1. As **7 fases substituem** as 5 categorias (`PilotCategory`).
2. A fase **não limita a compra de avião**; limita **o aceite das missões**.
3. As regras de geração do quadro de missões continuam como estão, **exceto** os contratos novos da fase 1 (voo panorâmico e TGL/circuito). O que muda é quais missões ficam **liberadas** para o piloto aceitar.
4. Declividade de pista fica de fora por enquanto.
5. Fase = faixa de nível de XP (`xp-proficiencia.md` › 6.8, curva 300 × (L − 1)²).

## 2. As fases
| Fase | Nome | Níveis | Licença (exibida) | Aviões típicos | Foco |
|---|---|---|---|---|---|
| 1 | Escola de aviação / aeroclube | 1–5 | Piloto privado (PPL) e horas iniciais | C152, C172, PA-28 | orçamento limitado, aeródromos pequenos e médios, instrução, VFR curta |
| 2 | Serviços aéreos especializados (SAE) | 6–12 | Piloto comercial (CPL) monomotor | C182, PA-18, Husky, DHC-2 | trabalho aéreo remunerado, desafios de pista e precisão |
| 3 | Táxi aéreo e carga leve | 13–20 | IFR e multimotor (MLTE) | C208B, Baron 58, Bonanza | pessoas com expectativa de conforto, cargas com prazo |
| 4 | Executiva de alta performance e MEDEVAC | 21–28 | Type rating turboélice/jato leve e RVSM | King Air 350i, PC-12, TBM 930 | VIP, aeromédico, pistas curtas em altitude, todo-tempo |
| 5 | Regional (commuter) | 29–36 | ATPL — fase regional | ATR 72-600, Dash 8 Q400, CRJ-700 | operação regular, rotas de 150–600 NM |
| 6 | Linha aérea doméstica (narrowbody) | 37–44 | ATPL + type rating | A320neo, 737-800 | aeroportos de alta densidade e pistas exigentes (SDU, CGH) |
| 7 | Companhia internacional (widebody) | 45–50+ | Type rating widebody + ETOPS | 777, 787, A330/A350 | rotas intercontinentais, logística complexa |

## 3. Contratos por fase (existentes no `MissionRules` × novos)
Os ids são os subtipos atuais (o primeiro dígito é o tier do aeroporto de origem). **Proposta de mapeamento para o usuário revisar**:

| Fase | Contratos que já existem | Contratos novos (a criar) |
|---|---|---|
| 1 | 3.4.1 Instrução local, 3.4.2 Navegação (cross-country), 3.4.3 Voo de cheque (exame) | **Voo panorâmico** (decolar, sobrevoar POIs da região e voltar ao mesmo aeroporto); **TGL / circuito**; **Translado básico** (avião de manutenção para aeródromo vizinho < 80 NM, só piloto e bagagem) |
| 2 | 4.1.x Logística de sede (rancho, transporte do patrão, troca de equipe), 4.2.x Aviação agrícola (apoio ao batedor, translado de piloto, insumos urgentes), 3.2.3 Suprimento de campanha | **Levantamento / fotografia aérea** (pernas paralelas com velocidade e altitude constantes sobre área demarcada); **Lançamento de paraquedistas** (subida rápida a 10–12 mil ft, estabilização na zona de salto, descida rápida sem choque térmico) |
| 3 | 3.1.x Táxi aéreo/charter, 2.2.x Executiva agro/biz, 2.3.x Carga expressa, 3.2.1/3.2.2 AOG e insumos agro, 4.3.2 AOG máquina, 2.4.x Apoio | **Fretamento executivo** capital → interior (penaliza curvas bruscas e pouso duro — XP); **Carga frágil/expressa** com janela apertada e bônus de entrega antecipada (XP) |
| 4 | 3.3.x Saúde/remoção (urgência, eletiva, órgãos), 4.3.1 Medivac bush, 1.3.1 Executive business, 1.3.4 Diplomático | **MEDEVAC dinâmico** (urgência máxima, pista curta à noite); **VIP internacional curto** (países vizinhos, alternados distantes) |
| 5 | 2.1.1 Alimentação de hub, 2.1.2 Regional direto, 1.3.2 Logística crew & parts | **Multi-leg sequencial** (atraso ou desgaste de freio da 1ª perna afeta a seguinte) — o multi-leg já existe; muda a liberação |
| 6 | 1.1.1 Doméstico / hub link, 1.2.1 Carga heavy nacional, 1.3.3 Ferry | **Ponte aérea / troncais** (150–220 pax, cost index, pontualidade, IFR completo); **Cargueiro noturno dedicado** |
| 7 | 1.1.2 Regional internacional, 1.1.3 Long haul, 1.2.2 / 1.2.3 Carga internacional | **Long-haul > 2.500 NM** (MTOW, step climbs, manutenção preventiva antes da travessia); **Fretamento global / carga de alto valor** |

## 4. Impacto no código (para a spec)
- `PilotCategory` (int 1–5 no banco) → enum de 7 fases; migração dos pilotos pelo XP/nível equivalente (coluna nova ou reaproveitar com `ALTER TABLE`/conversão em `EnsureSchemaUpdatedAsync`).
- `Mission.RequiredCategory` hoje vem da carga e dos passageiros (`MissionGenerator`); passa a vir do **subtipo** (tabela acima), mantendo o limite por capacidade do avião.
- Promoção: hoje custo de homologação + horas + XP (`PilotCategory.GetPromotionCost/GetRequiredHours/GetRequiredXP`). Com fases por nível, decidir se continua havendo custo e mínimo de horas por fase.
- Quadro de missões, aceite, briefing, criação de carreira, traslado e simulador de carreira usam a categoria (11 arquivos).
- Contratos novos com geometria (POIs, pernas paralelas, zona de salto) precisam de regra de conclusão própria (sobrevoo do ponto, desvio de altitude/velocidade) — maior esforço; podem vir depois.

## 5. Pendências do usuário
1. Revisar o mapeamento da § 3.
2. Missões de fases **anteriores** continuam liberadas para quem subiu (sugestão: sim)?
3. ~~Promoção de fase~~ **Decidido (2026-11-22)** — ver § 6.
4. Ordem de implementação dos contratos novos (sugestão: fase 1 — panorâmico, TGL e translado básico — primeiro, porque o jogador começa nela).
5. ~~`REGRAS_tiers.txt`~~ **Não tem relação com as fases** (é outro assunto do usuário); continua sem ler até ele pedir.

## 6. Nível × fase (decisão do usuário, 2026-11-22)
- **Subir de nível depende só de XP.** Os níveis continuam subindo livremente (ex.: 1 → 5 → 9) mesmo que o piloto não tenha mudado de fase.
- **Mudar de fase exige três coisas:** (1) ter o **nível de entrada** da fase seguinte (ex.: nível 6 para a fase 2); (2) **pagar a homologação**; (3) ter o **mínimo de horas**. Enquanto não pagar ou não tiver as horas, o piloto fica na fase atual (com o nível acima), e as missões da fase seguinte ficam bloqueadas.
- Hoje existem 4 promoções (horas 40 / 150 / 500 / 1.500 h; homologação $35 mil / $120 mil / $500 mil / $2 mi). Proposta para as 6 promoções das 7 fases (calibrar na ECON-STAB junto com o pagamento):

| Para a fase | Nível de entrada | Horas mínimas | Homologação |
|---|---|---|---|
| 2 SAE | 6 | 40 h | $35.000 |
| 3 Táxi aéreo | 13 | 150 h | $120.000 |
| 4 Executiva/MEDEVAC | 21 | 300 h | $300.000 |
| 5 Regional | 29 | 500 h | $500.000 |
| 6 Doméstica | 37 | 1.000 h | $1.200.000 |
| 7 Internacional | 45 | 1.500 h | $2.000.000 |
- **Depois da fase 7 (decisão do usuário, 2026-11-22):** os níveis continuam subindo **sem limite** pelo XP (mesma curva 300 × (L − 1)², sem nível máximo), mas não há mais fases nem destraves além da 7. O nível alto vira só marca de experiência (exibida no perfil).
