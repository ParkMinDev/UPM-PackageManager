# Changelog
All notable changes to this package will be documented in this file.

## [5.4.3] - 2026-10-04

### Changed
- Updated the nested-package documentation example to MediaPipeNativeRuntime/UPM.

## [5.4.2] - 2026-10-04

### Changed
- Standardized package identity and display name as `com.parkmindev.upm.packagemanager` / `ParkMinDev.UPM.PackageManager`.
- Synchronized own-package dependency versions for this release; C# namespaces and assembly names remain unchanged.

## [5.4.1] - 2026-10-04

### Changed
- Built the package manager window and package rows entirely in C#, removing the UXML templates and dedicated stylesheet.
- Unified public Git, public ParkMinDev, and private ParkMinDev section headings with the same blue, bold, 15-pixel style.
- Removed UnityUtils from the public package catalog without uninstalling existing project packages.

### Fixed
- Converted Color32 values explicitly to Color when assigning UI Toolkit style colors.

## [5.4.0] - 2026-10-04

### Added
- Included accessible private repositories in personal-account discovery when a GitHub Personal Access Token is provided.
- Split ParkMinDev packages into public and private sections based on GitHub repository visibility, hiding the private section when empty.

### Changed
- Displayed package locations using the repository name and packagePath while preserving Unity package identities and displayName metadata.
- Documented repository access permissions and the separate Git authentication required for private package installation.

## [5.3.2] - 2026-10-04

### Changed
- Standardized the nested-package metadata example to the UPM directory name.
- Kept package discovery based on parkmin-upm.json; no folder-name filtering was added.

## [5.3.1] - 2026-10-04

### Changed
- Renamed the repository to UPM-PackageManager and updated repository links without changing the Unity package identity, namespaces, assemblies, or asset GUIDs.
- Kept package discovery based on parkmin-upm.json rather than the repository name prefix.

## [5.3.0] - 2026-10-04

### Changed
- Moved repository links and dependency URLs to ParkMinDev while preserving the package identity.
- Replaced repository dependency metadata with parkmin-upm.json and aligned ParkMin dependency release versions.
- Added user/organization repository discovery, paginated listings, multiple UPM packages per repository, and legacy metadata fallback.
- Recognized locally linked packages in the package list.

## [5.2.1] - 2026-08-27

### Added
- Added repository subdirectory package discovery through `parkmin-dependencies.json` `packagePath` values.

### Changed
- Generated Git installation URLs with the Unity Package Manager `?path=/...` query when a package is stored below the repository root.

## [5.2.0] - 2026-08-16

### Added
- Added section headers for public Git packages and ParkMinPackages organization packages.
- Added package version suffixes with a `#최신` fallback for unversioned public Git entries.
- Added Git dependency version display and installed-version mismatch detection.

### Changed
- Extended public Git package catalog entries and Git dependency metadata with explicit version information.

## [5.1.0] - 2026-08-16

### Added
- Added Git and NuGet dependency discovery through repository `parkmin-dependencies.json` files.
- Added dependency visibility controls and installed, missing, and version-mismatch status indicators.
- Added ScriptableObject-based public Git package catalog entries and an Inspector creation workflow.

### Changed
- Unified public and organization package dependency resolution through a shared resolver.
- Split the public Git package catalog into individually editable data assets.

### Fixed
- Escaped branch names when requesting branch information from GitHub.

## [5.0.0] - 2026-07-25

### Breaking Changes
- Changed editor namespaces to the `ParkMinPackages.PackageManager.Editor` convention.

### Fixed
- Updated serialized package catalog type metadata to the new namespace.
## [2.1.2] - 2026-07-25

### Fixed
- Restored Unity's parameterless CreateGUI entry point for the Package Manager window.

## [2.1.1] - 2026-07-25

### Fixed
- Fixed Unity editor compilation errors in package catalog loading.

## [2.1.0] - 2026-07-25

### Changed
- Replaced per-repository GitHub API discovery with a central package catalog.
- Added a 15-minute local catalog cache and an expired-cache fallback when refresh fails.
- Compare installed and catalog package versions to determine update availability.

## [2.0.0] - 2026-07-25

### Breaking Changes
- Renamed public namespaces and assembly definitions from Mutant to ParkMinPackages.
- Projects using the previous namespaces or assembly names must update their references.

## [1.1.7] - 2026-07-24
### Changed
- Updated GitHub organization discovery to ParkMinPackages.
- Renamed the Package Manager menu and editor UI to ParkMinPackages.
- Made the GitHub Personal Access Token optional for public packages; it remains available for private packages.

## [1.1.2] - 2026-04-07
### gitingore 수정해서 .unitypackage 무시안하게 변경

## [1.1.1] - 2026-04-07
### 외부에서 참조 못하게 internal로 변경, assemblydefinition 옵션 변경

## [1.1.0] - 2026-04-07
### 필수 package 목록들 추가, 로컬 패키지 폴더 기능 추가

## [1.0.0] - 2026-04-07
### 실사용 가능 수준으로 완성

## [0.1.0] - 2026-04-06
### This is the first release of *\<Mutant.PakcageManager\>*.
