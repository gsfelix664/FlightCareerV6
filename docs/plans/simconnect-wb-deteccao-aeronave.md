# Plano: SimConnect, W&B, Combustível, Localização e Detecção de Aeronave

> **Status: pendente / não verificado.** Este plano foi escrito em 2026-11-07 e o checklist original foi marcado como concluído, mas em 2026-11-12 o código ainda não tinha o `AircraftMatcher` em camadas (`IsAircraftMatching` continua recebendo só o `Title`), nem `AtcModel`/`AtcType` na telemetria, nem o `SyncFuelAndWeightsToSimCommand`. Confirme no código antes de tratar qualquer etapa como feita. Parte do W&B foi alterada depois (ver CHANGELOG, 2026-11-12 "Limpeza Visual da Tela de Briefing").

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
- [ ] Executar todas as 5 etapas preservando o padrão MVVM e o estilo visual Avionics Dark UI (`#0B0F14`, `#141C24`, `#00E5FF`, `#00E676`, `#FFB300`, `#FF3D00`).
- [ ] Garantir que o fator `* 1.15` / `/ 1.15` de combustível foi 100% erradicado.
- [ ] Compilar o projeto com `dotnet build src/FlightCareer.UI/FlightCareer.UI.csproj` e assegurar **0 Error(s)**.
- [ ] Registrar a conclusão da implementação no `GEMINI.md`.

---
