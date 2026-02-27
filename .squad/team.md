# ElBruno.PersonaPlex Team

> C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Neo | Lead / Architect | `.squad/agents/neo/charter.md` | ✅ Active |
| Morpheus | ML / Python Dev | `.squad/agents/morpheus/charter.md` | ✅ Active |
| Trinity | C# Dev | `.squad/agents/trinity/charter.md` | ✅ Active |
| Tank | Tester | `.squad/agents/tank/charter.md` | ✅ Active |
| Dozer | DevOps | `.squad/agents/dozer/charter.md` | ✅ Active |
| Niobe | Security Specialist | `.squad/agents/niobe/charter.md` | ✅ Active |
| Seraph | ONNX Model Transformation Specialist | `.squad/agents/seraph/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Review Gates

All work MUST pass through both reviewers before it's considered done:

| Reviewer | Reviews | Gate |
|----------|---------|------|
| Niobe | All code, CI/CD, dependencies, model loading | 🔒 Security approval required |
| Seraph | ONNX export scripts, C# inference code, model optimization | 🧬 Model correctness approval required |

Review order: Agent completes work → Niobe (security) → Seraph (model correctness, if applicable) → Done.
Work touching only CI/CD or docs may skip Seraph. Work touching only model scripts may skip detailed security review (Niobe still does a light pass).

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- ONNX model export or optimization work
- Security-critical changes (auth, model loading, supply chain)
- Multi-system integration requiring coordination
- Performance-critical inference paths requiring benchmarking

## Project Context

- **Owner:** Bruno Capuano
- **Stack:** C#, .NET, ONNX Runtime, Python (model export), xUnit, GitHub Actions, NuGet
- **Description:** C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Created:** 2026-02-27
