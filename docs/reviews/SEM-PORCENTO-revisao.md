# SEM-PORCENTO — revisão (Claude, 2026-11-27)

**Veredito: APROVADO** com ajustes do revisor.

- Escopo conforme a spec; arquivos quentes (`ComponentFlightSession`, `SimFailureActuator`) com mudança só de texto. Auditoria do Gemini: 0 ações para conferir.
- Build 0 erros; 1.301/1.301.

## Ajustes do revisor
1. Estimativa com mínimo cortado em 0 mostrava "Quebrado a ..."; agora só "Quebrado" quando o máximo é 0.
2. Motivo da troca sem parênteses duplos; usa `PartHangarView.GoodAbovePct` em vez de 80 fixo.
3. Concordância no texto da retífica de motor a turbina.

## Observações (fora do escopo)
- `AircraftStatusEvaluator` ainda decide "RISCO DE PANE"/"ATENÇÃO" pela saúde do componente: pode denunciar peça nunca inspecionada (mudar é regra). Usuário decidiu em 2026-11-27 deixar como está.
- Mercado: "Desgaste moderado..." (pneus) e "Rádio ... saudável" (aviônicos) são textos fixos e podem contradizer o estado (já era assim).
- `EstimatedPostBuyCost` permite deduzir a condição média pelo valor em dinheiro.
- Não conferido com o app aberto nem no MSFS.
