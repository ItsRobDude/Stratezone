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

- Left click selects a unit or places the active building ghost.
- Right click moves the selected unit, or cancels placement mode.
- Right click an enemy unit or building with a selected combat unit to attack it.
- Select the Worker, then press `1` Power Plant, `2` Pylon, `3` Barracks, `4` Extractor/Refinery, or `5` Defense Tower.
- Mouse wheel zooms. `WASD` or arrow keys pan.
- `F9` decreases debug UI scale, `F10` increases it, and `F8` resets it.
- The demo starts with extra materials so the construction loop can be tested without waiting on income.
- Powered buildings show a small `⚡` prefix in their label.
- A first-pass enemy base starts with its own materials, powered Barracks, and production queue.
- The enemy construction planner can spend enemy materials to build or replace basic infrastructure from authored slots.
- Enemy units spend enemy materials, train from the enemy base, and then move toward the Colony Hub.
- Energy walls force the enemy to attack a nearby wall anchor before it can continue toward the Hub.

Current simulation smoke check:

```powershell
dotnet run --project tests/SimulationSmoke/SimulationSmoke.csproj
```
