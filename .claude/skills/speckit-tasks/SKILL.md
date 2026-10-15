---
name: speckit-tasks
description: Break a specified and planned feature into traceable, dependency-ordered tasks.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Create the implementation task list for $ARGUMENTS (or the current feature when omitted).

1. Read `spec.md`, `plan.md`, `data-model.md`, contracts, checklists, and current project structure.
2. If the spec has unresolved behavior decisions or required design artifacts are missing, report that tasks cannot be made reliable yet; do not fill the gaps.
3. Create `tasks.md` with short, atomic checkbox tasks. Order dependencies from data/domain through application/persistence, API/UI, and tests as applicable.
4. Link every task to its requirement IDs and acceptance criteria. Include meaningful test work and final `dotnet build`/`dotnet test` verification.
5. Keep all tasks unchecked. Do not modify `src/` or `tests/`.
6. Report task count, any uncovered requirements, and the next command.
