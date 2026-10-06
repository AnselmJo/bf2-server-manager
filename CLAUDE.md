# CLAUDE.md – project guide for Claude Code

BF2 Manager: Windows Forms tool for Battlefield 2 LAN parties (coop server with bots, player PC setup, LAN sync, updater). Owner: AnselmJo. UI languages: German and English. Docs: English.

## Build and test

- Windows: `Build.cmd test` (uses the .NET Framework csc in %WINDIR%, C# 5). Output: `out\BF2Manager.exe`, `out\Tests.exe`.
- All tests must pass before committing.

## Hard rules

- C# 5 only (no `$""`, `?.`, `=>` members, `nameof`, `out var`, tuples).
- Only .NET Framework 4.5 APIs. Never introduce NuGet packages or extra DLLs – the product is a single exe.
- Every UI string as `T("Deutsch", "English")`.
- `src/Server/coopbots.py` runs inside BF2's Python 2.3: no `sorted()`, no generator expressions, no `with`, no `x if c else y`, no `except X as e`. When changing it, bump `MODULE_VERSION` and `AppInfo.ModuleVersion`.
- Never write game files without `Util.Backup` and never without a user action.
- Do not use `Control.Visible` as state (it is false while a page is hidden).
- Keep the UI understandable for non-technical users: tooltip for every setting, confirmation for dangerous actions, actionable error messages.

## Layout

See `docs/ARCHITECTURE.md`. Main window = partial class `MainForm` in `src/App/MainForm.*.cs` (one file per page). Pages: Player, Server, Bots, Maps, Live, Extras.

## Releases

1. Bump `AppInfo.Version` (+ AssemblyVersion/AssemblyFileVersion) in `src/Game/GameInstall.cs`, and the version numbers in `build/app.manifest`.
2. Add `## [x.y.z]` to `CHANGELOG.md`.
3. Commit, `git tag vX.Y.Z`, `git push origin main --tags`. The release workflow builds, tests, checksums and publishes. The in-app updater relies on the asset names `BF2Manager.exe` and `SHA256SUMS.txt`.
