# Spec CALENDARIO-JOGO — Relógio de carreira para tudo que é medido em dias/meses

**Objetivo (1 frase):** criar um calendário de jogo por piloto, que só avança com o tempo voado e o tempo de oficina, e usá-lo no lugar da data real do Windows para a vida em meses das peças e o seguro de 30 dias.

Decisão do usuário (2026-11-19): "usar tempo simulado acumulado de carreira (dias avançam conforme a duração dos voos e os tempos de parada no hangar), nunca a data real do relógio do Windows (não punir quem fica semanas sem abrir o simulador)".

**Limite de tempo:** 1h30. Sem simulações longas.

## Fatos levantados
- Vida por calendário: `PartDefinition.CalendarLifeMonths`; avaliada hoje só em `Domain/Parts/PartInspectionFlags.Evaluate(part, def, nowUtc)` com `part.LastReplacedAtUtc` (data real). Chamadores passam `DateTime.UtcNow` (`HangarViewModel`, `PilotAircraftWrapper.SetFlaggedParts`).
- `AircraftPart.LastReplacedAtUtc` é gravado com `DateTime.UtcNow` em `AircraftPartProvisioner`, `PartMigrationService` e reparos.
- Seguro: `EconomyServices`/`InsuranceService` + `HangarViewModel` (contratação de 30 dias; veja onde a validade é guardada e comparada).
- Fim de voo: `Application/Services/FlightService.cs` (desconta horas, cobra custos) — ponto para avançar o relógio.
- Banco: coluna nova = propriedade + `ALTER TABLE ... ADD COLUMN ... DEFAULT` em `AppDbContext.EnsureSchemaUpdatedAsync` (arquivo quente: só acrescente a linha). Teste de banco antigo em `ComponentPersistenceTests`.

## Contrato
- `Pilot` (ou a entidade de carreira que já existir — confira): `DateTime CareerClockUtc` (coluna nova; default na migração = data real no momento da migração, uma vez; carreira nova = data de criação).
- **Novo** `Domain/Services/CareerClock.cs` (lógica pura): `DateTime Advance(DateTime clock, double hours)`; constantes documentadas. Regras:
  - fim de voo concluído ou cancelado depois de ativo: avança pela duração simulada do voo (horas da célula somadas no voo) + **0,5 h** de solo;
  - serviço de oficina no Hangar que tem tempo parado: avança pelo tempo parado (`downtimeHours` da peça/serviço; inspeções sem parada definida: 12 h → 2 h, 25 h → 8 h, 125 h → 6 h, retífica → 72 h — documente como estimativa);
  - nada avança com o app fechado.
- `PartInspectionFlags.Evaluate` recebe o relógio de carreira (renomeie o parâmetro para `careerNow`); chamadores passam `pilot.CareerClockUtc`. `LastReplacedAtUtc` passa a ser gravado com o relógio de carreira nas trocas/provisionamento de peças novas (nome da coluna fica; documente no comentário que é data de carreira).
- Seguro: validade comparada com o relógio de carreira.
- Hangar: mostrar a data de carreira em algum lugar discreto (ex.: cabeçalho "Dia da carreira: dd/MM/yyyy").

## Fora do escopo
- Expiração das missões no quadro (continua em tempo real). `parts_catalog.json`, preços, TBO, inspeções (outra tarefa mexe agora). Arquivos de voo quentes (`FlightMonitor`, `ComponentFlightSession`, `SimConnectService`, `SimFailureActuator`, `SimTelemetry`): se o avanço no fim do voo precisar deles, use o `FlightService`.

## Testes
- `CareerClock_VooAvancaDuracaoMaisSolo`; `CareerClock_ServicoAvancaParada`; `Inspection_CalendarioUsaRelogioDeCarreira` (peça de 3 meses: 100 dias reais sem voar → sem marca; 3 meses de carreira → amarela); `Seguro_ExpiraPeloRelogioDeCarreira`; migração de banco antigo cria a coluna.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 5`.
- [ ] CHANGELOG (`## 2026-11-19 - Calendário de jogo (Gemini)`); `AGENTS.md` › 7.
- [ ] Commit(s) na branch; relatório `docs/reviews/CALENDARIO-JOGO-relatorio.md`. "Não testado no MSFS".
