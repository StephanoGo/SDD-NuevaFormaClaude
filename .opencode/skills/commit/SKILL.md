---
name: commit
description: Review staged Git changes, generate an English Conventional Commit message, and create the local commit when authorized. Does not push or release.
metadata:
  opencode/slash: "true"
---

# /commit

## Scope

Work on staged changes by default. Generate a message and ask once before creating the commit, unless the user already explicitly asked to commit those changes. A request for a message alone does not authorize a commit. Push, amend, release, and branch changes are outside this workflow.

If the user asks to include unstaged or new files, inspect them and stage only the authorized paths or hunks. Do not use blanket staging or `git commit -am`. Preserve unrelated staged and unstaged work; if the index mixes unrelated changes, ask how to separate them without resetting the user's index.

## Workflow

1. Locate the repository with `git rev-parse --show-toplevel`. Read applicable repository instructions and commit conventions. Check `git status --short`, the current branch, and whether a merge, rebase, cherry-pick, or revert is in progress. If an operation is active or HEAD is detached, report the state and resolve the workflow choice before committing.
2. Inspect `git diff --cached --stat`, `git diff --cached --name-status`, and `git diff --cached`. If nothing is staged, report that and stop unless the user explicitly asked you to stage files. Review for unrelated changes and accidentally staged credentials; do not echo secret values.
3. Generate the message from the actual diff. Use `feat`, `fix`, `docs`, `refactor`, `chore`, `style`, `test`, `perf`, or `ci`. New files do not automatically imply a feature. Use a meaningful scope when useful; omit it rather than invent one. For a breaking change, use `!` and explain it in a `BREAKING CHANGE:` footer.
4. Show the intended files and full message. If commit authorization is missing, request it once here. Before committing, compare HEAD and the staged diff with the reviewed snapshot; if they changed, review them again.
5. Commit with `git commit -F <message-file>` using a temporary file. Resolve its location with `git rev-parse --git-path` or OS temporary storage; `.git` may be a file in a worktree. Do not interpolate untrusted message text as shell code. Do not disable hooks, signing, or configured checks. On failure, report the cause and retain the index state; do not retry blindly or create an empty commit. If a hook changes the diff, review it again.
6. Verify the commit SHA, message, changed paths, and remaining working-tree changes. Delete only the exact temporary file created by this invocation. Do not push.

## Message conventions

Follow repository conventions first. Otherwise:

- Subject: `type(scope): summary.` in English and imperative mood. Aim for 50 characters; allow up to 72 when needed for clarity. Omit unnecessary scopes instead of using awkward abbreviations.
- Use lowercase prose, including headings and bullets, with a final period. Preserve literal paths, identifiers, package names, and case-sensitive text. Preserve the exact `BREAKING CHANGE:` footer token.
- Add a body for nontrivial changes explaining behavior and purpose. Optional `added:`, `modified:`, and `deleted:` sections should describe meaningful changes, not inventory every file. Omit empty sections.
- Keep identifiers verbatim only where necessary; surrounding prose remains lowercase.

Example:

```text
feat(ui): add sticky scroll reveal.

keep the device preview visible while scrolling through features.

modified:
- src/scripts/site.ts: add initStickyReveal with reduced motion support.
```
