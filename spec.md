# Cadenza SDK Family — Specification

> 본 문서는 1.0.x 라인 출시 이후 1.1을 향한 작업용 명세이며, §13에 변종 SDK(Cadenza.Mcp / Cadenza.Agent) 명세, §14에 선택적 도구(Cadenza.SdkResolver) 명세를 포함한다. 결정되지 않은 항목은 §10 Open Decisions에 명시.

---

## 1. Identity

| 항목 | 값 |
| ------ | ----- |
| **Brand** | `Cadenza` |
| **Tagline** | A single-file scripting SDK family for .NET 10+ file-based apps |
| **Current line** | `1.0.x` (출시 중) / `1.1.0` (다음 마이너 — §12 참조) |
| **License** | MIT |
| **Target framework** | `net10.0` |
| **Minimum SDK** | .NET SDK 10.0.300 (file-based apps + `#:sdk` custom SDK 지원) |
| **External dependencies** | base SDK 외 — `Microsoft.Extensions.*` (Worker/Web/Agent), `ModelContextProtocol` (Mcp), `Microsoft.Extensions.AI` 및 LLM 클라이언트 (Agent) |
| **Distribution channel** | NuGet **MSBuild SDK** 패키지 5종 + 옵션 도구 패키지 (§14) |

### 1.1 SDK 가족 구성

| SDK | 용도 | 기반 SDK | Default deployment | AOT-clean |
| ----- | ------ | --------- | ------------------- | --------- |
| `Cadenza` | 콘솔 스크립트, CLI 유틸리티 | `Microsoft.NET.Sdk` | JIT + R2R/SingleFile/SCD on publish | ✅ |
| `Cadenza.Worker` | 백그라운드 서비스, 데몬 | `Microsoft.NET.Sdk.Worker` | JIT + R2R/SingleFile/SCD on publish | ✅ |
| `Cadenza.Web` | 웹 API, Minimal API 스크립트 | `Microsoft.NET.Sdk.Web` | JIT + R2R/SingleFile/SCD on publish | ✅ |
| `Cadenza.Mcp` | MCP (Model Context Protocol) 서버 | `Microsoft.NET.Sdk` | JIT + R2R/SingleFile/SCD on publish | ✅ |
| `Cadenza.Agent` | AI 에이전트 + OpenAI 호환 HTTP 프런트엔드 | `Microsoft.NET.Sdk.Web` | JIT + R2R/SingleFile/SCD on publish | ⚠️ (§13.2.10) |

다섯 변종 모두 NativeAOT는 default off, `#:property PublishAot=true`로 opt-in. 자세한 deployment 모델은 §3.4. **Cadenza.Agent는 가족 중 유일하게 AOT-clean이 아니다** — `Microsoft.Extensions.AI.AIFunctionFactory`가 사용자 핸들러를 reflection으로 바인딩하기 때문 (§13.2.10 참조). AOT가 필요한 사용자는 Agent 대신 `Cadenza.Web` + 수동 `IChatClient` 호출 경로를 사용한다.

변종 SDK 명세:

- §13.1 — `Cadenza.Mcp`
- §13.2 — `Cadenza.Agent`
- §13.3 — v1.2+ 변종 후보 (`Cadenza.Wasi` 등)

사용자는 스크립트 첫 줄로 변종을 선택 (버전 문자열은 작성 시점의 최신 1.0.x를 가정):

```csharp
#:sdk Cadenza@1.0.155           // 콘솔
#:sdk Cadenza.Worker@1.0.155    // 워커
#:sdk Cadenza.Web@1.0.155       // 웹
#:sdk Cadenza.Mcp@1.0.15       // MCP 서버
#:sdk Cadenza.Agent@1.0.15     // AI 에이전트
```

> **버전은 정확한 SemVer만 허용된다.** MSBuild SDK reference resolver는 `1.*` 같은 floating 버전을 지원하지 않으며, wildcard 사용 시 "버전이 지정되지 않음" 오류로 떨어진다. 새 릴리스가 나오면 `#:sdk` 줄의 버전 문자열을 직접 갱신해야 한다 (정책 근거: §11.4. version-less / `@latest` UX를 원하는 사용자를 위한 선택적 SDK resolver는 §14 참조).

### 1.2 Naming Rationale

**Cadenza**(카덴차)는 협주곡(concerto) 중 오케스트라가 멎고 독주자가 홀로 연주하는 독립 구간을 가리킨다. 역사적으로 즉흥 연주였고, 작곡가가 명시하든 연주자가 그 자리에서 만들든 — **반주 없이 한 명의 연주자가 완성된 한 곡을 펼친다**는 본질은 변하지 않는다. 그리고 카덴차는 단지 짧은 막간이 아니라 *연주자의 기량이 가장 선명히 드러나는 자리*다.

이 의미가 file-based apps의 구조와 정확히 일치한다. 솔루션, csproj, MSBuild 의식(ceremony)이 모두 사라지고, `.cs` 파일 한 장이 완결된 프로그램을 표현한다. 즉 **단일 파일 .NET 스크립트는 .NET 음악의 카덴차**이다. 그리고 카덴차는 협주곡과 대립하지 않는다 — 같은 음악 안의 다른 부분일 뿐이다. 마찬가지로 Cadenza SDK는 csproj 기반 .NET 개발을 부정하지 않는다. 둘은 같은 .NET 생태계의 다른 모드이고, 같은 개발자가 두 모드 사이를 자유롭게 오갈 수 있다.

이 명명은 또한 **C#이 이미 음악적 어원에 속한다는 사실** — `#`이 sharp 기호 — 과 자연스럽게 연결된다. C#과 Cadenza는 같은 음악 우주에서 이름을 가져온 자매 관계를 형성하며, 이는 우연이 아니라 SDK의 정체성을 의도적으로 C# 자체의 어원으로 회귀시키는 디자인 선택이다.

함의 정리:

- **Solo without orchestra** → 단일 파일 스크립트는 자립적이다.
- **Virtuosity** → 적은 줄 수로 깊은 결과를 만든다.
- **Same musical world as concerto** → 프로젝트 기반 개발과 대립이 아닌 보완 관계.
- **C# 어원과의 자매성** → 음악적 명명 일관성, 우연 아닌 의도적 선택.

이 narrative는 README의 첫 단락 및 첫 외부 글(devto, 블로그)의 도입부 재료로 그대로 활용 가능하다.

---

## 2. Design Principles

### 2.1 핵심 원칙

1. **PascalCase 유지.** C# 정체성 보존, 인접 코드와의 시각적 정합성 우선.
2. **Bare names via `global using static`.** 접두사 비용은 SDK가 흡수.
3. **Tier 1 (bare) ≤ 10개 / SDK.** 작고, 안정적이고, 한 화면 README.
4. **CLI는 콘솔 변종의 캐노니컬 use case**, general scripting 스코프 유지.
5. **Cadenza API는 AOT-clean by construction.** AOT 모드 활성화 시 Cadenza 코드는 추가 작업 없이 동작. 단 AOT는 default가 아니며 사용자의 deliberate opt-in 자리에 위치.
6. **Iteration 단계의 마찰 최소화.** Default 모드(JIT)에서 모든 .NET 라이브러리가 그대로 동작.
7. **Distribution은 publish 한 번으로 단일 자체 포함 바이너리 산출.** FDD 회피, Go-like 배포 모델.

### 2.2 SDK 가족 추가 원칙

1. **Tier 1은 SDK 컨텍스트에 적응한다.** 같은 bare name(`Run`)이 SDK에 따라 다른 의미를 가질 수 있음 — `#:sdk` 선언이 의미를 disambiguate.
2. **공유 모듈은 모든 변종에서 동일 의미.** `Fs.ReadText`는 어디서나 파일 읽기.
3. **변종은 기반 SDK의 의미론을 손상하지 않는다.** `Cadenza.Web`은 ASP.NET Core의 정상 패턴을 그대로 노출.

---

## 3. SDK Architecture

### 3.1 저장소 구조 (개발 측)

```text
cadenza/                          # rkttu/cadenza monorepo
├── src/
│   ├── core/                    # 모든 변종이 공유 (namespace: Cadenza)
│   │   ├── Sh.cs / Fs.cs / Http.cs / Env.cs / Prompt.cs / Json.cs
│   │   └── CadenzaRuntime.cs
│   ├── worker/                  # namespace: Cadenza.Worker
│   │   ├── Worker.cs
│   │   └── Log.cs
│   ├── web/                     # namespace: Cadenza.Web
│   │   └── Web.cs
│   ├── mcp/                     # namespace: Cadenza.Mcp
│   │   ├── Mcp.cs
│   │   └── Log.cs
│   └── agent/                   # namespace: Cadenza.Agent
│       ├── Agent.cs / AgentServer.cs / AgentRepl.cs / AgentResponsesEndpoint.cs
│       └── OpenAiWire.cs / OpenAiResponsesWire.cs
├── packaging/                   # SDK 패키지 + 보조 패키지
│   ├── Cadenza/Sdk/
│   │   ├── Sdk.props
│   │   └── Sdk.targets
│   ├── Cadenza.Worker/Sdk/
│   │   └── Sdk.targets
│   ├── Cadenza.Web/Sdk/...
│   ├── Cadenza.Mcp/Sdk/...
│   ├── Cadenza.Agent/Sdk/...
│   ├── Cadenza.Templates/             # dotnet new 템플릿 (§13.4)
│   └── Cadenza.SdkResolver/           # 옵트인 SDK resolver (§14)
├── samples/                            # canonical .cs 스크립트
├── docs/                               # spec.md, troubleshooting.md, publishing-single-binary.md (영문 + .ko.md)
└── skills/                             # vibe coding 어댑터 (§13.4)
```

