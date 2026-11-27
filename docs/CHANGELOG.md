# Histórico de Mudanças do FlightCareer V6

> Registro cronológico do que foi feito por cada agente (Gemini e Claude). **Não é a fonte de verdade do estado atual**: entradas antigas podem ter sido substituídas por mudanças posteriores (marcadas com ⚠️). O estado atual do projeto e as regras de trabalho ficam em [`AGENTS.md`](../AGENTS.md).
>
> Ao concluir uma tarefa, acrescente uma entrada **no fim** deste arquivo no formato `## AAAA-MM-DD - Título (Agente)`.

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

> ⚠️ **Substituído em parte:** o compressor stall foi removido (2026-11-12); o carb icing foi removido e depois reimplementado (2026-11-12); o nome correto do evento é `TOGGLE_ENGINE1_FAILURE`; a despressurização agora só vale para aeronave pressurizada (2026-11-12, correções de falhas).
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

> ⚠️ **Correção:** o DTO `FlightTelemetryDto` citado abaixo não existe no código.
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

> ⚠️ **Será substituído:** a curva de break check por minuto (1%/min a 20%, 50%/min a 10%) é do sistema legado. O sistema novo (`Domain/Components`, fase 1) usa taxa por hora suave; ver AGENTS.md › Sistema de falhas.
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

> ⚠️ **Correção (2026-11-12):** `MAGNETO1_LEFT/RIGHT` apenas move a chave; a pane agora é mantida por enforcement da chave. As horas de 500 h eram descontadas em dobro (em voo e no fim do voo); hoje só o `FlightService` desconta.
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

> ⚠️ **Revertido:** o carb icing foi reimplementado na entrada "Detecção Inteligente e Realista de Carb Icing" logo abaixo.
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

> ⚠️ **Substituído:** incêndios não disparam mais `TOGGLE_ENGINE1_FAILURE` nem zeram o motor na hora; usam o fogo progressivo (`ENG ON FIRE:1`). As chances de partida agora são escaladas pelo tempo do tick.
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

> ⚠️ **Substituído:** o bird strike gera fogo progressivo (`ENG ON FIRE:1`), sem `TOGGLE_ENGINE1_FAILURE` nem motor a 0% imediato.
- **Modelagem Ambiental Externa (`CheckBirdStrikeFailure` em `WearAndFailureService`)**:
  - Implementada a Opção 1 (Física Pura): evento ambiental externo de ingestão de ave de grande porte (> 2 kg, ex: urubu/ganso), independente da saúde mecânica prévia do motor (`EngineCondition`).
  - Envelope operacional: exclusivo para aeronaves turboélice (`EngineType == 5.0`), em voo (`!IsOnGround && IsEngineRunning`), em baixa altitude (`Altitude <= 3500.0 ft`) e com velocidade ativa (`Airspeed >= 75.0 kts`).
  - Dinâmica de Energia Cinética ($E_k = \frac{1}{2} m v^2$): a taxa de Poisson ($\lambda = 0.0012 \times (\text{Airspeed} / 100.0)^2$) escala quadraticamente com a velocidade aerodinâmica (~1 evento a cada 600 a 800 horas de voo em baixa altitude).
  - Desfecho: rompimento não contido (*uncontained failure*) da carcaça do compressor com perfuração das linhas de combustível Jet-A1 e óleo, disparando `FailureType.EngineFire` com corte via `TOGGLE_ENGINE1_FAILURE` e destruição imediata do grupo motopropulsor (`EngineCondition = 0.0%`).
- **Auditoria Crítica (`@reviewer`)**: Parecer **APROVADO** com compilação limpa (0 Erros) em `dotnet build src/FlightCareer.slnx`.


## 2026-11-12 - Ciclo de Incêndio Progressivo (`ENG ON FIRE:1`) e Sistema de Vazamento Progressivo de Óleo (`GENERAL ENG OIL LEAKED PERCENT:1`)
- **Integração de SimVars Escrevíveis (`SetDataOnSimObject` em `SimConnectService` e `SimTelemetry`)**:
  - Mapeadas as variáveis nativas do MSFS 2024 `ENG ON FIRE:1` (`Bool`) e `GENERAL ENG OIL LEAKED PERCENT:1` (`Percent`, 0 a 100%) para leitura contínua em `SimTelemetry` e escrita direta via `SimConnect.SetDataOnSimObject` (`SetEngineFire(bool)` e `SetOilLeakedPercent(double)`).
- **Ciclo de Vida Realista de Fogo no Motor (`ProcessActiveEngineFire` em `WearAndFailureService`)**:
  - **Fogo $\neq$ Motor Apagado Automaticamente**: Quando `FailureType.EngineFire` é disparado (em qualquer um dos 9 cenários de incêndio), o motor não sofre corte imediato: `ENG ON FIRE:1 = 1` é ativado no MSFS 2024 e o motor continua girando enquanto sofre degradação térmica acelerada (`-0.8%/s`).
  - **Combate ao Fogo pelo Piloto (Memory Items)**: Se o piloto cortar o combustível (`MixturePercent < 5%` em pistão ou `ConditionLever < 5%` em turboélice/jato) e desligar a bomba de combustível (`FuelPumpSwitch <= 0.5`), após **25 segundos** de resfriamento pelo vento relativo, o sistema escreve **`ENG ON FIRE:1 = 0`** (fogo extinto), preservando a saúde residual do motor e salvando a aeronave de perda total.
  - **Derretimento Catastrófico por Negligência (90 Segundos)**: Se o piloto ignorar o fogo por **90 segundos** sem cortar o combustível, o fogo destrói o bloco/seção quente por completo, disparando `TOGGLE_ENGINE1_FAILURE` (`FailureType.EngineIFSD`) e aplicando `EngineCondition = 0.0%` no SQLite.
- **Sistema de Vazamento Progressivo de Óleo (`ProcessOilLeakSystem` em `WearAndFailureService`)**:
  - Incrementa `GENERAL ENG OIL LEAKED PERCENT:1` de 0 a 100%, fazendo o MSFS 2024 reduzir progressivamente a pressão do óleo (`GENERAL ENG OIL PRESSURE:1`) e elevar a temperatura (`GENERAL ENG OIL TEMPERATURE:1`), dando ao piloto uma janela de decisão para alternar o pouso antes da fusão de bronzina.
  - **Gatilhos Operacionais e de Telemetria**:
    1. **Acelerar com Óleo Frio (Cold Oil Overpressure)**: Aplicar potência alta (`RPM > 1500`, `Throttle > 65%` ou decolar) com `EngineOilTemp < 38°C` (100°F) incrementa o vazamento gradualmente por tick e **ativa o Break Check imediato se a saúde do óleo estiver $\le$ 20%**, rompendo o radiador/vedação do filtro.
    2. **Superaquecimento Crítico Prolongado**: Manter `CHT > 245°C` ou `OilTemp > 115°C` por mais de 60 segundos deforma juntas de silicone/borracha do cárter e tampas de válvulas, gerando vazamento progressivo.
    3. **Sobrevelocidade (Over-RPM) ou Excesso de Torque**: Operar acima de 102% do RPM máximo ou Torque máximo eleva a pressão da bomba e a vibração mecânica, fissurando mangueiras de alta pressão.
    4. **Pouso Duro (Hard Landing / Prop Strike)**: Impacto severo no toque (`>= 650 FPM` em `CheckHardLandingOilLeakFailure`) pode trincar o cárter ou romper o retentor frontal do virabrequim.
  - **Gatilhos de Manutenção, Erro Humano e FOD**:
    5. **Erro Pós-Manutenção (Primeiros 30 Minutos)**: Nas primeiras 0,5h após inspeção de 50h/100h, pequena chance estocástica de filtro de óleo mal apertado ou anel O-ring duplo iniciar vazamento sob pressão (`+2.0%/min`).
    6. **Tampa de Abastecimento / Vareta Solta (Walkaround)**: Avaliado após a decolagem quando `OilCondition <= 20%`, soprando óleo pelo bocal a `+0.5% a +1.0%/min`.
    7. **Fadiga de Mangueiras Flexíveis (Aeroquip) & Válvula Reguladora de Pressão Travada**: Avaliados no Break Check de 60s quando `EngineCondition <= 20%` ou `OilCondition <= 20%`.
    8. **Detritos na Pista (FOD)**: Durante corrida de pista em alta potência (`GS > 25 kts` e `Throttle > 65%`), chance estocástica de pedras perfurarem o radiador de óleo inferior.

## 2026-11-12 - Correções do Sistema de Falhas no SimConnect (Claude)
- `SimConnectService.SendEvent`: cada evento recebe um ID próprio, mapeado uma única vez por conexão. Antes o mesmo ID era remapeado e provavelmente só a primeira falha da sessão chegava ao simulador.
- `SetFailure` não reenvia toggle se a SimVar já mostra a falha; `ClearSimFailures()` desfaz falhas remanescentes no início do voo (`Reset(clearSimState: true)`).
- Novas SimVars no fim de `SimTelemetry`: `PARTIAL PANEL VACUUM/PITOT/STATIC`, `RECIP ENG LEFT/RIGHT MAGNETO:1`, `GENERAL ENG MASTER ALTERNATOR:1`.
- Removidos eventos inexistentes no SDK (`TOGGLE_ALTERNATOR1_FAILURE`, `TOGGLE_TIRE_FAILURE`) e mapeamentos errados (velas→vácuo, despressurização→alternador, `FLAPS_UP`).
- Magneto, alternador e flaps mantidos por enforcement (`EnforceActiveFailures`).
- Despressurização só em aeronave pressurizada; chances de partida escaladas por `dt`; fogo encerra após destruir o motor; 500 h não contam mais em dobro; desgaste usa tempo simulado (sim rate).

## 2026-11-12 - Sistema de Saúde por Componente, Fase 1 (Claude)
- Branch `feat/component-health-system`, commit `b56cc23`.
- Domínio (`src/FlightCareer.Domain/Components/`): `AircraftComponent` (8 barras por aeronave), `ComponentCatalog` + `Infrastructure/Data/component_catalog.json`, `WearEngine`, `FailureEngine`, `HealthVisibility`, `AircraftHealthSummary`, `LegacyComponentMigrator`.
- Persistência: tabela `AircraftComponents` (EF + `EnsureSchemaUpdatedAsync`) e `AircraftComponentProvisioner` na inicialização (backfill a partir das colunas antigas).
- Testes: projeto `src/FlightCareer.Tests` (xUnit), 46 testes incluindo Monte Carlo e migração SQLite.
- **Ainda não integrado ao voo**: o `FlightMonitor` continua usando o `WearAndFailureService` legado (integração = fase 2).

## 2026-11-12 - Reorganização das Instruções dos Agentes (Claude)
- `AGENTS.md` (novo): guia compartilhado com arquitetura atual (5 projetos), comandos, regras de trabalho em dupla Gemini + Claude, convenções, regras de SimConnect/banco e estado atual de cada sistema.
- `gemini.md` reduzido às instruções do Gemini (importa `AGENTS.md`); `CLAUDE.md` (novo) com as instruções do Claude.
- Histórico movido para `docs/CHANGELOG.md`: removidas duas seções duplicadas e marcadas com ⚠️ as entradas que o código já superou.
- Plano de W&B/detecção de aeronave movido para `docs/plans/`, com o checklist reaberto (não estava implementado no código).
- `.gemini/gemini.md` virou ponteiro para o `gemini.md` da raiz (removida a regra contraditória "zero código direto"); `.antigravity` sem caminho fixo do `dotnet`; `@reviewer`/`@qa` exigem `dotnet build` **e** `dotnet test`.

## 2026-11-12 - Sistema de Saúde por Componente, Fase 2: Integração ao Voo (Claude)
- `FlightMonitor` passa a usar `ComponentFlightSession` + `SimFailureActuator`; o `WearAndFailureService` foi removido.
- Novo componente **Instrumentos** (vácuo, pitot, estática), totalizando 9 por aeronave.
- Cada modo de falha do catálogo ganhou `effect`, `effectValue`, `damage` e `damageKind`. Efeitos reais no MSFS: motor, fogo progressivo, vazamento de óleo, magnetos, alternador, pane elétrica (`TOGGLE_ELECTRICAL_FAILURE`), vácuo, pitot, estática, flaps travados, freio de um lado (estouro de pneu / perda de freio) e hidráulico (trem, só turbinas).
- Eventos externos sempre ativos (`ExternalEventEngine`): bird strike, FOD, falha pós-manutenção.
- Gelo no carburador reimplementado como recuperável (`CarbIcingMonitor`).
- Limites por modelo no catálogo (`aircraftOverrides`): C152 (redline 2550, utilidade +4,4/−1,76 G), aviões de transporte (+2,5/−1,0 G), DA40NG/DA62 como pistão.
- `SimConnectService`: `PARTIAL PANEL ELECTRICAL` no fim do `SimTelemetry`; toggles sem SimVar de estado (freios, hidráulico) rastreados por sessão e esquecidos no `FlightLoaded`; `ClearFailure` para recuperar o motor.
- `LegacyConditionBridge`: as colunas antigas recebem a menor saúde de cada grupo durante o voo; reparos/inspeções/perda total feitos fora do voo são aplicados aos componentes no início do próximo voo.
- Componentes salvos junto com o estado do hangar (a cada 60 s, cancelamento, perda total e fim de voo); aeronaves novas (compra/criação de piloto) recebem componentes no início do primeiro voo.
- Removidos (substituídos por faixas de estresse e panes por componente): incêndios de partida por backfire, vento de cauda e starter travado; gatilhos diretos de vazamento de óleo.
- Testes: 63 (catálogo, overrides, eventos externos, carb icing, ponte com colunas antigas). **Não testado no MSFS.**

## 2026-11-12 - Sistema de Saúde por Componente, Fase 3: Hangar (Claude)
- `HangarView.xaml` redesenhado no tema Avionics Dark: frota com local, mini-barra de aeronavegabilidade e status; cabeçalho com local, horas de célula, pousos e classe do motor; painel com as barras de todos os componentes (faixa oculta, marcas de 20%/30%, vida útil, inspeção) e reparo por componente com orçamento visível.
- Ações separadas: reparar componente / reparar tudo, walkaround (grátis), inspeções 50 h, 100 h, 500 h (só pistão), retífica, abastecimento, seguro (com vencimento), conversão de cabine.
- Domínio: `ComponentMaintenance` (orçamento, reparo, inspeção, walkaround, vida restante) e `AircraftStatusEvaluator` (Pronta / Manutenção próxima / Atenção / Risco de pane / Interditada / Perda total, pela faixa exibida). Catálogo com `serviceName`, `repairCostFactor` e `minFullRepairCost`.
- `HangarViewModel` com contexto novo por operação (antes um contexto longo deixava dados velhos após o voo); `MainViewModel.RefreshPilotBalanceAsync` relê o saldo.
- Corrigidos: chip "MOTOR" sempre vazio, aviônicos sem reparo, um botão cobrando reparo + combustível juntos, tooltip de TBO fixo em 1.800 h (retífica agora restaura o TBO da classe do motor).
- `ThemedDialog` substitui as caixas do Windows no Hangar.
- 70 testes. **Tela não verificada visualmente (app não executado).**

## 2026-11-12 - Auditoria do Fluxo Completo: Persistência entre Telas (Claude)
- **Bug crítico corrigido:** vários serviços/telas singleton (`FlightService`, `PilotRepository`, `MainViewModel`, `MissionGenerator`, mercado, translado) mantinham um contexto do EF pela sessão inteira e liam entidades do cache. Consequências: a partir do 2º voo, o `FlightService` sobrescrevia inspeções/reparos/abastecimentos feitos no Hangar e desfazia gastos (saldo antigo); o cabeçalho mostrava saldo/horas antigos após o voo e podia devolver o piloto ao aeroporto de origem; compras no mercado podiam apagar receita de voos. Correção: `ChangeTracker.Clear()` no início das operações desses contextos (e `ChangeTracker` exposto em `IAppDbContext`). Teste de regressão `FlightPersistenceTests` (falha sem a correção).
- **Aceite/descarregamento de missões** gravava no contexto errado (as missões vêm do contexto do `MissionGenerator`): o manifesto podia sumir ao recarregar e cargas em escala voltavam ao mercado. Agora o estado é copiado para a entidade do contexto do `MainViewModel` (`StageMissionStateAsync`).
- **Homologação de categoria** não gravava saldo nem categoria (piloto vinha do repositório, `SaveChanges` em outro contexto). Agora usa o contexto do `MainViewModel`.
- `FlightService` passa a carregar o tipo da aeronave: preço do combustível (Avgas × Jet A-1) e taxa de pouso por MTOW estavam usando valores padrão.
- Excedências de limites não abrem mais caixa modal durante o voo (só blackbox).

## 2026-11-13 - Integração: Panes Persistentes sobre o Sistema de Componentes (Claude)
- Merge do `main` (panes persistentes, commit ffd2649) na branch de componentes. O sistema de componentes continua sendo o motor do voo; as panes persistentes viram a camada de **discrepâncias entre voos** (primeira parte da fase 4).
- `DefectCatalog.CodeFor(effect, kind, critical)` (Domain, com testes) converte a pane do componente em discrepância; novos códigos `ELECTRICAL`, `BRAKE_FAILURE`, `HYDRAULIC`, `STRUCTURAL`. O `FlightMonitor` grava a discrepância antes do alerta.
- `FlightFailureRecord.Persistent`: gelo no carburador é transitório e avisa o `PersistentFailureService` para não confundir o apagamento com pane nativa.
- `SimConnectService`: leitura rápida `SimFailureState`, escrita de magnetos/flaps, grupo de comandos mascarados, `AircraftLoaded` (evento de arquivo, junto com `FlightLoaded`) e `SetPersistentToggles` (`ClearSimFailures`/`ClearFailure` preservam os toggles das panes abertas). `SendEvent` passa a guardar o nome de cada ID para identificar comandos bloqueados.
- `SimFailureActuator`: alternador forçado com `ALTERNATOR_OFF` (idempotente, testado no MSFS 2024) em vez de `TOGGLE_ALTERNATOR1`, que podia religar a chave junto com a correção do vigia.
- `FlightMonitor`: `StartMonitoring` retorna `bool` e bloqueia NO-GO; modo vigia fora do voo (`WatchAircraft`); detecção nativa de ENG FAILED saiu do tick e foi para o `PersistentFailureService` (motor limitado a 15%, fogo a 5% e −20% célula, dano proporcional, trem −20%, freios a 30%).
- Hangar: card "Panes em aberto" (NO-GO/DIFERIDA, origem, fase, causa, reparo individual ou todas). Status "GROUNDED (PANE)" / "PANE DIFERIDA" sobre o `AircraftStatusEvaluator`.
- `WearAndFailureService` removido também do lado do `main`.
- Testes: 96 (mapeamento de panes e migração da tabela `AircraftDefects` em banco antigo). **Não testado no MSFS nem na tela do app após a integração.**

## 2026-11-13 - Plano de peças, sensores e revisões para execução pelo Gemini (Claude)
- `docs/plans/sensor-inventory.md`: inventário de SimVars do SDK MSFS 2024 por sistema, com dicas de peça (todas candidatas até a validação em voo).
- `docs/plans/pecas-desgaste-falhas.md`: plano em 5 fases (sensores → peças e desgaste → walkaround/revisões/troca → panes → UI), matriz de aviões A–I e regras do usuário (revisão só inspeciona; revisão vencida só libera trocando todo o pacote).
- `docs/plans/INSTRUCOES-GEMINI.md` e `docs/reviews/`: protocolo de execução e revisão entre Gemini e Claude.

## 2026-11-14 - Orquestração Claude + Gemini com travas no git (Claude)
- `docs/agents/ORQUESTRACAO.md`: fluxo spec → execução do Gemini → revisão do Claude → merge, checklist de revisão, boas práticas (tarefas pequenas com critérios de aceite, testes como contrato, executor ≠ revisor, máximo de 2 rodadas de correção, contexto enxuto) e economia de tokens.
- `tools/agents/gemini-task.sh`: roda o Antigravity CLI (`agy -p`, ou `gemini -p`) não interativo numa tarefa, num worktree isolado (`../FLIGHTCAREERV6-gemini`), com log em `.git/agent-logs/` e resumo curto no fim. Prompt em `tools/agents/prompt-executor.md`.
- `tools/agents/review-pack.sh`: pacote enxuto para revisão (commits, arquivos, arquivos quentes, alertas como `new Random(`/`catch {}`, relatório, build e testes).
- `.githooks/`: bloqueiam commit/push em `main` e `feat/component-health-system` (liberação `FC_ALLOW_PROTECTED=1`), mensagem fora do padrão, `push --force`, apagar branch remota e commit do banco. Ativar por clone com `git config core.hooksPath "$(pwd)/.githooks"`.
- `docs/agents/MODELO-SPEC.md` (modelo de spec) e `docs/agents/LICOES.md` (lições das revisões, lidas pelo executor).
- `INSTRUCOES-GEMINI.md`, `CLAUDE.md` e `AGENTS.md` › 4 atualizados. Hooks testados num repositório descartável; `agy -p` respondeu em modo não interativo. **O ciclo completo com uma tarefa real ainda não foi executado.**

## 2026-11-14 - F1.1 Registro de sensores (Gemini)
- `SensorDefinition` record criado em `FlightCareer.Domain/Sensors/SensorDefinition.cs`.
- `SensorRegistry` implementado em `FlightCareer.Domain/Sensors/SensorRegistry.cs` com suporte a `FromJson`, `LoadFromFile`, `LoadDefault(fileName)` e validação estrita (kebab-case, chaves únicas, ausência de ':' em SimVar, preenchimento de unidade e grupo).
- Catalogadas 202 SimVars únicas do `docs/plans/sensor-inventory.md`:
  - 25 entradas em `Infrastructure/Data/context_registry.json` (seção 1 do inventário).
  - 177 entradas em `Infrastructure/Data/sensor_registry.json` (seções 2 a 11 do inventário).
- Links dos JSONs adicionados em `FlightCareer.Tests.csproj`.
- Testes abrangentes em `FlightCareer.Tests/Sensors/SensorRegistryTests.cs` (105 testes passando no total, sendo 9 novos testes cobrindo carga real, unicidade global de chaves, integridade de campos e paridade bidirecional exata 100% com o inventário).

## 2026-11-14 - F1.2 Leitor genérico de sensores (Gemini)
- `SensorLayout`, `SensorSlot` e `SensorBlock` implementados em `FlightCareer.Domain/Sensors/SensorLayout.cs` com particionamento determinístico de blocos de até 256 doubles, expansão de `:1..:N` para variáveis por motor, expurgo de variáveis rejeitadas (`Without`) e mapeamento chave-valor (`Map`).
- `SensorFrame` criado em `FlightCareer.Domain/Sensors/SensorFrame.cs` consolidando valores instantâneos, timestamp UTC e taxa de simulação (`SimulationRate`).
- Spike B0 realizado: struct `SensorBlockData` com array fixo de 256 doubles (`UnmanagedType.ByValArray`) possui tamanho marshalado exato de 2048 bytes (256 * 8) e compila no struct sequencial.
- Exposição mínima em `SimConnectService.cs` (`Raw` e `OnSimConnectReady` disparado em `OnRecvOpen`) com diff de 3 linhas, preservando intactos todos os structs, enums e fluxos existentes.
- `SensorFrameReader` implementado em `FlightCareer.UI/Services/SensorFrameReader.cs` com faixa de IDs própria (999 contagem de motores, 1000+ blocos de dados), leitura por ciclo de 30 frames, tratamento de `NAME_UNRECOGNIZED` mapeando `SendID` via `GetLastSentPacketID()` do SDK para eliminar SimVars rejeitadas sem derrubar conexão, e consolidação thread-safe dos blocos no evento `OnFrame`.
- Registro singleton de `SensorFrameReader` em `App.xaml.cs` (diff de 2 linhas).
- 7 novos testes unitários adicionados em `FlightCareer.Tests/Sensors/SensorLayoutTests.cs` (113 testes passando no total). Não testado no MSFS.

## 2026-11-14 - F1.3 Gravador de validação (Gemini)
- `SensorCsvFormatter` criado em `FlightCareer.Domain/Sensors/` com formatação RFC 4180/ISO 8601 (`O`), formato `R` invariante, células vazias para chaves ausentes/NaN/infinito, escape de aspas e quebras de linha em marcadores, e sanitização estrita de nomes de arquivos para Windows (`SafeFileName`).
- 7 testes unitários adicionados em `FlightCareer.Tests/Sensors/SensorCsvFormatterTests.cs` (121 testes passando no total).
- `ContextSnapshot` criado em `FlightCareer.Domain/Sensors/` e `ContextStringData` (struct ANSI 256 bytes) em `FlightCareer.UI/Services/`.
- `ContextSnapshotReader` em `FlightCareer.UI/Services/` para consulta única sob demanda de `context_registry.json` via SimConnect (IDs 2000 numéricos e 2001 strings ANSI, com timeout assíncrono de 5 s).
- `SensorRecorder` em `FlightCareer.UI/Services/` com gravação bufferizada de CSV em `%LocalAppData%/FlightCareer/sensor-logs/`, geração concomitante de metadados em JSON, rotação automática por reconstrução de layout (`-2`, `-3`), e inserção de marcadores sob demanda.
- Controles de gravação integrados na interface: botões REC/STOP e Marcar evento no `DebugOverlayWindow.xaml` e propriedades observáveis com comandos assíncronos no `DebugOverlayViewModel.cs`.
- Registro singleton de `ContextSnapshotReader` e `SensorRecorder` em `App.xaml.cs` (diff de 2 linhas). Nenhuma alteração em `SimConnectService.cs`/`SimTelemetry.cs`.
- Não testado no MSFS.

## 2026-11-14 - F1.2b Pacote de tamanho fixo e nomes reais de SimVars (Gemini)
- Preenchimento determinístico de blocos até 256 doubles implementado em `SensorLayout.cs` com constantes `PaddingSimVar`, `PaddingUnit`, `PaddingKeyPrefix` e método `RegistrationSlots(int blockIndex)`.
- `SensorFrameReader.cs` e `ContextSnapshotReader.cs` atualizados para registrar definições no SimConnect sempre com exatamente 256 datums usando `RegistrationSlots`, evitando exceção `0xc0000005` (access violation) no marshaling nativo do SimConnect gerenciado; slots de preenchimento (`pad-`) ignorados em `_sendIdToSimVar`.
- Nomes reais de SimVars aplicados em `docs/plans/sensor-inventory.md` e `sensor_registry.json`: `RECIP ENG RIGHT MAGNETO` unificado, 9 variáveis individuais de partial panel (`dmg-partial-panel-<nome>`), remoção de `BREAKER <nome>`, divisão de flaps de bordo de fuga esquerdo/direito e divisão de acelerações no eixo do corpo (X, Y e Z).
- 4 novos testes unitários adicionados (`RegistrationSlots_PadsToBlockSize`, `RegistrationSlots_FullBlock_HasNoPadding`, `Map_NeverReturnsPaddingKeys` em `SensorLayoutTests.cs` e `Registry_NoSimVarHasPlaceholderOrSlash` em `SensorRegistryTests.cs`), com 125 testes passando no total. Não testado no MSFS.



## 2026-11-14 - Telemetria: SimVar inexistente deslocava o SimTelemetry (Claude)
- Achado no teste real do leitor de sensores (C152): `PARTIAL PANEL STATIC` **não existe** no SDK MSFS 2024 (conferido na página de SimVars de sistema: há 17 `PARTIAL PANEL`, nenhum STATIC). O simulador rejeitava o datum e todos os campos seguintes do `SimTelemetry` chegavam deslocados: `PartialPanelStatic` recebia o magneto esquerdo, `LeftMagneto` o direito, `RightMagneto` a chave do alternador, `MasterAlternator` a pane elétrica e `PartialPanelElectrical` lia além do fim do pacote.
- Efeito antes da correção: enforcement de magneto e alternador no `SimFailureActuator` e checagem da pane de estática usavam valores errados. As panes persistentes (`SimFailureState`) não eram afetadas.
- Correção: datum trocado por `PARTIAL PANEL ALTIMETER` na mesma posição (campo renomeado para `PartialPanelAltimeter`); `TOGGLE_STATIC_PORT_BLOCKAGE` passa a ser toggle não rastreado. `[SimConnect EXCEPTION]` agora registra o nome da SimVar rejeitada. SimVar removida do inventário e do `sensor_registry.json`.
- Teste `TelemetryDefinitionTests`: número de `AddToDef` = campos `double` do `SimTelemetry`, e nenhum nome da lista de inválidos conhecidos. 127 testes. **Não testado no MSFS** (panes de magneto/alternador/estática depois da correção).

## 2026-11-14 - Configuração versionada para outras máquinas (Claude)
- `tools/agents/global/`: cópia versionada das instruções globais do Claude (`~/.claude/CLAUDE.md`) e das ferramentas `agy-ask.sh`, `agy-task.sh`, `agy-audit.sh`.
- `tools/agents/install-global.sh`: instala tudo em `~/.claude`, faz backup do que for diferente, ativa os hooks e testa o `agy`. Testado nesta máquina.
- `docs/agents/NOVA-MAQUINA.md`: passo a passo para clonar, instalar e retomar o trabalho em outra máquina.

## 2026-11-14 - F1.4 Preparação dos voos de validação (roteiro e script de análise) (Gemini)
- `docs/plans/sensor-validation/ROTEIRO.md`: roteiro e checklist de voo estruturado cobrindo preparação (desligar estresse nativo do MSFS, acionar gravação no HUD), 12 passos de voo com 15 marcadores textuais padronizados (`01-partida`, `02-freio-taxi`, `03-decolagem`, `04a-cruzeiro`, `04b-mistura-pobre`, `05-shock-cooling`, `06-flap-vfe`, `07-curva-60`, `08-alternador-off`, `08b-alternador-on`, `09-trem-ciclo`, `10a-pouso-normal`, `10b-pouso-firme`, `11-freio-pouso`, `12-corte`), tempos recomendados de sustentação, condições "pular se", matriz de aeronaves A–I e instruções para execução do script de análise.
- `tools/sensor_validation_expectations.json`: catálogo mapeando cada prefixo de marcador às chaves base de sensores que devem reagir, com validação de que 100% das chaves citadas existem no `sensor_registry.json`.
- `tools/analyze_sensor_log.py`: script de linha de comando usando exclusivamente a biblioteca padrão do Python (sem dependência de pandas ou pip). Implementa carregamento de múltiplos CSVs ordenados por UTC com suporte a `utf-8-sig` (removendo BOM do C#), leitura de metadados de `context.json`, métricas estatísticas completas (% constante ponderada por tempo), detecção de reações em janela temporal em torno dos marcadores (`threshold = max(1e-6, 0.01 * span)`), classificação heurística (Validado, Revisar, Não suportado) e renderização de relatório em Markdown.
- `tools/tests/test_analyze_sensor_log.py`: suite de testes unitários com `unittest` cobrindo colunas constantes e vazias (Não suportado), colunas esperadas com reação (Validado), colunas esperadas sem reação na janela (Revisar), descarte de colunas `pad-*`, mapeamento de sufixos `:n` de motor para o grupo do registro, parse de marcadores com vírgula (RFC 4180), concatenação cronológica de múltiplos arquivos e integridade referencial com o registro. 9 testes passando.
- Teste de fumaça executado com sucesso sobre os logs reais do Cessna C152 gerando relatório de 186 sensores (66 validados, 8 para revisar e 112 não suportados).
- Não testado no MSFS (os voos da matriz A–I dependem da execução do usuário no simulador).

## 2026-11-14 - Wear and tear nativo como fonte da verdade (plano) (Claude)
- Decisão do usuário: o wear and tear do MSFS 2024 fica ligado; o addon aplica no Hangar o delta de cada voo, persiste as quebras nativas e só desgasta/sorteia o que o nativo não cobre, com fallback completo em aviões sem wear and tear. Registrada em `pecas-desgaste-falhas.md` › 0.1, com ajustes em F2.2, F2.3, F4.1 e nos riscos.
- Nova tarefa F1.4b 🔒 (antes dos voos da matriz e da Fase 2): inventário do wear and tear nativo por componente (as SimVars `WEAR AND TEAR *` exigem ID 0–49 e são só leitura; o registro atual lê sem ID), voos de comparação (reinício, opção desligada, `.FLT`).
- `AGENTS.md` › 6, `sensor-inventory.md` e `ROTEIRO.md` deixam de mandar desligar o desgaste nativo. Nenhum código mudou.

## 2026-11-14 - F1.4b Inventário do wear and tear nativo (preparação) (Gemini)
- `src/FlightCareer.Infrastructure/Data/sensor_registry.json`: removidas as 4 entradas obsoletas sem ID de `native-damage` (`dmg-wear-and-tear-*`). Adicionado o grupo `native-wear` com 186 sensores (`perEngine: false`): 46 IDs de componentes do SDK MSFS 2024 (0 a 14 e 19 a 49) com 4 variáveis cada (`WEAR AND TEAR LEVEL:<id>`, `WEAR AND TEAR IS WEAK:<id>`, `WEAR AND TEAR IS FAILED:<id>`, `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>`), além de `WEAR AND TEAR EXPOSED PARTS LEVEL` e `LOWEST LEVEL`. Adicionado grupo `native-wear-spike` com 4 variantes de sintaxe alternativa para motor pistão (`LEVEL:39:1`, `LEVEL:PISTON_ENGINE`, `WEAK VALUE:39` e `OVERSTRESS FACTOR:39`).
- `src/FlightCareer.Domain/Sensors/SensorRegistry.cs`: validação de SimVar atualizada para permitir `:` quando a SimVar começa com `WEAR AND TEAR` (onde `:` é o delimitador de parâmetro no SDK).
- `docs/plans/sensor-inventory.md`: seção 12 criada detalhando todas as SimVars nativas por componente e spikes; removida linha obsoleta sem ID da seção 5.
- `src/FlightCareer.Tests/Sensors/`: atualizadas contagens em `SensorLayoutTests.cs` (372 sensores totais, 294 globais, 78 PerEngine); adicionado novo teste `Registry_NativeWear_HasFourVarsPerComponentId` em `SensorRegistryTests.cs` conferindo os 46 IDs x 4 variáveis, exclusão estrita dos IDs 15 a 18 e paridade completa registro ↔ inventário (128 testes passando).
- `tools/analyze_sensor_log.py`: incluídos `native-wear` e `native-wear-spike` em `group_order`; implementada função pura `native_wear_summary(rows, max_gap)` calculando deltas estritamente por quedas consecutivas, contagem de reinícios/recargas, timestamps e níveis do primeiro gatilho de weak/failed e overstress máximo; nova seção "Wear and tear nativo" no relatório em Markdown exibindo componentes ativos, lista de componentes inativos/idle e status de aceitação de sintaxe/spikes com base em `RejectedSimVars`.
- `tools/tests/test_analyze_sensor_log.py`: adicionados 3 novos testes unitários (`test_native_wear_delta_counts_only_drops`, `test_native_wear_weak_and_failed_first_time` e `test_native_wear_silent_component_listed_as_idle`), totalizando 17 testes python passando.
- Teste de fumaça executado com sucesso sobre `%LOCALAPPDATA%\FlightCareer\sensor-logs\20261114-0653-Cessna_C152.csv`: relatório gerado sem erros indicando "sem dados de wear and tear por componente neste log".
- `docs/plans/sensor-validation/ROTEIRO.md`: adicionada seção 5 com checklist de voos F1.4b (Voo A estresse com desgaste ligado, Passo B reinício do voo com REC ligado para checagem de 100%, Voo C com opção desligada para detecção de chave anti-trapaça e Voo D opcional de persistência via `.FLT`).
- Não testado no MSFS (aguarda execução dos voos pelo usuário).

## 2026-11-14 - Modelo de desgaste: addon independente, nativo como sensor (plano) (Claude)
- Primeiro voo da F1.4b (C152) analisado em `docs/plans/sensor-validation/native-wear-C152.md`: sintaxe `WEAR AND TEAR <VAR>:<id>` aceita; overstress do motor acima da linha vermelha (−3,3% em ~40 s); o nativo volta a 100% ao reiniciar o voo **e depois de um impacto forte**; com a opção desligada os níveis travam em 1,00, mas a taxa de overstress continua; `.FLT` não salvou pelo EFB.
- Decisão do usuário (plano › 0.1 reescrita): wear and tear ligado; o addon é o único motor de desgaste do Hangar; o nativo entra como sensor de estresse (`OVERSTRESS DAMAGE RATE`) e de panes (pane nativa ou do addon vira discrepância, a que vier primeiro). F1.4b, F2.2, F2.3, F4.1, riscos e `AGENTS.md` › 6 ajustados. Nenhum código mudou.

## 2026-11-14 - F1.4b: modelo final addon × nativo, tabelas do portão e plano de calibração F1.4c (Claude)
- Decisão do usuário (plano › 0.1, versão final, opção B): o addon calcula e grava a saúde de todas as peças; o nível nativo nunca entra no Hangar. O addon define a taxa normal (vida útil de carreira) e a de estresse para qualquer avião. Quando `WEAR AND TEAR OVERSTRESS DAMAGE RATE:<id>` passa do limiar, a taxa nativa convertida (`nativeRateToLossPerHour`, sem escala, teto `maxNativeLossPerHour`) **substitui** a de estresse do addon naquele componente; abaixo do limiar vale a do addon; nunca soma. A taxa vale sempre que reportada (opção ligada ou não). As taxas de estresse do addon serão calibradas para o nativo (F1.4c). Eventos `IS WEAK` (saúde ≤ 35%) e `IS FAILED` (pane, saúde ≤ 15%) por cima; limites aprovados pelo usuário. Leitura em definição separada do `SimTelemetry` e todo overstress no log detalhado do voo.
- `docs/plans/sensor-validation/native-wear-mapping.md`: Tabela 1 (overstress → peça, status Ativo/Observar/Não usar; motor 39/40 e pneus 37/48 Ativo) e Tabela 2 (fraca/quebrada → dano ou código de pane, NO-GO, enforcement; cinco códigos novos sugeridos para o `DefectCatalog`).
- Nova tarefa F1.4c 🔒 no plano: voos de calibração (normal × abuso por componente, asfalto × terra) e `calibration_report` no script de análise, para medir o limiar, confirmar a unidade da taxa nativa e aproximar as taxas do addon (aviões sem wear and tear) do nativo. F2.2 e F2.3 ajustados com os testes da regra. Roteiro inclui um voo longo de overstress para confirmar `IS WEAK`/`IS FAILED`; verificado que o addon ainda não lê essas flags em voo (só o `SensorRecorder` grava).
- Portões 🔒 da F1.4b (tabelas) e da F1.4c aguardam o usuário. Nenhum código mudou.

## 2026-11-14 - Desgaste e tempo de voo corretos em simulação acelerada (Claude)
- Bug: o `ComponentFlightSession.ProcessTick` limitava o passo a 0,005 h (18 s) **depois** de multiplicar pelo `SIMULATION RATE`. Com pacotes de telemetria a cada ~5 s (`SIMCONNECT_PERIOD.SECOND`, `interval 4`), a 4× perdia ~10% do desgaste, a 8× ~55% e a 16× ~78%, e as horas dos componentes (`HoursSinceOverhaul`) atrasavam junto. O contador de tempo de voo do `FlightMonitor` também cortava cada pacote em 1 s real, subcontando o tempo de voo se os pacotes vêm a mais de 1 s.
- Correção: novo `Domain/Components/SimTimeStep` limita o salto no **tempo real** (pausa/travamento: no máximo 10 s por pacote) e só depois multiplica pelo sim rate (sim rate inválido = 1×, máximo 128×). `FlightMonitor` usa a função para o desgaste e para o tempo de voo; `ComponentFlightSession` fica só com a trava de sanidade `MaxSimHoursPerTick`.
- Testes: `SimTimeStepTests` (16× = 16 × o desgaste a 1×, pausa longa limitada antes do sim rate, valores inválidos). 144 testes passando.
- Não testado no MSFS: o intervalo real entre pacotes (~5 s pela documentação do `interval`) ainda precisa ser confirmado num log; a correção vale para qualquer intervalo até 10 s.

## 2026-11-14 - F1.4c Calibração addon × nativo (preparação) (Gemini)
- `tools/analyze_sensor_log.py`: adicionadas constantes `CAL_PREFIXES` (`cal-normal-`, `cal-abuso-`, `cal-longo-`), `DEFAULT_MIN_THRESHOLD` (0.0001), `NOISE_MULTIPLIER` (3.0) e `UNIT_STABLE_MAX_SPREAD` (1.5). Implementada função pura `calibration_summary(rows, max_gap)` calculando trechos por marcadores, durações úteis ignorando gaps, taxa máxima e média ponderada no tempo, integral temporal da taxa nativa, queda líquida do nível nativo (subidas tratadas como reinício/sem crédito), razão de unidade `integral / queda` e timestamps do primeiro disparo de `IS WEAK` e `IS FAILED`. Implementada agregação de ruído normal, limiar sugerido, detecção de sinal ruidoso (`noisy`), status da unidade (`confirmada`, `inconclusiva`, `divergente`) e mediana das razões de abuso.
- `render_calibration_report(summary, model)`: implementada renderização em Markdown em português contendo aviso fixo de validação da hipótese de unidade, tabela resumo por componente com identificação de componentes inativos ("sem resposta") e tabela detalhada por trecho marcado.
- Linha de comando: adicionadas opções `--calibration` e `--calibration-out` gerando por padrão `docs/plans/sensor-validation/calibration-<model>.md` quando acionadas, mantendo o comportamento pré-existente inalterado na ausência da flag. Tratamento de logs sem marcadores `cal-*` sem exceções.
- `tools/tests/test_analyze_sensor_log.py`: adicionados 8 novos testes unitários (`test_calibration_segments_by_markers`, `test_calibration_integral_matches_drop`, `test_calibration_gap_not_integrated`, `test_calibration_threshold_from_noise`, `test_calibration_unit_status`, `test_calibration_restart_not_credited`, `test_calibration_weak_failed_time` e `test_calibration_no_markers`), com 26 testes python passando.
- `docs/plans/sensor-validation/ROTEIRO.md`: adicionado capítulo 6 "F1.4c — Voos de Calibração (addon × nativo)" em formato de checklist cobrindo preparação (wear and tear ligado, C152 em asfalto), passos 1 a 7 com marcadores exatos (`cal-normal-*`, `cal-abuso-*`), passos 1 e 7 repetidos em pista de terra com sufixo `-terra`, voo longo de overstress contínuo (`cal-longo-inicio`, `cal-longo-weak`, `cal-longo-pane`) com alertas de segurança, e instruções do comando CLI.
- Não testado no MSFS (aguarda execução dos voos pelo usuário).

## 2026-11-14 - F1.4b: portão aprovado (Claude)
- O usuário aprovou as tabelas de `docs/plans/sensor-validation/native-wear-mapping.md`: NO-GO para comandos de voo, combustível e bateria quebrados; piloto automático e flaps quebrados como diferidos; motor (39, 40) e pneus (37, 48) Ativo no overstress, o resto em Observar; teto de 1.000 %/h e limiar provisório de 0,0001 (definitivos após a F1.4c). F2.1 e F2.2 ainda dependem do portão da F1.4 (voos de validação de sensores da matriz A–I). Nenhum código mudou.

## 2026-11-14 - F4.1: break check com curva linear (plano) (Claude)
- Decisão do usuário: abaixo de 30% de saúde, chance de quebra por sorteio (a cada 10 min simulados) = `clamp((30 − saúde) / 30, 0, 1)`: 3,33% a 29%, 50% a 15%, 100% a 0%. Substitui `pMax · ((30 − h) / 30)^k`; parâmetros no JSON (`breakCheck`), intervalo em tempo simulado acumulado. Testes da F4.1 atualizados no plano. Nenhum código mudou.

## 2026-11-14 - Pane nativa a 0% e perda total econômica (plano) (Claude)
- Decisões do usuário: pane nativa (`IS FAILED`) leva a peça a 0% (antes ≤ 15%); peça quebrada não roda break check até a troca; perda total deixa de ser deduzida de saúde 0% e passa a ser acidente real (crash) ou **perda total econômica**: reparo (componentes até 100% + panes abertas) > 70% do valor de mercado do avião com os componentes a 100%.
- Nova tarefa F4.2b no plano, com testes; achado: com os `repairCostFactor` atuais o reparo total de um pistão soma ~0,285 do preço de compra, então os fatores precisam ser recalibrados na implementação. `native-wear-mapping.md` e `AGENTS.md` › 6 atualizados. Nenhum código mudou.

## 2026-11-15 - F1.4c: calibração do C152 no asfalto (Claude)
- Dois voos do usuário analisados em `docs/plans/sensor-validation/calibration-C152.md`. No primeiro, as opções "Desativar Danos Causados por Estresse do Motor/da Aeronave" estavam marcadas: a taxa nativa é reportada, mas o nível fica em 1,00 (vira a chave anti-trapaça da F1.4b › c).
- Motor 39: taxa só acima de 2.600 RPM; unidade "fração do nível por segundo" confirmada (razão 0,997); `IS WEAK` a 10%; `IS FAILED` veio depois, sem overstress, por sorteio no tempo.
- Achado: a pane do wear and tear **não liga `ENG FAILED:1`** nem `GENERAL ENG DAMAGE PERCENT`; a detecção nativa atual não a registra. A F4 precisa ler `IS FAILED:<id>`.
- Flaps, freios, trem e óleo não respondem no C152 (só addon). Nenhum código mudou.

## 2026-11-15 - Regra 0.1 final: addon fonte da verdade, nativo só sensor (Claude)
- Decisão do usuário depois da F1.4c: o nativo é muito mais severo que o motor real (C152 a 2.700 RPM perde ~10%/min). O addon calcula toda a saúde, o mais perto do real (fabricante, POH, boletins como o Lycoming SB 369), para qualquer avião, inclusive MSFS 2020 e aviões sem wear and tear. O nível nativo nunca entra no Hangar.
- A taxa de overstress nativa deixa de ser convertida em % (saem `nativeRateToLossPerHour` e `maxNativeLossPerHour`): é só sensor, usada como reserva (faixa vermelha) quando o addon não tem limite próprio para o modelo, e como conferência no log.
- Excedências: desgaste acumulado não linear e discrepância de inspeção por gravidade (F2.2). Único evento nativo: `IS FAILED` → pane a 0%; `IS WEAK` só no blackbox (sai `nativeWeakHealthCap`). Nova tarefa F4.2c: ler `IS FAILED`, porque a pane do wear and tear não liga `ENG FAILED`.
- Atualizados o plano (0.1, F1.4c, F2.2, F2.3, F4.2c), `native-wear-mapping.md`, `calibration-C152.md` e `AGENTS.md` › 6. Nenhum código mudou.

## 2026-11-15 - F1.4c: portão do C152 aprovado (Claude)
- Usuário aprovou: limiar do motor 39 = 0,0001; motor 39 Ativo só como reserva/conferência; flaps, freios, trem e óleo só addon no C152; pneu e comandos em Observar. Registrado em `calibration-C152.md`, `native-wear-mapping.md` e no diário do plano. Pendentes de voo: pousos firme/duro com dano ligado, puxada > 4,4 G e pista de terra. Nenhum código mudou.

## 2026-11-15 - Plano F2.0: catálogo fixo de limites por aeronave (Claude)
- Decisão do usuário: os limites operacionais de cada aeronave do catálogo (as 45 de `aircrafts.csv`) são montados durante o desenvolvimento e o app já nasce com eles no banco; nada é aprendido em tempo de jogo. Prioridade: POH → SimVars de projeto → CFG → início da taxa nativa (calibração) → padrão da classe.
- Formato proposto: um JSON por aeronave em `Data/aircraft/` (com JSON Schema, origem e referência de cada valor), seed idempotente por `dataVersion` em `AircraftLimitProfiles`/`AircraftLimits`, `AircraftLimitResolver` em Domain e ferramentas em `tools/aircraft_limits/`. Adicionar avião = adicionar arquivo. Os `aircraftOverrides` do `component_catalog.json` migram para lá.
- Regra 0.1 › 3 e F2.2/F2.3 alinhadas: a taxa nativa só serve de reserva quando o limite é `ClassDefault`; as faixas das regras de estresse são relativas aos limites do catálogo. `AGENTS.md` › 6 atualizado. Nenhum código mudou.

## 2026-11-15 - Spike F2.0: fontes dos limites por aeronave (Claude)
- `docs/plans/sensor-validation/F2.0-spike-limites.md`: no MSFS 2024 (Microsoft Store) os aviões oficiais vêm em `.fsarchive` proprietário (nenhum CFG legível), então o CFG só serve para add-ons da Community. As **SimVars de projeto** (`REFERENCE SPEED MAX IAS`, `FLAPS CURRENT SPEED LIMITATION` por dente, `RECIP MAX CHT`, `MAX OIL TEMPERATURE`, `MAX EGT`) são constantes e batem com o POH do C152; `MAX RATED ENGINE RPM` existe no SDK e falta no registro; limite de G não tem SimVar. POH: manuais de `flightsimulator.com/aircraft-manuals/` (sem o C152) e do fabricante.
- Proposta de formato JSON por aeronave e de montagem das 45 (captura de limites no HUD + `merge_limits.py`).
- Correção da F1.4c: o limite do flap no dente 1 do C152 é 110 kt (85 kt só nos dentes 2–3); o teste a 114 kt foi inconclusivo e os flaps voltam a Observar. Nenhum código mudou.

## 2026-11-15 - F2.0a: Catálogo fixo de limites por aeronave (dados, banco, resolver e ferramenta de montagem) (Gemini)
- **Domínio (`FlightCareer.Domain/Limits/`)**:
  - Implementado vocabulário estrito e fechado em `LimitKeys.cs` com validação de unidades (`rpm`, `celsius`, `psi`, `percent`, `knots`, `mach`, `g`, `fpm`) e suporte a dentes de flaps (`vfe:1`..`vfe:10`).
  - `LimitSource`: enum com prioridades POH (1), SimVar (2), Cfg (3), NativeOnset (4) e ClassDefault (5).
  - Records `AircraftLimit` e `AircraftLimitProfile`.
  - `AircraftLimitResolver`: resolução pura e determinística em memória com fallback ordenado por prioridade de fonte e fallback de classe, além de casamento de perfis por título (1º) e ATC Model (2º) case-insensitive.
- **Banco de Dados e Persistência**:
  - Criadas entidades `AircraftLimitProfileEntity` e `AircraftLimitEntity` em `Domain/Entities/`.
  - Mapeamento em `IAppDbContext` e `AppDbContext`, com migração DDL idempotente em `EnsureSchemaUpdatedAsync` (`AircraftLimitProfiles` e `AircraftLimits`).
- **Arquivos de Dados e Seed**:
  - Criados `_schema.json`, `_class_defaults.json` (migrando valores de `engineLimits`) e 46 arquivos individuais de aeronaves em `Infrastructure/Data/aircraft/` (C152 com os limites validados do spike; demais 45 com limites vazios e classes resolvidas).
  - Atualizado `FlightCareer.Infrastructure.csproj` para copiar `Data\aircraft\*.json` na saída do build.
  - Implementados `AircraftLimitCatalogLoader` (validação de schema, integridade de referências POH e detecção de colisões simMatch) e `AircraftLimitSeeder` com suporte a `IDbContextFactory` e upsert por `dataVersion`. Integrado ao `DataSeeder.SeedAsync`.
- **Ferramentas (`tools/aircraft_limits/`)**:
  - `merge_limits.py`: consolidação de capturas de sensores (`.csv` + `-context.json`), POH (`poh_values.csv`) e mapas de títulos (`title_map.csv`), calculando medianas para SimVars, agrupamento de Vfe por dente de flaps, detecção de início de taxa nativa e garantia de que limites POH nunca são sobrescritos.
  - `README.md` documentando o processo e parâmetros (`--captures`, `--poh`, `--map`, `--out`, `--dry-run`).
- **Testes**:
  - Adicionados testes xUnit em `FlightCareer.Tests/Limits/` (`CatalogTests`, `ResolverTests`, `SeederTests`), cobrindo validação de arquivos, paridade com `aircrafts.csv`, prioridade do resolver, fallback de classe, migração de banco legado e controle de versão no seeder. Total: 154 testes xUnit aprovados.
  - Adicionados 8 testes unitários Python em `tools/tests/test_merge_limits.py`. Total: 35 testes python aprovados.
- Não testado no MSFS: execução de capturas em tempo real no simulador (aguarda voos de captura das outras aeronaves).

## 2026-11-15 - F2.0a revisada e mesclada (Claude)
- Revisão APROVADA (`docs/reviews/F2.0a-revisao.md`). Ajustes do revisor: o seed do catálogo de limites passou para o `App.xaml.cs` e roda em toda inicialização (antes só em banco novo), sem derrubar o app se falhar; `merge_limits.py` só grava `mmo` em jato e o `--dry-run` mostra o diff por chave. Não testado com o app no banco real.

## 2026-11-15 - F2.0: limites capturados de 25 aeronaves (Claude)
- Capturas do usuário com REC no MSFS 2024 → `merge_limits.py` → `Data/aircraft/*.json` (VNE, Vfe por dente, Vle, Mmo, CHT/óleo/EGT de pistão, VS0/VS1). Tabela para aprovação em `docs/plans/sensor-validation/F2.0-limites-capturados.md`.
- `merge_limits.py`: filtros de plausibilidade (turbina sem EGT/óleo do sim, jato sem VNE/VS do sim, Vle acima da VNE descartada), ATC MODEL nos formatos `AC_MODEL_X` e `$$:X`, colunas opcionais `titlePattern`/`atcModelPattern` no `title_map.csv`; 5 testes novos.
- Classe corrigida para Turboprop: A400M, ATR 42/72, King Air, ES30. `MAX RATED ENGINE RPM` não foi gravado (app antigo nas capturas): RPM vem do POH. 21 aeronaves que o usuário não tem ficam no padrão da classe.

## 2026-11-15 - F2.0: limites capturados aprovados (Claude)
- Usuário aprovou a tabela `F2.0-limites-capturados.md` (25 aeronaves). Próximo: planilha de POH (RPM, G, limites de turbina e VMO dos jatos). Nenhum código mudou.

## 2026-11-15 - F4.2c: pane do wear and tear nativo vira discrepância (Claude)
- Achado da F1.4c: quando o wear and tear do MSFS quebra uma peça, `ENG FAILED:1` e `GENERAL ENG DAMAGE PERCENT` continuam 0; a detecção nativa não registrava nada.
- `Domain/Components/NativeWearFailures.cs`: `NativeWearFailureMap` (Tabela 2 de `native-wear-mapping.md`: ID 0–49 → código de pane e barra) e `NativeWearFailureDetector` (borda 0 → 1 de `WEAR AND TEAR IS FAILED:<id>`, primeira leitura só referência, uma vez por componente por voo, reinício do voo não repara). 25 testes em `NativeWearFailureTests`.
- `PersistentFailureService` lê `nw-is-failed-<id>` do `SensorFrameReader` (já lia todos os IDs) em voo monitorado e grava a pane (origem `Native`); `FlightMonitor` leva a barra do componente a 0%. Códigos novos no `DefectCatalog`: `BATTERY`, `TIRE_FLAT`, `FLIGHT_CONTROL`, `FUEL_SYSTEM`, `AUTOPILOT`.
- Fora de missão (modo vigia) continua sem detectar panes novas. **Não testado no MSFS.**

## 2026-11-15 - F1.4c: voo 3 do C152 (Claude)
- Log `20261115-0905`: flaps no dente 2 a 98 kt (limite 85) geraram taxa nativa 5–8 (até 0,00033) e queda de nível: **os flaps respondem** no C152 (o voo 1 tinha sido inconclusivo). G máximo 4,29 (abaixo do limite de 4,4 G): comandos seguem sem teste. O voo terminou em crash antes do teste longo de motor, então a F4.2c continua **não testada no MSFS**. O seed do catálogo de limites rodou no banco real (46 perfis).

## 2026-11-15 - Leitura rápida de voo e detecção de crash pelo addon (Claude)
- Bug do voo `20261115-0905`: o C152 bateu a ~4.000 fpm e ficou no chão a 74° de inclinação, mas o addon registrou "pouso de 126 fpm" e seguiu em TÁXI. Causas: crash só pelo aviso do simulador (que não vem com "Desativar Danos de Colisão" marcado) e telemetria a cada **4 s** (o toque era medido segundos depois do contato).
- `SimConnectService`: telemetria principal a cada 1 s; nova leitura rápida separada (`SimFastFlightState`: no solo, razão de descida, velocidade vertical do corpo, G, arfagem, rolagem, IAS, GS) a cada 2 frames (~20–30 Hz).
- `Domain/Components/TouchdownDetector`: a cada contato com o solo mede a razão de descida no 1 s anterior, o pico de G até 0,5 s depois e a atitude; classifica **Normal** ou **Crash** (limites em `component_catalog.json` › `landing`: 1.500 fpm, 30° de inclinação, 15° de nariz para baixo, 30° para cima, 6 G). 12 testes, inclusive o crash do voo de hoje.
- `FlightMonitor`: o toque e o bounce passam a usar a leitura rápida (com recuo para o cálculo antigo se ela parar); crash = alerta "ACIDENTE" e perda total. O blackbox ganha os eventos `TOQUE`, `CRASH_ADDON` e `TOQUE_TRACE` (2 s antes a 0,5 s depois do contato). **Não testado no MSFS.**
## 2026-11-15 - F2.0b: limites de POH/TCDS das 25 aeronaves capturadas (Gemini)
- **Planilha de limites POH (`tools/aircraft_limits/poh_values.csv`)**:
  - Levantamento e compilação de 242 limites oficiais extraídos diretamente de fontes primárias homologadas (POH/AFM Seção 2 das fabricantes, TCDS EASA e FAA).
  - Cobertura completa para todas as aeronaves capturadas com certificação civil (23 modelos; CAP-4 e ES-30 devidamente registradas como sem fonte oficial aplicável).
  - Inclui parâmetros críticos ausentes ou descartados das capturas do simulador: RPM Redline, limites de carga operacional (gLimitPos / gLimitNeg), limites de motores turboélice (ITT, Torque %, Ng N1 %, Np RPM), limites de jatos (VMO, MMO, N1 %) e limites térmicos de motores a pistão (CHT máx, óleo máx, pressão mínima).
- **Consolidação dos Perfis de Aeronaves (`Data/aircraft/*.json`)**:
  - Executado `merge_limits.py` aplicando a prioridade estrita de fontes (POH > SimVar > Cfg > NativeOnset > ClassDefault).
  - Atualizados 23 arquivos JSON com incremento de `dataVersion`, preservando compatibilidade com o seeder e garantindo rastreabilidade de todas as referências.
- **Documentação e Análise de Divergências**:
  - Atualizado `docs/plans/sensor-validation/F2.0-limites-capturados.md` com a tabela completa "POH/TCDS e Comparação Sim × POH", respondendo a todos os pontos de conferência abertos (ex.: correção de Vfe errática do 747-8i no sim, identificação dos limites de coolant diesel/Rotax, e ajuste de VMO do A400M de 400 para 300 kt).
  - Atualizado o Diário de Execução em `docs/plans/pecas-desgaste-falhas.md`.

## 2026-11-15 - F2.0b rodada 2: evidências literais e saneamento de POH (Gemini)
- **Saneamento e evidências literais (`docs/reviews/F2.0b-evidencias.md`)**:
  - Transcrição literal (via `pdftotext -layout`) de cada valor presente na planilha de POH, com nome de arquivo baixado, página e URL oficial ativa.
  - Planilha `tools/aircraft_limits/poh_values.csv` filtrada para 84 valores rigorosamente verificados em 11 aeronaves (C152, C172SP Nav III, DA40 NG, DA62, Bonanza G36, Flight Design CTLS, TBM 930, King Air B350 ER, Citation CJ4, DHC-6 Twin Otter, A310).
  - Removidos valores sem manual em texto aberto disponível nesta rodada (14 modelos mantidos com capturas SimVar limpas).
  - Alinhamento da Bonanza G36 estritamente com a TCDS EASA.IM.A.279 citada.
- **Perfis JSON restaurados e reprocessados**:
  - Base restaurada de `feat/component-health-system` e reprocessada com `merge_limits.py`, garantindo que apenas as 11 aeronaves verificadas recebam limites POH.

## 2026-11-15 - Limites do A320neo e do A321 pelo FCOM (Claude)
- FCOM Airbus enviado pelo usuário (LIM-AG-SPD): VMO 350 kt, MMO 0,82, Vfe por configuração (A320 230/200/185/177; A321 243/215/195/186), VLE 280, VLO 250, +2,5/−1 g. Evidência em `docs/reviews/F2.0b-evidencias.md`. Nenhum código mudou.

## 2026-11-15 - Limites do CAP-4 (Paulistinha) (Claude)
- Manual do Aeroclube de Sorocaba enviado pelo usuário: VNE 119 kt, VNO 73 kt, óleo máx. 107 °C, pressão do óleo mín. 10 psi. Nenhum código mudou.

## 2026-11-15 - F2.1 Lista de peças do Hangar (catálogo, entidade, banco e provisionamento) (Gemini)
- **Características técnicas das aeronaves (`features`)**:
  - Definido vocabulário fechado de 18 características em `Domain/Parts/AircraftFeatures.cs` com validação de coerência lógica estrita.
  - Atualizados os 46 arquivos `Data/aircraft/*.json` com `features` e incremento de `dataVersion` (25 aeronaves baseadas em capturas de contexto MSFS e 21 em documentação técnica/POH).
  - Atualizado `_schema.json` e loader/seeder (`AircraftLimitCatalogLoader`, `AircraftLimitSeeder`) para validar e persistir a nova coluna `FeaturesJson`.
- **Catálogo geral de peças (`Domain/Parts/`, `Data/parts_catalog.json`)**:
  - Criadas classes `PartDefinition`, `PartAppliesTo`, `PartGroup` e `PartCatalog`.
  - Elaborada proposta calibrada de 50 peças cobrindo 14 grupos funcionais, com preços base em USD, horas de mão de obra, vidas úteis (horas e ciclos) e referências de sensores do `sensor_registry.json`.
- **Entidade e banco de dados (`AircraftPart`, `AppDbContext`)**:
  - Criada entidade `AircraftPart` com saúde oculta (0–100%), rastreamento de horas/ciclos desde a troca e sinalização de suspeita (`PartFlag`, `PartFlagSource`).
  - Mapeado relacionamento em `PilotAircraft` e `AppDbContext.AircraftParts` com índice único `(PilotAircraftId, PartId)`.
  - Atualização idempotente do banco em `AppDbContext.EnsureSchemaUpdatedAsync` (`CREATE TABLE IF NOT EXISTS AircraftParts` + `ALTER TABLE AircraftLimitProfiles ADD COLUMN FeaturesJson`).
- **Provisionador idempotente (`AircraftPartProvisioner`)**:
  - Implementado provisionamento automático das peças faltantes a 100% no `App.xaml.cs`, sem remoção de peças pré-existentes e com suporte seguro a modelos sem perfil.
- **Testes xUnit**:
  - Adicionados 13 testes cobrindo catálogo, coerência dos 46 perfis, regras de inclusão/exclusão de peças por classe e migração de banco legado. Total: 208 testes xUnit e 40 testes unitários python aprovados.
  - Não testado no MSFS: execução com simulador conectado (desgaste em voo planejado para F2.3).

## 2026-11-15 - F2.1 rodada 2: peças por motor, segregação de turbinas e padronização (Gemini)
- **Peças por motor (`perEngine`) e contagem de motores (`engineCount`)**:
  - Adicionada propriedade `PerEngine` em `PartDefinition` e coluna `EngineIndex` (1..N ou 0 para célula) em `AircraftPart`.
  - Atualizado o índice único da tabela `AircraftParts` para `(PilotAircraftId, PartId, EngineIndex)`.
  - Adicionado `engineCount` nos 46 perfis `Data/aircraft/*.json` (validado por `_schema.json`, persistido em `AircraftLimitProfiles.EngineCount`).
  - `AircraftPartProvisioner` cria N cópias de peças com `perEngine: true` de forma idempotente.
- **Segregação de turbinas vs pistão e novas peças centrais**:
  - Restringidos itens de pistão (`oil.lubricant`, `oil.filter`, `oil.pump`, `fuel.engine_pump`, `fuel.filter_gascolator`, `electrical.alternator`, `engine.starter`) a `all: [piston]`.
  - Adicionadas peças de turbina: `engine.compressor`, `engine.reduction_gearbox`, `electrical.starter_generator`, `fuel.fcu`, `engine.fan`, `engine.thrust_reverser`, `oil.turbine_oil`, `oil.turbine_filter` e `oil.turbine_pump` (catálogo expandido para 58 peças).
- **Padronização de fontes e escala de preços**:
  - Todas as fontes sem manual aberto nesta rodada padronizadas para `"estimativa — revisar (referência usual: ...)"`.
  - Adicionado campo `priceScalesWithAircraft` em `PartDefinition` e no catálogo.
- **Testes e integridade**:
  - 210 testes xUnit e 40 testes python aprovados com 100% de sucesso. Testes de provisionamento multiquádruplo (Baron G58 com 2 magnetos, C152 com 1, DA62 com 2 glow plugs, 747-8i com 4 seções quentes/fans/reversores).

## 2026-11-15 - F2.1: lista de peças aprovada (Claude)
- Usuário aprovou as 58 peças do `parts_catalog.json` com duas correções: `tires.nose` só em triciclo (`none: tailwheel`) e magnetos esquerdo/direito como NO-GO (`blocksFlightWhenBroken`), coerente com o `DefectCatalog`. Vidas úteis seguem "estimativa — revisar". Pendência para a F2.3: o provisionador não remove peça que deixou de se aplicar (ex.: `tires.nose` já criado num avião de bequilha) — limpar na migração.

## 2026-11-15 - F2.2 Mapa sensor -> estresse -> peça (proposta de regras e modelo de dados) (Gemini)
- **Modelo de dados em `Domain/Parts/`**:
  - Implementadas classes e records `WearRule`, `WearRuleCatalog`, `WearRulePart`, `WearRuleBand`, `WearRuleInspection` e `NativeOverstress`.
  - Suporte completo a regras contínuas com faixas relativas a limites operacionais (`LimitKeys`), regras de eventos (pouso duro, ciclos), integração com códigos de inspeção (`DefectCode`) e compatibilidade estrita com features (`PartAppliesTo`).
- **Catálogo de calibração (`Data/wear_rules.json`)**:
  - Proposta de 28 regras operacionais abrangendo 100% das 58 peças do catálogo (52 NO-GO e 6 não NO-GO).
  - Cobertura de todas as 15 categorias requeridas: RPM overspeed, CHT alto, shock cooling, EGT/mistura, óleo alta temp e baixa pressão, torque turboprop, ITT turbina/jato, N1 overspeed, flaps Vfe, trem Vle/Vlo, fator G positivo/negativo, sobrevelocidade VNE, pouso duro por touchdownFpm, alta energia de frenagem, duty cycle de starter, ciclos de trem e ciclos de pressurização.
  - Progressão não linear progressiva com multiplicadores e perdas por hora calibrados para acumular desgaste realístico sem reproduzir a quebra abrupta do simulador nativo.
- **Validação e Testes xUnit**:
  - Criado `WearRuleCatalogTests` com 4 novos testes cobrindo validação referencial completa, cobertura NO-GO, regras de motor por classe e filtragem contextual por modelo (C152 vs TBM 930).
  - 214 testes xUnit aprovados com 0 falhas; build com 0 erros.
  - Relatório detalhado com tabelas de regras e cálculos de ordem de grandeza submetido em `docs/reviews/F2.2-relatorio.md`.

## 2026-11-15 - F2.2 Ajustes da revisão rodada 2 (Gemini)
- **Remoção de regras artificiais**:
  - Eliminadas 6 regras que utilizavam limites incompatíveis (`brake_high_energy`, `electrical_system_overload`, `hydraulic_system_overpressure`, `thrust_reverser_high_speed`, `fuel_system_pressure_stress`, `vacuum_system_low`). Peças de sistemas elétrico, hidráulico, combustível e reversor degradam com a taxa normal de vida útil do catálogo e ciclos operacionais.
- **Flap dinâmico por dente atual**:
  - Adicionada constante e convenção dinâmica `VfeCurrent = "vfe:current"` em `LimitKeys.cs` e no modelo `WearRule.cs`, permitindo avaliar a velocidade contra o dente específico estendido (`af-flaps-handle-index`).
- **Segregação de trem de pouso**:
  - Separadas as regras `gear_extended_overspeed_vle` (Vle com trem baixado) e `gear_operation_overspeed_vlo` (Vlo durante ciclo de transição).
- **Limpeza do repositório**:
  - Removido `tools/generate_wear_rules.py` do repositório.
- **Testes e integridade física**:
  - Removido teste antigo de cobertura cega NO-GO; adicionados testes `LimitKeys_VfeCurrent_IsValidAndHasKnotsUnit` e `ContinuousRules_HaveCoherentLimitKeyAndSensors` (validando correlação estrita entre limites operacionais e sensores).
  - 215 testes xUnit aprovados com 0 falhas; build com 0 erros.
  - Relatório atualizado em `docs/reviews/F2.2-relatorio.md`.

## 2026-11-15 - F2.2: regras de desgaste aprovadas (Claude)
- Usuário aprovou as 23 regras do `wear_rules.json`. Na aprovação pediu a expansão do catálogo de peças (aviônicos/IFR/GPS/FMS/piloto automático, elétrico, superfícies de comando e empenagem, estrutura de fuselagem pressurizada, asa, trem de pouso detalhado), com custo de reparo em % do valor da aeronave e tempo de hangar — tarefa F2.1b. Nenhum código mudou.

## 2026-11-15 - Decisão: Hangar por setor (Claude)
- Plano › Fase 5: o Hangar mostra setores, não a lista de peças; amarelo só o que a inspeção/walkaround descobriu, vermelho só o que está quebrado; o resto fica recolhido. Nenhum código mudou.
## 2026-11-15 - F2.1b: expansão do catálogo de peças e regras (Gemini)
- **Expansão do catálogo de peças (`parts_catalog.json`)**:
  - Catálogo ampliado de 58 para 96 peças, cobrindo detalhadamente superfícies de comando e empenagem (6 peças), trem de pouso estrutural e rodas (5 peças), asa e hipersustentadores/spoilers/degelo (9 peças), fuselagem e anteparas pressurizadas (8 peças), elétrico AC (1 peça) e aviônicos/IFR/telas/FMS/autopilot (9 peças).
  - Adicionados campos `repairCostPctOfAircraft` ([min, max]), `downtimeHours` ([min, max]) e `failureEffect` a 100% das 96 peças do catálogo com validação estrita.
- **Novos grupos e vocabulário de características**:
  - `PartGroup`: adicionados grupos `Empenagem`, `Asa`, `Fuselagem` e `Aviônicos`.
  - `AircraftFeatures`: adicionadas 11 features funcionais (`glassCockpit`, `fms`, `autopilot`, `weatherRadar`, `slats`, `spoilers`, `thsTrim`, `deIceBoots`, `bleedAntiIce`, `antiskid`, `noseWheelSteering`) com validações de coerência mandatória.
- **Aeronaves e limite Va (`Data/aircraft/*.json`)**:
  - Atualizados os 46 arquivos JSON com incremento de `dataVersion` e mapeamento de features funcionais.
  - Adicionada chave `va` (velocidade de manobra) a `LimitKeys` e preenchida a partir de POH nas aeronaves calibradas.
- **Ampliação das regras de desgaste (`wear_rules.json`)**:
  - Adicionadas 12 novas regras operacionais (total de 35 regras): comando brusco acima de Va, ciclos de trim, acúmulo de gelo sem degelo, ciclos de botas pneumáticas, tailstrike, wingtip strike, vento cruzado com carga lateral, curva rápida no táxi, overspeed de speedbrake, ciclos de pressurização de fuselagem, ciclos estruturais de pouso e frenagem de alta energia com antiskid.
- **Sensores e Inventário**:
  - Confirmadas no SDK e registradas 5 novas SimVars globais: `ELEVATOR POSITION`, `AILERON POSITION`, `RUDDER POSITION`, `AUTOPILOT MASTER` e `AVIONICS MASTER SWITCH` (total de 378 sensores, paridade 100% com `sensor-inventory.md`).
- **Testes e verificação**:
  - Build 0 erros; 219 testes xUnit e 40 testes python aprovados com 100% de sucesso.
  - Relatório completo submetido para aprovação do usuário em `docs/reviews/F2.1b-relatorio.md`.

## 2026-11-15 - F2.1b Rodada 2: incorporação dos adendos do usuário (Gemini)
- **Ampliação e unificação do catálogo de peças (`parts_catalog.json`)**:
  - Incorporados os três adendos do usuário (Elétrico/Aviônicos, Hidráulico/Pneumático/Combustível e Motopropulsor), expandindo o catálogo para 134 peças calibradas (meta 130–180 atendida).
  - Segregação de subsistemas essenciais: divisão de AHRS/IRS e ADC, acréscimo de standby/ISIS, TCAS/EGPWS, gravadores FDR/CVR/ELT, painel de áudio, bateria standby, geradores IDG e RAT, TRU, iluminação externa, disjuntores/relés, bombas hidráulicas elétricas ACMP, acumuladores de pressão, válvulas seletoras e PTU, válvulas de sangria pneumática, pré-resfriadores, detecção de vazamento pneumático, packs ACM, alívio de pressão, válvulas crossfeed e corte de fogo, trocador FOHE, FQIS, coxins/berço, nacele/carenagem, wastegate, filtro de ar, partida pneumática ATS, FADEC/EEC, bicos injetores e NGV de turbina, excitadores de ignição, mecanismo de passo/embandeiramento, degelo elétrico de hélice, módulo APU e portas de carga.
  - Todas as 134 peças configuradas com faixas de custo percentual de reparo (`repairCostPctOfAircraft`), horas de hangar (`downtimeHours`) e efeitos de falha funcionais.
- **Novas características funcionais e grupos (`AircraftFeatures` e `PartGroup`)**:
  - Adicionado novo grupo `Pneumático` em `PartGroup`.
  - Adicionadas 5 novas características em `AircraftFeatures`: `ptu`, `rat`, `propDeIce`, `fadec`, `bleedAir` com validações estritas de coerência aeronáutica.
  - Atualizados os 46 perfis de aeronave em `Data/aircraft/*.json` com bump de `dataVersion` para 3 e mapeamento preciso das novas tecnologias por modelo.
- **Ampliação do catálogo de regras de desgaste (`wear_rules.json`)**:
  - Adicionadas 10 novas regras operacionais (totalizando 45 regras): partida com aviônicos ligados, descarga profunda da bateria, sobrecarga contínua de amperagem, piloto automático em turbulência severa, operação de bomba de combustível a seco, operação com pressão hidráulica baixa/deprimida, alta potência com óleo frio, ciclos de degelo de hélice, transientes rápidos de torque na caixa de redução e descompressão rápida de cabine.
  - 100% das novas regras utilizam exclusivamente sensores existentes e limites físicos coerentes.
- **Testes e integridade**:
  - `PartCatalogTests` atualizado para validar a meta de 130–180 peças e coerência das novas features.
  - `WearRuleCatalogTests` atualizado para validar catálogo de regras >= 40 regras.
  - 219 testes xUnit em `FlightCareer.Tests` e 40 testes python aprovados com 100% de sucesso.

## 2026-11-15 - F2.1b Rodada 3: incorporação do Adendo 4 (Gemini)
- **Ampliação do catálogo com Adendo 4 e envelhecimento calendárico (`parts_catalog.json`)**:
  - Incorporado o Adendo 4 do usuário cobrindo Cabine (assentos, galley, lavatório, entretenimento), Oxigênio (cilindro tripulação, sistema de passageiros, máscaras/reguladores), Proteção contra Fogo (detecção de motor, garrafas extintoras de motor e APU, extintores portáteis de cabine), Emergência e Específicos (escorregadeiras de evacuação, coletes/botes, limpadores de para-brisa, detector de gelo, esquis e flutuadores).
  - Catálogo ampliado para 151 peças unificadas (meta de 130–180 peças rigorosamente mantida).
  - Adicionado campo `calendarLifeMonths` em `PartDefinition` e calibrado em todas as peças que sofrem envelhecimento estacionário (baterias, pneus, mangueiras, vedações, garrafas de extintor, cilindros de O2, coletes/escorregadeiras, ELT e lubrificantes).
- **Novos grupos e vocabulário de características (`PartGroup` e `AircraftFeatures`)**:
  - Adicionados 4 novos grupos em `PartGroup`: `Cabine`, `Oxigênio`, `Proteção contra Fogo` e `Emergência` (totalizando 23 grupos).
  - Adicionadas 9 novas features em `AircraftFeatures`: `passengerCabin`, `galley`, `lavatory`, `oxygen`, `fireProtection`, `floats`, `skis`, `wipers`, `iceDetector` com validações de coerência aeronáutica (`galley`/`lavatory` requerem `passengerCabin`; `floats`/`skis` mutuamente exclusivos; `iceDetector` restrito a turboprop/jet).
  - Atualizados os 46 arquivos de aeronaves em `Data/aircraft/*.json` com bump de `dataVersion` para 4 e mapeamento preciso das características (ex.: DHC-6 Twin Otter com `skis`, `passengerCabin`, `oxygen`, `wipers`; 747-8i com conjunto completo; C152 mantido estritamente enxuto).
- **Métricas de evento fechadas e documentadas (`WearRuleMetrics`)**:
  - Criada classe canônica `WearRuleMetrics` com o vocabulário fechado das 26 métricas de evento e estresse operacional suportadas pelo motor de desgaste, com fórmulas de derivação a partir da telemetria e sensores do simulador documentadas para a F2.3.
  - `WearRuleCatalog` passa a validar que toda regra de evento referencia uma métrica de `WearRuleMetrics.All`.
- **Testes e verificação**:
  - `PartCatalogTests` e `WearRuleCatalogTests` atualizados com novos testes cobrindo todos os 23 grupos, peças com vida calendárica, coerência das novas features e métricas fechadas.
  - Build com 0 erros; 225 testes xUnit e 40 testes python aprovados com 100% de sucesso.

## 2026-11-15 - F4.2d Crash no solo e flaps sem briga com pane nativa (Gemini)
- **Bug 1: Crash do simulador no solo vira acidente com perda total direta**:
  - Criada a política pura `CrashPolicy` (`src/FlightCareer.Domain/Components/CrashPolicy.cs`) com o método `Decide(bool isMonitoring, bool isFlightActive, bool alreadyHandled)` retornando `TotalLoss`, `AlertOnly` ou `Ignore`.
  - Em `FlightMonitor.cs`, unificado o tratamento de acidentes do simulador no ponto de entrada `HandleSimulatorCrash(string cause)`.
  - Conectados tanto `_persistentFailures.OnNativeCrash` (`CRASH FLAG`) quanto `_simConnect.OnCrashEvent` (evento `Crashed`, inscrito em `StartMonitoring` e desinscrito em `StopMonitoring`).
  - Em `ComponentFlightSession.cs`, expostos `CrashApplied` e `MarkCrashApplied()` para evitar alertas duplicados ou reentrância entre `ComponentFlightSession.ProcessTick`, `TouchdownDetector` e `HandleSimulatorCrash`.
- **Bug 2: Flaps sem briga com pane nativa do simulador**:
  - Criada a política pura `FlapEnforcementPolicy` (`src/FlightCareer.Domain/Components/FlapEnforcementPolicy.cs`) com `ShouldWrite(double currentIndex, double lockedIndex, bool nativeFlapFailed, DateTime? mismatchSince, DateTime? lastWrite, DateTime now)`.
  - No `PersistentFailureService.cs`, o estado nativo dos flaps (`_nativeFlapFailed = nw-is-failed-5..8 >= 0,5`) passa a ser acompanhado em todos os modos (voo e vigia) através de `_sensorFrames.OnFrame`.
  - No `Enforce()`, suspensa a reescrita de `FLAPS HANDLE INDEX` enquanto a pane nativa estiver ativa (`_nativeFlapFailed`), com confirmação de mismatch de 0,5 s e throttle de escrita de 1,0 s (`FlapCorrectionThrottle`), eliminando o loop de som do motor de flaps do MSFS.
- **Testes unitários**:
  - Criados `CrashPolicyTests.cs` e `FlapEnforcementPolicyTests.cs` em `src/FlightCareer.Tests/Components/`, totalizando 241 testes passando com 100% de sucesso.

## 2026-11-15 - F2.1c Limpeza das sobreposições do catálogo de peças (Gemini)
- **Eliminação de sobreposições no catálogo de peças (`parts_catalog.json`)**:
  - **Wastegate**: Removido `engine.wastegate_controller`; `engine.turbocharger` ("Turbocompressor, wastegate e controlador de pressão") passa a unificar todo o conjunto de sobrealimentação e controle; sensores atualizados para incluir `eng-wastegate-position`.
  - **Vedações**: Removido `pressurization.door_seals`; `fuselage.doors_seals` ("Portas, escotilhas e vedações de cabine") unifica todas as vedações com `appliesTo: {}` cobrindo todas as aeronaves (incluindo pressurizadas); sensores atualizados com `af-pressurization-cabin-altitude` e `calendarLifeMonths: 60`.
  - **Linhas hidráulicas**: Unificados `hydraulic.reservoir_lines` e `hydraulic.lines_fittings` em `hydraulic.lines_fittings` ("Reservatório, tubulações, mangueiras e conexões hidráulicas"), adotando a menor vida útil (3.500 h), faixa de custo abrangente [0.15, 0.60], vida calendárica de 60 meses e sensores combinados (`hyd-reservoir-percent`, `hyd-system-integrity`, `nw-level-21`, `nw-is-failed-21`).
  - **Extinção do grupo Célula**: Migrados `airframe.wing_structure` e `airframe.flaps` para o grupo `Asa`, e `airframe.flight_controls` para o grupo `Empenagem`. IDs mantidos para compatibilidade com o banco.
- **Redirecionamento em regras de desgaste (`wear_rules.json`)**:
  - Atualizadas as regras de pressurização (`pressurization.door_seals` → `fuselage.doors_seals`) e turbulência (`hydraulic.reservoir_lines` → `hydraulic.lines_fittings`), sem nenhuma perda de regra ou peça órfã.
- **Domínio e Testes (`PartGroup` e `PartCatalogTests`)**:
  - Removido `Celula` de `PartGroup.cs`, restando 22 grupos ativos no vocabulário fechado.
  - `PartCatalogTests` atualizado para validar catálogo consolidado com 148 peças exatas e todos os 22 grupos cobertos.
  - Build com 0 erros; 241 testes xUnit passando com 100% de sucesso.





## 2026-11-15 - Flap: barulho mantido como indicativo (Claude)
- Decisão do usuário: com pane de flap aberta, o addon continua corrigindo a alavanca mesmo quando o próprio sim já segura o flap quebrado; o som do motor do flap fica como aviso ao piloto. Mantidos o limite de 1 correção por segundo e a espera de 0,5 s fora da posição (`FlapEnforcementPolicy`). O estado nativo dos flaps continua sendo acompanhado pelo `PersistentFailureService` (sem uso por enquanto).

## 2026-11-15 - Testes no MSFS: F4.2c e crash pelo addon (Claude)
- Voo com missão (banco novo): pane do motor pelo wear and tear registrada como `ENGINE_FAILURE` (F4.2c OK no MSFS); crash detectado pelo addon e perda total aplicada uma vez. No MSFS 2024 o `CRASH FLAG` não foi visto em nenhum crash e o sim reposiciona o avião no ar. Detalhes em `calibration-C152.md` (voo 4). Seed em banco novo conferido (46 perfis com características, 68 peças no C152). Nenhum código mudou.

## 2026-11-15 - Spike falhas sem mexer no comando (Gemini)
- Levantamento completo no SDK oficial do MSFS 2024 de como aplicar cada pane sem mover chaves ou alavancas no painel.
- Alternador: identificado `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` (bus: 1, alt: 1), que desliga a geração mantendo a chave física ON e descarregando a bateria (exige `_EX1` no SimConnect).
- Flaps e Magnetos: detalhadas as limitações técnicas da engine do sim (conflito com o solver aerodinâmico e amarrações de Model Behaviors do modelo 3D).
- Criados documento técnico `docs/plans/spike-falhas-sem-comando.md` com matriz comparativa e roteiro de teste em voo para o C152, e relatório `docs/reviews/SPIKE-FALHAS-relatorio.md`. Nenhum arquivo de código foi alterado.


## 2026-11-15 - Botões de teste de falhas no HUD de depuração (Claude)
- HUD Overlay › "TESTE DE FALHAS (DEPURAÇÃO)": envia qualquer evento do SimConnect com até 5 parâmetros (`SimConnectService.SendEventEx`, `TransmitClientEvent_EX1`), com atalhos para `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE`, `ELECTRICAL_ALTERNATOR_BREAKER_TOGGLE` e `ELECTRICAL_CIRCUIT_TOGGLE`, e liga/desliga a escrita contínua da superfície dos flaps em 50% (`WriteFlapSurfacePercent`). Cada envio grava um marcador `teste-...` no REC. Para validar o spike `docs/plans/spike-falhas-sem-comando.md`. Não testado no MSFS.

## 2026-11-15 - F4.2e Panes de alternador e flap sem mexer no comando do piloto (Gemini)
- **Alternador livre no cockpit (chave ON, bateria descarregando)**:
  - Criada classe pura `ToggleStateTracker` (`src/FlightCareer.Domain/Components/ToggleStateTracker.cs`) para gerenciar o estado desejado vs. aplicado com reset em recargas da aeronave/voo no simulador.
  - Em `SimConnectService.cs`, implementado `SetAlternatorConnected(int alternatorIndex, bool connected)` enviando `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE (1, alternatorIndex)` via `SendEventEx` apenas quando o estado muda.
  - Em `SimFailureActuator.cs` e `PersistentFailureService.cs`, removido o envio de `ALTERNATOR_OFF` e o mascaramento `AlternatorPilotEvents`, acionando `SetAlternatorConnected(1, false)` nas panes de alternador e `SetAlternatorConnected(1, true)` ao reparar.
  - Atualizado `DefectCatalog.Alternator` para `DefectEnforcement.NativeToggle` com evento de alternador.
- **Flaps com alavanca física livre e superfície travada**:
  - Acrescentadas as SimVars `TRAILING EDGE FLAPS LEFT PERCENT` (`Percent Over 100`) e `FLAPS NUM HANDLE POSITIONS` (`Number`) no fim da struct `SimFailureState` e da definição `AddFailureDef`.
  - Criada classe pura `FlapSurfaceLock` (`src/FlightCareer.Domain/Components/FlapSurfaceLock.cs`) com método `TargetFraction(index, positions)` para calcular a fração da superfície a partir do índice da alavanca e total de posições.
  - Em `SimFailureActuator.cs`, pane de flap em voo (`FlapJam`) trava na fração atual da superfície no momento da pane; removido o envio de `FLAPS_SET`.
  - Em `PersistentFailureService.cs`, enforcement a cada leitura de `SimFailureState` (6 frames) escreve diretamente a superfície com `WriteFlapSurfacePercent` calculando a fração pelo `FlapSurfaceLock`; removida a escrita de `FLAPS HANDLE INDEX` e o mascaramento `FlapPilotEvents`.
  - Removido `FlapEnforcementPolicy` tornado obsoleto pela escrita contínua da superfície.
- **Testes unitários**:
  - Criados `ToggleStateTrackerTests.cs` e `FlapSurfaceLockTests.cs`.
  - `TelemetryDefinitionTests.cs` atualizado com teste de contagem de datums em `SimFailureState`.
  - Build com 0 erros; 247 testes xUnit aprovados com 100% de sucesso.
## 2026-11-15 - F2.3 Motor de desgaste por peça e balanceamento (Gemini)
- **Motor de Desgaste por Peça (`PartWearEngine`)**:
  - Implementado motor de desgaste puro em `FlightCareer.Domain/Parts/PartWearEngine.cs`.
  - Suporta desgaste nominal (70/LifeHours) ao longo da vida útil, estresse contínuo por regras operacionais com multiplicador máximo ativo e soma de perdas horárias (`lossPerHourPct`), e resolução dinâmica de limites (`vfe:current` por dente de flap em uso via `af-flaps-handle-index`).
  - Reserva nativa ativada exclusivamente quando o limite for `ClassDefault` (sob POH/SimVar/Cfg/NativeOnset os sensores do addon são soberanos); detecção anti-trapaça quando taxa nativa > 0 com nível nativo travado em 1.00; tolerância a sensores ausentes ou NaN.
  - Métodos `ApplyEvent` para métricas discretas e `ApplyNativeFailed` (0% de saúde e `IsBroken`, idempotente por voo).
- **Ponte Peças → Componentes (`PartComponentBridge`)**:
  - As 9 barras de componente passam a ser derivadas do agregado das peças do seu grupo por mínimo ponderado (`0.7 * min + 0.3 * avg`), com mapeamento configurado em `part_component_mapping.json` e propagação para as colunas legadas de `PilotAircraft`.
  - Salvaguarda `HasParts(aircraft)` implementada para garantir que o sistema antigo não cobre desgaste duas vezes quando a aeronave possui peças.
- **Precificação e Custos (`PartPricing`)**:
  - Preço da peça física com piso em `PartPrice` e escala percentual `repairCostPctOfAircraft / 100.0` para aeronaves de maior porte, mão de obra e cálculo de downtime.
- **Migração e Compra no Mercado (`PartMigrationService`)**:
  - Migração idempotente (`PartsMigratedAtUtc`) herda saúde das barras com variação determinística por aeronave, e remove peças virgens obsoletas ou não aplicáveis ao modelo (ex.: `tires.nose` em bequilha).
  - Integrado ao `MarketSimulationService` para provisionamento de peças na compra (novas a 100%, usadas coerentes com o anúncio).
- **Correção da Falha Pós-Manutenção em Avião Novo**:
  - Corrigido bug no `ExternalEventEngine` onde aeronaves novas abriam a janela de falha pós-manutenção no primeiro voo. Agora requer `c.LastShopServiceAtUtc.HasValue` registrado em oficina/Hangar.
- **Simulador e Alvos Econômicos (`PartEconomySimulator` e `economy_targets.json`)**:
  - Simulador de 1.000 h de voo típico calibrado e verificado para as 9 classes (A a I), com todos os custos/hora dentro das faixas-alvo.
- **Testes**:
  - Criados 17 novos testes em `PartWearEngineTests`, `PartBridgeAndMigrationTests`, `PostMaintenanceCorrectionTests` e `PartEconomyBalanceTests`. Total da suíte: 258 testes passando com 100% de sucesso.

## 2026-11-15 - F2.3 Ajustes da revisão rodada 2 (Gemini)
- **Ativação da Migração no Startup (`App.xaml.cs` e `PartMigrationService`)**:
  - Integrada chamada `PartMigrationService.MigrateAllPendingAsync(dbContext)` na inicialização de `App.xaml.cs` logo após `EnsurePartsAsync`, com tratamento de exceção registrado.
  - Implementado teste de integração completo de inicialização e migração com idempotência e remoção de obsoletas virgens em `PartBridgeAndMigrationTests`.
- **Eliminação de `catch { }` silencioso**:
  - Exceções ao deserializar `FeaturesJson` em `MarketSimulationService` e `PartMigrationService` registradas com `Debug.WriteLine`.
- **Calibração de Custos e Recalibração de `economy_targets.json`**:
  - Adicionado teto de preço por porte da aeronave em `PartPricing.GetClassPartPriceCap` (US$ 30k pistão, US$ 80k turboélice/jato executivo, US$ 55k narrowbody, US$ 110k widebody/cargueiro pesado).
  - Recalibradas todas as faixas de classes (A a J) em `economy_targets.json` com base nas ordens de grandeza reais (A320neo a US$ 1.941/h, A330 a US$ 3.807/h, 747-8i a US$ 5.084/h, CJ4 a US$ 1.558/h, Baron a US$ 436/h, C152 a US$ 88/h).
  - Adicionado teste automatizado cobrindo todas as 46 aeronaves capturadas no catálogo (`EconomySimulation_All46CapturedAircraft_FallWithinTargetBands`) com 100% de conformidade. Total da suíte: 260 testes passando.

## 2026-11-15 - Validação dos sensores novos no C172 G1000 (Claude)
- Profundor, aileron, piloto automático e trim do profundor validados no MSFS. Master de aviônicos (fica em 1), tensão da bateria (constante) e alternador (0) não funcionam como lidos hoje; leme a confirmar. Detalhes em `docs/plans/sensor-validation/sensores-novos-C172.md`. Nenhum código mudou.

## 2026-11-15 - F2.4 peças no voo (Gemini)
- **Correção do Duplo Incremento em `PartWearEngine`**: Corrigido bug onde `CyclesSinceReplacement` incrementava tanto em `touchdownFpm` quanto em `landingCycles`. Agora incrementa apenas em `landingCycles`.
- **Derivador de Eventos Operacionais (`PartEventDeriver`)**: Criada classe pura de domínio que deriva ciclos de partida de pistão (`engineStartCycle`) ou turbina (`turbineStartCycle`), tempo contínuo de acionamento do starter (`starterSeconds`), ciclos de trem retrátil (`gearCycles`) e ciclo completo de voo (`flightCycles`, emitido no primeiro pouso após estar no ar, sem duplicar em bounce).
- **Reconciliação no Início do Voo (`PartComponentBridge.ReconcilePartsFromComponents`)**: Reconciliação bidirecional antes do voo. Reparos ou revisões no Hangar (barra > agregado + 0,5) elevam a saúde das peças do grupo e limpam `IsBroken` se barra ≥ 99,5; danos fora de voo (barra < agregado - 0,5) aplicam clamp nas peças do grupo. As barras em seguida refletem exatamente as peças.
- **Rastreador de Peças em Voo (`PartFlightTracker`)**: Lógica pura de Domain que processa `SensorFrame` e tempo simulado, pausa desgaste se o frame for mais antigo que 5 s, aplica eventos derivados e de toque, direciona panes nativas para as peças afetadas ou menor saúde do grupo (idempotente) e sincroniza as 9 barras e colunas legadas.
- **Persistência em Lote (`PartStateWriter`)**: Cópia otimizada por ID para `AppDbContext` em `Infrastructure/Services/`, preservando `Health`, `HoursSinceReplacement`, `CyclesSinceReplacement`, `IsBroken` e `BrokenAtUtc`.
- **Sessão de Voo com Peças (`PartFlightSession`)**: Orquestrador na UI que gerencia thread-safety do `SensorFrame`, dispara eventos para o blackbox (`EXCEDENCIA_PECA`, `INSPECAO_PECA`, `PECAS_DIAG`) e delega para o `PartFlightTracker`.
- **Integração no `ComponentFlightSession` e `FlightMonitor`**:
  - Em aeronaves com peças, `WearEngine.ApplyTick` e `ApplyLanding` não executam; danos de modo de falha, eventos externos e panes nativas são roteados diretamente para as peças do grupo via `DamageGroup`/`ClampGroup`.
  - Salvamento das peças nos 4 pontos do ciclo de voo (atualização periódica de 60 s, cancelamento, perda total e conclusão do voo).
  - Aeronaves sem peças continuam operando 100% pelo sistema legado de componentes sem qualquer alteração comportamental.
- **Testes Unitários e de Integração**:
  - Criado `PartFlightIntegrationTests.cs` cobrindo regressão de ciclos, derivador de eventos, reconciliação bidirecional, panes nativas, danos em grupo, integração com frames sintéticos e persistência em SQLite em memória.
  - Adicionado teste para `AircraftLimitResolver.FindById`.
  - Total da suíte: 284 testes xUnit passando com 100% de sucesso.

## 2026-11-15 - Spike sensores elétricos MSFS 2024 (Gemini)
- **Spike de pesquisa elétrica no MSFS 2024**:
  - Documento `docs/plans/sensor-validation/spike-eletrico-2024.md` detalhando causas-raiz dos valores no C172 G1000, matriz de candidatos com URLs da documentação oficial do SDK 2024, recomendações para as 3 regras mudas (`avionics_start_with_avionics_master_on`, `battery_deep_discharge_damage`, `electrical_system_overload_amps`) e análise de riscos de compatibilidade.
- **Novos candidatos no registro de sensores (`sensor_registry.json` e `sensor-inventory.md`)**:
  - Adicionados 16 novos sensores candidatos de medição elétrica em nível de barramento, circuito e índices específicos (`elec-avionics-master-switch-1/2`, `elec-avionics-bus-voltage`, `elec-avionics-bus-amps`, `elec-circuit-switch-on-1/2`, `elec-circuit-on-1/2`, `elec-battery-voltage-1`, `elec-battery-load-1`, `elec-battery-bus-voltage`, `elec-hot-battery-bus-voltage`, `elec-main-bus-voltage-1/2`, `elec-main-bus-amps-1`, `elec-alternator-connection-on-1`).
  - Mantida paridade estrita testada entre o catálogo JSON e o inventário Markdown; contagens atualizadas em `SensorLayoutTests.cs`.
- **Roteiro de teste elétrico no C172 G1000 (`ROTEIRO.md`)**:
  - Adicionada seção 7 com checklist objetivo de teste em solo (motor parado, aviônicos ON/OFF, partida, alternador OFF/ON e deflexão de leme) e tabela com os comportamentos esperados por coluna.

## 2026-11-15 - Spike sensores elétricos MSFS 2024 ajustes da revisão r2 (Gemini)
- **Correções do Spike Elétrico baseadas no sistema elétrico modular v2 (`spike-eletrico-2024.md`)**:
  - URLs atualizadas exclusivamente para a documentação retail oficial do SDK MSFS 2024 (todas com HTTP 200).
  - Matriz de candidatos reestruturada com classificação v2+ / v1.00 / FSX; SimVars legadas e obsoletas do FSX rebaixadas com fundamentação técnica do SDK.
  - Análise documentada sobre indexação: uso de nomes com aspas (`:'Bus_1'_n`) é não documentado no SimConnect (`AddToDataDefinition`) e arrisca desalinhamento de memória (`NAME_UNRECOGNIZED`), adotando-se estritamente índices numéricos globais.
  - Metodologia de identificação dos barramentos documentada pelo cruzamento temporal com os marcadores do teste de solo.
  - Diagnóstico revisado distinguindo valores observados no CSV de hipóteses técnicas documentadas.
- **Reconfiguração dos 16 candidatos no registro e inventário (`sensor_registry.json` e `sensor-inventory.md`)**:
  - Adicionadas variáveis modernas v2+: `ELECTRICAL BUS VOLTAGE:1..5`, `ELECTRICAL BUS AMPS:1..5`, `CIRCUIT ON:1..3`, `ELECTRICAL BATTERY VOLTAGE:1`, `ELECTRICAL BATTERY LOAD:1` e mantido `ELECTRICAL MAIN BUS VOLTAGE:1` como controle legado.
  - Eliminadas variáveis obsoletas (`ELECTRICAL HOT BATTERY BUS VOLTAGE`, `ALTERNATOR CONNECTION ON:1`).
  - Paridade estrita mantida e validada por `SensorRegistryTests` e `SensorLayoutTests`.
- **Roteiro de teste atualizado (`ROTEIRO.md`)**:
  - Tabela da seção 7 atualizada com as novas colunas e explicação de identificação de barramentos.

## 2026-11-15 - Teste elétrico de solo do C172 G1000 analisado (Claude)
- `docs/plans/sensor-validation/teste-eletrico-C172.md`: linha do tempo e resultado das 16 SimVars do spike. Aviônicos detectados por `CIRCUIT ON:3` e `ELECTRICAL BUS AMPS:4`; alternador e descarga pela `ELECTRICAL BUS VOLTAGE:1` (igual nos 5 barramentos); `ELECTRICAL BATTERY VOLTAGE/LOAD:1`, `MAIN BUS`, `GENALT` e `AVIONICS MASTER SWITCH` não atualizam no C172 2024. `RUDDER POSITION` validado.
- Proposta para as três regras elétricas mudas (índice de circuito por aeronave no catálogo; descarga por tensão do barramento; sobrecarga pela carga total × capacidade do alternador). Nada de código mudou.

## 2026-11-15 - F2.4b-CAL regras elétricas e modo calibração (Gemini)
- **Ativação das 3 Regras Elétricas no Domínio (`PartEventDeriver`)**:
  - `avionics_start_with_avionics_master_on`: na borda de partida do motor (`eng-starter:n` 0->1), detecta aviônicos ligados via `elec-circuit-on-<elecAvionicsCircuitIndex>` e emite o evento `avionicsMasterStartSpikes` com contador cumulativo no voo.
  - `battery_deep_discharge_damage`: com master da bateria ligado e tensão no barramento `elecMainBusIndex` entre 1 V e `elecDeepDischargeVolts`, acumula minutos simulados e emite eventos ao cruzar cada faixa cumulativa (10, 25, 45 min); zera apenas após interrupção contínua ≥ 10 s simulados.
  - `electrical_system_overload_amps`: com alternador carregando (tensão ≥ 0,95 × nominal), sem starter engatado e carga total superior à capacidade do alternador (`elec-total-load-amps > alternatorRatedAmps`), acumula minutos simulados e emite eventos ao cruzar faixas cumulativas (5, 15, 30 min).
  - Ausência de limites elétricos no perfil da aeronave desliga a respectiva regra silenciosamente, sem interromper o monitoramento.
- **Catálogo de Limites por Aeronave (`LimitKeys`, Schema e C172SP G1000)**:
  - Adicionadas chaves de limites em `LimitKeys`: `elecSystemVolts` (volts), `elecAvionicsCircuitIndex` (index), `elecAvionicsBusAmpsIndex` (index), `elecMainBusIndex` (index), `alternatorRatedAmps` (amperes), `elecDeepDischargeVolts` (volts). Unidades `volts`, `index`, `amperes` registradas no schema JSON.
  - `c172sp-g1000.json` calibrado com `elecSystemVolts: 28.0`, `elecAvionicsCircuitIndex: 3.0`, `elecAvionicsBusAmpsIndex: 4.0`, `elecMainBusIndex: 1.0` (fonte `SimVar`), e `alternatorRatedAmps: 60.0` (fonte `POH`, Sec. 7 p. 7-47).
- **Varredura e Modo de Calibração de Solo no HUD Overlay**:
  - Catálogo de sensores ampliado com varredura estendida: `CIRCUIT ON:4..20`, `ELECTRICAL BUS VOLTAGE:6..10`, `ELECTRICAL BUS AMPS:6..10`, com suporte a `calibrationOnly: true` (lidos e alocados nos blocos do SimConnect exclusivamente durante calibração).
  - Roteiro guiado em dados (`calibration_script.json`) e classe de domínio (`CalibrationScript`) com 13 passos de solo sequenciais e filtragem por features da aeronave (ex.: exclusão automática de teste de flap para aeronaves sem flap).
  - HUD Overlay integrado (`DebugOverlayViewModel` e XAML) com card de calibração, botão "Calibrar", contador de tempo mínimo, botões "Pronto", "Pular" e "Cancelar", exibindo comando de análise ao concluir.
- **Analisador de Calibração (`tools/aircraft_limits/calibrate.py`)**:
  - Script Python (somente biblioteca padrão) que processa os marcadores `cal:<passo>` do log de calibração, identifica índices e tensões nominais por transições de estado e análise estatística, exibe relatório de evidências no terminal e grava os candidatos com `--write` no catálogo sem sobrescrever dados de POH.
  - Testado e validado tanto com suite sintética unitária (45 testes Python) quanto com log real de voo do C172SP.
- **Testes**:
  - Criados testes unitários C# em `ElectricalRuleDeriverTests.cs`, `CalibrationScriptTests.cs` e `ElectricalLimitsTests.cs`.
  - Suíte completa com 301 testes xUnit passando com 100% de sucesso.
- **Não testado no MSFS**:
  - Execução interativa completa da UI do card de calibração no simulador com SimConnect ao vivo.

## 2026-11-15 - Primeira calibração pelo modo Calibrar: C152 (Claude)
- Log `20261115-1733-Cessna_C152.csv` analisado pelo `calibrate.py`: `elecMainBusIndex` 1 e `elecSystemVolts` 28 (queda de 3,7 V com o ALT desligado), `rpmMaxRated` 2.550 (`source: SimVar`); `cessna-c152.json` `dataVersion` 5.
- Passos de aviônicos pulados (sem chave de aviônicos usada no C152): `elecAvionicsCircuitIndex` ausente, a regra de partida com aviônicos ligados fica desligada no C152. Passos `rudder-full` e `end` não marcados. `alternatorRatedAmps` do C152 ainda depende do POH (regra de sobrecarga desligada até lá).

## 2026-11-15 - Alternador do C152 pelo POH (Gemini)
- **Capacidade Nominal do Alternador do C152 (`poh_values.csv` e `cessna-c152.json`)**:
  - Evidência literal localizada no POH oficial do Cessna 152 (1979), Seção 7 (Airplane & Systems Descriptions › Electrical System), página 7-22: *"The airplane is equipped with a 28-volt, direct-current electrical system ... and a belt-driven 60-amp alternator to maintain the battery's state of charge."*.
  - Adicionada entrada em `tools/aircraft_limits/poh_values.csv` (`Cessna C152,alternatorRatedAmps,60,amperes,...`).
  - Executado `tools/aircraft_limits/merge_limits.py`, atualizando `cessna-c152.json` com `alternatorRatedAmps: 60.0`, `source: POH`, unidade `amperes` e incrementando `dataVersion` para 6.
  - Habilita a regra de sobrecarga elétrica (`electrical_system_overload_amps`) no Cessna 152.

## 2026-11-15 - Regras de desgaste por motor e diagnóstico único (Gemini)
- **Resolução de Sensores por Motor no `PartWearEngine`**:
  - `PartWearEngine.ApplyTick` agora resolve dinamicamente se os sensores requeridos de cada regra contínua são globais (chave exata presente no frame) ou por motor (chave presente com índice `:1`).
  - Número de motores detectado dinamicamente a partir do maior índice numérico `:<n>` presente nas chaves do frame.
  - Regras com sensores por motor são avaliadas separadamente para cada motor `1..engineCount`, com as leituras `chave:n`.
  - Estresse resultante aplicado diretamente às peças daquele motor (`EngineIndex == n`).
  - Para peças compartilhadas (`EngineIndex == 0`), aplica-se o estresse do pior motor entre os avaliados, uma única vez sem somar motores.
  - Chaves de rastreio de excedência, picos, durações e inspeções segregadas por motor (`<ruleId>#<n>`), incluindo o motor no `TargetId` e em `Notes` para visualização clara no blackbox.
  - Mensagens de sensor ausente passam a citar a chave tentada (ex.: `'eng-rpm' (nem 'eng-rpm:1')`).
- **Suporte Multimotor e Prevenção de Exceções (`ApplyTick` e `ApplyEvent`)**:
  - Dicionários e agrupamentos internos refatorados de `PartId` para `(PartId, EngineIndex)`.
  - Em `ApplyEvent`, agrupamento por `PartId` via `GroupBy`, aplicando dano e incrementos a todas as instâncias correspondentes sem lançar `ArgumentException` em aeronaves multimotor com peças repetidas.
- **Deduplicação de Diagnósticos no `PartFlightTracker`**:
  - `PartFlightTracker` mantém um conjunto de mensagens diagnósticas já emitidas durante o voo, drenando e repassando apenas mensagens inéditas.
  - Conjunto limpo em `Begin()` e `Reset()`, eliminando a repetição contínua a cada tick no blackbox (`PECAS_DIAG`).
- **Testes**:
  - Adicionados 7 novos testes em `PartWearEngineTests.cs`:
    - `ApplyTick_RpmPorMotor_EngRpm1AcimaDoRedline_DesgastaPecasDoMotor`
    - `ApplyTick_ChaveGlobal_ContinuaFuncionando`
    - `ApplyTick_Bimotor_SoMotor2EmVermelho_DesgastaSoPecasDoMotor2`
    - `ApplyTick_Bimotor_ExcedenciaRastreadaPorMotor`
    - `ApplyEvent_BimotorPecasRepetidas_NaoLancaExcecao_EAplicaEmTodas`
    - `Tracker_DiagnosticoDeSensorAusente_SaiUmaVezPorVoo`
    - `WearRules_TodoSensorExisteNoLayoutReal_PorPerfil` (teste de contrato registro × regras cobrindo todas as 46 aeronaves do catálogo).
  - Testes unitários atualizados para refletir o frame real com sufixos `:1`.
  - 309 testes xUnit passando com 100% de sucesso; 45 testes Python passando.
- **Não testado no MSFS**:
  - Execução de voo com aeronave bimotor e monomotor no MSFS ao vivo.

## 2026-11-15 - Replay de voos gravados e CLI (Gemini)
- **Motor de Replay Determinístico (`FlightCareer.Domain/Replay/FlightReplayer.cs`)**:
  - Motor puro de domínio que consome sequências de `SensorFrame` e eventos de toque (`ReplayTouchdown`) sem dependências de I/O ou EF.
  - Orquestra `PartFlightTracker` tick a tick com conversão precisa de tempo simulado via `SimTimeStep.ToSimHours`.
  - Disparo de toques com o solo no instante correto do voo (por tempo decorrido ou UTC), aplicando ciclos de pouso e danos de impacto.
  - Consolida perdas de saúde por peça, horas acumuladas, ciclos, excedências, inspeções, eventos operacionais e diagnósticos únicos (`FlightReplayResult`).
  - Exposição de `LastDerivedEvents` em `PartFlightTracker` para captura de eventos com carimbo de tempo.
- **Leitor de Logs e Blackbox (`FlightCareer.Infrastructure/Replay/SensorLogReader.cs`)**:
  - Leitura transparente de arquivos de telemetria `.csv` e comprimidos `.csv.gz`, com suporte a BOM UTF-8, cultura invariante, valores numéricos e markers com escape RFC 4180.
  - Parser robusto de toques a partir do log do blackbox (`# [EVENTO <seg>s] TOQUE: ...`) e de arquivos JSON (`.touchdowns.json`).
- **Script Gerador de Fixtures (`tools/replay/make_fixture.py`)**:
  - Script em Python stdlib que filtra logs brutos de gravação do REC (`%LocalAppData%\FlightCareer\sensor-logs\`), mantendo apenas as colunas consumidas por regras de desgaste e pelo `PartEventDeriver`.
  - Remove coordenadas e dados geográficos sensíveis de posição.
  - Comprime a saída para `.csv.gz` (< 1 MB) e gera `<nome>.touchdowns.json` a partir do blackbox.
  - Fixtures geradas para o C152 voo SBMT→SNEJ (774 KB), C172 teste elétrico (115 KB) e C152 calibração (98 KB) em `src/FlightCareer.Tests/Fixtures/Flights/`.
  - Testes unitários do script em `tools/tests/test_make_fixture.py`.
- **Ferramenta de Linha de Comando (`src/FlightCareer.Replay`)**:
  - CLI .NET 10 console adicionada a `src/FlightCareer.slnx`.
  - Execução: `dotnet run --project src/FlightCareer.Replay -- <csv|csv.gz> [--aircraft <modelo>] [--blackbox <arquivo>|--touchdowns <json>] [--json]`.
  - Dedução automática de aeronave a partir do arquivo `-context.json` via `AircraftLimitResolver`.
  - Relatório legível em português com resumo de tempo simulado, top 15 peças desgastadas, excedências consolidadas, inspeções requeridas, eventos operacionais derivados e diagnósticos únicos.
  - Opção `--json` para integração estruturada.
- **Testes Automatizados xUnit (`src/FlightCareer.Tests/Replay/`)**:
  - 10 novos testes cobrindo voo real do C152 (RPM redline motor 1, horas simuladas, danos e ciclos do toque a 658 fpm, deduplicação de diagnósticos, estresse vs desgaste natural), teste elétrico do C172 (partida sem spike com aviônicos desligados, ausência de descarga profunda) e calibração do C152 (smoke test).
  - Testes de parser de BOM/CSV/markers e toques de blackbox.
  - Suíte completa de 319 testes xUnit passando em 3 segundos; 48 testes Python passando em 0,25s.
- **Não testado no MSFS**:
  - O replay opera offline sobre logs gravados e foi validado contra os dados reais persistidos no banco de dados. Execução de novos voos no MSFS não realizada nesta tarefa.

## 2026-11-15 - Correções dos bugs achados pelo replay (Gemini)
- **Sobrecarga Elétrica Refinada (`PartEventDeriver.CheckElectricalOverload`)**:
  - Condição de sobrecarga atualizada para exigir alternador ligado (`elec-master-alternator:n > 0.5`), motor em combustão (`eng-combustion:n > 0.5`), ausência de starter engatado, corrente acima da capacidade nominal (`elec-total-load-amps > alternatorRatedAmps`) e queda de tensão no barramento principal na faixa de sag `[0,88, 0,95) × elecSystemVolts`.
  - Tensão estável (≥ 0,95) indica capacidade suficiente sem estresse; abaixo de 0,88 indica alternador fora/em pane (bateria isolada descarregando).
  - Documentação da heurística em `docs/plans/sensor-validation/teste-eletrico-C172.md` e no código.
- **Regras com Exigência de Motor em Funcionamento (`WearRule.RequiresEngineRunning`)**:
  - Novos campos `requiresEngineRunning` (bool) e `engineRunningDelaySeconds` (segundos simulados após a borda de combustão 0→1 por motor) adicionados a `WearRule`, `WearRuleCatalog` e `wear_rules.json`.
  - Aplicado atraso de 30 s a `oil_pressure_low` (conforme POH do C152: pressão mínima vale com motor em marcha após 30 s da partida), eliminando o falso positivo de pressão 0 psi e a inspeção NO-GO indevida no pré-voo.
  - No `PartWearEngine`, rastreamento contínuo da borda de combustão e tempo estabilizado por motor, zerando o rastreio de excedência quando o motor está parado ou no atraso.
- **Choque Térmico como Taxa Móvel de Resfriamento da CHT**:
  - `PartFlightTracker` calcula por motor a taxa de resfriamento da CHT `eng-cht-cooling-rate:n` (°C/min, positivo = esfriando) em média móvel em janela de 60 s simulados, somente com motor em combustão, injetando no frame antes do tick.
  - Limite novo `chtCoolingRateMax` em `LimitKeys.cs`, unidade `celsius_per_minute` no schema `_schema.json`, e valor padrão para classe Piston em `_class_defaults.json` (28 °C/min ≈ 50 °F/min conforme Lycoming SI 1094D / Continental M-0).
  - Regra `piston_shock_cooling` atualizada em `wear_rules.json` para sensor `eng-cht-cooling-rate`, `limitKey: chtCoolingRateMax`, `direction: Above`, `requiresEngineRunning: true`, `engineRunningDelaySeconds: 60.0`, eliminando disparos falsos com motor frio.
  - Exposição de `DerivedSensorKeys` em `PartFlightTracker` mantendo o teste de contrato registro × regras 100% verde.
- **Replay Fiel ao Jogo (`ReplayActivationMode`)**:
  - `FlightReplayer` atualizado com `ReplayActivationMode` (`GameLike` padrão, `FromStart`).
  - No modo `GameLike`, imita o `FlightMonitor` do simulador, ativando o cômputo de horas simuladas e desgaste apenas após 10 segundos reais contínuos de motor em combustão com freio de estacionamento solto (`ParkingBrake < 0.5`).
  - Duração simulada no voo do C152 passou de 1,137 h para 1,105 h (muito mais próximo das 1,086 h gravadas no banco pelo jogo).
  - CLI `src/FlightCareer.Replay` atualizada com a opção `--from-start`.
  - Script `tools/replay/make_fixture.py` ajustado para reter `brake-parking-position` e `elec-master-alternator`, e fixtures regeneradas preservando arquivos de toques existentes.
- **Descrição de Inspeção**:
  - Adicionada descrição em português para `GENERATOR_OVERHEAT_INSPECTION` em `wear_rules.json`.
- **Testes Automatizados**:
  - Testes de replay no voo do C152 provando ausência de sobrecarga elétrica e ausência de óleo baixo no pré-voo, disparo de choque térmico apenas com queda rápida de 40 °C/min, e horas simuladas no modo `GameLike` e `FromStart`.
  - Testes unitários para sobrecarga em barramento estável, sag de tensão, alternador em pane, atraso de motor funcionando e independência do SimRate na taxa de resfriamento.
  - 327 testes xUnit aprovados; 48 testes Python aprovados.
- **Não testado no MSFS**:
  - Execução ao vivo com simulador MSFS conectado (validado offline via replay de voo real gravado).
## 2026-11-15 - Pesquisa de variáveis em projetos abertos (Gemini)
- **Pesquisa de Variáveis e SimVars (SDK MSFS 2024 e Projetos de Referência)**:
  - Investigação completa de 42 variáveis cobrindo motor, elétrico, hidráulica, pneumática, trem/estrutura e walkaround do MSFS 2024.
  - Documentação em `docs/plans/sensor-research/variaveis-referencias.md` com tabela detalhada, links oficiais do SDK 2024, verificação contra o registro do FlightCareer (`sensor_registry.json`) e evidências em 4 projetos abertos (`FsConnect`, `MobiFlight`, `msfs2020-go`, `aircraft` FlyByWire).
  - Identificação e comprovação técnica de 5 nomes errados ou inventados (`PARTIAL PANEL STATIC`, `AIRCRAFT STRESS DAMAGE`, `ELECTRICAL LINE BREAKER PULLED`, sintaxe de índices em barramentos e rádios).
  - Análise da evolução da leitura de LVars: confirmação na documentação oficial de que o SimConnect do MSFS 2024 suporta diretamente `L:VARIABLE_NAME` via `SimConnect_AddToDataDefinition` com tipo `FLOAT64`, eliminando a dependência histórica de módulos WASM intermediários.
  - Análise dos modelos de freios e hidráulica de primeiros princípios do FlyByWire em Rust/WASM e documentação de armadilhas conhecidas (rotação residual em rodas paradas, `GEAR TOTAL PCT EXTENDED` obsoleta retornando 0).
  - Relação de 20 variáveis prioritárias candidatas ao próximo teste de solo, com passo a passo de validação em solo no MSFS 2024 (incluindo `COVER ON:0..2` e `L:IS_AVATAR`).
- **Não testado no MSFS**:
  - Tarefa estritamente de documentação e pesquisa técnica; nenhum código ou regra foi alterado. Teste das variáveis prioritárias será executado no próximo voo/teste de solo.

## 2026-11-15 - Auditoria de variáveis, Key Events e Input Events - Rodada 2 (Gemini)
- **Auditoria Completa do Registro de Sensores e Definições (`docs/plans/sensor-research/auditoria-registro.md`)**:
  - Auditoria exaustiva cobrindo `sensor_registry.json` (421 sensores), `context_registry.json` (25 variáveis) e as definições fixas de `SimConnectService.cs` (`SimTelemetry`, `SimFailureState`, `SimFastFlightState` e escrita).
  - Identificação de nomes inexistentes no SDK (`FLAP SPEED EXCEEDED`, `GEAR SPEED EXCEEDED` que eram notificações do modo Carreira, e `ELECTRICAL BATTERY LEVEL`) com risco de exceção no SimConnect e indicação de substitutas oficiais.
  - Correção de erros críticos de indexação: sensores de ponto de contato (`CONTACT POINT COMPRESSION`, `CONTACT POINT IS SKIDDING`) e combustível (`FUELSYSTEM PUMP ACTIVE`, `FUELSYSTEM LINE FUEL PRESSURE`) que estavam marcados com `perEngine: true` (expandindo por motor em vez de por contact point / bomba / linha de combustível).
  - Identificação de SimVars que exigem índice mas estavam sem índice (`WING FLEX PCT`, `CIRCUIT BREAKER PULLED`, `PNEUMATICS DUCT PRESSURE`, `PNEUMATICS PACK STATUS`).
  - Alinhamento de unidades divergentes (`pneu-pack-status` como `percent over 100`, `elec-battery-estimated-capacity-pct` como `Percent`, `gear-skidding-factor` como `Percent Over 100`, `Rankine` vs `celsius`).
- **Key Events, Input Events e Suporte Nativo a LVars (`docs/plans/sensor-research/eventos-e-input-events.md`)**:
  - Confirmação formal no SDK 2024 de todos os eventos de falha utilizados pelo `SimFailureActuator` e `SetAlternatorConnected`.
  - Mapeamento de novos key events candidatos a panes (`TOGGLE_ALTERNATE_STATIC`, `EXTINGUISH_ENGINE_FIRE`, `ELECTRICAL_CIRCUIT_BREAKER_TOGGLE`, `TOGGLE_ELECT_FUEL_PUMP1..4`).
  - Análise aprofundada da API de Input Events do MSFS 2024 (`SimConnect_EnumerateInputEvents`, `SimConnect_GetInputEvent`, `SimConnect_SetInputEvent`, `SimConnect_SubscribeInputEvent`): como ela resolve a leitura de switches físicos como o Avionics Master do G1000 e disjuntores, além de suas limitações (hashes dinâmicos e nomenclatura não universal).
  - Documentação da sintaxe oficial para variáveis indexadas por nome de componente (`SIMVAR:'ComponentName'_n`) e suporte nativo a múltiplos escopos (`A:`, `E:`, `L:`, `I:`, `O:`, `Z:`).
- **Ferramentas de Conferência ao Vivo e Atualização de Candidatas (`docs/plans/sensor-research/variaveis-referencias.md`)**:
  - Guia técnico de uso do SimConnect Inspector (Dev Mode) e do SimVarWatcher (SDK Sample) para validação ao vivo de SimVars e diagnóstico de exceções `NAME_UNRECOGNIZED` sem mexer no código do FlightCareer.
  - Complementação da tabela com a lista completa dos índices de `COVER ON:0..7` (0 calços, 1 motor, 2 pitot, 3 tomada estática, 4 rotores, 5 trem de pouso, 6 hélices, 7 para-brisa).
  - Refinamento da lista de 20 variáveis candidatas para o teste de solo.
- **Não testado no MSFS**:
  - Tarefa estritamente de documentação e pesquisa técnica baseada nas páginas oficiais do SDK MSFS 2024; nenhum código executável ou arquivo de catálogo de regras foi modificado.

## 2026-11-15 - Descoberta de variáveis novas e correções da auditoria - Rodada 3 (Gemini)
- **Correção da Auditoria com Prova em CSVs Reais (`docs/plans/sensor-research/auditoria-registro.md`)**:
  - Reavaliação das SimVars auditadas contra os logs reais do `SensorRecorder` (`20261115-1747-Cessna_C152.csv`, `20261115-1636-C172SP_G1000_Passengers.csv`) e fixtures: o leitor dinâmico descarta SimVars rejeitadas por `NAME_UNRECOGNIZED`, logo a presença da coluna no CSV comprova aceitação pelo simulador.
  - Reclassificação de `FLAP SPEED EXCEEDED` e `GEAR SPEED EXCEEDED` como aceitas nativamente pelo SimConnect (presentes nos 4 logs de teste).
  - Comprovação de que `ELECTRICAL BATTERY LEVEL` é aceita e ativamente variável nos voos (98.6% a 100% no C152).
  - Confirmação de que `ELECTRICAL TOTAL LOAD AMPS` está documentada oficialmente no SDK 2024 (não obsoleta) e varia amplamente (0 a 175 A).
  - Rebaixamento de divergências de escala com valores coerentes (`ELECTRICAL BATTERY ESTIMATED CAPACITY PCT`, 0.986 a 1.0) para nível informativo.
  - Proposta técnica para leitura de trem de pouso: desacoplar `CONTACT POINT COMPRESSION` e `IS SKIDDING` de `perEngine`, passando a ler explicitamente por ponto de contato (`:0` nariz, `:1` principal esquerdo, `:2` principal direito) para diagnosticar impacto localizado e toque de três pontos.
- **Descoberta de Variáveis Novas por Peça (`docs/plans/sensor-research/descoberta-variaveis.md`)**:
  - Mapeamento de cima para baixo a partir do `parts_catalog.json` (Motor, Óleo, Hélice, Trem, Freios, Superfícies, Célula, Luzes, Pressurização, Combustível, Fogo, Walkaround e Ambiente).
  - Relação de 28 novas variáveis do MSFS 2024 não registradas anteriormente no FlightCareer, com URL oficial do SDK, unidade, índice, modo R/W, suporte no MSFS 2020 e evidências em projetos abertos locais (`FsConnect`, `aircraft` FlyByWire, `MobiFlight`).
  - Destaque para `COVER ON:0..7` (inspeção externa/walkaround com calços, capas de pitot e amarração de hélice), `LEFT/RIGHT/CENTER WHEEL RPM` (spin-up no toque e flat spot por roda travada), `SUCTION PRESSURE` (vácuo físico das bombas para instrumentos giroscópicos), `ENG OIL QUANTITY:1` (nível do cárter) e `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT` (assimetria física de flaps).
  - Seleção "Top 25 para o Próximo Teste" com roteiro cronometrado e integrado de 15 minutos em 17 passos para validação em solo e voo curto.
  - Seção "Variáveis Descartadas ou Esclarecidas" demonstrando que `BRAKE TEMPERATURE` não é SimVar nativa da Asobo (implementada por termodinâmica de primeiros princípios no FlightCareer), além do esclarecimento sobre SimVars inexistentes ou sintaxes inválidas.
- **Relatório de Execução (`docs/reviews/RESEARCH-SIMVARS-r3-relatorio.md`)**:
  - Relatório da rodada 3 conforme o modelo das instruções do Gemini e atualização de `RESEARCH-SIMVARS-relatorio.md`.
- **Não testado no MSFS**:
  - Tarefa documental e de pesquisa técnica; as novas variáveis descobertas serão validadas fisicamente no simulador através do roteiro Top 25 em tarefa subsequente.




## 2026-11-15 - Eventos de partida antes da ativação do voo (Gemini)
- **Observação de Eventos no Pré-Voo (`PartFlightTracker.ObservePreflight` e `PartFlightSession.ObservePreflight`)**:
  - Implementado modo de observação pré-voo que processa exclusivamente `PartEventDeriver.Update` e aplica os eventos derivados resultantes via `PartWearEngine.ApplyEvent` (com filtragem por motor e preservação contínua de estado).
  - Não executa `ApplyTick`, mantendo horas acumuladas, desgaste natural, regras contínuas e taxas de resfriamento intactas durante a fase de preparação.
  - Exposição da propriedade `HasPreflightChanges` para indicar se algum evento derivado de pré-voo causou dano, incrementou ciclos ou emitiu inspeções nas peças.
- **Integração no Ciclo de Vida do Monitor (`FlightMonitor.cs`)**:
  - No ramo `!IsFlightActive` de `OnSimDataReceived`, relógio próprio de preparação (`_lastPreflightTick`) computa o delta de tempo simulado e invoca `ObservePreflight`, mantendo sincronizadas as barras de componentes e colunas legadas.
  - Em `CancelMonitoringAsync`, caso o voo seja cancelado na fase de preparação mas com `HasPreflightChanges == true`, o estado das peças é persistido em contexto curto sem alterar combustível, localização ou missões.
- **Replay Fiel ao Pré-Voo (`FlightReplayer.cs`)**:
  - No modo `GameLike`, frames recebidos antes da ativação do voo passam por `ObservePreflight`, capturando ciclos de partida (`engineStartCycle`), tempo de starter (`starterSeconds`) e picos elétricos sem antecipar a contagem de horas do voo.
  - No voo do C152, a partida aos 86,4 s (1,7 s de starter) é computada com fidelidade total e o tempo simulado permanece em 1,105 h.
- **Filtragem de Excedências Sem Efeito no Blackbox (`PartWearEngine.cs`)**:
  - `PartWearExceedance` agora só é emitida para o blackbox se a faixa de estresse ativa tiver efeito real na peça (`Multiplier > 1.0` ou `LossPerHourPct > 0.0`) ou inspeção associada.
  - Suporte ao campo `eventDamagePct` no DTO do catálogo de regras de desgaste.
- **Testes Automatizados**:
  - Novos testes unitários e de replay: `Replay_C152Voo_GameLike_ContaPartidaNaPreparacao`, `Tracker_ObservePreflight_NaoAplicaDesgasteNatural_NemHoras`, `Tracker_PartidaNaPreparacao_NaoContaDeNovoNoVoo`, `Tracker_PartidaComAvionicsLigadoNaPreparacao_EmiteSpike` e `ApplyTick_FaixaSemEfeito_NaoRegistraExcedencia`.
  - 332 testes xUnit aprovados com 0 falhas; 48 testes Python aprovados.
- **Não testado no MSFS**:
  - O caminho do `FlightMonitor` ao vivo com o simulador (observação de eventos na preparação e cancelamento com peças modificadas) não foi testado no MSFS.

## 2026-11-15 - Voos sintéticos procedurais e Fuzz Testing (Gemini)
- **Gerador Procedural de Voos Sintéticos (`FlightCareer.Domain.Replay.Synthetic.SyntheticFlightGenerator`)**:
  - Geração determinística de telemetria baseada em semente (`IRandomSource`) com física procedural coerente particionada em 9 fases: cold & dark, partida a frio, táxi out, corrida de decolagem, subida, cruzeiro, descida, aproximação com flaps e toque/táxi in/corte.
  - Chaves dos frames derivadas exclusivamente do layout real (`SensorLayout.Build`) com o número de motores e perfil da aeronave, eliminando riscos de divergência de chaves de telemetria.
  - Injeção controlada de abusos operacionais (sobrevelocidade de RPM, superaquecimento de CHT/óleo, baixa pressão de óleo, choque térmico, sobretemperatura de EGT, sobrevelocidade de flaps e Vne, excedência de fator de carga positivo e negativo, pouso duro, partida prolongada, partida com aviônicos ligados, descarga profunda de bateria, sobrecarga de alternador e ciclos de trem retrátil) e anomalias de dados (sensores ausentes, valores NaN, gaps temporais > 10 s, pausas do simulador e cancelamento prematuro).
  - Metadados de rastreio e gabarito estruturados via `SyntheticGroundTruth` e `SyntheticFlightPlan`.
- **Oráculo de Invariantes e Propriedades Metamórficas (`SyntheticFlightOracle`)**:
  - Validação universal de invariantes: saúde de peças em `[0, 100]` estritamente não-crescente no voo, horas simuladas coincidentes com a telemetria (±1%), ciclos de pouso atribuídos apenas às peças pertinentes, diagnósticos estritamente únicos e ausência de `NaN`/`Infinity`.
  - Validação de gabarito: conferência de que abusos injetados disparam as regras e peças esperadas no motor correto e geram maior perda em relação ao voo controle correspondente.
  - Validação metamórfica: invariância de perda sob sim rate acelerada (1x vs 4x), isolamento entre motores em bimotores e monotonicidade (abuso mais intenso acarreta perda maior ou igual).
- **Subcomando CLI de Fuzz (`FlightCareer.Replay fuzz`)**:
  - Execução em lote de voos sintéticos (`--runs N [--aircraft <modelo>] [--seed <n>]`) com round-robin por classes representativas (`Cessna C152`, `C172SP G1000`, `Beechcraft Baron G58`, `Asobo TBM 930`).
  - Resumo estatístico de violações e exportação automática de artefatos de falha (`.csv` RFC 4180 + `.touchdowns.json` + `.groundtruth.json`) em `%LocalAppData%\FlightCareer\fuzz-failures\` para depuração direta via comando de replay.
  - Execução de 1.000 voos sintéticos completada com 1.000/1.000 voos aprovados (100,0%) e 0 falhas a ~7 voos/s.
- **Suíte de Testes Automatizados xUnit**:
  - Novos testes em `FlightCareer.Tests.Replay.Fuzz.SyntheticFlightFuzzTests`: 100 corridas fixas cobrindo as 4 aeronaves de teste, testes de gabarito para todos os tipos de abusos, determinismo bit a bit e propriedades metamórficas.
  - 439 testes xUnit aprovados com 0 falhas; 48 testes Python aprovados.
- **Não testado no MSFS**:
  - O motor de fuzz e replay é pura lógica de teste e simulação sintética sem conexão ao vivo com o SimConnect.

## 2026-11-16 - Ferramenta SimVarProbe de sondagem e gravação de SimVars (Gemini)
- **Domínio (`FlightCareer.Domain/Sensors/Probe/`)**:
  - Implementados `ProbeTypes` (`ProbeIndexKind`, `ProbeEntry`, `ProbeSlot`, `ProbeOptions`, `ProbeStatus`, `ProbeBlock`).
  - `ProbeListLoader`: carregamento resiliente de CSV com suporte a RFC 4180 (aspas duplas escapadas, campos com vírgulas), comentários `#` e fallback automático para unidade `"number"` quando variáveis numéricas não tiverem unidade definida.
  - `ProbeSlotExpander`: expansão determinística de índices para motores (`1..MaxEngine`), pontos de contato (`0..MaxContact`), wear nativo (`0..49`), cover (`0..7`), genéricos (`sem índice` + `0..MaxGeneric`) e multi-índice (`sem índice`), com filtragem regex opcional e garantia de unicidade de nomes.
  - `ProbeBlockPlanner`: particionamento de slots numéricos em blocos sequenciais de até `BlockSize` (padrão 200), garantindo exclusão absoluta de slots rejeitados e strings.
  - `ProbeStatsAccumulator`: acumulação concorrente de estatísticas por slot (amostras válidas, contagem de NaN, valores distintos limitados a 1.000, mín, máx, primeiro e último valor numérico/texto) e classificação automática de estado (`AceitaConstante`, `AceitaVariando`, `AceitaSemAmostras`).
  - `ProbeReportBuilder`: consolidação de inventário e geração estruturada de `report.csv` e `summary.txt`, respeitando a precedência estrita de rejeição sobre amostras gravadas.
  - `ProbeCsvWriter`: escrita de CSV compatível com RFC 4180, formatação numérica invariante (`CultureInfo.InvariantCulture`) e codificação UTF-8 com BOM.
- **Console (`FlightCareer.SimVarProbe`)**:
  - Projeto console `net10.0-windows` x64 em `src/FlightCareer.SimVarProbe/` referenciando `Domain` e DLLs do SimConnect gerenciada/nativa.
  - Conexão nativa sem janela com `EventWaitHandle` e tratamento gracioso de falha de conexão quando o MSFS não estiver aberto (saída com código ≠ 0).
  - Execução em duas fases:
    - **Fase A (Sondagem/Validação)**: registro de 1 slot por definição via `SIMCONNECT_PERIOD.ONCE` em lotes de até 200 com detecção precisa de `NAME_UNRECOGNIZED` via `GetLastSentPacketID` e mapeamento de exceções por slot.
    - **Fase B (Gravação Contínua)**: blocos numéricos de até 200 floats com preenchimento via `SIMULATION RATE` gravados a cada 1 s em `values.csv` (com coluna `sim_rate` e marcador de eventos), e monitoramento de transição de variáveis de texto em `strings.csv`.
  - Reconstrução dinâmica e segura de blocos fora do handler de eventos em caso de exceções na Fase B, sem interrupção da gravação dos demais blocos.
  - Comandos interativos no console: `m` ou `m <texto>` para registrar marcações operacionais com contador e `q` para encerramento gracioso com descarregamento total de arquivos.
  - Tratamento de `Console.CancelKeyPress` (Ctrl+C) e opção `--duration <seg>` para gravação temporizada.
- **Testes e Documentação**:
  - Criados 15 testes unitários em `FlightCareer.Tests/Sensors/Probe/ProbeDomainTests.cs` cobrindo carregamento real de 1.755 entradas, expansão de todas as classes de índices, planejamento de blocos, acumulador, oráculo de status e formatação CSV. Total de testes xUnit: 455 aprovados (0 falhas).
  - Criado `tools/simvar_probe/README.md` com instruções de uso, roteiro operacional passo a passo de voo (pátio, partida, táxi, voo, pouso) e dicionário de status dos relatórios.
  - Atualizados `AGENTS.md` e `src/FlightCareer.slnx`.
- **Validação no MSFS**:
  - Testado no MSFS 2024 em tempo real com o Cessna C152 em solo: sondagem de 4.750 slots completada (4.656 aceitas, 94 rejeitadas) e gravação contínua em 23 blocos validada nos arquivos `report.csv`, `summary.txt`, `values.csv` e `strings.csv`.

## 2026-11-16 - Lista de SimVars, revisão do SimVarProbe e primeiros testes no C152 (Claude)
- `docs/plans/sensor-research/simvars-msfs2024.csv`: 1.678 SimVars da documentação do SDK 2024 cruzadas com o SimvarWatcher e com o registro; geradores em `tools/simvar_probe/` (`scrape_simvars.py`, `make_probe_list.py`, `build_matrix.py`) e arquivos do Watcher em `tools/simvar_probe/watcher/`.
- Revisão da SIMVAR-PROBE (aprovada): `catch {}` silenciosos removidos e `--block-size` respeitado; rodada completa conferida no MSFS (C152 parado, sem deslocamento entre blocos).
- Plano `docs/plans/validacao-simvars.md` (validação das SimVars, teste de panes/Key Events, ligação com as peças).
- Testes do usuário no C152 (1 parado + 2 voos) em `docs/plans/sensor-validation/probe-runs/`, com matriz `matriz-C152.csv/.md`. Achados: `ELECTRICAL GENERATOR *` substitui `GENALT`; pontos de contato por índice funcionam; limites de G do sim ≠ POH; `ENG VIBRATION`, `GEAR SKIDDING FACTOR`, `SEMIBODY LOADFACTOR X/Z` e `MIN G FORCE` mortas no C152. O MSFS caiu 3 vezes com o mesmo erro interno durante a sondagem (suspeita: variáveis `ATC *`/`AI *` na troca de controle do ATC; não provado).

## 2026-11-16 - Análise das variáveis "Miscellaneous" do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-diversos.md`: as 88 SimVars da página Miscellaneous cruzadas com a matriz do C152 e com o que o app já lê, com usos para HUD, qualidade do voo/passageiros, missões e economia, integridade da carreira (crash desligado, pausa, teletransporte) e desgaste/panes.
- Achado: `FlightQualityService` conta violações por pacote de telemetria (depende da frequência); corrigir para tempo ao mexer na nota dos passageiros.
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis de freios e trem do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-freios-trem.md`: as 86 SimVars de freios, pontos de contato e trem cruzadas com a matriz do C152, com usos para HUD, qualidade do pouso (ordem do toque por roda, quiques, frenagem brusca), missões (piso por roda, tipo de trem), integridade e desgaste/panes (SimVars graváveis de trem/freio como efeito de pane, a testar).
- Achado: `BRAKE LEFT/RIGHT WEIGHT REMAINING` variou no C152 (desgaste nativo das pastilhas), entra na calibração addon × nativo.
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis de comandos de voo do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-comandos.md` (96 SimVars de superfícies, compensadores, flaps, spoilers, manche e fly-by-wire) com usos para HUD, qualidade (suavidade dos comandos, voo fora de compensação), treino/diário (horas manuais × piloto automático) e desgaste/panes.
- Achado: `FLAPS CURRENT SPEED LIMITATION` dá a Vfe do dente atual ao vivo (C152: 110 kt) e pode cobrir as 22 aeronaves sem `vfe` no catálogo.
- 32 variáveis acrescentadas a `tools/simvar_probe/watcher/flightcareer-candidatas-hud-qualidade.simvars`. Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis do sistema elétrico do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-eletrica.md` (111 SimVars) com usos para HUD (bateria, gerador, GPU, APU), passageiros (sinais de cabine), economia (GPU, APU), integridade (disjuntor/alternador religado) e desgaste/panes (sobrecarga por amperes do gerador, ciclos de motor de flap/trem, disjuntores como pane por sistema).
- 35 variáveis acrescentadas a `tools/simvar_probe/watcher/flightcareer-candidatas-hud-qualidade.simvars`. Só documentação; nada testado no MSFS.

## 2026-11-16 - Planos de taxas de solo e de iluminação/panes por disjuntor (Claude)
- `docs/plans/taxas-aeroporto-servicos-solo.md`: catálogo JSON de taxas por tier × porte (pouso, estacionamento, GPU, pushback, degelo, handling, taxa de embarque, catering, navegação), calculador puro no Application, serviços medidos no sim por tempo simulado, fases F1–F4 e decisões pendentes.
- `docs/plans/iluminacao-panes-disjuntor.md`: panes por sistema via disjuntor/circuito/linha do sistema elétrico v2 (flap elétrico, GPS, piloto automático, transponder, barramento parcial, luzes) com mapa elétrico por aeronave e watchdog; grupo "Iluminação" no Hangar com lâmpadas e ligação com a MEL. Magneto continua fora do sistema elétrico.
- Só planos; nada implementado nem testado no MSFS.
## 2026-11-16 - Limites estimados de Vne, Vfe e alternador nas aeronaves sem POH (Claude)
- 42 perfis em `Infrastructure/Data/aircraft/` ganharam `vne`, `vfe:n` e `alternatorRatedAmps` onde faltavam, com `source: ClassDefault`, `reference` começando por "estimativa — revisar" e `notes` "ESTIMATIVA — trocar pelo valor do POH/AFM". Valores aproximados de POH/AFM publicados e pesquisa rápida (vários não confirmados: 188, 404, 408, 787).
- Fonte `ClassDefault` de propósito: mantém a reserva nativa (overstress do sim) ativa nesses limites de baixa confiança.
- Sem Vfe no CAP-4 e no J-3 Cub (sem flap) e sem amperagem no CAP-4, J-3 e A400M. Índices elétricos (`elecMainBusIndex`, `elecAvionicsCircuitIndex`) **não** foram inventados: dependem da calibração no sim (`tools/aircraft_limits/calibrate.py`).
- Build 0 erros; `dotnet test` 455/455. Não testado no MSFS.
## 2026-11-16 - Regra de delegação: dados também vão para o Gemini (Claude)
- `CLAUDE.md` e `tools/agents/global/CLAUDE.md`: catálogos e dados (JSON/CSV) com mais de ~20 linhas vão para o Gemini, com o critério na spec e revisão por amostragem; pesquisa, análise de documentação e planos continuam com o Claude (decisão do usuário).
## 2026-11-16 - REGRAS-C152 Regras e sensores do C152 e correções de fuzz (Gemini)
- **Registro de Sensores (`sensor_registry.json`, `sensor-inventory.md`, `SensorRegistryTests.cs`)**:
  - Pontos de contato de trem e derrapagem convertidos de `perEngine: true` para sensores explícitos por roda com `perEngine: false`: `gear-contact-point-compression-0/-1/-2` (`CONTACT POINT COMPRESSION:0..2`) e `gear-contact-point-is-skidding-0/-1/-2` (`CONTACT POINT IS SKIDDING:0..2`).
  - Adicionados sensores de contato com solo por roda `gear-contact-point-on-ground-0/-1/-2` (`CONTACT POINT IS ON GROUND:0..2`).
  - Adicionadas variáveis ativas de gerador elétrico MSFS 2024 `elec-generator-amps`, `elec-generator-voltage`, `elec-generator-active` (`perEngine: true`). Chaves legadas `elec-genalt-*` preservadas.
  - Adicionada pressão dinâmica `af-dynamic-pressure` (`DYNAMIC PRESSURE`, unidade `pound-force per square foot`).
  - Preservadas variáveis que não variaram no C152 (`eng-vibration`, `gear-skidding-factor`, `af-min-g-force`) para continuidade de gravação e prova em outros modelos.
- **Catálogo de Regras e Peças (`wear_rules.json`, `parts_catalog.json`)**:
  - `airframe_over_g_negative`: simplificado para usar exclusivamente `["af-g-force"]` (desacoplado de `af-min-g-force`).
  - `touchdown_hard_landing` e `landing_cycle_structural_stress`: sensores atualizados para rodas principais (`gear-contact-point-compression-1` e `-2`).
  - Peças atualizadas com novas chaves por roda e geradores elétricos.
- **Domínio (`PartWearEngine.cs`, `PartEventDeriver.cs`, `_class_defaults.json`)**:
  - `PartWearEngine`: suporte completo a limites negativos quando `Direction == LimitDirection.Below` (cálculo de excesso sem inversão, ordenação de faixas por `limit * Threshold` decrescente, e emissão de diagnóstico quando limite negativo for usado com `Above`).
  - Resolução de sensores por motor prioriza `${sensorKey}:1` sobre chave desindexada global.
  - `PartEventDeriver`: corrigido limiar de posição de trem para percentual 0–100 (`isUp <= 10.0`, `isDown >= 90.0`) e adicionado fallback por `gear-contact-point-on-ground-*` na detecção de solo.
  - `_class_defaults.json`: adicionado `egtMax` padrão = 893.52 °C para classe `Piston`.
- **Fuzz Testing e Replay (`SyntheticFlightGenerator.cs`, `SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs`, `Program.cs`)**:
  - Posição do trem em percentual (0–100) e geração determinística dos novos sensores (`0..2`, gerador, pressão dinâmica).
  - Sincronização do voo controle e validação de gabarito para voos cancelados prematuramente.
  - Duração mínima garantida para abusos contínuos acumulados e proteção contra truncamento de abusos por cancelamento.
- **Testes xUnit**:
  - Criados 8 testes em `C152RulesTests.cs` cobrindo limites de G negativo, descarte de limite negativo em Above, EGT Max padrão, posição de trem e paridade do registro. Total de 463 testes unitários aprovados (0 falhas). Fuzz testing de 100 corridas com 100% de aprovação (0 falhas).


## 2026-11-16 - REGRAS-C152-r2 Correções da rodada 1 de revisão (Gemini)
- **Fuzz Testing de 1000 Voos (`SyntheticFlightPlan.cs`, `SyntheticFlightGenerator.cs`, `SyntheticFlightOracle.cs`, `Program.cs`)**:
  - Reproduzidas e classificadas as 14 falhas (17 violações de gabarito) da rodada 1: todas decorrentes de colisões/sobrescritas no gerador e plano sintético (nenhum bug de produção na lógica de peças).
  - Eliminadas sobreposições destrutivas de abusos antagônicos: escalonamento temporal entre `PositiveGExceeded` e `NegativeGExceeded`, exclusão mútua entre `BatteryDischarging` e `ElectricalOverload` no mesmo voo, alinhamento exato de `HardLanding` com o touchdown (`0.90 * duration`) e garantia de cancelamento (`DataCancelledMidFlight`) estritamente posterior aos abusos físicos.
  - No gerador, preservado o resfriamento contínuo de `ThermalShock` durante cruzeiro e ajustado `EgtHigh` para `egtMax * (1 + intensity)` (faixa Warning com perda de saúde ativa).
  - Execução completa do fuzz de 1000 voos (`--runs 1000 --seed 777`): **0 falhas, 0 violações de gabarito** (847 aprovados / 153 com limites ausentes no catálogo segregados).
- **Lacunas do Catálogo e Limites Ausentes (`SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs`, `Program.cs`)**:
  - Restaurado o sorteio incondicional de abusos em todas as aeronaves em `SyntheticFlightPlan.cs`.
  - Oráculo e runner da CLI registram `limite ausente: <chave>` em categoria segregada sem computar falha ou aprovado (58 em `elecAvionicsCircuitIndex`, 45 em `alternatorRatedAmps`, 32 em `elecMainBusIndex`, 22 em `vfe`, 14 em `vne`).
- **Tempos Reais no Oráculo (`SyntheticFlightOracle.cs`)**:
  - Substituídos tempos fixos (600s/300s) pela leitura dinâmica do primeiro limiar de `wear_rules.json` para descarga profunda e sobrecarga elétrica.
- **Resolução de Sensores por Motor (`PartWearEngine.cs`, `PartFlightTracker.cs`)**:
  - Evidenciado o frame real em `PartFlightTracker.cs#L490-L494` onde `eng-cht-cooling-rate:1` e `eng-cht-cooling-rate` coexistem, justificando a prioridade para `${sensorKey}:1` para que regras de motor recebam o índice correto.
- **Validação de Build e Testes**:
  - `dotnet build src/FlightCareer.slnx`: 0 erros.
  - `dotnet test src/FlightCareer.Tests`: 463 testes aprovados, 0 falhas.

## 2026-11-16 - Análise das variáveis de motor do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-motor.md` (189 SimVars): foco no que falta no registro e nos usos fora do desgaste — HUD (fluxo, % de potência, carburador), gestão do motor na nota de pilotagem, Hobbs e combustível real na economia, FLX × TOGA nos jatos, panes por SimVar gravável (falha do turbo, falta de combustível, limite do manete) e a decisão pendente de gravar as perdas de eficiência da turbina.
- 24 variáveis acrescentadas à lista do Watcher. Só documentação; nada testado no MSFS.

## 2026-11-16 - SimVars obsoletas do SDK 2024 (Claude)
- A documentação marca obsoletas com `class=obsolete`, que o extrator não capturava. Levantamento nas 15 páginas: 70 obsoletas, em `docs/plans/sensor-research/variaveis-obsoletas.md`.
- Removidas das listas do Watcher: `CRASH FLAG`, `CRASH SEQUENCE`, `STRUCT DAMAGEVISIBLE`, `SEMIBODY LOADFACTOR X/Z`, `EXIT OPEN`; `ELEVATOR TRIM PCT` trocada por `EX1`. Aviso nas análises `variaveis-*.md` e no plano de validação.
- Pendente (código, Gemini): `CRASH FLAG` e `GENERAL ENG FIRE DETECTED` ainda são lidos em produção; coluna `obsoleta` no extrator com teste.

## 2026-11-16 - Análise das variáveis de modelo de voo do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-modelo-voo.md` (99 SimVars): HUD (peso/CG, ângulo de ataque, Vr, portas), passageiros (solavanco por `SEMIBODY LOADFACTOR YDOT`, voo descoordenado), missões (capacidade útil, CG, planejamento, jetway, avião convencional), integridade (peso mudando em voo) e desgaste (acima do peso, fora do CG, porta aberta com risco).
- 23 variáveis acrescentadas à lista do Watcher (`INTERACTIVE POINT OPEN` no lugar da obsoleta `EXIT OPEN`). Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis de combustível do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-combustivel.md` (72 SimVars): HUD por tanque e desbalanceamento, reserva no pouso, consumo real (o cálculo inicial − final falha com abastecimento pelo menu), `UNLIMITED FUEL` como trapaça, despejo cobrado, vazamento de combustível como pane (tanques graváveis) e a decisão pendente de o app gravar no sim o combustível comprado.
- 13 variáveis acrescentadas à lista do Watcher. Só documentação; nada testado no MSFS.
## 2026-11-16 - TURB-DEGRADACAO Degradação de motor a turbina gravada no MSFS (Gemini)
- **Decisão do usuário (2026-11-16):** gravação no MSFS das 4 perdas de eficiência da turbina (`TURB ENG N1 LOSS`, `TURB ENG FUEL EFFICIENCY LOSS`, `TURB ENG ITT COOLING EFFICIENCY LOSS` e `TURB ENG THRUST EFFICIENCY LOSS`, unidade `Percent` 0–100, indexadas por motor `:1`..`:4`) calculadas a partir da saúde dos componentes Motor e Seção Quente, para que o desgaste do motor afete o desempenho em voo (ITT mais alta, maior consumo, menor empuxo).
- **Domínio (`TurbineDegradation.cs`, `ComponentCatalog.cs`, `component_catalog.json`)**:
  - `TurbineLosses`: record imutável com as 4 grandezas de perda, suporte a tolerância e constante `None`.
  - `TurbineDegradationConfig`: limites parametrizados lidos de `component_catalog.json` (`startHealthPct: 60`, perdas máximas `maxN1LossPct: 5`, `maxFuelEfficiencyLossPct: 10`, `maxIttCoolingLossPct: 15`, `maxThrustEfficiencyLossPct: 8`, e passo de tolerância `writeToleranceStepPct: 0.5`). Ausência do bloco = desativado (`null`).
  - `TurbineDegradation.Compute`: cálculo linear de fator `(startHealth - health) / startHealth` clampado em `[0, 1]` com arredondamento no passo de escrita.
  - `TurbineDegradation.ShouldWrite`: função pura com proteção de intervalo mínimo de relógio (5 s) e detecção de alteração de alvo ou divergência da leitura no simulador.
- **SimConnect e Interfaces (`SimConnectService.cs`, `ISimConnectService.cs`)**:
  - Mapeadas 16 definições de escrita (4 SimVars × 4 motores) com struct `SimDoubleValue`.
  - Mapeada definição separada de leitura (`TurbineLossesRead`) com struct `SimTurbineLossesState` para conferência periódica (1 s) sem alterar `SimTelemetry`.
  - Métodos `SetTurbineLosses` e `GetTurbineLosses` adicionados a `SimConnectService` e `ISimConnectService`.
- **Atuação em Voo (`TurbineDegradationActuator.cs`, `ComponentFlightSession.cs`)**:
  - `TurbineDegradationActuator`: atua apenas em motores a turbina (`EngineType == 1.0` ou `5.0`), avalia perdas alvo, respeita janela de 5 s de relógio por motor, reaplica perdas em recarregamentos do simulador (`OnAircraftReloaded`) e zera em `Reset()` apenas motores onde o addon havia gravado perdas.
  - Chamadas integradas em `ComponentFlightSession.ProcessTick` e `ComponentFlightSession.Reset`.
- **Testes xUnit**:
  - Criados 10 testes em `TurbineDegradationTests.cs` cobrindo cálculo linear, motor pior entre motor/seção quente para combustível, tolerância de arredondamento, regras de decisão de escrita e carregamento do catálogo com e sem o bloco.
  - Criado teste de validação de layout em `TelemetryDefinitionTests.cs` para os campos do struct de conferência.
  - Total: 474 testes aprovados, 0 falhas.

## 2026-11-16 - TURB-DEGRADACAO-r2 Ajustes da revisão de degradação de turbina (Gemini)
- **Correções da rodada 1 da revisão**:
  - `NUMBER OF ENGINES` (unidade `Number`) adicionado ao final da definição de leitura de conferência `TurbineLossesRead` e struct `SimTurbineLossesState`; exposta propriedade `TurbineEngineCount` em `ISimConnectService` e `SimConnectService`; `ComponentFlightSession` agora repassa a contagem real de motores (fallback 1 se 0).
  - `HandleAircraftReloaded`: removida escrita imediata no callback de recarga de aeronave. O método agora apenas marca os motores como não gravados (`_lastWritten = None`, `_lastWriteTime = default`, `_hasWritten = false`), delegando a gravação para o próximo `Apply` após a verificação de tipo de motor (turbina grava; pistão não grava).
  - Removido método sem uso `SetDebugTurbineLosses` de produção em `SimConnectService`.
  - Movido `TurbineDegradationActuator` para `FlightCareer.Application/Services` para desacoplamento de UI e suporte pleno a testes com `ISimConnectService` falso em `FlightCareer.Tests`.
  - Novos testes unitários: atuador gravando em múltiplos motores (2 motores) e ciclo de recarga garantindo ausência de escrita no evento, gravação posterior em turbina e bloqueio em pistão.
  - Testes: 476 aprovados, 0 falhas.

## 2026-11-16 - Análise das variáveis diversas da aeronave do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-aeronave-diversos.md` (116 SimVars): vento no eixo do avião e alertas no HUD, precisão do pouso (ponto e rumo do toque), manobras bruscas (taxas angulares), fim de missão no estacionamento, pintura/matrícula, capas e travas no walkaround, slew como trapaça e desgaste das partes expostas.
- 22 variáveis acrescentadas à lista do Watcher. Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis de rádio e navegação do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-radio-navegacao.md` (278 SimVars): HUD (ETE, radioaltímetro, transponder), qualidade (precisão ILS/RNAV, autorizações do ATC, desvio do plano, código de emergência), missões (pista mínima por avião, comprimento/largura da pista, gate e chegada à vaga, aeroporto com torre) e matrícula/companhia/voo graváveis (`ATC ID`, `ATC AIRLINE`, `ATC FLIGHT NUMBER`).
- `ATC *` em lista separada do Watcher (`flightcareer-candidatas-atc.simvars`) por causa da suspeita das quedas do MSFS; 18 variáveis de GPS/NAV/COM/transponder na lista principal. Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise das variáveis de sistemas do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/variaveis-sistemas.md` (280 SimVars): painel parcial gravável como pane nativa por instrumento, vazamento hidráulico, congelamentos como trapaça, conforto de cabine (bola, sinais, razão e temperatura de cabine), luzes por fase e horas acesas, carga da missão gravada nas estações (`PAYLOAD STATION WEIGHT`) e tipos de missão futuros (combate a incêndio, reboque).
- 37 variáveis acrescentadas à lista do Watcher. Só documentação; nada testado no MSFS.

## 2026-11-16 - COMBUSTIVEL-REAL Consumo real, abastecimento gravado no sim e travas de combustível (Gemini)
- **Decisões do usuário (2026-11-16):**
  - Consumo cobrado pelo consumo real dos motores (`GENERAL ENG FUEL USED SINCE START:1..4`).
  - Abastecimento do aplicativo gravado diretamente nos tanques do MSFS no solo com motores desligados.
  - `UNLIMITED FUEL` ativado bloqueia o início do monitoramento e, se ligado em voo, invalida o voo.
  - Abastecimento em voo (subida de combustível além da tolerância) invalida o voo sem pagamento da missão, cobrando o combustível consumido real.
- **Domínio (`FlightCareer.Domain/Fuel/`)**:
  - `FuelUsageAccumulator`: acumula incrementos por motor em libras, lida com reinício de motores (contador zera), valores inválidos e converte em galões via `weightPerGallon`.
  - `FuelIntegrityMonitor`: detecta combustível ilimitado, abastecimento em voo e divergências no solo antes do voo.
  - `FuelTankWritePlan`: distribui proporcionalmente o combustível desejado pela capacidade útil dos tanques sem exceder as capacidades individuais (sistema novo `FUELSYSTEM` e tanques legados).
- **SimConnect e Interfaces (`ISimConnectService.cs`, `SimConnectService.cs`)**:
  - Definição separada `FuelStateRead` e struct `SimFuelState` a 1 s contendo `UNLIMITED FUEL`, consumo por motor `:1..:4`, `NEW FUEL SYSTEM` e capacidades/quantidades dos tanques 1..8 e legados (`LEFT/RIGHT MAIN/AUX/TIP, CENTER/CENTER2/CENTER3`).
  - Mapeadas 17 definições de escrita de tanques (`SetFuelTanks`).
  - `GetActiveFuelTanks()` expõe os tanques presentes com capacidade útil > 0.
- **Abastecimento e Hangar (`HangarViewModel.cs`, `RefuelViewModel.cs`)**:
  - No abastecimento concluído, se conectado ao sim no solo com motores desligados, grava automaticamente a nova quantidade nos tanques do simulador.
- **Travas no Monitoramento (`FlightMonitor.cs`)**:
  - `StartMonitoring`: bloqueia se `UNLIMITED FUEL` = 1. Sincroniza automaticamente no solo se a quantidade no sim divergir do aplicativo além da tolerância, registrando no blackbox.
  - Em voo: monitora integridade continuamente; `UNLIMITED FUEL` ou abastecimento em voo invalida o voo (`_flightInvalidated`).
  - Pouso/Finalização: repassa o consumo real de `FuelUsageAccumulator` e o estado de invalidação para `FlightService`.
- **Economia e Cobrança (`FlightService.cs`)**:
  - `FuelUsed` calculado pelo consumo real dos motores (`realFuelUsed`).
  - Conferência de divergência > 10% em relação a `(initialFuel - finalFuel)` registrada no blackbox.
  - Voo invalidado: sem receita de missão nem conclusão de missões, cobrando despesa de combustível real e taxa de pouso.
  - `PilotAircraft.FuelAmount` atualizado com o combustível final real do simulador.
- **Testes xUnit**:
  - 10 novos testes em `FuelDomainTests.cs` cobrindo acumulador com dois motores, reinício de motor (10 → 12 → 0,5 → 1 = 13 lb), descarte de NaN/inválidos, detecção de ilimitado, detecção de subida em voo, tolerância a ruído, divergência em solo, distribuição em asas (13/13 gal) com e sem excedente, suporte a tanques legados e fixture real de voo `c152-sbmt-snej-20261115.csv.gz`.
  - Total: 486 testes aprovados, 0 falhas.

## 2026-11-16 - COMBUSTIVEL-REAL-r2 Correções da rodada 1 de revisão (Gemini)
- **Correções da rodada 1**:
  - `FuelUsageAccumulator.Reset()`: implementado método que zera os incrementos acumulados, últimos valores lidos por motor e diagnósticos; invocado no `FlightMonitor.StartMonitoring` para que o consumo de voos anteriores não seja cobrado no voo subsequente.
  - Sincronização pendente com conferência assíncrona (`FuelSyncCoordinator`): criada classe pura no domínio com relógio injetável, tolerância e timeout de ~5 s por tentativa (máx 3 tentativas). A sincronização no `StartMonitoring` agora entra em modo pendente e só confirma após nova leitura dos pacotes do simulador no solo; se divergir após as tentativas, alerta "ERRO DE SINCRONIZAÇÃO" e chama `StopMonitoring`. A integridade (`FuelIntegrityMonitor`) só é ativada após a sincronização ser confirmada.
  - Filtro de gravação no simulador por aeronave ativa: `FlightMonitor.WatchedAircraftId` expõe o id da aeronave ativa/vigiada; `HangarViewModel.TryWriteFuelToSimulator` e `RefuelViewModel` conferem se a aeronave abastecida corresponde à aeronave assistida antes de enviar escritas aos tanques do MSFS.
  - Novos testes unitários: 6 testes em `FuelDomainTests.cs` validando o reset do acumulador entre voos e todas as ramificações de estado e tempo do `FuelSyncCoordinator`.
  - Total: 492 testes aprovados, 0 falhas.

## 2026-11-16 - Análise dos Key Events elétricos do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/eventos-eletricos.md` (129 eventos): como enviar (panes e serviços) e receber (ações do piloto) Key Events; `BREAKER_*_SET` para pane por sistema rearmável, `ELECTRICAL_BUS_TO_CIRCUIT_CONNECTION_TOGGLE` para fio rompido, barramento parcial, bateria fora do barramento, `SET_EXTERNAL_POWER` para a GPU contratada, luzes por `*_LIGHTS_SET` e eventos de APU. Preferir `_SET` a `_TOGGLE`.
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise dos Key Events de motor do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/eventos-motor.md`: parada por falta de combustível (`SHUTOFF_VALVE_OFF`, `SET_FUEL_VALVE_ENG*`), ignição da turbina, panes por mascaramento do comando do piloto (manete, governador, mistura, cowl flap, partida, reverso), cheque de magnetos e outros procedimentos recebidos do piloto. `ISOLATE_TURBINE_*` só serve ao DarkStar; as panes nativas de motor ficam na página de falhas.
- `eventos-eletricos.md`: decisão do usuário de usar as duas panes elétricas (disjuntor rearmável e fio rompido).
- Só documentação; nada testado no MSFS.
## 2026-11-16 - SLEW-INTEGRIDADE Slew no solo permitido, voo invalidado no ar e detecção de congelamento (Gemini)
- **Regra de integridade do Slew e Congelamentos**:
  - Antes de iniciar o monitoramento: uso livre.
  - No solo antes da decolagem: reposicionamento por slew permitido até 1.000 m acumulados (padrão configurável), registrando "slew no solo: X m" no blackbox ao concluir cada uso.
  - Slew no ar, slew no solo após decolagem (pousou e usou slew), ou deslocamento total em slew no solo excedendo 1.000 m: voo invalidado.
  - Congelamentos do simulador (`IS ALTITUDE FREEZE ON`, `IS ATTITUDE FREEZE ON`, `IS LATITUDE LONGITUDE FREEZE ON`): permitidos no solo antes da decolagem; ativados em voo ou após a decolagem invalidam o voo ("Voo invalidado: congelamento").
  - Reutilização do canal de invalidação (`InvalidateFlight`), cancelando recompensas de missões, mantendo cobrança de combustível/taxas e emitindo alerta no HUD.
- **Domínio (`FlightCareer.Domain/Integrity/`, `FlightCareer.Domain/Utils/`)**:
  - `SlewMonitor` e `SlewVerdict`: lógica pura que acumula deslocamento Haversine durante `IS SLEW ACTIVE`, valida solo/ar/decolagem, limites configuráveis e flags de congelamento.
  - `LocationUtils.CalculateDistanceMeters`: cálculo de distância geográfica esférica direta em metros.
- **SimConnect e Interfaces (`ISimConnectService.cs`, `SimConnectService.cs`)**:
  - Leitura a 1 s na nova definição separada `IntegrityStateRead` com a struct `SimIntegrityState` (`IS SLEW ACTIVE`, `IS ALTITUDE FREEZE ON`, `IS ATTITUDE FREEZE ON`, `IS LATITUDE LONGITUDE FREEZE ON`). Nenhuma escrita em `IS SLEW ACTIVE`.
- **FlightMonitor (`FlightMonitor.cs`)**:
  - Extraído método comum `InvalidateFlight(string reason)` reutilizado para integridade de combustível, slew e congelamento.
  - Alimentação contínua de `SlewMonitor` a cada frame recebido. Registro no blackbox com tag `SLEW` para reposicionamentos permitidos no solo.
- **Testes xUnit**:
  - 7 novos testes em `SlewMonitorTests.cs`: `Slew_NoSoloDentroDoLimite_Permitido`, `Slew_NoSoloAcimaDoLimite_Invalida`, `Slew_NoAr_Invalida`, `Slew_DepoisDaDecolagem_NoSolo_Invalida`, `Slew_Inativo_NaoSomaDistancia`, `Freeze_EmVoo_Invalida`, `Freeze_NoSoloAntesDaDecolagem_Permitido`.
  - 1 novo teste em `TelemetryDefinitionTests.cs` conferindo a contagem e correspondência dos datums de `IntegrityStateRead` com os campos de `SimIntegrityState`.
  - Total: 500 testes aprovados, 0 falhas.


## 2026-11-16 - Análise dos Key Events de comandos de voo do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/eventos-comandos.md`: compensador inoperante nativo (`*_TRIM_DISABLED_SET`) e compensador disparado, spoilers armados, auto-leme, e bloqueio do slew em voo por mascaramento de `SLEW_ON/SET/TOGGLE`.
- `eventos-motor.md`/`eventos-comandos.md`: lembrete do princípio F4.2e (falhar o sistema sem mexer no comando; mascaramento abandonado para flap e alternador) e decisão do usuário sobre o auto-start (perde XP, não invalida).
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise dos Key Events de combustível do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/eventos-combustivel.md`: `REPAIR_AND_REFUEL` e `ADD_FUEL_QUANTITY` como detecção de trapaça (invalidar e reaplicar panes), caminhão de combustível do sim (`REQUEST_FUEL_KEY`, decisão pendente), panes no sistema por `FUELSYSTEM_VALVE_SET`/`PUMP_SET`/`JUNCTION_SET` e procedimentos (seletor, alimentação cruzada, bombas, despejo).
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Análise de seis páginas de Key Events do SDK 2024 (Claude)
- Novo `docs/plans/sensor-research/eventos-diversos.md` (sistemas gerais, instrumentos, aeronave diversos, rádio/navegação, diversos e câmera): panes no lado do sistema (atuador/válvula hidráulica, packs, deriva do giro, curso da bequilha), alarme nativo e ELT, serviços de solo pelo sim (pushback, GPU, catering, bagagem, jetway, escada), integridade (`BACK_TO_FLY`, resets, pausa, replay) e procedimentos recebidos do piloto. Câmera: nada útil.
- Só documentação; nada testado no MSFS.

## 2026-11-16 - Estudo da API do SimConnect do MSFS 2024 (Claude)
- Novo `docs/plans/sensor-research/simconnect-api.md` (Actions, Debug, Events And Data/Flow API, Flights, General, Input Events): "continuar de onde parou" com `FlightSave`/`FlightLoad` (ideia do usuário), plano da missão no GPS com `FlightPlanLoad`, Flow API para integridade e acidente (teleporte, Back On Track, replay, `PLANE_CRASH`), `ExecuteAction` (`FailureAction` com saúde, `RefillAction`, mensagens e destaque de instrumento dentro do sim), pinturas instaladas e atalho reservado.
- Só documentação; nada testado no MSFS.
- Decisões do usuário (2026-11-16): caminhão de combustível do sim **cobra e aceita** (`eventos-combustivel.md`); **captura automática do pouso** para o diário de bordo aprovada (`eventos-diversos.md`).

## 2026-11-16 - Fuzz em todas as aeronaves (Gemini)
- **Feature `noFlaps` e ressalva 2**:
  - Nova constante `AircraftFeatures.NoFlaps = "noFlaps"` (grupo "Aerodinâmica e superfícies") adicionada ao vocabulário fechado e ao enum de `_schema.json`.
  - Atribuída feature `noFlaps` aos perfis `piper-j-3-cub.json` (dataVersion 6) e `cap-4.json` (dataVersion 5).
  - `SyntheticFlightPlan.Random` não sorteia `FlapOverSpeed` quando o perfil tem `noFlaps`.
  - `SyntheticFlightOracle.CheckMissingLimit` não reporta ausência de limite `vfe` para aeronaves `noFlaps`. Em planos manuais com `FlapOverSpeed` em aeronaves `noFlaps`, o gabarito é ignorado sem violação.
- **Ressalva 1 do Oráculo (desgaste natural proporcional)**:
  - Substituída a tolerância fixa de 0.05 %/h por comparação escalada pela taxa real da peça (`scaledControl = controlPart.HealthLoss * (abusedHours / controlHours)`) quando `abusedHours < controlHours - 1e-5`, com folga de 1% (`ScaledControlToleranceRatio = 0.01`) para não linearidade.
- **CLI `FlightCareer.Replay fuzz --aircraft all` e Agregação**:
  - Nova classe pura `FuzzRunSummary` no Domain consolidando estatísticas e ordenando resumos por falhas decrescentes.
  - CLI suporta `--aircraft all` (50 voos padrão por aeronave em todas as 46 aeronaves do catálogo, ordenadas por `AircraftId`) e mantém compatibilidade com modelo único ou padrão de 4 modelos.
  - Semente e abusos idênticos bit a bit por aeronave (`seed = baseSeed + run`), com campo `reproduce` adicionado ao `groundtruth.json` despejado em falhas e tratamento de erro sem catch silencioso.
- **Testes xUnit**:
  - 9 novos testes em `SyntheticFlightFuzzAllTests.cs` cobrindo feature `noFlaps`, ressalvas 1 e 2 do oráculo, agregação e ordenação de `FuzzRunSummary`, carregamento de perfis do catálogo e validação rápida de invariantes em todos os 46 modelos.
  - Total: 509 testes aprovados, 0 falhas.

## 2026-11-16 - egtMax pelo padrão da classe em 4 aviões a pistão (Claude)
- Bonanza G36, CAP-4, DA40NG e Robin DR400 tinham `egtMax` = 649,07 °C (1.200 °F) lido da SimVar `MAX EGT`, que é a escala do instrumento e não o limite do motor: o voo normal do fuzz ficava em excedência contínua de EGT. Agora usam o padrão da classe pistão (893,52 °C, `source: ClassDefault`, "estimativa — revisar com POH"); `dataVersion` incrementado.
- `tools/aircraft_limits/merge_limits.py` descarta `MAX EGT` = 649,07 °C para a captura não regravar o valor. Decisão do usuário: os POHs reais serão revisados depois.
- Build 0 erros, `dotnet test` 509/509, `python -m unittest` 48/48; fuzz do Robin DR400 (50 voos) sem falhas. Não testado no MSFS.

## 2026-11-16 - Limites estimados, noElectrical e índices elétricos de teste no fuzz (Gemini)
- **Feature `noElectrical`**:
  - Nova constante `AircraftFeatures.NoElectrical = "noElectrical"` (grupo "Célula e sistemas adicionais") adicionada ao vocabulário fechado e ao enum de `_schema.json`.
  - Atribuída feature `noElectrical` aos modelos Piper J-3 Cub e CAP-4 (`piper-j-3-cub.json` dataVersion 7, `cap-4.json` dataVersion 6).
  - `SyntheticFlightPlan.Random` não sorteia os 3 abusos elétricos (`BatteryDischarging`, `ElectricalOverload`, `AvionicsMasterOnStart`) quando a aeronave tem `noElectrical`.
  - `SyntheticFlightOracle.CheckMissingLimit` não reporta ausência de limite elétrico para aeronaves `noElectrical`. Em planos manuais com abuso elétrico nessas aeronaves, o gabarito é ignorado sem violação.
- **Limites estimados no catálogo (`Infrastructure/Data/aircraft/*.json`)**:
  - Preenchidos limites operacionais ausentes que possuem especificação de manual (voltagem `elecSystemVolts`, rotação `rpmRedline`, temperaturas `oilTempMax`, `ittMax`, `chtMax`, e corrente `alternatorRatedAmps` no A400M) em 44 arquivos JSON, com `source: "ClassDefault"`, referência padronizada (`"estimativa — atualizar pelo POH real: <base>"`) e `dataVersion` incrementado em +1.
  - Índices elétricos (`elecMainBusIndex`, `elecAvionicsCircuitIndex`, `elecAvionicsBusAmpsIndex`) intencionalmente mantidos fora do catálogo do jogo.
- **Índices elétricos de teste para Fuzz (`Domain/Replay/Synthetic/FuzzTestDefaults.cs`)**:
  - Criada classe pura `FuzzTestDefaults.WithTestElectricalIndices(profile)` que devolve uma cópia do perfil injetando apenas os índices ausentes (`elecMainBusIndex` = 1, `elecAvionicsCircuitIndex` = 3, `elecAvionicsBusAmpsIndex` = 4) com `ClassDefault` e nota `"padrão de teste do fuzz — não usar no jogo"`. Modelos com `noElectrical` são retornados intactos.
  - Aplicada exclusivamente no `RunFuzz` do CLI (`FlightCareer.Replay`) e nos testes xUnit de fuzz (`SyntheticFlightFuzzAllTests.cs`). Nunca usada pelo seeder, resolver ou UI.
  - `SyntheticFlightGenerator.ResolveLimit` atualizado para priorizar limites definidos no perfil injetado antes da resolução externa.
  - Atualizada mensagem de ajuda do CLI informando o uso de índices elétricos de teste.
- **Testes e Validação**:
  - 7 novos testes em `CatalogEstimatesTests.cs` cobrindo todas as regras de limites estimados, feature `noElectrical` e injeção de índices de teste.
  - Suíte completa de 46 modelos no fuzz (`fuzz --aircraft all --runs 50`, 2.300 voos): **0 limites ausentes em todos os 46 modelos** (eliminando 100% das falhas por limite ausente, incluindo as 12 anteriores do 737 Max 8). 38 aeronaves com 100% de aprovação (50/50).
  - Build 0 erros, `dotnet test` 516/516 aprovados, `python -m unittest` 48/48 aprovados. Não testado no MSFS.
- Revisão (Claude): Dakota ainda tinha `egtMax` = 615,74 °C da escala do instrumento; trocado pelo padrão da classe e `merge_limits.py` passa a descartar EGT de pistão < 700 °C. Fuzz do Dakota, SR22 e Baron G58 sem falhas.

## 2026-11-16 - Gerador sintético respeita CHT e Vfe por avião (Gemini)
- **Escala proporcional de CHT (`chtScale`)**:
  - `SyntheticFlightGenerator`: calculada escala `chtScale = Math.Min(1.0, 0.85 * chtMax / 175.0)` aplicada a todos os alvos, tetos e pisos do voo normal (120, 165, 175, 155 ± 0,5, 50, 130) e piso do choque térmico (50). Mantidas inalteradas a temperatura inicial ambiente (20 °C) e a injeção de sobreaquecimento de CHT (`chtMax + ...`).
  - Para aeronaves com `chtMax >= 205,9` °C (como C152 e C172), `chtScale = 1,0`, mantendo o voo estritamente idêntico bit a bit e determinístico.
  - Em motores refrigerados a líquido (Diamond DA40NG diesel com `chtMax = 105`, DA62 diesel com `121,29`, Flight Design CT Rotax com `120`), o pico do voo normal permanece em 85% do limite, abaixo dos limiares de caution (0,92 e 0,95) das regras de desgaste contínuo.
- **Proteção de temperaturas normais de Óleo e EGT**:
  - Conferidos limites de todos os 46 modelos contra picos normais de óleo (90 °C) e EGT (730 °C).
  - Aplicada proteção para 85% do limite quando o pico normal for `>= 0,90` do limite (`oilScale` para DHC-6 Twin Otter com `oilTempMax = 99,0` e `egtScale` para C400 Corvalis com `egtMax = 782,41`), mantendo escala 1,0 para todos os demais modelos do catálogo. ITT não é gerado pelo voo sintético.
- **Seleção de dente de flap existente (`SyntheticFlapSelector`)**:
  - Nova classe pura `SyntheticFlapSelector` (`FlightCareer.Domain/Replay/Synthetic/SyntheticFlapSelector.cs`): seleciona dente e Vfe com prioridade: 1) dente 2 se o perfil contiver `vfe:2`; 2) menor dente `n >= 2` com `vfe:n` no perfil (A330-200 -> 4, A400M -> 3, ES30 -> 3); 3) dente 1 se contiver `vfe:1`; 4) fallback para dente 2 com `ResolveLimit("vfe", 85)`.
  - Integrada em `SyntheticFlightGenerator` no cálculo de `normalApproachIas`, flap de aproximação e velocidade/dente do abuso `FlapOverSpeed`, garantindo que o `PartWearEngine` localize o limite `vfe:n` correto correspondente ao dente acionado.
- **Testes e Aceitação**:
  - 9 novos testes unitários em `SyntheticGeneratorTemperatureAndFlapTests.cs` cobrindo `ChtScale`, `FlapSelector`, teste de baseline bit a bit para C152 e C172, e conferência de ausência de excedências de temperatura e flap em todos os 46 perfis em voo controle.
  - Fuzz testing em todos os modelos (`fuzz --aircraft all --runs 50`, 2.300 voos): **100% de aprovação (2.300/2.300 voos), 0 falhas e 0 limites ausentes em todos os 46 modelos do catálogo**.
  - Build 0 erros, `dotnet test` 525/525 aprovados. Não testado no MSFS.

## 2026-11-16 - Nota dos passageiros por tempo simulado (Gemini)
- **Desconforto medido por segundos simulados**:
  - `FlightQualityService` passa a receber `ProcessSample(FlightQualitySample sample, bool isPaxFlight, double simSeconds)` medindo o desconforto em segundos acumulados fora dos limites operacionais, multiplicados por `SIMULATION RATE` e ignorando pausas/saltos.
  - Mantida compatibilidade com chamadores legados através de `ProcessFrame(SimTelemetry data, bool isPaxFlight)` marcado como `[Obsolete]` e contagens derivadas `ComfortViolations` / `CargoViolations`.
- **Sinal do pitch corrigido**:
  - `PLANE PITCH DEGREES` do MSFS (negativo com nariz para cima) é invertido em `FlightQualitySample.FromTelemetry` (`PitchUpDeg = -data.PlanePitch`), eliminando o bug de sinal invertido. Desconforto aplicado apenas quando nariz para cima > 20° ou nariz para baixo > 15°.
- **Diferenciação passageiro × carga**:
  - Em voos de carga, manobras de inclinação lateral (bank), arfagem (pitch) e derrapagem (slip) não geram penalidade; apenas aceleração vertical (G) e solavancos (jerk) são contabilizados.
- **Turbulência atmosférica**:
  - Presença de velocidade de vento vertical `WindVerticalFps` (|vento| >= 5 ft/s) reduz o acúmulo de tempo de violação de G e solavanco para 0,25× (`TurbulenceFactor`), reconhecendo que a turbulência não é induzida por pilotagem brusca.
- **Regras configuráveis**:
  - Limites e pesos extraídos para o record `FlightQualityRules`, injetável via construtor (padrão com limites da FAA/IATA).
- **Integração cirúrgica no `FlightMonitor`**:
  - Atualizada chamada no monitor de qualidade para usar o tempo simulado `qualitySimSeconds = isPaused ? 0.0 : SimTimeStep.ToSimSeconds(dt, data.SimulationRate)`.
- **Testes e Aceitação**:
  - 12 novos testes em `FlightQualityServiceTests.cs` cobrindo invariância de taxa de amostragem (1 Hz vs 4 Hz), taxa de aceleração de tempo (4×), pausas, sinal do pitch, distinção de carga, atenuação por turbulência, solavanco/derrapagem opcionais e escala de compatibilidade legada.
  - Build 0 erros, `dotnet test` 537/537 aprovados. Não testado no MSFS.
- Revisão (Claude): penalidade acumulada por amostra com o peso do tipo de voo do momento (`ComfortPenaltyPoints`; antes valia o tipo do último pacote) e comentário de origem dos limites corrigido. 538/538 testes.

## 2026-11-16 - Auto-start tira XP e caminhão de combustível cobrado (Gemini)
- **Grupo de Notificação Observado no SimConnect (`GROUPS.ObservedPilotInputs = 3`)**:
  - `SimConnectService`: adicionado grupo não-mascarável para interceptação passiva de comandos (`AddClientEventToNotificationGroup(..., maskable: false)` com prioridade `SIMCONNECT_GROUP_PRIORITY_DEFAULT`), monitorando `ENGINE_AUTO_START`, `ENGINE_AUTO_SHUTDOWN` e `REQUEST_FUEL_KEY` registrado na inicialização da conexão.
  - Exposto evento `OnObservedEvent` e roteamento discriminado por `e.uGroupID` em `OnRecvEvent` (`ObservedPilotInputs` para `OnObservedEvent`; `MaskedPilotInputs` para `OnMaskedEvent`).
- **Penalidade de Partida Automática (`Domain/Services/ProcedurePenalties.cs`)**:
  - Implementada função pura `ProcedurePenalties.ApplyAutoStartPenalty(xp, autoStartUsed)` com constante `AutoStartXpPenaltyFraction = 0.10` (10% de redução, arredondado para baixo, não negativo).
  - `FlightMonitor`: observa `ENGINE_AUTO_START` durante monitoramento ativo e sinaliza `_autoStartUsed = true` (com log no blackbox); reseta a cada início de voo e propaga para `FlightService.ProcessFlightFinishAsync`.
  - `FlightService`: aplica penalidade ao `totalXP` antes de salvar e anexa o sufixo `" | Partida automática usada (−10% XP)"` à mensagem final.
- **Cobrança do Caminhão de Combustível (`Domain/Fuel/FuelTruckDelivery.cs`)**:
  - Implementada máquina de estados com relógio injetável `FuelTruckDelivery` (`Idle`, `Armed`, `Completed`, `Expired`) com tolerância de subida, detecção de estabilidade por 10 s, cancelamento/expiração por decolagem ou timeout de 10 min, tolerância a consumo/descidas e fechamento antecipado via `ForceComplete`.
  - `FlightMonitor`: ao receber `REQUEST_FUEL_KEY` fora de voo no solo com aeronave vigiada, arma a entrega (`Arm`); atualiza continuamente a cada pacote de telemetria fora de voo; ao concluir (`Completed`), cobra a quantidade entregue ao preço do aeroporto atual do piloto (`FuelPriceService.GetCurrentPrice(fuelType, airportTier)`), cria transação financeira de despesa (`FinancialTransaction` "Combustível"), atualiza `PilotAircraft.FuelAmount` e `pilot.Balance`, notifica alerta no HUD e registra no blackbox.
  - Ao iniciar monitoramento (`StartMonitoring`), se houver entrega armada com subida já medida, executa `ForceComplete` e realiza a cobrança antes da sincronização de combustível, evitando reversão da quantidade nos tanques do sim.
- **Testes e Aceitação**:
  - 14 novos testes unitários e de persistência:
    - `ProcedurePenaltiesTests`: 100 → 90, 0 → 0, sem auto-start → inalterado, 7 → 6 e tratamento de valor negativo.
    - `FuelTruckDeliveryTests`: subida de 20 gal com estabilidade de 10 s, ruído abaixo da tolerância, cancelamento por decolagem, timeout de 10 min, descida ignorada, `ForceComplete` com e sem subida, e relógio injetável com sobrecargas.
    - `FlightPersistenceTests.FlightService_AutoStartUsed_ReducesXpAndMentionsInMessage`: valida redução de XP no logbook (`Flight.XP`), acúmulo no piloto (`pilot.ExperiencePoints`) e inclusão do sufixo na mensagem final.
  - Build 0 erros, `dotnet test` 552/552 aprovados (538 baseline + 14 novos), `TelemetryDefinitionTests` 5/5 aprovados. Não testado no MSFS.
- Revisão (Claude): `OnFuelTruckCharged` faz o `MainViewModel` recarregar saldo e aeronave (contexto longo); cobrança passa a ser `sim final − combustível do app`, aceitando o valor do sim. 552/552 testes. Não testado no MSFS.

## 2026-11-16 - Moeda genérica $ (Claude)
- Decisão do usuário: valores em `$` genérico, base dólar, sem escrever "US$". O app já força a cultura en-US no `App.xaml.cs` (`:C2` → `$1,234.56`); as duas mensagens do caminhão de combustível que escreviam "R$" passam a usar `:C2`. Plano de taxas (`taxas-aeroporto-servicos-solo.md` § 6) atualizado com a decisão e as recomendações ainda a confirmar.

## 2026-11-16 - Taxas de aeroporto F1: catálogo e calculador (Gemini)
- **Catálogo de Taxas (`Infrastructure/Data/airport_fees.json`)**:
  - Parâmetros por tier (1 a 4) e por porte da aeronave (`light` < 12.500 lb, `medium` 12.500–60.000 lb, `heavy` > 60.000 lb): taxa de pouso (`LANDING`), taxa de embarque por passageiro (`PAX_FEE`), taxa de handling/rampa (`HANDLING`), tarifa de navegação/terminal (`NAV_TERMINAL`) e taxa de estacionamento (`PARKING` com franquia de 2 h).
  - Moeda genérica `$`, base dólar, com `countryOverrides` e `icaoOverrides` previstos.
- **Domínio (`FlightCareer.Domain/Economy/`)**:
  - `AirportFeeCatalog`: carregamento e validação de schema JSON (`LoadFromJson`), busca padrão multi-candidato (`LoadDefault`), singleton em cache (`Default`).
  - `AircraftSizeClass` e classificador estático `AircraftSizeClassifier.Classify(mtowLbs)`.
  - `FeeLine` e `FeeContext`: modelos imutáveis de entrada e saída com arredondamento em 2 casas decimais.
  - `GroundServiceCalculator`: cálculo determinístico puro de taxas aeroportuárias (retornando lista vazia para Tier 4 rural/remoto).
- **Integração no `FlightService`**:
  - Substituição da fórmula inline de pouso pelo `GroundServiceCalculator.Calculate`, computando passageiros de missões concluídas no destino e indicador de missão ativa.
  - Geração de `FinancialTransaction` individual para cada serviço com total > 0 (categorias "Taxa de Pouso", "Taxa de Embarque", "Handling", "Navegação/Terminal"), debitando o saldo do piloto.
  - `outcome.LandingFee` mantém o nome por compatibilidade e consolida a soma de todas as taxas aeroportuárias; mensagem final do voo atualizada com `" | Taxas: {total:C2}"` quando houver taxas adicionais além do pouso.
  - `CalculateLandingFee(airport, mtowLbs)` mantido para compatibilidade, delegando ao `GroundServiceCalculator`.
- **Testes e Aceitação**:
  - 24 novos testes unitários em `src/FlightCareer.Tests/Economy/AirportFeeTests.cs` cobrindo paridade com a fórmula legada de pouso por tier × peso, fronteiras de porte, taxa de passageiros, handling, navegação/terminal, estacionamento com franquia, tier 4, voo C152 em hub (~$332), jato comercial em hub (~$10.170) e integridade de carregamento do catálogo.
  - 1 novo teste de persistência em `FlightPersistenceTests.cs` validando execução fim a fim com persistência de transações no SQLite por categoria e mensagem final.
  - Build 0 erros, `dotnet test` 577/577 aprovados (552 baseline + 25 novos). Não testado no MSFS.

## 2026-11-17 - Limites do aircraft_limits-v4 nos perfis (Gemini)
- **Preenchimento de Limites Faltantes e ClassDefault (LIMITES-V4)**:
  - Processado o arquivo `tools/aircraft_limits/sources/aircraft_limits-v4.json` (30 modelos fornecidos pelo usuário, não conferidos no POH real).
  - Mapeamento: 19 modelos com correspondência exata no catálogo (17 com limites faltantes ou ClassDefault atualizados; C152 e C172SP G1000 já 100% POH/SimVar sem alterações), 8 sem correspondência no catálogo (Dreamlifter, A330-900, SF50, XCub tailwheel, ICON A5, VL-3, PC-6, Savage Cub) e 3 com correspondência duvidosa não utilizados (CTSL × CT, CAP 10 × CAP-4, DR400/100 × DR400).
  - 130 novas linhas de limites geradas em `tools/aircraft_limits/poh_values.csv` com referência explícita `"aircraft_limits-v4.json (usuário, 2026-11-17; declara <operational_manual>; não conferido no POH)"`.
  - Consolidação via `merge_limits.py`: 17 perfis atualizados (`737-max-8`, `a320neo`, `a330-belugaxl`, `asobo-tbm-930`, `atr-42-600`, `atr-72-600`, `beechcraft-baron-g58`, `beechcraft-bonanza-g36`, `beechcraft-king-air-c90gtx`, `beechcraft-king-air`, `c208b`, `cessna-citation-cj4`, `diamond-da40ng`, `diamond-da62`, `nxcub`, `pc-12-ngx`, `pilatus-pc-24`), com incremento de `dataVersion`.
  - Nenhum valor existente de fonte POH, SimVar, Cfg ou NativeOnset foi alterado ou sobrescrito.
  - Identificadas 2 divergências > 5% com POHs existentes (mantidos os valores POH originais): King Air B350 (`vne` atual 245 kt vs v4 263 kt, 7,3%) e DA62 (`vle` atual 201 kt vs v4 156 kt, 22,4%).
- **Testes e Verificação**:
  - `dotnet build src/FlightCareer.slnx`: 0 erros.
  - `dotnet test src/FlightCareer.Tests`: 577/577 aprovados (100%).
  - `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`: 920 voos procedurais em 46 aeronaves aprovados (100%), 0 limites ausentes, 0 falhas.
  - `python -m unittest discover -s tools/tests`: 48/48 aprovados.

## 2026-11-17 - Iluminação como peças (Gemini)
- **Grupo de Peças Iluminação e Mapeamento (`PartGroup.Iluminacao`)**:
  - Adicionado o 23º grupo de peças `PartGroup.Iluminacao = "Iluminação"`, mapeado para o componente `Electrical` em `part_component_mapping.json` e no fallback de `PartComponentBridge.CreateDefault()`.
  - 6 peças de lâmpadas halógenas adicionadas em `parts_catalog.json`: `lights.landing` (300 h, $60), `lights.taxi` (300 h, $60), `lights.nav` (1.000 h, $40), `lights.strobe` (1.000 h, $250), `lights.beacon` (1.000 h, $120), `lights.panel` (2.000 h, $50), com `appliesTo: { "none": ["noElectrical"] }`, `visibleOnWalkaround: true`, `minorPart: true`, `failsByUsageRate: true`.
- **Desgaste por Uso e Ciclos de Liga (`Domain/Parts/PartWearEngine.cs`)**:
  - `PartDefinition` e loader de catálogo estendidos com `usageSensor`, `damagePerOnCyclePct`, `failsByUsageRate` e `minorPart`, com validação de obrigatoriedade de `usageSensor` e existência no registro de sensores.
  - O desgaste natural de peças com `usageSensor` corre exclusivamente quando o sensor no frame está ativo (`>= 0,5`), acumulando horas acesas em tempo simulado (`dtSimHours`). Sensor ausente ou desligado não aplica desgaste nem lança exceção.
  - Detecção de ciclo de acendimento (transição 0 → 1) aplicando `damagePerOnCyclePct`, ignorando a primeira leitura do voo.
- **Probabilidade de Pane por Taxa de Uso (`Domain/Parts/UsageFailureModel.cs`)**:
  - Curva de taxa horária $\lambda(h)$ reaproveitando `FailureEngine.HazardPerHour` ($\lambda = 0$ para saúde $\ge 20\%$; crescimento exponencial suave até 2,0 falhas/h em 0%).
  - Sorteio determinístico $p = 1 - e^{-\lambda \cdot dt}$ via `IRandomSource` enquanto a luz está acesa. Saúde $\le 0\%$ enquanto acesa causa queima certa (`IsBroken = true`, `Health = 0`).
  - Emissão de diagnóstico único por voo para o blackbox (`"Lâmpada queimada: <nome>"`). Peça queimada não desgasta mais nem sorteia novas falhas.
- **Isolamento de Barra e Danos (`minorPart`)**:
  - Peças com `minorPart = true` entram apenas na média da agregação de saúde da barra do componente em `PartComponentBridge.AggregateHealth`, sem afetar o mínimo.
  - `PartFlightTracker.DamageGroup` poupa peças `minorPart` de danos originados do componente Elétrico. `ClampGroup` continua atuando normalmente.
- **Avisos da MEL Noturna (`Domain/Parts/LightingMel.cs`)**:
  - Avaliação de lâmpadas queimadas do grupo Iluminação: `lights.nav`, `lights.strobe` e `lights.panel` classificadas como `NoGoNight`; `lights.beacon` classificada como `NoGoNight` se a strobe estiver quebrada/ausente, ou `DayOnly` se a strobe estiver operacional; `lights.landing` e `lights.taxi` classificadas como `DayOnly`.
- **Sensores e Fuzz Testing**:
  - Adicionados 6 sensores elétricos (`light-landing-on`, `light-taxi-on`, `light-nav-on`, `light-strobe-on`, `light-beacon-on`, `light-panel-on`) ao final de `sensor_registry.json` e sincronizados na seção Elétrico de `docs/plans/sensor-inventory.md`.
  - `SyntheticFlightGenerator` preenche os estados realistas das luzes ao longo das fases de voo (táxi, decolagem, cruzeiro, aproximação e corte).
  - `SyntheticFlightOracle` atualizado para verificar horas acesas para peças `lights.*`.
- **Testes e Aceitação**:
  - 17 novos testes em `src/FlightCareer.Tests/Parts/LightingPartsTests.cs`.
  - Build 0 erros, `dotnet test` 594/594 aprovados (577 baseline + 17 novos).
  - Fuzz testing com 920 voos (46 aeronaves × 20 corridas) 100% aprovados, 0 falhas.
  - Sem atuação no simulador (MSFS); não testado no MSFS.

## 2026-11-17 - Calibração do motor, J-3/CAP-4 e remoção da iluminação antiga (Gemini)
- **Calibração do Grupo Motor (`parts_catalog.json`)**:
  - Aplicada a calibração do usuário (decisões de 2026-11-17) às 18 peças do grupo Motor: `engine.cylinders` (250 h, $1.600, 8 h MO, 4–14 h parada), `engine.crankshaft` (350 h, $2.200, 12 h MO, 6–18 h parada), `engine.valves` (150 h, $550, 5 h MO, 3–10 h parada), `engine.carburetor` (180 h, $500, 2 h MO, 3–10 h parada), `engine.fuel_injectors` (200 h, $650, 3 h MO, 3–10 h parada), `engine.turbocharger` (180 h, $1.400, 4 h MO, 4–12 h parada), `engine.starter` (200 h, 150 cic, $320, 1,5 h MO, 2–8 h parada), `engine.coolant_system` (200 h, $680, 3 h MO, 3–10 h parada), `engine.compressor` (500 h, $11.500, 10 h MO, 6–18 h parada), `engine.reduction_gearbox` (400 h, $6.500, 7 h MO, 4–14 h parada), `engine.fan` (600 h, 400 cic, $16.000, 8 h MO, 6–18 h parada), `engine.thrust_reverser` (300 cic, $7.800, 5 h MO, 4–12 h parada; horas removidas), `engine.mounts_trusses` (650 h, $2.800, 6 h MO, 4–12 h parada), `engine.cowling_nacelle` (1.000 h, $3.200, 4 h MO, 3–8 h parada), `engine.air_intake_filter` (80 h, $220, 1 h MO, 1–2 h parada), `engine.pneumatic_starter` (250 cic, $5.600, 3 h MO, 2–5 h parada), `engine.fadec_eec` (800 h, $8.500, 3 h MO, 2–6 h parada) e `engine.apu_core` (450 h, $12.000, 8 h MO, 4–12 h parada).
  - Todas as 18 peças atualizadas com `lifeSource: "calibração do usuário 2026-11-17"`.
- **Remoção de Peça Antiga e Restrições de Features (`parts_catalog.json`)**:
  - Removida a peça redundante `electrical.external_lighting` do catálogo (154 → 153 peças), sendo totalmente substituída pelas 6 lâmpadas halógenas do grupo Iluminação (`lights.*`).
  - Adicionado `"none": ["noElectrical"]` a todas as peças dos grupos Elétrico e Aviônicos, exceto `avionics.analog_flight_instruments` (velocímetro, altímetro e VSI mecânicos de pitot-estática presentes em aeronaves sem sistema elétrico).
  - Adicionado `"none": ["noFlaps"]` à peça `airframe.flaps`.
- **Limpeza no Provisionamento (`AircraftPartProvisioner`)**:
  - `EnsurePartsAsync` agora remove do banco de forma idempotente quaisquer peças instaladas que não existam mais no catálogo ou que deixaram de se aplicar às características do modelo da aeronave (ex.: peças elétricas e flaps em J-3/CAP-4), emitindo log de auditoria via `Debug.WriteLine`.
- **Documentação e Balanceamento**:
  - Atualizado `docs/plans/lista-pecas-hangar.md` refletindo o catálogo consolidado com 153 peças, nova calibração do grupo Motor e contagens limpas para CAP-4 e Piper J-3 Cub (54 peças cada).
  - Atualizadas as faixas-alvo de custo de manutenção simulado em `economy_targets.json` para acomodar os novos valores reais de desgaste/h decorrentes da redução das vidas úteis do motor.
- **Testes e Verificação**:
  - Adicionados testes unitários: `Catalogo_SemExternalLighting`, `Catalogo_NoElectrical_SemPecasEletricas`, `Catalogo_NoFlaps_SemFlap`, `Catalogo_Motor_CalibracaoDoUsuario`, `Provisioner_RemovePecasQueNaoSeAplicam` e `Provisioner_RemovePecaForaDoCatalogo`.
  - `dotnet build src/FlightCareer.slnx`: 0 erros.
  - `dotnet test src/FlightCareer.Tests`: 600/600 aprovados (100%).
  - `fuzz --aircraft all --runs 20`: 920 voos procedurais em 46 aeronaves aprovados (100%), 0 falhas.
  - Não testado no MSFS.

## 2026-11-17 - Exclamação das peças no Hangar após inspeção (Gemini)
- **Sinalização de Peças no Hangar (HANGAR-ICONES)**:
  - Adicionado `Broken = 2` ao enum `PartFlag` e a propriedade `double? FlaggedAirframeHours` em `AircraftPart`, com migração de coluna no SQLite em `AppDbContext.EnsureSchemaUpdatedAsync`.
  - Criada a classe `Domain/Parts/PartInspectionFlags.cs` contendo a lógica pura de avaliação de peças (`Evaluate`), inspeção com escopo (`ApplyInspection`), limpeza pós-reparo (`ClearForKinds`) e visibilidade transitória até o próximo voo (`IsVisible`).
  - Regras de marcação:
    - Vermelha (`PartFlag.Broken`): peça com `IsBroken = true` ("Quebrada").
    - Amarela (`PartFlag.Suspect`): horas acumuladas $\ge 90\%$ de `LifeHours` ("Vida: X / Y h" ou "Vida vencida: X / Y h"); ciclos $\ge 90\%$ de `LifeCycles`; meses $\ge 90\%$ de `CalendarLifeMonths`; ou saúde $< 40\%$ ("Desgaste avançado"). Peças normais e sem aviso recebem `PartFlag.None`.
  - **Interface do Hangar (`ComponentRowViewModel`, `PilotAircraftWrapper`, `HangarView.xaml`)**:
    - Cada card de componente exibe chips horizontais (`WrapPanel`) com ícone de exclamação e nome da peça (+ `(motor n)` se `perEngine` em aeronave multimotor) em cores correspondentes (amarelo `#FFB300` e vermelho `#FF5252`), com `ToolTip` contendo o motivo exato. Lista vazia não ocupa espaço visual.
    - O Hangar carrega as peças do avião selecionado em contexto de curta duração e filtra apenas peças visíveis (`IsVisible`), garantindo que o alerta permaneça até a realização do próximo voo (quando a aeronave acumula horas).
  - **Inspeções e Serviços no Hangar (`HangarViewModel`)**:
    - Inspeção de 50 h: avalia peças dos componentes Motor e Óleo.
    - Inspeção de 100 h: avalia todas as peças da aeronave.
    - Revisão de 500 h: avalia peças do componente Ignição.
    - Walkaround: avalia peças marcadas com `VisibleOnWalkaround`.
    - Mensagens de sucesso das inspeções ganham o sufixo `" — N peça(s) com atenção"` quando $N > 0$.
    - Reparo de componente e Overhaul limpam as sinalizações das peças dos componentes reparados (`ClearForKinds`).
- **Testes e Aceitação**:
  - 12 novos testes unitários em `src/FlightCareer.Tests/Parts/PartInspectionFlagsTests.cs`.
  - 1 novo teste de migração e persistência em `src/FlightCareer.Tests/Components/ComponentPersistenceTests.cs` (`Banco_antigo_sem_FlaggedAirframeHours_recebe_coluna_e_grava_peca`).
  - Build 0 erros, `dotnet test` 607/607 aprovados (594 baseline + 13 novos).
  - Fuzz testing com 230 voos (46 aeronaves × 5 corridas) 100% aprovados, 0 falhas.
  - Não testado visualmente na UI WPF em execução (verificação estática e por testes unitários/integração).

## 2026-11-17 - Simulador de carreira com bots (Gemini, revisado pelo Claude)
- Novo `Application/Simulation/CareerSimulator` e subcomando `FlightCareer.Replay career`: bots aceitam missões reais do `MissionGenerator`, voam (desgaste por voo sintético ou `--fast`), pagam combustível, taxas de aeroporto (`GroundServiceCalculator`), seguro e manutenção com os preços do Hangar; gera `flights.csv`, `maintenance.csv`, `daily.csv` e `summary.md` (receita/h, custos/h, margem, horas para pagar o avião, frequência de manutenção e panes).
- `MaintenancePricing` (Domain) passa a guardar os preços de inspeção/retífica antes fixos em `PilotAircraftWrapper` (mesmos valores). `MissionGenerator` aceita aleatoriedade e data de referência injetáveis (ordem estável das consultas); `FuelPriceService` aceita data.
- Não muda nenhum valor de economia. Execução completa (8 aviões × 5 bots × 90 dias) não terminou em 1 h: rodar em lotes menores. Moeda do relatório em `$` (cultura en-US).

## 2026-11-17 - Ícones dos grupos no Hangar (Claude)
- Os cards de componentes do Hangar trocam a barra de saúde por um **ícone vetorial por grupo** (motor, óleo, ignição, seção quente, elétrico, instrumentos, trem, pneus, freios, célula), desenhado em `UI/ViewModels/HangarIcons.cs` (sintaxe de path SVG, 24×24, traço) e pintado no tom da saúde do grupo (`HangarPalette.ForHealth`, mesmas faixas verde/amarelo/vermelho da barra antiga). A faixa de saúde continua no texto ao lado e no tooltip do ícone; os chips das peças marcadas na inspeção seguem dentro do card.
- Não visto na tela (sem rodar a UI).

## 2026-11-19 - Moeda $ no thread da UI (Claude)
- A cultura en-US era aplicada dentro do `async void OnStartup`; no .NET a cultura atual segue o contexto assíncrono, então depois do primeiro `await` o thread da UI voltava à cultura do Windows e os valores `:C` apareciam como "R$ 1.200" (Hangar, conversão de cabine, reparos). Agora é aplicada no construtor do `App`, antes de qualquer código assíncrono. Não conferido na tela.

## 2026-11-19 - Calibração do usuário, preço por classe, TBO 500/900 (Gemini)
- **Calibração do usuário (`parts_catalog.json`)**:
  - Aplicada a calibração com fator de compressão regulatória ÷ 4 a todas as 153 peças do catálogo (`calibracao-pecas-usuario-2026-11-19.json`), cobrindo `lifeHours`, `lifeCycles`, `calendarLifeMonths` (null removido/adicionado), `partPrice`, `priceScalesWithAircraft`, `laborHours`, `downtimeHours`, `blocksFlightWhenBroken` (NO-GO), `visibleOnWalkaround` (WA), `failureEffect` e `lifeSource` = "calibração do usuário 2026-11-19 (real ÷ 4)".
  - Restrito `engine.air_intake_filter` a `{ "all": ["piston"] }`.
  - Suporte completo no carregador e motor de desgaste a peças com vida exclusivamente por ciclos ou calendárica sem divisão por zero nem exceção de parsing.
- **Precificação por classe econômica (`Domain/Economy/AircraftEconomyClass`, `Domain/Parts/PartPricing`)**:
  - Mapeamento centralizado de classes econômicas (Classes A a J) e resolução a partir de motorização, quantidade de motores, features e preço.
  - Multiplicadores de preço por classe: A 0,75 · B 1,25 · C 1,25 · D 1,25 · E 1,5 · F 1,75 · G 1,75 · H 2,5 · I 3,25 · J 5,5 gravados em `economy_targets.json`.
  - Cálculo de preço em `PartPricing.CalculatePartPrice`: se `PriceScalesWithAircraft`, preço = `partPrice × multiplicador da classe`; senão `partPrice`. Removido uso de `repairCostPctOfAircraft` e `GetClassPartPriceCap`.
  - `PartEconomySimulator`, `CareerSimulator` e testes adaptados para o modelo de multiplicador por classe.
- **TBO e Inspeções periódicas**:
  - TBO unificado em 500 h para motores a pistão e 900 h para turbinas (`component_catalog.json`, `PilotAircraft`, `MarketSimulationService`, `MarketOffer`, `CreatePilotViewModel`).
  - Migração de banco no `AppDbContext.EnsureSchemaUpdatedAsync` atualizando `EngineTboRemainingHours > 500.0` para 500.0 h em frotas existentes de pistão.
  - Inspeções com fator de compressão ÷ 4: troca de óleo 12 h ($212,50), inspeção anual 25 h ($625,00) e revisão de ignição 125 h ($362,50). Retífica a 5% do valor da aeronave (mínimo $6.250). Textos do Hangar e simuladores alinhados.
- **Limites de POH**:
  - Diamond DA62: `vle` = 156 kt (POH/AFM do usuário 2026-11-19), dataVersion 8.
  - Beechcraft King Air: `vne` = 263 kt (POH/AFM do usuário 2026-11-19), dataVersion 8.
  - `tools/aircraft_limits/poh_values.csv` atualizado para ambos os modelos.
- **Documentação e Testes**:
  - `docs/plans/lista-pecas-hangar.md` regenerado com os 153 componentes e contagens por modelo.
  - Novos testes unitários: `PartPricing_MultiplicadorPorClasse`, `Catalogo_CalibracaoDoUsuario`, `Tbo_Pistao500_Turbina900` e atualização de `Catalogo_Motor_CalibracaoDoUsuario`, `MaintenancePricing_MesmosValoresDaUI`, `PartEconomyBalanceTests` (ambos verdes dentro das faixas originais de meta econômica).
  - Build 0 erros, `dotnet test` 624/624 aprovados (100%), fuzz testing em 46 aeronaves (230 voos) 100% aprovados sem violações.

## 2026-11-19 - Calendário de jogo (Gemini)
- **Relógio de Carreira Simulada (`Domain/Services/CareerClock.cs`)**:
  - Implementado o relógio de carreira por piloto (`Pilot.CareerClockUtc`), desvinculando prazos e seguros do relógio do Windows para não penalizar jogadores que passam semanas sem abrir o simulador.
  - Regras de avanço puras em `CareerClock`:
    - Fim de voo ou cancelamento ativo: avança pela duração simulada do voo na célula + 0,5 h de solo/turnaround (`AdvanceFlight`).
    - Paradas de oficina no Hangar: avança pelo tempo de indisponibilidade (`downtimeHours` das peças reparadas ou 2,0 h padrão; inspeção 50h/12,5h acelerada → 2 h; inspeção 100h/25h acelerada → 8 h; revisão 500h/125h acelerada → 6 h; retífica de motor/TBO → 72 h).
    - O relógio nunca avança com o simulador ou aplicativo fechados.
- **Integração com Peças e Inspeções (`Domain/Parts/PartInspectionFlags.cs`)**:
  - Parâmetro de data renomeado para `careerNow` em `Evaluate` e `ApplyInspection`.
  - Avaliação de vida por meses (`CalendarLifeMonths`) agora compara o tempo transcorrido a partir de `part.LastReplacedAtUtc` contra o relógio de carreira.
  - Substituições e provisionamento de peças gravam `LastReplacedAtUtc` usando o relógio de carreira.
- **Seguro Aeronáutico e Interface do Hangar (`HangarViewModel`, `PilotAircraftWrapper`, `HangarView.xaml`)**:
  - A contratação e renovação de apólice de 30 dias de seguro (`InsuranceService.CalculateNewExpirationDate`) e a verificação de cobertura (`InsuranceService.IsInsured`, `PilotAircraftWrapper.IsInsured`) usam estritamente o relógio de carreira.
  - O cabeçalho de frota do Hangar exibe discretamente a data atual de carreira (`Dia da carreira: dd/MM/yyyy`).
- **Banco de Dados e Persistência (`AppDbContext.EnsureSchemaUpdatedAsync`)**:
  - Adicionada coluna `CareerClockUtc TEXT NULL` na tabela `Pilots` com backfill automático para a data atual em bancos legados.
- **Testes e Aceitação**:
  - 5 novos testes unitários em `src/FlightCareer.Tests/Services/CareerClockTests.cs`.
  - 1 novo teste de migração de banco legado em `src/FlightCareer.Tests/Components/ComponentPersistenceTests.cs`.
  - Build 0 erros, `dotnet test` 618/618 aprovados.
  - Fuzz testing executado em 46 modelos (230 voos) com 100% de aprovação e 0 falhas.
  - Não testado no MSFS.

## 2026-11-19 - Simulação de estabilidade da carreira (Gemini)
- **Extração da Inicialização de Carreira (`Domain/Services/CareerStart.cs`)**:
  - Centralizado o saldo inicial por dificuldade (`Easy` $1.000.000.000, `Medium` $100.000, `Hard` $10.000) e parâmetros iniciais da aeronave (C152 com 5.000 h de célula, meio tanque, TBO 1.800 h, 50 h para próxima inspeção 50h, 100 h para inspeção 100h).
  - Atualizado `CreatePilotViewModel` para consumir `CareerStart`.
- **Avaliação Pura de Mercado (`Domain/Economy/AircraftValuation.cs`)**:
  - Extraído o método estático e puro `CalculateBaseMarketValue`, eliminando dependências de banco e permitindo avaliações rápidas em memória.
- **Geração de Missões em Memória (`MissionGenerator.GenerateMissionsInMemory`)**:
  - Suporte completo a geração de contratos em memória com cache por aeroporto/dia, desacoplado de `DbContext` e sem chamadas a `SaveChangesAsync`.
- **Simulador de Progressão e Estabilidade (`Application/Simulation/CareerProgressionSimulator.cs` e CLI `career-stab`)**:
  - Modelação de carreira completa com progressão por escada de aeronaves (Classes A→B→E→F→G→H→I→J), respeito às homologações de categoria do piloto (Categorias 1 a 5), políticas cauteloso/equilibrado/agressivo com reservas de caixa operacionais (3 meses, 1 mês ou 0), venda com 8% de taxa de corretagem, quebras e panes via `FailureEngine`, inflação de 10 anos sobre o combustível e detecção estrita de falência.
  - Relatório `stability.md` com tabela detalhada por marco (Dias 30, 180 e 720), alertas automáticos (falência precoce, enriquecimento rápido, estagnação, inflação come margem) e exportação tabular `stability.csv`.
- **Comando CLI**:
  - Adicionado subcomando `career-stab` ao `FlightCareer.Replay`, com salvamento em `%LocalAppData%/FlightCareer/career-sim/`.
- **Testes e Aceitação**:
  - 5 novos testes unitários em `src/FlightCareer.Tests/Economy/CareerProgressionTests.cs` (CareerStart, determinismo estrito, reserva de compra, falência e restrição de categorias).
  - Build 0 erros, `dotnet test` 623/623 aprovados.
  - Fuzz testing executado em 46 modelos (230 voos) com 100% de aprovação e 0 falhas.

## 2026-11-19 - Reparo por troca de peça (Gemini)
- **Planejador de Troca de Peças (`Domain/Parts/PartReplacementPlanner.cs`)**:
  - Nova lógica pura de substituição física de peças inteiras no Hangar a preço integral + mão de obra: na aviação real não se troca 5 % de freio, troca-se a peça física inteira (100 %) pagando preço cheio.
  - Critérios de elegibilidade: peças com `Health < 80.0`, `IsBroken == true`, ou sinalizadas em inspeção (`PartInspectionFlags.Evaluate(...).Level != None` ou flag ativa).
  - Custo = soma de (preço cheio da peça + mão de obra) por instância física (peças por motor contam individualmente).
  - Parada na oficina = maior tempo de downtime entre as peças trocadas (oficina em paralelo).
  - Ao aplicar (`Apply` / `ApplyToParts`): peça volta a 100 % de saúde, `IsBroken` = false, horas e ciclos zerados, contadores `LastReplaced*` e `LastShopService*` atualizados e marcas de inspeção limpas.
- **Hangar e MVVM (`HangarViewModel.cs`, `ComponentRowViewModel.cs`, `PilotAircraftWrapper.cs`)**:
  - Para aeronaves com peças (`PartComponentBridge.HasParts`): o custo e texto do botão de reparo de cada linha vêm do plano (`"Trocar N peças · $X"` ou `"Em dia"` se nenhuma peça do grupo estiver gasta).
  - Diálogo de confirmação lista cada peça a trocar, seu motivo específico ("Saúde X %", "Vida: X / Y h", "Quebrada") e o custo.
  - Ao confirmar: cobra o valor do plano, aplica a substituição física apenas nas peças do plano, recalcula a barra do componente diretamente pelo agregado (`SyncComponentsFromParts`), garantindo barra = agregado (eliminando qualquer reconciliação gratuita no voo seguinte), e avança o relógio de carreira pela parada do serviço.
  - "Reparar tudo" consolida os planos de todos os grupos com peças gastas e aplica o maior downtime entre todos os itens.
  - Retífica (overhaul) e troca de óleo/revisões mantêm os preços dos pacotes e aplicam a troca física a 100 % em todas as peças dos respectivos grupos (motor, seção quente, óleo, ignição).
  - Aeronaves sem peças mantêm 100 % da compatibilidade com o comportamento legado.
- **Testes e Verificação**:
  - Nova suíte de testes unitários em `src/FlightCareer.Tests/Parts/PartReplacementPlannerTests.cs` cobrindo todos os cenários da spec: freio sem peça gasta ("Em dia"), peça < 80 % trocada a preço cheio, vida vencida, peça quebrada, peças por motor contando instâncias, parada máxima em paralelo, zeramento de horas/ciclos na aplicação, barra = agregado pós-troca sem reconciliação gratuita, e overhaul restaurando todas as peças do motor/óleo.
  - `dotnet build src/FlightCareer.slnx`: 0 erros.
  - `dotnet test src/FlightCareer.Tests`: 639/639 aprovados (100 %).
  - Fuzz testing CLI (`fuzz --aircraft all --runs 5`): 230 voos em 46 modelos aprovados com 100 % e 0 falhas.
  - Não testado na tela do app nem no MSFS.

## 2026-11-19 - ECON-STAB: Desgaste e manutenção de peças na progressão de carreira (Gemini)
- **Desgaste e Vida de Peças em Carreira (`CareerProgressionSimulator.cs`)**:
  - Peças instaladas por bot provisionadas pelo catálogo com suporte a `perEngine` e features técnicas do modelo.
  - A cada voo no modo `--fast`: desgaste natural a `(70 / lifeHours) × horas × 1.15`, acúmulo de horas e ciclos; lâmpadas halógenas condicionadas ao `usageSensor` (nav/strobe/beacon 100%, pouso/táxi 15%, painel 0%).
  - Barra dos componentes sincronizada com o agregado das peças (`PartComponentBridge.AggregateHealth`) antes do `FailureEngine.Evaluate`.
  - Danos de panes roteados para as peças físicas do grupo do modo de falha (`DamageGroup`).
  - Interdição NO-GO por panes ou quebra de peças que bloqueiam voo (`BlocksFlightWhenBroken`), resolvida via substituição de peças.
- **Manutenção pelas Regras do Hangar (`PartReplacementPlanner`)**:
  - `cauteloso` e `equilibrado`: plano completo (`PartReplacementPlanner.Plan`) na inspeção anual (25 h / 100 h), substituindo peças < 80% ou com alertas a preço cheio; troca de óleo e filtro a cada 12 h / 50 h pagando peças e taxa de serviço.
  - `agressivo`: adia manutenções preventivas; ao ficar parado, substitui apenas peças estritamente quebradas ou NO-GO; falência imediata se houver peça NO-GO quebrada com saldo negativo e sem fundos para troca.
- **Relatórios Consolidados (`stability.md` e `stability.csv`)**:
  - Adicionadas métricas de custo de troca de peças por hora (`Peças/h ($)` e `custo_pecas_hora`), peças trocadas por 100 h (`Peças/100h` e `pecas_trocadas_100h`) e tabela de detalhamento dos custos operacionais por hora (combustível, taxas, troca de peças, manutenção geral, seguro).
- **Testes**:
  - Novo teste `Progression_DesgasteETrocaDePecas_MedidosComSucesso` em `CareerProgressionTests.cs`.
  - Build 0 erros, `dotnet test` 645/645 aprovados, `fuzz --aircraft all --runs 5` (230 voos em 46 modelos) 100% aprovados.

## 2026-11-19 - Conversão de cabine (Gemini)
- **Feature `convertibleCabin` no vocabulário fechado (`AircraftFeatures.cs`, `_schema.json`)**:
  - Nova característica `AircraftFeatures.ConvertibleCabin = "convertibleCabin"` adicionada ao vocabulário fechado e ao enum de `_schema.json`.
  - Atribuída aos 11 modelos conversíveis da especificação em `Data/aircraft/*.json`: `c208b`, `pc-12-ngx`, `beechcraft-king-air`, `beechcraft-king-air-c90gtx`, `beechcraft-king-air-250`, `dhc-6-300-twin`, `atr-42-600`, `atr-72-600`, `cessna-408-skycourier`, `cessna-404-titan` e `cessna-207t-stationair`. Demais aeronaves sem a feature têm o card e o chip de cabine ocultados no Hangar.
- **Economia e Operação de Cabine (`Domain/Economy/CabinConversion.cs`)**:
  - Nova lógica pura de regras e cálculos de conversão de cabine sem dependência de UI ou banco.
  - Custo e tempo de parada (downtime) por porte da aeronave (`AircraftSizeClass` / MTOW): Leve ($800 / 4 h), Médio ($3.000 / 8 h) e Pesado ($12.000 / 24 h), avançando o relógio de carreira (`CareerClock`).
  - Cálculo de capacidade efetiva de passageiros e carga (`CabinCapacity`):
    - Configuração Passageiro: passageiros até `PaxMax`, carga limitada a 25% de `CargaMax` (bagageiro).
    - Configuração Carga: passageiros = 0, carga expandida até `CargaMax + (PaxMax × 170 lb)` com teto máximo de `CargaMax × 1,3`.
  - Validador centralizado de compatibilidade de missão (`IsMissionCompatible`): na configuração Carga, rejeita qualquer missão com passageiros; na configuração Passageiro, rejeita missões de carga com peso superior ao limite de bagagem e missões com passageiros excedendo o número de assentos ou carga de bagageiro.
- **Filtros e Travas de Missão (`CareerSimulator.cs`, `MissionDisplayItem.cs`, `MainViewModel.cs`)**:
  - Simulação de carreira dos bots (`CareerSimulator`): filtro de missões compatíveis agora usa `CabinConversion.IsMissionCompatible`, impedindo aceitação de passageiros em cargueiros e carga excessiva em aviões de passageiros.
  - Quadro de contratos (`MissionDisplayItem`): `IsOverweight` avaliado contra a capacidade da cabine ativa, permitindo ao filtro "Carga Compatível" ocultar automaticamente contratos incompatíveis.
  - Validação de aceite e início de voo (`MainViewModel.AcceptSelectedMissionsAsync` e `StartFlight`): alertas informativos e bloqueio de decolagem caso o manifesto selecionado contenha passageiros em avião cargueiro ou carga acima do limite de bagagem em avião de passageiros.
- **Hangar e Interface (`PilotAircraftWrapper.cs`, `HangarViewModel.cs`, `HangarView.xaml`)**:
  - `PilotAircraftWrapper`: expõe `HasConvertibleCabin`, `CostToggleConfiguration`, `DowntimeToggleConfiguration`, `TextToggleConfiguration` (ex: "Converter para Carga · $3,000 · 8 h") e `ConfigurationDisplayText` (ex: "Carga: até 4.797 lb").
  - `HangarViewModel`: comando `ToggleConfigurationCommand` valida a presença da feature `convertibleCabin`, executa o serviço pago e passa `downtimeHours` para avanço do `CareerClock`.
  - `HangarView`: card de configuração de cabine e chip de cabine do cabeçalho vinculados a `HasConvertibleCabin` (somem se a aeronave não suportar conversão).
- **Testes e Verificação**:
  - Nova suíte de testes unitários `CabinConversionTests.cs` cobrindo: `Cabin_SemFeature_NaoConverte`, `Cabin_CustoEParadaPorPorte`, `Cabin_CapacidadeCarga`, `Cabin_CapacidadePassageiro`, `Missions_ConfigCarga_SemPassageiros`, `Missions_ConfigPassageiro_CargaLimitada` e `Missions_AviaoNaoConversivel_RegraAntiga`.
  - Teste de integração `Seeder_UpdatesAircraftType_HasConvertibleCabin` em `SeederTests.cs` validando sincronização idempotente de `AircraftType.HasConvertibleCabin`.
  - `dotnet build src/FlightCareer.slnx`: 0 erros.
  - `dotnet test src/FlightCareer.Tests`: 647/647 aprovados (100%).
  - Não testado na tela do app nem no MSFS.

## 2026-11-19 - ECON-STAB: Continuidade de voos, diversificação de bots e validação de carreira (Gemini)
- **Continuidade de voo dos bots e voo de reposicionamento (Ferry)**:
  - Eliminação da paralisação de bots em aeródromos sem contratos compatíveis.
  - Voo de reposicionamento (Ferry, sem receita, pagando combustível e tarifas regulatórias) para o Hub mais próximo (Tier 1 ou 2 com pista compatível com a aeronave) quando não houver missões disponíveis.
  - Voo de instrução/treinamento local para escolas de aviação (CIAC) quando o bot já estiver no Hub e não houver contratos comerciais no dia (1,0 h a $220/h de receita cobrindo combustível e taxas).
  - Continuidade do loop diário de voos até o cumprimento da meta configurada (`options.HoursPerDay`), elevando as horas voadas para o ritmo real de operação (~85 h a 116 h em 60 dias).
- **Sementes individuais e diversificação de rotas**:
  - Método determinístico `CareerProgressionSimulator.DeriveBotSeed(...)` combinando semente base, dificuldade, política e índice do bot.
  - Cache de missões por chave de aeroporto e dia `(ICAO, dia)` gerado com semente independente de quem o visita primeiro.
  - Seleção estocástica ponderada entre as top 3 missões compatíveis, gerando trajetórias e patrimônios distintos para cada bot (faixas [Mín - Máx] reais).
- **Escada de compra de aeronaves e categorias**:
  - Resolução de configuração de aeronaves `ResolveAircraftConfiguration` tornada estática.
  - Promoção de categoria (Cat 1 ➔ Cat 2) atingida aos 40 h de voo e 2.500 XP com pagamento da taxa de homologação ($35.000).
  - Motivo de bloqueio de compra informado explicitamente nos relatórios quando a categoria ou saldo com reserva impedem a aquisição.
- **Métricas e sanidade no relatório (`stability.md`, `stability.csv`)**:
  - Adicionadas colunas `Horas Voadas` e `Voos/Dia` nas tabelas consolidadas.
  - Alerta automático `Bot Parado` ativado caso algum bot ativo registre média inferior a 50 % da meta diária configurada (0 alertas na amostra).
- **Testes**:
  - Novos testes unitários `Progression_BotVoaHorasPorDiaAoLongoDoTempo` e `Progression_BotsComSementesDiferentes` em `CareerProgressionTests.cs`.
  - Build 0 erros, `dotnet test` 655/655 aprovados (100 %), `fuzz --aircraft all --runs 5` 230/230 voos aprovados (100 %) em 46 aeronaves.

## 2026-11-20 - MANUT-FAIXAS-A: Manutenção por faixas de saúde, panes por peça e novo seguro (Gemini)
- **Faixas de saúde por peça (`PartHealthBands.cs`, `component_catalog.json`)**:
  - Classificação das peças em três faixas de saúde: Verde [100, 40], Amarela [40, 20) e Vermelha [20, 0].
  - Desgaste acelerado progressivo na faixa amarela (`PartWearEngine`): fator `1 + ((40 - h) / 40)^2` (até 2× em 20%) aplicado ao desgaste natural e lâmpadas acesas antes do multiplicador de estresse.
  - Taxa base de risco de falhas por peça na faixa vermelha (`HazardPerHour`): zero acima de 20%, subindo para 2,0 falhas/hora em 0% de saúde.
- **Sorteio e aplicação de panes por peça (`PartFailureEngine.cs`, `ComponentFlightSession.cs`, `FlightMonitor.cs`)**:
  - Em aeronaves com peças (`PartsDriven = true`), as panes são avaliadas por peça via `PartFailureEngine.Evaluate`, respeitando no máximo uma pane por `ComponentKind` por voo.
  - Reutilização do sorteio de modo ponderado por peso (`FailureEngine.PickMode`) e roteamento para quebra da peça (`PartBreakSink`), acionando os atuadores do simulador (`ApplyMode`) com identificação nominal da peça e saúde anterior.
- **Planejador de manutenção e inspeções (`PartReplacementPlanner.cs`, `PartInspectionFlags.cs`, `PartKnowledge.cs`)**:
  - `PartReplacementPlanner.Plan(selectedParts)` precifica estritamente as peças fornecidas.
  - `SelectRecommended` seleciona apenas peças quebradas ou na faixa vermelha (< 20%) para o "Reparar tudo" do Hangar.
  - `PartInspectionFlags.Evaluate` sinaliza exclusivamente pela faixa de saúde (`Suspect` para amarela, `Broken` para vermelha ou quebrada). Vida vencida não sinaliza e vira anotação descritiva informativa (`LifeNote`).
  - Precisão do walkaround ajustada para `0.0` (exibe valor exato no catálogo e Hangar).
  - Inspeção anual (25 h) revela todas as peças (`PartKnowledge.Reveal`), registra defeitos não corrigidos em `FoundRedAtAnnualAirframeHours` (`PartKnowledge.MarkAnnualFindings`), zera o contador de 25 h e limpa interdição. **Não troca óleo** nem zera o contador de 12 h.
- **Fim da interdição por TBO / Óleo / Ignição (`FlightService.cs`, `FerryFlightViewModel.cs`, `HangarViewModel.cs`)**:
  - Apenas a Inspeção Anual vencida interdita a aeronave no `FlightService` e no `FerryFlightViewModel`. TBO, óleo e ignição vencidos não bloqueiam operações nem translado.
  - `HangarViewModel.ClearInterdictionIfResolved` exige apenas anual em dia. Aeronaves legadas interditadas por TBO são desinterditadas no carregamento da frota.
- **Seguro aeronáutico mais barato e condicionado (`EconomyServices.cs`, `HangarViewModel.cs`)**:
  - Prêmio anual ajustado de 12% para 2,5% ao ano (`MONTHLY_RATE = 0.025 / 12`), piso mínimo reduzido de $250 para $50/mês.
  - Sinistro de perda total negado (`InsuranceService.ClaimDenied`) caso a aeronave tenha voado com defeito apontado na inspeção anual e não corrigido (`FoundRedAtAnnualAirframeHours != null`).
- **Simulador de progressão de carreira (`CareerProgressionSimulator.cs`)**:
  - Desgaste por faixas e avaliação de panes por peça (`PartFailureEngine`) nos voos locais e translados.
  - Políticas de manutenção adaptadas: `cauteloso` faz walkaround antes de cada voo e troca peças em faixas amarela e vermelha; `equilibrado` troca peças vermelhas e quebradas e respeita inspeções regulatórias; `agressivo` pula revisões preventivas até a interdição por anual e só troca peças quebradas.
  - Validação estatística: política agressiva registra taxa de panes/100h comprovadamente superior à política cautelosa na mesma semente.
- **Persistência (`AircraftPart.cs`, `AppDbContext.cs`, `ComponentPersistenceTests.cs`)**:
  - Adicionadas colunas `KnownHealth`, `KnownAtAirframeHours` e `FoundRedAtAnnualAirframeHours` à tabela `AircraftParts` de forma idempotente em `EnsureSchemaUpdatedAsync`.
- **Testes e Verificação**:
  - Nova suíte `PartHealthBandsAndFailuresTests.cs` (16 testes cobrindo faixas, desgaste acelerado, hazard, isolamento de panes, planner, flags, inspeção anual, seguro e interdições de serviço).
  - Teste de persistência e schema evolution `Banco_antigo_recebe_novas_colunas_de_aircraft_parts`.
  - Teste de comparação de políticas `Agressivo_PulaRevisoes_TemMaisPanesQueCauteloso` em `CareerProgressionTests.cs`.
  - Build: 0 erros. Suíte completa de testes xUnit: 673/673 aprovados (100%).

## 2026-11-20 - Revisão MANUT-FAIXAS-A e pesquisa de perda de desempenho (Claude)
- Revisão aprovada com ajustes: o simulador de carreira deixou de cobrar em dobro as peças da troca de óleo, revisão de ignição e retífica (o pacote já inclui as peças, como no Hangar); `InsuranceService.ClaimDenied` recebe o catálogo para o nome da peça (sem `catch` silencioso); `ComponentFlightSession.Reset` limpa as panes do voo.
- `docs/plans/perda-desempenho-amarelo.md`: o que o SDK permite gravar para a perda de desempenho na faixa amarela e roteiro de teste; primeiro resultado no MSFS (`GENERAL ENG OIL LEAKED PERCENT` parece ser óleo restante — provável bug no vazamento do app). Não testado no MSFS: pane por peça em voo.

## 2026-11-21 - Limpeza do vazamento de óleo não esvazia mais o motor (Claude)
- `SimFailureActuator.Reset(clearSimState: true)` (chamado em todo início de monitoramento e ativação do voo) só grava `GENERAL ENG OIL LEAKED PERCENT:1 = 0` quando o próprio app criou o vazamento. Antes gravava 0 sempre que o sim informava valor > 0; no MSFS 2024 a SimVar parece valer 100 sem pane (probe `20261120-0822-C172SP_G1000_Passengers`), o que esvaziaria o óleo a cada voo.
- Correção mínima e segura para o teste de voo. A semântica da SimVar no vazamento ativo (`ProcessOilLeak`, `TransmitOilLeak`) continua a confirmar e inverter (HANDOFF › item 3). Não testado no MSFS.

## 2026-11-21 - Lista de peças no Hangar e troca individual (Gemini)
- **Visualização de peças por componente (`PartHangarView.cs`, `ComponentRowViewModel.cs`, `HangarView.xaml`)**:
  - Implementado `PartHangarView` com `PartKnownBand` (Broken, Red, Yellow, Unknown, Green), gerando linhas de visualização (`PartHangarRow`) baseadas exclusivamente na saúde conhecida (`KnownHealth`), tempo desde a última inspeção (`KnownAtAirframeHours`), nota de vida útil (`PartInspectionFlags.LifeNote`) e formatação nominal por motor.
  - Cards de componente no Hangar agora contêm um `Expander` "Peças (n)" fechado por padrão com listagem detalhada de cada peça, indicador de faixa colorido, nota de vida e botão para troca individual a preço integral.
  - Paleta de cores do Hangar ajustada para as faixas de saúde 40/20 (`< 20` Red, `< 40` Amber, senão Green).
- **Troca individual de peças (`HangarViewModel.cs`, `PilotAircraftWrapper.cs`)**:
  - Novo comando `ReplacePart` no Hangar que orça e substitui uma peça individual a preço de tabela + mão de obra, zerando horas/ciclos, restaurando a saúde para 100% e sincronizando com as barras agregadas sem conceder reset de TBO.
- **Proteção de integridade regulatória (correção de reset de TBO / 125 h)**:
  - Removido `ApplyRegulatoryReset` dos caminhos com peças de `RepairComponent` e `RepairAll`. A troca de componentes avulsos do motor ou ignição (arranque, velas, coletores) não restaura mais indevidamente o TBO ou a revisão de 125 h, preservando a exclusividade dos serviços dedicados de overhaul e revisão.
- **Recomendação e motivos baseados no conhecimento do piloto (`PartReplacementPlanner.cs`)**:
  - Novo método `SelectRecommendedKnown` selecionando apenas peças quebradas ou com `KnownHealth < 20%`.
  - Motivo de substituição no `Plan` atualizado para reportar a saúde conhecida ou escolha do piloto, eliminando qualquer vazamento de saúde real antes da inspeção.
- **Testes**:
  - Nova suíte `PartHangarViewTests.cs` com 11 testes cobrindo todas as regras de visualização, ordenação por gravidade, faixas e motivos conhecidos.
  - Build 0 erros, suíte xUnit 684/684 testes aprovados (100%).

## 2026-11-21 - Sincronização do combustível com o sim corrigida (Claude)
- Causa do erro "Não foi possível sincronizar o combustível com os tanques do simulador" no primeiro voo real (C152): no MSFS 2024 a escrita de `FUELSYSTEM TANK QUANTITY:n` define a quantidade **total** do tanque, mas a leitura devolve a **utilizável**. O app gravava 6,3 gal por tanque, o sim ficava com 5,55 (0,75 gal não utilizável por tanque) e as 3 tentativas falhavam.
- `SimFuelState` e a definição de leitura ganham `FUELSYSTEM TANK CAPACITY:1..8` no fim; `FuelTank` ganha `UnusableGallons` e `ToSimWriteValue`; `SetFuelTanks` soma a parte não utilizável de cada tanque. Corrigido também o nome do tanque de ponta esquerdo legado (`FUEL TANK LEFT TIP QUANTITY`) e o `catch {}` silencioso do `WndProc`.
- **Testado no MSFS 2024 (C152)** com um teste de console que usa o mesmo struct e a mesma distribuição do app: alvo 12,60 gal → sim 12,60 gal. Escrita dos tanques legados não testada.

## 2026-11-21 - Gravação de combustível no sistema legado testada (Claude)
- **Testado no MSFS 2024 (TBM 930, sistema legado):** gravar `FUEL TANK LEFT/RIGHT MAIN QUANTITY` com a distribuição do app levou o total de 150,5 a 160,5 gal (diferença 0) e voltou a 150,5. No legado a leitura e a escrita são do total com o não utilizável (9 gal no TBM), então a correção do sistema moderno não se aplica e não é necessária.
- Levantamento do sistema de combustível dos 25 aviões com log de calibração registrado no `AGENTS.md` › 6 (8 legados, 17 modernos). Não testado: aviões com mais de 8 tanques no sistema moderno (747-8i).

## 2026-11-21 - Gravação de combustível testada no 747-8i (Claude)
- **Testado no MSFS 2024 (747-8i, sistema moderno):** 8 tanques nos índices 1–8 (o app lê até o 8), capacidade total = utilizável; alvo +500 gal → diferença de 1,2 gal (tolerância 629 gal); restaurado tanque a tanque ao estado original.
- Achado: a distribuição proporcional à capacidade enche também o tanque central e os de reserva/estabilizador com carga parcial (no teste: central 0 → 8.635 gal, principais externos 4.233 → 2.702). Funciona para a sincronização, mas foge da ordem real de abastecimento e mexe no CG. Registrado como pendente no `AGENTS.md` › 6.

## 2026-11-21 - Abastecimento manual no simulador (Claude)
- Decisão do usuário: o piloto compra no Hangar e coloca o combustível no simulador ele mesmo (imersão e CG). O app deixou de gravar nos tanques: saiu a sincronização automática do `FlightMonitor.StartMonitoring` (`FuelSyncCoordinator`/`DispatchFuelWrite`) e a gravação após o abastecimento no Hangar e na janela de abastecimento.
- O briefing continua liberando o voo só com combustível, carga e passageiros batendo. O `StartMonitoring` repete a conferência (a bordo + não utilizável × pago, mesma tolerância do briefing) e bloqueia com "COMBUSTÍVEL DIVERGENTE"; combustível colocado entre o início do monitoramento e a ativação invalida o voo (em voo já invalidava). O caminhão do sim continua cobrando a entrega, agora na mesma convenção do app (a bordo + não utilizável).
- Não testado no MSFS.

## 2026-11-21 - Mensagens do voo em overlay por cima do simulador (Claude)
- Novo `AlertOverlayService` + `AlertOverlayWindow`/`AlertOverlayViewModel` (UI): janela sempre no topo, transparente, fora da barra de tarefas e sem roubar o foco do MSFS (`WS_EX_NOACTIVATE`), centralizada no alto da tela e arrastável. Até 5 mensagens empilhadas; avisos somem em 10 s, atenção em 20 s, erros e o resultado do voo ficam até o piloto fechar (✕).
- Passam a ir para o overlay: todos os alertas do `FlightMonitor.OnAlert` (antes `MessageBox` modal), a perda total e o resultado do voo (concluído / não registrado). As perguntas Sim/Não do planejamento continuam na janela do app. Se o overlay falhar, a mensagem volta para a caixa do app.
- Funciona com o MSFS em janela ou tela cheia sem borda (como o HUD). Não visto na tela nem testado no MSFS.

## 2026-11-21 - Excedências agrupadas no blackbox (Claude)
- Achado no voo de SNEJ (C152): a caixa-preta tinha 1.221 linhas "EXCEDENCIA_PECA" em 3 min, uma por peça por segundo. Não era registro abaixo do limite: a regra `piston_rpm_overspeed` tem faixa `caution` a partir de 97 % da linha vermelha (2.474 RPM no C152, desgaste natural × 1,5–2,5 em 12 peças), e o log mostra a linha vermelha como "limite".
- Novo `Domain/Parts/ExceedanceLogAggregator`: uma linha `INICIO` e uma `FIM` por ocorrência (regra + faixa/motor), com duração em tempo simulado, pico, dano somado e peças. `PartFlightSession.OnExceedanceTick` substitui o evento por peça; o `FlightMonitor` encerra as ocorrências abertas antes de fechar o blackbox (fim, cancelamento, perda total). Testes: `ExceedanceLogAggregatorTests` (4). Não testado no MSFS.

## 2026-11-21 - RPM do pistão: sem penalidade no arco verde (Claude)
- Decisão do usuário: subir a pleno dentro do arco verde é operação normal do POH (C152 até 2.550 RPM). `piston_rpm_overspeed` em `wear_rules.json`: faixa `caution` passa de 97 % para 100 % da linha vermelha e `warning` de 100 % para 104 %. Antes, a subida a pleno do C152 (2.475–2.570 RPM) acelerava 1,5–2,5× o desgaste de 12 peças e encurtava ~10–13 % a vida delas na carreira. Teste: `WearRuleCatalogTests.PistonRpm_SubidaAPlenoNoArcoVerde_SemPenalidade`. Não testado no MSFS.

## 2026-11-21 - Pneus, freios e rodas envelhecem por pouso (Claude)
- Achado no voo de SNEJ: no jogo, as peças de vida em ciclos (pneus 75, freios 150, rodas 350) só se gastavam por eventos (pouso duro, frenagem, deriva, táxi) e `CyclesSinceReplacement` só subia nas peças da regra `landing_cycle_structural_stress`; o simulador de carreira já gastava 70 %/vida por pouso. Novo `PartWearEngine.ApplyLandingCycle` (chamado pelo `PartFlightTracker.ProcessTouchdown` em todo toque que não é bounce): aplica as regras de ciclo, conta +1 ciclo em toda peça com vida em ciclos (sem contar duas vezes) e aplica o desgaste natural 70/vida × faixa amarela nas que não têm vida em horas. Testes: `LandingCycleWearTests` (5); suíte 697/697 com replay e fuzz. Não testado no MSFS.

## 2026-11-21 - ECON-DIFF: Calibração de pagamento em JSON e economia por dificuldade (Gemini)
- **Tabela de remuneração de missões desacoplada (`MissionPayTable.cs`, `mission_pay.json`)**:
  - Parâmetros tarifários das missões movidos do código para `src/FlightCareer.Infrastructure/Data/mission_pay.json` com 11 famílias ordenadas (`instrucao_basica`, `instrucao`, `medevac`, `aog`, `agro_bush`, `agro_municipal`, `taxi_aereo`, `carga_expressa`, `regional`, `linha`, `fallback`), cada uma com fator `scale` (padrão 1.0).
  - Resolução determinística por ordem de precedência (`Resolve`) casando IDs exatos (ex.: `4.3.1`, `3.2.1`, `3.4.1`) e prefixos (ex.: `4.`, `3.2`, `1.1`), com fallback automático idêntico aos valores originais em caso de arquivo ausente ou corrompido.
  - `MissionGenerator.CalculateReward` e `GenerateMissionsInMemory` agora recebem e aplicam `family.Scale * payMultiplier`.
- **Configurações e multiplicadores por dificuldade (`DifficultyEconomy.cs`, `CareerStart.cs`)**:
  - Saldo inicial da dificuldade Fácil ajustado de $1 bi para $1.000.000 (Médio permanece em $100.000 e Difícil em $10.000).
  - Multiplicadores aprovados:
    - **Fácil**: `PayMultiplier = 1.25`, `CostMultiplier = 0.80`, `FailureMultiplier = 1.00`.
    - **Médio**: `PayMultiplier = 1.00`, `CostMultiplier = 1.00`, `FailureMultiplier = 1.00` (zero desvio em relação ao comportamento nominal).
    - **Difícil**: `PayMultiplier = 0.85`, `CostMultiplier = 1.15`, `FailureMultiplier = 1.25`.
- **Aplicação no jogo e Hangar**:
  - No `MainViewModel`, `MissionGenerator.PayMultiplier` é configurado a partir da dificuldade do piloto ativo antes da geração e no recálculo de rotas.
  - No `FlightService`, custos de combustível e taxas aeroportuárias são multiplicados por `CostMultiplier`.
  - No `HangarViewModel.RunPaidServiceAsync`, custos de manutenção/reparos recebem o multiplicador e o diálogo de autorização informa `Dificuldade {nome}: custos ×{m:0.00}` quando o fator difere de 1,0.
  - No `FlightMonitor`, entregas por caminhão de combustível aplicam `CostMultiplier`.
- **Multiplicador de panes (`FailureEngine.cs`, `PartFailureEngine.cs`, `ComponentFlightSession.cs`, `PartFlightSession.cs`)**:
  - Adicionada propriedade `HazardMultiplier` que escala a taxa de risco $\lambda$ antes do cálculo da probabilidade de falha exponencial.
  - `FlightMonitor.StartMonitoring` injeta `FailureMultiplier` da dificuldade do piloto nas sessões de voo.
- **Simulador de progressão (`CareerProgressionSimulator.cs`)**:
  - Isolamento do cache de missões por chave de aeroporto, dia e dificuldade.
  - Aplicação de `PayMultiplier` em todas as missões geradas e de `CostMultiplier` em combustível, taxas, peças, revisões de manutenção, seguro e retífica.
  - Injeção de `HazardMultiplier` nos motores de pane do bot.
- **Testes e Validação**:
  - Nova suíte `DifficultyEconomyTests.cs` com 8 testes validando os valores da dificuldade, saldo de $1M, equivalência matemática exata com a fórmula antiga, proporcionalidade de multiplicador e escala por família, integridade do JSON e taxa de risco dobrada.
  - Simulação `career-stab` (30 dias, 1 bot, equilibrado, all): Dia 30 do Médio com saldo $150.162 e patrimônio $196.962 (idêntico à referência nominal).
  - `dotnet build`: 0 erros. Suíte xUnit: 692/692 testes aprovados (100%).

## 2026-11-22 - Revisão e merge da ECON-DIFF (Claude)
- `feat/econ-diff` aprovada (`docs/reviews/ECON-DIFF-revisao.md`) e mergeada. `career-stab --days 30 --bots 1 --difficulty medium --policy equilibrado` rodado duas vezes na branch e duas na base com o hash estável: resultados idênticos (saldo $133.933, patrimônio $180.733 no dia 30), então a simulação é reprodutível e o Médio não mudou. Não testado no MSFS.

## 2026-11-22 - Hangar: faixa de saúde sem "51–51%" e legenda atualizada (Claude)
- `HealthRange.ToString` mostra "~51%" (estimativa) quando os dois lados da faixa arredondam para o mesmo inteiro (logo após inspeção a faixa abre ±0,4 %/h e aparecia "51–51%"). Teste `Texto_da_faixa_nao_repete_o_mesmo_numero`.
- Legenda do Hangar atualizada para as faixas atuais (verde > 40 %, amarelo 40–20 %, vermelho < 20 %); citava marcas de 20/30 % das barras antigas. Não visto na tela.

## 2026-11-22 - HANGAR-ICONES-PECAS Estado das peças por ícone e horas por grupo (Gemini)
- No Hangar de aeronaves com peças (`HasParts`), remoção de porcentagens visíveis em peças, grupos e caixas do topo, substituídas por ícones vetoriais de estado (`Check`, `Warning`, `Broken`, `Question`) e contagens resumidas de peças.
- Implementação de `PartDisplayState`, `PartDisplayInfo`, `PartHangarView.DisplayState`, `Summarize` e `GroupLifeText`/`GroupLifeInfo`. Peças tornam-se `Uncertain` ("?") quando nunca vistas ou quando a margem de incerteza por horas decorridas (`growthPerHour × horas`) indica risco de ter caído para faixa inferior. Peças vermelhas e quebradas nunca viram incertas.
- Cabeçalhos de grupos exibem o ícone do pior estado entre as peças e a contagem compacta de estados presentes (`✖ n · ⚠ n · ? n · ✓ n`).
- Caixa de Aeronavegabilidade passa a exibir o pior estado entre as peças de grupos críticos (`ComponentDefinition.Critical`) com texto explicativo correspondente; Condição Geral resume a contagem total de peças.
- Textos de vida e horas nos grupos Motor, Óleo e Ignição passam a refletir os dados de manutenção reais da aeronave (TBO, troca de óleo 12 h, revisão 125 h), sem texto de horas nos demais grupos.
- Aeronaves legadas sem peças mantêm integralmente a visualização anterior de barras numéricas e faixas percentuais.
- Nova suíte de testes unitários `PartDisplayStateTests` cobrindo todas as regras de transição de faixa, incerteza, isolamento da saúde real e resumos.
- Não visto na tela / não testado no MSFS.
- Ajustes do revisor (Claude): quinto estado `Worn` ("gasta mas boa", 80–40 %, ícone de meio círculo com check em verde-limão), pedido do usuário para imitar o mecânico que vê o desgaste sem %; a marca de 80 % não gera "?". Ícone de quebrado trocado por círculo trincado (o "X" lia como "proibido"); contagem junta vermelho e quebrado num só "✖" e foi centralizada em `HangarPalette.CountsText`; tooltips e legenda sem %. Testes `Faixa_gasta_mas_boa_entre_80_e_40` e `Resumo_gasta_e_pior_que_nova_e_melhor_que_incerta`; suíte 737/737.

## 2026-11-22 - Briefing não via o combustível abastecido no Hangar depois de aceitar a missão (Claude)
- Achado do usuário (C152, SNEJ→SBJD): com 6,7 gal aceitou a missão, abasteceu até 10 gal no Hangar e o briefing continuou conferindo 6,7 gal (verde com 6,7 no sim). Causa: `MainViewModel.RefreshBriefingAircraftAsync` lia o avião pelo contexto de longa duração e o EF devolvia a entidade em cache (AGENTS › 6). Agora a leitura é `AsNoTracking`. O início do voo já lia o banco atualizado (`PilotRepository.GetAllAsync` limpa o cache), então não era possível decolar com combustível divergente: o `StartMonitoring` bloquearia. Sem teste automatizado (lógica de ViewModel fora do projeto de testes); não visto na tela.

## 2026-11-22 - Voo invalidado sem XP e valor da missão fixo ao remover do manifesto (Claude)
- `FlightService.ProcessFlightFinishAsync`: voo invalidado (slew, congelamento, combustível) mostrava 0 XP no resultado, mas o XP era somado ao piloto; agora zera antes de somar. Teste `InvalidatedFlightXpTests` (válido soma, invalidado não).
- `MainViewModel.RemoveMission`: remover uma missão do manifesto recalculava o pagamento com nova flutuação de mercado sorteada e com a família errada (passava o título no lugar do subId), então remover e aceitar de novo mudava o valor. Agora só a distância restante e o XP da missão são atualizados; o valor fica fixo (pouso intermediário já mantinha o valor no `FlightService`). Texto do aviso de descarregamento ajustado.
- `Mission.SubId` (coluna nova `Missions.SubId`, nula em missões antigas) gravado pelo gerador: base para os critérios de XP por tipo de missão (`xp-proficiencia.md` › 5.2). Suíte 739/739. Não visto na tela.

## 2026-11-22 - TOQUE-NORMAL Razão de descida do toque pela SimVar PLANE TOUCHDOWN NORMAL VELOCITY (Gemini)
- `SimConnectService.cs`: adicionado `TouchdownNormalVelocity` ao final da struct `SimFastFlightState` e mapeada a SimVar `PLANE TOUCHDOWN NORMAL VELOCITY` ("feet per second") via `AddFastDef` mantendo a ordem dos datums.
- `TouchdownDetector.cs`: `FastFlightSample` recebe `TouchdownNormalFps = double.NaN` no final. No cálculo da razão de descida do toque (`Classify`), remoção do termo `BodyVerticalFps` (que transformava ângulo de ataque com nariz alto em falsa razão de descida). A razão de descida passa a ser obtida a partir da última amostra entre o contato e a janela pós-contato com valor de `TouchdownNormalFps` finito e divergente da referência pré-contato (|Δ| > 1e-6); caso ausente ou inalterado, recai para o maior `-VerticalSpeedFpm` da janela pré-contato.
- `FlightMonitor.cs`: repasse de `s.TouchdownNormalVelocity` ao construir `FastFlightSample` em `OnFastFlight`.
- `TouchdownDetectorTests.cs`: novos testes unitários cobrindo pouso suave com nariz alto (C152 ~100 fpm com nariz alto e Body Y de -10,6 ft/s), fallback para vertical speed ignorando Body Y, valor inalterado no contato, pouso duro e crash por velocidade normal; teste anterior atualizado para confirmar que `BodyVerticalFps` é ignorado.
- `TelemetryDefinitionTests.cs`: novo teste `FastFlightState_DatumCount_MatchesSimFastFlightStateFields` garantindo que os 9 campos da struct correspondem aos datums registrados em `AddFastDef`.
- Não testado no MSFS.

## 2026-11-22 - App encerra ao fechar a janela principal (Claude)
- O processo do FlightCareer continuava vivo, sem janelas, depois de fechar o app (travava as DLLs e o `dotnet build`). Causa: `ShutdownMode="OnLastWindowClose"` e janelas que só se escondem (`AlertOverlayService` faz `Hide()` no overlay de alertas; HUD). Agora `ShutdownMode="OnMainWindowClose"` (o login já troca `Application.MainWindow` para a janela principal). Não visto na tela.

## 2026-11-22 - FIM-VOO-ACIDENTE: Combustível cobrado uma vez, acidente no diário e missões devolvidas na perda total (Gemini)
- `FlightService.ProcessFlightFinishAsync`: removida a cobrança duplicada de combustível consumido (débito no saldo e transações "Combustível") nos caminhos normal e de pouso fora de pista, alinhando com a decisão de 2026-11-21 em que o combustível é pago na compra no Hangar (`outcome.FuelCost = 0` mantendo `outcome.FuelUsed` para o blackbox); `AircraftModel` preenchido no registro de `Flight`.
- `FlightService.ProcessTotalLossAsync`: novo método para persistência de acidente na perda total gravando o `Flight` (origem, destino do local do impacto ou "ACIDENTE", `AircraftModel`, XP e Revenue 0), somando horas voadas ao piloto (`TotalHours`), avançando o relógio de carreira (`CareerClock.AdvanceFlight`) e devolvendo missões ativas ao mercado (`IsAccepted = false`, `PilotId = null`, `OriginICAO` revertido para a origem do voo e distância recalculada, mantendo o `RewardMoney` intacto).
- `FlightMonitor`: em `HandleTouchdown` (ramo `Crash`), chamada a `_qualityService.RecordTouchdownFpm(ev.TouchdownFpm)` antes de `HandleTotalLossAsync` para que o resumo do blackbox use a medição do detector; em `HandleTotalLossAsync`, captura prévia dos dados do voo antes de `StopMonitoring()` e invocação de `_flightService.ProcessTotalLossAsync` antes de `OnTotalLoss`.
- `FlightLogEntry`: status `PERDA_TOTAL_DESTRUIDA` mapeado para `StatusDisplay` "✖ ACIDENTE", `StatusColor` "#FF5252" e `StatusBg` "#2E1214".
- Suíte `FlightFinishTotalLossTests` com 3 novos testes unitários validando fim de voo sem cobrança de combustível, pouso fora de pista sem cobrança e registro de perda total com devolução de missões; ajuste no teste `FlightPersistenceTests.Segundo_voo_ve_inspecao_e_gastos_feitos_entre_os_voos`.
## 2026-11-22 - Métricas de solo, partida e motor que as regras usam mas ninguém calcula (METRICAS-A) (Gemini)
- Implementado no `PartEventDeriver` o cálculo das 6 métricas operacionais pendentes do catálogo de regras de desgaste (`wear_rules.json`):
  - `brakeEnergyPctRef`: cálculo de energia específica de frenagem no solo $100 \times \sum \Delta(v^2/2) / ((1,3 \times V_{so})^2 / 2)$ em m/s, com acompanhamento de frenagem contínua (freios $\ge 30\%$, sem freio de estacionamento) e finalização após 2 s com freio $< 10\%$ ou GS $< 2$ kt. Limites na regra `brake_high_energy_antiskid` atualizados para as faixas 60, 100 e 150.
  - `taxiTurnSpeedKnots`: monitoramento de velocidade máxima no solo durante curvas de táxi ($|\text{ângulo de direção}| \ge 30\%$, GS $< 40$ kt) para aeronaves com `noseWheelSteering`, emitindo a maior velocidade atingida se $\ge 15$ kt após retorno da direção por 2 s ou GS $< 3$ kt.
  - `highPowerColdOilMinutes`: minutos acumulados em tempo simulado com óleo abaixo da temperatura mínima (`oilTempMin` do perfil ou padrão 24 °C pistão / 0 °C turbina) e potência elevada (RPM $\ge 90\%$ da redline ou manete $\ge 80\%$ em pistão; torque $\ge 80\%$ em turbina) com motor em combustão. Emissão nos cruzamentos de 1, 3 e 6 minutos com reinício após 2 minutos fora da condição.
  - `trimCycles`: contagem de ciclos de deflexão de compensador de profundor no ar ($\ge 10\%$ do curso com reversão ou parada). Emissão nos cruzamentos de 1, 4 e 8 ciclos.
  - `turbulenceGExceedanceCycles`: contagem de ciclos de variação de G no ar com piloto automático ativo ($|G - 1| \ge 0,5$, rearmando com $|G - 1| < 0,2$). Emissão nos cruzamentos de 10, 25 e 50 ciclos.
  - `rapidPowerTransientCycles`: contagem de transientes rápidos de potência em turboélices (variação de torque $\ge 40$ pp em janela de 2 s simulados). Emissão nos cruzamentos de 3, 8 e 15 ciclos.
- Novas regras de desgaste em `wear_rules.json`:
  - `ground_tire_skid` (métrica `skidSeconds`): tempo acumulado de derrapagem de pneus no solo (GS $> 3$ kt com `gear-is-skidding` ativo), emitindo nas faixas de 1 s, 3 s e 6 s (com inspeção `TIRE_FLAT_SPOT_INSPECTION` na faixa grave).
  - `engine_start_electrical_load` (métrica `startLightsOnCount`): partida com cargas elétricas acesas, contando farol de pouso, táxi e strobe acesos durante o ciclo de partida (faixas 1, 2 e 3 lâmpadas).
- Novo suporte a `oilTempMin` em `LimitKeys` e `EngineLimitsConfig` no `PartFlightTracker`, lido do perfil da aeronave ou usando padrões com diagnóstico único quando ausente.
- Suíte `GroundAndEngineMetricsTests` com 10 testes cobrindo todas as novas regras e métricas.
- Replay do voo real C152 (`20261122-1007-Cessna_C152.csv`) confirmou emissão de partida com luzes acesas (`startLightsOnCount: 3.0`) e compensador (`trimCycles: 1.0`).
- Não testado no MSFS.

## 2026-11-22 - Oficina em tempo real, serviço expresso e prazos reais (PARADA-REAL) (Gemini)
- `Domain/Components/WorkshopDowntime.cs`: classe estática pura implementando as regras de parada na oficina em minutos de relógio real do computador (`RealMinutesFor = clamp(round(2 × √horasDeJogo), 1, 20)`), serviço expresso (`Apply(gameHours, express)` com +50% no custo, metade das horas de jogo e dos minutos reais), conferência de disponibilidade (`IsInWorkshop`), tempo restante (`Remaining`) e cálculo de prazo sobre parada em andamento usando o maior prazo (`CalculateNewUntilUtc`).
- `Domain/Components/MaintenancePricing.cs`: unificação do fator de compressão regulatória `CompressionFactor = 4.0` e funções puras `RealHours(gameHours)` e `RealCycles(gameCycles)`.
- `Domain/Entities/PilotAircraft.cs` e `Infrastructure/Persistence/AppDbContext.cs`: novas colunas `WorkshopUntilUtc` (TEXT/DateTime? em UTC) e `WorkshopDescription` (TEXT) com migração idempotente em `EnsureSchemaUpdatedAsync`.
- `Domain/Parts/PartHangarView.cs` e `Domain/Parts/PartInspectionFlags.cs`: `GroupLifeInfo` e `LifeNote` atualizados para exibir os prazos da vida real (× 4) e o restante em horas de voo do jogo com o rótulo "de voo", acompanhados de estado vencido.
- `UI/Views/ThemedDialog.cs`: novo método `ConfirmService` permitindo ao piloto escolher entre serviço Normal e Expresso (+50% no custo, metade do tempo de parada real e de jogo).
- `UI/ViewModels/HangarViewModel.cs` e `PilotAircraftWrapper.cs`: `RunPaidServiceAsync` grava o prazo de oficina e descrição, além do avanço do relógio de carreira; `DispatcherTimer` de 1 s ativo exclusivamente enquanto houver aeronave na oficina da frota atualizando a contagem regressiva; card do avião no Hangar exibe faixa estilizada de liberação estimada ("Na oficina — libera em mm:ss (serviço)"); rótulos de inspeções e revisões no XAML vinculados a propriedades dinâmicas do wrapper baseadas em `MaintenancePricing` com legenda fixa explicativa sobre o ritmo acelerado (×4).
- `UI/ViewModels/MainViewModel.cs` e `BriefingViewModel.cs`: início de voo bloqueado em `StartFlight` com modal explicativo e checklist de despacho no Briefing bloqueado com aviso "Na oficina — libera em mm:ss" quando a aeronave ativa estiver em manutenção na oficina.
- Suíte `WorkshopDowntimeTests` com testes unitários cobrindo conversão de minutos, piso, teto, serviço expresso, relógio injetado e cálculo de maior prazo sobre parada; atualização em `PartDisplayStateTests`, `PartInspectionFlagsTests`, `PartHealthBandsAndFailuresTests` e teste de migração em banco antigo em `ComponentPersistenceTests`.
## 2026-11-22 - Métricas de sistemas e cobertura das regras por avião (METRICAS-B) (Gemini)
- Implementado no `PartEventDeriver` o cálculo das 8 métricas de sistemas que estavam sem produtor:
  - `icingExposureMinutes`: tempo acumulado em minutos no ar com gelo estrutural $\ge 5\%$ ou gelo no pitot $\ge 10\%$, emitindo nos cruzamentos de 5, 15 e 30 minutos, com reinício após 10 minutos sem gelo.
  - `deiceCycles`: contagem de ciclos de acionamento das botas de degelo (`inst-structural-deice-switch` 0→1) para aeronaves com feature `deIceBoots`, emitindo em 2, 6 e 12 ciclos.
  - `propDeiceCycles`: contagem de ciclos de degelo de hélice (`inst-prop-deice-switch` 0→1), emitindo em 2, 6 e 12 ciclos.
  - `glowPlugCycles`: contagem de partidas de motor para aeronaves com motor `diesel`, emitindo em 1, 3 e 5 ciclos.
  - `dryRunPumpMinutes`: tempo acumulado com bomba de combustível ligada em condição de tanque seco ($< 0,5$ gal) ou no solo com motor desligado, emitindo em 2, 5 e 10 minutos com reinício após 1 minuto fora.
  - `lowHydraulicPressureMinutes`: tempo acumulado com pressão hidráulica abaixo de 50% da máxima vista com motor em combustão, armado apenas após o sistema registrar pressão $\ge 500$ psi, emitindo em 3, 8 e 15 minutos.
  - `pressurizationCycles`: contagem de ciclos de pressurização de cabine para aeronaves com feature `pressurized` (diferencial sobe acima de 2 psi e retorna abaixo de 0,5 psi), emitindo em 1, 2 e 4 ciclos.
  - `rapidDecompressionDeltaPsf`: detecção de descompressão rápida para aeronaves `pressurized` com taxa de queda do diferencial $\ge 150$ psf/s em janela de 1 s, emitindo o pico do evento após estabilização ($< 20$ psf/s por 5 s).
- `PartEventDeriver.ProducedMetrics` e `PartFlightTracker.ProducedMetrics`: vocabulário estático de métricas produzidas exposto no domínio, totalizando exatamente as 29 métricas do catálogo fechado `WearRuleMetrics.All` sem nenhuma constante órfã.
- Sincronização em `wear_rules.json`: regra `turbine_start_cycle` atualizada para `"metric": "turbineStartCycle"` (alinhando a regra com o evento emitido pelo derivador para turbinas).
- `WearRuleMetricCoverageTests`: nova suíte xUnit garantindo que 100% das regras de evento possuem produtor ativo, 100% das regras contínuas utilizam sensores registrados e chaves de limite válidas, além de 8 testes cobrindo thresholds, sensores zerados e guardas de feature de cada métrica.
- `RuleCoverageService` e comando `coverage` no `FlightCareer.Replay`: geração automática de relatório completo de cobertura das 47 regras para os 46 modelos do catálogo em `docs/plans/cobertura-regras-por-aviao.md`.
- Não testado no MSFS.

## 2026-11-22 - XP por esforço e habilidade com bônus fixos e nível do piloto (XP-NOVO) (Gemini)
- `Domain/Xp/`: módulo de lógica pura implementando o cálculo aprovado do plano `xp-proficiencia.md` § 6:
  - `XpFlightInputs.cs`: records imutáveis de entrada de fatos de voo (`XpFlightInputs`), fatos de pista de origem/destino (`RunwayFacts`), fatos ambientais e aproximação (`EnvFacts`) e detalhamento de cálculo (`XpBreakdown`).
  - `PilotLevel.cs`: modelo de progressão por nível contínuo sem nível máximo baseado na curva quadrática $300 \times (L - 1)^2$ (`ForXp` e `XpForLevel`).
  - `XpRules.cs` e `Infrastructure/Data/xp_rules.json`: calibração 100% desacoplada em dados com fallback embutido idêntico para taxas base (2 XP/min no ar, 0,5 XP/NM, 50 XP/perna), multiplicadores de tipo (`M_Tipo`), multiplicadores de realismo (`M_Realismo` 0,80 a 1,20), multiplicadores de qualidade e infrações (`M_Qualidade` 0,50 a 1,15) e bônus fixos de pista, ambiente e prazo ETE.
  - `XpCalculator.cs`: classe estática pura computando `Total = round(Base × M_Tipo × M_Realismo × M_Qualidade + Bônus)` e gerando lista explicativa de linhas para o resumo do voo.
- `Pilot.cs`: nova propriedade calculada `Level => PilotLevel.ForXp(ExperiencePoints)` sem adicionar coluna nova no banco.
- `PilotCategory.cs`: novos requisitos de XP regulatório para homologação de categorias (10.000 / 45.000 / 175.000 / 600.000) adaptados à escala da curva de níveis.
- `SimTelemetry.cs` e `SimConnectService.cs`: adicionadas 6 novas SimVars no final da struct e definições `AddToDef` mantendo correspondência exata de posições e tipos: `REALISM CRASH DETECTION` (Bool), `AUTO COORDINATION` (Bool), `REALISM` (Number), `AMBIENT VISIBILITY` (Meters), `AMBIENT IN CLOUD` (Bool), `DENSITY ALTITUDE` (Feet).
- `FlightOutcome.cs`: nova propriedade `XpBreakdownLines` para transporte do detalhamento do cálculo.
- `FlightService.cs`: `ProcessFlightFinishAsync` atualizado para receber `XpFlightInputs? xpInputs = null`, calculando o XP pelo `XpCalculator.Compute` (com fallback seguro base × M_Tipo × qualidade da nota se ausente), preservando XP 0 para voos invalidados e anexando linhas explicativas em `outcome.Message`.
- `FlightMonitor.cs`: coleta em tempo de voo dos minutos no ar (`_airborneSeconds`), aceleração de tempo (`_usedTimeAcceleration`), pior toque de pernas/TGL (`_worstTouchdownFpm`, `_worstTouchdownG`, `_flightTouchdowns`), contagem de infrações de Vne (`_overspeedInfractions`), rajadas em janela de 3 minutos (`_landingGustSpreadKt`), condições de aproximação (nuvens, visibilidade e altitude-densidade), resolução de fatos de pista do banco e repasse dos dados consolidados ao `ProcessFlightFinishAsync`.
- `MainViewModel.cs`: interface principal exibe a patente e o nível do piloto no formato "PATENTE · NÍVEL N".
- `CareerProgressionSimulator.cs`: simulador de carreira atualizado para utilizar o `XpCalculator.Compute` com estimativas operacionais de pista, distância e pontualidade nos voos de missões, traslados e voos locais dos bots.
- Suíte de testes:
  - `PilotLevelTests.cs`: testes unitários de marcos de nível da curva, progressão sem limite superior e invariante de consistência `XpForLevel(ForXp(x)) <= x`.
  - `XpCalculatorTests.cs`: testes de validação cobrindo o exemplo do plano (185 XP), pior toque de pernas com TGL, combo bush em altitude com pista de terra, pouso fora de pista com zeramento de bônus de pouso e penalidade, pisos/tetos dos multiplicadores e faixas de cumprimento de prazo.
  - `FlightFinishXpPersistenceTests.cs`: teste de integração em SQLite em memória conferindo persistência exata de XP, avanço de nível do piloto e zeramento em voo invalidado.
- Build 0 erros, `dotnet test` 809/809 testes aprovados (100%), Fuzz Testing em todas as 46 aeronaves (920 voos) 100% aprovado sem nenhuma violação.
- Não testado no MSFS. Bônus noturno desligado (não confirmada SimVar direta no SDK); infrações de estol e luzes/velocidade não monitoradas atualmente pelo monitor entram como 0.

## 2026-11-22 - Calibração do pagamento pela curva de progressão (Claude)
- `mission_pay.json`: instrução 1,5; carga/agro/AOG/aeromédico/fallback 0,7; táxi aéreo/regional/linha 0,28. Medido na ECON-STAB (3 anos, 3 bots por estilo): Médio — 1ª troca no dia 79–96, turboélice no dia 365, jato no início do 3º ano; Difícil ~1,4–1,5× mais lento, sem jato em 3 anos. Antes: classe G no 1º ano e $8,3 mi de patrimônio. Detalhes e o conflito com a meta de patrimônio (≤ 20×) em `economia-estabilidade.md` › 4.4.
- `CareerProgressionSimulator`: marcos 30/60/90/180/365/540/730/1095 dias. Testes da ECON-DIFF ajustados: a equivalência com o código antigo usa a tabela embutida (escala 1,0); o JSON calibrado só precisa de escala positiva. Suíte 821/821. Revalidar depois da FASES-1.
## 2026-11-22 - Ativação de regras por detecção elétrica em voo e limites Va/Vno (LIMITES-FALTANTES) (Gemini)
- `Domain/Parts/PartEventDeriver.cs`: detecção dinâmica em voo do barramento elétrico principal (`_detectedMainBusIndex`, primeiro k de 1 a 5 com tensão >= 0,5 × tensão do sistema com bateria ligada) e da tensão nominal (`_detectedSystemVolts`, >= 20 V com alternador ligado define 28 V, senão 14 V); emissão de diagnóstico `barramento principal detectado: k (x V)` e permanência em espera sem acusar desativação enquanto não detectados.
- `Infrastructure/Data/aircraft/_class_defaults.json`: configurado `alternatorRatedAmps` padrão de classe (60 A pistão, 250 A turboélice, 400 A jato) como `ClassDefault` com referência de estimativa.
- `Domain/Parts/AircraftFeatures.cs`, `cessna-c152.json` e `wear_rules.json`: adicionada feature `noAvionicsMaster` ao vocabulário fechado; Cessna C152 anotado com a feature e limite informativo `elecAvionicsCircuitIndex = 0` (POH: sem chave de aviônicos); regra `avionics_start_with_avionics_master_on` atualizada para excluir `noAvionicsMaster` (contando como não aplicável por feature em vez de inativa).
- Catálogo de aeronaves (`Infrastructure/Data/aircraft/`): preenchido `va` em 15 perfis que possuem `vs1` via fórmula operacional $Va = Vs1 \times \sqrt{n}$ ($n=3,8$ para aeronaves leves e $n=2,5$ para transporte/jatos), marcados como `ClassDefault`; 18 aeronaves sem `vs1` mantidas sem estimativa conforme especificação.
- `SyntheticFlightPlan.cs`, `SyntheticFlightOracle.cs` e `FuzzTestDefaults.cs`: oráculo e planejador sintético atualizados para respeitar `noAvionicsMaster` (não injetando abusos espúrios de chave de aviônicos no C152) e reconhecer a detecção de barramento em voo para regras de descarga e sobrecarga.
- `Infrastructure/Services/RuleCoverageService.cs`: regras elétricas reconhecidas como ativas via detecção em voo com anotação `(detecção em voo)`; comparativo Antes / Depois no topo do relatório `cobertura-regras-por-aviao.md` demonstrando aumento de regras ativas médias de 30,3 para 32,6 e redução de inativas de 4,5 para 2,2.
- Testes unitários e de integração: novos testes em `ElectricalRuleDeriverTests.cs` (detecção de barramento com bateria ligada/desligada, detecção de 14 V, disparo de descarga e sobrecarga sem índices no catálogo) e `CatalogTests.cs` (fórmula de Va e integridade do POH); suíte xUnit aprovada com 789 testes (100%) e fuzz testing com 920 voos procedurais (46 modelos × 20 corridas) aprovado com 100%.
- Não testado no MSFS.
## 2026-11-22 - 7 fases por nível no lugar das categorias (FASES-1) (Gemini)
- `Domain/Entities/CareerPhase.cs` e `Domain/Career/CareerPhaseCatalog.cs`: novo enum `CareerPhase` (1 a 7) e catálogo desacoplado em `Infrastructure/Data/career_phases.json` com fallback embutido idêntico:
  - Fases 1 a 7: Escola de aviação / aeroclube (1), Serviços aéreos especializados - SAE (2), Táxi aéreo e carga leve (3), Executiva de alta performance e MEDEVAC (4), Regional commuter (5), Linha aérea doméstica narrowbody (6), Companhia internacional widebody (7).
  - Tríade de promoção: nível $\ge$ nível de entrada (L6, L13, L21, L29, L37, L45), horas mínimas (40, 150, 300, 500, 1.000, 1.500 h) e homologação regulatória paga ($35 mil, $120 mil, $300 mil, $500 mil, $1,2 mi, $2 mi).
  - Progressão de nível: contínua via ganho de XP (curva quadrática $300 \times (L - 1)^2$, sem nível máximo); após a fase 7 os níveis continuam subindo sem novas fases.
  - Mapeamento de subtipos: 26 subtipos de missão mapeados para as 7 fases conforme § 3 de `fases-carreira.md`. Missões de fases anteriores permanecem liberadas para pilotos de fase superior.
- `Pilot.cs` e `Mission.cs`: novas propriedades mapeadas `CareerPhase` e `RequiredPhase`. Antigas `PilotCategory`, `CompanyTier`, `RequiredCategory` e `RequiredTier` preservadas com `[Obsolete]` como adaptadores para compatibilidade.
- `AppDbContext.cs`:
  - Migração de schema idempotente em `EnsureSchemaUpdatedAsync` adicionando as colunas `CareerPhase` e `RequiredPhase` e convertendo dados legados pela regra 5 (1→1, 2→2, 3→3, 4→5, 5→6).
  - `HasDefaultValue(CareerPhase.Phase1_FlightSchool)` configurado no modelBuilder para suportar criação e inserts legados.
- `MissionGenerator.cs`: resolução de `RequiredPhase` e `MissionCategoryType` a partir de `sub.Id` via `CareerPhaseCatalog`.
- `MainViewModel.cs`:
  - Checagens de aceite e início de voo verificam `m.RequiredPhase > pilot.CareerPhase`.
  - Promoção `PromoteCompanyTierAsync` atualizada para as 7 fases com conferência da tríade (nível, horas, taxa) e mensagem descritiva do que falta ("Nível X — ok · Horas Y/Z · Homologação $W").
  - Identificação de fase e status no cabeçalho exibindo "Fase N — nome · Nível L · licença".
- `FlightMonitor.cs`: validação de início de voo bloqueia com alerta de fase insuficiente quando `m.RequiredPhase > pilot.CareerPhase`.
- `MissionDisplayItem.cs`: elegibilidade checada por `RequiredPhase`, tooltip com o nome da fase exigida.
- `CreatePilotViewModel.cs` e `FerryFlightViewModel.cs`: inicialização e contratos de traslado configurados em `CareerPhase.Phase1_FlightSchool`.
- `CareerProgressionSimulator.cs`: bots promovem pelas novas regras da tríade (`CheckPhasePromotion`) com escada de compra de aeronaves mapeada para as fases operacionais (A, B → 1; E, F → 3; G, H → 4; I → 6; J → 7).
- Suíte de testes:
  - `CareerPhaseTests.cs`: 5 novos testes xUnit cobrindo progressão por XP puro, bloqueios da tríade de promoção (horas, homologação, sucesso), mapa dos 26 subtipos para as 7 fases, liberação de missões anteriores e bloqueio de superiores, níveis após fase 7 (L60), e migração de banco legado idempotente com conversão 4→5 e 5→6.
  - Atualização dos testes existentes `CareerProgressionTests.cs` e `PartFlightIntegrationTests.cs`.
- `dotnet build` 0 erros, `dotnet test` 827/827 testes aprovados (100%), simulação `career-stab --days 90 --bots 1 --difficulty medium --policy equilibrado` executada com sucesso absoluto.
- Não testado no MSFS / não visto na tela.

## 2026-11-22 - Ponto de partida no mapinha do HUD e saúde por ícone (HUD-ICONES) (Gemini)
- `FlightMapControl.xaml.cs`: adicionadas propriedades de dependência `OriginLat` e `OriginLon` com callback `OnOriginChanged`, renderizando ao vivo na partida um marcador circular ciano neon (`#00E5FF`, 9×9 com borda preta) idêntico em estilo ao ponto de chegada com tooltip do ICAO de origem (`ToolTip = $"ORIGEM: {OriginIcao}"`), integrando ao enquadramento suave de pan-zoom tático.
- `DebugOverlayViewModel.cs`:
  - Propriedades observáveis `OriginLat` e `OriginLon` preenchidas com as coordenadas do aeroporto de origem da missão ativa (`FindAirport` via `AsNoTracking` e contexto curto) ou, em voos locais/circuito sem missão, pela posição GPS em que o monitoramento do voo começou.
  - Eliminação total da leitura das colunas legadas `AirframeCondition`, `EngineCondition` e `TiresCondition` que vazavam a saúde real oculta da aeronave em tempo real no voo.
  - `RefreshAircraftHealth`: carrega as peças do avião ativo (ou componentes) via contexto curto com `AsNoTracking` e invoca `HudHealthSummary.Build` (ou `BuildFromComponents`).
  - Novas propriedades reativas de status para os três grupos: `AirframeStatusText`, `AirframeStatusBrush`, `AirframeStatusIconGeometry`, e equivalentes para `Engine` e `Tires`.
- `DebugOverlayWindow.xaml`:
  - `FlightMapControl` no HUD recebe bindings para `OriginLat="{Binding OriginLat}"` e `OriginLon="{Binding OriginLon}"`.
  - Grupo 5 ("INTEGRIDADE DA AERONAVE & DESGASTE"): removidas todas as porcentagens numéricas e `ProgressBar`s. Cada grupo (Célula, Motor, Pneus) agora exibe o ícone de estado vetorial idêntico ao Hangar (`HangarIcons.ForDisplayState`) e texto curto de estado ("Em ordem", "Em uso", "Atenção", "Risco", "Incerta") na cor da paleta do Hangar (`HangarPalette.ForDisplayState`).
- `Domain/Parts/HudHealthSummary.cs`: módulo de lógica pura que agrupa as peças nos 3 sistemas exibidos (Célula, Motor e Pneus/Freios), obtém o `PartDisplayInfo` pela saúde conhecida (`KnownHealth` / `KnownAtAirframeHours` com crescimento de incerteza por hora de voo) e calcula o resumo do pior estado (`PartHangarView.Summarize`) sem jamais vazar a saúde oculta real. Fornece fallback `BuildFromComponents` para aviões sem peças baseado nas faixas de inspeção (`HealthVisibility`).
- `Tests/Parts/HudHealthSummaryTests.cs`: suíte de testes xUnit cobrindo:
  - Não vazamento de saúde real (peça com saúde real 5 e saúde conhecida 90 vistas há 0 h reporta "Em ordem" verde).
  - Peça amarela conhecida reporta "Atenção".
  - Peça nunca inspecionada reporta "Incerta".
  - Peça gasta reporta "Em uso".
  - Peça quebrada reporta "Quebrada".
  - Priorização de pior estado no agregado do grupo.
  - Fallback por faixas de componentes para aeronaves legadas sem peças.
- Build com 0 erros, `dotnet test` com 834/834 testes aprovados (100%).
- Não visto na tela / não testado no MSFS.

## 2026-11-22 - Pacotes com vulnerabilidade conhecida fixados em versões corrigidas (Claude)
- `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14 (Infrastructure), no lugar do `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 vindo do EF Core Sqlite (GHSA-2m69-gcr7-jv3q, alta); `System.Data.SqlClient` 4.8.6 (UI), no lugar do 4.8.3 vindo do GMap.NET (GHSA-98g6-xh36-x2p7 alta, GHSA-8g2p-5pqh-5jmc moderada). Restore sem avisos NU190x; suíte 840/840 (SQLite real em memória). Não aberto na tela.

## 2026-11-22 - Multiplicador de preço de peças por classe com fonte única (Claude)
- `AircraftEconomyClass.GetPartPriceMultiplier` lê `classes.<X>.partPriceMultiplier` do `economy_targets.json` (antes um `switch` duplicava os valores do JSON); reserva embutida igual aos valores atuais se o arquivo faltar. Testes `AircraftEconomyClassMultiplierTests` (JSON real, classe ausente, arquivo alterado). Suíte 849/849.
## 2026-11-22 - Contratos novos da Fase 1: voo panorâmico, circuito de TGL e translado básico (FASES-2A) (Gemini)
- `Mission.cs`: novas propriedades `RequiredTouchAndGoes` (int, default 0) e `OverflyWaypoints` (string, default NULL).
- `AppDbContext.cs`:
  - Adicionadas colunas `RequiredTouchAndGoes` e `OverflyWaypoints` na lista de migrações idempotentes de `EnsureSchemaUpdatedAsync`.
  - Configurado `HasDefaultValue(0)` em `RequiredTouchAndGoes` no `OnModelCreating` para compatibilidade com DDL SQLite.
- Configurações JSON e Tabelas de Domínio:
  - `career_phases.json`: mapeamento dos subtipos `3.5.1`, `3.5.2` e `3.5.3` para a Fase 1 (`CareerPhase.Phase1_FlightSchool`).
  - `mission_pay.json` e `MissionPayTable.cs`: adicionadas famílias `panoramico` (3.5.1: base $300, 6,5/NM, 0,10/lb, escala 1,0), `translado` (3.5.3: base $250, 5,0/NM, escala 1,0) e inclusão de `3.5.2` na família `instrucao_basica`. Paridade garantida entre JSON e `MissionPayTable.Defaults`.
  - `xp_rules.json`: multiplicadores de tipo `panoramico` (1,15) e `translado` (0,80).
- `MissionRules.cs`: categoria nova "3.5 Aeroclube" adicionada nos Tiers 2, 3 e 4 com peso 5, contendo os subtipos 3.5.1 (Panorâmico), 3.5.2 (Circuito TGL) e 3.5.3 (Translado Básico).
- `MissionGenerator.cs`:
  - `3.5.1 Voo Panorâmico`: busca 2 a 3 aeródromos vizinhos (8–30 NM da origem, com fallback para 45 NM), ordena por azimute angular ao redor da origem formando rota circular sem ziguezague, calcula percurso total do circuito para distância e remuneração, gera lista `OverflyWaypoints` e 1–3 passageiros.
  - `3.5.2 Circuito de TGL`: sorteia 3 a 6 toques (`RequiredTouchAndGoes`), 0 a 1 passageiro (instrutor), calcula distância estimada do circuito e formata descrição e título específicos.
  - `3.5.3 Translado Básico`: aeródromo vizinho a < 80 NM, 0 passageiros e carga $\le 50$ lb ("avião de manutenção").
- Lógica Pura no Domínio (`Domain/Missions/`):
  - `OverflyPoint`: record com `Ident`, `Latitude`, `Longitude` e `ElevationFt`.
  - `OverflyTracker`: rastreia waypoints por proximidade horizontal ($\le 2$ NM) e teto de altitude ($\le$ elevação + 3.000 ft).
  - `TouchAndGoCounter`: máquina de estados precisa para TGL (rolo no solo $\ge 1$ s, GS máxima $> 20$ kt, sem parada completa $\text{GS} < 5$ kt, e nova decolagem; primeira decolagem do voo ignorada).
  - `MissionCompletion.Evaluate`: avalia pouso na origem/destino esperado, conferência de TGLs mínimos e sobrevoo de todos os pontos obrigatórios.
- `FlightMonitor.cs`: integrado `TouchAndGoCounter` e `OverflyTracker` alimentados pela telemetria em voo monitorado e repassados no encerramento para o `FlightService`.
- `FlightService.cs`:
  - Avaliação de conclusão via `MissionCompletion.Evaluate`.
  - Se os requisitos não forem atendidos (falta de pontos ou TGLs insuficientes), a missão não é concluída, retorna ao mercado no aeroporto atual com o mesmo valor (regra de descarregar) e uma mensagem detalhada do que faltou é adicionada ao `FlightOutcome.Message`.
- Suíte de Testes xUnit:
  - `Phase1ContractsTests.cs`: 11 testes cobrindo `MissionCompletion` (panorâmico aprovado/rejeitado, circuito aprovado/rejeitado), `TouchAndGoCounter` (TGL válido, quique rejeitado, parada completa rejeitada, decolagem inicial ignorada), `OverflyTracker` (distância e teto válidos/inválidos), gerador de contratos e migração idempotente de banco legado.
  - 851/851 testes xUnit aprovados (100%).
- Não visto na tela / não testado no MSFS.
## 2026-11-22 - XP estimado no quadro, infrações, pista difícil no pagamento e usado coerente (XP-PONTAS) (Gemini)
- `Domain/Xp/XpCalculator.cs`: implementada a função pura `XpCalculator.Estimate(...)` para cálculo prévio de "≈ XP" a partir da distância, velocidade de cruzeiro, complexidade da família (`M_Tipo`), qualidade 1,0, realismo 1,0, bônus de pista de origem/destino e prazo "no prazo" (+15).
- `UI/ViewModels/MissionDisplayItem.cs`: tabela de missões exibe `XpDisplay => "≈ N XP"` calculada ao lado do pagamento.
- `Domain/Entities/RunwaySurface.cs`: centralização da classificação de superfícies duras e não pavimentadas (`IsHard` e `IsUnpaved`), eliminando as duplicações de código no `FlightMonitor` e no `DataSeeder`.
- `Domain/Xp/FlightInfractionTracker.cs`: rastreador desacoplado e 100% testável de infrações operacionais:
  - Estol: borda de subida de `STALL WARNING` no ar (ignorado em missões de instrução).
  - Velocidade: borda de subida de IAS > 250 kt com altitude < 10.000 ft.
  - Luzes: farol de pouso e táxi simultaneamente desligados na decolagem (+1) e no toque final (+1).
  - Vne: borda de subida de overspeed estrutural.
  - Adicionadas linhas explicativas de penalidades por infração no resumo do XP (`XpBreakdown.Lines`).
- `Domain/Entities/SimTelemetry.cs` e `UI/Services/SimConnectService.cs`: adicionadas 3 novas SimVars no fim da struct e definições `AddToDef`: `STALL WARNING` (Bool), `LIGHT LANDING` (Bool) e `LIGHT TAXI` (Bool).
- `UI/Services/FlightMonitor.cs`: integração contínua do `FlightInfractionTracker` no tick de telemetria, na decolagem e nos pousos/toque final, repassando `VneOrStallInfractions` e `LightsOrSpeedInfractions` para o fechamento do voo.
- `Application/Services/MissionGenerator.cs`: `CalculateReward` atualizado para receber informação de pista do destino (`destRunway` / `isDifficultRunway`); o bônus bush (+25%) agora premia aeródromos com pista não pavimentada (`RunwaySurface.IsUnpaved`) ou com comprimento < 2.500 ft (com cache `_runwayCache` por aeroporto para evitar consultas repetidas por missão; destinos sem dado de pista caem no tier 4 como reserva).
- `Infrastructure/Services/PartMigrationService.cs` e `MarketSimulationService.cs`: provisionamento coerente de peças de aeronaves adquiridas no mercado usado:
  - Horas das peças: `clamp((100 - saúde) / 30 × vidaHoras, 0, horas de célula)`.
  - Ciclos das peças: `clamp((100 - saúde) / 30 × vidaCiclos, 0, pousos estimados)`.
  - Peças do motor limitadas pelo TSMO (`hoursSince <= tsmoHours`).
  - `KnownHealth = health` em todas as peças compradas (inspeção pré-compra realizada).
- Suíte de testes:
  - `XpEstimatorAndInfractionTests.cs`: testes unitários do estimador (exemplo do plano 60 NM a 100 kt instrução = 182 XP, bônus de pista), bordas de estol e isolamento de instrução, bordas de 250 kt abaixo de 10.000 ft, regras de luzes de decolagem/toque final e classificação de superfícies duras/não pavimentadas.
  - `DifficultRunwayRewardTests.cs`: validação do bônus bush (+25%) para destino tier 2 com grama, dest tier 4 com asfalto longo sem bônus e fallback sem dado de pista.
  - `CoherentUsedAircraftProvisioningTests.cs`: validação do caso real PP-UZJ (7.221 h célula, TSMO 0,8 h -> peças do motor <= 0,8 h, peças estruturais envelhecidas proporcionalmente, KnownHealth == health).
  - `TelemetryDefinitionTests.cs`: conformidade de layout de memória do `SimTelemetry` aprovada (6/6).
- `dotnet build` 0 erros, `dotnet test` 850/850 aprovados (100%), fuzz testing (10 corridas) aprovado com 100% sem nenhuma regressão.
- Não testado no MSFS.

## 2026-11-22 - Cancelar um voo ativo avança o relógio de carreira (Claude)
- `FlightMonitor.CancelMonitoringAsync` chama `FlightService.ProcessFlightCancellationAsync` (tempo simulado voado + 0,5 h de solo) depois de salvar o estado; o método existia mas nunca era chamado (pendência de 2026-11-19). Teste `FlightCancellationClockTests`. Suíte 873/873. Não testado no MSFS.

## 2026-11-22 - Contratos da Fase 2: Levantamento Aéreo e Lançamento de Paraquedistas (FASES-2B) (Gemini)
- **Categoria 3.6 Serviços Aéreos Especializados (SAE) (`MissionRules.cs`):** adicionada nos Tiers 2, 3 e 4 com os contratos:
  - `3.6.1` Levantamento / Fotografia Aérea: área retangular de 2–4 NM de lado a 5–25 NM da origem, altitude-alvo sorteada (2.000–6.000 ft MSL), velocidade-alvo (cruzeiro × 0,75), 3–5 pernas paralelas na orientação sorteada. Conclusão exige cruzar a área de borda a borda com rumo dentro de ±15° da orientação, altitude ±150 ft e velocidade ±10 kt em $\ge 80\%$ das amostras dentro da área para cada perna, com retorno à origem. Família `aerofoto` (base $600, 6,0/NM do percurso estimado, escala 1,0, M_Tipo 1,15).
  - `3.6.2` Lançamento de Paraquedistas: zona de salto no aeródromo de origem, altitude de salto 10.000–12.000 ft AGL, 4–10 paraquedistas. Conclusão exige subida até altitude de salto a $\le 2$ NM da zona, estabilização dentro de ±200 ft e IAS $\le 1,3 \times Vs1$ (ou $\le 90$ kt) por $\ge 30$ s contínuos sobre a zona (salto feito) e descida com pouso na origem. Choque térmico da CHT na descida (> 28 °C/min) emite aviso explicativo no resultado do voo sem reprovação. Paraquedistas saltam em voo e são isentos da taxa de embarque de passageiros no pouso. Família `paraquedismo` (base $120/saltador + 4,0/NM, `paxAtLeastOne`, escala 1,0, M_Tipo 1,15).
- **Rastreadores de Domínio (`Domain/Missions/`):**
  - `SurveyLegTracker`: projeção em coordenadas rotacionadas $u$ (longitudinal) e $v$ (transversal), validação contínua de tolerâncias e cômputo de travessia borda-a-borda ($\Delta u \ge 0.75 \times L$).
  - `SkydiveDropTracker`: rastreia janela estável contínua de 30 s sobre a zona de salto e monitora choque térmico da CHT durante a descida após o salto.
  - `MissionCompletion.Evaluate`: estendido com validações dos contratos 3.6.1 e 3.6.2, gerando motivos descritivos quando incompleto e repassando alertas de choque térmico.
- **Persistência de Dados e Integração:**
  - `Mission.cs` e `AppDbContext.EnsureSchemaUpdatedAsync`: adicionada coluna `ContractData` (TEXT DEFAULT NULL) para parâmetros específicos serializados em JSON (`SurveyContractData` e `SkydiveContractData`).
  - `PartFlightTracker.cs` e `PartFlightSession.cs`: exposta a propriedade `LatestChtCoolingRate`.
  - `FlightMonitor.cs`: inicialização e alimentação dos rastreadores com telemetria e taxa de resfriamento da CHT, repassando ao `FlightService.ProcessFlightFinishAsync`.
  - `FlightService.cs`: avaliação de conclusão, isenção de taxa de embarque no pouso para paraquedistas e inclusão de avisos no resultado.
- **Calibração em dados:** `career_phases.json` (3.6.x na fase 2), `mission_pay.json` (famílias `aerofoto` e `paraquedismo`) e `xp_rules.json` (M_Tipo 1,15).
- Suíte de testes: `Phase2ContractTests.cs` (13 novos testes) cobrindo rastreadores, completion, gerador, migração de banco e isenção de taxas. Total de 886/886 testes xUnit aprovados (100%), fuzz testing (10 corridas) com 100% de aprovação e 0 falhas.
- Não testado no MSFS / não visto na tela.

## 2026-11-22 - Contratos das fases 3 a 7 (FASES-2C) (Gemini)
- **Subtipos de Contratos das Fases 3 a 7 (`MissionRules.cs`):** adicionadas categorias dedicadas nos Tiers 1, 2 e 3 (`1.7.1`–`1.7.8`, `2.7.1`–`2.7.4`, `3.7.1`–`3.7.2`):
  - **Fase 3:**
    - Fretamento executivo (`1.7.1`, `2.7.1`, `3.7.1`): origem tier 1–3 → destino tier 3–4 do mesmo país; 2–6 pax; 100–400 NM; família `taxi_aereo` (M_Tipo 1,25).
    - Carga frágil / expressa (`1.7.2`, `2.7.2`, `3.7.2`): 100–800 lb; 80–400 NM; Urgency 1,3; expiração curta (1–3 h); família nova `carga_fragil` (escala 0.7 da `aog`, M_Tipo 1,35).
  - **Fase 4:**
    - MEDEVAC dinâmico (`1.7.3`, `2.7.3`): destino com maior pista $\le 4.000$ ft ou não pavimentada; 1–2 pax + 300–600 lb; Urgency 1,5; expiração 1–2 h; família `medevac` (M_Tipo 1,50).
    - VIP internacional curto (`1.7.4`, `2.7.4`): destino em país vizinho/diferente (`Country` diferente); 300–900 NM; 2–8 pax; família `taxi_aereo` (M_Tipo 1,25).
  - **Fase 6:**
    - Ponte aérea / troncal (`1.7.5`): hub ↔ hub do mesmo país (tier 1 ↔ tier 1); 150–220 pax; 150–700 NM; família `linha` (M_Tipo 1,25).
    - Cargueiro noturno (`1.7.6`): hubs tier 1; 20.000–60.000 lb; 300–1.500 NM; texto "voo noturno"; família `linha` com M_Tipo 1,00 para carga.
  - **Fase 7:**
    - Long-haul internacional (`1.7.7`): > 2.500 NM entre hubs de países diferentes; 200–400 pax; família `linha` (M_Tipo 1,25).
    - Fretamento global / carga de alto valor (`1.7.8`): > 2.000 NM; carga 30.000–100.000 lb; Urgency 1,2; família nova `carga_global` (escala 0.28 da `linha`, M_Tipo 1,35).
- **Gerador de Missões (`MissionGenerator.cs`):**
  - Implementada filtragem operacional de destinos para os 8 contratos em `CreateMissionFromSubCategory` e `CreateMissionFromSubCategoryInMemory`, garantindo destino em mesmo país / país diferente, hubs tier 1, aeródromos com restrição de pista ($\le 4.000$ ft ou não pavimentada) e faixas de distância/pax/carga.
- **Calibração e Economia:**
  - `career_phases.json` e `CareerPhaseCatalog.cs`: mapeamento completo de todos os 14 subtipos novos para suas fases regulatórias (3, 4, 6 e 7), com fallback seguro embutido.
  - `mission_pay.json` e `MissionPayTable.cs`: adicionadas famílias `carga_fragil` (escala 0.7) e `carga_global` (escala 0.28) com paridade e ordenação 1:1.
  - `xp_rules.json`, `XpRules.cs` e `XpCalculator.cs`: configurados `carga_fragil: 1.35`, `carga_global: 1.35` e `1.7.6: 1.00`.
- **Suíte de Testes:**
  - `Phase3To7ContractsTests.cs`: 12 testes cobrindo integridade do catálogo, oráculos do gerador com sementes fixas para os 8 contratos, regras geográficas/pistas de destino, escalas de pagamento e persistência no banco. Total de 898/898 testes aprovados (100%).
- Não testado no MSFS / não visto na tela.

## 2026-11-22 - Calibração do pagamento com XP e fases (Claude)
- `mission_pay.json`: instrução 2,0; táxi aéreo, regional, linha e carga global 0,35. Rodada de 3 anos registrada em `docs/plans/economia-estabilidade.md` › 4.5 (Médio: 1ª compra dia 55–113, classe F no 1º ano; Difícil ≈ 1,5× mais lento; sem falências).
## 2026-11-22 - Locais reais, textos de RP e marcadores no briefing (MISSOES-LOCAIS) (Gemini)
- **Bancos de locais reais (`Application/Services/PlacesDataService.cs` e `Data/places/`):**
  - Integração com lazy loading das 3 bases em JSON: `tourist_pois.json` (pontos turísticos com indexação espacial por células de 1° de lat/lon), `training_aerodromes.json` (aeródromos de instrução unificados a `HubDataService.IsInstructionHub`) e `dropzones.json` (zonas de salto de paraquedismo com coordenadas).
  - Csproj de Infrastructure atualizado para copiar `Data\places\*.json` para a pasta de saída.
  - Registro de `IPlacesDataService` como Singleton no container de injeção de dependência (`App.xaml.cs`).
- **Pontos de sobrevoo por coordenada (`Domain/Missions/OverflyWaypointCodec.cs` e `OverflyPoint`):**
  - Novo padrão de codificação para waypoints: `P:<nome>@<lat>,<lon>[,<raioNM>[,<maxAglFt>]]`.
  - Suporte a raio horizontal customizado (padrão 2.0 NM) e teto AGL customizado (padrão 3.000 ft) em `OverflyPoint` e `OverflyTracker`.
  - Atualização do `FlightMonitor` para carregar coordenadas `P:` direto do codec usando elevação do aeródromo de origem, e de `MissionCompletion` para checar todos os waypoints visitados.
- **Remodelação dos subtipos de voo local (`MissionGenerator.cs` e `MissionRules.cs`):**
  - `3.4.1` (Instrução local): origem = destino, área de instrução a 5–15 NM (raio 3 NM, até 6.000 ft AGL) evitando tráfego de aeroportos tier 1–2, 1–3 TGLs requeridos, manobras procedurais no briefing e distância calculada `2 × dist + 10 NM`.
  - `3.4.3` (Voo de cheque): origem = destino (removida travessia de 30–120 NM), 1 pax examinador, 2 TGLs, texto citando pane simulada de motor e examinador credenciado.
  - `3.5.1` (Panorâmico): busca POIs reais a 5–30 NM (até 45 NM) via `FindPois`, sorteando de 2 a 3 pontos ponderados por popularidade `p`, espaçados em $\ge 3$ NM e ordenados por azimute; fallback para aeródromos quando não houver POIs.
  - `3.6.1` (Aerofoto): prioriza centralizar a área de levantamento em POI geográfico de infraestrutura/natureza a 5–25 NM com cliente institucional contextualizado.
  - `3.6.2` (Paraquedismo): centraliza na zona de salto do aeródromo quando existente, com altitude conforme a motorização (pistão 10.000–12.000 ft AGL, turboélice 12.000–14.000 ft AGL) e nome da DZ no briefing.
  - Calibração de pesos: aeródromo com DZ ganha +40 em SAE (3.6) e 80% de chance de 3.6.2 contra 20% de 3.6.1 (sem DZ: 15% vs 85%). Aeródromos de instrução recebem +45 em 3.4 e +15 em 3.5.
- **Catálogo de Textos de RP (`Infrastructure/Data/mission_texts.json` e `Domain/Missions/MissionTextCatalog.cs`):**
  - 59 subtipos com $\ge 3$ variantes imersivas de título e briefing de operações em português, com interpolação limpa de marcadores e sem deixar chaves soltas `{}`.
- **Visualização tática no Briefing (`Domain/Missions/ContractMarkerBuilder.cs`, `FlightMapControl.xaml.cs` e `BriefingViewModel.cs`):**
  - Renderização no mapa de briefing de marcadores amarelos para pontos turísticos e áreas de instrução, círculos tracejados de raio para áreas, círculos neon laranja para zonas de salto, e retângulo ciano com pernas de voo finas para aerofoto.
  - Enquadramento automático cobrindo aeronave, pernas e todos os pontos/áreas contratuais.
- **Suíte de Testes:**
  - Adicionados testes em `FlightCareer.Tests/Missions/`: `OverflyWaypointCodecTests`, `OverflyTrackerTests`, `MissionCompletionLocalTests`, `ContractMarkerBuilderTests`, `MissionTextCatalogTests`, `PlacesDataServiceTests`, `MissionGeneratorLocalTests`.
  - Total de 925/925 testes xUnit aprovados (100%).
- Não testado no MSFS / não visto na tela.

## 2026-11-22 - Zonas de paraquedismo e aeródromos turísticos curados (Claude)
- `tools/places/dropzones_curated.csv`: 32 zonas de salto da lista do usuário (11 no Brasil + 21 no mundo), com nome e descrição; `dropzones.json` passa a 180 zonas (13 no Brasil). A descrição entra no texto do 3.6.2 (`{dz_note}`).
- `tools/places/scenic_aerodromes_curated.csv`: 16 aeródromos de voo panorâmico (8 no Brasil) com as atrações; `scenic_aerodromes.json` com 59 pontos de coordenada real (banco de pontos ou Wikidata, ≤ 75 NM). Ainda não usado pelo gerador.
- `build_places.py --lnm`: confere cada código na base do MSFS 2024 (o OurAirports traz KL65/KE60/KX26, o simulador usa L65/E60/X26; Skydive Dubai = OMDU); 13 aeródromos de instrução que não existem no simulador saíram.
- `PlacesDataServiceTests`: o teste de Boituva usava SDTI (código errado; Boituva é SDOI) e passava sem conferir nada; trocado por zonas curadas reais.

## 2026-11-22 - Aeródromos turísticos curados no voo panorâmico e ferry de entrega (PANORAMICOS-CURADOS) (Gemini)
- **Base de aeródromos turísticos (`PlacesDataService.cs` e `IPlacesDataService.cs`):**
  - Carga lazy e thread-safe de `scenic_aerodromes.json` (16 aeródromos e 59 atrações com coordenadas reais), expondo `ScenicPoint`, `ScenicAerodromeInfo`, `ScenicAerodromesCount` e lookup `GetScenicAerodrome(icao)`.
  - Construtor com injeção de dependência atualizado para permitir mocks determinísticos nos testes.
- **Gerador de voos panorâmicos (`MissionGenerator.cs`):**
  - Em aeródromos turísticos com $\ge 2$ pontos curados, `CreateScenicTourMissionSync` seleciona de 2 a 4 atrações da lista curada sem peso de popularidade, mantendo distância mínima de 2 NM entre si e ordenadas por azimute, calculando a distância pelo percurso real `origem ➔ pontos ➔ origem`.
  - Injeção dos novos marcadores operacionais `{scenic_name}` (nome do circuito turístico) e `{scenic_note}` (descrição contextual) nos dicionários de renderização de briefing. Fallback seguro para aeródromos sem curadoria via busca espacial `FindPois`.
  - Calibração de pesos em `GetCalibratedCategories`: aeródromos turísticos curados recebem bônus de +40 na Categoria 3.5 (importando a categoria caso ausente no Tier do aeroporto, ex.: Tier 1), com distribuição interna de 70 para 3.5.1 (voo panorâmico) contra 15/15 para 3.5.2 (TGL) e 3.5.3 (translado básico).
- **Textos de RP (`mission_texts.json`):**
  - `3.5.1`: 2 novas variantes utilizando `{scenic_name}` e `{scenic_note}`.
  - `3.5.3`, `1.3.3` e `4.2.2`: 2 novas variantes cada focadas em entrega técnica / ferry de aeronave ao proprietário (pós-revisão na oficina ou venda/entrega rural).
- **Suíte de Testes:**
  - `PlacesDataServiceTests`: teste de carregamento do arquivo real com 16 aeródromos e validação dos pontos e notas de SBJR.
  - `MissionGeneratorLocalTests`: testes com dados mockados injetados validando waypoints `P:`, atributos (raio 1,5 NM, 3.000 ft AGL), percurso real, ausência de marcadores soltos, fallback para POIs, skew de peso em 500 corridas (~70% de 3.5.1) e importação de categoria em aeródromo Tier 1.
  - Total de 934/934 testes xUnit aprovados (100%).
- Não testado no MSFS / não visto na tela.
## 2026-11-22 - Escada de aviões dos bots determinística (Claude)
- `CareerProgressionSimulator.BuildAircraftLadder`: o avião de cada degrau era o primeiro do banco que continha algum dos nomes, e a ordem do banco segue a chave Guid (muda a cada execução). Na t8 o degrau H do Médio caiu no Citation Longitude ($28 mi) em vez do CJ4 e os bots ficaram parados na classe G do dia 540 ao 1095. Agora a escolha segue a ordem de preferência dos nomes (`FindByPreference`). Teste `AircraftLadderTests`.

## 2026-11-23 - Filtro do quadro de missões volta a funcionar (Claude)
- Desde as 7 fases (FASES-1), `Mission.MissionCategoryType` guarda o nome da fase, então a etiqueta e os filtros do quadro (INSTRUÇÃO, TÁXI AÉREO, CARGA / SAE...) não reconheciam mais nada e todas as missões apareciam como "GERAL".
- Nova classificação pura pelo subtipo (`Domain/Missions/MissionBoardCategory`, a partir de `Mission.SubId`), usada pela etiqueta, pelas cores e pelo filtro (`MissionDisplayItem.BoardKind`, `MainViewModel.ApplyMissionFilters`). Missões sem subtipo (banco antigo, ferry manual) usam o texto antigo como reserva.
- Instrução = 3.4.x e 3.5.2 (TGL); panorâmico em Táxi aéreo; aerofoto e paraquedismo em SAE; contratos das fases 3–7 nas categorias correspondentes; translados e governo em Geral.
- Teste `MissionBoardCategoryTests` falha se um subtipo novo do gerador ficar sem categoria. Não visto na tela.

## 2026-11-23 - Listas curadas de aeroportos, perfis e papel dos aeródromos turísticos (Claude)
- Listas enviadas pelo usuário gravadas em `tools/places/*_curated.csv` (turísticos, pontos turísticos, carga, linha, executiva, alimentadores, saúde, instrução, manutenção, agro, militares/uso misto), com os códigos conferidos na base do MSFS 2024 (Little Navmap) e corrigidos ou removidos quando não existiam (lista em `docs/plans/catalogo-missoes.md` › 9.1).
- `tools/places/airport_profiles.py` gera `Data/places/airport_profiles.json` (8.752 aeroportos do MSFS com perfis curados e heurísticos). Ainda não usado pelo gerador (entra no catálogo de missões).
- Aeródromo turístico ganhou papel (`role`): só os de `panoramico` puxam a categoria de voo panorâmico no gerador; os de `destino` só fornecem os pontos (`ScenicAerodromeInfo.IsScenicBase`). `build_places.py` junta os pontos de `scenic_pois_curated.csv` aos aeródromos.
- Tabela de pesos por missão e perfil de aeroporto proposta no plano (§ 9.3). Não visto na tela.

## 2026-11-23 - Catálogo de missões em JSON com IDs de texto estáveis (CATALOGO-MISSOES) (Gemini)
- **Catálogo de missões centralizado em JSON (`Infrastructure/Data/mission_catalog.json` e `Domain/Missions/MissionCatalog.cs`):**
  - Todas as 59 missões extraídas de código puro (`MissionRules.cs` e condicionais numéricas) para um catálogo em JSON unificado, embutido como recurso em `FlightCareer.Domain` e copiado como arquivo de dados, com IDs estáveis em snake_case em português (`instrucao_local`, `voo_de_cheque`, `voo_panoramico`, `circuito_tgl`, `translado_basico`, `aerolevantamento`, `paraquedismo`, `linha_domestica_hub`, `carga_fragil`, `medevac_dinamico`...).
  - Mapeamento bidirecional retrocompatível via `legacyCodes` em `MissionCatalog.Shared` com métodos `Get(id)`, `Normalize(idOuCodigo)`, `Tiers` e `AirportAdjustments`.
- **Esquema de dados e entidades puras (`Domain/Missions/`):**
  - Entidade pura `MissionDefinition` com `id`, `legacyCodes[]`, `name`, `description`, `board`, `vip`, `phase`, `payFamily`, `xpType`, `generator`, `destination`, `fixedTexts`, `rangeRules`, `paxRules`, `lbsRules`, `minExpHours`, `maxExpHours`, `urgency`, `requiresHardSurface`, `minRunwayDest`, `luggageWeightKg`.
  - Value object `ProbRange` suportando deserialização dual (array `[peso, min, max]` ou objeto com propriedades) via `ProbRangeJsonConverter`.
- **Refatoração do Gerador (`Application/Services/MissionGenerator.cs` e `MissionRules.cs`):**
  - `MissionRules.cs` transformado em adaptador fino que monta `Rules` a partir do catálogo.
  - Eliminação de todos os `if (sub.Id == "x.y.z")` do gerador (tanto síncrono quanto em memória), passando a rotear regras de destino por `Destination.Rule` (`same_country_tier_3_4`, `other_country`, `tier1_same_country`, `tier1`, `tier1_other_country`, `tier_max_2`, `short_or_unpaved`), geradores especiais por `Generator` (`local_instruction`, `checkride`, `scenic_tour`, `tgl_circuit`, `basic_ferry`, `survey`, `skydive`), textos fixos por `FixedTexts` e destinos preferenciais por `Destination.PreferredHubs`.
  - Ajustes de pesos e importação dinâmica de grupos por aeródromo em `GetCalibratedCategories` refatorados para interpretar genericamente a lista `AirportAdjustments` de dados.
- **Consumidores atualizados para os novos IDs:**
  - `MissionPayTable.cs`: resolve família pelo `def.PayFamily` via `MissionCatalog.Shared.Get(subId)`; remoção de `prefixes` numéricos de `mission_pay.json` e dos defaults.
  - `CareerPhaseCatalog.cs`: resolve fase pelo `def.Phase` do catálogo; remoção de `subIdToPhase` numérico de `career_phases.json`.
  - `XpRules.cs` e `XpCalculator.cs`: chave de multiplicador `"cargueiro_noturno"` em `xp_rules.json`; `XpCalculator` resolve multiplicador através de `def.XpType`/`def.PayFamily`.
  - `MissionTextCatalog.cs` e `mission_texts.json`: re-chaveado pelos novos IDs de texto (incluindo `voo_panoramico-turistico`); `Render` normaliza códigos legados transparentemente.
  - `MissionCompletion.cs`, `FlightService.cs`, `FlightMonitor.cs`, `CareerProgressionSimulator.cs`, `MissionBoardCategory.cs` e `MissionDisplayItem.cs`: migrados para consultar `generator`, `board` ou `vip` do catálogo em vez de strings numéricas.
- **Persistência e Migração de Banco (`AppDbContext.cs`):**
  - Instruções idempotentes em `EnsureSchemaUpdatedAsync` atualizando `SubId` na tabela `Missions` para o ID textual correspondente a cada um dos 59 códigos antigos.
- **Garantia de Equivalência e Testes:**
  - Snapshot golden com 270 missões em 14 aeródromos (T1 a T4) validado com equivalência estrita (100% idêntico) via `MissionCatalogEquivalenceTests`.
  - Suíte de integridade `MissionCatalogTests` aprovada (unicidade dos 59 legacyCodes, validação de campos, integridade de textos, normalização, ausência de códigos literais no código-fonte de produção e teste de migração SQLite).
  - Simulação de 90 dias do `career-stab` (`--days 90 --bots 1 --seed 7 --fast`) com resultados financeiros e operacionais idênticos nos 9 perfis (Easy, Medium, Hard × Agressivo, Cauteloso, Equilibrado).
  - Total de 969/969 testes xUnit aprovados (100%). Não testado no MSFS.

## 2026-11-23 - Atrações turísticas curadas no mundo todo (Claude)
- `scenic_aerodromes.json` passou de 16 para 409 aeródromos (59 bases de voo panorâmico e 350 destinos turísticos, 844 pontos com coordenadas do Wikidata), gerado por `tools/places/build_places.py` a partir de `scenic_aerodromes_curated.csv` e `scenic_pois_curated.csv`. Só as bases `panoramico` reforçam a categoria de voo panorâmico.
- `airport_profiles.json` regerado: 402 aeroportos com a etiqueta `turistico_curado` (antes 16).
- Snapshot do gerador (`missions_golden.json`) regravado: só mudaram as missões de SBJR, SBFI e SDUB (bases panorâmicas com atrações novas); os outros 11 aeroportos ficaram idênticos. `PlacesDataServiceTests` não fixa mais a contagem em 16.
- Não testado no MSFS / não visto na tela.

## 2026-11-23 - Carga paga do 747-400 e faixas de missão aprovadas (Claude)
- `aircrafts.csv`: `CargaMax(lbs)` do Boeing 747-400 (passageiros) corrigido de 475.000 (carga útil) para 148.412 lb (MZFW − OEW). Outros jatos com o mesmo tipo de erro listados no plano `catalogo-missoes.md` › 9.7.2, aguardando aprovação.
- Plano: tabela de faixas (§ 9.7.1) aprovada pelo usuário, limitada aos aviões do `aircrafts.csv`.

## 2026-11-24 - Carga paga máxima dos jatos no aircrafts.csv (Claude)
- `CargaMax(lbs)` de 12 aviões trocado da carga útil para a carga paga máxima estrutural (MZFW − OEW), com fonte no plano `catalogo-missoes.md` › 9.7.2: 747-8i 157.500, A400M 81.600, 787-10 126.300, A330-300 114.000, A330-200 109.100, A321 56.200, A321LR 51.800, A320neo 46.300, 737 MAX 8 46.000, Longitude 3.200, PC-24 3.100, SkyCourier 5.500.
- Efeito: o quadro e os bots deixam de aceitar nesses aviões missões acima da carga paga real. Bancos existentes guardam o valor antigo em `AircraftTypes`; o banco de teste será apagado na etapa 2. Não testado no MSFS / não visto na tela.

## 2026-11-24 - Catálogo de 69 tipos de missão com pesos por perfil de aeroporto (E2A, Gemini + Claude)
- `mission_catalog.json` v2 com os 69 tipos da lista geral (`docs/plans/catalogo-missoes.md` › 6): 52 sorteados, 17 desligados até terem regras/HUD próprios (E2C). Os 59 ids antigos foram fundidos (`legacyIds`) e os textos de RP somados por id novo.
- Peso de cada missão = base do porte do aeroporto × etiquetas do `airport_profiles.json` (teto ×12; base militar pura ×0,2 nas civis), em `Domain/Missions/MissionWeighting` e `AirportProfileCatalog`.
- Núcleo único `Application/Services/MissionBoardCore` para o jogo e os bots: perfis correlacionados (distância, pax e carga juntos), bagagem sorteada por pessoa com tripulação de apoio, destino preferencial por peso (nunca exclusivo). Caminho do jogo: candidatos por área real (sem o corte alfabético), mantendo todos até 600 NM.
- `FlightCareer.Replay board-report` (tipos por aeroporto, destinos preferenciais, faixas de pax/carga/bagagem/distância, cobertura).
- Removida a tradução de `SubId` antigos no banco (banco de teste apagado pelo usuário).
- Revisão: `docs/reviews/E2A-revisao.md`. Não testado no MSFS / não visto na tela.

## 2026-11-24 - Cidade errada em códigos reaproveitados e plano do importador de aeroportos (Claude)
- `DataSeeder.EnrichFromCsvAsync`: com o banco do Little Navmap, a cidade, o país e a elevação faltantes vinham do `airports.csv` casando só o código; nos códigos reaproveitados (433 de 7.822 a mais de 10 NM do MSFS) entrava a cidade de outro aeródromo (ex.: SBOK, Amontada/CE no MSFS, recebia "Lucas do Rio Verde"). Agora só completa quando a posição do csv bate até 10 NM (`DataSeeder.IsSameAirportPosition`, função SQLite `fc_same_place`). Testes `AirportCsvEnrichmentTests`.
- Plano novo `docs/plans/importador-aeroportos-simconnect.md`: base de aeroportos e pistas lida do simulador aberto pelo SimConnect (como o Little Navmap faz no MSFS 2024), com o Little Navmap e o csv como reserva.
- Não conferido num banco gerado de verdade nem no MSFS.

## 2026-11-24 - Quadro de SBMT conferido: peso militar, navegação VFR e bagagem (Claude)
- Conferência das 120 missões geradas em SBMT (banco do usuário): `militar_can` saía em 18 % porque a etiqueta `militar` casava os subtipos militares de aeroporto de uso misto. Novo casamento exato `etiqueta!` (`AirportProfileCatalog.MatchTag`); `militar_can` passa a usar `militar!` ×6 e `militar_misto` ×2.
- `navegacao_vfr` nunca saía (faixa 80–250 NM com limite de 80 NM = nenhum destino): faixa de uma etapa 30–90 NM até a E2C (triângulo).
- Bagagem por pessoa também nos geradores especiais (instrução, cheque, panorâmico, levantamento, paraquedismo): `MissionBoardCore.WithPersonLuggage`.
- Snapshot regravado. Pendente: o gerador de paraquedismo ainda sorteia 4–10 saltadores fixos em vez dos perfis do catálogo.

## 2026-11-24 - Pista mínima por perfil de missão e tier por movimento (TIER-A) (Gemini)
- **Parte A — Pista mínima por perfil de missão (capacidade física):**
  - Adicionados `MinRunwayFt` e `Paved` a `MissionProfileDefinition` (`Domain/Missions/MissionDefinition.cs`).
  - Preenchidos todos os 110 perfis das 69 missões em `mission_catalog.json` pelas classes de aeronave de referência (§ 9.7.1 de `catalogo-missoes.md` e § 2 de `tiers-por-movimento.md`): widebody 8.000 ft/sim, narrowbody 4.300 ft/sim, jato executivo longo alcance 4.900 ft/sim, jato executivo médio 3.400 ft/sim, turboélice regional 3.600 ft/sim, turboélice leve 2.500 ft/não, utilitário bush 1.200 ft/não e instrução/local 0 ft.
  - `MissionBoardCore.GenerateBoard` recebe a melhor pista da origem (`originRunway`). `PickProfile` filtra apenas os perfis que a pista da origem comporta; se nenhum perfil couber, o tipo de missão tem peso 0 na origem e sai do sorteio prévio em `GenerateBoard`.
  - `FilterCandidates` exige que os candidatos a destino tenham pista $\ge$ `MinRunwayFt` do perfil e pavimentada se `Paved` (candidatos sem pista conhecida são excluídos quando `MinRunwayFt > 0`).
  - Chamadores `GenerateMissionsAsync` e `GenerateMissionsInMemory` em `MissionGenerator.cs` atualizados para obter e repassar a melhor pista da origem.
- **Parte B — Tier por movimento e sinais estruturais:**
  - Classificador puro `AirportTierClassifier` e regras configuráveis em `AirportTierRules` (`Domain/Airports/`) com arquivo de calibração `airport_tier_rules.json` (`Infrastructure/Data/`).
  - Pontuação combinada: etiquetas curadas do `AirportProfileCatalog` (excluindo heurísticas `isolado`, `interior_distante`, `fazenda`, `altitude`, `turistico_poi`), estrutura do simulador via Little Navmap (ILS, procedimentos, gates, pontes, pátio de carga, torre, pista e pavimentação) e OurAirports (`type` e `scheduled_service` do CSV validados por proximidade geográfica).
  - Regra de corte: Tier 1 $\ge 90$; Tier 2 $\ge 45$; Tier 3 $\ge 15$ ou qualquer pista pavimentada; senão Tier 4. Pistas não pavimentadas com teto em Tier 3 (e exigem $\ge 45$).
  - `Airport` e `AppDbContext`: persistência dos sinais estruturais (`IlsEnds`, `Approaches`, `Gates`, `CargoParking`, `HasTower`, `LongestRunwayFt`, `LongestRunwayPaved`, `TierScore`) com `ALTER TABLE` idempotente.
  - `DataSeeder`: `SeedFromLittleNavmapAsync` lê estrutura do banco do simulador e classifica os aeroportos em C#, gravando em lote via transação SQLite; `EnrichFromCsvAsync` valida posições geográficas com `fc_same_place` e recalcula os tiers com dados do OurAirports. Caminho só-CSV classificado com o que houver.
- **Ferramentas CLI e Relatórios:**
  - Subcomando CLI `FlightCareer.Replay tier-report` implementado (`TierReporter.cs`), gerando `tier-report.md` e `tier-report.csv` sobre a base completa do Little Navmap e OurAirports.
  - Execução de `board-report` demonstrando a eliminação de `longo_curso` (0,0%) e perfis pesados de widebody de `carga_paletizada` (1,0%, restrito a narrowbody) e `ponte_aerea` (1,3%) em SBMT.
- **Testes e Snapshot:**
  - Suíte completa de testes unitários para o classificador (`AirportTierClassifierTests`) e regras de pista por perfil (`MissionRunwayProfileTests`).
  - Snapshot `missions_golden.json` regravado e validado. 997/997 testes aprovados. Não testado no MSFS / não visto na tela.

## 2026-11-24 - País pelo CSV, linha aérea entre aeroportos de linha e recálculo do tier (TIER-A rodada 2) (Gemini)
- **Correção de País pelo CSV (`iso_country`):**
  - Em `DataSeeder.EnrichFromCsvAsync` e `FlightCareer.Replay/TierReporter.cs`, sempre que a posição geográfica confere com o CSV OurAirports (`IsSameAirportPosition` / `fc_same_place`), o campo `Country` recebe o `iso_country` do CSV sempre, corrigindo regiões ICAO (ZS, UI, UW...) para códigos ISO oficiais (CN, RU...). Se a posição for divergente (> 10 NM), mantém a resolução inicial.
- **Linha aérea entre aeroportos de linha (`RequireAnyTag`):**
  - Implementado suporte ao campo `requireAnyTag` em `MissionDefinition` (origem), `MissionDestinationRule` (destino) e `MissionProfileDefinition` (origem e destino daquele perfil).
  - Em `MissionWeighting.OriginWeight` e `MissionBoardCore.GenerateBoard`, missões sem etiquetas correspondentes na origem têm peso 0 e são descartadas. Perfis sem compatibilidade de tag na origem não são sorteados em `PickProfile`.
  - Em `MissionBoardCore.FilterCandidates`, destinos sem etiquetas correspondentes são descartados.
  - No `mission_catalog.json`: `ponte_aerea`, `troncal_domestico`, `regional_internacional`, `longo_curso`, `feeder_inbound` (destino) e `feeder_outbound` (origem) configurados com `["linha", "linha_auto"]`. Perfis com `minRunwayFt >= 4300` de `carga_paletizada`, `carga_overnight`, `ecommerce` e `cadeia_fria` configurados com `["linha", "linha_auto", "carga:cargueiros", "carga_auto"]`.
  - Resultado no `board-report`: SBMT gera 0,0% de `ponte_aerea`, `troncal_domestico` e `carga_paletizada`, enquanto SBRJ gera 27,4% de ponte aérea e 13,5% de troncal.
- **Recálculo do Tier por versão das regras (`TierRulesVersion`):**
  - Adicionadas colunas `CsvType`, `ScheduledService` e `TierRulesVersion` em `Airport` e `AppDbContext.EnsureSchemaUpdatedAsync`.
  - `DataSeeder.EnsureTiersUpToDateAsync` chamado no início de `SeedAsync` (antes de qualquer `return`), detectando aeroportos com `TierRulesVersion != AirportTierRules.Default.Version` e recalculando todos em lote a partir dos sinais estruturais gravados e etiquetas curadas numa transação única, com log informativo. Semeaduras novas gravam a versão atual.
- **Testes e Snapshot:**
  - Testes unitários dedicados em `AirportTierRecalculationAndCountryTests.cs` (país corrigido pelo CSV e recálculo em SQLite em memória) e `MissionRunwayProfileTests.cs` (restrições de etiquetas de linha e perfis).
  - Snapshot `missions_golden.json` regravado: apenas SBMT teve alterações nas missões sorteadas (deixando de sortear tipos de linha). Todos os outros 13 aeródromos permaneceram idênticos.
  - Total de 1.004/1.004 testes aprovados (100%). Não testado no MSFS / não visto na tela.

## 2026-11-24 - País pela coordenada com os contornos do Natural Earth (PAIS-A) (Gemini)
- **Localizador de Países por Contorno (`CountryLocator`):**
  - Implementado `FlightCareer.Domain.Geo.CountryLocator` puro, carregando polígonos simplificados e bounding boxes de 249 países do Natural Earth (`Data/places/country_borders.json`), com singleton `CountryLocator.Shared` e método `Locate(lat, lon)` (filtro rápido por bbox e algoritmo Ray Casting even-odd no anel externo respeitando exclusão de buracos/enclaves como Lesoto em ZA).
  - Suporte a anéis planos de alta performance e conjunto imutável de códigos oficiais conhecidos (`KnownIsoCodes`).
- **Indexador Espacial e Vizinho Mais Próximo (`AirportSpatialGrid`):**
  - Implementado `FlightCareer.Domain.Geo.AirportSpatialGrid` para busca de vizinho mais próximo em grade esférica de 1° com distância náutica Haversine (`DistanceNm`), resolvendo aeródromos costeiros e insulares a até 50 NM de um aeroporto válido sem busca quadrática.
- **Integração no `DataSeeder` e `TierReporter`:**
  - `DataSeeder.ResolveMissingCountriesAsync`: novo passo executado após o enriquecimento CSV e normalização nos dois caminhos de semeadura (Little Navmap e só-CSV). Aeródromos com país não ISO (vazio, região ICAO do sim ou numérico) recebem o país pelo contorno, pelo vizinho até 50 NM ou vazio se isolados, gravados em lote numa transação SQLite com log informativo de progresso.
  - `TierReporter` (`FlightCareer.Replay`): resolução idêntica aplicada à coluna País com seção dedicada no relatório Markdown e console (6.928 resolvidos por contorno, 89 por vizinho, apenas 13 sem país em ~5,5 s totais).
- **Testes Unitários:**
  - 4 novos testes em `CountryLocatorTests.cs` cobrindo pontos conhecidos (SP, Ufa, Bethel, Lisboa, Atlântico), buracos/enclaves (Maseru -> LS), países sem ISO_A2 padrão (Paris -> FR, Oslo -> NO) e teste SQLite em memória do `DataSeeder` (Ohio -> US, água a 10 NM -> BR, oceano -> vazio).
  - Total de 1.008/1.008 testes aprovados (100%). Não testado no MSFS / não visto na tela.

## 2026-11-24 - Remoção da camada antiga de missões (E2A-L) (Gemini)
- **Remoção de Código Morto da Geração Antiga (`MissionGenerator.cs` e `MissionRules.cs`):**
  - Removido arquivo `src/FlightCareer.Application/Services/MissionRules.cs` (183 linhas: `TierConfig`, `CategoryConfig`, `SubCategoryConfig` e `MissionRules`).
  - Removidos de `MissionGenerator.cs`: `CreateMissionFromSubCategory`, `CreateMissionFromSubCategoryInMemory`, `ResolveG20Hubs`, `ResolveDynamicHubs`, as três sobrecargas de `GetCalibratedCategories`, `AddCategoryWeight`, `PickWeighted`, `_runwayCache`, `GetDestinationRunwayAsync` e o método assíncrono privado `CreateMissionObject`.
  - Substituídas as guardas de tier em `GenerateMissionsAsync` e `GenerateMissionsInMemory` por verificação explícita `currentAirport.Tier < 1 || currentAirport.Tier > 4`.
  - Geradores síncronos de `MissionGenerator` (`CreateMissionObjectSync`, `CreateLocalInstructionMissionSync`, `CreateCheckrideMissionSync`, `CreateScenicTourMissionSync`, `CreateSurveyMissionSync`, `CreateSkydiveMissionSync`) atualizados para receber `MissionDefinition` diretamente.
  - Adaptador transitório `ToSubCategoryConfig` e chamadas intermediárias removidos de `MissionBoardCore.cs`.
  - Removida pasta `tools/missions/` contendo scripts descartáveis de migração (`generate_catalog_and_texts.py` e `patch_catalog_v2.py`).
- **Reescrita e Preservação de Testes:**
  - `MissionBoardCategoryTests.cs`: `AllSubIds()` atualizado para iterar `MissionCatalog.Shared.Missions.Values` habilitadas com seus códigos legados.
  - `MissionGeneratorLocalTests.cs`: `GetSubConfig(id)` migrado para `MissionCatalog.Shared` retornando `MissionDefinition`; teste de aeródromo turístico curado reescrito validando `turistico_curado:base` e aumento de peso de `panoramico` via `MissionBoardCore.AugmentOriginTags` e `MissionWeighting.OriginWeight`.
  - `Phase1ContractsTests.cs`, `Phase2ContractTests.cs`, `Phase3To7ContractsTests.cs` e `CareerProgressionTests.cs`: migrados para `MissionCatalog.Shared` e `MissionDefinition` mantendo asserções de fases, regras e faixas operacionais.
- **Validação e Determinismo:**
  - 1.008/1.008 testes aprovados (100%), incluindo `MissionCatalogEquivalenceTests` com snapshot `missions_golden.json` intacto (0 alterações nas missões geradas). Não testado no MSFS.

## 2026-11-24 - Jato de linha só em hub e destino das missões de saúde (Claude)
- Decisões do usuário após o `board-report`: (1) aeroporto com `linha_auto` sem `linha` curada só gera/recebe jato de linha (ponte aérea, troncal, regional internacional, longo curso; 120+ pax) se for tier 1–2: etiqueta derivada `linha_auto:hub` em `MissionBoardCore.WithDerivedTags` (`LinhaAutoHubMaxTier = 2`), usada na origem (`AugmentOriginTags`) e no filtro de destinos. Só 209 aeroportos têm `linha` curada; Ufa e Dar es Salaam (só `linha_auto`) mantêm voos se forem tier 1–2; Sorocaba e Bethel deixam de ter se forem tier 3. Alimentadores inalterados.
- (2) Peso do destino preferido nas missões de saúde de 5–6 para 150 (`orgaos`, `uti_longa_distancia`, `medevac_remoto`), com teto novo por missão `destination.maxMultiplier` = 150 (`MissionDestinationRule.MaxMultiplier`; padrão continua ×12 em `MissionWeighting.DefaultDestinationMaxMultiplier`). Voos ao destino preferido no `board-report` só-CSV: órgãos 47 % → 84 %, UTI longa distância 18 % → 86 %, medevac remoto 8 % → 20 % (poucas cidades tier 2–3 com hospital etiquetado ao alcance das pistas remotas; sem nova curadoria, por decisão do usuário). `medevac_remoto` corrigido para o sentido do plano (pista remota → cidade com hospital): destino tier 2–3, qualquer pista (antes exigia pista curta/terra e aceitava tier 1), preferência `saude:hospital_referencia` ×150 e `saude` ×30.
- Snapshot `missions_golden.json` regravado: 4 registros mudaram, todos de saúde (2 de órgãos e 2 de medevac remoto, que deixou de mandar paciente de SWCA para o SBGR). 1.022/1.022 testes. Testes `MissionLineHubAndHealthTests`.
- Medido só com o `airports.csv` (sem Little Navmap); conferir o `board-report` no PC de casa, porque o efeito depende do tier real. Não testado no MSFS / não visto na tela.

## 2026-11-24 - Primeiro lote de missões com regra própria (E2C-1) (Gemini)
- **Rastreadores puros de regras operacionais (`Domain/Missions/`):**
  - `MultiStopTracker`: paradas completas sequenciais (`GroundSpeed < 3 kt` por $\ge 5$ s simulados a $\le 1,5$ NM) em cada escala intermediária com nova decolagem exigida entre elas e verificação de reabastecimento indevido (`max(1 gal, 2%)`).
  - `StolLandingTracker`: monitoramento de pousos curtos com aproximação íngreme (ângulo médio $\ge 4^\circ$ nos últimos 300 ft AGL) e distância de parada $\le \text{clamp}(0,6 \times \text{RunwayLength}, 300, 1.000)$ ft, com detecção de vento de través $\ge 8$ kt para bônus/aviso positivo.
  - `CheckFlightTracker`: validação sequencial por passos de voo de teste técnico (atingir altitude $\pm 300$ ft, potência máxima por 10 s, manutenção dos 4 rumos cardeais por 15 s na altitude e pouso na origem).
  - `FerryRestrictionMonitor`: restrição técnica de translado com verificação de teto de voo (máximo 10 s simulados acumulados acima de $MaxAlt + 200$) ou travamento de superfícies de flaps ($\pm 5\%$).
  - `MissionTrackerResults`: agregação desacoplada dos resultados dos rastreadores para avaliação em `MissionCompletion.Evaluate`.
- **Contratos e Geradores Especiais (`ContractData.cs`, `MissionGenerator.cs`, `MissionBoardCore.cs`):**
  - Classes de dados tipadas em JSON: `MultiStopContractData`, `StopPoint`, `StolContractData`, `CheckFlightContractData`, `FerryPermitContractData`.
  - Habilitados e configurados no `mission_catalog.json` e `MissionBoardCore.cs`: `pinga_pinga` (multi_stop), `navegacao_vfr` (vfr_triangle com ponto de reportagem), `instrucao_stol` (stol_training), `check_flight` (check_flight) e `ferry_permit` (ferry_permit com preferência por destinos de manutenção MRO).
  - Paraquedismo (`skydive`): atualizado para respeitar os perfis de ocupação do catálogo (35% 3–6 pax, 50% 8–14 pax, 15% 15–20 pax com `bigAirport`) e cálculo de bagagem proporcional individual por saltador (12–16 kg em lb).
- **Textos de RP (`mission_texts.json`):**
  - $\ge 3$ variantes imersivas de títulos e descrições para cada um dos 5 novos tipos operacionais com interpolação de marcadores.
- **Integração no `FlightMonitor` e `FlightService`:**
  - Instanciação, reset operacional e atualização contínua dos rastreadores por telemetria de 1 s em `OnSimDataReceived` usando tempo simulado; repasse de `MissionTrackerResults` na conclusão do voo para o `MissionCompletion.Evaluate`.
- **Suíte de Testes e Snapshot:**
  - Suíte xUnit dedicada com 29 novos testes cobrindo todas as máquinas de estado e geradores (`MultiStopTrackerTests`, `VfrTriangleTests`, `StolLandingTrackerTests`, `CheckFlightTrackerTests`, `FerryRestrictionMonitorTests`, `PhaseE2C1GeneratorsTests`).
  - Total de 1.051/1.051 testes aprovados (100%). Snapshot `missions_golden.json` regravado refletindo a ativação dos 6 tipos. Não testado no MSFS / não visto na tela.

## 2026-11-24 - Variantes da navegação VFR (E2C-1b) (Gemini)
- **Variantes de instrução VFR (`navegacao_vfr`):**
  - Três perfis e variantes configurados no catálogo (`mission_catalog.json`) e despachados pelo gerador `CreateVfrTriangleMissionSync`:
    - `triangle` (peso 50): triângulo clássico origem → A → B → origem, etapas 25–90 NM, total 80–250 NM, paradas completas em A e B.
    - `out_and_back` (peso 25): ida e volta origem → A → origem, etapa única de 40–120 NM, total 80–240 NM, parada completa em A.
    - `triangle_tgl` (peso 25): triângulo origem → A → B → origem, etapas 25–70 NM, total 75–200 NM, toque e arremetida em A e B (`StopMode = touch_and_go`).
  - Todas as variantes mantêm ponto de reportagem `P:` na primeira perna (desvio lateral 3–8 NM), retorno para pouso na base de origem e 1 passageiro (instrutor).
- **Rastreamento de toques e arremetidas (`MultiStopTracker`):**
  - Propriedade `StopMode` em `MultiStopContractData` (`full_stop` padrão, `touch_and_go`).
  - Reutilização da regra de TGL do `TouchAndGoCounter` (`IsValidTouchAndGo`): toque $\ge 1,0$ s sem quique, GS máxima no solo $> 20$ kt, sem parada completa ($< 5$ kt) e decolagem a $\le 1,5$ NM da escala intermediária.
  - Parada completa continua aceita no modo TGL como alternativa segura de cumprimento da escala.
- **Textos por variante (`mission_texts.json`):**
  - Variantes textuais dedicadas para `navegacao_vfr:out_and_back` (citando `{stop_a}` e pouso completo), `navegacao_vfr:triangle_tgl` (citando `{stop_a}`, `{stop_b}` e toque e arremetida) e `navegacao_vfr:triangle` com fallback transparente no gerador.
- **Testes e Snapshot:**
  - 6 novos testes de unidade cobrindo modos TGL, quiques, distâncias, gerador procedural N quadros com as 3 variantes e avaliação em `MissionCompletion`.
  - Snapshot `missions_golden.json` regravado e validado.
  - Total de 1.057/1.057 testes aprovados (100%). Não testado no MSFS.

## 2026-11-24 - AGENTS.md › 7 enxuto (AGENTS-ENXUTO) (Gemini)
- **Enxugamento da seção 7 do AGENTS.md ('Estado atual dos sistemas'):**
  - Redução do tamanho da seção de 62.053 bytes para 20.669 bytes (~20,2 KB, limite <= 22 KB), eliminando números de calibração que já constam nos JSONs, repetições de IDs de tarefas e algoritmos passo a passo.
  - Reorganização em 13 sistemas coesos: Desgaste e falhas, Peças, Manutenção e Hangar, Panes persistentes, Voo e integridade, Combustível, Economia e dificuldade, XP e fases, Missões e quadro, Aeroportos e tiers, Limites por aeronave, Sensores, HUD.
  - Preservação integral das seções 1 a 6 e 8 byte a byte.

## 2026-11-24 - Regras de economia de quota (Claude)
- `AGENTS.md` › 4.9 (uma tarefa por sessão, passagem reescrita a cada sessão, nunca ler o CHANGELOG inteiro) e › 4.10 (modelo por tipo de sessão, saída filtrada para Claude e Gemini, decisões sempre nos planos, mensagens agrupadas). `CLAUDE.md` aponta para a regra.
- `docs/agents/MODELO-SPEC.md` reescrito com fatos levantados, fora do escopo, snapshot esperado e o que não rodar; `tools/agents/prompt-executor.md` com saída filtrada e sem ler arquivos grandes inteiros.
- `tools/agents/global/CLAUDE.md` ganhou a seção "Economia de quota" (vale para todos os projetos; instalar com `tools/agents/install-global.sh` em cada PC).
- `docs/agents/HANDOFF.md` reduzido ao estado atual, com os passos para configurar o PC de casa.

## 2026-11-25 - Instruções sob demanda: estado dos sistemas em docs/sistemas (SISTEMAS) (Gemini)
- `AGENTS.md` reduzido de 39.707 bytes para 14.937 bytes (limite <= 15.000 bytes): regras, comandos, armadilhas técnicas e índice tabular na seção 7.
- Criação de `docs/sistemas/` com 13 arquivos temáticos (`desgaste-falhas.md`, `pecas.md`, `manutencao-hangar.md`, `panes-persistentes.md`, `voo-integridade.md`, `combustivel.md`, `economia-dificuldade.md`, `xp-fases.md`, `missoes-quadro.md`, `aeroportos-tiers.md`, `limites-aeronave.md`, `sensores.md`, `hud.md`) e `README.md` contendo os blocos literais da antiga seção 7 e decisões técnicas da seção 6.
- Seção `Sessões e economia de quota` adicionada ao final de `docs/agents/ORQUESTRACAO.md` preservando integralmente o texto das seções 4.9 e 4.10 do AGENTS.
- Atualização das referências cruzadas em `CLAUDE.md`, `gemini.md`, `.antigravity/rules.md`, `docs/agents/MODELO-SPEC.md`, `tools/agents/prompt-executor.md` e `docs/plans/INSTRUCOES-GEMINI.md`.

## 2026-11-25 - Revisão e merge SISTEMAS (Claude)
- Revisão (`docs/reviews/SISTEMAS-revisao.md`, APROVADO): cobertura linha a linha do AGENTS antigo conferida; restaurados detalhes perdidos ao encurtar a § 6 (Key Events: prioridade e roteamento `OnObservedEvent`/`OnMaskedEvent`; Banco: regra de copiar valores entre contextos e nomes/tipos do EF; SimVar inválida). Índice com 2 classes por sistema. `AGENTS.md` final: 14.951 bytes no git. Lição nova em `LICOES.md`.
- Build 0 erros; testes 1.057/1.057.

## 2026-11-25 - Corte dos jatos de linha e 115 aeroportos com voo regular (Claude)
- `tier-report`/`board-report` no PC de casa: SDCO (58), SBTF (79) e PABE (99) são tier 2 e geravam jatos de linha.
- Decisão do usuário: aeroporto só com `linha_auto` vira `linha_auto:hub` se for tier 1, ou tier 2 com `TierScore` ≥ 100 (`LinhaAutoHubTier2MinScore`).
- Lista de 300 ICAO do usuário: 115 novos em `pax_hubs_curated.csv` (`SBQV` → `SBVC`; `OKKK` sem efeito no MSFS, que usa `OKBK`).
- `airport_profiles.py` aceita o `tier-report.csv` e usa o tier do jogo nas heurísticas; `airport_profiles.json` regenerado (10.974 → 12.385 aeroportos com perfil; bem mais `isolado`/`interior_distante`). Regras de tier na versão 3 (recalcula bancos existentes). Tier 1: 457 → 486.
- Snapshot `missions_golden.json` regenerado. Build 0 erros; testes 1.058/1.058.
- `career-stab` Médio, 1095 dias, 3 bots, base × branch: sem alertas; patrimônio de cauteloso e equilibrado no dia 1095 +1 a 2 %. Agressivo: falência 67 % → 100 % no dia 1095 (3 bots; já era frágil). Não testado no MSFS.

## 2026-11-25 - Ajustes do primeiro voo de teste das missões (Claude)
- Fim de voo: contrato não cumprido mostra o motivo ("CONTRATO NÃO CUMPRIDO", `FlightOutcome.IncompleteReasons`); antes aparecia só "Recompensa $0".
- Briefing: o texto do contrato aparece no cartão da perna.
- HUD: o mini-mapa desenha a área de instrução, pontos e escalas; célula, motor e pneus só com ícone colorido (texto na dica).
- Build 0 erros; testes 1.059/1.059. Telas não conferidas visualmente.

## 2026-11-25 - Quique na decolagem, avião inicial com peças e vento na blackbox (Claude)
- Quique na decolagem não conta como pouso (`TouchdownDetector.IsTakeoffBounce`, `TouchdownEvent.AirborneSec`, `takeoffBounceMaxAirborneSec` = 8 s); dano só pela razão de descida (pouso duro ≥ 400 fpm). No voo de SBJS, 1,5 s no ar + toque a 158 fpm com 33 fps de deriva tirou trem e pneus.
- Avião inicial nasce com peças (`CreatePilotViewModel` → `PartMigrationService.ProvisionPurchasedAircraft`): antes o 1º voo rodava no desgaste antigo e o Hangar mostrava faixas em % até reabrir o app.
- Blackbox grava `WindDirDeg;WindKt` (`AMBIENT WIND *`) no fim da linha.
- Vento do voo de SBJS reestimado sem usar a proa (GS gravada × trilha verdadeira): ~20 kt de ~070° verdadeiro de 0 a 1.500 ft (resíduo 4–6 kt), consistente por faixa de altura. Decolagem (proa ~349° mag.) e pouso com cauda de ~6–15 kt (GS − IAS; o IAS do C152 lê baixo em baixa velocidade). O vento existiu; o cálculo de través do app (proa magnética × vento verdadeiro) continua errado.
- Build 0 erros; testes 1.063/1.063. Não testado no MSFS.

## 2026-11-25 - Vento de través, vento no REC e faixa de altitude da instrução (VOO-VENTO-INSTRUCAO) (Gemini)
- `SimTelemetry`: adicionado `PlaneHeadingTrue` no final do struct (espelhado com `PLANE HEADING DEGREES TRUE` no `SimConnectService.cs`); `CrosswindComponentKnots` e `TailwindComponentKnots` passam a usar a proa verdadeira, eliminando distorção por declinação magnética.
- `sensor_registry.json`: adicionadas variáveis `amb-wind-velocity`, `amb-wind-direction`, `nav-heading-true` e `nav-heading-magnetic` (grupo `airframe`) para gravação no REC.
- `OverflyWaypointCodec` e `OverflyPoint`: adicionado parâmetro opcional `MinAglFt` (codec aceita 5º campo `minAglFt`).
- `OverflyTracker`: introduzida tolerância `AltitudeToleranceFt = 500.0`. Quando `MinAglFt > 0`, valida faixa `[Elev + MinAglFt - 500, Elev + MaxAglFt + 500]`.
- `MissionGenerator`: instrução local ajustada para 1 a 2 TGL (`random.Next(1, 3)`) e waypoint com `altTop`/`altBase`; cheque prático com waypoint na faixa de altitude.
- `MissionCompletion`: detalhamento do motivo de não cumprimento de ponto quando houver `MinAglFt > 0`.
- Documentação atualizada em `docs/plans/missoes-locais-rp.md`, `docs/sistemas/missoes-quadro.md` e `docs/sistemas/sensores.md`.

## 2026-11-25 - Progresso do contrato no HUD (HUD-PROGRESSO) (Claude)
- Spec e revisão do Claude; implementação do Gemini (`feat/hud-progresso`, revisão em `docs/reviews/HUD-PROGRESSO-revisao.md`).
- `ContractProgressFormatter` (Domain/Missions): texto curto do andamento dos rastreadores (TGL, pontos com distância e faixa de altitude abaixo/na faixa/acima, escalas, passos do voo de teste, pousos STOL, pernas, lançamento, restrição do translado), até 3 linhas, cultura pt-BR.
- `OverflyTracker`: `EvaluateAltitude` (mesma regra de antes), `Targets` e última posição.
- `FlightMonitor.ContractProgressText` calculado a cada pacote na thread dos rastreadores; HUD mostra linha "CONTRATO" nos modos compacto e expandido.
- Não testado no MSFS.


## 2026-11-25 - Distâncias de check_flight, paraquedismo e pinga_pinga (MISSOES-DIST) (Gemini)
- `MissionGenerator`: assinaturas de `CreateCheckFlightMissionSync` e `CreateSkydiveMissionSync` recebem `double distanceNm` obrigatório após `referenceTime`, eliminando valores fixos (30 e 25 NM) e aplicando nos textos e no objeto de missão; `CreateMultiStopMissionSync` recebe `profile` opcional, respeita `minTotal` e `maxTotal` com reserva dinâmica de 25 NM por etapa faltante para atingir o mínimo de 3 etapas.
- `MissionBoardCore`: `CreateSingleMission` repassa `distanceNm` sorteado dentro de `profile.RangeNm` aos geradores `skydive` e `check_flight`, e repassa `profile` ao gerador `multi_stop`.
- `MissionCatalogEquivalenceTests`: snapshot golden regravado refletindo distâncias/recompensas calibradas por perfil em `check_flight` (6 registros) e `paraquedismo` (5 registros).
- Testes: novos testes de unidade em `PhaseE2C1GeneratorsTests` validando distâncias nos limites dos perfis, geração de valores variados e rejeição de rotas fora da faixa.
- Documentação atualizada em `docs/sistemas/missoes-quadro.md`.

## 2026-11-25 - Áreas de instrução reais do DECEA (AREAS-INSTRUCAO) (Gemini)
- `tools/places/build_training_areas.py`: script de extração e conversão de áreas reais do DECEA GeoAISWEB (camada WFS `ICA:airspace`), filtrando 11 áreas de instrução e 16 de acrobacia com cálculo de centroide, raio e cotas de altitude.
- `training_areas.json` e `ATTRIBUTION.md`: base publicada em `Infrastructure/Data/places/` e atribuição oficial do DECEA (AIP Brasil).
- `IPlacesDataService` e `PlacesDataService`: adicionados `TrainingAreaInfo`, leitura de `training_areas.json` e consulta `FindTrainingAreas` ordenada por distância e ID.
- `MissionGenerator`: adicionados `TryResolveRealAreaBand` (conversão de cotas SFC/MSL para AGL pela elevação da origem, faixa de 1.500 ft a partir de 1.500 ft AGL em múltiplos de 500 ft, mínimo de 1.000 ft) e `CalculateInitialBearing`. `CreateLocalInstructionMissionSync` e `CreateCheckrideMissionSync` priorizam áreas reais a ≤ 25 NM, com fallback procedural determinístico em origens sem área real.
- `MissionBoardCore`: repasse de `placesService` aos geradores `local_instruction` e `checkride`.
- Testes: novo conjunto `TrainingAreasTests` (8 testes aprovados) e atualização de snapshot `missions_golden.json`.
- Documentação atualizada em `docs/sistemas/missoes-quadro.md`.


## 2026-11-25 - Trabalho aéreo e ronda de saúde (E2C-2) (Gemini)
- Ativados no catálogo (`mission_catalog.json`) os 5 tipos: `patrulha_vigilancia` (gerador `patrol`), `inspecao_linhas` (`line_inspection`), `monitoramento_trafego` (`traffic_orbit`), `sar` (`sar_pattern` com variantes `expanding_square` e `creeping_line`) e `saude_familia` (`round_trip`).
- `AerialWorkTracker` (Domain/Missions): novo rastreador puro com quatro modos operacionais (`sectors`, `route`, `orbit`, `search`), controlando altitude relativa à elevação de referência da origem (`RefElevationFt`), limite de velocidade (`MaxIasKt`), raio de pontos/alvo e acúmulo de tempo simulado (`simDt`).
- `saude_familia`: implementada como viagem de ida e volta (`round_trip`) com pouso completo na escala intermediária via `MultiStopTracker` e retorno à origem.
- Geradores em `MissionGenerator` e despacho em `MissionBoardCore` com filtros geométricos, alvos procedurais/POIs e bagagem por passageiro.
- `ContractProgressFormatter` e `ContractMarkerBuilder`: formatação de linha única no HUD para cada modo de trabalho aéreo e projeção no mapa tático (com alvo SAR mantido oculto).
- `FlightMonitor`: integração completa dos rastreadores no ciclo de monitoramento e apuração de resultados de voo.
- Adicionadas variantes textuais em `mission_texts.json` e testes unitários em `AerialWorkTrackerTests` e `PhaseE2C2MissionTests`.
- Snapshot `missions_golden.json` regravado e relatório de quadros executado com sucesso.


## 2026-11-25 - board-report mais rápido (BR-PERF) (Gemini)
- `BoardCache` (`Application/Services/BoardCache.cs`): nova classe pura para aceleração e reaproveitamento de cálculos em simulações de quadros em memória. Pré-computa pistas mais longas/iluminadas e lista base de candidatos uma única vez; gerencia o ciclo por origem computando distâncias geográficas e tags uma vez por aeroporto de partida; cacheia grupos de candidatos e pesos de sorteio (`DestinationChoiceGroup`) por perfil de missão preservando estritamente a precisão de ponto flutuante e o consumo determinístico do `Random`.
- `MissionBoardCore` e `MissionGenerator`: adicionado parâmetro opcional `BoardCache? cache = null` em `GenerateBoard`, `CreateSingleMission` e `GenerateMissionsInMemory`; reordenados filtros rápidos (país, ICAO de origem e teto de distância) antes de comparações de tags em `FilterCandidates`.
- `BoardReporter`: integração do `BoardCache` instanciado uma única vez após a carga do banco temporário e reaproveitado entre todos os quadros da execução; adicionada medição de tempo segregada (seed vs simulação de quadros). Tempo de simulação reduzido de ~39 s para 12.39 s nos 10 aeroportos x 10 quadros (aceleração de 3,15×).
- `BoardCacheTests`: novos testes unitários garantindo que a geração com e sem cache é rigorosamente idêntica missão por missão (`BoardCache_MesmaSemente_MesmoQuadro`) e que a alternância entre diferentes origens não polui o estado (`BoardCache_OrigensDiferentes_NaoVazaCandidatos`).
- Suite de testes completa aprovada (1.120 testes, 0 falhas) com golden snapshot `missions_golden.json` inalterado.

## 2026-11-25 - Revisão do BR-PERF (Claude)
- Um só caminho de sorteio: elegibilidade e pesos de destino deixaram de estar duplicados entre o relatório e o jogo; o jogo também usa o cache (vida de um quadro).
- `board-report` lê aeroportos e pistas em ordem fixa: antes a mesma semente variava entre execuções (Ids novos a cada seed).
- Medido contra o código antigo com a mesma ordem (10 aeroportos × 10 quadros, Release): CSVs e relatório idênticos; tempo total 64 s → 31 s (quadros ~16 s, cerca de 3×); pico de memória ~415 MB nos dois.

## 2026-11-25 - Decisões abertas das missões: CAN militar e saúde (Claude)
- `militar_can` ganhou `requireAnyTag` (`militar`, `militar_misto`): não sai mais de aeroporto civil.
- Peso do destino preferido de saúde 150 → 100 em `orgaos`, `uti_longa_distancia` e `medevac_remoto`.
- `missions_golden.json` regenerado; teste `MilitarCan_OrigemCivil_NaoSorteia`. 1.121/1.121 testes. Não testado no MSFS.
- `career-stab --days 1095 --bots 20 --seed 1 --difficulty medium --policy agressivo --fast`: falidos 25 % no dia 365 e 40 % no dia 1095; patrimônio mediano no dia 1095 $3,9 mi (classe F). Sem comparação com a versão anterior aos aeroportos novos.

## 2026-11-25 - Corte de isolado configurável (Claude)
- `airport_profiles.py` aceita `ISOLADO_NM` (padrão 150, sem mudar o JSON) e imprime a contagem. Medição de 200 e 250 NM pendente no PC de casa. Script só conferido na sintaxe.

## 2026-11-25 - Tipo de carga coerente e carga perigosa (CARGA-COERENTE) (Gemini)
- `cargo_kinds.json` adicionado com catálogo de tipos de carga e classes de artigos perigosos (DGR) para 15 missões de carga.
- `CargoKindCatalog` e `DangerousGoodsRules` implementados no Domain com sorteio determinístico por `StableSeed` (FNV-1a 32-bit) e ponderação por tags de origem e destino.
- Bloqueio de manifesto operacional (`CheckManifest`) para cargas "somente cargueiro" (Cargo Aircraft Only) com passageiros a bordo integrado no `MainViewModel` (ao aceitar e ao iniciar voo).
- `Mission` expandida com `CargoKind` e `CargoName` e migração de schema idempotente em `AppDbContext`.
- `MissionDisplayItem` e `MissionsView.xaml` exibem resumo e sinalização de carga perigosa no pré-despacho.
- `mission_texts.json` expandido com variantes dinâmicas usando marcadores `{cargo}` e `{cargo_note}`.
- Testes unitários dedicados em `CargoKindCatalogTests` e snapshot golden atualizado exclusivamente nos títulos de missões de carga (59 registros, 0 divergências em demais campos).

## 2026-11-25 - Prazo do bônus de XP no quadro (Claude)
- `Domain/Xp/XpDeadline.cs`: limites do bônus de prazo antes do voo, com a mesma regra do `XpCalculator` › 5.3 (ETE = distância ÷ cruzeiro + `EteFixedBufferMinutes`; ≤ 90 % → +30/+60 XP; ≤ 115 % → +15 XP). Nulo em voo local.
- Pré-despacho (`MissionDisplayItem.DeadlineSummary`, `MissionsView.xaml`): linha "Prazo: +N XP com voo até …", vermelha em missão urgente ou do quadro `Medica`. Melhoria 2 (saúde) do `missoes-locais-rp.md` fechada.
- Testes `XpDeadlineTests`. 1.141/1.141. Não conferido no app (só build).

## 2026-11-25 - Safra por estação nas missões agro (SAFRA-ESTACAO) (Gemini)
- `Domain/Missions/AgroSeason.cs`: cálculo das fases da safra (Plantio, Colheita, Entressafra) baseado no mês equivalente austral (invertido por 6 meses para latitude >= 0) e relógio de carreira do piloto.
- Geração de etiquetas dinâmicas `safra:<fase>` e `safra:mes:<m>` e nota textual contextual para briefings.
- `MissionDefinition` e `MissionWeighting.OriginWeight`: campo `Season` desserializado do catálogo (`mission_catalog.json`) e aplicado aos pesos de origem após o teto de 12 e ajuste de bases militares.
- `mission_catalog.json`: pesos de safra calibrados para `insumos_agro`, `agro_executivo`, `translado_piloto_agricola` e `pulverizacao`.
- `cargo_kinds.json`: tags de safra adicionadas às cargas de `insumos_agro` e novo tipo `revisao_entressafra` (peso base 0, favorecido na entressafra).
- `mission_texts.json`: marcador `{safra}` adicionado nas descrições de `insumos_agro`.
- `MissionBoardCore.GenerateBoard`: aceita parâmetro opcional `seasonDate`, mescla tags dinâmicas sem poluir o cache e injeta `{safra}` nos textos.
- Integração de `seasonDate` em `MissionGenerator.GenerateMissionsAsync` (lendo `CareerClockUtc` do piloto), `GenerateMissionsInMemory` e `CareerProgressionSimulator`.
- Testes unitários dedicados em `AgroSeasonTests` (11 testes) e teste `CargoKindCatalogTests.LoadDefault_EtiquetasExistemNosPerfis` atualizado para aceitar etiquetas dinâmicas de safra.

## 2026-11-25 - Cabeçalhos de estado dos planos atualizados (Claude)
- Linha de estado de 15 planos de `docs/plans/` atualizada ou acrescentada, conferida contra o CHANGELOG, `docs/sistemas/` e buscas no código. Só cabeçalhos; o corpo dos planos não mudou.
- Build 0 erros; testes 1.154/1.154. Nada a testar no MSFS.

## 2026-11-25 - Linha aérea prefere rotas reais (ROTAS-REAIS) (Gemini)
- `tools/places/build_routes.py`: script de extração e mapeamento de rotas regulares a partir do OpenFlights `routes.dat` (2014-06) e `airports.csv` gerando `airline_routes.json` (17.223 pares únicos, 330 KB).
- `Domain/Missions/AirlineRouteCatalog.cs`: catálogo em memória de pares de rotas comerciais, busca insensível a maiúsculas e contagem de companhias.
- `MissionDefinition` e `MissionDestinationRule`: propriedade `RealRouteMultiplier` configurada para 6 em missões troncais, internacionais e ponte aérea, e 4 em regionais e alimentadores (`mission_catalog.json`).
- `MissionWeighting.RealRouteMultiplier`: multiplicador aplicado aos destinos elegíveis que possuem rota real consolidada a partir da origem, fora do teto padrão de destino.
- `BoardCache` e `MissionBoardCore`: repasse de `AirlineRouteCatalog` na seleção e cálculo de pesos do `DestinationChoiceGroup` e acionamento da nota contextual.
- `mission_texts.json`: marcador `{route_note}` adicionado às descrições das 8 missões de linha aérea regular.
- `places/ATTRIBUTION.md`: atribuição formal dos dados do OpenFlights sob a licença ODbL.
- Testes unitários novos em `AirlineRouteCatalogTests`.

## 2026-11-25 - Base de aeroportos do simulador: lógica e persistência (AIRSIM-1) (Gemini)
- `Domain/Airports/Sim/`: tipos de entrada (`SimAirportListItem`, `SimRunwayData`, `SimAirportDetail`), mapeamento e classificação de superfícies (`SimSurface`), cálculo de impressões digitais e resumo SHA-256 (`SimAirportFingerprint`), cálculo de diferenças (`SimAirportDiff`) e construtor de registros com recálculo de tier e iluminação herdada (`SimAirportRecordBuilder`).
- Entidades `SimSyncState` e `SimAirportQueueItem` mapeadas para tabelas `SimSyncStates` e `SimAirportQueue`.
- `Airport`: novas propriedades `Source`, `SimRegion`, `SimFingerprint` e `SimCheckedUtc`. Migração em `AppDbContext.EnsureSchemaUpdatedAsync` com `ALTER TABLE` e `CREATE TABLE IF NOT EXISTS`.
- `DataSeeder`: identificação de origem dos aeroportos gravados (`Source = "LittleNavmap"` no caminho LNM e `Source = "Csv"` no caminho OurAirports).
- `SimAirportSyncStore`: store de sincronização com controle de versão, digest da lista, fila persistente por prioridade/distância, aplicação em lote de detalhes com transação, marcação incremental de ausentes com proteção de lista suspeita e rodízio em segundo plano.
- Testes unitários dedicados em `FlightCareer.Tests/Airports/Sim/` (11 testes).
- Revisão (Claude): APROVADO com 2 ajustes (`docs/reviews/AIRSIM-1-revisao.md`): aeroporto ausente que volta ao simulador é reenfileirado (impressão limpa ao marcar ausente) e `ApplyDetailsAsync` consulta fila, aeroportos e pistas uma vez por lote. 1.189 testes. Nada ligado ao SimConnect ainda (AIRSIM-2); não testado no MSFS.

## 2026-11-25 - Medição do corte de isolado em 200 e 250 NM (Claude)
- `isolado`: 1.399 (150 NM), 581 (200 NM), 244 (250 NM). Tabela em `docs/plans/catalogo-missoes.md` › 12. Nenhum dado ou código alterado; corte segue em 150 até a decisão do usuário.
- Recorte do banco do Little Navmap (tabelas `airport`, `runway`, `metadata`: 22 MB, 8,7 MB compactado) gera `tier-report` e perfis idênticos aos do banco completo (506 MB). Ainda não commitado (repositório público).

## 2026-11-25 - Corte de isolado em 200 NM e limpeza dos arquivos de agentes (Claude)
- Decisão do usuário: `isolado` a partir de 200 NM; `airport_profiles.json` regerado (1.399 → 581 aeroportos).
- Apagadas as pastas `.antigravity/` e `.gemini/` (personas repetidas, fora do fluxo do executor); referências removidas de `gemini.md` e `CLAUDE.md`.
- Banco do Little Navmap não vai para o git: recorte de 8,7 MB em `references/lnm/` (ignorado), copiado à mão.

## 2026-11-25 - Pasta antigos apagada, lista de aeronaves candidatas salva (Claude)
- `docs/plans/aeronaves-candidatas.csv`: 17 aviões da v3 antiga que não estão no catálogo atual (valores antigos, **não conferidos**: usar só como lista; preços e desempenho pedem POH/fabricante).
- Apagada a pasta local `antigos/` (v3 e PROJETO_152, fora do git), a pedido do usuário.

## 2026-11-25 - Limpeza da pasta docs (Claude)
- Apagados: `tarefas_sem_IA.md`, `VARS.txt`, `plans/lista-pecas-hangar.md`, `agents/TESTES-PC-2026-11-22.md` (testes sem registro de conferência passaram para o HANDOFF) e os brutos `report.csv`, `strings.csv`, `values.csv.gz` das sondagens do C152. Tudo recuperável pelo git.
- Mantida `probe-runs/matriz-C152.csv`: é citada pelas análises de `sensor-research`.

## 2026-11-25 - Instruções enxutas e roteiro de voo (Claude)
- `AGENTS.md`: detalhes da § 6 movidos para `docs/sistemas/simconnect.md` e `banco.md`; comandos de ferramentas em `docs/agents/COMANDOS.md`; índice de sistemas resumido.
- `CLAUDE.md` do projeto e global resumidos; delegação ao Gemini em `tools/agents/global/gemini/DELEGACAO.md`.
- `install-global.sh` desativa skills sem uso (`global/settings.json`, `skillOverrides`; efeito a conferir com `/context`).
- HANDOFF limitado a 40 linhas; testes de voo em `docs/agents/TESTES-PENDENTES.md`; roteiro em `ROTEIRO-VOO.md`.
- Só documentação e ferramentas; build e testes rodados antes das edições (0 erros, 1.189/1.189).

## 2026-11-26 - Correções do voo de instrução em SDCO (Claude)
- Dados de lugares (VOO-SDCO-1): o DI escolhia o construtor de listas do `PlacesDataService`, todas vazias; o jogo rodava sem áreas do DECEA, zonas de salto, POIs, aeródromos cênicos e de instrução. Construtor de dados virou `PlacesDataService.FromData`; teste de DI.
- Toque e arremetida (VOO-SDCO-2): `LandingCycleTracker` abre ciclo novo após 8 s no ar; antes, 3 toques contavam 1 pouso, o 2º virava "BOUNCE" e o status ficava "TÁXI / PÁTIO" em voo. Dano do pouso usa a razão daquele toque; perna de XP só com parada completa.
- Sobrevoo (VOO-SDCO-3): folga de altitude de 500 para 100 ft.
- Mini-mapa do HUD (VOO-SDCO-4) mostra os contratos aceitos antes do monitoramento.
- Faixa de altitude do texto do contrato sempre em pt-BR (VOO-SDCO-5).
- Build 0 erros; `dotnet test` 1.195/1.195. Não testado no MSFS.

## 2026-11-26 - Cartão de objetivo (OBJ-CARD-1) (Gemini)
- Cartão de objetivos de voo em janela overlay transparente e arrastável (`ObjectiveCardWindow`, `ObjectiveCardViewModel`, `ObjectiveCardService`) exibindo missões, objetivos e pousos finais com status visual colorido (âmbar ○ pendente, verde ✓ cumprido, vermelho ✖ falhou).
- Abre automaticamente ao iniciar monitoramento de voo; atalho global Ctrl+Alt+O e botão 🎯 no cabeçalho do HUD reabrem a janela.
- Linhas estruturadas `ContractObjectiveLine` e `BuildLines` em `ContractProgressFormatter`; `ObjectiveCardBuilder` agrega missões e objetivos com deduplicação de destinos finais.
- Build 0 erros; 1.203/1.203 testes aprovados. Janela, atalho e botão do HUD não testados no MSFS.


## 2026-11-26 - Posições das janelas e cores do CONTRATO (OBJ-CARD-2) (Gemini)
- Posições salvas do HUD, do cartão de objetivo e do overlay de alertas em `%LocalAppData%\FlightCareer\window-positions.json` (`WindowPositionStore.Shared`), persistidas ao soltar o arrasto (`DragMove`), ao esconder e ao fechar.
- Algoritmo puro de contenção de janela `WindowPlacement.ClampToArea` (`Domain/Utils`), garantindo título e margem visíveis na tela virtual (`SystemParameters.VirtualScreen*`) em configurações com um ou múltiplos monitores.
- Linha CONTRATO do HUD (`DebugOverlayViewModel`, `DebugOverlayWindow.xaml`) renderizada com `ItemsControl` e `HudContractItem` coloridos por estado do objetivo (âmbar `#FBBF24` pendente, verde `#00E676` cumprido, vermelho `#FF5252` falhou), atualizando somente quando o conteúdo ou estado muda.
- Build 0 erros; 1.212/1.212 testes aprovados. Posições persistidas e cores não testadas no MSFS nem em múltiplos monitores.

## 2026-11-26 - Mapa de contratos unificado e voo de conferência em SDCO (Claude)
- Quadro de missões mostra a área, os pontos e a faixa do contrato selecionado, como o briefing. `FlightMapControl.ContractMissions` (missão, item do quadro ou coleção) monta os visuais com `ContractMarkerBuilder`; saem `ContractMarkers`/`SurveyArea` duplicados do briefing e do HUD.
- Voo de 26/11 (SDCO, C152): contrato cumprido, 2 pousos com toque e arremetida, área de Tatuí ok; pneus −4,6 % e RPM alto registrados no plano `achados-voo-sdco.md`.
- Revisão e merge de OBJ-CARD-1 e OBJ-CARD-2 (Gemini). Build 0 erros; `dotnet test` 1.212/1.212. Não testado no MSFS.

## 2026-11-26 - Análise do pouso forçado e plano de correções (Claude)
- Plano `docs/plans/correcoes-voo-forcado-sdco.md`: pedidos do usuário (mapa, briefing VFR, Hangar sem %), bugs achados no REC, blackbox e banco (11 peças quebradas por uma pane, cobrança em dobro, dano de pouso fora das peças) e 10 tarefas com modelo recomendado.
- Só documentação; nenhum código alterado. Nada testado no MSFS.

## 2026-11-26 - Pane quebra uma peça e reparo único (PANE-PECAS) (Gemini)
- Uma pane nativa do simulador quebra exatamente uma peça física candidata via `NativeFailurePartMap` (menor saúde, ordem da tabela, menor motor), eliminando quebra em massa de peças por uma falha nativa.
- Reparo de panes no Hangar unificado via `DefectPartLink`: panes nativas associadas a peças quebradas são resolvidas com a troca física das peças (sem cobrança em dobro); trocar a peça no Hangar resolve as panes correspondentes.
- Fluxo único em "Reparar tudo" (`RepairAll`): agrega substituição de peças recomendadas e todas as panes em aberto (panes com peças saem sem custo duplicado e panes avulsas cobram seu valor), habilitando o botão mesmo que só haja pane aberta.
- Interdição obrigatória por peça quebrada com `BlocksFlightWhenBroken == true` (`PartAirworthiness`): reflete no status do Hangar ("INTERDITADA (PEÇA)") e bloqueia início de voo em `FlightMonitor.StartMonitoring`.
- 10 testes unitários novos em `NativeFailureAndDefectTests`; 1.222/1.222 testes aprovados. Pane nativa real no simulador e interface do Hangar em tempo de execução não testadas no MSFS.

## 2026-11-26 - Quebra de trem/pneu no pouso, pouso forçado nas peças e inspeções no Hangar (POUSO-DANO) (Gemini)
- Chance dinâmica de quebra de trem de pouso e pneus no pouso duro (`LandingBreakModel`, Domain puro), calibrada via `LandingSettings` e `component_catalog.json` por razão de descida (curva bilinear 600/850/900 FPM), inclinação de asa, terreno fora de pista (`ctx-on-any-runway`) e desgaste; sorteia no máximo 1 peça de trem e 1 de pneu por toque (ordem main → nose → tailwheel). Interdição por peça física sem criar pane duplicada.
- Pouso forçado fora de pista (`FlightMonitor` e `FlightService`): dano de terreno despreparado aplicado diretamente nas peças/componentes (pneus -35%, trem -20%, célula -20%); cancelamento de todas as missões ativas do voo sem pagamento (receita 0); pontuação com XP parcial (fator 0.5); avanço do relógio de carreira (`pilot.CareerClockUtc`).
- Inspeções operacionais e de toque integradas ao Hangar (`InspectionDefectPolicy`, `WearRuleCatalog`, `DefectCatalog`): suporte ao campo `isGrounding` em regras de desgaste; inspeções de pouso/toque severo interditam imediatamente a aeronave, enquanto as demais são registradas com custo base de 400 e listadas como pendentes no relatório final de voo.
- Blackbox e telemetria: pico rápido de G-Force capturado no contato (`TouchdownEvent.PeakG`) registrado em `FlightTelemetryLogger.RecordPeakG` e `_worstTouchdownG`; status `"POUSO_FORCADO"` com formatação dedicada na lista de logs de voo.
- 23 testes unitários novos em `LandingBreakModelTests`, `InspectionDefectPolicyTests`, `WearRuleCatalogTests` e `FlightServiceOffFieldLandingTests`; 1.245/1.245 testes aprovados. Não testado no MSFS.

## 2026-11-26 - Limpeza do mapa e texto de reporte VFR (LIMPA-MAPA) (Gemini)
- Remoção do recurso morto de waypoints manuais do mapa tático (`FlightMapControl`): removidos os controles visuais de overlay de distância ("WAYPOINTS ATIVOS", `TxtDistance`), botão LIMPAR (`BtnClear_Click`), o conversor `BoolToVisConverter`, e o binding `ShowWaypointsOverlay="False"` em `DebugOverlayWindow.xaml`.
- Limpeza no code-behind de `FlightMapControl`: removidos `_waypoints`, `_waypointRoute`, `ShowWaypointsOverlayProperty`, propriedade `ShowWaypointsOverlay` e manipulador `BtnClear_Click`. Método utilitário `CreateWaypointCircle` renomeado para `CreateRoutePointCircle` e região atualizada para "Aeroportos e Trajetória Gravada".
- Correção terminológica VFR: "ponto de reportagem" padronizado para "ponto de reporte" em `mission_texts.json`, gerador de missões (`MissionGenerator.cs`), testes de navegação em triângulo (`VfrTriangleTests.cs`, `PhaseE2C1GeneratorsTests.cs`) e arquivo de referência `missions_golden.json`.
- Build 0 erros; 1.245/1.245 testes aprovados. Não testado visualmente no simulador/WPF em execução.


## 2026-11-26 - Avisos do Hangar (HANGAR-AVISOS) (Gemini)
- Preço final nos botões do Hangar com ajuste de dificuldade da carreira (`DifficultyEconomy.ApplyCost` e `CostAdjustmentText`): botões de reparo individual de componente (`RepairButtonText`), troca física de peças (`UpdateReplacementPlans`), "Reparar tudo", "Reparar todas as panes", botão individual de cada pane, revisões periódicas (óleo, anual, ignição, retífica, conversão de cabine), abastecimento de combustível e contratação/renovação de seguro exibem o preço final com desconto/acréscimo da dificuldade (sem duplicar incidência no diálogo de confirmação). Textos informativos de desconto/acréscimo percentual em `RunPaidServiceAsync` substituem o formato multiplicador antigo.
- Frases amigáveis de panes e contexto de ocorrência (`DefectText`, `DefectRowViewModel`): panes em aberto no Hangar exibem linguagem clara contextualizada pelo momento do voo ("em voo", "no pouso", "em solo", "no acidente") para todos os códigos do catálogo e inspeções; detalhes técnicos ("Origem", "Fase", "Causa", descrição técnica) movidos para o ToolTip da linha da discrepância; formatação de botão de reparo de pane corrigida para exibir o valor monetário. Remoção da tag `OriginLabel` da caixa de diálogo de panes NO-GO em `MainViewModel`.
- Unificação do aviso de oficina (E1): faixa secundária de oficina (`ShowWorkshopBanner`) passa a ser ocultada quando o cartão de status principal já exibe "OFICINA", evitando redundância na tela do Hangar e sendo reativada se houver interdição/NO-GO prioritário.
- 11 testes unitários novos em `DefectTextTests` e `DifficultyEconomyTests`; 1.267/1.267 testes aprovados. Telas do Hangar e comportamento em tempo de execução no simulador não testados com o MSFS aberto.


## 2026-11-27 - Briefing: navegação com escalas e enquadramento do mapa (BRIEF-NAV) (Gemini)
- Planejamento puro de pernas no domínio (`BriefingLegPlanner`): leitura de escalas (`ReadStops`), segregação de circuitos TGL reais (`RequiredTouchAndGoes > 0`), voos locais (`Local`) e navegações com escalas (`ContractStop`), eliminando circuitos e rótulos TGL falsos em voos triangulares ou locais sem TGL. Expansão de escalas sem duplicatas consecutivas, com suporte a modos de parada (`full_stop`/`touch_and_go`) e coordenadas geográficas de aeródromos/pistas remotas via `StopPoint`.
- Reformulação de `BriefingViewModel.RebuildItineraryAndValidateAsync`: integração com `BriefingLegPlanner`; textos contextuais de ação para TGL, voo local, escalas técnicas contratuais e desembarque/entrega; remuneração de contrato atribuída apenas na perna final da missão; cálculo de distância com suporte a coordenadas de escalas contratuais.
- Modelo de perna (`FlightLegModel`): nova propriedade `IsLocal` e formatação contextual de `RouteDisplay` ("(TGL)", "(LOCAL)" ou sem sufixo).
- Enquadramento e visualização no mapa (`FlightMapControl`): circuito âmbar restrito a instruções TGL reais (`IsTglOrLocal`); voos locais com retorno plotam marcador de base (`#00E676`) sem circuito; enquadramento de câmera unificado via `ApplyCombinedFraming()`, combinando simultaneamente os pontos da rota (`_routeFramingPoints`) e marcadores/polígonos de contratos (`_contractFramingPoints`) sem sobrescrever zoom e eliminando blocos `catch` vazios.
- 10 testes unitários novos em `BriefingLegPlannerTests`; 1.277/1.277 testes aprovados. Não testado no MSFS nem com o app aberto.
