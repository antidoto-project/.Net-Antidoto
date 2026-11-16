# Antídoto API (.NET) — Global Solution 2026/2 · CitizenTech

API em **.NET 9 Minimal API** do Antídoto. Uma pessoa picada por um animal peçonhento descobre, **sem login**, a urgência do caso, o que fazer e qual unidade perto dela tem o soro certo. Com login, o cidadão registra a ocorrência, e a prefeitura (perfil `gestor`) vê onde os acidentes se concentram.

## Tecnologias

| Item | Uso |
| --- | --- |
| .NET 9 Minimal API (`MapGroup`) | Endpoints |
| EF Core + Oracle | Banco (Oracle da FIAP) |
| JWT Bearer + BCrypt | Autenticação e autorização (`cidadao` e `gestor`) |
| Rate Limiter nativo do ASP.NET | 4 políticas com resposta 429 |
| Health Checks | Liveness, readiness e startup |
| Serilog | Logs estruturados com enrichment |
| OpenTelemetry | Traces e métricas (Counter e Histogram) |
| Swagger (Swashbuckle) | Documentação e testes manuais |
| xUnit, Moq, FluentAssertions, WebApplicationFactory | Testes |

## Estrutura

```
Antidoto.slnx
├── Antidoto.API/
│   ├── Models/            Entidades e catálogo (grupos de animal, soros, urgências)
│   ├── DTOs/              Requests/responses com DataAnnotations
│   ├── Data/              AppDbContext, seed e migrations
│   ├── Services/          Regras de negócio (interface + implementação)
│   ├── Endpoints/         Grupos da Minimal API
│   ├── Filters/           ValidacaoFilter e IdempotenciaFilter
│   ├── HealthChecks/      Oracle, IA e startup
│   ├── Observabilidade/   ActivitySource e métricas próprias
│   ├── Configuracoes/     Rate limiting, Swagger e health checks
│   └── Program.cs
├── Antidoto.UnitTests/         Testes unitários (Moq, padrão AAA)
├── Antidoto.IntegrationTests/  Testes de integração (WebApplicationFactory + banco em memória)
└── docs/                       swagger.json, script SQL e resultado dos testes
```

Os endpoints só validam a entrada, chamam um service e devolvem um `TypedResults`. A regra de negócio fica nos services, que dependem de interfaces (SRP e DIP). Por isso dá para testar cada um com Moq.

## Como executar

Pré-requisito: SDK do .NET 9 (ou Visual Studio 2022 atualizado).

1. Abra `Antidoto.API/appsettings.json` e troque `SEU_RM` e `SUA_SENHA` pelo usuário e pela senha do Oracle da FIAP:

```json
"ConnectionStrings": {
  "Oracle": "User id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL"
}
```

2. Rode o projeto `Antidoto.API` (F5 no Visual Studio ou `dotnet run --launch-profile http`).

Ao subir, a API cria sozinha as tabelas `TB_ANT_*` e o seed no Oracle (aplica as migrations). O Swagger abre em **http://localhost:5150/**.

> **Avaliação acadêmica:** o usuário e a senha do banco e a chave do JWT ficam direto no `appsettings.json` para facilitar a correção. Em um sistema real, eles iriam para variáveis de ambiente ou para um cofre de segredos.

O SQL equivalente às migrations está em `docs/InitialCreate.sql`.

**Gestor do seed:** `gestor@antidoto.gov.br` / `Gestor@2026`. O seed também traz 15 unidades de exemplo em SP, com coordenadas aproximadas.

O serviço de IA (Python) é opcional. A URL fica em `Ia:BaseUrl` (padrão `http://localhost:8000`). Se ele estiver fora do ar, a triagem usa as regras locais.

## Endpoints

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
| GET | `/api/ocorrencias/minhas` | `cidadao` | `usuario` | 200, 401, 429 |
| GET | `/api/ocorrencias/{id}` | Dono ou `gestor` | `usuario` | 200, 401, 403, 404, 429 |
| GET | `/api/estatisticas/regiao` | `gestor` | `usuario` | 200, 401, 403, 429 |
| GET | `/health`, `/health/live`, `/health/startup`, `/health/ready` | Público | — | 200, 503 |

Valores aceitos:
- **grupoAnimal:** JARARACA, CASCAVEL, SURUCUCU, CORAL, ESCORPIAO, ARMADEIRA, ARANHA_MARROM, LAGARTA ou DESCONHECIDO.
- **soro:** BOTROPICO, CROTALICO, LAQUETICO, ELAPIDICO, ESCORPIONICO, ARACNIDICO, LOXOSCELICO ou LONOMICO.
- **urgencia:** BAIXA, MEDIA, ALTA ou CRITICA.

