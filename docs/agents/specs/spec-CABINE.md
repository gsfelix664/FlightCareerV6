# Spec CABINE — Conversão passageiro ↔ carga com efeito real

**Objetivo (1 frase):** a conversão de cabine passa a existir só nos aviões que permitem, custa e para o avião conforme o porte, e muda de fato as missões aceitas e a capacidade.

Decisão do usuário (2026-11-19): melhorar a mecânica (não remover).

**Limite de tempo:** 1h30.

## Fatos levantados
- Hoje: `HangarViewModel.ToggleConfiguration` troca `PilotAircraft.Configuration` entre "Passageiro" e "Carga (Sem Assentos)" por `MaintenancePricing.CostToggleConfiguration` ($1.200), sem efeito conhecido (confira onde `Configuration` é lido: `MissionRules`, `MissionGenerator`, `BriefingViewModel`, mercado).
- Capacidades: `Infrastructure/CSV/aircrafts.csv` (`PaxMax`, `CargaMax(lbs)`). Porte: `Domain/Economy/AircraftSizeClassifier` (leve/médio/pesado, 12.500 / 60.000 lb).
- Features por aeronave: `Data/aircraft/<modelo>.json` › `features` (vocabulário fechado em `Domain/Parts/AircraftFeatures.cs`).

- Relógio de carreira já existe (`Domain/Services/CareerClock`, parâmetro `downtimeHours` do `RunPaidServiceAsync` no Hangar). Preços de serviço ficam em `Domain/Components/MaintenancePricing` (`CostToggleConfiguration` hoje 1.200).
- Rode `dotnet build src/FlightCareer.slnx` (o `dotnet test` não compila a UI).

## Contrato
- Feature nova `convertibleCabin` (no vocabulário) nos perfis: `c208b`, `pc-12-ngx`, `beechcraft-king-air`, `beechcraft-king-air-c90gtx`, `beechcraft-king-air-250`, `dhc-6-300-twin`, `atr-42-600`, `atr-72-600`, `cessna-408-skycourier`, `cessna-404-titan`, `cessna-207t-stationair`. Sem a feature: o card de cabine some do Hangar.
- **Novo** `Domain/Economy/CabinConversion.cs` (lógica pura): custo e parada por porte — leve $800 / 4 h, médio $3.000 / 8 h, pesado $12.000 / 24 h (valores de jogo, documentados como estimativa); capacidade efetiva:
  - Passageiro: pax até `PaxMax`; carga limitada a 25 % de `CargaMax` (bagagem).
  - Carga: pax = 0; carga até `CargaMax` + `PaxMax × 77 kg` (170 lb) de assentos removidos, sem passar do `CargaMax × 1,3`.
- Missões: na configuração Carga o avião não recebe/aceita missões com passageiros; na Passageiro, missões de carga acima do limite de bagagem ficam fora (use o mesmo filtro de compatibilidade que já decide alcance/carga/pista; documente onde).
- Hangar: botão "Converter para Carga · $3,000 · 8 h" (mostra custo e parada); avião fica indisponível durante a parada (avança o relógio de carreira, se `CareerClock` já estiver na base); texto da configuração atual com a capacidade efetiva ("Carga: até 3.600 lb").

## Fora do escopo
- Preços de peças, reparo, pagamento das missões (o valor da missão já depende de pax/carga). Arquivos de voo quentes; `BriefingViewModel` só se o filtro de missões passar por ele (mudança mínima).

## Testes
- `Cabin_SemFeature_NaoConverte`; `Cabin_CustoEParadaPorPorte`; `Cabin_CapacidadeCarga` / `Cabin_CapacidadePassageiro`; `Missions_ConfigCarga_SemPassageiros`; `Missions_ConfigPassageiro_CargaLimitada`.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %.
- [ ] CHANGELOG (`## 2026-11-19 - Conversão de cabine (Gemini)`); `AGENTS.md` › 7.
- [ ] Commit(s); relatório `docs/reviews/CABINE-relatorio.md`. "Não testado na tela".
