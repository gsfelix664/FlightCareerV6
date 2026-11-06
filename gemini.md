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

## Antigravity Autonomous Lead Developer & Sub-Agent Orchestrator

Você atua como Arquiteto de Software Sênior e Orquestrador Autônomo de Agentes para este repositório via Antigravity CLI. Sua prioridade máxima é entregar código pronto para produção, seguro, coeso e validado, minimizando retrabalho e quebras de build.

### 1. Modos de Operação (Sub-Agentes)
Sempre declare e assuma mentalmente o papel correspondente antes de executar a tarefa:
* **@architect**: Mapeia dependências, schemas de dados, contratos de API e arquitetura antes de qualquer alteração estrutural.
* **@coder**: Implementa código limpo, tipado e com separação clara de responsabilidades. Nunca remove blocos de código funcionais sem justificativa técnica.
* **@reviewer**: Analisa diffs em busca de falhas de segurança, gargalos de performance, vazamentos de memória e conformidade com os padrões do projeto.
* **@qa**: Roda e gera testes unitários/integração, validação de lint e comandos de build.
* **@backend**: Endpoints, serviços de aplicação, regras de negócio, telemetria, desgaste e tratamento de exceções.
* **@frontend**: Componentes de interface (XAML/WPF), binding de dados MVVM, controles e renderização.
* **@database**: Schemas, entidades relacionais, migrations, persistência EF Core e SQLite.

### 2. Protocolo de Execução Estrita (Loop de Ação)
Ao receber um comando ou demanda, execute sempre na seguinte ordem:
1. **Investigar primeiro (Read-First)**: Inspecione arquivos afetados, schemas e assinaturas existentes antes de propor edições.
2. **Edição Cirúrgica**: Edite apenas os trechos estritamente necessários, preservando formatação e convenções.
3. **Validação Automática (Self-Check)**: Após cada alteração, execute imediatamente os comandos de compilação/build (`dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj`). Resolva qualquer erro antes de responder.
4. **Relatório de Saída**: Liste apenas os arquivos modificados, a causa-raiz tratada e o status da compilação/teste.

### 3. Diretrizes de Segurança & Sanidade
* **Sem mocks cegos**: Não substitua implementações reais por stubs ou placeholders a menos que solicitado explicitamente.
* **Tratamento de Exceções**: Todo I/O, queries e chamadas de rede devem ter tratamento resiliente de erros e logs adequados.
* **Sem confirmações redundantes**: Execute as correções e passos necessários sem parar para pedir aprovação de comandos de rotina.

### 4. Orquestração Autônoma e Gestão Independente de Personas
Você NÃO deve esperar que o usuário mencione explicitamente `@agente`. Você atua como o **Orquestrador Líder Autônomo** e tem total autonomia para gerenciar e alternar as personas de forma automática e independente:

1. **Ativação e Alternância Automática**:
   - Analise a intenção do usuário e decomponha a demanda nas personas necessárias sem exigir comandos manuais.
   - **@architect**: Acionado automaticamente em decisões estruturais, novos módulos, interfaces entre camadas ou refatorações complexas.
   - **@database**: Acionado automaticamente quando houver modificação de entidades, schemas, persistência EF Core ou seeds.
   - **@backend**: Acionado automaticamente em regras de negócio, serviços (`MissionGenerator`, `FlightMonitor`, etc.), telemetria SimConnect e cálculos.
   - **@frontend**: Acionado automaticamente em criação/edição de telas XAML, bindings MVVM, estilos ou controles de mapa.
   - **@reviewer**: Acionado automaticamente para validar segurança, desempenho e conformidade de diffs.
   - **@qa**: Acionado automaticamente após qualquer modificação para compilar e testar a solução (`dotnet build`).

2. **Pipeline Multi-Agente Transparente**:
   - Quando uma demanda cruzar mais de uma área (ex: novo recurso com banco + tela + regra de negócio), execute a cadeia completa de forma sequencial e declare brevemente a persona em ação no início de cada etapa (ex: `[🎭 Atuando como @architect]`, `[🎭 Atuando como @backend]`, `[🎭 Atuando como @qa]`).
   - Siga rigorosamente o escopo e regras de cada persona definidas em `.antigravity/agents/<nome>.md`.

3. **Ciclo de Resolução Autônomo**:
   - Execute o ciclo completo (Investigação ➡️ Planejamento ➡️ Implementação ➡️ Build/Correção ➡️ Conclusão) de ponta a ponta sem pausas desnecessárias para pedir confirmações triviais.

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


## 2026-11-05 - Sistema de Progressão Regulatória em 5 Tiers, TBO e Manutenção
- **Progressão da Empresa (5 Tiers Operacionais)**:
  - Implementado enum `CompanyTier`:
    - **Tier 1 (CIAC / Escola de Aviação)**: Foco na instrução de pilotos (PP/PC/INVA), voos locais e aeronaves de treinamento.
    - **Tier 2 (Serviços Aéreos Especializados - SAE)**: Operações agro, reboque de faixas, foto aérea e pequenas cargas com conversão de assentos.
    - **Tier 3 (Táxi Aéreo / Charter & Carga Fechada)**: Transporte não regular sob demanda, bimotores, turboélices e jatos leves com gestão SGSO/DTS.
    - **Tier 4 (Aviação Regional & Sub-regulares)**: Rotas regulares diárias/semanais conectando cidades ao hub, com suporte DOV e ground handling.
    - **Tier 5 (Grande Linha Aérea Regular)**: Malha de alta densidade nacional/internacional e transporte de cargas heavy (widebody).
