# Cadenza

> [English](README.md)로 보기.

`Cadenza`는 Cadenza SDK 가족의 콘솔 변종입니다 — .NET 10+ file-based 앱을 위한 단일 파일 스크립팅 MSBuild SDK.

## 빠르게 시작

`hello.cs` 파일 생성:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.9

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
```

iteration 실행:

```bash
dotnet run hello.cs
```

자체 포함 단일 바이너리로 publish:

```bash
dotnet publish hello.cs -r linux-x64 -c Release
```

전체 명세 및 Worker / Web / Mcp 변종은 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
