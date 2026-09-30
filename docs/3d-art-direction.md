# 3D Art Direction

Status: **working guide** (2026-09-30). It supports `docs/3d-presentation-plan.md`, which owns the pipeline, budgets, and order of work. This doc owns *what the 3D assets should look like* and *how armored characters are built and animated*.

## Rule Zero: The Sprites Are The Design Bible

The 3D assets should look like the 2D units and buildings we already designed. They should match as closely as a real-time 3D model allows: silhouette, gear, proportions, materials, markings, and color.

- The in-game sprites are the reference:
  - units: `game/assets/units/<unit>/directional/`
  - buildings: `game/assets/buildings/<building>/directional/`
  - Each has 8 angles, so every side of every design is already drawn.
- Source turntables and concept files live outside the repo in `C:\Users\Rob\Pictures\stratezone\` (`*360` folders, `.ai` files).
- When a 3D model and its sprite disagree, the sprite wins unless the difference is a deliberate readability fix, written down in this doc.
- The hand-drawn *rendering* (painted shading, bold ink outline) is not a hard requirement. We get as close as cheap real-time techniques allow (see Rendering Look), but the *designs* are the requirement.

## Shared Visual Language

These are observed across the whole sprite set.

- **Palette:** olive-drab armor and fabric, dark gunmetal frames and gear, small cyan glow accents. Towers add pale green-gray concrete. The Commander is the only near-black/brown figure.
- **Shapes:**
  - chunky, beveled, rounded-corner armor panels
  - stepped and tapered masses
  - bolted plates
  - nothing sleek or organic
  - buildings read as modular prefab bunkers
- **Lights:** cyan window bands, slit lights, glowing cells and cores. They are the "this is powered and alive" signal.
- **Markings:**
  - winged faction insignia over doors and on roofs
  - yellow-black hazard stripes
  - yellow warning-triangle plates
  - chevron rank patch on the Commander
- **Silhouette clutter:** antennas, dishes, pipes, crates, and pouches give scale and a "used equipment" feel. Keep them in 3D, but chunky enough to survive at 20–60 px.

### Palette seeds (sampled from the sprites)

These are starting albedo values. Sprite pixels include painted shading and outlines, so 3D base colors run slightly lighter than the darkest sprite pixels. Tune them under the in-game light at Steam Deck size.

| Material | Sprite samples | Starting 3D albedo |
| --- | --- | --- |
| Olive-drab armor/fabric (units) | `#4b4534` `#5f5842` `#7f7960` | `#655e46` |
| Olive-drab vehicle hull | `#4f4836` `#766c53` | `#6a6149` |
| Building armor panel (olive-gray) | `#4b4b3e` `#606355` | `#5c5d4d` |
| Gunmetal frames, gear, weapons | `#1e1e19` `#272a24` `#2e2c22` | `#2c2d28` |
| Tower concrete | `#879d95` (light) | `#8a948c` |
| Commander armor (brown-black) | `#392e25` `#453a31` `#55493e` | `#4a3e34` |
| Glow / emissive (player) | `#46c2cc` `#64cad6` `#1da5ab` | emissive `#46c2cc` |
| Hazard yellow | stripes and triangles | about `#c9a227` (sample precisely during texturing) |

### Team color

Both factions are "same tech", so the olive and gunmetal base stays shared.

- **Team color drives the glow accents plus a few marking panels:** light bands, cores, visor glints, insignia plates, shoulder markings. Player = cyan (as in the sprites). Enemy = red (as in the map mockup).
- **Implementation:** a mask channel times a per-instance shader uniform. One model, no red duplicate.
- **Enemy base tone:** the enemy may also get a slightly cooler or greyer base tint through the same uniform, if playtests show red lights alone don't read at Deck size.

## Rendering Look (getting close to the sprites cheaply)

Test these in the scale-lineup scene (P0), at close, default, and far zoom, on desktop and at 1280×800.

