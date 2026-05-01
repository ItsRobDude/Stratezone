# Stratezone Godot Project

This folder contains the Godot 4 C# project for Stratezone.

Current scaffold:

- `project.godot`: Godot project settings.
- `Stratezone.csproj`: C# project file for Godot .NET.
- `scenes/main/Main.tscn`: minimal launch scene.
- `scripts/simulation/`: plain C# simulation-facing code.
- `scripts/presentation/`: Godot-facing presentation scripts.
- `data/`: text-reviewable prototype content data.
- `assets/placeholders/`: placeholder art/data staging area.

The first scaffold is intentionally tiny. Gameplay rules should grow from `scripts/simulation/` and content data, not from scene-only logic.

`Stratezone.csproj` currently pins `Godot.NET.Sdk/4.6.2` to match the installed Godot .NET 4.6.2 editor.

Current greybox demo controls:

- Left click selects a unit/building or places the active building ghost.
- Drag with left mouse selects multiple visible friendly units.
- Right click moves selected units with a small formation spread, or cancels placement mode.
- Right click an enemy unit or building with selected combat units to attack it.
- Select one or more Workers, then press `1` Power Plant, `2` Pylon, `3` Barracks, `4` Extractor/Refinery, or `5` Defense Tower.
- Select a powered Barracks, then press `Q` Worker, `W` Cadet, or `E` Rifleman to queue Level 1 training.
- Select a Defense Tower, then press `G` Gun Tower or `T` Rocket Tower to upgrade in place.
- The command panel mirrors the same build, train, and upgrade actions with disabled-state tooltips.
- Mouse wheel zooms. `WASD` or arrow keys pan.
- `F9` decreases debug UI scale, `F10` increases it, and `F8` resets it.
- The demo starts with extra materials so the construction loop can be tested without waiting on income.
- Powered buildings show a small `⚡` prefix in their label.
- A first-pass enemy base starts with its own materials, powered Barracks, and production queue.
- The enemy construction planner can spend enemy materials to build or replace basic infrastructure from authored slots.
- Enemy units spend enemy materials, train from the enemy base, and then move toward the Colony Hub.
- Energy walls force the enemy to attack a nearby wall anchor before it can continue toward the Hub.
- Unit movement uses first-pass grid pathfinding around live building footprints and hostile energy wall segments.
- Fog starts black outside explored cells; scouted terrain stays revealed, and enemies in explored terrain remain visible in real time.
- Mission state is simulation-owned: Commander death loses, and destroying all enemy targets wins.
- The HUD alert line shows classic RTS warnings for player-known events such as enemy spotted, own assets under attack, power offline, construction complete, and training complete.
- Level 1 hides advanced Barracks training commands even though future-unit content records already exist.
- Rocket/Tank explosive attacks use simple full-strength splash. Friendly-fire explosives can hurt allied units.
- Rovers and Tanks can crush enemy infantry while moving through them; friendly infantry is ignored for now.

Current simulation smoke check:

```powershell
dotnet run --project tests/SimulationSmoke/SimulationSmoke.csproj
```
