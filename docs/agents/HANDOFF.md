# Passagem de sessão (Claude) — 2026-11-16 (noite, PC do escritório)

Leia junto: `AGENTS.md` (§3 comandos, §6 regras, §7 estado), `CLAUDE.md`, `docs/agents/LICOES.md` (4 lições novas hoje), `docs/plans/taxas-aeroporto-servicos-solo.md` e os vereditos em `docs/reviews/`.

## Máquina e regras
- **PC do escritório: sem MSFS e sem SDK.** Build, testes e fuzz rodam; nada que precise do simulador.
- Divisão de trabalho: código **e dados** → Gemini com spec (`tools/agents/gemini-task.sh <ID> <branch> <spec>` em segundo plano, worktree `../FlightCareerV6-gemini`); Claude escreve a spec, revisa (`tools/agents/review-pack.sh`), roda build/testes/fuzz, escreve `docs/reviews/<ID>-revisao.md`, faz merge com `FC_ALLOW_PROTECTED=1` e **push só quando o usuário autorizar** (push também precisa de `FC_ALLOW_PROTECTED=1`).
- O Gemini erra com frequência em: afirmar fatos não conferidos no relatório, contextos longos do EF, comando de reprodução. Sempre confira os dados e os arquivos quentes linha a linha.

## Estado
- Base `feat/component-health-system` **enviada ao GitHub** (inclui este handoff). Build 0 erros; `dotnet test` **577/577**; `fuzz --aircraft all --runs 50`: **2.300/2.300**, 0 limites ausentes.
- Worktree do Gemini limpo (todas as branches mergeadas).

## Feito nesta sessão (tudo aprovado e mergeado; **nada testado no MSFS**)
1. **FUZZ-ALL** — `fuzz --aircraft all` (tabela por avião, `reproduce` correto), feature `noFlaps` (J-3, CAP-4), tolerância do oráculo pela taxa real da peça.
2. **egtMax** pelo padrão da classe em G36, CAP-4, DA40NG, DR400 e Dakota (MAX EGT do sim era escala do instrumento); `merge_limits.py` descarta EGT de pistão < 700 °C.
3. **CATALOGO-PADROES** — limites estimados em 44 aviões (`source: ClassDefault`, "estimativa — atualizar pelo POH real"); índices elétricos **só no fuzz** (`FuzzTestDefaults`); feature `noElectrical` (J-3, CAP-4).
4. **GERADOR-CHT-VFE** — gerador sintético escala CHT/óleo/EGT pelo limite do avião e usa dente de flap existente (`SyntheticFlapSelector`).
5. **NOTA-PASSAGEIROS** — conforto por segundos simulados (pausa e solo ignorados), sinal do pitch corrigido (MSFS negativo = nariz para cima), carga só conta G/solavanco, turbulência pronta para sensores futuros; penalidade acumulada por amostra.
6. **AUTOSTART-CAMINHAO** — Key Events observados sem mascarar (grupo `ObservedPilotInputs`); auto-start −10 % XP; caminhão do sim cobra `sim final − combustível do app` e o `MainViewModel` recarrega o contexto longo (`OnFuelTruckCharged`).
7. **Moeda genérica `$`** (decisão do usuário; o app já força en-US).
8. **TAXAS-F1** — `airport_fees.json` + `Domain/Economy` (`AirportFeeCatalog`, `GroundServiceCalculator`): pouso (igual ao antigo), embarque, handling, navegação/terminal cobrados no fim do voo; estacionamento calculado mas não cobrado.

## Decisões do usuário registradas hoje
- `egtMax` com escala de instrumento → padrão da classe; POHs reais serão revisados depois.
- Estimativas no catálogo para testes, sempre marcadas para o POH; índices elétricos fora do catálogo do jogo.
- J-3 Cub e CAP-4 sem sistema elétrico.
- Moeda: `$` genérico, base dólar, sem "US$".
- Taxas de aeroporto (§ 6 do plano): porte leve/médio/pesado (12.500 / 60.000 lb); obrigatórios pouso, estacionamento acima de 2 h, embarque; handling médio/pesado tier 1–2; GPU/catering/degelo/hangar opcionais; tabela por país depois; estacionamento só em tempo de jogo.
- Auto-start: −10 % XP (valor sugerido; constante `ProcedurePenalties.AutoStartXpPenaltyFraction`).

## Próximos passos (sem simulador), na ordem proposta
1. **Iluminação no Hangar** (só domínio) — plano `docs/plans/iluminacao-panes-disjuntor.md`.
2. **Taxas F3** (interface): estimativa das taxas no briefing, extrato por categoria nas finanças, recibo no fim do voo.
3. **Taxas F2 parcial**: medir o tempo parado no solo (tempo de jogo) para cobrar estacionamento; GPU/degelo/pushback dependem do Watcher.
4. **Peças de flap/elétricas** não devem ser provisionadas em aviões `noFlaps`/`noElectrical` (achado da FUZZ-ALL).
5. **Caminhão pedido depois de iniciar o monitoramento** (preparação) ainda não é cobrado: decidir depois do teste no sim.
6. **Integridade pela Flow API** — código pronto, validação no MSFS.

## Decisões ainda pendentes do usuário
- Auto-leme (`AUTORUDDER_TOGGLE`) e outras ajudas: mesma regra do auto-start?
- `eventos-diversos.md` § 3, `eventos-comandos.md` § 3, `eventos-combustivel.md` § 3 (panes `FUELSYSTEM_*`, `REPAIR_AND_REFUEL`), `simconnect-api.md` § 3, plano de iluminação (§ decisões).

## Pendências que precisam do simulador (máquina de casa)
- **Novas de hoje:** Ctrl+E e caminhão chegam como Key Events (`ENGINE_AUTO_START`, `REQUEST_FUEL_KEY`) a um cliente SimConnect?; sinal do `PLANE PITCH DEGREES` (captura subindo e descendo); pausa pelo menu na nota dos passageiros.
- Antigas: SimvarWatcher com as listas novas; SimVarProbe com `--filter "^(?!ATC |AI )"`; escrita de turbina, combustível (novo e legado), slew no pátio; teste de panes (`BREAKER_*_SET`, `PARTIAL PANEL *`, `FUELSYSTEM_*`, `HYDRAULIC_ACTUATOR_ACTIVE_SET`, trim disabled); `FlightSave`/`FlightLoad`; Flow API; índices elétricos reais por avião (`systems.cfg`); voos em outros aviões; F4.2e, crash declarado, pista de terra.
