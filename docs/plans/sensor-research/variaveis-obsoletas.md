# SimVars obsoletas no SDK do MSFS 2024

Criado em 2026-11-16 (Claude). A documentação marca o nome das variáveis obsoletas com `<span class=obsolete>` (aparece riscado/esmaecido na página). O extrator `tools/simvar_probe/scrape_simvars.py` **não capturava** essa marca, então a `simvars-msfs2024.csv` e as análises `variaveis-*.md` não a mostravam. Levantamento feito nas 15 páginas do extrator (mesmos links `msfs2024/retail/...` que o usuário usa): **70 obsoletas**.

Regra: **obsoleta não entra no registro, no contexto nem nas listas do Watcher**, mesmo que o sim ainda aceite o nome ou o valor varie. Também vale para variáveis cuja descrição diz "No longer used", "Deprecated, do not use" ou "legacy … use X instead".

## 1. Obsoletas que o projeto usa ou citou

| Variável | Onde estava | Ação |
|---|---|---|
| **`CRASH FLAG`** | `sensor_registry.json`, `SimConnectService` (`AddFailureDef`), `PersistentFailureService` (pane nativa de acidente, borda em `CrashFlag`) | **Código em produção.** Trocar a detecção de acidente pelo evento de sistema `Crashed` do SimConnect (já citado no `TouchdownDetector`) + detecção própria (toque, G). Tarefa do Gemini com spec; arquivos quentes. Até lá, manter (o sim ainda responde). |
| **`CRASH SEQUENCE`** | lista do Watcher; `variaveis-diversos.md` | Removida do Watcher. Perda total por acidente (F4.2b) deve usar o evento `Crashed`. |
| `REALISM CRASH DETECTION` / `WITH OTHERS` | Watcher | **Não** estão marcadas como obsoletas: continuam (integridade). |
| `STRUCT DAMAGEVISIBLE` | Watcher; `variaveis-diversos.md` | Removida ("No longer used by the simulation"). |
| `SEMIBODY LOADFACTOR X/Z` | Watcher de desgaste; `validacao-simvars.md` › passo 4; `HANDOFF.md` (mortas no C152) | Removidas. Explica por que ficaram 0 no C152. Carga lateral: usar `ACCELERATION BODY X` (já no registro) e `G FORCE`. |
| `EXIT OPEN` (e `EXIT POSX/Y/Z`, `EXIT TYPE`) | Watcher (top 25 e desgaste) | Removida. Portas: procurar a variável atual na página de sistemas antes de usar. |
| `COWL FLAPS` | `variaveis-motor.md` | Usar só `RECIP ENG COWL FLAP POSITION` (já no registro). |
| `ENGINE PRIMER`, `ENGINE MIXURE AVAILABLE`, `ENG RPM SCALER`, `OIL AMOUNT`, `PROPELLER ADVANCED SELECTION` | `variaveis-motor.md` | Não usar. Primer: `RECIP ENG PRIMER` (já no registro). |
| `GENERAL ENG FIRE DETECTED` | **`sensor_registry.json`** | Descrição: "No longer used in the simulation". Tirar do registro e usar `ENG ON FIRE` (já lido). Junto com a tarefa do `CRASH FLAG`. |
| `ELEVATOR TRIM PCT` | Watcher; `variaveis-comandos.md` | Legada: trocada por `ELEVATOR TRIM PCT EX1` no Watcher. |
| `FLAP POSITION SET`, `YOKE X INIDICATOR` | `variaveis-comandos.md` (não usar) | Confirmado: não usar. |
| `AI *` (16) | `variaveis-diversos.md` (não usar) | Todas obsoletas — reforça a suspeita das quedas do MSFS ao ler `AI *`. |
| `CONTROLLABLE`, `IDLE ANIMATION ID`, `STRUC/STRUCT * PID/PBH32/REALISM VARS/BODY ROTATION ACCELERATION`, munição | `variaveis-diversos.md` (não usar) | Confirmado. |
| `CIRCUIT STANDY VACUUM ON` | `variaveis-eletrica.md` (`CIRCUIT STANDBY VACUUM ON`) | Grafia antiga obsoleta; conferir a grafia atual antes de usar. |
| `STROBE FLASH`, `GPS HSI NEEDLE`, `CG MAX/MIN MACH`, `FLY ASSISTANT *` | — | Não usar (luzes: ver a página de luzes quando for analisada). |

## 2. Lista completa (70)
`AI CURRENT WAYPOINT`, `AI DESIRED HEADING`, `AI DESIRED SPEED`, `AI GROUNDCRUISESPEED`, `AI GROUNDTURNSPEED`, `AI GROUNDTURNTIME`, `AI TRAFFIC ASSIGNED PARKING`, `AI TRAFFIC ASSIGNED RUNWAY`, `AI TRAFFIC CURRENT AIRPORT`, `AI TRAFFIC ETA`, `AI TRAFFIC ETD`, `AI TRAFFIC FROMAIRPORT`, `AI TRAFFIC ISIFR`, `AI TRAFFIC STATE`, `AI TRAFFIC TOAIRPORT`, `AI WAYPOINT LIST`, `BOMB AMMO`, `CANNON AMMO`, `CG MAX MACH`, `CG MIN MACH`, `CIRCUIT STANDY VACUUM ON`, `CONTROLLABLE`, `COWL FLAPS`, `CRASH FLAG`, `CRASH SEQUENCE`, `ENG RPM SCALER`, `ENGINE MIXURE AVAILABLE`, `ENGINE PRIMER`, `EXIT OPEN`, `EXIT POSX`, `EXIT POSY`, `EXIT POSZ`, `EXIT TYPE`, `FLAP POSITION SET`, `FLY ASSISTANT *` (20), `GPS HSI NEEDLE`, `GUN AMMO`, `IDLE ANIMATION ID`, `OIL AMOUNT`, `PROPELLER ADVANCED SELECTION`, `ROCKET AMMO`, `SEMIBODY LOADFACTOR X`, `SEMIBODY LOADFACTOR Z`, `STROBE FLASH`, `STRUC AIRSPEED HOLD PID CONSTS`, `STRUC HEADING HOLD PID CONSTS`, `STRUCT BODY ROTATION ACCELERATION`, `STRUCT DAMAGEVISIBLE`, `STRUCT PBH32`, `STRUCT REALISM VARS`, `YOKE X INIDICATOR`.

## 3. Próximos passos
1. **Extrator:** acrescentar a coluna `obsoleta` na `simvars-msfs2024.csv` (detectar `class=obsolete` e as frases "Deprecated"/"No longer used") e um teste que falhe se o `sensor_registry.json`/`context_registry.json` tiver obsoleta. Tarefa do Gemini.
2. **Produção:** trocar `CRASH FLAG` pelo evento `Crashed` e tirar `GENERAL ENG FIRE DETECTED` do registro (arquivos quentes; spec passo a passo).
