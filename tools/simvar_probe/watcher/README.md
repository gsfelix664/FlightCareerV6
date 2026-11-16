# Listas para o SimvarWatcher

Arquivos `.simvars` (uma SimVar por linha: `NOME[:índice],unidade`) para carregar no SimvarWatcher do SDK e conferir no MSFS antes de uma variável entrar no `sensor_registry.json`.

| Arquivo | Conteúdo | Origem |
|---|---|---|
| `flightcareer-top25.simvars` | Top 25 da pesquisa de variáveis | `docs/plans/sensor-research/variaveis-referencias.md` |
| `flightcareer-candidatas-desgaste.simvars` | candidatas para desgaste por peça | `docs/plans/validacao-simvars.md` › passo 4 |
| `flightcareer-candidatas-hud-qualidade.simvars` | candidatas para HUD, qualidade do voo/passageiros, missões, integridade e desgaste (páginas Miscellaneous, Brake/Landing Gear, Control, Electrics e Engine do SDK) | `variaveis-diversos.md`, `variaveis-freios-trem.md`, `variaveis-comandos.md`, `variaveis-eletrica.md`, `variaveis-motor.md` |

Fluxo: Watcher (o nome é aceito e o valor muda?) → SimVarProbe/REC num voo real (matriz) → registro com teste. Cada nova página analisada acrescenta as úteis ao arquivo de candidatas da área.
Índices de pontos de contato e de trem variam por aeronave: confira no Developer Mode › Wheels.
