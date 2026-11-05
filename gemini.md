# Registro de Progresso do Projeto FlightCareer V6

## Diretrizes e Arquitetura do Projeto

### Tecnologias Principais
- **Linguagem**: C# (.NET 10.0)
- **Interface**: WPF (Windows Presentation Foundation) com Material Design.
- **Padrão de Projeto**: MVVM (Model-View-ViewModel) utilizando o `CommunityToolkit.Mvvm`.
- **Banco de Dados**: Entity Framework Core com SQLite para persistência local.
- **Integração Externa**: Microsoft Flight Simulator (MSFS 2020/2024) via SimConnect SDK.

### Estrutura da Solução
- **FlightCareer.Domain**: Contém as entidades de negócio (Pilot, Mission, Aircraft, Airport), interfaces de repositório e utilitários de lógica pura (ex: cálculos de distância).
- **FlightCareer.Infrastructure**: Implementação da persistência (AppDbContext, Repositories), serviços de dados (MissionGenerator) e seed de dados via CSV.
- **FlightCareer.UI**: Camada de apresentação contendo as Views (XAML) e ViewModels, além de serviços específicos da interface como `SimConnectService` e `FlightMonitor`.

### Fluxo de Funcionamento (Loop Principal)
1. **Login/Perfil**: O piloto faz login ou cria um perfil. O sistema carrega os dados do aeroporto atual do piloto.
2. **Quadro de Missões**: O `MissionGenerator` gera ofertas baseadas no Tier do aeroporto atual. O piloto visualiza os detalhes (Pax, Peso, Recompensa) e "Aceita" as missões.
3. **Briefing**: As missões aceitas são movidas para o manifesto. O sistema conecta ao simulador, verifica a posição da aeronave e os pesos configurados.
4. **Voo**: O `FlightMonitor` observa os eventos do SimConnect (Motores, Ground Speed, Parking Brake) para iniciar e finalizar o registro do voo automaticamente.
5. **Debriefing**: Ao pousar e desligar os motores, o voo é processado, as missões são concluídas, e o piloto recebe XP, Dinheiro e atualização de horas.

---

## 2026-02-15 - Briefing e Detalhes da Missão
- **Tela de Briefing**: Implementada a visualização de missões aceitas e informações em tempo real do simulador.
- **SimConnect**: Adicionada lógica de reconexão automática e exibição de status no cabeçalho.
- **Localização**: Implementada busca automática do aeroporto mais próximo (ICAO, Cidade, País) baseado nas coordenadas atuais.
- **Detalhes da Missão**:
  - Adicionados campos para Nº de Passageiros, Peso da Bagagem e Peso da Carga na entidade `Mission`.
  - Implementada visualização detalhada da distribuição de peso na tela de Missões (lista vertical com Pax, Bagagem, Carga e Piloto).
  - Cálculo automático do peso total do voo (incluindo piloto fixo em 170 lbs).
- **Gerenciamento de Missões**:
  - Adicionado botão "Excluir Missão" na tela de Briefing, permitindo retornar a missão para a lista de ofertas.
  - Renomeada coluna "CARGA" para "PESO (LBS)" no grid de briefing.

## 2026-02-15 - Logística Persistente e Escalas (Multi-Leg)
- **Nome da Companhia**: Adicionado campo para definir o nome da empresa na criação do piloto e exibição no menu lateral.
- **Número do Voo Personalizado**: Novo padrão `XY1234`, onde `XY` são as iniciais da companhia e `1234` é um número aleatório (1000-9999).
- **Rota Dinâmica**: O cabeçalho do briefing agora mostra `[Local Atual] -> [Destino da Missão Selecionada]`.
- **Escalas e Persistência**:
  - A localização da carga (`OriginICAO`) agora é persistente.
  - Se uma missão for cancelada em um aeroporto diferente do original, ela retorna ao mercado naquele aeroporto.
  - Implementado recálculo automático de recompensa e distância baseado na nova localização da carga.
- **Interface e Manifesto**:
  - Implementado **Manifesto Detalhado** com listagem individual por missão no formato: `X PAX BOARDED Y LBS BAGS Z LBS CARGO`.
  - Adicionado **Resumo do Manifesto** consolidando Total de PAX, Total de Carga/Bagagem e Peso Total (incluindo piloto).
  - Adicionado aviso de confirmação ao cancelar missões, alertando sobre a perda de pagamento em caso de transporte parcial.

## 2026-02-15 - Reporte de Voo e Sistema Financeiro
- **Novo Fluxo de Monitoramento**: Gravação inicia ao soltar o Parking Brake (com motores ligados) e encerra 5s após desligar motores e setar PB no destino.
- **Conclusão Parcial de Missões**: Implementada lógica de escala onde apenas cargas/pax do destino atual são descarregados e pagos. Cargas restantes permanecem no manifesto com origem atualizada.
- **Módulo Financeiro**:
  - Implementada tela de Financeiro com resumo de Receitas, Despesas e Lucro Líquido.
  - Histórico detalhado de transações persistido no banco de dados.
  - Registro automático de receita ao concluir missões.
