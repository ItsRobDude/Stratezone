# 3D Presentation Overhaul Plan

Status: **accepted; all blocking decisions locked** (2026-09-30).

Rob has decided to move presentation to 3D, with custom characters and real animation. The work is split into three tracks:
- **Track A:** fix building footprints and pathfinding. This is needed regardless of 3D.
- **Track B:** overhaul controls and hotkeys to real-RTS conventions.
- **Track C:** the 3D presentation itself.
- **Track D:** playable elevation (smooth hills that scale weapon range and sight), debuting in Level 2.

The pivot-rule docs pass is done (2026-09-30): `AGENTS.md`, `README.md`, `docs/technical-architecture.md`, `docs/scaffold-plan.md`, and the related lines in the other docs now describe the 3D direction.

What the assets should look like, and how armored characters are rigged and animated, lives in `docs/3d-art-direction.md`. The existing sprites are the design bible.

## Locked Decisions (Rob, 2026-09-30)

- **3D presentation** with real animation. The simulation stays 2D and Godot-free.
- **Camera:** fixed 45° pitch, **orthographic** projection, **free rotation** on middle-mouse drag, `Home` resets to north, wheel zooms.
- **Hardware:** the Steam Deck is the weakest target and sets the performance floor. The desktops (Radeon RX 6900-class, RTX 5090) are not a constraint.
- **Characters are built in full by us.** No stock characters ship. Only *some* animations come from Mixamo. Custom animations are expected for anything Mixamo lacks.
- **Hotkeys get a full overhaul** modeled on real RTS conventions. The current bindings are not preserved.
- **Building footprint and pathing problems are fixed as their own workstream.** Buildings look roughly the right size today; the invisible blocking shapes are the problem.
- **Footprints follow each building's real shape plus a small buffer.** They are not generic circles or squares: an L-shaped building blocks an L, and a round silo blocks a circle.
- **Buildings are physical objects made of walls and a roof.** They block movement exactly where they stand. Players (and the enemy) can wall off routes with buildings, or trap themselves with a bad layout. There is no guaranteed walking lane. The old rule, "spacing buffer to prevent over-cramming", is retired.
- **Grid hotkeys.** Command-bar keys match button positions (`Q W E R T` / `A S D F G` / `Z X C V B`), like the StarCraft II and Age of Empires IV grid presets.
- **New unit commands:** Stop, Hold Position, Attack-move, and Shift-queued orders.
  - Rally points are not wanted for now.
  - Patrol may come later.
- **Playable elevation: smooth hills** (Company of Heroes / Total Annihilation style), not discrete cliff levels. See Track D.
  - Units walk anywhere that isn't too steep.
  - **Height difference scales weapon range both ways.**
    - Shooting downhill reaches farther and shooting uphill reaches shorter, scaled by the height difference.
    - Past a big climb, targets above can't be hit until the shooter is almost on top of them.
  - **Height difference scales sight (exploration) the same way.**
    - Ridges reveal farther.
    - Looking uphill reveals less, so plateau tops stay unexplored, and enemies up there stay hidden, until scouted.
  - There is no uphill damage penalty; range carries the rule.
  - **Hill crests blocking shots (line-of-fire cover) is deferred** as a possible upgrade if Level 2 playtests ask for it.
  - **Debuts in Level 2** (Wells at the Ridge).

## Open Decisions

Values to tune during implementation:
- footprint buffer size
- unit `collision_radius`
- zoom range
- hero scale
- elevation numbers (maximum walkable slope, range/sight scaling, hard-block height; starting proposals in Track D)

Deferred on purpose (possible upgrade after Level 2 playtests):
- **Crest cover:** hill crests blocking shots between shooter and target.
  - It is cheap to compute at our short ranges (a few height-grid samples per shot).
  - The real cost is unit behavior: repositioning until a clear shot exists. It also needs a player-facing "no line of fire" cue.

## Goal

The game presents the same simulation as:
- animated custom infantry
- rigid-part vehicles
- modular 3D buildings
- terrain generated from map data

It uses an orthographic 45° camera that rotates freely, stays readable, and holds frame rate on the Steam Deck with a full battle on screen.

The durable rule does not change: *RimWorld for colony stakes, Dominion/C&C-style RTS for battlefield control.*

## What Stays The Same

- **The simulation rules don't change for 3D.** Nothing under `game/scripts/simulation/` references Godot. It works on a flat plane (`SimVector2`, pixels, +Y = south). Track A changes sim pathing and placement because they're broken, not because of 3D.
- **Terrain art never becomes gameplay truth.** The 3D terrain is generated *from* map data.
- **The HUD stays a `CanvasLayer` Control tree** over the 3D viewport.
- **The F5 map editor stays a 2D top-down data tool**, with a "preview in 3D" toggle later.

## Current State (facts the plan is built on)

- **Scene:** `Main` is a Node2D with `WorldRoot` (Node2D) and `UiRoot` (CanvasLayer). Views are layered by ZIndex.
- **Camera:** Camera2D with WASD/arrow pan and wheel zoom 0.55–1.8. No rotation.
- **Units:** Sprite2D with 8 directional frames. Animation state is guessed from position deltas and timers. There are no health bars and no death animations.
- **Buildings:** Sprite2D (south frame only). The placement ghost is a square, while the sim uses circles.
- **Map:** `_Draw` rects and circles. Level 1 has no terrain regions.
- **Fog:** 64 px cells.
- **Picking:** 2D canvas only (`GetGlobalMousePosition`, world-space box select).
- **Scale:** 1 content unit = 24 px.
  - Level 1 is about 102×61 units.
  - Level 2 is about 142×92 units.
- **Renderer:** Godot 4.7 .NET, Forward+.

## Track A: Footprints And Pathfinding

### Diagnosis

**1. New troops spawn inside the Colony Hub and walk through buildings.**
- Units spawn at `hub.Position + (70 + 34 × row, …)` px (`RtsSimulation.Production.cs:299-307`).
- The Hub footprint radius is 96 px, so the first six spawns are *inside* it.
- Pathfinding then ignores every building within footprint + clearance of a unit's start point **for the entire path** (`PathfindingSystem.cs:205-209, 304`). So fresh troops, Grunts repairing, and defenders parked next to buildings path straight through them.

