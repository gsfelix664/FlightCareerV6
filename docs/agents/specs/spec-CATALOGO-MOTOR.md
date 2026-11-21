# Spec CATALOGO-MOTOR — calibração do grupo Motor, remoção de peça antiga e J-3/CAP-4

**Objetivo (1 frase):** aplicar a calibração do usuário ao grupo Motor, remover `electrical.external_lighting`, impedir peças elétricas/aviônicas/flap em aviões `noElectrical`/`noFlaps` e limpar do banco as peças que deixaram de se aplicar.

## Fatos já levantados
- Catálogo: `src/FlightCareer.Infrastructure/Data/parts_catalog.json` (154 peças após LUZES-F2). Validação no loader `Domain/Parts/PartCatalog.cs`; testes em `Tests/Parts/PartCatalogTests.cs` (contagem exata de peças).
- `electrical.external_lighting` (linha ~3197) só existe no JSON e na doc `docs/plans/lista-pecas-hangar.md`; as 6 lâmpadas `lights.*` (grupo Iluminação) a substituem. Confira com grep se alguma regra em `wear_rules.json` ou teste a cita e ajuste.
- `piper-j-3-cub.json` e `cap-4.json` têm features `noElectrical` e `noFlaps`. Hoje recebem: `electrical.battery`, `electrical.alternator`, `electrical.voltage_regulator`, `electrical.wiring_harness`, `electrical.circuit_breakers_relays`, `avionics.com_nav_radios`, `avionics.transponder`, `avionics.gps_receiver`, `avionics.analog_flight_instruments`, `avionics.flight_recorders_elt`, `avionics.audio_panel` e `airframe.flaps`. Confira a lista completa por script (todas as peças dos grupos Elétrico/Aviônicos e toda peça de flap) e acrescente `"none": ["noElectrical"]` / `"none": ["noFlaps"]` ao `appliesTo` (mesclando com `all`/`any` existentes). Atenção: `avionics.analog_flight_instruments` — instrumentos de vácuo/pitot/estática do J-3 existem sem elétrica; se a peça cobre instrumentos não elétricos (leia nome/sensores), **não** exclua e explique no relatório.
- `Infrastructure/Services/AircraftPartProvisioner.cs` só **adiciona** peças que faltam (linhas ~72-135). Não remove as que deixaram de se aplicar ou saíram do catálogo.
- Regras de peças que referenciam partIds em `wear_rules.json` (`bands[].parts[].partId`): uma peça removida/não aplicável não pode quebrar o carregamento (confira `WearRuleCatalog` valida partId contra o catálogo).

## O que fazer
1. Remover `electrical.external_lighting` do catálogo (e de qualquer referência).
2. `appliesTo` com `none` conforme acima.
3. Calibração do grupo **Motor** (decisões do usuário 2026-11-17; `priceScalesWithAircraft`, `blocksFlightWhenBroken`, `visibleOnWalkaround`, `appliesTo`, `perEngine`, `failureEffect`, sensores: **não mudam**). `lifeSource` dessas peças: `"calibração do usuário 2026-11-17"`.

| id | lifeHours | lifeCycles | partPrice | laborHours | downtimeHours |
|---|---|---|---|---|---|
| engine.cylinders | 250 | — | 1600 | 8 | [4, 14] |
| engine.crankshaft | 350 | — | 2200 | 12 | [6, 18] |
| engine.valves | 150 | — | 550 | 5 | [3, 10] |
| engine.carburetor | 180 | — | 500 | 2 | [3, 10] |
| engine.fuel_injectors | 200 | — | 650 | 3 | [3, 10] |
| engine.turbocharger | 180 | — | 1400 | 4 | [4, 12] |
| engine.starter | 200 | 150 | 320 | 1.5 | [2, 8] |
| engine.coolant_system | 200 | — | 680 | 3 | [3, 10] |
| engine.compressor | 500 | — | 11500 | 10 | [6, 18] |
| engine.reduction_gearbox | 400 | — | 6500 | 7 | [4, 14] |
| engine.fan | 600 | 400 | 16000 | 8 | [6, 18] |
| engine.thrust_reverser | — (remover) | 300 | 7800 | 5 | [4, 12] |
| engine.mounts_trusses | 650 | — | 2800 | 6 | [4, 12] |
| engine.cowling_nacelle | 1000 | — | 3200 | 4 | [3, 8] |
| engine.air_intake_filter | 80 | — | 220 | 1 | [1, 2] |
| engine.pneumatic_starter | — | 250 | 5600 | 3 | [2, 5] |
| engine.fadec_eec | 800 | — | 8500 | 3 | [2, 6] |
| engine.apu_core | 450 | — | 12000 | 8 | [4, 12] |

   "—" em lifeCycles = remover o campo/ficar null se existir hoje. Se o loader exigir `lifeHours` ou `lifeCycles` (pelo menos um), o reverser e o pneumatic_starter ficam só com ciclos (já é o caso do pneumatic_starter hoje). Confira como o desgaste natural trata peça sem `lifeHours` (não pode dividir por zero) e diga no relatório.
4. **Limpeza no provisionamento** (`AircraftPartProvisioner`): ao provisionar, remover do banco as `AircraftParts` do avião cujo `PartId` não existe mais no catálogo **ou** que não se aplicam mais às features do avião (ex.: J-3 com bateria). Log com `Debug.WriteLine` de quantas foram removidas. Idempotente. Teste com SQLite em memória (padrão dos testes de persistência existentes): avião `noElectrical` com `electrical.battery` e `electrical.external_lighting` no banco → após provisionar, nenhuma das duas; C152 mantém as suas.
5. Atualizar `docs/plans/lista-pecas-hangar.md`: regenere as tabelas (script no scratchpad do agente, não no repo) refletindo o catálogo novo (154 → 153 peças, grupo Motor recalibrado, sem os achados de J-3/CAP-4).

## Fora do escopo
- Qualquer arquivo de UI, `HangarViewModel`, `AircraftPart.cs`, `AppDbContext.cs` (outra tarefa mexe neles agora). Outros grupos de peças (o usuário manda as calibrações depois). `component_catalog.json`.

## Testes
- `Catalogo_NoElectrical_SemPecasEletricas` (J-3 e CAP-4: nenhuma peça dos grupos Elétrico, Aviônicos, Iluminação exceto as justificadas; nenhuma de flap).
- `Catalogo_NoFlaps_SemFlap`.
- `Catalogo_SemExternalLighting`.
- `Catalogo_Motor_CalibracaoDoUsuario` (confere 3–4 linhas da tabela: cylinders, starter, fan, thrust_reverser).
- `Provisioner_RemovePecasQueNaoSeAplicam` e `Provisioner_RemovePecaForaDoCatalogo` (SQLite em memória).
- Ajustar a contagem exata em `PartCatalogTests` (explique a mudança).

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 20` sem falhas (cole o resumo). Se o fuzz falhar por causa das vidas novas (ex.: peça chegando a 0 em voo longo), **não altere o oráculo nem a calibração**: descreva no relatório.
- [ ] CHANGELOG (`## 2026-11-17 - Calibração do motor, J-3/CAP-4 e remoção da iluminação antiga (Gemini)`); AGENTS.md › 7 se o provisionamento passou a remover peças.
- [ ] Um commit: `feat(parts): calibracao do motor, pecas de J-3/CAP-4 e limpeza no provisionamento (CATALOGO-MOTOR)`.
- [ ] Relatório só com fatos conferidos.
