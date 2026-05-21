# Bopl Battle MorePlayers Mod

Allows up to **8 players** in a single Bopl Battle lobby (game default is 4). Works over Steam multiplayer.

> **Every player in the lobby must have this mod installed** — not just the host.

---

## Requirements

- **Bopl Battle** v2.5.1 (Steam)
- **BepInEx** 5.4.23.5 — mod loader for Unity games

---

## Installation

### Step 1 — Install BepInEx

1. Download **BepInEx 5.4.23.5** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5)
   - Get the file named `BepInEx_win_x64_5.4.23.5.zip`
2. Extract the zip — you'll get a `BepInEx/` folder and two files: `winhttp.dll` and `doorstop_config.ini`
3. Copy those into your Bopl Battle game folder (the same folder that contains `BoplBattle.exe`)
   - Default Steam path: `C:\Program Files (x86)\Steam\steamapps\common\Bopl Battle\`
4. Launch the game once, then close it — BepInEx will create its config files

### Step 2 — Install the mod

1. Download **MorePlayers.dll** from the [latest release](https://github.com/Kakaman-Kakaman/bopl-battle-mod/releases/latest)
2. Place it in:
   ```
   Bopl Battle/BepInEx/plugins/MorePlayers.dll
   ```

### Step 3 — Configure (optional)

Open `BepInEx/config/com.MorePlayersTeam.MorePlayers.cfg` and change the player limit:

```
[General]
MaxPlayers = 8
```

You can set it to any number — though very high values may cause instability.

### Step 4 — Verify it's working

Launch the game and check `BepInEx/LogOutput.log`. You should see:

```
[Message:MorePlayers] More players acquired! Max players: 8
```

---

## Playing with 5+ players

1. **Every player** follows the installation steps above
2. Host creates a Steam friends lobby — it will have 8 slots
3. Up to 7 others can join (8 total including host)

---

## Building from source

Requires .NET SDK 8 and .NET Framework 4.7.2 targeting pack.

```powershell
cd src
dotnet build -c Release
```

Output: `src/bin/Release/net472/MorePlayers.dll`

Or run `build.ps1` from the repo root — it builds and copies the DLL to `BepInEx/plugins/` automatically.

---

## Compatibility

| Game version | Mod version | Status |
|---|---|---|
| v2.5.1 | v1.0.0 | ✓ Working |

---

## License

MIT
