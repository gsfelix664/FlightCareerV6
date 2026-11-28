# Revisão E2A (Claude, 2026-11-24)

**Veredito: APROVADO com ajustes do revisor** (tarefa interrompida a pedido do usuário antes do commit; o revisor terminou).

## Conferido
- Catálogo `mission_catalog.json` v2: 69 tipos, 52 habilitados, 17 desligados (lista da spec), todos com `baseByTier`, perfis, bagagem e destino conforme o plano §§ 9.3, 9.6 e 9.7.1 (amostra: `carga_overnight`, `paraquedismo`, `instrucao_local`, `orgaos`).
- `MissionWeighting` (peso de origem com teto ×12 e base militar ×0,2; destino com piso 1) e `AirportProfileCatalog` (casamento de etiqueta com e sem subtipo): corretos.
- `MissionBoardCore`: sorteio num nível, perfil correlacionado com `bigAirport`, bagagem por pessoa e tripulação, destino ponderado não exclusivo; os dois caminhos (`GenerateMissionsAsync` e `GenerateMissionsInMemory`) chamam o núcleo.
- Build 0 erros; 981 testes passam (rodado pelo revisor).

## Ajustes feitos pelo revisor
1. **Etiqueta de base panorâmica:** os dois caminhos davam `turistico_curado:base` a qualquer aeródromo turístico, inclusive os de papel "destino". Agora `MissionBoardCore.AugmentOriginTags` (único) confere `IsScenicBase`. Teste `AugmentOriginTags_TuristicoDestino_NaoViraBasePanoramica`.
2. **Candidatos do caminho do jogo:** com o longo curso (7.700 NM) a caixa cobria o mundo e o corte sorteava 3.000 aeroportos do planeta, deixando as missões curtas quase sem destino. `MissionGenerator.LimitCandidates` mantém todos até 600 NM (até 2.500) e completa com amostra dos distantes, com ordenação estável antes do sorteio. Testes `LimitCandidates_*`.
3. **Desempenho:** a distância origem → candidato era recalculada para todos os candidatos a cada missão; agora é calculada uma vez por quadro e o filtro de distância vem primeiro (mesmo resultado, mesma ordem de sorteio).
4. **`orgaos`:** a equipe de captação era contada duas vezes (pax e tripulação de apoio); tripulação de apoio zerada, bagagem 3–8 kg por pessoa da equipe.
5. **`board-report`:** padrão de 1.000 para 100 quadros por aeroporto (1.000 levava ~6 min por aeroporto); fazenda do MT trocada de SBOK (no MSFS 2024 é Amontada/CE: código reaproveitado) para SDAO (Fazenda Portela, MT).
6. **Perfis de aeroporto (base, `fix/perfis-posicao-msfs`):** `airport_profiles.py` usava nome e posição do `airports.csv` antigo; 433 de 7.822 códigos divergem > 10 NM do MSFS (códigos reaproveitados, maioria no Brasil). Agora usa os dados do simulador nesses casos.
7. Snapshot `missions_golden.json` regravado depois dos ajustes (mudaram SBGR, SBGL, KJFK, SBMT, SBSJ, SBFI pelas correções 1 e 6).

## Pendências (não bloqueiam o voo de teste; vão para a limpeza E2A-L)
- O `MissionRules` ainda monta a estrutura antiga por tier e repete as missões com os **ids antigos** para manter testes antigos; `GetCalibratedCategories` virou um ajuste fixo ("aeroclube" 70/15/15) que só serve a um teste; `CreateMissionFromSubCategory`, `CreateMissionFromSubCategoryInMemory`, `ResolveG20Hubs` e `ResolveDynamicHubs` (com ids antigos) não são mais chamados. Remover e reescrever os testes que dependem deles sobre o núcleo novo (spec item 9).
- `tools/missions/*.py` (scripts que o Gemini usou para gerar o catálogo e os textos): manter só se forem reaproveitados; senão remover.
- `board-report --coverage` precisa ser mais rápido (amostrar origens) e gravar por aeroporto.
- Relatório do quadro ainda não enviado ao usuário.
