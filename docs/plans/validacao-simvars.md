# Plano — Validação das SimVars e ligação com o desgaste por peça

Criado em 2026-11-16 (Claude). Pré-requisito: ferramenta `FlightCareer.SimVarProbe` (tarefa SIMVAR-PROBE, branch `feat/simvar-probe`).

## Contexto
- `docs/plans/sensor-research/simvars-msfs2024.csv`: 1.678 SimVars da documentação do SDK 2024, cruzadas com o SimvarWatcher e com o `sensor_registry.json`.
- `src/FlightCareer.Infrastructure/Data/simvar_probe_list.csv`: lista de entrada do SimVarProbe (1.755 = documentação ∪ lista interna do SimvarWatcher; ~4.500 leituras com índices).
- Ferramentas de descoberta no Developer Mode do MSFS 2024: Behaviors Debug (abas Variables e Input Events, Ctrl+G sobre um botão), Electrical/Fuel/Hydraulic System Debug (índices de circuitos, barramentos e linhas), Wheels (índices dos pontos de contato), SimConnect Inspector (exceções e definições do nosso app).
- Regra: **existir na documentação ou ser aceita pelo sim não prova que a variável mede algo**. Só entra no registro o que varia de forma coerente num voo real.

## Passo 1 — Rodar o SimVarProbe em 3 ou 4 aviões
- Aviões: C152, C172 G1000, um retrátil/bimotor (Baron ou Bonanza: trem, flap, hidráulico, dois motores) e um turbo-hélice (King Air ou TBM: turbina e perdas nativas `TURB ENG ... LOSS`).
- Em cada um, voo curto com marca (`m`+Enter) em cada fase: pátio desligado → partida → táxi (curvas e frenagem) → decolagem → cruzeiro com comandos (aileron, profundor, leme) → pouso.
- Opção `--max-generic 20` para cobrir os circuitos.
- Quem faz: usuário (precisa do simulador).

## Passo 2 — Matriz variável × avião
- Script (em `tools/`, só biblioteca padrão, com teste) que junta os `report.csv` numa tabela: aceita e varia / aceita e parada / rejeitada, por avião.
- Resultado na `auditoria-registro.md` e numa coluna `validado` da `simvars-msfs2024.csv`.
- A partir daqui, nenhuma variável entra no registro sem prova da matriz.

## Passo 3 — Limpar e corrigir o registro atual (Gemini)
- Variáveis que alimentam regra e nunca mudaram nos logs do C152/C172: `ENG VIBRATION`, `GEAR SKIDDING FACTOR` → trocar por sensor que funcione ou tirar da regra.
- `CONTACT POINT COMPRESSION` / `CONTACT POINT IS SKIDDING`: ler os índices certos (`:0..N`) em vez do `:1` herdado de `perEngine`.
- `ELECTRICAL GENALT *` não funciona no C172 2024 → trocar por `ELECTRICAL GENERATOR AMPS/VOLTAGE/ACTIVE:n` se a matriz confirmar.
- Variáveis novas aprovadas (Top 25 da pesquisa e candidatas de desgaste) entram no registro.

## Passo 4 — Ligar as variáveis às peças (Gemini; decisões do Claude na spec)
Para cada peça de `parts_catalog.json`: sensor → regra em `wear_rules.json` → limite por aeronave em `Data/aircraft/` (prioridade POH → SimVar → CFG → início da taxa nativa → padrão da classe). Cada regra nova com teste de replay. Exemplos:
- `POSITIVE/NEGATIVE G LIMIT FLAPS UP/DOWN` → camada "SimVar" dos limites de G (resolve também o G negativo).
- Deflexão dos comandos × `DYNAMIC PRESSURE` → comando pleno acima da Va (empenagem, dobradiças, comandos).
- `TOTAL WEIGHT` × `MAX LANDING WEIGHT` / `MAX TAKEOFF WEIGHT` → pouso e decolagem acima do peso (inspeção estrutural).
- `CG PERCENT` × `CG FWD/AFT LIMIT` → operação fora do CG.
- `SEMIBODY LOADFACTOR X/Z` → carga lateral no trem e na célula.
- `REJECTED TAKEOFF BRAKES ACTIVE`, `BRAKE * TEMPERATURE EFFECT` → freios.
- Pontos de contato por roda (`IS ON GROUND`, `SKIDDING FACTOR`, `SURFACE TYPE`) → pneus e rolamentos por roda e por piso.
- Disjuntores e conexões (`ALTERNATOR/BATTERY CONNECTION ON`, `LINE/BUS BREAKER PULLED`) → eventos de sobrecarga elétrica.
- Turbina: `TURB ENG N1 LOSS`, `... EFFICIENCY LOSS` → só sensor/conferência (decisão de 2026-11-15: o addon é a fonte da verdade, o nativo não vira % de desgaste e nada é gravado no sim).

## Passo 5 — Bugs do fuzz (precisa de autorização do usuário)
Pode ir junto com o passo 4 (mesma área). Lista em `docs/reviews/FUZZ-revisao.md` (rodada 2):
- `PartWearEngine.cs:176` descarta limite `<= 0` → `airframe_over_g_negative` nunca dispara.
- `egtMax` ausente em 35/46 aeronaves e sem padrão de classe → `piston_egt_excess` mudo.
- Gerador: posição do trem em 0–1 (o sim usa 0–100), voos curtos demais para os acumuladores; endurecer `isUp` do `PartEventDeriver`.
- Depois: o gerador sintético simula também os sensores novos; `fuzz --runs 1000` com falhas perto de zero.

## Passo 6 — Prova no simulador
- Voo normal com REC ligado → nova fixture de replay.
- Voos de abuso de propósito (comando pleno acima da Va, pouso acima do peso, flap acima da Vfe, trem acima da Vle num retrátil) para confirmar que cada regra dispara.
- Fecha também as pendências antigas de teste no MSFS (`docs/agents/HANDOFF.md`).

## Prioridade
O passo 4 é o que dá mais retorno; os passos 1 a 3 garantem que ele não seja construído sobre variáveis que não funcionam.
