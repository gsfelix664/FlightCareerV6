# Variáveis de freios e trem de pouso do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Brake And Landing Gear Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-brake-and-landing-gear-variables/) do SDK 2024 (86 SimVars, `simvars-msfs2024.csv`).
Mesmo formato de `variaveis-diversos.md`. **C152** = `probe-runs/matriz-C152.csv` (3 rodadas, trem fixo, voos curtos): muitas "constantes" aqui são esperadas num C152 (trem fixo, sem autobrake, sem antiskid, sem flutuadores) e precisam ser provadas num retrátil, num jato e num anfíbio.
**Hoje**: `Tel` (`SimTelemetry`), `Ctx` (contexto), `Reg` (`sensor_registry.json`), `Fail` (panes). **Grav.** = o SDK permite escrever.

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | `BRAKE PARKING POSITION` (já), temperatura dos freios, `GEAR HANDLE POSITION`/`GEAR POSITION`/`GEAR WARNING` (trem em cima no pouso), `GEAR SPEED EXCEEDED`, `AUTOBRAKES ACTIVE`/`AUTO BRAKE SWITCH CB`, `NOSEWHEEL/TAILWHEEL LOCK ON`, flutuadores e leme d'água |
| Qualidade do voo / passageiros | **ordem do toque por roda** (`CONTACT POINT COMPRESSION`/`IS ON GROUND :0..2`: bequilha antes das principais, pouso numa roda só), **quiques** (`GEAR IS ON GROUND`), frenagem brusca e derrapagem (`BRAKE * POSITION`, `CONTACT POINT IS SKIDDING`), curva rápida no táxi (`GEAR STEER ANGLE` × velocidade) |
| Missões e economia | `CONTACT POINT SURFACE TYPE` por roda (pista de grama/terra/neve/água: missões bush e bônus), `IS GEAR FLOATS/SKIS/SKIDS/WHEELS` (quem pode ir aonde), `GEAR WATER DEPTH`, freio quente = tempo de solo extra antes da próxima perna |
| Integridade da carreira | `BRAKE * WEIGHT REMAINING` (desgaste nativo dos freios: se volta a 100 % no meio da carreira, foi reset do sim), `GEAR DAMAGE BY SPEED`/`GEAR SPEED EXCEEDED` (já), `REJECTED TAKEOFF BRAKES ACTIVE` (decolagem abortada registrada) |
| Desgaste e panes | `BRAKE * WEIGHT REMAINING` e `BRAKE * TEMPERATURE EFFECT` (lona e fading), temperatura dos freios, pontos de contato por roda (pneu e rolamento por roda e por piso), `WHEEL RPM:n`, `GEAR HYDRAULIC PRESSURE`, `GEAR EMERGENCY HANDLE POSITION`; **graváveis** que podem virar efeito de pane (trem que não baixa, freio agarrado, bequilha sem comando) |

## 2. Tabela (o que interessa)

### Freios
| Variável | Unidade | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|---|
| `BRAKE LEFT/RIGHT POSITION` | posição | varia (0–1) | Reg, Tel | sim | **Qualidade:** frenagem brusca no pouso/táxi (pedal > x % com velocidade) → desconforto. **Pane:** escrever pressão residual = freio agarrado (testar no sim). |
| `BRAKE LEFT/RIGHT TEMPERATURE` | kelvin | varia (288–296 K, voos curtos) | Reg | — | **HUD:** freio quente após pouso/táxi. **Economia:** tempo de resfriamento antes da próxima perna. **Desgaste:** já em regras de energia de frenagem. |
| `BRAKE LEFT/RIGHT TEMPERATURE EFFECT` | 0–1 | constante (freio frio) | — | — | **Desgaste:** fading real do sim (eficiência < 1) → faixa de estresse dos discos e pastilhas; HUD "freio perdendo eficiência". Provar com frenagem forte repetida. |
| `BRAKE LEFT/RIGHT WEIGHT REMAINING` | 0–1 | **varia** (1,0 → 0,9999) | Reg, Tel | — | **Desgaste nativo das pastilhas**: comparar com `brakes.pads_discs` do addon (calibração F1.4c). **Integridade:** volta a 1,0 = reset do sim, não reparo. |
| `BRAKE PARKING POSITION` / `INDICATOR` | bool | varia | Reg, Tel (position) | — | HUD (já "PB"); **qualidade:** táxi com freio de estacionamento puxado (desgaste e reclamação). |
| `BRAKE INDICATOR` | 0–16K | varia (0–3) | — | — | Redundante com `BRAKE * POSITION`. |
| `AUTOBRAKES ACTIVE` / `AUTO BRAKE SWITCH CB` | bool / número | constante (C152 não tem) | Reg (active) | — | **HUD** jatos: nível de autobrake; **qualidade:** pouso com autobrake adequado ao comprimento da pista. |
| `ANTISKID BRAKES ACTIVE` | bool | constante | Reg | — | Já em regra; provar num jato. |
| `REJECTED TAKEOFF BRAKES ACTIVE` | bool | constante | — | — | **Missões/diário:** decolagem abortada (RTO) registrada; **desgaste:** freios e pneus em RTO de alta energia. |
| `BRAKE DEPENDENT HYDRAULIC PRESSURE` | psf | constante | — | — | Jatos: freio sem pressão hidráulica → pane/alerta. |
| `PARKING BRAKE AVAILABLE`, `TOE BRAKES AVAILABLE` | bool | constante | — | — | **Contexto** (características da aeronave). |

