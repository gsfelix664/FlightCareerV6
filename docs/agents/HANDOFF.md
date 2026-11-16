# Passagem de sessão (Claude) — 2026-11-16 (tarde, PC do escritório)

Leia junto: `AGENTS.md` (§3 comandos, §6 regras, §7 estado), `CLAUDE.md`, `docs/agents/LICOES.md`, `docs/plans/validacao-simvars.md` e o índice de análises em `docs/plans/sensor-research/` (abaixo).

## Máquina e regras
- **PC do escritório: sem MSFS e sem SDK.** Build e testes rodam (DLLs do SimConnect versionadas em `src/FlightCareer.UI/`). Nada que precise do simulador roda aqui.
- **Divisão de trabalho (decisões do usuário, em `CLAUDE.md` e `tools/agents/global/CLAUDE.md`):** código **e dados** (catálogos JSON/CSV > ~20 linhas) vão para o Gemini com spec (`tools/agents/gemini-task.sh` em segundo plano); pesquisa, análise de documentação do SDK e planos ficam com o Claude. Claude revisa (`tools/agents/review-pack.sh`), roda build/testes, faz merge (`FC_ALLOW_PROTECTED=1`) e **push quando o usuário autorizar** (o push precisa de `FC_ALLOW_PROTECTED=1` também).
- Depois de um pull que mude `tools/agents/global/`, rodar `tools/agents/install-global.sh` (foi rodado hoje aqui).
- `docs/VARS.txt` e `REGRAS_tiers.txt` não rastreados são do usuário: não commitar nem apagar.

## Estado
- Base `feat/component-health-system` no GitHub em `7ebb15d` (+ este handoff). Build 0 erros; `dotnet test` **500/500**.
- Worktree do Gemini `../FlightCareerV6-gemini` limpo (branches já mergeadas).

## Feito hoje (código, tudo aprovado e mergeado; **nada testado no MSFS**)
1. **REGRAS-C152** (2 rodadas): pontos de contato por roda (`-0..-2`), gerador 2024, `DYNAMIC PRESSURE`, G negativo funcionando, `egtMax` de classe, trem em escala 0–100, fuzz com categoria "limite ausente". Ressalvas abertas: tolerância 0,05 %/h no oráculo; `FlapOverSpeed` em avião sem flap conta como limite ausente.
2. **Limites estimados** de Vne/Vfe/alternador em 42 aeronaves (`source: ClassDefault`, "estimativa — revisar").
3. **TURB-DEGRADACAO** (2 rodadas): saúde de Motor/Seção quente vira `TURB ENG N1/FUEL EFFICIENCY/ITT COOLING/THRUST EFFICIENCY LOSS` gravados no sim, por motor (decisão do usuário reverteu "nada é gravado no sim" só para essas 4).
4. **COMBUSTIVEL-REAL** (2 rodadas): consumo por `GENERAL ENG FUEL USED SINCE START` (soma incrementos, reinício de motor), combustível do app gravado nos tanques, sincronização automática no solo com conferência (3 tentativas), `UNLIMITED FUEL` trava o início, abastecimento em voo invalida o voo.
5. **SLEW-INTEGRIDADE**: slew no solo até 1 km antes da decolagem (blackbox); no ar/acima do limite invalida; congelamentos em voo invalidam; `InvalidateFlight(motivo)` único. **Ressalva:** confirmar no MSFS que `SIM ON GROUND` fica 1 durante o slew no pátio.

## Feito hoje (documentação, em `docs/plans/sensor-research/`)
- SimVars: `variaveis-diversos`, `-freios-trem`, `-comandos`, `-eletrica`, `-motor`, `-modelo-voo`, `-combustivel`, `-aeronave-diversos`, `-radio-navegacao`, `-sistemas`, `variaveis-obsoletas` (70 obsoletas; **`CRASH FLAG` e `GENERAL ENG FIRE DETECTED` ainda em produção**).
- Key Events: `eventos-eletricos`, `-motor`, `-comandos`, `-combustivel`, `-diversos` (6 páginas).
- API: `simconnect-api` (Flow API, `FlightSave`/`FlightLoad`, `FlightPlanLoad`, `ExecuteAction`).
- Lista para o SimvarWatcher: `tools/simvar_probe/watcher/flightcareer-candidatas-hud-qualidade.simvars` (~260) e `flightcareer-candidatas-atc.simvars` (testar `ATC *` à parte: suspeita das quedas).
- Planos novos: `docs/plans/taxas-aeroporto-servicos-solo.md`, `docs/plans/iluminacao-panes-disjuntor.md` (inclui flap: pane elétrica rearmável × mecânica), atualização em `simconnect-wb-deteccao-aeronave.md` (carga gravada em `PAYLOAD STATION WEIGHT`).

