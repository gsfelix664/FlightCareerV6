# XP-NOVO — XP por esforço × habilidade + bônus fixos, e nível do piloto

**Branch:** feat/xp-novo · **Commit:** a970820 · **Data:** 2026-11-22

## Plano (antes de codar)
- Criar modelo de domínio puro em `Domain/Xp/` (`XpFlightInputs`, `PilotLevel`, `XpRules`, `XpCalculator`), desacoplado e 100% testável.
- Desacoplar todos os valores, taxas e limites de calibração em `Infrastructure/Data/xp_rules.json` com fallback embutido igual.
- Adicionar 6 novas SimVars de realismo e ambiente no final de `SimTelemetry` e `AddToDef` no `SimConnectService`.
- Atualizar `Pilot.Level` e `PilotCategory.GetRequiredXP` com os valores calibrados para a escala dos níveis.
- Conectar o `FlightMonitor` para coletar fatos operacionais e ambientais e repassar ao `ProcessFlightFinishAsync`.
- Atualizar `CareerProgressionSimulator` para bots calcularem XP via `XpCalculator` com fatos estimados.
- Criar suíte completa de testes (`PilotLevelTests`, `XpCalculatorTests`, `FlightFinishXpPersistenceTests`) e validar build, testes e fuzz.

## O que foi feito
- `src/FlightCareer.Domain/Xp/XpFlightInputs.cs`: records imutáveis de entrada de fatos operacionais (`XpFlightInputs`), fatos de pista (`RunwayFacts`), fatos ambientais e aproximação (`EnvFacts`) e detalhamento (`XpBreakdown`).
- `src/FlightCareer.Domain/Xp/PilotLevel.cs`: progressão contínua sem nível máximo baseada na curva $XP = 300 \times (L - 1)^2$ (`ForXp(long)` e `XpForLevel(int)`).
- `src/FlightCareer.Infrastructure/Data/xp_rules.json`: arquivo JSON contendo todas as taxas base, multiplicadores de tipo de missão, limites e bônus de realismo, faixas de pouso e penalidades de qualidade, e bônus aditivos fixos.
- `src/FlightCareer.Domain/Xp/XpRules.cs`: leitor de regras e DTO com fallback seguro embutido caso o JSON não seja encontrado.
- `src/FlightCareer.Domain/Xp/XpCalculator.cs`: cálculo de XP puro `Total = round(Base × M_Tipo × M_Realismo × M_Qualidade + Bônus)` com geração de detalhamento em linhas (`Lines`).
- `src/FlightCareer.Domain/Entities/Pilot.cs`: exposta propriedade `Level => PilotLevel.ForXp(ExperiencePoints)` sem alteração de esquema/banco.
- `src/FlightCareer.Domain/Entities/PilotCategory.cs`: novos requisitos de XP regulatório para categorias 1 a 4 (10.000 / 45.000 / 175.000 / 600.000 XP).
- `src/FlightCareer.Domain/Entities/SimTelemetry.cs`: adicionados 6 novos campos `double` no final do struct (`RealismCrashDetection`, `AutoCoordination`, `Realism`, `AmbientVisibility`, `AmbientInCloud`, `DensityAltitude`) e propriedades auxiliares (`IsCrashDetectionOn`, `IsAutoCoordinationOn`, `IsInCloud`).
- `src/FlightCareer.UI/Services/SimConnectService.cs`: adicionadas chamadas `AddToDef` para as 6 novas variáveis respeitando tipos e ordem exata.
- `src/FlightCareer.Application/DTOs/FlightOutcome.cs`: adicionada propriedade `XpBreakdownLines` para repassar o detalhamento do cálculo ao resumo do voo.
- `src/FlightCareer.Application/Services/FlightService.cs`: `ProcessFlightFinishAsync` recebe `XpFlightInputs? xpInputs = null`, computa o XP novo (com fallback seguro caso ausente), respeita zeramento em voos invalidados e adiciona o detalhamento na mensagem do voo.
- `src/FlightCareer.UI/Services/FlightMonitor.cs`: tracking contínuo de minutos no ar (`_airborneSeconds`), uso de aceleração (`_usedTimeAcceleration`), pior toque de pernas/TGL (`_worstTouchdownFpm`, `_worstTouchdownG`, `_flightTouchdowns`), contagem de transições de overspeed Vne (`_overspeedInfractions`), janela deslizante de 3 min de vento para rajadas (`_landingGustSpreadKt`), condições de visibilidade/nuvem no pouso, resolução de pistas de origem e destino e repasse consolidado ao finalizar o voo.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`: interface principal exibe a patente e o nível no formato "PATENTE · NÍVEL N".
- `src/FlightCareer.Application/Simulation/CareerProgressionSimulator.cs`: simulação de carreira dos bots adaptada para computar XP via `XpCalculator.Compute` com estimativas de pista, distância e pontualidade.
- `src/FlightCareer.Tests/Xp/PilotLevelTests.cs`: testes unitários dos marcos de nível, progressão ilimitada (> 50) e invariante matemática.
- `src/FlightCareer.Tests/Xp/XpCalculatorTests.cs`: testes unitários cobrindo o exemplo do plano (185 XP), pior toque em TGL, bônus bush em altitude com pista de terra, pouso fora de pista com zeramento de bônus e penalidade, pisos/tetos dos multiplicadores e faixas de pontualidade.
- `src/FlightCareer.Tests/Xp/FlightFinishXpPersistenceTests.cs`: teste de integração em SQLite em memória conferindo persistência de XP no logbook, avanço de nível do piloto e zeramento em voo invalidado.
- Atualizados `AGENTS.md` (seção 7) e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] Build 0 erros; `dotnet test` 100 % (809/809 aprovados); fuzz `--aircraft all --runs 20` sem regressão (920 voos aprovados, 0 falhas).
- [x] Campos novos só no fim do `SimTelemetry`/`AddToDef`, com teste de contagem (`TelemetryDefinitionTests` 5/5 aprovados).
- [x] `xp_rules.json` com os multiplicadores e bônus; nenhum número de calibração espalhado no código.
- [x] CHANGELOG e AGENTS.md › 7 atualizados.
- [x] Um commit: `feat(xp): xp por esforco e habilidade com bonus fixos e nivel do piloto (XP-NOVO)`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 17 avisos conhecidos (vulnerabilidades NuGet upstream do SQLitePCLRaw e avisos de nulabilidade legados).
- `dotnet test src/FlightCareer.Tests`: 809 aprovados, 0 falhas (38 novos testes adicionados em `PilotLevelTests`, `XpCalculatorTests` e `FlightFinishXpPersistenceTests`).
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`: 920 voos executados em todas as 46 aeronaves do catálogo, 920 aprovados (100,0%), 0 falhas.

## Não testado
- Não testado no MSFS com voo em tempo real (depende de simulador aberto e voo manual).
- Bônus noturno (+30): desligado por padrão conforme instrução da spec, pois `TIME OF DAY` é variável de ambiente de gauge (`E:TIME OF DAY`), não confirmada como SimVar direta acessível via SimConnect sem rejeição de datum.
- Infrações de estol e luzes/velocidade (luzes esquecidas ou > 250 KIAS abaixo de 10.000 ft): não são monitoradas pelo `FlightMonitor` atualmente e entram como 0. As infrações ativas medidas pelo monitor são: Vne/overspeed (`OverspeedWarning`), G excessivo com passageiros e pouso fora de pista.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A implementação seguiu rigorosamente a especificação do revisor e o plano `xp-proficiencia.md` § 6.