### Pontos de contato (índice = ponto; no C152 `:0` bequilha, `:1`/`:2` principais)
| Variável | C152 | Hoje | Uso proposto |
|---|---|---|---|
| `CONTACT POINT COMPRESSION:n` | varia `:0..2` | Reg (REGRAS-C152) | **Qualidade:** ordem e simetria do toque (bequilha primeiro = pouso ruim + dano na bequilha; uma roda só = vento cruzado mal corrigido). **Desgaste:** amortecedor por roda. |
| `CONTACT POINT IS ON GROUND:n` | varia `:0..2` | Reg (REGRAS-C152) | Solo por roda; **quiques** e toque assimétrico. |
| `CONTACT POINT IS SKIDDING:n` | varia `:0..2` | Reg (REGRAS-C152) | Pneu derrapando por roda (flat spot); qualidade da frenagem. |
| `CONTACT POINT SURFACE TYPE:n` | varia (0–17) | — (Ctx só `SURFACE TYPE`) | **Missões:** pouso em grama/terra/cascalho/neve por roda (bush, bônus); **desgaste:** pneu e rolamento por piso; roda fora da pista (borda). Gravável: não escrever. |
| `CONTACT POINT STEER ANGLE(_PCT):0` | varia | — | Curva fechada em velocidade (já há regra com `GEAR STEER ANGLE PCT`). |
| `CONTACT POINT WATER DEPTH:n` | constante | — | Anfíbios/hidroaviões: profundidade da água; provar num anfíbio. |
| `CONTACT POINT SKIDDING FACTOR:n`, `CONTACT POINT POSITION:n` | constante | — | Não usar até prova (skidding factor ficou 0 mesmo com `IS SKIDDING` = 1). |

