# Revisão MANUT-FAIXAS-A (Claude) — 2026-11-20

Branch `feat/manut-faixas-a`, commit `47a2633`. Spec: `docs/agents/specs/spec-MANUT-FAIXAS-A.md`.

## Conferido
- Faixas (`PartHealthBands`), curva de desgaste acelerado e `partBands` no catálogo: conforme a spec; o fator entra antes do estresse no `PartWearEngine` e nas lâmpadas.
- `PartFailureEngine`: λ por peça com a curva do catálogo, ignora `minorPart` e quebradas, uma pane por componente por voo, `IRandomSource` injetado.
- `ComponentFlightSession`/`FlightMonitor` (quentes, lidos linha a linha): só as ligações pedidas; caminho legado intacto para aeronaves sem peças.
- Colunas novas com `ALTER TABLE` e no `CREATE TABLE`.
- Planner sem os 80 %; `SelectRecommended` = quebradas + vermelhas; `ApplyToPart` limpa o achado da anual.
- TBO não interdita (voo, translado, Hangar e liberação das interditadas por TBO); anual não troca óleo nem zera 12 h.
- Seguro 2,5 %/ano, mínimo $50.

## Veredito: **APROVADO com ajustes do revisor** (feitos pelo Claude na própria branch)
1. **Simulador cobrava as revisões em dobro:** troca de óleo, ignição e retífica cobravam o pacote **e** o preço das peças trocadas (e somavam a parada das peças à do serviço). No Hangar o pacote já inclui as peças. Isso penalizava o cauteloso na ECON-STAB. Agora: só o pacote e a parada do serviço; peças extras reveladas continuam pagas à parte.
2. `InsuranceService.ClaimDenied` tinha `catch {}` silencioso e lia o catálogo do disco dentro da Application: passa a receber o `PartCatalog` opcional para o nome da peça (sem `try/catch`).
3. `ComponentFlightSession.Reset` não limpava `_failedKindsInFlight` (só o `Begin`): limpa também no `Reset`.

## Não testado
- Nada no MSFS: pane por peça em voo, alerta com o nome da peça, quebra persistida.
- O simulador de carreira não modela perda total, então a negação do seguro só é exercida no Hangar (teste de domínio).
- A ECON-STAB completa roda depois do merge (relatório à parte).
