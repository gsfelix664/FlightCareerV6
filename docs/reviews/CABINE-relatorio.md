# CABINE — Conversão passageiro ↔ carga com efeito real

**Branch:** feat/cabine · **Commit:** 2bf2dd2 · **Data:** 2026-11-19

## Plano (antes de codar)
1. **Feature `convertibleCabin` no vocabulário fechado**:
   - Adicionar `AircraftFeatures.ConvertibleCabin = "convertibleCabin"` em `AircraftFeatures.cs` e no conjunto `AircraftFeatures.All`.
   - Adicionar `convertibleCabin` aos 11 perfis JSON em `src/FlightCareer.Infrastructure/Data/aircraft/`: `c208b`, `pc-12-ngx`, `beechcraft-king-air`, `beechcraft-king-air-c90gtx`, `beechcraft-king-air-250`, `dhc-6-300-twin`, `atr-42-600`, `atr-72-600`, `cessna-408-skycourier`, `cessna-404-titan`, `cessna-207t-stationair`.
   - Adicionar `"convertibleCabin"` no enum de features do `_schema.json`.
2. **Novo `Domain/Economy/CabinConversion.cs` (lógica pura)**:
   - Custo e parada por porte (`AircraftSizeClass` / MTOW via `AircraftSizeClassifier`): Light ($800 / 4 h), Medium ($3.000 / 8 h), Heavy ($12.000 / 24 h).
   - Cálculo de capacidade efetiva (`CabinCapacity`):
     - Passageiro: pax até `PaxMax`; carga limitada a 25% de `CargaMax` (bagagem).
     - Carga: pax = 0; carga até `CargaMax + (PaxMax * 170 lb)` limitado a `CargaMax * 1,3`.
   - Validador de compatibilidade de missão `IsMissionCompatible(...)` e checagem de feature `HasConvertibleCabin(...)`.
3. **Filtro de missões e compatibilidade**:
   - `CareerSimulator.cs`: utilizar `CabinConversion.IsMissionCompatible` para seleção dos bots.
   - `MissionDisplayItem.cs`: calcular `IsOverweight` e compatibilidade com a cabine da aeronave ativa.
   - `MainViewModel.cs`: bloquear aceitação (`AcceptSelectedMissionsAsync`) e início do voo (`StartFlight`) em desacordo com a cabine.
4. **Hangar (UI / ViewModels)**:
   - `PilotAircraftWrapper.cs`: expor `HasConvertibleCabin`, `CostToggleConfiguration`, `DowntimeToggleConfiguration`, `TextToggleConfiguration` ("Converter para Carga · $3,000 · 8 h"), `ConfigurationDisplayText` ("Carga: até 3.600 lb").
   - `HangarViewModel.cs`: validar `HasConvertibleCabin` no comando `ToggleConfigurationCommand`, passando `downtimeHours` para `RunPaidServiceAsync` (que avança `CareerClock`).
   - `HangarView.xaml`: ocultar card de cabine com `Visibility="{Binding HasConvertibleCabin, Converter={StaticResource BooleanToVisibilityConverter}}"`.
5. **Testes unitários e verificação**:
   - Criar `FlightCareer.Tests/Economy/CabinConversionTests.cs` cobrindo: `Cabin_SemFeature_NaoConverte`, `Cabin_CustoEParadaPorPorte`, `Cabin_CapacidadeCarga`, `Cabin_CapacidadePassageiro`, `Missions_ConfigCarga_SemPassageiros`, `Missions_ConfigPassageiro_CargaLimitada`.
   - `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests`.

## O que foi feito
- `src/FlightCareer.Domain/Parts/AircraftFeatures.cs`: adicionada constante `ConvertibleCabin = "convertibleCabin"` no vocabulário fechado `All`.
- `src/FlightCareer.Infrastructure/Data/aircraft/_schema.json`: adicionada `"convertibleCabin"` e features homologadas restantes ao enum do schema de validação.
- `src/FlightCareer.Infrastructure/Data/aircraft/*.json`: adicionada feature `"convertibleCabin"` aos 11 perfis elegíveis (`c208b`, `pc-12-ngx`, `beechcraft-king-air`, `beechcraft-king-air-c90gtx`, `beechcraft-king-air-250`, `dhc-6-300-twin`, `atr-42-600`, `atr-72-600`, `cessna-408-skycourier`, `cessna-404-titan` e `cessna-207t-stationair`), com bump de `dataVersion`.
- `src/FlightCareer.Domain/Economy/CabinConversion.cs`: nova classe de domínio com lógica pura:
  - Custo e parada por porte (Leve: $800 / 4 h; Médio: $3.000 / 8 h; Pesado: $12.000 / 24 h) via `AircraftSizeClassifier`;
  - Capacidade efetiva de PAX e carga (`GetEffectiveCapacity`);
  - Verificação de elegibilidade (`HasConvertibleCabin`);
  - Validação centralizada de compatibilidade de missão (`IsMissionCompatible`).
