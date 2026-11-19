# Spec ECON-STAB — Simulação da carreira completa (progressão, falência, enriquecimento) nas 3 dificuldades

**Objetivo (1 frase):** estender o simulador de carreira para bots que começam como o jogador (saldo da dificuldade + C152), voam, fazem (ou adiam) manutenção, compram e vendem aviões e sobem de categoria por 30, 180 e 720 dias, medindo se a economia é estável: ninguém falindo cedo por azar e ninguém ficando rico rápido demais.

Pedido do usuário (2026-11-19): "verificar se manutenção, pagamento das missões (pax e carga) e preço dos aviões deixam o mercado estável a curto, médio e longo prazo, nos 3 níveis de dificuldade; o jogador não pode se frustrar falindo cedo nem ficar rico rápido e perder a graça".

**Esta tarefa só mede.** Não mude preço, pagamento, vida, taxa nem saldo inicial. **Limite de 1h30:** não rode a bateria completa; rode só a amostra do item 5. O revisor roda a completa.

## Fatos levantados
- Dificuldade (`Domain/Entities/Pilot.DifficultyMode`): só muda o saldo inicial em `UI/ViewModels/CreatePilotViewModel.cs` ~l.58 — Fácil $1.000.000.000, Médio $100.000, Difícil $10.000. Todos começam com um C152 com 5.000 h de célula e meio tanque. Extraia esses valores para uma classe pura (ex.: `Domain/Services/CareerStart`) com os **mesmos** números e faça o `CreatePilotViewModel` usá-la.
- Categorias do piloto (`PilotCategory` 1–5) limitam as missões em `MissionGenerator` ~l.348-373. Veja como o jogador sobe de categoria (XP/horas/licenças) e aplique a mesma regra aos bots; se a subida for manual (compra de licença etc.), reproduza o custo.
- Mercado: `Infrastructure/Services/MarketSimulationService` (`CalculateBaseMarketValue` por ano/condição, anúncios com queda de preço, venda com 8 % de corretagem, `BuyMarketListingAsync`); avião novo pelo preço do `aircrafts.csv`.
- Combustível: `FuelPriceService` dobra a cada 10 anos (inflação), já aceita data. Pagamento das missões **não** tem inflação.
- Simulador atual: `Application/Simulation/CareerSimulator.cs` + `FlightCareer.Replay career` (bot com avião fixo, missões reais, combustível, taxas, seguro, manutenção do Hangar). **Lento**: 8 aviões × 5 bots × 90 dias não terminou em 1 h.

## O que construir
1. **Desempenho primeiro:** gere missões em memória (aeroportos e regras carregados uma vez; cache por aeroporto e dia de jogo), sem `SaveChanges` por voo; desgaste só no modo `--fast` (sem frames). Meta: 3 dificuldades × 3 políticas × 5 bots × 720 dias em < 10 min. Meça e informe.
2. **Bot de carreira** (`CareerProgressionSimulator` ou modo novo do `CareerSimulator`):
   - Início igual ao jogador (saldo da dificuldade, C152 5.000 h, base Tier 2 configurável).
   - Políticas: `cauteloso` (manutenção em dia, reserva de 3 meses de custo antes de comprar), `equilibrado` (manutenção quando vence, reserva de 1 mês), `agressivo` (adia manutenção até NO-GO/quebra, compra assim que o saldo cobre o avião).
   - Compra: escolhe o próximo avião da escada A→B→…→J que a categoria permite, novo (csv) ou usado (mercado, se o `MarketSimulationService` puder ser usado em memória; senão usado = valor de `CalculateBaseMarketValue`), vende o anterior com 8 % de corretagem. Uma aeronave por bot (frota fica fora).
   - Falência: saldo < 0 **e** sem conseguir pagar o combustível do próximo voo nem a manutenção NO-GO → falido no dia X (para de voar).
   - Relógio: usa o `CareerClock` se já estiver na base; senão, dias = horas de voo + paradas / 24.
3. **CLI** `FlightCareer.Replay career-stab --days 720 --bots 5 --seed 1 [--difficulty easy|medium|hard|all] [--policy all|...] [--out <pasta>]` (saída em `%LocalAppData%/FlightCareer/career-sim/`, nunca no repo).
4. **Relatório `stability.md`** (português, moeda `$`): para cada dificuldade × política, nos dias **30, 180 e 720**:
   - % de bots falidos e dia médio da falência; saldo e patrimônio (saldo + valor de mercado do avião) mediano, mín. e máx.;
   - classe de avião atingida (A–J) e dia da 1ª compra; categoria do piloto;
   - receita/h, custo/h por categoria (combustível, taxas, manutenção, seguro) e margem % no início e no fim (efeito da inflação do combustível);
   - manutenções e panes por 100 h; dias parados.
   - Alertas automáticos: "falência precoce" (> 10 % dos cautelosos falidos até o dia 30), "enriquecimento rápido" (patrimônio > 20 × o inicial até o dia 30 no Médio/Difícil), "estagnação" (sem 1ª compra até o dia 180 no Médio), "inflação come a margem" (margem do dia 720 < 50 % da do dia 30).
   - CSV `stability.csv` com uma linha por bot × marco.
5. **Amostra para o relatório do executor:** `--days 60 --bots 2 --difficulty medium --policy all --fast`; cole o resumo em `docs/reviews/ECON-STAB-relatorio.md`.

## Fora do escopo
- Mudar qualquer valor de economia/dificuldade. UI (exceto a extração do saldo inicial). Arquivos de voo quentes.

## Testes
- `CareerStart_SaldosIguaisAoJogo` (1e9 / 1e5 / 1e4, C152 5.000 h).
- `Progression_MesmaSemente_MesmoResultado` (2 bots, 20 dias).
- `Progression_BotCompraQuandoCabeNaReserva`; `Progression_FalenciaQuandoSemDinheiro`; `Progression_CategoriaLimitaMissoes`.

## Critérios de aceite
- [ ] Build 0 erros; `dotnet test` 100 %; `fuzz --aircraft all --runs 5`.
- [ ] Tempo medido da bateria (estimado pela amostra) no relatório.
- [ ] CHANGELOG (`## 2026-11-19 - Simulação de estabilidade da carreira (Gemini)`); `AGENTS.md` › 3 (comando `career-stab`).
- [ ] Commit(s); relatório só com fatos medidos.
