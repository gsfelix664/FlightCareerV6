# Plano: pista mínima por perfil de missão e tier por movimento (2026-11-24)

**Pedido do usuário:** o quadro de SBMT (Campo de Marte, pista de 5.120 ft, sem linha aérea) gerou carga paletizada de 95.190 lb, ponte aérea com 182 pax e e-commerce de 35.704 lb. O usuário aprovou separar **o que a pista aguenta** de **o movimento do aeroporto**.

## 1. Diagnóstico
- **Tier hoje = só comprimento de pista** (`DataSeeder.SeedFromLittleNavmapAsync`): ≥ 7.500 ft pavimentada → 1; ≥ 4.500 → 2; pavimentada ou ≥ 2.500 → 3; senão 4. Caminho sem Little Navmap: tipo do `airports.csv` (`large_airport` → 1, `medium_airport` → 2, `small_airport` pavimentado → 3, resto 4).
- Distorções: SBMT 5.120 ft → tier 2; Congonhas (SBSP) 6.206 ft → tier 2; **Santos Dumont (SBRJ) 4.338 ft → tier 3**, o que quase elimina a ponte aérea saindo dele.
- O gerador só confere a pista do **destino** (`MissionDefinition.MinRunwayDest`, `RequiresHardSurface`), nunca a da **origem**; e os perfis "grandes" (`bigAirport`) ganham ×1,5 em qualquer tier 1–2.
- O tier é usado em: pesos das missões (`baseByTier`), taxas de aeroporto (`AirportFeeCatalog`), pagamento (`CalculateReward`, tiers de origem/destino), `FlightService`, `FlightMonitor`, `MainViewModel`, `MissionDisplayItem`, `FerryFlightViewModel`, simuladores de carreira.

## 2. Parte A — pista mínima por perfil (capacidade física)
- Cada perfil do `mission_catalog.json` ganha `minRunwayFt` e `paved` pela **classe do avião de referência** do perfil (§ 9.7.1 de `catalogo-missoes.md`):

| Classe | Exemplos do `aircrafts.csv` | `minRunwayFt` | `paved` |
|---|---|---|---|
| widebody / cargueiro pesado | 747-400, 747-8i, 787-10, A330, A400M (perfil pesado) | 8.000 | sim |
| narrowbody | A320neo, 737 MAX 8, A321 | 4.300 (opera em SBRJ, 4.338 ft) | sim |
| jato executivo de longo alcance | Citation Longitude | 4.900 | sim |
| jato executivo leve / médio | CJ4, PC-24 | 3.400 | sim |
| turboélice regional | ATR 42/72, ES30 | 3.600 | sim |
| turboélice / bimotor leve | King Air, PC-12, Baron, TBM, Titan, SkyCourier | 2.500 | não |
| monomotor utilitário / bush | Caravan, Twin Otter, C207T, C172, Cub, NXCub | 1.200 | não |
| instrução / local | C152, C172, DA40 | 0 (não filtra) | não |

- Vale para **origem e destino**: o gerador só escolhe perfis que a pista mais longa da origem comporta; se nenhum perfil do tipo couber, o tipo sai do sorteio naquele aeroporto. O destino é filtrado pelo perfil sorteado (substitui `MinRunwayDest`/`RequiresHardSurface` quando o perfil tem o campo).
- Fonte das distâncias: decisão do revisor com base no csv (`PistaMin` é conservador, de peso máximo) e em operações reais (A320/737 em SBRJ).

## 3. Parte B — tier por movimento (pontuação)
Função pura `Domain/Airports/AirportTierClassifier.Classify(AirportTierInput) → int`, limites em `Data/airport_tier_rules.json` (com padrão embutido). Entradas:
- **Etiquetas curadas** do `airport_profiles.json` (só as das listas curadas: `linha*`, `carga*`, `alimentador*`, `executivo*`, `saude*`, `mro*`, `instrucao*`, `aeroclube`, `agro*`, `militar*`, `zona_salto`, `turistico_curado*`; **não** usa as heurísticas `isolado`, `interior_distante`, `fazenda`, `altitude`, `turistico_poi`, que dependem do próprio tier).
- **Estrutura no simulador** (banco do Little Navmap hoje; SimConnect depois): cabeceiras com ILS (`num_runway_end_ils`), procedimentos (`num_approach`), gates (`num_parking_gate`), pontes (`num_jetway`), pátio de carga (`num_parking_cargo`), torre (`tower_frequency`/`has_tower_object`), pista mais longa e se é pavimentada.
- **OurAirports** (só com posição a até 10 NM): `type` e `scheduled_service`.

