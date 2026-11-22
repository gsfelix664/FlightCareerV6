# Spec TOQUE-NORMAL — Razão de descida do toque pela SimVar do simulador

**Objetivo (1 frase):** medir a razão de descida no pouso pela `PLANE TOUCHDOWN NORMAL VELOCITY` (velocidade normal ao solo no último toque, calculada pelo MSFS), com `VERTICAL SPEED` como reserva, e parar de usar `VELOCITY BODY Y`, que transforma o ângulo de ataque em "descida".

## Fatos já levantados (não redescobrir)
- Voo real de 2026-11-22 (C152, SNEJ→SBJD, blackbox `flight_20261122_082546_SNEJ_SBJD_PP-UZJ.csv`): pouso suave (VERTICAL SPEED de +64 a −2 fpm no último segundo, pico 1,09 G), o addon registrou **650 fpm** ("pouso duro", inspeção do trem, pneus −4,8 %, nota 65 %). O próprio simulador mediu `PLANE TOUCHDOWN NORMAL VELOCITY` = **1,666 ft/s ≈ 100 fpm** (REC `20261122-0822-Cessna_C152.csv`).
- Causa: `TouchdownDetector.Classify` (`src/FlightCareer.Domain/Components/TouchdownDetector.cs`, linhas 106–111) usa `max(−VerticalSpeedFpm, −BodyVerticalFps × 60)` na janela antes do contato. `VELOCITY BODY Y` é no eixo do avião: com nariz 6° acima a ~101 ft/s, dá ≈ −10,6 ft/s → 630 fpm mesmo sem descer.
- SDK (MSFS 2024, aircraft-miscellaneous-variables): `PLANE TOUCHDOWN NORMAL VELOCITY`, "the player's plane speed according to ground normal from the last touchdown", unidade **feet per second**, só leitura. Mantém o valor do último toque até o próximo (por isso só vale como medida **depois** do contato, e só se mudou em relação ao valor de antes do contato). No REC do C152 o valor fica ≥ 0 (positivo = descendo); usar `Math.Abs` por segurança. Aceita pelo simulador na sonda do C152 (`docs/plans/sensor-validation/probe-runs/20261116-0750-Cessna_C152/report.csv`).
- Leitura rápida (a cada 2 frames): struct `SimFastFlightState` em `src/FlightCareer.UI/Services/SimConnectService.cs` (~linhas 70–79) e as chamadas `AddFastDef(...)` (~linhas 326–333), registradas por `RegisterDataDefineStruct<SimFastFlightState>(DEFINITIONS.FastFlight)`. Único consumidor: `FlightMonitor` (~linha 1206) monta `new FastFlightSample(...)` e chama `_touchdown.Update(...)`.
- Janelas (`LandingSettings` em `Domain/Components/ComponentCatalog.cs`): `ContactWindowBeforeSec` 1,0; `ContactWindowAfterSec` 0,5; `MinAirborneSec` 0,5; `TraceBeforeSec` 2,0. Limites de pouso duro no `component_catalog.json` › `landing` (hardLandingFpm 400, severeLandingFpm 600; crash em `CrashFpm`).

## Arquivos a mexer (passo a passo)
1. `src/FlightCareer.UI/Services/SimConnectService.cs` (**arquivo quente**):
   - no **fim** do struct `SimFastFlightState`, depois de `GroundSpeed`, acrescentar `public double TouchdownNormalVelocity; // PLANE TOUCHDOWN NORMAL VELOCITY (ft/s, normal ao solo no último toque)`;
   - logo depois de `AddFastDef("GROUND VELOCITY", "knots");`, acrescentar `AddFastDef("PLANE TOUCHDOWN NORMAL VELOCITY", "feet per second");`.
   - **Não** remover `BodyVertical`/`VELOCITY BODY Y` (o struct é lido por posição; manter a ordem — AGENTS.md › 6). Não mexer em nenhuma outra definição.
