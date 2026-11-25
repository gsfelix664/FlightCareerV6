# Manutenção e Hangar

> Estado atual do sistema. Lido sob demanda (índice em AGENTS.md › 7). Atualize este arquivo quando o estado do sistema mudar.

- **O que faz:** Gerencia revisões periódicas, inspeção anual, retífica de motor (TBO), substituição de peças e oficina com tempo de parada.
- **Onde está:** `UI/ViewModels/HangarViewModel`, `Domain/Components/WorkshopDowntime`, `Domain/Parts/PartReplacementPlanner`, `PartPricing`, `PartInspectionFlags`, `ComponentMaintenance`.
- **Regras e integridade:**
  - Acesso ao banco via `IDbContextFactory<AppDbContext>` com contexto curto por operação.
  - Inspeções com fator de compressão regulatória 4× (`MaintenancePricing.CompressionFactor = 4.0`): 12 h (troca de óleo), 25 h (inspeção anual obrigatória, única que interdita a aeronave se vencida e marca `FoundRedAtAnnualAirframeHours`), 125 h (ignição para pistão) e TBO (500 h pistão / 900 h turbina; não interdita). Horas descontadas pelo `FlightService`.
  - Oficina em tempo real (`WorkshopDowntime`, `PilotAircraft.WorkshopUntilUtc`): serviços pagos geram indisponibilidade em minutos reais de relógio; serviço expresso reduz pela metade o tempo com acréscimo de custo. Bloqueia início de voo em `MainViewModel.StartFlight` e `BriefingViewModel`. Libera automaticamente ao fim do prazo.
  - Preços de peças físicas (`PartPricing`): base escala por classe econômica da aeronave (Classes A a J) somada à mão de obra, substituindo o percentual fixo sobre o valor do avião.
  - Lista de peças no Hangar (`PartHangarView`, `PartRowViewModel`): expander por componente exibindo ícones de estado (`✓`, `◐`, `⚠`, `✖`, `?`) sem percentuais numéricos para aeronaves com peças. "Reparar tudo" seleciona recomendadas (quebradas e vermelhas via `SelectRecommended`), permitindo troca individual (`Plan(selectedParts)`). Troca zera horas/ciclos, limpa anotações e sincroniza a barra (`SyncComponentsFromParts`).
  - Walkaround exibe saúde exata. Vida vencida é informativa (`LifeNote`), sem interdição automática.
