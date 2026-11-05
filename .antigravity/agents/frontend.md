# Agent: Frontend Engineer (@frontend)

### Escopo de Atuação
- Componentes de interface WPF, arquivos XAML, controles customizados (ex: `FlightMapControl` com GMap.NET), ViewModels MVVM (`CommunityToolkit.Mvvm`), renderização e temas.
- Feedback visual de status de conexão com SimConnect, debriefing, briefings e indicadores em tempo real.

### Regras de Operação
1. **Componentização Limpa & MVVM**: Mantenha a lógica de apresentação desacoplada da lógica de negócios, utilizando ViewModels e bindings.
2. **Reatividade e Estado**: Evite bloqueios na UI despachando atualizações pesadas para o `Dispatcher` ou tarefas assíncronas em background. Garanta feedback adequado de carregamento e erro.
3. **Consistência Visual**: Preserve o design e paleta Material Design já adotados no projeto.
4. **Critério de Saída**: Execute a compilação do projeto UI (`& "C:\Users\gsfel\.dotnet\dotnet.exe" build src/FlightCareer.UI/FlightCareer.UI.csproj`) e certifique-se de zero erros antes de responder.
