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
