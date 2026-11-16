# Antídoto API (.NET) — Global Solution 2026/2 · CitizenTech

API em **.NET 9 Minimal API** que ajuda pessoas picadas por animais peçonhentos (cobras, escorpiões, aranhas e lagartas) a agir rápido e chegar à unidade de saúde que tem o soro certo.

---

## 1. O problema e a solução

No acidente com um animal peçonhento, o tempo até o soro faz diferença. Muitas vezes a pessoa não sabe:

- se o caso é grave;
- o que fazer (e o que **não** fazer) enquanto busca ajuda;
- qual hospital perto dela tem o soro para aquele animal (nem toda unidade tem todos os soros).

A Antídoto API responde essas três perguntas em uma chamada, **sem exigir login**. Ela também guarda as ocorrências dos cidadãos cadastrados para que a prefeitura veja onde e quando os acidentes acontecem.

---

## 2. Como a aplicação funciona

### Quem usa

| Perfil | Como entra | O que pode fazer |
| --- | --- | --- |
| **Qualquer pessoa** | Sem login | Fazer a triagem SOS e buscar unidades com soro |
| **Cidadão** (`cidadao`) | Cria a conta em `/api/auth/registrar` | Tudo acima, mais registrar ocorrências e ver as suas |
| **Gestor** (`gestor`) | Conta criada pelo seed (prefeitura) | Ver qualquer ocorrência, cadastrar e editar unidades e ver estatísticas |

### Fluxo 1 — SOS (público)

```
Pessoa informa: animal + sintomas + localização
        │
        ▼
POST /api/sos/triagem
        │
        ├─► Tenta o serviço Antídoto IA (Python)
        │       └─ se a IA estiver fora do ar ou demorar → usa as REGRAS LOCAIS
        │
        ├─► Define: urgência (BAIXA, MEDIA, ALTA, CRITICA), soro e orientação
        │
        └─► Busca as 3 unidades mais próximas que têm aquele soro
        │
        ▼
Resposta: urgência, soro, orientação, fonte ("ia" ou "regras") e unidades com distância em km
```

**Regras locais** (usadas quando a IA não responde):

- **Soro:** sai do animal. Por exemplo, jararaca → botrópico, cascavel → crotálico, escorpião → escorpiônico.
- **Urgência:**
  - qualquer **sinal grave** (falta de ar, visão dupla, pálpebra caída, urina escura, sangramento, desmaio ou vômitos) deixa o caso **CRÍTICO** e a orientação manda ligar para o 192;
  - sem sinal grave, serpentes são **ALTA** e escorpiões, aranhas e lagartas são **MEDIA**.

O SOS nunca deixa a pessoa sem orientação: se a IA cair, valem as regras. Se o banco cair, a resposta sai sem a lista de unidades, mas com a urgência e o que fazer.

### Fluxo 2 — Busca de unidades (público)

`GET /api/unidades?soro=BOTROPICO&lat=-23.55&lng=-46.63&raioKm=50`

A API filtra as unidades que têm o soro, calcula a distância até cada uma pela **fórmula de Haversine**, descarta as que ficam fora do raio e ordena da mais próxima para a mais distante.

### Fluxo 3 — Registro de ocorrência (cidadão logado)

```
Cidadão faz login → recebe um token JWT
        │
        ▼
POST /api/ocorrencias  (token + header Idempotency-Key)
        │
        ├─► Valida os dados (400 se inválidos)
        ├─► Confere a Idempotency-Key:
        │       nova                   → cria a ocorrência (201)
        │       repetida, mesmo corpo  → devolve a mesma resposta, sem duplicar
        │       repetida, outro corpo  → 422
        │       ainda processando      → 409
        ▼
Ocorrência salva no Oracle
```

A **idempotência** existe porque, em uma emergência e com internet ruim, o celular pode reenviar o mesmo pedido. Com a mesma chave, a ocorrência nunca é duplicada.

### Fluxo 4 — Painel da prefeitura (gestor)

- `GET /api/estatisticas/regiao`: total de ocorrências, por tipo de animal e por mês.
- `POST` e `PUT /api/unidades`: cadastra ou atualiza unidades e os soros que cada uma tem.
- `GET /api/ocorrencias/{id}`: o gestor vê qualquer ocorrência; o cidadão vê só as dele.

### Caminho de uma requisição dentro da API

```
Requisição
   │
   ▼
Autenticação JWT ──► sem token / token inválido → 401
   │
Autorização (role) ──► role errada → 403
   │
Rate limiting ──► passou do limite → 429 + Retry-After
   │
ValidacaoFilter ──► dados inválidos → 400 com os campos
   │
IdempotenciaFilter (só no POST de ocorrências)
   │
Endpoint ──► Service (regra de negócio) ──► AppDbContext (Oracle) / IaClient (IA)
   │
   ▼
Resposta (TypedResults) ou erro em ProblemDetails
```

