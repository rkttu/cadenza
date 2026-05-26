# Troubleshooting

> [English](troubleshooting.md)로 보기.

## `#:sdk Cadenza@1.*` (wildcard / floating 버전) 사용 시 "버전이 지정되지 않음" 오류

```text
SDK 확인자 "Microsoft.DotNet.MSBuildWorkloadSdkResolver"이(가) null을 반환했습니다.
The NuGetSdkResolver did not resolve this SDK because there was no version specified in the project or global.json.
지정된 'Cadenza/1.*' SDK를 찾을 수 없습니다.
```

원인: **MSBuild SDK reference는 `1.*`, `1.0.*` 같은 floating/wildcard 버전을 지원하지 않습니다.** `PackageReference`의 floating 패턴과 메커니즘이 다릅니다 — SDK 해석은 MSBuild 평가의 가장 첫 단계에서 한 번에 끝나야 하는데, wildcard 해석은 NuGet restore 단계의 동작이라 그 시점에는 사용할 수 없습니다. NuGet 측 resolver는 wildcard 입력을 "버전 미지정"으로 fallback 처리하고, 그 결과 위 오류로 떨어집니다.

### 해결: 정확한 버전 핀

스크립트에 정확한 SemVer 버전을 직접 적습니다:

```csharp
#:sdk Cadenza@1.0.13
#:sdk Cadenza.Worker@1.0.13
#:sdk Cadenza.Web@1.0.13
#:sdk Cadenza.Mcp@1.0.13
#:sdk Cadenza.Agent@1.0.13
```

새 릴리스로 이동할 때 이 줄을 직접 갱신해야 합니다. 최신 버전은 [nuget.org/packages/Cadenza](https://www.nuget.org/packages/Cadenza)에서 확인합니다.

### 대안: `global.json`로 중앙화

스크립트와 같은 디렉터리(또는 상위 디렉터리)에 `global.json`을 두면 버전을 한 곳에서 관리할 수 있습니다:

```json
{
  "msbuild-sdks": {
    "Cadenza": "1.0.13",
    "Cadenza.Worker": "1.0.13",
    "Cadenza.Web": "1.0.13",
    "Cadenza.Mcp": "1.0.13",
    "Cadenza.Agent": "1.0.13"
  }
}
```

그러면 스크립트에서는 버전을 생략할 수 있습니다:

```csharp
#:sdk Cadenza
```

여러 스크립트를 같은 폴더에 두고 한꺼번에 버전을 끌어올리고 싶을 때 유용합니다.

---

## 새로 게시된 버전이 인식되지 않음 (stale NuGet cache)

새 버전(예: `1.0.6`)이 nuget.org에 게시되어 웹에서 확인되는데도, `dotnet run`/`dotnet build`가 그 버전을 못 찾고 이전 버전만 인식하는 경우가 있습니다.

```text
SDK 확인자 "Microsoft.DotNet.MSBuildWorkloadSdkResolver"이(가) null을 반환했습니다.
- nuget.org에서 N 버전을 찾았습니다[가장 가까운 버전: 1.0.5].
지정된 'Cadenza/1.0.6' SDK를 찾을 수 없습니다.
```

원인: NuGet HTTP 캐시가 이전 시점의 버전 목록을 보관하고 있어, 신규 버전이 인덱싱된 후에도 로컬에서는 이를 다시 조회하지 않습니다. SDK resolver 역시 동일 캐시를 사용합니다.

### Cadenza 캐시만 선택적으로 비우기

전체 NuGet 캐시(`dotnet nuget locals all --clear`)를 비우지 않고 Cadenza 관련 항목만 정리할 수 있습니다.

#### macOS / Linux

```bash
# 1. HTTP 메타데이터 캐시에서 Cadenza의 버전 목록·패키지 항목 제거
find "$(dotnet nuget locals http-cache --list | awk '{print $NF}')" \
  \( -name 'list_cadenza*.dat' -o -name 'nupkg_cadenza*.dat' \) \
  -delete

# 2. 글로벌 패키지 폴더에서 이미 추출된 Cadenza 패키지 제거
rm -rf ~/.nuget/packages/cadenza ~/.nuget/packages/cadenza.worker \
       ~/.nuget/packages/cadenza.web ~/.nuget/packages/cadenza.mcp
```

#### Windows (PowerShell)

```powershell
# 1. HTTP 캐시
$httpCache = (dotnet nuget locals http-cache --list).Split(' ')[-1]
Get-ChildItem -Path $httpCache -Recurse -Include 'list_cadenza*.dat','nupkg_cadenza*.dat' |
  Remove-Item -Force

# 2. 글로벌 패키지 폴더
Remove-Item "$env:UserProfile\.nuget\packages\cadenza", `
            "$env:UserProfile\.nuget\packages\cadenza.worker", `
            "$env:UserProfile\.nuget\packages\cadenza.web", `
            "$env:UserProfile\.nuget\packages\cadenza.mcp" `
            -Recurse -Force -ErrorAction SilentlyContinue
```

다음 `dotnet run` 시 NuGet이 nuget.org에서 최신 인덱스를 다시 받아옵니다.

### 그래도 안 되는 경우

NuGet 소스 설정이 nuget.org가 아닌 미러를 가리키고 있을 수 있습니다:

```bash
dotnet nuget list source
```

`nuget.org`의 URL이 `https://api.nuget.org/v3/index.json`로 설정되어 있어야 합니다. 별도 미러가 설정되어 있으면 그 미러의 인덱싱이 지연되어 있을 가능성이 있습니다.

---

## macOS: `error MSB3552: 리소스 파일 "**/*.resx"을(를) 찾을 수 없습니다`

Cadenza 1.0.0 이하 버전에서 발생할 수 있는 macOS 전용 빌드 오류. 1.0.1에서 수정됐습니다. 위의 캐시 리셋 절차로 1.0.1 이상을 받은 뒤 `#:sdk Cadenza@1.0.13`로 정확한 버전을 핀하세요.

---

## `Capture(...)` 출력: Windows에서 CJK / emoji 깨짐

1.0.4에서 수정됐습니다. Cadenza가 호스트의 OEM 코드페이지(한국어 Windows의 CP949, 일본어의 CP932 등)로 캡처된 subprocess 출력을 디코드하고, 출력 측 `Console.OutputEncoding = UTF-8`을 강제합니다. 1.0.4 이상으로 업그레이드하세요.

터미널 렌더링까지 깨끗하게 가려면 Windows Terminal(기본 UTF-8) 안에서 실행하거나, 기존 `cmd.exe` / `conhost`에서 `chcp 65001`을 한 번 실행해두면 됩니다.
