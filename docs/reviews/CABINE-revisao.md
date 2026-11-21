# CABINE — Revisão (Claude, 2026-11-19)

**Veredito: CORRIGIR**

## Corrigir
1. **Regressão em todos os aviões sem cabine conversível.** `PilotAircraft.Configuration` vale "Passageiro" por padrão em toda aeronave; com a regra nova, C152, 737, A330 etc. passam a ter carga pura limitada a 25 % de `MaxPayloadLbs` e bagagem de missões de passageiro também limitada (ex.: C152 com 530 lb de payload deixaria de aceitar carga de 472 lb, comum hoje). A regra de bagagem/assentos vale **só** para aeronaves com a feature `convertibleCabin`; as demais seguem exatamente a regra antiga (`pax ≤ MaxPax` e `payload ≤ MaxPayloadLbs`).
   - Disponibilize a informação para quem filtra missões: por exemplo `AircraftType.HasConvertibleCabin` (coluna nova + `ALTER TABLE ... DEFAULT 0`, preenchida a partir das features do perfil no `AircraftLimitSeeder`/`DataSeeder` na inicialização) ou outro meio que funcione no `MainViewModel`, `MissionDisplayItem` e `CareerSimulator` sem consulta extra por missão.
   - `CabinConversion.GetEffectiveCapacity`/`IsMissionCompatible` recebem esse booleano; sem conversível = regra antiga.
   - Testes: `Missions_AviaoNaoConversivel_RegraAntiga` (C152: carga 472 lb aceita; pax 1 + bagagem aceita até o payload) e os atuais continuam.
2. `MainViewModel.cs` é arquivo quente: mudanças mínimas, só nos pontos de filtro/aceite; mantenha as mensagens.
3. Rode `dotnet build src/FlightCareer.slnx` antes do relatório.

## Rodada 2 — APROVADO (Claude, 2026-11-19)
- `AircraftType.HasConvertibleCabin` (coluna + `ALTER TABLE`, preenchida pelo perfil no seeder); `CabinConversion` com regra de fábrica quando não conversível; `MainViewModel` (quente) lido: mudanças só nos filtros de aceite e início do voo, mensagens mantidas.
- Build 0 erros; `dotnet test` 647/647.
- Observação: carreiras antigas com um avião não conversível marcado "Carga" passam a ser tratadas pela regra de fábrica (a conversão nesses aviões deixou de existir). Não testado na tela.
