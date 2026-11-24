# Revisão E2C-1 — primeiro lote de missões com regra própria + paraquedismo pelos perfis

**Veredito: APROVADO com ajustes do revisor** (Claude, 2026-11-24). Branch `feat/e2c-1-missoes-regra`, commit do Gemini `877432b` + commit de ajustes da revisão.

## Ajustes feitos pelo revisor
1. `FlightMonitor`: os 4 rastreadores novos usavam um relógio próprio (`_lastSimDataTick`, tempo de parede × sim rate) que **não respeita a pausa** (ex.: o voo de teste contaria tempo de rumo com o sim pausado). Movidos para junto do `_skydiveTracker.Update`, usando o mesmo `simDt` do tempo de voo; campo e reset removidos.
2. `MissionCompletion` (contrato STOL): `catch { }` silencioso trocado por `catch (JsonException)` com `Debug.WriteLine` (AGENTS.md › 5).

## Conferido
- Domínio: `MultiStopTracker` (parada completa GS < 3 kt por 5 s a ≤ 1,5 NM, nova decolagem ≥ 5 s, ordem, combustível com tolerância max(1 gal, 2 %) sobre o mínimo desde a decolagem), `StolLandingTracker`, `CheckFlightTracker`, `FerryRestrictionMonitor`, `MissionTrackerResults`. Testes cobrem os cenários da spec.
- `MissionCompletion`: parâmetro opcional `trackers`; chamadas antigas inalteradas. `FlightService` só repassa `trackers`. `MissionBoardCore`: despacho dos geradores novos; paraquedismo recebe o pax do perfil.
- `FlightMonitor` (arquivo quente, lido inteiro no trecho): só campos, `Configure` por contrato no início, `Update` no tick ativo e repasse ao `Evaluate`. Início/fim do voo, combustível, slew, toque e crash intactos.
- Snapshot: 252/280 registros mudaram — efeito esperado da roleta ao ligar 5 tipos (os sorteios seguintes mudam em todo aeroporto onde eles têm peso); nenhuma regra de outro tipo foi alterada no código.
- Build (revisor): 0 erros. `dotnet test`: 1.051/1.051.
- `board-report --boards 50` (só `airports.csv`): pax/carga/distância dos 6 tipos dentro das faixas; paraquedismo 3–20 saltadores e 79–628 lb de equipamento (12–16 kg × pax).

## Pendências menores (não bloqueiam)
- `check_flight` paga sempre por 30 NM (perfil diz local 20–60).
- `pinga_pinga` chega a 442 NM no total (perfil 100–400): a soma das etapas não é limitada ao total.
- Paraquedismo continua com distância fixa de 25 NM (comportamento anterior).
- HUD com o `StatusText` dos rastreadores e trem travado no translado: próximas tarefas.

Não testado no MSFS (todo o comportamento em voo).