### Trem de pouso
| Variável | Unidade | C152 | Hoje | Grav. | Uso proposto |
|---|---|---|---|---|---|
| `GEAR HANDLE POSITION`, `GEAR POSITION:n`, `GEAR TOTAL PCT EXTENDED` | % | constante (fixo) | Reg | sim (handle, position) | **HUD:** trem em trânsito/baixado; **qualidade/integridade:** pouso de barriga. **Pane:** escrever posição = trem que não baixa/não recolhe (o SDK não tem evento de falha de trem). Testar no sim. |
| `GEAR LEFT/RIGHT/CENTER POSITION` | 0–1 | constante | — | sim | Idem por perna: **trem assimétrico** como efeito de pane. |
| `GEAR WARNING:n` | enum | constante | Reg | — | **HUD:** alarme de trem; **qualidade:** pouso com alarme ativo. |
| `GEAR SPEED EXCEEDED`, `GEAR DAMAGE BY SPEED` | bool | constante | Reg, Fail | — | Já usados (pane nativa, Vle/Vlo). HUD: alerta. |
| `GEAR HYDRAULIC PRESSURE` | psf | constante | Reg | — | Retrátil hidráulico: pane de extensão; provar num retrátil. |
| `GEAR EMERGENCY HANDLE POSITION` | bool | constante | — | — | **Desgaste/manutenção:** extensão de emergência exige inspeção (discrepância); **missões:** procedimento correto numa pane de trem. |
| `GEAR IS ON GROUND:n` | bool | varia `:0..1` | — | — | Solo por perna (índice de trem, não de ponto de contato); **quiques**. |
| `GEAR IS SKIDDING` | bool | varia | Reg | — | Já em regra. |
| `GEAR STEER ANGLE(_PCT):n`, `GEAR CENTER STEER ANGLE(_PCT)` | rad / % | varia | Reg (index) | — | Curva no táxi em velocidade (já há regra). |
| `NOSEWHEEL MAX STEERING ANGLE` | rad | constante | Reg | sim | Limite da aeronave. **Pane:** escrever 0 = bequilha sem comando (testar). |
| `NOSEWHEEL LOCK ON`, `TAILWHEEL LOCK ON` | bool | constante | — | — | **HUD:** lembrete de trava da bequilha (convencional); **qualidade:** decolagem/pouso com trava solta em avião de bequilha. |
| `WHEEL RPM:n`, `LEFT/RIGHT/CENTER WHEEL RPM` | rpm | varia (até ~2160) | Reg (left/right) | — | Rolamentos e pneus por roda; roda travada (rpm 0 com velocidade) = derrapagem; bequilha em velocidade alta (shimmy). |
| `GEAR ANIMATION POSITION:n` | % | **varia 100–185** | — | — | Passa de 100 % no solo (provável compressão animada); investigar antes de usar. |
| `IS GEAR WHEELS/FLOATS/SKIS/SKIDS/RETRACTABLE` | bool | constante | Ctx (menos skids) | — | **Missões:** elegibilidade por tipo de trem (água, neve); acrescentar `IS GEAR SKIDS` (helicópteros). |
| `RETRACT FLOAT SWITCH`, `RETRACT LEFT/RIGHT FLOAT EXTENDED` | enum / % | constante | — | — | Anfíbios: **HUD** rodas dos flutuadores (pousar na água com rodas baixadas = capotagem); **desgaste/pane** grave. Provar num anfíbio. |
| `GEAR WATER DEPTH:n`, `WATER RUDDER HANDLE POSITION`, `WATER LEFT/RIGHT RUDDER *` | cm / % | constante | — | sim (handle) | Hidroaviões: **HUD** leme d'água; **desgaste:** leme d'água baixado em velocidade/fora d'água. |

## 3. Não usar
- `* WHEEL ROTATION ANGLE`, `WHEEL ROTATION ANGLE:n` (animação; o RPM basta), `BRAKE * POSITION EX1` (só dispara aumento de pressão), `STEER INPUT CONTROL`, `GEAR AUX/TAIL *` e `AUX WHEEL *` (raros), `GEAR LEFT/RIGHT STEER ANGLE*`.
- Não escrever `CONTACT POINT SURFACE TYPE` nem as posições de trem fora de um modo de pane testado.

## 4. Observações
- **Índices diferentes:** `CONTACT POINT *:n` usa o ponto de contato (C152: 0 bequilha, 1 e 2 principais); `GEAR *:n` usa o índice do trem (0 centro, 1 esquerdo, 2 direito no SDK). Confirmar no Developer Mode › Wheels por aeronave antes de ligar a peças.
- `BRAKE * WEIGHT REMAINING` é a primeira prova de desgaste nativo dos freios variando num voo do C152; entra na calibração addon × nativo (`native-wear-mapping.md`).
- Escrever SimVars de trem/freio como efeito de pane exige o modo de teste de panes do SimVarProbe (plano `validacao-simvars.md` › 1b): aceitar, manter, desfazer.

## 5. Próximos passos sugeridos
1. **Qualidade do pouso:** ordem e simetria do toque por roda, quiques e frenagem brusca (lógica pura em `Domain`, com testes de replay).
2. **Registro:** `BRAKE * TEMPERATURE EFFECT`, `CONTACT POINT SURFACE TYPE:0..2`, `GEAR IS ON GROUND:0..2`, `REJECTED TAKEOFF BRAKES ACTIVE`, `GEAR EMERGENCY HANDLE POSITION`, `NOSEWHEEL/TAILWHEEL LOCK ON`, `WHEEL RPM:0`; contexto: `IS GEAR SKIDS`, `PARKING BRAKE AVAILABLE`, `TOE BRAKES AVAILABLE`.
3. **HUD:** temperatura dos freios, trem/alarme, autobrake, trava da bequilha.
4. **No simulador (usuário):** retrátil (trem, hidráulico, Vle), jato (autobrake, antiskid, RTO), anfíbio (flutuadores, água), frenagem forte repetida (fading), e teste de escrita das posições de trem/freio para efeitos de pane.
