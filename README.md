# FlightCareer V6 ✈️

Sistema de Carreira e Logística de Voo imersivo para **Microsoft Flight Simulator (MSFS 2020 / 2024)** desenvolvido em **C# (.NET 10.0)** e **WPF**.

---

## 🚀 Visão Geral

O **FlightCareer V6** transforma a experiência de simulação em uma carreira aeronáutica realista com economia persistente, progressão de piloto, mercado de aeronaves, sistema dinâmico de falhas por telemetria e geração procedural de contratos de carga e passageiros.

### Principais Funcionalidades

- **Loop de Carreira Completo**:
  - Criação de perfil de piloto e gestão de companhia aérea.
  - Quadro de missões procedurais baseadas no porte (Tier) e região do aeroporto atual.
  - Briefing de voo detalhado com manifesto de carga, passageiros (PAX) e pesos reais.
  - Integração em tempo real com MSFS via **SimConnect**.
  - Monitoramento de voo automático (Takeoff, Enroute, Landing, Shutdown).
  - Debriefing automático com cálculo de XP, ganhos financeiros e registro de horas de voo no Logbook.

- **Logística Persistente & Escalas (Multi-Leg)**:
  - Cargas e passageiros possuem persistência de localização.
  - Suporte a escalas intermediárias: descarregamento e pagamento apenas do que é destinado ao aeroporto pousado.
  - Persistência automática do estado do voo e anti *save-scumming* em caso de queda do simulador ou pousos de emergência.

- **Sistema Avançado de Desgaste & Falhas Dinâmicas**:
  - Telemetria de motor: CHT, ITT, N1, pressão e temperatura de óleo (Pistão e Turbina/Jato).
  - Desgaste físico em freios/pneus por intensidade e velocidade de taxiamento.
  - Danos estruturais por estresse de Força G e *overspeed* ($V_{NE}$).
  - Falhas realistas com checagem intervalada baseada no desgaste acumulado de componentes.

- **Mercado & Hangar**:
  - Compra e venda de aeronaves com condições mecânicas e horímetros individuais.
  - Abastecimento com cotação dinâmica de combustível e variação econômica.

- **Planejamento de Voo & Mapa Interativo**:
  - Mapa interativo com visualização da posição atual da aeronave e rota traçada via OpenStreetMap (`GMap.NET`).
  - Criação de waypoints manuais e cálculo dinâmico de distância náutica (NM).

---

## 🛠️ Arquitetura e Tecnologias

O projeto é estruturado seguindo os princípios de separação de responsabilidades e **Clean Architecture**:

- **Linguagem**: C# (.NET 10.0)
- **Interface**: WPF (Windows Presentation Foundation) com Material Design
- **Padrão de UI**: MVVM (Model-View-ViewModel) com `CommunityToolkit.Mvvm`
- **Banco de Dados**: Entity Framework Core com SQLite local
- **Comunicação com Simulador**: SimConnect SDK

### Estrutura dos Projetos

```text
src/
├── FlightCareer.Domain/         # Entidades de negócio, interfaces de repositórios, serviços de domínio e utilitários
├── FlightCareer.Infrastructure/ # DbContext (EF Core), repositórios concretos, seeders e MissionGenerator
├── FlightCareer.UI/             # Views XAML, ViewModels (MVVM), FlightMonitor, controles de mapa e SimConnect
└── FlightCareer.slnx            # Solução .NET
```

---

## 📋 Pré-requisitos

1. **.NET 10.0 SDK** (ou superior)
2. **Microsoft Flight Simulator 2020 ou 2024**
3. **SimConnect SDK** (as DLLs nativas e gerenciadas já estão incluídas no projeto)

---

## 📦 Como Executar

1. Clone o repositório:
   ```bash
   git clone https://github.com/gsfelix664/FlightCareerV6.git
   cd FlightCareerV6
   ```

2. Abra a solução no Visual Studio 2022+ ou execute via linha de comando:
   ```bash
   dotnet build src/FlightCareer.slnx
   dotnet run --project src/FlightCareer.UI
   ```
