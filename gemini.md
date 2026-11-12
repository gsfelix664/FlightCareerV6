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

## Role & Operational Identity: Tech Lead & Master Orchestrator

Você atua exclusivamente como **Staff/Lead Software Engineer e Orquestrador de Agentes**. 
Sua responsabilidade é analisar requisitos, desenhar a arquitetura técnica, decompor tarefas complexas em subtarefas atômicas e **delegar a execução** aos subagentes especializados configurados no workspace.

---

### 1. Regras Fundamentais de Execução (Prime Directives)
1. **Zero Código Direto em Escopo Especializado:** Você está expressamente proibido de implementar diretamente lógicas de domínio, queries/migrações de banco, componentes de UI ou regras de negócio profundas. Seu papel é planejar, orquestrar e revisar.
2. **Decomposição em Grafo de Tarefas (DAG):** Antes de disparar qualquer execução, construa um plano sequencial com dependências explícitas:
   - Quem executa primeiro? (Geralmente contrato de dados/arquitetura)
   - O que roda em paralelo?
   - O que depende de validação prévia?
3. **Contratos Estritos de Contexto:** Ao delegar para um subagente, você deve isolar e passar apenas os arquivos, schemas e regras estritamente necessários para aquela subtarefa, evitando poluição de contexto.
4. **Validação Crítica Obrigatória:** Toda entrega de subagente deve ser inspecionada contra padrões de código limpo, cobertura de testes, segurança e performance antes de ser dada como concluída.

---

### 2. Mapa do Workspace e Especialistas
Os subagentes residem na pasta `.gemini/agents/` (e `.antigravity/agents/`):
* **@architect** (`.gemini/agents/architect.md`): Modela domínios, define contratos de API, boundaries de serviços e integridade de dados.
* **@database** (`.gemini/agents/database.md`): Schemas, migrações, índices, triggers, queries complexas e integridade relacional.
* **@backend** (`.gemini/agents/backend.md`): Regras de negócio, endpoints de API, serviços de aplicação, injeção de dependência e telemetria.
* **@frontend** (`.gemini/agents/frontend.md`): Componentes visuais (XAML/WPF), estado de tela, reatividade, integração de API cliente e UX/acessibilidade.
* **@reviewer** (`.gemini/agents/reviewer.md`): Análise estática, code smells, convenções de nomenclatura, SOLID, auditoria e validação de build (`dotnet build`).

---

### 3. Protocolo de Orquestração (Loop de 4 Etapas)
Sempre que uma solicitação de feature, refatoração ou bug fix for recebida, siga rigorosamente este ciclo:
1. **Etapa 1: Análise e Especificação (Orchestrator)**:
   - Inspecione a base de código atual sem modificar arquivos.
   - Defina o escopo, arquivos afetados e potenciais riscos.
   - Monte o plano de execução numerado especificando qual subagente executará cada bloco (DAG).
2. **Etapa 2: Ordem de Despacho (Dispatch Protocol)**:
   - Invoque os subagentes passando o contexto isolado e o escopo restrito da tarefa.
3. **Etapa 3: Auditoria Crítica e Revisão (`@reviewer`)**:
   - Valide as alterações geradas pelo subagente especialista.
   - Cheque integridade relacional, segurança e build limpo (`dotnet build`).
4. **Etapa 4: Consolidação e Relatório**:
   - Apresente o resumo da execução, status da compilação e reporte ao usuário.

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

## 2026-11-07 - Exibição de Roleplay e Situação Operacional no Quadro de Missões
- **Integração de Briefing Narrativo no Pré-Despacho**:
  - Adicionado card dedicado **"SITUAÇÃO OPERACIONAL"** logo acima do painel METAR no Card de Pré-Despacho Operacional (`MainWindow.xaml`).
  - Exibe o título da missão (`SelectedMissionItem.Title`) em destaque âmbar e o texto de roleplay contextual (`SelectedMissionItem.Description`) em tipografia legível (`Inter`) com container de rolagem suave (`ScrollViewer MaxHeight="85"`).
  - Permite aos pilotos lerem a narrativa operacional detalhada (situações de safra, urgências AOG, transporte médico, executivos e instruções) em tempo real conforme selecionam ou navegam pelas missões do quadro via teclado ou mouse.

## 2026-11-07 - Calibração Holística da Economia (Dólar Realista, Recompensas e Combustíveis)
- **Precificação Dinâmica de Missões (`CalculateReward`)**:
  - Implementada taxa base de despacho fixa (*Minimum Dispatch Fee*) por categoria operacional (Instrução: $220/$350; Táxi Aéreo: $950; AOG Agrícola: $950; Aeromédico UTI: $2.400; Regional: $1.800; Regular/Heavy: $5.500).
  - Tarifas variáveis calculadas por milha náutica e passageiro/peso segundo o tipo de operação aeronáutica real.
  - Multiplicador de porte do aeroporto de origem (`OriginTier`: Tier 1 = 1.20x; Tier 2 = 1.08x; Tier 3 = 1.00x; Tier 4 = 0.95x).
  - Adicional de risco para pistas desafiadoras/não pavimentadas (*Bush Premium* de +25% quando o destino for Tier 4).
  - Flutuação estocástica de mercado de $\pm 14\%$ ($0.88$ a $1.14$), gerando variabilidade realista entre contratos semelhantes.
- **Calibração Realista do Mercado de Combustíveis (`FuelPriceService`)**:
  - Ajustados os preços base para cotações reais da aviação em 2026: Avgas 100LL ($6.80/gal) e Jet A-1 ($5.40/gal).
  - Adicionado multiplicador logístico por Tier do aeroporto: Hubs internacionais (Tier 1 = 0.94x por economia de escala); Pistas remotas/rurais (Tier 4 = 1.22x por frete rodoviário de tambores de combustível).
  - Unificação de `FuelService` com `FuelPriceService` para garantir fonte única e coesa de precificação em todo o sistema.

## 2026-11-07 - Redesign da Tela de Briefing de Voo (Dispatch & Pre-Flight Gate)
- **Arquitetura de Layout em 3 Colunas (30% / 45% / 25%)**:
  - **Coluna 1 (30%): Manifesto & Rota Multi-Leg**:
    - **Itinerário Dinâmico (Leg Sequencer)**: Construção automática de pernas com pontos de embarque, ação no destino (desembarque de PAX/Cargas) e envelope visual de Takeoff Weight com barra de MTOW dinâmica e alerta `OVERWEIGHT`.
    - **Cards de Manifesto**: Cartões compactos de cada missão aceita com detalhe de carga, destino e botão sutil de exclusão `[✕]` para abortar o contrato individualmente.
  - **Coluna 2 (45%): Mapa Tático & Navegação Interativa**:
    - Viewport com `FlightMapControl` integrado e overlay superior com telemetria do corredor (Distância Total da rota em NM, contagem de escalas e ETE Total estimado).
  - **Coluna 3 (25%): Gatekeeper "Plan vs Sim" (Diff Checklist)**:
    - Checklist comparativo em tempo real para os 5 parâmetros fundamentais: Conexão SimConnect, Modelo da Aeronave, Posição no Pátio/Aeródromo, Payload (W&B) e Combustível nos Tanques.
    - Status cromático visual (`🟢 OK` ou `🔴 DIVERGENTE` com indicação precisa da diferença em lbs ou galões).
    - Botão contextual `[ 📋 COPIAR W&B P/ O SIM ]` para copiar despacho de peso para a área de transferência.
    - **Botão Mestre de Liberação**: Bloqueado em cinza escuro com tooltip explicativo enquanto houver qualquer discrepância; ativado em verde brilhante quando todos os 5 parâmetros estiverem validados.

## 2026-11-07 - Telemetria em Tempo Real no Mapa, Blackbox Logger, Auditoria de Voo e RPG de Progressão
- **Ícone Dinâmico da Aeronave no Mapa de Briefing (`FlightMapControl`)**:
  - Marcador dinâmico de alta fidelidade visual com alternância automática de estado:
    - **Modo Solo (On Ground)**: Radar ping com círculo tático em `#FFB300`/`#00E676` e badge de pátio `SOLO • {OriginIcao} • {GroundSpeed} kt`.
    - **Modo em Voo (Airborne)**: Silhueta vetorial de aeronave com preenchimento em `#00E5FF` (Ciano Aviônica) e rotação de proa em tempo real (`RotateTransform` centrado em `PlaneHeading`) acompanhado de badge de telemetria ativo `✈️ {Altitude:N0} FT • {GroundSpeed:N0} KT • {Heading:N0}°`.
  - Novas DependencyProperties integradas via MVVM: `IsOnGround`, `AircraftHeading`, `Altitude`, `GroundSpeed`.
- **Correção de Bugs do Status de Pré-Voo Durante o Voo (`BriefingViewModel`)**:
  - Eliminado o bug onde o afastamento do aeródromo de origem durante a decolagem reacusava indevidamente "LOCALIZAÇÃO DIVERGENTE" ou mantinha "PRONTO PARA DECOLAGEM".
  - Durante o monitoramento ativo (`IsMonitoring == true`), o Gatekeeper suspende as validações estáticas de rampa e transiciona para painel operacional de voo ativo (`VOO EM PROGRESSO • GS: {gs} KT • ALT: {alt} FT`), com itens do checklist exibindo carimbos operacionais contínuos (`ATIVO`, `CONFIRMADO`, `A BORDO`, `DESPACHADO`, `EM USO`).
- **Auditoria do Ciclo de Voo e Encerramento Automático Infalível (`FlightMonitor`)**:
  - **Início**: Validação com MSFS real e Simulador de Debug (`data.IsEngineRunning && data.ParkingBrake < 0.5` por 10s contínuos).
  - **Encerramento Automático**: Refatorada a checagem de corte final para `_isAirborne && data.IsOnGround && !data.IsEngineRunning && (data.ParkingBrake > 0.5 || data.GroundSpeed < 1.0)`. Garante o encerramento do voo mesmo se o piloto cortar a mistura no pátio e a variável do freio não atingir o threshold exato.
- **Flight Blackbox Logger (`FlightTelemetryLogger`)**:
  - Novo serviço de telemetria contínua gravando registros estruturados em `logs/flights/flight_{data}_{origem}_{destino}_{prefixo}.csv` a cada 2 segundos.
  - Monitora 32 parâmetros aeronáuticos por frame: Timestamp, tempo decorrido, coordenadas, altitude, velocidades (IAS, GS, VS), Força G, atitude (Pitch/Bank), proa, controles (PB, Flaps, Freios), motores (RPM, CHT, Temp/Pressão de Óleo, ITT, N1), combustível e desgaste dos componentes (fuselagem, motor, pneus).
  - Finaliza com sumário de touchdown FPM, G-Force range, score de qualidade e status de término.
- **Sistema RPG de Progressão da Companhia (Tríade: Horas + XP + Dinheiro)**:
  - Adicionado método `GetRequiredXP` em `PilotCategory`:
    - **Cat 1 ➔ Cat 2 (SAE)**: 40h de voo, 2.500 XP, $35.000.
    - **Cat 2 ➔ Cat 3 (Táxi Aéreo)**: 150h de voo, 12.000 XP, $120.000.
    - **Cat 3 ➔ Cat 4 (Regional)**: 500h de voo, 45.000 XP, $500.000.
    - **Cat 4 ➔ Cat 5 (Regular)**: 1.500h de voo, 150.000 XP, $2.000.000.
  - O comando `PromoteCompanyTierAsync` valida simultaneamente a tríade completa. Voos com baixa qualidade ou pousos duros reduzem o ganho de XP (via `FlightQualityService`), exigindo que o piloto voe mais e com melhor técnica para atingir a meta, além de manter o saldo bancário da empresa em dia.

---

## 📋 PLANO MESTRE DE REFATORAÇÃO (PRÓXIMA SESSÃO): COMUNICAÇÃO SIMCONNECT, PESOS & BALANCEAMENTO (W&B), COMBUSTÍVEL, LOCALIZAÇÃO E DETECÇÃO DE AERONAVE

> **ATENÇÃO AGENTE EXECUTOR**: Este plano técnico detalhado foi elaborado com base na auditoria do código atual do `FlightCareer V6` e na análise de engenharia reversa de três repositórios de referência clonados localmente em `C:\PROJETOS\FLIGHTCAREERV6\references\`:
> 1. **`references/FSTRaK`** (`https://github.com/o4oren/FSTRaK`): Referência em C# WPF + SimConnect (`FSTRaK/BusinessLogic/SimconnectService/SimConnectService.cs`, `FlightStateEvaluator.cs`, `AircraftResolver.cs`, `ConnectionRecovery.cs`).
> 2. **`references/MSFS-Passenger-Map`** (`https://github.com/Inigo2210/MSFS-Passenger-Map`): Referência em validação de coordenadas de menu/sessão e telemetria (`app.py`).
> 3. **`references/flightsim-mcp`** (`https://github.com/eythan-decker/flightsim-mcp`): Referência em arquitetura de SimVars, Polling Tiers e sistema de combustível/pesos (`spec.md`).

---

### 1. Diagnóstico de Bugs Críticos Encontrados no Sistema Atual

1. **BUG CRÍTICO #1 — Fator `1.15` Indevido no Combustível (`BriefingViewModel.cs` e `DebugViewModel.cs`)**:
   - **Onde está o erro**:
     - Em `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs` (linha 127): `FuelQuantity = Math.Round(data.FuelTotalGallons * 1.15, 1);`
     - Em `src/FlightCareer.UI/ViewModels/DebugViewModel.cs` (linha 156): `FuelTotalGallons = FuelGallons / 1.15;`
   - **Causa-Raiz**: A SimVar `"FUEL TOTAL QUANTITY"` requisitada com unidade `"Gallons"` no SimConnect **já retorna US Gallons exatos**. Multiplicar por `1.15` infla a leitura do MSFS em **+15%** (ex: o usuário coloca `20.0 gal` no MSFS e o FlightCareer lê `23.0 gal`), travando o Gatekeeper por divergência falsa de combustível.
   - **Correção**: Remover imediatamente o fator `* 1.15` em `BriefingViewModel.cs` (`FuelQuantity = Math.Round(data.FuelTotalGallons, 1);`) e o `/ 1.15` em `DebugViewModel.cs` (`FuelTotalGallons = FuelGallons;`).

2. **BUG CRÍTICO #2 — Cálculo Errôneo do Payload Reportado pelo Simulador (`BriefingViewModel.cs`)**:
   - **Onde está o erro**:
     - Em `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`: `double reportedSimPayload = Math.Max(0, TotalWeight - EmptyWeight - (FuelQuantity * 6.0));`
   - **Causa-Raiz**:
     - Assume que todo combustível pesa `6.0 lbs/gal` (densidade da Avgas 100LL). Porém, no MSFS 2020/2024, aeronaves Turboélice e Jato (Cessna 208 Caravan, King Air 350i, CJ4, A320, B737) utilizam **Jet A-1 (~6.7 lbs/gal)**. Em um tanque com 200 galões de Jet A-1, o erro é de **140 lbs**, fazendo o Payload calculado divergir mesmo quando o jogador configurou o peso exato!
     - Além disso, o SimConnect **já fornece diretamente** `FUEL TOTAL QUANTITY WEIGHT` (`data.FuelWeightLbs`), `ZERO FUEL WEIGHT` (`data.ZeroFuelWeightLbs`) e `FUEL WEIGHT PER GALLON` (`data.FuelWeightPerGallon`).
   - **Correção**:
     - Calcular o Payload atual do simulador usando o peso real de combustível reportado pela física do MSFS:
       ```csharp
       double simFuelWeightLbs = data.FuelWeightLbs > 0 
           ? data.FuelWeightLbs 
           : data.FuelTotalGallons * (data.FuelWeightPerGallon > 0 ? data.FuelWeightPerGallon : 6.0);
       double reportedSimPayload = Math.Max(0, data.TotalWeightLbs - data.EmptyWeightLbs - simFuelWeightLbs);
       ```
     - Calcular o peso planejado do combustível (`PlannedFuelWeightLbs`) usando a densidade real da aeronave conectada (`double density = data.FuelWeightPerGallon > 0 ? data.FuelWeightPerGallon : 6.0; double plannedFuelLbs = HangarAircraftFuel * density;`).

3. **BUG CRÍTICO #3 — Detecção de Aeronave Frágil contra Pinturas/Liveries da Comunidade (`SimConnectService.cs` & `BriefingViewModel.cs`)**:
   - **Onde está o erro**: Hoje o `SimTelemetry` lê apenas a SimVar `"TITLE"`. Quando o piloto usa uma pintura customizada (ex: `"PMDG 737-800 Gol Linhas Aereas PR-GXP"` ou `"Asobo Cessna 208B Grand Caravan EX Azul Conecta"`), o `TITLE` muitas vezes omite o nome do fabricante ou muda a grafia do modelo do hangar.
   - **Inspiração no `FSTRaK` (`SimConnectService.cs` linhas 566-588)**:
     - O `FSTRaK` requisita simultaneamente:
       - `"TITLE"` (`STRING256`) — Nome da variante/livery
       - `"ATC MODEL"` (`STRING32`) — Código ICAO/Modelo interno da aeronave (ex: `"C152"`, `"C172"`, `"C208"`, `"B350"`, `"G58"`, `"A320"`, `"B738"`)
       - `"ATC TYPE"` (`STRING32` ou `STRING256`) — Fabricante/Tipo (ex: `"CESSNA"`, `"BEECH"`, `"AIRBUS"`, `"BOEING"`)
       - `"ATC ID"` (`STRING32`) — Matrícula/Prefixo no simulador
       - `"CAMERA STATE"` (`INT32` / `FLOAT64`) — Para detectar se o piloto está no Menu Principal do MSFS (`CameraState >= 11`) ou já carregado no cockpit/pátio (`CameraState` 2 a 9).

4. **BUG CRÍTICO #4 — Falta de Clareza e Decomposição Visual de Pesos para o Usuário no Briefing**:
   - **Problema de UX**: O usuário vê apenas "Payload: 820 lbs" e não sabe quanto desse valor vem do Piloto/Tripulação, quanto vem dos Passageiros (`X PAX`), quanto vem de Bagagem/Carga, qual é o Peso Vazio (`Empty Weight`), qual é o `Zero Fuel Weight (ZFW)` e se deve configurar o combustível em Galões ou em Libras no simulador.
   - **Problema de Menu Principal (`0,0 Lat/Lon`)**: Como visto em `MSFS-Passenger-Map/app.py` (linha 183: `if abs(lat) < 0.1 and abs(lon) < 0.1`), quando o MSFS está aberto no Menu Principal, o SimConnect conecta mas envia coordenadas `(0,0)` e pesos zerados. O sistema deve detectar `IsInMainMenu` e exibir um aviso claro: `"MSFS NO MENU PRINCIPAL — Carregue o voo no pátio de [ICAO] e clique em VOAR"`.

---

### 2. Arquitetura e Passo a Passo da Implementação (Executar na Ordem Abaixo)

#### ETAPA 1: Expandir `SimTelemetry.cs` e `SimConnectService.cs` (Padrão FSTRaK + flightsim-mcp)
**Arquivos afetados**:
- `src/FlightCareer.Domain/Entities/SimTelemetry.cs`
- `src/FlightCareer.UI/Services/SimConnectService.cs`
- `src/FlightCareer.UI/ViewModels/DebugViewModel.cs`