1. **Stylized PBR.**
   - Painted-leaning albedo with baked ambient occlusion and edge highlights. That carries the sprites' "hand-inked" edge wear.
   - Mostly matte roughness.
   - Strong emissive for team-colored lights.
2. **Screen-space outline pass (strongly recommended to A/B).**
   - A post-process edge detector on depth and normals draws dark silhouette and crease lines.
   - It's the single biggest lever for "looks like our sprites", and it helps Deck readability.
   - It's one full-screen pass, so the cost doesn't grow with unit count. That makes it cheaper than inverted-hull outlines for crowds.
3. **Lighting.**
   - A high, bright directional sun plus soft sky ambient, matching the sprites' top-lit look.
   - The sun is fixed in the world, so shadows stay honest when the camera rotates.
   - A/B test: a key light that follows camera yaw (constant shading like the sprites, but shadows swing with rotation).
4. **Bloom:** light bloom on emissives only, so cyan and red cores glow like the sprites without washing out the terrain.

## Scale And Proportions

- **Characters:**
  - The sprites use fairly natural proportions (about 7 heads tall). Keep them.
  - Readability comes from scaling the whole figure to RTS hero scale (about 2.0–2.3 m) and slightly oversizing helmets, weapons, backpacks, and shoulder armor. The body itself is not caricatured.
- **Buildings:**
  - The mesh base matches the sim footprint outline exactly.
  - Flat aprons, ramps, and pads (Barracks apron, Hub ramp, tower dirt pads) are walkable decoration, not footprint. Only walls block.
- **Lock numbers in the scale lineup** before modeling past blockout.

## Per-Asset Design Notes

"Rigid" means the piece is weighted 100% to a single bone inside the unit's one merged mesh. It is not smooth-skinned, and not a separately bone-parented object. See Armored Characters below for why.

### Infantry (one shared base body and skeleton)

- **Grunt** (combat engineer / technician). This must read as a *non-combatant worker*.
  - **Design:**
    - olive fatigues with dark armor pieces
    - helmet with side antenna and cyan goggles
    - an oversized equipment backpack with glowing cyan cells and an orange emblem patch
    - a cable to a handheld cyan-tipped tool (welder or cutter)
    - no rifle
  - **Silhouette:** slightly hunched under the big pack.
  - **3D:** pack and tool are rigid (spine and hand bones). The cable is a static curve or a 2-bone chain, with no physics.
- **Cadet** (cheapest, weakest).
  - **Design:**
    - plain helmet with blue goggles
    - fatigues with a light chest rig and knee pads
    - a compact sidearm or SMG held low
  - **Silhouette:** the slimmest, with the least armor.
  - **3D:** mostly the base body with the uniform; few rigid pieces.
- **Rifleman** (baseline infantry).
  - **Design:**
    - helmet and goggles
    - a plate carrier with pouches
    - shoulder pads and gloves
    - a bullpup assault rifle at the ready
    - tan/olive two-tone
  - **Silhouette:** bulkier than the Cadet, with a long rifle line.
  - **3D:** plate carrier and pouches are rigid on the chest; shoulder pads need a helper-bone strategy (see Armored Characters).
- **Guardian** (heavy anti-armor, energy weapon).
  - **Design:**
    - powered armor: an enclosed helmet with visor slit
    - large rounded pauldrons and an armored chest and limbs
    - a big power backpack with a hose to a heavy energy weapon with glowing cells, carried two-handed at the hip
  - **Silhouette:** the widest, top-heavy.
  - **3D:** the most armor pieces. This is the stress test for the armor rig and animation approach.
- **Commander** (fragile, fail-condition unit).
  - **Design:**
    - a dark brown-black armored suit
    - an enclosed helmet with visor
    - a chevron rank patch
    - an athletic, slimmer build
    - a pistol
  - **Silhouette:** the dark palette makes him read instantly as "the one to protect". Keep that contrast.

### Vehicles (rigid part hierarchies, no skeleton)

