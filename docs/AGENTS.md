# docs/ — Documentation guidance

Keep [`README.md`](README.md) current when adding, moving, renaming, or removing documentation.

## Document types

| Kind | Location | Purpose |
| --- | --- | --- |
| Reference | `docs/*.md` | Living description of the current agreed system direction or shipped behavior |
| ADR | `docs/adr/NNNN-*.md` | Durable record of an accepted architectural decision |
| Backlog plan | `docs/backlog/*-plan.md` | Potential work without an implementation commitment |
| Active plan | `docs/*-plan.md` | Approved or active implementation work |
| Archived plan | `docs/archive/*-plan.md` | Historical plan retained after implementation |

Reference documentation must describe the current state. Update it in the same change that modifies
the behavior, contract, architecture, schema, operations, or security assumptions it covers.

## ADRs

- Start from [`adr/template.md`](adr/template.md) and use the next unused zero-padded number.
- Record an accepted decision as it was actually agreed or implemented, including meaningful
  consequences and rejected alternatives.
- Do not rewrite an accepted ADR to fit a later decision. Add a new ADR, mark the earlier record as
  `Superseded by ADR-NNNN`, and link them.
- Keep implementation details in reference documentation when they are expected to evolve without
  changing the underlying decision.

## Plans

- Move approved work from `backlog/` into `docs/` with `git mv` and set its status to `Active`.
- When planned work ships, record any durable decision in an ADR and move the plan into `archive/`
  with a banner linking to that ADR.
- Never delete a plan merely because it was completed or superseded.

