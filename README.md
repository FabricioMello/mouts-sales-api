# Mouts Sales API

API desenvolvida para o teste de avaliacao de backend, com foco no dominio de vendas, aplicacao de regras de desconto, cancelamento logico e publicacao de eventos.

## Objetivo

O projeto implementa uma API para registro e consulta de vendas.

Uma venda possui numero, data, cliente, filial, itens, quantidades, precos, descontos, total por item, total da venda e status de cancelamento.

As principais regras de negocio sao:

- compras com menos de 4 unidades do mesmo produto nao recebem desconto;
- compras com 4 a 9 unidades do mesmo produto recebem 10% de desconto;
- compras com 10 a 20 unidades do mesmo produto recebem 20% de desconto;
- nao e permitido vender mais de 20 unidades do mesmo produto;
- uma venda nao pode ter mais de um item com o mesmo `ProductId`;
- venda cancelada nao e apagada fisicamente;
- item cancelado recalcula o total efetivo da venda;
- venda ja cancelada nao pode ser cancelada novamente.

## Decisoes de dominio

A venda foi tratada como um registro transacional. Por isso, a API nao possui `PUT` amplo nem `DELETE` fisico para vendas.

As alteracoes permitidas foram modeladas como operacoes explicitas:

- cancelar a venda inteira;
- cancelar um item especifico da venda.

Essa abordagem preserva historico e evita reescrever uma venda depois que ela foi criada.

## Estrutura do projeto

O backend esta em:

```text
template/backend
```

Principais projetos:

```text
src/Ambev.DeveloperEvaluation.Domain       Regras de dominio e entidades
src/Ambev.DeveloperEvaluation.Application  Casos de uso, validacoes e eventos
src/Ambev.DeveloperEvaluation.ORM          Persistencia com Entity Framework
src/Ambev.DeveloperEvaluation.WebApi       Controllers, configuracao HTTP e RabbitMQ
tests/Ambev.DeveloperEvaluation.Unit       Testes unitarios
tests/Ambev.DeveloperEvaluation.Integration Testes de integracao
```

## Tecnologias

- .NET 8
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- MediatR
- AutoMapper
- FluentValidation
- xUnit
- Testcontainers
- Docker Compose

## Como rodar com Docker

Pre-requisitos:

- Docker Desktop
- Git
- Postman ou Insomnia para testes manuais
- .NET SDK 8, apenas se quiser rodar testes localmente fora do Docker

Entre na pasta do backend:

```bash
cd template/backend
```

Suba a aplicacao e as dependencias:

```bash
docker compose up -d --build
```

A API ficara disponivel em:

```text
http://localhost:8080
```

Swagger:

```text
http://localhost:8080/swagger
```

RabbitMQ Management:

```text
http://localhost:15672
```

Credenciais do RabbitMQ:

```text
usuario: developer
senha: ev@luAt10n
```

Para parar o ambiente:

```bash
docker compose down
```

## Como testar manualmente

Foi criada uma collection Postman para facilitar os testes:

```text
template/backend/Ambev.DeveloperEvaluation.postman_collection.json
```

Importe esse arquivo no Postman ou Insomnia.

A collection possui as pastas:

- `Auth`, com login e captura automatica do token JWT;
- `Users`, com criacao, consulta e exclusao de usuario;
- `Sales`, com listagem, criacao, consulta, cancelamento de item e cancelamento de venda.

Fluxo sugerido:

1. Execute `Users > Create user`.
2. Execute `Auth > Login`.
3. Execute `Sales > Create sale`.
4. Execute `Sales > Get sale by id`.
5. Execute `Sales > Cancel sale item`.
6. Execute `Sales > Cancel sale`.

Apos o login, o token JWT e salvo na variavel `authToken` e usado automaticamente pelos demais requests da collection.

## Endpoints principais

Auth:

```text
POST /api/Auth
```

Users:

```text
POST   /api/Users
GET    /api/Users/{id}
DELETE /api/Users/{id}
```

Sales:

```text
GET   /api/Sales
GET   /api/Sales/{id}
POST  /api/Sales
PATCH /api/Sales/{id}/cancel
PATCH /api/Sales/{saleId}/items/{itemId}/cancel
```

## Eventos

A API publica eventos de venda via MediatR e RabbitMQ:

- `SaleCreatedEvent`
- `SaleCancelledEvent`
- `SaleItemCancelledEvent`

O evento `SaleModified` nao foi implementado porque a API nao possui update amplo de venda.

## Testes automatizados

Para rodar os testes localmente:

```bash
cd template/backend
dotnet test Ambev.DeveloperEvaluation.sln
```

A suite cobre regras de dominio, validadores, handlers de vendas e publicacao de eventos no RabbitMQ usando Testcontainers.
