# Cadenza SDK Family — v0.1 Specification (draft)

> 본 문서는 v0.1 출시 이전의 작업용 명세이며, 모든 결정은 토론을 거쳐 변경될 수 있음. 결정되지 않은 항목은 §10 Open Decisions에 명시.

---

## 1. Identity

| 항목 | 값 |
| ------ | ----- |
| **Brand** | `Cadenza` |
| **Tagline** | A single-file scripting SDK family for .NET 10+ file-based apps |
| **Initial version** | `0.1.0` |
| **License** | MIT |
| **Target framework** | `net10.0` |
| **Minimum SDK** | .NET SDK 10.0.300 (file-based apps + `#:sdk` custom SDK 지원) |
| **External dependencies** | base SDK 외 없음 — `System.*` 및 `Microsoft.Extensions.*` (Worker/Web) |
| **Distribution channel** | NuGet **MSBuild SDK** 패키지 3종 |

### 1.1 SDK 가족 구성

| SDK | 용도 | 기반 SDK | Default deployment |
| ----- | ------ | --------- | ------------------- |
| `Cadenza` | 콘솔 스크립트, CLI 유틸리티 | `Microsoft.NET.Sdk` | JIT + R2R/SingleFile/SCD on publish |
| `Cadenza.Worker` | 백그라운드 서비스, 데몬 | `Microsoft.NET.Sdk.Worker` | JIT + R2R/SingleFile/SCD on publish |
| `Cadenza.Web` | 웹 API, Minimal API 스크립트 | `Microsoft.NET.Sdk.Web` | JIT + R2R/SingleFile/SCD on publish |

세 변종 모두 NativeAOT는 default off, `#:property PublishAot=true`로 opt-in. 자세한 deployment 모델은 §3.4.

사용자는 스크립트 첫 줄로 변종을 선택:

```csharp
#:sdk Cadenza@1.*           // 콘솔
#:sdk Cadenza.Worker@1.*    // 워커
#:sdk Cadenza.Web@1.*       // 웹
```

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
cadenza-sdks/
├── src/
│   ├── core/                    # 모든 변종이 공유 (namespace: Cadenza)
│   │   ├── Sh.cs
│   │   ├── Fs.cs
│   │   ├── Http.cs
│   │   ├── Env.cs
│   │   ├── Prompt.cs
│   │   └── Json.cs
│   ├── worker/                  # namespace: Cadenza.Worker
│   │   ├── Worker.cs
│   │   └── Log.cs
│   └── web/                     # namespace: Cadenza.Web
│       └── Web.cs
├── shared/
│   └── Cadenza.Common.props     # 공유 <Using> + <Compile>
├── cadenza/Sdk/
│   ├── Sdk.props
│   └── Sdk.targets
├── cadenza.worker/Sdk/
│   ├── Sdk.props
│   └── Sdk.targets
└── cadenza.web/Sdk/
    ├── Sdk.props
    └── Sdk.targets
```

빌드는 3개의 독립 NuGet SDK 패키지를 산출.

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

**의도적으로 tier 2로 미루는 것**: `Log.Info/Warn/Error` — 워커 스크립트 작성자가 가장 헷갈리는 부분이 로깅 vs. WriteLine 분기이므로, v0.1에서는 `Log.*` 명시 호출만 허용.

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

이게 혼란이 아닌 이유: **`#:sdk` 선언이 스크립트 첫 줄에 있고, 그 선언이 `Run`의 의미를 결정**. Python 스크립트가 첫 줄 `#!/usr/bin/env python3`로 컨텍스트를 잡듯, 동등한 효과가 발생. 한 스크립트 안에서 `Run`의 의미는 항상 단일.

쉘 실행이 필요한 워커/웹 스크립트는 `Sh.Run(...)` 접두사 호출 — tier 2 경로는 모든 변종에서 동일하게 열림.

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

### 5.5 `Prompt` — 콘솔/워커만 (웹에서는 의미 없음)

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

### 5.8 `Log` — 워커 변종 전용

```csharp
Log.Info(string message);
Log.Warn(string message);
Log.Error(string message, Exception? ex = null);
Log.Debug(string message);
```

`ILogger`로 라우팅. 콘솔/웹 변종에서는 namespace 미노출.

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

---

## 6. Async Strategy