- **Manutenção Regulatória & TBO**:
  - Implementado controle de TBO (Time Between Overhaul) de motor (1.800h padrão) em `PilotAircraft`.
  - Adicionado controle das inspeções regulatórias periódicas obrigatórias de **50 Horas** e **100 Horas**.
  - Interdição automática da aeronave caso o TBO ou a inspeção de 100 horas atinjam zero, impedindo decolagem até a realização dos serviços.
  - Comandos dedicados no Hangar para:
    - **Inspeção de 50h** ($850)
    - **Inspeção de 100h** ($2.500)
    - **Retífica Geral de Motor / Overhaul TBO** ($25.000 ou 20% do valor da aeronave)
    - **Conversão de Cabine Pax/Carga** ($1.200) permitindo remover/adicionar assentos.
- **Homologação e Restrições de Missões**:
  - O `MissionGenerator` classifica missões por categoria e atribui o `RequiredTier` correspondente.
  - A interface bloqueia o aceite e o início de voos de missões que excedam a certificação atual da companhia.
  - Criado botão de evolução e consulta de requisitos regulatórios no badge de Tier do perfil lateral.
- **Migração Dinâmica de Banco de Dados**:
  - Adicionado método `EnsureSchemaUpdatedAsync` em `AppDbContext` para atualizar automaticamente a estrutura de bancos SQLite existentes sem quebrar dados de pilotos anteriores.

## 2026-11-06 - Nomenclatura de Categorias de Piloto e Missões de Instrução em Tiers 2, 3 e 4
- **Diferenciação Semântica (Categorias vs Tiers)**:
  - Renomeada a progressão do piloto/empresa de "Tier" para **"Categoria"** (`PilotCategory`), reservando o termo "Tier" exclusivamente para classificação de porte de aeroportos (Tiers 1 a 4).
  - Categorias:
    - **Categoria 1**: CIAC / Escola de Aviação
    - **Categoria 2**: Serviços Especializados (SAE)
    - **Categoria 3**: Táxi Aéreo & Charter
    - **Categoria 4**: Linha Aérea Regional
    - **Categoria 5**: Linha Aérea Regular
  - Retrocompatibilidade preservada com colunas antigas e aliases de propriedades (`CompanyTier`, `RequiredTier`) para não quebrar persistência de dados nem bindings legados.
- **Geração de Missões de Instrução / Treinamento**:
  - **Tier 1 (Grandes Hubs)**: Mantido em **0%** de instrução (aeroportos internacionais de grande porte não geram missões de aeroclube/instrução).
  - **Tier 2 (Regionais Médios)**: Adicionada categoria de Treinamento com peso de **5%** (Subcategorias 3.4.1 Local, 3.4.2 Navegação e 3.4.3 Cheque), ajustando Linha Aérea Regional de 50% para 45%.
  - **Tier 3 (Municipais / Interior)**: Mantida a categoria de Treinamento com peso de **5%**.
  - **Tier 4 (Pistas Remotas / Rurais)**: Adicionada categoria de Treinamento com peso de **5%**, ajustando Logística de Sede de 60% para 55%.
- **Mapeamento de Probabilidades de Instrução**:
  - Aeroportos Tiers 2, 3 e 4 possuem agora exatamente 5% de probabilidade em cada sorteio de missão de gerar Treinamento/Instrução.
  - Aeródromos da lista `InstructionHubs` (ex: SBMT, SBJD, SBJR, SBBI, SBBH) geram conforme seu respectivo Tier (Tier 2 ou 3) com 5% de peso base, mantendo a opção de 20% para implementação futura.

## 2026-11-06 - Sistema de Voo de Translado / Reposicionamento (Ferry Flight)
- **Recurso Anti-Bloqueio de Piloto**:
  - Implementado sistema de translado livre que permite ao piloto reposicionar sua aeronave e empresa para qualquer aeroporto do mundo (`_context.Airports`).
  - Previne que pilotos novatos (Categoria 1) fiquem presos em aeródromos grandes (Tier 1) sem missões compatíveis.
- **Opção 1: Voo no Simulador (Voo Real no Manche)**:
  - Gera missão de translado técnico (`PayloadLbs = 0`, sem passageiros/carga) com destino escolhido e rota traçada no Briefing e mapa.
  - Custo direto reduzido apenas ao combustível gasto nos tanques da aeronave.
  - Computa horas no perfil do piloto (`TotalHours`), desgaste da aeronave e concede XP integral.
- **Opção 2: Translado Imediato (Pular / Piloto Terceirizado Contratado)**:
  - Custo financeiro calculado com sobretaxa (`(Combustível * 1.5) + Diária Piloto Comercial Ferry + Taxas de Despacho`).
  - Custo total significativamente superior ao voo manual para desencorajar pulos artificiais e incentivar a simulação de voo real.
  - Não concede horas de voo no perfil do piloto e não concede XP.
  - Aplica as horas correspondentes à célula, desgaste de motor e TBO da aeronave.
