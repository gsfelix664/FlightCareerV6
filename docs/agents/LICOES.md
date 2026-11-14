# Lições das revisões (o executor lê antes de toda tarefa)

Regras curtas tiradas de correções reais pedidas pelo revisor. Uma linha por lição, com a tarefa de origem.
Se a lição virar regra geral do projeto, o revisor move para `AGENTS.md` e apaga daqui.

- Não use `git commit --amend` depois do relatório: o hash citado deixa de existir. Correções = commit novo. (F1.1)
- Unidade de SimVar precisa ser um nome de unidade do SimConnect, sem texto explicativo; em linha com N SimVars e unidades ambíguas, pergunte no relatório. (F1.1)
- Valor que o código precisa ler (ex.: SIMULATION RATE) tem que estar no layout de leitura de fato; confira de onde a chave vem antes de usá-la. (F1.2)
- Eventos assíncronos do SimConnect (exceções, dados): acumule e processe uma vez; não reconstrua estado dentro do handler de exceção. (F1.2)
- Antes de usar um nome de SimVar, confira na documentação do SDK; nome inexistente não dá erro de compilação, só desloca os dados em voo. (fix PARTIAL PANEL STATIC)
