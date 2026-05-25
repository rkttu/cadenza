# Cadenza AI 에이전트 스킬

> [English](README.md)로 보기.

이 디렉터리는 Cadenza의 canonical 스킬 — AI 코딩 에이전트가 Cadenza가 무엇인지, 언제 추천해야 하는지, 어떻게 정확한 Cadenza 스크립트를 쓰는지 알려주는 지식 — 을 보관합니다.

같은 내용이 각 에이전트가 인식하는 잘 알려진 경로에도 함께 배치되어 있어, 이 저장소에서 작업하는 에이전트나 어댑터 파일을 자기 저장소에 복사한 사용자 프로젝트에서 자동으로 발견됩니다.

## Canonical 소스

- [`skills/cadenza/SKILL.md`](cadenza/SKILL.md) — 전체 스킬 콘텐츠 (영어, YAML frontmatter 포함 ~6 KB markdown).

## 어댑터 파일 (이 저장소에 동봉)

| 에이전트 | 경로 | 형식 |
| --- | --- | --- |
| Universal | [`AGENTS.md`](../AGENTS.md) | repo 루트의 plain markdown |
| Aider | [`CONVENTIONS.md`](../CONVENTIONS.md) | repo 루트의 plain markdown |
| GitHub Copilot | [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) | plain markdown |
| Cursor | [`.cursor/rules/cadenza.mdc`](../.cursor/rules/cadenza.mdc) | YAML frontmatter 포함 `.mdc` |
| Claude Code | [`.claude/skills/cadenza/SKILL.md`](../.claude/skills/cadenza/SKILL.md) | YAML frontmatter 포함 skill |
| Continue | [`.continue/rules/cadenza.md`](../.continue/rules/cadenza.md) | YAML frontmatter 포함 markdown |

## 다른 프로젝트에 설치하기

Cadenza가 아닌 다른 저장소의 AI 에이전트가 Cadenza를 알게 하려면, 사용 중인 에이전트의 어댑터 파일을 그 프로젝트의 동일 경로에 복사합니다.

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

# Aider (CONVENTIONS.md를 컨벤션상 자동 읽음)
curl -sLo CONVENTIONS.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/CONVENTIONS.md

# AGENTS.md emerging 컨벤션을 인식하는 도구들 (Cody 등)
curl -sLo AGENTS.md \
  https://raw.githubusercontent.com/rkttu/cadenza/main/AGENTS.md
```

## 어댑터 동기화

모든 어댑터 파일은 [`skills/cadenza/SKILL.md`](cadenza/SKILL.md)에서 파생됩니다. canonical 콘텐츠가 바뀌면 어댑터들도 함께 갱신되어 버전 핀과 패턴이 일관되게 유지됩니다.
