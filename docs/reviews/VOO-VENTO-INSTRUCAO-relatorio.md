# VOO-VENTO-INSTRUCAO — vento de través, vento no REC e instrução local

**Branch:** fix/vento-instrucao · **Commit:** (a preencher após commit) · **Data:** 2026-11-25

## Plano (antes de codar)
- Adicionar `PlaneHeadingTrue` ao final do struct `SimTelemetry` e espelhar com `AddToDef("PLANE HEADING DEGREES TRUE", "degrees")` no `SimConnectService.cs`.
- Atualizar o cálculo de `CrosswindComponentKnots` e `TailwindComponentKnots` para usar `PlaneHeadingTrue`.
- Adicionar ao fim de `sensor_registry.json` as SimVars `amb-wind-velocity`, `amb-wind-direction`, `nav-heading-true` e `nav-heading-magnetic` (grupo `airframe`).
- Estender `OverflyWaypointItem`, `OverflyPoint` e `OverflyWaypointCodec` para suportar o 5º campo opcional `minAglFt` mantendo compatibilidade retroativa.
- Atualizar `OverflyTracker` com `AltitudeToleranceFt = 500.0` para checar faixa de altitude quando `MinAglFt > 0`.
- Ajustar `MissionGenerator` para instrução local (1 a 2 TGL, faixa no waypoint) e cheque prático (faixa no waypoint).
- Enriquecer mensagem de erro de ponto não visitado em `MissionCompletion.cs` com a faixa exigida.
- Atualizar testes de unidade, inventário de sensores, layouts de leitura e snapshot `missions_golden.json`.

## O que foi feito
- `src/FlightCareer.Domain/Entities/SimTelemetry.cs`: campo `PlaneHeadingTrue` adicionado como último double do struct (posição 49 no layout); propriedades `CrosswindComponentKnots` e `TailwindComponentKnots` agora usam `PlaneHeadingTrue`, corrigindo desvio de ~22° por declinação magnética.
- `src/FlightCareer.UI/Services/SimConnectService.cs`: adicionado `AddToDef("PLANE HEADING DEGREES TRUE", "degrees");` após `LIGHT TAXI`, preservando alinhamento estrito com `SimTelemetry`.
- `src/FlightCareer.Infrastructure/Data/sensor_registry.json`: adicionadas variáveis `amb-wind-velocity`, `amb-wind-direction`, `nav-heading-true` e `nav-heading-magnetic` no grupo `airframe` (`perEngine: false`) para gravação contínua no REC.
- `docs/plans/sensor-inventory.md`: inventário atualizado na seção 10 com as novas SimVars.
- `src/FlightCareer.Domain/Missions/OverflyPoint.cs` e `OverflyWaypointCodec.cs`: adicionado `MinAglFt = 0.0`. `Parse` processa 5º parâmetro opcional (`minAglFt`); `Format` e `FormatCoordinatePoint` gravam o 5º parâmetro quando `MinAglFt > 0`, preservando integralmente formato legado sem o 5º parâmetro.
- `src/FlightCareer.Domain/Missions/OverflyTracker.cs`: adicionada constante `public const double AltitudeToleranceFt = 500.0;`. Quando `point.MinAglFt > 0`, valida passagem dentro do raio e com altitude MSL em `[ElevationFt + MinAglFt - 500, ElevationFt + MaxAglFt + 500]`; se `MinAglFt <= 0`, preserva regra anterior sem limite inferior.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: repassa `item.MinAglFt` na montagem dos pontos de sobrevoo.
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: `CreateLocalInstructionMissionSync` agora sorteia `random.Next(1, 3)` (1 ou 2 TGL) e define o waypoint da área de instrução com raio 3.0, `maxAglFt = altTop` e `minAglFt = altBase`; `CreateCheckrideMissionSync` define o waypoint com a mesma faixa de altitude.
- `src/FlightCareer.Domain/Missions/MissionCompletion.cs`: quando o ponto não visitado possui `MinAglFt > 0`, anexa ` (voe entre {min:N0} e {max:N0} ft acima da origem)`.
- `docs/plans/missoes-locais-rp.md`: adicionada seção 4 ("Decisões do usuário (2026-11-25)") com as deliberações de instrução local, faixa de altitude e SimConnect facilities.
- `docs/sistemas/missoes-quadro.md`, `docs/sistemas/sensores.md` e `docs/CHANGELOG.md`: documentação atualizada conforme o estado atual dos sistemas.
- Testes implementados / atualizados:
  - `TelemetryDefinitionTests.cs`: `SimTelemetry_Vento_UsaProaVerdadeira` e `SimTelemetry_Vento_Traves90Graus`.
  - `OverflyTrackerTests.cs`: `OverflyTracker_ComFaixa_ForaAbaixo_NaoConta`, `OverflyTracker_ComFaixa_ForaAcima_NaoConta` e `OverflyTracker_SemMinimo_RegraAntiga`.
  - `OverflyWaypointCodecTests.cs`: `Codec_MinAgl_IdaEVolta`.
  - `MissionGeneratorLocalTests.cs`: `LocalInstruction_Tgl_EntreUmEDois`.
  - `MissionCompletionLocalTests.cs`: `MissionCompletion_FaixaNaoCumprida_MensagemComAltitudes`.
  - `SensorLayoutTests.cs`: contagem de sensores atualizada para 443 (+4 sensores).
  - `missions_golden.json`: snapshot atualizado.

## Critérios de aceite
- [x] dotnet build src/FlightCareer.slnx passa com 0 erros (0 erros, 81 warnings preexistentes).
- [x] dotnet test src/FlightCareer.Tests passa 100% (1.071 testes aprovados, 0 falhas).
- [x] Snapshot missions_golden.json mudou apenas nos contratos de instrução local e cheque prático (confirmado via git diff).
- [x] Sem simulações longas (career-stab não executado).
- [x] CHANGELOG e documentos de sistema atualizados.
- [x] Commit único no formato especificado.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 1.071 aprovados, 0 falhas (novos: `SimTelemetry_Vento_UsaProaVerdadeira`, `SimTelemetry_Vento_Traves90Graus`, `OverflyTracker_ComFaixa_ForaAbaixo_NaoConta`, `OverflyTracker_ComFaixa_ForaAcima_NaoConta`, `OverflyTracker_SemMinimo_RegraAntiga`, `Codec_MinAgl_IdaEVolta`, `LocalInstruction_Tgl_EntreUmEDois`, `MissionCompletion_FaixaNaoCumprida_MensagemComAltitudes`).

## Não testado
- Conexão e visualização em tempo real com MSFS aberto (leitura do SimConnect em voo real).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. Todas as correções e testes foram executados rigorosamente dentro do escopo.
