# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

OptiScaler Client is a C# Avalonia UI desktop app (targeting `net10.0`) for installing, managing, and configuring the [OptiScaler](https://github.com/cdozdil/OptiScaler) mod across a user's game library. It supports multi-platform game scanning (Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox on Windows; Steam only on Linux), DLL injection into game directories, INI-based profile management, and component version management via GitHub releases.

## Commands

```sh
dotnet restore                                    # Restore NuGet packages
dotnet build                                      # Build for current platform
dotnet run                                        # Launch the app locally
dotnet test                                       # Run xUnit tests
dotnet publish -c Release -r win-x64              # Windows self-contained release
dotnet publish -c Release -r linux-x64            # Linux self-contained release
```

To run a single test class: `dotnet test --filter "FullyQualifiedName~AppConfigurationTests"`

## Architecture

**No DI container.** Services are instantiated directly with `new` in constructors or `MainWindow`. `PlatformServiceFactory` is a static factory for OS-specific implementations (GPU detection, shell operations).

**Layer overview:**
- `Views/` — Avalonia XAML + code-behind pairs. No MVVM framework; code-behind manages `ObservableCollection`s and calls services directly. `Dispatcher.UIThread.InvokeAsync()` is used for async→UI marshalling.
- `Services/` — All business logic. Scanners (`SteamScanner`, `EpicScanner`, etc.) are stateless. `GamePersistenceService` reads/writes `%APPDATA%/OptiscalerClient/games.json`. `GameAnalyzerService` caches upscaler version detection. `ComponentManagementService` handles GitHub release downloads.
- `Models/` — Plain data types. `OptimizerContext` is a source-generated `System.Text.Json` context for AOT-compatible serialization. `AppConfiguration` is the settings schema.
- `Helpers/` and `Converters/` — Shared UI utilities and XAML value converters.
- `Languages/` — 14 localized `Strings.<culture>.axaml` resource files. Language is swapped at runtime via `App.ChangeLanguage(langCode)`.
- `assets/configs/` — JSON schemas for the profile editor UI and DLL version maps (`dlss_version_map.json`, etc.).
- `config.json` — Bundled static config: GitHub repo mappings, scan exclusions, pinned beta releases. Treat as a user-facing contract.

**Platform branching** is done at runtime with `OperatingSystem.IsWindows()` / `OperatingSystem.IsLinux()` — not via project-level conditionals.

**Crash logging** goes to `%APPDATA%/OptiscalerClient/crash.log` via a global unhandled exception handler in `App.axaml.cs`.

## Coding Conventions

- Four-space indentation, PascalCase for public types/members, camelCase for locals/private fields, `Async` suffix on async methods.
- Nullable reference types and implicit usings are enabled. Keep new code null-safe.
- UI definitions go in `.axaml`; interaction logic in the matching code-behind `.axaml.cs`.
- Name services by responsibility: `SteamScanner`, `GamePersistenceService`, `WindowsGpuDetectionService`.

## Testing

Tests live in `Tests/` and use xUnit. The main project grants internals access via `[assembly: InternalsVisibleTo("OptiscalerClient.Tests")]`. Name test files after the class under test (e.g., `GameScannerServiceTests.cs`). Prefer deterministic unit tests; mock filesystem, HTTP, and shell boundaries rather than making live network calls.

## Security Notes

This app downloads archives and DLLs from external GitHub repos and writes them into game directories. When touching download/install flows: keep source URLs explicit, preserve backup/restore behavior, and avoid logging user-specific paths or API keys.

## Pull Request Guidelines

- Use concise, imperative commit messages; keep unrelated changes in separate commits.
- PR descriptions should explain the behavior change, list validation performed, link related issues, and include screenshots or recordings for UI changes.
- Note platform impact when touching Windows/Linux scanner, GPU detection, shell, or publishing behavior.
