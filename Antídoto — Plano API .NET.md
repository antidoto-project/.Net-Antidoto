# Antídoto — Plano API .NET

Nov 10, 2026 · @Afetto

## Visão geral

A API `.Net-Antidoto` entrega o SOS, a busca de unidades com soro e o registro de ocorrências do Antídoto. Ela usa o serviço Antídoto IA (Python) como serviço externo. A base é o `.Net-Afetto`, com a mesma Clean Architecture, middleware de erros, JWT, Serilog, health checks e testes, mais três mudanças exigidas pela GS.

**Valor para o cidadão:** sem login, a pessoa picada descobre a urgência, o que fazer e qual unidade perto dela tem o soro certo. Com login, ela registra a ocorrência e a prefeitura (perfil `gestor`) vê onde os acidentes se concentram.

| Item | .Net-Afetto | .Net-Antidoto |
| --- | --- | --- |
| Estilo da API | Controllers | **Minimal API** com `MapGroup` (obrigatório na GS) |
| Banco | Oracle (EF Core) + MongoDB (auditoria) | **Oracle (EF Core)**: o mesmo Oracle da FIAP do Afetto, consultado pelo SQL Developer; sai o MongoDB |
| Validação | DataAnnotations automáticas dos controllers | DataAnnotations por um **filtro de endpoint** (Minimal API não valida sozinha no .NET 9) |
| Idempotência | Não tem | **Nova**: header `Idempotency-Key` |
| Rate limiting | Não tem | **Novo**: `AddRateLimiter` com 429 |
| Health checks | API, Oracle, Mongo | API, Oracle e **serviço de IA** |
| Mantém igual | — | Camadas, `Extensions/`, `ExceptionHandlingMiddleware` (ProblemDetails), `TokenService`, BCrypt, Serilog, Swashbuckle, xUnit + Moq + FluentAssertions, `ArchitectureTests` |

**Decisão em aberto (confirmar com o professor antes da Fase 2):** o enunciado diz que esta API é a mesma consumida pelo app de Mobile, e o grupo tinha decidido que o app consome só a API Java. Ou o app passa a usar o .NET para SOS, unidades e ocorrências, ou o professor libera a API .NET como entrega separada.

## Regras do projeto

Estas regras definem o comportamento da API e viram casos de teste. Qualquer mudança nelas exige teste atualizado.

### Endpoints e acesso