- **Interface**: Adicionado botão "Financeiro" no menu de Carreira e nova visualização de transações.

## 2026-02-15 - Mercado de Aeronaves, Economia e Estabilidade
- **Redesign do Mercado**: Cards listados verticalmente com expansão horizontal. Hierarquia: Nome/Prefixo -> Condição Geral (%) -> Valor.
- **Estabilidade do Banco de Dados**: 
  - Corrigido erro de concorrência ("0 rows affected") ao mudar o ciclo de vida do `AppDbContext` e Repositories para **Singleton**.
  - Lógica de compra agora é atômica (desconto de saldo + adição ao hangar + registro financeiro em uma única transação).
- **Logística de Compra**:
  - Aeronaves mantêm prefixo, condições, TTAF e combustível originais do anúncio.
  - O valor de venda inclui o custo do combustível nos tanques.
- **Economia Dinâmica**:
  - **Dolarização**: Interface forçada para cultura `en-US` ($).
  - **FuelPriceService**: Implementada cotação diária com inflação (dobra em 10 anos) e variação aleatória de +/- 3%.
- **Especificações Técnicas**: Painel detalhado com Alcance, Vel. Cruzeiro, Carga Máx, PAX, Pista Mínima e Consumo.

## 2026-02-22 - Refatoração do Gerador de Missões e Instrução
- **Organização do Código**: Refatorado o `switch` de categorias no `MissionGenerator` para agrupamento lógico (1.x, 2.x, 3.x, 4.x), facilitando a manutenção.
- **Novas Categorias**:
  - Adicionada subcategoria `3.2.3` mapeada para `AgroPoints`.
  - Criada a categoria **`InstructionHubs`** no `DestinationProfile`.
  - Mapeamento de aeroportos de instrução brasileiros (SBMT, SBJD, SBJR, SBBI, SBBH, etc.) no perfil "BR".
- **Lógica de Instrução**:
  - Subcategorias `3.4.1` (Instrução Local) e `3.4.3` (Cheque) agora utilizam `InstructionHubs` para priorizar destinos de treinamento.
  - `3.4.1` configurada para gerar voos locais (origem -> origem) com 10 NM de distância.
  - Implementada segurança com `?? new List<string>()` para evitar falhas em países sem hubs de instrução definidos.
- **Lógica de Pesos e Bagagem**:
  - **Universalização**: Removidas todas as mais de 100 linhas de exceções e lógicas manuais por ID no `MissionGenerator`.
  - **Bagagem**: Implementada regra universal de sorteio entre **0 e 50 lbs por passageiro** para todas as categorias.
  - **Carga**: Definida como **0** para voos puramente de passageiros (sem `LbsRules`) e baseada no sorteio da regra para voos de carga.
- **Interface e UI**:
  - Modificado o grid de **Missões Disponíveis** para incluir a coluna **PESO**, exibindo o peso total (PayloadLbs) de cada missão.
  - Reorganizados os tamanhos das colunas (Destino, Distância, Pagamento, XP, Peso) para melhor legibilidade.
  - **Briefing de Voo**: Removidas as informações de Combustível, Peso Vazio e Peso Total do simulador para simplificar a interface e focar nos dados da missão.
  - **Manifesto**: Reformulada a exibição do manifesto detalhado para listar PAX, BAGS e CARGO em linhas separadas por missão, com separadores visuais.
  - **Destaque de Decolagem**: Renomeado o campo de peso total para **"TOW - Takeoff Weight (AJUSTAR NO SIMULADOR)"** em destaque amarelo para configuração manual no simulador.
- **Segurança e Persistência**:
  - **Cancelamento de Voo**: Implementada a funcionalidade de "Cancelar Voo" no Briefing, que substitui o botão de início enquanto o monitoramento está ativo.
  - **Anti Save-Scumming**: Se o voo for cancelado após o início (motores ligados/PB solto), o sistema salva automaticamente em tempo real a localização atual (aeroporto mais próximo), combustível e o desgaste da aeronave no banco de dados.
  - **Persistência de Missões**: As missões ativas agora têm seu progresso salvo a cada 60 segundos. A origem (`OriginICAO`) da carga/pax é atualizada para a posição atual e a distância restante é recalculada, permitindo que o piloto continue a missão de onde parou em caso de queda do simulador.
  - **Sincronização**: O estado do monitoramento é sincronizado entre as ViewModels para garantir que o botão de cancelamento esteja sempre disponível durante o voo.