2. `src/FlightCareer.Domain/Components/TouchdownDetector.cs`:
   - `FastFlightSample` ganha o parâmetro **no fim** `double TouchdownNormalFps = double.NaN` (default para não quebrar os testes existentes). `BodyVerticalFps` continua no record (só deixa de entrar na conta).
   - Em `Classify`: (a) `vsFpm` = maior `−VerticalSpeedFpm` (positivo = descendo) nas amostras `before`, como hoje, **sem** o termo de `BodyVerticalFps`; (b) `normalFpm`: valor de referência = `TouchdownNormalFps` da última amostra com `TimeSec < contact` (se não houver, NaN); entre as amostras de `contact` até `contact + ContactWindowAfterSec`, pegar a **última** com `TouchdownNormalFps` finito e **diferente** da referência (|Δ| > 1e-6); se existir, `normalFpm = Math.Abs(valor) × 60`. (c) `fpm = normalFpm` se existir; senão `vsFpm`. O resto (`peakG`, atitude, razões de crash, trace) fica igual.
   - Atualizar o comentário da classe dizendo a fonte da medida.
3. `src/FlightCareer.UI/Services/FlightMonitor.cs` (**arquivo quente**): na construção de `FastFlightSample` (~linha 1206), passar `s.TouchdownNormalVelocity` como último argumento. Nada mais.
4. `src/FlightCareer.Tests/Components/TouchdownDetectorTests.cs`: testes novos (abaixo). Os existentes devem continuar passando; se algum dependia de `BodyVerticalFps` para dar pouso duro, mude o teste para usar `VerticalSpeedFpm` ou `TouchdownNormalFps` e explique no relatório.

## Fora do escopo (não mexer)
- Limites do `component_catalog.json`, `WearEngine`, `PartFlightTracker`, nota de qualidade, HUD, demais definições do SimConnect, `SimTelemetry`.

## Testes que devem existir e passar
- `Pouso_suave_de_nariz_alto_usa_a_velocidade_normal_do_sim`: aproximação com VS caindo de −60 a 0 fpm, `BodyVerticalFps` = −10,6 e nariz +6° nas amostras antes do contato; `TouchdownNormalFps` = 0,30 antes do contato (toque anterior) e 1,666 depois → `TouchdownFpm` ≈ 100 (±1), `Kind` Normal.
- `Sem_velocidade_normal_usa_vertical_speed_e_ignora_body_y`: `TouchdownNormalFps` = NaN em tudo; VS mínimo −250 fpm antes do contato; `BodyVerticalFps` = −12 → `TouchdownFpm` = 250 (não 720).
- `Velocidade_normal_sem_mudar_no_contato_usa_vertical_speed`: `TouchdownNormalFps` constante 1,5 antes e depois (o sim não atualizou) → usa VS.
- `Pouso_duro_pela_velocidade_normal`: `TouchdownNormalFps` muda para 12,5 ft/s (750 fpm) depois do contato, VS −700 → `TouchdownFpm` = 750.
- `Crash_pela_velocidade_normal`: valor normal acima de `CrashFpm`/60 → `Kind` Crash com o motivo de razão de descida.
- `TelemetryDefinitionTests` (se cobrir a leitura rápida) continua passando com +1 datum e +1 campo.

## Critérios de aceite
- [ ] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
- [ ] Campo novo só no **fim** do struct, na mesma ordem e unidade do `AddFastDef`.
- [ ] `BodyVerticalFps` não entra mais na razão de descida.
- [ ] Entrada no fim do `docs/CHANGELOG.md`; `AGENTS.md` › 7 (`FlightMonitor`/toque) cita a fonte nova. Relatório diz "não testado no MSFS".
- [ ] Um commit: `fix(toque): razao de descida pela PLANE TOUCHDOWN NORMAL VELOCITY (TOQUE-NORMAL)`.

## Armadilhas
- AGENTS.md › 6: struct e `AddFastDef` na mesma ordem, só acrescentar no fim; nome de SimVar inválido desloca o struct (este nome foi aceito pelo MSFS 2024 na sonda).
- O valor da SimVar é do **último** toque: antes do contato ele traz o pouso anterior (ou 0). Por isso a comparação com a referência.
