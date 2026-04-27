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

`Stratezone.csproj` currently pins `Godot.NET.Sdk/4.4.1`. When Godot .NET is installed locally, verify that this SDK version matches the installed Godot version and update the project file if needed.