- **Rover:**
  - an open-top 4×4 buggy with roll cage and visible driver
  - a roof remote turret, antennas, cargo crates, cyan headlights, big off-road wheels
  - Parts: chassis, 4 wheels (spin, front steer), turret yaw.
- **Medium Tank:**
  - a modern olive main battle tank with a long gun, side skirts, antennas, and a remote weapon station
  - Parts: hull, tracks (UV scroll), turret yaw, barrel recoil.
- **Heavy Tank:** a bulkier hull and turret, a heavier gun, and more armor blocks. Same part scheme.

### Buildings (modular kit on a shared trim sheet)

- **Colony Hub:**
  - a two-tier stepped square bunker with a wraparound cyan window band
  - a roof dish and antennas
  - a front ramp and door under the winged insignia
  - The door is the unit `exit_point`.
- **Barracks:**
  - a long rounded-roof armored hall with an end door and ramp, and antennas
  - a front apron pad with bollards (walkable, not footprint)
- **Power Plant:**
  - a blocky base with twin tall glass reactor cylinders glowing cyan, pipes, steam vents, and a hazard panel
  - Unpowered or destroyed: the cylinders go dark and the steam stops.
- **Pylon:** a tall tapered obelisk with a cyan light tip, on a small dirt pad.
- **Extractor / Refinery:**
  - a square armored housing around a circular glowing well cap, with pipes and tanks
  - Unpowered: the ring dims.
- **Defense Tower:**
  - a tapered square tower with a concrete-gray core, olive/dark armor corners, and cyan slit lights
  - a flat top that emits the energy wall
- **Gun Tower / Rocket Tower:** the Defense Tower body plus a gun turret or twin rocket-pod module on top. This literally matches the sprites and the in-place upgrade rule.
- **Vehicle Bay:** a garage hangar with a rolling door, a ramp, an overhead crane arm, and the insignia.
- **Med Hall, Logistics / Repair Pad:** sprite designs exist; build them only if they re-enter the demo scope.
- **Shared kit parts:**
  - armor panels (flat, beveled, curved)
  - corner blocks and frames
  - window-light bands and slit lights
  - vents, pipes and elbows
  - antennas and dishes
  - hazard-stripe trims, warning plates, the insignia decal
  - doors and ramps

## Armored Characters: Rigging And Animation

Research pass on 2026-09-30, checked against the Godot 4.6 docs and source, and Blender 5.x release notes. Tool versions and free tiers change, so recheck before relying on anything marked *(unverified)*.

### The approach in one paragraph

Model one unarmored base body and let Mixamo's auto-rigger rig it once. That skeleton becomes the *only* game skeleton, and every clip (Mixamo or custom) is baked onto it in Blender. Armor plates are **rigid-weighted**: 100% to one bone, inside a single merged mesh. Smooth weights are used only on the soft undersuit at joints. A few helper bones handle plates that span a joint (pauldrons, maybe thigh plates); their motion is baked into every clip, so Godot needs no runtime rig logic. Godot imports one GLB per unit plus one shared animation GLB as an `AnimationLibrary`.

### Rigging the armor

- **Rigid skinning, one mesh.**
  - Weight each plate 100% to the bone it rides on.
  - Use soft weights only on the undersuit: waist, neck, armpits, inner elbows and knees.
  - Don't ship bone-*parented* armor objects. Godot imports each one as its own `BoneAttachment3D` + `MeshInstance3D`, which means extra nodes and draw calls per unit. Bone parenting is fine for quick prototypes; convert to weights and join before export.
- **Helper bones only where a plate spans a joint.** Target 2–4 helpers, 28 bones or fewer in total.
  - **Pauldrons:** a pad bone parented to the clavicle that follows the upper arm part-way. Options: Damped Track toward a target bone under the upper arm, or Copy Rotation from the upper arm at about 0.3–0.5 influence.
  - **Thigh plates:** 100% to the thigh (a small hip gap is fine), or a hip-child helper copying about 50% of the thigh's rotation.
  - **Helmet, chest plate, backpack, weapon:** no helper. Weight 100% to Head / Spine2 / Hand; add a dedicated `weapon` bone if weapon swaps need it.
  - **No twist bones.** A hard cuff or gauntlet seam at the wrist hides forearm roll.
