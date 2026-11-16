# Ferramentas de Replay de Voos — FlightCareer V6

Ferramenta de linha de comando e scripts para replay determinístico de voos reais gravados via REC (CSV de sensores) pela lógica de desgaste contínuo, faixas de estresse e regras do voo (`PartFlightTracker`), sem necessidade do simulador.

## 1. Ferramenta CLI (`FlightCareer.Replay`)

A ferramenta de linha de comando permite reexecutar um log em segundos, inspecionando o desgaste por peça, excedências operacionais, inspeções, eventos derivados e diagnósticos:

```bash
dotnet run --project src/FlightCareer.Replay -- <csv|csv.gz> [opções]
```

### Opções:
- `--aircraft <modelo>`: Identificador ou modelo da aeronave (ex.: `"Cessna C152"`, `"C172SP G1000"`). Se omitido, deduz automaticamente a partir do arquivo `-context.json` correspondente.
- `--blackbox <arquivo>`: Caminho de arquivo blackbox (`.csv`) para leitura automática de dados de toque.
- `--touchdowns <json>`: Caminho de arquivo `.touchdowns.json` com parâmetros de toque.
- `--json`: Emite a saída estruturada em JSON para integração com scripts e automações.

### Exemplo:
```bash
dotnet run --project src/FlightCareer.Replay -- src/FlightCareer.Tests/Fixtures/Flights/c152-sbmt-snej-20261115.csv.gz --aircraft "Cessna C152"
```

## 2. Geração de Fixtures (`make_fixture.py`)

O script `make_fixture.py` processa um log de sensores bruto (`%LocalAppData%\FlightCareer\sensor-logs\*.csv`) e extrai uma fixture compacta (`.csv.gz` com tamanho alvo < 1 MB) mantendo apenas as colunas consumidas pelas regras de desgaste e pelo `PartEventDeriver`, eliminando dados de coordenadas geográficas:

```bash
python tools/replay/make_fixture.py <csv> [--blackbox <arquivo>] --out <pasta_destino> [--name <nome_base>]
```

### Exemplo:
```bash
python tools/replay/make_fixture.py "%LocalAppData%\FlightCareer\sensor-logs\20261115-1747-Cessna_C152.csv" --blackbox "src/FlightCareer.UI/bin/Debug/net10.0-windows/logs/flights/flight_20261115_174949_SBMT_SNEJ_PS-BYN.csv" --out "src/FlightCareer.Tests/Fixtures/Flights" --name "c152-sbmt-snej-20261115"
```

O script gera:
1. `<nome>.csv.gz`: telemetria filtrada e comprimida;
2. `<nome>.touchdowns.json`: lista de toques extraída das linhas `TOQUE:` do blackbox.

## 3. Fuzz Testing Procedural com Voos Sintéticos

O subcomando `fuzz` gera centenas de voos sintéticos realistas e reprodutíveis (via semente), injeta abusos operacionais e falhas de dados com gabarito de rastreio, e verifica se todas as invariantes fundamentais (saúde, horas simuladas, ausência de NaN, diagnósticos únicos, monotonicidade) foram rigorosamente respeitadas:

```bash
dotnet run --project src/FlightCareer.Replay -- fuzz [--runs <n>] [--aircraft <modelo>] [--seed <n>]
```

### Opções:
- `--runs <n>`: Número de voos sintéticos procedurais a simular (padrão 1000).
- `--aircraft <modelo>`: Restringe os voos a um modelo específico (ex.: `"Cessna C152"`, `"C172SP G1000"`, `"Beechcraft Baron G58"`, `"Asobo TBM 930"`). Se omitido, distribui as corridas entre as classes representativas.
- `--seed <n>`: Semente pseudoaleatória inicial determinística.

### Artefatos de Falha:
Para cada voo em que uma invariante for violada, a ferramenta grava automaticamente os artefatos de reprodução em `%LocalAppData%\FlightCareer\fuzz-failures\`:
- `<base>.csv`: telemetria sintética exportada no mesmo padrão RFC 4180 do gravador do simulador.
- `<base>.touchdowns.json`: lista de toques injetados.
- `<base>.groundtruth.json`: gabarito dos abusos planejados e detalhes das violações detectadas.

Para reproduzir e depurar um voo com falha detectado pelo fuzz, basta reabrir o CSV com o próprio comando de replay:
```bash
dotnet run --project src/FlightCareer.Replay -- "%LocalAppData%\FlightCareer\fuzz-failures\<arquivo>.csv" --touchdowns "%LocalAppData%\FlightCareer\fuzz-failures\<arquivo>.touchdowns.json" --aircraft "<modelo>"
```
