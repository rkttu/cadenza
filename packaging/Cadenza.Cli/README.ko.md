# Cadenza.Cli

> [English](README.md)로 보기.

`cadenza`는 [Cadenza 단일 파일 .NET 스크립팅 SDK 가족](https://github.com/rkttu/cadenza)의 **옵션 액세서리 CLI**입니다. Cadenza 스크립트를 실행하는 정석은 여전히 `dotnet run app.cs`로 스크립트 안에 정확한 `#:sdk` 버전을 핀한 형태이며, 이 도구 설치는 필수가 아닙니다.

설치 시 얻는 것:

- 번들된 **MSBuild SDK resolver** — **첫 `cadenza` 명령 호출**(어떤 subcommand든) 시점에 자동 활성화. 이후엔 `dotnet run app.cs`가 `#:sdk Cadenza` (버전 생략) 또는 `#:sdk Cadenza@latest`로 곧바로 동작.
- `cadenza new <변종>` — `dotnet new cadenza-*` 단축
- `cadenza run app.cs` / `cadenza publish app.cs` — `@latest`를 미리 해석한 뒤 `dotnet run` / `dotnet publish`로 위임. resolver 활성화 전에도 동작.

## 설치

```bash
dotnet tool install -g Cadenza.Cli
cadenza --version          # 동시에 SDK resolver 초기 설치도 수행
```

Windows에서는 user-level `MSBUILDADDITIONALSDKRESOLVERSFOLDER` 환경변수가 자동으로 설정됩니다 — 이후 새 터미널을 열어주세요. macOS / Linux에서는 셸 프로필(`~/.bashrc`, `~/.zshrc` 등)에 추가할 한 줄 `export ...`을 출력합니다.

CI나 샌드박스 환경에서 first-run setup을 끄려면 `CADENZA_SKIP_AUTOINSTALL=1`을 사용하세요.

## 사용

```bash
# 스캐폴딩
cadenza new console -n mytool -o ./mytool   # 별칭: cli, c
cadenza new worker  -n mydaemon             # 별칭: w, svc, daemon
cadenza new web     -n myapi                # 별칭: api
cadenza new mcp     -n myserver             # 별칭: m, server

# @latest / 버전 생략 스크립트 실행
cat > app.cs <<'EOF'
#:sdk Cadenza@latest
WriteLine("hi");
EOF
cadenza run app.cs       # 항상 동작 (CLI가 @latest를 먼저 rewrite)
dotnet run app.cs        # SDK resolver 활성화 후에도 동작

# publish
cadenza publish app.cs -r linux-x64 -c Release
```

`@latest` (또는 빈 버전)는 `https://api.nuget.org/v3-flatcontainer/cadenza/index.json`을 질의해 가장 높은 안정 SemVer를 선택합니다. CLI는 결과를 `~/.cadenza/cache/<pkgid>.version`에 24시간 캐시합니다 (SDK resolver는 글로벌 NuGet 패키지 폴더를 사용해 별도 캐시 없음).

## 두 가지 활성화 경로

| 경로 | 동작 |
| --- | --- |
| `dotnet run app.cs` (CLI 미설치) | `#:sdk Cadenza@<정확한 버전>` — canonical workflow, 설치 불필요. |
| `cadenza run app.cs` (CLI 설치, resolver 부트스트랩 선택) | wrapper로 `#:sdk Cadenza@latest` / 버전 생략 지원. |
| `dotnet run app.cs` (CLI 설치, resolver가 셸 env에 활성화됨) | MSBuild resolver로 `#:sdk Cadenza@latest` / 버전 생략 지원 — IDE, `dotnet publish`, 모든 MSBuild 진입점에서 동작. |

## 명시적 설정 / 제거 명령

```bash
cadenza install-resolver     # 전체 출력과 함께 first-time setup을 명시적 실행
cadenza uninstall-resolver   # resolver 파일과 env var (Windows) 제거
```

`uninstall-resolver`는 도구 자체를 제거하지 않습니다 — 도구 제거는 `dotnet tool uninstall -g Cadenza.Cli`.

## 기본 워크플로는 여전히 `dotnet run`

Cadenza 가족의 일차 진입점은 변함없이:

```bash
dotnet run app.cs
```

스크립트가 정확한 SDK 버전을 핀한 형태입니다. 이 CLI 없이도 동작하며, README·샘플·AI 에이전트 스킬·IDE 템플릿 모두 그 경로를 정석으로 안내합니다.

CLI는 다음 두 시나리오에서만 의미를 가집니다:

1. 스크립트에 `@latest` 시맨틱을 쓰고 싶을 때 (정확한 버전이 중요하지 않은 빠른 실험)
2. `dotnet new cadenza-*`보다 `cadenza new`가 짧아 좋을 때

둘 다 해당이 없다면 이 패키지를 설치할 필요가 없습니다.

## 제거

```bash
cadenza uninstall-resolver         # resolver 파일 + env var 정리 먼저
dotnet tool uninstall -g Cadenza.Cli
```

전체 SDK 가족은 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
