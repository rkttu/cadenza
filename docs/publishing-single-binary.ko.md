# 단일 바이너리 배포

> [English](publishing-single-binary.md)로 보기.

이 가이드는 Cadenza SDK 다섯 변종 모두(`Cadenza`, `Cadenza.Worker`, `Cadenza.Web`, `Cadenza.Mcp`, `Cadenza.Agent`)에 동일하게 적용됩니다. 기본값은 자체 포함 단일 바이너리 + R2R (ready-to-run) 컴파일 + single-file 압축입니다.

## 기본 publish

```bash
dotnet publish app.cs -r linux-x64 -c Release
```

산출물: `bin/Release/net10.0/linux-x64/publish/app` — 압축 후 약 30–40MB.

지원 runtime identifier (`-r`):

| Platform | RID |
| --- | --- |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |
| macOS x64 | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |
| Windows x64 | `win-x64` |
| Windows ARM64 | `win-arm64` |

## 튜닝

Single-file 압축 끄기 (콜드 스타트 빠름, 바이너리 큼):

```bash
dotnet publish app.cs -r linux-x64 -c Release -p:EnableCompressionInSingleFile=false
```

Native 라이브러리를 single-file 안에 묶기 (실행 시 temp 추출 없음):

```bash
dotnet publish app.cs -r linux-x64 -c Release -p:IncludeNativeLibrariesForSelfExtract=true
```

## NativeAOT (opt-in)

스크립트 최상단에 다음 줄을 추가:

```csharp
#:property PublishAot=true
```

그리고 평소처럼 publish:

```bash
dotnet publish app.cs -r linux-x64 -c Release
```

산출물: ~10–30MB native binary, JIT 없음, 가장 빠른 콜드 스타트. AOT는 모든 의존성이 AOT 호환이어야 합니다 — Cadenza core API들은 by construction AOT-clean입니다.

## 컨테이너 packaging

Cadenza.Web의 경우 바이너리가 self-contained이므로 `mcr.microsoft.com/dotnet/runtime-deps` 베이스 이미지로 충분:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
COPY ./bin/Release/net10.0/linux-x64/publish/app /app
ENTRYPOINT ["/app"]
```

최종 이미지가 ~100MB 안쪽으로 정리됩니다.
