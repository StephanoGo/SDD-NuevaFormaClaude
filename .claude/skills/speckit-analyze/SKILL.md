---
name: speckit-analyze
description: Cross-audit a feature's SDD artifacts and block implementation until they are coherent.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Analyze $ARGUMENTS (or the current feature) as a strict implementation gate.

Compare `spec.md`, `plan.md`, `data-model.md`, contracts, checklists, `tasks.md`, the constitution, affected module specs, and relevant code/tests. Check that:

- Clarifications affecting behavior are resolved.
- Every RF has acceptance coverage, a compatible design/contract, a task, and planned tests.
- No endpoint, data field, invariant, dependency, or behavior appears only in design/tasks without spec support.
- Module boundaries, state transitions, error codes, and cross-artifact terminology agree.
- Proposed technology and layering obey the constitution.
- Quality checks cover failure cases and critical domain/UX rules.

Return `BLOCKED` with each finding and the artifact/requirement to fix, or `READY FOR IMPLEMENTATION` with any non-blocking notes. Do not modify `src/`, `tests/`, or silently rewrite approved requirements. A blocked result forbids `/speckit-implement` until the artifacts are corrected and analysis passes.
