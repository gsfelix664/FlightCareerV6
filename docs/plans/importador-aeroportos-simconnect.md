# Plano: base de aeroportos e pistas lida do próprio simulador (2026-11-24)

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

## 4. Proposta
1. **`AirportImporter` (Infrastructure, interface `IAirportSource`)** com três fontes, nesta ordem:
   1. **Simulador aberto (SimConnect)** — caminho principal: lista mundial + `RequestFacilityData` em lotes (fila com N pedidos em voo, reenvio em falha, barra de progresso e cancelamento). Grava `Airports` e `Runways` com o mesmo critério de tier de hoje (`DataSeeder`).
   2. **Banco do Little Navmap** (como hoje), se o sim estiver fechado e o LNM instalado.
   3. **`airports.csv`/`runways.csv`** como último recurso, com aviso na tela ("dados podem estar desatualizados; abra o simulador e clique em Reconstruir base").
2. **Enriquecimento:** cidade, país e elevação faltantes vêm do csv **só com posição a até 10 NM** (`DataSeeder.IsSameAirportPosition`, já existe).
3. **Primeira inicialização:** tela de preparo ("Abra o MSFS e espere o menu principal" → progresso); o jogador pode pular e usar a fonte 2 ou 3.
4. **Botão "Reconstruir base de aeroportos"** (Configurações) para depois de atualizações do sim ou de cenário novo; a reconstrução **não apaga** missões, frota nem carreira (aeroportos têm chave pelo código; revisar o que referencia `Airport.Id`).
5. **Mesmo código para 2020 e 2024** pelo SimConnect (a lista muda de função conforme a versão detectada). Leitura de BGL do 2020 **fora do escopo** (decisão do usuário: foco no MSFS 2024; o 2020 só como reserva).
6. **Depois:** regerar `airport_profiles.json` a partir da base importada (o script `tools/places/airport_profiles.py` passa a ler essa base ou o banco do jogo), para etiquetas e posições virem da mesma fonte.

## 5. Testes
- Lógica pura (montagem do registro de aeroporto/pista a partir dos campos recebidos, tier, enriquecimento por posição, fila de pedidos com reenvio) testável sem o simulador, com respostas gravadas.
- **Teste no PC com o MSFS (usuário):** importar o mundo no 2024 (tempo total, quantidade de aeroportos, SBOK = Amontada, pistas de 10 aeroportos conferidas contra o Little Navmap); repetir no 2020 se disponível. Não dá para testar no PC do escritório (sem simulador).

## 6. Limitações aceitas
- Aeroportos de cenário de terceiros podem não aparecer no 2024 (igual ao Little Navmap).
- A primeira carga exige o simulador aberto e alguns minutos.
