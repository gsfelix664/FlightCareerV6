# Passagem de sessão (Claude) — 2026-11-16 (manhã)

Leia junto: `AGENTS.md` (§3 comandos, §6 regras, §7 estado), `docs/agents/LICOES.md`, `docs/plans/validacao-simvars.md` (plano em andamento), `docs/plans/sensor-validation/probe-runs/matriz-C152.md` (resultado dos testes de hoje), `tools/simvar_probe/README.md`.

## Próxima máquina: PC do escritório, **sem MSFS e sem SDK**
- Build e testes funcionam sem o SDK: as DLLs do SimConnect estão versionadas em `src/FlightCareer.UI/` (o `FlightCareer.SimVarProbe` usa as mesmas).
- Nada que precise do simulador roda lá: SimVarProbe, REC, Calibrar, SimvarWatcher, teste de panes. Trabalhar só com dados já gravados (matriz, fixtures de replay, voos sintéticos) e lógica testável.
- Os logs de sensores e do probe ficam em `%LocalAppData%` de **esta** máquina; o que interessa já foi copiado para o repositório (abaixo).

## Estado
- Base `feat/component-health-system` em dia com o GitHub. Build 0 erros; `dotnet test` **455/455**.
- `REGRAS_tiers.txt` na raiz: **não rastreado e não é dos agentes** (anotações do usuário sobre tiers de missão). Não commitar nem apagar sem perguntar.
- Worktrees do Gemini nesta máquina (`../FLIGHTCAREERV6-gemini` em `feat/simvar-probe`, `../FLIGHTCAREERV6-gemini2`): limpos e já mergeados. No PC do escritório o `gemini-task.sh` cria o worktree de novo.
- **Regras combinadas com o usuário (mantidas):** tudo vai para o Gemini (inclusive arquivos quentes) com spec passo a passo; o Claude revisa linha a linha, roda build/testes, faz o merge **e o push ao fim de cada trabalho** (`FC_ALLOW_PROTECTED=1` na base). `gemini-task.sh` sempre com `run_in_background`.

## Feito hoje
1. **Lista de SimVars do SDK 2024:** `docs/plans/sensor-research/simvars-msfs2024.csv` (1.678, com índice, unidade, gravável, se está no Watcher e no registro). Gerador: `tools/simvar_probe/scrape_simvars.py`.
2. **Ferramentas de descoberta** (Developer Mode): Behaviors Debug (abas Variables e Input Events, Ctrl+G), debugs de sistema (índices elétricos), Wheels (pontos de contato), SimConnect Inspector (exceções do nosso app). Resumo no plano.
3. **SimVarProbe** (SIMVAR-PROBE, Gemini, aprovado; revisão em `docs/reviews/SIMVAR-PROBE-revisao.md`): console que valida e grava ~4.750 leituras (ou ~11.100 com `--max-generic 20`) a cada 1 s, sem deslocamento entre blocos. Lista de entrada: `src/FlightCareer.Infrastructure/Data/simvar_probe_list.csv` (documentação ∪ lista interna do SimvarWatcher, 1.755). Gerador: `tools/simvar_probe/make_probe_list.py`.
4. **Plano** `docs/plans/validacao-simvars.md` (passos 1, 1b panes/Key Events, 2 a 6).
5. **Testes no MSFS (C152, usuário):** 1 rodada parada + 2 voos até ~580 ft. Resultados copiados para `docs/plans/sensor-validation/probe-runs/` (`report.csv`, `summary.txt`, `strings.csv`, `values.csv.gz`) e matriz `matriz-C152.csv` / `matriz-C152.md` (gerada por `tools/simvar_probe/build_matrix.py`).
6. Arquivos do SimvarWatcher (`.simvars` do Top 25 e das candidatas de desgaste) em `tools/simvar_probe/watcher/`.

