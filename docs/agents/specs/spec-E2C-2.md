# Spec E2C-2 — trabalho aéreo (patrulha, inspeção de linhas, monitoramento de tráfego, SAR) e ronda de saúde

## 1. Objetivo
Ligar `patrulha_vigilancia`, `inspecao_linhas`, `monitoramento_trafego`, `sar` e `saude_familia`, com um rastreador puro novo de trabalho aéreo (`AerialWorkTracker`) em `Domain/Missions` e a ronda de saúde reaproveitando o `MultiStopTracker`. Nenhuma SimVar nova.

## 2. Fatos já levantados (não redescobrir)
- Regras: `docs/plans/catalogo-missoes.md` › 6 (linhas 122, 132–135), › 9.3 (277, 283–286), › 9.7.1 (420, 430–433) e › 11 (decisões deste lote).
- Catálogo (`src/FlightCareer.Infrastructure/Data/mission_catalog.json`): os 5 tipos existem com `enabled: false`, `generator: "standard"`, pesos e perfis já certos (patrulha 30–200 NM, 1–2 pax; inspeção 40–200, 1 pax; monitoramento 10–40, 1–2 pax, 0–50 lb; sar 40–250, 1–3 pax; saude_familia 50–250 ida, 3–8 pax, 100–600 lb, destino `profiles: {isolado: 4}`, tiers 3–4).
- Despacho dos geradores: `MissionBoardCore.cs` ~linhas 190–280 (`if (def.Generator == "...")`). Padrão de destino escolhido pelo catálogo: ramo `ferry_permit` (`FilterCandidates(origin, def, profile, candidates, getAirportTags, candidateDistances)` + `PickDestination(...)`).
- Padrão de gerador com área e POI: `MissionGenerator.CreateSurveyMissionSync` (`MissionGenerator.cs:881`): `places.FindPois(lat, lon, minNm, maxNm, kinds)`, `PickWeightedPoi`, `FormatPoiWithKind`, coordenada procedural quando não há POI, `ContractData` em JSON, `textCatalog.Render(sub.Id, random, values, fallbackTitle, fallbackDesc)`, `CreateMissionObjectSync(origin, origin, distance, ...)` (paga pela distância passada). Tipos de POI existentes (`places/tourist_pois.json`, campo `k`): `parque_nacional`, `reserva_natural`, `lago`, `barragem`, `estadio`, `ponte`, `porto`, `arranha_ceu`, `marco_arquitetonico`, entre outros.
- Ida e volta com parada completa já existe: `navegacao_vfr` variante `out_and_back` em `CreateVfrTriangleMissionSync` (`MissionGenerator.cs:1175`) com `MultiStopContractData { Stops, RequireNoRefuel, StopMode }`.
- `SimTelemetry` **não tem AGL**: altitudes são MSL (`Altitude`). Como no `OverflyTracker`, a faixa "acima do solo" é relativa a uma elevação de referência gravada no contrato (aqui: elevação da origem). Não acrescentar SimVar.
- `FlightMonitor.cs` (arquivo quente): rastreadores configurados por `def?.Generator` no laço ~linhas 684–740 (`Reset` logo antes, ~670–682); `Update` no tick ativo ~linhas 1221–1243 com o `simDt` simulado (que respeita pausa); `ContractProgressFormatter.Build(...)` em ~1246; `new MissionTrackerResults(...)` em ~1937.
- `MissionCompletion.Evaluate`: ramos por `generator` (ex.: `multi_stop`, `vfr_triangle` ~linhas 139–165) usando `trackers`.
- HUD: `ContractProgressFormatter` (`ContractProgressInputs` record) e mapa: `ContractMarkerBuilder` (detecta o contrato pelas propriedades do JSON).

