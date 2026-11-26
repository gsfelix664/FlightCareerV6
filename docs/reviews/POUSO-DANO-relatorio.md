# POUSO-DANO — Quebra de trem/pneu no pouso, pouso forçado nas peças e inspeções no Hangar

**Branch:** feat/pouso-dano · **Commit:** (a gerar) · **Data:** 2026-11-26

## Plano (antes de codar)
1. Criar `LandingBreakModel` no Domain com cálculo determinístico de chance base (`GearBaseChance`), multiplicadores de pneu, inclinação, pista e desgaste (`ChanceFor`), e sorteio de quebra (`Roll`), calibrado via `LandingSettings` e `component_catalog.json`.
2. Integrar suporte a `isGrounding` no `WearRuleCatalog` (D6) e criar `InspectionDefectPolicy` / método `IsInspection` em `DefectCatalog` (D5).
3. Atualizar `FlightService` para cancelar missões ativas, aplicar XP parcial (fator 0.5), avançar relógio de carreira e aceitar flag de dano prévio de terreno (B4, D1, D10).
4. Atualizar `FlightTelemetryLogger` com `RecordPeakG` e `FlightLogEntry` com status `"POUSO_FORCADO"` (D11).
5. Integrar quebra no toque em `PartFlightSession` e interdição/alertas/inspeções em `FlightMonitor`.
6. Cobrir com testes unitários abrangentes, verificar build com 0 erros e suite xUnit verde.

## O que foi feito
- `src/FlightCareer.Domain/Parts/LandingBreakModel.cs`: modelo puro de cálculo de probabilidade e sorteio de quebra de montantes de trem de pouso e pneus no toque.
- `src/FlightCareer.Domain/Components/ComponentCatalog.cs` e `src/FlightCareer.Infrastructure/Data/component_catalog.json`: parâmetros de calibração adicionados na seção `landing`.
- `src/FlightCareer.Domain/Parts/WearRuleCatalog.cs`: suporte a `isGrounding` / `grounding` nullable nos DTOs de inspeção.
- `src/FlightCareer.Domain/Services/DefectCatalog.cs`: reconhecimento de `*_INSPECTION` via `IsInspection` com spec padrão de inspeção não interditante (custo base 400).
- `src/FlightCareer.Domain/Parts/InspectionDefectPolicy.cs`: política que restringe interdição imediata às 5 inspeções de toque/manobra severa (`HARD_LANDING_INSPECTION`, `TAILSTRIKE_INSPECTION`, `WINGTIP_STRIKE_INSPECTION`, `CROSSWIND_LANDING_LOAD_INSPECTION`, `NOSEWHEEL_STEERING_INSPECTION`) e formata títulos amigáveis.
- `src/FlightCareer.Application/Services/FlightService.cs`: pouso forçado fora de pista agora remove todas as missões ativas do banco, gera XP parcial (fator 0.5), avança `pilot.CareerClockUtc` e preserva condições se `terrainDamageApplied: true`.
- `src/FlightCareer.UI/Services/PartFlightSession.cs`: evento `OnLandingBreak`, detecção de fora de pista via sensor `ctx-on-any-runway` no `_lastFrame` e quebra direta da peça sorteada.
- `src/FlightCareer.UI/Services/PersistentFailureService.cs`: suporte ao parâmetro opcional `isGrounding` em `Record`.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`:
  - Assinatura de `_parts.OnLandingBreak` com telemetria e overlay de alerta sem criar defeito duplicado.
  - Assinatura de `_parts.OnInspection` registrando pane persistente via `PersistentFailureService` e acumulando títulos para exibição no relatório de fim de voo (`outcome.Message`).
  - Atualização do cálculo de `_worstTouchdownG` e telemetria com pico rápido de G (`RecordPeakG`).
  - Dano de terreno despreparado aplicado nas peças/componentes antes do salvamento em pouso fora de pista.
  - Registro de `"POUSO_FORCADO"` no blackbox posicionado após o cálculo de fora de pista.
- `src/FlightCareer.UI/Services/FlightTelemetryLogger.cs`: método `RecordPeakG(double g)`.
- `src/FlightCareer.UI/ViewModels/FlightLogEntry.cs`: formatação de cor `#FFA726`, fundo `#2B1E0D` e rótulo `"⚠ POUSO FORÇADO"`.
- Testes:
  - `LandingBreakModelTests.cs`: 12 testes validando curva de razão de descida, fatores multiplicadores, teto e sorteio sequencial.
  - `InspectionDefectPolicyTests.cs`: 7 testes cobrindo interdição seletiva, geração de títulos e catálogo.
  - `WearRuleCatalogTests.cs`: teste validando parsing de `"isGrounding": true` em `wear_rules.json`.
  - `FlightServiceOffFieldLandingTests.cs`: 3 testes validando cancelamento de missões, relógio, XP e dano condicional.

## Critérios de aceite
- [x] C1 — Chance de quebra de trem/pneu no toque com razão alta, inclinação e fora de pista: verificado com testes unitários `LandingBreakModelTests`.
- [x] D1 — Pouso fora de pista causa dano direto nas peças de trem, pneus e célula antes de gravar no banco: verificado em `FlightMonitor.FinalizeFlightAsync` e `FlightServiceOffFieldLandingTests`.
- [x] B4 + D10 — Missões canceladas, XP parcial (50%), relógio de carreira avançado: verificado com `FlightServiceOffFieldLandingTests`.
- [x] D6 — Campo `isGrounding` do JSON de regras reconhecido no carregamento: verificado com `WearRuleCatalogTests.WingtipStrikeInspection_RecognizesIsGroundingFromJson`.
- [x] D5 — Inspeções registradas como pane no Hangar (custo 400), interditando apenas as 5 de toque severo, e listadas no resumo final do voo: verificado com `InspectionDefectPolicyTests` e integração no `FlightMonitor`.
- [x] D11 — Blackbox com status "POUSO_FORCADO", visual dedicado e pico real de G do contato (`RecordPeakG`): verificado na compilação e testes.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos.
- `dotnet test src/FlightCareer.Tests`: 1.245 aprovados, 0 falhas (23 novos testes).

## Não testado
- Comportamento interativo dentro do MSFS 2024 / SimConnect (leitura real de `ctx-on-any-runway`, abertura visual dos alertas de quebra no overlay e persistência física ao reiniciar o voo no simulador).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. O fluxo seguiu exatamente as decisões da spec POUSO-DANO.
