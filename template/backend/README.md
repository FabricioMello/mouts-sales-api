# Backend - Ambev Developer Evaluation

Este README cobre a execucao local do backend em `template/backend`, incluindo Docker Compose, migrations do Entity Framework e comandos basicos de teste.

## Requisitos

- .NET SDK 8 ou superior
- Docker Desktop com Docker Compose
- Ferramenta do Entity Framework:

```powershell
dotnet tool install --global dotnet-ef
```

Se ela ja estiver instalada:

```powershell
dotnet tool update --global dotnet-ef
```

## Estrutura principal

- `Ambev.DeveloperEvaluation.sln`: solution do backend.
- `src/Ambev.DeveloperEvaluation.WebApi`: API ASP.NET Core.
- `src/Ambev.DeveloperEvaluation.ORM`: DbContext, mapeamentos e migrations EF Core.
- `tests/Ambev.DeveloperEvaluation.Unit`: testes unitarios com xUnit.
- `tests/Ambev.DeveloperEvaluation.Integration` e `tests/Ambev.DeveloperEvaluation.Functional`: projetos de teste existentes, ainda sem arquivos `.cs` de teste.
- `docker-compose.yml`: sobe PostgreSQL, MongoDB, Redis e a WebApi com portas fixas.

## Subir dependencias com Docker Compose

Entre na pasta do backend:

```powershell
cd template\backend
```

Para subir apenas a infraestrutura usada pela API:

```powershell
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql ambev.developerevaluation.cache
```

Servicos configurados no compose:

| Servico | Container | Porta no host | Credenciais |
| --- | --- | --- | --- |
| PostgreSQL | `ambev_developer_evaluation_database` | `5432` | database `developer_evaluation`, usuario `developer`, senha `ev@luAt10n` |
| MongoDB | `ambev_developer_evaluation_nosql` | `27017` | usuario `developer`, senha `ev@luAt10n` |
| Redis | `ambev_developer_evaluation_cache` | `6379` | senha `ev@luAt10n` |
| WebApi | `ambev_developer_evaluation_webapi` | `8080` | `http://localhost:8080` |

Para parar tudo:

```powershell
docker compose down
```

Para apagar tambem os volumes de dados criados pelos containers:

```powershell
docker compose down -v
```

## Configuracoes locais

O `appsettings.json` esta configurado para rodar a API fora do Docker, usando os bancos publicados nas portas fixas do host:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n"
  },
  "Api": {
    "BaseUrl": "http://localhost:5119",
    "ContainerBaseUrl": "http://localhost:8080"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://developer:ev%40luAt10n@localhost:27017",
    "DatabaseName": "developer_evaluation"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=ev@luAt10n"
  }
}
```

Se precisar sobrescrever a connection string sem editar arquivo, use user-secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n" --project src\Ambev.DeveloperEvaluation.WebApi
```

Tambem funciona por variavel de ambiente no PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n"
```

## Rodar migrations

Primeiro suba o PostgreSQL:

```powershell
docker compose up -d ambev.developerevaluation.database
```

As migrations sao aplicadas automaticamente sempre que a WebApi inicia. O Entity Framework registra as migrations aplicadas na tabela `__EFMigrationsHistory` e executa apenas as que ainda estiverem pendentes.

Se quiser aplicar manualmente sem subir a API, use:

```powershell
dotnet ef database update --project src\Ambev.DeveloperEvaluation.ORM\Ambev.DeveloperEvaluation.ORM.csproj --startup-project src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj
```

Para listar as migrations conhecidas pelo projeto:

```powershell
dotnet ef migrations list --project src\Ambev.DeveloperEvaluation.ORM\Ambev.DeveloperEvaluation.ORM.csproj --startup-project src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj
```

Para criar uma nova migration depois de alterar entidades ou mapeamentos:

```powershell
dotnet ef migrations add NomeDaMigration --project src\Ambev.DeveloperEvaluation.ORM\Ambev.DeveloperEvaluation.ORM.csproj --startup-project src\Ambev.DeveloperEvaluation.WebApi\Ambev.DeveloperEvaluation.WebApi.csproj
```

## Rodar a API localmente com dotnet

Com as dependencias de infraestrutura no Docker e a migration aplicada:

```powershell
dotnet run --project src\Ambev.DeveloperEvaluation.WebApi
```

Pelo `launchSettings.json`, a API local usa:

- HTTP: `http://localhost:5119`
- HTTPS: `https://localhost:7181`
- Swagger: `http://localhost:5119/swagger`
- Health check: `http://localhost:5119/health`

