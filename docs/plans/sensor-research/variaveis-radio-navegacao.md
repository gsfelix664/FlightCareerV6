# Variáveis de rádio e navegação do MSFS 2024 — usos no FlightCareer

Criado em 2026-11-16 (Claude). Fonte: página [Aircraft Radio Navigation Variables](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-radio-navigation-variables/) do SDK 2024 (278 SimVars: ATC 57, GPS 70, NAV 48, COM 22, ADF 16, TACAN 15, HSI 11, marcadores 11, TCAS 10, FLARM 8, diversos 10).
Mesmo formato das outras análises. **C152** = `probe-runs/matriz-C152.csv`. Obsoleta: `GPS HSI NEEDLE` (`variaveis-obsoletas.md`). Rejeitadas: as `* LATLONALT`/`LLAF64`, `TCAS INTRUDER DATA/RELATIVE SPEED`, `TCAS RECEIVED RESOLUTION ADVISORY`, `FLARM THREAT DATA` (tipos struct).

> **Cuidado com `ATC *`:** não são obsoletas, mas o MSFS caiu 3 vezes durante a sondagem logo depois de `ATC FUTURE AGENT NAME` mudar (troca de controle do ATC) — suspeita **não provada** (`HANDOFF.md`). Até o teste com `--filter "^(?!ATC |AI )"`, ler `ATC *` só **uma vez** (contexto, no início/fim do voo), nunca a cada tick. As candidatas de ATC ficam numa lista separada do Watcher.

## 1. Resumo por área

| Área | Variáveis que mais rendem |
|---|---|
| HUD | ETE/ETA e distância do GPS (`GPS ETE`, `GPS WP DISTANCE`, `GPS WP NEXT ID`), **radioaltímetro** (`RADIO HEIGHT`), transponder (`TRANSPONDER CODE/STATE`), frequências COM/NAV, altitude autorizada (`ATC ASSIGNED ALTITUDE`) |
| Qualidade do voo / procedimento | **precisão da aproximação** (`NAV CDI`/`NAV GSI`/`NAV GLIDE SLOPE ERROR`, `GPS WP CROSS TRK`, `GPS VERTICAL ERROR`), **autorizações do ATC** (decolar/pousar sem `ATC CLEARED TAKEOFF/LANDING` em aeroporto com torre), desvio do plano (`ATC FLIGHTPLAN DIFF ALT/DISTANCE/HEADING`), sobrevoo sem aviso (`ATC OVERFLYING AIRPORT UNANNOUNCED`), **código de emergência** (7700/7600) |
| Missões e economia | **comprimento e largura da pista** (`ATC RUNWAY LENGTH/WIDTH`), **pista mínima sugerida por avião** (`ATC SUGGESTED MIN RWY TAKEOFF/LANDING`) para o gerador de missões, aeroporto com torre (`ATC AIRPORT IS TOWERED`: taxa), **gate designado e chegada à vaga** (`ATC DESIGNATED GATE *`, `ATC ON PARKING SPOT`), distância de táxi (`ATC TAXIPATH DISTANCE`), destino do plano do GPS × destino da missão, **matrícula/companhia/voo graváveis** (`ATC ID`, `ATC AIRLINE`, `ATC FLIGHT NUMBER`, `ATC NAME`, `ATC HEAVY`) |
| Integridade da carreira | destino do GPS diferente da missão (aviso), `GPS IS ARRIVED` |
| Desgaste e panes | confirmação de pane de aviônicos (`COM STATUS`, `NAV SIGNAL`, `TRANSPONDER STATE`, `GPS *` sem dados com disjuntor puxado) — liga ao plano de panes por disjuntor |

## 2. O que interessa

