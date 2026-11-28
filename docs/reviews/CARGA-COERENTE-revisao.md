# Revisão CARGA-COERENTE (Claude, 2026-11-25)

**Veredito: APROVADO** (com 2 ajustes de dado feitos pelo revisor).

## Conferido
- Escopo: só os arquivos da spec. `AppDbContext.cs` e `MainViewModel.cs` (quentes) lidos linha a linha: 2 `ALTER TABLE` e 2 checagens de carga perigosa, nada mais.
- O sorteio do tipo de carga usa `Random` local com semente FNV-1a (`StableSeed`) e não consome o `Random` do gerador. Golden comparado pelo revisor: 280 registros, só 59 `Title` diferentes, todos de subtipos de carga; nenhum outro campo mudou.
- `cargo_kinds.json`: 15 missões, classes só da tabela da spec, `carga_dgr` e `combustivel_tambor` com `dg` em todos os tipos; etiquetas existentes nos perfis (teste).
- Build 0 erros; `dotnet test` 1.134/1.134 (13 novos em `CargoKindCatalogTests`).

## Ajustes do revisor (dado)
1. "espoletas" saiu do tipo de explosivos 1.1D (detonadores são da divisão 1.1B/1.4B).
2. "recipientes de nitrogênio líquido" virou "contêineres criogênicos secos" (nitrogênio líquido é classe 2.2, fora da tabela; dry shipper não é carga perigosa).

## Não testado
- Linha de carga no painel de pré-despacho e as `MessageBox` de aceite e início de voo (não abri o app).
- Títulos longos com o nome da carga podem cortar no quadro: conferir visualmente.