---

## 3. Tecnologias

| Item | Uso |
| --- | --- |
| .NET 9 Minimal API (`MapGroup`) | Endpoints |
| EF Core + Oracle | Banco de dados (Oracle da FIAP) |
| JWT Bearer + BCrypt | Login, senhas e perfis `cidadao` e `gestor` |
| Rate Limiter do ASP.NET Core | Limite de requisições com resposta 429 |
| Health Checks | Liveness, startup e readiness |
| Serilog | Logs estruturados |
| OpenTelemetry | Traces e métricas |
| Swagger (Swashbuckle) | Documentação e testes pelo navegador |
| xUnit, Moq, FluentAssertions, WebApplicationFactory | Testes unitários e de integração |

---

## 4. Estrutura do projeto

```
.Net Antidoto/
├── Antidoto.slnx                   Solução (abrir no Visual Studio)
├── Antidoto.API/
│   ├── Program.cs                  Configuração e pipeline da API
│   ├── appsettings.json            Banco, JWT, IA e limites
│   ├── Antidoto.API.http           Roteiro de demonstração
│   ├── Models/                     Entidades (Usuario, Unidade, Ocorrencia, ChaveIdempotencia) e catálogo
│   ├── DTOs/                       Dados de entrada e saída, com validações
│   ├── Data/                       AppDbContext, seed e migrations
│   ├── Services/                   Regras de negócio (Auth, Triagem, Unidade, Ocorrencia, Idempotencia, IA)
│   ├── Endpoints/                  Rotas da Minimal API, agrupadas por assunto
│   ├── Filters/                    Validação e idempotência
│   ├── HealthChecks/               Oracle, IA e startup
│   ├── Observabilidade/            Traces e métricas próprias
│   └── Configuracoes/              Rate limiting, Swagger e health checks
├── Antidoto.UnitTests/             Testes unitários
├── Antidoto.IntegrationTests/      Testes de integração
└── docs/                           swagger.json, script SQL e resultado dos testes
```

Os **endpoints** só recebem a requisição, chamam um **service** e devolvem a resposta. Toda regra de negócio fica nos services, que são usados por meio de interfaces. Isso segue o SRP e o DIP e permite testar cada service com Moq.

---

## 5. Como rodar

### Pré-requisitos

- **.NET 9 SDK** (confira com `dotnet --list-sdks`) ou **Visual Studio 2022** atualizado.
- Usuário e senha do **Oracle da FIAP**.

### Passo 1 — Configurar o banco

Abra `Antidoto.API/appsettings.json` e troque `SEU_RM` e `SUA_SENHA`:

```json
"ConnectionStrings": {
  "Oracle": "User id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL"
}
```

> **Avaliação acadêmica:** o usuário e a senha do banco e a chave do JWT ficam direto no `appsettings.json` para facilitar a correção. Em um sistema real, eles iriam para variáveis de ambiente ou para um cofre de segredos.

### Passo 2 — Rodar a API

**No Visual Studio:** abra `Antidoto.slnx`, escolha o projeto **Antidoto.API** e o perfil **http**, e aperte **F5**.

**No terminal:**

```bash
cd Antidoto.API
dotnet run --launch-profile http
```

Ao subir, a API **cria sozinha as tabelas e o seed** no Oracle (aplica as migrations). Não é preciso rodar `Update-Database`. O SQL equivalente está em `docs/InitialCreate.sql`.

### Passo 3 — Conferir se subiu

| O que abrir | Esperado |
| --- | --- |
| http://localhost:5150/ | Tela do Swagger |
| http://localhost:5150/health/ready | `oracle` como `Healthy` |
| SQL Developer, com o mesmo usuário | Tabelas `TB_ANT_USUARIO`, `TB_ANT_UNIDADE`, `TB_ANT_OCORRENCIA` e `TB_ANT_CHAVE_IDEMPOTENCIA` |

No SQL Developer, os nomes das colunas usam maiúsculas e minúsculas, então vão entre aspas: `SELECT "Nome", "Soros" FROM TB_ANT_UNIDADE;`

### Passo 4 — Testar pelo Swagger

1. **SOS sem login:** abra `POST /api/sos/triagem` → **Try it out** e envie:
   ```json
   { "grupoAnimal": "CASCAVEL", "sintomas": ["DOR_LOCAL", "VISAO_DUPLA"], "latitude": -23.5503, "longitude": -46.6339 }
   ```
   O retorno mostra urgência `CRITICA`, soro `CROTALICO`, `fonte: "regras"` (se a IA não estiver rodando) e as unidades mais próximas.