## Achados dos testes de hoje (C152)
- **O MSFS caiu 3 vezes com o mesmo erro interno** (`0xc0000005`, offset `0x2fbae4c` em `FlightSimulator2024.exe` 1.9.24.0). Nos dois voos a queda veio ~1–1,5 min depois de `AI TRAFFIC STATE`=`STATE_TAKEOFF` e de `ATC FUTURE AGENT NAME` mudar (troca de controle do ATC); numa terceira vez caiu durante a fase A, entre lotes de `AI TRAFFIC *`. **Suspeita (não provada):** ler variáveis `ATC *` / `AI *` do avião do jogador. Próximo teste do usuário (só nesta máquina): `--filter "^(?!ATC |AI )"`. O registro do addon não lê essas famílias (só `ATC MODEL`/`ATC TYPE` uma vez no contexto).
- **Validadas (variam de forma coerente):** `DYNAMIC PRESSURE`, deflexões de aileron/profundor/leme, `ELECTRICAL GENERATOR AMPS/VOLTAGE/ACTIVE:1` (substitui `GENALT`, que fica 0 em todos os índices), `CONTACT POINT COMPRESSION/IS ON GROUND/IS SKIDDING:0..2`, `COVER ON:0/2`, `CIRCUIT ON:3`, `ELECTRICAL BUS VOLTAGE:1`, `ENG OIL QUANTITY:1`, primer, freios, rodas, flaps, luzes, `SURFACE TYPE`, `SUCTION PRESSURE`, `GYRO DRIFT ERROR`, `TOTAL WEIGHT`, `CG PERCENT`, `GENERAL ENG PCT MAX RPM:1`, `GENERAL ENG ELAPSED TIME:1`, `WEAR AND TEAR LEVEL:39`, `LEAKS DIRT PCT`.
- **Limites de G que o sim informa no C152:** +6/−4,8 (flap recolhido) e +4,2/−3,6 (flap baixado) — **não são os do POH** (+4,4/−1,76 utilitária). Servem só de reserva; POH continua com prioridade.
- **Mortas no C152 (sempre 0):** `ENG VIBRATION`, `GEAR SKIDDING FACTOR`, `SEMIBODY LOADFACTOR X/Z`, `GENERAL ENG DAMAGE PERCENT:1` (sem abuso de rotação no voo; não conclusivo), `MIN G FORCE` (usada na regra `airframe_over_g_negative`).
- `matriz-C152.md` lista os 31 sensores **usados em regras** que não variaram. Muitos são esperados num C152 (trem fixo, sem turbina, hidráulico ou pressurização); os que importam estão acima.

## Próximos passos (no PC do escritório, sem simulador)
1. **Revisar regras × peças × sensores com a matriz do C152** (passos 3 e 4 do plano): para cada regra de `wear_rules.json` que se aplica a pistão/trem fixo, conferir se os sensores dela variaram; trocar ou complementar os mortos (`ENG VIBRATION`, `GEAR SKIDDING FACTOR` → `CONTACT POINT SKIDDING FACTOR`/`IS SKIDDING` por roda; `MIN G FORCE` → `G FORCE` mínimo do voo; `GENALT` → `ELECTRICAL GENERATOR *`); corrigir a leitura dos pontos de contato (`:0..N`, hoje só `:1` por `perEngine`). Decisões na spec, implementação pelo Gemini, testes de replay com as fixtures existentes.
2. **Bugs do fuzz** (`docs/reviews/FUZZ-revisao.md`, rodada 2): G negativo, `egtMax` em 35/46 aeronaves, gerador (trem 0–100, voos curtos). **Pergunte ao usuário antes** (autorização ainda não dada).
3. Regras novas com as variáveis validadas (comando × Va com `DYNAMIC PRESSURE`, peso de pouso, CG) — também só com dados e testes.
4. Extrair os Key Events de pane da documentação (plano › 1b) — só documentação, sem simulador.

## Pendências que precisam do simulador (só nesta máquina)
Teste com filtro ATC/IA; voos em outros aviões (C172, retrátil, turbo-hélice) com o SimVarProbe; abuso de rotação para provar `GENERAL ENG DAMAGE PERCENT`; modo de teste de panes (a implementar); preparação/cancelamento com eventos de partida (FIX-START-EVENTS); F4.2e (pane real de alternador e flap); pneu/trem com dano e pista de terra; crash declarado pelo sim.
