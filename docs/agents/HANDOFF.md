# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-28 (32ª sessão), Gemini. Branch `fix/hud-pouso-e-destino` (sobre `feat/component-health-system`).

## Estado
- **HUD pouso e destino corrigidos (nesta sessão):** `"Pouso final em {ICAO}"` marca `✓` verde ao pousar no destino com requisitos cumpridos (`ObjectiveCardBuilder`, `FlightMonitor`, `ObjectiveCardService`), antes do corte de motor/freio; mini-mapa resolve origem/destino via `HudRouteResolver` + `DebugOverlayViewModel.RefreshRouteFromCurrentState`, corrigindo destino preso de voo anterior e exibição com `AcceptedMissions` antes de monitorar. Build UI ok em `%TEMP%` (app aberto PID 10972), 1.347/1.347 testes. **Não testado no MSFS.**
- **Análise de logs REC (2026-11-28):**
  - **C172SP G1000 (estol + crash):** `af-stall-warning` (84 frames), touchdown 11.172 fpm, `CRASH_MSFS` e perda total (80 peças e 9 componentes a 0%, contrato devolvido) funcionaram; `dmg-crash-flag` seguiu 0 (esperado no MSFS 2024).
  - **Pontos de atenção nos sensores/regras:** (1) `control_surface_abrupt_above_va` (`wear_rules.json:2212`) disparou 18× em cruzeiro reto com AP acima de `Va` (regra `Continuous` lê só IAS, sem checar deflexão brusca de `elevator/aileron/rudder`, e usa `lossPctPerHour` em vez de `lossPerHourPct`, gerando `dano 0.00%`); (2) `gear-contact-point-on-ground/compression-0..2` ficam sempre `0` no C172SP G1000 — em `PartEventDeriver.cs:1284` isso faz `onGround` virar `false` o voo inteiro; (3) C152: `oil-temperature:1` ficou entre 17–29 °C (no C172 chegou a 95 °C) e Landing+Taxi lights levam alternador a 62,2 A (limite 60 A).
- **OFICINA-TEMPO-JOGO / MAPA-COORD / TELEMETRIA-RECARGA:** integrados na base; **não conferidos com o app aberto/MSFS.**

## Próximo passo
- **Próxima tarefa (sessão nova, Opus):** tarefa 9, EXCEDENCIAS (D7–D9) — incluir revisão de `control_surface_abrupt_above_va` (deflexão + `lossPerHourPct`) e `PartEventDeriver` (`gear-contact-point-*` zerados no C172). Depois tarefa 10, HANGAR-REDESENHO (E5; `HangarViewModel` nunca em paralelo).
- **No app/MSFS:** fechar app aberto e recompilar `src/FlightCareer.slnx`; conferir pouso final no cartão de objetivo, ponto verde de destino no HUD (`SBSJ → SNZA`), Hangar/oficina (horas), briefing (COORDENADAS) e reconexão `SimLinkWatchdog` (≤ 35 s).
- PC do escritório: `git pull` e `tools/agents/install-global.sh`.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo; toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0`. Mercado: textos fixos de pneus e aviônicos podem contradizer o estado.
- RBAC 61.81 conferido só pela orientação da ANAC (150 NM, dois pousos completos), não pelo texto vigente.

## Decisões abertas do usuário
- Altitude da área genérica (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez. App aberto trava o build: use `-o %TEMP%\...` ou `-p:OutDir=<scratchpad>`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC).