## 3. Arquivos
- Criar: `src/FlightCareer.Domain/Missions/AerialWorkTracker.cs`.
- Mexer: `ContractData.cs` (classes novas), `MissionTrackerResults.cs` (`AerialWorkTracker? AerialWork = null` no fim), `MissionCompletion.cs`, `ContractProgressFormatter.cs` (`ContractProgressInputs.AerialWork` no fim + linha do HUD), `ContractMarkerBuilder.cs`, `MissionBoardCore.cs`, `MissionGenerator.cs` (5 geradores), `mission_catalog.json` (só os 5 tipos: `enabled: true`, `generator`, perfis da SAR), `mission_texts.json` (≥ 3 variantes por tipo; SAR com variantes por padrão), `FlightMonitor.cs` (integração fina, § 6), testes.
- **Fora do escopo:** `adaptacao_noturna`, `evacuacao_por_do_sol`, `instrucao_ifr`, `demo_flight`, `pulverizacao`, `reboque_planador`, `publicidade_aerea`, `calibracao_auxilios`, `combate_incendio`; regra do pôr do sol na ronda de saúde; pouso na pista da fazenda na patrulha; `SimTelemetry`/`SimConnectService`; pesos e faixas de outros tipos.

## 4. Contrato

### 4.1 Dados (`ContractData.cs`)
```csharp
public sealed class AerialWorkContractData
{
    public string Mode { get; set; } = "sectors"; // "sectors" | "route" | "orbit" | "search"
    public List<WorkPoint> Points { get; set; } = new();
    public double PointRadiusNm { get; set; } = 1.0;
    public double RefElevationFt { get; set; }      // elevação da origem
    public double MinAglFt { get; set; }            // 0 = sem piso
    public double MaxAglFt { get; set; }            // teto acima de RefElevationFt
    public double MaxIasKt { get; set; }            // 0 = sem limite
    public double OrbitCenterLat { get; set; }
    public double OrbitCenterLon { get; set; }
    public double OrbitRadiusNm { get; set; }
    public double OrbitMinutes { get; set; }
    public double TargetLat { get; set; }           // SAR: alvo escondido (não vai para o mapa)
    public double TargetLon { get; set; }
    public double TargetRadiusNm { get; set; } = 0.3;
    public string TargetName { get; set; } = string.Empty;
}
public sealed class WorkPoint { public string Ident { get; set; } = ""; public double Lat { get; set; } public double Lon { get; set; } }
```

### 4.2 `AerialWorkTracker` (lógica pura, tempo simulado)
`Configure(AerialWorkContractData?)`, `Reset()`, `Update(bool onGround, double lat, double lon, double altitudeFt, double iasKt, double simDt)`.
Amostra **válida** = no ar, `RefElevationFt + MinAglFt ≤ altitude ≤ RefElevationFt + MaxAglFt` e (`MaxIasKt == 0` ou `ias ≤ MaxIasKt`). Expor `HasActiveContract`, `Mode`, `CompletedPoints`, `TotalPoints`, `NextPointIdent`, `OrbitSecondsAccumulated`, `TargetFound`, `IsCompleted`, `FailureReason` (texto em português do primeiro requisito não cumprido) e o último estado para o HUD (distância ao próximo ponto/centro, `AltitudeState` Below/InBand/Above, IAS acima do limite).
- `sectors`: cada ponto conta com amostra válida a ≤ `PointRadiusNm`, **qualquer ordem**. Completo com todos. Falha: "Setores sem cobertura: S2, S4 (voe a até X ft e Y kt)".
- `route`: pontos **na ordem**; só o próximo conta (válida, ≤ `PointRadiusNm`). Expor `LateralDeviationNm` = distância à reta (ponto anterior → próximo; o primeiro trecho parte da posição onde o voo cruzou a ≤ 2 NM do 1º ponto, ou só a distância ao 1º ponto). Completo com o último. Falha: "Traçado incompleto: ponto L4 de 8 não inspecionado".
- `orbit`: soma `simDt` das amostras válidas a ≤ `OrbitRadiusNm` do centro. Completo quando soma ≥ `OrbitMinutes × 60`. Falha: "Órbita de 7 de 15 min".
- `search`: pontos na ordem só para progresso ("Perna x/N"); `TargetFound` com amostra válida a ≤ `TargetRadiusNm` do alvo, em qualquer momento. Completo = alvo achado. Falha: "Alvo não encontrado (perna 5 de 8 do padrão)".
- Distância: reutilizar `OverflyTracker.CalculateDistanceNM`.

