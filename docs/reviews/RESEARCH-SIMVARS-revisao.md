# Revisão RESEARCH-SIMVARS — rodada 1

**Veredito: APROVADO** (documentação; rodada 2 vem com as páginas do SDK indicadas pelo usuário)

Conferido pelo revisor na documentação oficial:
- `COVER ON:<id>` existe (Aircraft Miscellaneous Variables › Aircraft States, Bool). **Faltou na pesquisa** a lista completa dos índices: 0 calços, 1 motores, 2 pitot, 3 tomadas estáticas, 4 rotores, 5 trem de pouso, 6 hélices, 7 para-brisa. Corrigir na rodada 2.
- `SimConnect_AddToDataDefinition` aceita `"L:NOME"` (sempre FLOAT64; unidade converte o tipo), além de `I:`/`O:` (caminho do componente) e `Z:` / `L:1:` (locais ao SimObject). Sem módulo WASM.
- `fseconomy/fse-simconnect` não existe; `PARTIAL PANEL STATIC` não existe (já sabido).
