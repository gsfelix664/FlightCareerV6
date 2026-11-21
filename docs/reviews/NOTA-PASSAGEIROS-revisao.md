# NOTA-PASSAGEIROS — Revisão (Claude)

**Branch:** `fix/nota-passageiros-tempo` · **Commits:** `952e80b`, `66f373e` + ajuste do revisor · **Data:** 2026-11-16

## Veredito: APROVADO (com 2 ajustes feitos pelo revisor)

## Conferido
- `FlightQualityService.ProcessSample(sample, isPax, simSeconds)`: segundos simulados por categoria (G, bank, pitch, solavanco, derrapagem); pausa (`simSeconds` 0 ou `MotionPaused`) e solo ignorados; carga só conta G e solavanco; turbulência (vento vertical ≥ 5 ft/s) atenua G/solavanco a 25 %; sensores opcionais `null` = comportamento antigo.
- Pitch: `PitchUpDeg = -PlanePitch` (MSFS negativo = nariz para cima) — corrige a inversão do código antigo. **Conferir com captura no simulador.**
- Escala mantida: 0,1 ponto/s passageiro, 0,05 carga (igual ao legado a 1 Hz e 1×). Toque e `GetRating()` inalterados.
- `FlightMonitor.cs`: só a chamada (2 linhas), usando `dt`/`isPaused` já existentes e `SimTimeStep.ToSimSeconds`.
- `dotnet build` 0 erros; `dotnet test` **538/538** (12 novos do executor + 1 do revisor).

## Ajustes feitos pelo revisor
1. A nota usava o tipo de voo (passageiro/carga) do **último** pacote para todos os segundos acumulados; agora `ComfortPenaltyPoints` soma a cada amostra com o peso do momento. Teste `MixedFlight_PaxThenCargo_EachSecondWeightedByItsType`.
2. O comentário de `FlightQualityRules` atribuía os limites a um "envelope de conforto da FAA/IATA" sem fonte; corrigido para "valores herdados do código antigo; calibrar com voos reais".

## Não testado no MSFS
Sinal do pitch, frequência real dos pacotes, pausa pelo menu (o `MOTION SIMULATION` ainda não é lido).
