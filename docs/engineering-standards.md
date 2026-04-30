# Stratezone Engineering Standards

This document defines how Stratezone code, content, tools, and documentation should be written and maintained.

Its purpose is to keep Stratezone understandable as a systems-heavy game built with AI assistance. A prototype that works once but cannot be understood later is not a good Stratezone prototype.

## Documentation Role

- **Doc role:** Active source of truth for contributor process, code quality expectations, validation standards, and review/reporting norms.
- **Owns:** engineering discipline, tool expectations, source-of-truth hierarchy, validation strategy, dependency discipline, and AI-assisted contribution rules.
- **Does not own:** product vision, gameplay balance, architecture boundaries, or final milestone priority.
- **Read when:** adding tooling, writing systems, changing validation, refactoring, or asking Codex/Jules/other agents to implement features.
- **Do not read for:** lore, mission design details, or exact player-facing UI copy.

## Core Engineering Philosophy

Stratezone should be built with boring, explicit engineering choices.

Prefer:

- readable code
- small systems with clear ownership
- explicit data
- simple validation
- practical tests
- stable naming
- clear debug surfaces

Avoid:

- clever architecture that only one run understands
- hidden state coupling
- gameplay rules buried in UI callbacks
- procedural content before authored content works
- dependency sprawl
- AI-generated code that looks plausible but violates docs

## Anti-Drift Rules

These rules exist to prevent common AI-assisted and vibe-coded failure modes.

- No silent scope expansion. New mechanics, factions, lore systems, dependencies, tools, or content types must map to a current milestone, source doc, or explicit user request.
- No undocumented gameplay truth. If a behavior cannot be justified from a doc, update the smallest relevant doc before implementing it.
- No scene-only rules. Godot scenes may present and route commands, but simulation systems must own gameplay outcomes.
- No unrelated cleanup. Do not mix feature work with broad refactors, formatting churn, or speculative architecture cleanup.
- No invented finality. Placeholder balance, placeholder assets, and provisional tools must be labeled as such.
- No hidden failures. If validation cannot run, or only part of a behavior was tested, say that clearly in the closeout.

Practical examples:

- Good: `PowerSystem` decides whether `building_barracks` is powered, and the UI displays that state.
- Bad: a Barracks scene script disables itself because a sprite is outside a visual radius.
- Good: `MissionObjectiveSystem` decides win/loss and emits state for the HUD.
- Bad: a HUD label decides the mission is won.
- Good: content data uses stable IDs such as `unit_worker` and `building_colony_hub`.
- Bad: save data depends on display names or Godot node paths as canonical identity.

## Source of Truth Hierarchy

Follow this order when making decisions:

1. `docs/project-identity.md`
2. `docs/technical-architecture.md`
3. `docs/system-contracts.md`
4. `docs/content-data-spec.md`
5. `docs/engineering-standards.md`
6. `docs/first-landing-mission-spec.md`
7. `docs/product-roadmap.md`
8. `docs/implementation-checklists.md`
9. `docs/release-roadmap.md`
10. implementation code and assets

If code and docs disagree, do not paper over it. Either update the code or update the docs with a clear reason.

## Current Tool Direction

Locked first prototype stack:

- Godot 4
- C#
- Git and GitHub
- Windows-first development
- packaged desktop builds before web/browser builds

Recommended editor setup:

- Godot Editor for scenes, project settings, maps, UI, and runtime checks
- VS Code or JetBrains Rider for C# editing
- Photoshop for asset cleanup

Do not add a second engine, custom renderer, or large framework without a concrete reason tied to the first playable mission.

## Code Organization Standards

Keep code organized by ownership, not by whatever scene happened to need it first.

Expected ownership groups:

- simulation
- presentation
- content loading/validation
- input
- UI
- missions
- tooling/tests

Rules:

- gameplay rules belong in simulation systems
- scene scripts may call simulation commands, but should not secretly become rule engines
- entity IDs should be stable and explicit
- content definitions should be data, not scattered constants across scenes
- save data should serialize simulation truth, not node paths

## File Size and Module Structure

Stratezone should avoid files that become thousand-line knowledge traps. Large files are hard for humans to review, hard for AI agents to patch safely, and easy places for gameplay rules to drift into the wrong layer.

Default trigger for hand-written code:

- **900 lines:** review trigger. Split the file, or leave a clear reason in the closeout for why it should stay together.

Godot scene scripts should be smaller than simulation files when possible. A scene script should wire nodes, route input, and present state. If it starts owning rules for power, economy, combat, construction, fog, objectives, or AI, move that behavior into the simulation layer.

Preferred split pattern for growing systems:

- `*Definition` or content records for tunable values.
- `*State` or entity records for saveable runtime state.
- `*Command` records for player, AI, or mission actions.
- `*System` classes for rule application.
- `*Event` records for emitted simulation facts.
- `*Debug` helpers for inspection surfaces.
- presentation adapters under `presentation/` for Godot node wiring.

Splitting rules:

- split by reason to change, not by arbitrary line count alone
- keep public APIs small and explicit
- avoid `Manager`, `Controller`, or `Utils` files that collect unrelated behavior
- keep content IDs and data schemas close to content loading/validation, not duplicated across gameplay files
- do not create one-file-per-method fragmentation; split only when a new file has a clear owner

