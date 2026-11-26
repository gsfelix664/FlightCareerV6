# FlightCareer V6 — Instruções do Claude

@AGENTS.md

O guia acima (`AGENTS.md`) é a fonte de verdade sobre arquitetura, comandos, regras técnicas e o índice dos sistemas (estado em `docs/sistemas/`). Este arquivo traz apenas o que é específico do Claude.

## Você divide o repositório com o Gemini

- O Gemini também programa aqui (instruções dele em `gemini.md` e `docs/plans/INSTRUCOES-GEMINI.md`). Sigam as mesmas regras de `AGENTS.md` › 4. Trabalho em dupla.
- Comece lendo `docs/agents/HANDOFF.md`; o restante (git, uma tarefa por sessão, CHANGELOG como `(Claude)`, sistemas) segue `AGENTS.md` › 4 e 8.

## Como trabalhar

- Responda ao usuário em português.
- Rode `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests` antes de dar algo como pronto. Diga com clareza o que depende do MSFS e não foi testado.
- Para afirmações sobre o MSFS/SimConnect (nomes de eventos, SimVars, comportamento do wear and tear nativo), confira a documentação do SDK em vez de confiar na memória ou nos comentários do código.
- Para estes arquivos quentes, leia o arquivo inteiro antes de editar: `ComponentFlightSession.cs`, `SimFailureActuator.cs`, `FlightMonitor.cs`, `SimConnectService.cs`, `SimTelemetry.cs`.
- Scripts auxiliares e arquivos temporários ficam no scratchpad, nunca no repositório (exceção: as ferramentas versionadas de `tools/agents/`).

## Orquestrando o Gemini (você revisa, ele implementa)

Antes de delegar ou revisar, leia `docs/agents/ORQUESTRACAO.md` (fluxo, travas, checklist) e `docs/agents/MODELO-SPEC.md`.

- `tools/agents/gemini-task.sh <ID> <branch> <spec>` em segundo plano; revisão com `tools/agents/review-pack.sh <ID> <branch>`; veredito em `docs/reviews/`; máx. 2 rodadas de CORRIGIR.
- Merge na base só após APROVADO: `FC_ALLOW_PROTECTED=1 git merge --no-ff <branch>`. Push só quando o usuário pedir.
- Decisões do usuário: **arquivos quentes** (2026-11-15) e **dados de catálogo** com mais de ~20 linhas (2026-11-16) também vão para o Gemini, com spec detalhada; você revisa quentes linha a linha e dados por amostragem. Pesquisa, SDK, planos e arquitetura continuam com você.