**2. Blocking shapes don't match the buildings, so visible gaps aren't real gaps.**
- Every building blocks a circle (`FootprintWorldRadius` + 18 px clearance), whatever its shape.
- Placement spacing uses a different, larger circle (footprint + buffer on both buildings, `RtsSimulation.cs:193`).
- Pathing runs on a coarse 32 px grid.
- Result: two Barracks at minimum spacing *look* like they have a gap, but only a 12 px band is left open. That is narrower than one grid cell, so the grid sees a solid wall.
- The reverse also happens: a box-shaped building's corners stick out past its circle, and units walk through them.

**3. Staircase paths.**
- The 8-direction grid path is "smoothed" only by removing collinear points (`PathfindingSystem.cs:124-149`), so units zigzag across open ground.

**4. Units stack and ghost through each other.**
- Movement is a straight line between waypoints, with no unit-to-unit separation (`RtsSimulation.Movement.cs:101-104`).

**5. Pathing cost explodes with troop count.**
- Every `FindPath` builds a fresh grid and tests each cell against every building, wall, and region with LINQ (`PathfindingSystem.cs:291-306`).
- An unreachable target tries up to about 168 fallback cells, each a full A* search (`:23-31, 226-260`).
- A blocked unit re-requests a path every tick (`RtsSimulation.Movement.cs:136-139`).
- Chasers repath every 32 px of target movement.

**6. What you see is not what blocks.**
- The 2D sprite extends above the collision circle.
- The placement ghost is a square (`PlacementGhost.cs`).
- The building click area merges sprite bounds with the footprint (`GreyboxBuilding.cs:255-273`).

**7. Game logic runs once per rendered frame with a variable time step.**
- `Main._Process` calls `_simulation.Tick(deltaSeconds)` every frame (`Main.cs:130`).
- A 144 Hz desktop runs the whole simulation 144 times a second: pathing, combat, AI, power, fog. A Deck at 40 fps runs it 40 times.
- Results can differ with frame rate, and every per-tick cost above is multiplied by the display's refresh rate.

**8. Every unit plans its own path, even when a whole group gets one order.**
- A 20-unit move order runs 20 independent searches toward 20 formation spots, with 20 repaths when things change.
- The classic RTS answer is one search per group, with members following offsets. Company of Heroes and Dawn of War pathfind per squad; Supreme Commander 2 used shared flow fields.

### Fix plan

The rule this track delivers: **buildings block exactly where their walls are.** If a soldier visibly fits through a gap, it walks through. If the gap is too tight, it's a wall, and that's the player's layout choice.

- **A0. Fixed-rate simulation with smooth rendering. Do this first; everything else in Track A is cheaper on top of it.**
  - **Sim-owned step length.** The sim owns one step length, proposed 20 steps per second (`TickSeconds = 0.05`). This follows the classic RTS pattern: StarCraft II's game logic runs about 16 times a second on Normal speed, Supreme Commander about 10.
  - **Two sim methods:**
    - `Step()` runs exactly one fixed step.
    - `Advance(seconds)` runs as many whole steps as fit and carries the remainder.
  - **`Main` accumulates frame time and calls `Step()`.** It caps catch-up at a few steps per frame, so a hitch can't spiral into a freeze.
  - **Player commands** are queued and applied at the next step boundary, never mid-step.
  - **Presentation interpolates.** Views keep the previous and current step's position and facing, and draw between them by `accumulator / TickSeconds`. Movement stays smooth at any frame rate while game logic runs at 20 Hz.
  - **Result:** the same inputs give the same outcome on a 144 Hz desktop and a 40 Hz Deck. Sim CPU cost no longer scales with refresh rate. Smoke tests can assert exact step-by-step behavior.
  - **Test impact:** smoke tests currently call `Tick` with 0.1–1000 s jumps. They move to `Advance(seconds)`. Watch smoke-test runtime, since a 1000 s jump becomes 20,000 steps; this is fine once A2's cached grid lands.
  - Update the "Game Loop Direction" section of `docs/technical-architecture.md` to match.
- **A1. Shape-accurate footprints.**
  - Each building's footprint is its real ground outline: one or more convex polygons, plus circles for round parts such as silos and tower bases. Coordinates are content units relative to the building origin, with fixed orientation.
  - It lives in `buildings.json`, so content data stays the sim's source of truth.
  - A small uniform placement buffer (proposed 0.25 content units, about 6 px) keeps models from touching or clipping. It is not a spacing rule.
  - **Sources:**
    - Until 3D models exist, outlines are hand-authored to match the current building art.
    - Once models exist, the Blender export script takes the model's ground outline (a slice just above ground, simplified and split into convex parts) and writes it into content data.
    - A validator flags drift between a model and its footprint.
  - **Everything uses the same shape:**
    - placement overlap
    - pathing
    - the placement ghost
    - click-picking
    - **weapon and repair range to buildings, measured to the nearest point on the outline.** Today these use the center plus a radius (`RtsSimulation.Combat.cs:480, 525, 679-696`, `RtsSimulation.Repair.cs:154`, `CombatResolver.cs:221`).
  - This closes the open footprint/buffer item in `docs/product-roadmap.md`.
