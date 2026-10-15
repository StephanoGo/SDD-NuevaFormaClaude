---
name: speckit-checklist
description: Audit requirement quality and add focused domain or UX checks for a feature.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Create quality checklists for the feature at $ARGUMENTS (or the current feature when omitted).

Read the feature spec, plan, data model, contracts, affected module specs, and relevant code. Do not edit code or infer business decisions. The project description specifies a 16-check formal checklist but did not provide the original wording. The 16 checks below are a proposed baseline derived from the supplied goals (atomic, testable, unambiguous requirements); identify them as proposed in `requirements.md` until the project owner confirms them. Do not turn these process checks into product behavior. Mark each pass/fail/needs clarification and link it to requirement IDs:

1. Every requirement has a unique stable ID.
2. Each requirement describes one atomic behavior.
3. Actor or responsible system component is identifiable.
4. Trigger or condition is explicit.
5. Expected result is explicit.
6. Wording has one unambiguous interpretation.
7. Vague terms have measurable definitions or are removed.
8. Domain terms are consistent across the spec and module baseline.
9. In Scope and Out of Scope are explicit.
10. Preconditions and relevant state are stated.
11. Success behavior has observable acceptance criteria.
12. Relevant validation and failure behavior are covered.
13. Data ownership and affected module boundaries are clear.
14. State transitions and invariants are consistent with the model.
15. Every requirement maps to at least one Given/When/Then scenario.
16. Functional requirements do not prescribe unnecessary implementation details.

Add a separate concise domain checklist for clinical/business invariants or a UX checklist for interaction/accessibility when relevant. Only assert domain rules present in approved specs; mark uncertain rules as questions, not failures to implement. Report critical findings and the next command.
