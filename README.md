# DisplayGunStats

A BepInEx mod for MycoPunk that displays comprehensive statistics for your currently equipped weapon in a real-time HUD overlay.

## Description

This client-side mod provides detailed, real-time statistics for your active weapon, including damage, fire rate, ammunition capacity, recoil patterns, spread data, burst settings, reload times, and more. The information is presented in a clean, color-coded HUD positioned near your reticle for quick reference during gameplay.

Built as a complete rebuild from the ground up, the mod uses Harmony patches to access weapon data and automatically detects changes in your active weapon. All statistics update every 0.5 seconds to ensure accuracy, and the display can be toggled on or off with the F9 key. The mod is designed to be compatible with other HUD elements like damage meters and speedometers.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "DisplayGunStats" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `DisplayGunStats.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once installed, the gun statistics display will appear automatically:

**In-Game Controls:**
- Press F9 to toggle the statistics display on/off
- The HUD appears in the upper-left area near your reticle
- Statistics update every 0.5 seconds as you switch weapons or upgrades

**What Gets Displayed:**
- Damage per shot and damage type
- Fire rate and burst fire settings
- Ammunition capacity and magazine sizes
- Reload and charge durations
- Explosion/hit force for explosives
- Range and accuracy metrics
- Recoil pattern data (X/Y axes)
- Spread size information
- Firing mode (automatic/semi-automatic)

Statistics adjust automatically when you equip different weapons or upgrade them.

## Help

* **HUD not visible?** Make sure you have a weapon equipped and try pressing F9 to toggle it
* **Wrong stats showing?** The mod detects your active weapon - make sure you're looking at the right weapon in your inventory
* **Stats not updating?** They update every 0.5 seconds - rapid changes might take a moment to reflect
* **Moving UI elements?** The HUD repositions automatically if you have other mods like DamageMeter or Speedometer active
* **Performance issues?** The mod only updates active weapons and shouldn't impact gameplay performance
* **Color meaning?** Different colors help categorize stats (red for damage, blue for ammo, yellow for fire rate, etc.)
* **Weapons not supported?** Supports all standard weapons - exotic weapons may show partial data
* **Not compatible?** This mod patches Gun class methods - other mods modifying weapon data may interfere

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
