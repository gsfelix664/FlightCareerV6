# Roteiro de Voos de Validação de Sensores (F1.4)

Este documento é o guia de voo para o piloto validar a telemetria do simulador para cada uma das classes de aeronave (matriz A–I). A validação dos sensores é necessária para que as fases seguintes (peças, desgaste, panes) utilizem apenas dados comprovadamente funcionais no Microsoft Flight Simulator 2020/2024.

---

## 1. Preparação Pré-Voo

- [ ] **Wear and tear do MSFS ligado** (padrão do jogo; decisão de 2026-11-14, plano › 0.1). O addon lê o desgaste nativo e complementa. Só desligue no voo de comparação da F1.4b.
- [ ] **Iniciar o FlightCareer e o Simulador:** Conecte o FlightCareer ao MSFS e carregue o voo na posição de pátio (Cold & Dark).
- [ ] **Ligar gravação de sensores:** No HUD Overlay do FlightCareer, clique no botão **REC** para iniciar o `SensorRecorder`. Confirme que o indicador exibe gravação ativa.
- [ ] **Localização dos arquivos:** Os dados brutos são gravados em `$env:LOCALAPPDATA\FlightCareer\sensor-logs\` (ou `%LocalAppData%\FlightCareer\sensor-logs\` no cmd) nos formatos `AAAAMMDD-HHMM-<TITULO>.csv` e `...-context.json`.

---

## 2. Checklist dos 12 Passos do Voo

Ao atingir cada etapa, clique no botão **Marcar** do HUD Overlay, digite o **texto exato do marcador** indicado abaixo e execute a manobra pelo tempo estipulado.

- [ ] **Passo 01 — Partida:**
  - **Marcador:** `01-partida`
  - **Ação:** Ligar bateria, acionar bomba (se aplicável), partida do(s) motor(es).
  - **Tempo:** Manter em marcha lenta (idle) até estabilizar pressões e temperaturas (~30 a 60 segundos).

- [ ] **Passo 02 — Frenagem no Táxi:**
  - **Marcador:** `02-freio-taxi`
  - **Ação:** Iniciar táxi, atingir ~10–15 kt em linha reta e aplicar frenagem forte e simétrica nos pedais até quase parar.
  - **Tempo:** Frenagem imediata (~3 a 5 segundos).

- [ ] **Passo 03 — Decolagem e Subida:**
  - **Marcador:** `03-decolagem`
  - **Ação:** Alinhar na pista, aplicar potência máxima de decolagem, corrida e subida inicial mantendo Vy.
  - **Tempo:** Subir até pelo menos 1.000 ft AGL em subida contínua (~60 a 90 segundos).

- [ ] **Passo 04a — Voo de Cruzeiro Nivelado:**
  - **Marcador:** `04a-cruzeiro`
  - **Ação:** Nivelar na altitude de cruzeiro, ajustar potência e passo para condição padrão de cruzeiro.
  - **Tempo:** Manter voo reto e nivelado estável por pelo menos 60 segundos.

- [ ] **Passo 04b — Empobrecimento de Mistura / Condição Fora do Ideal:**
  - **Marcador:** `04b-mistura-pobre`
  - **Ação:** Em motores a pistão sem FADEC, empobrecer a mistura além do pico de EGT (lean of peak) ou fora do ideal até notar leve queda de RPM/oscilação; em seguida retornar à mistura rica.
  - **Tempo:** Segurar empobrecido por ~30 a 45 segundos antes de enriquecer.
  - *Pular se:* Aeronave FADEC (diesel), turboélice ou jato.

- [ ] **Passo 05 — Arrefecimento Brusco (Shock Cooling):**
  - **Marcador:** `05-shock-cooling`
  - **Ação:** Reduzir a manete de potência bruscamente para marcha lenta (idle) e comandar descida rápida (~1.500 a 2.000 fpm).
  - **Tempo:** Sustentar descida por 30 segundos para registrar queda térmica brusca em CHT/EGT/óleo.

- [ ] **Passo 06 — Flap Acima da Vfe:**
  - **Marcador:** `06-flap-vfe`
  - **Ação:** Acelerar ligeiramente acima da velocidade máxima de flap (Vfe) e estender 1 dente de flap por 5 segundos; recolher imediatamente.
  - **Tempo:** Flap estendido por 5 segundos acima de Vfe.
  - *Atenção:* Não exceder excessivamente a Vfe para não quebrar no ar; o objetivo é acionar o gatilho de sobrevelocidade de flap do simulador.

- [ ] **Passo 07 — Curva de Grande Inclinação (60° / ~2 G):**
  - **Marcador:** `07-curva-60`
  - **Ação:** Entrar em curva coordenada mantendo 60° de inclinação (fator de carga sustentado de ~2 G) e altitude constante.
  - **Tempo:** Manter por 360° ou pelo menos 20 a 30 segundos.

- [ ] **Passo 08 — Desligamento do Alternador/Gerador:**
  - **Marcador:** `08-alternador-off`
  - **Ação:** Desligar a chave do alternador/gerador principal (Master Alternator / Generator OFF), mantendo a bateria ligada para descarregar o barramento.
  - **Tempo:** Manter desligado por 60 segundos.

- [ ] **Passo 08b — Religamento do Alternador/Gerador:**
  - **Marcador:** `08b-alternador-on`
  - **Ação:** Religar a chave do alternador/gerador e observar o retorno da carga e tensão.
  - **Tempo:** Manter por ~30 segundos para estabilização elétrica.

- [ ] **Passo 09 — Ciclo do Trem de Pouso:**
  - **Marcador:** `09-trem-ciclo`
  - **Ação:** Em voo nivelado na velocidade de operação de trem (Vlo), comandar extensão e, após travado, recolhimento (ou vice-versa).
  - **Tempo:** Ciclo completo de extensão e recolhimento (~15 a 30 segundos).
  - *Pular se:* Aeronave de trem de pouso fixo.

- [ ] **Passo 10a — Pouso Normal:**
  - **Marcador:** `10a-pouso-normal`
  - **Ação:** Aproximação final e pouso com toque suave e flare adequado.
  - **Tempo:** Do arredondamento até o toque e compressão dos amortecedores.

- [ ] **Passo 10b — Pouso Firme:**
  - **Marcador:** `10b-pouso-firme`
  - **Ação:** Executar toque firme na pista (~400 a 600 fpm na razão de descida do toque) em touch-and-go ou circuito adicional.
  - **Tempo:** Toque firme na pista.

- [ ] **Passo 11 — Frenagem Máxima Pós-Pouso:**
  - **Marcador:** `11-freio-pouso`
  - **Ação:** Após o toque final na pista, aplicar frenagem máxima nos pedais até velocidade de táxi (ou parada completa), verificando temperatura e desgaste.
  - **Tempo:** Frenagem contínua durante a desaceleração (~10 a 20 segundos).

- [ ] **Passo 12 — Corte e Desligamento no Pátio:**
  - **Marcador:** `12-corte`
  - **Ação:** Táxi até a vaga, acionamento do freio de estacionamento, corte de mistura/combustível/ignição e desligamento do master elétrico.
  - **Tempo:** Até parada total do(s) motor(es) (~15 a 30 segundos).
- [ ] **Finalizar Gravação:** Desligar o botão **REC** no HUD Overlay do FlightCareer.

---

## 3. Matriz de Aeronaves para Validação (A–I)

| # | Tipo | Exemplos | O que valida |
|---|---|---|---|
| **A** | Pistão carburado, passo fixo, trem fixo | Cessna C152, C172 (analógico), Piper Cub | Carb heat, magnetos, sem hélice variável, trem fixo |
| **B** | Pistão injetado, passo variável | Beechcraft Bonanza G36, Cirrus SR22 | Manifold pressure (MP), governador de hélice, sem carburador |
| **C** | Pistão turboalimentado | Cirrus SR22T, Mooney M20R Turbo | TIT, wastegate, pressão do turbocompressor |
| **D** | Diesel FADEC (bimotor) | Diamond DA40 NG, DA62 | Arrefecimento líquido, glow plug, 2 motores FADEC |
| **E** | Bimotor pistão, trem retrátil | Beechcraft Baron G58 | Índices de telemetria `:1`/`:2`, hidráulico de trem retrátil |
| **F** | Turboélice monomotor | Pilatus PC-12, Daher TBM 930, Kodiak 100, Cessna 208 Caravan | ITT, torque, beta/feather de hélice, sangria de ar (bleed) |
| **G** | Turboélice bimotor pressurizado | Beechcraft King Air 350 | Pressurização de cabine, diferencial, sistemas hidráulico/pneumático |
| **H** | Jato executivo | Cessna Citation CJ4, Citation Longitude | N1/N2, reversor de empuxo, antiskid, spoiler/speedbrake |
| **I** | Jato de linha aérea | Airbus A320neo, Boeing 737 | APU, autobrake, arquitetura hidráulica completa |

> [!NOTE]
> Outros tipos existentes no MSFS (helicópteros, planadores, elétricos, anfíbios e warbirds) estão temporariamente fora do escopo da Fase 1 e serão abordados em fases posteriores se necessário.

---

## 4. Análise dos Resultados

Após completar o voo de uma aeronave da matriz:

1. Localize o arquivo gerado em `$env:LOCALAPPDATA\FlightCareer\sensor-logs\`.
2. Execute o script de análise a partir da raiz do repositório:

```powershell
python tools/analyze_sensor_log.py --type <TIPO> --model <MODELO> "$env:LOCALAPPDATA\FlightCareer\sensor-logs\AAAAMMDD-HHMM-<ARQUIVO>.csv"
```

*Exemplo para o Cessna 152 (Tipo A):*
```powershell
python tools/analyze_sensor_log.py --type A --model C152 "$env:LOCALAPPDATA\FlightCareer\sensor-logs\20261114-0653-Cessna_C152.csv"
```

> [!IMPORTANT]
> Passe apenas um arquivo CSV por voo. Junte múltiplos arquivos apenas se forem da **mesma** gravação (por exemplo, arquivos divididos com sufixo). Nunca misture arquivos de voos ou sessões diferentes no mesmo comando.

O relatório em Markdown será gerado automaticamente em `docs/plans/sensor-validation/<TIPO>-<MODELO>.md`.

---

## 5. F1.4b — Voos do Wear and Tear Nativo

Esta seção tem como objetivo validar em voo real o comportamento das SimVars `WEAR AND TEAR *` do MSFS 2024, testar as sintaxes de ID do SDK e observar como o simulador persiste o desgaste nativo e se reinicia em 100%.

Execute os passos a seguir preferencialmente no **Cessna C152** primeiro:

### Checklist de Voos (F1.4b)

- [ ] **Voo A — Roteiro Completo com Estresse Forte (Wear and Tear LIGADO):**
  - **Configuração:** Opção de Wear and Tear ligada no MSFS (padrão).
  - **Procedimento:** Seguir o roteiro completo dos 12 passos da seção 2, enfatizando estresse nos itens de teste (mistura pobre, shock cooling, flap acima de Vfe, curva 60°, frenagem forte).
  - **Marcador adicional no corte:** Gravação contínua até o passo 12.

- [ ] **Passo B — Reinício do Voo no Simulador (com REC ligado):**
  - **Ação:** No final do Voo A (com o motor cortado e a gravação REC **ainda ativa**), reiniciar o voo diretamente pelo menu do simulador (Esc → Reiniciar / Restart).
  - **Marcador:** Imediatamente após o recarregamento na pista/pátio, clicar em **Marcar** e registrar:
    - **Marcador:** `nw-reinicio`
  - **Objetivo:** Registrar se os níveis das SimVars `nw-level-*` voltam a 100% (1.00) ou se preservam o desgaste do voo anterior.

- [ ] **Voo C — Voo Curto com Wear and Tear DESLIGADO:**
  - **Configuração:** No menu de assistência / opções do MSFS, **desligar** a opção de falhas/wear and tear.
  - **Procedimento:** Voo curto de ~5 minutos: decolagem, 1 curva de 60° (~2 G) e flap 1 estendido acima da Vfe por 5 segundos.
  - **Marcador:** Antes de iniciar as manobras de estresse:
    - **Marcador:** `nw-opcao-desligada`
  - **Objetivo:** Comparar os dados para identificar se há alguma SimVar ou indicativo que sirva como detecção de opção desligada (chave anti-trapaça).

- [ ] **Voo D (Opcional/Se possível) — Persistência via Arquivo `.FLT` do EFB:**
  - **Ação:** Após um voo com desgaste, salvar o estado do voo em arquivo `.FLT` pelo EFB do avião (com avatar dentro da aeronave).
  - **Recarregamento:** Recarregar o voo a partir desse arquivo `.FLT` salvo.
  - **Marcador:** Com o REC ligado após o carregamento:
    - **Marcador:** `nw-flt-carregado`
  - **Objetivo:** Verificar se o MSFS carrega o desgaste persistido no arquivo de situação de voo.

### Comandos de Análise dos Voos F1.4b

Para cada CSV gerado no `$env:LOCALAPPDATA\FlightCareer\sensor-logs\`:

```powershell
# Voo com estresse e reinício (C152):
python tools/analyze_sensor_log.py --type A --model C152 "$env:LOCALAPPDATA\FlightCareer\sensor-logs\AAAAMMDD-HHMM-Cessna_C152.csv" --out docs/plans/sensor-validation/native-wear-C152.md