**O que implementar**:
1. Em `SimTelemetry.cs` (mantendo `[StructLayout(LayoutKind.Sequential, Pack = 1)]` e a ordem **exata** do `AddToDataDefinition`):
   - Adicionar ao final do struct:
     ```csharp
     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
     public string AtcModel;

     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
     public string AtcType;

     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
     public string AtcId;

     public double CameraState;
     public double PayloadStationCount;
     ```
   - Adicionar propriedades auxiliares calculadas no struct `SimTelemetry`:
     ```csharp
     // No MSFS 2020/2024, CameraState entre 2 e 9 indica dentro do voo (Cockpit/External/Drone).
     // Coordenadas próximas de (0,0) ou EmptyWeight <= 0 indicam que o jogador ainda está no Menu Principal.
     public bool IsInMenuOrLoading => (Math.Abs(Latitude) < 0.1 && Math.Abs(Longitude) < 0.1) 
                                      || EmptyWeightLbs <= 10 
                                      || (CameraState >= 11 && CameraState <= 35);

     public double EffectiveFuelDensityLbsPerGal => FuelWeightPerGallon > 1.0 
         ? FuelWeightPerGallon 
         : (EngineType >= 1.0 ? 6.7 : 6.0); // 6.7 lbs/gal p/ Jet-A1, 6.0 lbs/gal p/ Avgas

     public double ActualPayloadLbs => Math.Max(0, TotalWeightLbs - EmptyWeightLbs - FuelWeightLbs);
     ```
2. Em `SimConnectService.cs`:
   - No método `Connect()`, logo após `AddToDef("PLANE HEADING DEGREES MAGNETIC", "degrees");`, registrar na mesma ordem:
     ```csharp
     _simConnect.AddToDataDefinition(DEFINITIONS.AircraftData, "ATC MODEL", null, SIMCONNECT_DATATYPE.STRING32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
     _simConnect.AddToDataDefinition(DEFINITIONS.AircraftData, "ATC TYPE", null, SIMCONNECT_DATATYPE.STRING32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
     _simConnect.AddToDataDefinition(DEFINITIONS.AircraftData, "ATC ID", null, SIMCONNECT_DATATYPE.STRING32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
     AddToDef("CAMERA STATE", "Enum");
     AddToDef("PAYLOAD STATION COUNT", "number");
     ```
   - Assinar `OnRecvException` (inspirado em `FSTRaK`) para logar qualquer erro de protocolo SimConnect sem derrubar a aplicação.
   - **NOVO RECURSO — Injeção Direta de Combustível no Simulador (`SetFuelInSimulator`)**:
     - Criar uma segunda `DEFINITIONS.FuelWriteData` ou método `TrySyncFuelToSim(double targetTotalGallons)` em `SimConnectService` que permita enviar `FUEL TANK LEFT MAIN QUANTITY` e `FUEL TANK RIGHT MAIN QUANTITY` (cada um com `targetTotalGallons / 2.0`) via `_simConnect.SetDataOnSimObject(...)`. Isso permite ao jogador sincronizar o combustível com 1 clique caso queira!
3. Em `DebugViewModel.cs`:
   - Remover a divisão `/ 1.15` na linha `FuelTotalGallons = FuelGallons;`!
   - Calcular `FuelWeightLbs = FuelGallons * (IsJetMode ? 6.7 : 6.0);` e `FuelWeightPerGallon = IsJetMode ? 6.7 : 6.0;`.
   - Preencher `AtcModel`, `AtcType` e `CameraState = 2` (Cockpit) nos presets (`C152`, `C172`, `B350`) e adicionar um botão no Debug Window: **`[ ⚡ IGUALAR PESOS E COMBUSTÍVEL AO BRIEFING ATUAL ]`** para facilitar testes instantâneos!

---

#### ETAPA 2: Motor Inteligente de Identificação de Aeronave (`AircraftMatcher` inspirado no `FSTRaK`)
**Arquivo afetado**: `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`

**O que implementar**:
1. Substituir o método `IsAircraftMatching(string? simTitle, string? hangarModel)` por `IsAircraftMatching(SimTelemetry data, string? hangarModel)` que avalia em 4 camadas:
   - **Camada 1 (Match Direto por `Title`, `AtcModel` ou `AtcType`)**: Concatena `"{data.Title} {data.AtcType} {data.AtcModel}"` e compara com o modelo do hangar.
   - **Camada 2 (Dicionário de Códigos ICAO e Aliases Oficiais MSFS 2020/2024)**:
     - Mapear equivalências canônicas entre os modelos do banco do FlightCareer e os códigos `ATC MODEL` / `TITLE` do MSFS:
       - `Cessna 152` ➔ `["C152", "152", "AEROBA"]`
       - `Cessna 172` ➔ `["C172", "172", "SKYHAWK"]`
       - `Cessna 208 Caravan` ➔ `["C208", "208", "CARAVAN", "EX"]`
       - `Beechcraft Baron G58` ➔ `["BE58", "G58", "BARON", "58"]`
       - `Beechcraft Bonanza G36` ➔ `["BE36", "G36", "BONANZA", "36"]`
       - `Beechcraft King Air 350i` ➔ `["B350", "BE30", "KING AIR", "350"]`
       - `Daher TBM 930` ➔ `["TBM9", "TBM", "930"]`
       - `Pilatus PC-12` / `PC-6` ➔ `["PC12", "PC6", "PILATUS", "PORTER"]`
       - `CubCrafters` / `Savage Cub` / `XCUB` ➔ `["XCUB", "SAVAGE", "CUB"]`
       - `Diamond DA40` / `DA62` ➔ `["DA40", "DA62", "DIAMOND"]`
       - `Cirrus SR22` / `SF50` ➔ `["SR22", "SF50", "VISION", "CIRRUS"]`
       - `Cessna Citation CJ4` / `Longitude` ➔ `["C25C", "CJ4", "C700", "LONGITUDE", "CITATION"]`
       - `ATR 42` / `ATR 72` ➔ `["AT45", "AT46", "AT75", "AT76", "ATR"]`
       - `Embraer` ➔ `["E110", "E120", "E170", "E175", "E190", "E195", "PHENOM", "PRAETOR"]`
       - `Airbus A320` ➔ `["A320", "A20N", "FBW", "FENIX"]`
       - `Boeing 737` / `747` / `787` ➔ `["B737", "B738", "B748", "B78X", "737", "747", "787", "PMDG"]`
   - **Camada 3 (Intersecção de Designadores Numéricos)**: `152`, `172`, `208`, `350`, `930`, `320`, `737`, etc.
   - Exibir no Checklist do Gatekeeper tanto o `Title` quanto o `AtcModel` detectado (ex: `"C172SP Skyhawk (ICAO: C172)"`).

---

#### ETAPA 3: Painel Detalhado de Decomposição de Pesos e Combustível ("LOADSHEET & W&B GUIDE")
**Arquivos afetados**:
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`
- `src/FlightCareer.UI/MainWindow.xaml`

**O que o Usuário Precisa Ver Claramente na Tela de Briefing**:
No `BriefingViewModel.cs`, expor propriedades calculadas em tempo real para alimentar um card **"FOLHA DE CARREGAMENTO (LOADSHEET) — CONFIGURE NO SIMULADOR"** na Coluna 3 (logo acima ou integrado ao Gatekeeper):

1. **Decomposição Exata do Payload (O que colocar na aba Pesos do MSFS)**:
   - `CrewWeightLbs`: **`170 lbs`** (1 Piloto em comando fixo em 170 lbs; mostrar claramente `"Piloto / Tripulação: 170 lbs"`).
   - `TotalPaxCount`: Quantidade total de passageiros embarcados (ex: `2 PAX`).
   - `PassengersWeightLbs`: `TotalPaxCount * 170.0` lbs (mostrar: `"Passageiros ({TotalPaxCount} PAX × 170 lbs): {PassengersWeightLbs:N0} lbs"`).
   - `TotalLuggageWeightLbs`: `AcceptedMissions.Sum(m => m.LuggageWeightLbs)` (mostrar: `"Bagagem dos PAX: {TotalLuggageWeightLbs:N0} lbs"`).
   - `TotalPureCargoWeightLbs`: `AcceptedMissions.Sum(m => m.CargoWeightLbs)` (mostrar: `"Carga Comercial: {TotalPureCargoWeightLbs:N0} lbs"`).
   - `TotalCargoAndBagsLbs`: `TotalLuggageWeightLbs + TotalPureCargoWeightLbs` (mostrar: `"Total Bagagem + Carga: {TotalCargoAndBagsLbs:N0} lbs"`).
   - **`PlannedPayloadLbs` (PAYLOAD TOTAL A CONFIGURAR NO SIMULADOR)**:
     - Fórmula: `CrewWeightLbs (170) + PassengersWeightLbs + TotalCargoAndBagsLbs`
     - Comparativo ao vivo:
       - **Meta (Briefing)**: `{PlannedPayloadLbs:N0} lbs` (`{PlannedPayloadLbs / 2.20462:N0} kg`)
       - **Atual no Simulador**: `{SimReportedPayloadLbs:N0} lbs` (`{SimReportedPayloadLbs / 2.20462:N0} kg`)
       - **Diferença (Ajuste necessário)**: `{SimReportedPayloadLbs - PlannedPayloadLbs:+0;-0;0} lbs`

2. **Decomposição Exata do Combustível (Quantidade vs. Peso)**:
   - `FuelDensityLbsPerGal`: Lida de `data.EffectiveFuelDensityLbsPerGal` (`6.0 lbs/gal` para Avgas ou `6.7 lbs/gal` para Jet A-1).
   - `SimFuelCapacityGallons`: Lida de `data.FuelTotalCapacity` (ex: `56.0 gal`).
   - **`PlannedFuelGallons` (QUANTIDADE DE COMBUSTÍVEL NO HANGAR)**: `{HangarAircraftFuel:F1} US Gal` (`{HangarAircraftFuel * 3.78541:F0} Litros`).
   - **`PlannedFuelPercent` (% DOS TANQUES NO SLIDER DO MSFS)**: `SimFuelCapacityGallons > 0 ? (HangarAircraftFuel / SimFuelCapacityGallons) * 100.0 : 0` (ex: `50.0%`).
   - **`PlannedFuelWeightLbs` (PESO DO COMBUSTÍVEL)**: `HangarAircraftFuel * FuelDensityLbsPerGal` (ex: `168 lbs` / `76 kg`).
   - Comparativo ao vivo no Gatekeeper:
     - **Planejado (Configure no Sim)**: `{HangarAircraftFuel:F1} gal ({PlannedFuelWeightLbs:N0} lbs | {PlannedFuelPercent:F0}%)`
     - **Atual no Simulador**: `{FuelQuantity:F1} gal ({SimFuelWeightLbs:N0} lbs)`
     - **Diferença**: `{FuelQuantity - HangarAircraftFuel:+0.0;-0.0;0.0} gal ({SimFuelWeightLbs - PlannedFuelWeightLbs:+0;-0;0} lbs)`

3. **Pesos Estruturais Resultantes (Conferência Final com a Barra Inferior do MSFS)**:
   - **`EmptyWeightLbs` (Peso Vazio da Aeronave no Sim)**: `{EmptyWeight:N0} lbs`
   - **`PlannedZeroFuelWeightLbs` (ZFW Esperado = Vazio + Payload)**: `{EmptyWeight + PlannedPayloadLbs:N0} lbs` vs `Sim ZFW: {EmptyWeight + SimReportedPayloadLbs:N0} lbs`
   - **`PlannedTakeoffWeightLbs` (TOW / Peso Total de Decolagem = Vazio + Payload + Combustível)**: `{EmptyWeight + PlannedPayloadLbs + PlannedFuelWeightLbs:N0} lbs` vs `Sim Total Weight: {TotalWeight:N0} lbs` (MTOW Máx: `{MaxGrossWeight:N0} lbs`).

---

#### ETAPA 4: Refinar Validação de Localização e Estado do Simulador no Gatekeeper
**Arquivo afetado**: `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`

**Regras de Validação (`ValidatePreFlightConditions`)**:
1. **Detecção de Menu Principal**:
   - Se `_simConnect.IsConnected && data.IsInMenuOrLoading`:
     - Item 0 (Conexão): `"Conectado (No Menu Principal)"` — Status Âmbar/Vermelho `"AGUARDANDO VOO"`.
     - `WeightStatusText`: `"MSFS NO MENU PRINCIPAL — Carregue o voo em " + HangarAircraftLocation`.
     - `GatekeeperBlockedReason`: `"Inicie o voo no pátio do aeroporto " + HangarAircraftLocation + " e clique em 'VOAR' no MSFS para carregar os pesos da aeronave."`
2. **Tolerância e Precisão de Localização (ICAO)**:
   - Aumentar o raio de busca de aeroportos próximos em `UpdateNearestAirportAsync` para aceitar até `8.0 NM` (cobrindo pistas e terminais afastados de grandes hubs Tier 1 como SBGR, KJFK, etc.).
   - Exibir no item "Posição" do Gatekeeper:
     - Planejado: `"{HangarAircraftLocation} (Pátio / Rampa)"`
     - Simulador: `$"{simLocation} ({nearestDistance:F1} NM)"`
3. **Tolerâncias Justas de Peso e Combustível**:
   - **Combustível (`fuelOk`)**: Aceitar diferença de até `±1.5 gal` (ou `±5%` do combustível planejado, o que for maior), pois sliders do MSFS arredondam porcentagens.
   - **Payload (`payloadOk`)**: Aceitar diferença de até `±35 lbs` (ou `±3%` do payload planejado), permitindo rápido ajuste manual nos sliders do MSFS.

---

#### ETAPA 5: Sincronização Rápida para Testes e Simulador (`SyncWeightsCommand`)
**Arquivos afetados**:
- `src/FlightCareer.UI/ViewModels/BriefingViewModel.cs`
- `src/FlightCareer.UI/Services/SimConnectService.cs`
- `src/FlightCareer.UI/MainWindow.xaml`

**O que implementar**:
1. Adicionar comando `SyncFuelAndWeightsToSimCommand` em `BriefingViewModel`:
   - Se estiver em `DebugMode` (`_simConnect.DebugMode == true`):
     - Atualiza automaticamente os dados do `SimConnectService.CurrentData` (e `DebugViewModel`) com a aeronave correta (`HangarAircraftModel`), coordenadas exatas do aeroporto `HangarAircraftLocation`, combustível exato (`HangarAircraftFuel`) e peso total exato (`EmptyWeight + PlannedPayloadLbs + PlannedFuelWeightLbs`)! Com **1 único clique**, o desenvolvedor/jogador em modo Debug deixa todos os 5 indicadores `🟢 OK` instantaneamente!
   - Se estiver conectado ao **MSFS Real**:
     - Tenta injetar o combustível via SimConnect (`SetDataOnSimObject` nos tanques Left/Right Main) e copia o guia passo-a-passo de Payload (Piloto + PAX + Carga em lbs e kg) para a Área de Transferência, exibindo um resumo visual.

---

### 3. Checklist de Validação para o Agente Executor
Ao implementar este plano, o agente executor deve:
- [x] Executar todas as 5 etapas preservando o padrão MVVM e o estilo visual Avionics Dark UI (`#0B0F14`, `#141C24`, `#00E5FF`, `#00E676`, `#FFB300`, `#FF3D00`).
- [x] Garantir que o fator `* 1.15` / `/ 1.15` de combustível foi 100% erradicado.
- [x] Compilar o projeto com `dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj` e assegurar **0 Error(s)**.
- [x] Registrar a conclusão da implementação no `GEMINI.md`.

---

## 2026-11-08 - Redesign de UX do Briefing, Status Topo Sim/Debug e Despacho Humanizado
- **Status Central de Conexão no Topo do Briefing**:
  - Incorporado no cabeçalho tático operacional um badge proeminente de telemetria com detecção visual instantânea:
    - **MSFS 2020/2024 CONECTADO**: Ícone `🟢`, cor `#00E676` (Verde Esmeralda), badge `#0D2B1E` com legenda *"SimConnect SDK sincronizado em tempo real"*.
    - **MODO DEBUG / TESTE ATIVO**: Ícone `⚡`, cor `#00E5FF` (Ciano Neon), badge `#0B2533` com legenda *"Telemetria artificial simulada via painel de Depuração"*.
    - **SIMULADOR DESCONECTADO**: Ícone `🔴`, cor `#EF4444` (Vermelho Alerta), badge `#2E1214` com instrução *"Inicie o simulador MSFS ou ative o painel Debug"*.
- **Eliminação de Rótulos Mecânicos ("Planejado/Simulador")**:
  - Cards do Gatekeeper pré-voo reformulados com linguagem de aviação e cockpit:
    - Bloco superior destaca a especificação da empresa na carreira (ex: `Cessna C152 (PR-FCA)`).
    - Bloco de status do simulador exibe a identificação direta do modelo reportado em **Verde** se compatível (`✓ Sincronizada no MSFS: Cessna 152`) ou em **Vermelho** se divergente (`✕ Detectada no MSFS: Boeing 737-800`).
- **Instruções e Orientações Operacionais Acionáveis**:
  - Textos explicativos contextuais para cada pilar físico do despacho:
    - **Aeronave**: Explica se está pronta ou instrui qual modelo selecionar no menu do simulador.
    - **Posição / Rampa**: Informa a base de saída e alerta caso a aeronave esteja posicionada em outro aeródromo.
    - **Payload & Passageiros**: Apresenta PAX, carga e piloto, orientando o ajuste exato no menu de Combustível e Carga do MSFS.
    - **Combustível**: Valida o nível dos tanques e avisa se há falta ou excesso para a rota.

## 2026-11-08 - Tela de Logs de Voo & Telemetria Blackbox com Mapa de Trajetória
- **Nova Tela Integrada (`Carreira ➔ Logs de Voo (Blackbox)`)**:
  - Implementada tela completa split-screen no padrão Avionics Dark UI (`32% / 68%`).
  - **Painel Esquerdo (Lista de Voos Gravados)**:
    - Varredura assíncrona automática dos arquivos CSV na pasta `logs/flights/`.
    - Cards de voo com rota (`ORIGEM ➔ DESTINO`), status (Concluído/Cancelado), modelo/prefixo, data/hora, duração e rating de voo (ex: `100% • EXCELENTE`).
    - Campo de busca rápida por ICAO ou aeronave e botão de atualizar lista.
  - **Painel Direito (Detalhes Completos, Mapa e Blackbox)**:
    - **Banner de KPIs da Missão**: Duração, Touchdown FPM, Nota de Qualidade, Faixa de Força G (`Min/Max`) e Altitude Máxima atingida.
    - **Mapa de Trajetória Interativo**: Renderização da rota real voada com a polilinha ciano neon conectando todos os pontos de telemetria GPS registrados, pins com badges ICAO de decolagem (Verde) e pouso (Amarelo Ouro), e auto-enquadramento de zoom (`SetZoomToFitRect`).
    - **Tabela de Telemetria Detalhada**: DataGrid navegável com amostragem de 2 em 2 segundos (Tempo decorrido, Altitude ft, Ground Speed kt, Vertical Speed fps, Força G, RPM, Combustível gal e Fase/Status).
    - **Ações Rápidas**: Botão para abrir o arquivo CSV nativamente no Microsoft Excel ou Bloco de Notas, e botão para abrir a pasta de arquivos no Windows Explorer.

## 2026-11-08 - Arquitetura Data-Driven de Hubs Operacionais (700 Aeródromos e Calibração Dinâmica)
- **Eliminação de Listas Hardcoded**:
  - Toda a base de hubs foi desacoplada de código estático C# e migrada para configuração externa [`src/FlightCareer.Infrastructure/Data/hubs_data.json`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Infrastructure/Data/hubs_data.json).
  - O arquivo é copiado automaticamente no build para a pasta de saída da aplicação (`PreserveNewest`).
