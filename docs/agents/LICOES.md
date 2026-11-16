# Lições das revisões (o executor lê antes de toda tarefa)

Regras curtas tiradas de correções reais pedidas pelo revisor. Uma linha por lição, com a tarefa de origem.
Se a lição virar regra geral do projeto, o revisor move para `AGENTS.md` e apaga daqui.

- Não use `git commit --amend` depois do relatório: o hash citado deixa de existir. Correções = commit novo. (F1.1)
- Unidade de SimVar precisa ser um nome de unidade do SimConnect, sem texto explicativo; em linha com N SimVars e unidades ambíguas, pergunte no relatório. (F1.1)
- Valor que o código precisa ler (ex.: SIMULATION RATE) tem que estar no layout de leitura de fato; confira de onde a chave vem antes de usá-la. (F1.2)
- Eventos assíncronos do SimConnect (exceções, dados): acumule e processe uma vez; não reconstrua estado dentro do handler de exceção. (F1.2)
- Antes de usar um nome de SimVar, confira na documentação do SDK; nome inexistente não dá erro de compilação, só desloca os dados em voo. (fix PARTIAL PANEL STATIC)
- 2026-11-15 (F4.2e): SimVar de contagem pode não contar o zero — FLAPS NUM HANDLE POSITIONS conta só os dentes estendidos. Confira o significado com uma captura antes de montar fórmula.
- 2026-11-15 (F2.4): antes de limpar estado em StopMonitoring, confira quem chama StopMonitoring antes de salvar (FinalizeFlightAsync salva depois); estado que ainda vai ser gravado não pode ser zerado ali.
- 2026-11-15 (F2.4b-CAL): lista de diagnósticos lida a cada tick precisa ser drenada (entregar e esvaziar), senão a mesma mensagem vai ao blackbox em todo tick. Script Python que imprime símbolos (✅ ❌) precisa forçar UTF-8 na saída: o console do Windows usa cp1252.
- 2026-11-15 (FIX-PARTS-SENSORS): sensores PerEngine geram chaves 'chave:n' no SensorLayout; regras contínuas que usam chaves sem índice precisam resolver se o frame traz chave exata ou por motor, avaliar cada motor separadamente com chave 'regra#n' e aplicar estresse a (PartId, EngineIndex). Peças compartilhadas (EngineIndex 0) recebem o estresse do pior motor sem somar.
- 2026-11-15 (FIX-REPLAY-BUGS): Regras com direction Below (ex: pressão mínima de óleo) exigem motor funcionando (requiresEngineRunning + engineRunningDelaySeconds) para não disparar falso positivo durante pré-voo ou corte; choque térmico de cilindro é taxa de resfriamento (°C/min com direction Above), não CHT baixa absoluta; sobrecarga de alternador exige alternador ativo com motor em combustão e queda de tensão no barramento (sag em [0.88, 0.95)), pois barramento regulado (>=0.95) indica capacidade suficiente sem estresse.
- 2026-11-15 (RESEARCH-SIMVARS): 'não achei na documentação' não prova que a SimVar não existe. Coluna presente no CSV do REC = nome aceito pelo MSFS (o leitor remove as rejeitadas). Use os logs reais como prova antes de declarar um nome inexistente.
- 2026-11-15 (FIX-START-EVENTS): Eventos operacionais de partida e transições antes da ativação do voo devem ser observados em modo pré-voo (ObservePreflight) sem aplicar desgaste natural nem tempo simulado contínuo; excedências operacionais só devem ser emitidas para o blackbox quando a faixa ativa tiver efeito na peça (Multiplier > 1 ou LossPerHourPct > 0) ou inspeção.
- 2026-11-16 (REGRAS-C152): no gerador de voos sintéticos, abusos que mexem na mesma grandeza (G±, estado elétrico) ou que encerram o voo (cancelamento) não podem cair no mesmo instante nem antes do evento esperado; separe no tempo ou torne exclusivos.
- 2026-11-16 (FUZZ-ALL): comando de reprodução impresso tem que ser executado uma vez e conferido: se o voo depende do índice da corrida (não só da semente), o comando precisa recriar o mesmo índice.
