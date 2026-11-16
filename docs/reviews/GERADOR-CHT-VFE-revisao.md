# GERADOR-CHT-VFE — Revisão (Claude)

**Branch:** `fix/gerador-cht-vfe` · **Commits:** `b09e142`, `6d5d1a9` · **Data:** 2026-11-16

## Veredito: APROVADO

## Conferido
- `SyntheticFlightGenerator`: CHT, óleo e EGT do voo normal escalados por multiplicação (sem mudar ordem/quantidade de chamadas a `rnd`); escala 1,0 em quem já passava (`chtMax ≥ 205,9`; óleo só no DHC-6, `oilTempMax` 99; EGT só no C400, `egtMax` 782,41). Abusos não escalados.
- `SyntheticFlapSelector`: vfe:2 → 2; menor n ≥ 2; vfe:1 → 1; genérico → 2. Usado na aproximação e no `FlapOverSpeed` (A330 → 4, A400M/ES30 → 3), casando com o `vfe:current` do `PartWearEngine`.
- Testes novos: seletor, escala de CHT, C152 idêntico à linha de base gravada antes da mudança, voo de controle sem excedência de temperatura/flap nos 46 perfis.
- Revisor rodou: `dotnet build` 0 erros; `dotnet test` **525/525**; fuzz de 30 voos em DA40NG, DA62, Flight Design CT, A330-200, A400M, ES30, DHC-6, C400 e C152: 0 falhas. Fuzz completo do executor: 2.300/2.300.
- Hash do relatório (`b09e142`) existe na branch: sem `--amend`.

## Observação
- `egtMax` do C400 (782,41 °C, SimVar) fica perto da EGT normal do gerador; conferir com o POH junto com os demais (decisão do usuário: revisão dos POHs depois).
