# Revisão FUZZ — rodada 1

**Veredito: CORRIGIR**

Conferido pelo revisor: build 0 erros; `dotnet test` 439/439; Python 48/48; `fuzz --runs 300 --seed 777` 300/300.

**Teste de mutação (o que decidiu o veredito):** o revisor reintroduziu de propósito o bug real de 2026-11-15 (`PartWearEngine` sem resolver `chave:1`, linha ~155) e rodou:
- suíte xUnit de fuzz → **pegou** (2 falhas, ex.: `Fuzz_Gabarito_RpmOverspeed_GeraExcedenciaNoMotorCertoEPerda`);
- CLI `fuzz --runs 100 --seed 777` → **não pegou** (100/100 aprovados). A CLI só verifica invariantes; não roda o gabarito (voo-controle × voo com abuso). Logo, os "1000/1000 aprovados" do relatório não provam nada sobre os abusos.

## Correções
1. **CLI `fuzz` com gabarito:** para cada voo sorteado, gere também o voo-controle (mesma semente, sem abusos) e aplique `SyntheticFlightOracle` de gabarito a **cada abuso** do `GroundTruth` (regra certa disparada, motor certo, perda maior que no controle), além das invariantes. Opção `--fast` para rodar só invariantes.
2. **Gabarito mais forte:** quando a faixa da regra-alvo tem efeito (multiplicador > 1 ou perda > 0), exigir perda **estritamente maior** que no controle (hoje aceita igual), com tolerância documentada.
3. **Horas por peça:** o invariante de horas só confere peças com prefixo `airframe.` ou `eng.`; o prefixo real das peças do motor é `engine.`. Confira todas as peças instaladas (exceto as que o catálogo diz não acumularem horas, se houver).
4. **Prova de que funciona:** acrescente um teste que reproduz a mutação do revisor sem mexer no código de produção (ex.: frame sintético só com chaves sem `:n` ou um `PartWearEngine` com resolução desativada por um ponto de teste interno) e confirma que o gabarito falha; e rode a CLI com gabarito em 1000 voos, colando o resumo no relatório.
5. Dúvida do executor (`landingCycles` só nas 4 peças estruturais): fora do escopo; listar para decisão do usuário.

---

# Revisão FUZZ — rodada 2

**Veredito: APROVADO** (a ferramenta; os bugs que ela achou vão para a próxima tarefa)

- CLI `fuzz` agora roda o gabarito com voo-controle; `--fast` só invariantes. Gabarito exige perda estritamente maior; invariante de horas cobre todas as peças. Teste de mutação `Fuzz_Mutacao_SemResolucaoChavePorMotor_GabaritoDeveFalhar` aprovado (ponto de teste `DisablePerEngineSensorResolution`, desligado por padrão).
- Conferido pelo revisor: build 0 erros; `dotnet test` 440/440; `fuzz --runs 300 --seed 777` com gabarito: 181 aprovados / 119 falhas (261 violações), coerente com o relatório (1000 voos: 644/356).

## Bugs achados — classificação do revisor (conferida no código e no catálogo)
1. **Produção, grave:** `PartWearEngine.cs:176` descarta limite `<= 0`; `gLimitNeg` é sempre negativo (padrão −1,52 pistão/turboélice, −1,0 jato; 38 das 46 aeronaves usam o padrão) → `airframe_over_g_negative` nunca dispara em nenhum avião.
2. **Catálogo, grave:** `egtMax` ausente em 35 das 46 aeronaves e sem padrão de classe em `_class_defaults.json` → `piston_egt_excess` mudo nessas (ex.: Baron G58).
3. **Gerador (não é bug de produção):** `GEAR POSITION`/`GEAR HANDLE POSITION` no registro são `percent` (0–100); o gerador manda 0–1. O teste `isUp = gearPos <= 10 || ...` do `PartEventDeriver` é ambíguo e merece endurecimento, mas funciona com o dado real.
4. **Gerador:** voos curtos demais para os acumuladores (10 min de descarga, 5 min de sobrecarga); o abuso tem de durar o suficiente ou o gabarito não deve esperar o evento.
5. `landingCycles` só nas 4 peças estruturais: decisão do usuário.
