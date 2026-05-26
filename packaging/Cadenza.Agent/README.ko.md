# Cadenza.Agent

> [English](README.md)로 보기.

`Cadenza.Agent`는 Cadenza SDK 가족의 AI 에이전트 변종입니다 — [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)(.NET의 벤더 중립 LLM 추상화) 위에 얹은 단일 파일 스크립팅 MSBuild SDK로, **OpenAI 호환 Chat Completion HTTP 엔드포인트**를 기본 제공합니다. 그래서 Codex, Aider, Continue, Cursor 같은 도구가 마치 OpenAI처럼 여러분의 에이전트와 대화할 수 있습니다 — `http://localhost:8080/v1`을 가리키기만 하면 됩니다.

## 빠르게 시작

`agent.cs` 파일 생성:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.13

SystemPrompt("당신은 파일 시스템에 접근할 수 있는 친절한 비서입니다.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

UseOllama("llama3.2");      // 또는 UseOpenAi("gpt-4o-mini")
                            //     UseAnthropic("claude-3-5-sonnet-latest")
                            //     UseAzureOpenAi(endpoint, deployment)

await Run();                // OpenAI 호환 HTTP 서버를 http://localhost:8080에서 부팅
```

반복 실행:

```bash
dotnet run agent.cs
```

OpenAI 호환 클라이언트를 가리키게 하면 끝:

```bash
export OPENAI_BASE_URL=http://localhost:8080/v1
export OPENAI_API_KEY=any-non-empty-string
codex      # 또는 aider, continue, cursor, sgpt, …
```

HTTP 서버 대신 REPL을 사용하고 싶다면:

```csharp
UseOllama("llama3.2");
await ChatLoop();           // 대화형 콘솔 — HTTP 서버 없음
```

자체 포함 단일 바이너리로 publish:

```bash
dotnet publish agent.cs -r linux-x64 -c Release
```

## 무엇이 제공되는지

| 헬퍼 | 용도 |
|---|---|
| `Tool(name, description, delegate)` | 호출 가능한 도구 등록. 파라미터 이름/타입이 JSON 스키마가 되고, 반환값이 도구 결과로 전달됩니다. |
| `SystemPrompt(text)` | 기본 시스템 프롬프트 덮어쓰기. |
| `UseOllama(model, baseUrl?)` | 로컬 [Ollama](https://ollama.com) 데몬을 LLM으로 사용. |
| `UseOpenAi(model, apiKey?)` | OpenAI 사용. `OPENAI_API_KEY` 환경변수로도 가능. |
| `UseAnthropic(model, apiKey?)` | Anthropic의 OpenAI 호환 엔드포인트를 사용. `ANTHROPIC_API_KEY`로도 가능. |
| `UseAzureOpenAi(endpoint, deployment, apiKey?)` | Azure OpenAI 사용. `AZURE_OPENAI_API_KEY`로도 가능. |
| `UseChatClient(IChatClient)` | 임의의 `IChatClient`를 꽂아 넣기 — `UseFunctionInvocation()`이 자동으로 적용됩니다. |
| `Run()` | OpenAI 호환 HTTP 서버 시작 (기본 `localhost:8080`). |
| `ChatLoop()` | 대화형 콘솔 REPL 시작. |
| `Reply(prompt)` | 단발성 비대화 호출. 어시스턴트 텍스트 반환. |
| `Port`, `HostName`, `ServedModelName` | `Run()` 호출 전 HTTP 표면 구성. |

## `Run()`이 노출하는 엔드포인트

- `POST /v1/chat/completions` — OpenAI Chat Completion 전체 형식; `stream=true`(SSE) 지원. Aider / Continue / Cursor / GitHub Copilot BYOK / sgpt 등 거의 모든 OpenAI 호환 클라이언트가 사용.
- `POST /v1/responses` — OpenAI Responses API (SSE 스트리밍). **OpenAI Codex CLI**가 사용 — 2026년 2월부터 Chat Completion 지원이 제거되어 이 wire format만 받습니다.
- `GET  /v1/models` — `ServedModelName`을 id로 하는 단일 항목 목록.
- `GET  /health` — 라이브니스 프로브.

### Chat Completion vs Responses — 무엇이 다른가

두 엔드포인트 모두 `UseOllama` / `UseOpenAi` 등으로 구성한 동일 `IChatClient`를 사용합니다. 차이는 도구(tool) 처리 방식:

| 경로 | 서버 사이드 `Tool(...)` 등록 | 클라이언트 제공 도구 | 사용 사례 |
| --- | --- | --- | --- |
| `/v1/chat/completions` | `UseFunctionInvocation` 미들웨어로 자동 호출. 에이전트 프로세스에서 실행. | 보통 없음 — Chat Completion 클라이언트는 서버 측 실행을 원할 때만 `tools`를 보냄. | Aider / Continue / Cursor / Copilot |
| `/v1/responses` | **모델에 노출하지 않음.** | `function_call` 아이템으로 스트림돼 클라이언트(Codex)가 직접 실행. | Codex CLI |

즉 `Tool("read_file", ...)` 등록은 Aider/Continue/Cursor에서는 동작하지만 Codex에서는 동작하지 않습니다. Codex에는 모델 어댑터 패턴으로 충분 — Codex가 `shell` / `apply_patch` / `update_plan`을 가져와서 직접 실행합니다.

## Codex CLI 연결

`~/.codex/config.toml` (Windows에서는 `%USERPROFILE%\.codex\config.toml`)에 추가:

```toml
model_provider = "cadenza"
model          = "cadenza-agent"

[model_providers.cadenza]
name     = "Cadenza.Agent local"
base_url = "http://localhost:8080/v1"
wire_api = "responses"
env_key  = "CADENZA_API_KEY"
stream_idle_timeout_ms = 300000
```

그리고 `CADENZA_API_KEY`에 아무 값이나 채운 뒤 codex 실행.

## 왜 HTTP 프론트엔드인가

이미 존재하는 대부분의 코딩 에이전트와 챗 UI는 OpenAI wire format(Chat Completion 또는 Responses)을 사용합니다. 두 형식 모두 채택하면:

- **글루 코드 없음** — Codex / Aider / Continue / Cursor / sgpt / LangChain 클라이언트 등과 그대로 통합.
- **무료 스트리밍** — OpenAI와 동일한 SSE 형식.
- **기본적으로 멀티 프로세스** — 에이전트 프로세스가 에디터와 분리됨.
- **함수 호출 자동 배선** — `Microsoft.Extensions.AI`의 `UseFunctionInvocation` 미들웨어가 등록한 모든 도구를 지원 모델 전부에서 호출 가능하게 만들어 줌.

전체 명세, 샘플 에이전트, 보안 노트는 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
