# DSC.Tech.Hypatia

**Lightweight Messaging and CQRS infrastructure for .NET**

## Why Hypatia?

The project is named after **Hypatia of Alexandria**, mathematician, astronomer,
philosopher, and one of the most remarkable scholars of late antiquity.

Hypatia dedicated her life to knowledge, teaching, mathematics, and scientific
thought. Her role in preserving, organizing, and transmitting knowledge makes
her an inspiration for this project.

Just as Hypatia connected ideas and knowledge, **DSC.Tech.Hypatia** connects
Commands and Queries to their respective Handlers through a small, explicit,
predictable, and testable messaging infrastructure.

## Purpose

DSC.Tech.Hypatia provides lightweight messaging and CQRS building blocks for
.NET applications.

Its goal is not to reproduce MediatR. The library intentionally implements a
small and explicit set of capabilities centered on Commands, Queries, Handlers,
dispatching, dependency injection, and cancellation.

The library is designed to be reusable independently of DSC.Tech code
generators or any specific application.

## Design Principles

DSC.Tech.Hypatia is designed around:

- SOLID
- Clean Code
- Clean Architecture
- CQRS
- Command Pattern
- Dependency Inversion
- Minimal dependencies
- Explicit behavior
- Testability

## Architecture

The initial architecture is organized around three responsibilities:

- **Abstractions** — public contracts for Commands, Queries, Handlers, and dispatching.
- **Dispatching** — runtime request-to-handler dispatch implementation.
- **DependencyInjection** — registration and integration with the .NET dependency injection container.

The public API will be kept intentionally small. New capabilities should only
be introduced when supported by a concrete use case.

## Quality and Testing

A release is not considered ready only because it compiles.

The project must validate, at minimum:

- Commands without results.
- Commands with results.
- Queries with results.
- Correct handler resolution.
- Dependency injection.
- CancellationToken propagation.
- Predictable behavior when a handler is missing.
- Handler isolation.
- Dependency injection scopes and lifetimes.
- Concurrent dispatch without unintended shared state.
- Release build without warnings.
- Unit tests.
- Integration tests.
- NuGet package generation.
- Consumption of the generated NuGet package by an external test application.
- No direct or transitive dependency on MediatR/LuckyPenny.

## NuGet

Package:

`DSC.Tech.Hypatia`

Initial development version:

`1.0.0`

During development, packages can be generated into a local NuGet feed.

## Status

DSC.Tech.Hypatia is currently under initial development.

The public API will be defined and tested before integration into consuming
projects.

## Architecture and Patterns

Hypatia deliberately keeps the messaging boundary small.

Patterns and principles used:

- **Command Pattern**: commands represent application intent.
- **CQRS**: commands and queries have distinct contracts and handlers.
- **Mediator/Dispatcher Pattern**: callers depend on `IMessageDispatcher`, not concrete handlers.
- **Dependency Injection**: handlers are resolved from the current .NET DI scope.
- **Dependency Inversion**: application code depends on abstractions.
- **Single Responsibility**: dispatcher routes; handlers execute use cases.
- **Open/Closed Principle**: adding a command/query and handler does not require changing the dispatcher.
- **Interface Segregation**: small command, query, handler, and dispatcher contracts.

### DDD Boundary

Hypatia supports DDD-oriented applications without owning the domain model. It does
not define Entity, Aggregate Root, Repository, Value Object, or domain-specific
business rules. Those belong to the consuming domain/application. Hypatia only
coordinates application messages.

## Usage

```csharp
services.AddHypatia(typeof(MyApplicationAssemblyMarker).Assembly);

await dispatcher.Send(new CreateCustomerCommand(...), cancellationToken);

var id = await dispatcher.Send<Guid>(
    new CreateCustomerWithResultCommand(...),
    cancellationToken);

var customer = await dispatcher.Query<CustomerDto?>(
    new GetCustomerByIdQuery(id),
    cancellationToken);
```

## Quality Gate

A Hypatia release must pass Release build, unit/integration tests, coverage
collection, NuGet pack, external NuGet consumer execution, and a dependency
scan proving zero MediatR/LuckyPenny references.
