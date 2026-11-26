# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-26 (18ª sessão, casa), Claude. Base `feat/component-health-system` (merge de `feat/mapa-contratos-unificado`, que traz OBJ-CARD-1 e OBJ-CARD-2 aprovados). Build 0 erros; `dotnet test` 1.212/1.212.

## Estado
- Voo de conferência em SDCO (26/11) analisado: contrato cumprido, 2 pousos com toque e arremetida, área de Tatuí ok. Achados (pneus −4,6 %, RPM alto) em `docs/plans/achados-voo-sdco.md`.
- Mapa unificado: `FlightMapControl.ContractMissions` desenha área, pontos e faixa no quadro, no briefing e no HUD.
- Cartão de objetivo (OBJ-CARD-1): abre no início do voo, 🎯 no HUD e Ctrl+Alt+O. Posições salvas e cores na linha CONTRATO (OBJ-CARD-2).
- Nada disso foi testado no MSFS (itens em `TESTES-PENDENTES.md`).

## Próximo passo
- PC de casa: voo que confira o mapa do quadro, o cartão (foco do MSFS, atalho, conflito do Ctrl+Alt+O), as posições salvas e as cores (`docs/agents/TESTES-PENDENTES.md`).
- Investigar (Opus, plano acima): dano de pneu por pouso/táxi (`tireDamagePer100Fpm` 2,0), `piston_rpm_overspeed` do C152, reinício do contador de combustível após pausa e detecção de pausa.
- PC do escritório, primeiro: `git pull` e `tools/agents/install-global.sh`. Depois, uma por sessão:
  1. **AIRSIM-2** (spec Opus): `UI/Services/SimAirportImporter` (padrão `ContextSnapshotReader`, IDs ≥ 3000), versão do sim no `OnRecvOpen`, lista e detalhes em lotes, `SimAirportSyncStore`, registro no `App.xaml.cs`, luzes de aproximação no `SimRunwayData` (plano › 7). Só fecha com o MSFS.
  2. HUD em modo tablet: spec (Opus), quando o usuário quiser.
  - Os 8 tipos restantes da E2C precisam de SimVar a conferir no MSFS ou de dados (`docs/plans/catalogo-missoes.md`).

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`, `ProcessOilLeak`/`TransmitOilLeak`); desempenho no amarelo (`perda-desempenho-amarelo.md` › 3); toque (`TOQUE-NORMAL`) contra o REC.
- Casa: conferir em sessão nova com `/context` se as skills desativadas sumiram; no app, desligar conector Claude Docs, sugestões de prompt, navegador e Claude in Chrome se não usar (usuário).
- Decisões abertas do usuário: altitude da área genérica e variedade de destinos na fase 1 (plano acima); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário: não analisar sem pedido.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git; `aeroportos-tiers.md`).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC). REC tem `sim_rate` (horas do Hangar = tempo simulado).
