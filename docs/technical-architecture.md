# Stratezone Technical Architecture

This document defines the intended technical architecture for Stratezone.

Its purpose is to turn the colony RTS design into an implementation shape that is maintainable, testable, packageable, and realistic for an AI-assisted solo indie project.

This document is the technical source of truth for:

- stack direction
- runtime boundaries
- simulation ownership
- content data ownership
- save/load direction
- map and mission architecture
- asset pipeline direction
- testing and debugging strategy

If future code disagrees with this document, either align the code or intentionally update this document. Do not let accidental scene structure become the architecture.

## Documentation Role

- **Doc role:** Active source of truth for architecture boundaries and system shape.
- **Owns:** stack direction, simulation/presentation separation, repo layout direction, save-state shape, data ownership, and engine integration boundaries.
- **Does not own:** product identity, exact unit balance, final mission content, store readiness, or build command names once tooling docs exist.
- **Read when:** choosing tools, creating project structure, adding systems, changing save data, or deciding where gameplay rules belong.
- **Do not read for:** exact player-facing design pillars or milestone status.

## Current Architecture Decision

The first prototype stack is locked as:

- **Engine:** Godot 4
- **Language:** C# for game logic and larger systems
- **Target:** native desktop first, Windows as the first practical platform. The Steam Deck is the performance floor; ship it as a native Linux build or the Windows build under Proton, decided by measurement.
- **Distribution direction:** itch.io and Steam-friendly packaged builds
- **Visual mode:** readable 3D military-industrial presentation.
  - Skeletal-animated infantry, rigid-part vehicles, modular buildings, and terrain generated from map data.
  - A fixed 45° orthographic camera with free rotation.
  - The simulation remains a 2D plane; presentation maps it onto the 3D ground at 1 content unit = 1 Godot unit.
  - Details: `docs/3d-presentation-plan.md`.
- **Renderer:** Godot Forward+ on desktop. The Steam Deck renderer (Forward+ or Mobile) is chosen from the crowd-performance spike.

If the project pivots to another engine or visual mode, update this document, `README.md`, `AGENTS.md`, and `docs/scaffold-plan.md` in the same pass.

## Core Philosophy

Godot should present the game. Plain C# systems should own the game.

That means:

- scenes and nodes should not become the only source of gameplay truth
- core systems should be testable without launching a full visual scene where practical
- data should be explicit and inspectable
- simulation state should be serializable
- UI should display simulation truth, not invent parallel truth

The project should avoid custom engine work. Stratezone is hard because it is a systems game, not because it needs a clever renderer. The 3D presentation uses stock Godot 3D features (skeletal animation, MultiMesh, shaders, post-process) rather than custom rendering tech.

## Practical Constraints

The architecture is shaped around these realities:

- the project is likely to be built with heavy AI assistance
- the primary development machine is Windows
- the game may eventually be sold as a downloadable desktop indie game
- art production is a small in-house Blender pipeline, not a large art team or a reliable paid-artist pipeline:
  - Custom characters on one shared base body and skeleton, with gear kits per unit.
  - A modular building kit.
  - Selected Mixamo clips plus custom animation.
  - CC0 terrain props and textures.
  - Scripted (bpy) automation for cleanup and export.
  - Designs follow the established 2D concept and sprite art; AI-assisted concepts remain acceptable as reference.
- the Steam Deck sets the performance and readability floor (1280×800, small screen)
- the first playable mission matters more than future-perfect engine abstraction
- levels are fresh authored scenarios rather than a persistent colony campaign
- resource gathering uses powered refinery/extractor buildings on scarce limited wells that trickle resources and can deplete, not survival-style hauling
- fog of war uses black unexplored areas; explored areas stay visible after scouting rather than reverting to gray shroud, and units/buildings in explored areas remain visible in real time
- building placement should feel freeform, with no visible grid. Footprints follow each building's real outline plus a small buffer, and buildings are physical walls that players and AI can use to wall off routes (or trap themselves)
- first prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower
- Guardian production is planned as a Barracks upgrade; Vehicle Bay is planned as a powered physical Barracks add-on built adjacent to the Barracks
- Gun Tower and Rocket Tower should be modeled as in-place Defense Tower upgrades that keep wall-anchor behavior while adding direct attack stats and higher cost
- first prototype units are Grunt, Cadet, Rifleman, Guardian, Rover, and Commander
- Colony Hub is the spawn location for trained units, while Barracks controls what can be trained by level, troop capacity, and unlocks
- enemy bases can rebuild and produce from limited resources
- First Landing is a playable ugly 5-10 minute mission before final art or cutscenes
- the first public build target is a demo built from the first five levels, but the project is still pre-demo and should not optimize release tooling ahead of working level design
- debugging must be straightforward enough for future Codex runs to reason about quickly