| Método | Rota | Acesso | Limite | Retornos |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/registrar` | Público | `auth` | 201, 400, 409, 429 |
| POST | `/api/auth/login` | Público | `auth` | 200, 400, 401, 429 |
| POST | `/api/sos/triagem` | Público | `sos` | 200, 400, 429 |
| GET | `/api/unidades?soro=&lat=&lng=&raioKm=` | Público | `consulta` | 200, 400, 429 |
| GET | `/api/unidades/{id}` | Público | `consulta` | 200, 404 |
| POST | `/api/ocorrencias` | `cidadao` | `usuario` | 201, 400, 401, 409, 422, 429 |
| GET | `/api/ocorrencias/minhas` | `cidadao` | `usuario` | 200, 401 |
| GET | `/api/ocorrencias/{id}` | Dono ou `gestor` | `usuario` | 200, 401, 403, 404 |
| POST / PUT | `/api/unidades` | `gestor` | `usuario` | 201/200, 400, 401, 403 |
| GET | `/api/estatisticas/regiao` | `gestor` | `usuario` | 200, 401, 403 |
| GET | `/health`, `/health/live`, `/health/ready` | Público | sem limite | 200, 503 |

### JWT

- Mesmo `TokenService` do Afetto, com `Issuer`, `Audience`, `Key` ≥ 32 caracteres e expiração de 60 minutos.
- O token carrega `sub` (id do usuário) e `role` (`cidadao` ou `gestor`). O registro público só cria `cidadao`; o `gestor` vem do seed.
- Sem token = 401. Token válido sem a role = 403. Ocorrência de outra pessoa = 403 (dono ou gestor apenas).
- A chave nunca vai para o Git: `user-secrets` ou variável `Jwt__Key`.

### Idempotência (POST /api/ocorrencias)

1. O header `Idempotency-Key` (UUID) é obrigatório. Sem ele = 400.
2. A chave vale por usuário e por 24 horas. Fica salva com o hash do corpo, o status e a resposta.
3. Mesma chave + mesmo corpo = devolve a resposta salva, com o mesmo id, e o header `Idempotency-Replayed: true`. Nada é criado de novo.
4. Mesma chave + corpo diferente = 422.
5. Mesma chave ainda em processamento = 409.
6. Um índice único em (`Chave`, `UsuarioId`) garante isso mesmo com duas requisições ao mesmo tempo.

### Rate limiting

| Política | Chave | Algoritmo | Limite sugerido | Motivo |
| --- | --- | --- | --- | --- |
| `sos` | IP | Janela deslizante | 10 por minuto | Cada chamada aciona visão + LLM, que são caros |
| `auth` | IP | Janela fixa | 5 por minuto | Evita tentativa de senha em massa |
| `consulta` | IP | Token bucket | 60 por minuto | Leitura barata, mas sem abuso |
| `usuario` | id do usuário (`sub`) | Janela fixa | 30 por minuto | Limite justo por pessoa logada |

Ao estourar: 429 em ProblemDetails com o header `Retry-After`. Os limites ficam no `appsettings.json` para os testes usarem valores menores.

### Health checks

- `api`: tag `live`, sempre Healthy se a API responde.
- `oracle`: tag `ready`, `OracleHealthCheck` copiado do Afetto. Unhealthy se cair, porque sem banco nada funciona.
- `ia-antidoto`: tag `ready`, `GET {Ia:BaseUrl}/health` com timeout de 3 s. **Degraded** se cair, porque o SOS continua pelo fallback.

### Serviço de IA e fallback

- Interface `IIaClient` em Application, implementada por um `HttpClient` tipado em Infrastructure, com timeout de 15 s.
- IA fora do ar ou lenta = `TriagemService` decide pelas regras locais e a resposta sai com `fonte: "regras"`. O SOS nunca devolve 500.
- As regras locais usam o mesmo `casos-triagem.json` do Python e são testadas com os mesmos casos.

### Código e forma de trabalho

- Mesmas camadas e nomes do `.Net-Afetto`: Domain, Application, Infrastructure, `Endpoints/` no lugar de `Controllers/`, registro em `Extensions/`.
- Endpoint não tem regra de negócio: valida, chama o service e devolve `TypedResults`.
- Todo endpoint declara no Swagger nome, resumo, corpo e todas as respostas possíveis.
- Biblioteca nova só com acordo do time. Commits sem atribuição ao Claude.
- **Pronto = endpoint + teste unitário da regra + teste de integração do endpoint + Swagger completo.**

## Começo — Fase 1: base da solução

A Fase 1 termina com a API subindo, o Swagger abrindo na raiz e o banco criado por migration. Nenhum endpoint de negócio antes disso.

1. **Repositório `.Net-Antidoto`** com a mesma organização do `.Net-Afetto`:

```
.Net-Antidoto/
├── Antidoto-.Net.slnx
├── Antidoto-.Net/
│   ├── Domain/            # Entities, Interfaces, Filters
│   ├── Application/       # DTOs, Interfaces, Services
│   ├── Infrastructure/    # Persistence, Repositories, Security, HealthChecks, Ia
│   ├── Endpoints/         # AuthEndpoints, SosEndpoints, UnidadeEndpoints, OcorrenciaEndpoints
│   ├── Filters/           # ValidacaoFilter, IdempotenciaFilter
│   ├── Middleware/        # ExceptionHandlingMiddleware
│   ├── Extensions/        # AddApplication, AddInfrastructure, AddRateLimiting...
│   └── Program.cs
├── Antidoto-.Net.Tests.Unit/
├── Antidoto-.Net.Tests.Integration/
└── README.md
```

2. **Copiar do Afetto, sem mudar:** `ExceptionHandlingMiddleware`, `TokenService` + `JwtSettings`, `HttpCurrentUserService`, configuração do Serilog, `ApiHealthCheck`, `ArchitectureTests` e a `WebApplicationFactory` (com EF Core InMemory no lugar do Oracle, como no Afetto).
3. **Todos os projetos em `net9.0`** (no Afetto o projeto de testes unitários usava `net10.0` e obrigava a ter dois SDKs instalados).
4. **Entidades:** `Usuario` (com `Role`), `Unidade` (nome, endereço, telefone, latitude, longitude), `SoroDisponivel` (unidade + tipo de soro), `Ocorrencia` (usuário, data, grupo do animal, urgência, latitude, longitude, status), `ChaveIdempotencia`.
5. **EF Core + Oracle:** pacote `Oracle.EntityFrameworkCore` (o mesmo do Afetto), `AppDbContext` e migration `InitialCreate` aplicada com `Update-Database`. A connection string (`User id=SEU_RM;Password=SUA_SENHA;Data Source=host:1521/SERVICO`) fica só em `user-secrets`. Depois da migration, abrir o SQL Developer com o mesmo usuário e conferir as tabelas, o índice único de `ChaveIdempotencia` e o seed.
6. **Seed:** um usuário `gestor`, de 10 a 20 unidades de exemplo com soros e `casos-triagem.json` (o mesmo do repositório de IA).
7. **Configuração:** `appsettings.json` sem segredos, com as seções `Jwt`, `Ia:BaseUrl`, `RateLimiting` e `ConnectionStrings`. Segredos por `user-secrets`.

**Critério de saída da Fase 1:** `dotnet run` sobe a API, o Swagger abre em `http://localhost:5150/`, `/health/ready` mostra o Oracle Healthy, as tabelas aparecem no SQL Developer e o `ArchitectureTests` passa.

