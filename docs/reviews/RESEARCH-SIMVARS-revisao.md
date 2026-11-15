# Revisão RESEARCH-SIMVARS — rodada 1

**Veredito: APROVADO** (documentação; rodada 2 vem com as páginas do SDK indicadas pelo usuário)

Conferido pelo revisor na documentação oficial:
- `COVER ON:<id>` existe (Aircraft Miscellaneous Variables › Aircraft States, Bool). **Faltou na pesquisa** a lista completa dos índices: 0 calços, 1 motores, 2 pitot, 3 tomadas estáticas, 4 rotores, 5 trem de pouso, 6 hélices, 7 para-brisa. Corrigir na rodada 2.
- `SimConnect_AddToDataDefinition` aceita `"L:NOME"` (sempre FLOAT64; unidade converte o tipo), além de `I:`/`O:` (caminho do componente) e `Z:` / `L:1:` (locais ao SimObject). Sem módulo WASM.
- `fseconomy/fse-simconnect` não existe; `PARTIAL PANEL STATIC` não existe (já sabido).

---

# Revisão RESEARCH-SIMVARS — rodada 2

**Veredito: CORRIGIR** (junto com a rodada 3)

O revisor cruzou a auditoria com os CSVs reais (`%LocalAppData%\FlightCareer\sensor-logs\20261115-1747-Cessna_C152.csv` e `20261115-1636-C172SP_G1000_Passengers.csv`). O `SensorFrameReader` **remove do layout** toda SimVar rejeitada pelo MSFS (`NAME_UNRECOGNIZED`); portanto **coluna presente no CSV = nome aceito pelo simulador**.

1. **Errado:** `FLAP SPEED EXCEEDED` e `GEAR SPEED EXCEEDED` "inexistentes" — as duas foram aceitas (presentes nos dois logs).
2. **Errado:** `ELECTRICAL BATTERY LEVEL` "inexistente" — aceita e varia no C152 (98,6–100), é útil.
3. **Errado:** `ELECTRICAL TOTAL LOAD AMPS` "legada, valores estáticos ou zerados" — varia nos dois aviões (0–175 A); a própria página oficial não a marca como obsoleta.
4. **Certo e importante:** `CONTACT POINT COMPRESSION` (e `IS SKIDDING`) indexadas por motor: o índice é o ponto de contato; hoje só lemos `:1`. Mantenha.
5. Afirmações de unidade que "falham a conversão" (ex.: `ESTIMATED CAPACITY PCT` em `percent over 100`) não se confirmam: o valor chega coerente (0,986–1,0). Rebaixe para "informativo" quando o CSV mostrar valor coerente.

**Regra para as rodadas seguintes:** "não encontrado na documentação" ≠ "não existe". Classifique cada item como: **aceita pelo sim** (coluna no CSV), **rejeitada pelo sim** (ausente do CSV embora no registro, ou no log `[SimConnect EXCEPTION]`), **documentada**, **não documentada**. Só chame de inexistente o que o sim rejeitou.