## 2026-02-23 - Estabilidade, Manifesto e Lógica de Escalas (Multi-Leg)
- **Correção de Bugs de Compilação**:
  - Resolvido erro de acesso a tipos anuláveis (`SimData?`) no `FlightMonitor`.
  - Corrigida referência incorreta de propriedade em `MainViewModel` (`LastData` para `CurrentData`).
  - Adicionadas diretivas `using Microsoft.EntityFrameworkCore;` ausentes para operações assíncronas de banco de dados.
- **Persistência de Manifesto (Anti-Loss)**:
  - Implementado o carregamento automático de missões aceitas no `InitializeForPilotAsync`. O manifesto agora persiste entre sessões do aplicativo.
  - Sincronização automática dos dados da aeronave no Hangar (Localização e Combustível) ao carregar o perfil.
  - **Logística de Pouso de Emergência e Cancelamento**:
  - **Novo Sistema de Falhas**: Refatorada a lógica de falhas aleatórias para Motor, Pneus e Fuselagem. Agora, as checagens de falha ocorrem apenas a cada **10 minutos** de voo quando a condição de um componente está abaixo de **30%**, tornando as panes eventos mais previsíveis e menos baseados em "sorte por segundo".
  - **Monitoramento de Motor Avançado**:
    - **Pistão**: Implementada detecção de superaquecimento (CHT > 230°C), baixa pressão de óleo (< 20 psi) e alta temperatura de óleo (> 110°C), com danos acelerados.
    - **Jato/Turboprop**: Adicionado monitoramento de ITT (> 800°C) e N1 (> 100%) para simular fadiga de turbina.
  - **Danos em Pneus e Fuselagem**:
    - **Pneus**: Desgaste dinâmico baseado na intensidade da frenagem (0-32K) e velocidade de taxi (> 25 kts).
    - **Fuselagem**: Estresse por Força G realista (+3.8/-1.5), alerta de Overspeed (Vne) e detecção de Crash Sequence (Perda Total).
  - **Cancelamento Inteligente**: Ao cancelar um voo (via botão ou queda do sim), o sistema identifica o aeroporto mais próximo e move automaticamente todas as cargas e passageiros para essa nova localização.
  - **Validação de Rota**: Corrigido o erro "LOCALIZAÇÃO INCORRETA" no Briefing após pousos intermediários. O sistema agora valida a nova perna a partir do aeroporto atual de onde a aeronave se encontra, não mais do aeroporto de origem original da missão.
  - **Preservação de Receita**: Missões em progresso mantêm seus valores originais ao serem movidas para escalas, a menos que sejam explicitamente removidas pelo piloto (onde ocorre o recálculo baseado na nova distância).
- **Segurança de Threads**:
  - Refatorado `UpdateHangarStateAsync` para garantir que atualizações em segundo plano (a cada 60s) atualizem corretamente tanto o banco de dados quanto os objetos em memória, evitando dessincronia na interface.
  - Implementada proteção contra `ObjectDisposedException` em tarefas assíncronas de gravação de estado.

## 2026-03-01 - Mapa Interativo e Planejamento de Voo
- **Integração de Mapa**: Adicionado o componente `GMap.NET.WindowsPresentation` (OpenStreetMap) na tela de Briefing para visualização interativa do plano de voo sem necessidade de chaves de API.
- **Localização e Destino**: 
  - Exibição de um marcador azul em tempo real para a posição atual da aeronave (coordenadas obtidas do SimConnect).
  - Linha tracejada magenta conectando dinamicamente a localização atual ao destino da missão selecionada.
- **Sistema de Waypoints**:
  - Permite a adição de marcadores personalizados (waypoints) clicando com o botão direito no mapa.
  - Desenho automático de rota (linha laranja) interligando os waypoints sequencialmente.
  - Cálculo instantâneo da distância total em Milhas Náuticas (NM) entre os pontos inseridos, exibida em um overlay superior.
  - Botão "LIMPAR" para resetar o planejamento de rota de forma prática.
- **Visualização Auxiliar (Debug)**: O novo mapa também foi incorporado ao painel de Debug do sistema, exibindo dinamicamente os aeroportos próximos à aeronave na forma de círculos brancos. Isso facilita identificar e teleportar a aeronave para aeródromos na região durante os testes e configurações de desenvolvimento.

## 2026-11-05 - Criação do Repositório Git e Sincronização com GitHub
- **Repositório Oficial**: Criado o novo repositório dedicado no GitHub: `https://github.com/gsfelix664/FlightCareerV6`.
- **Configuração de Versionamento**:
  - Adicionado `.gitignore` cobrindo artefatos de compilação (.NET/VS), bancos locais (`*.db`, `*.db-wal`), arquivos de usuário e backups da pasta `antigos/`.
  - Criado `README.md` detalhado documentando os recursos, arquitetura (Clean Architecture, MVVM, EF Core) e instruções de execução.
  - Inicializada branch `main` e realizado o primeiro push para o GitHub.
