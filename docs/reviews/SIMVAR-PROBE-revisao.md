# Revisão SIMVAR-PROBE — APROVADO

**Revisor:** Claude · **Data:** 2026-11-16 · **Branch:** `feat/simvar-probe`

## Verificado
- `dotnet build src/FlightCareer.slnx`: 0 erros. `dotnet test src/FlightCareer.Tests`: 455/455 (15 novos).
- Leitura linha a linha de `ProbeSession.cs` (fases A e B, handlers, gravação) e da camada de domínio pelos testes.
- **Rodado pelo revisor no MSFS 2024 (C152 no pátio, 10 s):** 4.750 leituras; 94 rejeitadas (`NAME_UNRECOGNIZED`), 4.552 aceitas e constantes, 104 aceitas e variando, 0 sem resposta; 23 blocos numéricos + 133 strings.
- **Sem deslocamento entre blocos:** constantes conhecidas conferem nos blocos 0 a 17 (`NUMBER OF ENGINES`=1, `RECIP ENG NUM CYLINDERS:1`=4, `IS GEAR WHEELS`=4, `FLAPS NUM HANDLE POSITIONS`=3, `NEW FUEL SYSTEM`=1, `CARB HEAT AVAILABLE`=1, `SURFACE TYPE`=4, `SIM ON GROUND`=1).
- Somente leitura: nenhuma chamada de `SetDataOnSimObject` nem de evento.
- Auditoria do Gemini: só leu as pastas de saída em `%LocalAppData%\FlightCareer\probe-logs` (dentro do esperado).

## Ajustes feitos pelo revisor
1. Três `catch {}` silenciosos (limpeza da fase A, leitura do teclado, `Dispose`) passam a registrar a mensagem.
2. `--block-size` era ignorado na fase B (fixo em 200): agora vale, limitado a 1–200 (tamanho do struct).

## Observações (não bloqueiam)
- O relatório do executor dizia "4.656 aceitas"; as pastas que ele deixou só tinham rodadas filtradas (5 variáveis). A rodada completa foi feita pelo revisor.
- Alguns campos aceitos devolvem valores denormais (ex. `CAMERA GAMEPLAY PITCH YAW:2` = 3,9e-265, autopilot de velocidade no C152): é lixo do próprio sim para índice ou sistema inexistente, não deslocamento. Entram como "constante"; a matriz do plano deve tratá-los como não validados.
- Os IDs de pedido da fase A são reutilizados entre lotes; uma resposta atrasada de um lote poderia ser atribuída ao lote seguinte. Na rodada real não houve nenhum "sem resposta", então o risco é baixo.
- O CSV grava o último valor recebido de cada bloco a cada 1 s (não espera todos os blocos do ciclo).
