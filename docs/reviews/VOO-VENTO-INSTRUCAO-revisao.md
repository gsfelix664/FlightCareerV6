# Revisão VOO-VENTO-INSTRUCAO (Claude, 2026-11-25)

**Veredito: APROVADO**

- Arquivos quentes (`SimTelemetry.cs`, `SimConnectService.cs`, `FlightMonitor.cs`): diff lido linha a linha. Campo `PlaneHeadingTrue` e `AddToDef("PLANE HEADING DEGREES TRUE", "degrees")` só no fim, mesma ordem; `FlightMonitor` só repassa `MinAglFt`.
- Vento de través e de cauda usam a proa verdadeira; testes cobrem proa magnética divergente.
- REC: 4 sensores novos no `sensor_registry.json` (vento, proa verdadeira e magnética); contagens do layout ajustadas.
- Faixa de altitude: codec com 5º campo opcional (textos antigos inalterados), tolerância de 500 ft só quando há mínimo; mensagem de conclusão mostra a faixa.
- Instrução local 1–2 TGL sem mudar a sequência do `random`; snapshot mudou só em instrução local e cheque prático.
- Build 0 erros; testes 1.071/1.071 (rodados pelo revisor). Auditoria do `agy`: só comandos de teste e regeneração do snapshot dentro do worktree.
- Não testado no MSFS: leitura de `PLANE HEADING DEGREES TRUE`, colunas novas no REC e cobrança da faixa em voo.
