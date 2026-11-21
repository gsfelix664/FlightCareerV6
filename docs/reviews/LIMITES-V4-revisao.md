# LIMITES-V4 — Revisão (Claude, 2026-11-17)

**Veredito: APROVADO**

Branch `feat/limites-v4` (commits `debf7ca` fonte do usuário, `ea7c622` Gemini).

## Conferido pelo revisor
- Build 0 erros; `dotnet test` 577/577; `fuzz --aircraft all --runs 3` 138/138 (o Gemini rodou `--runs 20`: 920/920).
- Script de conferência base × branch nos 17 perfis alterados: **0** limites POH/SimVar/Cfg/NativeOnset alterados ou removidos; 49 `ClassDefault` substituídos; demais chaves são novas. `poh_values.csv`: só adições (130 linhas), referência marcada "não conferido no POH".
- Mapeamento: 19 aeronaves mapeadas, 8 sem perfil (não criados), 3 duvidosas (CTSL × CT, CAP 10 × CAP-4, DR400/100 × DR400) fora, para decisão do usuário.
- Vfe por dente segue o índice do esquema (`vfe:1..n`); valores SimVar existentes mantidos.

## Observações (não bloqueiam)
1. `vlo` = velocidade de **extensão** do trem (737 270 kt, ATR 170, C90 182, PC-24 200, BelugaXL 250), igual à convenção já usada nas linhas POH antigas (A320neo, TBM, King Air). A regra `gear_operation_overspeed_vlo` vale para extensão e recolhimento; o recolhimento costuma ser menor (737 235, ATR 160, C90 163, PC-24 180, A330 220). Decisão do usuário: manter extensão ou usar o menor dos dois em todo o catálogo.
2. `rpmRedline` em turbohélice (C90 1900, C208 1900, King Air 1700, PC-12 1700) é rotação da **hélice**; hoje só o gerador sintético usa essa chave em turbina (a regra `piston_rpm_overspeed` é só pistão). Se surgir regra de turbina com `rpmRedline`, conferir o sensor.
3. Divergências com POH existente mantidas: King Air 350 `vne` 245 × 263 (v4); DA62 `vle` 201 × 156 (v4) — o valor atual do DA62 merece conferência no AFM.
4. Relatório citava hash `98bcad7`; corrigido para `ea7c622`.