### 4.3 Geradores (`MissionGenerator`), um por tipo, distância paga = rota estimada
Todos voltam à origem (`dest = origin`), `RefElevationFt = origin.ElevationFt`, pax/carga do perfil sorteado (passar `paxCount`, `cargoLbs` do `MissionBoardCore`), bagagem via `WithPersonLuggage` como os demais. Retornam `null` se não couber na faixa do perfil depois de 10 tentativas.

| Tipo | `generator` | Geometria | Faixa e limites |
|---|---|---|---|
| `patrulha_vigilancia` | `patrol` | centro a 10–60 NM (POI `parque_nacional`/`reserva_natural`/`lago`/`barragem` nessa distância, senão procedural "área de patrulha a N NM de ORIG"); 3–5 setores `S1..Sn` num círculo de 3–6 NM do centro, ângulos espaçados ≥ 50° | `sectors`, raio 1,0 NM, teto 1.500 ft, sem piso, IAS ≤ 130 kt; distância = 2 × centro + perímetro entre setores |
| `inspecao_linhas` | `line_inspection` | 1º ponto a 3–8 NM da origem; 6–10 pontos `L1..Ln`, trechos de 5–15 NM, rumo do trecho variando ≤ ±25° do anterior; `{line_kind}` sorteado: "linha de transmissão", "gasoduto", "ferrovia" | `route`, raio 0,5 NM, teto 1.000 ft, sem piso, sem limite de IAS; distância = origem→L1 + traçado + Ln→origem, dentro de 40–200 |
| `monitoramento_trafego` | `traffic_orbit` | alvo a 5–20 NM (POI `estadio`/`ponte`/`porto`/`arranha_ceu`/`marco_arquitetonico`, senão "trecho de rodovia a N NM de ORIG") | `orbit`, raio 1,5 NM, 10–20 min (inteiro), faixa 1.000–2.500 ft, sem limite de IAS; distância = 2 × alvo + minutos × 1,5 |
| `sar` | `sar_pattern` | datum a 15–100 NM; perfis com `variant` `expanding_square` (peso 50) e `creeping_line` (50). Quadrado crescente: espaçamento 1 NM, pernas 1,1,2,2,3,3,4,4 NM, rumo inicial múltiplo de 10°, curvas de 90° à direita, pontos `P1..P8` no fim de cada perna. Rota paralela: 5–7 pernas de 4–6 NM espaçadas 1 NM, pontos alternando as pontas. Alvo numa perna ≥ 2 sorteada, a 20–80 % do comprimento, deslocado ≤ 0,15 NM da perna; `TargetName` sorteado ("aeronave desaparecida", "embarcação à deriva", "excursionistas perdidos") | `search`, raio dos pontos 0,5 NM, alvo 0,3 NM, teto 1.500 ft, sem piso, IAS ≤ 160 kt; distância = 2 × datum + comprimento do padrão, dentro de 40–250 |
| `saude_familia` | `round_trip` | destino pelo catálogo (`FilterCandidates` + `PickDestination`, como o `ferry_permit`), ida 50–250 NM | `MultiStopContractData { Stops = [destino], StopMode = "full_stop", RequireNoRefuel = false }`; distância paga = 2 × ida |

### 4.4 Conclusão, HUD e mapa
- `MissionCompletion`: para `patrol`, `line_inspection`, `traffic_orbit`, `sar_pattern`: `trackers.AerialWork` não completo → falha com `FailureReason`. `round_trip`: mesma regra do `vfr_triangle` (escala não visitada → falha citando o ICAO). Pouso final na origem como nos demais. Chamadas antigas sem `trackers` inalteradas.
- HUD (`ContractProgressFormatter`), uma linha: `Setores 2/4 · S3 1,8 NM · máx. 2.100 ft: acima`; `Traçado 3/8 · L4 2,4 NM · desvio 0,3 NM`; `Órbita 6/15 min · 1,2 NM`; `Busca: perna 3/8 · alvo não achado` → `Alvo encontrado ✓`; IAS acima do limite acrescenta ` · reduza para 130 kt`. Concluído: `✓` no fim, como os outros. `round_trip` já sai pela linha de escalas existente.
- Mapa (`ContractMarkerBuilder`): setores/pontos como `Point` com o raio; centro da órbita como `Area` (raio da órbita); SAR: só o datum e os pontos do padrão, **nunca o alvo**.