빌드는 5개의 독립 NuGet SDK 패키지 + 보조 패키지(`Cadenza.Templates`, `Cadenza.SdkResolver`)를 산출.

### 3.2 SDK 패키지 구조 (배포 측)

각 SDK 패키지는 표준 MSBuild SDK 레이아웃:

```text
Cadenza.nupkg/
├── Sdk/
│   ├── Sdk.props
│   └── Sdk.targets
├── content/
│   ├── core/*.cs
│   └── _globals.cs (선택)
└── Cadenza.nuspec
```

### 3.3 `Sdk.props` 핵심 내용 (예: `Cadenza.Web`)

```xml
<Project>
  <!-- 1) 기반 SDK 임포트 -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk.Web" />

  <!-- 2) Cadenza 적용: 소스 파일과 global usings -->
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../content/core/**/*.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)../content/web/**/*.cs" />

    <Using Include="Cadenza.Fs"      Static="true" />
    <Using Include="Cadenza.Web.Web" Static="true" />
    <Using Include="System.Console"  Static="true" />
  </ItemGroup>

  <!-- 3) Default 컴파일 설정 -->
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- 4) Default deployment 설정 (publish 시점에만 효과) -->
  <PropertyGroup>
    <PublishAot>false</PublishAot>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <SelfContained>true</SelfContained>
  </PropertyGroup>
</Project>
```

`Sdk.targets`는 대칭적으로 기반 SDK의 `Sdk.targets`를 import. 세 변종 모두 4번 블록은 동일 — deployment 정책이 변종 간 통일됨.

### 3.4 Deployment 단계 (세 가지 tier)

Cadenza SDK는 세 가지 deployment 단계를 명확히 분리한다.

| 단계 | Trigger | 산출물 | 동작 모드 |
| ------ | --------- | -------- | --------- |
| **Iteration** | `dotnet run app.cs` | 실행만 | JIT, 표준 동작, hot reload 가능 |
| **Distribution** | `dotnet publish app.cs -r <rid> -c Release` | 단일 자체 포함 바이너리 (~60-80MB) | R2R + SingleFile + SCD (SDK default) |
| **Performance-critical** | 위 + `#:property PublishAot=true` | NativeAOT 바이너리 (~10-30MB) | AOT, AOT-clean 의존성 필요 |

핵심 속성은 모두 `dotnet publish` 시점에만 효과 발휘. `dotnet run`은 모든 단계에서 JIT으로 동작하므로 **iteration 비용은 0**.

`PublishAot=true` opt-in 시 SingleFile/R2R 설정은 AOT 결과물 안에 자연스럽게 흡수 (AOT가 더 강한 조건이므로 우선).

---

## 4. Tier 1 — Bare Names per SDK Variant

`global using static`을 통해 접두사 없이 호출되는 멤버. 변종별로 다름.

### 4.1 `Cadenza` (콘솔) — 9개

| Bare name | Origin | Signature | Notes |
| ----------- | -------- | ----------- | ------- |
| `Run` | `Cadenza.Sh` | `int Run(string cmd)` | **쉘 실행**, exit code 반환 |
| `Capture` | `Cadenza.Sh` | `string Capture(string cmd)` | stdout 캡처 |
| `ReadText` | `Cadenza.Fs` | `string ReadText(string path)` | UTF-8 텍스트 읽기 |
| `WriteText` | `Cadenza.Fs` | `void WriteText(string, string)` | UTF-8 텍스트 쓰기 |
| `Glob` | `Cadenza.Fs` | `IEnumerable<string> Glob(string)` | 패턴 매칭 |
| `TempDir` | `Cadenza.Fs` | `IDisposable TempDir()` | `using` 시 자동 정리 |
| `WriteLine` | `System.Console` | (표준) | stdout 출력 |
| `Write` | `System.Console` | (표준) | stdout 출력 |
| `ReadLine` | `System.Console` | (표준) | stdin 한 줄 |

### 4.2 `Cadenza.Worker` — 6개

| Bare name | Origin | Signature | Notes |
| ----------- | -------- | ----------- | ------- |
| `Run` | `Cadenza.Worker.Worker` | `Task Run(Func<CancellationToken, Task> work)` | **호스트 시작 + BackgroundService 실행** |
| `ReadText` | `Cadenza.Fs` | `string ReadText(string)` | (공유) |
| `WriteText` | `Cadenza.Fs` | `void WriteText(string, string)` | (공유) |
| `Glob` | `Cadenza.Fs` | `IEnumerable<string> Glob(string)` | (공유) |
| `WriteLine` | `System.Console` | (표준) | (공유) |
| `Config` | `Cadenza.Worker.Worker` | `T Config<T>(string key)` | IConfiguration 단축 |

**의도적으로 tier 2로 미루는 것**: `Log.Info/Warn/Error` — 워커 스크립트 작성자가 가장 헷갈리는 부분이 로깅 vs. WriteLine 분기이므로, 1.x 동안은 `Log.*` 명시 호출만 허용.

### 4.3 `Cadenza.Web` — 9개

| Bare name | Origin | Signature | Notes |
| ----------- | -------- | ----------- | ------- |
| `Get` | `Cadenza.Web.Web` | `void Get(string path, Delegate handler)` | GET 엔드포인트 매핑 |
| `Post` | `Cadenza.Web.Web` | `void Post(string path, Delegate handler)` | POST 엔드포인트 매핑 |
| `Put` | `Cadenza.Web.Web` | `void Put(string path, Delegate handler)` | PUT 엔드포인트 매핑 |
| `Delete` | `Cadenza.Web.Web` | `void Delete(string path, Delegate handler)` | DELETE 엔드포인트 매핑 |
| `Map` | `Cadenza.Web.Web` | `void Map(string path, Delegate handler)` | 모든 메서드 매핑 |
| `Run` | `Cadenza.Web.Web` | `Task Run()` | **웹 서버 시작** |
| `ReadText` | `Cadenza.Fs` | `string ReadText(string)` | (공유) |
| `WriteText` | `Cadenza.Fs` | `void WriteText(string, string)` | (공유) |
| `Glob` | `Cadenza.Fs` | `IEnumerable<string> Glob(string)` | (공유) |

### 4.4 컨텍스트 의존 bare name 규칙

`Run`은 SDK 변종에 따라 의미가 다름:

- `Cadenza` → `Sh.Run` (쉘)
- `Cadenza.Worker` → `Worker.Run` (호스트)
- `Cadenza.Web` → `Web.Run` (서버)
- `Cadenza.Mcp` → `Mcp.Run` (MCP 서버, §13.1)

이게 혼란이 아닌 이유: **`#:sdk` 선언이 스크립트 첫 줄에 있고, 그 선언이 `Run`의 의미를 결정**. Python 스크립트가 첫 줄 `#!/usr/bin/env python3`로 컨텍스트를 잡듯, 동등한 효과가 발생. 한 스크립트 안에서 `Run`의 의미는 항상 단일.

쉘 실행이 필요한 워커/웹/MCP 스크립트는 `Sh.Run(...)` 접두사 호출 — tier 2 경로는 모든 변종에서 동일하게 열림.

### 4.5 Tier 1 안정성 약속

- v0.x → v1.0 전환 시점에 최종 확정.
- v1.0 이후 tier 1 멤버는 deprecate되더라도 제거하지 않음.
- tier 1 신규 추가는 메이저 버전 격상에서만 허용.
- 변종 간 bare name의 의미 변경은 메이저 버전 격상에서만 허용.

---

## 5. Tier 2 — Prefixed Modules

모든 변종에서 공통으로 접근 가능. 변종별 추가 모듈은 별도 표시.

### 5.1 `Sh` (Shell execution) — 전 변종 공통

```csharp
int    Sh.Run(string cmd, bool throwOnError = false);
string Sh.Capture(string cmd);                          // throws on non-zero
void   Sh.Pipe(string cmd);                             // stream stdout
Task<int>    Sh.RunAsync(string cmd, CancellationToken ct = default);
Task<string> Sh.CaptureAsync(string cmd, CancellationToken ct = default);
```

### 5.2 `Fs` (File system) — 전 변종 공통

```csharp
string Fs.ReadText(string path);
void   Fs.WriteText(string path, string content);
IEnumerable<string> Fs.Glob(string pattern);
IDisposable Fs.TempDir();

bool   Fs.Exists(string path);
void   Fs.Delete(string path);
void   Fs.Move(string src, string dst);
void   Fs.Copy(string src, string dst);
void   Fs.MakeDir(string path);
byte[] Fs.ReadBytes(string path);
void   Fs.WriteBytes(string path, byte[] bytes);
Task<string> Fs.ReadTextAsync(string, CancellationToken ct = default);
```

### 5.3 `Http` (HTTP, async-first) — 전 변종 공통

```csharp
Task<T>      Http.GetJson<T>(string url, JsonSerializerContext ctx, CancellationToken ct = default);
Task<TResp>  Http.PostJson<TReq, TResp>(string url, TReq body, JsonSerializerContext ctx, CancellationToken ct = default);
Task<string> Http.GetText(string url, CancellationToken ct = default);
Task         Http.Download(string url, string path, CancellationToken ct = default);
HttpClient   Http.Client { get; }
```

`JsonSerializerContext`는 AOT 호환을 위한 명시적 요구. AOT opt-in 시 추가 작업 없이 동작.

### 5.4 `Env` (Environment & arguments) — 전 변종 공통

