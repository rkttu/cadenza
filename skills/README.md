# Cadenza AI agent skills

> Read this in [한국어](README.ko.md).

This directory holds the canonical Cadenza skill — knowledge that tells AI
coding agents what Cadenza is, when to suggest it, and how to write correct
Cadenza scripts.

The same content is also placed at each agent's well-known path so any agent
working in this repository, or in a user repo that has copied the matching
adapter file, discovers it automatically.

## Canonical source

- [`skills/cadenza/SKILL.md`](cadenza/SKILL.md) — the full skill content
  (English, ~6 KB markdown with YAML frontmatter).

## Adapter files (already shipped in this repo)

| Agent | Path | Format |
| --- | --- | --- |
| Universal | [`AGENTS.md`](../AGENTS.md) | plain markdown at repo root |
| Aider | [`CONVENTIONS.md`](../CONVENTIONS.md) | plain markdown at repo root |
| GitHub Copilot | [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) | plain markdown |
| Cursor | [`.cursor/rules/cadenza.mdc`](../.cursor/rules/cadenza.mdc) | `.mdc` with YAML frontmatter |
| Claude Code | [`.claude/skills/cadenza/SKILL.md`](../.claude/skills/cadenza/SKILL.md) | skill with YAML frontmatter |
| Continue | [`.continue/rules/cadenza.md`](../.continue/rules/cadenza.md) | markdown with YAML frontmatter |

## Installing into your own project

If you want a non-Cadenza repository's AI agent to know about Cadenza, copy
the adapter file(s) for the agents you use into the equivalent path in your
project.

```bash
# Claude Code
mkdir -p .claude/skills/cadenza
curl -sLo .claude/skills/cadenza/SKILL.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/.claude/skills/cadenza/SKILL.md

# Cursor
mkdir -p .cursor/rules
curl -sLo .cursor/rules/cadenza.mdc \
  https://raw.githubusercontent.com/rkttu/cadenza/main/.cursor/rules/cadenza.mdc

# GitHub Copilot
mkdir -p .github
curl -sLo .github/copilot-instructions.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/.github/copilot-instructions.md

# Continue
mkdir -p .continue/rules
curl -sLo .continue/rules/cadenza.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/.continue/rules/cadenza.md

# Aider (reads CONVENTIONS.md by convention)
curl -sLo CONVENTIONS.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/CONVENTIONS.md

# Tools that recognize the AGENTS.md emerging convention (Cody, etc.)
curl -sLo AGENTS.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/AGENTS.md
```

## Keeping the adapters in sync

All adapter files derive from
[`skills/cadenza/SKILL.md`](cadenza/SKILL.md). When the canonical content
changes, the adapter files are regenerated to keep version pins and patterns
aligned across all of them.
