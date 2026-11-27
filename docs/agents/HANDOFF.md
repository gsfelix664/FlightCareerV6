# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-27 (28ª sessão, casa), Claude. Base `feat/component-health-system`. TELEMETRIA-RECARGA feita pelo Gemini e aprovada na 1ª rodada.

## Estado
- **TELEMETRIA-RECARGA feito (B6):** o temporizador de 5 s não para mais depois de conectar; pede `RequestSystemState("Sim")` e desconecta/reconecta se a chamada lançar ou se não chegar mensagem em 30 s (`SimLinkWatchdog` no Domain, com testes). Build ok, 1.323/1.323. Revisão: `docs/reviews/TELEMETRIA-RECARGA-revisao.md`. **Não testado no MSFS.** A documentação online do SDK deu 404 (comportamento do pipe quebrado não conferido).
- NAV-IDA, SEM-PORCENTO, MAPA-ESCALAS, BRIEF-NAV, HANGAR-AVISOS, LIMPA-MAPA e POUSO-DANO feitos e não conferidos com o app aberto.

## Próximo passo
- **No MSFS:** fechar o sim à força com o app aberto → "DESCONECTADO" em ≤ 35 s e volta sozinho; no menu principal e no carregamento do voo, o log `[SimConnect]` não pode mostrar reconexão em laço. Se laço, subir o limite de 30 s. Depois ver se B5 (BRIEF-LINHA) ainda acontece.
- **Sessão Sonnet: A4 (mini mapa do HUD sem textos) e A5 (coordenadas dos pontos no briefing)**, pedidos do usuário 2026-11-27; `correcoes-voo-forcado-sdco.md` › A.
- **Sessão Opus: OFICINA-TEMPO-JOGO** (decisão do usuário 2026-11-27; `manutencao-parada-real.md` › 7).
- Sessão Opus: tarefa 9, EXCEDENCIAS (D7–D9). Depois tarefa 10, HANGAR-REDESENHO (inclui E5). `HangarViewModel` nunca em paralelo.
- No app/MSFS: missões só de ida no quadro de SNZG (nível 1 e 3); textos sem %; escalas nos mapas; pouso duro e fora de pista.
- PC do escritório: `git pull` e `tools/agents/install-global.sh`. Fila anterior: AIRSIM-2, HUD em modo tablet (quando pedido), 8 tipos da E2C.

## Pendências
- Precisam do MSFS: óleo (`GENERAL ENG OIL LEAKED PERCENT`); desempenho no amarelo; toque (`TOQUE-NORMAL`) contra o REC.
- Teste de XP do pouso forçado só confere `XP > 0`.
- Mercado: textos fixos de pneus e aviônicos podem contradizer o estado mostrado.
- RBAC 61.81 conferido só pela orientação da ANAC (150 NM, dois pousos completos), não pelo texto vigente.

## Decisões abertas do usuário
- Altitude da área genérica (`achados-voo-sdco.md`); manter Ctrl+Alt+O.

## Notas
- Repositório público: não commitar dados do simulador sem perguntar. `REGRAS_tiers.txt` (se existir) é do usuário.
- `C:/projetos/ref/` tem `atools` e `littlenavmap` (GPL, só consulta). Base LNM: `references/lnm/` (fora do git).
- PC do escritório: 4 GB, uma tarefa do Gemini por vez.
- App aberto trava o build: use `-p:OutDir=<scratchpad>`; testes sem `OutDir`.
- Logs: blackbox em `src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/` (hora local); REC em `%LocalAppData%\FlightCareer\sensor-logs\` (UTC). Sonda rápida do sim: `dotnet run --project src/FlightCareer.SimVarProbe -- --duration 5 --filter "<regex>"`.
