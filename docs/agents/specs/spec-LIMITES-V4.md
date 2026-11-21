# Spec LIMITES-V4 — Preencher limites faltantes do catálogo com `aircraft_limits-v4.json`

**Objetivo (1 frase):** usar o arquivo fornecido pelo usuário (`tools/aircraft_limits/sources/aircraft_limits-v4.json`, já copiado no worktree; commite-o) para preencher **só os limites que faltam** (ausentes ou com `source: ClassDefault`) nos perfis `src/FlightCareer.Infrastructure/Data/aircraft/*.json`, pelo fluxo existente `poh_values.csv` + `merge_limits.py`.

## Fatos já levantados
- Pipeline: `tools/aircraft_limits/poh_values.csv` (`aircraftId,key,value,unit,reference`) + `title_map.csv` + `merge_limits.py` (leia o `README.md` da pasta e o script). Prioridade de fonte: POH → SimVar → Cfg → NativeOnset → ClassDefault (`Domain/Limits/AircraftLimitResolver`). Esquema e chaves válidas: `Data/aircraft/_schema.json`; padrões: `_class_defaults.json`.
- O `merge_limits.py` aceita `--dry-run` e roda sem capturas do simulador? Confira; se exigir `--captures`, aponte para uma pasta vazia no worktree (não commitada) e diga no relatório.
- 44 perfis têm limites estimados `source: ClassDefault` com nota "estimativa — atualizar pelo POH real" (tarefa CATALOGO-PADROES). C152 e C172SP G1000 já têm POH conferido.
- O arquivo v4 tem 30 aeronaves por `icao`/`name`/`variant`, em kt, kg, °C, psi, litros. **Não conferido**: ele se declara POH/AFM/TCDS, mas não foi checado por ninguém. Trate como fonte do usuário.

## O que fazer
1. Mapear cada aeronave do v4 para um `aircraftId` do catálogo pelo nome/variante (ex.: `C152`→`Cessna C152`, `BE58`→Baron G58, `A20N`→a320neo, `AT46`/`AT76`→ATR, `C208`→c208b, `TBM9`→asobo-tbm-930, `CC19`→nxcub, `CTSL`→flight-design-ct se for o mesmo modelo, `DR40`→robin-dr400 ...). Sem correspondência clara (ex.: Dreamlifter, A330-900, PC-6, ICON A5, VL-3, Savage Cub, CAP 10, XCub, SF50 se não existirem no catálogo): **não crie perfil**; liste no relatório. Correspondência duvidosa (variante diferente, ex.: CAP 10 × CAP-4, DR400/100 × DR400 do sim, CTSL × CT): não use; liste para decisão do usuário.
2. Para cada aeronave mapeada, converter os campos do v4 para as chaves do `_schema.json` que **existem** (ex.: `vne_never_exceed`/`vmo_max_operating` → vne/vmo, `vfe_flaps` → a chave de Vfe que o esquema usa (se houver por dente, respeite o formato; se for valor único, use o **menor** Vfe do avião e explique), `vle_gear_extended`, `vlo_*`, `g_load_clean*` → gMax/gMin (para normal/utility, use a categoria **normal**), `max_rpm_continuous` → rpmRedline, `max_oil_temp_c` → oilTempMax, `oil_pressure_psi.min` → oilPressMin, `max_cht_c` → chtMax, ITT/EGT de turbina → as chaves de ITT/EGT do esquema). Campo do v4 sem chave no esquema: **ignore** (não invente chave nova); liste os ignorados por tipo no relatório. Unidades iguais às do esquema (converta se preciso).
3. Gerar as linhas no `poh_values.csv` **apenas** para (aeronave, chave) cujo valor atual no perfil é ausente ou `ClassDefault`. Nunca substitua valor de fonte POH/SimVar/Cfg/NativeOnset existente; se o v4 divergir de um POH existente em > 5 %, liste a divergência no relatório (sem mudar nada).
   - `reference`: `"aircraft_limits-v4.json (usuário, 2026-11-17; declara <operational_manual do v4>; não conferido no POH)"`.
4. Rodar `merge_limits.py` (primeiro `--dry-run`, depois de verdade) e conferir que só os perfis/chaves esperados mudaram (`git diff --stat`) e que `dataVersion` foi incrementado como o script faz.
5. Pistão com EGT: não importar EGT de pistão < 700 °C (regra já existente do script).

## Fora do escopo
- Código C# (nenhum), `component_catalog.json`, `parts_catalog.json`, `wear_rules.json`, índices elétricos. Pesos, vento, altitude de pista, AP mínimo: só se o esquema já tiver chave; senão ignore.

## Testes / verificação
- `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (os testes do catálogo/loader validam os JSON).
- `dotnet run --project src/FlightCareer.Replay -- fuzz --aircraft all --runs 20`: cole o resumo (0 limites ausentes, 0 falhas). Se algum avião passar a falhar no fuzz por causa de um limite novo, **não ajuste o oráculo**: reverta aquele valor e liste.
- `python -m unittest discover -s tools/tests` verde.

## Critérios de aceite
- [ ] Relatório com tabela: aeronave v4 → aircraftId (ou "sem correspondência"/"duvidosa"), chaves preenchidas, chaves ignoradas, divergências com POH existente.
- [ ] Nenhum valor POH existente alterado (prove com o diff).
- [ ] Entrada no fim de `docs/CHANGELOG.md` (`## 2026-11-17 - Limites do aircraft_limits-v4 nos perfis (Gemini)`); `AGENTS.md` › 7 (Catálogo de limites) com uma linha sobre a fonte v4 não conferida.
- [ ] Um commit: `feat(limits): limites faltantes dos perfis a partir do aircraft_limits-v4 (LIMITES-V4)`.
- [ ] Só fatos conferidos no relatório (lições CATALOGO-PADROES e NOTA-PASSAGEIROS).
