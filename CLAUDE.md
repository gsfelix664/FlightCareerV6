# FlightCareer V6 — Instruções do Claude

@AGENTS.md

O guia acima (`AGENTS.md`) é a fonte de verdade sobre arquitetura, comandos, regras técnicas e o índice dos sistemas (estado em `docs/sistemas/`). Este arquivo traz apenas o que é específico do Claude.

## Você divide o repositório com o Gemini

- O Gemini também programa aqui (instruções dele em `gemini.md`, `docs/plans/INSTRUCOES-GEMINI.md`, `.gemini/` e `.antigravity/`). Sigam as mesmas regras de `AGENTS.md` › 4. Trabalho em dupla.
- **Uma tarefa por sessão** (`AGENTS.md` › 4.9): comece lendo `docs/agents/HANDOFF.md`; ao terminar, reescreva o HANDOFF, faça commit e push e peça ao usuário uma sessão nova para a próxima tarefa.
- Comece por `git status` e `git log --oneline -5`. Não altere nem descarte alterações não commitadas que não foram feitas nesta sessão: pergunte ao usuário.
- Ao terminar uma tarefa que mudou código, acrescente uma entrada no fim de `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (Claude)`) e atualize `docs/sistemas/<sistema>.md` se o estado do sistema mudou.

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
- **Arquivos quentes também vão para o Gemini** (decisão do usuário, 2026-11-15, para economizar tokens): a spec traz o passo a passo (o que mudar, em qual arquivo/método, por quê, o que **não** tocar, armadilhas do `AGENTS.md` › 6) e o revisor lê o diff desses arquivos **linha a linha** (arquivo inteiro) e roda build e testes antes do merge. Faça você mesmo só o que é trivial (≲ 20 linhas), os ajustes da própria revisão e as decisões de arquitetura (que entram na spec).
- **Dados também vão para o Gemini** (decisão do usuário, 2026-11-16): preencher ou alterar catálogos (`Data/aircraft/*.json`, `wear_rules.json`, `parts_catalog.json`, `poh_values.csv` etc.) com mais de ~20 linhas. Você decide o critério (fonte, notas, o que não inventar) e escreve na spec; o Gemini preenche; você revisa por amostragem e roda build/testes. Pesquisa, análise de documentação do SDK e planos continuam com você.
