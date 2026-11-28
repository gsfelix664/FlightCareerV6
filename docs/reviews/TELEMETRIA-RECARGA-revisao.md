# Revisão TELEMETRIA-RECARGA — APROVADO (1ª rodada)

**Revisor:** Claude (Opus), 2026-11-27 · **Branch:** `fix/telemetria-recarga` (`997c2c3`)

- `SimConnectService.cs` (quente) lido linha a linha: o diff contém só os passos 1–8 da spec (enum no fim, vigia armado no `Connect`, `Stop()` removido do `OnRecvOpen`, `MarkMessage` no `WndProc`, `CheckLinkAlive` na thread da UI, `Dispose` protegido). Nada fora do escopo.
- `SimLinkWatchdog` puro no Domain; testes conferem valores (limite exato de 30 s não é morto, 30,001 s é; reset por mensagem; rearme).
- Build 0 erros; testes 1.323/1.323.
- **Não testado no MSFS:** fechar o sim à força com o app aberto (status "DESCONECTADO" em ≤ 35 s e reconexão sozinha); conferir no log `[SimConnect]` que o menu principal e o carregamento de voo não causam reconexão em laço.
- Documentação online do SDK (RequestSystemState, pipe quebrado) deu 404; o desenho usa exceção **e** silêncio de 30 s para não depender desse detalhe.
