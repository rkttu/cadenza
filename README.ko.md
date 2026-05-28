# Cadenza

> [English](README.md)로 보기.

[![CI](https://github.com/rkttu/cadenza/actions/workflows/ci.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/ci.yml)
[![Release](https://github.com/rkttu/cadenza/actions/workflows/release.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0%2B-512BD4.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

.NET 10+ file-based 앱을 위한 단일 파일 스크립팅 SDK 가족. 다섯 개의 MSBuild SDK 패키지로 배포됩니다:

| SDK | 패키지 | 용도 |
| --- | --- | --- |
| `Cadenza` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.svg?label=nuget)](https://www.nuget.org/packages/Cadenza) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.svg?label=downloads)](https://www.nuget.org/packages/Cadenza) | 콘솔 스크립트, CLI 유틸리티 |
| `Cadenza.Worker` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Worker.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Worker) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Worker.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Worker) | 백그라운드 서비스, 데몬 |
| `Cadenza.Web` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Web.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Web) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Web.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Web) | 웹 API, Minimal API 스크립트 |
| `Cadenza.Mcp` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Mcp.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Mcp) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Mcp.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Mcp) | Claude / Cursor / VS Code AI 에이전트용 MCP 서버 |
| `Cadenza.Agent` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Agent.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Agent) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Agent.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Agent) | 로컬 AI 에이전트 — 같은 `IChatClient` 백엔드(Ollama / OpenAI / Anthropic / Azure OpenAI) 위에서 OpenAI Chat Completion(Aider / Continue / Cursor / Copilot BYOK)과 OpenAI Responses API(Codex CLI) 둘 다 제공 |

