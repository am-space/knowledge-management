# ADR-0001: Use a feature-oriented modular monolith

- **Status:** Accepted
- **Date:** 2026-08-29
- **Deciders:** Project maintainers

## Context

The system has a domain-rich knowledge model involving stable nodes, revisions, graph relations,
workspaces, permissions, search, background processing, consistency analysis, and HTTP/MCP delivery.
It needs clear boundaries without the operational and coordination cost of starting with distributed
services.

Horizontal solution layouts can make a feature span many assemblies and encourage boilerplate.
Mechanical CQRS handlers, generic repositories, and interfaces for every class would add structure
without yet protecting a real boundary.

## Decision

Build the backend as a feature-oriented modular monolith in one primary ASP.NET Core application
assembly.

Organize business capabilities into Knowledge, Workspaces, Search, and Consistency modules. Within a
module, use Domain, Features/Application, Presentation, and Infrastructure folders only as needed.
Dependencies flow from Presentation through Features to Domain; Infrastructure implements technical
capabilities without leaking provider types inward.

Use focused application services for cohesive entity-centric operations. Extract a dedicated
workflow when its behavior, dependencies, or lifecycle becomes substantial.

Do not introduce microservices, MediatR, generic repositories, mechanical one-handler-per-operation
CQRS, or separate horizontal layer assemblies without a demonstrated need.

## Consequences

- Features remain discoverable and can be changed as vertical slices.
- HTTP and MCP adapters reuse the same application behavior.
- Transactions and cross-module workflows remain straightforward initially.
- Module boundaries depend on disciplined ownership and selected architecture tests rather than
  process isolation.
- Additional assemblies or services may be introduced later for real deployment, provider,
  ownership, build, or scaling boundaries.

## Alternatives considered

- **Microservices:** rejected initially because they add distributed transactions, deployment,
  observability, and compatibility work before those costs solve a demonstrated problem.
- **Separate Domain/Application/Infrastructure/API assemblies:** deferred because the initial system
  benefits more from feature locality than from horizontal build isolation.
- **MediatR and one handler per operation:** rejected as a default because focused services provide a
  simpler starting point and workflows can be extracted when complexity appears.

## Related documentation

- [Architecture](../architecture.md)
- [Initial system design](../../knowledge-management-system-design.md)