## Proposed Repo Shape

The exact Godot project layout can change after scaffolding, but the target ownership should be clear:

```text
Stratezone/
  README.md
  AGENTS.md
  docs/
    project-identity.md
    technical-architecture.md
    engineering-standards.md
    product-roadmap.md
    scaffold-plan.md
    first-landing-mission-spec.md
    system-contracts.md
    content-data-spec.md
    implementation-checklists.md
    release-roadmap.md
  game/
    project.godot
    scenes/
    scripts/
    assets/
    data/
  tests/
  tools/
```

Possible domain layout inside `game/scripts/`:

```text
simulation/
  core/
  economy/
  power/
  grunts/
  combat/
  ai/
  missions/
  events/
presentation/
  camera/
  input/
  ui/
  fx/
content/
  loading/
  validation/
```

Keep paths honest. If the actual Godot layout differs, update this document after the first scaffold.

## System File Shape

Each gameplay domain should grow as a small package of cooperating files instead of one large script. For example, a mature power implementation may include:

```text
simulation/power/
  PowerDefinition.cs
  PowerNodeState.cs
  PowerCommand.cs
  PowerSystem.cs
  PowerEvent.cs
  PowerDebugSnapshot.cs
presentation/power/
  PowerOverlayView.cs
  PowerNodePresenter.cs
```

The exact names can change by domain, but the ownership should stay legible:

- definitions describe tunable content
- state records describe saveable simulation truth
- commands describe requested actions
- systems apply rules
- events describe what happened
- debug snapshots expose inspectable state
- presentation adapters turn simulation state into Godot visuals

When a hand-written code file reaches 900 lines, treat that as an architecture review point. Split it if it has more than one reason to change, especially if scene code is starting to own simulation behavior, or document why the file should stay together.

Current architecture pressure points:

- `game/scripts/presentation/main/Main.cs` remains large, but command-panel rendering/detail helpers now live in `Main.CommandPanel.cs`; keep moving presentation-only surfaces out as they gain independent reasons to change.
- `tests/SimulationSmoke/Program.cs` is still the broad mechanics harness, while content, First Landing, Wells at the Ridge, and mission trigger checks now live in focused smoke files.
- Mission 2 is a data-plus-seams exercise. Current runtime uses mission JSON for map, starts, presentation keys, trigger pacing, and trainable units instead of copying First Landing setup into scene code.

## Simulation Boundary

The simulation owns all rules that must survive save/load and all decisions the player should be able to trust.

Simulation-owned systems:

- entity identity and lifetime
- map occupancy and passability
- resources and extraction
- power networks, pylon links, and build radius
- defense tower wall links and path blocking
- Barracks add-on adjacency, power state, and training unlock effects
- in-place tower upgrade state
- grunts as expensive recruitable units
- construction and repair
- unit stats and combat resolution
- projectiles or hitscan rules, if used
- enemy raid timing and objective AI
- mission trigger grace, cooldown, and coalescing
- mission objectives and win/loss state
- mission-specific failure criteria
- environmental events
- fog/scouting truth
- saveable game state

Presentation-owned systems:

- 3D models, materials, and team-color shading
- animations
- particles
- camera movement
- selection visuals
- audio triggers
- screen shake
- UI layout
- tooltips
- input device plumbing

Presentation may ask the simulation to do things. Presentation should not directly mutate game truth without going through a command/action layer.

## Localization Boundary

Player-facing text is a presentation concern, not simulation truth.

Rules:

