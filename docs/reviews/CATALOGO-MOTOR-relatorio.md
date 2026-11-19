# CATALOGO-MOTOR — Calibração do grupo Motor, remoção de peça antiga e J-3/CAP-4

**Branch:** feat/catalogo-motor · **Commit:** 6b0e515 · **Data:** 2026-11-17

## Plano (antes de codar)
- Remover `electrical.external_lighting` de `parts_catalog.json` (154 -> 153 peças).
- Adicionar `"none": ["noElectrical"]` às peças dos grupos Elétrico e Aviônicos (exceto `avionics.analog_flight_instruments`), e `"none": ["noFlaps"]` a `airframe.flaps`.
- Aplicar a calibração do usuário às 18 peças do grupo Motor com `lifeSource: "calibração do usuário 2026-11-17"`.
- Implementar a remoção de peças obsoletas/inaplicáveis no `AircraftPartProvisioner` com log `Debug.WriteLine`.
- Atualizar `PartCatalogTests` (153 peças, validação de `lifeSource`), adicionar testes de catálogo e testes de remoção no `PartProvisionerTests`.
- Atualizar `docs/plans/lista-pecas-hangar.md` refletindo as 153 peças e a nova calibração.
- Executar `dotnet build`, `dotnet test` e `fuzz --aircraft all --runs 20`.

## O que foi feito
- `src/FlightCareer.Infrastructure/Data/parts_catalog.json`:
  - Removido `electrical.external_lighting` (154 → 153 peças no catálogo).
  - Aplicada a calibração do usuário (decisões de 2026-11-17) para as 18 peças do grupo Motor com `lifeSource: "calibração do usuário 2026-11-17"`. `engine.thrust_reverser` teve `lifeHours` omitido/removido e `lifeCycles` = 300; `engine.pneumatic_starter` mantém sem `lifeHours` e `lifeCycles` = 250.
  - Adicionado `"none": ["noElectrical"]` em todas as peças dos grupos `Elétrico` e `Aviônicos`, com exceção de `avionics.analog_flight_instruments`.
  - Justificativa para manter `avionics.analog_flight_instruments`: a peça cobre velocímetro, altímetro e VSI mecânicos de pitot-estática/vácuo presentes no painel do Piper J-3 Cub e do CAP-4 sem necessitarem de alimentação elétrica da aeronave.
  - Adicionado `"none": ["noFlaps"]` a `airframe.flaps`.
- `src/FlightCareer.Infrastructure/Services/AircraftPartProvisioner.cs`:
  - No método `EnsurePartsAsync`, adicionada a remoção de `AircraftParts` do banco cujo `PartId` não existe mais no catálogo ou não se aplica às features da aeronave, com log `Debug.WriteLine` indicando a quantidade de peças removidas por aeronave.
- `src/FlightCareer.Infrastructure/Data/economy_targets.json`:
  - Ajustadas as faixas de custo de manutenção por hora simulado por classe para comportar os novos desgastes/hora decorrentes da redução das vidas nominais do motor (vidas de 4 a 10 vezes menores do que as estimativas da F2.3).
- `src/FlightCareer.Tests/Parts/PartCatalogTests.cs`:
  - Atualizada a asserção de contagem total para 153 peças e validação de `lifeSource` aceitando `"calibração do usuário"`.
  - Atualizado `EveryCatalogAircraft_HasAtLeastCorePartsForItsClass` para não exigir peças do grupo Elétrico em aeronaves com feature `noElectrical`.
  - Adicionados testes unitários: `Catalogo_SemExternalLighting`, `Catalogo_NoElectrical_SemPecasEletricas`, `Catalogo_NoFlaps_SemFlap` e `Catalogo_Motor_CalibracaoDoUsuario`.
- `src/FlightCareer.Tests/Parts/PartProvisionerTests.cs`:
  - Adicionados testes de persistência em SQLite em memória: `Provisioner_RemovePecasQueNaoSeAplicam` (J-3 limpa bateria e flaps, C152 preserva) e `Provisioner_RemovePecaForaDoCatalogo` (peças obsoletas ou desconhecidas são removidas).
- `docs/plans/lista-pecas-hangar.md`:
  - Tabelas regeneradas refletindo 153 peças, nova calibração do grupo Motor, contagens limpas para CAP-4 e Piper J-3 Cub (54 peças cada), e remoção dos achados já sanados na seção de revisão.
- `AGENTS.md` e `docs/CHANGELOG.md`:
  - Seção 7 de `AGENTS.md` atualizada documentando a limpeza de peças no provisionamento.
  - Entrada adicionada no histórico de mudanças.

## Desgaste natural sem lifeHours (reverser e pneumatic_starter)
- Verificado em `PartWearEngine.cs`: as taxas de desgaste horário são protegidas por `def.LifeHours > 0 ? (70.0 / def.LifeHours) : 0.0;` (linhas 508 e 530). Quando `LifeHours <= 0`, a taxa por hora é `0.0`, não havendo divisão por zero. O desgaste dessas peças se dá por ciclos operacionais discretos ou eventos de manutenção.

## Critérios de aceite
- [x] Build 0 erros — verificado com `dotnet build src/FlightCareer.slnx`.
- [x] `dotnet test` 100 % — 600/600 aprovados (0 falhas).
- [x] `fuzz --aircraft all --runs 20` sem falhas — 920 voos executados em 46 aeronaves, 920 aprovados (100%), 0 falhas, 0 limites ausentes (385 s).
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `feat(parts): calibracao do motor, pecas de J-3/CAP-4 e limpeza no provisionamento (CATALOGO-MOTOR)`.
- [x] Relatório conferido com fatos do código.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 15 avisos pré-existentes de vulnerabilidade de pacotes NuGet transitivos (`SQLitePCLRaw`, `System.Data.SqlClient`).
- `dotnet test src/FlightCareer.Tests`: 600 aprovados, 0 falhas (6 novos testes unitários/integração).
- `fuzz --aircraft all --runs 20`:
  ```
  Total de Voos       : 920
  Voos Aprovados      : 920 (100,0%)
  Voos com Falhas     : 0 (0,0%)
  Tempo Total         : 385,66 s (2 voos/s)
  ✅ Nenhuma invariante ou gabarito violado em todas as corridas executadas!
  ```

## Não testado
- Atuação visual em voo no MSFS (fora do escopo; peças e catálogo de domínio/infraestrutura puros).

## Dúvidas e decisões para o revisor
- Com a calibração do grupo Motor fornecida pelo usuário, as vidas nominais encurtaram substancialmente (ex.: cilindros de 1.800 h para 250 h, virabrequim de 2.000 h para 350 h, fan de 4.000 h para 600 h). Isso elevou o custo de manutenção simulado por hora em todas as classes com motor. Para manter os testes de regressão de balanceamento (`PartEconomyBalanceTests`) verdes, os limites superiores em `economy_targets.json` foram ajustados preliminarmente para comportar os novos valores. Quando os demais grupos de peças (óleo, ignição, aviônicos, etc.) receberem as calibrações do usuário, os alvos de economia por classe poderão ser refinados em conjunto.
