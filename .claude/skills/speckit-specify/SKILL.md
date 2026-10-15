---
name: speckit-specify
description: Create or refine a module feature specification using the project's SDD workflow.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Create or refine a functional specification for: $ARGUMENTS

1. Read `CLAUDE.md`, `.specify/memory/constitution.md`, `specs/README.md`, the affected module spec, and relevant code.
2. Keep the user's original request verbatim in the feature spec. Identify the owning module and affected modules.
3. Ask about business decisions that change behavior. Never fill gaps with invented rules. Record unanswered questions and keep the spec in Draft until resolved.
4. Create `specs/modulos/<module>/<feature-slug>/spec.md` with feature name, module, branch name, date, status, original request, In Scope, Out of Scope, Clarifications, functional requirements with stable module-prefixed IDs, and Given/When/Then acceptance criteria mapped to those IDs.
5. Keep requirements user-visible and testable; do not put architecture or implementation choices in functional requirements.
6. Do not modify `src/` or `tests/`, and do not create later-phase artifacts. Report the spec path, open clarifications, and next command.