- `src/FlightCareer.Application/Simulation/CareerSimulator.cs`: filtro de missões compatíveis para os bots atualizado para consultar `CabinConversion.IsMissionCompatible`, impedindo missões de passageiros em cargueiros e carga excessiva em aviões de passageiros.
- `src/FlightCareer.UI/ViewModels/MissionDisplayItem.cs`: cálculo de `IsOverweight` atualizado para verificar compatibilidade contra a cabine da aeronave ativa, permitindo ao filtro "Carga Compatível" ocultar contratos incompatíveis.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`: implementadas travas com avisos modais em `AcceptSelectedMissionsAsync` e `StartFlight`, impedindo aceitar ou decolar com passageiros em configuração Carga ou com carga além do limite de bagagem em Passageiro.
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`: propriedades `HasConvertibleCabin`, `EffectiveCapacity`, `ConfigurationDisplayText`, `CostToggleConfiguration`, `DowntimeToggleConfiguration` e `TextToggleConfiguration` formatadas conforme especificação.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`: `ToggleConfigurationCommand` valida `HasConvertibleCabin` e repassa `downtimeHours` para `RunPaidServiceAsync` avançar o relógio de carreira (`CareerClock`).
- `src/FlightCareer.UI/Views/HangarView.xaml`: card de cabine e chip de cabine do cabeçalho vinculados a `HasConvertibleCabin` (ocultados em aeronaves não conversíveis).
- `src/FlightCareer.Tests/Economy/CabinConversionTests.cs`: suíte com 6 novos testes unitários.
- `AGENTS.md` e `docs/CHANGELOG.md`: documentação do sistema atualizada.

## Onde o filtro de compatibilidade foi aplicado
O filtro foi centralizado em `FlightCareer.Domain.Economy.CabinConversion.IsMissionCompatible` e documentado conforme contrato:
1. `FlightCareer.Application.Simulation.CareerSimulator.cs` (linhas 322-330): filtro operacional dos bots onde já se decidia alcance, peso e pista;
2. `FlightCareer.UI.ViewModels.MissionDisplayItem.cs` (linhas 346-355): computação de `IsOverweight`, consumido pelo filtro "Carga Compatível" (`FilterOnlyCompatiblePayload`) do quadro de contratos da UI;
3. `FlightCareer.UI.ViewModels.MainViewModel.cs`:
   - `AcceptSelectedMissionsAsync` (linhas 764-795): bloqueia a aceitação de contratos com cabine incompatível;
   - `StartFlight` (linhas 892-907): bloqueia o início do monitoramento/voo caso o manifesto ativo viole a configuração da cabine.

## Critérios de aceite
- [x] Feature nova `convertibleCabin` no vocabulário fechado de `AircraftFeatures.cs` e nos 11 perfis JSON.
- [x] Sem a feature: card e chip de cabine somem do Hangar (`Visibility="{Binding HasConvertibleCabin...}"`).
- [x] `Domain/Economy/CabinConversion.cs` (lógica pura): custo e parada por porte ($800/4h, $3.000/8h, $12.000/24h) e capacidade efetiva calculada rigorosamente conforme spec.
- [x] Missões: na configuração Carga não recebe/aceita passageiros; na Passageiro, carga acima do limite de bagagem fica fora.
- [x] Aeronaves não conversíveis mantêm regra original de missão (carga pura até MaxPayloadLbs e passageiros até MaxPax sem restrição de bagagem de 25%).
- [x] Coluna `HasConvertibleCabin` adicionada a `AircraftTypes` com migração DDL e populada via `AircraftLimitSeeder` e `DataSeeder`.
- [x] Hangar: botão com custo e parada ("Converter para Carga · $3,000 · 8 h"), avanço do relógio de carreira (`CareerClock`), e texto com capacidade efetiva ("Carga: até 4.797 lb").
- [x] Build 0 erros; `dotnet test` 100% aprovado (647/647 testes).
- [x] CHANGELOG atualizado com `## 2026-11-19 - Conversão de cabine (Gemini)`.
- [x] `AGENTS.md` › 7 atualizado.

## Correções da revisão (Claude, 2026-11-19)
1. **Regressão em aeronaves sem cabine conversível corrigida**:
   - `AircraftType.HasConvertibleCabin`: adicionada propriedade na entidade e coluna no banco (`ALTER TABLE AircraftTypes ADD COLUMN HasConvertibleCabin INTEGER NOT NULL DEFAULT 0;`).
   - Sincronização automática em `DataSeeder.SeedAircraftsAsync` (ao carregar do CSV) e em `AircraftLimitSeeder.SeedInternalAsync` (atualizando registros existentes na inicialização a partir dos perfis de limite).
   - `CabinConversion.GetEffectiveCapacity` e `CabinConversion.IsMissionCompatible`: adicionado parâmetro `hasConvertibleCabin` (padrão `true`). Quando `false`, aplica-se a regra original direta (`pax <= MaxPax` e `payload <= MaxPayloadLbs`), permitindo que C152, 737, etc. aceitem carga pura e bagagens até o payload total.
   - `CareerSimulator.cs` e `MissionDisplayItem.cs`: atualizados para repassar `aType.HasConvertibleCabin` diretamente da entidade carregada, sem consultas extras.
   - `MainViewModel.cs`: alterações cirúrgicas mantendo as mensagens originais intactas, aplicando as travas de incompatibilidade de cabine apenas quando `type.HasConvertibleCabin == true`.
   - Teste unitário adicionado: `Missions_AviaoNaoConversivel_RegraAntiga` validando C152 aceitando carga pura de 472 lb e passageiro com bagagem até o limite.
   - Teste de integração adicionado: `Seeder_UpdatesAircraftType_HasConvertibleCabin` em `SeederTests.cs` validando atualização e idempotência.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos (vulnerabilidades conhecidas em pacotes legados de SQLite/SqlClient).
- `dotnet test src/FlightCareer.Tests`: 647 aprovados, 0 falhas (novos: `Missions_AviaoNaoConversivel_RegraAntiga`, `Seeder_UpdatesAircraftType_HasConvertibleCabin`).

## Não testado
- Não testado na tela do app nem no MSFS (modo não interativo headless).

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. Correções da revisão 100% atendidas.
