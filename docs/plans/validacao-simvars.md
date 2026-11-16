# Plano — Validação das SimVars e ligação com o desgaste por peça

Criado em 2026-11-16 (Claude). Pré-requisito: ferramenta `FlightCareer.SimVarProbe` (tarefa SIMVAR-PROBE, **aprovada e mergeada** em 2026-11-16; uso em `tools/simvar_probe/README.md`). Primeira rodada no C152 parado: 4.750 leituras, 94 rejeitadas, 104 variando.
Resultados do C152 (2026-11-16): `docs/plans/sensor-validation/probe-runs/matriz-C152.md`. O MSFS caiu 3 vezes durante a sondagem (mesmo erro interno; suspeita: `ATC *`/`AI *`); próximo teste com `--filter "^(?!ATC |AI )"`.

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

## Passo 1b — Teste das panes que o addon envia (escrita)
- O SimvarWatcher não envia eventos: a lista abaixo é **só o que o addon implementa hoje** (`SimFailureActuator` / `SimConnectService`):
  - eventos: `TOGGLE_ENGINE1_FAILURE`, `TOGGLE_ELECTRICAL_FAILURE`, `TOGGLE_VACUUM_FAILURE`, `TOGGLE_PITOT_BLOCKAGE`, `TOGGLE_STATIC_PORT_BLOCKAGE`, `TOGGLE_HYDRAULIC_FAILURE`, `TOGGLE_TOTAL/LEFT/RIGHT_BRAKE_FAILURE`, `ELECTRICAL_BUS_TO_ALTERNATOR_CONNECTION_TOGGLE` (bus, alt; `TransmitClientEvent_EX1`), `MAGNETO1_OFF`;
  - SimVars escritas: `ENG ON FIRE:1`, `GENERAL ENG OIL LEAKED PERCENT:1`, `RECIP ENG LEFT/RIGHT MAGNETO:1`, `FLAPS HANDLE INDEX`, `TRAILING EDGE FLAPS LEFT/RIGHT PERCENT`.
- Extrair da documentação do SDK 2024 a lista completa de **Key Events** de pane e falha (ex.: `TOGGLE_ENGINE2..4_FAILURE`, freios, sistemas), no mesmo formato da `simvars-msfs2024.csv`, marcando os que o addon já usa.
- Novo modo da ferramenta (`FlightCareer.SimVarProbe panes`, tarefa separada da leitura): para cada pane, (1) o sim aceita o evento/SimVar, (2) a SimVar de confirmação muda (ex.: `ENG FAILED:1`, `PARTIAL PANEL VACUUM`, `ALTERNATOR CONNECTION ON:1`), (3) quanto tempo ela se mantém sem reaplicar, (4) desfazer volta ao normal (prova que toggle duas vezes repara). Efeitos só visuais (superfície do flap, ponteiro, fogo) com pergunta s/n no console.
- Segurança: só no solo, parado e com freio de estacionamento; pergunta antes de cada pane; desfaz cada uma; no fim e no Ctrl+C confere que nada ficou ativo. Saída `panes-report.csv` (aceita / aplicou / manteve / desfez / segundos).
- Não prova o fluxo do addon em voo (`PersistentFailureService`, modo vigia): isso continua nas pendências de teste no MSFS (F4.2e).

## Passo 2 — Matriz variável × avião
- Script (em `tools/`, só biblioteca padrão, com teste) que junta os `report.csv` numa tabela: aceita e varia / aceita e parada / rejeitada, por avião.
- Resultado na `auditoria-registro.md` e numa coluna `validado` da `simvars-msfs2024.csv`.
- A partir daqui, nenhuma variável entra no registro sem prova da matriz.

## Passo 3 — Limpar e corrigir o registro atual (Gemini) — [CONCLUÍDO em 2026-11-16, tarefa REGRAS-C152]
- Variáveis que alimentam regra e nunca mudaram nos logs do C152/C172: `ENG VIBRATION`, `GEAR SKIDDING FACTOR`, `MIN G FORCE` mantidas no registro para continuar gravação em outros modelos; regras de desgaste desacopladas delas (`airframe_over_g_negative` usa `af-g-force`).
- `CONTACT POINT COMPRESSION` / `CONTACT POINT IS SKIDDING`: substituídos por chaves explícitas por ponto de contato (`gear-contact-point-compression-0..2`, `gear-contact-point-is-skidding-0..2`, `gear-contact-point-on-ground-0..2`) com `perEngine: false`.
- `ELECTRICAL GENERATOR AMPS/VOLTAGE/ACTIVE` adicionados com `perEngine: true`. Chaves legadas `elec-genalt-*` mantidas.
- Variáveis novas aprovadas (`af-dynamic-pressure`) adicionadas ao registro.

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

## Passo 5 — Bugs do fuzz (precisa de autorização do usuário) — [CONCLUÍDO em 2026-11-16, tarefa REGRAS-C152]
Resolvidos bugs 1–4 de `docs/reviews/FUZZ-revisao.md` (rodada 2):
- `PartWearEngine.cs:176`: suporte a limites negativos na direção Below (`airframe_over_g_negative` operacional e testada).
- `egtMax`: padrão de classe adicionado em `_class_defaults.json` › `Piston` (893.52 °C).
- Gerador e deriver: escala do trem de pouso ajustada para percentual (0–100), `isUp <= 10.0` e `isDown >= 90.0`, duração de abusos contínuos garantida e sincronismo de voo controle em cancelamentos.
- Gerador sintético atualizado com sensores novos (pontos de contato por roda, gerador e pressão dinâmica).

## Passo 6 — Prova no simulador
- Voo normal com REC ligado → nova fixture de replay.
- Voos de abuso de propósito (comando pleno acima da Va, pouso acima do peso, flap acima da Vfe, trem acima da Vle num retrátil) para confirmar que cada regra dispara.
- Fecha também as pendências antigas de teste no MSFS (`docs/agents/HANDOFF.md`).

## Prioridade
O passo 4 é o que dá mais retorno; os passos 1 a 3 garantem que ele não seja construído sobre variáveis que não funcionam.