## Meio — Fase 2: endpoints e recursos

A ordem vai do que as outras partes usam para o que depende delas. Cada passo termina com os testes dele passando.

&#91;embedded content: caminho de uma requisição na API .NET\]

Cada barreira tem a sua resposta de erro. A idempotência só se aplica ao `POST /api/ocorrencias`, e o fallback mantém o SOS funcionando sem a IA.

1. **Filtro de validação.** `ValidacaoFilter<T>` (`IEndpointFilter`) roda as DataAnnotations do DTO e devolve `TypedResults.ValidationProblem` (400). Aplicado com `.AddEndpointFilter<ValidacaoFilter<T>>()` em todo endpoint com corpo.
2. **Auth.** `AuthEndpoints` com `MapGroup("/api/auth")`: registrar (BCrypt, 409 se o e-mail já existe) e login. Políticas `cidadao` e `gestor` com `AddAuthorization`.
3. **Unidades.** Busca por soro e distância: fórmula de Haversine em C# no service, depois ordenação por distância. Validações: latitude entre −90 e 90, longitude entre −180 e 180, raio de 1 a 100 km, soro dentro da lista conhecida.
4. **Cliente de IA + Triagem.** `IIaClient` e `IaHttpClient` (`AddHttpClient` com timeout). `TriagemService` chama a IA e, se ela falhar, decide pelas regras locais. `POST /api/sos/triagem` devolve urgência, soro, orientação, fonte (`ia` ou `regras`) e as 3 unidades mais próximas com aquele soro.
5. **Ocorrências + idempotência.** `IdempotenciaFilter` lê o header, calcula o SHA-256 do corpo, consulta `ChaveIdempotencia` e aplica as regras 1 a 6. O `OcorrenciaService` só é chamado na primeira vez. Depois vêm `/minhas` (paginado como no Afetto) e `/{id}` (dono ou gestor).
6. **Gestão e estatísticas.** CRUD de unidades e soros para `gestor` e contagem de ocorrências por grupo de animal e por mês em `/api/estatisticas/regiao`.
7. **Rate limiting.** `AddRateLimiter` em `Extensions/` com as 4 políticas, `OnRejected` escrevendo ProblemDetails + `Retry-After`, e `.RequireRateLimiting("sos")` etc. nos grupos.
8. **Health checks.** `OracleHealthCheck` (Unhealthy, copiado do Afetto) + `IaHealthCheck` (Degraded) + `ApiHealthCheck`, com o mesmo `ResponseWriter` JSON do Afetto em `/health`.
9. **Swagger.** Swashbuckle como no Afetto, com botão Authorize (Bearer). Em cada endpoint: `.WithName()`, `.WithSummary()`, `.Accepts<T>()`, `.Produces<T>(200/201)`, `.ProducesProblem(401/403/404/409/422/429)` e `.ProducesValidationProblem()`. Um `OperationFilter` documenta o header `Idempotency-Key`. O JSON exportado fica em `docs/swagger.json`.

