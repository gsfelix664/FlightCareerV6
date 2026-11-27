# SEM-PORCENTO — nenhum aviso/tela mostra % de condição

**Branch:** fix/sem-porcento · **Commit:** (commit pendente nesta branch) · **Data:** 2026-11-27

## Plano (antes de codar)
- Criar `HealthWords.cs` no Domain com mapeamento de faixas de saúde para palavras (masculino e feminino).
- F1: Converter `HealthRange.ToString()` para palavras sem números, atualizar avisos de risco/atenção em `ComponentMaintenance.cs`, `HealthText` em `PartHangarView.cs`, motivos em `PartInspectionFlags.cs` e `PartReplacementPlanner.cs`, legendas e tooltips em `HangarView.xaml`, mensagens de revisão e overhaul em `HangarViewModel.cs` e `PilotAircraftWrapper.cs`.
- F2: Remover % de condição/saúde em descrições de panes e danos em `ComponentFlightSession.cs`, extinção de fogo em `SimFailureActuator.cs`, e causa de panes nativas em `PersistentFailureService.cs`.
- F3: Adicionar helper de estado/cores de mercado (`MarketConditionHelper`), expor propriedades de texto/cor em `MarketOffer.cs` e `PlayerAircraftSaleItem.cs`, atualizar `MarketView.xaml` para exibir estado em palavras e cores nos 4 subsistemas e badges sem %.
- Manter percentuais que não representam condição física (F4: taxas, descontos comerciais, instrumentos HUD, envelope/MTOW).
- Atualizar e criar testes unitários cobrindo `HealthWords`, `HealthRange`, asserções de peças e planejador.

## O que foi feito
- `src/FlightCareer.Domain/Components/HealthWords.cs`: criado contrato `HealthWords.For(health, feminine, redBelow, yellowBelow, goodAbove)` convertendo valores numéricos em "Quebrado(a)", "Crítico(a)", "Atenção", "Bom(Boa)" e "Íntegro(a)".
- `src/FlightCareer.Domain/Components/HealthVisibility.cs`: `HealthRange.ToString()` atualizado para retornar estados em palavras ("Quebrado", palavra única se exato/mesma faixa, ou "{min} a {max em minúscula}").
- `src/FlightCareer.Domain/Components/ComponentMaintenance.cs`: textos de "RISCO DE PANE" e "ATENÇÃO" simplificados sem percentuais nem interpolação da faixa.
- `src/FlightCareer.Domain/Parts/PartHangarView.cs`: `HealthText` exibe palavra de saúde no feminino para peças conhecidas ou "—" quando desconhecida.
- `src/FlightCareer.Domain/Parts/PartInspectionFlags.cs`: motivos de bandeira vermelha e amarela sem números percentuais.
- `src/FlightCareer.Domain/Parts/PartReplacementPlanner.cs`: motivos de troca orçada sem números, utilizando "Troca preventiva (peça boa)" e "Troca preventiva (peça íntegra)".
- `src/FlightCareer.UI/Views/HangarView.xaml`: tooltip de walkaround e legenda clássica sem percentuais de condição.
- `src/FlightCareer.UI/ViewModels/HangarViewModel.cs`: mensagens de sucesso de substituição, reparo e revisões ("como novo", "por nova(s)") sem ocorrências de "100%".
- `src/FlightCareer.UI/ViewModels/PilotAircraftWrapper.cs`: tooltip de overhaul com "como novo".
- `src/FlightCareer.UI/Services/ComponentFlightSession.cs`: remoção de percentuais de saúde/dano em avisos de panes por desgaste e eventos externos.
- `src/FlightCareer.UI/Services/SimFailureActuator.cs`: mensagem de extinção de fogo sem expor percentual numérico do motor.
- `src/FlightCareer.UI/Services/PersistentFailureService.cs`: causas de dano de motor e desgaste de pastilhas sem números percentuais.
- `src/FlightCareer.UI/Models/MarketConditionHelper.cs`: criado com regras de texto via `HealthWords` e tabela semântica de cores (`#2ECC71`, `#A3E635`, `#F1C40F`, `#E74C3C`).
- `src/FlightCareer.UI/Models/MarketOffer.cs` e `PlayerAircraftSaleItem.cs`: adicionadas propriedades `...ConditionText` e `...ConditionColor` para os 4 subsistemas.
- `src/FlightCareer.UI/Views/MarketView.xaml`: exibição de palavras e cores para Célula, Motor, Pneus e Aviônicos; badges de condição geral/média simplificados sem %; texto de revisão estimada atualizado.
- `docs/plans/correcoes-voo-forcado-sdco.md`, `docs/sistemas/manutencao-hangar.md` e `docs/CHANGELOG.md`: documentação atualizada e itens F1-F3 marcados.
- Testes unitários novos e atualizados:
  - `VisibilityAndMigrationTests.cs`: adicionado `HealthWords_For_Faixas_RetornaPalavra` cobrindo todas as faixas (0, 19.9, 20, 39.9, 40, 79.9, 80, 100 em masculino e feminino) e `HealthRange_ToString_SemNumeros`.
  - `PartHangarViewTests.cs`, `PartHealthBandsAndFailuresTests.cs`, `PartInspectionFlagsTests.cs`, `PartReplacementPlannerTests.cs` e `MaintenanceTests.cs`: asserções atualizadas para os novos textos sem `%`.

## Critérios de aceite
- [x] 1. `dotnet build src/FlightCareer.slnx` com 0 erros e `dotnet test src/FlightCareer.Tests` 100% aprovados.
- [x] 2. `missions_golden.json` não foi alterado.
- [x] 3. Relatório documentando não-teste em MSFS/app aberto e listagem de % remanescentes.
- [x] 4. Entrada no `docs/CHANGELOG.md`, plano com F1–F3 marcados e `docs/sistemas/manutencao-hangar.md` atualizado.
- [x] 5. Commit na branch `fix/sem-porcento`.

## Build e testes
- dotnet build: 0 erros, 0 avisos novos
- dotnet test: 1.300 aprovados, 0 falhas (100% verde)
  - Novos/atualizados: `HealthWords_For_Faixas_RetornaPalavra`, `HealthRange_ToString_SemNumeros`, `PartHangarViewTests`, `PartHealthBandsAndFailuresTests`, `PartInspectionFlagsTests`, `PartReplacementPlannerTests`, `MaintenanceTests`.

## Não testado
- Comportamento visual com a interface WPF aberta em execução ou conectado ao simulador MSFS.

## Dúvidas e decisões para o revisor
- Nenhum % de condição ou saúde física restou nas telas ou avisos. Os únicos percentuais mantidos no sistema são os previstos pela decisão do usuário em F4: taxa de corretagem do mercado (8%), selo de desconto comercial no mercado (`- N%`), nota de voo no debriefing, envelope e MTOW no briefing, e instrumentos do HUD (flaps, N1, etc.).
