# Instruções globais do Claude (todos os projetos)

Responda em português. As instruções do projeto (CLAUDE.md / AGENTS.md do repositório) têm prioridade sobre este arquivo.
A fonte deste arquivo está em `FLIGHTCAREERV6/tools/agents/global/` (instalação: `tools/agents/install-global.sh`): ao mudar este arquivo ou os scripts, atualize lá.

## Economia de quota (decisões do usuário, 2026-11-24 e 2026-11-25)

Cada resposta relê a conversa inteira e as instruções; sessão longa custa caro, mesmo com janela de 1M.
- **Uma tarefa por sessão.** Comece pela passagem do projeto (ex.: `docs/agents/HANDOFF.md`). Ao terminar: build/testes, reescreva a passagem (máx. 40 linhas: estado, pendências, próximo passo, decisões abertas), commit e push, e peça sessão nova.
- **Modelo:** spec, arquitetura e revisão delicada → Opus; rotina → Sonnet. Você não troca o próprio modelo: se a sessão estiver no errado, diga em uma linha.
- **Saída filtrada:** build só com erros; testes só resumo e falhas; `git diff --stat` antes do diff; `grep`/`sed -n` em arquivos grandes; nunca leia inteiros changelogs, snapshots, catálogos ou logs.
- **Decisões vão para os planos** do projeto (com data). Detalhe fica em documentos lidos sob demanda, não em `AGENTS.md`/`CLAUDE.md` nem na passagem.

## Divisão de trabalho com o Gemini (Antigravity CLI `agy`)

O Claude especifica, revisa, aprova e faz merge; o Gemini faz o trabalho pesado.
**Antes de delegar ou revisar, leia `~/.claude/tools/gemini/DELEGACAO.md`** (ferramentas, spec, checklist).

| Situação | Quem |
|---|---|
| Pergunta ampla sobre o código (varrer diretórios, >~100 KB de leitura) | `agy-ask.sh` (somente leitura) |
| Implementação com critérios de aceite claros (> ~20 linhas, ciclos de build) | `agy-task.sh` (worktree) |
| Catálogo/configuração (JSON, CSV) com mais de ~20 linhas | `agy-task.sh` com o critério na spec (fonte, o que não inventar) + revisão por amostragem |
| Pesquisa, análise de documentação, planos, arquitetura | Claude |
| Mudança pequena (≲ 20 linhas), ajuste de revisão, busca pontual | Claude direto |
| Código delicado ou arquivo "quente" | `agy-task.sh` com spec detalhada (passo a passo, o que não tocar) + revisão linha a linha |

## Segurança e git

- O Gemini roda sem pedir confirmação: em projeto novo, proponha hooks que bloqueiem commit/push em `main`, `push --force` e mensagem fora do padrão. Rode `agy-audit.sh` e avise se ele saiu do projeto.
- Nunca `--no-verify`. Um commit por tarefa, sem `--amend` depois do relatório. **Push só quando o usuário pedir** (a passagem de sessão já está autorizada).
