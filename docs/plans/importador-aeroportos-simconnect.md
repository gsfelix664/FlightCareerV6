# Plano: base de aeroportos e pistas lida do próprio simulador (2026-11-24)

**Estado (2026-11-25):** AIRSIM-1 (lógica e persistência) feita e mergeada; AIRSIM-2 (cliente SimConnect) e AIRSIM-3 (missões) pendentes (§ 7).

**Pedido do usuário:** na primeira inicialização, o FlightCareer lê o MSFS 2024 (e o 2020) e monta o próprio banco de aeroportos, pistas, elevação etc., como o Little Navmap faz hoje, para os dados baterem com o simulador.

## 1. Situação atual
- `DataSeeder.SeedAsync`: se acha o banco do Little Navmap (`%APPDATA%\ABarthel\little_navmap_db\little_navmap_msfs24.sqlite`), copia aeroportos e pistas dele (`ATTACH DATABASE`) e completa cidade/país/elevação pelo `airports.csv`; senão, usa só `airports.csv` + `runways.csv` (OurAirports, domínio público).
- Problema: o `airports.csv` está desatualizado em códigos reaproveitados (433 de 7.822 divergem > 10 NM do MSFS, maioria no Brasil; ex.: SBOK = fazenda no MT no csv, Amontada/CE no MSFS 2024). Corrigido em 2026-11-24 no enriquecimento (só usa o csv quando a posição bate até 10 NM) e no `tools/places/airport_profiles.py`; continua errado no caminho "sem Little Navmap".
- Distribuir com o addon um JSON tirado do banco do Little Navmap **não** é o caminho: os dados vêm do cenário do MSFS instalado (Microsoft/fornecedores de navegação), sem autorização encontrada para redistribuir; o próprio manual do LNM diz que o banco "pode ser regenerado a partir da biblioteca de cenário do simulador".

