# Repository Guidelines

## Project Structure & Module Organization

Optiscaler Client is a C# Avalonia desktop app targeting `net10.0`. The app entry point is `Program.cs`, with application setup in `App.axaml` and `App.axaml.cs`. UI screens live in `Views/` as paired `.axaml` and `.axaml.cs` files. Business logic and platform integrations live in `Services/`; data types live in `Models/`; shared UI utilities live in `Helpers/`; value converters live in `Converters/`. Localized strings are in `Languages/Strings.<culture>.axaml`. Static assets, icons, bundled config schemas, and version maps are under `assets/`.

## Build, Test, and Development Commands

- `dotnet restore` restores NuGet packages.
- `dotnet build` compiles the app for the current platform.
- `dotnet run` launches the Avalonia desktop app locally.
- `dotnet publish -c Release -r win-x64` creates a Windows self-contained release build.
- `dotnet publish -c Release -r linux-x64` creates a Linux self-contained release build.
- `dotnet test` runs tests when a test project is present in the solution or working tree.

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, PascalCase for public types and members, camelCase for locals and private fields, and `Async` suffixes for asynchronous methods. Nullable reference types and implicit usings are enabled; keep new code null-safe and avoid unnecessary `using` directives. Keep Avalonia UI definitions in `.axaml` files and interaction logic in the matching code-behind file. Name services by responsibility, for example `SteamScanner`, `GamePersistenceService`, or `WindowsGpuDetectionService`.

## Testing Guidelines

No test project is currently checked in, but the app grants internals access to `Optiscaler-Client.Tests`. Add tests in a separate test project with focused coverage for services, scanners, parsing, persistence, and migration behavior. Prefer deterministic unit tests over live network calls; mock filesystem, shell, and HTTP boundaries where practical. Name test files after the class under test, such as `GameScannerServiceTests.cs`.

## Commit & Pull Request Guidelines

Recent history uses short descriptive commits and merge commits from feature branches, for example `Update README with additional game management images` and `feat/linux-port-backup`. Use concise, imperative commit messages and keep unrelated changes separate. Pull requests should describe the behavior change, list validation performed, link related issues, and include screenshots or screen recordings for UI changes. Note platform impact when touching Windows/Linux scanner, GPU, shell, or publishing behavior.

## Security & Configuration Tips

Be careful with download and install flows: this app retrieves archives and DLLs from external projects and writes into game directories. Keep source URLs explicit, preserve backup/restore behavior, and avoid logging user-specific paths or API keys. Treat `config.json` and `assets/configs/*.json` as user-facing configuration contracts.
