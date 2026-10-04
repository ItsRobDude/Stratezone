# Colony Hub: first 3D look study

Created 2026-10-03 through the live Blender MCP connection to Blender 5.2.2 LTS.
Status: **visual proposal, entrance revision 2 / height revision 1**. This is art implementation,
not a docs-only change. The default game still uses existing sprites; a separate
mission comparison scene opts into the study Hub.

The user-approved entrance enlargement gives **2.70 m clear height and 2.62 m
clear width**, above the 0.52 m threshold. Doors, jambs, header, lights, badge,
ramp and curbs were adjusted together. The later height pass raises the lower
wall from **2.95 m to 3.85 m**, raises the upper deck with it, and adds 0.30 m
to the glazed story: the main roof is now **6.44 m**. The door header ends at
3.76 m, below the shoulder deck's 3.81 m underside. Footprint, door clearance,
and triangle count stay unchanged. This is a personnel entrance. Vehicles do not need to
fit through it. The new `RoverScaleStudy.tscn` has a dedicated entrance view
with the actual Rifleman standing on the ramp at the same meter scale.
The original v01 previews below predate the enlargement; current captures are
`art/units/rover/previews/hub_entrance.png` and `all_three.png`.

## Review files

- `colony_hub_study.blend`: editable named parts, four materials, daylight stage,
  45-degree orthographic camera, and all eight packed source reference images.
  The original default Blender scene is preserved separately.
- `previews/blender_front.png`: Blender EEVEE render.
- `previews/godot_front.png`: actual Godot Forward+ capture at 1280 x 800.
- `previews/godot_rear.png`: reverse-angle engine capture.
- `previews/godot_far.png`: engine capture at a candidate far zoom.
- `previews/godot_enemy.png`: the same mesh with red material overrides.
- `study_metrics.json`: evaluated Blender mesh counts and bounds.
- `game/assets/models/studies/colony_hub/colony_hub_study.glb` (repository-relative).
  The GLB contains only the merged Hub, not the studio or the original scene.
- `game/scenes/studies/ColonyHubStudy.tscn` (repository-relative): isolated static
  engine preview, with no simulation or mission wiring.
- `game/scenes/studies/ColonyHubMissionStudy.tscn`: actual mission comparison.
  F2 toggles the player Hub between its original sprite and the 3D model.
  The Grunt, Guardian, Commander, Rover, and any trained troops use the existing
  sprite renderer at its unchanged scale. No dummy figures are added.
- `previews/mission_3d_hub.png` and `previews/mission_2d_hub.png`: matched camera
  captures from that comparison scene.

## Design and scope

Reference: `game/assets/buildings/colony_hub/directional/`, especially the 180,
045, and 270 frames. All model geometry was authored for this study; no stock
models, external textures, or generative 3D services were used.

The design carries over the stepped bunker, smaller upper command deck, cyan
window band, corner armor, front entrance and ramp, winged badge, dish, mast,
and service panels. Small details are simplified. Matte material colors and
beveled geometry establish an initial real-time interpretation; painted wear,
texture baking, and a screen-space outline comparison remain future art work.

All study dimensions are provisional. The upper deck was reduced during visual
review to better match the reference's silhouette. The ramp is intended as
walkable decoration. No collision shapes or exported gameplay footprint are
included. Do not substitute these bounds for the current circular sim footprint.

## Measured asset properties

- 10,917 triangles, within the proposed Hub LOD0 budget of 6,000-12,000.
- One exported mesh, four material surfaces; shadows/depth passes are additional.
- 271 editable mesh objects in the source.
- Bounds including ramp and antenna: approximately 8.39 x 9.49 x 8.74 m in
  Blender XYZ dimensions. The building body is approximately 7.78 m deep;
  the entrance ramp increases the overall depth.
- Ground plane is Z=0; the foundation begins at Z=0.06 m.
- Main roof top: Z=6.44 m; highest antenna: Z=8.80 m.
- Front is Blender -Y, converted to Godot +Z; Godot +Y is up.
- GLB is self-contained and has vertex colors, normals, and no external textures.
- Source `.blend` and runtime `.glb` match the existing Git LFS patterns.

This first pass uses vertex colors and four explicit material groups. It has
not adopted the planned shared trim sheet, UVs, texture team mask, or per-instance
team shader. The red preview uses temporary Godot material overrides; it is not
a second mesh or a mission faction implementation.

