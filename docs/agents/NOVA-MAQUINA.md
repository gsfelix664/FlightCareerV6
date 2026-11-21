# Configurar outra máquina (Claude + Gemini iguais a esta)

Tudo o que o Claude e o Gemini precisam está versionado neste repositório:

| O quê | Onde no repositório | Para onde vai |
|---|---|---|
| Regras do projeto (os dois agentes) | `AGENTS.md`, `CLAUDE.md`, `gemini.md`, `docs/plans/INSTRUCOES-GEMINI.md`, `docs/agents/` | já usados direto do repositório |
| Instruções globais do Claude (todos os projetos) | `tools/agents/global/CLAUDE.md` | `~/.claude/CLAUDE.md` |
| Ferramentas globais (consulta, tarefa, auditoria do Gemini) | `tools/agents/global/gemini/*.sh` | `~/.claude/tools/gemini/` |
| Ferramentas deste projeto | `tools/agents/gemini-task.sh`, `review-pack.sh` | usadas direto do repositório |
| Travas de git | `.githooks/` | ativadas por `core.hooksPath` |

**Não vão pelo git** (ficam só em cada máquina): o banco `flightcareer.db`, os logs de sensores em
`%LocalAppData%\FlightCareer\sensor-logs\`, o login do Antigravity e do Claude, e o worktree `../FLIGHTCAREERV6-gemini`.

## Passo a passo

1. **Pré-requisitos:** Git for Windows (Git Bash), .NET 10 SDK, Python 3 (`py`), Claude Code (app desktop ou CLI) e Antigravity
   (com o CLI `agy` no PATH). Faça login no Antigravity uma vez (abra `agy` no terminal) e no Claude.
2. **Código:**
   ```bash
   git clone <url-do-remoto> C:/PROJETOS/FLIGHTCAREERV6
   cd C:/PROJETOS/FLIGHTCAREERV6
   git checkout feat/component-health-system
   ```
   Se a pasta já existir: `git fetch` e `git checkout feat/component-health-system` e `git pull`.
3. **Configuração global + hooks:**
   ```bash
   tools/agents/install-global.sh
   ```
   O script guarda backup de qualquer `~/.claude/CLAUDE.md` diferente que já exista (`.bak-<data>`), ativa os hooks
   e testa se o `agy` responde. Se aparecer "ATENÇÃO", resolva o item e rode de novo.
4. **Modelo do Gemini:** deixe o padrão do Antigravity em **Gemini 3.8 Flash (Medium)** (`agy models` lista os disponíveis;
   escolha no próprio `agy`). O Claude não passa `--model` sem você pedir.
5. **Conferir:** `dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests`.
6. **Abrir o Claude Code na pasta `C:\PROJETOS\FLIGHTCAREERV6`.** Ele lê `~/.claude/CLAUDE.md`, `CLAUDE.md` e `AGENTS.md`,
   e já sabe delegar ao Gemini, revisar e auditar. Para retomar, diga: *"Continue o plano de peças: próxima tarefa F1.4"*.

## Ao voltar para esta máquina (ou trocar de novo)

```bash
git pull
tools/agents/install-global.sh
```

Se mudar as instruções globais em uma máquina, copie para `tools/agents/global/`, commite e rode o script na outra.

## Observações

- O caminho `C:\PROJETOS\FLIGHTCAREERV6` não é obrigatório, mas mantê-lo igual evita ajustes (o worktree do Gemini fica
  sempre ao lado: `..\FLIGHTCAREERV6-gemini`, criado automaticamente na primeira tarefa).
- A memória automática do Claude é por máquina; o que importa dela está nas instruções globais e neste repositório.
- `FC_ALLOW_PROTECTED=1` libera commit/push nas branches protegidas: só para merge aprovado ou quando você pedir.
