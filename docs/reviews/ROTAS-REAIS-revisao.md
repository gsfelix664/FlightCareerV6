# Revisão ROTAS-REAIS — Linha aérea prefere rotas reais (2026-11-25, Claude)

**Veredito: APROVADO** (commit `07189f6`, branch `feat/rotas-reais`).

## Conferido
- Escopo: só os arquivos da spec; nenhum arquivo quente; `FilterCandidates`, pagamento e XP intactos.
- `DestinationChoiceGroup` com `extraMultiplier` nulo mantém o sorteio atual (teste de sequência idêntica); multiplicador de rota fica fora do teto `MaxMultiplier`, como pedido.
- `AirlineRouteCatalog`: segue o padrão do `CargoKindCatalog`; falhas registradas com `Debug.WriteLine`, sem `catch {}` silencioso.
- Dados: `airline_routes.json` com 330 KB, 17.223 pares e 3.090 origens; pares simétricos (0 assimétricos); amostra SBSP↔SBRJ = 4 companhias, SBGR com rotas a EDDF, EGLL, KDFW etc. Copiado para a saída pelo `Data\places\*.json` do csproj.
- `realRouteMultiplier` nas 8 missões certas (6 nas regulares, 4 nas regionais); `{route_note}` nas 8 descrições.
- Golden: 9 registros mudaram, todos dos subtipos de linha.
- Build no worktree 0 erros; `dotnet test` 1.165/1.165.
- Auditoria (`agy-audit.sh`): procurou a base do Little Navmap em `C:\Users\Usuario` (só leitura, não achou: gerado sem `--lnm`) e baixou `routes.dat` do GitHub. Nada gravado fora do worktree.

## Observações (sem correção)
- Sem `--lnm`, códigos que não existem no MSFS só deixam de casar (não dão peso); regerar com `--lnm` no PC de casa se quiser.
- Dados de 2014: rota que fechou depois só fica mais provável, não é obrigatória.

## Não testado
- Quadro no app (nota "Rota regular com demanda consolidada." e frequência real de destinos) e voo no MSFS.
