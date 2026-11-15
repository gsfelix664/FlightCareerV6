# RESEARCH-SIMVARS — Pesquisa de Variáveis em Projetos Abertos + SDK 2024

**Branch:** docs/pesquisa-variaveis · **Commit:** a definir · **Data:** 2026-11-15

## Plano (antes de codar)
- Investigar a documentação oficial do SDK MSFS 2024 (APIs de SimVars e SimConnect) e os repositórios de referência locais (`FsConnect`, `MobiFlight-Connector`, `msfs2020-go`, `aircraft` FlyByWire).
- Conferir cada uma das variáveis da lista fornecida na especificação quanto à existência, obsolescência, unidade, índice, modo de leitura/escrita, presença no registro do FlightCareer, evidências em projetos abertos e utilidade no app.
- Desmascarar e comprovar os nomes incorretos ou inventados (destaque para `PARTIAL PANEL STATIC`, `AIRCRAFT STRESS DAMAGE` e `ELECTRICAL LINE BREAKER PULLED`).
- Investigar como projetos abertos leem LVars (WASM no MSFS 2020 vs SimConnect direto no MSFS 2024), detectam toque/pouso e modelam freios/hidráulica.
- Selecionar 20 variáveis prioritárias com passos operacionais claros para o próximo teste de solo.
- Entregar a pesquisa em `docs/plans/sensor-research/variaveis-referencias.md` sem alterar código.

## O que foi feito
- `docs/plans/sensor-research/variaveis-referencias.md`:
  - Tabela completa de 42 variáveis cobrindo motor, elétrico, hidráulica, pneumática, trem/estrutura e walkaround do MSFS 2024, confrontando o SDK 2024 e os projetos abertos.
  - Seção de análise com as provas de nomes inexistentes ou com sintaxe incorreta (`PARTIAL PANEL STATIC`, `AIRCRAFT STRESS DAMAGE`, `ELECTRICAL LINE BREAKER PULLED`, índices de barramento e parciais).
  - Seção técnica detalhando como projetos abertos implementam LVars (destaque para o suporte nativo a `L:VARIABLE_NAME` via `SimConnect_AddToDataDefinition` no MSFS 2024), como FBW trata WoW por compressão de contact points, modelagem termodinâmica de freios e hidráulica realista.
  - Seleção de 20 candidatas para o próximo teste de solo com o roteiro de verificação de cada uma.
- `docs/CHANGELOG.md`: Adicionada entrada da pesquisa realizada pelo Gemini.

## Critérios de aceite
- [x] Conferir todas as variáveis da lista na documentação oficial do SDK MSFS 2024 (com URLs de cada página oficial) — Verificado na seção 1 do relatório.
- [x] Verificar evidências em projetos abertos (`FsConnect`, `MobiFlight`, `msfs2020-go`, `aircraft`) com arquivo e linha citados — Verificado em varredura automatizada nos 4 repositórios.
- [x] Registrar que o repositório `fseconomy/fse-simconnect` não existe no GitHub — Registrado no cabeçalho e na documentação.
- [x] Não mexer em código (`sensor_registry.json`, `wear_rules.json`, C# inalterados) — Nenhum arquivo de código ou catálogo de regras foi modificado.
- [x] Identificar e provar nomes errados/inventados — Comprovado para `PARTIAL PANEL STATIC`, `AIRCRAFT STRESS DAMAGE`, `ELECTRICAL LINE BREAKER PULLED`, `ELECTRICAL BUS VOLTAGE:name` e `PARTIAL PANEL COMM:n`.
- [x] Análise de como projetos abertos leem LVars e novidade do SimConnect 2024 — Detalhado com sintaxe C++ oficial (`SimConnect_AddToDataDefinition` com `FLOAT64`).
- [x] Relação das 20 candidatas ao próximo teste de solo com passo a passo — 20 variáveis com testes acionáveis descritos na seção 4.
- [x] Executar build e testes para garantir que nada foi quebrado — `dotnet test src/FlightCareer.Tests` executado.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos (código não foi alterado).
- `dotnet test src/FlightCareer.Tests`: 319 aprovados, 0 falhas.

## Não testado
- O teste prático das SimVars de walkaround (`COVER ON:0..2`) e das variáveis do sistema modular pneumático depende de execução física do usuário no MSFS 2024 (definido como roteiro para o próximo teste de solo).

## Dúvidas e decisões para o revisor
- **Leitura nativa de LVars no MSFS 2024:** A documentação oficial do SDK confirma que `SimConnect_AddToDataDefinition` suporta diretamente `"L:VARIABLE_NAME"` com tipo `SIMCONNECT_DATATYPE_FLOAT64` e unidade `"number"`, sem necessidade de módulo WASM intermediário. Isso abre caminho para ler `L:IS_AVATAR` e flags de sistemas de aeronaves de terceiros diretamente pelo `SimConnectService` no futuro, caso aprovado pelo revisor.
- **Walkaround (`COVER ON:0..2`):** É uma excelente adição para integrar a inspeção externa pré-voo com penalidades (ex.: decolar com calços, voar com pitot coberto gerando indicação zero de velocímetro). Recomendamos incluí-las no `sensor_registry.json` assim que validadas no teste de solo.
