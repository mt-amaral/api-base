# CLAUDE.md — Diretrizes para desenvolvimento neste projeto

## Regras absolutas

- **Nomes de classes, métodos, variáveis, parâmetros, propriedades e tabelas em inglês**
- **Mensagens para o usuário (respostas da API e validações) em português**
- **Toda resposta da API é envolvida em `Response<T>` ou `PagedResponse<T>`** — nunca retornar entidade ou DTO solto
- **Nenhuma lógica de negócio na controller** — ela só repassa para o service e devolve `StatusCode(status, data)`
- **Nenhum comentário no código** — o código deve ser autoexplicativo (exceção: os comentários já existentes do `ConfigApp`)
- **Nunca expor entidade do EF na resposta** — sempre mapear para um `XxxResponseDto`
- **Todo método assíncrono recebe `CancellationToken ct` e repassa para o EF**
- **Nenhum `Console.WriteLine`** — use `ILogger<T>` (Serilog por baixo)

---

## Stack e ambiente

- .NET 10 / ASP.NET Core (controllers, não minimal API)
- EF Core 10 + Npgsql (PostgreSQL 17)
- ASP.NET Identity customizado com chave `long`
- Autenticação por cookie do Identity + refresh token em cookie `HttpOnly`
- Autorização por policies geradas a partir de `Permissions`
- Newtonsoft.Json (camelCase, `NullValueHandling.Ignore`)
- Swashbuckle (Swagger) com XML docs
- Serilog + OpenTelemetry (métricas Prometheus em `/metrics`, traces OTLP)
- xUnit para testes

---

## Estrutura do projeto

```
Api/
├── Configurations/
│   ├── ConfigApp.cs            # Parâmetros estáticos da aplicação
│   ├── Identity/               # Permissions, ClaimsTransformation
│   ├── Seed/                   # Seeds (IAppSeed), rodam só em Staging
│   └── Setup/                  # Extensões AddXxxSetup chamadas no Program
├── Context/                    # ApplicationDbContext
├── Controllers/
│   ├── BaseController.cs
│   └── V1/                     # Controllers versionados
├── Dto/
│   ├── Response.cs             # Response<T>
│   ├── PagedResponse.cs        # PagedResponse<T>
│   ├── PaginationRequestDto.cs
│   └── {Dominio}/              # Request/Response DTOs do domínio
├── Entities/
│   └── Identity/               # User, Role, RoleClaim
├── Mapping/{Area}/             # IEntityTypeConfiguration<T>
├── Middleware/                 # ExceptionMiddleware
├── Migrations/
└── Services/
    ├── Abstractions/           # Interfaces IXxxService
    └── XxxService.cs
Api.UnitTests/
└── Validators/                 # Testes de validação dos request DTOs
```

Novo domínio = DTOs em `Dto/{Dominio}/`, interface em `Services/Abstractions/`, service em `Services/`, controller em `Controllers/V1/`, entidade em `Entities/`, mapping em `Mapping/`, registro do service no `Program.cs`.

---

## Estilo de código

- **file-scoped namespace** (`namespace Api.Services;`) e namespace sempre declarado, seguindo a pasta
- **Primary constructor** para injeção de dependência em controllers e services
- **PascalCase** para classes, métodos e propriedades; **camelCase** para variáveis e parâmetros
- Métodos assíncronos com sufixo `Async`
- `CancellationToken` sempre chamado `ct`
- `var` quando o tipo é óbvio
- Nullable habilitado: tratar `null` explicitamente (`is null`, `?.`, `??`)
- Collection expression para listas vazias: `= [];`
- `ImplicitUsings` ligado — não repetir `using System;` e afins

---

## DTOs

- **Request** = `class` com DataAnnotations e `ErrorMessage` em português
- **Response** = `record` posicional
- Nome: `{Acao}{Dominio}RequestDto`, `{Dominio}ResponseDto`, `Filter{Dominio}RequestDto`
- Filtros de listagem herdam de `PaginationRequestDto` (`PageNumber`, `PageSize` 1–100) e usam `SearchString` para busca
- Strings de request inicializadas com `= string.Empty`
- Limites de tamanho do DTO batem com o `HasMaxLength` do mapping

```csharp
namespace Api.Dto.Product;

public class CreateProductRequestDto
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "CategoryId inválido.")]
    public long CategoryId { get; set; }
}

public record ProductResponseDto(long Id, string Name, long CategoryId);
```

