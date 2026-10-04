# Rifleman and shared infantry study

First asset/rig study, 2026-10-03. **Rough rig/scale blockout; major visual polish
is required.** This is not the accepted quality bar or a mission art replacement.
Authored through Blender MCP 5.2.2 LTS and reviewed
in the repository's pinned Godot .NET build on Windows at 1280x800.

## Open and review

- `rifleman_study.blend`: editable body, separate tagged gear, master armature,
  three baked actions, and a hidden merged runtime mesh. Directional sprite
  references are packed into the file.
- `../shared_infantry/shared_infantry.blend`: reusable custom body, **the same
  `SZ_Infantry` rest skeleton**, and actions. Open this for the next soldier;
  derive identity from gear. Do not auto-rig every kit independently.
- `../../../../game/assets/models/studies/rifleman/`: runtime exports and material.
  The model has no animation. `infantry_anims.glb` imports as AnimationLibrary.
- `study_metrics.json`: measured geometry, weights, hierarchy, clips, and hashes
  of the original downloaded FBXs.
- `previews/godot_reference.png`: model beside `rifleman_180.png`.
- `previews/godot_hub_scale.png`: real model beside the existing Hub in meters.

From the repository root:

```powershell
& .\tools\godot.ps1 -GodotArgs @('res://scenes/studies/RiflemanStudy.tscn')
```

Controls: **1** model and sprite, **2** beside Hub, **R** next clip, **Space**
pause, **T** cyan/red team accents, middle drag to rotate, wheel to zoom,
**Home** north. The camera stays at 45 degrees. These are developer study
controls, not new mission controls.

## Design and source provenance

The eight existing `game/assets/units/rifleman/directional/` sprites are the
design reference, with front, side, and rear examined for gear. The kit has
olive/tan fatigues, dark carrier and pads, helmet/goggles, radio backpack,
utility pouches, boots, and a short bullpup. All geometry is custom-authored;
no stock character mesh was downloaded.

The base is a closed 982-triangle T-pose mesh with joint rings. Rob authorized
uploading it in his logged-in Mixamo Chrome session. `No Fingers (25)` was
selected; the downloaded skeleton retains 22 useful bones after removal of
three named end bones. Two pauldrons and a weapon bone bring the runtime
count to **25**. Armor vertices have exactly one influence at weight 1; soft
body weights are normalized and limited to four.

Original files are retained under `../shared_infantry/mixamo/`:

| File | Purpose and settings |
| --- | --- |
| `infantry_base_tpose.fbx` | Our custom body, T-pose, FBX with skin |
| `rifle_aim_idle_source.fbx` | Aiming idle, Arm-Space 60, without skin, 30 fps, no key reduction |
| `rifle_idle_source.fbx` | Lowered two-hand rifle idle, Arm-Space 60, without skin, 30 fps, no key reduction |
| `rifle_run_source.fbx` | Rifle run, In Place, Arm-Space 60, without skin, 30 fps, no key reduction |

Arm-Space 60 is a trial setting for this kit, not approved for the full roster.
The uploaded OBJ uses meters, Y up, forward +Z. The authored Blender mesh
uses meters, Z up, forward -Y. FBX scale and forward orientation are normalized
once on the master. The retained source weights are transferred to our body.
Keep the saved master for reuse instead of repeating that normalization.

## Animation/export contract

`tools/art/rig_infantry_study.py` imports both FBX types through Blender's
`wm.fbx_import`. Keep `ignore_leaf_bones=False`, then remove only `*_End`;
otherwise unskinned clips lose real hand bones. Skinned and unskinned FBXs
from this character use different rest normalization, so sampled absolute
poses are baked onto the master before export. No Godot humanoid retargeting
or runtime IK is involved.

Pauldron motion and an analytical support-arm correction are baked at 30 fps.
The left palm follows this bullpup's foreguard. Weapons with different grips
may need their own corrected clips on the **same** skeleton. A shared skeleton
does not mean every weapon must use identical arm poses.

