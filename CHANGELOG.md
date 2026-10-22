# Changelog

## [1.2.2]

Easier start for new players, profiles in the manager, bot radio switch.

### New
- **Welcome window on first start:** game folder (auto-detected, otherwise *Choose folder*) and player name in one step. If there is no BF2 profile yet, the manager creates it. Can be run again from the *Profile* menu.
- **"Your player" card on Play:** name, **Change name** (applies right away – no waiting, no restart), profile switcher, and the *Profile* menu with **New profile**, **Repair profile** (fresh files, same name – the fix for BF2 hanging at start or when joining), **Delete profile** and *Open profile folder*. Every change is backed up to `Documents\BF2_Backups`.
- **"Game stuck? Close it"** while BF2 runs, with tips (repair profile, ReShade off, windowed mode, check the server).
- **Game folder:** *Choose folder* directly on Play when BF2 is not found (also accepts the parent folder), *Settings › Folders › Change game folder* for later changes.
- **Bot radio messages on/off** (*Bots › Radio*): silences "Medic!", "Roger that" etc. by switching the mod's `ai\AIRadioMessages.ai` (backup created, applies at the next server start).
- **ReShade (optional) with setup guide:** what it does, how to install it from reshade.me for BF2 (Direct3D 9), freeze warning. The on/off switch appears once ReShade is installed.

### Changed
- The green "This PC is ready" bar is gone; the player card shows a small *✓ This PC is ready*. The yellow bar with **Fix** only appears when something needs attention.
- *Settings › Profile* is now *Game start* (windowed, skip intro, log in automatically); name and profile live on the Play page.
- "Log in automatically" passes the profile's account name, so login and profile always match.
- Bot module versions are only shown on the server PC (Host).
- Console commands window: close with **✕** at the top right (or Esc).

### Fixed
- *Settings › This PC* could not be expanded when everything was fine.
- *Fix* for the player name jumped to a page without a name field; it now asks for the name directly or creates the profile.
- Buttons in the status bar were square at rest and rounded on hover.
- No profile or only BF2's *Default* template: the name could not be set.

## [1.2.1]

First public release as **BF2 Manager** (previously BF2 Server Manager).

### New
- **Two modes: Play and Host**, switched at the top left. Play shows only what players need; Host contains everything including a Join button for the own server.
- **Play view like a server browser:** running servers with map, size, mode, players, bots and mod; **join compatibility** (mod present, same version) with one-click file fetch from the server PC; servers that are off are no longer listed; one entry per PC even with several network adapters.
- **Health bar:** green when the PC is ready, otherwise one *Fix* button; details are collapsed when everything is fine.
- **Server view follows the server state:** configuration while stopped, live view while running (players, upcoming maps, actions). Rotation and bot changes during a game are sent automatically and apply from the next map; the next map stays the one after the current.
- **Clear timing of changes:** ⚡ immediately, next map, or after a restart (with a *Restart now* button and a counter).
- **Rotation:** drag handles, right-click menu (custom bot values, duplicate, remove), shuffle, filter by map size, undo; rows show bots and difficulty, ★ marks custom values.
- **Your own default** for all mods (*Options › Default*), **recently started** configurations and presets in one menu.
- **Automatic saving** – no save button and no "unsaved changes" dialogs.
- **Crash and hang detection:** "Server stopped unexpectedly" / "Server not responding" with restart; automatic restart in *Options › Server behaviour* (default on).
- **Start check** before launching the server (rotation, bot module, port, server exe).
- **Diagnostics:** detected problems, server log, export without passwords, user names and public IP addresses.
- First start asks for the player name.

### Removed
- Getting-started card, pre-release updates, manual IP field on the main view (still available under *Server not found?*), the separate log card on the live view, one-time "next map only" values.

## [1.2.0]

Internal test version: player page, PC check, LAN discovery and file sync, updater, command assistant, leaderboard, scheduled messages, adaptive difficulty, portable mode.

## [1.1.1]

- Fixed: running-server bar after stop, mouse wheel in bot names, header overlap in small windows, silent removal of invalid rotation entries, Mono-only API crash at start.

## [1.1] / [1.0]

- Single exe with in-app setup, German/English, collapsible cards, live player list, bot names and AI file editors, unlock patches. First version as BF2 Coop Manager.
