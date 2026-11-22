# TOQUE-NORMAL — Revisão (Claude)

**Branch:** `fix/toque-normal` (`68c65f9`, `497810d`) · **Data:** 2026-11-22

## Veredito: **APROVADO**

## Conferido (arquivos quentes linha a linha)
1. `SimConnectService`: `TouchdownNormalVelocity` só no fim de `SimFastFlightState`; `AddFastDef("PLANE TOUCHDOWN NORMAL VELOCITY", "feet per second")` logo depois de `GROUND VELOCITY`, mesma ordem e unidade. Nada mais mudou.
2. `FlightMonitor`: só o argumento novo na construção de `FastFlightSample`.
3. `TouchdownDetector`: `BodyVerticalFps` fora da conta; a velocidade normal só vale se mudar depois do contato em relação à última amostra antes dele (a SimVar guarda o último toque); reserva pela `VERTICAL SPEED`. Igual à spec.
4. Teste antigo `BodyVerticalSpeed_IsUsedWhenLarger` substituído (exigia o comportamento errado); 5 testes novos no detector e 1 de contagem da leitura rápida.
5. Build 0 erros; `dotnet test` 744/744 (rodado pelo revisor). `agy-audit`: 39 ações, 0 para conferir.

## Não testado
- No MSFS. Conferir no próximo voo: o TOQUE do blackbox deve bater com `gear-plane-touchdown-normal-velocity` × 60 do REC (no voo de 2026-11-22 o certo seria ~100 fpm).