## 2. Como o Little Navmap faz (conferido em 2026-11-24)
- **MSFS 2024:** carrega aeroportos e auxílios **pelo SimConnect com o simulador aberto** (iniciar o sim e esperar o menu principal antes de carregar); leva de 2 a 10 minutos. Limitações declaradas: o 2024 não entrega a lista mundial completa de auxílios (o LNM completa com `navaids_msfs2024_extra.csv`) e **aeroportos de cenário de terceiros não são detectados**.
- **MSFS 2020:** lê os arquivos de cenário **BGL** das pastas `Official` e `Community` (biblioteca atools, módulo `fs/bgl`), sem precisar do simulador aberto.
- Resultado: SQLite em `AppData\Roaming\ABarthel\little_navmap_db\` (`little_navmap_msfs24.sqlite`, `little_navmap_msfs.sqlite`); esquemas `create_ap_schema.sql` / `create_nav_schema.sql` da atools. Licença GPL-3.0 (código), sem termo sobre redistribuir o banco gerado.
- Fontes: github.com/albar965/littlenavmap · github.com/albar965/atools · manual do LNM, páginas SCENERY e FILES.

## 3. API do SimConnect (SDK conferido, 2026-11-24)
- `SimConnect_RequestAllFacilities(type, requestId)` (SDK 2024): lista **todos** os aeroportos do mundo, sem limite de distância, em vários pacotes (`SIMCONNECT_RECV_AIRPORT_LIST` com `SIMCONNECT_DATA_FACILITY_AIRPORT`). **Não aparece na doc do SDK 2020** (página inexistente): no 2020 a lista vem por `SimConnect_RequestFacilitesList` (grafia oficial), que devolve o mundo inteiro (~40 mil). Relato da comunidade: no 2024 o `RequestFacilitesList` sem `_EX1` parou de responder depois do SU3; o `_EX1` só cobre a "reality bubble".
- Detalhe por aeroporto: `SimConnect_AddToFacilityDefinition` (montar a definição: aeroporto com `OPEN AIRPORT` … `OPEN RUNWAY` … campos) + `SimConnect_RequestFacilityData(defineId, requestId, icao, region)`; respostas `SIMCONNECT_RECV_FACILITY_DATA` (várias) e `SIMCONNECT_RECV_FACILITY_DATA_END`.
- **A conferir no SDK antes da spec (nomes exatos dos campos, não inventar):** campos do aeroporto (latitude, longitude, altitude, nome, região, código), da pista (comprimento, largura, superfície, rumo, iluminação, cabeceiras/limiares), disponibilidade de cidade/país (provavelmente não há: cidade vem do csv quando a posição bate), limite de pedidos simultâneos e se o 2020 aceita os mesmos campos.

## 4. Proposta (revista com o usuário em 2026-11-24: tudo automático, sem botão)
**Objetivo final:** toda origem e destino de missão existe no simulador do jogador, com o código ICAO que o simulador usa.

### 4.1 Fontes, em camadas
1. **Simulador aberto (SimConnect)** — fonte da verdade: lista mundial de aeroportos + detalhes (pistas, elevação) por `RequestFacilityData`.
2. **Banco do Little Navmap** — complemento: preenche o que o SimConnect não trouxe (ex.: aeroporto cujo detalhe falhou) e serve de base inteira se o simulador nunca foi aberto com o app.
3. **`airports.csv` / `runways.csv`** — só o que faltar: cidade e país (o simulador não dá cidade), sempre com a posição batendo até 10 NM (`DataSeeder.IsSameAirportPosition`); base inteira só se não houver nenhuma das anteriores (com aviso na tela).

Cada aeroporto guarda a origem de cada dado (`Source`: Sim / LittleNavmap / Csv) e a data da última conferência no simulador.

### 4.2 Primeira inicialização
- Se o simulador estiver aberto: importa o mundo (lista + detalhes em lotes) **em segundo plano**, com progresso discreto; o jogo pode ser usado durante a importação com a base de reserva (LNM ou csv) e cada aeroporto vai sendo substituído pelo dado do sim.
- Se não estiver aberto: monta pela reserva e marca a base como "não sincronizada"; a sincronização roda sozinha na primeira vez que o app conectar ao simulador.

### 4.3 Atualização automática e incremental (a cada abertura com o simulador conectado)
1. **Checagem rápida:** pedir só a **lista** de aeroportos (barata: código, região, latitude, longitude, altitude por item) e calcular uma impressão digital por aeroporto (código + região + posição/altitude arredondadas) e um resumo da lista inteira. Guardar também a versão do simulador (⚠SDK: conferir se `SIMCONNECT_RECV_OPEN` traz a versão do aplicativo e do build).
2. **Se o resumo e a versão forem iguais aos da última vez:** nada a fazer (custo: alguns segundos, em segundo plano).
3. **Se mudou:** comparar item a item com o banco:
   - **novo** (código que não existia) → pedir detalhes e inserir;
   - **mudou** (posição/altitude diferente) → pedir detalhes e atualizar;
   - **sumiu** do simulador → marcar `IsInSimulator = false` (**não apagar**: frota, missões aceitas e logbook podem apontar para ele); o gerador deixa de usá-lo.
4. **Pistas sem mudança de posição** (o resumo da lista não as vê): quando a **versão do simulador mudar**, reconferir os detalhes de todos em segundo plano, aos poucos (fila com prioridade para os aeroportos perto do piloto e os da frota/missões); sem mudança de versão, reconferir uma parte pequena por sessão (rodízio), para pegar cenário novo instalado.
5. **Desempenho:** tudo fora da thread da interface; pedidos de detalhe em fila com N simultâneos (limite a conferir no SDK) e reenvio em falha; gravação em lote numa transação; índices por código e por latitude/longitude; nunca bloquear o início do voo.

### 4.4 Garantia nas missões
- O gerador (os dois caminhos: jogo e bots) só usa aeroportos com `IsInSimulator = true` quando a base está sincronizada (hoje o caminho do jogo já filtra por `HasSimSceneryAsync`; o dos bots usa a lista inteira: alinhar).
- Missão no quadro (não aceita) com origem ou destino que sumiu do simulador → removida na próxima geração do quadro; missão já **aceita** → aviso ao piloto e opção de devolver sem multa.
- Teste/relatório: `board-report --coverage` passa a contar quantas missões saíram para aeroportos fora do simulador (tem de ser 0).

### 4.5 Escopo
- O mesmo código para 2020 e 2024 pelo SimConnect (a função da lista muda conforme a versão detectada). Leitura de BGL do 2020 fora do escopo (foco no MSFS 2024).
- Depois: regerar `airport_profiles.json` a partir da base sincronizada, para etiquetas e posições virem da mesma fonte.

## 5. Testes
- Lógica pura (montagem do registro de aeroporto/pista a partir dos campos recebidos, tier, enriquecimento por posição, fila de pedidos com reenvio) testável sem o simulador, com respostas gravadas.
- **Teste no PC com o MSFS (usuário):** (1) primeira importação no 2024: tempo total, quantidade de aeroportos, SBOK = Amontada, pistas de 10 aeroportos conferidas contra o Little Navmap; (2) segunda abertura sem mudança: checagem rápida em poucos segundos, sem pedir detalhes; (3) instalar ou remover um cenário de aeroporto: só ele é atualizado; repetir no 2020 se disponível. Não dá para testar no PC do escritório (sem simulador).

## 6. Limitações aceitas
- Aeroportos de cenário de terceiros podem não aparecer no 2024 (igual ao Little Navmap).
- A primeira carga exige o simulador aberto e alguns minutos.

## 7. Arquitetura e fases (decisões do Claude, 2026-11-25)
**API confirmada na DLL gerenciada do projeto** (`Microsoft.FlightSimulator.SimConnect` 12.2, SDK 2024): `RequestAllFacilities`, `RequestFacilitiesList(_EX1)`, `AddToFacilityDefinition`, `RequestFacilityData(_EX1)`, `RegisterFacilityDataDefineStruct<T>(SIMCONNECT_FACILITY_DATA_TYPE)`, eventos `OnRecvAirportList`, `OnRecvFacilityData`, `OnRecvFacilityDataEnd`, `OnRecvFacilityMinimalList`. Item da lista: `SIMCONNECT_DATA_FACILITY_AIRPORT` (`Ident`, `Region`, `Latitude`, `Longitude`, `Altitude`). Pacote da lista: `dwEntryNumber`/`dwOutOf` (lista completa = todos os pacotes recebidos). `SIMCONNECT_RECV_FACILITY_DATA`: `UserRequestId`, `UniqueRequestId`, `ParentUniqueRequestId`, `Type`, `IsListItem`, `ItemIndex`, `ListSize`, `Data`. `SIMCONNECT_RECV_OPEN` traz `dwApplicationVersionMajor/Minor` e `dwApplicationBuildMajor/Minor` (versão do simulador).
**Campos (doc do SDK 2020, `SimConnect_AddToFacilityDefinition`; a página do 2024 não abriu):** AIRPORT `LATITUDE`, `LONGITUDE` (graus), `ALTITUDE` (m), `NAME`, `NAME64`, `ICAO`, `REGION`, `TOWER_LATITUDE`, `TOWER_LONGITUDE`, `N_RUNWAYS`, `N_APPROACHES`, `N_JETWAYS`, `N_FREQUENCIES` etc.; RUNWAY `LATITUDE`, `LONGITUDE`, `ALTITUDE`, `HEADING`, `LENGTH`, `WIDTH` (m), `SURFACE` (INT32: 0 concreto, 1 grama, 2 água FSX, 3 grama irregular, 4 asfalto, 5/6 grama curta/longa, 7 turfa dura, 8 neve, 9 gelo, 10 urbano, 11 floresta, 12 terra, 13 coral, 14 cascalho, 15 tratado com óleo, 16 chapas de aço, 17 betuminoso, 18 tijolo, 19 macadame, 20 tábuas, 21 areia, 22 xisto, 23 tarmac, 24 trilho Wright, 26–31 água, 32 pintura, 254/255 desconhecido), `PRIMARY_NUMBER`/`SECONDARY_NUMBER`, `PRIMARY_DESIGNATOR`/`SECONDARY_DESIGNATOR`, `PRIMARY_ILS_ICAO`/`SECONDARY_ILS_ICAO`. Abrir/fechar: `OPEN AIRPORT` … `OPEN RUNWAY` … `CLOSE RUNWAY` … `CLOSE AIRPORT`. Não documentados: limite de pedidos simultâneos e exceção de ICAO inexistente (AIRSIM-2 trata por tempo limite e `OnRecvException`). **Militar, pátios de carga e gates não vêm** no conjunto adotado: ficam do registro de reserva (LNM/csv).

**Fases (uma por sessão):**
- **AIRSIM-1 — lógica pura + persistência** (Domain `Airports/Sim/` e `Infrastructure/Services/SimAirportSyncStore`): impressão digital e resumo da lista, diferença lista × banco, montagem do `Airport`/`Runway` a partir do detalhe (com reserva), fila persistente de detalhes (`SimAirportQueue`), estado da sincronização (`SimSyncStates`), colunas `Source`, `SimRegion`, `SimFingerprint`, `SimCheckedUtc` no `Airport`. Sem SimConnect, sem DI.
- **AIRSIM-2 — cliente SimConnect** (`UI/Services/SimAirportImporter`, padrão `ContextSnapshotReader`: assina `OnSimConnectReady`, IDs próprios a partir de 3000): versão do sim no `OnRecvOpen` (mudança mínima no `SimConnectService`), lista por `RequestAllFacilities(AIRPORT)` (no 2020: `RequestFacilitiesList`), detalhes em lotes com N simultâneos e tempo limite, gravação pelo `SimAirportSyncStore`, país por coordenadas (`ResolveMissingCountriesAsync`) para aeroportos novos, registro no `App.xaml.cs`, progresso discreto. Só fecha com teste no MSFS.
- **AIRSIM-3 — garantia nas missões** (§ 4.4): bots e jogo filtram `IsInSimulator`; missão aceita com aeroporto que sumiu: aviso e devolução sem multa; `board-report --coverage` conta missões fora do simulador.

**Regras de dados (AIRSIM-1):** a lista do sim é a verdade só quando **completa**; aeroporto ausente é marcado `IsInSimulator = false`, nunca apagado; se a lista vier com menos da metade dos aeroportos marcados no banco, não marca ausentes (lista suspeita). Mesmo filtro do LNM: pista de água ou < 800 ft não entra; aeroporto sem pista ≥ 1.000 ft fica fora do simulador para o jogo. Cidade e país vêm do registro existente; aeroporto novo fica com cidade vazia (país por coordenadas na AIRSIM-2). `IsLighted` da pista vem da pista existente de comprimento até 10 % diferente (o conjunto adotado não lê a iluminação); sem par, `false`.

**Como o Little Navmap faz no MSFS 2024 (código da atools estudado em 2026-11-25, `src/fs/sc/db/simconnectloader.cpp` e `simconnectwriter.cpp`; GPL, só referência, nada copiado; clone em `C:\projetos\ref\atools`):**
- Lista: usa `RequestFacilitiesList(AIRPORT)` e despacha até receber a lista inteira (não usa `RequestAllFacilities`). Na AIRSIM-2, tentar `RequestAllFacilities` e cair para `RequestFacilitiesList` se não vier nada em alguns segundos.
- Detalhes em **várias definições por aeroporto**, todas começando por `OPEN AIRPORT` + `ICAO` (para casar a resposta): (1) contagens `N_RUNWAYS`, `N_STARTS`, `N_FREQUENCIES`, `N_HELIPADS`, `N_APPROACHES`, `N_DEPARTURES`, `N_ARRIVALS`, `N_TAXI_POINTS`, `N_TAXI_PARKINGS`, `N_TAXI_PATHS`, `N_TAXI_NAMES`, `N_JETWAYS`; (2) base `ICAO`, `REGION`, `LATITUDE`, `LONGITUDE`, `ALTITUDE`, `NAME64`, `TOWER_LATITUDE`, `TOWER_LONGITUDE`, `TOWER_ALTITUDE`, `TRANSITION_ALTITUDE`, `TRANSITION_LEVEL`; (3) `FREQUENCY` (`TYPE`, `FREQUENCY`, `NAME`); (4) `RUNWAY` (`LATITUDE`, `LONGITUDE`, `ALTITUDE`, `HEADING`, `LENGTH`, `WIDTH`, `PATTERN_ALTITUDE`, `SURFACE`, `PRIMARY_/SECONDARY_NUMBER`, `_DESIGNATOR`, `_ILS_ICAO`, `_ILS_REGION`, `_ILS_TYPE`, structs `_THRESHOLD`/`_BLASTPAD`/`_OVERRUN` (`LENGTH`), `_APPROACH_LIGHTS` (`SYSTEM`, `HAS_END_LIGHTS`, `HAS_REIL_LIGHTS`, `HAS_TOUCHDOWN_LIGHTS`), `_LEFT_VASI`/`_RIGHT_VASI` (`TYPE`, `ANGLE`)); (5) `TAXI_PARKING` (`TYPE`, …), `TAXI_PATH` (`TYPE`, `RUNWAY_NUMBER`, `RUNWAY_DESIGNATOR`, `LEFT_EDGE_LIGHTED`, `RIGHT_EDGE_LIGHTED`, `CENTER_LINE_LIGHTED`, …). Só pede a definição se a contagem correspondente for > 0.
- Lotes de **2.000 pedidos**, despachando até cada pedido receber `FACILITY_DATA_END` ou exceção, com pausa de 50 ms entre despachos.
- Pista iluminada = luz de borda do `TAXI_PATH` do tipo pista com o mesmo número/designador **ou** luz de toque, de fim ou REIL **ou** sistema de aproximação > 0 **ou** ILS em qualquer cabeceira. Gates e carga = contagem de `TAXI_PARKING` por `TYPE`. Militar = pelo **nome** do aeroporto (lista de palavras e expressões). Torre = frequência de torre.
- **Ajuste para a AIRSIM-2:** acrescentar ao `SimRunwayData` as luzes de aproximação (as duas cabeceiras) e passar a usar a regra acima no `IsLighted` (no lugar da herança da pista existente da AIRSIM-1); avaliar `TAXI_PARKING` para gates/carga e o militar pelo nome (critério próprio, não copiar a lista da atools).