- simulation systems return message keys and argument values for blocked actions, objectives, and mission state
- presentation/UI resolves those keys through localization data under `game/data/i18n/`
- content IDs remain stable identifiers and must not be translated
- `display_name` fields remain English fallback/readability aids during the prototype
- save data, tests, and gameplay rules must never depend on localized strings

The first implementation uses a small text-reviewable English catalog. Additional locales can be added later without changing simulation rules.

## Game Loop Direction

The simulation runs at a **fixed step rate**, proposed at 20 steps per second (`TickSeconds = 0.05`). Presentation interpolates between steps. This is the classic RTS pattern: StarCraft II and Supreme Commander run their game logic at about 16 and 10 steps per second. It makes outcomes identical at any frame rate and stops simulation cost from scaling with the display's refresh rate.

This is planned as Track A0 in `docs/3d-presentation-plan.md`. The current code still ticks once per rendered frame with a variable delta (`Main.cs`).

Recommended flow:

1. Input layer converts mouse/keyboard/gamepad events into game commands.
2. Command layer validates whether the action is legal; accepted commands are queued for the next step boundary.
3. The frame loop accumulates real time and runs whole fixed simulation steps (`Step()`), capping catch-up at a few steps per frame. Tests use `Advance(seconds)`, which runs the same whole steps.
4. Each step applies commands, jobs, AI, combat, power updates, and events.
5. Presentation reads the latest two step states and interpolates positions and facing by the leftover accumulator fraction, so motion stays smooth at any frame rate.
6. UI displays current state, warnings, objectives, and selected entity actions.

Do not tie combat or economy outcomes to animation completion unless there is a deliberate reason.

## Entity Model

Start simple. Do not introduce a heavyweight ECS unless the project earns it.

Recommended early model:

- stable entity IDs
- explicit data records for individual units, buildings, resources, and map objects
- small domain services for systems like power, economy, combat, and grunts
- content definitions for base stats and build costs

This gives us enough structure to scale without turning the first prototype into framework archaeology.

## Core Systems

### Map System

Owns terrain, buildability, passability, resource wells, starting zones, neutral objects, and mission-specific markers.

Early requirements:

- freeform-feeling placement using shape-accurate building footprints (real outline plus a small buffer)
- passability checks
- adjacency checks for physical Barracks add-ons such as Vehicle Bay
- resource-well positions
- base start area
- enemy base area
- objective markers

### Power System

Owns powered territory, power-source links, Pylon transmission, outage consequences, and build radius.

Power should be a core identity system, not a decorative requirement.

Early requirements:

- structures can require power
- power plants generate power in a small radius
- pylons link power over long distances
- disconnected buildings lose function or degrade
- underpowered buildings shut off
- power overlay is readable

### Defense Wall System

Owns Defense Tower links, wall segments, path blocking, wall shutdown when an anchor tower is destroyed or unpowered, and wall continuity when a Defense Tower is upgraded in place.

Early requirements:

- Defense Towers can link to nearby compatible Defense Towers.
- A valid Defense Tower pair creates an energy wall segment between them.
- Energy wall segments block enemy movement/pathing.
- Destroying or disabling either tower removes the wall segment.
- Gun Towers and Rocket Towers use the same wall-anchor behavior while also attacking.
- Gun Towers and Rocket Towers should normally be created by upgrading an existing Defense Tower in place, preserving the tower's anchor identity during the transition.
- Armed tower variants cost more than basic Defense Towers.

### Economy System

Owns materials, extraction rates, storage, and build costs.

Early requirements:

- resource well extractor
- material income over time
- limited well capacity and depletion
- spend materials on construction and units
- consequences when extractors are destroyed or unpowered
- enemy economy uses limited resources and can race the player for unclaimed wells

### Grunt System

Owns recruitable grunts, construction, repair, grunt availability, and grunt consequence tracking.

Early requirements:

- expensive grunt units
- resource-cost replacement
- build tasks
- Barracks add-on construction tasks adjacent to the Barracks
- tower upgrade tasks that convert Defense Towers into armed variants in place
- repair tasks
- grunt danger or casualty consequences
- flee behavior when threatened
- player-commanded construction and repair
- simple priority rules

Avoid deep personality simulation in the first prototype. Grunts should behave like costly utility troops with no combat value: they can die under attack, should flee when threatened, and losing them is a meaningful economic and tactical setback.