```csharp
string?  Env[string key];
string   Env.Get(string key, string defaultValue);
string[] Env.Args { get; }
string   Env.Cwd { get; }
void     Env.Exit(int code);
bool     Env.IsCi { get; }
bool     Env.IsWindows { get; }
bool     Env.IsMacOS { get; }
bool     Env.IsLinux { get; }
```

### 5.5 `Prompt` — 콘솔/워커만 (웹·MCP에서는 의미 없음)

```csharp
bool   Prompt.Confirm(string question, bool defaultValue = false);
int    Prompt.Select(string question, string[] options);
string Prompt.Text(string question, string? defaultValue = null);
string Prompt.Password(string question);
```

### 5.6 `Json` — 전 변종 공통

```csharp
T      Json.Parse<T>(string json, JsonSerializerContext ctx);
string Json.Stringify(object value, JsonSerializerContext ctx);
```

reflection-based 오버로드 미제공 (AOT clean by construction).

### 5.7 `Worker` — 워커 변종 전용

```csharp
Task Worker.Run(Func<CancellationToken, Task> work);
Task Worker.Run(Func<IServiceProvider, CancellationToken, Task> work);
T    Worker.Config<T>(string key);                       // tier 1
T    Worker.Service<T>() where T : notnull;
```

### 5.8 `Log` — 워커/MCP 변종 전용

```csharp
Log.Info(string message);
Log.Warn(string message);
Log.Error(string message, Exception? ex = null);
Log.Debug(string message);
```

`ILogger`로 라우팅. 콘솔/웹 변종에서는 namespace 미노출. **MCP 변종에서는 stderr 라우팅이 critical** — stdout은 MCP 프로토콜 전송에 점유됨.

### 5.9 `Web` — 웹 변종 전용

```csharp
void Web.Get(string path, Delegate handler);          // tier 1 bare
void Web.Post(string path, Delegate handler);         // tier 1 bare
void Web.Put(string path, Delegate handler);          // tier 1 bare
void Web.Delete(string path, Delegate handler);       // tier 1 bare
void Web.Map(string path, Delegate handler);          // tier 1 bare
Task Web.Run();                                        // tier 1 bare

WebApplication Web.App { get; }                        // 고급 사용자용
IServiceCollection Web.Services { get; }
void Web.UseHttps();
void Web.UseCors(string policy = "default");
```

### 5.10 `Mcp` — MCP 변종 전용

§13.1.4 참조.

---

## 6. Async Strategy

| 도메인 | 기본 형태 | 비동기 오버로드 |
| -------- | ---------- | ---------------- |
| File I/O | 동기 (`ReadText`) | `ReadTextAsync` 명시적 |
| HTTP | 비동기 only | 없음 |
| Shell | 동기 우선 | `RunAsync`, `CaptureAsync` |
| Worker.Run | 비동기 (`await Run(...)`) | 없음 |
| Web.Run | 비동기 (`await Run()`) | 없음 |
| Mcp.Run | 비동기 (`await Run()`) | 없음 |

근거: 콘솔 한 줄 스크립트의 가독성 + 워커/웹/MCP의 자연스러운 async 흐름.

---

## 7. Error Handling

- C# 예외 모델 그대로. `Result<T, E>` 도입 없음.
- `Sh.Run`은 exit code 반환, throw하지 않음 (caller 결정).
- `Sh.Capture`는 exit ≠ 0 시 throw.
- `Worker.Run`/`Web.Run`/`Mcp.Run` 내부의 예외는 호스트 표준 동작에 위임 (Worker: 로그 후 재시작 정책, Web: problem details 응답, MCP: JSON-RPC error response).

---

## 8. Canonical Example Scripts

> 아래 예제의 `@1.0.15`는 본 문서 작성 시점의 latest 1.0.x를 가리키는 placeholder다. 실제 사용 시에는 nuget.org의 최신 stable 버전으로 교체. 정책 근거는 §11.4.

### 8.1 콘솔: 파일 처리

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.155

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
```

### 8.2 콘솔: 배포 가드

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

var branch = Capture("git rev-parse --abbrev-ref HEAD").Trim();
if (branch != "main")
{
    WriteLine($"Refusing to deploy from branch '{branch}'");
    Env.Exit(1);
}

Run("dotnet build -c Release");
Run("dotnet publish -c Release -o ./dist");
```

> 이 스크립트 자체를 단일 바이너리로 배포하려면:
> `dotnet publish deploy.cs -r linux-x64 -c Release` → `./bin/Release/.../deploy` 산출 (~60-80MB).
> 최소 사이즈가 필요하면 첫 줄 다음에 `#:property PublishAot=true` 추가 (~10-30MB, 의존성 AOT 호환 확인 필요).

### 8.3 워커: 주기적 작업

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

### 8.4 워커: 설정 + DI

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.15

var endpoint = Worker.Config<string>("Api:Endpoint")
    ?? throw new InvalidOperationException("Api:Endpoint missing");

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        var status = await Http.GetText($"{endpoint}/health", ct);
        Log.Info($"Upstream status: {status}");
        await Task.Delay(TimeSpan.FromMinutes(1), ct);
    }
});
```

### 8.5 웹: Minimal API

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.15

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
```

> 컨테이너에 단일 바이너리로 배포 시: `dotnet publish app.cs -r linux-x64 -c Release`로 ~80MB 자체 포함 바이너리 산출. 컨테이너 베이스 이미지를 `mcr.microsoft.com/dotnet/runtime-deps`로 가져가면 최종 이미지가 100MB 안쪽으로 정리됨.

### 8.6 웹: 정적 파일 + API 혼합

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.15

Web.App.UseStaticFiles();          // 고급 옵션은 Web.App로 접근
Get("/api/files", () => Glob("wwwroot/**/*.html").Select(Path.GetFileName));

await Run();
```

추가 예제: MCP 변종은 §13.1.7, Agent 변종은 §13.2.8 참조.

---

## 9. Scope Boundaries

### 9.1 1.0.x에서 출시 완료

- **5개 SDK 패키지**: `Cadenza`, `Cadenza.Worker`, `Cadenza.Web` (v1.0.0 / 1.0.1), `Cadenza.Mcp` (1.0.x), `Cadenza.Agent` (1.0.15)
- §5의 tier 2 모듈 풀세트 + 변종 전용 모듈 (`Worker`, `Web`, `Mcp`, `Agent`)
- §4의 각 변종 tier 1
- 변종별 canonical example (`samples/`에 13개 .cs 스크립트)
- Deployment 매트릭스 CI: 3 OS × 변종 × {default publish, AOT publish — Agent 제외}
- README per SDK + 공통 "Publishing as a single binary" 가이드 (영문 default, `.ko.md` 자매 파일)
- **보조 자산**: `Cadenza.Templates` (`dotnet new cadenza-*` 스타터), `Cadenza.SdkResolver` (옵트인, §14), Cadenza skill (AI 어시스턴트 어댑터, §13.4)

### 9.2 1.1~1.2에서 후속 평가

| 항목 | 시점 / 트리거 |
| ------ | ---------------- |
| `Cadenza.Wasi` (WASI wasm 모듈 변종) | `wasm-tools` workload GA 신호 기반 (§13.3) |
| `Cadenza.Aspire` (Aspire AppHost 변종) | v1.2+ 평가 |
| `Cadenza.Lambda` / `Cadenza.Function` | v1.2+ 평가 |
| `Cadenza.MewUI` (SwiftUI-style 데스크톱) | MewUI 1.0 도달 후 평가 |
| `Csv` 모듈 | 옵트인 패키지 (사용자 요청 누적 시) |
| `Hangul` (한국어 특화) | 별도 패키지 |
| CLI argparse | `Cadenza.Cli`는 1.0.10에서 도입 후 1.0.11에서 회수 (commit f2d8b66). 재도입 시 별도 4-criterion 평가 필요 |
| 진행률/스피너/TUI | `Cadenza.Tui` 옵트인 |
| Trimmed publish 가이드 | opt-in 전제, 1.2에서 작성 검토 |
| MCP HTTP/Streamable HTTP 전송 | `Cadenza.Mcp` open decision (§13.1.9 a) |

---

## 10. Open Decisions

### 10.1 명칭 & 케이싱 — RESOLVED ✅

**결정**: 브랜드명 `Cadenza`. SDK 패키지 ID는 `Cadenza` / `Cadenza.Worker` / `Cadenza.Web`로 시작.

**확인 결과** (1.0.0 출시 시점):

- nuget.org에서 `Cadenza` 패키지 ID 점유 — 5개 SDK 모두 `Cadenza` / `Cadenza.Worker` / `Cadenza.Web` / `Cadenza.Mcp` / `Cadenza.Agent`로 게시 완료. `.Sdk` 접미사 fallback은 불필요.
- GitHub 호스팅 — `rkttu/cadenza` 단일 monorepo (§10.9 참조).
- 도메인 — 현재 미사용. README/NuGet 페이지가 정식 entry point.

브랜드 명명 배경은 §1.2 참조.

### 10.2 `global using` 메커니즘 — RESOLVED ✅

**결정**: SDK `Sdk.props`의 `<Using>` 항목(패턴 B) 채택. 모든 변종의 `packaging/Cadenza*/Sdk/Sdk.props`가 동일한 패턴을 사용. `_globals.cs` 방식은 미사용.

### 10.3 HTTP sync wrapper — RESOLVED ✅

**결정**: async-only 유지 (`Http.GetText`, `Http.GetJson<T>`, `Http.PostJson<,>`, `Http.Download` 모두 `Task` 반환). `await` 한 글자가 sync 환상을 만드는 비용보다 작다는 판단. 변경 시 메이저 격상 필요.

### 10.4 `Sh` 실행 시 셸 경유 방식 — RESOLVED ✅

**결정**: 자동 OS 분기 (`cmd /c` on Windows / `/bin/sh -c` elsewhere). Windows에서는 `Sh.Capture` stdout을 호스트 OEM 코드 페이지로 디코딩 (commit 3c083d2). 셸 메타문자(`|`, `>`, `&&`)는 의도된 사용 패턴.

### 10.5 Deployment 정책 세부 조정 — RESOLVED ✅

기본 골격(AOT off / R2R+SingleFile+SCD on)은 §3.4에서 확정. 세부 결정:

- **(a) `IncludeNativeLibrariesForSelfExtract`** — `false` (외부 추출). 모든 변종 Sdk.props에 명시.
- **(b) `EnableCompressionInSingleFile`** — `true` (Worker/Web/Mcp/Agent 기준). Cadenza(콘솔)도 동일.
- **(c) `PublishTrimmed`** — default off 유지, 90a75df에서 file-based AOT default override 추가. trim 가이드는 위키 [Troubleshooting](https://github.com/rkttu/cadenza/wiki/Troubleshooting) 페이지로 이관 완료.

### 10.6 비대화형 Prompt 정책

`Env.IsCi == true`일 때 `Prompt.*` 동작.

**권장**: defaultValue 우선 → 환경변수(`CADENZA_PROMPT_<NAME>`) → 없으면 throw.

**현재 상태**: 미구현. 1.1 안에서 결정/구현 또는 1.2로 이월.

### 10.7 워커 `Log` vs. `WriteLine` — RESOLVED ✅

**결정**: `WriteLine`은 stdout 그대로, `Log.*`는 ILogger. 두 채널의 분리를 명시적으로 유지. `Cadenza.Mcp`에서는 stdio 오염 방지를 위해 `WriteLine` 자체를 미노출 (§13.1.4).

### 10.8 웹 변종 라우팅 컨벤션

`Get("/path", handler)` 외에 `Group("/api", g => { g.Get(...); })` 같은 그룹 라우팅 도입.

**권장**: 1.1에서는 단일 라우트만 유지. 그룹 라우팅은 사용자 요청 누적 시 1.2 평가.

### 10.9 저장소 & 조직 — RESOLVED ✅

**결정**: 단일 monorepo `rkttu/cadenza` 채택. 별도 `cadenza-sdks` org는 미신설 — 현 시점에서 검색성 이득이 org 분리 비용을 정당화하지 못한다는 판단. 필요 시 1.x 라인 안에서 org 이전 가능 (NuGet 패키지 ID는 안정).

### 10.10 Agent의 AOT 호환

`Cadenza.Agent`의 `Tool(name, desc, handler)`는 `AIFunctionFactory.Create`를 사용하며 이는 reflection 기반 — AOT 모드에서 trim warning. 자세한 내용은 §13.2.10.

**권장**: 1.x 동안은 AOT-incompatible 상태를 유지하고 README/Sdk.props에서 명시적 경고. AOT clean 경로는 별도 source-generator 기반 wrapper(`Cadenza.Agent.Tools.Source`)로 평가, 1.2+에서 결정.

---

## 11. Build & Publish (구현 노트)

### 11.1 SDK 패키지 빌드

- 표준 `dotnet pack` + custom `.nuspec`으로 빌드.
- `Sdk/Sdk.props` 및 `Sdk/Sdk.targets`가 패키지 루트의 `Sdk/` 폴더에 위치해야 함 (MSBuild SDK 규약).
- CI 매트릭스: GitHub Actions, 3 OS × 3 변종 × {default publish, AOT publish}.

### 11.2 사용자 측 publish 워크플로우

기본 (자체 포함 + R2R + SingleFile):

```bash
dotnet publish app.cs -r linux-x64 -c Release
# → bin/Release/net10.0/linux-x64/publish/app  (~60-80MB)
```

압축 활성화 시(권장):

```bash
dotnet publish app.cs -r linux-x64 -c Release \
  -p:EnableCompressionInSingleFile=true
