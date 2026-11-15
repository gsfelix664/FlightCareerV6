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

## Regras de Prioridade
1. **POH**: Prioridade máxima. Valores de POH nunca são sobrescritos uma vez registrados.
2. **SimVar**: Extraídos por mediana das linhas válidas do voo/pátio no simulador.
3. **Cfg**: Extraídos de arquivos de configuração quando disponíveis.
4. **NativeOnset**: Menor RPM registrada quando a taxa nativa de overstress torna-se positiva.
5. **ClassDefault**: Valores padrão por classe de aeronave (`_class_defaults.json`).