- **Base Curada de 700 Aeródromos Verificados (100% de Compatibilidade no Simulador)**:
  - Catalogados **50 aeródromos para o Brasil** e **50 aeródromos Globais** para cada uma das 7 categorias operacionais:
    1. **InstructionHubs** (100 aeródromos): Escolas de aviação, aeroclubes históricos e academias de cadetes (ex: SBJD, SBBP, SDCO, SDTB, KDVT, KFFZ, CYHU, YMMB, etc.).
    2. **CargoHubs** (100 aeródromos): Terminais de carga pesada, logística e e-commerce (ex: SBKP, SBGR, SBEG, KMEM, KSDF, PANC, EDDP, etc.).
    3. **ExecutiveHubs** (100 aeródromos): Aviação corporativa, táxi aéreo e FBOs VIP (ex: SBMT, SBSP, SBJH, KTEB, KVNY, LFPB, EGLF, etc.).
    4. **CommercialPaxHubs** (100 aeródromos): Conexões troncais de passageiros e malha aérea regular (ex: SBGR, SBBR, SBCF, KATL, EGLL, LFPG, RJTT, etc.).
    5. **MaintenanceHubs** (100 aeródromos): Oficinas aeronáuticas, centros de overhaul e MROs (ex: SBSJ Embraer, SBKP Azul, SDSC Latam MRO, WSSL Seletar, LETL Teruel, etc.).
    6. **AgroHubs** (100 aeródromos): Pistas do agronegócio, apoio à safra e aviação agrícola (ex: SBSO Sorriso, SWLC Rio Verde, SDNM Nova Mutum, KICT, SAAR, etc.).
    7. **HealthHubs** (100 aeródromos): Centros hospitalares, transporte de órgãos e bases aeromédicas/UTI aérea (ex: SBGR, SBBH, KROC Mayo Clinic, YBAS Flying Doctor, etc.).
  - 100% dos códigos ICAO validados e confirmados na base de dados `airports.csv`.
- **Serviço de Registro e Injeção de Dependência (`HubDataService`)**:
  - Criada a interface [`IHubDataService`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Domain/Services/IHubDataService.cs) no Domain e implementada em [`HubDataService`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Infrastructure/Services/HubDataService.cs).
  - Estrutura indexada com `HashSet<string>` para buscas instantâneas $O(1)$ em tempo de execução.
  - Registrado como `Singleton` no container de DI em [`App.xaml.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/App.xaml.cs).
- **Calibração Dinâmica de Probabilidades no Gerador de Missões (`MissionGenerator`)**:
  - Implementado o método `GetCalibratedCategories`:
    - **Aeroporto é Hub de Instrução**: Categoria `3.4 (Treinamento)` tem peso elevado dinamicamente para ~35-40% do total de contratos (mesmo em aeroportos grandes como SBMT ou KDVT).
    - **Aeroporto é Hub de Carga**: Categorias `1.2` e `2.3` recebem boost de até 50%.
    - **Aeroporto é Hub Executivo**: Categorias `1.3` e `3.1` sobem para 50%.
    - **Aeroporto é Hub de Passageiros**: Categorias `1.1` e `2.1` sobem para 60-65%.
    - **Aeroporto é Hub Agro**: Categorias `2.2`, `3.2` e `4.x` sobem para 50-70%.
    - **Aeroporto é Hub de Saúde**: Categoria `3.3 (Aeromédico)` sobe para 35%.
  - O sorteio de destinos agora utiliza os novos conjuntos do `HubDataService`, garantindo voos de navegação e cheques direcionados a aeródromos reais de treinamento e hubs da categoria.

## 2026-11-08 - Expansão de Hubs (AERODROMOS_PRINCIPAIS.txt), Multi-Role Balancing e Cotas por Tier
- **Processamento Integral de `AERODROMOS_PRINCIPAIS.txt`**:
  - Ingestão automatizada de todos os aeródromos descritos no documento de referência, cobrindo polos de Aviação Executiva, MRO/Manutenção, Carga Aérea, Grandes Conexões Comerciais, Hubs Alimentadores (Feeder), Portais Internacionais e AgroPoints/InvestPoints.
  - Adição de nova categoria **`FeederHubs`** (Hubs Alimentadores Regionais) à arquitetura.
  - Base de dados [`hubs_data.json`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Infrastructure/Data/hubs_data.json) enriquecida com mais de 1.200 mapeamentos de vocações operacionais.
- **Solução Arquitetural para Conflito Multi-Vocação (Additive Affinity Balancing)**:
  - Resolvido matematicamente o problema de aeroportos com múltiplas vocações simultâneas (ex: `SBKP` como Carga + Pax + Manutenção + Executiva, ou `SBMT` como Instrução + Executiva + Saúde).
  - Em vez de substituições fixas ou sobrescritas de pesos, o sistema adota um modelo aditivo onde cada perfil do aeroporto concede bônus de afinidade proporcionais para suas respectivas categorias.
  - Em `SBKP`: Carga, Linha Regular, MRO e Táxi Aéreo coexistem proporcionalmente de forma idêntica à operação real de Viracopos.
  - Em `SBMT`: Instrução (3.4), Executiva (2.2/3.1) e Saúde/Aeromédico (3.3) dividem o painel harmonicamente.
- **Emparelhamento Realista de Destinos Conforme Perfil Operacional**:
  - Voos de instrução (3.4.1/3.4.2/3.4.3) direcionados preferencialmente para polos de treinamento e alívio (ex: Jundiaí, Bragança, Sorocaba, Atibaia, Pampulha, Bacacheri).
  - Voos cargueiros conectam grandes terminais de carga e capitais industriais.
  - Voos executivos ligam grandes centros a resorts VIP, capitais e feiras do agronegócio.
  - Voos de manutenção (1.3.3/2.4.1) direcionados a centros MRO reais (Embraer SBSJ, Azul MRO SBKP, Latam MRO SDSC, Gol Aerotech SBCF, etc.).
- **Escalação de Cotas de Missões Disponíveis por Tier**:
  - **Tier 1 (Grandes Hubs Internacionais)**: Cota mínima elevada para **200 missões ativas** simultâneas (painel vibrante de conexões).
  - **Tier 2 (Regionais Médios / Capitais Estaduais / Polos de Instrução como SBMT)**: Cota ajustada para **120 missões ativas** (garantindo 30 a 40 contratos de instrução disponíveis de imediato para novos pilotos).
  - **Tier 3 (Municipais / Polos do Interior / Aeroclubes Locais)**: Cota ampliada para **60 missões ativas**.
  - **Tier 4 (Pistas Remotas / Fazendas e Garimpos)**: Cota ajustada para **25 missões ativas**.

## 2026-11-08 - Limpeza da Interface de Produção e Migração do Reset para o Painel Debug
- **Remoção do Botão de Reset da View de Produção**:
  - Removido o botão de teste `ATUALIZAR MERCADO (RESET)` do cabeçalho do quadro de missões em [`MainWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/MainWindow.xaml), mantendo a experiência imersiva e livre de controles de desenvolvimento na tela do jogador.
- **Integração no Painel de Debug (`DebugWindow`)**:
  - Adicionado novo grupo **"Mercado & Missões (DEBUG)"** com o botão `[🔄 RESETAR E REGERAR MISSÕES]` em [`DebugWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Views/DebugWindow.xaml).
  - Acessível a qualquer momento durante testes clicando em ` DEBUG SIM` no menu lateral esquerdo.
  - O comando executa a limpeza das ofertas não aceitas do aeroporto atual e comanda a regeneração instantânea via `MainViewModel` e `IMissionGenerator`, disparando um diálogo de confirmação.

## 2026-11-08 - Renderização Completa de Pernas Multi-Leg e Circuito TGL no Mapa do Briefing
- **Visualização Completa de Pernas no Mapa Tático (`FlightMapControl`)**:
  - Implementada a propriedade de dependência `RouteLegs` em [`FlightMapControl.xaml.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs), conectando diretamente a coleção de pernas ativas do Briefing ao mapa.
  - O mapa agora traça todas as pernas sequencialmente com linhas tracejadas em Ciano Aviônica (`#00E5FF`), além de posicionar badges informativos para cada escala (`PARTIDA`, `LEG X • ICAO`, `LEG N • ICAO (FINAL)`).
- **Suporte a Circuitos de Toque e Arremetida (TGL / Voo Local)**:
  - Quando a missão é de instrução local ou TGL (ex: `SDTB -> SDTB`), o mapa desenha o circuito de tráfego retangular padrão (Upwind, Crosswind, Downwind, Base e Final) em Âmbar (`#FFB300`), com badge dedicado `LEG 1 • SDTB (TGL)` e distância real de 10 NM (em vez de 0 NM).
  - Em rotas combinadas como `SDTB (TGL) -> SDTK -> SDIM`, o mapa exibe primeiro o circuito TGL em SDTB e, em seguida, as pernas de travessia para SDTK e SDIM de forma contínua e integrada.
- **Reestruturação do Algoritmo Multi-Leg (`BriefingViewModel`)**:
  - Encadeamento automático de missões aceitas priorizando voos locais na base seguidos das pernas de navegação.
  - Enquadramento automático suave (`SetZoomToFitRect`) cobrindo toda a extensão geográfica da rota com margem de segurança de 30%.

## 2026-11-08 - Guia de Abas MSFS (Assentos vs Carga) e Detecção Inteligente de PAX
- **Diagnóstico da Divergência de W&B**:
  - Identificado que no menu "Massa e Equilíbrio" do MSFS (2020/2024), passageiros e pilotos são configurados exclusivamente na aba **"Assentos"**, enquanto cargas/bagagens são na aba **"Carga"**.
  - O Gatekeeper acusava falta de 171 lbs porque o assento do passageiro permaneceu vazio (1/2 assentos ocupados = apenas o piloto de 170 lbs) no simulador.
- **Implementação do Guia Visual de Abas MSFS no Manifesto**:
  - Adicionado painel no rodapé do manifesto em [`MainWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/MainWindow.xaml) detalhando a distribuição exata:
    - **💺 ABA ASSENTOS**: Quantidade de assentos a preencher e peso total (Piloto + PAX x 170 lbs).
    - **📦 ABA CARGA**: Peso total de bagagens/mercadorias.
- **Mensagens Contextuais e Copiar W&B Otimizado**:
  - O Gatekeeper em [`BriefingViewModel.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/ViewModels/BriefingViewModel.cs) agora identifica especificamente quando um assento de passageiro foi deixado vazio e instrui o piloto a preencher o assento correspondente na aba "Assentos".
  - O texto do botão "Copiar W&B" foi formatado no padrão exato das abas do MSFS.

## 2026-11-08 - Correção de Falso-Positivo de Perda Total, Aborto em Solo e Telemetria Real no Debug
- **Correção da Detecção de Crash Catastrófico (`WearAndFailureService.cs`)**:
  - Identificada a causa-raiz do falso-positivo de perda total imediata: a condição `data.CrashSequence > 0` zerava instantaneamente a fuselagem, motor e pneus assim que o voo iniciava, devido a estados transitórios da variável `CRASH SEQUENCE` do SimConnect ou assistências de colisão desligadas no MSFS.
  - Implementada restrição estrita: crash real catastrófico agora exige `CrashSequence == 11` (início formal de crash no MSFS) ou `CrashSequence > 0` com evidências dinâmicas de impacto no ar / alta velocidade / estresse g-force severo (`!data.IsOnGround || data.GroundSpeed > 45 || Math.Abs(data.GForce) > 4.5 || Math.Abs(data.VerticalSpeed * 60) > 2500`).
  - Operações no pátio, táxi ou parada com motores cortados são blindadas contra falsos crashes.
- **Tratamento de Aborto / Corte de Motor em Solo (`FlightMonitor.cs`)**:
  - Implementada detecção de aborto de voo quando o piloto liga os sistemas mas desliga o motor com PB acionado sem decolar (`!_isAirborne && data.IsOnGround && !data.IsEngineRunning && isParkedOrStopped`).
  - O voo é cancelado de forma segura após 5 segundos como `VOO CANCELADO EM SOLO`, preservando a aeronave sem danos ou penalidades.
  - Substituído o valor fixo de `999` fpm no log de perda total pelo valor dinâmico real lido de velocidade vertical.
  - Restauradas as condições de célula, motor e pneus da aeronave `PR-NZY` de volta a 100% no banco de dados.
- **Monitoramento de Telemetria ao Vivo no Painel de Debug (`DebugViewModel.cs` e `DebugWindow.xaml`)**:
  - Inscrito o `DebugViewModel` nos eventos de telemetria `SimConnectService.OnDataReceived`.
  - Quando a opção "Injeção Manual (Modo Debug)" estiver desmarcada, a janela de debug exibe em tempo real TODOS os parâmetros transmitidos diretamente pelo Microsoft Flight Simulator (Velocidades, Altitude, Coordenadas, Pesos, Combustível, Rotação do Motor, Pressão, CHT/ITT, G-Force e Crash Sequence).
  - Adicionado banner superior com indicador de status dinâmico:
    - `🟢 TELEMETRIA AO VIVO DO SIMULADOR (MSFS CONECTADO)`
    - `🟡 MODO DEBUG ATIVO (INJEÇÃO MANUAL HABILITADA)`
    - `⚪ SIMULADOR DESCONECTADO (AGUARDANDO MSFS)`

## 2026-11-08 - Tela HUD Overlay Flutuante / Topmost para Acompanhamento em Voo
- **Criação da Janela HUD Overlay (`DebugOverlayWindow.xaml` e `DebugOverlayWindow.xaml.cs`)**:
  - Janela flutuante no estilo *Glass Dark Avionics* com `Topmost="True"`, `WindowStyle="None"`, `AllowsTransparency="True"` e fundo translúcido (`#E6070B12`) para se sobrepor diretamente à tela do Microsoft Flight Simulator sem minimizar o jogo.
  - Arrastável livremente em qualquer monitor clicando e segurando com o botão esquerdo.
  - **Controle de Opacidade Rápido (`CycleOpacityCommand`)**: Ciclo de transparência configurável (100% ➔ 80% ➔ 60% ➔ 40%) para não obstruir o painel da aeronave.
  - **Modo Compacto / Mini HUD (`ToggleCompactCommand`)**: Botão de alternância entre ticker horizontal minimalista (apenas velocidades essenciais, altitude e fase) e o painel técnico completo.
  - **Pin Topmost**: Botão para fixar ou desafixar o comportamento flutuante da janela.
- **Implementação do ViewModel Dedicado (`DebugOverlayViewModel.cs`)**:
  - Telemetria de alta frequência conectada ao `SimConnectService.OnDataReceived` e fases do `FlightMonitor`.
  - **Instrumentos Primários**: Airspeed (IAS), Ground Speed (GS), Altitude (ft) e Razão Vertical (VS fpm com coloração dinâmica por gradiente de subida/descida).
  - **Atitude e Dinâmica**: Força G com alertas de cor em estresse, Pitch, Bank, Proa (HDG), Flaps (%) e Parking Brake (Livre / Acionado).
  - **Grupo Moto-Propulsor**: Detecção automática de pistão vs jato/turboprop, monitorando RPM/N1, CHT/ITT, pressão de óleo e temperatura de óleo.
  - **Pesos e W&B**: TOW em tempo real vs Peso Máximo Estrutural e galões/libras de combustível.
  - **Saúde e Desgaste (Hangar)**: Barras coloridas de desgaste em tempo real da Célula, Motor e Pneus.
- **Acesso Rápido na Interface**:
  - Botão integrado no cabeçalho superior de [`MainWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/MainWindow.xaml): `[ 🖥️ HUD OVERLAY (MSFS) ]`.
  - Botão integrado no menu lateral esquerdo sob a categoria DEBUG.
  - Botão dedicado dentro da janela clássica [`DebugWindow.xaml`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Views/DebugWindow.xaml).

## 2026-11-08 - Correção Definitiva do Deslocamento de 8 Bytes do SimConnect (`ZERO FUEL WEIGHT`) e Crash no HUD
- **Diagnóstico com o MSFS 2024 em Execução**:
  - Conectamos uma rotina de teste ao vivo no MSFS 2024 e capturamos a exceção exata do SimConnect:
    `[EXCEPTION] ID: NAME_UNRECOGNIZED, SendID: 19, Datum: 'ZERO FUEL WEIGHT', Unit: 'Pounds'`
  - **Causa-Raiz**: O SimVar `ZERO FUEL WEIGHT` não existe no Microsoft Flight Simulator.
  - Ao descartar essa variável, o SimConnect encurtou a mensagem de telemetria em 8 bytes, deslocando **todas as variáveis subsequentes** na memória:
    - O campo `GForce` recebeu o valor de Pitch (ex: `-5.21G`).
    - `PlanePitch` recebeu Bank (ex: `3°`).
    - `PlaneBank` recebeu Heading (ex: `4°`).
    - `PlaneHeading` recebeu Flaps (`000°`).
    - `FlapsPercent` recebeu VelocityBodyY (resultando em `-931%`).
    - `Overspeed` recebeu o valor de RPM (ex: `2630 > 0.5`), acendendo falsamente o alerta de Overspeed.
    - `EngineRPM` recebeu CHT.
    - `EngineOilPressure` recebeu `-273.15` (valor nulo/zero absoluto de uma temperatura).
    - O estresse aerodinâmico surreal de `-5.21G` e overspeed constante destruíam a integridade da aeronave para `0%` em segundos, disparando falso crash/perda total.
- **Correções Aplicadas**:
  - **Remoção de `ZERO FUEL WEIGHT`**: Removido de [`SimConnectService.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Services/SimConnectService.cs) e de [`SimTelemetry.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Domain/Entities/SimTelemetry.cs).
  - **Telemetria 100% Validada ao Vivo com MSFS 2024**:
    - `GForce`: **1.19 G** (estável).
    - `Heading (Proa)`: **004°** (preciso).
    - `Flaps`: **0%** (normalizado).
    - `RPM`: **2630 RPM** (cruzeiro do C152).
    - `Pressão de Óleo`: **69.1 PSI** (faixa verde perfeita).
    - `Temperatura de Óleo`: **27.4 °C**.
    - `CHT`: **208.3 °C** (ideal).
    - `Overspeed`: **0** (desativado).
  - **Reset do Estado de Crash**:
    - Adicionado método `ResetCrashState()` em `SimConnectService`.
    - `FlightMonitor` redefine `_simConnect.ResetCrashState()` ao iniciar ou cancelar o monitoramento.
    - Restabelecida a integridade de `PR-NZY` no banco de dados local para **100% Célula, 100% Motor, 100% Pneus**.
    - Atualização periódica da saúde da célula no HUD flutuante vinculada ao loop de telemetria.

## 2026-11-08 - Renderização da Trajetória Voada no Mapa dos Logs de Voo (Blackbox)
- **Diagnóstico do Traçado Não Exibido**:
  - Os arquivos CSV da Blackbox gravavam os pontos GPS perfeitamente (ex: 112 amostras de latitude e longitude).
  - No entanto, a propriedade `MapRoutePoints` em [`FlightLogEntry.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/ViewModels/FlightLogEntry.cs) era uma `List<PointLatLng>` estática com getter simples.
  - Como o parsing do CSV roda em segundo plano assincronamente após o clique no card de voo, a adição de pontos na lista não notificava o WPF (`INotifyPropertyChanged`), e a `DependencyProperty` `RecordedPath` do mapa nunca recebia o evento de alteração (`OnRecordedPathChanged`).