# Voo com opção desligada:
python tools/analyze_sensor_log.py --type A --model C152 "$env:LOCALAPPDATA\FlightCareer\sensor-logs\AAAAMMDD-HHMM-Cessna_C152-desligado.csv" --out docs/plans/sensor-validation/native-wear-C152-desligado.md
```

---

## 6. F1.4c — Voos de Calibração (addon × nativo)

Este roteiro tem como objetivo:
1. Obter a base de ruído em operação normal para determinar o **limiar** de overstress de cada componente (`limiar = max(ruído normal × 3, 0.0001)`).
2. Confirmar a **unidade** da taxa nativa (`integral(taxa·dt) ≈ queda do nível`).
3. Permitir a calibração das taxas de estresse do catálogo do addon (para aviões sem wear and tear).
4. Avaliar o comportamento de `IS WEAK` e `IS FAILED` em voo longo de overstress contínuo.

### Preparação Pré-Voo

- [ ] **Wear and tear do MSFS LIGADO:** Opção padrão do simulador ativada (essencial para que as taxas e níveis nativos respondam).
- [ ] **Aeronave:** Cessna C152 (ou primeiro avião da classe A disponível).
- [ ] **Pista inicial:** Pista asfaltada (depois os passos 1 e 7 serão repetidos em pista de terra).
- [ ] **Gravação de sensores:** Clicar em **REC** no HUD Overlay do FlightCareer ao iniciar a sessão. Confirmar gravação ativa em `%LocalAppData%\FlightCareer\sensor-logs\`.

### Checklist dos Passos de Calibração (Pista Asfaltada)

- [ ] **Passo 1 — Operação Normal (Base de Ruído):**
  - **1a. Táxi normal:**
    - **Marcador:** `cal-normal-taxi`
    - **Ação:** Táxi suave com potência baixa, sem frenagens bruscas.
    - **Duração:** ~30 a 60 segundos.
  - **1b. Decolagem e subida normais:**
    - **Marcador:** `cal-normal-decolagem`
    - **Ação:** Alinhamento, aceleração de decolagem e subida mantendo parâmetros dentro da faixa verde.
    - **Duração:** Até nivelar em altitude segura.
  - **1c. Cruzeiro normal:**
    - **Marcador:** `cal-normal-cruzeiro`
    - **Ação:** Voo reto e nivelado com RPM e temperaturas estabilizadas na faixa verde.
    - **Duração:** 5 minutos contínuos.
  - **1d. Pouso e táxi normais:**
    - **Marcador:** `cal-normal-pouso`
    - **Ação:** Aproximação estabilizada, toque suave (~100–200 fpm) e táxi de retorno sem travamento de freios.
    - **Duração:** Do flare até o término do táxi.

- [ ] **Passo 2 — Overstress de RPM do Motor (ID 39):**
  - **2a. RPM acima da linha vermelha por 30 s:**
    - **Marcador:** `cal-abuso-rpm-30s`
    - **Ação:** Manete de potência no máximo / mergulho leve para ultrapassar a linha vermelha de RPM (~2.600+ RPM no C152).
    - **Duração:** Sustentar por exatamente 30 segundos; retornar à faixa verde.
  - **2b. RPM acima da linha vermelha por 60 s:**
    - **Marcador:** `cal-abuso-rpm-60s`
    - **Ação:** Repetir a condição de sobre-rotação contínua.
    - **Duração:** Sustentar por exatamente 60 segundos; retornar à faixa verde.

- [ ] **Passo 3 — Superaquecimento CHT e Óleo (ID 39, 41):**
  - **Marcador:** `cal-abuso-cht`
  - **Ação:** Subida longa em potência máxima com velocidade baixa (refrigeração reduzida) e mistura rica incorreta para levar CHT e temperatura do óleo ao topo da faixa / linha vermelha.
  - **Duração:** Sustentar na faixa de alerta por ~60 segundos antes de restabelecer refrigeração.

- [ ] **Passo 4 — Flap Acima da Vfe (IDs 5–8):**
  - **Marcador:** `cal-abuso-flap-vfe`
  - **Ação:** Nivelado, acelerar ligeiramente acima da velocidade máxima de operação de flaps (Vfe) e estender 1 dente de flap.
  - **Duração:** Sustentar estendido por 10 segundos acima de Vfe; recolher imediatamente.

- [ ] **Passo 5 — Excesso de Fator de Carga G (IDs 9–14, 19, 20):**
  - **Marcador:** `cal-abuso-g`
  - **Ação:** Curva de grande inclinação acelerada (60°) ou puxada gradual sustentando ~3,0 a 3,5 G para estressar comandos e célula.
  - **Duração:** ~15 a 20 segundos de manobra.

- [ ] **Passo 6 — Frenagem Forte em Velocidade (IDs 36, 37):**
  - **Marcador:** `cal-abuso-frenagem`
  - **Ação:** Na pista, acelerar até ~25–30 kt e aplicar frenagem máxima contínua nos pedais até a parada quase completa.
  - **Duração:** Até a desaceleração total (~5 a 8 segundos).

- [ ] **Passo 7 — Pouso Firme e Pouso Duro (IDs 35, 37):**
  - **7a. Pouso firme:**
    - **Marcador:** `cal-abuso-pouso-firme`
    - **Ação:** Toque na pista com razão de descida em torno de ~500 fpm (arredondamento tardio ou reduzido).
  - **7b. Pouso duro:**
    - **Marcador:** `cal-abuso-pouso-duro`
    - **Ação:** Toque deliberadamente mais forte (~700 fpm) sem arredondamento excessivo, mas sem colidir com o solo.

### Checklist de Repetição em Pista de Terra

Repetir os passos 1 (base de ruído de solo) e 7 (impacto) em aeródromo ou pista não pavimentada (terra/grama) para diferenciar o atrito de superfície:

- [ ] **Táxi normal em terra:** `cal-normal-taxi-terra`
- [ ] **Decolagem normal em terra:** `cal-normal-decolagem-terra`
- [ ] **Cruzeiro em terra:** `cal-normal-cruzeiro-terra`
- [ ] **Pouso normal em terra:** `cal-normal-pouso-terra`
- [ ] **Pouso firme em terra:** `cal-abuso-pouso-firme-terra`
- [ ] **Pouso duro em terra:** `cal-abuso-pouso-duro-terra`

### Voo Longo de Overstress (Motor Contínuo)

> [!WARNING]
> **Aviso de Segurança:** Realize este teste em altitude de segurança (pelo menos 3.000–5.000 ft AGL) e próximo a uma pista para pouso planado. O motor pode sofrer falha catastrófica ou apagar em voo quando `IS FAILED` for acionado.

Objetivo: manter o motor (39) continuamente em sobre-rotação/sobre-temperatura por 15 a 20 minutos (ou até quebrar) para identificar se `IS WEAK` e `IS FAILED` disparam no simulador e a que nível de desgaste.

- [ ] **Início do overstress longo:**
  - **Marcador:** `cal-longo-inicio`
  - **Ação:** Manter RPM acima da linha vermelha e potência máxima de forma sustentada e contínua.
- [ ] **Detecção de fraqueza no motor (se houver indicação/queda de rendimento):**
  - **Marcador:** `cal-longo-weak`
  - **Ação:** Marcar ao notar engasgo, vibração ou queda de pressão/rendimento.
- [ ] **Pane total do motor:**
  - **Marcador:** `cal-longo-pane`
  - **Ação:** Marcar no momento do corte/falha total do motor; planar e pousar com segurança.

### Comando de Análise de Calibração

Após os voos, execute a análise com a flag `--calibration`:

```powershell
py -3 tools/analyze_sensor_log.py --type A --model C152 --calibration "$env:LOCALAPPDATA\FlightCareer\sensor-logs\AAAAMMDD-HHMM-<ARQUIVO>.csv"
```

O script irá gerar:
1. `docs/plans/sensor-validation/A-C152.md` (relatório padrão de sensores).
2. `docs/plans/sensor-validation/calibration-C152.md` (relatório estruturado de calibração com limiares sugeridos e confirmação da razão integral/queda).

---

## 7. Teste Elétrico e Comandos (C172 G1000)

Roteiro de solo/pátio rápido (~5 minutos) para validar as novas SimVars elétricas (tensão/corrente dos barramentos, carga da bateria, alternador e chaves de aviônicos) e a deflexão de pedais do leme no **Cessna 172SP G1000**.

### Checklist do Teste Elétrico

- [ ] **1. Preparação:** Aeronave no pátio em Cold & Dark. Clicar em **REC** no HUD Overlay do FlightCareer para iniciar a gravação.
- [ ] **2. Bateria ON com Aviônicos ON (Motor Parado):**
  - **Ação:** Ligar a chave Master Battery (`BAT`) mantendo o motor desligado. Ligar ambas as chaves de aviônicos (`AVIONICS BUS 1` e `BUS 2`).
  - **Marcador:** `teste-bateria-avionics-on`
  - **Duração:** Manter por 2 minutos para registrar descarga da bateria sob carga dos aviônicos.
- [ ] **3. Aviônicos OFF (30 segundos):**
  - **Ação:** Desligar ambas as chaves de aviônicos (`AVIONICS BUS 1` e `BUS 2`), mantendo a bateria ligada.
  - **Marcador:** `teste-avionics-off`
  - **Duração:** Manter desligado por 30 segundos.
- [ ] **4. Partida do Motor:**
  - **Ação:** Religar os aviônicos se necessário pelo checklist, acionar a chave de ignição / starter e estabilizar o motor em marcha lenta (idle).
  - **Marcador:** `teste-partida`
  - **Duração:** ~30 segundos até estabilização de RPM e tensão do alternador.
- [ ] **5. Alternador OFF (60 segundos):**
  - **Ação:** Desligar a chave `ALT` do Master Switch (deixando `BAT` ligado).
  - **Marcador:** `teste-alternador-off`
  - **Duração:** Manter desligado por 60 segundos para registrar a transição de recarga para descarga no barramento principal.
- [ ] **6. Alternador ON:**
  - **Ação:** Religar a chave `ALT` do Master Switch.
  - **Marcador:** `teste-alternador-on`
  - **Duração:** ~30 segundos para observar retorno de tensão e pico de recarga.
- [ ] **7. Movimentação dos Pedais do Leme:**
  - **Ação:** Pressionar o pedal do leme totalmente para a esquerda por 3 segundos, depois totalmente para a direita por 3 segundos, e retornar ao centro.
  - **Marcador:** `teste-leme`
- [ ] **8. Finalizar:** Clicar em **REC** no HUD Overlay do FlightCareer para encerrar a gravação do log.

### Como Identificar a Função de Cada Barramento pelos Marcadores

Como o sistema elétrico modular v2 do MSFS 2024 indexa os barramentos por índice numérico global (`ELECTRICAL BUS VOLTAGE:1..5` e `ELECTRICAL BUS AMPS:1..5`), a correspondência física de cada barramento é identificada diretamente comparando o comportamento das colunas com as ações do teste:
1. **Barramento de Aviônicos (Avionics Bus):** Identificado pelo barramento que apresenta ~24 V em `teste-bateria-avionics-on` e **cai para 0,0 V e 0,0 A** no momento exato em que a chave de aviônicos é desligada (`teste-avionics-off`).
2. **Barramentos Principais (Main Bus 1 / Main Bus 2):** Identificados pelos barramentos que sobem para ~28 V com o motor e alternador ativos (`teste-partida`), caem para ~24 V quando o alternador é desligado (`teste-alternador-off`) e voltam a ~28 V quando o alternador é religado (`teste-alternador-on`).
3. **Barramento da Bateria (Battery Bus):** Barramento que permanece em ~24 V com motor desligado e reflete a tensão dos terminais da bateria em todas as etapas.

### O que Cada Coluna Deve Mostrar se Estiver Certa

| Coluna | Comportamento Esperado no Teste |
|---|---|
| `elec-bus-voltage-1` a `5` | ~24 V com bateria; sobem para ~28 V com alternador ON. O barramento de aviônicos **cai para 0,0 V** em `teste-avionics-off`. Os barramentos principais caem de ~28 V para ~24 V em `teste-alternador-off`. |
| `elec-bus-amps-1` a `5` | Corrente consumida por cada barramento. O barramento de aviônicos **vai a 0,0 A** em `teste-avionics-off`. |
| `elec-circuit-on-1` a `3` | `1` quando o circuito indexado estiver energizado; `0` se desligado/desenergizado. |
| `elec-battery-load-1` | No v2+, reporta a corrente de fornecimento da bateria: **positivo (> 0 A, ex: +10 a +25 A)** com motor parado ou alternador desligado (bateria suprindo o sistema); **0 A** (ou não fornecedora) quando o alternador está suprindo o sistema e recarregando. |
| `elec-battery-voltage-1` | No v2+, reporta a tensão nos terminais da bateria quando ela é fornecedora (~24 V a 25 V com motor parado ou alternador desligado); **0 V** quando o alternador é o fornecedor principal do sistema. |
| `elec-main-bus-voltage-1` | Datum de controle do sistema legado: ~24 V com bateria; ~28 V com alternador ativo. |
| `rudder-position` | Varia entre **-1,00** (leme totalmente à esquerda) e **+1,00** (totalmente à direita) em `teste-leme`. |


