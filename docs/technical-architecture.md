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

The current intended path is:

- **Engine:** Godot 4
- **Language:** C# for game logic and larger systems
- **Target:** native desktop first, Windows as the first practical platform
- **Distribution direction:** itch.io and Steam-friendly packaged builds
- **Visual mode:** readable 2D top-down or slight-isometric military-industrial presentation

This is a direction, not a final locked stack. If the project pivots to another engine, update this document, `README.md`, and `AGENTS.md` in the same pass.

## Core Philosophy

Godot should present the game. Plain C# systems should own the game.

That means:

- scenes and nodes should not become the only source of gameplay truth
- core systems should be testable without launching a full visual scene where practical
- data should be explicit and inspectable
- simulation state should be serializable
- UI should display simulation truth, not invent parallel truth

The project should avoid custom engine work. Stratezone is hard because it is a systems game, not because it needs a clever renderer.

## Practical Constraints

The architecture is shaped around these realities:

- the project is likely to be built with heavy AI assistance
- the primary development machine is Windows
- the game may eventually be sold as a downloadable desktop indie game
- art production should assume AI-assisted concepts plus Photoshop cleanup, not a large art team
- the first playable mission matters more than future-perfect engine abstraction
- levels are fresh authored scenarios rather than a persistent colony campaign
- resource gathering uses powered refinery/extractor buildings on map-controlled wells, not survival-style hauling
- fog of war uses black unexplored areas; explored areas stay visible after scouting rather than reverting to gray shroud
- building placement should feel freeform, with no visible grid, while still enforcing footprint buffers and spacing constraints
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
  workers/
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

## Simulation Boundary

The simulation owns all rules that must survive save/load and all decisions the player should be able to trust.

Simulation-owned systems:

- entity identity and lifetime
- map occupancy and passability
- resources and extraction
- power networks and build radius
- workers as expensive recruitable units
- construction and repair
- unit stats and combat resolution
- projectiles or hitscan rules, if used
- enemy raid timing and objective AI
- mission objectives and win/loss state
- mission-specific failure criteria
- environmental events
- fog/scouting truth
- saveable game state

Presentation-owned systems:

- sprites
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

## Game Loop Direction

Use a deterministic or mostly deterministic simulation tick where practical.

Recommended flow:

1. Input layer converts mouse/keyboard/gamepad events into game commands.
2. Command layer validates whether the action is legal.
3. Simulation tick applies commands, jobs, AI, combat, power updates, and events.
4. Presentation reads the updated simulation state and animates toward it.
5. UI displays current state, warnings, objectives, and selected entity actions.

Do not tie combat or economy outcomes to animation completion unless there is a deliberate reason.

## Entity Model

Start simple. Do not introduce a heavyweight ECS unless the project earns it.

Recommended early model:

- stable entity IDs
- explicit data records for individual units, buildings, resources, and map objects
- small domain services for systems like power, economy, combat, and workers
- content definitions for base stats and build costs

This gives us enough structure to scale without turning the first prototype into framework archaeology.

## Core Systems

### Map System

Owns terrain, buildability, passability, resource wells, starting zones, neutral objects, and mission-specific markers.

Early requirements:

- freeform-feeling placement with hidden footprint/buffer constraints
- passability checks
- resource-well positions
- base start area
- enemy base area
- objective markers

### Power System

Owns powered territory, pylon/generator links, outage consequences, and build radius.

Power should be a core identity system, not a decorative requirement.

Early requirements:

- structures can require power
- pylons extend territory
- disconnected buildings lose function or degrade
- power overlay is readable

### Economy System

Owns materials, extraction rates, storage, and build costs.

Early requirements:

- resource well extractor
- material income over time
- spend materials on construction and units
- consequences when extractors are destroyed or unpowered

### Worker System

Owns recruitable workers, construction, repair, worker availability, and worker consequence tracking.

Early requirements:

- expensive worker units
- resource-cost replacement
- build tasks
- repair tasks
- worker danger or casualty consequences
- flee behavior when threatened
- simple priority rules

Avoid deep personality simulation in the first prototype. Workers should behave like costly utility troops with no combat value: they can die under attack, should flee when threatened, and losing them is a meaningful economic and tactical setback.