## Export and engine materials

`tools/art/build_colony_hub_study.py` owns construction and export. The export
bakes evaluated geometry into a temporary mesh, writes only the active scene's
selected Hub, then removes that temporary mesh while retaining editable parts.
`raise_stories()` applies the height revision to an existing enlarged-entrance
source, with an idempotent scene revision tag. It preserves the entrance and
ground footprint, stretches wall masses, and lifts equipment with its deck.

During validation, Godot imported the paint surface with vertex-color albedo
disabled. Four explicit `.tres` materials next to the GLB, referenced through
its `.import` file, now preserve that flag across reimports. Their vertex colors
are linear (`vertex_color_is_srgb=false`). Recheck these resources when changing
the Blender shader values. [Godot's import documentation explains the material
flag](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).

## Actual mission scale comparison

`game/scripts/presentation/studies/ColonyHubMissionStudy.cs` extends the normal
mission presentation only in the opt-in scene. A transparent 3D viewport renders
the model from a frontal 45-degree camera into the existing player's Hub view.
The model's ground origin is anchored to the building's simulation position.
One meter maps to `RtsSimulation.ContentUnitScale` (24 world pixels), with no
change to the unit sprites, authored starts, game rules, or main scene.

The normal sprite's picking, footprint, fog visibility, and selection remain
owned by the existing building view. This is a mixed-renderer scale comparison,
not completed 3D integration: picking and collisions still use the old bounds,
units retain the old drawing order, and the model casts no shadow on the 2D map.
It should not be used to approve physical doorway clearance or occlusion.
The model appears larger than the current Hub sprite at this provisional scale.

Launch from the repo root:

```powershell
$env:STRATEZONE_UI_SCALE = '1.0'
& .\tools\godot.ps1 -GodotArgs @('res://scenes/studies/ColonyHubMissionStudy.tscn', '--resolution', '1280x800')
```

The UI scale setting is for a 1280 x 800 comparison window on a high-resolution
desktop; it changes HUD size only. Use the usual pan/zoom and unit controls.
F2 is exclusive to this study; the normal game's F8 UI-reset hotkey is retained.

## Reproduce

From the repo root, using Blender's Python runtime:

```python
import runpy
study = runpy.run_path("tools/art/build_colony_hub_study.py", run_name="hub_study")
study["build"]()
study["render"]()
```

Use an empty Blender session when rebuilding; the script deliberately refuses
to replace an existing `SZ_ColonyHub_Study` scene. The same script supports
Blender's `--background --python` entry point. Rebuilding writes the study
source, metrics, and GLB paths shown above. It does not rebuild engine materials.

Import or open the engine preview from PowerShell:

```powershell
pwsh -NoProfile -File tools/godot.ps1 -Import
& .\tools\godot.ps1 -GodotArgs @('res://scenes/studies/ColonyHubStudy.tscn', '--resolution', '1280x800')
```

## Validation and limits

The Blender source and exported GLB were inspected for matching triangle count,
one export mesh, four surfaces, vertex colors, bounds, and exclusion of the
default cube, stage, camera, and lights. Godot imported the GLB and rendered
front/rear, medium/far framing, and cyan/red variants at 1280 x 800 using the
desktop RTX 5090 and Forward+ renderer. All final captures were visually checked.

The candidate zoom distances are visual samples, not approved gameplay zoom
limits. The mission comparison was checked for rendering, unchanged unit sizes,
and the original/model toggle. No full mission playthrough, Steam Deck profiling, collision integration,
damage/construction states, or dish animation was performed. Those checks depend
on later integration and art approval. Current material and shape simplification
needs user review before treating this as the visual quality target.

Full local validation **passed**: content validation (37 records), C# build
(zero warnings/errors), simulation smoke, both missions' content drift checks,
and 120-frame headless Godot smoke. No `-SkipGodot` was used.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File plugins/stratezone-mission-steward/scripts/validate_stratezone.ps1
```

Additional checks: `tools/godot.ps1 -Import`, an isolated Godot capture run via
`tools/godot.ps1 -GodotArgs`, a structural read of the GLB, and `git check-attr
filter` on the two 3D binaries. Temporary capture scripts and intermediate
images are under ignored `artifacts/colony-hub-study/`.
