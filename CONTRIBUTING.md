# Contributing

Thanks for helping! A few rules keep the manager easy to build and easy to use.

## Code

- **C# 5 only** (the compiler that ships with Windows). No string interpolation, `?.`, expression-bodied members, `nameof`, `out var`.
- **Target .NET Framework 4.5 APIs.** Mono-only overloads break on Windows – CI builds on Windows to catch this.
- **Every UI text in both languages:** `T("Deutsch", "English")`.
- **Non-technical users first:** every setting needs an explanation (ⓘ tooltip), dangerous actions need a confirmation, errors must say what to do next.
- **Do not write game files without a backup** (`Util.Backup`), and never without the user's action.
- `src/Server/coopbots.py` must stay Python 2.3 compatible (see ARCHITECTURE.md). If you change it, increase `MODULE_VERSION` and `AppInfo.ModuleVersion`.

## Before a pull request

```cmd
Build.cmd test
```

All tests must pass. Add tests for new logic that does not need the UI (`tests/Tests.cs`).

## Reporting bugs

Please use the issue template and attach *Extras › Maintenance › Export diagnostics*.
