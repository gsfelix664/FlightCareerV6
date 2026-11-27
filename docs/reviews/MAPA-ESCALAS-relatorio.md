# MAPA-ESCALAS — Escalas e linha do contrato nos três mapas

**Branch:** fix/mapa-escalas · **Data:** 2026-11-27

## Plano (antes de codar)
- Expandir `ContractMarkerKind` com `Stop` e introduzir `ContractRouteLine` e `ContractMapVisuals.Routes` no Domain.
- Atualizar `ContractMarkerBuilder.Build(missions, airportByIcao)` para gerar marcadores de escala numerados (`"{n} · {ICAO}"`, `RadiusNm = 0`), marcador de pista STOL (`"1 · {StripIcao}"`), deduplicação de marcadores idênticos e traçado de `ContractRouteLine` ligando origem, pontos de reporte VFR (escalas), escalas / pista STOL e destino, omitindo vértices nulos/inválidos e deduplicando vértices consecutivos.
- Em `FlightMapControl.xaml.cs`: implementar `ResolveAirport(icao)` com regras de âncora de origem/destino e isolamento de aeronave em solo vs voo, `RefreshContractAnchors()` para evitar reenquadramentos contínuos por telemetria mantendo a âncora obtida pela posição do avião enquanto `ContractMissions` e `OriginIcao` não mudarem, renderização de `ContractRouteLine` com `GMapRoute` (traço `#B388FF`, espessura 1.8, tracejado {2,2}, opacidade 0.9, `ZIndex = 90`) e marcadores `Stop` (`CreatePinBadge(label, "#B388FF")`, `ZIndex = 121`).
- Adicionar testes de unidade em `ContractMarkerBuilderTests` cobrindo triângulo, pinga-pinga, fallback sem resolver, pulo de coordenadas 0,0 mantendo numeração, STOL e deduplicação de missões iguais.
- Atualizar documentação dos sistemas (`missoes-quadro.md`, `hud.md`), plano `correcoes-voo-forcado-sdco.md` e `docs/CHANGELOG.md`.

## O que foi feito
- `src/FlightCareer.Domain/Missions/MapContractMarker.cs`: adicionado `ContractMarkerKind.Stop` no fim do enum e definido o record `ContractRouteLine(IReadOnlyList<GeoPoint> Points)`.
- `src/FlightCareer.Domain/Missions/ContractMarkerBuilder.cs`: adicionada propriedade `Routes` a `ContractMapVisuals`; assinatura atualizada para `Build(IEnumerable<Mission>? missions, Func<string, GeoPoint?>? airportByIcao = null)`; mapeamento de escalas de `BriefingLegPlanner.ReadStops` para marcadores `Stop` com numeração `"{n} · {ICAO}"` e pulo de coordenadas (0,0); detecção de pista STOL em `ContractData` com marcador `Stop` `"1 · {StripIcao}"`; deduplicação de marcadores idênticos (label, lat, lon, kind); traçado de `ContractRouteLine` unindo origem resolvida, pontos overfly por coordenada, escalas/STOL e destino resolvido com omissão de vértices nulos e remoção de pontos consecutivos duplicados.
- `src/FlightCareer.UI/Utils/FlightMapControl.xaml.cs`: método privado `ResolveAirport(string icao)` resolvendo origem via `OriginLat/OriginLon` ou `AircraftLat/AircraftLon` quando `IsOnGround` e destino via `DestLat/DestLon`; método `RefreshContractAnchors()` evitando reconstruções e reenquadramentos contínuos de telemetria; atualização de callbacks de dependência preservando a sequência rota-depois-contrato; renderização de `ContractRouteLine` via arcos Great-Circle em `GMapRoute` com estilo `#B388FF`, tracejado {2,2}, espessura 1.8 e ZIndex 90; renderização de marcadores `Stop` com `CreatePinBadge(label, "#B388FF")` e ZIndex 121; retorno antecipado ajustado para considerar `Routes.Count == 0`.
- `src/FlightCareer.Tests/Missions/ContractMarkerBuilderTests.cs`: adicionados 6 testes unitários cobrindo todos os cenários solicitados (triângulo fechado com 5 vértices, pinga-pinga finalizando no destino, sem resolver aeroportos com 2 vs 1 escalas, escala com coordenadas (0,0) mantendo numeração, STOL com ida e volta em 3 vértices e deduplicação de marcadores de missões idênticas).
- Documentação e plano: atualizados `docs/sistemas/missoes-quadro.md`, `docs/sistemas/hud.md`, `docs/plans/correcoes-voo-forcado-sdco.md` (marcado B2 e item 5) e `docs/CHANGELOG.md`.

## Critérios de aceite
- [x] 1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1.283/1.283 aprovados).
- [x] 2. `missions_golden.json` **não muda** (intocado).
- [x] 3. Relatório declara: não conferido com o app aberto nem no MSFS.
- [x] 4. `docs/CHANGELOG.md`, `docs/sistemas/missoes-quadro.md`, `docs/sistemas/hud.md` e `docs/plans/correcoes-voo-forcado-sdco.md` atualizados.
- [x] 5. Um commit: `fix(mapa): escalas e linha do contrato nos mapas (MAPA-ESCALAS)`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos.
- dotnet test: 1.283 aprovados, 0 falhas (6 testes novos em `ContractMarkerBuilderTests`).

## Não testado
- Não conferido visualmente com o aplicativo aberto nem em execução no Microsoft Flight Simulator (MSFS).

## Dúvidas e decisões para o revisor
- Nenhuma.
