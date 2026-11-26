# Delegação ao Gemini (Antigravity CLI `agy`): detalhes

Lido sob demanda, antes de delegar ou revisar. A tabela de quando delegar está no `~/.claude/CLAUDE.md`.

## Ferramentas (`~/.claude/tools/gemini/`)

```bash
# Consulta somente leitura (modo plan, resposta cortada em 80 linhas, com caminho:linha)
~/.claude/tools/gemini/agy-ask.sh "@src/services/ Onde o token de sessão é renovado? Liste funções e arquivos."

# Tarefa de implementação num worktree ../<repo>-gemini (commita na branch, sem push)
~/.claude/tools/gemini/agy-task.sh F2.1 feat/minha-tarefa /caminho/spec.md [base]

# Auditoria das ações do último run (o que saiu da pasta permitida ou parece arriscado)
~/.claude/tools/gemini/agy-audit.sh "C:\PROJETOS\<repo>-gemini"
```

- Se o projeto tiver ferramentas próprias (`tools/agents/` etc.), elas têm prioridade sobre estas.
- Rode `agy-task.sh` **em segundo plano** e espere a notificação. Não acompanhe o log.
- `@caminho` no prompt funciona no `agy` (relativo à pasta onde ele roda). Prefira **diretórios específicos** a `@./`. Não existe mais `--all_files`.
- Leitura pura sempre em `--mode plan` (o `agy-ask.sh` já faz). Escrita só no worktree.
- O `gemini` CLI (npm) só se o usuário pedir e ele estiver autenticado (em 2026-11 falhava na autenticação).
- Modelo: o padrão do usuário (Gemini 3.8 Flash Medium). Não passe `--model` sem o usuário pedir.

## Spec (spec boa evita rodada de correção)

Escreva no scratchpad, curta:
1. objetivo em 1 frase;
2. **fatos já levantados** (arquivos, linhas, assinaturas, números) para o Gemini não redescobrir;
3. arquivos a criar/mexer e **fora do escopo**;
4. contrato (tipos, métodos, formato de dados);
5. testes com nome e cenário (entrada → saída);
6. critérios de aceite verificáveis e padrão de commit.

Lógica pura em camada testável; integração (UI, rede, hardware, SDK) fina por cima.

## Revisão (barata e por checklist)

1. Resumo do `agy-task.sh` → `git diff --stat` → diff só dos arquivos que importam (sensíveis: inteiros).
2. Rode você mesmo build e testes em modo silencioso (não confie só no relatório).
3. Checklist: escopo; critérios de aceite de verdade; regras do projeto; testes testam comportamento; erros não silenciados; concorrência/assíncrono; o que não pôde ser testado está declarado.
4. `agy-audit.sh` e avise o usuário se o Gemini mexeu fora do projeto/worktree ou em partes sensíveis do PC (sistema, registro, credenciais, outras pastas).
5. Veredito em `docs/reviews/<ID>-revisao.md`: **APROVADO** ou **CORRIGIR** numerado. Erro recorrente vira uma linha em `docs/agents/LICOES.md` (o Gemini lê antes de cada tarefa).
6. Máximo **2 rodadas** de CORRIGIR; depois corrija você mesmo ou leve a decisão ao usuário.
7. Merge `--no-ff` na base só após APROVADO. **Push só quando o usuário pedir.**

## Projeto sem essa estrutura

Proponha ao usuário criar `docs/reviews/`, `docs/agents/LICOES.md`, as regras no AGENTS.md/GEMINI.md e hooks de git antes de delegar.
