# PAIS-A — País do aeródromo pelos contornos dos países

**Branch:** feat/pais-contorno · **Commit:** 3e5f229 · **Data:** 2026-11-24

## Plano (antes de codar)
- Criar `FlightCareer.Domain.Geo.CountryLocator` com carregamento do `country_borders.json` (construtor texto/stream e `Shared` singleton com fallback de caminhos sem catch silencioso), `KnownIsoCodes`, e método `Locate(lat, lon)` via bounding box e ray casting (anel externo menos buracos). Criar `AirportSpatialGrid` para busca de vizinho válido mais próximo a até 50 NM em grade de 1°.
- Adicionar no `DataSeeder` o método `ResolveMissingCountriesAsync` chamado nos dois caminhos de seed (Little Navmap e só-csv) após o enriquecimento por CSV e normalização, resolvendo por contorno, vizinho até 50 NM ou deixando vazio, gravando em lote numa transação com log informativo no `Debug.WriteLine`.
- Integrar a resolução em `TierReporter` (`FlightCareer.Replay`): atualizar a coluna País e gerar estatísticas no relatório markdown (total sem país ≤ 30 e 10 exemplos resolvidos por contorno).
- Implementar testes xUnit cobrindo pontos conhecidos, exclusão de enclaves/buracos (Lesoto em ZA), França/Noruega (ISO_A2_EH) e teste SQLite em memória do `DataSeeder` para Ohio (US), litoral (BR) e oceano (vazio).

## O que foi feito
- `src/FlightCareer.Domain/Geo/CountryLocator.cs`:
  - Implementada classe pura `CountryLocator` que carrega polígonos simplificados de 249 países do `Data/places/country_borders.json`.
  - Método `Locate(lat, lon)` com descarte rápido por bounding box e algoritmo Ray Casting even-odd (`IsPointInRing`) no anel externo testando exclusão contra os buracos/enclaves de cada polígono.
  - Expõe `KnownIsoCodes` (`IReadOnlySet<string>`) e `DistanceNm` (fórmula Haversine).
  - Construtores a partir de string e stream, mais fábrica estática `Shared` com fallback idêntico a `AirportProfileCatalog` (sem `catch {}` vazio, registrando erros em `Debug.WriteLine`).
- `src/FlightCareer.Domain/Geo/AirportSpatialGrid.cs`:
  - Implementada grade espacial esférica de 1° para busca de aeroporto com país válido mais próximo em até 50 NM (`FindNearestCountry`), evitando busca quadrática.
- `src/FlightCareer.Infrastructure/Services/DataSeeder.cs`:
  - Implementado `ResolveMissingCountriesAsync`: lê os aeródromos da tabela `Airports`, identifica os que não possuem código ISO em `KnownIsoCodes`, resolve por contorno e, se fora de todos, pelo vizinho válido mais próximo a até 50 NM (ou vazio se isolado no oceano).
  - Gravação em lote com transação única e mensagem informativa em `Debug.WriteLine`.
  - Integrado ao fluxo de `SeedAsync` nos dois caminhos (após `NormalizeAirportsInDatabaseAsync` no ramo Little Navmap e no ramo só-CSV).
- `src/FlightCareer.Replay/TierReporter.cs`:
  - Integrada a resolução de países na lista de aeródromos classificados.
  - Atualizada a geração do relatório Markdown (`WriteMarkdownReport`) com a seção `## 5. Resolução de Países (Natural Earth e Vizinhos)` exibindo as contagens e tabela com 10 exemplos resolvidos por contorno.
- `src/FlightCareer.Tests/Geo/CountryLocatorTests.cs`:
  - Testes cobrindo pontos conhecidos (`CountryLocator_PontosConhecidos`), buracos/enclaves (`CountryLocator_Buraco`), países com ISO_A2_EH (`CountryLocator_FrancaSemIsoA2`) e teste de integração SQLite em memória do `DataSeeder` (`DataSeeder_PaisInvalido_ResolvidoPorContornoOuVizinho`).
- Documentação:
  - Entrada adicionada no fim de `docs/CHANGELOG.md`.
  - Atualizada a seção 7 de `AGENTS.md`.

## Critérios de aceite
- [x] `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` passa — 0 erros no build e 1.008/1.008 testes aprovados.
- [x] `dotnet run --project src/FlightCareer.Replay -- tier-report --out <pasta fora do repositório>`: no relatório, quantos aeródromos ficaram sem país (alvo ≤ 30) e 10 exemplos resolvidos por contorno — Verificado em `$env:TEMP\fc_tier_report_pais\tier-report.md`:
  - Total com país inicial inválido: 7.030
  - Resolvidos por contorno: 6.928
  - Resolvidos por vizinho (<= 50 NM): 89
  - Ficaram sem país: **13** (meta ≤ 30 cumprida com folga)
  - 10 exemplos registrados na seção 5 (UWUO, UWUF, UWUS, UWWT, ZEE5, ZEE6, ZEE7, 32PA, 32OK, 32OI).
- [x] Carga do `country_borders.json` e o passo inteiro em poucos segundos — Tempo total de execução do `tier-report` medido com `Measure-Command`: **5,54 segundos**.
- [x] Nenhum `catch {}` silencioso; nada de saída de relatório dentro do repositório — Saída gerada em `$env:TEMP\fc_tier_report_pais`.
- [x] Entrada no fim do `docs/CHANGELOG.md`; uma linha no `AGENTS.md` › 7 — Atualizados.
- [x] Commit: `feat(aeroportos): pais pela coordenada com os contornos do Natural Earth (PAIS-A)`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1.008 aprovados, 0 falhas (novos: `CountryLocator_PontosConhecidos`, `CountryLocator_Buraco`, `CountryLocator_FrancaSemIsoA2`, `DataSeeder_PaisInvalido_ResolvidoPorContornoOuVizinho`)

## Não testado
- Inicialização do zero do app WPF contra o MSFS em execução.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida impeditiva. A cobertura de 249 países pelo Natural Earth com grade espacial de 1° para vizinhos costeiros/insulares reduziu os aeródromos sem país no simulador para apenas 13 (ilhas oceânicas ultra-isoladas e alto-mar).
