# Knowledge Management System — Initial Architecture and Design Approach

> **Initial design record.** This document preserves the original architecture proposal and remains
> useful background. The living documentation index is [`docs/README.md`](docs/README.md); accepted
> ADRs and current reference documentation take precedence if this proposal becomes outdated.

## 1. Purpose

The goal is to build a multi-user knowledge management system designed primarily for use by both humans and AI agents.

The system will store articles, structured knowledge, decisions, rules, concepts, and other reusable information. Knowledge must support hierarchical organization while also allowing arbitrary cross-references between related items.

A key requirement is consistency: lower-level knowledge should remain aligned with higher-level knowledge, and linked knowledge items should remain semantically compatible with each other. The system should be able to identify potentially affected knowledge when something changes and, later, use AI to detect contradictions or outdated information.

AI agents must be able to work with the system through MCP tools and resources.

The system must support multiple users from the beginning. Multi-tenancy for multiple organizations should be anticipated in the data model, even if the first version only exposes personal or workspace-level isolation.

---

## 2. Core Design Principles

### 2.1 Knowledge is not just a collection of documents

The system should be modeled as a combination of:

- hierarchical knowledge organization;
- a graph of semantic relationships;
- versioned content;
- provenance and change history;
- search and retrieval;
- consistency and impact analysis.

A document or article is therefore only one representation of knowledge, not the entire data model.

### 2.2 Tree for organization, graph for meaning

Knowledge may be organized hierarchically:

```text
Software Architecture
└── Security
    └── Authentication
        └── Token Lifetime
```

At the same time, nodes may have semantic relationships that do not follow the hierarchy:

```text
Token Lifetime
    ├── depends_on ──> Session Policy
    ├── referenced_by -> API Security Guide
    └── supersedes ──> Previous Token Policy
```

The hierarchy is useful for browsing and contextual inheritance. Cross-links form a knowledge graph used for navigation, impact analysis, and consistency checking.

---

## 3. Knowledge Model

The core entity should be a `KnowledgeNode`.

Possible node types may include:

- Article
- Section
- Concept
- Rule
- Decision
- Fact
- Claim
- Reference

The first implementation does not need all of these types. The model should allow adding them later without changing the basic architecture.

### Main entities

```text
Workspace
User
Membership

KnowledgeNode
KnowledgeRevision
KnowledgeRelation

ChangeSet
ConsistencyReport
ConsistencyIssue
```

### KnowledgeNode

Represents stable identity and structural information.

Possible fields:

```text
Id
WorkspaceId
ParentId
Type
Path
CurrentRevisionId
Status
CreatedAt
CreatedBy
```

### KnowledgeRevision

Stores the actual versioned knowledge content.

```text
Id
NodeId
Version
Title
Content
CreatedAt
CreatedBy
Source
```

Content should initially be stored as Markdown.

### KnowledgeRelation

Represents cross-links and graph edges.

Example relation types:

```text
relates_to
depends_on
refines
implements
supersedes
contradicts
derived_from
example_of
```

Canonical relationships should be stored structurally in the database rather than existing only as Markdown links.

Markdown-style links such as `[[Authentication Policy]]` may later be parsed and synchronized with `KnowledgeRelation` records.

---

## 4. Versioning

Versioning should be part of the system from the beginning.

A `KnowledgeNode` keeps stable identity while each modification creates a new `KnowledgeRevision`.

This allows the system to answer questions such as:

- What is currently known?
- What changed?
- When did it change?
- Who or what changed it?
- Which knowledge depended on the previous version?
- Which AI-generated changes were applied?

Versioning is also important for safe AI-assisted updates and rollback.

---

## 5. Consistency Model

Consistency should be treated as a first-class system concept.

Two different types of consistency should be handled.

### 5.1 Structural consistency

Deterministic checks that can be performed without AI, for example:

- referenced nodes exist;
- forbidden cycles do not exist;
- relations are valid;
- workspace boundaries are respected;
- node status transitions are valid;
- revisions are sequential;
- self-references are rejected where inappropriate.