### ATC (ler só no contexto até o teste do filtro)
| Variável | C152 | Grav. | Uso proposto |
|---|---|---|---|
| `ATC ID`, `ATC AIRLINE`, `ATC FLIGHT NUMBER`, `ATC NAME`, `ATC HEAVY` | constante | **sim** | **Carreira:** gravar matrícula da aeronave do Hangar, companhia e número do voo da missão (ATC chama pelo prefixo da carreira). Escrita de string: tipo `STRING32/64`. |
| `ATC CLEARED TAXI/TAKEOFF/LANDING`, `ATC CLEARED IFR` | constante (sem ATC no teste) | — | **Procedimento:** decolar/pousar sem autorização em aeroporto com torre (`ATC AIRPORT IS TOWERED`) → nota e, se a carreira quiser, multa. |
| `ATC AIRPORT IS TOWERED`, `ATC DEPARTURE/ARRIVAL AIRPORT IS TOWERED`, `ATC ARRIVAL AIRPORT HAS ATIS` | varia / constante | — | **Economia:** aeroporto com torre = taxa maior; briefing. |
| **`ATC RUNWAY LENGTH`, `ATC RUNWAY WIDTH`** | **varia** | — | **Missões:** pista de chegada/partida cabe no avião? (com `ATC SUGGESTED MIN RWY *`); pista estreita para avião grande. |
| **`ATC SUGGESTED MIN RWY TAKEOFF/LANDING`** | constante | — | Pista mínima do avião (o próprio sim calcula): filtro do gerador de missões por aeronave. |
| `ATC DESIGNATED GATE ARRIVAL/DEPARTURE`, `ATC DESIGNATED RUNWAY LANDING/TAKEOFF`, **`ATC ON PARKING SPOT`** | constante / **varia** | — | **Missões:** chegada à vaga/gate designado = fim da missão; taxa de gate (plano de taxas). |
| `ATC TAXIPATH DISTANCE` | varia | — | Distância de táxi: tempo de solo e combustível no táxi (estimativa do briefing). |
| `ATC ASSIGNED ALTITUDE`, `ATC FLIGHTPLAN DIFF ALT/DISTANCE/HEADING`, `ATC NEXT WAYPOINT *` | constante | — | **Procedimento IFR:** desvio de altitude e de rota (nota de navegação). |
| `ATC OVERFLYING AIRPORT UNANNOUNCED`, `ATC CLEARED OVERFLIGHT CTR` | constante | — | Infração de espaço aéreo (nota). |
| `ATC RUNWAY DISTANCE/START DISTANCE/END DISTANCE`, `ATC RUNWAY TDPOINT RELATIVE POSITION *`, `ATC RUNWAY HEADING DEGREES TRUE` | constante (sem pista do ATC) | — | **Precisão do pouso** (distância ao ponto de toque e à cabeceira) quando houver pista atribuída; alternativa: calcular com `PLANE TOUCHDOWN LATITUDE/LONGITUDE` e o banco de pistas. |
| `ATC FUTURE AGENT *`, `ATC MODEL`, `ATC TYPE` | varia / constante | — | `ATC MODEL/TYPE` já no contexto. **Não** ler `ATC FUTURE AGENT *` (ligada às quedas). |

### GPS, NAV e HSI
| Variável | C152 | Uso proposto |
|---|---|---|
| `GPS ETE`, `GPS ETA`, `GPS WP DISTANCE`, `GPS WP NEXT ID`, `GPS WP ETE`, `GPS FLIGHTPLAN TOTAL DISTANCE`, `GPS IS ACTIVE FLIGHT PLAN`, `GPS IS ARRIVED` | constante (sem plano no teste) | **HUD:** tempo e distância; **missões:** plano carregado no GPS × destino da missão (aviso no início). |
| `GPS GROUND SPEED`, `GPS GROUND TRUE/MAGNETIC TRACK`, `GPS POSITION LAT/LON/ALT` | varia | Alternativas de posição/velocidade (já há no `SimTelemetry`). |
| `GPS WP CROSS TRK`, `GPS WP TRACK ANGLE ERROR`, `GPS VERTICAL ERROR`, `GPS CDI NEEDLE`, `GPS GSI NEEDLE` | varia (parte) | **Qualidade:** erro lateral/vertical na rota e na aproximação RNAV. |
| `GPS IS APPROACH ACTIVE/LOADED`, `GPS APPROACH TYPE`, `GPS APPROACH IS FINAL/MISSED`, `GPS HAS GLIDEPATH` | constante | Tipo de aproximação voada (bônus IFR; arremetida registrada). |
| `NAV CDI:n`, `NAV GSI:n`, `NAV GLIDE SLOPE ERROR:n`, `NAV RADIAL ERROR:n`, `NAV HAS LOCALIZER/GLIDE SLOPE:n`, `NAV LOC AIRPORT IDENT`, `NAV LOC RUNWAY NUMBER` | varia (parte) | **Qualidade:** ILS estabilizado (desvio de localizador e rampa em pontos de "dots"); aproximação de precisão no diário. |
| `HSI CDI NEEDLE`, `HSI GSI NEEDLE`, `HSI DISTANCE`, `HSI HAS LOCALIZER` | varia | Mesmo uso pelo HSI (instrumento ativo). |
| `NAV SIGNAL:n`, `NAV HAS NAV:n`, `NAV TOFROM:n` | varia | Confirmação de pane de NAV (sinal 0 com estação em alcance). |

