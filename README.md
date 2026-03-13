# API - resumo rápido

API em ASP.NET Core com foco em autenticação usando **ASP.NET Identity**, **cookie auth** e **refresh token**.

A ideia do projeto é manter tudo simples, organizado e com um padrão bem definido:

- **DTOs com `record`** para entrada e saída
- **Response padrão** para quase todas as respostas da API
- **PagedResponse** para consultas paginadas
- **FluentValidation** para validar requests
- **Validação** na controller
- **Services** focados em regra de negócio
- **BaseController** para reaproveitar método, como de validação
- **Identity customizado** com entidades próprias e `long` como chave
- **Mappings do Identity** separados para controlar tamanho de campos, nomes de tabela e afins
- **ConfigApp** centralizando parâmetros estáticos da aplicação
- **ExceptionMiddleware** para capturar erro inesperado e devolver no padrão da API

## Como a autenticação funciona

A autenticação usa **cookie do Identity** como login principal.
Além disso, existe um **refresh token** salvo no banco e enviado por cookie `HttpOnly`.

Fluxo resumido:

1. Usuário faz login
2. API cria o cookie de autenticação
3. API gera e salva refresh token
4. Quando precisar renovar a sessão, usa o refresh token
5. No logout, encerra sessão e revoga o refresh token

## Padrão de retorno

A API trabalha com um objeto `Response<T>` simples:

- `data`
- `message`

Para paginação, usa `PagedResponse<T>` com:

- `data`
- `message`
- `currentPage`
- `pageSize`
- `totalCount`
- `totalPages`

## Padrão de validação

A validação segue esse fluxo:

1. Request chega no controller
2. O controller valida com FluentValidation
3. Se estiver inválido, já retorna `400`
4. Se estiver válido, chama o service

Isso evita jogar validação de entrada dentro da entidade ou do service sem necessidade.

## Seed inicial

Você pode usar esse endpoint para criar um usuário inicial:

- nome: `teste`
- email: `admin@teste.com`
- senha forte já pronta para teste

## Testes e Benchmark

A pasta tests/ concentra tudo relacionado a validação automática do projeto.

### Api.UnitTests (xUnit)

Objetivo: testar lógica isolada, sem pipeline HTTP, sem banco real e sem autenticação real.

Esse projeto tenta manter um estilo bem direto:
 - Validators (FluentValidation)
 - Helpers / Extensions

Rodar testes: 
**dotnet test**

### Api.Benchmarks

Objetivo: medir performance de trechos críticos (sem achismo), com resultados replicáveis.
O que faz sentido benchmarkar nesse projeto:
 - criação de Response<T> / PagedResponse<T>
 - geração de refresh token
 - serialização JSON
 - regras internas que podem virar gargalo

Rodar benchmark:
**dotnet run -c Release -p .\Api.Benchmarks\Api.Benchmarks.csproj**


## Observações

Esse projeto tenta manter um estilo bem direto:

- controller enxuto
- service com regra de negócio
- config centralizada
- retorno padronizado
- monolito significado
- código fácil de manter

## Banco
- docker run --name postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=postgres -p 5432:5432  -d postgres