- **A2. "Fits if it looks like it fits".**
  - **Cached obstacle grid:** building shapes, terrain blockers, energy walls, and broken bridges are drawn into a cached 8 px grid (1/3 content unit).
  - **Clearance field:** from that grid, compute a distance-to-nearest-obstacle field. A cell is passable for a unit when that distance is at least the unit's radius. Visible gaps and real gaps then agree to within about 4 px.
  - **Per-unit radius:** units get a `collision_radius` in content data. Proposed starting values:
    - infantry about 0.6
    - Rover about 1.1
    - tanks about 1.5
  - So a gap that lets a Rifleman through can still stop a tank.
  - The grid and field update locally when a building, wall, or bridge changes (a revision counter, like the power dirty flag), not on every path request.
  - **Navmesh fallback:** if the grid proves too slow or imprecise with many building shapes, the fallback is a navigation mesh, for example DotRecast (a pure C#, zlib-licensed library). That is a new dependency and needs approval first.
- **A3. Spawn from the door.**
  - Spawning buildings (the Colony Hub) get an authored `exit_point` just outside their footprint. Units spawn there and step clear of the door.
  - If a bad layout walls the exit, units spawn at the nearest free cell. If that's inside a walled pocket, they're stuck until the player opens it. That is intended.
  - The whole-path "ignore buildings near my start" exemption is removed. A unit that starts inside blocked space paths out from its nearest free cell.
- **A4. Reachability.**
  - Connected-region labels (per unit-radius class) answer "can I get there?" instantly.
  - When a target is walled off, the unit moves to the nearest reachable point with one search. Today that can mean about 168 full searches.
- **A5. Straight paths.** Line-of-sight string-pulling over the grid path, using the clearance field, so open-ground moves are 1–2 straight segments.
- **A6. Separation.** A spatial hash plus separation steering, so units don't stack. Group arrivals spread using the existing formation offsets.
- **A6b. Group moves path once.**
  - A move or attack order for several units runs **one search per radius class** (infantry together, vehicles together) from the group's center to the destination.
  - Each member follows the shared path plus its formation offset, with separation steering (A6) handling the crowding.
  - A member re-paths on its own only when it's blocked or has strayed too far from the shared route, for example after being split by a wall.
  - Chase and attack groups share the path to the target area too, then break into individual approach points near the target.
  - **Flow fields** are the next step if large groups still stutter on the Deck: one "which way to go" grid per destination, shared by every unit headed there, as Supreme Commander 2 did. They're listed as an open architecture option in `docs/technical-architecture.md` and are not built unless profiling asks for them.
- **A7. Request budget.**
  - Blocked units retry on a timer or when the nav revision changes.
  - Chase repaths run on a timer.
  - A per-tick cap limits path searches.
- **A8. AI and building walls.**
  - **Placement:** the enemy AI rejects a placement that would disconnect its own Hub exit from its internal AI rally position. The player is allowed to wall themselves in; the AI shouldn't do it by accident.
  - **Attack:** when an attack target is unreachable because of *building* walls, attackers pick the nearest blocking building on their route and destroy it. This extends today's energy-wall-blocked handling (`RtsSimulation.Combat.cs:529-560`), which only checks energy walls along a straight line.
- **A9. Tests** in `tests/SimulationSmoke`:
  - A soldier passes a gap exactly one soldier wide but a tank does not.
  - A sealed ring of buildings blocks everything.
  - No spawn lands inside a footprint.
  - A walled-off target resolves with a bounded search count.
  - Open-field paths have at most 2 waypoints.
  - Range to an L-shaped building is measured to its nearest wall.
  - The AI never walls off its own Hub exit.
  - 100 units re-pathing in one tick stays within a set time budget.
  - The same command script gives identical unit positions when run as 20 separate `Step()` calls or as one `Advance(1.0)`.
  - A 20-unit group move runs at most 2 path searches (one per radius class), not 20.

Track A is pure sim plus content data plus tests. It can start immediately, and the current 2D view keeps working: its ghost and outline switch to drawing the real footprint shapes. Contract changes go into `docs/system-contracts.md`. Schema changes go into `docs/content-data-spec.md`: footprint shape, `exit_point`, and `collision_radius`.

## Track B: Controls And Hotkeys Overhaul

### Principles

These follow StarCraft II, Age of Empires IV, and C&C Remastered conventions.

- **The number row is control groups only.** Today 1–6 are build keys whose meaning shifts per mission, because unavailable buildings are filtered out of the list.
- **A context command card with fixed slots.** A slot never shifts when something is unavailable; it greys out. Buttons show their key.
- **F-keys are player navigation.** Developer tools move to `Ctrl+Shift` in dev builds only.
- **Every binding is a named Godot input action** defined in one keymap file. Buttons read their labels from it, which enables a rebinding UI later (Steam players expect one) and a shipped Steam Input config for the Deck.
- **WASD camera pan is removed.** It collides with command keys.

### Command-card layout: grid (locked)

When you select something, the bottom command bar shows its buttons: train Cadet, upgrade tower, and so on.

- **Layout.** The buttons sit in a 3-row block that mirrors the left side of the keyboard: `Q W E R T` on top, `A S D F G` in the middle, `Z X C V B` on the bottom.
- **Keys follow position, not name.** The top-left button is always `Q` and the one below it is always `A`, whatever the button does.
- **Why:**
  - Every card works the same way.
  - No two buttons share a key. Today `G` means both Guardian and Gun Tower, and `W` trains a Grunt *and* pans the camera.
  - Translation can't break it.
  - It maps cleanly to a Steam Deck layout.
- **Universal habits stay:** `A` = attack-move, number row = control groups, `Esc` = cancel.

### Keymap

**Mouse**

| Input | Action |
| --- | --- |
| Left click / drag | Select / box select |
| Shift + click or drag | Add to or remove from selection |
| Ctrl + click, or double-click | Select all of that type on screen |
| Right click | Smart command: move, attack, repair, or bridge (existing behavior) |
| Shift + right click | Queue the command (new) |
| Middle drag | Rotate camera |
| Wheel | Zoom |
| Screen edge | Pan |

**Camera and navigation**

| Key | Action |
| --- | --- |
| Arrow keys | Pan |
| `Home` | Reset rotation to north |
| `Space` | Jump to last alert |
| `Backspace` | Center on Colony Hub |
| `F1` | Select next idle Grunt (double-tap centers) |
| `F2` | Select all combat units |
| `F3` | Select Commander (double-tap centers) |

**Control groups**

| Key | Action |
| --- | --- |
| `Ctrl + 1–0` | Set group |
| `Shift + 1–0` | Add selection to group |
| `1–0` | Recall group; double-tap centers the camera |

**Command cards**

| Selection | Q | W | E | R | T | A |
| --- | --- | --- | --- | --- | --- | --- |
| Units | Move | Stop | Hold | (Patrol, later) | | Attack-move |
| Barracks | Grunt | Cadet | Rifleman | Guardian | Rover (with Vehicle Bay) | Guardian retrofit |
| Defense Tower | Gun Tower upgrade | Rocket Tower upgrade | | | | |
| Build card | Colony Hub | Power Plant | Pylon | Barracks | | Extractor |

- **Units card:** the S/D slots hold context actions, such as Grunt Repair.
- **Barracks card:** Shift + key queues 5.
- **Build card:** S = Defense Tower; D = Vehicle Bay (hidden until unlocked, slot reserved). `B` opens the build card from anything; with nothing selected it is the default card. Shift + place keeps placing.

**System**

| Key | Action |
| --- | --- |
| `Esc` | Cancel mode, then deselect, then pause menu |
| `F10` | Game menu |
| `Pause` | Pause |
| `O` | Objectives / briefing (was F1) |

- UI scale moves into an options menu (it was F8–F11).
- Dev-only, under `Ctrl+Shift`: restart (F4), map editor (F5), next mission (F6), commander debug (F7), quit (F12).
- Map editor keys stay modal inside the editor.

**Steam Deck.** Ship a Steam Input layout:
- right trackpad = mouse
- R2 = left click, L2 = right click
- L1/R1 = Shift/Ctrl
- D-pad or left stick = pan
- back grips = control groups
- a touch or radial menu for the grid slots

The grid layout is what makes this mapping clean.

**Sim work implied:** Stop, Hold Position, Attack-move, and the Shift command queue. Control groups, idle-Grunt and army selection, and camera jumps are presentation-only.

## Track C: 3D Presentation

### World scale

- **1 content unit = 1 Godot unit ("1 m").** The sim maps as `(x / 24, 0, y / 24)`. Sim +Y maps to Godot +Z, so the default yaw looks north like today's screen.
- **RTS "hero scale".** Ranges are short (a Rifleman shoots 6 m; a Barracks is 4 m wide), so infantry are modeled at about 2.0–2.3 m and vehicles at 3–5 m. A building mesh base matches its footprint exactly (Track A1). Upper structure may overhang slightly.
- **Lock with a scale-lineup scene** before modeling: Hub, Barracks, Pylon, Defense Tower, Rifleman, Rover, and Medium Tank at sim footprints. View it through the gameplay camera at close, default, and far zoom, and also at Steam Deck resolution (1280×800).

### Camera

- **Setup:** orthographic `Camera3D`, 45° pitch.
- **Zoom:** matches today's framing, about 30 m (close) to about 95 m (far) of ground across the screen.
- **Controls:**
  - Free yaw on middle-drag, with `Home` to reset to north.
  - Pan with arrows, the screen edge, and the minimap.
  - The focus point is clamped to `playable_bounds`.
- **Minimap:** moves earlier than "later", because rotation makes orientation loss real. It shows a rotated view trapezoid and supports click-to-pan.

### Gameplay ground height

- **The sim owns ground height** (Track D). Walkable ground is a smooth height field; maps without hills are simply height 0 everywhere.
- **One seam, `GroundHeightAt(simPoint)`.** Presentation reads height only through it, and it calls the sim's height field. The sim and the visuals can't disagree, and no presentation code assumes y = 0.
- **Picking:** the mouse ray marches against the height grid (cheap at our map sizes) instead of intersecting one plane.
- **Units and buildings** sit at the sampled height. Units tilt slightly to the slope for vehicles only; infantry stay upright.
- **Selection rings, the placement ghost, and footprint outlines conform to the terrain:** flat quads offset and tilted by the surface normal, or Godot `Decal`s where a quad would visibly float.
- **Visual terrain may add fine detail** on walkable ground only within a small tolerance of the gameplay height (Gaea section). It may rise or drop freely inside blocked regions and under water.
- **Bridge decks** are authored at the height of the banks they connect.

### Presentation architecture

- **Node tree.** `Main` becomes a plain `Node` with three children:
  - `World3D`
  - `World2D` (legacy greybox, used by the editor and the fallback)
  - `UiRoot`
- **New code goes in `game/scripts/presentation/world3d/`, one owner per file:**

| File | Owns |
| --- | --- |
| `RtsCamera3D` | camera controls and the ground footprint of the view |
| `GroundPicker` | mouse ray to the ground plane |
| `UnitView3D` | position, smoothed yaw, animation state, team color |
| `BuildingView3D` | power, damage, and upgrade-module visuals |
| `EnergyWallView3D` | energy wall visuals |
| `TerrainBuilder` | terrain generated from map data |
| `FogOfWar3D` | fog post-process |
| `WorldOverlay2D` | screen-space health bars, labels, callouts |
| `SelectionRings3D` | selection rings |

- **Picking seam.** An `IWorldPicker` does three things: screen to sim point, entities under a point, and entities in a screen rect. The 2D and 3D worlds each implement it, so selection logic is shared.
  - 3D click-select projects each unit's body center to the screen and tests a screen radius, so clicking a soldier's head works.
  - Box select tests projected positions against the screen rectangle.
  - Hover is one query per frame, not a poll in every view.
- **Health bars and labels:** one screen-space `_Draw` pass using `Camera3D.UnprojectPosition`. They stay crisp at any rotation and cost one canvas draw.
- **Selection rings:** one `MultiMeshInstance3D` of flat ring quads.
- **Fog of war:** a full-screen post-process.
  1. Reconstruct world XZ from depth.
  2. Sample an R8 explored texture, uploaded when `ExploredRevision` changes.
  3. Paint unexplored areas black with a soft edge.
  - One shader covers terrain, props, and third-party assets. The existing visibility rules still hide units and buildings.
- **Energy walls:** chains of glowing columns and beams, plus a ground-line decal. A single ribbon plane vanishes edge-on under rotation, and walls are path-blocking gameplay.
- **Placement ghost:** the exact sim footprint shape plus the building mesh, in green or red.
- **Small sim-side additions** (facts only):
  - `UnitRenderSnapshot` exposes explicit activity (moving, attacking, fleeing, working/repairing), replacing the delta and timer guessing.
  - A unit-killed event carries position, facing, and cause (shot or crushed), for death clips on short-lived corpses.
  - `CameraZoom` leaves the sim snapshots.

## Track D: Elevation (Smooth Hills)

Decided 2026-09-30:
- smooth hills, walkable wherever the slope isn't too steep
- height difference scales weapon range and sight both ways
- a hard block for big climbs
- no uphill damage penalty
- crest cover deferred
- debuts in Level 2

**Height is gameplay truth, so it lives in map data and the sim, not in art.** "Terrain art never becomes gameplay truth" still holds: the 3D terrain and any Gaea dressing follow the sim's height field.

- **D1. Height field in the sim.**
  - A Godot-free `HeightField` is built at mission load from map data.
  - The grid is 1 content unit (1 m, 24 px) per cell with bilinear sampling. Heights are in content units, and the grid is deterministic.
  - It exposes `GroundHeightAt(SimVector2)` and `SlopeAt(SimVector2)` on `RtsSimulation`.
  - It is rebuilt on map load and on F5 editor edits only.
  - Maps without hill shapes are height 0 everywhere, so Level 1 is unaffected.
- **D2. Authoring in map data and the F5 editor.**
  - New terrain-shape entries in `maps.json`:
    - `hill`: center, x/y radii, peak height, falloff
    - `ridge`: polyline, width, crest height, falloff
    - `plateau`: polygon, top height, edge slope width
    - `depression`: negative height
  - Positive shapes combine by maximum; depressions then cut below it.
  - Water regions get a `water_level` (default 0). The shoreline is where the height field meets it.
  - The F5 editor creates, moves, and resizes these like regions.
  - The 2D greybox view and the editor draw **contour lines plus slope-blocked cells**, so elevation is visible and playable in 2D before 3D exists.
  - The schema goes into `docs/content-data-spec.md` when implemented.
- **D3. Slopes and pathing.**
  - Cells steeper than the maximum walkable slope become blockers in Track A's cached nav grid. This is just one more obstacle source.
  - Start with one threshold for everyone (proposal 30°). Separate vehicle thresholds only if playtests ask.
  - **No slope speed modifiers** (not requested).
  - **Building placement** requires near-flat ground under the footprint (proposal: at most 8°). A building sits at its footprint's average height, with a visual foundation plinth hiding small gaps.
- **D4. Height scales weapon range, both ways.** These are deterministic multipliers with no randomness. Tunables live in a content data block (`elevation_rules`).
  - **Height difference:** Δh = shooter ground height − target ground height, in meters. Buildings use their base height.
  - **Effective range** = base range × (1 + 0.06 × Δh), clamped to 0.6×–1.25×. That's +6% per meter downhill and −6% per meter uphill (proposal).
    - Example: a Rifleman's 6 m range becomes 7.4 m shooting from 4 m up, and 4.6 m shooting 4 m uphill.
  - **Hard block (proposal):** a target more than 6 m above the shooter can only be hit from within 2 m horizontally. This is the "you can't shoot up a cliff" case.
  - **Scope:** it applies to units and armed towers alike, so a Gun Tower on a hill really does outrange attackers below, and attackers must close in to hit it.
  - **No uphill damage penalty.** Range carries the rule.
  - **Where it applies:** auto-targeting, attack approach points, and chase logic all use effective range. Attackers climbing toward a target naturally close the distance.
  - **Enemy AI** gets the rule automatically but does not seek high ground yet. Authored AI markers can place defenders on hills.
  - **Readability:**
    - The selected unit's range ring is drawn as the *effective* boundary over the terrain: it bulges downhill, pulls in uphill, and collapses at cliffs.
    - The boundary is found by sampling directions against the height field.
    - Any new player-facing text goes through localization.
- **D5. Height scales sight for exploration, the same way.**
  - Fog reveal uses a per-cell effective sight radius: base sight × (1 + 0.06 × (viewer height − cell height)), clamped to 0.6×–1.25×.
  - Cells more than 6 m above the viewer are revealed only within 2 m.
  - **Effects:**
    - Ridges become lookout posts, which is good for Wells at the Ridge's unproven flank-lane scouting.
    - Plateau tops stay unexplored until someone climbs or scouts them.
  - **This works with the existing fog rule, not against it.** Enemies are visible in real time only inside explored ground and can slip back into unexplored fog to hide. An unexplored plateau top stays black, and enemies up there stay hidden, until it's revealed.
  - The cost is small: each viewer checks only the cells within 1.25× its sight radius against the height grid, and only when it moves.
- **D6. Level 2 debut.**
  - Rework Wells at the Ridge with a ridge that matters, for example a lookout ridge over a bridge approach or a flank well on a rise.
  - Use gentle climbable slopes and steep blocked faces.
  - Do it together with the mockup-review data fixes (base clearings, bridge width, flank wells).
  - Level 1 stays flat.
- **D7. Tests** in `tests/SimulationSmoke`:
  - Height sampling is deterministic.
  - Steep faces produce slope-blocked cells.
  - Paths climb a gentle slope and route around a steep face.
  - Effective range matches the formula at sample Δh values, including both clamps.
  - The hard block holds beyond 6 m up and releases within 2 m.
  - Sight reveals farther downhill and less uphill.
  - An unexplored plateau top stays unrevealed from below until a unit climbs.
  - Building placement is rejected on steep ground.
  - Hill shapes survive an F5 editor save/load round trip.

**Presentation side.** This lands with P1 (the picking seam, conforming rings and ghost) and P2 (the terrain mesh from the height field). See "Gameplay ground height" in Track C.
- **Visibility.** With a 45° camera and a maximum walkable slope below 45°, **a unit standing on walkable ground can never be hidden by the hill it's on.** Only faces steeper than 45° (blocked cliffs) and props (trees) can occlude. So keep steep faces short or away from lanes.
- **Reading height through an orthographic camera.** Orthographic projection flattens depth cues, so hills must read through:
  - lighting and shadows
  - slope-based texturing (rock on steep faces, grass on gentle ones)
  - possibly a subtle elevation tint
- Put a test hill in the P0 scale lineup to check this at Deck size.

## Asset Pipeline

### Characters (custom, built by us)

- **One shared base body and proportions** for all infantry: Grunt, Cadet, Rifleman, Guardian, Commander.
  - Unit identity comes from the gear kit (helmet, armor plates, pack, weapon) and silhouette.
  - Team color comes from a mask channel times a per-instance shader uniform, so the enemy "reskin in red" is the same mesh.
- **Rig** (researched 2026-09-30; details in `docs/3d-art-direction.md`, "Armored Characters").
  - Mixamo's auto-rigger rigs the unarmored base body once, with the "No Fingers (25)" skeleton. That is the only game skeleton.
  - Armor and gear are **rigid-weighted**: 100% to one bone, inside one merged mesh per unit. Smooth weights only on the undersuit at joints.
  - 2–4 baked helper bones (pauldrons, maybe thigh plates), 28 bones or fewer in total.
- **All clips are baked onto that one skeleton in Blender:**
  - Mixamo downloads for our uploaded character (no retargeting needed)
  - custom clips authored on the Mixamo Rig control rig
  - mocap or asset-pack clips, retargeted in Blender with the free Retarget extension
- **Shared library.** Helper-bone motion is baked into every clip. Godot imports one shared `anims.glb` as an `AnimationLibrary`, used by every infantry unit's `AnimationPlayer`.
- **No import-time retargeting for our own skeleton.** Godot's humanoid retargeting (BoneMap → `SkeletonProfileHumanoid`) would freeze the helper bones. It stays in reserve for third-party libraries only.
- **Mixamo (partial):** locomotion and common combat clips.
  - Download settings: FBX, 30 fps, **In Place** for locomotion, "without skin", with one fixed Arm-Space value chosen for the bulky Guardian.
  - Rob downloads them (Adobe account) in one batched session from a clip list.
  - Mixamo animations are royalty-free for commercial games but may not be redistributed as raw files. Record them in the asset provenance notes.
- **Custom clips.** Expected, because Mixamo likely lacks these:
  - Grunt build/repair work loop with a tool
  - Guardian energy-weapon firing stance
  - crushed-by-vehicle death
  - Commander-specific idles
- **Authoring routes for custom clips:**
  - Hand keyframing in Blender with an IK control rig.
  - Video-to-motion capture tools (record reference, convert, clean up).
  - Claude-scripted procedural clips for mechanical motion: recoil, breathing idle, tool swings, weapon bob. Scripted clips are serviceable for loops, but not for expressive acting.
- **Blender pipeline scripts** (Claude writes and runs them headless or through Blender MCP):
  - batch-import clips
  - delete leaf bones and add helper bones
  - rigid-weight the plates and join the mesh
  - run a clearance check and NLA arm corrections
  - bake with visual keying
  - export one GLB per unit plus `anims.glb` and a clip manifest
  - The full step table is in `docs/3d-art-direction.md`.
- **Crowd spike exception:** the P0 performance spike may use any stock Mixamo character as a throwaway stand-in. It never ships.

### Clip list (first pass)

| Unit | Clips |
| --- | --- |
| Grunt | unarmed idle, run, fleeing run, work loop (custom), death |
| Cadet, Rifleman | rifle idle, rifle run, rifle fire, 2 death variants |
| Guardian | heavy idle, heavy run, energy-weapon fire (custom), death |
| Commander | pistol idle, pistol run, pistol fire, death |
| Shared | crushed death (custom); hit react (later) |

### Vehicles

- Rover, Medium Tank, and later Heavy Tank are rigid part hierarchies:
  - Rover: hull and wheels.
  - Tanks: hull, turret, and barrel.
- The turret yaws to the target. A UV-scroll shader handles tracks and wheels.
- No skeleton.

### Buildings

- **A modular military-industrial kit** (panels, vents, pipes, catwalks, lights, antennas). It shares one trim sheet and a team-color mask.
- **The mesh base equals the sim footprint** (Track A1). The export script writes the ground outline into `buildings.json`, and a validator flags drift.
- **States come from instance uniforms and small child nodes:**
  - **Powered vs. unpowered:** lights, fans, radar spin, desaturation, sparks.
  - **Damage:** smoke below 50% health, fire below 25%.
  - **Destroyed:** explosion and debris, then removed.
  - **Tower upgrades:** Gun and Rocket modules mount on the Defense Tower base, a literal in-place upgrade.
  - **Barracks Guardian retrofit:** add-on module.
  - **Construction** stays instant; an optional 0.5–1 s deploy rise is presentation only.
- **Level 1 set first:** Hub, Barracks, Power Plant, Pylon, Extractor, Defense Tower with Gun and Rocket modules. Then Vehicle Bay.

### Terrain and props

- **`TerrainBuilder`** generates the ground at mission load from `maps.json` regions, deterministic per map:
  - **Ground:** 32 m chunks with a 4-layer splat shader (grass, dirt, rock, mud).
  - **Water:** sunken bed, water plane, and a shoreline foam band.
  - **Cliff and rock regions:** raised rock kit meshes.
  - **Forest regions:** `MultiMesh` tree and undergrowth scatter, with edge falloff.
  - **Resource basins:** orange dirt splat and ore rocks.
  - **Bridges:** kit meshes, intact and broken.
  - **A 20–30 m border skirt**, so rotation never shows the void.
- **Roads are decoration,** derived from lane centerlines, bridges, and base markers. An explicit non-gameplay `decor` list is added to map data only if needed.
- **Textures:** Poly Haven CC0 ground textures to start.
- **Props:** CC0 low-poly trees and rocks, restyled to one palette.
- **Level 1** needs its first terrain regions authored.
- This closes the "editor can't show the vibe" gap: regions drawn in the F5 editor become real terrain in 3D.
- **Water look:** color comes from water depth (shallow turquoise to deep blue, as in the mockup), with foam where the water surface meets the bank. The shape of the bed is what sells it, which is where Gaea helps (next section).

### Optional Gaea dressing pass (P2)

Rob owns a QuadSpinner Gaea Professional license, currently installed on his other machine.

- **What Gaea is for:** making the *non-playable* terrain look like the mockup:
  - shorelines and lake beds
  - cliffs, rock outcrops, and ridges inside blocked regions
  - the map border skirt
  - natural texture-blend masks (erosion flow, deposits, slope rock, wet banks)
- **What Gaea is not for:** it never authors map layout or playable ground. `maps.json` stays the only gameplay truth.
- **Round trip:**
  1. **Mask export (script in `tools/`).**
     - Rasterize one map's regions from `maps.json` into grayscale PNG masks over playable bounds plus the skirt, at a fixed meters-per-pixel. Masks: water, blocked/cliff, forest, walkable, lanes/roads, resource basins.
     - Also export the sim's **gameplay height field** (Track D) as a 32-bit EXR.
  2. **Gaea template graph (built once by Rob).**
     - File nodes read the masks and the gameplay height field.
     - Cliffs and rocks appear only inside blocked masks, and lake and river beds are carved only inside water masks.
     - Walkable ground follows the gameplay height field. Gaea adds only fine detail there (erosion streaks, small bumps) within ±0.25 m.
     - **Hills and ridges themselves are authored in map data (Track D2), never in Gaea.** Gaea dresses them.
     - Outputs: a 16/32-bit heightmap plus splat masks (rock, dirt/deposits, sand/wet bank, grass) and a shoreline mask for foam and scatter density.
  3. **Scripted builds.** The Professional edition's command-line automation (variables plus batch builds) rebuilds the template per map. This needs Gaea installed on the machine running the script.
  4. **`TerrainBuilder` uses Gaea output when present.**
     - It uses the heightmap for cliffs, beds, and the skirt, and the masks for splat blending and prop scatter density (boulders, reeds, lily pads along shorelines).
     - Without Gaea output it falls back to the procedural-from-regions terrain, so the game never depends on Gaea being available.
  5. **Validation (automated, fails the build):**
     - Every walkable cell's visual height is within ±0.25 m of the gameplay height field.
     - Every water-region cell is below that region's `water_level`.
     - **The waterline is the water region's edge in map data.** Bank slopes happen only below it, so land stays walkable, at its gameplay height, right up to where gameplay says water begins.
- **Outputs are committed per map** as derived art assets under `game/assets/terrain/<map_id>/`. When a map's layout changes, re-export the masks and re-run the graph. A stale output fails validation instead of silently lying.
- **When to adopt:** build the procedural terrain first. Then do one Gaea pass on Wells at the Ridge (shorelines, top and bottom cliffs, border) and compare side by side at the gameplay camera. Adopt it as the standard dressing pass only if it clearly wins.
- **Mountains beside roads:** visual-only cliffs next to a lane are fine. Keep the tallest mass back from the lane edge, because the rotating 45° camera can hide units behind it. Walkable hills and ridges that troops can stand on are gameplay terrain from Track D. Gaea only dresses them.
- **License:** Professional is Gaea's commercial tier. Skim the EULA once before shipping Gaea-derived output, and record Gaea in the asset provenance notes.

### Budgets (set by the Steam Deck)

| Asset | Triangles (LOD0) | Texture | Draw calls |
| --- | --- | --- | --- |
| Infantry | 2k–4k | 512²–1k², team mask | 1 (+ shadow) |
| Vehicle | 3k–6k | 1k² | 2–4 parts |
| Small building | 2k–6k | shared 2k² trim sheet | 1–3 |
| Colony Hub | 6k–12k | shared trim sheet | 2–4 |
| Tree / rock | 200–800 | shared atlas | MultiMesh, 1 per species |

Godot generates mesh LODs on import. On the Deck an infantry model is roughly 20–60 px tall, so silhouette, pose, and team color matter far more than detail.

### Character rendering tech

- **`Skeleton3D` + `AnimationPlayer` per unit:**
  - about 25 runtime bones
  - one merged mesh and material per unit (gear merged at export)
  - offscreen units don't animate
  - at far zoom, animation advances at a staggered 15–20 Hz (`AnimationMixer` manual callback)
  - no per-frame allocations in C# sync
- **Fallback if the Deck misses the bar:** vertex animation textures with one `MultiMeshInstance3D` per unit type. Both sit behind one `UnitVisual` seam.
  - Research shows CPU-side animation evaluation, not GPU skinning, is Godot's crowd bottleneck. One reported case was about 100 animated characters at 60 fps on desktop hardware.
  - The Deck's CPU is weaker. Treat VAT as a likely need at the 300-infantry stress bar, not a remote one.
  - Mitigations: a manual `advance()` scheduler and no `AnimationTree`.
- **Far-zoom strategic icons (optional, decided in the P0 scale lineup).**
  - At the farthest zoom band, units swap from animated models to flat team-colored type icons. Supreme Commander's strategic zoom is the reference.
  - The icons are one `MultiMeshInstance3D` of camera-facing quads, or glyphs in the screen-space overlay. Either way that's one draw call and zero animation cost.
  - Two payoffs:
    - **Readability on a 7" Deck screen,** where a crisp icon may beat a 20 px soldier.
    - **Performance,** because the worst case for animation cost (the whole army on screen at far zoom) is exactly when models are replaced.
  - Buildings, terrain, and VFX keep rendering normally.
  - Test it on the Deck next to the plain far-zoom view before deciding. If adopted, it may make the VAT fallback unnecessary.

## Performance Targets

| Target | Value |
| --- | --- |
| Floor device | Steam Deck (LCD and OLED), 1280×800 |
| Deck, typical battles | 60 fps on a "Deck" quality preset |
| Deck, stress bar | at least 40 fps (Deck 40 Hz mode), never below 30 |
| Stress bar content | 300 animated infantry, 30 vehicles, 40 buildings, about 5k tree/rock instances |
| Desktop | RX 6900-class and RTX 5090 are not constraints. A "High" preset may raise shadows, ambient occlusion, anti-aliasing, and foliage density. |
| Deck OS | Measure a native Linux export and the Windows build under Proton in P0; pick the ship path from the numbers |
| Deck renderer | Measure Forward+ vs. the Mobile renderer in the P0 spike |
| Textures | VRAM-compressed with mipmaps |
| Shadows (Deck preset) | 1 directional light, 1–2 short cascades; blob decals for units at far zoom if needed |

## Mockup Review (Wells at the Ridge)

The mockup is effectively Level 2: two bases, a central island well, two bridges, and north/south flank lanes. The visual direction is right. Layout notes:

1. **It is top-down, not 45°.** With rotation, cliffs and tall trees beside lanes will hide units, so keep tall features at map edges and deep in blocked regions, with low shrubs and rocks at lane borders.
2. **The roads read as single-file.** The data lanes are about 13 m wide, but the art pinches forest onto a 3 m track. Draw the road as a 3–4 m decal inside a 12–16 m open lane.
3. **The base clearings are tight.** The data radius is about 17 m. Recommend 20–22 m (480–530 px), with an open side toward the well.
4. **The bridges are too narrow.** At 5 m, a Medium Tank plugs the crossing. Recommend 7–8 m, or a ford on one channel.
5. **There are only three wells.** One small flank well per lane would make the long routes worth contesting.

Items 3–5 are `maps.json` data changes. Items 1–2 are `TerrainBuilder` rules.

## Order Of Work

1. **Docs lock pass.** Update AGENTS, README, technical-architecture, and scaffold-plan (plus the other lines listed below) to the locked decisions.
2. **Track A: footprints and pathing.** Start with A0 (fixed-rate simulation and render interpolation), then footprints, the nav grid, and group paths. Sim plus tests; the 2D view keeps working. The nav grid is built to accept slope blockers from Track D.
   - **Track D sim side (D1–D5, D7)** follows right after: the height field, hill authoring in the F5 editor, slope blocking, height-scaled range and sight, and 2D contour drawing. It's testable in the 2D build before 3D exists.
3. **P0 spikes** (can run alongside step 2):
   - the scale lineup, including the far-zoom strategic-icon A/B
   - the crowd spike: 300 animated stand-ins, rotating ortho camera, profiled **on the Steam Deck** for Forward+ vs. Mobile, Linux vs. Proton, and with and without far-zoom icons
4. **Track B: input layer and keymap,** plus the new sim verbs. The keymap is renderer-agnostic, so it lands in the 2D build first.
5. **P1: 3D greybox parity.**
   - Primitive stand-ins sized from sim footprints.
   - Camera, picking seam, fog, overlays, rings, walls, ghost, minimap.
   - A 2D/3D toggle.
   - **Exit:** Levels 1 and 2 are playable end to end in 3D.
6. **P2: terrain generator** (the terrain mesh from the height field), Level 1 regions, the Level 2 data fixes, and **Level 2's elevation debut** (D6). Then the optional Gaea dressing pass, trialed on Wells at the Ridge.
7. **P3: characters.**
   - The base body and rig first, then the Rifleman end to end.
   - Then Cadet, Grunt, Commander, Guardian.
   - The custom clips.
8. **P4: vehicles.**
9. **P5: buildings kit,** with the Level 1 set first.
10. **P6: retire 2D.** Remove the sprite views and atlas tools, and update asset READMEs and provenance notes.

## Docs And Tools To Update

- **`AGENTS.md`**
  - Visual style lock (line 89)
  - Art pipeline (58)
  - "Presentation owns sprites" (135)
  - "top-down" wording
  - Hardware and camera locks
- **`README.md`**: line 28.
- **`docs/technical-architecture.md`**
  - Visual mode (36)
  - Renderer remarks (52, 61)
  - Camera (440–447)
  - The "Avoid ... full 3D modeling, complex animation" guidance (465–478)
  - Performance targets (497–510)
  - Platforms (514–525): the Steam Deck makes Linux/Proton testing required
- **`docs/scaffold-plan.md`**: lines 23, 25, 81, 105.
- **`docs/project-identity.md`**: art pipeline (137–139); keep the readability pillar in 3D terms.
- **`docs/product-roadmap.md`**
  - Art lines (18–19)
  - Footprint and buffer open item: closed by Track A
- **`docs/engineering-standards.md`**: lines 89–91.
- **`docs/first-landing-mission-spec.md`**: "Mode: top-down mission RTS" (line 20).
- **Building-wall rule** (Track A decisions). Replace "spacing buffer to prevent over-cramming" with "footprints follow the building shape plus a small buffer; buildings are physical walls; players can wall off routes or trap themselves" in:
  - `AGENTS.md` line 105
  - `docs/technical-architecture.md` lines 66, 254, 531
  - `docs/system-contracts.md` lines 70, 80
  - `docs/product-roadmap.md` lines 168, 493
- **`docs/system-contracts.md`**: pathing, placement, spawn/exit point, unit radius, range-to-outline, AI wall handling, and new unit-verb contracts (Tracks A and B). Also elevation: slope blocking, height-scaled range and sight, the hard block (Track D).
- **`docs/content-roadmap.md`**: the Cliffs and Ridges terrain grammar ("Do not add height bonuses yet" is superseded by Track D).
- **`docs/content-data-spec.md`**
  - Footprint shape (polygons and circles)
  - `exit_point` and `collision_radius`
  - Hill/ridge/plateau/depression shapes, `water_level`, and `elevation_rules`
  - `placement_buffer` meaning (217, 300)
  - The stale bridge example (437–438)
  - `decor`, only if added
- **`docs/release-roadmap.md`**: Steam Deck verification, Steam Input config, rebinding UI.
- **`game/assets/units/README.md` and `game/assets/buildings/README.md`**: replaced by 3D asset conventions.

## Risks

- **Art and animation throughput is the real bottleneck.** Building characters in full plus custom clips is the largest cost in this plan. Mitigations:
  - one base body
  - rigid gear
  - a lean clip list
  - shared animation libraries
  - a modular building kit
  - CC0 terrain props
  - greybox primitives that keep the game playable at every step
- **Readability on a 7" Deck screen.** Guarded by hero scale, strong team color, overlay health bars, and the orthographic camera.
- **Occlusion from rotation.** Handled by terrain-generator placement rules first.
- **Crowd performance on the Deck.** Front-loaded as the P0 spike, with VAT as the known fallback. Godot's CPU animation cost makes VAT a realistic outcome for large battles on the Deck.
- **Mixamo is no longer actively updated by Adobe** *(indirect report)*. Download the full clip list early, in batched sessions, and keep the source FBX files archived outside the repo.
- **Elevation readability and tuning.**
  - Height-scaled range and sight only work if players can read which ground is higher through an orthographic camera. Guard this with the effective range ring, slope-based texturing, lighting, and a test hill in the P0 lineup.
  - The numbers (6% per meter, the 0.6–1.25 clamps, the 6 m hard block, the 30° walkable slope) are starting proposals for Level 2 playtests.
- **Behavior changes from Track A.** Small buffers, building walls, per-unit radius, and separation change how bases pack and how fights flow.
  - Re-run the Level 1 and Level 2 playtest checks after Track A.
  - Check that authored missions don't start with the player or enemy already walled in.
- **Scope creep into "3D features"** (height advantage, destructible terrain, physics ragdolls): out of scope unless the design docs reopen them.
