# Revisão FIX-REPLAY-BUGS

**Veredito: APROVADO** (com um ajuste do revisor)

- Conferido pelo revisor: build 0 erros; `dotnet test` **327/327** e Python **48/48** (o relatório do executor dizia 123/19 por engano de contagem).
- Replay do voo do C152 (`GameLike`): sem `electricalOverloadMinutes`, sem `GENERATOR_OVERHEAT_INSPECTION`, sem `LOW_OIL_PRESSURE_INSPECTION`, sem choque térmico com o motor frio; horas 1,105 h (banco 1,086 h); excedência de RPM mantida (pico 2.606 / limite 2.550).
- Sobrecarga: alternador ligado + combustão + sem starter + carga > nominal + tensão caída em [0,88; 0,95) × tensão do sistema. `requiresEngineRunning`/`engineRunningDelaySeconds` em `oil_pressure_low` (30 s) e `piston_shock_cooling` (60 s). Choque térmico pela taxa `eng-cht-cooling-rate:n` (°C/min) contra `chtCoolingRateMax`.

## Ajuste do revisor
1. **Citação errada:** a fonte do `chtCoolingRateMax` citava a Lycoming SI 1094D, que trata de **empobrecimento da mistura**, não de choque térmico. Trocada por "estimativa — revisar" (orientação comum de ~50 °F/min, documento do fabricante não confirmado) no `_class_defaults.json` e no `wear_rules.json`.

## Achados novos (próxima tarefa)
1. **Partida do motor nunca é contada no jogo:** no modo `GameLike` sumiram `engineStartCycle` e `starterSeconds`. O piloto sempre dá a partida **antes** da ativação do voo (motor ligado + freio solto por 10 s), e o `FlightMonitor` só processa as peças depois da ativação. Logo, `starterSeconds`, `engineStartCycle` e `avionicsMasterStartSpikes` nunca disparam em voo real. Os eventos derivados precisam ser observados desde o `StartMonitoring` (fase PREPARAÇÃO), sem desgaste contínuo antes da ativação; o replay `GameLike` deve imitar isso.
2. `piston_egt_excess` aparece como excedência com pico abaixo do limite (849 / 893) e perda 0: faixa de cautela sem efeito poluindo o relatório. Avaliar.