- **Interface e Usabilidade**:
  - Nova janela modal dedicada [`FerryFlightWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Views/FerryFlightWindow.xaml) com visualização comparativa lado a lado dos custos e benefícios.
  - Botão de acesso rápido "VOO DE TRANSLADO (FERRY)" integrado no topo do Painel de Controle e no menu lateral de Operações.

## 2026-11-06 - Redesign do Quadro de Missões (Split-Screen Tático, Filtros e Pré-Despacho)
- **Layout Split-Screen Assimétrico (58/42)**:
  - Tabela detalhada de contratos à esquerda (58%) e Viewport de mapa tático à direita (42%) com sincronização bidirecional em tempo real.
- **Barra Superior de Filtros e Busca**:
  - Chips interativos com seleção por tipo de operação (Todos, Táxi Aéreo, Linha Regional, Linha Regular, Carga / SAE, Instrução, Médica/Especial).
  - Slider dinâmico de raio de distância (0 – 1000 NM / Ilimitado) e checkbox de compatibilidade de carga com a aeronave ativa.
  - Campo de busca instantânea aceitando código ICAO ou cidade (ex: SBRJ ou Rio de Janeiro).
- **Tabela Densa e Acessibilidade**:
  - Tipografia monoespaçada tabular para valores numéricos, permitindo comparações verticais instantâneas.
  - Coluna de seleção múltipla em lote (`IsSelectedForBatch`) com botão de selecionar todos no cabeçalho.
  - Badges cromáticos de categoria e alerta de `SOBREPESO` quando o payload exceder a capacidade da aeronave.
  - Suporte completo a navegação por teclado (setas ↑ e ↓ atualizam o mapa instantaneamente; tecla `Enter` abre a confirmação de voo).
- **Viewport Tático & Card de Pré-Despacho**:
  - Câmera responsiva com auto-enquadramento (`SetZoomToFitRect`) do par Origem–Destino com margem de segurança.
  - Rota geodésica pontilhada magenta de alto contraste e marcadores com badges ICAO.
  - Card de Pré-Despacho operacional com ETE (calculado com velocidade real da aeronave), consumo estimado de combustível (gal e lbs) e METAR simulado simplificado (VFR/MVFR/IFR com resumo meteorológico).
  - CTA proeminente e travado no rodapé com suporte a aceite individual ou em lote de contratos, direcionando diretamente para o Briefing.
- **Empty State**:
  - Tratamento visual para quando filtros excluírem todas as missões, exibindo a mensagem orientadora e botão de restauração de filtros.

## 2026-11-06 - Design System Dark Mode Avionics (Estilo Cockpit Moderno)
- **Paleta de Cores Cockpit 60-30-10**:
  - `Background Base` (`#0B0F14`): Fundo principal da aplicação para redução de fadiga visual em cockpit noturno.
  - `Surface Elevada` (`#141C24`): Cartões de métricas, containers de tabela e painel de mapa.
  - `Surface Hover / Active` (`#1F2B37`): Linha selecionada na tabela, cards com foco e hover de botões secundários.
  - `Bordas e Divisores` (`#263545`): Separadores de coluna, contornos de inputs e molduras.
  - `Texto Primário` (`#F0F4F8`), `Texto Secundário` (`#8B9BAE`) e `Texto Muted` (`#526375`).
- **Cores Semânticas & Status Aeronáutico**:
  - `Ciano Aviônica` (`#00E5FF`): Rota geodésica no mapa, seleção ativa do cursor, botões de ação principal (CTA) e destaques de navegação.
  - `Verde Lucro / VFR` (`#00E676`): Valores monetários, status meteorológico VFR, operações de Táxi Aéreo lucrativas.
  - `Âmbar / Alerta` (`#FFB300`): Operações de Carga Pesada, avisos de peso próximo ao MTOW da aeronave, meteorologia MVFR.
  - `Magenta GPS / IFR` (`#E040FB`): Identificadores de fixos de aproximação, missões com plano IFR obrigatório.
  - `Vermelho Aviso` (`#FF5252`): Carga acima do payload disponível, missão bloqueada por homologação, meteorologia IFR severa / fechada.
- **Tipografia Aeronáutica Dual**:
  - Tipografia de Interface e Navegação: `Inter, Segoe UI, sans-serif` (pesos Regular 400 para descrições e nomes de cidades; Semi-Bold 600 para cabeçalhos e badges de operação).
  - Tipografia Numérica e ICAO: `JetBrains Mono, Consolas, monospace` com numerais tabulares uniformes para códigos ICAO, distâncias, peso e valores monetários.
  - Escala Tipográfica: H1 / Resumo de Rota em 20px (Bold / Ciano `#00E5FF`), cabeçalhos de tabela em 11px Semi-Bold (`#8B9BAE`), células em 13px monoespaçadas e badges de operação em 10px Bold All Caps com padding compacto `2,6` e border radius `3`.