2. **Criar uma conta:** em `POST /api/auth/registrar`, envie:
   ```json
   { "nome": "Maria Silva", "email": "maria@email.com", "senha": "senha123" }
   ```

3. **Fazer login:** em `POST /api/auth/login`, envie o mesmo e-mail e senha e copie o `token` da resposta.

4. **Autorizar:** clique em **Authorize** (cadeado no topo), cole **só o token** e confirme.

5. **Registrar uma ocorrência:** em `POST /api/ocorrencias`, preencha o campo **Idempotency-Key** com um UUID (por exemplo `3f1c8e2a-1111-4c3b-9a55-000000000001`) e envie:
   ```json
   { "grupoAnimal": "JARARACA", "urgencia": "ALTA", "latitude": -23.55, "longitude": -46.63 }
   ```
   Envie de novo com a mesma chave: volta o **mesmo id**, com o header `Idempotency-Replayed: true`.

6. **Entrar como gestor:** faça login com `gestor@antidoto.gov.br` / `Gestor@2026`, autorize com o novo token e chame `GET /api/estatisticas/regiao`.

### Testar pelo Postman ou pelo VS Code

O arquivo `Antidoto.API/Antidoto.API.http` tem um roteiro de **16 passos**, com os casos de sucesso e de erro (401, 403, 400, 422, 429). No Visual Studio ou no VS Code (extensão REST Client), é só clicar em **Send Request** em cada passo. No Postman, copie as mesmas URLs, headers e corpos.

### Serviço de IA (opcional)

A API chama o serviço Antídoto IA (Python) em `Ia:BaseUrl` (padrão `http://localhost:8000`, rota `POST /triagem`). Ele **não é obrigatório**: sem a IA, a triagem usa as regras locais e o `/health` mostra `Degraded`.

### Problemas comuns

| Sintoma | Causa provável |
| --- | --- |
| `/health/ready` com `oracle` Unhealthy | RM ou senha errados no `appsettings.json`, ou sem acesso à rede da FIAP |
| Log diz "Não foi possível conectar no Oracle" | Mesmo caso acima. A API sobe, mas os endpoints que usam o banco falham |
| 401 em rota protegida | Faltou o token ou ele expirou (dura 60 minutos). Faça login de novo |
| 403 | O usuário não tem o perfil exigido (por exemplo, cidadão em rota de gestor) |
| 429 | Limite de requisições atingido. Espere os segundos indicados em `Retry-After` |

---

## 6. Endpoints

