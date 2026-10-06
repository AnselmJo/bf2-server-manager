# BF2 Manager

[![CI](https://github.com/AnselmJo/bf2-manager/actions/workflows/ci.yml/badge.svg)](https://github.com/AnselmJo/bf2-manager/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/AnselmJo/bf2-manager)](https://github.com/AnselmJo/bf2-manager/releases/latest)

**The Battlefield 2 launcher for LAN parties: find the server and join with one click – or host a coop server with bots.**

One single `.exe`, no installation, German and English. Two modes, switched at the top left:

| **Play** | **Host** |
|---|---|
| Servers on the network appear automatically – click **Join**. Map, size, mode, players, bots, mod and compatibility at a glance. | Build a rotation, set bots per map, start the server. While it runs: live view with players, upcoming maps and actions. |

![Play](docs/images/play.png)

## Features

**Play**
- Automatic server discovery on the LAN – no IP addresses
- **Join compatibility:** mod present and same version? If not, fetch the files from the server PC with one click
- Health bar: green when this PC is ready, otherwise one **Fix** button (DirectX 9, firewall, 4 GB patch, unlock weapons, player name)
- Settings: profile and name, graphics and audio presets, windowed mode, skip intro, ReShade, mods, updates, diagnostics

**Host** (includes everything above plus a **Join** button for your own server)
- **Rotation with previews:** drag handles, right-click menu, shuffle, filter by map size, undo
- **Bots per map** (★ custom values) and defaults per map size in one place, adaptive difficulty
- **Live view** when the server runs: players with kick, upcoming maps, next map, restart round, message to all, console with command assistant, scheduled messages, evening leaderboard
- **Clear timing:** ⚡ applies immediately · rotation and bots from the next map · everything else after a restart (one click)
- **Your own default** for all mods, **recently started** configurations and presets
- Automatic restart when the server crashes or hangs, start check before launching
- Everything is saved automatically

![Host](docs/images/host.png)

## Quick start

1. Download **`BF2Manager.exe`** from the [latest release](https://github.com/AnselmJo/bf2-manager/releases/latest).
2. Put it into your Battlefield 2 folder (next to `BF2.exe`) – or anywhere, it finds the game.
3. Start it (SmartScreen: *More info › Run anyway*), enter your player name, click **Fix** if the bar is yellow.
4. **Play:** click **Join** on a server. **Host:** switch to *Host* at the top left and follow the one-time setup.

Preparing a LAN party? **[docs/LAN-PARTY.md](docs/LAN-PARTY.md)** – one folder, everything inside.

## Documentation

- [User guide](docs/USER-GUIDE.md) · [LAN party guide](docs/LAN-PARTY.md) · [Building](docs/BUILDING.md) · [Architecture](docs/ARCHITECTURE.md) · [Changelog](CHANGELOG.md)

## Requirements

Windows 10/11 (.NET Framework 4.5+ is part of Windows) and Battlefield 2 with patch 1.5. Tested mainly with the mod **bf2all64**; hosting works with any mod that has `python/game/scoringCommon.py`.

## FAQ

**No server appears.** Run *Fix* on both PCs (firewall). Same network? For VPN LANs (Radmin, ZeroTier, Hamachi) use *Server not found?* and enter the IP.
**Join stops at the login screen.** *Settings › Profile › Log in automatically* must be on and an offline profile selected.
**No unlock weapons.** Host: *Options › Server behaviour › all unlocks*. Every player: *Fix* (client unlock).

## Deutsch

Der BF2 Manager ist komplett zweisprachig (*Einstellungen › Sprache*). `BF2Manager.exe` in den Battlefield-2-Ordner legen, starten, Namen eingeben – Server erscheinen automatisch, **Beitreten** klicken. Wer hostet, schaltet oben links auf **Hosten**.

## License and disclaimer

MIT License, see [LICENSE](LICENSE). Fan project, not affiliated with or endorsed by Electronic Arts or DICE. Battlefield 2 is a trademark of Electronic Arts Inc. No game files included.