- **Solução Implementada**:
  - `MapRoutePoints` convertido para `[ObservableProperty] private ObservableCollection<PointLatLng> _mapRoutePoints`.
  - O carregamento em [`FlightLogsViewModel.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/ViewModels/FlightLogsViewModel.cs) agora instancia a coleção completa e atribui na UI Dispatcher, disparando a atualização do WPF.
  - `FlightMapControl` atualizado para aceitar `IEnumerable` genérico e observar `INotifyCollectionChanged`, limpando e redesenhando automaticamente a rota ciano neon, os pins de partida (Verde) e pouso (Amarelo Ouro), e chamando `SetZoomToFitRect` para enquadrar a trajetória completa voada.

## 2026-11-08 - Matriz de Panes Contextuais e Realistas (Ambiente, Meteorologia e Operação do Piloto)
- **Integração de Telemetria Ambiental e de Comandos do Piloto**:
  - Adicionadas 12 novas variáveis ao [`SimTelemetry.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.Domain/Entities/SimTelemetry.cs) e mapeadas no [`SimConnectService.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Services/SimConnectService.cs) com validação em tempo real contra o MSFS 2024:
    - `SURFACE CONDITION` (0 = Normal, 1 = Molhado, 2 = Gelo, 3 = Neve)
    - `AMBIENT TEMPERATURE` (°C) e `AMBIENT PRECIP STATE` (Mask de Chuva/Precipitação)
    - `AMBIENT WIND VELOCITY` e `AMBIENT WIND DIRECTION` (cálculo de componente de vento cruzado / crosswind)
    - `VELOCITY BODY X` (velocidade lateral no toque e na rolagem de pista)
    - `GENERAL ENG THROTTLE LEVER POSITION:1` e `MIXTURE LEVER POSITION:1` (%)
    - `ENG ANTI ICE:1` (Status do aquecimento de carburador / anti-gelo do motor)
    - `STRUCTURAL ICE PCT` e `PITOT ICE PCT` (% de gelo acumulado na fuselagem e no pitot)
    - `PRESSURIZATION CABIN ALTITUDE` (altitude de pressão interna da cabine)
- **Panes Operacionais Contextuais Implementadas em [`WearAndFailureService.cs`](file:///C:/PROJETOS/FLIGHTCAREERV6/src/FlightCareer.UI/Services/WearAndFailureService.cs)**:
  1. **Aquaplanagem e Estouro por Borracha Revertida (*Reverted Rubber Blowout*)**:
     - Quando `SURFACE CONDITION >= 1` (Pista Molhada/Gelo) e velocidade de solo $> 40\text{ kts}$.
     - Se os pneus estiverem com banda gasta ($< 60\%$) e o piloto frear bruscamente, a capacidade de drenagem falha e o multiplicador de estouro salta até **25x**, simulando fricção térmica violenta sobre lâmina d'água.
  2. **Congelamento de Carburador (*Carb Icing*)**:
     - Monitora descidas em baixa potência (`Throttle < 35%`) em ar úmido (`Ambient Temperature` entre 4°C e 24°C ou chuva ativa).
     - Se o piloto esquecer o `Carb Heat` desativado, o gelo acumula progressivamente em `_carbIceAccumulator` até provocar engasgo e apagamento total do motor (`TOGGLE_ENGINE_FAILURE_1`).
     - Acionar o `Carb Heat` derrete o gelo de forma dinâmica.
  3. **Encharcamento de Velas (*Spark Plug Fouling*)**:
     - Detecta táxi prolongado no solo com mistura 100% rica, rotação baixa ($< 1200\text{ RPM}$) e motor frio ($< 130^\circ\text{C}$).
     - Simula depósito de chumbo nos eletrodos gerando aspereza e falha de centelha, reversível apenas acelerando a 1800 RPM com mistura empobrecida para queima de chumbo.
  4. **Stall de Compressor (*Engine Surge*) em Jatos/Turbinas**:
     - Detecta avanço brusco de manete (*slam throttle* $> 45\%/\text{s}$) durante decolagem sob vento cruzado severo ($> 20\text{ kts}$).
     - Provoca desorganização de fluxo de ar nas palhetas, disparando corte/stall de compressor no simulador.
  5. **Colapso Estrutural de Perna de Trem por Carga Lateral (*Side-Load Collapse*)**:
     - Avalia no toque o vetor lateral (`VelocityBodyX > 7.0 fps`) decorrente de pouso caranguejado com vento de través.
     - Se a célula estiver desgastada ($< 50\%$), o estresse cisalha os pinos mestres da tesoura do amortecedor e recolhe a perna no impacto.
  6. **Bloqueio de Pitot por Gelo**:
     - Disparado automaticamente se `PITOT ICE PCT > 25%` durante o voo em nuvens/gelo sem aquecimento.
  7. **Despressurização de Cabine**:
     - Detecta altitude de voo $> 12.000\text{ ft}$ com altitude de cabine subindo além de $10.000\text{ ft}$ em células com fadiga ($< 65\%$).








## 2026-11-08 - Redesign do Quadro de Missões, Dark Map Tático e Análise de W&B do MSFS 2024
- **Diagnóstico de Divergência de W&B no Gatekeeper (MSFS 2024 EFB)**:
  - Identificada a causa-raiz da divergência de payload reportada pelo Gatekeeper (-161 lbs):
    - No MSFS 2024, ajustar os controles deslizantes de "Assentos", "Carga" e "Combustível" no tablet/EFB altera apenas o rascunho de pré-visualização.
    - O simulador **não aplica** as alterações aos pesos reais da aeronave até que o piloto clique fisicamente no botão azul **`[ CARREGAR A AERONAVE ]`** no rodapé do EFB.
    - Antes de clicar no botão, a aeronave permanece com a configuração padrão do Cessna C152 (227 lbs de payload e 9.4 gal de combustível), que era lida pelo SimConnect com precisão.
    - Teste ao vivo via SimConnect confirmou que após clicar no botão do EFB, os pesos sobem instantaneamente para 1.547 lbs de TOW e ~397 lbs de payload, validando a sincronização com o manifesto.
    - Atualizada a mensagem orientativa de pré-voo no Gatekeeper com instrução explícita sobre o botão `[ CARREGAR A AERONAVE ]`.
- **Redesign Completo da Interface do Quadro de Missões & Contratos**:
  - **Eliminação do Espaço Vazio na Tabela**:
    - Distribuídas as colunas de forma fluida nos 58% do split, eliminando o espaço escuro ocioso.
    - Inseridas colunas operacionais de alto valor: `CARGA/PAX`, `DISTÂNCIA`, `PISTA MÍN`, `REGRA (VFR/IFR)`, `XP` e `PAGAMENTO`.
  - **Alinhamento Numérico e Tabular Rigoroso**:
    - Colunas numéricas e financeiras alinhadas à direita com fonte `JetBrains Mono` monoespaçada, tanto nos cabeçalhos (`RightAlignedHeader`) quanto nas células.
  - **Hierarquia Visual Cromática e Badges Semânticos**:
    - Badges padrão neutros (`#2A3749` / `#161F2A`), reservando cores vibrantes para contratos VIP (dourado `#F59E0B`), Emergência/Médica (vermelho `#EF4444`), Cargas Especiais (ciano `#00E5FF`) e Instrução (roxo `#C084FC`).
  - **Dark Map Tático (Redução de Fadiga Visual)**:
    - Implementado `CartoDbDarkMapProvider` no `FlightMapControl`, substituindo o mapa diurno brilhante por camada noturna escura de cartas táticas.
  - **Card de Pré-Despacho Operacional Escaneável**:
    - Adicionados chips visuais para tipo de carga, pista mínima requerida, categoria da companhia e regras de voo.
    - Tipografia da descrição operacional calibrada com alto contraste (`#E2E8F0`).
  - **Botão de Ação Primária**:
    - Atualizado para 48px de altura com tipografia ExtraBold e ícone destacado: `[➕ ACEITAR CONTRATO & INICIAR BRIEFING]`.
  - **Remoção de Redundância de Saldo**:
    - Saldo duplicado substituído no banner superior pelo card **AEROPORTO DE PARTIDA** (`CurrentAirportICAO`).
  - **Correção no HUD Overlay**:
    - Resolvido o problema de sobreposição entre o texto da Fase de Voo e a Rota com uso de Grid de 2 colunas e DockPanel com reticências (`TextTrimming="CharacterEllipsis"`).


## 2026-11-08 - Redesign do Despacho Operacional (Pre-Flight Gatekeeper, W&B e Manifesto)
- **Eliminação de Espaços Vazios Verticais (Layout Collapse)**:
  - Reestruturada a Coluna Esquerda (30%) em três blocos operacionais auto-ajustáveis:
    1. `ITINERÁRIO DINÂMICO MULTI-LEG` (Compacto com limite de altura).
    2. `CONTRATOS NO MANIFESTO` (Ocupa o espaço intermediário com scroll).
    3. `ENVELOPE DE MASSA E BALANCEAMENTO (W&B)` (Bloco dedicado ancorado ao rodapé).
  - Eliminado o grande "buraco" escuro que existia quando havia apenas 1 ou 2 contratos no manifesto.
- **Micro-Tipografia de Auditoria Plan vs. Sim (Coluna Direita)**:
  - Criado componente de comparação lado a lado de 2 colunas para cada item do checklist:
    - `PLANO (CARREIRA)` vs `SIMULADOR (MSFS)` com fontes monoespaçadas `JetBrains Mono` de 11px em negrito e alto contraste.
    - Badges consolidadas de tolerância integradas diretamente no cabeçalho do valor simulado (ex.: `✓ 100% EXATO`, `✓ ±9 lbs`, `✓ ±0.6 gal`).
    - Alerta explicativo dinâmico exibido apenas quando houver divergência, orientando o piloto sobre os ajustes específicos no EFB do MSFS.
- **Centralização do Status do Gatekeeper**:
  - Removido o badge duplicado de "Despacho Aprovado" da barra de título do briefing.
  - O cabeçalho do Gatekeeper agora hospeda o status centralizado de auditoria: `🟢 4/4 SISTEMAS CONFORMES` ou `⚠️ X/4 SINCRONIZADOS`.
  - A barra superior do briefing agora exibe com exclusividade o status de link ao vivo do SimConnect (`🟢 MSFS 2020/2024 CONECTADO`).
- **Unificação do Envelope de Massa e Centragem (W&B)**:
  - Removida a barra isolada de MTOW do card de pernas e os dados fragmentados do rodapé do manifesto.
  - Agrupados todos os dados em um único bloco de W&B:
    - Indicador de margem livre (`X LBS LIVRES` ou `⚠️ SOBREPESO`).
    - Barra contínua de envelope de MTOW com comparativo TOW vs MTOW e porcentagem.
    - Três cards técnicos detalhando: Assentos (`TotalSeatsWeight`), Carga (`TotalCargoWeight`) e Combustível (`FuelWeightLbs`).
    - Guia prático de preenchimento das abas 'Assentos' e 'Carga' do tablet do MSFS 2024.
- **Botão de Exclusão Ghost / Outline**:
  - Substituído o botão vermelho agressivo `[ X ]` de cancelamento por um botão discreto de estilo outline/ghost (`#263545` / `#64748B`), que só ganha coloração vermelha ao passar o mouse (`MouseOver`).
- **Limpeza do Viewport do Mapa Tático (Coluna 2)**:
  - Removidos os chips flutuantes redundantes sobre o mapa (`ROTA CORREDOR`, `DISTÂNCIA`, `TEMPO ESTIMADO`), que competiam com a barra superior de despacho.
  - O mapa agora é 100% livre de sobreposições para navegação visual limpa.
- **Botões de Ação com Espaçamento e Estado Inteligente**:
  - Adicionado respiro de 16px na margem inferior dos botões de ação da coluna direita.
  - Botão de Cópia W&B atualizado para estado inteligente:
    - Quando o W&B já estiver sincronizado com o simulador: `✓ W&B JÁ SINCRONIZADO NO MSFS` em verde/neutro.
    - Quando houver divergência: `📋 COPIAR W&B P/ O SIMULADOR` em ciano com tooltip explicativo.

## 2026-11-08 - Expansão do Quadro de Missões (Colunas ETE, Rumo, METAR, Expiração), Mapa Noturno ESRI e Correção de Crash
- **Correção Crítica de Crash de Runtime (`TowPercentage`)**:
  - **Causa Raiz**: O controle WPF `ProgressBar` herda de `RangeBase.ValueProperty`, cujo padrão é `BindsTwoWayByDefault`. Como a propriedade `TowPercentage` na ViewModel tinha apenas getter (read-only), o WPF lançava `InvalidOperationException` ao abrir a janela principal.
  - **Resolução**:
    - Convertido `TowPercentage` em `[ObservableProperty] private double _towPercentage;` com recálculo automático em `OnCalculatedTotalWeightChanged` e `OnHangarAircraftMtowChanged`.
    - Explicitado `Mode=OneWay` no XAML (`<ProgressBar Value="{Binding TowPercentage, Mode=OneWay}" ... />`).
- **Eliminação de Vazio Horizontal no Quadro de Missões (Novas Colunas)**:
  - Adicionadas 4 novas colunas técnicas ao grid do Quadro de Missões, preenchendo 100% da largura útil de forma densa e balanceada:
    1. **`RUMO` (Proa Geográfica)**: Rumo inicial em graus e direção cardeal (ex: `045° NE`, `180° S` ou `LOCAL`), calculado via trigonometria esférica (Great-Circle Bearing) entre as coordenadas de origem e destino.
    2. **`ETE` (Tempo Estimado de Voo)**: Calculado dinamicamente com base na velocidade de cruzeiro da aeronave ativa no Hangar (ex: `1h 32m`, `28 min`).
    3. **`METAR` (Meteorologia do Destino)**: Badge de alta visibilidade avionics (`☀️ VFR`, `⛅ MVFR`, `🌧️ IFR`) com cores semânticas e tooltip detalhando vento, visibilidade e teto.
    4. **`EXPIRAÇÃO`**: Tempo restante de validade do contrato (ex: `2h 15m`, `1d 04h`), com badge adaptativo em âmbar/vermelho para contratos próximos ao vencimento.
- **Substituição de Tiles de Mapa (Fim das Marcas d'Água do CartoDB)**:
  - O antigo CartoDB passou a injetar marca d'água (`API KEY REQUIRED`) nas requisições gratuitas sem token.
  - Substituído pelo **ESRI World Dark Gray Canvas** (`server.arcgisonline.com`), combinado com uma camada de sobreposição transparente (`EsriDarkReferenceMapProvider`) para renderização nítida de fronteiras e cidades.
  - Atualizado o GUID e o identificador do provedor para forçar o GMap.NET a purgar o cache local e carregar os novos tiles noturnos limpos e sem marcas d'água.

## 2026-11-08 - Suporte a Códigos ICAO Legados do MSFS (Ex: SBPG vs SSZW Ponta Grossa)
- **Diagnóstico da Divergência de ICAO**:
  - Na aviação real brasileira (DECEA / AISWEB), o aeroporto de Ponta Grossa - PR (Comandante Antonio Amilton Beraldo) foi modernizado e teve seu código ICAO alterado do antigo **SSZW** para o oficial **SBPG**.
  - O banco de dados de navegação padrão do Microsoft Flight Simulator (Asobo / NavBlue) não atualizou esse aeródromo e ainda o registra sob o código legado **SSZW**.
- **Solução Integrada**:
  - Implementado o dicionário de equivalências `MsfsIcaoAliases` mapeando aeroportos brasileiros cujos códigos diferem no MSFS (ex.: `SBPG` ➔ `SSZW`, `SBCA` ➔ `SSKW`, `SBTD` ➔ `SSTW`, etc.).
  - No Pré-despacho, Itinerário de Briefing e Tooltip da tabela de contratos, o sistema exibe dinamicamente a anotação clara: `SBPG (MSFS: SSZW)`.
  - Como a detecção de pouso e tracking de voo do `FlightMonitor` utiliza coordenadas geográficas puras (Latitude/Longitude), pousar na pista de Ponta Grossa no MSFS (mesmo selecionando SSZW no simulador) conclui o voo normalmente no software com 100% de precisão.

## 2026-11-08 - Integração Oficial com a API da NOAA (AviationWeather.gov) e METAR Real Contínuo
- **Remoção Integral do Gerador Aleatório/Simulado**:
  - Eliminado o método `GenerateWeather` de sorteio procedural determinístico. O sistema agora opera exclusivamente com observações meteorológicas reais oficiais.
- **Criação do Serviço de Meteorologia Aeronáutica Real (`MetarService.cs`)**:
  - Conectado diretamente à API REST pública da NOAA (`https://aviationweather.gov/api/data/metar`).
  - Implementada consulta em lote (*batching*) para múltiplos destinos simultaneamente em uma única requisição HTTP leve e assíncrona.
  - Parse de campos oficiais: METAR bruto (`rawOb`), categoria de voo (`fltCat`: VFR, MVFR, IFR, LIFR), vento (direção, velocidade e rajadas), temperatura, ponto de orvalho e altímetro.
- **Resolução de Aeródromos Remotos sem Estação**:
  - Para pistas e aeródromos locais desprovidos de estação meteorológica oficial (AWOS/METAR), o `MetarService` calcula a distância geodésica em relação às estações ativas e vincula a estação reportante real mais próxima, sinalizando explicitamente: `Via XXXX (YY NM): [Raw METAR]`.
- **Motor de Atualização Contínua e em Tempo Real**:
  - Criado timer de atualização automática a cada 5 minutos em segundo plano no `MainViewModel`.
  - Adicionado botão dedicado `[ 🔄 METAR ]` na barra de filtros para forçar atualização instantânea a qualquer momento.
  - No card de Pré-despacho, o cabeçalho agora exibe `METAR DESTINO (TRANSMISSÃO REAL NOAA) • AO VIVO`, acompanhado da string METAR oficial em destaque ciano monoespaçado.

## 2026-11-08 - Sincronização Dinâmica com Cenários do MSFS 2024 e Sistema Dual de ICAO
- **Verdade Absoluta no Simulador (Zero Missões Fantasmas)**:
  - O banco de dados do Flight Simulator 2024 (extraído diretamente do banco SQLite do Little Navmap em `%APPDATA%\ABarthel\little_navmap_db\little_navmap_msfs24.sqlite`, com 83.095 aeródromos) passou a ser a **fonte de verdade primária** para a existência física de qualquer aeródromo.
  - Missões no `MissionGenerator` são estritamente restritas a aeroportos que de fato existem no simulador (`query = query.Where(a => a.IsInSimulator)`). Pistas inexistentes na instalação do usuário recebem `IsInSimulator = false` e são completamente excluídas da geração.
- **Enriquecimento Híbrido (DECEA / ANAC + MSFS 2024)**:
  - Os dados oficiais ricos do CSV (cidades, nomes reais atualizados, Tiers regulatórios e códigos ICAO modernos) são preservados para o planejamento do piloto.
  - Implementado o serviço `MsfsScenerySyncService` que executa correlação espacial ultra-rápida (grid espacial de 0.5°):
    - **22.665 aeródromos** identificados com correspondência exata de ICAO (distância <= 5 NM).
    - **3.309 divergências** identificadas e correlacionadas automaticamente (pistas existentes no MSFS sob idents legados/alternativos dentro de 3 NM, ex: `SBPG` ➔ `SSZW` Ponta Grossa com dist 0.004 NM).
    - **4.317 aeródromos fictícios ou não modelados** excluídos da geração de voos.
- **Exibição Dual de ICAO na Interface**:
  - **Quadro de Missões**: Na coluna `DESTINO`, além do ICAO oficial e da cidade, quando há divergência o card exibe um badge visual ciano monoespaçado `[MSFS: SSZW]`.
  - **Pré-Despacho Operacional**: Exibe `SBPG (MSFS: SSZW)`.
  - **Itinerário do Briefing**: As pernas multi-leg e títulos exibem o destino acompanhado do ident simulado.
  - **Gatekeeper Pré-Voo**: Valida a presença da aeronave na rampa tanto pelo código ICAO oficial quanto pelo ident MSFS (ex: se a aeronave spawnar em `SSZW`, o Gatekeeper reconhece como a posição correta de `SBPG` e libera o voo).
  - **Voo de Translado (Ferry Flight)**: A busca de aeródromos agora aceita indistintamente o ICAO oficial (`SBPG`) ou o ident do simulador (`SSZW`).

## 2026-11-08 - Calibração Tipográfica e Aumento Geral de Escala Visual (Legibilidade Total)
- **Remoção de Badge Redundante e Limpeza Visual de Categoria da Empresa**:
  - Removido o badge `[Cat 1: CIAC / Escola]` que aparecia repetido ao lado do título principal `QUADRO DE MISSÕES & CONTRATOS`, eliminando poluição visual no cabeçalho.
  - Removido o card duplicado `HOMOLOGAÇÃO DA EMPRESA` do banner de métricas superiores (a categoria operacional permanece acessível e centralizada exclusivamente no perfil do piloto na barra lateral: `[Cat 1: CIAC / Escola ⮵]`).
  - O banner de métricas rápidas foi reorganizado em 3 colunas amplas e equilibradas (`CONTRATOS DISPONÍVEIS`, `SELECIONADOS EM LOTE`, `AEROPORTO DE PARTIDA`).
- **Padronização Global de Tipografia no Projeto (Fim de Fontes Pequenas)**:
  - Eliminadas todas as ocorrências de fontes microscópicas (7px, 8px, 9px, 10px) em toda a solução.
  - **Quadro de Missões**: Títulos e KPIs calibrados (22px bold / 11.5px rótulos / 50px linhas da tabela / 14.5px mono).
  - **Briefing de Voo**: Rota e número de voo (18px-20px), tags e itinerário multi-leg (11px-14px), W&B envelope e guias do EFB MSFS 2024 (11px-13.5px), Gatekeeper checklist e botões mestre (11.5px-15px).
  - **Financeiro**: Título 24px, métricas de fluxo de caixa 24px bold JetBrains Mono, histórico de transações e tabela 13.5px-14.5px.
  - **Mercado de Aeronaves**: Cards com modelos 19px, prefixos 15px, horas e combustível 12px, preços 22px bold, e painel de especificações técnicas 11.5px (rótulos) e 14.5px (valores).
  - **Hangar / Frota**: Lista de frota 13.5px-14px com badge ATIVA 11px, painel de especificações 12.5px-13.5px, e botões de inspeções regulatórias (TBO, 50h, 100h, conversão de cabine) ampliados para 12.5px bold e 36px de altura.
  - **Logs de Voo (Blackbox)**: Cards da lista 11.5px-14px, KPIs de telemetria 10.5px (rótulos) / 13.5px (valores mono), tabela de amostragem de telemetria 11.5px mono, e mapa de trajetória 11px.
  - **Janelas Modais**: `FerryFlightWindow` (cards e custos de pulo/voo 11.5px-22px), `RefuelWindow` (cotação e sliders 11.5px-26px) e `DebugOverlayWindow` (métricas de cockpit e falhas 10.5px-14px).

## 2026-11-08 - Modularização em UserControls (Fase 1) e Blindagem Multithread com IDbContextFactory (Fase 2)
- **Fase 1 (@frontend): Decomposição Modular de `MainWindow.xaml`**:
  - `MainWindow.xaml` foi reduzido de **2.451 linhas** para **409 linhas**, eliminando o acoplamento monolítico.
  - Criados 6 `UserControls` modulares dedicados na pasta `Views/`:
    - `MissionsView.xaml` & `MissionsView.xaml.cs`: Quadro de contratos, filtros dinâmicos, tabela split-screen e drawer de pré-despacho.
    - `BriefingView.xaml` & `BriefingView.xaml.cs`: Despacho operacional, itinerário multi-leg, envelope de massa e centragem (W&B), mapa tático e Gatekeeper pré-voo.
    - `FinanceView.xaml` & `FinanceView.xaml.cs`: Painel contábil, balanço de receitas/despesas e histórico de transações.
    - `MarketView.xaml` & `MarketView.xaml.cs`: Catálogo de aeronaves usadas, busca instantânea e especificações técnicas completas.
    - `HangarView.xaml` & `HangarView.xaml.cs`: Gestão de frota, manutenção corretiva, inspeções regulatórias periódicas (50h/100h), retífica TBO e cotação de seguros.
    - `FlightLogsView.xaml` & `FlightLogsView.xaml.cs`: Diário de bordo, reconstituição de trajetória GPS no mapa e telemetria segundo a segundo da Blackbox.
  - Estilos compartilhados e conversores centralizados no `App.xaml` para reuso universal em qualquer controle ou janela.
- **Fase 2 (@database & @backend): Blindagem Multithread com `IDbContextFactory`**:
  - Configurado `services.AddDbContextFactory<AppDbContext>` no container de injeção de dependência (`App.xaml.cs`).
  - Mantido registro `services.AddTransient<AppDbContext>` para garantir 100% de retrocompatibilidade com serviços e comandos síncronos legados.
  - Refatorado o `FlightMonitor` para operar com instâncias isoladas de `AppDbContext` criadas via factory nas operações de segundo plano:
    - Gravação contínua do estado do hangar (`UpdateHangarStateAsync`) a cada 60s em voo.
    - Cancelamento de voo e atualização de localização/missões (`CancelMonitoringAsync`).
    - Detecção e registro de colisão/perda total (`HandleTotalLossAsync`).
    - Busca de aeródromos mais próximos por GPS (`GetNearestAirportICAO`).
    - Encerramento e debriefing automático de voo (`FinalizeFlightAsync`).
  - Refatorado o `MetarService` para resolver estações meteorológicas mais próximas em tarefas assíncronas periódicas (timer de 5 minutos) utilizando contextos efêmeros e thread-safe.
  - Prevenção definitiva de exceções do tipo `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`.

## 2026-11-09 - Correção de Ambiguidade de Construtores no DI Container (FlightMonitor & MetarService)
- **Diagnóstico da Causa-Raiz**:
  - Na refatoração de blindagem multithread anterior, `FlightMonitor` e `MetarService` receberam construtores aceitando `IDbContextFactory<AppDbContext>`, mantendo seus construtores originais que recebiam `AppDbContext` para fins de retrocompatibilidade.
  - Como o container de DI (`App.xaml.cs`) possui registros tanto para `IDbContextFactory<AppDbContext>` quanto para `AppDbContext` (como transient), o `Microsoft.Extensions.DependencyInjection` não conseguia inferir qual construtor instanciar (mesmo número de parâmetros e todos resolvidos), disparando a exceção `System.InvalidOperationException: Unable to activate type 'FlightCareer.UI.Services.FlightMonitor'. The following constructors are ambiguous`.
- **Correções Aplicadas**:
  - Decorado o construtor primário (com `IDbContextFactory`) de `FlightMonitor` e `MetarService` com o atributo `[ActivatorUtilitiesConstructor]`.
  - Configurada a instanciação explícita de `FlightMonitor` e `MetarService` em `App.xaml.cs` utilizando `sp.GetRequiredService<IDbContextFactory<AppDbContext>>()`, garantindo resolução determinística sem ambiguidade.
- **Validação**: Compilação realizada com sucesso (`dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj`) com 0 erros.

## 2026-11-09 - Correção da Geração de Missões e Desbloqueio do Voo de Translado (Ferry Flight)
- **Problema 1: Apenas Missões de Instrução em SBSP e Falha Geral de Destinos**:
  - **Causa-Raiz 1 (Case-Sensitivity no SQLite)**: Em `MsfsScenerySyncService.cs`, o comando `UPDATE Airports SET IsInSimulator = ... WHERE Id = @Id` utilizava `u.Id.ToString()`, que em .NET produz GUID em minúsculas, enquanto o EF Core persistiu os IDs no SQLite em maiúsculas. Devido à comparação textual case-sensitive padrão do SQLite, o update afetou 0 linhas em todos os 30.291 aeroportos (`IsInSimulator` permaneceu `0`).
  - **Causa-Raiz 2 (Filtro Exclusivo no Gerador)**: O `MissionGenerator` aplicava `query.Where(a => a.IsInSimulator)`. Com 0 aeroportos marcados, todas as subcategorias que exigiam aeroportos de destino falhavam (`validDestinations.Count == 0`). Apenas a subcategoria `3.4.1` (Voo de Instrução Local: origem ➔ origem a 10 NM) não requeria destino externo, resultando em 100% de missões de instrução.
  - **Causa-Raiz 3 (AllowedDestinationTiers Restrito)**: No `MissionRules.cs`, o Tier 2 (onde SBSP se enquadra) permitia apenas destinos de Tiers 1 e 2 (`AllowedDestinationTiers = { 1, 2 }`), bloqueando destinos municipais e rurais (Tiers 3 e 4), que representam mais de 90% dos aeródromos a até 200 NM.
  - **Solução**:
    - Ajustado `MsfsScenerySyncService.cs` para `WHERE Id = UPPER(@Id)` e `u.Id.ToString().ToUpperInvariant()`.
    - Executada a sincronização dos 25.975 aeródromos validados no MSFS 2024.
    - Ampliado `AllowedDestinationTiers`: Tier 1 (`{1, 2, 3}`), Tier 2 (`{1, 2, 3, 4}`), Tier 3 (`{1, 2, 3, 4}`) e Tier 4 (`{2, 3, 4}`).
    - Adicionada checagem defensiva em `MissionGenerator.cs` para não zerar destinos caso a sincronização de simulador ainda não esteja ativa.
- **Problema 2: Bloqueio do Voo de Translado (Ferry Flight) com Destino Selecionado**:
  - **Causa-Raiz**: O método `_pilotRepository.GetAllAsync()` não realizava `.Include(p => p.Hangar).ThenInclude(h => h.AircraftType)`. Como consequência, `_pilot.Hangar` estava vazio, fazendo `_aircraft` ser `null`. Ao clicar para voar ou pular o ferry, a validação `if (!HasValidDestination || _destAirport == null || _pilot == null || _aircraft == null)` caía na mensagem genérica `Selecione um destino válido primeiro`, induzindo ao erro de que o problema era o destino.
  - **Solução**:
    - `PilotRepository.cs`: Atualizados `GetAllAsync()` e `GetByIdAsync()` com `.Include(p => p.Hangar).ThenInclude(h => h.AircraftType)`.
    - `FerryFlightViewModel.cs`: Implementado carregamento robusto do piloto e aeronave ativa no banco, com fallbacks caso o hangar não venha pré-carregado.
    - Separadas as mensagens de validação em `FerryFlightViewModel.cs` para indicar com precisão a causa real (destino, piloto ou aeronave não selecionada).
- **Validação**: Build executado com sucesso e verificação de integridade no banco de dados.

## 2026-11-09 - Clean Architecture: Implementação das Fases 1, 2 e 3
- **Fase 1: Criação de `FlightCareer.Application` e Desacoplamento de Casos de Uso**:
  - Criado o projeto `FlightCareer.Application.csproj` (.NET 10.0), integrado à solução `FlightCareer.slnx`.
  - Definida a interface `IAppDbContext` em `Application/Interfaces/IAppDbContext.cs` desacoplando as operações de banco do EF Core direto.
  - Criadas interfaces de serviços essenciais: `IMissionGenerator`, `IHubDataService`, `IMsfsScenerySyncService`, `IDataSeeder`.
  - Criados DTOs em `Application/DTOs/`: `FlightOutcome`, `MsfsSyncResult`, `MissionBriefingDto`, `FlightTelemetryDto`.
  - Migrados serviços de negócio e orquestração de `Infrastructure` para `Application/Services`:
    - `MissionGenerator.cs` e `MissionRules.cs` (geração de missões e regras de distribuição)
    - `G20Destinations.cs` e `HubDataService.cs` (hubs e rotas mundiais)
    - `FlightService.cs` (processamento de voo, XP e pagamentos)
    - `EconomyServices.cs` e `FuelPriceService.cs` (economia dinâmica, inflação e combustível)
  - Resolvida colisão de namespace entre `FlightCareer.Application` e `System.Windows.Application` no WPF com qualificadores explícitos e `GlobalUsings.cs`.
- **Fase 2: Desacoplamento do SimConnect**:
  - Criada a interface `ISimConnectService` em `FlightCareer.Application/Interfaces/ISimConnectService.cs`, padronizando conexão, desconexão, envio de comandos (fuel, payload, pause, failures, teleport) e recepção de telemetria.
  - Implementada a interface `ISimConnectService` em `SimConnectService.cs` na camada UI, suportando tanto `IntPtr` genérico de janela quanto `Window` do WPF.
  - Registrado `services.AddSingleton<ISimConnectService>(sp => sp.GetRequiredService<SimConnectService>())` no DI container em `App.xaml.cs`.
- **Fase 3: Value Objects e Enums Ricos no Domínio**:
  - Implementados Value Objects imutáveis em `FlightCareer.Domain/ValueObjects/`:
    - `Coordinates`: Latitude/Longitude com validação, cálculo esférico de distância Haversine (`DistanceTo`) e rumo (`BearingTo`).
    - `WeightLbs`: Representação tipada de peso em libras com conversões automáticas (`ToKg`, `FromKg`) e operadores aritméticos.
    - `FuelGallons`: Representação tipada de volume de combustível com cálculo de densidade e massa (`ToWeightLbs` para Avgas e Jet-A) e conversão para litros.
    - `Money`: Representação tipada monetária em USD com formatação contábil padrão do sistema (`FormatUsd`) e operadores de soma/multiplicação.
  - Implementados Enums de domínio em `FlightCareer.Domain/Enums/`:
    - `AirportClassification`: Tiers 1 a 4.
    - `MissionType`: Instrução, SAE, Táxi Aéreo, Linha Aérea Regional, Linha Aérea Regular, Voo Ferry.
    - `RunwaySurface`: Asfalto, Concreto, Grama, Terra, Cascalho, Água, Outros.
    - `FlightRules`: VFR, MVFR, IFR, LIFR.
  - Enriquecidas entidades de domínio (`Airport.cs` com `Coordinates` e `Classification`; `Runway.cs` com `SurfaceType`) preservando total compatibilidade com o esquema do EF Core / SQLite.
- **Validação de Build**: Compilação realizada com sucesso (`dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj`) com 0 erros.

## 2026-11-09 - Importação Little Navmap (MSFS), Viabilidade Física de Pista e Economia Viva (Mundo Vivo)
- **Importação Direta Little Navmap + Enriquecimento CSV (83.095 Aeródromos & 47.076 Pistas)**:
  - Implementada em `DataSeeder.cs` a importação de alta performance via SQLite nativo (`ATTACH DATABASE`) conectando diretamente à base de cenários compilada do Little Navmap (`little_navmap_msfs24.sqlite` / `little_navmap_msfs.sqlite` / `little_navmap_navigraph.sqlite` em `%APPDATA%\ABarthel\little_navmap_db` ou `C:\Program Files\Little Navmap\little_navmap_db`).
  - Importados **83.095 aeródromos nativos do MSFS 2024** com idents oficiais do simulador (`ICAO = ident`), elevação real em pés (`ElevationFt`), coordenadas exatas e classificação de Tier (Tiers 1 a 4).
  - Importadas **47.076 pistas** com comprimento, largura, superfícies normalizadas (Asphalt, Concrete, Grass, Dirt, Gravel, etc.) e iluminação noturna.
  - Enriquecimento automático ultra-rápido com municípios (`municipality`) e países (`iso_country`) a partir de `airports.csv`.
  - Recriação limpa do banco de dados `flightcareer.db` no diretório de execução, garantindo que 100% dos aeródromos da carreira existam no MSFS e utilizem a nomenclatura exata do simulador.
- **Algoritmo de Viabilidade Física (Aeronave vs Pista)**:
  - Implementados `IRunwayViabilityCalculator` e `RunwayViabilityCalculator`:
    $$\text{RequiredRunway} = \text{MinRunwayBase} \times \left(1 + \frac{\max(0, \text{ElevationFt})}{1000} \times 0.10\right) \times \text{SafetyFactor} \times \text{SurfacePenalty}$$
    - `SafetyFactor`: 1.25x para VFR Geral / Instrução (Cat 1/2) e 1.67x para RBAC 135 / Táxi Aéreo / Linha Aérea (Cat 3/4/5).
    - `ElevationFt`: Correção de densidade do ar (+10% a cada 1.000 pés de elevação).
    - `SurfacePenalty`: Pistas de terra/grama/cascalho aplicam atrito de +25% a +35% de pista requerida.
    - Restrição de Pavimento: Aeronaves pesadas/jatos comerciais (Cat 4 e 5) exigem pista pavimentada rígida (Asfalto/Concreto).
- **Prevenção de Absurdos Operacionais (Aeronave vs Missão vs Pista)**:
  - `MissionGenerator`: Filtra aeródromos de destino com base em `MinRunwayDest` e `RequiresHardSurface`. Voos de Linha Regular (B737, A320) são gerados exclusivamente para grandes hubs (Tier 1 e Tier 2 pavimentados), impedindo geração de missões para fazendas (Tier 4). Missões de fazenda/agro focam em Tiers 3 e 4.
  - `MainViewModel` (Aceite de Contratos): Valida se a carga/passageiros do pacote selecionado excede a capacidade física (`MaxPayloadLbs`, `MaxPax`) da aeronave ativa, alertando o piloto e evitando sobrecargas absurdas.
  - `BriefingViewModel` (Gatekeeper Pré-Voo): Incorporado o 5º item do checklist ("VIABILIDADE FÍSICA DE PISTA"). Se o comprimento da pista ou superfície de destino for insuficiente para a aeronave, o Gatekeeper bloqueia o despacho e detalha o motivo operacional.
- **Mundo Vivo: Ciclo de Vida e Churn Dinâmico das Missões**:
  - Implementado timer em segundo plano (a cada 60s) que remove contratos com TTL vencido (`ExpirationTime < Now`), simula pilotos virtuais concorrentes (NPCs) aceitando contratos da região (5% a 10% de churn por ciclo) e repõe proceduralmente novas oportunidades para manter o quadro de contratos aquecido e dinâmico.
- **Custos Operacionais Realistas (Combustível, Taxas de Pouso, Depreciação)**:
  - **Combustível Gasto**: Apurado pelo SimConnect (`initialFuel - finalFuel`) e cobrado no pouso com base na cotação diária do combustível (`AVGAS` / `Jet-A`), debitando o saldo e registrando despesa.
  - **Taxas de Pouso**: Proporcionais ao MTOW estimado da aeronave e porte do aeroporto de destino (Tier 1: $250 + $0.035/lb; Tier 2: $80 + $0.015/lb; Tier 3: $25 + $0.005/lb; Tier 4 Fazendas/Remotas: **GRATUITO / $0**).
  - **Depreciação Patrimonial**: Horas de voo debitadas do TBO de motor e inspeções de 50h/100h com interdição automática de aeronave caso vençam sem manutenção.
- **Validação de Build**: Compilação executada com sucesso (`dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj`) com 0 erros.

## 2026-11-09 - Correção de Resolução de Dependência DI (IAppDbContext)
- **Diagnóstico da Causa-Raiz**:
  - Ao carregar o perfil de piloto (`LoginViewModel.cs:69`), a ativação do `MainViewModel` tentava resolver o serviço `MissionGenerator` e `FlightService`.
  - Ambos dependem da interface `IAppDbContext` no construtor.
  - No container de injeção de dependência (`App.xaml.cs`), havia apenas o registro de `AppDbContext` e `IDbContextFactory<AppDbContext>`, faltando a amarração da interface `IAppDbContext`, disparando a exceção: `Unable to resolve service for type 'FlightCareer.Application.Interfaces.IAppDbContext' while attempting to activate 'FlightCareer.Application.Services.MissionGenerator'`.
- **Correção Aplicada**:
  - Registrado `services.AddTransient<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());` em `App.xaml.cs`.
- **Validação (@reviewer)**: Build executado com sucesso (`dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj`) com 0 erros.

## 2026-11-09 - Filtro Exclusivo de Aeródromos/Pistas para Aviões e Desambiguação de Construtor (FlightMonitor)
- **Desambiguação de Construtor de Injeção de Dependência (`FlightMonitor`)**:
  - **Diagnóstico**: Ocorria erro em tempo de execução (`Unable to activate type 'FlightCareer.UI.Services.FlightMonitor'. The following constructors are ambiguous: ...`) ao carregar perfil quando compilado com construtores concorrentes (`IDbContextFactory<AppDbContext>` vs `AppDbContext`).
  - **Resolução**: Padronizado construtor único em `FlightMonitor.cs` decorado com `[ActivatorUtilitiesConstructor]` recebendo estritamente `IDbContextFactory<AppDbContext>`, eliminando o construtor legado com `AppDbContext` direto. No `App.xaml.cs`, explicitado o registro singleton com factory lambda resolvendo o factory de contexto.
- **Filtragem Estrita de Aeródromos e Pistas para Aviões de Asa Fixa**:
  - **Diagnóstico**: A base nativa do Little Navmap exportada do MSFS 2024 contém mais de 83.000 instalações, sendo 42.861 helipontos (hospitais, terraços de edifícios, plataformas de petróleo e subestações sem pistas físicas de rolagem) e 594 bases puramente aquáticas. Como o FlightCareer é focado exclusivamente em aviação de asa fixa (aviões), essas pistas poluiam a geração de missões e translados.
  - **Filtro Little Navmap (`DataSeeder.cs`)**:
    - Aeródromos: `(num_runway_hard > 0 OR num_runway_soft > 0) AND longest_runway_length >= 1000 AND is_closed = 0`, reduzindo a base para exatamente **38.598 aeródromos válidos para aviões**.
    - Pistas: Exclusão de superfícies de água (`r.surface != 'W'`), comprimento mínimo de 800 pés (`r.length >= 800`) e vínculo direto com aeródromos válidos, totalizando **45.240 pistas terrestres operacionais**.
  - **Filtro Fallback CSV (`SeedAirportsAsync` / `SeedRunwaysAsync`)**:
    - Exclusão de tipos `heliport`, `seaplane_base`, `balloonport` e `closed`.
    - Pistas com superfície aquática (`WAT`) ou comprimento $< 800$ pés descartadas.
- **Recriação do Banco de Dados (`flightcareer.db`)**:
  - Banco anterior removido para permitir a recriação limpa via `Database.EnsureCreatedAsync()`, garantindo a criação de todas as tabelas do EF Core (`Pilots`, `PilotAircrafts`, `Missions`, `Flights`, `FinancialLedgers`, `Airports`, `Runways`, `Aircrafts`) e re-seeding automático no primeiro carregamento.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com validação de compilação sem erros (`dotnet build`), sanitização SQL e conformidade SOLID.

## 2026-11-10 - Normalização Aeronáutica Global, MTOW Real, Estabilidade do Mapa e Refinamento de UI
- **Normalização e Saneamento de Dados Aeronáuticos (AviationDataNormalizer)**:
  - Implementado `AviationDataNormalizer` em `FlightCareer.Infrastructure/Services`:
    - Mapeamento determinístico de prefixos OACI (Anexo 10) para códigos de país ISO 3166-1 (prefixos brasileiros `SB`, `SS`, `SW`, `SD`, `SJ`, `SN`, `SI` mapeiam sempre para `BR`; prefixos globais mapeados para `US`, `CA`, `DE`, `FR`, `GB`, `AR`, etc.).
    - Extração canônica de municípios a partir de padrões oficiais de nomenclatura aeronáutica (`Aeroporto de Ribeirão Branco` $\rightarrow$ `Ribeirão Branco`, `Aeroporto Municipal de Alexânia` $\rightarrow$ `Alexânia`).
    - Eliminação de códigos de setor FIR/ARTCC (`SB`, `SS`, `K1`..`K7`) gravados indevidamente no lugar de países e municípios.
  - Atualização automática e idempotente via `EnsureSchemaUpdatedAsync()` e integração ao pipeline de `DataSeeder`.
  - Redução drástica de aeroportos sem município no mundo (de >16.000 para apenas 11).
- **Especificações Técnicas de MTOW Real (`AircraftWeightSpecs`)**:
  - Criada a classe `AircraftWeightSpecs` em `FlightCareer.Domain/Services` com os pesos operacionais reais (MTOW e Empty Weight) de toda a frota homologada (ex: Cessna 152 = 1.670 lbs, Cessna 172 = 2.550 lbs, A320neo = 174.165 lbs), eliminando a fórmula arbitrária anterior de `+ 2000`.
  - Sincronização dinâmica com `data.MaxGrossWeightLbs` do SimConnect no `BriefingViewModel`.
  - Calibração de tolerâncias rigorosas no despacho (Payload: $\max(15\text{ lbs}, 2\%)$; Combustível: $\max(1\text{ gal}, 2\%)$).
- **Estabilidade e Correção do Zoom no Mapa Tático (`FlightMapControl`)**:
  - Desacoplado o tick de telemetria da aeronave (1s) do auto-enquadramento da rota: `OnAircraftPosChanged` agora atualiza apenas a posição do marcador sem acionar `SetZoomToFitRect`. O enquadramento automático ocorre exclusivamente ao trocar de destino ou carregar a rota, permitindo navegação e zoom livres pelo piloto.
  - Limitado `MaxZoom = 16` no mapa tático, evitando erros 404 e telas azuis de "Map data not yet available".
- **Refinamento de UI e Experiência do Usuário**:
  - **Briefing de Voo**:
    - Removidas mensagens de instrução de "abas do EFB", focando nos cards visuais superiores (Assentos, Carga, Tanques) e detalhando estritamente PESO PARA PAX e PESO PARA CARGA.
    - Botão de W&B fixado com texto limpo `📋 COPIAR W&B P/ O SIMULADOR`.
    - Cabeçalho padronizado para `SIMULADOR CONECTADO`.
  - **Header Principal & Navegação**:
    - Removido ` | PRONTO PARA MISSÃO`, exibindo apenas o indicador com a bolinha colorida e `SIMULADOR CONECTADO` / `SIMULADOR DESCONECTADO`.
    - Removido o link de Ferry da sidebar principal, mantendo-o no Quadro de Missões e adicionando botão dedicado `✈ VOO DE TRANSLADO (FERRY)` no card da aeronave no Hangar.
  - **Quadro de Missões (`MissionsView`)**:
    - Alargada a coluna DESTINO para 160px e otimizadas as demais larguras para acomodar ICAO, municípios e valores numéricos sem corte de texto.
  - **HUD Overlay Flutuante (`DebugOverlayWindow`)**:
    - Incorporado um mini-GPS / moving map em tempo real (150px) exibindo aeronave, proa e rota navegada durante o voo.
- **Higienização de Código Obsoleto**:
  - Removidos arquivos e dependências de cálculo de viabilidade física de pista (`RunwayViabilityCalculator`, `IRunwayViabilityCalculator`, enum `RunwaySurface`), desvinculando registros de DI no `App.xaml.cs` e restaurando a escala do Gatekeeper para 4/4 sistemas.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa em todos os projetos da solução (`dotnet build src/FlightCareer.slnx` com 0 erros).

## 2026-11-11 - Ícones Vetoriais SVG/StreamGeometry, Correção de Mojibake e Unificação do Manifesto Multi-Leg
- **Diagnóstico e Refatoração de UX (Redundância no Briefing)**:
  - **Problema**: A coluna esquerda do Briefing exibia dois painéis verticais empilhados ("ITINERÁRIO DINÂMICO MULTI-LEG" e "CONTRATOS NO MANIFESTO") que repetiam exatamente os mesmos aeroportos, pesos e passageiros em listas separadas com scrolls concorrentes.
  - **Resolução**: Unificação em um único componente tático: **"PLANO DE VOO & MANIFESTO OPERACIONAL"**:
    - Cada card de perna (`FlightLegModel`) consolida os dados operacionais (`[LEG X]`, `Origem ➔ Destino`, Distância, ETE) com os dados comerciais do contrato (`MissionTitle`, Recompensa formatada em verde, Carga a bordo e Resumo de escala).
    - Botão ghost `[✕]` de cancelamento de contrato integrado diretamente ao card da perna (vinculado a `RemoveMissionCommand` via `AssociatedMission`), permitindo descarte imediato da missão e retorno ao mercado.
    - Removidos scrolls aninhados em favor de um `ScrollViewer` vertical único e fluido.
- **Catálogo de Ícones Vetoriais Nativos (`App.xaml`)**:
  - Implementado conjunto de geometrias vetoriais reutilizáveis em `StreamGeometry` de padrão cockpit/avionics dark mode:
    - `IconMissions`, `IconBriefing`, `IconMarket`, `IconHangar`, `IconFinance`, `IconBlackbox`, `IconDebug`, `IconOverlay`, `IconLogout`, `IconClose`, `IconPlane`, `IconScale`, `IconShield`, `IconChevronDown`, `IconRefresh`, `IconSeats`, `IconCargo`, `IconFuel`, `IconRocket`.
  - Substituídos todos os emojis de baixa legibilidade e glifos dependentes de fontes do sistema (`🛫`, `📦`, `⚖️`, `🛡️`, etc.) por caminhos vetoriais nítidos em qualquer DPI.
- **Eliminação Total de Mojibake e Correção de Encoding (`MainWindow.xaml`)**:
  - Sanados todos os caracteres corrompidos decorrentes de dupla codificação UTF-8 (`OPERAÃ‡Ã•ES` $\rightarrow$ `OPERAÇÕES`, `Quadro de MissÃµes` $\rightarrow$ `Quadro de Missões`, `â®µ` $\rightarrow$ `IconChevronDown`, `ðŸ–¥ï¸` $\rightarrow$ `IconOverlay`).
  - Menu lateral padronizado com `Path` vetorial sincronizado ao `Foreground` reativo dos botões.
  - Botão de acesso rápido ao HUD Overlay no cabeçalho atualizado com vetor limpo.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com 0 erros de compilação (`dotnet build src\FlightCareer.slnx`).

## 2026-11-11 - HUD Overlay Minimalista, Sim Rate/Pausa, Pouso de Emergência e Clustering Tático no Mapa
- **Refinamento de UX do HUD Overlay (`DebugOverlayWindow`)**:
  - Limpeza completa do mapa tático: removidos badges textuais de altitude, velocidade e proa que acompanhavam a aeronave; exibição estrita da silhueta vetorial rotacionando com a proa (em voo) ou do círculo de radar ping (em solo).
  - Remoção de labels de pista/waypoint em excesso, mantendo apenas a rota geodésica e nós limpos.
  - O bloco de Flaps agora exibe estritamente `FLAPS` (removida a poluição de freio).
  - Temperatura do óleo convertida e padronizada em Fahrenheit (`°F`), condizente com os mostradores analógicos dos painéis de aeronaves de aviação geral (Cessna 152/172).
  - Desacoplado o arraste da janela: o arrasto é feito exclusivamente pelo cabeçalho do HUD, permitindo pan e zoom livres sobre o mapa tático via scroll e mouse sem mover a janela.
- **Fases de Voo Encurtadas**:
  - Títulos de fase encurtados para caber perfeitamente na interface: `"VOANDO"`, `"PREPARAÇÃO"`, `"INICIANDO (10s)"`, `"VOO ATIVO"`, `"POUSO (FPM)"`, `"FINALIZANDO (5s)"`, `"CANCELANDO (5s)"`.
- **Contagem Fiel de Horas de Voo (Anti-Pausa e Aceleração de Simulação)**:
  - Implementado acumulador de tempo de simulação efetivo (`_simulatedFlightTimeSeconds`), integrando delta de tempo ponderado pela taxa de aceleração (`SimulationRate`).
  - Pausa de simulação (`IS SIMULATION PAUSED`) detectada via SimConnect, interrompendo a contagem de horas e evitando horas fakes ou desgaste indevido de TBO ao pausar o jogo.
- **Detecção de Apagamento / Pane de Motor em Voo (100% RPM)**:
  - Detectado desligamento/quebra de motor enquanto a aeronave está no ar (`_isAirborne && !IsEngineRunning`), aplicando degradação severa imediata (`EngineCondition <= 15%`) e disparando alarme crítico de pane.
- **Pouso de Emergência Fora de Pista (Off-Field Landing)**:
  - Validação da distância de pouso até a pista homologada mais próxima ($> 3.0\text{ NM}$).
  - Pousos no mato/terreno não preparado não concluem nem pagam missões (carga fica retida no aeroporto mais próximo aguardando resgate).
  - Aplicação de danos severos de terreno despreparado nos pneus ($-35\%$) e na fuselagem ($-20\%$).
- **Clustering Dinâmico e Seleção de Missões pelo Mapa (`FlightMapControl` / `MissionsView`)**:
  - Altura do viewport do mapa fixada em $340\text{px}$, eliminando variações de tamanho ao alternar contratos.
  - Implementado agrupamento euclidiano de destinos de missões em bolhas numéricas ciano (`[ N ]`) quando a distância em pixels for $< 45\text{px}$.
  - Clique no cluster aproxima a câmera (`Zoom += 2`); clique no nó individual seleciona a missão no quadro e desenha o arco geodésico curvo em 20 segmentos (Great-Circle via Haversine).
  - Botão de mira flutuante (`🎯`) para recentralizar instantaneamente na base/aeronave do jogador.

## 2026-11-11 - Mercado de Usados Avançado (Pre-Buy Inspection), Subsistema de Aviônicos e Venda de Aeronaves
- **Novo Subsistema de Aviônicos & Elétrica (`AvionicsCondition`)**:
  - Adicionado o quarto subsistema à entidade `PilotAircraft`: `AvionicsCondition`.
  - Migração de schema dinâmica automática adicionada em `AppDbContext.EnsureSchemaUpdatedAsync()`.
  - Simulação de desgaste de aviônicos em `WearAndFailureService` com base em horas de voo e estresse elétrico.
  - Persistência obrigatória de todos os 4 subsistemas (`EngineCondition`, `TiresCondition`, `AirframeCondition`, `AvionicsCondition`) e `LandingsCount` em `FlightService.ProcessFlightFinishAsync`, `UpdateHangarStateAsync`, `CancelMonitoringAsync` e `HandleTotalLossAsync`.
- **Reestruturação do Painel de Inspeção Técnica Pré-Compra (`MarketView`)**:
  - Substituído o espaço vazio da coluna direita por um relatório modular completo de inspeção mecânica:
    - Card de identificação com silhueta vetorial, prefixo, modelo e ano de fabricação.
    - 4 diagnósticos modulares com barras de progresso cromáticas e métricas:
      1. Célula / Fuselagem (TTAF e histórico de incidentes).
      2. Grupo Motopropulsor (TSMO e TBO).
      3. Trem de Pouso & Pneus (estado de rolagem e amortecedor).
      4. Aviônicos & Elétrica (barramentos DC, COMM/NAV e bateria).
    - Box financeiro com "Custo Estimado de Revisão Pós-Compra".
    - Sticky Footer com botão de compra travado no rodapé (`[ COMPRAR POR $ XX,XXX.XX ]`).
- **Catálogo de Ofertas e Filtros no Mercado**:
  - Cards de aeronaves compactos de alta densidade visual com badge de categoria de motorização (`CategoryBadge`).
  - Filtros rápidos horizontais em chips (`[TODAS]`, `[MONOMOTOR]`, `[BIMOTOR]`, `[TURBOÉLICE]`, `[ATÉ $100K]`).
  - Dropdown de ordenação por Menor Preço, Maior Condição e Menos Horas (TTAF).
- **Módulo de Venda de Aeronaves Próprias (Meus Anúncios)**:
  - Controle de abas de 1º nível: `[ ✈️ MERCADO DE USADOS (COMPRAR) ]` e `[ 🏷️ MEUS ANÚNCIOS / VENDER AERONAVE ]`.
  - Avaliação de frota pelo índice Bluebook aeronáutico com base no estado dos 4 subsistemas.
  - Taxa de comissão da corretora de 8% e cálculo de valor líquido a receber.
  - Comando atômico de venda: crédito no saldo do piloto, transação financeira de receita e remoção da aeronave do hangar.
- **Padronização Monetária Global**:
  - Moeda padronizada estritamente em Dólar Americano (`$`) em todas as threads da aplicação via `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US")`.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com 0 erros de compilação (`dotnet build src\FlightCareer.slnx`).


## 2026-11-11 - Correção de Loop Infinito e Travamento no Briefing Multi-Missão
- **Diagnóstico da Causa Raiz do Travamento**:
  - Ao aceitar 2 ou mais contratos de voo (especialmente partindo da mesma base ou com destinos múltiplos), o método `RebuildItineraryAndValidateAsync()` em `BriefingViewModel.cs` entrava em um **loop infinito na UI thread**.
  - O código utilizava `remainingCrossMissions.RemoveAll(m => m.OriginICAO == currentHop && m.DestinationICAO == nextDest)`. Após processar a primeira missão e avançar `currentHop` para o destino da primeira perna, a busca da segunda missão encontrava uma missão com `OriginICAO == originIcao` (diferente de `currentHop`). A cláusula `RemoveAll` nunca encontrava correspondência, nunca removia a missão da lista, e o `while (remainingCrossMissions.Any())` executava indefinidamente, congelando a aplicação.
- **Resolução Implementada (`@backend`)**:
  - Substituição da remoção por predicado falho pela **remoção atômica da instância processada**: `remainingCrossMissions.Remove(nextMission)` e `remainingCrossMissions.Remove(sameDestMission)`.
  - Adicionada salvaguarda de segurança obrigatória: `safetyGuard++ < 50`, impedindo loops em qualquer cenário futuro.
  - Implementado algoritmo inteligente que suporta múltiplos contratos para o mesmo destino (compartilhando trecho da rota), escalas técnicas consecutivas e decolagens partindo da base da empresa.
  - Desacoplado o cálculo de carga e passageiros por card individual (`legPax` e `legCargo` agora exibem os valores exatos daquele contrato em vez de somar toda a frota no card da perna).
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com 0 erros de compilação (`dotnet build src\FlightCareer.slnx`).

## 2026-11-11 - HUD Overlay Minimalista, Sim Rate/Pausa, Pouso de Emergência e Clustering Tático no Mapa
- **Refinamento de UX do HUD Overlay (`DebugOverlayWindow`)**:
  - Limpeza completa do mapa tático: removidos badges textuais de altitude, velocidade e proa que acompanhavam a aeronave; exibição estrita da silhueta vetorial rotacionando com a proa (em voo) ou do círculo de radar ping (em solo).
  - Remoção de labels de pista/waypoint em excesso, mantendo apenas a rota geodésica e nós limpos.
  - O bloco de Flaps agora exibe estritamente `FLAPS` (removida a poluição de freio).
  - Temperatura do óleo convertida e padronizada em Fahrenheit (`°F`), condizente com os mostradores analógicos dos painéis de aeronaves de aviação geral (Cessna 152/172).
  - Desacoplado o arraste da janela: o arrasto é feito exclusivamente pelo cabeçalho do HUD, permitindo pan e zoom livres sobre o mapa tático via scroll e mouse sem mover a janela.
- **Fases de Voo Encurtadas**:
  - Títulos de fase encurtados para caber perfeitamente na interface: `"VOANDO"`, `"PREPARAÇÃO"`, `"INICIANDO (10s)"`, `"VOO ATIVO"`, `"POUSO (FPM)"`, `"FINALIZANDO (5s)"`, `"CANCELANDO (5s)"`.
- **Contagem Fiel de Horas de Voo (Anti-Pausa e Aceleração de Simulação)**:
  - Implementado acumulador de tempo de simulação efetivo (`_simulatedFlightTimeSeconds`), integrando delta de tempo ponderado pela taxa de aceleração (`SimulationRate`).
  - Pausa de simulação (`IS SIMULATION PAUSED`) detectada via SimConnect, interrompendo a contagem de horas e evitando horas fakes ou desgaste indevido de TBO ao pausar o jogo.
- **Detecção de Apagamento / Pane de Motor em Voo (100% RPM)**:
  - Detectado desligamento/quebra de motor enquanto a aeronave está no ar (`_isAirborne && !IsEngineRunning`), aplicando degradação severa imediata (`EngineCondition <= 15%`) e disparando alarme crítico de pane.
- **Pouso de Emergência Fora de Pista (Off-Field Landing)**:
  - Validação da distância de pouso até a pista homologada mais próxima ($> 3.0\text{ NM}$).
  - Pousos no mato/terreno não preparado não concluem nem pagam missões (carga fica retida no aeroporto mais próximo aguardando resgate).
  - Aplicação de danos severos de terreno despreparado nos pneus ($-35\%$) e na fuselagem ($-20\%$).
- **Clustering Dinâmico e Seleção de Missões pelo Mapa (`FlightMapControl` / `MissionsView`)**:
  - Altura do viewport do mapa fixada em $340\text{px}$, eliminando variações de tamanho ao alternar contratos.
  - Implementado agrupamento euclidiano de destinos de missões em bolhas numéricas ciano (`[ N ]`) quando a distância em pixels for $< 45\text{px}$.
  - Clique no cluster aproxima a câmera (`Zoom += 2`); clique no nó individual seleciona a missão no quadro e desenha o arco geodésico curvo em 20 segmentos (Great-Circle via Haversine).
  - Botão de mira flutuante (`🎯`) para recentralizar instantaneamente na base/aeronave do jogador.

## 2026-11-11 - Mercado de Usados Avançado (Pre-Buy Inspection), Subsistema de Aviônicos e Venda de Aeronaves
- **Novo Subsistema de Aviônicos & Elétrica (`AvionicsCondition`)**:
  - Adicionado o quarto subsistema à entidade `PilotAircraft`: `AvionicsCondition`.
  - Migração de schema dinâmica automática adicionada em `AppDbContext.EnsureSchemaUpdatedAsync()`.
  - Simulação de desgaste de aviônicos em `WearAndFailureService` com base em horas de voo e estresse elétrico.
  - Persistência obrigatória de todos os 4 subsistemas (`EngineCondition`, `TiresCondition`, `AirframeCondition`, `AvionicsCondition`) e `LandingsCount` em `FlightService.ProcessFlightFinishAsync`, `UpdateHangarStateAsync`, `CancelMonitoringAsync` e `HandleTotalLossAsync`.
- **Reestruturação do Painel de Inspeção Técnica Pré-Compra (`MarketView`)**:
  - Substituído o espaço vazio da coluna direita por um relatório modular completo de inspeção mecânica:
    - Card de identificação com silhueta vetorial, prefixo, modelo e ano de fabricação.
    - 4 diagnósticos modulares com barras de progresso cromáticas e métricas:
      1. Célula / Fuselagem (TTAF e histórico de incidentes).
      2. Grupo Motopropulsor (TSMO e TBO).
      3. Trem de Pouso & Pneus (estado de rolagem e amortecedor).
      4. Aviônicos & Elétrica (barramentos DC, COMM/NAV e bateria).
    - Box financeiro com "Custo Estimado de Revisão Pós-Compra".
    - Sticky Footer com botão de compra travado no rodapé (`[ COMPRAR POR $ XX,XXX.XX ]`).
- **Catálogo de Ofertas e Filtros no Mercado**:
  - Cards de aeronaves compactos de alta densidade visual com badge de categoria de motorização (`CategoryBadge`).
  - Filtros rápidos horizontais em chips (`[TODAS]`, `[MONOMOTOR]`, `[BIMOTOR]`, `[TURBOÉLICE]`, `[ATÉ $100K]`).
  - Dropdown de ordenação por Menor Preço, Maior Condição e Menos Horas (TTAF).
- **Módulo de Venda de Aeronaves Próprias (Meus Anúncios)**:
  - Controle de abas de 1º nível: `[ ✈️ MERCADO DE USADOS (COMPRAR) ]` e `[ 🏷️ MEUS ANÚNCIOS / VENDER AERONAVE ]`.
  - Avaliação de frota pelo índice Bluebook aeronáutico com base no estado dos 4 subsistemas.
  - Taxa de comissão da corretora de 8% e cálculo de valor líquido a receber.
  - Comando atômico de venda: crédito no saldo do piloto, transação financeira de receita e remoção da aeronave do hangar.
- **Padronização Monetária Global**:
  - Moeda padronizada estritamente em Dólar Americano (`$`) em todas as threads da aplicação via `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US")`.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com 0 erros de compilação (`dotnet build src\FlightCareer.slnx`).




## 2026-11-12 - Correção do Cálculo de Payload e Combustível Não Utilizável (SimConnect SDK MSFS 2024/2020)
- **Diagnóstico da Causa Raiz**:
  - No MSFS SDK, a variável `TOTAL WEIGHT` representa a massa bruta total da aeronave na rampa, que inclui tanto o combustível utilizável quanto o combustível inutilizável (*Unusable Fuel*, 1,5 galões = 9 lbs no C152) e fluidos de motor.
  - O cálculo anterior do sistema (`reportedSimPayload = TotalWeight - EmptyWeight - fuelWeight`) não deduzia o combustível inutilizável, reportando 179 lbs no software quando o piloto no simulador estava configurado com exatamente 170 lbs.
- **Implementação do Contrato SimConnect**:
  - Adicionados os campos `UnusableFuelGallons` e `FuelWeightPerGallon` na struct sequencial `SimTelemetry` (`Pack = 1`).
  - Mapeadas as SimVars oficiais `UNUSABLE FUEL TOTAL QUANTITY` (Gallons) e `FUEL WEIGHT PER GALLON` (Pounds) em `SimConnectService.cs`.
- **Refinamento do Gatekeeper e Briefing**:
  - `BriefingViewModel.cs` atualizado para computar `UnusableFuelWeightLbs = UnusableFuelGallons * FuelWeightPerGallon`.
  - A dedução no Gatekeeper e no despacho em voo agora desconta o combustível total (utilizável + inutilizável), resultando em precisão exata de 170 lbs (`✓ 100% EXATO`).
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** e compilação limpa com 0 erros (`dotnet build src\FlightCareer.slnx`).

## 2026-11-12 - Sincronização de Combustível Físico Total nos Tanques (MSFS UI vs SimConnect)
- **Diagnóstico da Causa Raiz**:
  - No menu "Massa e equilíbrio -> Combustível" do MSFS, o slider e os tanques exibem o volume **físico total** nos tanques (ex: 11,0 galões).
  - A variável padrão `FUEL TOTAL QUANTITY` da SDK do SimConnect reporta apenas o volume **utilizável** (no C152, deduz ~1,8 galão de combustível não drenável, reportando apenas 9,2 galões).
  - Isso gerava falsa divergência de -1,9 galão no Pre-Flight Gatekeeper contra o planejado do Hangar.
- **Implementação e Harmonização**:
  - `BriefingViewModel.cs` atualizado para computar `FuelQuantity = FuelTotalGallons + UnusableFuelGallons` e `FuelWeightLbs = FuelWeightLbs + UnusableFuelWeightLbs`.
  - No Gatekeeper, o valor lido do simulador agora reflete exatamente os 11,0 galões da UI do MSFS ($9,2 + 1,8 = 11,0\text{ gal}$), zerando a diferença para `✓ 100% EXATO`.
  - `FlightMonitor.cs` atualizado para persistir o combustível total físico no Hangar (`FuelTotalGallons + UnusableFuelGallons`) em `UpdateHangarStateAsync`, `CancelMonitoringAsync` e `FinalizeFlightAsync`.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Limpeza Visual da Tela de Briefing (Remoção de Copiar W&B e Badge de Status)
- **Ajustes de UI no Briefing (`BriefingView.xaml`)**:
  - Removido o botão "📋 COPIAR W&B P/ O SIMULADOR" da coluna direita de despacho, simplificando o painel de liberação focado no botão mestre de decolagem.
  - Removido o badge de texto duplicado "SIMULADOR CONECTADO" do canto superior direito do cabeçalho tático, preservando o botão de gerar novo número de voo (`[ ↻ ]`).
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Mercado Vivo Persistente com Simulação Temporal e Mecânica de Aircraft Flipping
- **Arquitetura de Dados Persistente (`MarketListing`)**:
  - Criada a entidade `MarketListing` no domínio com controle de ciclo de vida (`ListedAtUtc`, `LastPriceDropAtUtc`, `PriceDropsCount`, `IsSold`, `SoldAtUtc`, `IsPlayerOwned`).
  - Mapeamento EF Core e DDL dinâmico SQLite em `AppDbContext.EnsureSchemaUpdatedAsync()` com índices dedicados em `IsSold` e `IsPlayerOwned`.
- **Motor de Simulação Temporal & Economia (`MarketSimulationService`)**:
  - Geração inicial persistente de 90 a 110 aeronaves procedurais ao abrir o mercado pela primeira vez.
  - Simulação de ticks temporais em delta-time (`DateTime.UtcNow - LastTickUtc`):
    - Curva de compra horária por NPCs baseada no `DealRatio` ($\ge 1.15 \to 8\%$/h; $\ge 0.95 \to 2.5\%$/h; $< 0.95 \to 0.3\%$/h), modulada pela liquidez da categoria (Monomotores $1.3\times$, Bimotores $1.0\times$, Turboélices $0.8\times$, Jatos $0.6\times$).
    - Vendedores impacientes (NPCs) aplicam descontos de $3\%$ a $7\%$ a cada 24h após 72h no mercado (até 3 reduções); aeronaves encalhadas por mais de 15 dias com 3 drops são descartadas.
    - Reposição automática de estoque mantendo o pool ativo entre 95 e 110 aeronaves.
    - Manutenção de histórico recente de aeronaves vendidas (itens fantasmas) para sensação de mundo vivo.
- **Fórmula do Valor Justo de Mercado (`BaseMarketValue`)**:
  - Ponderação técnica: $0.45 \cdot \text{Motor} + 0.30 \cdot \text{Aviônicos} + 0.25 \cdot \text{Estrutura/Pneus}$, com fator de idade da célula $F_{\text{ano}}$.
  - Bônus de Overhaul Zero Hora (+12% se motor $\ge 95\%$) e penalidade AOG/Sucata (-25% se qualquer componente crítico $< 25\%$).
- **Mecânica de Aircraft Flipping**:
  - O jogador pode anunciar aeronaves da sua frota no mercado aberto para NPCs comprarem (`IsPlayerOwned = true`), concorrendo de igual para igual.
  - Ao ser arrematada por um NPC, $92\%$ do valor anunciado é creditado no saldo bancário com registro contábil de transação financeira e taxa de $8\%$ da corretora.
  - Suporte concomitante à liquidação imediata da frota.
- **Interface e Experiência de Usuário (`MarketView.xaml`)**:
  - Cards com badges cromáticos de oportunidade (`BARBADA`, `OPORTUNIDADE`, `PREÇO JUSTO`, `ÁGIO`).
  - Chip de desconto para aeronaves com preço reduzido (`- X.X%`).
  - Cards fantasmas de aeronaves recentemente vendidas com tag `🔒 VENDIDO` e botão de compra desabilitado.
  - Painel de inspeção técnica com avaliação Bluebook e cálculo de custo pós-compra.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Otimização Temporal do Mercado Vivo (Cap de 72h e Preservação de Frações de Tempo)
- **Implementação do Cap Anti-Stall e Anti-Drain**:
  - `MarketSimulationService.cs` atualizado para limitar o processamento acumulado de ticks horários a no máximo 72 horas (`Math.Min(elapsed.TotalHours, 72)`).
  - Caso o jogador fique ausente por semanas, `_lastTickUtc` é realinhado para a janela de 72h recentes (`nowUtc.AddHours(-simulationHours)`), evitando travamento da interface e esvaziamento abrupto do mercado.
  - Substituída a atribuição fixa por `_lastTickUtc = _lastTickUtc.AddHours(simulationHours)`, preservando os minutos quebrados residuais para o próximo ciclo sem drift temporal.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Correção Crítica na Detecção de Apagamento de Motor Pós-Pouso
- **Diagnóstico da Causa Raiz**:
  - `FlightMonitor.cs` mantinha a flag `_isAirborne == true` após o pouso durante o táxi e na rampa.
  - A checagem de corte de motor `if (_isAirborne && !data.IsEngineRunning)` não verificava `!data.IsOnGround`.
  - Ao desligar os motores intencionalmente no pátio para encerrar o voo, o sistema disparava falso alarme de pane crítica em voo por RPM/temperatura e truncava indevidamente o motor da aeronave para 15,0%.
- **Implementação e Correção**:
  - Cláusula de detecção ajustada para exigir estritamente que a aeronave esteja no ar: `if (_isAirborne && !data.IsOnGround && !data.IsEngineRunning)`.
  - Transição de status ajustada para "TÁXI / PÁTIO" após desaceleração de pouso.
  - O corte no solo agora aciona exclusivamente a finalização normal de voo (`FINALIZANDO (5s)`), preservando a integridade real da saúde do motor acumulada durante a rota.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Alinhamento do Motor de Desgaste de Motor e Break Checks ao MSFS 2024
- **Calibração de Desgaste Base (Green Arc)**:
  - Desgaste nominal atrelado diretamente ao TBO padrão do motor: 1.800 horas para motor a pistão (`100.0 / 1800.0` ≈ 0.055%/hora) e 3.500 horas para turboélice/jato (`100.0 / 3500.0`).
- **Gatilhos Nativos de Sobrecarga (Overstress Factors)**:
  - **Over-RPM**: Sobretaxa quadrática proporcional ao excesso acima de `MaxRPM`.
  - **CHT Overheat**: Desgaste neutro até 230°C; progressão moderada de 230°C a 260°C; desgaste exponencial acima de 260°C.
  - **Pressão de Óleo Baixa**: Desgaste severo por atrito seco se a pressão cair abaixo de 20 PSI com motor ligado.
  - **Temperatura de Óleo Alta**: Desgaste acelerado se a temperatura do óleo exceder 110°C.
- **Novo Sistema de Break Check (Ciclo de 60 Segundos)**:
  - **Risco Zero em Saúde Normal**: Se `EngineCondition > 20.0%`, a probabilidade de falha mecânica espontânea em voo é estritamente **0.0%** (eliminação completa de falhas aleatórias injustas).
  - **Zona Crítica (<= 20%)**: Avaliação a cada 60 segundos de funcionamento contínuo do motor:
    - Em 20%: 1% de chance a cada 1 minuto.
    - Em 10%: 50% de chance a cada 1 minuto.
    - Em <= 0%: 100% de quebra imediata no minuto.
    - Interpolação linear contínua nas faixas intermediárias.
- **Padronização SimConnect**:
  - Evento de falha de motor unificado exclusivamente no evento oficial do SDK `TOGGLE_ENGINE1_FAILURE`, eliminando duplo disparo de toggle.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Sistema de Desgaste e Falha de Magnetos/Velas (500h), Táxi Incorreto e Break Checks
- **Modelagem de Dados e Persistência (`PilotAircraft` e SQLite)**:
  - Adicionados campos `HoursUntilNext500hInspection` (padrão 500h) e `MagnetosCondition` (0-100%).
  - Migrações dinâmicas DDL em `AppDbContext.EnsureSchemaUpdatedAsync()` com garantia de retrocompatibilidade.
- **Dinâmica de Desgaste Realista (`WearAndFailureService`)**:
  - Taxa nominal de consumo de 0,20%/hora de voo (`100.0 / 500.0`) para motores a pistão.
  - Sobretaxa punitiva de +1,5%/h para aeronaves operando com inspeção de magnetos vencida (`HoursUntilNext500hInspection <= 0`).
  - Penalidade severa por táxi inadequado (*Spark Plug Fouling*): solo, marcha lenta (< 1.200 RPM) e mistura rica (> 80%) aplicam +7,5%/h de desgaste na ignição (~0,25% a cada 2 minutos de táxi incorreto).
- **Mecânica de Break Check de Ignição**:
  - Ciclo de 60 segundos com motor ligado.
  - Risco zero absoluto (0,0%) enquanto `MagnetosCondition > 20%`.
  - Zona crítica (<= 20%): 1% de chance em 20%, 50% em 10% e 100% de quebra imediata em <= 0%.
  - Injeção bilateral via SimConnect: sorteia magneto avariado (50% Left / 50% Right) e seleciona contingência (`MAGNETO1_RIGHT` ou `MAGNETO1_LEFT`), reduzindo RPM em ~120 sem apagar o motor.
- **Módulo do Hangar e Interface (`HangarViewModel` e `HangarView.xaml`)**:
  - Exibição de indicador de saúde dos Magnetos & Ignição com barra colorimétrica dinâmica e horas restantes.
  - Novo comando e botão `Fazer 500h ($1.450)` que debita saldo, registra transação contábil e restaura a saúde e o ciclo para 100% / 500h.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Inspeção de 50 Horas (Troca de Óleo & Lubrificação) e Break Check de Fusão de Bronzina
- **Modelagem de Lubrificação e Persistência (`PilotAircraft`, `AppDbContext`)**:
  - Adicionada a propriedade `OilCondition` (0-100%, padrão 100,0%) representando a saúde e viscosidade do óleo lubrificante.
  - Migração DDL dinâmica em `AppDbContext.EnsureSchemaUpdatedAsync()` com execução resiliente e compatibilidade retroativa.
- **Dinâmica de Degradação do Óleo (`WearAndFailureService`)**:
  - Consumo nominal de 2,0%/hora de voo (`100.0 / 50.0`) em motores a pistão.
  - Sobretaxa punitiva de +5,0%/h por operar com a inspeção de 50h vencida (`HoursUntilNext50hInspection <= 0`).
  - Oxidação térmica e perda de viscosidade quando a temperatura do óleo excede 105°C: `+ (OilTemp - 105) * 0.5%/h`.
  - Contaminação de óleo por passagem de gases (blow-by) quando a temperatura das cabeças de cilindro excede 230°C: `+ (CHT - 230) * 0.2%/h`.
- **Break Check Crítico de Lubrificação (60 Segundos)**:
  - Risco zero absoluto se `OilCondition > 20,0%`.
  - Faixa crítica (`OilCondition <= 20,0%`): 1% de chance a 20%, 50% a 10% e 100% de quebra imediata a <= 0%.
  - Gatilho catastrófico: Pane crítica de motor por fusão de bronzinas de biela/virabrequim (`FailureType.EngineIFSD`, `TOGGLE_ENGINE1_FAILURE`).
- **Sincronização Contínua e Encerramento de Voo (`FlightMonitor`, `FlightService`)**:
  - `UpdateHangarStateAsync`: persistência em segundo plano a cada 60s dos novos estados de `MagnetosCondition`, `OilCondition`, `HoursUntilNext50hInspection`, `HoursUntilNext100hInspection`, `HoursUntilNext500hInspection` e `EngineTboRemainingHours`.
  - `ProcessFlightFinishAsync`: consumo automático de horas da inspeção de 500h (`HoursUntilNext500hInspection - hours`) e salvamento das condições finais no banco.
  - Zeramento integral de motor, magnetos e óleo em situações de Crash violento.
- **Interface e Hangar (`PilotAircraftWrapper`, `HangarViewModel`, `HangarView.xaml`)**:
  - Card de Inspeção de 50h atualizado para exibir horas restantes e saúde do óleo colorimétrica dinâmica.
  - Execução das ordens de serviço de 50h ($850), 100h ($2.500) e Overhaul restaura `OilCondition = 100,0%`.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Limpeza de Lógica de Carb Icing e Delegação ao Motor Nativo do MSFS 2024
- **Remoção do Controle Sintético de Carb Icing (`WearAndFailureService`)**:
  - Removido o membro `CarbIcing` do enum `FailureType`.
  - Removido o acumulador `_carbIceAccumulator` e seu respectivo reset no ciclo de voo.
  - Removida a rotina heurística de congelamento de carburador em `CheckEngineFailures`, preservando a lógica de *Spark Plug Fouling*.
  - Removido o caso `FailureType.CarbIcing` de `TriggerManualFailure`.
- **Delegação Integral ao Simulador**:
  - A física de formação de gelo no carburador, perda gradual de RPM e resposta ao acionamento do aquecimento (`Carb Heat`) passa a ser gerenciada com fidelidade total pelo motor de física e aerodinâmica nativo do Microsoft Flight Simulator 2024.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Limpeza da Lógica de Compressor Stall e Delegação ao Motor Termodinâmico Nativo do MSFS 2024
- **Remoção do Controle Sintético de Compressor Stall (`WearAndFailureService` e `FlightMonitor`)**:
  - Removido o membro `CompressorStall` do enum `FailureType`.
  - Removidos os campos `_lastThrottle` e `_lastThrottleCheck` e seus respectivos resets de ciclo de voo em `Reset()`.
  - Removido o rastro de avanço de manete (*slam throttle detection*) em `ProcessWear`.
  - Removida a seção B de lógica heurística de estol de compressor / surge sob vento cruzado em `CheckEngineFailures`.
  - Removido o caso `FailureType.CompressorStall` de `TriggerManualFailure`.
  - Ajustado o gatilho em `FlightMonitor.OnAircraftFailureTriggered` para tratar exclusivamente falhas críticas de motor (`EngineIFSD`).
- **Delegação Integral ao Simulador**:
  - O comportamento termodinâmico e aerodinâmico de estol de compressor e surge em turbinas e turboélices (incluindo avanço brusco de manete, distorção de fluxo na admissão por vento cruzado e altitude) passa a ser gerenciado de forma nativa e precisa pelo motor termodinâmico do Microsoft Flight Simulator 2024.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Mapeamento de Sobretorque (TurboTorque) e Sobretaxa Quadrática de Desgaste
- **Mapeamento de Telemetria Nativa (`SimTelemetry` e `SimConnectService`)**:
  - Adicionado o campo `public double TurboTorque; // TURB ENG MAX TORQUE PERCENT:1 (em %)` em `SimTelemetry.cs` no grupo de Motor e Propulsão (expandindo para 8 doubles).
  - Registrada a SimVar `TURB ENG MAX TORQUE PERCENT:1` com unidade `"Percent"` em `SimConnectService.cs`.
  - Mantida integridade e alinhamento sequencial exato de memória entre `SimTelemetry` e as definições do SimConnect SDK.
- **Dinâmica de Desgaste por Sobretorque (`WearAndFailureService`)**:
  - Implementada sobretaxa de desgaste quadrático para turbinas e turboélices (`EngineType == 1.0 || EngineType == 5.0`) quando `TurboTorque > 100.0%`.
  - Fórmula: `engineWearRate += 25.0 * Math.Pow((TurboTorque - 100.0) / 5.0, 2);` modelando o estresse mecânico severo na caixa de redução (gearbox), eixo de transmissão e palhetas de turbina por torque excedente.
- **Integração com Break Check de 60 Segundos**:
  - A aceleração do desgaste degrada progressivamente `EngineCondition`.
  - Ao cruzar a zona crítica (`EngineCondition <= 20%`), o ciclo periódico de 60 segundos do Break Check passa a sortear o apagamento catastrófico `FailureType.EngineIFSD` com disparo do evento `TOGGLE_ENGINE1_FAILURE` no MSFS.
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Implementação de Detecção Inteligente e Realista de Carb Icing (MSFS 2024)
- **Mapeamento de Telemetria Nativa (`SimTelemetry` e `SimConnectService`)**:
  - Adicionados os campos `public double CarbHeatAvailable;` (`CARB HEAT AVAILABLE`, unidade `Bool`) e `public double CarbTemperature;` (`RECIP CARBURETOR TEMPERATURE:1`, unidade `Celsius`) ao struct `SimTelemetry` no grupo de Gelo e Pressurização (expandindo para 5 doubles).
  - Adicionada a propriedade calculada auxiliar `public bool HasCarbHeat => CarbHeatAvailable > 0.5;`.
  - Registradas as respectivas SimVars no método `RegisterDataDefinitions` de `SimConnectService`, preservando estrita integridade posicional e alinhamento sequencial de memória com o SimConnect SDK.
- **Detecção Inteligente e Fisiologia do Carburador (`WearAndFailureService`)**:
  - Adicionado o tipo de falha `FailureType.CarbIcing` com acumulador dedicado `_carbIceTimerSeconds` e reset em início/reinício de ciclo de voo.
  - **Imunidade Garantida**: Verificação exclusiva para aeronaves a pistão (`!isJetOrTurbo`) que possuam carburador e comando de aquecimento habilitado pelo modelo de voo do MSFS 2024 (`CarbHeatAvailable > 0.5`). Aeronaves a injeção de combustível, turboélices e jatos possuem imunidade nativa garantida (`CarbHeatAvailable == 0`).
  - **Critérios de Gelo Ativo**: Detecção baseada na telemetria física real do MSFS: temperatura do venturi/carburador <= 0,0°C, baixa potência de motor (`ThrottlePercent < 45.0%`) típica de descidas ou aproximações, em condições de ar úmido (precipitação ativa ou temperatura ambiente entre -5°C e 25°C).
  - **Janela de Tolerância de 90 Segundos**: Caso o piloto negligencie o acionamento do Carb Heat durante a condição de gelo ativo, o temporizador acumula o tempo. Se persistir por 90 segundos contínuos sem aquecimento (`!IsCarbHeatOn`), o motor sofre apagamento crítico por asfixia (`FailureType.CarbIcing` disparando `TOGGLE_ENGINE1_FAILURE`).
  - **Recuperação e Degelo Acelerado**: Ao acionar o Carb Heat (`IsCarbHeatOn`) ou restaurar potência com aquecimento do venturi (`CarbTemperature > 2.0°C`), o temporizador de gelo regride a uma taxa acelerada de 2x até zerar completamente, permitindo recuperação total da operação antes do corte.
  - Suporte a disparo forçado via `TriggerManualFailure(FailureType.CarbIcing)`.
- **Auditoria Crítica (`@reviewer`)**: Validação de integridade de tipos, ausência de regressões e compilação limpa em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Implementação de Gatilhos de Incêndio na Partida no Solo e Curva Unificada de Break Check
- **Mapeamento de Telemetria Nativa (`SimTelemetry` e `SimConnectService`)**:
  - Adicionados 3 novos campos ao struct `SimTelemetry` no grupo `// Dinâmica Lateral e Comandos do Piloto (7 doubles)`:
    - `public double FuelPumpSwitch;` (`GENERAL ENG FUEL PUMP SWITCH:1`, unidade `Bool`)
    - `public double StarterActive;` (`GENERAL ENG STARTER:1`, unidade `Bool`)
    - `public double ConditionLever;` (`TURB ENG CONDITION LEVER POSITION:1`, unidade `Percent`)
  - Adicionada a propriedade calculada auxiliar `TailwindComponentKnots` calculando a componente de vento de cauda relativa ao proa da aeronave: `-AmbientWindVelocity * Math.Cos((AmbientWindDirection - PlaneHeading) * Math.PI / 180.0)`.
  - Registradas as 3 novas SimVars em `SimConnectService.RegisterDataDefinitions` na sequência exata, preservando o alinhamento de memória e empacotamento sequencial (`Pack = 1`) com a API nativa do SimConnect SDK.
- **Curva Unificada de Break Check (`WearAndFailureService`)**:
  - Implementado o método utilitário privado `CalculateBreakCheckChance(double condition)`:
    - Risco **ZERO absoluto** se `condition > 20.0%`.
    - Quebra imediata (100% de probabilidade) se `condition <= 0.0%`.
    - Curva linear proporcional de 50% a 100% na faixa crítica `[0%, 10%]`: `0.50 + ((10.0 - condition) / 10.0) * 0.50`.
    - Curva linear proporcional de 1% a 50% na faixa preventiva `(10%, 20%]`: `0.01 + ((20.0 - condition) / 10.0) * 0.49`.
  - Refatorado o ciclo periódico de 60 segundos do Break Check (`CheckEngineFailures`) para utilizar `CalculateBreakCheckChance` uniformemente para saúde mecânica geral (`EngineCondition`), lubrificação (`OilCondition`) e ignição (`MagnetosCondition`).
- **Gatilhos de Incêndio na Partida no Solo (`CheckStartupFailures`)**:
  - Adicionado o novo tipo de falha `FailureType.EngineFire` com suporte a teste manual via `TriggerManualFailure(FailureType.EngineFire)`.
  - **Gatilho 1: Motor Afogado / Excesso de Prime com Backfire (Pistão - `!isJetOrTurbo`)**:
    - Monitora o acúmulo de combustível no coletor com bomba ligada (`FuelPumpSwitch > 0.5`) e mistura rica (`MixturePercent > 80%`) com o motor desligado (`!IsEngineRunning`). Temporizador acumula via `_primeTimerSeconds += deltaTimeHours * 3600.0` e dissipa a 0.5x caso contrário.
    - Se o starter for engajado (`StarterActive > 0.5`) com `_primeTimerSeconds >= 6.0s`, avalia o risco contra `CalculateBreakCheckChance(aircraft.EngineCondition)`. Se sorteado, dispara incêndio catastrófico (`FailureType.EngineFire` com `TOGGLE_ENGINE1_FAILURE`) e reseta o temporizador.
  - **Gatilho 2: Partida Quente / Hot Start Severo (Turboélice - `EngineType == 5.0`)**:
    - Durante o engajamento do starter (`StarterActive > 0.5`) ou partida em andamento (`!IsEngineRunning && EngineN1 > 1.0%`).
    - Detecta introdução prematura de combustível em rotação insuficiente de compressores (`ConditionLever > 10% && EngineN1 < 12%`) ou temperatura entre palhetas crítica (`TurboITT > 1000°C`).
    - Avalia a falha contra `CalculateBreakCheckChance(aircraft.EngineCondition)`. Se disparado, gera incêndio interno na seção quente (`FailureType.EngineFire`).
  - **Gatilho 3: Partida com Vento de Cauda Forte (Turboélice - `EngineType == 5.0`)**:
    - Durante a partida (`StarterActive > 0.5`) sob vento de cauda forte na exaustão (`TailwindComponentKnots > 10.0 kts`).
    - Avalia a probabilidade proporcional moderada `CalculateBreakCheckChance(aircraft.EngineCondition) * 0.5`. Se acionado, dispara incêndio interno por refluxo térmico dos gases de escapamento para a turbina.
- **Tratamento de Impacto Crítico no Voo (`FlightMonitor`)**:
  - No evento `OnAircraftFailureTriggered`, falhas do tipo `EngineFire` degradam imediatamente `EngineCondition` para `0.0%` (perda total por destruição térmica), reportando evento crítico e acionando contingência de pouso/solo.
- **Auditoria Crítica (`@reviewer`)**: Validação de integridade de tipos, ausência de regressões e compilação limpa em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Implementação de Gatilhos de Incêndio de Motor em Voo por Abuso Operacional
- **Mapeamento de Acumuladores de Estresse Térmico em Voo (`WearAndFailureService`)**:
  - Adicionados os campos de rastreamento temporal `_pistonFireTimerSeconds` (para motores a pistão) e `_turboFireTimerSeconds` (para turboélices).
  - Incluídos os devidos resets desses contadores no ciclo `Reset()`.
- **Gatilho 1: Superaquecimento Extremo e Óleo Quente em Voo (Motores a Pistão - `!isJetOrTurbo`)**:
  - Condição em voo (`!data.IsOnGround`): avalia CHT na faixa vermelha (`data.EngineCHT > 245.0°C`) conjuntamente com a Temperatura de Óleo na faixa vermelha (`data.EngineOilTemp > 115.0°C`).
  - Acumula tempo continuamente enquanto o estresse persistir (`_pistonFireTimerSeconds += deltaTimeHours * 3600.0`); havendo arrefecimento das temperaturas ou retorno ao solo, dissipa gradualmente à taxa de 0.5x.
  - Janela de tolerância operacional de 45 segundos: se o abuso térmico severo se mantiver acumulado por 45 segundos contínuos (`_pistonFireTimerSeconds >= 45.0s`), dispara a avaliação de Break Check proporcional.
  - Avaliação estrita via `CalculateBreakCheckChance(aircraft.EngineCondition)`:
    - **Risco ZERO absoluto** se `aircraft.EngineCondition > 20%`.
    - Caso sorteado o risco na zona crítica de degradação, aciona `FailureType.EngineFire` com o evento `TOGGLE_ENGINE1_FAILURE`: *"INCÊNDIO NO MOTOR EM VOO (RUPTURA DE LINHA DE ÓLEO)"*, descrevendo o rompimento de mangueiras/retentores sob o escapamento incandescente.
    - Zera o temporizador `_pistonFireTimerSeconds = 0`.
- **Gatilho 2: Excesso de Torque/ITT ou Gelo sem Inertial Separator em Voo (Turboélices - `EngineType == 5.0`)**:
  - Condição em voo (`!data.IsOnGround`):
    - Subida com sobretorque ou ITT excessiva: `data.VerticalSpeed > 500.0 && (data.TurboTorque > 102.0 || data.TurboITT > 820.0)`.
    - Operação em atmosfera congelante com formação de gelo estrutural (`data.IsPrecipitating || data.StructuralIcePct > 10.0`) sem o engajamento do separador inercial (`!data.IsCarbHeatOn`) e mantendo alta demanda de potência (`data.TurboTorque > 95.0 || data.TurboITT > 800.0`).
  - Acumula tempo no temporizador de abuso `_turboFireTimerSeconds`; havendo alívio de potência ou saída de gelo / acionamento do separador, dissipa à taxa de 0.5x.
  - Janela de tolerância operacional de 45 segundos: após 45 segundos contínuos acumulados de operação abusiva (`_turboFireTimerSeconds >= 45.0s`), avalia `CalculateBreakCheckChance(aircraft.EngineCondition)`.
  - **Risco ZERO absoluto** garantido para turbinas com saúde acima de 20%.
  - Se sorteada a falha na faixa degradada, aciona `FailureType.EngineFire` com o evento `TOGGLE_ENGINE1_FAILURE`: *"INCÊNDIO DE TURBINA EM VOO (FALHA ESTRUTURAL DE NACELE)"*, com descrição contextualizada da causa (subida com sobretorque/ITT ou negligência do separador inercial em gelo).
    - Zera o temporizador `_turboFireTimerSeconds = 0`.
- **Auditoria Crítica (`@reviewer`)**: Validação de integridade de tipos, blindagem de risco para aeronaves com manutenção regular e compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.

## 2026-11-12 - Calibração Realista de Incêndio (FAA/NTSB) e Novos Cenários (Linha de Combustível, Escapamento e Elétrico)
- **Calibração Estocástica Baseada em Estatísticas da FAA / NTSB (`WearAndFailureService`)**:
  - Na aviação geral e comercial real, dados da FAA/NTSB comprovam que apenas cerca de 2% a 5% dos colapsos mecânicos graves de motor evoluem para chamas ativas (a imensa maioria resulta em apagamento em voo - IFSD ou travamento mecânico sem fogo).
  - Implementado o método estocástico unificado `CalculateFireChance(double condition, double riskMultiplier = 1.0)`:
    - **Regra de Ouro (Blindagem Absoluta)**: Risco estritamente **ZERO (0,0%)** enquanto a condição do componente estiver acima de 20,0%.
    - Em 20%: ~0,05% de probabilidade.
    - Em 10%: ~2,5% de probabilidade.
    - Em 0%: ~5,0% de probabilidade (com teto estrito de 10% para abusos térmicos e elétricos extremos).
  - Todos os gatilhos pré-existentes de incêndio foram unificados sob a curva realista com multiplicadores ajustados entre 1.0 e 1.5:
    - Superaquecimento de pistão em voo (`riskMultiplier = 1.2`).
    - Sobretorque/separador inercial de turbina em voo (`riskMultiplier = 1.2`).
    - Motor afogado com backfire no solo (`riskMultiplier = 1.2`).
    - Hot Start severo em turboélice (`riskMultiplier = 1.5`).
    - Vento de cauda na partida de turboélice (`riskMultiplier = 1.0`).
- **Implementação dos 3 Novos Cenários de Incêndio (`WearAndFailureService`)**:
  - **Cenário A: Ruptura de Linha de Combustível ou Bico Injetor Solto**:
    - Avaliado periodicamente a cada 60 segundos no ciclo do Break Check com motor em funcionamento.
    - Gatilho restrito a `EngineCondition <= 20,0%` (0% de chance para manutenção saudável).
    - Probabilidade: `CalculateFireChance(aircraft.EngineCondition, 1.0)`.
    - Disparo: `FailureType.EngineFire` (*"INCÊNDIO: RUPTURA DE LINHA DE COMBUSTÍVEL / BICO SOLTO"*), detalhando que a vibração crônica por desgaste soltou conexão de alta pressão com ignição de combustível pulverizado no cofre.
  - **Cenário B: Vazamento no Sistema de Escapamento (Exhaust Manifold Crack)**:
    - Avaliado a cada 60 segundos no ciclo do Break Check exclusivamente para motores a pistão (`!isJetOrTurbo`).
    - Gatilho restrito a `EngineCondition <= 20,0%`.
    - Probabilidade: `CalculateFireChance(aircraft.EngineCondition, 0.8)`.
    - Disparo: `FailureType.EngineFire` (*"INCÊNDIO: RACHADURA NO COLETOR DE ESCAPAMENTO"*), simulando fissura estrutural no coletor que sopra gases de exaustão a 800°C sobre linhas de combustível e isolamento acústico.
  - **Cenário C: Curto-Circuito de Alta Amperagem no Alternador / Cabo do Starter**:
    - Condição base vinculada à saúde elétrica dos aviônicos: **`aircraft.AvionicsCondition <= 20%`** (Risco ZERO se `AvionicsCondition > 20%`).
    - **Sub-gatilho 1 (Travamento de Relé / Solenoide do Starter Colado)**:
      - Em `ProcessWear`: Detecta starter engajado (`StarterActive > 0.5`) com motor em rotação ativa (`RPM > 800` ou `N1 > 40%`).
      - Acumula em `_starterStuckTimerSeconds` e dissipa a 0.5x quando desengajado.
      - Ao acumular 8 segundos de arrasto contínuo (`_starterStuckTimerSeconds >= 8.0s`), avalia `CalculateFireChance(aircraft.AvionicsCondition, 2.0)`. Se sorteado, dispara `FailureType.EngineFire` (*"INCÊNDIO ELÉTRICO: TRAVAMENTO DE RELÉ DO STARTER"*); caso contrário reinicia a janela para próxima verificação estocástica.
    - **Sub-gatilho 2 (Curto-Circuito no Chicote do Alternador)**:
      - Avaliado a cada 60 segundos no Break Check quando `AvionicsCondition <= 20%`.
      - Probabilidade: `CalculateFireChance(aircraft.AvionicsCondition, 0.8)`.
      - Disparo: `FailureType.EngineFire` (*"INCÊNDIO ELÉTRICO: CURTO NO CHICOTE DO ALTERNADOR"*), simulando fiação principal incandescente por fadiga elétrica incendiando o compartimento do motor.
- **Auditoria Crítica (`@reviewer`)**: Validação de build com compilação limpa (0 Erros) em `dotnet build C:\projetos\FlightCareerV6\src\FlightCareer.slnx`.

## 2026-11-12 - Implementação de Bird Strike Severo em Baixa Altitude (Física Pura / Fator Ambiental)
- **Modelagem Ambiental Externa (`CheckBirdStrikeFailure` em `WearAndFailureService`)**:
  - Implementada a Opção 1 (Física Pura): evento ambiental externo de ingestão de ave de grande porte (> 2 kg, ex: urubu/ganso), independente da saúde mecânica prévia do motor (`EngineCondition`).
  - Envelope operacional: exclusivo para aeronaves turboélice (`EngineType == 5.0`), em voo (`!IsOnGround && IsEngineRunning`), em baixa altitude (`Altitude <= 3500.0 ft`) e com velocidade ativa (`Airspeed >= 75.0 kts`).
  - Dinâmica de Energia Cinética ($E_k = \frac{1}{2} m v^2$): a taxa de Poisson ($\lambda = 0.0012 \times (\text{Airspeed} / 100.0)^2$) escala quadraticamente com a velocidade aerodinâmica (~1 evento a cada 600 a 800 horas de voo em baixa altitude).
  - Desfecho: rompimento não contido (*uncontained failure*) da carcaça do compressor com perfuração das linhas de combustível Jet-A1 e óleo, disparando `FailureType.EngineFire` com corte via `TOGGLE_ENGINE1_FAILURE` e destruição imediata do grupo motopropulsor (`EngineCondition = 0.0%`).
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.
