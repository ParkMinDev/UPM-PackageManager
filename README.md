# ParkMinPackages.PackageManager

ParkMinDev 계정의 Unity 패키지를 조회하고 관리하는 Editor 도구입니다. 패키지 식별자는 기존 com.parkminpackages.* 이름을 유지합니다.

## Dependency metadata

- Unity Registry 의존성은 각 패키지의 `package.json`에 작성합니다.
- Git 및 NuGet 의존성은 각 저장소 루트의 `parkmin-upm.json`에 패키지별로 작성합니다.
- `packages` 배열의 각 항목은 하나의 설치 가능한 패키지입니다. `packagePath`는 저장소 루트 기준이며, 루트 패키지는 빈 문자열을 사용합니다.
- 이름, 표시 이름, 버전은 지정한 경로의 `package.json`에서 읽습니다.
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
          "packageName": "com.parkminpackages.foundation",
          "version": "10.1.4",
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
