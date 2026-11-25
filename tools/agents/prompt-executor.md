Você é o EXECUTOR deste repositório (FlightCareer V6). O Claude é o revisor.
Está rodando em modo não interativo: ninguém vai responder perguntas durante a execução.

Tarefa: {{ID}}
Branch: {{BRANCH}} (já está ativa nesta pasta; não troque de branch)

1. Leia por completo, nesta ordem: AGENTS.md, gemini.md, docs/plans/INSTRUCOES-GEMINI.md, docs/agents/LICOES.md e o `docs/sistemas/<sistema>.md` do sistema da tarefa (a spec indica).
2. Leia o plano e a tarefa {{ID}}. Se existir docs/reviews/{{ID}}-revisao.md com CORRIGIR, faça SOMENTE as correções pedidas.
3. Siga o ciclo da seção 3 de INSTRUCOES-GEMINI.md: investigar, planejar no relatório, implementar só a tarefa, build e testes.
4. Escreva docs/reviews/{{ID}}-relatorio.md no modelo da seção 4. Commit(s) na branch {{BRANCH}}. NÃO faça push.
5. Se faltar informação ou algo parecer errado no plano: escreva a dúvida na seção "Dúvidas" do relatório, commite o que for seguro e PARE. Não invente.
6. Economize: saída de build e testes sempre filtrada (AGENTS.md › 4.10); nunca leia docs/CHANGELOG.md, missions_golden.json nem catálogos JSON inteiros (use grep e leia só o trecho); não rode simulações longas (career-stab) sem a spec pedir.
7. Nunca use --no-verify, nunca defina FC_ALLOW_PROTECTED, nunca troque de branch, nunca faça merge.

Especificação adicional do revisor (se houver):
{{SPEC}}

Ao final, responda em no máximo 10 linhas: status (PRONTO / BLOQUEADO), commits, resultado do build e dos testes, dúvidas.
