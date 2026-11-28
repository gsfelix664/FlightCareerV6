# SimVarProbe — Ferramenta de Sondagem e Gravação de SimVars

Ferramenta CLI do FlightCareer que conecta ao Microsoft Flight Simulator (MSFS), valida todas as SimVars do catálogo contra o simulador em execução (sem que variáveis rejeitadas desloquem os dados) e grava a telemetria contínua a cada 1 segundo em CSV.

---

## 1. Como Rodar

Execute a partir da raiz do repositório com o simulador aberto e aeronave carregada:

```bash
# Gravação completa (Fase A de sondagem + Fase B contínua até 'q'+Enter)
dotnet run --project src/FlightCareer.SimVarProbe

# Apenas validação de nomes (Fase A), gerando report.csv e summary.txt
dotnet run --project src/FlightCareer.SimVarProbe -- --probe-only

# Gravação com duração automática fixa de 60 segundos
dotnet run --project src/FlightCareer.SimVarProbe -- --duration 60

# Filtrando SimVars específicas por expressão regular
dotnet run --project src/FlightCareer.SimVarProbe -- --filter "^(GENERAL ENG|AIRSPEED)"

# Ver ajuda completa de argumentos
dotnet run --project src/FlightCareer.SimVarProbe -- --help
```

---

## 2. Roteiro de Teste Operacional

Para mapear e validar o comportamento das variáveis ao longo de um voo:

1. **Pátio (Cold & Dark):** com a aeronave estacionada e aviônicos/motor desligados, inicie o `SimVarProbe`.
2. Assim que a Fase B iniciar, digite:
   ```text
   m patio
   ```
   e pressione Enter para marcar o estado estático no log.
3. **Partida do motor:** ao iniciar o checklist de acionamento, digite:
   ```text
   m partida
   ```
4. **Táxi:** ao iniciar o rolamento para a pista:
   ```text
   m taxi
   ```
5. **Decolagem e Voo:** na corrida de decolagem e nivelamento:
   ```text
   m voo
   ```
6. **Pouso e Corte:** após pousar, taxiar e cortar o motor:
   ```text
   m pouso
   ```
7. **Finalizar:** digite:
   ```text
   q
   ```
   e pressione Enter (ou use `Ctrl+C`). O programa salvará os relatórios e exibirá o diretório de saída.

---

## 3. Estrutura dos Arquivos de Saída

Os arquivos são gravados em `%LocalAppData%\FlightCareer\probe-logs\<yyyyMMdd-HHmm-NOME_AERONAVE>\`:

- **`report.csv`**: inventário completo com o status final de cada SimVar testada:
  - `slot`: nome com índice (ex.: `GENERAL ENG RPM:1`).
  - `simvar`, `index`, `unit`, `isString`, `source`, `category`, `settable`.
  - `status`:
    - `AceitaVariando`: SimVar aceita pelo MSFS e com valores dinâmicos durante a sessão.
    - `AceitaConstante`: SimVar aceita, mas com valor inalterado (ex.: motor inativo ou recurso fixo).
    - `AceitaSemAmostras`: SimVar aceita na sondagem, mas sem leituras válidas (ou apenas NaN).
    - `Rejeitada`: nome não reconhecido pelo MSFS (`NAME_UNRECOGNIZED`). Nunca entra nos blocos de gravação.
    - `Erro`: erro ou exceção reportada pelo SimConnect.
    - `SemResposta`: timeout durante a sondagem inicial.
  - `samples`, `distinct`, `min`, `max`, `first`, `last`.
- **`summary.txt`**: sumário executivo com contagens por status, aeronave, duração e lista explícita de SimVars rejeitadas.
- **`values.csv`**: telemetria a cada 1 segundo das variáveis numéricas aceitas (`utc,sim_rate,marker,<slots...>`).
- **`strings.csv`**: registro cronológico de mudanças em variáveis textuais (`utc,slot,valor`).
