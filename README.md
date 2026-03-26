# API de autenticação .net 9.0 

API em ASP.NET Core com foco em autenticação usando **ASP.NET Identity**, **cookie auth** e **refresh token**.

A ideia do projeto é manter tudo simples, organizado e com um padrão bem definido, para servir como template para futuros projetos:

- **DTOs com `record`** para entrada e saída
- **Response padrão** para quase todas as respostas da API
- **PagedResponse** para consultas paginadas
- **Validação** na controller
- **Services** focados em regra de negócio
- **Identity customizado** com entidades próprias e `long` como chave
- **Mappings do Identity** separados para controlar tamanho de campos, nomes de tabela e afins
- **ConfigApp** centralizando parâmetros estáticos da aplicação
- **ExceptionMiddleware** para capturar erro inesperado e devolver no padrão da API

## Autenticação

A autenticação usa **cookie do Identity** como login principal.
Além disso, existe um **refresh token** salvo no banco e enviado por cookie `HttpOnly`.

Fluxo:

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

Mantenha sempre esse padrão! E envolva as respostas da API com ele.


## Seed inicial

Se você iniciar o projeto como Staging, ele irá gerar as migrations e popular um usuário de teste (Lembre-se de trocar o usuário/senha em produção).
Você pode usar esse endpoint para criar um usuário inicial:

- nome: `Admin`
- email: `admin@teste.com`
- Vai encontrar a senha do Admin senha dentro de ``Configurations/Seed/AdminUserSeed``


### Api.UnitTests (xUnit)

O teste está bem simples, só para garantir a validação dos requests das controllers (Input Model Validation), conforme as regras forem definidas
Seria interessante criar teste nos principais métodos de serviços.

Rodar testes: 
**dotnet test**

## Observações

o objetivo é ser simples e pratico:

- controller simples
- service com regra de negócio
- configurações centralizada
- retorno padronizado
- monolito significado

## Banco
- docker run --name postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=postgres -p 5432:5432  -d postgres
