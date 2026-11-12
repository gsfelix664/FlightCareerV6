# FlightCareer V6 — Instruções do Gemini

**Antes de qualquer tarefa, leia o arquivo [`AGENTS.md`](AGENTS.md) por completo.** Ele é a fonte de verdade sobre a arquitetura, os comandos, as regras de SimConnect e banco de dados e o estado atual de cada sistema. Este arquivo traz apenas o que é específico do Gemini.

@AGENTS.md

---

## 1. Você divide o repositório com o Claude

- O Claude também programa neste projeto (instruções dele em `CLAUDE.md`). Sigam as mesmas regras de `AGENTS.md`, em especial a seção **4. Trabalho em dupla**.
- Comece sempre por `git status` e `git log --oneline -5`. Alterações não commitadas que não são suas: não altere, não descarte, pergunte ao usuário.
- Ao terminar, registre o que fez no **fim** de `docs/CHANGELOG.md`, no formato `## AAAA-MM-DD - Título (Gemini)`. **Não** escreva o histórico neste arquivo.

## 2. Forma de trabalho (personas)

Você pode investigar, planejar e implementar. As personas abaixo são **etapas de checagem** que você declara ao mudar de fase (ex.: `[🎭 @backend]`). Os detalhes de cada uma estão em `.gemini/agents/`:

| Persona | Quando |
|---|---|
| `@architect` | Mudança estrutural, novo módulo, contrato entre camadas |
| `@database` | Entidade, tabela, coluna ou seed (siga `AGENTS.md` › Banco de dados) |
| `@backend` | Regras de negócio, serviços, telemetria, desgaste/falhas |
| `@frontend` | XAML/WPF, bindings MVVM, estilos, mapa |
| `@reviewer` | Sempre no fim: revisão do diff + `dotnet build` + `dotnet test` |

Fluxo: **investigar → planejar → implementar → `@reviewer` → relatório**.

1. **Investigar primeiro.** Confirme nomes de arquivos, classes, SimVars e eventos lendo o código e a documentação do SDK. Não presuma.
2. **Edição cirúrgica.** Mude só o necessário e preserve os padrões existentes.
3. **`@reviewer` só aprova se** o build passa com 0 erros, os testes passam e o diff foi lido procurando os erros da seção 6 de `AGENTS.md`. Build verde sozinho não é aprovação.
4. **Relatório curto:** arquivos alterados, causa raiz, resultado do build e dos testes, e o que **não** foi testado no MSFS.

## 3. Pontos de atenção neste momento

- Desgaste e falhas usam o sistema por componentes (`src/FlightCareer.Domain/Components/` + `component_catalog.json`); o `WearAndFailureService` não existe mais. Lógica nova vai para o domínio, com testes (ver `AGENTS.md` › 7).
- Para enviar eventos ao simulador use `SimConnectService.SendEvent`. Só use eventos que existem no SDK (lista em `AGENTS.md` › 6).
- Campos novos em `SimTelemetry` entram **somente no fim** do struct e na mesma posição em `SimConnectService.Connect()`.
