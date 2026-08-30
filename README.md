# MultiWeapon

Plugin for TShock (Terraria) that allows attacking with multiple weapons at the same time (hotbar slot 0 + 1 + 2).

## Features
- Simultaneous attacks from inventory slots 0, 1, and 2
- Individual cooldown per weapon based on `useTime`
- Supports melee and projectile-based weapons
- Compatible with **TShock 6.1.0** / **.NET 9** / Terraria 1.4.5.x

## Requirements
- TShock 6.1.0 or newer
- .NET 9 Runtime (already included in TShock 6)

## Installation
1. Download the latest `MultiWeapon.dll` from Releases (or build it yourself)
2. Place it in your server's `ServerPlugins` folder
3. Restart the server

## Building
```bash
dotnet restore
dotnet build -c Release
```
Output: `MultiWeapon/bin/Release/net9.0/MultiWeapon.dll`

## Author
ripproxy

## License
MIT