### 5.2 Semantic consistency

AI-assisted checks for cases such as:

```text
Parent policy:
Access tokens expire after 15 minutes.

Child document:
Access tokens are valid for one hour.
```

The system should detect such contradictions and create a consistency issue rather than silently modifying data.

Possible statuses:

```text
Consistent
NeedsReview
Conflict
Stale
Unknown
```

AI should initially propose changes rather than apply them automatically.

Recommended workflow:

```text
Knowledge Change
    ↓
Impact Analysis
    ↓
Deterministic Validation
    ↓
Semantic AI Validation
    ↓
Consistency Report
    ↓
Suggested Changes
    ↓
Review / Approval
```

---

## 6. Impact Analysis

Impact analysis should become one of the core capabilities of the system.

When a node changes, the system should determine potentially affected knowledge by traversing:

- ancestors;
- descendants;
- outgoing relations;
- incoming relations;
- dependencies;
- references;
- derived knowledge.

Example:

```text
Changed Node
   ├── Parent
   ├── Children
   ├── Dependencies
   ├── Referenced Nodes
   └── Referencing Nodes
```

The resulting `ImpactSet` should be passed to the consistency engine rather than sending the entire knowledge base to an LLM.

This keeps AI operations more relevant, predictable, and cost-efficient.

---

## 7. Technology Stack

### Backend

```text
.NET 10
ASP.NET Core
```

.NET was selected because the system is expected to develop a rich domain model involving:

- revisions;
- graph relationships;
- permissions;
- workspaces;
- consistency rules;
- change sets;
- transactional workflows;
- background processing;
- AI integrations.

The strong type system, mature runtime, tooling, PostgreSQL integration, and maintainability make it a better fit than Go for this particular domain-heavy application.

Go remains a strong option for infrastructure-oriented or small stateless services, but it does not provide enough advantage to justify using it for the main backend.

### Database

```text
Server / collaborative profile: PostgreSQL
Local / personal profile: SQLite
```

PostgreSQL will be the primary data store for hosted, multi-user, and production deployments.

SQLite should be supported as a first-class local profile for personal use, development, demos, and
simple self-contained installations. Local mode should run as a single application instance against
one database file. It is not expected to provide the same concurrent-write, operational, or tenant
isolation guarantees as the PostgreSQL profile.

Both profiles should preserve the same domain concepts, stable identifiers, revision semantics,
workspace ownership, application services, and HTTP/MCP contracts. Keep `WorkspaceId` in the local
schema even when the first local experience creates only one personal workspace. This keeps stored
knowledge portable and avoids creating a second domain model.

PostgreSQL can support:

- relational data;
- transactional consistency;
- hierarchical paths;
- graph edges;
- full-text search;
- vector search;
- multi-user and multi-tenant data isolation.

SQLite can support the knowledge core with:

- relational data and transactional revisions;
- foreign-key-enforced nodes and graph edges;
- adjacency-list hierarchy queries using recursive CTEs;
- keyword search using SQLite FTS5;
- a portable single-file local store.

Provider-specific behavior should live behind focused persistence and search implementations rather
than leaking database dialects into the domain. Do not reduce PostgreSQL capabilities to the lowest
common denominator merely to keep both profiles identical.

### Hierarchy

Use the parent relationship as the portable source of hierarchy truth. PostgreSQL `ltree` can be
considered as a derived optimization for efficient path queries. SQLite should use recursive CTEs
initially. Moves must keep any derived PostgreSQL path representation consistent with `ParentId`.

### Semantic Search

Use `pgvector` for semantic search in the PostgreSQL profile instead of introducing a separate vector
database initially.

The SQLite profile should expose semantic search only when a deliberate local vector implementation
is configured. Until then, it may provide keyword, hierarchy, and graph retrieval without advertising
vector similarity as available. Model this as an explicit storage/search capability so callers can
degrade predictably instead of discovering support through runtime failures.

Search should eventually be hybrid:

