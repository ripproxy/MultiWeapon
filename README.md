# MultiWeapon

Plugin for TShock that lets you attack with multiple hotbar weapons at the same time.

## Features (v1.3.0)
- Attack simultaneously from multiple slots (default: slot 0 + 1 + 2)
- **Individual cooldown** per weapon based on its `useTime`
- **Mana check & consumption** per weapon
- **Ammo check & consumption** per weapon
- Projectile follows player aim / item rotation
- Fully configurable via `tshock/MultiWeapon.json`
- Command `/mwreload` to reload config

## Requirements
- TShock **6.1.0+** (.NET 9)
- Terraria 1.4.5.x

## Installation
1. Build or download `MultiWeapon.dll`
2. Put it in `ServerPlugins` folder
3. Restart server
4. Config will be auto-generated at `tshock/MultiWeapon.json`

## Config (`tshock/MultiWeapon.json`)

```json
{
  "Enabled": true,
  "RequireSelectedSlot0": true,
  "ExtraSlots": [1, 2],
  "CheckMana": true,
  "CheckAmmo": true,
  "UsePlayerAim": true,
  "CooldownMultiplier": 1.0
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Master switch | `true` |
| `RequireSelectedSlot0` | Only work when holding slot 0 | `true` |
| `ExtraSlots` | Which slots will also attack | `[1, 2]` |
| `CheckMana` | Check & consume mana per weapon | `true` |
| `CheckAmmo` | Check & consume ammo per weapon | `true` |
| `UsePlayerAim` | Projectiles follow player aim | `true` |
| `CooldownMultiplier` | Multiply each weapon's useTime | `1.0` |

## Commands
- `/mwreload` — Reload config (permission: `multiweapon.reload`)

## Building
```bash
dotnet restore
dotnet build -c Release
```

## Author
ripproxy

## License
MIT
