# Decisions

> Shared decision log. All agents read this before starting work. Scribe maintains it.

### 2026-02-27: Review gates established
**By:** Squad (Coordinator)
**What:** All work must pass through Niobe (security review) before being considered done. Work touching ONNX export, inference code, or model optimization must additionally pass through Seraph (model correctness review). Reviewer rejection triggers the Reviewer Rejection Protocol — a different agent must revise, not the original author.
**Why:** Security and model correctness are critical for a library that downloads and loads ML models. Two-reviewer gate ensures no code ships without both security and correctness verification.