```text
Keyword / Full Text Search
        +
Vector Similarity
        +
Graph Relationships
        +
Hierarchy Context
        ↓
Ranking / Context Assembly
```

### Frontend

```text
React
TypeScript
```

### Deployment

```text
Local profile: single process + SQLite file
Server profile: Docker + PostgreSQL
```

The initial system should remain a modular monolith rather than starting with microservices.

Local mode is a deployment profile, not an offline replica of a hosted workspace. Bidirectional sync,
conflict resolution between local and server databases, and offline collaboration require a separate
design and are out of scope for the initial SQLite option. Export/import can provide portability first.

---

## 8. Multi-User and Multi-Tenant Design

The system should support workspaces from the first version.

Instead of attaching knowledge directly to a user:

```text
User -> Knowledge
```

use:

```text
User
  ↓
Membership
  ↓
Workspace
  ↓
Knowledge
```

Every major domain entity should include `WorkspaceId`.

Initially, the server profile should use:

```text
shared PostgreSQL database
shared schema
WorkspaceId column
```

The local SQLite profile should retain the same workspace-owned model even if it initially exposes a
single personal workspace. Authorization shortcuts used by a trusted single-user host must remain in
host configuration and must not remove workspace boundaries from the domain or persistence schema.

Later options may include:

- PostgreSQL Row Level Security;
- organization-level workspaces;
- database-per-tenant for enterprise isolation.

The first version does not need an explicit `Organization` entity, but the model should allow adding:

```text
Organization
    └── Workspace
```

without redesigning the knowledge model.

---

## 9. Application Architecture

The backend should use a modular monolith with feature-oriented organization.

Traditional horizontal Clean Architecture folders such as:

```text
Domain/
Application/
Infrastructure/
Presentation/
```

should not define the entire solution structure.

Instead, modules should be organized around business capabilities while preserving the same architectural dependency rules.

Recommended structure:

```text
src/
└── Knowledge.Server/

    Modules/

      Knowledge/
        Domain/
        Features/
        Presentation/
        Infrastructure/

      Search/
        Features/
        Infrastructure/

      Consistency/
        Domain/
        Features/
        Infrastructure/

      Workspaces/
        Domain/
        Features/
        Infrastructure/

    Infrastructure/
      Persistence/
      AI/
      Authentication/
      Observability/

    Common/

    Program.cs
```

---

## 10. Features as the Application Layer

In this architecture, the `Features` area effectively represents the Application layer.

The system does not need a separate top-level `Application` folder.

Conceptually:

```text
Domain
Features        <- Application / use cases
Presentation    <- HTTP and MCP adapters
Infrastructure
```

The distinction is organizational rather than architectural.

`Features` should contain application-level behavior and orchestration.

---

## 11. Application Services vs One Handler per Use Case

The system should not mechanically create one handler class per API operation.

Operations that belong naturally to the same concept can be grouped into a focused application service.

Example:

```text
Features/
  Nodes/
    NodeService.cs

  Relations/
    RelationService.cs

  Search/
    KnowledgeSearch.cs

  Consistency/
    ConsistencyChecker.cs

  Impact/
    ImpactAnalyzer.cs

  Context/
    ContextBuilder.cs
```

`NodeService` may initially expose:

```csharp
CreateAsync(...)
UpdateAsync(...)
GetAsync(...)
MoveAsync(...)
ArchiveAsync(...)
RestoreAsync(...)
```

This reduces unnecessary ceremony compared with classes such as:

```text
CreateNodeHandler
UpdateNodeHandler
MoveNodeHandler
ArchiveNodeHandler
RestoreNodeHandler
```

A separate class should be introduced when a use case becomes a substantial workflow with its own dependencies, rules, or lifecycle.

For example, a complex `UpdateNode` workflow may eventually perform:

```text
permission validation
load current revision
create revision
parse links
update graph
create change set
schedule embeddings
calculate impact
run consistency analysis
publish events
```

At that point it can be extracted into a dedicated use-case class without changing the external application service contract.