O roteiro completo da demonstração está em `Antidoto.API/Antidoto.API.http`.

## JWT

- O token expira em 60 minutos e carrega `sub` (id do usuário) e `role` (`cidadao` ou `gestor`).
- O registro público só cria `cidadao`. O `gestor` vem do seed.
- Sem token, a resposta é 401. Com token sem a role exigida, 403. Ocorrência de outra pessoa também dá 403 (só o dono ou um gestor podem ver).
- No Swagger, faça o login, clique em **Authorize** e cole só o token.

## Idempotência (`POST /api/ocorrencias`)

1. O header `Idempotency-Key` (UUID) é obrigatório. Sem ele, a resposta é **400**.
2. A chave vale por usuário e por 24 horas. Fica salva com o hash SHA-256 do corpo, o status e a resposta.
3. Mesma chave com o mesmo corpo devolve a resposta salva, com o mesmo id e o header `Idempotency-Replayed: true`. Nada é criado de novo.
4. Mesma chave com corpo diferente devolve **422**.
5. Mesma chave ainda em processamento devolve **409**.
6. Um índice único em (`Chave`, `UsuarioId`) na tabela `TB_ANT_CHAVE_IDEMPOTENCIA` garante isso mesmo com requisições simultâneas.

## Rate limiting

| Política | Chave | Algoritmo | Limite | Motivo |
| --- | --- | --- | --- | --- |
| `sos` | IP | Janela deslizante | 10/min | Cada chamada aciona a IA |
| `auth` | IP | Janela fixa | 5/min | Evita tentativa de senha em massa |
| `consulta` | IP | Token bucket | 60/min | Leitura barata, mas sem abuso |
| `usuario` | Id do usuário (`sub`) | Janela fixa | 30/min | Limite justo por pessoa logada |

Ao passar do limite, a resposta é **429** em ProblemDetails com o header `Retry-After`. Os limites ficam em `RateLimiting` no `appsettings.json`.

## Health checks

| Endpoint | Probe | Checks |
| --- | --- | --- |
| `/health/live` | Liveness: a aplicação está viva? | `api` |
| `/health/startup` | Startup: terminou de inicializar? | `startup` |
| `/health/ready` | Readiness: pronta para receber tráfego? | `oracle` (Unhealthy se cair) e `ia-antidoto` (Degraded se cair) |
| `/health` | Todos | Todos |

A IA fora do ar fica como **Degraded**, e não Unhealthy, porque o SOS continua funcionando pelas regras locais.

## Observabilidade

- **Logs:** Serilog com templates estruturados (`"Ocorrência {OcorrenciaId} criada pelo usuário {UsuarioId}"`, nunca interpolação) e enrichment de máquina, ambiente, thread, versão e RequestId. Os logs saem no console e em `logs/`.
- **Traces:** OpenTelemetry com ASP.NET Core, HttpClient (chamada à IA) e a Activity `Triagem` da própria API.
- **Métricas:** `antidoto_triagens_total` (Counter, com a tag `fonte`), `antidoto_ocorrencias_total` (Counter) e `antidoto_triagem_duracao` (Histogram, em ms), além das métricas de ASP.NET Core e de runtime. Em desenvolvimento, tudo sai no console.

## Testes

```bash
dotnet test
```

| Projeto | O que testa |
| --- | --- |
| `Antidoto.UnitTests` | `TriagemService` com Moq (IA ligada, IA fora do ar, banco fora, máximo de 3 unidades), regras locais de triagem, Haversine, regra de idempotência e o conteúdo do JWT |
| `Antidoto.IntegrationTests` | API em memória com `WebApplicationFactory`, EF Core InMemory e uma IA falsa, compartilhadas por `ICollectionFixture`. Cobre 401, 403, 400 com os campos, idempotência (mesmo id, 422, 400), 429 com `Retry-After`, `/health` Degraded com a IA parada e o Swagger |

Os testes seguem o padrão **AAA** e o nome `Metodo_Cenario_ResultadoEsperado`. O resultado da última execução está em `docs/resultado-testes.txt`.

O banco InMemory não respeita índice único. Por isso, o caso de duas requisições simultâneas com a mesma chave é conferido no Oracle, pelo arquivo `.http` e pelo SQL Developer.

## Equipe

| Nome | RM |
| --- | --- |
| | |
