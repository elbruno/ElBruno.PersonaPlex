# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, scope, priorities | Neo | System design, what to build next, trade-offs |
| ML research, Python scripts, ONNX export | Morpheus | Model export scripts, quantization, HuggingFace integration |
| C# library code, ONNX Runtime | Trinity | Core inference pipeline, API surface, NuGet packaging |
| Tests, quality assurance | Tank | xUnit tests, integration tests, edge cases |
| CI/CD, GitHub Actions, packaging | Dozer | Workflows, NuGet publishing, build pipelines |
| Security review, dependency audit | Niobe | Code security, supply chain, input validation, secret management |
| ONNX model correctness, optimization | Seraph | Model conversion review, quantization review, inference correctness |
| Code review (security gate) | Niobe | ALL work must pass security review before done |
| Code review (model gate) | Seraph | ONNX/inference work must pass model correctness review |
| Async issue work | @copilot 🤖 | Well-defined tasks matching capability profile |
| Session logging | Scribe | Automatic — never needs routing |

## Review Pipeline

All work follows this pipeline before it's considered done:

```
Agent completes work
    → Niobe reviews (security)
        → APPROVE → Seraph reviews (if ONNX/inference related)
            → APPROVE → Done ✅
            → REJECT → Different agent revises, re-review
        → REJECT → Different agent revises, re-review
```

**Niobe reviews ALL work** — every PR, every code change, every CI/CD modification.
**Seraph reviews ONNX-touching work** — export scripts, C# inference code, model optimization, quantization settings.

Work that touches neither ONNX nor inference (pure docs, pure CI/CD config) may skip Seraph but never skips Niobe.

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Neo |
| `squad:{name}` | Pick up issue and complete the work | Named member |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Neo** triages it.
2. **@copilot evaluation:** Neo checks if the issue matches @copilot's capability profile.
3. When a `squad:{member}` label is applied, that member picks up the issue.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` picks it up autonomously.
5. Members can reassign by swapping labels.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work in parallel.
2. **Scribe always runs** after substantial work, always as `mode: "background"`.
3. **Quick facts → coordinator answers directly.**
4. **Two agents could handle it** → pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel.
6. **Anticipate downstream work.** Spawn testers for test cases while implementers build.
7. **Review gates are mandatory.** Niobe (security) reviews everything. Seraph (model) reviews ONNX-touching work.
8. **Reviewer rejection → different agent revises.** Original author is locked out per Reviewer Rejection Protocol.
