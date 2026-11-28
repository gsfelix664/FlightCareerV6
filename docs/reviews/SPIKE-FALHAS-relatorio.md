# SPIKE-FALHAS — Falhar o Sistema sem Mexer no Comando do Piloto (MSFS 2024)

**Branch:** docs/spike-falhas-sem-comando · **Commit:** ba7e26e · **Data:** 2026-11-15

## Plano (antes de codar)
- Pesquisar na documentação oficial do SDK do MSFS 2024 (`docs.flightsimulator.com/msfs2024/retail/`) as opções de aplicação de falhas para flaps, alternador/sistema elétrico, magnetos e failures em geral sem mover chaves/comandos do piloto.
- Analisar os impactos e limitações de cada abordagem (SimConnect, modelo de voo, persistência após reload e Model Behaviors do cockpit).
- Mapear cada pane do `DefectCatalog` em uma tabela comparativa com URL oficial, opção sem mexer no comando, limitações e nível de confiança.
- Formular recomendações técnicas fundamentadas e roteiro prático de testes em voo para o Cessna 152.
- Entregar `docs/plans/spike-falhas-sem-comando.md`, relatório e entrada no CHANGELOG, sem alterar nenhum arquivo de código.

## O que foi feito
- `docs/plans/spike-falhas-sem-comando.md`: criado com o levantamento exaustivo dos 4 tópicos do spike, tabela comparativa com as 12 panes do `DefectCatalog` com efeito/enforcement no sim, recomendações técnicas por subsistema e roteiro de teste em voo para o C152 no MSFS 2024.
- `docs/CHANGELOG.md`: adicionada entrada resumindo a conclusão do spike.
- Nenhum arquivo de código foi alterado.

## Critérios de aceite
- [x] Flaps: pesquisadas SimVars de superfície (`TRAILING EDGE FLAPS LEFT/RIGHT PERCENT` é Settable: Y), Danos por velocidade (`FLAP DAMAGE BY SPEED` é Read-Only) e viabilidade de alavanca livre vs. superfície (analisados conflitos de sobrescrita/jitter e corte de `CIRCUIT_FLAP_MOTOR`) — verificado na doc oficial.
- [x] Alternador / Sistema elétrico: pesquisados eventos de barramento e disjuntores (`ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`, `ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE`, `ELECTRICAL_CIRCUIT_TOGGLE`) e SimVars (`ELECTRICAL GENALT BUS AMPS:1`, `ELECTRICAL BATTERY BUS AMPS`); identificado funcionamento no C152 e necessidade de chamada com múltiplos parâmetros (`SimConnect_TransmitClientEvent_EX1`) — verificado na doc oficial.
- [x] Magnetos: investigada existência de falhas nativas por índice ou SimVars de pane; confirmado acoplamento rígido entre `RECIP ENG LEFT/RIGHT MAGNETO` e os Model Behaviors da chave de ignição no cockpit 3D — verificado na doc oficial.
- [x] Failures do MSFS 2024: confirmado que o Wear and Tear nativo possui SimVars estritamente Read-Only (`WEAR AND TEAR LEVEL:<id>`, `IS FAILED:<id>`), sem nova API SimConnect de Failure Manager, mantendo-se os eventos de toggle legados — verificado na doc oficial.
- [x] Tabela comparativa para todas as panes com efeito no sim contendo como é hoje, opção sem comando, limitações e confiança com URLs oficiais — estruturada no documento do plano.
- [x] Roteiro de teste em voo no C152 detalhando ações e leituras esperadas no cockpit — estruturado no documento do plano.
- [x] Nenhum arquivo de código alterado — verificado via `git status`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 13 avisos pré-existentes de pacotes vulneráveis conhecidos.
- `dotnet test src/FlightCareer.Tests`: 241 aprovados, 0 falhas, 0 pulados.

## Não testado
- Os testes em voo no MSFS 2024 propostos no roteiro do documento dependem do usuário no simulador para validação empírica.

## Dúvidas e decisões para o revisor
- **Alternador:** A substituição de `ALTERNATOR_OFF` por `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` é altamente recomendada para o realismo da carreira (a chave fica ON e a bateria descarrega gradualmente). Para sua implementação numa fase futura, o `SimConnectService` precisará expor a chamada estendida do SimConnect (`_EX1` ou P/Invoke direto) para suportar os 2 parâmetros (`bus_index` e `alternator_index`).
- **Flaps e Magnetos:** Conforme documentado no relatório técnico, recomenda-se manter a implementação atual para Flaps (alavanca travada com enforcement espaçado) e Magnetos (chave posicionada no magneto são), devido às limitações do solver físico de superfícies e dos templates de cockpit do simulador.