**Critério de saída da Fase 2:** pelo Swagger dá para registrar, logar, fazer triagem com a IA ligada e desligada, criar uma ocorrência duas vezes com a mesma chave (mesmo id), estourar o limite (429) e ver `/health` como Degraded com a IA parada.

## Fim — Fase 3: testes e entrega

A Fase 3 prova cada regra com um teste. Os testes de integração usam a `WebApplicationFactory` do Afetto com EF Core InMemory no lugar do Oracle e um `IIaClient` falso, que pode ser configurado para responder ou falhar. O InMemory não respeita índice único, então o caso de duas requisições simultâneas com a mesma chave é conferido no Oracle, pelo arquivo `.http` e pelo SQL Developer.

### Testes unitários (regras de negócio)

| Alvo | Casos |
| --- | --- |
| `TriagemService` | Todos os casos de `casos-triagem.json`; sinal grave sempre "crítico"; IA falhando cai nas regras com `fonte = regras` |
| `IdempotenciaService` | Chave nova processa; repetida devolve a salva; corpo diferente = conflito; chave expirada processa de novo |
| `UnidadeService` | Distância por Haversine; filtro por soro; ordenação; raio fora do intervalo rejeitado |
| `OcorrenciaService` / `AuthService` | Validações, e-mail duplicado, login inválido |
| `ArchitectureTests` | Domain não depende de ninguém; endpoints não acessam o `DbContext` direto |

### Testes de integração (endpoints)

- [ ] Rota protegida sem token = 401; `cidadao` em rota de `gestor` = 403; ocorrência de outro usuário = 403
- [ ] Corpo inválido = 400 com os campos do erro
- [ ] `POST /api/ocorrencias` sem `Idempotency-Key` = 400
- [ ] Mesma chave duas vezes = mesmo id, uma linha só no banco e `Idempotency-Replayed: true`
- [ ] Mesma chave com outro corpo = 422
- [ ] Passar do limite = 429 com `Retry-After`
- [ ] IA falsa fora do ar: triagem responde 200 com `fonte = regras` e `/health` mostra Degraded
- [ ] `/swagger/v1/swagger.json` responde 200

### Entrega

- [ ] README no padrão do Afetto: visão geral, arquitetura (diagrama), tecnologias, como executar, configuração, JWT, endpoints, idempotência, rate limiting, health checks, testes, equipe
- [ ] `dotnet test` passando, com o resultado salvo em `docs/`
- [ ] `appsettings.json` sem o usuário e a senha do Oracle (só em `user-secrets` ou variável de ambiente)
- [ ] Arquivo `.http` com o roteiro de demonstração (como o `Afetto-.Net.http`)
- [ ] Arquivo `.txt` de identificação com RM, nomes e link do repositório

## Checklist de pontuação

Cada critério aponta para a parte do plano que o atende. A API depende do Oracle da FIAP: testem a conexão pelo SQL Developer e pelo `/health/ready` antes da apresentação. Sem a IA, ela continua funcionando pelo fallback.

| Critério | Pts | Onde é atendido |
| --- | --- | --- |
| 01 Minimal API, validações, códigos HTTP, Swagger | 20 | Grupos em `Endpoints/`, `ValidacaoFilter`, tabela de retornos, passo 9 da Fase 2 |
| 02 JWT, autenticação e autorização | 20 | Roles `cidadao` e `gestor`, regra de dono, testes 401/403 |
| 03 Idempotência | 15 | `IdempotenciaFilter` + tabela `ChaveIdempotencia` com índice único |
| 04 Rate limiting com 429 | 15 | 4 políticas + `OnRejected` com `Retry-After` |
| 05 Testes xUnit | 20 | Tabelas da Fase 3 |
| 06 Health checks (API, banco, serviços externos) | 10 | `api`, `oracle` e `ia-antidoto` |

Penalidades a evitar: API que não executa (−20), código desorganizado ou credenciais expostas (−20), falta do `.txt` (−100), solução fora do tema CitizenTech (−100), entrega fora do prazo (−30).