The architecture should therefore prefer simple services first and extract workflows when complexity justifies it.

---

## 12. Domain Layer Responsibility

The Domain layer contains the knowledge model and business invariants.

Examples:

```text
KnowledgeNode
KnowledgeRevision
KnowledgeRelation
RelationType
ConsistencyIssue
ChangeSet
```

The Domain layer is responsible for rules such as:

- a node cannot be its own parent;
- invalid relations cannot be created;
- archived nodes cannot be modified directly;
- revision ordering must remain valid;
- domain state transitions must be valid.

The Domain layer must not know about:

- HTTP;
- MCP;
- PostgreSQL;
- OpenAI or other LLM providers;
- the current authenticated transport;
- UI concerns.

---

## 13. Feature / Application Layer Responsibility

Application services coordinate use cases.

For example, `NodeService.UpdateAsync` may:

```text
1. Validate access permissions.
2. Load the node and current revision.
3. Invoke domain behavior.
4. Create a new revision.
5. Persist changes.
6. Update graph relationships.
7. Schedule embedding generation.
8. Trigger impact analysis.
```

Application services may depend directly on EF Core where appropriate.

A generic repository abstraction should not be introduced merely to hide EF Core.

Avoid boilerplate such as:

```text
IRepository<T>
Repository<T>
IUnitOfWork
```

unless there is a real domain-specific abstraction to protect.

A repository-like abstraction is justified when it encapsulates a meaningful operation, for example:

```text
IKnowledgeGraph.GetAffectedNodes(...)
```

rather than simply wrapping CRUD operations.

---

## 14. Presentation Layer

Presentation adapters should remain thin.

The two primary presentation mechanisms are expected to be:

```text
HTTP API
MCP
```

Both should invoke the same application services.

Conceptually:

```text
HTTP ─────┐
          ▼
      NodeService ──> Domain
          ▲
MCP ──────┘
```

This prevents business logic from being duplicated between API endpoints and MCP tools.

---

## 15. MCP Design

MCP is a first-class interface for AI agents.

The MCP layer should not expose only database-style CRUD operations.

Low-level operations may exist where useful:

```text
create_knowledge
update_knowledge
link_knowledge
```

but the more valuable MCP capabilities should be semantic operations such as:

```text
search_knowledge
get_knowledge
get_context
get_dependencies
get_related_knowledge
analyze_impact
validate_consistency
find_contradictions
suggest_related_knowledge
suggest_updates
```

For example:

```text
get_context(nodeId, depth)
```

may return:

```text
current node
important ancestors
relevant descendants
dependencies
references
related knowledge
semantically similar knowledge
```

This allows an AI agent to retrieve useful context with one meaningful operation rather than composing many low-level calls.

MCP Resources may also be exposed, for example:

```text
knowledge://{workspace}/{nodeId}
knowledge://{workspace}/architecture/security/authentication
```

MCP tools remain presentation adapters and should delegate to the same application services used by HTTP endpoints.

---

## 16. AI Integration

LLM integration should remain provider-independent.

Possible providers include:

```text
OpenAI
Anthropic
Gemini
local models
```

Most AI operations require only:

- model calls;
- embeddings;
- structured output;
- tool invocation;
- reranking or consistency evaluation.

There is no need to introduce Python into the main backend solely because the product uses AI.

If custom ML inference or specialized NLP pipelines are needed later, they can be implemented as a separate Python worker or service.

---

## 17. AI-Assisted Development Guidelines

Because much of the implementation is expected to be produced using AI coding tools, the architecture should optimize for clarity and low structural entropy.

Recommended rules:

```text
No generic repository abstraction without a real need.
No MediatR by default.
No unnecessary CQRS boilerplate.
No microservices at the beginning.
No unnecessary interfaces.
Prefer explicit code over framework magic.
Prefer focused application services.
Extract dedicated workflows only when complexity grows.
Keep module boundaries clear.
Keep domain invariants in the domain model.
Keep HTTP and MCP adapters thin.
```

