# Cadenza.SdkResolver

> [English](README.md)로 보기.

Cadenza 단일 파일 스크립팅 SDK 가족을 위한 **옵션 / opt-in MSBuild SDK resolver**입니다. 설치하면 `dotnet run app.cs`와 `dotnet build`에서 다음 형태를 받아들일 수 있게 됩니다:

```csharp
#:sdk Cadenza               // 버전 없음 — nuget.org에서 최신 안정 버전으로 해석
#:sdk Cadenza@latest        // 명시적 "latest" 별칭
#:sdk Cadenza@*             // wildcard 별칭
```

Cadenza의 canonical workflow는 이 패키지를 **요구하지 않습니다** — 스크립트에 정확한 버전을 핀(`#:sdk Cadenza@1.0.15`)하면 추가 설치 없이 `dotnet run app.cs`가 그대로 동작합니다. 이 resolver는 재현성이 중요하지 않은 시나리오(빠른 실험, REPL 류 일회성 코드)에서 버전 생략 / `@latest` 단축형을 굳이 쓰고 싶을 때만 설치하세요.

운영 스크립트는 어차피 정확한 버전을 핀하는 것이 좋습니다. 더 큰 그림은 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.

## MSBuild 내 역할

MSBuild는 우선순위 순서로 SDK resolver 체인을 호출합니다. 번들된 NuGet resolver는 버전이 명시된 경우에만 활성화됩니다. 이 resolver는 priority 4500 (NuGet의 5500보다 먼저) 으로 동작하며, `Cadenza*` SDK 참조 중 버전이 빈/`latest`/`*`인 경우를 인식해 `https://api.nuget.org/v3-flatcontainer/<id>/index.json`에서 가장 높은 안정 SemVer를 조회 → 매칭되는 nupkg를 `~/.nuget/packages/`에 받아내고 → 그 SDK 디렉터리 경로를 반환합니다. 정확한 버전(`Cadenza@1.0.15` 등)에는 `null`을 반환해 NuGet resolver에 위임.

## 설치

자동 설치 프로그램은 의도적으로 제공하지 않습니다 — resolver는 어셈블리를 특정 위치에 두고 환경변수 한 개를 설정해야 작동하므로 본인의 워크플로에 맞춰 직접 수행합니다.

### macOS / Linux

```bash
PKG=$(curl -fsSL https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/index.json | \
      grep -oE '"[^"]+"' | grep -vE '\-' | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | sort -V | tail -1)
TMP=$(mktemp -d)
curl -fsSL "https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/$PKG/cadenza.sdkresolver.$PKG.nupkg" -o "$TMP/pkg.nupkg"
unzip -q "$TMP/pkg.nupkg" 'tools/net10.0/*' -d "$TMP/x"
DEST="$HOME/.cadenza/sdk-resolvers/Cadenza.SdkResolver"
mkdir -p "$DEST"
cp "$TMP/x/tools/net10.0/Cadenza.SdkResolver.dll" "$DEST/"
cp "$TMP/x/tools/net10.0/Cadenza.SdkResolver.xml" "$DEST/"
rm -rf "$TMP"
echo 'export MSBUILDADDITIONALSDKRESOLVERSFOLDER="$HOME/.cadenza/sdk-resolvers"' >> ~/.bashrc  # 또는 ~/.zshrc, ~/.profile
```

새 터미널을 엽니다.

### Windows (PowerShell)

```powershell
$idx = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/index.json'
$ver = $idx.versions | Where-Object { $_ -notmatch '-' } | Sort-Object {[Version]$_} | Select-Object -Last 1
$tmp = New-Item -ItemType Directory -Force "$env:Temp\cadenza-resolver-$([guid]::NewGuid().ToString('N'))"
Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/$ver/cadenza.sdkresolver.$ver.nupkg" -OutFile "$tmp\pkg.nupkg"
Expand-Archive "$tmp\pkg.nupkg" -DestinationPath "$tmp\x" -Force
$dest = "$env:UserProfile\.cadenza\sdk-resolvers\Cadenza.SdkResolver"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item "$tmp\x\tools\net10.0\Cadenza.SdkResolver.dll" $dest -Force
Copy-Item "$tmp\x\tools\net10.0\Cadenza.SdkResolver.xml" $dest -Force
Remove-Item $tmp -Recurse -Force
[Environment]::SetEnvironmentVariable('MSBUILDADDITIONALSDKRESOLVERSFOLDER', "$env:UserProfile\.cadenza\sdk-resolvers", 'User')
```

새 터미널을 엽니다.

## 제거

resolver 폴더 삭제 + 환경변수 정리:

```bash
# POSIX
rm -rf ~/.cadenza/sdk-resolvers
# 그리고 shell profile에서 export 라인 삭제
```

```powershell
# Windows
Remove-Item "$env:UserProfile\.cadenza\sdk-resolvers" -Recurse -Force
[Environment]::SetEnvironmentVariable('MSBUILDADDITIONALSDKRESOLVERSFOLDER', $null, 'User')
```

## 사용자가 받아들이는 트레이드오프

- **비결정성.** `Cadenza@latest`는 시간에 따라 다른 버전으로 해석됩니다. 몇 주 간격으로 실행된 두 번의 `dotnet run`이 서로 다른 SDK를 컴파일러로 사용할 수 있음. 배포·공유할 스크립트는 정확한 버전 핀이 안전.
- **평가 시점의 네트워크 호출.** Resolver는 매 호출 시 nuget.org를 1회 조회합니다 (자체 캐시 없음). 오프라인/제한된 네트워크 환경에선 NuGet resolver로 폴백되고, NuGet은 "버전 미지정" 오류를 냅니다.
- **공개 피드 전용.** Resolver는 nuget.org에 직접 질의 — 사설 피드나 `nuget.config`로 설정된 소스를 버전 lookup에 사용하지 않습니다 (해석된 SDK 추출은 사용자의 글로벌 패키지 폴더를 사용).

전체 Cadenza 가족은 [메인 저장소](https://github.com/rkttu/cadenza) 참고.
