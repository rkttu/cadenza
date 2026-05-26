# Cadenza 샘플

> [English](README.md)로 보기.

각 `.cs` 파일은 file-based 프로그램 한 장으로 완결된 예시입니다 — 다음 명령으로 실행:

```bash
dotnet run <file>.cs
```

각 샘플의 `#:sdk` 줄에는 최신 게시 버전을 정확히 핀해야 합니다 (현재 이 폴더의 파일들은 `Cadenza@1.0.11`, `Cadenza.Worker@1.0.11`, `Cadenza.Web@1.0.11`, `Cadenza.Mcp@1.0.11`로 고정). MSBuild SDK 참조는 정확한 버전만 받습니다 — 자세한 내용은 [docs/troubleshooting.ko.md](../docs/troubleshooting.ko.md).

## 콘솔 스크립트 (`#:sdk Cadenza@...`)

| 샘플 | 보여주는 것 |
| --- | --- |
| [`console-hello.cs`](console-hello.cs) | 최소 — `Glob`, `ReadText`, `WriteLine` |
| [`console-count-files.cs`](console-count-files.cs) | 재귀 `Glob` + LINQ grouping |
| [`console-deploy-guard.cs`](console-deploy-guard.cs) | git 상태 읽는 `Capture`, 빌드 단계 `Run`, 실패 분기 `Env.Exit` |
| [`console-http-fetch.cs`](console-http-fetch.cs) | source-generated `JsonSerializerContext` 기반 `Http.GetJson` (AOT-clean) |
| [`console-prompt-setup.cs`](console-prompt-setup.cs) | 네 가지 `Prompt.*` 헬퍼 + CI fallback 패턴 |

## 워커 스크립트 (`#:sdk Cadenza.Worker@...`)

| 샘플 | 보여주는 것 |
| --- | --- |
| [`worker-heartbeat.cs`](worker-heartbeat.cs) | 우아한 종료 가능한 최소 주기 루프 |
| [`worker-polling.cs`](worker-polling.cs) | 타입화된 `Worker.Config<T>`, 주기 HTTP probe, `Log.Info/Warn/Debug` |

## 웹 스크립트 (`#:sdk Cadenza.Web@...`)

| 샘플 | 보여주는 것 |
| --- | --- |
| [`web-minimal.cs`](web-minimal.cs) | Minimal API record binding으로 hello + health + echo |
| [`web-todo-api.cs`](web-todo-api.cs) | in-memory store 위 `Get`/`Post`/`Put`/`Delete` 풀 CRUD |

## MCP 서버 스크립트 (`#:sdk Cadenza.Mcp@...`)

| 샘플 | 보여주는 것 |
| --- | --- |
| [`mcp-files.cs`](mcp-files.cs) | 최소 MCP 서버 — `Tool` 등록 + stdio transport `Run` |
| [`mcp-extended.cs`](mcp-extended.cs) | 풀 primitive 세트 — `Tool` (외부 API), `Resource` (고정 URI), `Prompt` (템플릿), `Log.Info` (stderr) |

Cadenza.Mcp 서버는 Claude Desktop 설정에 다음을 추가해 등록:

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/absolute/path/to/mcp-files.cs"]
    }
  }
}
```

**중요**: Cadenza.Mcp는 의도적으로 `WriteLine` / `Write` / `ReadLine`을 Tier 1 bare name으로 노출하지 않습니다. stdio MCP 서버는 stdout으로 JSON-RPC를 보내므로, 사용자 코드가 stdout에 무언가 적으면 클라이언트 연결이 끊깁니다. 진단 출력은 `Log.*`를 사용하세요 — `ILogger`를 통해 stderr로 라우팅됩니다.

## 단일 바이너리 배포

이 샘플들 중 어느 것이든 self-contained 바이너리로 배포할 수 있습니다:

```bash
dotnet publish console-deploy-guard.cs -r linux-x64 -c Release
```

전체 배포 매트릭스(압축, AOT opt-in, 컨테이너 packaging)는 [docs/publishing-single-binary.ko.md](../docs/publishing-single-binary.ko.md) 참고.
