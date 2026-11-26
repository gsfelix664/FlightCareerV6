# Roteiro de voo de teste (C152, instrução local, ~40 min)

Versão: código do commit `2474190`, build 0 erros, testes 1.189/1.189.
Anote só o que estiver **errado ou estranho** (com o horário). O resto sai dos logs.

## 0. Base

- **SDCO (Sorocaba), escolhido:** testa a área real do DECEA ("Aeroclube de Tatuí", ~23 NM). O banco anterior já está em `db-backup/`; crie o piloto com base SDCO e C152.

Abrir o app:

```powershell
dotnet run --project src/FlightCareer.UI
```

No MSFS: vento personalizado de través na pista de partida, cerca de 8–10 kt. Sem combustível ilimitado.

## 1. Quadro e briefing (no solo)

- [ ] Quadro: linha "Prazo: ..." acima da carga (vermelha em urgente/médica; ausente em voo local).
- [ ] Aceitar **Instrução local** (se não houver, um **Circuito de Tráfego**).
- [ ] Cartão da perna mostra o texto do contrato, com o nome da área e a faixa de altitude.
- [ ] Mapa do briefing desenha a área.
- [ ] Combustível: comprar no Hangar e deixar o sim **diferente** (uns 3 gal): o briefing deve bloquear. Depois igualar: deve liberar.

## 2. Partida e decolagem

- [ ] Abrir o HUD e clicar **⏺ REC** antes de ligar o motor (contador de linhas subindo).
- [ ] Linha "CONTRATO" legível nos modos compacto e expandido; célula, motor e pneus só com ícone.
- [ ] Mini-mapa com a área e o ponto de partida.
- [ ] Decolagem normal: nenhum pouso contado ao sair do chão.

## 3. Área de instrução

- [ ] Chegar **abaixo** da faixa (HUD: abaixo), subir para dentro (na faixa), passar um pouco acima (acima) e voltar.
- [ ] Botão **Marcar** ao entrar na área (texto `area`).
- [ ] O ponto só é cumprido dentro da área e da faixa.

## 4. Circuito e pouso

- [ ] Toques e arremetidas pedidos; o HUD conta "TGL n/N" a cada um.
- [ ] Marcar `tgl` logo após cada toque e anotar a razão de descida mostrada.
- [ ] Pouso final suave (< 200 fpm), com o vento de través.
- [ ] Táxi, cortar o motor, encerrar o voo no app e clicar **⏹ STOP**.

## 5. Depois do voo

- [ ] Tela final: contrato cumprido (ou o motivo), pagamento e detalhamento do XP.
- [ ] Nenhum combustível cobrado a mais no fim.
- [ ] Diário mostra o voo; o avião fica no aeroporto certo.
- [ ] Hangar: ícones de peça sem %; anotar qualquer dano.
- [ ] Fechar o app: o processo encerra.

Avise "voo terminado". O Claude analisa o blackbox, o REC e o banco: toque do blackbox × REC, través e cauda com a proa verdadeira, eventos falsos, desgaste dos pneus, consumo e XP.

## Fora deste voo

Safra (`insumos_agro`) e E2C-2 são de fase 2 ou mais; rotas reais exigem linha aérea; abusos (estol, pouso duro, pausa, aceleração) ficam para um segundo voo, para este log sair limpo.