- **Design armor so it animates well.** Modelers own this, and it saves the most rigging pain.
  - Overlap plates like shingles at joints, upper over lower, so they slide under instead of through.
  - Make elbow and knee caps rounded, centered on the joint's pivot.
  - Leave a dark undersuit band between the chest and pelvis plates; the spine bends inside that gap.
  - **Cut chest armor away under the armpits** and put the bulk on the front, back, shoulders, and pack. This is the biggest single fix for arms clipping through the torso.
  - Delete body faces hidden under armor.
- **What matters at game size.** At 20–60 px, a few pixels of clipping are invisible. Spend effort on silhouette, pose clarity, and color separation, not on perfect deformation.

### Tools (Blender 5.1)

- **Rig:** the Mixamo auto-rigger.
  - Upload the unarmored base body: T-pose, one closed mesh, no props. Bulky clothing or extra parts can make it fail.
  - Use Skeleton LOD **"No Fingers (25)"**.
  - Clips downloaded *for that uploaded character* already fit the skeleton, so there's no retargeting.
  - Mixamo is free with an Adobe ID but no longer actively updated *(indirect)*. Download everything we need in batched sessions.
- **Control rig for custom clips:** the **Mixamo Rig** extension. It gives IK/FK limbs on top of the Mixamo skeleton and bakes back to it (v1.2.x supports Blender 4.2 up to below 5.5).
- **Weights:** rigid plates get their 100% weights by script. For parts that hug the undersuit, use the Data Transfer modifier (Vertex Groups, Nearest Face Interpolated) or the **Robust Weight Transfer** add-on (tested through 5.1; needs SciPy/libigl).
- **Retargeting clips from *other* skeletons** (asset packs, mocap exports): the free **Retarget** extension (Blender 5.0+, has Mixamo presets). The paid fallback is Auto-Rig Pro's Remap.
  - Avoid Rokoko's add-on: its retargeting broke on Blender 5.0's action API.
  - Avoid Rigify for the game skeleton: its deform-bone exports are messy for a 25-bone game rig.
- **FBX import:** Blender 5.0 made the new ufbx importer the default (`bpy.ops.wm.fbx_import`). The legacy importer still has Automatic Bone Orientation. Use the *same* importer for the body and every clip, and delete the `*_End` leaf bones by script.

### Getting Mixamo clips right on bulky armor

Cheapest fix first:

1. **Design clearance in** (armpit cutaways, see above).
2. **Use Mixamo's Character Arm-Space slider** when downloading each clip; it spreads the arms. Pick one value that fits the bulkiest unit (the Guardian), record it, and reuse it for every clip.
3. **NLA correction layer.** Add an NLA track with Blend = Combine above each clip, key a small upper-arm abduction on it, then bake. This can be scripted across all clips.
4. **Retarget offsets** (Auto-Rig Pro Remap), if still needed.
5. **Last resort:** a separate "heavy" animation library for the Guardian only.

A bpy clearance check can sample frames of every clip and test arm plates against the chest, so we fix only the clips that actually clip.

### Authoring custom clips

The Grunt tool loop, Guardian energy-weapon fire, crushed death, and anything else Mixamo lacks.

- **Rig use:**
  - IK legs wherever feet stay planted.
  - FK arms for swings.
  - IK arms only where hands must hit a point (tool contact, two-handed weapon).
- **Pose-to-pose:**
  - Block key poses with stepped interpolation.
  - Judge them from the real game camera (orthographic, 45°, zoomed out to Deck size), then spline.
  - For loops, match the first and last poses and use a Cycles modifier while working, then bake within the frame range.
