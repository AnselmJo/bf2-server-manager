# Building

The manager is a single Windows Forms executable written in **C# 5** for **.NET Framework 4.5+**. No NuGet packages, no Visual Studio needed.

## Windows

```cmd
Build.cmd          rem builds out\BF2Manager.exe
Build.cmd test     rem additionally builds and runs out\Tests.exe
```

`Build.cmd` uses the `csc.exe` that ships with Windows (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`). That compiler only supports C# 5 – please keep the code C# 5 compatible (no `$"..."`, no `?.`, no `=>` members, no `nameof`).

## Linux (Mono)

```sh
build/build.sh        # out/BF2Manager.exe
build/build.sh test   # also out/Tests.exe; run with: mono out/Tests.exe
```

Requires `mono-mcs` and `binutils-mingw-w64` (for `windres`). The script compiles against the official .NET Framework 4.5 **reference assemblies** (downloaded once from github.com/mono/reference-assemblies). Never compile against Mono's own class libraries: they contain overloads that do not exist on Windows (e.g. `string.Split(char, StringSplitOptions)`), which compile fine and then crash on Windows.

## Embedded resources

- `src/Server/coopbots.py` – the server module, embedded as resource `coopbots.py` and installed into `mods/<mod>/python/game/` by the setup.
- `build/app.ico`, `build/app.manifest` – icon and manifest (`asInvoker`, DPI aware). The manifest is important: without it Windows would redirect writes to `Program Files` into the VirtualStore.

## Tests

`tests/Tests.cs` is a small console runner without a test framework. It covers settings conversion, bot planning, `coop_server.txt`, version comparison, checksums, the 4 GB and unlock patches (on synthetic files), LAN manifest diff and path safety, leaderboard, profile files, `mod.desc` and the command catalog. CI runs it on every push (`.github/workflows/ci.yml`).

## Releasing

1. Update `AppInfo.Version` in `src/Game/GameInstall.cs` (and `AssemblyVersion`/`AssemblyFileVersion` there).
2. If `coopbots.py` changed, increase `MODULE_VERSION` in it and `AppInfo.ModuleVersion`.
3. Add a section `## [x.y.z]` to `CHANGELOG.md` – it becomes the release notes and is shown in the app after updating.
4. Commit, then tag and push: `git tag v1.2.0 && git push origin v1.2.0`

`.github/workflows/release.yml` checks that the tag matches `AppInfo.Version`, builds and tests on Windows, writes `SHA256SUMS.txt` and publishes the release. Tags with a hyphen (`v1.3.0-beta.1`) become pre-releases, which the app only installs when *Pre-releases* is enabled.

The in-app updater downloads `BF2Manager.exe` and `SHA256SUMS.txt` from the latest release and refuses the update if the checksum does not match. Both asset names must stay exactly like this.
