# Manutenção e Hangar

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Gerencia revisões periódicas, inspeção anual, retífica de motor (TBO), substituição de peças e oficina com tempo de parada.
- **Onde está:** `UI/ViewModels/HangarViewModel`, `Domain/Components/WorkshopDowntime`, `Domain/Parts/PartReplacementPlanner`, `PartPricing`, `PartInspectionFlags`, `ComponentMaintenance`.
- **Regras e integridade:**
  - Acesso ao banco via `IDbContextFactory<AppDbContext>` com contexto curto por operação.
  - Inspeções com fator de compressão regulatória 4× (`MaintenancePricing.CompressionFactor = 4.0`): 12 h (troca de óleo), 25 h (inspeção anual obrigatória, única que interdita a aeronave se vencida e marca `FoundRedAtAnnualAirframeHours`), 125 h (ignição para pistão) e TBO (500 h pistão / 900 h turbina; não interdita). Horas descontadas pelo `FlightService`.
  - Oficina em tempo real (`WorkshopDowntime`, `PilotAircraft.WorkshopUntilUtc`): serviços pagos geram indisponibilidade em minutos reais de relógio; serviço expresso reduz pela metade o tempo com acréscimo de custo. Bloqueia início de voo em `MainViewModel.StartFlight` e `BriefingViewModel`. Libera automaticamente ao fim do prazo.
  - Preços de peças físicas (`PartPricing`): base escala por classe econômica da aeronave (Classes A a J) somada à mão de obra, substituindo o percentual fixo sobre o valor do avião.
  - Lista de peças no Hangar (`PartHangarView`, `PartRowViewModel`): expander por componente exibindo ícones de estado (`✓`, `◐`, `⚠`, `✖`, `?`) e saúde em palavras (`HealthWords`: "Quebrada", "Crítica", "Atenção", "Boa", "Íntegra") sem percentuais numéricos de condição. Faixas de saúde oculta de componentes (`HealthRange`) utilizam estimativa em palavras ("Atenção a bom", "Bom", "Quebrado"). "Reparar tudo" seleciona recomendadas (quebradas e vermelhas via `SelectRecommended`), permitindo troca individual (`Plan(selectedParts)`). Troca zera horas/ciclos, limpa anotações e sincroniza a barra (`SyncComponentsFromParts`). Serviços concluídos e revisões deixam componentes e peças "como novo".
  - Preço final nos botões e panes (`PilotAircraftWrapper`, `ComponentRowViewModel`, `DefectRowViewModel`, `DefectText`): botões de reparo, revisões, abastecimento e seguro exibem o valor final com ajuste da dificuldade do piloto (`DifficultyEconomy.ApplyCost`); panes em aberto mostram frase amigável contextualizada pela fase do voo com dados técnicos em tooltip; faixa de oficina é exibida apenas quando o cartão de status principal não estiver como "OFICINA".
  - Walkaround exibe saúde exata. Vida vencida é informativa (`LifeNote`), sem interdição automática.