## 5. Testes (xUnit, nomes em português)
- `AerialWorkTrackerTests`:
  - sectors: 3 setores visitados fora de ordem → completo; setor visitado acima do teto → não conta; IAS acima do limite → não conta; no solo → não conta; falha lista os setores faltantes.
  - route: pontos na ordem → completo; passar pelo L3 antes do L2 → L3 não conta até o L2; desvio lateral calculado (ponto a 0,4 NM da reta → 0,4 ± 0,05).
  - orbit: 15 min exigidos, 900 s simulados válidos (`simDt` 1 s × rate 4 = 225 pacotes) → completo; amostras fora do raio não somam; fora da faixa não somam.
  - search: passar a 0,2 NM do alvo → achado e completo; a 0,5 NM → não; falha cita a perna.
- Geradores (N quadros/semente fixa por tipo): faixas da tabela (distâncias, nº de pontos, raios, faixas de altitude); `sar` gera as duas variantes; alvo a ≤ 0,15 NM de uma perna ≥ 2; `inspecao_linhas` respeita ±25°; `saude_familia` destino tier 3–4 a 50–250 NM com 1 escala; textos sem `{}`; JSON do contrato desserializa.
- `MissionCompletion`: cada tipo com tracker incompleto → falha com o motivo; completo → sucesso; `round_trip` sem escala → falha citando o ICAO.
- `ContractProgressFormatter`: uma linha por modo (texto exato como no § 4.4).
- `ContractMarkerBuilder`: contrato SAR não gera marcador no alvo.

## 6. Integração no `FlightMonitor` (arquivo quente — ler inteiro; mexer só nisto)
1. Campo `_aerialWorkTracker` ao lado dos outros rastreadores.
2. `Reset()` junto dos outros resets (~670–682).
3. No laço de configuração (~684–740): `else if` para `patrol`/`line_inspection`/`traffic_orbit`/`sar_pattern` desserializando `AerialWorkContractData` (mesmo padrão de `try/catch (Exception ex)` com `Debug.WriteLine`); acrescentar `round_trip` à condição do ramo `multi_stop || vfr_triangle`.
4. `Update` logo depois do `_ferryMonitor.Update` (~1243) com `data.IsOnGround, data.Latitude, data.Longitude, data.Altitude, data.Airspeed, simDt` (o mesmo `simDt`; **não** criar relógio próprio).
5. Passar `AerialWork: _aerialWorkTracker` em `ContractProgressInputs` (~1246) e em `MissionTrackerResults` (~1937).
- Não mudar início/fim do voo, combustível, slew, toque, crash nem a ordem dos outros `Update`.

## 7. Critérios de aceite
1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
2. Snapshot `missions_golden.json` regravado; relatório diz quantos registros mudaram (esperado: os 5 tipos e o efeito de roleta nos aeroportos onde têm peso; nenhuma regra de outro tipo alterada).
3. `board-report --boards 50`: colar no relatório as linhas dos 5 tipos (frequência, pax, carga, distância). Não rodar `career-stab`.
4. `docs/CHANGELOG.md` (`## 2026-11-25 - Trabalho aéreo e ronda de saúde (E2C-2) (Gemini)`) e `docs/sistemas/missoes-quadro.md` (tipos ligados e o rastreador novo).
5. Relatório declara o não testado no MSFS (todo o voo) e as limitações: faixa "acima do solo" relativa à elevação da origem (sem AGL real), traçados e áreas procedurais, sem regra do pôr do sol.
6. Um commit: `feat(missoes): patrulha, inspecao de linhas, monitoramento de trafego, SAR e ronda de saude (E2C-2)`.
