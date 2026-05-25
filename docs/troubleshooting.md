# Troubleshooting

## "Cadenza/X.Y.Z" SDK를 찾을 수 없습니다 (newly-released version)

새 버전(예: `1.0.1`)이 nuget.org에 게시되어 웹에서 확인되는데도, `dotnet run`/`dotnet build`가 그 버전을 못 찾고 이전 버전만 인식하는 경우가 있습니다.

```text
SDK 확인자 "Microsoft.DotNet.MSBuildWorkloadSdkResolver"이(가) null을 반환했습니다.
- nuget.org에서 N 버전을 찾았습니다[가장 가까운 버전: 1.0.0].
지정된 'Cadenza/1.0.1' SDK를 찾을 수 없습니다.
```

원인: NuGet의 HTTP 캐시가 이전 시점의 버전 목록을 보관하고 있어서, 신규 버전이 인덱싱된 후에도 로컬에서는 이를 다시 조회하지 않습니다. SDK resolver 역시 동일 캐시를 사용합니다.

### 가장 간단한 우회 (floating 버전 사용)

스크립트 첫 줄을 floating 버전 패턴으로 작성하면 캐시 만료 시점에 자동으로 최신 버전을 가져옵니다:

```csharp
#:sdk Cadenza@1.*           // 1.x 최신
#:sdk Cadenza@1.0.*         // 1.0.x 최신
```

`@1.0.1` 같은 정확한 핀은 그 버전이 인덱스에 있어야만 해석되므로, 우회가 필요합니다.

### Cadenza 캐시만 선택적으로 비우기

전체 NuGet 캐시(`dotnet nuget locals all --clear`)를 비우지 않고 Cadenza 관련 항목만 정리할 수 있습니다.

**macOS / Linux**

```bash
# 1. HTTP 메타데이터 캐시에서 Cadenza의 버전 목록·패키지 항목 제거
find "$(dotnet nuget locals http-cache --list | awk '{print $NF}')" \
  \( -name 'list_cadenza*.dat' -o -name 'nupkg_cadenza*.dat' \) \
  -delete

# 2. 글로벌 패키지 폴더에서 이미 추출된 Cadenza 패키지 제거
rm -rf ~/.nuget/packages/cadenza ~/.nuget/packages/cadenza.worker ~/.nuget/packages/cadenza.web
```

**Windows (PowerShell)**

```powershell
# 1. HTTP 캐시
$httpCache = (dotnet nuget locals http-cache --list).Split(' ')[-1]
Get-ChildItem -Path $httpCache -Recurse -Include 'list_cadenza*.dat','nupkg_cadenza*.dat' |
  Remove-Item -Force

# 2. 글로벌 패키지 폴더
Remove-Item "$env:UserProfile\.nuget\packages\cadenza","$env:UserProfile\.nuget\packages\cadenza.worker","$env:UserProfile\.nuget\packages\cadenza.web" -Recurse -Force -ErrorAction SilentlyContinue
```

다음 `dotnet run` 시 NuGet이 nuget.org에서 최신 인덱스를 다시 받아옵니다.

### 그래도 안 되는 경우

NuGet 소스 설정이 nuget.org가 아닌 미러를 가리키고 있을 수 있습니다:

```bash
dotnet nuget list source
```

`nuget.org`의 URL이 `https://api.nuget.org/v3/index.json`로 설정되어 있어야 합니다. 별도 미러가 설정되어 있으면 그 미러의 인덱싱이 지연되어 있을 가능성이 있습니다.

## macOS에서 `error MSB3552: 리소스 파일 "**/*.resx"을(를) 찾을 수 없습니다`

`Cadenza` 1.0.0 이전 버전을 사용 중인 경우 발생할 수 있습니다. 1.0.1에서 수정됐으므로 [위 안내](#cadenza-캐시만-선택적으로-비우기)대로 캐시를 정리한 뒤 1.0.1 이상을 floating 버전(`@1.*`)으로 참조하세요.