## Decisao de dominio sobre vendas

Embora o enunciado mencione CRUD completo, esta implementacao trata venda como registro transacional. Depois de criada, uma venda nao pode ser alterada ou removida fisicamente por um `PUT` ou `DELETE` generico, porque isso reescreveria historico de numero, cliente, filial, itens, precos e descontos.

As mudancas permitidas sao comandos explicitos de negocio:

- `PATCH /api/sales/{id}/cancel`: cancela a venda inteira e preserva seus valores historicos.
- `PATCH /api/sales/{saleId}/items/{itemId}/cancel`: cancela um item especifico e recalcula o total efetivo da venda com base nos itens ainda ativos.

Correcoes administrativas ou sistemicas, se necessarias em um produto real, deveriam ser modeladas como fluxos especificos e auditaveis, por exemplo endpoints de correcao com motivo e trilha de auditoria, e nao como update amplo da venda.

## Rodar a API pelo Docker Compose

O compose principal do backend ja aponta para o Dockerfile da WebApi:

```yaml
dockerfile: src/Ambev.DeveloperEvaluation.WebApi/Dockerfile
```

Ou seja, nao existe um `docker-compose.yml` separado dentro do projeto WebApi que precise ser rodado alem do compose principal do backend. O que existe dentro de `src/Ambev.DeveloperEvaluation.WebApi` e o `Dockerfile` usado pelo compose.

Para subir tudo pelo compose:

```powershell
docker compose up -d --build
```

O `docker-compose.yml` ja sobrescreve as configuracoes da API para usar os nomes dos servicos dentro da rede Docker:

```yaml
environment:
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Host=ambev_developer_evaluation_database;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n
  - MongoDb__ConnectionString=mongodb://developer:ev%40luAt10n@ambev_developer_evaluation_nosql:27017
  - MongoDb__DatabaseName=developer_evaluation
  - Redis__ConnectionString=ambev_developer_evaluation_cache:6379,password=ev@luAt10n
```

A URL da API em container fica:

```text
http://localhost:8080
```

Swagger em container:

```text
http://localhost:8080/swagger
```

## Build e testes

Restaurar pacotes:

```powershell
dotnet restore Ambev.DeveloperEvaluation.sln
```

Compilar:

```powershell
dotnet build Ambev.DeveloperEvaluation.sln
```

Rodar todos os testes:

```powershell
dotnet test Ambev.DeveloperEvaluation.sln
```

Rodar somente testes unitarios:

```powershell
dotnet test tests\Ambev.DeveloperEvaluation.Unit\Ambev.DeveloperEvaluation.Unit.csproj
```

Gerar cobertura dos testes unitarios:

```powershell
dotnet test tests\Ambev.DeveloperEvaluation.Unit\Ambev.DeveloperEvaluation.Unit.csproj /p:CollectCoverage=true
```

## Checklist para testar o projeto do zero

1. Subir PostgreSQL, MongoDB e Redis:

```powershell
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql ambev.developerevaluation.cache
```

2. Conferir se `ConnectionStrings:DefaultConnection` aponta para o PostgreSQL.
3. Rodar build e testes:

```powershell
dotnet build Ambev.DeveloperEvaluation.sln
dotnet test Ambev.DeveloperEvaluation.sln
```

4. Subir a API localmente. As migrations pendentes serao aplicadas automaticamente no startup:

```powershell
dotnet run --project src\Ambev.DeveloperEvaluation.WebApi
```

5. Abrir o Swagger:

```text
http://localhost:5119/swagger
```

Fluxo alternativo para API no Docker:

```powershell
docker compose up -d --build
```

```text
http://localhost:8080/swagger
```
