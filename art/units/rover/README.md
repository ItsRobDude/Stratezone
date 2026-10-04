# Rover / infantry / Hub scale study

2026-10-03, Blender 5.2.2 LTS via MCP, Godot 4.7 .NET on Windows.
Art and presentation implementation; **provisional scale study**, not final
surface quality or mission integration. The existing Rifleman remains a rough
blockout requiring major polish, as requested in review.

## Review

```powershell
& .\tools\godot.ps1 -GodotArgs @('res://scenes/studies/RoverScaleStudy.tscn','--resolution','1280x800')
```

- **1**: Rover, actual Rifleman and original Rover sprite.
- **2**: Hub, Rifleman and Rover together at unit scale 1.
- **3**: enlarged Hub entrance with the Rifleman on the ramp.
- **R**: preview tire spin, front steering and radar scanning; press again to pause.
- **T**: cyan/red team accents. Middle drag rotates, wheel zooms, Home faces north.

The camera stays at 45 degrees and orthographic. No arbitrary per-asset visual
scaling is applied. The preview's motion tests articulation in place, not
vehicle travel or simulation movement.

| Measurement | Meters |
| --- | ---: |
| Rover length, bumpers included | 4.827 |
| Rover width, outer hubs included | 2.600 |
| Roll cage top above ground | 2.331 |
| Radar top above ground | 3.285 |
| Highest antenna above ground | 3.070 |
| Wheelbase | 2.880 |
| Equipped Rifleman helmet top | 2.225 |
| Hub opening clear height | 2.700 |
| Hub opening clear width | 2.620 |

The Rover is a compact light vehicle beside this infantry. The personnel door
is not a garage: the radar is taller than the opening and the vehicle would
have virtually no width clearance. Hub size, vehicle spawning, collision and
gameplay footprints are separate from this art-only study.

## Design and files

Reference: all eight existing `game/assets/units/rover/directional/` frames,
with front, side and rear inspected before modeling. All vehicle geometry was
authored for Stratezone. No stock vehicle/character or generation service was used.
The sprite's open 4x4, roll cage, sloped hood, rear cargo, antennas, cyan
headlights and large off-road wheels establish the shape. **Sensor revision 1**
replaces its gun with an original compact surveillance radar, as approved by
the user, to match the Rover's non-shooting scout role. Chassis, crew and wheel
geometry are unchanged. The sealed panel, pedestal and rear cooling fins use
the [FLIR Ranger R20SS](https://defense.flir.com/defense-products/ranger-r20ss/)
as a visual reference; its shape was also inspected in this
[product photograph](https://kvrinfrared.com/product/ranger-r20ss/).
No manufacturer branding or real-world sensor performance is adopted.

The visible driver is a static seated pose of **our existing shared infantry
body at its original scale**, with its helmet/goggles. It is merged into the
chassis. This introduces no second infantry skeleton. Its posing is provisional.

- `rover_study.blend`: editable parts tagged by chassis, radar and wheel.
- `study_metrics.json`: evaluated triangle counts and bounds.
- `previews/`: actual 1280x800 Godot captures of lineup, doorway, sprite
  comparison, side/rear and moving parts with red accents.
- `game/assets/models/studies/rover/`: runtime GLB, shader, material and import sidecars.
- `game/scenes/studies/RoverScaleStudy.tscn` and
  `game/scripts/presentation/studies/RoverScaleStudy.cs`: isolated review viewer.
- `tools/art/build_rover_study.py` and `rover_study_common.py`: geometry and export.
- `tools/art/verify_rover_scale_study.gd`: engine contract checks and captures.

## Mesh and articulation

5,990 triangles **including crew**; one vertex-color/team-mask material. Six
rigid parts are exported: chassis, radar and four wheels, with useful local
pivots. The review viewer uses the front-right wheel mesh for all four wheels
in one MultiMesh, yielding three visible surfaces before shadows: chassis,
radar, wheels. The left wheel transforms face outward and their spin reverses
accordingly. Original individual wheel meshes stay hidden as source markers.

No runtime skeleton, physics suspension, IK, firing behavior, collision, or
mission wiring is introduced. UV0.x stores the team mask; vertex colors supply
temporary materials. Proper textures, trim detail and wear remain future art.

## Reproduction and validation

From an empty Blender session, run `build_rover_study.py` through MCP/runpy,
or `blender --background --factory-startup --python tools/art/build_rover_study.py`.
The builder imports the crew from the saved Rifleman source if it is not open.
It refuses to overwrite an existing Rover scene; edit the source instead.
Rebuilding writes the source, metrics and GLB. Back up local art edits first.
For export-only edits, open the source and call the module's `export()`.
For an existing source with the original gun, `replace_gun_with_radar()`
revises only that roof assembly and records an idempotent sensor revision.
The `Radar` export pivots at Blender (0, 0.23, 2.30); the viewer scans through
360 degrees in about 22 seconds. The 388-triangle head clears the cage and
antenna positions throughout the rotation. This is visual preview motion.

The Hub enlargement is in `build_colony_hub_study.py:enlarge_entry()`, called
by fresh builds and guarded by an entrance revision tag when editing a scene.
It checks the header underside against the threshold for 2.70 m clearance.
The subsequent `raise_stories()` pass brings the wall to 3.85 m and main roof
to 6.44 m without changing the door or footprint. The source and GLB backups
are under ignored `artifacts/hub-radar-revision/`.
The prior Hub source/export were retained under ignored
`artifacts/vehicle-scale-study/` before the edit.

Commands used:

```powershell
dotnet build game/Stratezone.csproj
& .\tools\godot.ps1 -Import
& .\tools\godot.ps1 -GodotArgs @('--script','res://../tools/art/verify_rover_scale_study.gd','--resolution','1280x800')
powershell -NoProfile -ExecutionPolicy Bypass -File plugins/stratezone-mission-steward/scripts/validate_stratezone.ps1
git diff --check
```

Height/radar revision checks also ran
`blender --background --factory-startup --python artifacts/hub-radar-revision/verify_rebuild.py`.
The scratch outputs matched both canonical assets' triangle counts and bounds;
reapplying either revision was a no-op. Before replacement, a source-vertex
hash verified the Rover chassis, crew and wheels stayed unchanged. Blender
measurements verified the Hub header sits below the deck and its horizontal
bounds are unchanged. Godot additionally checked that the radar replaces the
gun node, imports at the expected height, and rotates when motion is enabled.
Updated captures include `radar_scan.png` plus front, side, rear and doorway views.

The engine check verifies unit scale, no Rover skeleton, two visible individual
meshes plus four instanced wheels, wheel bounds/pivots matching the exported
parts, and working preview motion. Front, side, rear, enlarged entrance and
the three-asset lineup were visually inspected. Headless reproduction is
checked against the saved study's counts and bounds. All 3D binaries use LFS.
All listed checks passed, including the full content/build/simulation/drift/
headless-Godot validation stack, with no skipped stages. The clean background
rebuild matched the saved metrics exactly. The interactive viewer was opened
on the all-three lineup after verification.

Remaining: detailed materials/camouflage, less boxy cargo and armor, crew
hand/seat fitting, full suspension/steering clearance, final silhouette and
team readability at mission zoom, gameplay footprint integration, and Steam
Deck profiling. No finished-art or Deck-performance claim is made from this
desktop scale review.