# → ~30-40MB, 첫 실행 시 압축 해제 1회 비용
```

NativeAOT 옵트인 시 (스크립트 첫 부분에 `#:property PublishAot=true` 추가):

```bash
dotnet publish app.cs -r linux-x64 -c Release
# → ~10-30MB, AOT-clean 의존성 필요
```

### 11.3 게시

- 초기 NuGet 게시: `0.1.0-preview.1` (deprecated; v1.0.0 직진 채택).
- v1.0.0 / v1.0.1 게시 완료, 이후 1.0.x 패치 라인 (현재 1.0.15) — Cadenza.Mcp/Agent 추가 포함.
- v1.1.0 마이너 격상은 §12 참조.
- 향후 비호환 변경은 v2.0.0 메이저 격상에서만.

### 11.4 사용자 측 버전 핀 정책

`<Project Sdk="...">` 어트리뷰트와 `#:sdk` 디렉티브의 버전 필드는 **정확한 SemVer만 허용된다.** MSBuild SDK reference resolver(NuGet 기반)는 PackageReference와 달리 floating 패턴(`1.*`, `1.0.*`)을 평가하지 않는다 — 평가 단계가 restore 이전에 있기 때문.

따라서 **권장(기본) 패턴**은 다음 둘 중 하나:

```csharp
// 패턴 A: 스크립트에 정확한 버전 핀 (canonical)
#:sdk Cadenza@1.0.155
```

```json
// 패턴 B: 스크립트와 같은 디렉터리의 global.json에 중앙화
{
  "msbuild-sdks": {
    "Cadenza": "1.0.15"
  }
}
```

```csharp
// 그리고 스크립트는 버전 생략
#:sdk Cadenza
```

새 릴리스로 이동하려면 위 두 위치 중 어느 쪽이든 버전 문자열을 직접 갱신한다. "정확한 버전 핀"이 단일 파일 스크립트의 재현성·이식성을 보장하는 본질적 속성이라는 판단에서 이 정책을 기본으로 유지한다.

#### 11.4.1 선택적 floating: `Cadenza.SdkResolver`

"매번 최신 1.0.x를 받고 싶다"는 사용 시나리오(개인 머신, 빠른 prototyping)를 위해 옵트인 MSBuild SDK resolver를 제공한다. 설치 시 다음 두 표현이 동작:

```csharp
#:sdk Cadenza             // 버전 생략 → 최신 stable
#:sdk Cadenza.Agent@latest // 명시적 latest 토큰
```

resolver는 nuget.org를 조회해 가장 높은 stable 버전을 골라 글로벌 패키지 폴더에 풀어둔다. 설치/설계/제한은 §14에서 정의한다. **이 경로는 명시적 opt-in**이며 default 워크플로우가 아니다 — production 스크립트, 공유 Gist, README 예제는 모두 패턴 A(정확한 버전 핀)를 따른다.

---

## 12. Next Steps

### 12.1 1.0.x 출시 완료 (회고)

다음 항목들은 모두 1.0.x 패치 라인에서 게시 완료:

1. **5개 SDK 게시 완료**: `Cadenza` / `Cadenza.Worker` / `Cadenza.Web` (1.0.0 / 1.0.1), `Cadenza.Mcp` (cc12416), `Cadenza.Agent` (22e3aca).
2. **`Cadenza.Templates`** — `dotnet new cadenza-console / -worker / -web / -mcp / -agent` 스타터 (5b3f0a4, 66d9bf9).
3. **`Cadenza.SdkResolver`** — 옵트인 MSBuild SDK resolver. 1.0.10에서 `Cadenza.Cli`와 함께 도입했다가 1.0.11에서 CLI를 회수 (f2d8b66). resolver는 standalone 패키지로 유지 (§14).
4. **Codex CLI compatibility** — `POST /v1/responses` (OpenAI Responses API) 엔드포인트로 Agent가 Codex의 클라이언트-소유 도구 모델을 그대로 통과시킴 (09d201f).
5. **Env.ScriptPath + 기본 transitive `#:include`** — file-based program 파일 경로 노출, 중첩 `#:include`/`#:package`/`#:property` 디렉티브 기본 활성화 (4cbcd7a).
6. **영문 default + `.ko.md` 자매 패턴** — 모든 README/문서가 i18n 분리 적용 (928e2bb).
7. **공개 API XML 문서** — 모든 public 멤버에 영문 XML doc 추가 (4b6ba60).
8. **Cadenza skill (vibe coding 어댑터)** — Claude Code · Cursor · Copilot · Continue · Aider용 정적 어댑터 (96dcd4f, §13.4).

### 12.2 1.1 진입 작업 (이번 마이너의 스코프)

이번 마이너는 **출시된 표면의 명세 동결**이 핵심이며 신규 변종은 추가하지 않는다.

1. **명세 정합** — 본 문서가 1.0.x 출시 표면을 모두 반영 (Mcp/Agent §13, SdkResolver §14, Open Decisions 일괄 triage).
2. **Cadenza.Agent 표면 동결** — Tier 1 (`Tool`, `SystemPrompt`, `Use*`, `Run`, `ChatLoop`, `Reply`) + Responses API 와이어를 1.1에서 잠근다. 이후 비호환 변경은 2.0.
3. **`/v1/responses` 호환성 매트릭스 작성** — 현재 Codex 0.x 검증 완료. Aider/Continue/Cursor에 대해 동일 매트릭스를 1.1 안에서 작성.
4. **Tier 1 안정성 약속 (§4.5) 재선언** — 1.0.x에서 추가된 변종들을 포함해 "1.x 동안 deprecate 가능하나 제거 불가" 보장을 명시.
5. **Open Decision 10.6 (비대화형 Prompt) 매듭** — 구현하거나 1.2 이월 결정.

