# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (17ª sessão, casa), Claude. Base `feat/component-health-system` (merge de `fix/achados-voo-sdco` aprovado pelo usuário). Build 0 erros; `dotnet test` 1.195/1.195.

## Estado
- Voo de instrução em SDCO analisado; 5 bugs corrigidos (CHANGELOG, VOO-SDCO-1 a 5): dados de lugares no DI, toque e arremetida, folga de altitude, mini-mapa do HUD, formato pt-BR.
- Não testado no MSFS (item em `TESTES-PENDENTES.md`). O quadro já gravado mantém as áreas genéricas até ser reposto.
- Banco anterior (SBJS) em `db-backup/flightcareer.db*.sbjs-20261125-232827` na saída do UI.

## Próximo passo
- PC de casa: próximo voo confere as correções (`docs/agents/TESTES-PENDENTES.md`).
- Spec (Opus) do cartão de objetivo, posições salvas e cores do HUD: `docs/plans/achados-voo-sdco.md`.
- Outras pendências de voo: `docs/agents/TESTES-PENDENTES.md`.
- PC do escritório, primeiro: `git pull` e `tools/agents/install-global.sh`. Depois, uma por sessão:
  1. **AIRSIM-2** (spec Opus): `UI/Services/SimAirportImporter` (padrão `ContextSnapshotReader`, IDs ≥ 3000), versão do sim no `OnRecvOpen`, lista + detalhes em lotes, `SimAirportSyncStore`, registro no `App.xaml.cs`, luzes de aproximação no `SimRunwayData` (plano › 7). Só fecha com o MSFS.
  2. HUD em modo tablet: spec (Opus), quando o usuário quiser.
  - Os 8 tipos restantes da E2C precisam de SimVar a conferir no MSFS ou de dados (`docs/plans/catalogo-missoes.md`).

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`, `ProcessOilLeak`/`TransmitOilLeak`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC (bateu no voo de 25/11 em C152).
- A investigar (plano acima): RPM do C152, reinício do contador de combustível após pausa, detecção de pausa.
- Casa: conferir em sessão nova com `/context` se as skills desativadas sumiram; no app, desligar conector Claude Docs, sugestões de prompt, navegador e Claude in Chrome se não usar (usuário).
- Decisões abertas do usuário: altitude da área genérica e variedade de destinos na fase 1 (plano acima).

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário: não analisar sem pedido.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git; `aeroportos-tiers.md`).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
