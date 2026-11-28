# Revisão AIRSIM-1 — Base de aeroportos do simulador: lógica e persistência

**Veredito: APROVADO** (com 2 ajustes feitos pelo revisor, commit `aff41f6`). Revisor: Claude, 2026-11-25.

## Conferido
- Escopo: só Domain `Airports/Sim/`, entidades, `AppDbContext` (só acréscimos), `DataSeeder` (só `Source`), `SimAirportSyncStore` e testes. Nada de UI, SimConnect ou gerador.
- Contrato do § 4 da spec: superfícies, impressão digital/resumo, diferença, builder (filtros, preservação de cidade/país/Id, iluminação herdada, ILS, torre, gates, tier), fila e estado. Schema idempotente (`ALTER TABLE` + `CREATE TABLE IF NOT EXISTS`) com teste de banco antigo.
- Build da solução 0 erros; `dotnet test` 1.189/1.189 (24 testes novos). Auditoria do Gemini: 79 ações, 0 para conferir.

## Ajustes do revisor
1. **Aeroporto ausente que volta ao simulador não era reativado** (furo da spec): ao marcar ausente a impressão continuava gravada, então na volta ele caía em "igual" e ficava `IsInSimulator = false` para sempre. Agora `ApplyListAsync` limpa `SimFingerprint` ao marcar ausente; teste `ApplyList_AusenteQueVolta_Reenfileira`.
2. **Desempenho do `ApplyDetailsAsync`**: eram 3–4 consultas por aeroporto com `ToUpper()` (varredura da tabela inteira a cada item; minutos na importação do mundo). Agora fila, aeroportos e pistas são carregados uma vez por lote.

## Para a AIRSIM-2 (não bloqueia)
- Aeroporto novo excluído pelo builder (heliponto, só água, pista < 1.000 ft) não fica gravado, então volta à fila a cada mudança do resumo da lista. Avaliar guardar os excluídos (ex.: tabela de idents ignorados com a impressão) se o custo aparecer na medição.
- `EnqueueRotationAsync` carrega todos os aeroportos do simulador como entidades para pegar os mais antigos: trocar por `Take` no banco se ficar lento.
- `IsLighted` passa à regra do Little Navmap (luzes de aproximação/ILS), ver plano § 7.