The model is one mesh/material, 3,728 triangles, 25 bones, maximum four
influences. Covered body faces are removed only from the merged copy. Vertex
RGB supplies temporary flat colors; UV0.x carries the team mask because opaque
glTF export drops vertex alpha. The import sidecar assigns the external
`infantry_study.tres` shader. A final atlas is still to be authored.

Godot strips the `_loop` suffix and enables loops:

| Blender action | Godot name | Frames at 30 fps |
| --- | --- | --- |
| `rifle_ready_idle_loop` | `rifle_ready_idle` | 232 |
| `rifle_aim_idle_loop` | `rifle_aim_idle` | 94 |
| `rifle_run_loop` | `rifle_run` | 21 |

Remove Immutable Tracks is disabled for the shared library. Track paths
resolve to `SZ_Infantry/Skeleton3D`; bone colons become underscores in Godot.

Geometry-authoring helpers are `infantry_study_common.py` and
`build_rifleman_study.py`. The saved Blender source includes later fitting and
color edits and is the authoritative editable asset. To verify and regenerate
exports from that source, run `tools/art/finalize_rifleman_study.py` in a
separate Blender **background** process, with the `.blend` path before
`--python`. It writes metrics, the shared master, and both runtime GLBs.
The extraction deliberately removes unrelated scenes only in that disposable
process. Never run the finalizer against the live editor through MCP.

## Validation and remaining work

The Blender finalizer checks a closed base body, normalized weights, rigid
gear, the triangle/material/bone budgets, and support-palm error across every
baked frame. The Godot check verifies model material, skeleton/track binding,
all three loop flags, visible skeletal motion, and captures the real renderer:

```powershell
& .\tools\godot.ps1 -Import
& .\tools\godot.ps1 -GodotArgs @('--script','res://../tools/art/verify_rifleman_study.gd','--resolution','1280x800')
powershell -NoProfile -ExecutionPolicy Bypass -File plugins/stratezone-mission-steward/scripts/validate_stratezone.ps1
```

Manual checks cover sprite comparison, aiming/ready/run poses, front and rear
gear, faction accent switching, and Hub scale. Review the preview PNGs.

Post-recovery checks passed on 2026-10-03: background finalizer; reopening the
shared master (two objects, 25 bones, three actions); `godot.ps1 -Import`;
`verify_rifleman_study.gd`; the full `validate_stratezone.ps1` stack; and
`git diff --check`. Maximum sampled support-palm error is 1.44 mm. LFS
attributes were verified for Blender sources, retained FBXs, and runtime GLBs.
Nearest-source weight transfer has a 0.10 m maximum position difference after
forward normalization; foot contacts and deformation still need visual polish.

Rob's review: the current soldier is super rough and needs a major coat of
polish. Treat the present mesh as a rig/scale blockout, not a visual standard.
Priorities are proportions and anatomy, shaped/segmented armor, cloth volume
and folds, proper camouflage/materials, and better helmet/face/rifle detail.
Then check silhouette and team readability at mission zoom from all
cardinal views, clip seam/foot-contact polish, and clearance on the much
bulkier Guardian. Melee/death/fire/reload clips are not authored. No Steam
Deck crowd profiling or final atlas/LOD acceptance has been done. The isolated
study uses one normal AnimationPlayer; the planned crowd scheduler is a
separate milestone. These assets do not alter simulation units or missions.

Scale remains a proposal: helmet top is 2.225 m, boot sole starts at 0.018 m.
The original Hub's 1.78 m door leaves were too short for an upright equipped
soldier. The subsequent Rover scale pass enlarged the opening to 2.70 m clear
height and 2.62 m width; see `RoverScaleStudy.tscn` for the updated entrance.

Recovery note: the live Blender save/export attempt crashed. The recovered
checkpoint retained the custom body, 120 gear parts and 22-bone Mixamo rig,
but no actions/helpers. That file was inspected and preserved before restoring
the missing fitting/helpers and three actions from the retained downloads.
The custom body and existing gear were not rebuilt. Use normal .blend saves
and the separate background finalizer for subsequent exports.
