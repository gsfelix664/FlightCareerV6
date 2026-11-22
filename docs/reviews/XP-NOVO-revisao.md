# XP-NOVO — Revisão (Claude)

**Branch:** `feat/xp-novo` (`a970820`, `722d8e2` + ajustes do revisor) · **Data:** 2026-11-22

## Veredito: **APROVADO** (com ajustes do revisor, rodada única)

## Ajustes feitos pelo revisor (`FlightMonitor`, arquivo quente)
1. **IMC/nuvem só na aproximação:** a condição `altitude < 3000 || tempo > 60 s` valia o voo inteiro (nuvem em cruzeiro dava o bônus). Agora uma janela dos últimos 5 min simulados antes do toque, como a das rajadas.
2. **Jato:** `ENGINE TYPE` 5 (turboélice) era tratado como jato (vento cruzado de 20 kt). Agora só `ENGINE TYPE` 1; turboélice usa 12 kt, como a spec.
3. **ETE:** usava a distância voada (com desvios), o que deixava quase todo voo "no prazo". Agora a distância direta origem → destino.

## Conferido
- Struct/definição: 6 campos novos só no fim de `SimTelemetry` e do `AddToDef`, mesma ordem e unidade.
- `XpCalculator` (base, M_Tipo/M_Realismo/M_Qualidade com piso/teto do `xp_rules.json`, bônus), `PilotLevel` (300 × (L − 1)², sem máximo), integração no fim de voo e no simulador; `GetRequiredXP` 10 mil / 45 mil / 175 mil / 600 mil.
- Build 0 erros, 809/809 (revisor); fuzz all 920 voos 100 % (executor).

## Observações para o usuário
- Cada pouso conta como perna (+50 XP): um voo de TGL com 6 toques soma +300 de base. É o que o modelo pede (carga de trabalho de pouso), mas é o ponto mais "farmável"; rever na calibração se preciso.
- `REALISM` pode vir em 0–1 em vez de 0–100 (não confirmado sem o simulador); bônus noturno desligado (sem SimVar); infrações de estol e luzes ainda não medidas (contam 0).

## Não testado
- No MSFS (valores reais de `REALISM`, `AMBIENT IN CLOUD`, `DENSITY ALTITUDE`; resumo do XP na tela).