### 12.3 1.2 후보

1.2의 스코프는 다음 후보들 중 1.1 출시 후 사용자 데이터·요청 빈도 기반으로 결정한다:

- **MCP HTTP 전송** — `Cadenza.Mcp.Web` 분리 또는 property toggle (§13.1.9 a).
- **AOT-clean Agent 경로** — source-generator 기반 `Tool` 래퍼 (§10.10).
- **웹 그룹 라우팅** — `Group("/api", g => { ... })` (§10.8).
- **`Cadenza.Wasi`** — `wasm-tools` workload GA 신호 시 즉시 진입 (§13.3).
- **`Cadenza.Tasks` / `Cadenza.Tui` 옵트인 모듈**.
- **Cadenza skill Phase 2** — MCP 서버 형태(`Cadenza.Mcp.Server` dotnet tool)로 어댑터를 동적 제공.

---

## 13. Variants

### 13.1 `Cadenza.Mcp` — shipped in 1.0.x

#### 13.1.1 개요

**Use case**: MCP (Model Context Protocol) 서버 작성. AI 어시스턴트(Claude Desktop, Cursor, Cline 등)가 호출 가능한 도구·리소스·프롬프트를 단일 `.cs` 파일로 정의.

**전략적 위치**: Cadenza 가족의 **AI-targeted 단일 SDK 변종**. Agent CLI, AI 도구 호출 스크립트 등 다른 AI 인접 use case는 기존 `Cadenza` (콘솔) SDK + 패키지 참조로 처리되며, Cadenza.Mcp는 *MCP 서버라는 특정 사용 패턴*에만 책임을 진다.

마케팅 시퀀스의 무게중심 — 첫 6주의 콘텐츠가 MCP/agent 각도에서 유입을 만들고, 그 시점의 핵심 제품 demo가 `Cadenza.Mcp` (자세한 시퀀스는 §12.2).

#### 13.1.2 Foundation

Cadenza.Mcp는 **공식 .NET MCP SDK `ModelContextProtocol`** 위의 thin ergonomic layer로 설계된다.

| 항목 | 값 |
| ------ | ----- |
| **공식 SDK** | `ModelContextProtocol` (v1.0 released 2026-03-05, current 1.3.x) |
| **유지보수** | Anthropic + Microsoft 공동 |
| **License** | Apache-2.0 (Cadenza.Mcp 본체는 MIT) |
| **공식 SDK 패키지 구성** | `ModelContextProtocol.Core` (최소 의존성) / `ModelContextProtocol` (메인 + 호스팅·DI) / `ModelContextProtocol.AspNetCore` (HTTP 호스팅) |
| **Cadenza.Mcp가 참조하는 패키지** | `ModelContextProtocol` (메인) — stdio 전송에 충분 |

**책임 분담**:

- **공식 SDK 담당**: 프로토콜 implementation, JSON-RPC 직렬화, transport, authorization, 도구 sampling 등.
- **Cadenza.Mcp 담당**: file-based scripting ergonomic shell — DI/호스팅 boilerplate 흡수, tier 1 bare name 노출, default deployment 설정.

**호환성 약속**: 사용자가 Cadenza.Mcp로 시작한 프로젝트를 정식 csproj로 전환할 때, 같은 `ModelContextProtocol` 패키지를 그대로 사용. `Tool(name, desc, handler)` 호출이 `[McpServerTool]` attribute 방식으로 바뀌는 정도의 변경만 필요하고, 비즈니스 로직은 unchanged. 카덴차와 협주곡의 관계가 MCP 영역에서도 동일하게 작동.

#### 13.1.3 Identity

| 항목 | 값 |
| ------ | ----- |
| **SDK ID** | `Cadenza.Mcp` |
| **기반 SDK** | `Microsoft.NET.Sdk` |
| **자동 PackageReference** | `ModelContextProtocol 1.3.x` |
| **Default transport** | stdio (Claude Desktop · Cursor · Cline 표준) |
| **HTTP 전송** | 1.2+ explore (별도 `Cadenza.Mcp.Web` SDK 또는 property toggle로 결정) |
| **Default deployment** | JIT + R2R/SingleFile/SCD on publish (가족 정책 동일) |
| **AOT 정책** | default off, `#:property PublishAot=true`로 opt-in (가족 정책 동일) |
| **Namespace** | `Cadenza.Mcp` |
| **메인 정적 클래스** | `Cadenza.Mcp.Mcp` (Worker/Web 변종의 double-naming 패턴 일관) |

#### 13.1.4 Tier 1 — Bare Names (7개)

| Bare name | Origin | Signature | Notes |
| ----------- | -------- | ----------- | ------- |
| `Tool` | `Cadenza.Mcp.Mcp` | `void Tool(string name, string description, Delegate handler)` | **MCP 도구 등록** |
| `Resource` | `Cadenza.Mcp.Mcp` | `void Resource(string uri, string name, Delegate handler)` | MCP 리소스 등록 |
| `Prompt` | `Cadenza.Mcp.Mcp` | `void Prompt(string name, string description, Delegate handler)` | MCP **프롬프트 템플릿** 등록 (대화형 `Cadenza.Prompt`와 무관) |
| `Run` | `Cadenza.Mcp.Mcp` | `Task Run()` | **MCP 서버 시작** (stdio) |
| `ReadText` | `Cadenza.Fs` | `string ReadText(string)` | (공유) |
| `WriteText` | `Cadenza.Fs` | `void WriteText(string, string)` | (공유) |
| `Glob` | `Cadenza.Fs` | `IEnumerable<string> Glob(string)` | (공유) |

**의도적으로 tier 1에서 제외된 것**:

- **`WriteLine`, `Write`, `ReadLine` (System.Console)** — stdio 전송에서 stdout이 MCP 프로토콜에 점유되어 있음. 사용자가 무심코 `WriteLine("debug")`를 호출하면 **클라이언트 측에서 JSON 파싱 오류로 연결 끊김**. Cadenza.Mcp에서는 diagnostic output을 `Log.*` (stderr 경유)로 강제 통일.
- **`Sh.Run`, `Sh.Capture`** — `Run`이 이미 MCP 서버 시작 의미로 점유. 충돌 방지 위해 `Sh.` 접두사 유지.
- **`Cadenza.Prompt` 모듈** — 대화형 prompt는 MCP 서버 컨텍스트에서 의미 없음 (서버가 사용자에게 직접 묻지 않고, AI 클라이언트를 통한 elicitation은 별도 메커니즘). 이름 공간 비워서 `Prompt`를 MCP 프롬프트 템플릿용으로 사용.

#### 13.1.5 Tier 2 — Prefixed Modules

`Mcp` 본체 외에 다음 모듈이 변종 전용으로 추가됨:

```csharp
// 호출 컨텍스트 — 현재 도구 호출의 메타데이터
McpContext  Mcp.Context { get; }
ILogger     Mcp.Logger { get; }                   // ILogger 직접 접근

// MCP 클라이언트 — 다른 MCP 서버에 연결
IMcpClient       Mcp.Client(string serverUrl);
Task<string>     Mcp.Client.CallTool(string name, object args);

// 고급 설정
McpServerOptions Mcp.Options { get; }

// HTTP 전송 (1.2+ explore — 1.x 동안은 stdio only)
// Task Mcp.RunHttp(int port = 8080);
```

공통 모듈은 모두 그대로 사용 가능 (`Fs`, `Http`, `Env`, `Json`, `Sh`).

`Log` 모듈은 Worker 변종과 동일한 시그니처로 노출 — **stdio 환경에서 stderr 라우팅이 critical하므로 반드시 사용**:

```csharp
Log.Info(string message);     // stderr
Log.Warn(string message);     // stderr
Log.Error(string message, Exception? ex = null);
Log.Debug(string message);
```

#### 13.1.6 Default `Sdk.props` (`Cadenza.Mcp`)

```xml
<Project>
  <!-- 1) 기반 SDK 임포트 -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

  <!-- 2) 공식 MCP SDK 자동 참조 -->
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="1.3.0" />
  </ItemGroup>

  <!-- 3) Cadenza 코드 + global usings -->
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../content/core/**/*.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)../content/mcp/**/*.cs" />

    <Using Include="Cadenza.Fs"          Static="true" />
    <Using Include="Cadenza.Mcp.Mcp"     Static="true" />
    <Using Include="Cadenza.Mcp.Log"     Static="true" />
    <!-- WriteLine 의도적 미노출 - stdio 오염 방지 (§13.1.4) -->
  </ItemGroup>

  <!-- 4) Default 컴파일 설정 -->
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- 5) Default deployment 설정 (가족 정책 동일) -->
  <PropertyGroup>
    <PublishAot>false</PublishAot>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <SelfContained>true</SelfContained>
  </PropertyGroup>
</Project>
```

#### 13.1.7 Canonical Examples

##### **기본: 파일 도구 노출**

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.15

