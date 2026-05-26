# Cadenza.Worker

> [English](README.md)로 보기.

`Cadenza.Worker`는 Cadenza SDK 가족의 워커/데몬 변종입니다 — `Microsoft.NET.Sdk.Worker` 위에 얹은 단일 파일 스크립팅 MSBuild SDK.

## 빠르게 시작

`heartbeat.cs` 파일 생성:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.12

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"Heartbeat at {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

iteration 실행:

```bash
dotnet run heartbeat.cs
```

자체 포함 단일 바이너리로 publish:

```bash
dotnet publish heartbeat.cs -r linux-x64 -c Release
```

전체 명세는 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