| 도메인 | 기본 형태 | 비동기 오버로드 |
| -------- | ---------- | ---------------- |
| File I/O | 동기 (`ReadText`) | `ReadTextAsync` 명시적 |
| HTTP | 비동기 only | 없음 |
| Shell | 동기 우선 | `RunAsync`, `CaptureAsync` |
| Worker.Run | 비동기 (`await Run(...)`) | 없음 |
| Web.Run | 비동기 (`await Run()`) | 없음 |

근거: 콘솔 한 줄 스크립트의 가독성 + 워커/웹의 자연스러운 async 흐름.

---

## 7. Error Handling

- C# 예외 모델 그대로. `Result<T, E>` 도입 없음.
- `Sh.Run`은 exit code 반환, throw하지 않음 (caller 결정).
- `Sh.Capture`는 exit ≠ 0 시 throw.
- `Worker.Run`/`Web.Run` 내부의 예외는 호스트 표준 동작에 위임 (Worker: 로그 후 재시작 정책, Web: problem details 응답).

---

## 8. Canonical Example Scripts

### 8.1 콘솔: 파일 처리

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.*

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
```

### 8.2 콘솔: 배포 가드

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.*

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
#:sdk Cadenza.Worker@1.*

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
#:sdk Cadenza.Worker@1.*

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
#:sdk Cadenza.Web@1.*

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
#:sdk Cadenza.Web@1.*

Web.App.UseStaticFiles();          // 고급 옵션은 Web.App로 접근
Get("/api/files", () => Glob("wwwroot/**/*.html").Select(Path.GetFileName));

await Run();
```

---

## 9. v0.1 Scope Boundaries

### 9.1 포함

- 3개 SDK 패키지 (`Cadenza`, `Cadenza.Worker`, `Cadenza.Web`)
- §5의 9개 tier 2 모듈
- §4의 각 변종 tier 1
- 각 변종 1~2개 canonical example
- Deployment 매트릭스 CI: 3 OS × 3 변종 × {default publish, AOT publish}
- README per SDK (3 파일) + 공통 "Publishing as a single binary" 가이드

### 9.2 명시적 제외 (v0.2+)

| 항목 | 회수 시점 |
| ------ | ---------- |
| `Cadenza.Aspire` (Aspire AppHost 변종) | v0.2 평가 |
| `Csv` 모듈 | v0.2 옵트인 패키지 |
| `Hangul` (한국어 특화) | 별도 패키지 |
| CLI argparse | `Cadenza.Cli` 옵트인 |
| 진행률/스피너/TUI | `Cadenza.Tui` 옵트인 |
| `dotnet new` 템플릿 | v0.2 |
| Trimmed publish 가이드 | v0.2 — opt-in 전제 |

---

## 10. Open Decisions

### 10.1 명칭 & 케이싱 — RESOLVED ✅

**결정**: 브랜드명 `Cadenza`. SDK 패키지 ID는 `Cadenza` / `Cadenza.Worker` / `Cadenza.Web`로 시작.

**남은 확인 사항**:

- nuget.org에서 `Cadenza` 패키지 ID 가용성. 만약 점유 시 차순위로 `Cadenza.Sdk` 형태(.Sdk 접미사 추가, `Aspire.AppHost.Sdk` 패턴 차용) 채택. 이 경우 SDK 가족이 `Cadenza.Sdk` / `Cadenza.Worker.Sdk` / `Cadenza.Web.Sdk`로 정리됨.
- GitHub organization `cadenza-sdks` 또는 사용자 개인 namespace 중 선택은 §10.9.
- 도메인 가용성: `cadenza.dev`, `cadenza.run`, `cadenza-sdk.dev` 등.

브랜드 명명 배경은 §1.2 참조.

### 10.2 `global using` 메커니즘 — A / B / 둘 다

- A only: `_globals.cs`에 `global using static` 박기
- B only: SDK `Sdk.props`의 `<Using>` 항목
- 둘 다

**권장**: SDK 모델에서는 `Sdk.props`가 자연스러운 위치이므로 **B를 1차**, A는 fallback.

### 10.3 HTTP sync wrapper

async-only로 갈지, sync 편의 메서드를 함께 제공할지.

**권장**: v0.1은 async-only.

### 10.4 `Sh` 실행 시 셸 경유 방식