Tool("read_file", "Read a UTF-8 text file from disk",
    async (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    async (string pattern) => Glob(pattern).ToArray());

await Run();
```

##### **Claude Desktop 측 등록**

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/path/to/server.cs"]
    }
  }
}
```

##### **리소스·프롬프트 포함 (확장 예제)**

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.15

Tool("get_weather", "Get current weather for a city",
    async (string city) =>
    {
        var key = Env["OPENWEATHER_API_KEY"] ?? throw new InvalidOperationException("API key missing");
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={key}";
        return await Http.GetText(url);
    });

Resource("readme://current", "Project README",
    () => ReadText("README.md"));

Prompt("review_code", "Review the given code for issues",
    (string code) => $"Please review this code for bugs, security, and style:\n\n{code}");

Log.Info("Cadenza.Mcp server starting on stdio");
await Run();
```

##### **Single-binary 배포** (dotnet tool 또는 단일 실행 파일)

```bash
dotnet publish server.cs -r linux-x64 -c Release \
  -p:EnableCompressionInSingleFile=true
# → ~30-40MB 단일 바이너리
# Claude Desktop config의 "command"를 이 바이너리로 변경 가능
```

#### 13.1.8 보안 경계

Cadenza.Mcp가 ergonomic하게 만드는 것이 *프로토콜과 호스팅*이지 *권한*은 아님을 명확히 한다.

- **파일·네트워크·셸 접근은 사용자가 의도적으로 `Tool` 안에 박는 동작.** SDK가 자동으로 노출하지 않음. `Cadenza.Mcp`는 `Fs`·`Http`·`Sh` 모듈에 대한 접근을 제공할 뿐, 자동으로 도구화하지 않는다.
- **사용자 입력 escape는 사용자 책임.** `Tool(handler)`가 받는 인자를 그대로 `Sh.Run`이나 `ReadText`에 넘기면 path traversal·command injection·SSRF 등 위험. README 보안 섹션에서 explicit warning + 권장 패턴 (path canonicalization, allowlist 등) 제시.
- **MCP 서버 자체는 임의 코드 실행 도구가 아님.** 정현 님(또는 사용자)이 작성한 도구만 노출. 동적 코드 평가, eval 류 기능은 미제공.
- **`Log.*`는 stderr 전용.** 절대 stdout으로 라우팅되지 않음. 사용자가 실수로 `Console.Out`을 직접 사용하지 않도록 `WriteLine`을 의도적으로 미노출 (§13.1.4).

#### 13.1.9 Open Decisions (Cadenza.Mcp 전용)

- **(a) HTTP/Streamable HTTP 전송 지원.** 1.x는 stdio only. HTTP는 별도 `Cadenza.Mcp.Web` SDK(`Microsoft.NET.Sdk.Web` 기반)로 분리할지, 같은 SDK 안에서 `#:property UseHttp=true`로 toggle할지 1.2에서 결정. 4-criterion test 적용 결과는 분리 쪽이 약간 우세.
- **(b) Resource·Prompt tier 1 포함 여부.** 1.0.x는 셋 다 tier 1 (MCP 3대 primitive 일관성). 6개월 사용 데이터 수집 후 Resource·Prompt 빈도가 Tool 대비 극히 낮으면 tier 2 강등 검토.
- **(c) 공식 SDK 버전 핀 정책.** Cadenza.Mcp 0.1.x는 `ModelContextProtocol 1.3.x` 패치 범위에 핀. 메이저 변화 시 Cadenza.Mcp도 메이저 격상. v1.0.0 도달 시 동결.
- **(d) Bullseye-style declarative design vocabulary 차용.** `Tool(name, desc, handler)` 형태가 이미 declarative 패턴이지만, 추가적으로 `DependsOn` 같은 의존성 표현이 의미 있을지 검토. 1.2+ 평가.

### 13.2 `Cadenza.Agent` — shipped in 1.0.x

#### 13.2.1 개요

**Use case**: 로컬/원격 LLM에 도구를 붙여 만든 AI 에이전트를, **OpenAI 호환 HTTP 엔드포인트**(`/v1/chat/completions`, `/v1/responses`, `/v1/models`)로 노출하는 단일 `.cs` 파일 서버. Codex · Aider · Continue · Cursor · sgpt 같은 OpenAI 클라이언트가 `OPENAI_BASE_URL=http://localhost:8080/v1`만 바꾸면 그대로 연결.

**전략적 위치**: Cadenza 가족의 **AI-execution 변종**. `Cadenza.Mcp`가 *AI에게 도구를 노출*하는 서버라면, `Cadenza.Agent`는 *AI를 안에 품고 다른 AI 클라이언트에 노출*하는 서버. 둘은 서로 다른 사용자 세그먼트(에이전트 작성자 vs MCP 도구 작성자)에 대응하며, 4-criterion test 통과.

#### 13.2.2 Foundation

Cadenza.Agent는 **`Microsoft.Extensions.AI`** (MEAI)와 **`Microsoft.AspNetCore`** Minimal API 위의 thin ergonomic layer로 설계된다.

| 항목 | 값 |
| ------ | ----- |
| **LLM 추상화** | `Microsoft.Extensions.AI` 10.6.0 — vendor-neutral `IChatClient` |
| **OpenAI / Azure / Anthropic 브리지** | `Microsoft.Extensions.AI.OpenAI` 10.6.0 + `OpenAI` 2.10.0 |
| **Ollama 브리지** | `OllamaSharp` 5.4.25 (네이티브 `IChatClient` 구현) |
| **호스팅** | `Microsoft.Extensions.Hosting` 10.0.0 |
| **HTTP 서버** | `Microsoft.NET.Sdk.Web` (기반 SDK) |
| **License** | Cadenza.Agent 본체는 MIT; 의존성 라이선스는 각각 |

**책임 분담**:

- **MEAI 담당**: `IChatClient` 추상화, function-invocation 미들웨어 (`UseFunctionInvocation`), 스트리밍.
- **Cadenza.Agent 담당**: file-based scripting ergonomic shell — 도구 등록 syntax (`Tool(name, desc, handler)`), backend 선택 단축 (`UseOllama` / `UseOpenAi` / `UseAnthropic` / `UseAzureOpenAi`), OpenAI 호환 wire (`/v1/chat/completions` SSE 스트리밍 + `/v1/responses` Codex 호환).

**호환성 약속**: 사용자가 Cadenza.Agent로 시작한 프로젝트를 정식 csproj로 전환할 때, 같은 `Microsoft.Extensions.AI` + `Microsoft.AspNetCore` 조합을 그대로 사용. `Tool(name, desc, handler)` 호출이 `AIFunctionFactory.Create(handler, …)` 호출로 바뀌는 정도의 변경만 필요하고, 비즈니스 로직과 도구 본체는 unchanged.

#### 13.2.3 Identity

| 항목 | 값 |
| ------ | ----- |
| **SDK ID** | `Cadenza.Agent` |
| **기반 SDK** | `Microsoft.NET.Sdk.Web` |
| **자동 PackageReference** | `Microsoft.Extensions.AI` 10.6.x, `Microsoft.Extensions.AI.OpenAI` 10.6.x, `OpenAI` 2.10.x, `OllamaSharp` 5.4.x, `Microsoft.Extensions.Hosting` 10.0.x, `System.Text.Encoding.CodePages` 10.0.x |
| **Default frontend** | OpenAI 호환 HTTP 서버 (`localhost:8080`) |
| **Default transport** | HTTP/1.1 + SSE 스트리밍 |
| **REPL 모드** | `await ChatLoop()` — HTTP 없이 대화형 콘솔만 |
| **Default deployment** | JIT + R2R/SingleFile/SCD on publish (가족 정책 동일) |
| **AOT 정책** | **default off, opt-in 불가 (AOT-incompatible)** — §13.2.10 |
| **Namespace** | `Cadenza.Agent` |
| **메인 정적 클래스** | `Cadenza.Agent.Agent` (다른 변종의 double-naming 일관) |

#### 13.2.4 Tier 1 — Bare Names (16개)

| Bare name | Origin | Signature | Notes |
| ----------- | -------- | ----------- | ------- |
| `Tool` | `Cadenza.Agent.Agent` | `void Tool(string name, string description, Delegate handler)` | **도구 등록** — handler의 파라미터/리턴 타입이 자동으로 JSON 스키마화 |
| `SystemPrompt` | `Cadenza.Agent.Agent` | `void SystemPrompt(string text)` | 모든 대화에 prepend되는 시스템 메시지 |
| `UseChatClient` | `Cadenza.Agent.Agent` | `void UseChatClient(IChatClient)` | 임의의 MEAI `IChatClient` 주입 |
| `UseOllama` | `Cadenza.Agent.Agent` | `void UseOllama(string model, string baseUrl = "http://localhost:11434")` | 로컬 Ollama |
| `UseOpenAi` | `Cadenza.Agent.Agent` | `void UseOpenAi(string model, string? apiKey = null)` | API key는 `OPENAI_API_KEY` fallback |
| `UseAnthropic` | `Cadenza.Agent.Agent` | `void UseAnthropic(string model, string? apiKey = null)` | OpenAI 호환 엔드포인트(`api.anthropic.com/v1/`) 경유 |
| `UseAzureOpenAi` | `Cadenza.Agent.Agent` | `void UseAzureOpenAi(string endpoint, string deployment, string? apiKey = null)` | `AZURE_OPENAI_API_KEY` fallback |
| `Run` | `Cadenza.Agent.Agent` | `Task Run()` | **HTTP 서버 시작** — `localhost:8080`에 OpenAI 호환 엔드포인트 노출 |
| `ChatLoop` | `Cadenza.Agent.Agent` | `Task ChatLoop()` | 대화형 콘솔 REPL (HTTP 미사용) |
| `Reply` | `Cadenza.Agent.Agent` | `Task<string> Reply(string prompt)` | 단일 샷 호출 — 프롬프트 → 응답 문자열 |
| `ReadText` | `Cadenza.Fs` | `string ReadText(string)` | (공유) |
| `WriteText` | `Cadenza.Fs` | `void WriteText(string, string)` | (공유) |
| `Glob` | `Cadenza.Fs` | `IEnumerable<string> Glob(string)` | (공유) |
| `WriteLine` | `System.Console` | (표준) | stdout 출력 — HTTP 서버이므로 stdout 사용 안전 |
| `Write` | `System.Console` | (표준) | (공유) |
| `ReadLine` | `System.Console` | (표준) | (공유, `ChatLoop`이 직접 사용) |

추가로 **`Microsoft.Extensions.AI` 네임스페이스 자체를 re-export**하여 `ChatMessage`, `ChatRole`, `IChatClient`, `ChatOptions` 등이 import 없이 사용 가능.

**`Run` overload 충돌 처리**: `Cadenza.Sh`의 `int Run(string cmd)`와 `Cadenza.Agent.Agent`의 `Task Run()`이 모두 `using static`으로 노출되지만, **인자 시그니처가 다르므로 C# overload resolution이 자동 분기**한다. 사용자 입장에서는 `await Run();`은 에이전트 시작, `Run("git status")`는 쉘 호출.

#### 13.2.5 Tier 2 — Prefixed Modules

`Agent` 본체 외에 다음 정적 프로퍼티가 변종 전용으로 추가됨:

```csharp
int     Agent.Port           { get; set; }    // 기본 8080
string  Agent.HostName       { get; set; }    // 기본 "localhost"
string  Agent.ServedModelName { get; set; }   // /v1/models가 반환하는 ID, 기본 "cadenza-agent"
```

공통 모듈은 모두 그대로 사용 가능 (`Fs`, `Http`, `Env`, `Json`, `Sh`, `Prompt`).

#### 13.2.6 HTTP 표면 (OpenAI 호환 wire)

`await Run()`이 노출하는 엔드포인트:

| Method | Path | 용도 |
| ------ | ----- | ----- |
| `GET` | `/v1/models` | OpenAI `models.list` 호환 — `Agent.ServedModelName` 단일 항목 |
| `GET` | `/health` | liveness probe (Cadenza 자체 추가) |
| `POST` | `/v1/chat/completions` | OpenAI Chat Completions 호환. `stream=true` 시 SSE. **서버-측 등록된 `Tool`이 자동 호출됨** |
| `POST` | `/v1/responses` | OpenAI Responses API 호환 (Codex CLI 전용). **클라이언트-소유 도구만 사용** — 서버 `Tool` 등록은 노출되지 않음 |

`/v1/responses`와 `/v1/chat/completions`의 비대칭은 의도된 것 (§13.2.9 a 참조).

#### 13.2.7 Default `Sdk.props` (`Cadenza.Agent`)

```xml
<Project>
  <!-- 1) 기반 SDK 임포트 -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk.Web" />

  <!-- 2) Cadenza 컴파일 설정 -->
  <PropertyGroup>
    <TargetFramework Condition=" '$(TargetFramework)' == '' ">net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ExperimentalFileBasedProgramEnableTransitiveDirectives>true</ExperimentalFileBasedProgramEnableTransitiveDirectives>
    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
    <NoWarn>$(NoWarn);NU1510</NoWarn>
  </PropertyGroup>

  <!-- 3) Cadenza 코드 + global usings (※ Agent 본체와 Sh가 모두 static using된다 — Run() overload 충돌은 §13.2.4 참조) -->
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../content/core/**/*.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)../content/web/**/*.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)../content/agent/**/*.cs" />

    <Using Include="Cadenza" />
    <Using Include="Cadenza.Web" />
    <Using Include="Cadenza.Agent" />
    <Using Include="Cadenza.Sh"             Static="true" />
    <Using Include="Cadenza.Fs"             Static="true" />
    <Using Include="Cadenza.Agent.Agent"    Static="true" />
    <Using Include="System.Console"         Static="true" />
    <Using Include="Microsoft.Extensions.AI" />
  </ItemGroup>

  <!-- 4) 자동 PackageReference -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI"        Version="10.6.0" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.6.0" />
    <PackageReference Include="OpenAI"                          Version="2.10.0" />
    <PackageReference Include="OllamaSharp"                     Version="5.4.25" />
    <PackageReference Include="Microsoft.Extensions.Hosting"    Version="10.0.0" />
    <PackageReference Include="System.Text.Encoding.CodePages"  Version="10.0.0" />
  </ItemGroup>

  <!-- 5) Default deployment 설정 (가족 정책 동일) -->
  <PropertyGroup>
    <PublishAot>false</PublishAot>
    <PublishSingleFile Condition=" '$(PublishSingleFile)' == '' ">true</PublishSingleFile>
    <PublishReadyToRun Condition=" '$(PublishReadyToRun)' == '' ">true</PublishReadyToRun>
    <EnableCompressionInSingleFile Condition=" '$(EnableCompressionInSingleFile)' == '' ">true</EnableCompressionInSingleFile>
    <IncludeNativeLibrariesForSelfExtract Condition=" '$(IncludeNativeLibrariesForSelfExtract)' == '' ">false</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
</Project>
```

#### 13.2.8 Canonical Examples

##### **기본: 로컬 Ollama + 파일 읽기 도구**

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

SystemPrompt("You are a helpful assistant with read-only filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

UseOllama("llama3.2");

await Run();
```

외부 클라이언트 연결:

```bash
export OPENAI_BASE_URL=http://localhost:8080/v1
export OPENAI_API_KEY=any-non-empty-string
codex      # 또는 aider, continue, cursor, sgpt, …
```

##### **백엔드 스위칭 (env-driven multi-LLM)**

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

SystemPrompt("You are a helpful assistant.");

Tool("today", "Return today's local date",
    () => DateTime.Now.ToString("yyyy-MM-dd"));

switch ((Env.Get("LLM_BACKEND") ?? "ollama").ToLowerInvariant())
{
    case "openai":    UseOpenAi(Env.Get("OPENAI_MODEL")    ?? "gpt-4o-mini"); break;
    case "anthropic": UseAnthropic(Env.Get("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-latest"); break;
    case "azure":     UseAzureOpenAi(
        Env.Get("AZURE_OPENAI_ENDPOINT")   ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT missing"),
        Env.Get("AZURE_OPENAI_DEPLOYMENT") ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT missing")); break;
    default:          UseOllama(Env.Get("OLLAMA_MODEL") ?? "llama3.2"); break;
}

await Run();
```

##### **Codex CLI 통합 (Responses API)**

스크립트는 동일 (`await Run();`만 호출). Codex 측 `CODEX_HOME` 설정 파일을 가리키면 `/v1/responses` 엔드포인트로 자동 라우팅. 클라이언트가 보낸 `shell` / `apply_patch` 같은 도구는 **서버에서 자동 호출되지 않고** 클라이언트로 그대로 스트리밍 (§13.2.9 a).

##### **Single-binary 배포**

```bash
dotnet publish agent.cs -r linux-x64 -c Release \
  -p:EnableCompressionInSingleFile=true
# → ~80-110MB 단일 바이너리 (AI 의존성 포함)
```

#### 13.2.9 Open Decisions (`Cadenza.Agent` 전용)

- **(a) Chat-Completions vs Responses 도구 모델의 비대칭** — 현재 `/v1/chat/completions`는 서버 `Tool` 등록을 자동 호출, `/v1/responses`는 클라이언트 도구만 통과시킴. 이유: Codex 같은 Responses 클라이언트는 자체 도구셋(`shell`, `apply_patch` 등)을 매 턴 동봉하며, MEAI `FunctionInvokingChatClient`가 "내 도구 vs 클라이언트 도구"를 선택적으로 invoke하는 기능이 없음. 1.2에서 source-generator + 명시적 invoker 조합으로 양 엔드포인트 통일 가능성 평가.
- **(b) 인증** — 현재 `OPENAI_API_KEY=anything` 식으로 *임의의 비어있지 않은 문자열*을 받는다 (검증 없음). 로컬 dev 전용 위치에서는 OK이지만, 노출 시 인증 추가 필요. 1.1 README 보안 섹션에서 명시적 경고, 1.2에서 `Agent.RequireBearer(token)` 같은 단순 헬퍼 도입 검토.
- **(c) 다중 모델 분기** — `/v1/models`가 단일 모델만 반환. `req.Model` 값으로 `IChatClient` 라우팅하는 기능은 미구현. 사용 데이터 수집 후 1.2 평가.
- **(d) HTTPS / mTLS** — Kestrel 위에 그대로 얹혀 있으므로 `Agent.UseHttps()` 같은 helper만 추가하면 됨. 우선순위 낮음 (대부분 reverse proxy 뒤에서 운용).
- **(e) Responses API 상태 저장** — 현재 in-memory `ConcurrentDictionary` 256건 LRU. 1.2에서 옵트인 file/sqlite store 검토.

#### 13.2.10 AOT 비호환성 (가족 예외)

`Tool(name, desc, handler)` 구현은 `Microsoft.Extensions.AI.AIFunctionFactory.Create(handler, …)`에 위임되며, 이 팩토리는 `Delegate.Method`의 파라미터 메타데이터를 **reflection으로 읽어 JSON 스키마를 생성**한다. 결과:

- 메서드에 `[RequiresUnreferencedCode]` + `[RequiresDynamicCode]` 어트리뷰트가 박혀 있음.
- `#:property PublishAot=true`로 publish 시 trim/AOT warning 다수 발생.
- 권장 경로: **AOT 불필요**. JIT + SingleFile + R2R + Compression이 가족 기본 deployment.

이 제약은 **AI 도구 등록의 ergonomic 비용**으로 의도적 수용. AOT가 진짜 필요한 사용자는:

1. `Cadenza.Web` SDK + 직접 `Microsoft.Extensions.AI` 호출
2. 또는 1.2+에서 source-generator 기반 `[Tool]` attribute 경로 (§10.10)

§1.1 SDK 가족 표에 AOT-clean 컬럼이 ⚠️로 표기된 유일한 변종.

### 13.3 v1.2+ 변종 후보

다음은 v1.2+ 평가 대상이며, 모두 SDK 분화 4-criterion test (별도 기반 SDK / default property 세트 / 고유 tier 1 / 별도 사용자 세그먼트)를 통과한 후보. **시점 신호** 컬럼은 외부 조건이 명확한 경우 표기:

| 후보 | 동기 | 4-criterion | 시점 신호 / 우선순위 근거 |
| ------ | ------ | ------------ | --------------------------- |
| `Cadenza.Wasi` | WASI 2.0 production-ready 진입 (2026 Q1), 엣지 컴퓨팅 single-`.wasm` 배포 모델이 Cadenza 철학과 완벽 정합 | 4 | `wasm-tools` workload의 "experimental" 라벨 해제 시점 — Microsoft 공식 신호 기반 |
| `Cadenza.Aspire` | Aspire AppHost 단일 파일 작성 | 3-4 | — |
| `Cadenza.Lambda` | AWS Lambda 단일 파일 + AOT | 4 | — |
| `Cadenza.Function` | Azure Functions 단일 파일 + AOT | 4 | — |
| `Cadenza.MewUI` | SwiftUI-style 데스크톱 앱 | 4 | MewUI 1.0 도달 |

**우선순위 근거**: `Cadenza.Wasi`가 표 맨 위에 위치하는 이유는 다른 후보들과 달리 *외부 신호(Microsoft workload 라벨)가 명확한 진입 트리거*를 제공하기 때문. workload GA 격상 발표일이 사실상 작업 착수 시점. 다른 후보들은 사용자 데이터·요청 빈도 기반으로 1.2+ sequence 진입 시점에 1~2개 선정.

**`Cadenza.Wasi`의 적용 범위와 비대상**:

- **대상**: WASI 모듈 (서버사이드, 엣지 컴퓨팅) — Wasmtime, Wasmer, Fastly Compute, Cloudflare Workers, Fermyon Spin 등의 호스트에서 실행되는 `.wasm` 단일 산출물. `Microsoft.NET.Sdk` + `wasm-tools` workload 기반.
- **비대상 (의도적)**: 브라우저 타깃 wasm (`Microsoft.NET.Sdk.BlazorWebAssembly` 기반 또는 ".NET in browser without Blazor") — HTML 호스트·JS 글루·정적 자산 디렉토리가 필수라 Cadenza의 single-`.cs`-as-program 모델과 양립하지 않음. 이 영역은 Blazor·Avalonia·Uno·MAUI 등 기존 솔루션의 자리.

**`Cadenza.Wasi` 미래 제품 sketch** (1.2+ 진입 시 예상 형태):

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Wasi@0.1.0-preview.1

// Component Model 기반 export
Export("transform", (string input) => input.ToUpperInvariant());
Export("get_version", () => "1.0.0");
```

배포:

```bash
dotnet publish module.cs -c Release
# → bin/Release/net10.0/wasi-wasm/AppBundle/module.wasm (~3-8MB after trimming)
wasmtime module.wasm
```

**장기적 시너지 가능성**: Cadenza.Mcp × Cadenza.Wasi 결합 — MCP 서버를 wasm 모듈로 sandbox 배포 (path traversal·command injection 방어). 1.3+ 평가 카드.

**1.x 출시된 변종은 `Cadenza.Mcp` + `Cadenza.Agent`.** v1.2+ 진입 시 위 후보들 중 우선순위에 따라 1~2개 선정.

### 13.4 SDK가 아닌 보조 자산

다음은 SDK가 아니지만 가족 전반의 가치를 보완하는 자산들. 출시 상태도 함께 표기:

- **Cadenza skill (vibe coding 지원)** — Phase 1 출시 완료 (96dcd4f). GitHub 정적 파일 (Claude Code · Cursor · Copilot · Continue · Aider 어댑터). Phase 2(MCP 서버 형태 — `Cadenza.Mcp.Server` dotnet tool)는 1.2+ 검토.
- **`Cadenza.Templates`** — 출시 완료 (5b3f0a4). `dotnet new cadenza-console / -worker / -web / -mcp / -agent`.
- **`Cadenza.SdkResolver`** — 출시 완료 (§14 참조). 옵트인 MSBuild SDK resolver.
- **`Cadenza.Tasks` 옵트인 모듈** — Bullseye 위 thin layer. 1.2+ 평가.

---

## 14. Optional Tooling

가족 본체(SDK 패키지) 외부에서 동작하는 선택적 도구들. 일반 사용자는 설치할 필요가 없고, 특정 사용 패턴이 필요할 때만 opt-in.

### 14.1 `Cadenza.SdkResolver` — shipped in 1.0.x

#### 14.1.1 개요

**Use case**: `#:sdk Cadenza`(버전 생략) 또는 `#:sdk Cadenza.Agent@latest` 같은 floating SDK 참조를 동작시킨다. 표준 MSBuild의 NuGet-based SDK resolver는 정확한 SemVer만 받기 때문에(§11.4), 이런 표현은 기본 환경에서 "버전이 지정되지 않음" 오류로 실패한다. 본 resolver는 nuget.org를 직접 조회해 최신 stable 버전을 골라 글로벌 패키지 폴더에 풀어둔 뒤 그 경로를 MSBuild에 돌려준다.

**의도된 한계**: 이 도구는 **편의 기능**이지 권장 워크플로우가 아니다. canonical pattern은 §11.4의 정확한 버전 핀이며, production 스크립트·공유 Gist·공개 예제는 모두 그 패턴을 따라야 한다. SdkResolver는 개인 머신에서의 빠른 prototyping을 위한 옵트인 도구다.

#### 14.1.2 Identity

| 항목 | 값 |
| ------ | ----- |
| **Package ID** | `Cadenza.SdkResolver` |
| **타입** | MSBuild SDK resolver class library (NuGet 패키지) |
| **인터페이스** | `Microsoft.Build.Framework.SdkResolver` 상속 |
| **TFM** | `net10.0` |
| **Assembly size** | ~20 KB (NuGet.Protocol 의존성 없음 — 직접 v3 flat container 조회) |
| **Priority** | 4500 (번들 NuGet resolver의 5500보다 먼저 실행) |
| **License** | MIT |

#### 14.1.3 동작 원리

1. **매칭**: SDK 이름이 `Cadenza` 또는 `Cadenza.*` 패턴인지 확인. 아니면 `null` 반환 (resolver chain 다음으로 deference).
2. **버전 검사**: 요청된 버전이 비어 있거나 `latest` / `*`인 경우만 처리. 정확한 SemVer가 있으면 `null` 반환해 번들 NuGet resolver에 위임.
3. **버전 조회**: `https://api.nuget.org/v3-flatcontainer/<id-lower>/index.json`에서 버전 목록을 받아 hyphen이 없는(stable) 가장 높은 SemVer 선택.
4. **다운로드**: 같은 v3 flat container에서 `.nupkg`를 글로벌 패키지 폴더(`NUGET_PACKAGES` 또는 `~/.nuget/packages`)에 풀어둠. 이미 풀려 있으면 skip.
5. **반환**: 풀린 `Sdk/` 디렉터리 경로를 `SdkResult.IndicateSuccess`로 반환.

#### 14.1.4 설치

`Cadenza.Cli` global tool은 1.0.10에서 도입했다가 1.0.11에서 회수 (commit f2d8b66). 현재 설치 경로는 **수동 배치**:

```bash
# 1. nuget.org에서 패키지 다운로드
dotnet nuget download Cadenza.SdkResolver --output ./tmp

# 2. tools/net10.0/Cadenza.SdkResolver.dll을 SDK resolver 디렉터리에 복사
mkdir -p "$LOCALAPPDATA/Cadenza/SdkResolvers/Cadenza.SdkResolver"
cp tmp/tools/net10.0/Cadenza.SdkResolver.dll \
   "$LOCALAPPDATA/Cadenza/SdkResolvers/Cadenza.SdkResolver/"

# 3. MSBuild가 그 위치를 인식하도록 환경 변수 설정 (영구)
export MSBUILDADDITIONALSDKRESOLVERSFOLDER="$LOCALAPPDATA/Cadenza/SdkResolvers"
```

이후 모든 `dotnet run app.cs` 호출이 `#:sdk Cadenza`(버전 생략)를 받아들인다. PowerShell 등가 명령은 `docs/`의 설치 가이드 참조.

#### 14.1.5 사용 예

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza           // resolver가 최신 1.0.x 자동 선택

WriteLine("Hello from Cadenza (no version pin)!");
```

```csharp
#:sdk Cadenza.Agent@latest  // 명시적 latest 토큰
```

resolver가 설치되어 있지 않으면 두 표현 모두 표준 NuGet resolver에서 거부된다. 정확한 버전(`#:sdk Cadenza@1.0.155`)은 resolver 설치 여부와 무관하게 동작.

#### 14.1.6 Open Decisions

- **(a) 설치 자동화** — 현재는 수동 복사. 1.2에서 `dotnet tool install -g Cadenza.SdkResolver`로 풀고 첫 실행 시 자동 배치하는 패턴 재평가 (1.0.11에서 한 번 회수했던 결정이므로 사용자 데이터 누적 필요).
- **(b) Prerelease 채널** — 현재 stable만 선택. `@latest-preview`나 `@latest-beta` 같은 채널 토큰 도입 검토.
- **(c) 캐시 정책** — index.json을 매 호출 조회. 짧은 TTL 인메모리 캐시 추가 검토.
- **(d) 오프라인 mode** — nuget.org 도달 불가 시 글로벌 캐시의 최신 버전을 대신 반환할지 여부.
