# ParkMinDev.UPM.PackageManager

ParkMinDev 계정의 Unity 패키지를 조회하고 관리하는 Editor 도구입니다. 패키지 식별자는 com.parkmindev.* 규칙을 사용합니다.

## Repository access

- Personal Access Token(개인 접근 토큰)이 없으면 공개 저장소만 조회합니다.
- 개인 계정은 공개 저장소 목록과 토큰으로 접근 가능한 저장소 목록을 합치고, 지정 소유자의 저장소만 표시합니다.
- 조직은 조직 저장소 조회에 토큰을 사용하여 접근 권한이 있는 비공개 저장소도 조회합니다.
- 비공개 패키지를 검색하려면 토큰에 해당 저장소의 메타데이터와 파일 내용 읽기 권한이 있어야 합니다. 토큰 입력 후 새로고침하면 반영됩니다.
- 검색 인증과 Unity의 Git 패키지 설치 인증은 별개입니다. 비공개 패키지를 설치하려면 Git 인증도 설정되어 있어야 합니다.
- GitHub 저장소의 공개 상태를 기준으로 `[Public ParkMinDev]`와 `[Private ParkMinDev]` 영역에 나눠 표시합니다. 조회된 비공개 패키지가 없으면 Private 영역을 숨깁니다. 이 분류는 `parkmin-upm.json`에 별도로 선언하지 않습니다.

## Dependency metadata

- Unity Registry 의존성은 각 패키지의 `package.json`에 작성합니다.
- Git 및 NuGet 의존성은 각 저장소 루트의 `parkmin-upm.json`에 패키지별로 작성합니다.
- `packages` 배열의 각 항목은 하나의 설치 가능한 패키지입니다. `packagePath`는 저장소 루트 기준이며, 루트 패키지는 빈 문자열을 사용합니다.
- 패키지 식별자와 버전은 지정한 경로의 `package.json`에서 읽습니다.
- 자체 패키지 목록은 저장소 이름과 `packagePath`를 조합한 위치로 표시합니다. 루트 패키지는 `UPM-Foundation`, 내부 패키지는 `MediaPipeNativeRuntime/UPM`처럼 표시하며 `package.json`의 `displayName`은 변경하지 않습니다.
- 개인 계정과 조직을 모두 조회하며, 새 메타데이터가 없으면 기존 `parkmin-dependencies.json`을 읽습니다. 두 파일 모두 없으면 저장소를 제외합니다.
- 새 파일이 있으면 새 파일을 우선합니다. 잘못된 형식이나 지원하지 않는 스키마 버전은 오류로 표시합니다.
- 로컬 폴더로 연결된 패키지는 로컬 연결 상태로 표시합니다.
- PackageManager는 Git 및 NuGet 의존성의 설치 상태를 조회해 한 줄씩 표시하지만 자동으로 설치하지 않습니다.
- `Show Dependencies` 설정으로 의존성 표시 여부를 변경할 수 있습니다.

```json
{
  "schemaVersion": 1,
  "packages": [
    {
      "packagePath": "UPM",
      "gitDependencies": [
        {
          "packageName": "com.parkmindev.upm.foundation",
          "version": "10.1.5",
          "url": "https://github.com/ParkMinDev/UPM-Foundation.git"
        }
      ],
      "nugetDependencies": [
        {
          "packageName": "Example.Package",
          "version": "1.0.0"
        }
      ]
    }
  ]
}
```
