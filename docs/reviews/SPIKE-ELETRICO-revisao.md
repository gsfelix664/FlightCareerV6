# Revisão SPIKE-ELETRICO — rodada 1

**Veredito: CORRIGIR**

Conferido na documentação oficial (página retail, que responde; as URLs `.../html/6_Programming_APIs/...` citadas no spike dão **HTTP 404**):
`https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simvars/aircraft-simvars/aircraft-electrics-variables/` (o conteúdo das variáveis fica no fim da página, depois de ~480 mil caracteres de menu).

O que a documentação diz e o spike não considerou: o C172 do MSFS 2024 usa o sistema elétrico **modular v2**, e no v2 várias SimVars escolhidas são **legadas (FSX / v1.00)** ou **obsoletas**:

| SimVar do spike | Situação no SDK 2024 |
|---|---|
| `ELECTRICAL AVIONICS BUS VOLTAGE` / `AMPS` (1º candidato de aviônicos) | legada, só FSX; no v2 usar `ELECTRICAL BUS VOLTAGE` / `ELECTRICAL BUS AMPS` |
| `ELECTRICAL BATTERY BUS VOLTAGE` | legada (v1.00/FSX); no v2 usar `ELECTRICAL BUS VOLTAGE` |
| `ELECTRICAL HOT BATTERY BUS VOLTAGE` | obsoleta, só FSX |
| `ELECTRICAL MAIN BUS VOLTAGE:n` / `ELECTRICAL MAIN BUS AMPS:n` | legadas; no v2 usar `ELECTRICAL BUS VOLTAGE` / `ELECTRICAL BUS AMPS` |
| `ALTERNATOR CONNECTION ON:1` | no v2 é por **nome do gerador**; no v1.00, índice do alternador **e** do barramento: `:1` sozinho não é o formato documentado |
| `ELECTRICAL BATTERY VOLTAGE` / `LOAD` | válidas; no v2, por nome ou índice da bateria, e a tensão **só é devolvida quando a bateria é fornecedora** (0 caso contrário). A explicação de que "sem índice devolve a tensão de circuito aberto" não está na documentação |
| `CIRCUIT ON` / `CIRCUIT SWITCH ON` | válidas no v2, por nome do circuito ou índice global |
| `ELECTRICAL BUS VOLTAGE` / `ELECTRICAL BUS AMPS` | atuais no v2, por nome do barramento ou **índice global** (o spike não as listou) |

## Correções
1. Troque todas as URLs pela página retail acima (e, se citar outras, confira que respondem). Nada de URL não conferida.
2. Refaça as tabelas da §3 com a coluna "v2 / v1.00 / FSX" tirada da documentação; os legados e obsoletos saem da posição de preferência (podem ficar como fallback do MSFS 2020/aviões legados, marcados como tal).
3. Candidatos no registro (máximo de 16 datums novos, no total da branch): priorize `ELECTRICAL BUS VOLTAGE:<n>` e `ELECTRICAL BUS AMPS:<n>` por índice global (varra uma faixa pequena, ex. 1–5), `CIRCUIT ON:<n>` numa faixa pequena, `ELECTRICAL BATTERY VOLTAGE:1` e `ELECTRICAL BATTERY LOAD:1`. Remova `ELECTRICAL HOT BATTERY BUS VOLTAGE` e `ALTERNATOR CONNECTION ON:1`. Mantenha no máximo um ou dois legados como conferência (justifique).
4. Verifique se o SimConnect (`AddToDataDefinition`) aceita o formato por **nome** (`:'Bus_1'_n`) ou só índice numérico. Cite a fonte; se a documentação não disser, escreva "não documentado" e use só índices.
5. Diga como descobrir, no voo de teste, qual índice global é o barramento principal/aviônicos/alternador do C172 (ex.: comparar as colunas varridas com as ações do roteiro) e ajuste o roteiro (§7 do `ROTEIRO.md`) para isso.
6. `AVIONICS MASTER SWITCH`: confira na mesma página se é indexado no 2024 e em qual sistema (v2/v1/FSX); remova `:1`/`:2` se o índice não estiver documentado.
7. Na §2 (diagnóstico), marque o que é **observado** (coluna do CSV + valor) e o que é **hipótese**; tire afirmações que não estejam na documentação nem no CSV.
8. Refaça as recomendações da §4 em cima dos candidatos v2.
9. Atualize o relatório, o inventário (paridade), o `SensorLayoutTests` e a entrada do CHANGELOG. Commit `fix(sensors): ...` / `docs(sensors): ...`.

---

# Revisão SPIKE-ELETRICO — rodada 2

**Veredito: APROVADO**

- Os 9 itens da rodada 1 foram aplicados: URLs retail; matriz v2 / v1.00 / FSX; candidatos v2 por índice global (`ELECTRICAL BUS VOLTAGE:1..5`, `ELECTRICAL BUS AMPS:1..5`, `CIRCUIT ON:1..3`, `ELECTRICAL BATTERY VOLTAGE:1`, `ELECTRICAL BATTERY LOAD:1`) e um legado de conferência (`ELECTRICAL MAIN BUS VOLTAGE:1`), 16 datums no total; formato por nome no SimConnect marcado como não documentado (só índices); diagnóstico separando observado × hipótese; roteiro §7 com identificação dos barramentos pelos marcadores e teste do leme.
- Conferido pelo revisor: `dotnet build` 0 erros; `dotnet test` 264/264; `python -m unittest discover -s tools/tests` 40/40; nenhum sensor existente alterado; registro × inventário em paridade.
- **Não testado no MSFS.** Próximo passo: o usuário voa o teste de solo da §7 do `ROTEIRO.md` no C172 G1000; depois o revisor escolhe as SimVars das três regras mudas.