---

## Padrão de resposta

- `Response<T>`: `data`, `message`, `errors`
- `PagedResponse<T>`: `Response<T>` + `currentPage`, `pageSize`, `totalCount`, `totalPages`
- Erros de validação do model state já saem como `Response<object?>` com `errors` (configurado no `Program.cs`)
- Mensagem de sucesso só em operações de escrita (`"Perfil criado com sucesso."`); leitura retorna `message` nulo

---

## Services

- Interface `IXxxService` em `Services/Abstractions/`, implementação em `Services/`
- Registrar como `AddScoped<IXxxService, XxxService>()` no `Program.cs`
- Retorno: `Task<(Response<T?>, short)>` ou `Task<(PagedResponse<List<T>?>, short)>` — o service decide o status HTTP
- Corpo inteiro em `try/catch`; no `catch` devolver `500` com mensagem genérica em português
- Validar existência antes de alterar (`404`) e regras de negócio (`400`)
- Leitura com `AsNoTracking()`
- Projeção direto para o DTO com `.Select(...)` em vez de carregar a entidade
- Paginação: `OrderBy` estável → `CountAsync` → `Skip((PageNumber - 1) * PageSize)` → `Take(PageSize)`
- Escritas que tocam mais de uma tabela (ou `UserManager` + `DbContext`) dentro de `context.Database.BeginTransactionAsync(ct)` + `CommitAsync(ct)`
- Usuário: sempre via `UserManager<User>`; usuário logado via `IUserLoggedService`
- Um perfil por usuário: toda troca de perfil passa por um único método (`ReplaceRoleAsync`)

```csharp
namespace Api.Services;

public class ProductService(ApplicationDbContext context) : IProductService
{
    public async Task<(Response<ProductResponseDto?>, short)> CreateAsync(CreateProductRequestDto request, CancellationToken ct)
    {
        try
        {
            var categoryExists = await context.Category
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.CategoryId, ct);

            if (!categoryExists)
                return (new Response<ProductResponseDto?>(null, "Categoria não encontrada."), 404);

            var product = new Product(request.Name.Trim(), request.CategoryId);

            await context.Product.AddAsync(product, ct);
            await context.SaveChangesAsync(ct);

            var response = new ProductResponseDto(product.Id, product.Name, product.CategoryId);

            return (new Response<ProductResponseDto?>(response, "Produto criado com sucesso."), 201);
        }
        catch
        {
            return (new Response<ProductResponseDto?>(null, "Erro ao criar produto!"), 500);
        }
    }
}
```

---

## Controllers

- Herdam de `BaseController` (`[ApiController]`)
- Rota fixa na classe: `[Route("api/v1/{recurso-no-plural}")]`
- Recursos REST: `GET` lista, `POST` cria, `PUT {id:long}` atualiza, `DELETE {id:long}` remove
- Sub-recurso: `api/v1/roles/{id:long}/claims`; usuário logado: `users/me`
- Todo endpoint exige login (filtro global `RequireAuthenticatedUser`); público só com `[AllowAnonymous]`
- Endpoint restrito com `[Authorize(Policy = Permissions.Xxx)]`
- `[FromBody]` para body, `[FromQuery]` para filtros, `CancellationToken ct` sempre no fim
- Corpo da action é sempre o mesmo — nada de `if`, transformação ou validação manual

```csharp
namespace Api.Controllers.V1;

[Route("api/v1/products")]
public class ProductController(IProductService productService) : BaseController
{
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsRegister)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request, CancellationToken ct)
    {
        var (data, status) = await productService.CreateAsync(request, ct);
        return StatusCode(status, data);
    }
}
```

**Errado — lógica na controller:**
```csharp
if (string.IsNullOrWhiteSpace(request.Name))
    return BadRequest("Nome obrigatório");
```

**Correto — validação no DTO, regra no service, controller só repassa.**

---

## Status codes

| Situação                                   | Status |
|--------------------------------------------|--------|
| Leitura, atualização, remoção com sucesso  | `200`  |
| Criação                                    | `201`  |
| Validação de DTO ou regra de negócio       | `400`  |
| Não autenticado                            | `401`  |
| Sem permissão (policy)                     | `403`  |
| Recurso não encontrado                     | `404`  |
| Rate limit                                 | `429`  |
| Erro inesperado                            | `500`  |