Pontuação inicial (ajustável no JSON):
- Etiquetas (vale a maior): `linha:mega_hub` 100 · `linha:hub`, `linha:ponte_aerea`, `linha:internacional`, `linha:domestico` 70 · `carga:cargueiros`, `carga:internacional` 60 · `linha` 50 · `alimentador` 35 · `executivo`, `saude`, `mro`, `carga` 25 · `instrucao`, `aeroclube`, `agro`, `zona_salto`, `turistico_curado` 10.
- Estrutura (soma): ILS 8 por cabeceira (máx. 32) · procedimentos 1 cada (máx. 20) · gates + pontes 1 cada (máx. 40) · pátio de carga 2 cada (máx. 20) · torre 8 · pista pavimentada ≥ 8.000 ft 15, ≥ 5.000 ft 8, qualquer pavimentada 3.
- OurAirports: `large_airport` 30 · `medium_airport` 15 · voo regular 15.
- Corte: **tier 1 ≥ 90 · tier 2 ≥ 45 · tier 3 ≥ 15 ou pista pavimentada · senão tier 4**; sem pista pavimentada, no máximo tier 3 (e só com ≥ 45).
- **Calibração** pelo relatório `tier-report` (Replay) sobre o banco do Little Navmap da máquina: alvo tier 1 ≈ 400–600 no mundo, tier 2 ≈ 8–10 %, tier 3 ≈ 25 %, resto tier 4. Casos de referência: SBGR, SBKP, SBSP, SBRJ, KJFK, EGLL → 1; SBMT → 2; SDCO → 2 ou 3; SDAO (fazenda MT) → 4; PABE (Bethel) → 2.

## 4. Ordem
1. Parte A e Parte B juntas na tarefa **TIER-A** (Gemini), com `tier-report` e `board-report`.
2. Revisão (Claude) + o usuário confere o `tier-report` antes do merge.
3. Depois: `airport_profiles.py` passa a ler o tier novo para as heurísticas (`isolado`, `interior_distante`); `career-stab` para recalibrar a economia (taxas e pagamento mudam com o tier).
4. O importador pelo SimConnect (`importador-aeroportos-simconnect.md`) usa o mesmo classificador.

## 5. Não testado
Nada disso depende do MSFS para testar a lógica; o resultado real depende do banco do Little Navmap da máquina do usuário. Não testado no MSFS nem visto na tela.

## 6. Etiquetas automáticas e destino de linha aérea (usuário, 2026-11-24)
- **Cobertura medida:** dos 38.598 aeroportos com pista no MSFS, só 2.199 (5,7 %) têm etiqueta curada; dos 3.772 com voo regular no OurAirports (posição batendo), 3.340 (89 %) não têm etiqueta de linha nem de alimentador (ex.: Ufa, Samara, Dar es Salaam, Bobo-Dioulasso); 531 têm ≥ 4 gates/pontes no simulador sem etiqueta de linha.
- **Feito (base, `tools/places/airport_profiles.py`):** etiquetas automáticas, com nome próprio para **não** entrar na pontuação do tier nem casar com `linha`/`carga`:
  - `linha_auto`: voo regular no OurAirports (posição batendo) **ou** ≥ 2 gates/pontes no simulador com pista pavimentada ≥ 4.300 ft → 3.801 aeroportos (SBMT fica fora; Ufa, Dar es Salaam, Bethel entram).
  - `carga_auto`: pátio de carga no simulador e pista pavimentada → só 64 (o MSFS quase não marca pátio de carga; sinal fraco).
- **A fazer na revisão do TIER-A:** missões de linha (`board` LinhaRegular e LinhaRegional, e `carga_paletizada`/`carga_overnight` nos perfis de jato) exigem no destino a etiqueta `linha` **ou** `linha_auto` (novo campo do destino, ex.: `requireAnyTag`), além do tier e da pista mínima. As listas curadas continuam como preferência forte (multiplicadores).
- **A fazer na revisão do TIER-A:** recalcular o tier na abertura quando a versão do `airport_tier_rules.json` mudar, a partir dos sinais gravados (sem reimportar).
- **Sem fonte automática ainda:** executivo/FBO, saúde/hospital, manutenção/oficina, instrução: continuam só pelas listas curadas (5,7 % dos aeroportos); para os demais vale só o peso base do tier. Ampliar as listas é trabalho de dados (curadoria).
- **Decisão do usuário (2026-11-24):** manter só as listas curadas como estão, sem nova curadoria; aeroportos sem etiqueta seguem o peso base do tier (nunca ficam sem missões nem sem destinos).
