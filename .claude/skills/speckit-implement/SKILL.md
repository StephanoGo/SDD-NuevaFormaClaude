---
name: speckit-implement
description: Implement an approved feature from its analyzed tasks and verify it with the full test suite.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Implement $ARGUMENTS (or the current feature) only after its latest `/speckit-analyze` result is `READY FOR IMPLEMENTATION`.

1. Read the complete feature artifacts and implement only unchecked tasks backed by approved requirement IDs. If the spec or design is insufficient, stop and return to the relevant SDD command.
2. This is the only Speckit command authorized to create or modify executable files in `src/` and `tests/`. Keep changes within scope; add/update tests for the acceptance and checklist cases.
3. Run `dotnet build` and `dotnet test` from the repository root. Diagnose failures; do not mark related work complete unless both pass.
4. Mark a task `[x]` only after its implementation and relevant tests pass. Leave other tasks unchecked.
5. Report changed files, completed and remaining task IDs, build/test outcomes, and any blocker. Do not commit or publish changes.
