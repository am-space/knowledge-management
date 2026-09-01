# Conceptual database schema

This document describes the provider-neutral relational model. The Milestone 1 identity, workspace,
Article node, and revision subset is implemented through EF Core; later sections remain conceptual.

## Identity and tenancy

```text
User
  └── Membership ──> Workspace
                         └── tenant-owned knowledge
```

### User

```text
Id
DisplayName
CreatedAt
```

Authentication-specific fields should be modeled only after the authentication approach is chosen.

### Workspace

```text
Id
Name
CreatedAt
CreatedBy
```

### Membership

```text
WorkspaceId
UserId
Role
JoinedAt
```

The initial roles are expected to be owner, editor, and viewer. The exact permission matrix remains
to be specified before authorization is implemented.

## Knowledge

### KnowledgeNode

```text
Id
WorkspaceId
ParentId
Type
CurrentRevisionId
Status
CreatedAt
CreatedBy
```

The node carries stable identity and current structural state. A node cannot be its own parent;
hierarchy cycles and cross-workspace parents are invalid.

### KnowledgeRevision

```text
Id
WorkspaceId
NodeId
Version
Title
ContentMarkdown
Source
CreatedAt
CreatedBy
```

Revisions are immutable. `Version` is a positive integer beginning at 1 and increasing by exactly
one for each node. `(NodeId, Version)` must be unique. An update supplies the expected current
version; inserting the next revision and conditionally moving the current-revision pointer occur in
one transaction. A conflict or other rejected update commits neither change. See
[Knowledge application and HTTP contracts](knowledge-contracts.md) and
[ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md).

`Source` is reserved for the later provenance contract and is not stored by the Milestone 1 schema.
The implemented current-revision foreign key includes `(WorkspaceId, NodeId, Id)`, preventing a
node from pointing at another node's revision. `CurrentRevisionId` is nullable only as a relational
staging mechanism needed to insert the mutually referencing node and initial revision; persistence
sets it within the same transaction before commit.

### KnowledgeRelation

```text
Id
WorkspaceId
SourceNodeId
TargetNodeId
Type
CreatedAt
CreatedBy
```

Both endpoints must belong to the same workspace. Relation uniqueness, allowed self-relations, and
whether relation changes are versioned or audited remain explicit pre-implementation decisions.

## Derived and consistency data

Future records include embeddings, change sets, consistency reports, and consistency issues. Every
derived artifact must include the source revision identity and workspace scope needed to reject stale
or cross-workspace results.

## Provider behavior

| Capability | PostgreSQL server | SQLite local |
| --- | --- | --- |
| Hierarchy truth | `ParentId` | `ParentId` |
| Hierarchy optimization | Optional derived `ltree` path | Recursive CTE |
| Keyword search | PostgreSQL full-text search | FTS5 |
| Vector search | `pgvector` | Unavailable until explicitly implemented |
| Tenant isolation | Application checks; possible future RLS | Application checks in a single-process profile |
| Migrations | `PostgreSqlKnowledgeDbContext` history | `SqliteKnowledgeDbContext` history |

Database constraints and indexes must reinforce domain invariants on both providers. PostgreSQL must
not be reduced to SQLite's lowest common denominator.