- **Weight (Guardian):**
  - wide stance, lower hips
  - slow anticipation, fast release, long settle
  - recoil driven down into the hips
  - pauldrons and pack lag 1–2 frames behind the body
  - Exaggerate about 1.5× because the figure is tiny on screen.
- **Grunt tool loop:** 3–4 poses, about 1 second, a big arc on the tool, torso leading the hand by one frame.
- **Crushed death:** a 6–10 frame collapse. Do the flattening as a node-scale tween in Godot, not bone scale, because non-uniform bone scale shears.
- **Video-to-motion tools:**
  - Options for longer or organic motions:
    - DeepMotion Animate 3D
    - Rokoko Vision
    - QuickMagic, which can export a Mixamo-named skeleton
    - FreeMoCap: free and open source, 2+ webcams
    - Cascadeur: the Indie tier exports FBX/glTF; the free tier is non-commercial
  - **Never ship clips made on a non-commercial tier.**
  - For 10–30 frame game loops, hand keyframing is usually faster than cleaning up mocap.

### Blender → Godot export

- **Two-file pattern:**
  - **One GLB per unit:** armature plus merged mesh, no animation.
  - **One shared `anims.glb`:** armature plus all actions, imported in Godot with Import mode = **Animation Library**. Each unit's `AnimationPlayer` uses that library.
  - The armature object name must be identical in every export, so animation track paths match.
- **Exporter settings:**
  - Export Deformation Bones Only: on. Helper bones must be marked Deform.
  - Use Rest Position Armature: on.
  - Sampling: on, 30 fps.
  - Bone influences: 4.
  - Animation Mode = Actions (only active or NLA-stashed actions export), with "Reset pose bones between actions" on.
  - Exporter option names can drift between Blender versions, so lock them in the export script.
- **Clip naming:** clip names ending in `loop` (for example `rifle_run_loop`) import with looping on. The script also writes a manifest of clip names, lengths, and loop flags.
- **Godot import settings:**
  - Animation FPS 30.
  - Skins: 4 influences.
  - **No BoneMap / Rest Fixer for our own skeleton.**
  - Watch that "Remove Immutable Tracks" doesn't drop helper-bone tracks.
  - Bone names import with `:` turned into `_` (`mixamorig_Hips`).
- **Godot's humanoid retargeting is optional, not the default.** Clips retargeted through `SkeletonProfileHumanoid` leave unmapped helper bones frozen, so we bake everything onto our skeleton in Blender instead. Keep retargeting (or `RetargetModifier3D`, 4.4+) in reserve for third-party Godot animation libraries only.
- **Runtime helper-bone drivers are a fallback, not the plan.** Godot 4.5+ has `AimModifier3D`, `CopyTransformModifier3D`, and `ConvertTransformModifier3D`. They could drive pauldrons live, but they cost CPU per unit per frame. Baking is preferred.
- **Attachment points:** mesh-less `BoneAttachment3D` markers for muzzle flash and tool VFX.

### Performance note that shapes the crowd spike

- In Forward+, skinning runs on the GPU and is cheap at 2–4k triangles.
- **CPU-side animation evaluation is the bottleneck.** Godot issue reports put it at:
  - about 100 animated characters at 60 fps in one 4.3–4.4 case
  - about 1k units at around 100 fps on a desktop Ryzen 7 in another
- The Steam Deck CPU is much weaker than either machine.
- So:
  - Use an `AnimationPlayer` per unit, not an `AnimationTree`.
  - Use `callback_mode_process = Manual`, with one C# scheduler calling `advance()` at tiered rates by zoom and visibility.
  - Offscreen units don't animate.
  - Infantry get no runtime `SkeletonModifier3D` nodes.
  - Treat the vertex-animation-texture fallback as a *likely* need for the 300-infantry stress bar on the Deck, not a remote one. The P0 crowd spike decides.

### Pipeline steps (who does what)

