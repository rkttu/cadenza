# Cadenza.Web

> [English](README.md)로 보기.

`Cadenza.Web`은 Cadenza SDK 가족의 웹 / Minimal API 변종입니다 — `Microsoft.NET.Sdk.Web` 위에 얹은 단일 파일 스크립팅 MSBuild SDK.

## 빠르게 시작

`api.cs` 파일 생성:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.7

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });

await Run();
```

iteration 실행:

```bash
dotnet run api.cs
```

자체 포함 단일 바이너리로 publish:

```bash
dotnet publish api.cs -r linux-x64 -c Release
```

전체 명세는 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
