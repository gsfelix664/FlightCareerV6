# Spec E2C-1b — variantes da navegação VFR (ida e volta; triângulo com toques e arremetidas)

## 1. Objetivo
Dar à `navegacao_vfr` três variantes sorteadas por perfil: o triângulo atual com paradas completas, uma **ida e volta** (origem → A → origem) e um **triângulo com toque e arremetida** em A e B (sem parar).

## 2. Fatos já levantados (não redescobrir)
- Gerador: `MissionGenerator.CreateVfrTriangleMissionSync` (`src/FlightCareer.Application/Services/MissionGenerator.cs:1012`), chamado em `MissionBoardCore` no ramo `def.Generator == "vfr_triangle"`. Hoje não recebe o perfil sorteado (`profile`), que já existe no `MissionBoardCore` nesse ponto.
- Catálogo (`src/FlightCareer.Infrastructure/Data/mission_catalog.json`, `navegacao_vfr`): `generator: "vfr_triangle"`, um perfil `{ weight 100, rangeNm [80, 250], pax [1, 1] }`.
- Contrato: `MultiStopContractData { List<StopPoint> Stops; bool RequireNoRefuel }` (`src/FlightCareer.Domain/Missions/ContractData.cs:26`). Rastreador: `MultiStopTracker` (`src/FlightCareer.Domain/Missions/MultiStopTracker.cs`), que conta uma escala pela parada completa (solo, GS < 3 kt por ≥ 5 s simulados, a ≤ 1,5 NM) e exige nova decolagem (≥ 5 s no ar) antes da escala seguinte. Conclusão em `MissionCompletion` (ramo `vfr_triangle`). O ponto de reportagem fica em `OverflyWaypoints` (`P:`) e é checado pelo `OverflyTracker`.
- O `TouchAndGoCounter` (`src/FlightCareer.Domain/Missions/TouchAndGoCounter.cs`) já define um TGL válido: toque ≥ 1 s sem quique, GS máxima no solo > 20 kt, sem parada completa (GS < 5 kt), seguido de decolagem. Reaproveite a mesma definição (sem duplicar constantes: extraia ou reutilize).
- Textos: `mission_texts.json` › `navegacao_vfr` usa `{stop_a}` e `{stop_b}`.

## 3. Arquivos
- Mexer: `MissionDefinition.cs` (`MissionProfileDefinition.Variant`, string opcional), `ContractData.cs` (`MultiStopContractData.StopMode`: `"full_stop"` padrão | `"touch_and_go"`), `MultiStopTracker.cs`, `MissionBoardCore.cs` (passar `profile` ao gerador), `MissionGenerator.cs` (variantes), `mission_catalog.json` (perfis da `navegacao_vfr`), `mission_texts.json` (textos por variante), testes.
- **Fora do escopo:** `FlightMonitor` (não precisa mudar: o rastreador já recebe o `SimTelemetry` e o `simDt`), `pinga_pinga` (continua `full_stop`; o padrão do `StopMode` mantém o comportamento), outros tipos, pesos de origem.

## 4. Contrato
Perfis da `navegacao_vfr` no catálogo (o peso define a frequência):

| `variant` | peso | Rota | Etapas | Total | Escalas |
|---|---|---|---|---|---|
| `triangle` | 50 | origem → A → B → origem | 25–90 NM | 80–250 NM | parada completa em A e B |
| `out_and_back` | 25 | origem → A → origem | 40–120 NM | 80–240 NM | parada completa em A |
| `triangle_tgl` | 25 | origem → A → B → origem | 25–70 NM | 75–200 NM | toque e arremetida em A e B |

- A e B tier 3, sem repetir e diferentes da origem, como hoje. Todas as variantes têm ponto de reportagem `P:` no meio da primeira etapa (desvio lateral de 3–8 NM) e pouso final na origem; 1 pax (instrutor); pagamento pela distância total da rota.
- Perfil sem `variant` = `triangle` (compatível com o que existe).
- `ContractData` grava `StopMode` (`touch_and_go` só na `triangle_tgl`).
- `MultiStopTracker` com `StopMode = touch_and_go`: a escala conta com um TGL válido (definição do `TouchAndGoCounter`) a ≤ 1,5 NM da escala; uma parada completa também conta (o aluno não é reprovado por parar). Ordem, reabastecimento e "nova decolagem ≥ 5 s" iguais ao modo atual.
- Textos (≥ 2 variantes por perfil, chave por variante, ex.: `navegacao_vfr:out_and_back`, com fallback no texto atual se faltar): ida e volta cita `{stop_a}` e "pouso completo"; triângulo TGL cita `{stop_a}`, `{stop_b}` e "toque e arremetida". Nenhum `{}` sobrando.

## 5. Testes
- `MultiStopTrackerTests` (modo TGL): TGL em A e B na ordem → completa; TGL fora do raio de 1,5 NM → não conta; toque com quique (< 1 s) → não conta; parada completa em A (modo TGL) → conta.
- `MultiStopTrackerTests` (modo padrão): comportamento atual inalterado (os testes existentes continuam passando sem mudança).
- Gerador (N quadros com semente fixa): as 3 variantes aparecem; `out_and_back` tem 1 escala e etapa 40–120; `triangle_tgl` tem 2 escalas com `StopMode = touch_and_go`; todas voltam à origem e têm `P:`; distâncias dentro das faixas da tabela; textos sem `{}`.
- `MissionCompletion`: ida e volta sem a escala A → falha citando A.

## 6. Critérios de aceite
1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
2. Snapshot `missions_golden.json` regravado; o relatório diz quantos registros mudaram e por quê (esperado: `navegacao_vfr` e o efeito de roleta nos aeroportos onde ela tem peso).
3. `board-report --boards 50`: colar no relatório a linha da `navegacao_vfr` (frequência e faixas). Não rodar `career-stab`.
4. `docs/CHANGELOG.md` (`## 2026-11-24 - Variantes da navegação VFR (E2C-1b) (Gemini)`) e `AGENTS.md` › 7 (bloco E2C-1, se existir; senão o bloco E2A).
5. Um commit: `feat(missoes): navegacao VFR com ida e volta e triangulo com toques (E2C-1b)`. Não testado no MSFS: declarar.
