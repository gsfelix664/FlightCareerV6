# Passagem de sessão

> Uma tarefa por sessão (`AGENTS.md` › 4.9). Reescreva ao terminar, **máximo 40 linhas**; histórico fica no `docs/CHANGELOG.md` e no git.

**Última atualização:** 2026-11-27 (27ª sessão, casa), Claude. Base `feat/component-health-system`. NAV-IDA feita pelo Gemini e aprovada na 1ª rodada.

## Estado
- **NAV-IDA feito:** tipo `navegacao_vfr_ida` (só ida; iniciante com 3 destinos fixos por origem, intermediária a partir do nível 3) e perfil `triangle_long` (150–300 NM, nível 3+). Nível do piloto entra no sorteio (`minPilotLevel`). Build ok, 1.314/1.314. Revisão: `docs/reviews/NAV-IDA-revisao.md`. **Não conferido no app nem no MSFS.**
- SEM-PORCENTO, MAPA-ESCALAS, BRIEF-NAV, HANGAR-AVISOS, LIMPA-MAPA e POUSO-DANO feitos e não conferidos com o app aberto.
- **Bug confirmado (B6):** o sim fechado à força não manda Quit; `SimConnectService.IsConnected` = `_simConnect != null` e o app fica "conectado" a um sim morto (briefing com combustível/carga/posição congelados; B5, a linha tracejada presa, é o mesmo). Reiniciar o app resolve. Plano: `correcoes-voo-forcado-sdco.md` › B6.

## Próximo passo
- **Sessão Opus: TELEMETRIA-RECARGA** (B6; `SimConnectService` quente; conferir no SDK o que uma chamada com pipe quebrado lança e se chegam dados no menu). Depois BRIEF-LINHA (B5) só se ainda acontecer.
- **Sessão Opus: OFICINA-TEMPO-JOGO** (decisão do usuário 2026-11-27: parada só em tempo de jogo, missões expiram/aparecem, espera real de no máximo poucos segundos; `manutencao-parada-real.md` › 7).
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