### Combat System

Owns attack legality, damage, resistance, range, cooldowns, projectiles if used, death, and target selection.

Early requirements:

- individual infantry/security unit
- Cadet as the cheapest basic troop
- Rifleman as the baseline combat troop
- Guardian as the laser trooper
- Rover as the small fast scout vehicle for fog-of-war exploration
- fragile Commander with a pistol as a mission fail-condition unit
- group-benefit behavior for low-cost units where useful
- higher-cost specialist or heavy unit that can operate with less support
- turret
- powered support infrastructure such as Med Hall and Logistics / Repair Pad when the mission needs sustain decisions
- fragile static siege infrastructure such as Artillery Battery when the mission needs long-range base pressure
- enemy attacker
- building damage
- enemy infrastructure as valid targets
- first enemy faction can reuse the player-like technology set with different visuals, costs, timings, or tactical emphasis
- enemy production/rebuild behavior with limited resources; Level 1 should run slower than the normal baseline
- explosive friendly fire; normal gunfire should not cause friendly fire in the first prototype
- classic RTS resistance math: basic infantry dies fast, ballistic fire performs poorly against heavy armor, explosives crack structures, and crush damage punishes exposed infantry
- attack speed, damage, range, resistance, and movement speed live on unit/building content records, not separate weapon equipment records

### Mission System

Owns objectives, mission phases, scripted events, fresh-scenario setup, and win/loss state.

Early requirements:

- start from an already-landed base in Level 1
- build power plant, pylons, barracks, extractor/refinery, and defense towers
- survive raid
- destroy all enemies on the map
- defend an on-map commander unit
- support a permanent Medium Tank occupant release when either side's Colony Hub is destroyed, and keep mission completion blocked while enemy Hub occupants are alive
- support mission data choosing whether Barracks upgrades/add-ons are player-built, prebuilt, upgraded in place, or locked for the mission
- fail if mission-specific critical conditions are broken, such as Colony Hub destroyed, Commander killed, transport lost, convoy escaped, or required Grunt/equipment lost; timer-expiry mission failure is not planned for the first demo unless the roadmap is explicitly reopened

### Event Director

Owns timed and condition-based pressure events.

Early requirements:

- raid warning
- environmental event warning
- event start/end
- event consequences
- classic RTS command warnings for player-known events such as enemy spotted, own assets under attack, power offline, construction complete, and training complete
- trigger coalescing, grace windows, and cooldowns so multiple early mission conditions do not stack into unfair back-to-back raids

Events should be inspectable and tunable. Avoid opaque random chaos early. Do not surface omniscient hidden enemy intent in the normal HUD; enemy plans should be inferred from scouting, fog, visible units, attacks, and visible infrastructure state unless a future radar/scanner system grants extra information.

### AI System

Start with simple scripted or director-driven enemy behavior.

Early requirements:

- small committed enemy attack groups
- patrols or guards, including mission-authored patrol marker lists for early exploration pressure
- attack priority for visible player structures
- retreat or regroup only if easy
- a small internal rival-officer state for memory-shaped behavior, not player-facing adaptation narration

Do not build skirmish-grade AI before the authored mission loop works. Level 1 should run slow and readable: small groups attack, some units defend the enemy base, and all enemy construction/production spends resources. Level 2 may use a small deterministic-random patrol dispatcher over authored markers so top/bottom exploration feels active without becoming hidden-plan alert spam or a general-purpose skirmish AI.

The rival-officer layer is not a full character simulation. It should track a few mission-local facts, such as power strikes, wall blocks, wiped attack groups, exposed Commander sightings, scouting, and retreats. It may adjust target choice, regroup timing, or production weights, but it must not announce hidden enemy strategic changes to the player. The player should infer adaptation from visible enemy actions and scouted battlefield state.

## Content Data

Prefer explicit content definitions for:

- units
- buildings
- resources
- events
- missions
- faction modifiers

The current first pass uses JSON under `game/data/`. Future Godot resources or CSV tables can be considered later, but content data must stay separate from hardcoded scene behavior. See `docs/content-data-spec.md` for first-pass fields, IDs, and validation expectations.

Content definitions should be:

- reviewable in text where possible
- versionable in Git
- easy for Codex to inspect and patch
- validated by tooling once the schema stabilizes

## Save/Load Direction

Save simulation state, not Godot node state.

Save data should include:

- mission ID and version
- elapsed mission time
- player resources
- entity records and health
- building status and power links
- grunt/task state
- commander state when the mission uses an on-map commander
- objective progress
- event director state
- fog/scouting state

Save data should not depend on node paths as canonical identity.

Because levels start as fresh scenarios, save/load should prioritize in-mission reliability before cross-mission persistence. Campaign progression can track completed scenarios later without treating the colony as continuous.

## Input and UI

Keep input mapping explicit.

Controls follow real-RTS conventions. The full keymap is in `docs/3d-presentation-plan.md` (Track B).

- **Mouse:**
  - left click selects, drag box-selects
  - Shift adds to the selection; Ctrl+click or double-click selects all of a type on screen
  - right click is the smart command (move, attack, repair, bridge); Shift + right click queues it
- **Camera:**
  - arrow keys and screen edge pan
  - middle-mouse drag rotates; `Home` resets to north
  - mouse wheel zooms
  - WASD does not pan
- **Command card:** a grid, where keys follow button position (`Q W E R T` / `A S D F G` / `Z X C V B`) and slots never shift. `B` opens the build card.
- **Control groups:** `Ctrl + 1–0` sets, `1–0` recalls, and double-tap centers the camera.
- **Unit commands:** Stop, Hold Position, Attack-move (`A`), and Shift-queued orders.
- **Navigation:**
  - `F1` idle Grunt, `F2` all combat units, `F3` Commander
  - `Space` jumps to the last alert, `Backspace` centers on the Hub
- **Escape:** cancel, then deselect, then pause menu.
- **Developer hotkeys** live behind `Ctrl+Shift` in dev builds only.
- **Bindings:** every binding is a named Godot input action defined in one keymap. Button labels read from it, which enables rebinding and a Steam Input layout for the Steam Deck.

In 3D, picking goes through one screen-to-world seam: a mouse ray to the flat ground plane, plus screen-space tests for units and footprints. Views don't poll the mouse individually.

Use Godot UI for HUD and panels unless a later architecture change justifies a different layer. The HUD stays a `CanvasLayer` over the 3D viewport. Health bars and world labels draw in one screen-space overlay pass.

Early HUD surfaces:

- resource count
- power status
- troop cap / allowed troop count
- grunt status
- selected entity card
- build menu
- objective tracker
- classic RTS alert line for player-known warnings
- minimap with a rotated view outline and click-to-pan, needed once camera rotation lands

## Art and Asset Pipeline

Assets must favor readability at RTS distance (infantry are roughly 20–60 px tall on a Steam Deck) and a lean production pipeline. Full direction lives in `docs/3d-presentation-plan.md`.

- **Design reference.** The established 2D concept and sprite designs define silhouettes, gear, proportions, and color schemes. 3D assets should stay as close to them as practical. The old hand-drawn/cel rendering look is not itself required.
- **Characters.**
  - One shared base body and humanoid skeleton for all infantry.
  - Armor and gear are rigid-weighted (100% to one bone) inside one merged mesh per unit. A few baked helper bones handle plates that span joints.
  - Team color comes from a mask channel times a per-instance shader uniform.
  - The skeleton comes from Mixamo's auto-rigger on our base body.
  - Animation mixes selected Mixamo clips with custom clips. All clips are baked onto that one skeleton in Blender and shared as a single `AnimationLibrary` (`anims.glb`).
  - Godot's humanoid retargeting is kept in reserve for third-party libraries only.
  - Details: `docs/3d-art-direction.md`.
- **Vehicles:** rigid part hierarchies (hull, turret, barrel, wheels). No skeletons.
- **Buildings.**
  - A modular military-industrial kit on a shared trim sheet.
  - The mesh base matches the building's sim footprint outline.
  - Power, damage, and upgrade states come from shader uniforms and small child nodes.
- **Terrain:** generated at mission load from map-data regions, with CC0 ground textures and props. Terrain art never becomes gameplay truth.
- **Readability effects:** particles, muzzle flashes, smoke, lights, and selection rings.
- **Scripted Blender (bpy) steps** handle repeatable cleanup and export: bone stripping, gear parenting, footprint outline export, GLB export.
- **Provenance:** stable asset naming and manifest keys, plus provenance notes for every Mixamo clip and CC0 asset.

