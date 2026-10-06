# Changelog

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
