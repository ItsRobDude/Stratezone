# Basic pine and cherry world props

Original geometry authored through Blender MCP 5.2.2 LTS for Stratezone.
The requested cherry trees have **green summer foliage**, not blossoms.
These are simple low-poly props, intended to establish shapes and scale before
texture, wind and forest integration work.

| Asset | Shape | Height | Triangles |
| --- | --- | ---: | ---: |
| `pine_a` | Full layered evergreen crown | 7.00 m | 620 |
| `pine_b` | Slender, slightly leaning crown with exposed lower trunk | 6.00 m | 558 |
| `cherry_a` | Low fork and broad spreading canopy | 4.90 m | 750 |
| `cherry_b` | Taller trunk and asymmetric upright canopy | 6.02 m | 750 |

All four use one merged mesh and one shared, opaque, matte material. Geometry
and silhouettes differ; these are not just scaled duplicates. Cherry bark has
a few horizontal marks and the canopy contains six small red fruit accents.
Solid foliage avoids alpha-card overdraw. The current vertex colors replace
the planned shared atlas for this basic pass. There are no external textures,
stock meshes or external asset licensing requirements.

## Files and placement

- `tree_props.blend`: editable source scene `SZ_TreeProps_Study`, one named
  collection per variant. Other previously open study scenes are preserved.
- `tree_metrics.json`: measured Blender dimensions and triangle counts.
- `previews/`: real Godot 1280x800 lineup, close species views, reverse views
  and candidate far-zoom captures.
- Runtime: `game/assets/models/props/trees/` contains the four individual
  GLBs, the shared `tree_props.tres`, and persistent import sidecars.
- Builder: `tools/art/build_tree_props.py`.
- Viewer: `game/scenes/studies/TreePropsStudy.tscn` and its C# study controller.
- Export verification: `tools/art/verify_tree_props.gd`.
- Direction notes: `docs/3d-art-direction.md`, Basic World Tree Props.

Use exported GLBs at scale 1: one unit is one meter. Each origin sits at the
trunk's ground contact; Godot +Y is up. Blender source parts are laid out for
review with object X offsets, while their mesh coordinates remain relative to
their own tree's ground origin. The builder exports before that display layout.
For manual mesh editing, keep those display offsets out of the exported asset.

Each distinct mesh can be used in its own MultiMesh batch. All four reference
the same material, with vertex-color albedo enabled and sRGB conversion off.
Godot generates LOD and shadow meshes during import. No skeleton, animation,
collision, gameplay blocker, map placement or forest scatter is added here.

## Review and reproduction

```powershell
& .\tools\godot.ps1 -GodotArgs @('res://scenes/studies/TreePropsStudy.tscn','--resolution','1280x800')
```

**1** all four, **2** pines, **3** cherries. Middle drag rotates the fixed
45-degree orthographic camera, wheel zooms, and Home faces north. The existing
2.23 m Rifleman appears at unchanged scale. Labels are developer-only English.

From an empty Blender session, run
`blender --background --factory-startup --python tools/art/build_tree_props.py`.
Or load the builder with `runpy` through MCP and call `build()`. The builder
refuses to replace an existing source scene. Fresh builds write the source,
metrics and runtime GLBs; preserve manual edits before rebuilding. No downloads
or added dependencies are required.

Validation commands:

```powershell
dotnet build game/Stratezone.csproj
& .\tools\godot.ps1 -Import
& .\tools\godot.ps1 -GodotArgs @('--script','res://../tools/art/verify_tree_props.gd','--resolution','1280x800')
powershell -NoProfile -ExecutionPolicy Bypass -File plugins/stratezone-mission-steward/scripts/validate_stratezone.ps1
git diff --check
```

The export check compares Blender metrics with Godot heights and triangle
counts, requires one mesh/surface and the same external material, checks the
ground origin, and verifies MultiMesh compatibility. Source and GLBs use Git LFS.

All listed commands passed on Windows/Godot 4.7 .NET, including the complete
content/build/simulation/drift/headless-engine stack with no skipped stages.
A fresh background Blender build in `artifacts/tree-props/rebuild/` reproduced
all four variants' measured bounds and counts exactly. Front and reverse
species views, the Rifleman scale lineup and far framing were inspected at
1280x800 on the desktop RTX 5090. The pine close-up was reframed to keep its
tip below the review header; far-view labels shorten to avoid overlap.

This is art and review-viewer implementation. Current mission visuals and
simulation behavior are unchanged. No Steam Deck hardware profiling, dense
forest readability test, wind animation or mission placement has been done.
These depend on later world integration; colors and canopy density are still
provisional. All source files added for this pass are below 900 lines.