### COM, transponder e diversos
| Variável | C152 | Grav. | Uso proposto |
|---|---|---|---|
| **`TRANSPONDER CODE:n`**, **`TRANSPONDER STATE:n`**, `TRANSPONDER IDENT` | **varia** | sim (state, ident) | **HUD:** código e modo; **procedimento:** transponder desligado/STBY em voo em espaço controlado; **emergência:** 7700 (aciona o fluxo de pouso de emergência/nota), 7600 (falha de rádio, liga com pane de COM). |
| **`RADIO HEIGHT`** | **varia** | — | **HUD:** radioaltímetro e callouts; altura real sobre o terreno (precisão do toque, voo baixo). |
| `COM STATUS:n`, `COM ACTIVE FREQUENCY:n`, `COM TRANSMIT:n`, `COM RECEIVE:n` | varia (status) | — | Confirmação de pane de COM; frequência no HUD. |
| `PILOT TRANSMITTING`, `COPILOT TRANSMITTING` | constante | — | Baixa prioridade. |
| `TCAS INTRUDER DISTANCE/RELATIVE ALTITUDE/BEARING`, `TCAS MODE` | constante | — | **Segurança:** quase colisão com tráfego (evento no diário; nota). Jatos/G1000. |
| `MARKER BEACON STATE`, `OUTER/MIDDLE/INNER MARKER` | constante | sim | Passagem pelos marcadores na aproximação ILS (baixa prioridade). |
| `FLARM THREAT *` | constante | — | Planadores; fora da frota. |

## 3. Não usar
- Obsoleta `GPS HSI NEEDLE`; structs rejeitadas (`* LATLONALT`, `* LLAF64`, `TCAS INTRUDER DATA`, `TCAS RECEIVED RESOLUTION ADVISORY`, `FLARM THREAT DATA`).
- ADF (16) e TACAN (15): fora do perfil da carreira (baixa prioridade).
- `GPS OVERRIDDEN`, `GPS GSI SCALING`, `GPS HAS GLIDEPATH` graváveis: não escrever (afetam o GPS do avião).
- `ATC FUTURE AGENT *` (suspeita das quedas).

## 4. Próximos passos sugeridos
1. **Teste do usuário (prioridade):** SimVarProbe com `--filter "^(?!ATC |AI )"` para confirmar ou descartar a suspeita das quedas; depois, um teste só com as `ATC *` da lista separada do Watcher.
2. **Carreira:** gravar `ATC ID` (matrícula do Hangar), `ATC AIRLINE`/`ATC FLIGHT NUMBER` (missão de companhia).
3. **Missões:** pista mínima por avião (`ATC SUGGESTED MIN RWY *`) e comprimento/largura de pista no gerador; chegada à vaga/gate como fim da missão.
4. **Qualidade:** precisão da aproximação (ILS/RNAV), autorizações do ATC, transponder e código de emergência.
5. **HUD:** ETE/distância, radioaltímetro, transponder.