| # | Step | Owner |
| --- | --- | --- |
| 1 | Model the base body in T-pose (about 1–1.5k tris): clean loops at shoulders, elbows, knees, and hips, origin at the feet | hands-on |
| 2 | Mixamo auto-rig: "No Fingers (25)", download FBX with skin | hands-on (Rob, Adobe ID) |
| 3 | Download the clip list for that character: without skin, In Place, fixed Arm-Space value | hands-on (Rob) |
| 4 | Batch-import clips, rename actions to the naming scheme, move them onto the master armature | bpy |
| 5 | Clean the skeleton: delete `*_End` bones, add helper bones (`pauldron.L/R`, `weapon`), bone count check | bpy |
| 6 | Model each unit's gear kit inside a shared clearance envelope, following its sprite design | hands-on |
| 7 | Rigid-weight plates, transfer undersuit weights, join into one mesh, delete hidden faces, 4-influence limit, triangle check | bpy |
| 8 | Fix shoulder and hip weights against 3–4 key clips | hands-on |
| 9 | Clearance check across all clips, NLA Combine corrections, bake with visual keying (including helpers) | bpy |
| 10 | Author custom clips on the Mixamo Rig control rig, then bake | hands-on (Claude can script simple mechanical loops) |
| 11 | Export per-unit GLBs plus `anims.glb`, and write the clip manifest | bpy |
| 12 | Godot: import the `AnimationLibrary`, build unit scenes, add VFX markers | Claude (C#/scene setup, `EditorScenePostImport` script) |
| 13 | Readability check at 1280×800, profile 100+ units on the Deck | hands-on + Claude |

Pipeline scripts live in `tools/` and run headless (`blender --background --python ...`), per `docs/engineering-standards.md`.

### Research sources (key)

- Godot 4.6 retargeting: https://docs.godotengine.org/en/4.6/tutorials/assets_pipeline/retargeting_3d_skeletons.html
- Godot import configuration and animation libraries: https://docs.godotengine.org/en/4.6/tutorials/assets_pipeline/importing_3d_scenes/import_configuration.html
- Godot 4.5 bone constraints: https://godotengine.org/releases/4.5/
- Godot 4.6 IK nodes: https://godotengine.org/article/inverse-kinematics-returns-to-godot-4-6/
- Godot 3D performance: https://docs.godotengine.org/en/4.6/tutorials/performance/optimizing_3d_performance.html
- Animation CPU cost reports: https://github.com/godotengine/godot/issues/101494 and https://github.com/godotengine/godot/issues/99194
- Bone-parented mesh import behavior: https://github.com/godotengine/godot/issues/81601
- Blender glTF exporter docs: https://raw.githubusercontent.com/KhronosGroup/glTF-Blender-IO/main/docs/blender_docs/scene_gltf2.rst
- Blender 5.0 FBX importer change: https://developer.blender.org/docs/release_notes/5.0/pipeline_io/
- Mixamo Rig extension: https://extensions.blender.org/add-ons/mixamo-rig/versions/
- Retarget extension: https://extensions.blender.org/add-ons/retarget/
- Robust Weight Transfer: https://github.com/sentfromspacevr/robust-weight-transfer/releases
- Rokoko retarget breakage on Blender 5: https://github.com/Rokoko/rokoko-studio-live-blender/issues/135
- Shoulder-armor helper rig technique (Polycount): https://polycount.com/discussion/200175/rigging-a-spaulder-an-easy-way-a-simple-way-to-rig-shoulder-armor

## Readability Checklist (every asset, before it's "done")

- It's recognizable at 1280×800 at far zoom, from all 4 cardinal camera yaws.
- Team color reads for both factions.
- The unit type is identifiable by silhouette alone (test with a flat black silhouette render).
- The Grunt never reads as armed; the Commander never blends into the infantry.
- It's within the budget in `docs/3d-presentation-plan.md` (triangles, texture size, draw calls).
- It sits side by side with its sprite in a comparison render, and differences are deliberate.
