# Revisão PAIS-A (Claude, 2026-11-24)

**Veredito: APROVADO** (com 1 ajuste feito pelo revisor).

## Conferido
- `CountryLocator` (filtro por retângulo + ponto no polígono com buracos) e `AirportSpatialGrid` (grade de 1° para o vizinho a até 50 NM): corretos; carga sem `catch {}` silencioso.
- `DataSeeder.ResolveMissingCountriesAsync` nos dois caminhos (Little Navmap e só-csv), em lote numa transação, com log.
- Build 0 erros; 1.008 testes passam (rodado pelo revisor).

## Ajuste feito pelo revisor
1. **Regra de país confiável:** o passo tratava como válido só o ISO presente nos contornos do Natural Earth, que junta Guiana Francesa, Guadalupe, Reunião etc. com a França — esses aeroportos (país certo pelo OurAirports) virariam FR; e aeroportos sem csv cujo país do simulador (região ICAO) coincide com um ISO de outro país (ex.: SK = Colômbia no ICAO, Eslováquia no ISO) passariam sem correção. Agora: **com csv batendo (`CsvType`), vale o csv; sem csv, vale sempre o contorno** (depois o vizinho). Mesma regra no `TierReporter`. Teste de Caiena (GF) acrescentado.
- `tier-report` (revisor): 15.353 resolvidos por contorno, 202 por vizinho, 32 sem país; SOCA = GF, TFFR = GP, FMEE = RE, SKBO = CO, 32PA = US, SBOK = BR.