---

## Permissões

- Constante em `Configurations/Identity/Permissions.cs` no formato `"{modulo}.{recurso}.{acao}"` (ex.: `users.roles.update`)
- Ações: `view`, `register`, `update`, `delete`
- Toda constante nova entra também em `GetPermissions()` com descrição em português — a policy é criada automaticamente pelo `AuthorizationSetup` e o seed vincula ao Admin

```csharp
public const string ProductsView = "products.view";

new(ProductsView, Permission, "Pode visualizar produtos"),
```

---

## Entidades e banco

- Entidades do Identity em `Entities/Identity/`, herdando de `IdentityXxx<long>`
- Chave primária `long Id`
- Demais entidades em `Entities/`, com `private set` e métodos de domínio para mudar estado (`Revoke()`, `Refresh()`)
- Datas sempre UTC (`DateTime.UtcNow`) com sufixo `AtUtc` (`CreatedAtUtc`, `ExpiresAtUtc`)
- Configuração via Fluent API em `Mapping/{Area}/XxxMapping.cs` (`IEntityTypeConfiguration<T>`) — não usar DataAnnotations na entidade
- Tabela com nome em PascalCase singular (`ToTable("UserSession")`); tabelas do Identity com prefixo `Identity`
- Toda `string` com `HasMaxLength`
- Índice único com `HasIndex(...).IsUnique()` para campos normalizados
- `DbSet` no `ApplicationDbContext` com nome singular: `public DbSet<Product> Product => Set<Product>();`
- Mappings carregados por `ApplyConfigurationsFromAssembly` — basta criar a classe

---

## Migrations

- Nome sequencial: `V1`, `V2`, ... (próxima = maior `V` + 1)
- Nunca editar migration já aplicada; mudança nova = migration nova
- Depois de mexer em entidade/mapping, conferir com `has-pending-model-changes`
- Em Staging o `SeedSetup` aplica as migrations e roda os seeds na subida

---

## Configuração

- Parâmetros fixos da aplicação em `Configurations/ConfigApp.cs` (`public static`) com nome autoexplicativo
- Tempos de cookie e sessão vêm do `ConfigApp` — nunca valor fixo espalhado no código
- Connection string em `appsettings.{Ambiente}.json` (`ConnectionStrings:DefaultConnection`)
- Setup de infraestrutura como extensão em `Configurations/Setup/` (`AddXxxSetup` / `UseXxxSetup`) chamada no `Program.cs`

---

## Logging

- `ILogger<T>` injetado; Serilog configurado em `SerilogSetup` (console fora de produção, OTLP em produção)
- Message template estruturado, não interpolação: `logger.LogInformation("Usuário {UserId} criado", user.Id)`
- `LogError(ex, ...)` em falha inesperada

---

## Testes

- xUnit em `Api.UnitTests`, pasta por tipo (`Validators/`)
- Uma classe por domínio (`UserTests`), `#region` por DTO
- Nome: `{Classe}_{Cenario}_{Resultado}` em português — `CreateRequestDto_ComDadosValidos_DeveSerValido`
- Corpo em Arrange / Act / Assert (os marcadores `// Arrange`, `// Act`, `// Assert` são a única exceção à regra de comentários)
- Validação testada com `Validator.TryValidateObject` conferindo a `ErrorMessage` exata
- DTO novo com validação = testes novos

---

## Git

- Commits em português com prefixo: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `build:`
- Branch por tarefa: `feat/...`, `fix/...`, `chore/...`

---

## Comandos úteis

```bash
docker compose up -d postgres                                    # Postgres local (5432)
dotnet build Api.slnx                                            # Compilar
dotnet test                                                      # Testes
dotnet run --project Api --launch-profile https:Development      # Rodar (https 8091 / http 8090)
dotnet run --project Api --launch-profile FistStart:Staging      # Rodar com migrations + seed
dotnet tool install --global dotnet-ef                           # Ferramenta do EF (uma vez)
dotnet ef migrations add V7 --project Api                        # Nova migration
dotnet ef migrations has-pending-model-changes --project Api     # Modelo x migrations
dotnet ef database update --project Api                          # Aplicar migrations
```

Swagger em Development/Staging: `https://localhost:8091/swagger`
