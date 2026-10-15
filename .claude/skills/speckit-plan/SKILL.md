---
name: speckit-plan
description: Produce a concise technical design, data model, and contracts for a specified feature.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Plan the feature at $ARGUMENTS (or the current feature when omitted).

1. Read its `spec.md`, the constitution, affected module specs, current code, and only relevant shared contracts.
2. Stop and report blockers if business clarifications remain unresolved. Do not resolve them by assumption.
3. Create concise `plan.md` and `data-model.md`. Explain the selected architecture, affected modules, data ownership, invariants, persistence impact, error behavior, and verification approach. Check proposed technology against `.specify/memory/constitution.md`.
4. Create `contracts/` with precise HTTP/JSON and repository contracts for interfaces the feature changes. If none change, add a brief note referencing the shared contract and stating that no new interface is introduced. Reuse shared contracts by reference instead of copying them.
5. Keep rejected alternatives and implementation minutiae out unless they explain a material decision. Do not change `src/` or `tests/`.
6. Report artifacts created, decisions still open, and the next command.
