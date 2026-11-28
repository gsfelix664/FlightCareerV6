# CARGA-COERENTE — Tipo de carga coerente com origem/destino e regra de carga perigosa

**Branch:** feat/carga-coerente · **Commit:** pendente · **Data:** 2026-11-25

## Plano (antes de codar)
- Expandir a entidade `Mission` (`CargoKind`, `CargoName`) e registrar migração idempotente em `AppDbContext`.
- Implementar `cargo_kinds.json` em `Infrastructure/Data` com 15 missões de carga, perfis de etiquetas, classes DGR e nomes em português minúsculo, além de expandir `mission_texts.json` para >= 3 variantes por tipo com `{cargo}` e `{cargo_note}`.
- Criar `CargoKindCatalog` e `DangerousGoodsRules` em `Domain/Missions/` com algoritmo estável FNV-1a 32 bits (`StableSeed`), sorteio ponderado por tags de origem/destino e checagem de manifesto (bloqueio de pax em voos Cargo Aircraft Only).
- Integrar os catálogos no gerador padrão (`MissionGenerator` e `MissionBoardCore`) e nos pontos de despacho da UI (`MainViewModel`, `MissionDisplayItem`, `MissionsView.xaml`).
- Escrever testes unitários em `CargoKindCatalogTests`, validar equivalência do golden test (`missions_golden.json`) garantindo que apenas títulos de carga mudem e atualizar changelog e sistema.

## O que foi feito
- `src/FlightCareer.Infrastructure/Data/cargo_kinds.json`: Catálogo criado com 7 classes DGR oficiais, cobrindo as 15 missões de carga da § 4.1 (cada uma com 3 a 7 tipos de carga, nomes minúsculos em português sem empresas reais, e etiquetas validadas contra `airport_profiles.json`).
- `src/FlightCareer.Infrastructure/Data/mission_texts.json`: Expandido para 3 variantes em cada uma das 15 missões de carga, suportando marcadores `{cargo}` e `{cargo_note}` além dos marcadores legados (`{origin}`, `{dest}`, `{cargo_lbs}`, `{pax}`).
- `src/FlightCareer.Domain/Entities/Mission.cs`: Adicionadas propriedades `CargoKind` e `CargoName`.
- `src/FlightCareer.Infrastructure/Persistence/AppDbContext.cs`: Adicionadas instruções `ALTER TABLE Missions ADD COLUMN CargoKind TEXT DEFAULT NULL;` e `CargoName` no método idempotente `EnsureSchemaUpdatedAsync`.
- `src/FlightCareer.Domain/Missions/CargoKindCatalog.cs`: Carregamento do catálogo, FNV-1a 32 bits determinístico (`StableSeed`), e sorteio ponderado por tags combinadas de origem e destino (`Pick`).
- `src/FlightCareer.Domain/Missions/DangerousGoodsRules.cs`: Regras de negócio puras para resolução de classe DGR e checagem de integridade de manifesto (`CheckManifest`) bloqueando passageiros em voos com artigos perigosos "somente cargueiro".
- `src/FlightCareer.Application/Services/MissionGenerator.cs`: Integrado o sorteio estável de tipo de carga no caminho padrão, preenchimento de `CargoKind`/`CargoName`, e montagem dos marcadores `{cargo}` e `{cargo_note}`.
- `src/FlightCareer.Application/Services/MissionBoardCore.cs`: Passagem de `originTags` e `destTags` (via `DestinationTags`) para o gerador no caminho padrão.
- `src/FlightCareer.UI/ViewModels/MainViewModel.cs`: Checagem de manifesto contra carga perigosa em `AcceptSelectedMissionsAsync` (Warning) e em `StartFlight` (Error NO-GO).
- `src/FlightCareer.UI/ViewModels/MissionDisplayItem.cs` e `src/FlightCareer.UI/Views/MissionsView.xaml`: Exibição de `CargoSummary` no painel de pré-despacho, com destaque âmbar `#FFB300` quando for carga perigosa (`IsDangerousGoods`).
- `src/FlightCareer.Tests/Missions/CargoKindCatalogTests.cs`: 13 testes unitários cobrindo carregamento, validação de integridade das classes DGR e tags, determinismo da semente FNV-1a, favorecimento por etiqueta de destino, regras de bloqueio de manifesto e integração no gerador.
- `docs/sistemas/missoes-quadro.md` e `docs/CHANGELOG.md`: Documentação e histórico atualizados.

## Critérios de aceite
- [x] 1. `dotnet build src/FlightCareer.slnx` 0 erros; `dotnet test src/FlightCareer.Tests` 100 % (1134/1134 testes aprovados).
- [x] 2. Golden `missions_golden.json` validado com script Python comparando com a versão original no scratchpad. Apenas o campo `Title` sofreu alterações (59 registros), e estritamente nas 15 missões de carga da § 4.1. Zero divergências em qualquer outro campo (o consumo do `Random` do gerador foi rigorosamente preservado).
- [x] 3. Não foram rodadas simulações desnecessárias (`career`, `career-stab`, `fuzz`).
- [x] 4. Declaração explícita de itens não testados no simulador / interface gráfica (ver abaixo).
- [x] 5. Atualização em `docs/CHANGELOG.md` e linha dedicada em `docs/sistemas/missoes-quadro.md`. `missoes-locais-rp.md` preservado intacto.
- [x] 6. Commit único formatado: `feat(missoes): tipo de carga coerente e carga perigosa (CARGA-COERENTE)`.

## Comparação do Golden Snapshot (`missions_golden.json`)
Script executado fora do repositório (`scratch/compare_golden.py`) comparando 280 registros:
```
--- Contagem de registros com diferenca por campo ---
  Seed: 0 diferencas
  OriginIcao: 0 diferencas
  Subtype: 0 diferencas
  DestinationIcao: 0 diferencas
  Pax: 0 diferencas
  CargoLbs: 0 diferencas
  Reward: 0 diferencas
  DistanceNm: 0 diferencas
  Title: 59 diferencas
  RequiredTouchAndGoes: 0 diferencas
  OverflyWaypoints: 0 diferencas
  ContractData: 0 diferencas

Subtipos com diferenca de Title (13):
  - apoio_acampamento (e carga? True)
  - cadeia_fria (e carga? True)
  - carga_dgr (e carga? True)
  - carga_jit (e carga? True)
  - carga_overnight (e carga? True)
  - carga_paletizada (e carga? True)
  - carga_sensivel (e carga? True)
  - combustivel_tambor (e carga? True)
  - ecommerce (e carga? True)
  - humanitario (e carga? True)
  - insumos_agro (e carga? True)
  - malotes (e carga? True)
  - suprimentos_remotos (e carga? True)

SUCESSO: ZERO diferencas em campos nao-Title e ZERO diferencas de Title fora das 15 missoes de carga!
```

## Build e testes
- dotnet build: 0 erros, 34 avisos (avisos pré-existentes de nulidade e métodos obsoletos)
- dotnet test: 1134 aprovados, 0 falhas (13 novos testes em `CargoKindCatalogTests`)

## Não testado
- Renderização visual em runtime WPF do `TextBlock` de carga perigosa em `#FFB300` / `#8B9BAE` no painel de pré-despacho de `MissionsView.xaml`.
- Exibição de `MessageBox` na UI durante os fluxos interativos de aceitação e decolagem no MSFS.

## Dúvidas e decisões para o revisor
- Nenhuma dúvida pendente. Todos os requisitos da especificação e contratos de domínio foram integralmente atendidos.
