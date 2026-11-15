# POH-C152-ALT — Capacidade do alternador do C152 pelo POH

**Branch:** feat/poh-c152-alternador · **Data:** 2026-11-15

## Plano (antes de codar)
- Consultar o POH oficial do Cessna 152 (1979) em `https://www.touringmachine.com/PDFs-Txt/Cessna-152_POH.pdf`, inspecionando a Seção 7 (Airplane & Systems Descriptions › Electrical System).
- Localizar o trecho literal com o valor nominal do alternador em ampères, registrando seção, página e citação exata.
- Adicionar o limite em `tools/aircraft_limits/poh_values.csv` com `key: alternatorRatedAmps`, `value: 60`, `unit: amperes` e referência formatada idêntica ao padrão aceito no C172.
- Aplicar o novo limite em `src/FlightCareer.Infrastructure/Data/aircraft/cessna-c152.json` através do `tools/aircraft_limits/merge_limits.py`, incrementando `dataVersion` para 6.
- Executar os testes Python (`python -m unittest discover -s tools/tests`), build e testes xUnit da solução (`dotnet build src/FlightCareer.slnx` e `dotnet test src/FlightCareer.Tests`).
- Atualizar `docs/CHANGELOG.md` e gerar commit `feat(limits): ...` na branch.

## O que foi feito
- **Inspeção do POH do Cessna 152**:
  - Documento: *Cessna 152 Pilot's Operating Handbook 1979* (`https://www.touringmachine.com/PDFs-Txt/Cessna-152_POH.pdf`).
  - Seção: Section 7 (*Airplane & Systems Descriptions*), subseção *Electrical System*, página 7-22 (PDF p. 120).
  - Evidência literal: *"The airplane is equipped with a 28-volt, direct-current electrical system (see figure 7-7). This system uses a 24-volt battery mounted on the right forward side of the firewall as the source of electrical energy and a belt-driven 60-amp alternator to maintain the battery's state of charge."*
  - Não constam opções alternativas de alternador no manual ou em suplementos.
- `tools/aircraft_limits/poh_values.csv`:
  - Adicionada linha:
    `Cessna C152,alternatorRatedAmps,60,amperes,"Cessna 152 POH 1979, Sec. 7 p. 7-22 (28-volt DC 60-amp alternator, https://www.touringmachine.com/PDFs-Txt/Cessna-152_POH.pdf)"`
- `src/FlightCareer.Infrastructure/Data/aircraft/cessna-c152.json`:
  - Atualizado via `tools/aircraft_limits/merge_limits.py` com `alternatorRatedAmps: 60.0`, `source: POH`, unidade `amperes` e `dataVersion` incrementado de 5 para 6.
- `docs/CHANGELOG.md`:
  - Adicionada entrada no fim do arquivo documentando a capacidade nominal do alternador do C152 pelo POH.

## Critérios de aceite
- [x] Texto literal localizado com página, seção e trecho exato no POH: Seção 7, p. 7-22 (*"belt-driven 60-amp alternator"* em *"28-volt, direct-current electrical system"*).
- [x] Entrada registrada em `tools/aircraft_limits/poh_values.csv`.
- [x] Limite gravado em `cessna-c152.json` (`source: POH`, formato idêntico ao C172) com `dataVersion` incrementado de 5 para 6.
- [x] Atualização executada via fluxo oficial `merge_limits.py`.
- [x] `dotnet build src/FlightCareer.slnx`: 0 erros.
- [x] `dotnet test src/FlightCareer.Tests`: 302 testes passando (100%).
- [x] `python -m unittest discover -s tools/tests`: 45 testes passando (100%).
- [x] Entrada no fim de `docs/CHANGELOG.md`.

## Build e testes
- `dotnet build src/FlightCareer.slnx`: 0 erros, 0 avisos novos.
- `dotnet test src/FlightCareer.Tests`: 302 aprovados, 0 falhas.
- `python -m unittest discover -s tools/tests`: 45 aprovados, 0 falhas.

## Não testado
- Voo no MSFS monitorando acionamento da regra de sobrecarga elétrica com carga superior a 60 A no C152.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida. O POH oficial de 1979 é inequívoco quanto aos 60 A nominais em 28 V DC e não lista modelos ou anos com alternadores opcionais.
