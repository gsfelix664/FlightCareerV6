# Ferramenta de Montagem do Catálogo de Limites por Aeronave

Esta ferramenta processa os logs e contextos de telemetria capturados no simulador (`.csv` e `-context.json`) juntamente com a tabela de valores extraídos de manuais/POH (`poh_values.csv`) e mapeamentos de títulos (`title_map.csv`), aplicando regras estritas de prioridade de fontes para atualizar os arquivos JSON de cada aeronave em `src/FlightCareer.Infrastructure/Data/aircraft/`.

## Estrutura dos Arquivos

- `poh_values.csv`: Limites extraídos manualmente de manuais do fabricante (POH/AFM/SB).
  Formato: `aircraftId,key,value,unit,reference`
- `title_map.csv`: Mapeamento entre o título informado pelo simulador (`Title` no context) e o `aircraftId` canônico do catálogo.
  Formato: `simTitle,aircraftId`
- `merge_limits.py`: Script principal de consolidação e atualização.

## Como Executar

```bash
python tools/aircraft_limits/merge_limits.py \
  --captures "%LocalAppData%/FlightCareer/sensor-logs" \
  --poh tools/aircraft_limits/poh_values.csv \
  --map tools/aircraft_limits/title_map.csv \
  --out src/FlightCareer.Infrastructure/Data/aircraft
```

### Opções:
- `--dry-run`: Executa a leitura e análise sem gravar alterações no disco, exibindo o resumo das atualizações e divergências encontradas.
- Títulos sem mapeamento em `title_map.csv` são listados ao final da execução como aviso.

## Analisador de Calibração de Solo (`calibrate.py`)

O script `calibrate.py` analisa logs de calibração no solo (~10 min) gerados pelo roteiro guiado do HUD Overlay (`cal:<passo>`) para deduzir os parâmetros elétricos e superfícies da aeronave.

### Uso
```bash
python tools/aircraft_limits/calibrate.py <caminho_csv> [--context <caminho_context_json>] [--aircraft <id>] [--write]
```

### O que descobre:
1. `elecAvionicsCircuitIndex`: Índice global do circuito (`CIRCUIT ON:n`) que liga/desliga com a chave de aviônicos.
2. `elecAvionicsBusAmpsIndex`: Índice do barramento de corrente (`ELECTRICAL BUS AMPS:n`) que alimenta os aviônicos (queda de corrente significativa ao desligar a chave).
3. `elecMainBusIndex`: Índice do barramento principal de tensão (`ELECTRICAL BUS VOLTAGE:n`) conectado ao alternador.
4. `elecSystemVolts`: Tensão nominal do sistema elétrico (28 V ou 14 V).
5. Limites de superfícies e motor (aproveita extrações já suportadas por `merge_limits.py`).

### Opções:
- `--write`: Atualiza o arquivo `src/FlightCareer.Infrastructure/Data/aircraft/<id>.json` com os limites de fonte `SimVar` encontrados, sem jamais sobrescrever valores já registrados de `POH`. Incrementa `dataVersion`. Sem `--write`, apenas exibe o relatório e plano de alteração no terminal.
- `--aircraft <id>`: Força o identificador canônico da aeronave caso não seja detectado automaticamente pelo `-context.json` ou cabeçalho.