추가로 동반 `dotnet new` 템플릿 패키지 [`Cadenza.Templates`](https://www.nuget.org/packages/Cadenza.Templates)가 다섯 변종의 starter를 제공합니다 (아래 [새 스크립트 시작](#dotnet-new로-새-스크립트-시작) 참조).

스크립트 첫 줄에 `#:sdk` 디렉티브를 두어 변종을 선택합니다. **버전은 정확히 적어야 합니다** — MSBuild SDK 참조는 `1.*` 같은 wildcard를 지원하지 않습니다. 아래 버전은 nuget.org의 최신 버전으로 교체하세요:

```csharp
#:sdk Cadenza@1.0.15           // 콘솔
#:sdk Cadenza.Worker@1.0.15    // 워커
#:sdk Cadenza.Web@1.0.15       // 웹
#:sdk Cadenza.Mcp@1.0.15       // MCP 서버
#:sdk Cadenza.Agent@1.0.15     // AI 에이전트 (OpenAI 호환 HTTP 서버)
```

사용자 문서는 **[위키](https://github.com/rkttu/cadenza/wiki)** 에 있습니다 — 설치, 에디터 설정, SDK별 레퍼런스, 배포 레시피, 트러블슈팅. 형식 명세는 [docs/spec.md](docs/spec.md) (한국어), 배포 가이드는 [docs/publishing-single-binary.ko.md](docs/publishing-single-binary.ko.md) 참고.

## 예시

콘솔:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

foreach (var file in Glob("**/*.md"))
    WriteLine($"{file}: {ReadText(file).Length:N0} bytes");
```

워커:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.15

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"Heartbeat at {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

웹:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.15

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });

await Run();
```

MCP 서버:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.15

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

AI 에이전트 (OpenAI 호환 HTTP 서버 — Codex / Aider / Continue / Cursor가 OpenAI인 것처럼 대화):

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

SystemPrompt("당신은 파일 시스템에 접근할 수 있는 친절한 비서입니다.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

UseOllama("llama3.2");      // 또는 UseOpenAi / UseAnthropic / UseAzureOpenAi

await Run();                // http://localhost:8080에서 OpenAI Chat Completion 서비스
```

더 많은 샘플은 [samples/](samples/) 아래에 있습니다 — [샘플 인덱스](samples/README.ko.md)에 전체 목록이 정리돼 있습니다 (콘솔 glob/grep, git deploy guard, JSON 타입 HTTP fetch, 대화형 setup, config polling 워커, 웹 CRUD API, MCP 서버, AI 에이전트 등).

### `dotnet new`로 새 스크립트 시작

단일 템플릿 패키지 [`Cadenza.Templates`](https://www.nuget.org/packages/Cadenza.Templates)가 다섯 변종 모두의 starter를 제공합니다:

```bash
dotnet new install Cadenza.Templates
dotnet new cadenza         -n mytool   -o ./mytool      # 콘솔 (cadenza-console의 별칭)
dotnet new cadenza-worker  -n mydaemon -o ./mydaemon    # 백그라운드 서비스
dotnet new cadenza-web     -n myapi    -o ./myapi       # Minimal API
dotnet new cadenza-mcp     -n myserver -o ./myserver    # MCP 서버
dotnet new cadenza-agent   -n myagent  -o ./myagent     # AI 에이전트 (OpenAI 호환 HTTP)
```

`cadenza` 짧은 이름은 콘솔 변종의 별칭 — `dotnet new cadenza-console`도 동일하게 동작합니다. 각 명령은 `-n`에 지정한 이름으로 `.cs` 파일 한 장을 생성하고, 매칭되는 SDK 버전이 미리 핀돼 있으며 본문은 canonical starter 패턴을 담고 있습니다.

## 저장소 구조

```text
src/
  core/          # 모든 변종 공유 (namespace: Cadenza)
  worker/        # 워커 전용 (namespace: Cadenza.Worker)
  web/           # 웹 전용 (namespace: Cadenza.Web)
  mcp/           # MCP 전용 (namespace: Cadenza.Mcp)
  agent/         # 에이전트 전용 (namespace: Cadenza.Agent)
packaging/
  Cadenza/             # 콘솔 SDK 패키지 레이아웃
  Cadenza.Worker/      # 워커 SDK 패키지 레이아웃
  Cadenza.Web/         # 웹 SDK 패키지 레이아웃
  Cadenza.Mcp/         # MCP 서버 SDK 패키지 레이아웃
  Cadenza.Agent/       # AI 에이전트 SDK 패키지 레이아웃
build/
  Cadenza.Packaging.proj   # 5개 SDK를 한 번에 pack하는 traversal 프로젝트
samples/                   # canonical 예제 스크립트
.github/workflows/         # CI (pack) + release (pack + nuget.org push)
```

## 로컬 빌드

```bash
dotnet pack build/Cadenza.Packaging.proj -c Release -o ./artifacts -p:Version=1.0.15-local
```

`./artifacts` 아래에 4개의 `.nupkg`가 생성됩니다. 스크립트에서 소비하려면 옆에 `nuget.config`를 두고 `<add key="local" value="…/artifacts" />` 소스를 추가합니다.

## 게시

CI/CD는 [.github/workflows/](.github/workflows/)에 구성돼 있습니다:

- [ci.yml](.github/workflows/ci.yml) — push/PR마다 Linux/macOS/Windows에서 모든 SDK pack + Linux artifact 업로드
- [release.yml](.github/workflows/release.yml) — `v*` 태그 push 또는 `workflow_dispatch`로 실행. `NUGET_API_KEY` repository secret으로 nuget.org에 push 후 GitHub release 생성

릴리스 하려면 `v1.0.7` 같은 태그를 push 하거나, `release.yml`을 버전 입력과 함께 수동 실행.

## AI 코딩 에이전트 연동

Cadenza는 주요 AI 코딩 에이전트들이 자동으로 발견하고 정확한 스크립트를
작성할 수 있도록 어댑터 파일을 함께 배포합니다. canonical 스킬 콘텐츠는
[`skills/cadenza/SKILL.md`](skills/cadenza/SKILL.md)에 있고, 각 에이전트가
인식하는 경로에 어댑터가 미리 설치돼 있습니다:

| 에이전트 | 어댑터 경로 |
| --- | --- |
| Universal (Cody, OpenAI tools, …) | [`AGENTS.md`](AGENTS.md) |
| Aider | [`CONVENTIONS.md`](CONVENTIONS.md) |
| GitHub Copilot | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Cursor | [`.cursor/rules/cadenza.mdc`](.cursor/rules/cadenza.mdc) |
| Claude Code | [`.claude/skills/cadenza/SKILL.md`](.claude/skills/cadenza/SKILL.md) |
| Continue | [`.continue/rules/cadenza.md`](.continue/rules/cadenza.md) |

다른 프로젝트에서 Cadenza-aware 보조를 켜고 싶다면 본인이 쓰는 에이전트의
어댑터를 복사하면 됩니다 — [`skills/README.ko.md`](skills/README.ko.md)에
한 줄 curl 레시피가 있습니다.

## 트러블슈팅

자주 발생하는 문제와 우회법은 [docs/troubleshooting.ko.md](docs/troubleshooting.ko.md)에 모았습니다. 바로가기:

- [`#:sdk Cadenza@1.*` (wildcard) → "버전이 지정되지 않음" 오류](docs/troubleshooting.ko.md#sdk-cadenza1-wildcard--floating-버전-사용-시-버전이-지정되지-않음-오류) — `PackageReference`와 달리 MSBuild SDK는 정확한 버전 필요
- [새 버전이 인식 안 됨 (stale NuGet cache)](docs/troubleshooting.ko.md#새로-게시된-버전이-인식되지-않음-stale-nuget-cache) — Cadenza 관련 항목만 선택적으로 정리
- [macOS에서 `MSB3552: **/*.resx not found`](docs/troubleshooting.ko.md#macos-error-msb3552-리소스-파일-resx을를-찾을-수-없습니다) — 1.0.1에서 수정
- [Windows에서 `Capture(...)` 결과 CJK / emoji 깨짐](docs/troubleshooting.ko.md#capture-출력-windows에서-cjk--emoji-깨짐) — 1.0.4에서 수정

## 라이선스

[MIT](LICENSE) — © Cadenza contributors.
