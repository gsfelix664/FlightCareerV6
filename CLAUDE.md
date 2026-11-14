# FlightCareer V6 — Instruções do Claude

@AGENTS.md

O guia acima (`AGENTS.md`) é a fonte de verdade sobre arquitetura, comandos, regras técnicas e estado atual. Este arquivo traz apenas o que é específico do Claude.

## Você divide o repositório com o Gemini

- O Gemini também programa aqui (instruções dele em `gemini.md`, `docs/plans/INSTRUCOES-GEMINI.md`, `.gemini/` e `.antigravity/`). Sigam as mesmas regras de `AGENTS.md` › 4. Trabalho em dupla.
- Comece por `git status` e `git log --oneline -5`. Não altere nem descarte alterações não commitadas que não foram feitas nesta sessão: pergunte ao usuário.
- Ao terminar uma tarefa que mudou código, acrescente uma entrada no fim de `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (Claude)`) e atualize `AGENTS.md` › 7 se o estado de algum sistema mudou.

## Como trabalhar

- Responda ao usuário em português.
- Rode `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests` antes de dar algo como pronto. Diga com clareza o que depende do MSFS e não foi testado.
- Para afirmações sobre o MSFS/SimConnect (nomes de eventos, SimVars, comportamento do wear and tear nativo), confira a documentação do SDK em vez de confiar na memória ou nos comentários do código.
- Para estes arquivos quentes, leia o arquivo inteiro antes de editar: `ComponentFlightSession.cs`, `SimFailureActuator.cs`, `FlightMonitor.cs`, `SimConnectService.cs`, `SimTelemetry.cs`.
- Scripts auxiliares e arquivos temporários ficam no scratchpad, nunca no repositório (exceção: as ferramentas versionadas de `tools/agents/`).

## Orquestrando o Gemini (você revisa, ele implementa)

Fluxo, travas e checklist em `docs/agents/ORQUESTRACAO.md`. Resumo:

- Tarefa de implementação do plano → spec curta (`docs/agents/MODELO-SPEC.md`, no scratchpad) e `tools/agents/gemini-task.sh <ID> <branch> <spec>` **em segundo plano**. Não acompanhe o log; espere a notificação.
- Revisão: `tools/agents/review-pack.sh <ID> <branch>` primeiro, depois só o diff dos arquivos relevantes (quentes: inteiros). Veredito em `docs/reviews/<ID>-revisao.md` (APROVADO / CORRIGIR numerado). Erro recorrente → uma linha em `docs/agents/LICOES.md`.
- Máximo de 2 rodadas de CORRIGIR; depois corrija você mesmo ou leve a decisão ao usuário.
- Merge na base só após APROVADO: `FC_ALLOW_PROTECTED=1 git merge --no-ff <branch>`. Push só quando o usuário pedir.
- Faça você mesmo o que é pequeno (≲ 20 linhas) ou exige julgamento fino (arquivos quentes de SimConnect, decisões de arquitetura): delegar custa mais do que fazer.