This gives coding agents clear patterns to follow while keeping the resulting code easy to inspect, debug, and evolve.

---

## 18. Initial Project Structure

A practical first version could look like this:

```text
src/
└── Knowledge.Server/

    Modules/

      Knowledge/
        Domain/
          KnowledgeNode.cs
          KnowledgeRevision.cs
          KnowledgeRelation.cs
          RelationType.cs
          NodeType.cs

        Features/
          Nodes/
            NodeService.cs
            CreateNodeRequest.cs
            UpdateNodeRequest.cs
            NodeDto.cs

          Relations/
            RelationService.cs

          History/
            HistoryService.cs

        Presentation/
          Http/
            NodeEndpoints.cs
            RelationEndpoints.cs

          Mcp/
            KnowledgeTools.cs

        Infrastructure/
          EntityConfigurations.cs

      Search/
        Features/
          KnowledgeSearch.cs
          ContextBuilder.cs

        Infrastructure/
          PgVectorSearch.cs
          FullTextSearch.cs

      Consistency/
        Domain/
          ConsistencyIssue.cs
          ConsistencyReport.cs

        Features/
          ConsistencyChecker.cs
          ImpactAnalyzer.cs

        Infrastructure/
          LlmConsistencyAnalyzer.cs

      Workspaces/
        Domain/
        Features/
        Presentation/
        Infrastructure/

    Infrastructure/
      Persistence/
        KnowledgeDbContext.cs
        Migrations/

      AI/
        LlmClient.cs

      Authentication/
      Observability/

    Common/
      Result.cs
      Errors.cs
      Clock.cs

    Program.cs
```

The initial solution should remain small. There is no need to create separate assemblies such as:

```text
Knowledge.Domain
Knowledge.Application
Knowledge.Infrastructure
Knowledge.Api
```

from the beginning.

These can be introduced later if module size, ownership, build isolation, or deployment requirements justify them.

---

## 19. Suggested Implementation Phases

### Phase 1 — Knowledge Core

Implement:

```text
Users
Workspaces
KnowledgeNode
KnowledgeRevision
KnowledgeRelation
Hierarchy
Markdown content
HTTP API
Basic MCP access
Full-text search
SQLite local profile
PostgreSQL server profile
provider-specific migrations and integration tests
```

### Phase 2 — Semantic Retrieval

Add:

```text
pgvector for PostgreSQL
embeddings
hybrid search
related-knowledge suggestions
context building
explicit search capabilities for local mode
```

### Phase 3 — Consistency and Impact Analysis

Add:

```text
impact graph traversal
deterministic validation
semantic validation
consistency reports
contradiction detection
stale knowledge detection
```

### Phase 4 — AI-Assisted Knowledge Maintenance

Add:

```text
ChangeSet
suggested updates
multi-node update proposals
review / approval
AI-generated revisions
rollback and audit trail
```

---

## 20. Final Architectural Direction

The selected initial architecture is:

```text
.NET 10
ASP.NET Core
PostgreSQL server profile
SQLite local profile
pgvector for PostgreSQL
optional PostgreSQL ltree
React + TypeScript
Docker
MCP
Modular Monolith
Feature-oriented Application layer
```

The core architectural concepts are:

```text
Knowledge Graph
+ Hierarchy
+ Revisions
+ Workspaces
+ Impact Analysis
+ Consistency Engine
+ MCP Access
```

The physical code structure should be feature-oriented, while architectural dependency rules remain similar to Clean Architecture:

```text
Presentation
     ↓
Features / Application
     ↓
Domain

Infrastructure supports the above layers.
```

For simple entity-centric operations, focused application services such as `NodeService` and `RelationService` are preferred over one handler class per operation.

Complex workflows such as impact analysis, semantic consistency validation, imports, merges, and conflict resolution should be modeled as separate dedicated components when their complexity justifies it.

This approach keeps the first implementation simple while preserving a clear path toward a significantly more advanced AI-native knowledge platform.
