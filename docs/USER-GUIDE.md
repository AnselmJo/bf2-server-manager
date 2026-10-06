# User guide

Switch between **Play** and **Host** at the top left. Every setting has an **ⓘ** with an explanation, the default and when a change applies. Everything is saved automatically.

## Play

**Play** – the main view. Running servers on the network appear automatically with map, size, mode, players, bots and mod.
- **✓ Compatible** → click **Join**. The game starts in the server's mod, logs in and joins.
- **⚠ Mod missing / different version** → **Get files from server** compares your mod with the server PC and copies only what differs (each file is verified).
- **Manager x.y – update** appears when the server PC has a newer manager.
- No server? *Start game without server* opens the main menu. *Server not found?* lets you enter an IP (only needed for VPN LANs).
- The bar at the top is green when this PC is ready; otherwise **Fix** repairs everything with one Windows prompt.

**Settings** – This PC (check and fixes), Profile (name, windowed, skip intro, log in automatically), Graphics, Audio, Mods (versions), Updates, Diagnostics, Language, Folders.

## Host

On the first switch to *Host* you set up the server once (pick mods, one Windows prompt). The top bar shows mod, mode, server status, **Join** (your own server) and **Start/Stop server**.

**Server** – while the server is stopped: build the rotation.
- Double-click a map on the left (filter by size) to add it. Drag the **≡** handle to sort, **right-click** for more (custom bot values, duplicate, remove), shuffle, **Undo**.
- Select a map to see its preview and its **bot values**. *Custom values* gives this map its own bots (★).
- **Recent & presets**: reload a configuration you started before, or save/load named presets.

While the server runs, **Server** shows the **live view**: players (kick), current and upcoming maps, next map now, restart round, message to all, console with **Commands** assistant, scheduled messages, evening leaderboard. *Edit rotation & bots* switches to the configuration – changes apply from the next map and are sent to the server automatically.

**Bots** – default bot values per map size (16/32/64), humans per team, adaptive difficulty, bot names and the mod's AI file (*Advanced*).

**Options** – every server setting. Cards *Server behaviour* (automatic restart after a crash or hang, server unlocks, LAN service) and *Default* (use current values as your default for all mods, reset to default, factory settings). While the server runs: ⚡ values apply immediately, the bar shows how many changes need a restart and offers **Restart now**.

## When do changes apply?

| | |
|---|---|
| ⚡ immediately | message, kick, respawn time, friendly fire and other ⚡ settings |
| next map | rotation, bots, difficulty, custom map values |
| after a restart | mod, mode, max. players, port and other settings without ⚡ |

## Diagnostics

*Settings › Diagnostics*: detected problems, **Server log**, **Export diagnostics** (ZIP with versions, Windows version, mods, configuration, logs and problems – without passwords, user names and public IP addresses), **Repair**.

## Keyboard

F5 start/stop server · Ctrl+1 … pages in sidebar order

## Where things are stored

Settings, presets, recent configurations and your default: `%APPDATA%\BF2Manager` · backups of changed game files: `Documents\BF2_Backups` · server configs: `Documents\Battlefield 2\ServerConfigs`
