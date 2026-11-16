# Revisão REGRAS-C152 — rodada 1

**Veredito: CORRIGIR**

Conferido pelo revisor: build 0 erros; `dotnet test` 463/463. Aprovados sem ressalva: registro por roda (`-0..-2`), gerador 2024, `af-dynamic-pressure`, regras e catálogo, `egtMax` de classe, `isUp/isDown` em percent, fallback de solo por roda, limite negativo em `Below` (incluindo a ordenação por `limit × threshold`), `InternalsVisibleTo` para o teste de `DetectEngineCount`.

## Correções
1. **Fuzz de 1000 voos não foi rodado (critério de aceite).** O revisor rodou `fuzz --runs 1000 --seed 777` nesta branch: **986 aprovados / 14 falhas, 17 violações de gabarito** (artefatos em `%LocalAppData%\FlightCareer\fuzz-failures\`). Exemplos: `PositiveGExceeded` não disparou `airframe_over_g_positive` (4), `NegativeGExceeded` não disparou `airframe_over_g_negative` (2+), `VneExceeded`, `ThermalShock`, `HardLanding`, `BatteryDischarging` sem disparo, e "perda com abuso não estritamente maior que no controle" (válvulas, cilindros, bateria, asa, flaps). Reproduza cada falha (`--seed` dos artefatos), classifique **produção × gerador × gabarito** e corrija o que for gerador/gabarito. Bug de produção: não corrija sem descrever no relatório (arquivo, linha, causa) para decisão do revisor. Cole o resumo do fuzz 1000 final no relatório.
2. **`SyntheticFlightPlan` passou a esconder lacunas do catálogo.** Os abusos `VneExceeded`, `FlapOverSpeed`, `BatteryDischarging`, `ElectricalOverload` e `AvionicsMasterOnStart` agora só entram se a aeronave tiver o limite. Só 20/46 aeronaves têm `vne`, 24/46 têm `vfe` e 2/46 têm `elecMainBusIndex`: nas outras a regra nunca dispara **em produção**, e é isso que o fuzz deve mostrar. Volte a sortear esses abusos em todas as aeronaves; quando faltar o limite, o oráculo registra **"limite ausente: <chave>"** numa categoria própria do resumo (não conta como aprovado nem como violação de gabarito). No relatório, liste as aeronaves sem `vne`, `vfe`, `elecMainBusIndex`, `elecAvionicsCircuitIndex`, `alternatorRatedAmps`.
3. **Tempos fixos no oráculo (600 s / 300 s).** A spec pedia ler os valores reais. Obtenha o mínimo das regras/deriver (`batteryDeepDischargeMinutes` e a métrica de sobrecarga em `wear_rules.json`, primeiro `threshold` de banda, convertido para segundos) em vez de números no código.
4. **Mudança fora do escopo em `PartWearEngine` (resolução de sensores):** a prioridade passou de chave exata para `chave:1`. Isso só muda algo quando o frame tem as duas chaves, o que o layout real não produz. Reverta para a ordem original, a menos que haja um teste que falhe sem a mudança; nesse caso, explique no relatório qual frame real gera as duas chaves.

## Fora do escopo (continua)
- Bug 5 do fuzz (`landingCycles`) e preencher limites que faltam no catálogo: decisão do usuário.
- Não mexer nos arquivos quentes de SimConnect.

---

# Revisão REGRAS-C152 — rodada 2

**Veredito: APROVADO** (com ressalvas abaixo, sem nova rodada)

Conferido pelo revisor: build 0 erros; `dotnet test` 463/463; `fuzz --runs 1000 --seed 777`: 847 aprovados, 153 com limite ausente (categoria própria), **0 falhas**.

- Item 1: as 14 falhas eram do gerador/plano (colisão de abusos no mesmo instante, cancelamento antes do toque, choque térmico desfeito no cruzeiro, EGT abaixo do limite). Classificação conferida por amostragem.
- Item 2: abusos voltaram a ser sorteados em todas as aeronaves; limite ausente aparece por abuso, sem esconder os outros abusos do voo. Lista de aeronaves sem limite no relatório r2.
- Item 3: tempos mínimos lidos do `wear_rules.json`.
- Item 4: mudança mantida com justificativa real — `PartFlightTracker.DecorateSensorFrame` grava `eng-cht-cooling-rate` e `eng-cht-cooling-rate:1` no mesmo frame; na ordem antiga o choque térmico era avaliado como regra global (efeito em produção, agora por motor).

## Ressalvas (para tarefa futura, não bloqueiam)
1. Tolerância do gabarito por voo mais curto usa `0,05 %/h` fixo (`SyntheticFlightOracle`); trocar pela taxa natural real da peça.
2. `FlapOverSpeed` em avião sem flap (CAP-4, J-3 Cub) conta como "limite ausente: vfe"; o oráculo deveria pular pelo recurso da aeronave.
3. Os limites estimados de Vne/Vfe/alternador (merge `data/limites-estimados`) reduzem os "limite ausente"; rodar o fuzz de novo depois do merge.
