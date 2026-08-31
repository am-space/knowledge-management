# docs/ — Documentation guidance

Keep [`README.md`](README.md) current when adding, moving, renaming, or removing documentation.

## Document types

| Kind | Location | Purpose |
| --- | --- | --- |
| Reference | `docs/*.md` | Living description of the current agreed system direction or shipped behavior |
| ADR | `docs/adr/NNNN-*.md` | Durable record of an accepted architectural decision |
| Backlog plan | `docs/backlog/*-plan.md` | Shaped multi-issue work without an implementation commitment |
| Active plan | `docs/*-plan.md` | Approved or active implementation work |
| Archived plan | `docs/archive/*-plan.md` | Historical plan retained after implementation |

Reference documentation must describe the current state. Update it in the same change that modifies
the behavior, contract, architecture, schema, operations, or security assumptions it covers.

## Diagrams

- Use a diagram when it makes a relationship, hierarchy, lifecycle, or multi-step interaction
  materially easier to understand than prose or a small table.
- Prefer a fenced Mermaid diagram over ASCII art, an embedded raster image, or a separate
  hand-maintained diagram format when Mermaid can express the content clearly.
- Choose the smallest suitable Mermaid diagram type, such as a flowchart, sequence diagram, state
  diagram, class diagram, or entity-relationship diagram.
- Keep accompanying prose sufficient to explain the diagram's purpose and important constraints.
  The diagram complements the documentation; it is not the only statement of required behavior.
- Use syntax supported by GitHub's Markdown renderer, keep labels readable in light and dark themes,
  and avoid custom styling unless it is necessary for meaning.
- Update a diagram in the same change as the behavior or structure it describes. Commit a rendered
  image only when a required documentation target cannot render Mermaid.

## ADRs

- Start from [`adr/template.md`](adr/template.md) and use the next unused zero-padded number.
- Record an accepted decision as it was actually agreed or implemented, including meaningful
  consequences and rejected alternatives.
- Do not rewrite an accepted ADR to fit a later decision. Add a new ADR, mark the earlier record as
  `Superseded by ADR-NNNN`, and link them.
- Keep implementation details in reference documentation when they are expected to evolve without
  changing the underlying decision.

## Plans

- Follow [`work-tracking.md`](work-tracking.md) for the boundary between GitHub Issues and plans.
- Track actionable work, ownership, priority, dependencies, and delivery status in GitHub Issues.
- Create a plan only for shaped work that spans multiple issues or pull requests and needs durable
  scope, sequencing, risks, non-goals, or exit criteria.
- Do not duplicate live issue lists, assignees, or progress in a plan. Link the plan from its parent
  feature issue and use GitHub sub-issues for the executable breakdown.
- Move approved work from `backlog/` into `docs/` with `git mv` and set its status to `Active`.
- When planned work ships, record any durable decision in an ADR and move the plan into `archive/`
  with a completion banner linking to the parent issue, completing pull request, and related ADRs.
- Never delete a plan merely because it was completed or superseded.

## Issue discussions

Issue and pull-request discussion is working context, not durable system documentation. Before an
issue closes, transfer accepted outcomes into the appropriate ADR or living reference when they
change architecture, behavior, public contracts, schema, operations, or security assumptions.
