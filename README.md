# Mouts Sales API

API developed for the backend evaluation challenge, focused on the sales domain, discount rules, logical cancellation, authentication, automated tests, and sales event publishing.

## Goal

This project implements an API for creating, querying, and cancelling sales.

A sale contains number, date, customer, branch, items, quantities, prices, discounts, item totals, sale total, and cancellation status.

Main business rules:

- purchases with fewer than 4 units of the same product do not receive a discount;
- purchases with 4 to 9 units of the same product receive a 10% discount;
- purchases with 10 to 20 units of the same product receive a 20% discount;
- it is not possible to sell more than 20 units of the same product;
- a sale cannot contain more than one item with the same `ProductId`;
- a cancelled sale is not physically deleted;
- cancelling an item recalculates the effective sale total;
- an already cancelled sale cannot be cancelled again.

## Domain Decisions

Sales were modeled as transactional records. Because of that, the API does not expose a broad `PUT` endpoint or a physical `DELETE` endpoint for sales.

Allowed changes are explicit business operations:

- cancel the whole sale;
- cancel a specific sale item.

This approach preserves history and avoids rewriting a sale after it has been created.

## Project Structure

The backend is located at:

```text
template/backend
```

Main projects:

```text
src/Ambev.DeveloperEvaluation.Domain        Domain rules and entities
src/Ambev.DeveloperEvaluation.Application   Use cases, validations, and events
src/Ambev.DeveloperEvaluation.ORM           Entity Framework persistence
src/Ambev.DeveloperEvaluation.WebApi        Controllers, HTTP setup, and RabbitMQ integration
tests/Ambev.DeveloperEvaluation.Unit        Unit tests
tests/Ambev.DeveloperEvaluation.Integration Integration tests with Testcontainers
tests/Ambev.DeveloperEvaluation.Functional  HTTP functional tests with WebApplicationFactory
```

## Technologies

- .NET 8
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- MediatR
- AutoMapper
- FluentValidation
- Serilog
- xUnit
- Testcontainers
- Docker Compose

## Running With Docker

Prerequisites:

- Docker Desktop
- Git
- Postman or Insomnia for manual testing
- .NET SDK 8, only if you want to run tests locally outside Docker

Go to the backend folder:

```bash
cd template/backend
```

Start the API and dependencies:

```bash
docker compose up -d --build
```

The API will be available at:

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

RabbitMQ credentials:

```text
username: developer
password: ev@luAt10n
```

To stop the environment:

```bash
docker compose down
```

To run everything from a clean Docker state:

```bash
docker compose down -v --remove-orphans
docker compose up -d --build
```

## Manual Testing

A Postman collection is available at:

```text
template/backend/Ambev.DeveloperEvaluation.postman_collection.json
```

Import this file into Postman or Insomnia.

The collection contains:

- `Auth`, with login and automatic JWT token capture;
- `Users`, with user creation, retrieval, and deletion;
- `Sales`, with sale listing, creation, retrieval, item cancellation, and sale cancellation.

Suggested flow:

1. Run `Users > Create user`.
2. Run `Auth > Login`.
3. Run `Sales > Create sale`.
4. Run `Sales > Get sale by id`.
5. Run `Sales > Cancel sale item`.
6. Run `Sales > Cancel sale`.

After login, the JWT token is stored in the `authToken` variable and automatically used by requests that inherit authorization from the collection.

## Main Endpoints

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

## Events

The API creates sales events through MediatR and persists them through a transactional outbox before publishing them to RabbitMQ.

Implemented events:

- `SaleCreatedEvent`
- `SaleCancelledEvent`
- `SaleItemCancelledEvent`

`SaleModifiedEvent` was not implemented because the API does not expose a broad sale update operation.

## Automated Tests

To run the automated tests locally:

```bash
cd template/backend
dotnet test Ambev.DeveloperEvaluation.sln
```

The test suite covers:

- domain rules and discount calculations;
- validators and MediatR handlers;
- API authorization behavior;
- global exception handling;
- sales repository behavior against PostgreSQL;
- outbox persistence;
- RabbitMQ publishing and resilience;
- HTTP flows through functional tests.

Current validated result:

```text
Unit: 109
Functional: 19
Integration: 20
Total: 148 tests passing
```
