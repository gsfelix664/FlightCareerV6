# Passagem de sessão (Claude) — 2026-11-15 (noite)

Leia junto: `AGENTS.md` (§3 comandos do replay, §6 regras, §7 estado), `docs/agents/LICOES.md` (várias lições novas de hoje), `docs/CHANGELOG.md` (últimas entradas), `docs/agents/ORQUESTRACAO.md`, `tools/replay/README.md`.

## Estado
- Base `feat/component-health-system` em dia com o GitHub (merge `445bff1`). Build 0 erros; `dotnet test` **440/440**; Python **48/48**. Worktrees do Gemini livres e limpos: `../FLIGHTCAREERV6-gemini` (em `feat/voos-sinteticos`, já mergeada) e `../FLIGHTCAREERV6-gemini2` (em `docs/pesquisa-variaveis`, já mergeada).
- `REGRAS_tiers.txt` na raiz está **não rastreado e não é desta sessão**: não commitar nem apagar sem perguntar ao usuário.
- Repositórios de referência clonados (só leitura, nunca executar/compilar): `C:\PROJETOS\FLIGHTCAREERV6-refs\` (FsConnect, MobiFlight-Connector, msfs2020-go, flybywiresim/aircraft — GPL, não copiar código). `fseconomy/fse-simconnect` não existe.
- MSFS 2024 SDK Samples instalados em `C:\MSFS 2024 SDK\Samples\`; SimvarWatcher já compilado em `C:\MSFS 2024 SDK\Samples\VisualStudio\SimvarWatcher\bin\x64\Release\Simvars.exe`.
- **Regras de orquestração do usuário (mantidas):** tudo vai para o Gemini (inclusive arquivos quentes) com spec passo a passo; o Claude revisa linha a linha, roda build/testes e faz o merge; push ao fim de cada trabalho (`FC_ALLOW_PROTECTED=1` para a base). Rodar o `gemini-task.sh` sempre com `run_in_background` (nunca `&` dentro de comando comum: não chega notificação).

## Concluído hoje (todos aprovados, mergeados e enviados)
1. **F2.4** — peças no voo (`PartFlightSession`/`PartFlightTracker`), barras derivadas das peças; revisor corrigiu perda do salvamento no fim do voo.
2. **Spike elétrico + teste de solo do C172** (`teste-eletrico-C172.md`): aviônicos = `CIRCUIT ON:3`; tensão por `ELECTRICAL BUS VOLTAGE:1`; `BATTERY VOLTAGE/LOAD`, `GENALT`, `MAIN BUS` e `AVIONICS MASTER SWITCH` não servem no C172 2024.
3. **F2.4b-CAL** — três regras elétricas ativas + **modo Calibrar** no HUD + `tools/aircraft_limits/calibrate.py`. C172 e C152 calibrados; alternador 60 A pelo POH nos dois (C152 POH 1979 p. 7-22, conferido no PDF).
4. **Voo real completo no C152 (SBMT→SNEJ)** analisado: sem panes; falta de potência na subida foi pilotagem (IAS 43–56 kt, mistura a 30% cedo).
5. **FIX-PARTS-SENSORS** — regras contínuas resolvem sensores por motor (`eng-rpm:1`), multimotor, diagnóstico único, taxa nativa com o nome certo (`nw-overstress-rate-<id>`), teste de contrato registro × regras nas 46 aeronaves.
6. **REPLAY** — `FlightReplayer` + CLI `src/FlightCareer.Replay` + fixtures reais (`src/FlightCareer.Tests/Fixtures/Flights/`, C152 voo/calibração e C172 elétrico).
7. **FIX-REPLAY-BUGS** — sobrecarga elétrica exige queda de tensão; `requiresEngineRunning` (óleo 30 s); choque térmico por taxa de CHT (`chtCoolingRateMax` 28 °C/min = **estimativa**, fonte de fabricante não confirmada); replay `GameLike`.
8. **FIX-START-EVENTS** — partida/starter/aviônicos observados na preparação (`ObservePreflight`), salvos no cancelamento.
9. **Pesquisa de variáveis** (3 rodadas, `docs/plans/sensor-research/`): auditoria do registro (provas pelos CSVs: coluna no CSV = nome aceito), `COVER ON:0..7`, LVars direto pelo SimConnect (`L:NOME`), Input Events, 28 variáveis novas e Top 25 com roteiro de teste de ~15 min.
10. **FUZZ** — voos sintéticos com gabarito (voo-controle × voo com abuso); teste de mutação confirma que pega o bug `eng-rpm:1`. `fuzz --runs 1000 --seed 777`: 644 aprovados / 356 falhas, classificadas abaixo.

## Próximos passos (na ordem)
1. **Corrigir os bugs achados pelo fuzz** (`docs/reviews/FUZZ-revisao.md`, rodada 2) — o usuário ainda não autorizou; pergunte antes:
   - produção: `PartWearEngine.cs:176` descarta limite `<= 0` → `airframe_over_g_negative` nunca dispara (`gLimitNeg` sempre negativo, padrão −1,52/−1,0);
   - catálogo: `egtMax` ausente em 35/46 aeronaves e sem padrão de classe → `piston_egt_excess` mudo;
   - gerador: posição do trem em 0–1 (o sim usa 0–100) e voos curtos demais para os acumuladores (10 min bateria / 5 min sobrecarga); endurecer o teste `isUp` do `PartEventDeriver`;
   - prova: rodar `fuzz --runs 1000` de novo e as falhas caírem para perto de zero.
   - decisão do usuário: `landingCycles` só em 4 peças estruturais (pneus/amortecedores não somam ciclo).
2. **Variáveis novas no registro** a partir do Top 25 da pesquisa (`descoberta-variaveis.md`), incluindo `CONTACT POINT COMPRESSION/IS SKIDDING` por ponto de contato (`:0..N`, hoje só `:1` por engano de `perEngine`), `COVER ON:0..7`, `ENG OIL QUANTITY`, `LINE BREAKER PULLED` (nome certo; `ELECTRICAL LINE BREAKER PULLED` não existe). Depois, o usuário faz o teste de solo de ~15 min (ou confere com o SimvarWatcher).
3. Pendências de baixo valor: exibir no replay a perda por multiplicador das faixas de cautela (EGT aparece com "0%"); unidade `GEAR POSITION`.
4. Depois: Fase 3 (walkaround, revisões que só inspecionam, troca de peças com tempo de hangar), ideias em `docs/plans/pecas-sugestoes-usuario.md`.

## Pendências de teste no MSFS
Preparação/cancelamento do `FlightMonitor` com eventos de partida (FIX-START-EVENTS); botão Calibrar e leitura ampliada (já usado uma vez no C152, ok); F4.2e (pane real de alternador e flap); pneu/trem com dano e pista de terra; crash declarado pelo sim. Um voo normal com REC ligado vira nova fixture de replay.