## Decisões do usuário registradas hoje
- Fuzz dos bugs (G negativo, egtMax, gerador): feito junto da REGRAS-C152.
- Regra de delegação: código e dados → Gemini.
- Combustível: gravar no sim; `UNLIMITED FUEL` trava; abastecer em voo invalida; consumo por `FUEL USED SINCE START`; no solo **sincroniza sozinho**; caminhão do sim (`REQUEST_FUEL_KEY`) **cobra e aceita**.
- Slew: no solo até 1 km; em voo **só invalida** (não bloquear).
- Auto-start (Ctrl+E): **não invalida, perde um pouco de XP** (sugestão 10 %, a confirmar).
- Panes elétricas: **as duas** (disjuntor rearmável `BREAKER_*_SET` e fio rompido `ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE`).
- Nada que dependa do ATC do sim (voo online).
- Carga da missão nas estações (`PAYLOAD STATION WEIGHT`): prioridade.
- Captura automática do pouso para o diário de bordo: aprovada.
- "Continuar de onde parou" (`FlightSave`/`FlightLoad`): ideia do usuário, documentada.

## Próximos passos (sem simulador), na ordem proposta
1. **Fuzz em todas as 46 aeronaves** (`--aircraft all` no `FlightCareer.Replay`, resumo por avião) + ressalvas da REGRAS-C152 (tolerância pela taxa real da peça; pular Vfe em avião sem flap). Prometido ao usuário.
2. **Nota dos passageiros por tempo** (`FlightQualityService` conta por pacote): segundos fora da faixa, pausa ignorada, turbulência × manobra (`SEMIBODY LOADFACTOR YDOT`, `TURN COORDINATOR BALL`, `INCIDENCE BETA` — só depois do Watcher; a lógica pode vir antes com dados atuais).
3. **Auto-start → XP** e **caminhão de combustível cobrado** (lógica pura + integração fina).
4. **Taxas de aeroporto F1** — depende das 5 decisões do plano (`taxas-aeroporto-servicos-solo.md` § 6).
5. **Iluminação no Hangar** (só domínio).
6. **Integridade pela Flow API** (troca do `CRASH FLAG`, teleporte, Back On Track, `REPAIR_AND_REFUEL`, resets) — código pronto, validação no MSFS.

## Decisões ainda pendentes do usuário
- `eventos-diversos.md` § 3 (resets/`BACK_TO_FLY` invalidam?; serviços de solo pelo sim; alarme nativo; extintor apaga o fogo do addon; capas/walkaround).
- `eventos-comandos.md` § 3 (compensador inoperante/disparado; auto-leme = regra do auto-start?).
- `eventos-combustivel.md` § 3 (panes `FUELSYSTEM_*`; `REPAIR_AND_REFUEL` invalida).
- `simconnect-api.md` § 3 (continuar de onde parou; plano no GPS; Flow API; mensagens no sim; `FailureAction`).
- Planos de taxas e de iluminação (§ decisões).

## Pendências que precisam do simulador (máquina de casa)
SimvarWatcher com as listas novas; SimVarProbe com `--filter "^(?!ATC |AI )"`; escrita de turbina, combustível (novo e legado), slew no pátio; teste de panes (modo de panes do SimVarProbe: `BREAKER_*_SET`, `PARTIAL PANEL *`, `FUELSYSTEM_*`, `HYDRAULIC_ACTUATOR_ACTIVE_SET`, trim disabled); `FlightSave`/`FlightLoad`; Flow API; voos em outros aviões; pendências antigas (F4.2e, crash declarado, pista de terra).