OS별 자동 분기(`cmd /c` / `/bin/sh -c`)인지, 셸 미경유 직접 실행인지.

**권장**: 자동 OS 분기. 셸 메타문자(`|`, `>`, `&&`)가 자연스럽게 동작.

### 10.5 Deployment 정책 세부 조정

기본 골격(AOT off / R2R+SingleFile+SCD on)은 §3.4에서 확정. 다음 세부 결정만 남음:

- **(a) `IncludeNativeLibrariesForSelfExtract`** — native 라이브러리를 단일 파일에 포함시킬지(큰 바이너리, 호환성 ↑) 외부 추출할지(작은 바이너리, 디스크 fragment).
  **권장**: false (외부 추출, 표준 동작).
- **(b) `EnableCompressionInSingleFile`** — 단일 파일 압축 활성화 여부. 시작 시간 trade-off 있음.
  **권장**: true (60-80MB → 30-40MB로 감소, 첫 실행 시 압축 해제 비용 발생).
- **(c) `PublishTrimmed` opt-in 메커니즘** — opt-in 시 trim warning을 어떻게 가이드할지.
  **권장**: README 트러블슈팅 섹션에 별도 가이드, default off 유지.

### 10.6 비대화형 Prompt 정책

`Env.IsCi == true`일 때 `Prompt.*` 동작.

**권장**: defaultValue 우선 → 환경변수(`CADENZA_PROMPT_<NAME>`) → 없으면 throw.

### 10.7 워커 `Log` vs. `WriteLine`

워커 변종에서 `WriteLine`을 그대로 노출할지, `Log.Info`로 자동 라우팅할지.

**권장**: `WriteLine`은 stdout 그대로, `Log.*`는 ILogger. 두 채널의 분리를 명시적으로 유지.

### 10.8 웹 변종 라우팅 컨벤션

`Get("/path", handler)` 외에 `Group("/api", g => { g.Get(...); })` 같은 그룹 라우팅을 v0.1에 포함할지.

**권장**: v0.1은 단일 라우트만, 그룹 라우팅은 v0.2.

### 10.9 저장소 & 조직

- 3개 SDK를 한 monorepo로 관리할지 또는 별도 repo로 분리할지.
- 저장소 호스팅: 개인 GitHub vs. 새 org `cadenza-sdks` vs. Plainworks org.

**권장**: monorepo, 새 org `cadenza-sdks` 신설. 브랜드와 org 명이 일치하여 검색성 우수.

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

- 초기 NuGet 게시: `0.1.0-preview.1`.
- 버전 라이프사이클: 0.x.x-preview (실험), 0.x.x (안정화), 1.0.0 (tier 1 동결).

---

## 12. Next Steps (제안 순서)

1. **nuget.org에서 `Cadenza` 패키지 ID 가용성 확인.** 결과에 따라 §10.1 차순위 적용.
2. **GitHub org `cadenza-sdks` 생성 + 도메인 확보 (`cadenza.dev` 또는 차순위).**
3. **최소 SDK 빌드 + `#:sdk` 동작 검증.** 빈 `Cadenza` SDK 패키지를 만들고, `#:sdk Cadenza@0.1.0-preview.1`로 `WriteLine("hi")`가 동작하는지 확인. SDK 가족 모델의 단일 핵심 가설.
4. **Default deployment 설정 검증.** `dotnet publish app.cs -r linux-x64`가 SDK 기본값으로 R2R+SingleFile+SCD 산출물을 만드는지 확인. AOT off가 base SDK의 true를 정상 override하는지 명시적 확인.
5. **`Cadenza.Web` 추가.** 기반 SDK가 `Microsoft.NET.Sdk.Web`인 변종이 의도대로 동작하는지 확인.
6. **`Cadenza.Worker` 추가.** 호스트 라이프사이클이 `Worker.Run` 추상화 안에서 정상 동작하는지 확인.
7. **Tier 1 모듈 풀세트 구현 + canonical examples 작성.**
8. **NativeAOT opt-in 검증.** `#:property PublishAot=true` 추가 시 정상 격상되는지, Cadenza API가 AOT 모드에서 경고 없이 동작하는지 확인.
9. **README 3종 + "Publishing as a single binary" 가이드 동결, `0.1.0-preview.1` 푸시.**
