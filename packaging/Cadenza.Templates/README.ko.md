# Cadenza.Templates

> [English](README.md)로 보기.

Cadenza 단일 파일 스크립팅 SDK 가족을 위한 `dotnet new` 프로젝트 템플릿. 각 템플릿은 즉시 `dotnet run` 가능한 `.cs` 파일 한 장을 생성합니다.

## 설치

```bash
dotnet new install Cadenza.Templates
```

## 사용

| 짧은 이름 | 변종 | 생성물 |
| --- | --- | --- |
| `cadenza-console` | `Cadenza` | 콘솔 스크립트 (셸, CLI, 빌드 글루) |
| `cadenza-worker` | `Cadenza.Worker` | 백그라운드 서비스 / 데몬 |
| `cadenza-web` | `Cadenza.Web` | Minimal API 엔드포인트 |
| `cadenza-mcp` | `Cadenza.Mcp` | Claude Desktop / Cursor / VS Code AI용 MCP 서버 |

```bash
dotnet new cadenza-console -n mytool -o ./mytool
cd mytool
dotnet run mytool.cs
```

각 starter는 매칭되는 SDK 버전을 정확히 핀하고, 해당 변종의 Tier 1 bare name 목록 및 self-contained 바이너리 publish 명령을 주석으로 포함합니다.

## 카테고리

네 템플릿 모두 `Cadenza` classification 태그를 공유해 Visual Studio "New Project" 다이얼로그가 향후 표시할 때 한 그룹으로 묶이고, 각자 고유 변종 태그(`Console`, `Worker`, `Web`/`WebAPI`, `AI`/`MCP`)도 함께 가져 해당 필터에서도 노출됩니다. `defaultName`은 변종별 합리적 기본명을 미리 채워줍니다 (`MyScript`, `MyWorker`, `MyApi`, `MyMcpServer`).

## 제거

```bash
dotnet new uninstall Cadenza.Templates
```

전체 SDK 가족은 [프로젝트 저장소](https://github.com/rkttu/cadenza) 참고.