| Método | Rota | Acesso | Limite | Retornos |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/registrar` | Público | `auth` | 201, 400, 409, 429 |
| POST | `/api/auth/login` | Público | `auth` | 200, 400, 401, 429 |
| POST | `/api/sos/triagem` | Público | `sos` | 200, 400, 429 |
| GET | `/api/unidades?soro=&lat=&lng=&raioKm=` | Público | `consulta` | 200, 400, 429 |
| GET | `/api/unidades/{id}` | Público | `consulta` | 200, 404, 429 |
| POST | `/api/unidades` | `gestor` | `usuario` | 201, 400, 401, 403, 429 |
| PUT | `/api/unidades/{id}` | `gestor` | `usuario` | 200, 400, 401, 403, 404, 429 |
| POST | `/api/ocorrencias` | `cidadao` | `usuario` | 201, 400, 401, 403, 409, 422, 429 |
| GET | `/api/ocorrencias/minhas?pagina=&tamanho=` | `cidadao` | `usuario` | 200, 401, 429 |
| GET | `/api/ocorrencias/{id}` | Dono ou `gestor` | `usuario` | 200, 401, 403, 404, 429 |
| GET | `/api/estatisticas/regiao` | `gestor` | `usuario` | 200, 401, 403, 429 |
| GET | `/health`, `/health/live`, `/health/startup`, `/health/ready` | Público | — | 200, 503 |

**Valores aceitos**

| Campo | Valores |
| --- | --- |
| `grupoAnimal` | JARARACA, CASCAVEL, SURUCUCU, CORAL, ESCORPIAO, ARMADEIRA, ARANHA_MARROM, LAGARTA, DESCONHECIDO |
| `soro` | BOTROPICO, CROTALICO, LAQUETICO, ELAPIDICO, ESCORPIONICO, ARACNIDICO, LOXOSCELICO, LONOMICO |
| `urgencia` | BAIXA, MEDIA, ALTA, CRITICA |
| `sintomas` (sinais graves) | FALTA_DE_AR, VISAO_DUPLA, PALPEBRA_CAIDA, URINA_ESCURA, SANGRAMENTO, DESMAIO, VOMITOS |

Todos os erros saem no formato **ProblemDetails** (RFC 7807). Os erros de validação (400) listam cada campo com problema.

---

## 7. Requisitos técnicos

### Autenticação e autorização (JWT)

- O token expira em 60 minutos e carrega `sub` (id do usuário) e `role` (`cidadao` ou `gestor`).
- O registro público só cria `cidadao`. O `gestor` vem do seed.
- As senhas são guardadas com hash **BCrypt**.
- Sem token, a resposta é **401**. Com o perfil errado, **403**. Ocorrência de outra pessoa também dá **403**.

### Idempotência (`POST /api/ocorrencias`)

1. O header `Idempotency-Key` (UUID) é obrigatório. Sem ele, a resposta é **400**.
2. A chave vale por usuário e por 24 horas. Fica salva com o hash SHA-256 do corpo, o status e a resposta.
3. Mesma chave com o mesmo corpo devolve a resposta salva, com o mesmo id e o header `Idempotency-Replayed: true`.
4. Mesma chave com corpo diferente devolve **422**.
5. Mesma chave ainda em processamento devolve **409**.
6. Um índice único em (`Chave`, `UsuarioId`) garante isso mesmo com duas requisições ao mesmo tempo.

### Rate limiting

| Política | Separado por | Algoritmo | Limite | Motivo |
| --- | --- | --- | --- | --- |
| `sos` | IP | Janela deslizante | 10/min | Cada chamada aciona a IA, que é cara |
| `auth` | IP | Janela fixa | 5/min | Evita tentativa de senha em massa |
| `consulta` | IP | Token bucket | 60/min | Leitura barata, mas sem abuso |
| `usuario` | Usuário logado | Janela fixa | 30/min | Limite justo por pessoa |

Ao passar do limite, a resposta é **429** com o header `Retry-After`. Os limites ficam na seção `RateLimiting` do `appsettings.json`.

### Health checks

| Endpoint | Pergunta que responde | Checks |
| --- | --- | --- |
| `/health/live` | A aplicação está viva? | `api` |
| `/health/startup` | Terminou de inicializar? | `startup` |
| `/health/ready` | Está pronta para receber tráfego? | `oracle` (Unhealthy se cair) e `ia-antidoto` (Degraded se cair) |
| `/health` | Visão geral | Todos |

A IA fora do ar deixa o status **Degraded**, e não Unhealthy, porque o SOS continua funcionando pelas regras locais.

### Observabilidade

- **Logs (o que aconteceu?):** Serilog com templates estruturados (`"Ocorrência {OcorrenciaId} criada pelo usuário {UsuarioId}"`, sem interpolação de string) e dados extras de máquina, ambiente, thread, versão e RequestId. Os logs saem no console e na pasta `logs/`.
- **Traces (por onde passou?):** OpenTelemetry com as requisições HTTP, a chamada à IA e a etapa `Triagem`.
- **Métricas (como o sistema está?):**
  - `antidoto_triagens_total`: Counter, com a tag `fonte` (ia ou regras);
  - `antidoto_ocorrencias_total`: Counter;
  - `antidoto_triagem_duracao`: Histogram, em ms;
  - métricas do ASP.NET Core e do runtime.

---

## 8. Testes

```bash
dotnet test
```

| Projeto | O que testa |
| --- | --- |
| `Antidoto.UnitTests` (25 testes) | `TriagemService` com Moq (IA ligada, IA fora do ar, banco fora, no máximo 3 unidades), regras de triagem, distância por Haversine, regras de idempotência e o conteúdo do JWT |
| `Antidoto.IntegrationTests` (28 testes) | API em memória com `WebApplicationFactory`, EF Core InMemory e uma IA falsa, compartilhadas entre as classes por `ICollectionFixture`. Cobre 401, 403, 400 com os campos, idempotência (mesmo id, 422 e 400), 429 com `Retry-After`, `/health` Degraded com a IA parada e o Swagger |

Os testes seguem o padrão **AAA** (Arrange, Act, Assert) e o nome `Metodo_Cenario_ResultadoEsperado`. Por exemplo: `CriarOcorrencia_MesmaChaveDuasVezes_RetornaMesmoIdECriaUmaVez`.

Os testes não usam o Oracle, então rodam em qualquer máquina. O resultado da última execução está em `docs/resultado-testes.txt`.

O banco InMemory não respeita índice único. Por isso, o caso de duas requisições simultâneas com a mesma chave é conferido no Oracle, pelo arquivo `.http` e pelo SQL Developer.

---

## 9. Equipe

| Nome | RM |
| --- | --- |
| | |
| | |
| | |
| | |