### Combat System

Owns attack legality, damage, armor if used, range, cooldowns, projectiles, death, and target selection.

Early requirements:

- individual infantry/security unit
- group-benefit behavior for low-cost units where useful
- higher-cost specialist or heavy unit that can operate with less support
- scout rover or light vehicle
- turret
- enemy attacker
- building damage
- enemy infrastructure as valid targets
- first enemy faction can reuse the player-like technology set with different visuals, costs, timings, or tactical emphasis

### Mission System

Owns objectives, mission phases, scripted events, fresh-scenario setup, and win/loss state.

Early requirements:

- deploy colony hub
- build extractor
- survive raid
- destroy all enemies on the map
- defend an on-map commander unit
- fail if mission-specific critical conditions are broken, such as colony hub destroyed, commander killed, transport lost, convoy escaped, or objective timer expired

### Event Director

Owns timed and condition-based pressure events.

Early requirements:

- raid warning
- environmental event warning
- event start/end
- event consequences

Events should be inspectable and tunable. Avoid opaque random chaos early.

### AI System

Start with simple scripted or director-driven enemy behavior.

Early requirements:

- enemy waves
- patrols or guards
- attack priority for visible player structures
- retreat or regroup only if easy

Do not build skirmish-grade AI before the authored mission loop works.

## Content Data

Prefer explicit content definitions for:

- units
- buildings
- weapons
- resources
- events
- missions
- faction modifiers

The first pass can be simple JSON, CSV, or Godot resources. Choose the format after the Godot scaffold, but keep content data separate from hardcoded scene behavior.

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
- worker/task state
- commander state when the mission uses an on-map commander
- objective progress
- event director state
- fog/scouting state

Save data should not depend on node paths as canonical identity.

Because levels start as fresh scenarios, save/load should prioritize in-mission reliability before cross-mission persistence. Campaign progression can track completed scenarios later without treating the colony as continuous.

## Input and UI

Keep input mapping explicit.

Likely early controls:

- left click select
- drag select
- right click move/attack/context
- keyboard camera pan
- mouse edge pan if desired
- mouse wheel zoom
- hotkeys for build categories later
- escape/cancel

Use Godot UI for HUD and panels unless a later architecture change justifies a different layer.

Early HUD surfaces:

- resource count
- power status
- worker status
- selected entity card
- build menu
- objective tracker
- event warnings
- minimap later, not required first

## Art and Asset Pipeline

Early assets should favor readability and iteration speed.

Recommended approach:

- AI-assisted concept exploration
- Photoshop cleanup and transparent PNG exports
- simple top-down sprites
- minimal animation at first
- particles, muzzle flashes, smoke, lights, and selection rings for motion/readability
- stable asset naming and manifest keys once the asset set grows

Avoid making the first prototype depend on full 3D modeling, complex animation, or large sprite sheets.

## Debugging and Developer Tools

The project should eventually include:

- debug overlay for entity IDs, power, passability, and AI state
- mission event log
- deterministic test map or scenario
- fast restart hotkey in development builds
- simple balance dump for units/buildings
- screenshot-friendly debug mode for playtest notes

These tools matter because RTS bugs are often state bugs, not visual bugs.

## Performance Direction

Do not optimize prematurely, but avoid obvious traps.

Early rules:

- avoid per-frame full-map scans for common systems
- use spatial queries or simple indexing once unit counts grow
- keep UI updates tied to state changes where practical
- keep particles bounded
- keep pathfinding simple and visible before scaling unit counts

The first playable mission does not need hundreds of units. It needs clear systems and satisfying pressure.

## Platform and Packaging Direction

First target:

- Windows desktop packaged build

Later targets:

- itch.io downloadable build
- Steam demo/build
- Linux if the Godot path stays straightforward
- macOS only when signing/notarization complexity is worth it

Browser/web is not the default if Godot C# remains the technical path.

## Open Architecture Decisions

- Final Godot version and C# setup.
- Exact repo layout after Godot scaffolding.
- JSON vs Godot resources for content data.
- Exact building footprint/buffer values for constrained maps.
- Worker replacement cost relative to basic combat units.
- Whether to use a third-party pathfinding helper or stay engine-native/simple first.
