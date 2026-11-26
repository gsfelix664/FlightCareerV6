# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-25 (16ª sessão, casa), Claude. Base `feat/component-health-system`. Build 0 erros; `dotnet test` 1.189/1.189.

## Estado
- Instruções enxutas: detalhes em `docs/sistemas/simconnect.md` e `banco.md`, `docs/agents/COMANDOS.md` e `~/.claude/tools/gemini/DELEGACAO.md`.
- Voo de teste preparado (`docs/agents/ROTEIRO-VOO.md`), **ainda não voado**: o usuário cria piloto novo em SDCO (C152). Banco anterior (SBJS) em `db-backup/flightcareer.db*.sbjs-20261125-232827` na saída do UI.

## Próximo passo
- PC de casa: após o usuário dizer "voo terminado", analisar blackbox, REC e banco (Sonnet).
- Outras pendências de voo: `docs/agents/TESTES-PENDENTES.md`.
- PC do escritório, primeiro: `git pull` e `tools/agents/install-global.sh`. Depois, uma por sessão:
  1. **AIRSIM-2** (spec Opus): `UI/Services/SimAirportImporter` (padrão `ContextSnapshotReader`, IDs ≥ 3000), versão do sim no `OnRecvOpen`, lista + detalhes em lotes, `SimAirportSyncStore`, registro no `App.xaml.cs`, luzes de aproximação no `SimRunwayData` (plano › 7). Só fecha com o MSFS.
  2. HUD em modo tablet: spec (Opus), quando o usuário quiser.
  - Os 8 tipos restantes da E2C precisam de SimVar a conferir no MSFS ou de dados (`docs/plans/catalogo-missoes.md`).

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`, `ProcessOilLeak`/`TransmitOilLeak`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Casa: conferir em sessão nova com `/context` se as skills desativadas sumiram; no app, desligar conector Claude Docs, sugestões de prompt, navegador e Claude in Chrome se não usar (usuário).
- Decisões abertas do usuário: nenhuma.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário: não analisar sem pedido.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git; `aeroportos-tiers.md`).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
