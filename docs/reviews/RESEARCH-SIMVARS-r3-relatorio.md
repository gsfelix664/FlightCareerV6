# RESEARCH-SIMVARS-r3 — Descoberta de Variáveis Novas e Correção da Auditoria

**Branch:** docs/pesquisa-variaveis · **Commit:** a registrar · **Data:** 2026-11-15

## Plano (antes de codar)
- Corrigir a auditoria da rodada 2 em `docs/plans/sensor-research/auditoria-registro.md` aplicando as correções pedidas na revisão (`RESEARCH-SIMVARS-revisao.md`): comprovar a existência com base nos CSVs reais do `SensorRecorder` (onde coluna presente = nome aceito pelo simulador), reclassificar `FLAP SPEED EXCEEDED`, `GEAR SPEED EXCEEDED` e `ELECTRICAL BATTERY LEVEL` como aceitas nativamente pelo sim, restaurar `ELECTRICAL TOTAL LOAD AMPS` como documentada e dinâmica, tratar unidades coerentes como informativas e propor a solução técnica para a leitura dos pontos de contato do trem de pouso (`CONTACT POINT COMPRESSION / IS SKIDDING`).
- Mapear de cima para baixo todas as peças do catálogo (`parts_catalog.json`) identificando as variáveis documentadas do SDK MSFS 2024 e de projetos abertos de referência (`MobiFlight-Connector`, `aircraft` FBW, `FsConnect`, `msfs2020-go`) que ainda não foram registradas no FlightCareer.
- Gerar o documento de descoberta em `docs/plans/sensor-research/descoberta-variaveis.md` com tabela detalhada, links oficiais, unidades, evidências de repositórios, valor e regras de aplicação.
- Elaborar a seleção do "Top 25 para o próximo teste" em um roteiro linear de solo e voo curto (~15 min), acompanhado da lista de variáveis descartadas/esclarecidas.
- Executar build e testes da solução para assegurar que o repositório permanece 100% íntegro.

## O que foi feito
- `docs/plans/sensor-research/auditoria-registro.md`:
  - Corrigida a classificação de `FLAP SPEED EXCEEDED` e `GEAR SPEED EXCEEDED`: ambas foram aceitas nativamente pelo SimConnect sem erro (presentes nos 4 logs de teste do C152 e C172).
  - Corrigida a classificação de `ELECTRICAL BATTERY LEVEL`: aceita pelo sim e com variação ativa nos logs (608 valores distintos entre 98.6% e 100% no C152).
  - Corrigida a classificação de `ELECTRICAL TOTAL LOAD AMPS`: documentada oficialmente no SDK 2024, não obsoleta e com variação dinâmica real nos logs (0 a 175 A).
  - Rebaixada a divergência de escala de `ELECTRICAL BATTERY ESTIMATED CAPACITY PCT` para informativo (entrega valores coerentes entre 0.986 e 1.000).
  - Mantido e detalhado o diagnóstico de `CONTACT POINT COMPRESSION` e `CONTACT POINT IS SKIDDING`: atualmente marcadas com `perEngine: true`, o que causa leitura restrita ao índice `:1` (ignorando o trem dianteiro `:0` e direito `:2`). Proposta a separação explícita por ponto de contato (`:0`, `:1`, `:2`) mapeando para perna de nariz, principal esquerda e principal direita.
  - Adicionadas as colunas obrigatórias: "Aceita pelo Sim?", "Varia nos Logs?" e "Documentada?".
- `docs/plans/sensor-research/descoberta-variaveis.md`:
  - Mapeamento completo de cima para baixo a partir do `parts_catalog.json`, cobrindo 28 novas variáveis do MSFS 2024 com URLs oficiais, unidades canônicas, evidências em projetos abertos (`FsConnect`, FBW `aircraft`, `MobiFlight`) e regras físicas de dano e desgaste no FlightCareer.
  - Seção "Top 25 para o Próximo Teste": seleção das 25 variáveis de mais alto retorno técnico com roteiro cronometrado e passo a passo de solo e voo curto (~15 min).
  - Seção "Variáveis Descartadas ou Esclarecidas": análise de `BRAKE TEMPERATURE` (inexistente como SimVar nativa no SDK da Asobo; gerada por simulação própria em WASM/LVar nos aviões de terceiros; o FlightCareer a substitui por termodinâmica física $E = \frac{1}{2}mv^2$), `PARTIAL PANEL STATIC`, `AIRCRAFT STRESS DAMAGE`, sintaxes inválidas e variáveis redundantes.
- `docs/reviews/RESEARCH-SIMVARS-relatorio.md`: Atualizado com o histórico consolidado da rodada 3.
- `docs/CHANGELOG.md`: Adicionada entrada da rodada 3 de pesquisa de variáveis.

## Critérios de aceite
- [x] Corrigir auditoria da rodada 2 com base nos CSVs reais — Verificado e corrigido em `auditoria-registro.md`.
- [x] Cruzar prova de existência com remoção de SimVars rejeitadas pelo `SensorFrameReader` — Verificado (zero rejeições nos logs do C152/C172).
- [x] Propor leitura de contact points por ponto (`:0..N`) — Detalhado na Seção 6 de `auditoria-registro.md`.
- [x] Mapeamento de cima para baixo pelas peças do `parts_catalog.json` — 28 variáveis novas mapeadas cobrindo motor, óleo, hélice, trem, freios, superfícies, luzes, pressurização, walkaround e ambiente.
- [x] Variáveis documentadas com URLs oficiais do SDK MSFS 2024 — Todas com links diretos oficiais.
- [x] Evidências em projetos abertos com arquivo e linha — Citadas para FBW, FsConnect e MobiFlight.
- [x] Seção "Top 25 para o próximo teste" com roteiro linear — Criado roteiro de 15 minutos em 17 passos.
- [x] Seção "Descartadas ou Esclarecidas" — Detalhada com esclarecimento sobre `BRAKE TEMPERATURE` e SimVars inexistentes.
- [x] Não alterar código de produção — Nenhum arquivo em `src/` foi modificado.
- [x] Build e testes verdes — `dotnet build` e `dotnet test` executados com 0 erros e 0 falhas.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos (nenhum código C# alterado).
- `dotnet test src/FlightCareer.Tests`: 319 aprovados, 0 falhas.

## Não testado
- As variáveis novas documentadas na pesquisa ainda não foram adicionadas ao `sensor_registry.json` (tarefa estritamente documental/pesquisa) e serão validadas fisicamente no simulador na execução do roteiro Top 25 planejado.

## Dúvidas e decisões para o revisor
- **Mapeamento de Contact Points no Registro:** Recomendamos que na próxima tarefa de registro de sensores (`sensors`), desvinculemos `CONTACT POINT COMPRESSION` e `IS SKIDDING` da flag `perEngine`, criando chaves diretas `gear-contact-point-0-compression` (nariz), `:1` (trem esquerdo) e `:2` (trem direito), para que o `PartWearEngine` passe a registrar toque duro no trem dianteiro de forma individualizada.
- **Walkaround (`COVER ON:0..7`):** Como comprovado pela documentação do SDK e comportamentos de áudio/preflight, as variáveis `COVER ON:0` (calços), `COVER ON:2` (pitot) e `COVER ON:6` (amarração de hélice) oferecem enorme imersão operacional ao modo carreira. Recomendamos incluí-las no `sensor_registry.json` assim que realizado o teste de solo Top 25.