The old 2D sprite atlases and 8-direction frame tools are legacy. Retire them once 3D reaches parity.

## Debugging and Developer Tools

Current development tooling includes an internal in-game map editor/tuner overlay. A plain C# editor session owns the mutable tool state, diagnostics, keyboard selection, creation/deletion/resize edits, dependency-aware warnings, inspector field edits, undo/redo snapshots, JSON export path, edited mission/map materialization, validation, and guarded save data. The Godot overlay owns rendering and input. It reads the current mission, map, content, and simulation state, pauses live simulation while edit mode is active, exposes a visible tool palette, supports drag-created markers and terrain regions, copies/prints reviewable JSON snippets, can preview-and-write only owned marker/region/object arrays back to source JSON with a `.bak`, and keeps authored map truth in content data while making terrain, markers, map objects, pylon ranges, resource wells, and wall links visible during playtest tuning. Returning from edit mode reinitializes the mission from in-memory edits instead of carrying live unit/building state across the boundary.

The project should continue growing:

- debug overlay for entity IDs, power, passability, and AI state
- F5 map editor/tuner hardening for route proof, live validation breadth, and map-object/bridge authoring ergonomics
- mission event log
- deterministic test map or scenario
- fast restart hotkey in development builds
- simple balance dump for units/buildings
- screenshot-friendly debug mode for playtest notes
- later modder-facing map editor packaging and safe map-pack loading once internal map authoring is trustworthy

These tools matter because RTS bugs are often state bugs, not visual bugs.

## Performance Direction

Do not optimize prematurely, but avoid obvious traps.

Early rules:

- avoid per-frame full-map scans for common systems
- use spatial queries or simple indexing once unit counts grow
- keep UI updates tied to state changes where practical
- keep particles bounded
- keep pathfinding simple and visible before scaling unit counts
- pathfinding stays simulation-owned with no third-party dependency yet:
  - a cached fine grid that updates when buildings, walls, or bridges change
  - a clearance field for per-unit radius
  - reachability labels and straightened paths
  - a per-tick path-request budget
  - group orders path once per radius class, and members follow formation offsets with separation steering
  - A navmesh library needs explicit approval first.
- **3D presentation targets, with the Steam Deck as the floor:**
  - 60 fps in typical battles on the Deck preset
  - at least 40 fps, never below 30, at a stress bar of 300 animated infantry, 30 vehicles, 40 buildings, and about 5k tree/rock instances
- **3D presentation rules:**
  - one merged mesh and material per unit
  - about 25 runtime bones
  - no animation offscreen, reduced animation rate at far zoom
  - optionally, far-zoom strategic icons that replace unit models, decided on the Deck in the P0 scale lineup
  - `MultiMesh` for props and selection rings
  - VRAM-compressed textures

The first playable mission does not need hundreds of units. It needs clear systems and satisfying pressure. The stress bar exists so later missions never hit a rendering wall.

## Platform and Packaging Direction

First target:

- Windows desktop packaged build
- Steam Deck as the performance floor from the first 3D spike onward. Measure the native Linux export and the Windows build under Proton, then pick the ship path from the numbers.

Later targets:

- itch.io downloadable build
- Steam demo/build, with a shipped Steam Input layout for the Deck
- desktop Linux (it largely comes along with Deck support)
- macOS only when signing/notarization complexity is worth it

Browser/web is not the default if Godot C# remains the technical path.

## Open Architecture Decisions

- Exact repo layout after Godot scaffolding.
- Whether JSON remains the long-term content data format or Godot resources earn their place later.
- Exact footprint outlines and the small placement buffer value for each building (Track A of `docs/3d-presentation-plan.md`).
- Exact grunt replacement cost relative to basic combat units.
- Whether the cached simulation grid should later be replaced by a navmesh (for example DotRecast), a flow-field layer, or a dedicated RTS pathfinding helper.
- Steam Deck renderer (Forward+ or Mobile) and ship path (native Linux or Proton), decided by the crowd spike.
