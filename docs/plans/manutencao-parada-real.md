# Plano — Parada real de manutenção e textos com os prazos reais

**Estado:** aprovado pelo usuário em 2026-11-22. Implementar **depois da calibração do pagamento** (`economia-estabilidade.md` › 4.3), porque a parada muda quanto cada avião fatura por mês.

## 1. Como é hoje
- Todo serviço pago do Hangar (`HangarViewModel.RunPaidServiceAsync`, ~linha 236) só avança o relógio de carreira (`CareerClock.Advance`). Não existe campo de "disponível a partir de": o avião pode voar logo depois de uma retífica de 72 h.
- Paradas atuais (tempo de jogo): troca de óleo 2 h, inspeção anual 8 h, revisão de ignição 6 h, retífica 72 h (`CareerClock`), reparo padrão 2 h, troca de peça = maior parada entre as peças trocadas (faixa `downtimeHours` do `parts_catalog.json`, de 0,5–1 h no farol de pouso a 60–150 h nas longarinas), conversão de cabine 4/8/24 h.
- O relógio de carreira afeta seguro (30 dias), vida em meses das peças e a data mostrada no Hangar.
- **Incoerência:** o simulador de carreira (`CareerProgressionSimulator`, `remainingDowntimeHours`/`daysGrounded`) já deixa o bot sem voar durante a parada; o jogo não.
- Intervalos comprimidos ÷ 4 (decisão de 2026-11-17/19, `MaintenancePricing`): troca de óleo 12 h, inspeção anual 25 h, revisão de ignição 125 h, TBO 500 h (pistão) / 900 h (turbina), vidas de peça em horas e ciclos ÷ 4. Calendário (meses) **não** comprimido. A tela mostra só os números comprimidos ("Troca de óleo (12 h)"), como se fossem reais.

## 2. Decisões do usuário (2026-11-22)
1. **Duas durações para a mesma parada:**
   - **Tempo de jogo:** o relógio de carreira avança a parada já estabelecida (ex.: retífica 72 h), como hoje.
   - **Tempo real:** o avião fica **indisponível alguns minutos de relógio do computador**, conforme o tamanho do serviço. Durante esse tempo ele não pode ser escolhido para voo; o piloto espera ou voa outro avião da frota.
2. **Textos com os prazos reais:** a tela mostra os intervalos e vidas **da vida real** e explica que o jogo cobra esses prazos num ritmo acelerado. Ex.: "TBO de 2.000 h na vida real — no jogo conta em ritmo acelerado (500 h de voo)". Ninguém troca óleo a cada 12 h nem faz anual a cada 25 h: o jogador precisa ver 50 h e 100 h.

## 3. Proposta
### 3.1 Parada em tempo real
- `PilotAircraft.MaintenanceUntilUtc` (coluna nova, `DateTime?`, **relógio real**, UTC) e `MaintenanceDescription` (texto do serviço). `RunPaidServiceAsync` grava `agora + minutosReais` junto com o avanço do relógio de carreira.
- Conversão sugerida (calibrar): `minutosReais = clamp(round(2 × √horasDeJogo), 1, 20)` → troca de óleo (2 h) ≈ 3 min; revisão de ignição (6 h) ≈ 5 min; anual (8 h) ≈ 6 min; retífica (72 h) ≈ 17 min; longarina (150 h) = 20 min (teto); farol (1 h) = 2 min. Raiz quadrada para que serviços grandes demorem mais sem virar uma hora parada. Valores no `component_catalog.json` (calibração em dados, não em código).
- Bloqueios: Hangar mostra "Na oficina — libera em mm:ss" com contagem regressiva; o briefing marca o item da aeronave como bloqueado; `StartFlight`/`StartMonitoring` recusam. Serviços novos no mesmo avião durante a parada: permitidos, a parada vira a maior das duas (não soma).
- Persistência: o fim da parada é absoluto em UTC, então fechar o app não "congela" a oficina.
- **Serviço expresso** (opcional, decisão pendente): +50 % no preço e metade dos minutos reais e das horas de jogo.
- **Simulador:** conferir que o `CareerProgressionSimulator` usa as mesmas horas de jogo; os minutos reais não entram na simulação (bots não esperam relógio de parede). Sem efeito na calibração além do que já existe.

### 3.2 Textos com os prazos reais
- Uma fonte única do fator: `MaintenancePricing.CompressionFactor = 4.0` (hoje o ÷ 4 está espalhado em comentários e constantes). Funções `RealHours(gameHours)` e `RealCycles(gameCycles)`.
- Valor real exibido = valor de jogo × 4 (pistão: óleo 50 h, anual 100 h, ignição 500 h, TBO 2.000 h; turbina TBO 3.600 h; peças idem). Onde o catálogo tiver o valor real do fabricante (POH) — ex.: TBO do Lycoming O-235 do C152 = 2.400 h —, mostrar o do fabricante (decisão pendente: usar sempre × 4 ou o real por motor quando houver).
- Onde muda (Hangar): botões e rótulos ("Troca de óleo — a cada 50 h na vida real"), cabeçalho dos grupos (`PartHangarView.GroupLifeInfo`: "TBO 2.000 h (vida real) · faltam 1.120 h no ritmo do jogo"), notas de vida das peças (`PartInspectionFlags.LifeNote`), legenda do Hangar com uma linha fixa: "Os prazos de manutenção são os reais; o jogo conta as horas em ritmo acelerado (×4) para caber na carreira." Mensagens de confirmação de serviço e o HUD, se mostrarem prazos.
- Como contar o restante (decisão pendente): (a) mostrar o restante já convertido para horas reais (1.120 h "reais" = 280 h de voo no jogo) ou (b) mostrar o restante em horas de voo do jogo (280 h) ao lado do prazo real. Sugestão: (b), porque é o que o piloto precisa planejar ("posso fazer mais 280 h de voo").

## 4. Testes
- `RealHours`/`RealCycles` (× 4) e textos de `GroupLifeInfo` com o prazo real.
- Conversão horas de jogo → minutos reais (tabela da § 3.1, piso e teto).
- Disponibilidade: avião com `MaintenanceUntilUtc` no futuro é recusado no início do voo (lógica pura num serviço de Domain/Application com relógio injetável, sem `DateTime.Now` direto).
- Serviço sobre parada em andamento usa a maior parada.
- `ComponentPersistenceTests`: banco antigo recebe a coluna nova.

## 5. Decisões pendentes do usuário
1. Fórmula/tabela dos minutos reais (a sugestão da § 3.1 serve?).
2. Serviço expresso: sim ou não; preço e redução.
3. Prazo real: sempre × 4 ou o valor do fabricante por motor/peça quando o catálogo tiver.
4. Restante: em horas de jogo (b, sugerido) ou convertido em horas reais (a).

## 6. Decisões do usuário (2026-11-22, respostas à § 5)
1. Minutos reais: **aprovada** a sugestão da § 3.1 (`clamp(round(2 × √horasDeJogo), 1, 20)`).
2. Serviço expresso: **sim** (+50 % no preço, metade dos minutos reais e das horas de jogo).
3. Prazo real: **sempre × 4** (não usar o valor do fabricante por motor).
4. Como mostrar o restante: em discussão.
