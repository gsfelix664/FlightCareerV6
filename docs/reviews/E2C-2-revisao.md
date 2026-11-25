# Revisão E2C-2 — trabalho aéreo e ronda de saúde

**Veredito: APROVADO** (Claude, 2026-11-25). Branch `feat/e2c-2-trabalho-aereo`, commit do Gemini `eae6e4d`, sem ajustes do revisor.

## Conferido
- `AerialWorkTracker` (lido inteiro): amostra válida = no ar, faixa relativa a `RefElevationFt`, IAS ≤ limite; setores em qualquer ordem, traçado na ordem com desvio lateral, órbita somando o `simDt` simulado, busca com alvo em qualquer momento e pernas só para progresso. Motivos de falha em português.
- `FlightMonitor` (arquivo quente, diff linha a linha): só campo, `Reset`, configuração por `generator` com `try/catch` registrado, `Update` com o mesmo `simDt` dos outros rastreadores (sem relógio próprio) e repasse ao HUD e ao `Evaluate`. Início/fim do voo, combustível, slew, toque e crash intactos. `round_trip` entrou no ramo do `MultiStopTracker`.
- `MissionCompletion`: ramos novos; chamadas antigas sem `trackers` inalteradas.
- Mapa: SAR mostra o datum e o padrão, nunca o alvo. HUD com as linhas da spec.
- Geradores: inspeção com rumo ±25°, alvo da SAR numa perna ≥ 2 deslocado ≤ 0,15 NM, 10 tentativas e `null` fora da faixa. `board-report --boards 50`: patrulha 39–154 NM, inspeção 74–200, monitoramento 25–40, SAR 51–243, saúde 105–500 (ida e volta de 50–250); pax e carga dentro dos perfis.
- Snapshot: 246/280 registros mudaram (efeito de roleta ao ligar 5 tipos).
- Build (revisor): 0 erros. `dotnet test`: 1.118/1.118.
- `agy-audit`: 17 itens para conferir, todos scripts Python de leitura dentro do worktree e regravação do snapshot; nada fora do projeto.

## Pendências menores (não bloqueiam)
- Saúde (`saude_familia`) com `RequireNoRefuel = false`: pode abastecer no destino (decisão da spec).
- Teto "acima do solo" relativo à elevação da origem: em terreno mais alto que a origem a faixa fica apertada. Resolver com `PLANE ALT ABOVE GROUND` quando der para conferir no MSFS.

Não testado no MSFS (todo o comportamento em voo).
