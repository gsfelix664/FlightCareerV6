# HANGAR-ICONES — Revisão (Claude, 2026-11-17)

**Veredito: APROVADO**

Branch `feat/hangar-icones` (`7f6505e`, `dd610ba`).

## Conferido
- Build 0 erros; `dotnet test` 607/607 (12 em `PartInspectionFlagsTests`, 1 de banco antigo em `ComponentPersistenceTests`); fuzz `--runs 5` 230/230 (Gemini).
- `AppDbContext.cs` (arquivo quente) lido no diff: só a coluna `FlaggedAirframeHours` no `CREATE TABLE` e o `ALTER TABLE` idempotente.
- `PartInspectionFlags`: vermelho = quebrada; amarelo = ≥ 90 % de horas/ciclos/meses ou saúde < 40 %; escopo por inspeção (50 h motor+óleo, 100 h tudo, 500 h ignição, walkaround visíveis); reparo/overhaul limpam os kinds reparados; visível só enquanto `FlaggedAirframeHours == CurrentAirframeHours` (some no próximo voo).
- Hangar: contexto curto por operação, `AsNoTracking` na leitura, erro registrado com `Debug.WriteLine`. Chips só aparecem com peças marcadas; nenhuma lista completa de peças.

## Observações
- **Não testado visualmente** (sem rodar a UI): layout dos chips e cores dependem do teste do usuário.
- O calendário usa a data real do computador (não há relógio de jogo); conta com o app fechado.
- A CALIBRA-F4 vai renomear e comprimir as inspeções (12/25/125 h); os escopos daqui seguem valendo.
