# Spec <ID> — <título>

> Spec boa evita rodada de CORRIGIR (que custa mais que a spec). Levante os fatos antes; critérios mensuráveis; diga o que **não** fazer.

## 1. Objetivo (1 frase)

## 2. Fatos já levantados (não redescobrir)
- `caminho/Arquivo.cs:linha` — o que existe ali (assinatura, campo, regra)
- Padrão a seguir: classe/arquivo parecido que já resolve algo igual
- Decisões do usuário que valem (link para `docs/plans/<plano>.md` › seção)
- Sistema: `docs/sistemas/<sistema>.md` (o executor lê)
- Armadilhas do `AGENTS.md` › 6 que se aplicam (e, se for arquivo quente, o passo a passo do que mudar)

## 3. Arquivos
- Mexer: `caminho/Arquivo.cs` (o que muda)
- **Fora do escopo:** arquivos, sistemas e comportamentos que não podem mudar

## 4. Contrato
Tipos, métodos, campos do JSON, valores (de preferência em dado, não em código).

## 5. Testes (nome → cenário → resultado)
- `Classe_Cenario_Resultado`: entrada → saída

## 6. Critérios de aceite (verificáveis)
1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 %.
2. Snapshot `missions_golden.json`: **deve ficar igual** | pode mudar (dizer por quê e o que conferir).
3. Medição (se houver): comando exato e o que colar no relatório. O que **não** rodar (ex.: `career-stab`, simulações longas: o revisor roda).
4. Relatório declara o que não foi testado no MSFS.
5. `docs/CHANGELOG.md` (`## AAAA-MM-DD - Título (<ID>) (Gemini)`) e `docs/sistemas/<sistema>.md` se o estado de um sistema mudou.
6. Um commit: `tipo(escopo): descrição (<ID>)`.
