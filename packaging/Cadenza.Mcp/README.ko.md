# Cadenza.Mcp

> [English](README.md)로 보기.

`Cadenza.Mcp`는 Cadenza SDK 가족의 MCP-서버 변종입니다 — Anthropic + Microsoft 공동 유지보수의 공식 [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK 위에 얹은 단일 파일 스크립팅 MSBuild SDK.

## 빠르게 시작

`server.cs` 파일 생성:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.13

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

iteration 실행:

```bash
dotnet run server.cs
```

Claude Desktop (또는 다른 MCP 클라이언트) 설정에 등록:

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/absolute/path/to/server.cs"]
    }
  }
}
```

자체 포함 단일 바이너리로 publish:

```bash
dotnet publish server.cs -r linux-x64 -c Release
```

## 중요: stdout은 프로토콜이 소유

stdio MCP 서버는 stdout으로 JSON-RPC를 전송하므로, `System.Console` bare name (`WriteLine`, `Write`, `ReadLine`)을 의도적으로 `Cadenza.Mcp`의 Tier 1에 포함시키지 않았습니다. 사용자 코드가 stdout에 무언가를 적으면 프로토콜 스트림이 깨지고 클라이언트 연결이 끊깁니다. 진단 출력은 `Log.*` 헬퍼를 사용하세요 — `ILogger`를 통해 stderr로 라우팅됩니다.

전체 명세, 보안 경계 노트, 추가 샘플은 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