Exceptions are allowed for generated code, large static tables, serialized data, or temporary migration files. Mark the exception clearly and prefer a follow-up TODO if the file is expected to shrink.

## Naming Standards

Use names that reveal intent.

Prefer:

- `PowerNetwork`
- `BuildRadius`
- `ResourceWell`
- `WorkerJob`
- `MissionObjective`
- `RaidEvent`
- `UnitDefinition`
- `BuildingDefinition`

Avoid vague names:

- `Manager`
- `Thing`
- `Controller2`
- `DoStuff`
- `TempFinal`

If a name is temporary, mark it as temporary in a doc or TODO with a clear replacement condition.

## Content ID Standards

Use stable lowercase snake_case IDs for content, save references, and tests.

Initial IDs and first-pass content fields are listed in `docs/content-data-spec.md`. Use those IDs for first-pass units, buildings, factions, resources, and `mission_first_landing`.

Rules:

- display names may change without changing IDs
- save data should store IDs, not node paths or display names
- broad use of a new ID should be documented before implementation spreads it across code, data, and scenes

## Localization Standards

Treat localization as an early architecture boundary, even while English is the only supported language.

Rules:

- player-facing strings belong in localization data, not scattered through simulation and scene code
- simulation should return stable message keys plus arguments, with English fallback text only for debugging and migration safety
- presentation resolves message keys into UI text
- content `display_name` values are fallback/readability aids, not canonical player text
- debug-only logs, node names, and test assertion messages may remain English
- implementation closeouts should mention any new player-facing strings and whether they use localization keys

## Dependency Discipline

Every dependency should earn its place.

Before adding a dependency, ask:

- Does Godot already provide enough?
- Does this help the first playable mission?
- Will future Codex runs understand it?
- Does it make debugging easier or harder?
- Is it maintained and boring?

Do not add dependencies for:

- theoretical future multiplayer
- speculative mod support
- procedural generation before authored missions
- UI flourish that can wait
- replacing simple data structures with complex frameworks

## Testing Strategy

Once code exists, prioritize tests for systems where bugs would undermine player trust:

- power network behavior
- build legality
- resource extraction
- worker job assignment
- construction and repair
- combat damage rules
- mission objective state
- save/load round trips
- event director triggers

Visual polish and scene wiring can use lighter smoke checks at first, but simulation behavior should become increasingly testable.

## Validation Contract

Current validation commands:

- `python tools/validate_content.py`
- `dotnet build game/Stratezone.csproj`
- `%LOCALAPPDATA%/Microsoft/WinGet/Packages/GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe --headless --path game --quit`

`tools/validate_content.py` also validates the first-pass English localization catalog under `game/data/i18n/en.json`.

In a fresh shell, `godot --headless --path game --quit` may work if the WinGet command alias is active. If not, use the explicit Godot console path above.

Later validation should add:

- format check
- simulation unit tests
- Godot export sanity check
- packaged Windows build check when release work begins
- itch.io or Steam upload dry-run/checklist steps once public release work begins

The exact commands should be documented here and exposed from the repo root when possible.

Until then, every implementation closeout should report:

- changed files
- whether the work is docs-only or code/assets
- commands run
- manual tests performed
- commands not run and why
- behavior verified from the player or simulation perspective
- known risks or follow-up work
- assumptions made
- player-facing strings added and whether they are localizable
- any hand-written code files left over 900 lines, with the reason they were not split

## AI-Assisted Development Rules

AI agents should:

- read `AGENTS.md` first
- read focused docs before broad changes
- keep edits scoped
- prefer implementation that matches the existing architecture docs
- update docs when changing source-of-truth behavior
- avoid inventing lore or mechanics outside the current scope
- use `docs/implementation-checklists.md` to decide whether milestone work is actually done
- report verification honestly

AI agents should not:

- add systems just because they sound genre-appropriate
- turn open questions into silent decisions
- hide uncertain implementation under confident prose
- rewrite large docs or code areas unrelated to the request
- introduce multiplayer, procedural campaign, or deep colonist simulation early
- claim milestone completion without checklist evidence

## Git and Change Hygiene

- Keep commits or change batches focused.
- Do not mix engine scaffolding, game design changes, and unrelated formatting unless intentionally requested.
- Do not overwrite user-created assets or experiments without explicit approval.
- Prefer adding small docs or code paths over broad rewrites.
- Keep generated files out of Git unless they are required project artifacts.

## Debuggability Standards

Systems-heavy RTS work needs good debug visibility.

As systems are added, prefer debug surfaces for:

- power network state
- resource income
- worker job queues
- AI event timers
- selected entity IDs
- mission objective state
- pathing/passability
- combat target decisions

If a future bug would require guessing at hidden state, add a debug view before the system becomes complicated.

## Done Means

For docs:

- the doc is accurate for the current direction
- links point to real files
- open questions are marked honestly
- no doc claims implemented behavior that does not exist
- source-of-truth and checklist docs agree with each other

For code:

- behavior matches the relevant docs or the docs are updated
- validation commands pass or failures are reported honestly
- core rules live in the right ownership layer
- the work can be explained from repo evidence
- stable IDs are used for content and saveable references

For gameplay:

- the player-facing result is observable
- failure states are defined
- controls and feedback are understandable
- debug or test coverage exists for risky systems
- the relevant milestone checklist has evidence, not just intent
