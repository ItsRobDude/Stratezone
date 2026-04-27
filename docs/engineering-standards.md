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

## Source of Truth Hierarchy

Follow this order when making decisions:

1. `docs/project-identity.md`
2. `docs/technical-architecture.md`
3. `docs/system-contracts.md`
4. `docs/engineering-standards.md`
5. `docs/first-landing-mission-spec.md`
6. `docs/product-roadmap.md`
7. `docs/release-roadmap.md`
8. implementation code and assets

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

There are no validation commands yet because the engine project has not been scaffolded.

When tooling exists, this repo should define a small, obvious validation contract such as:

- format check
- C# build/compile check
- unit tests
- Godot project load/export sanity check
- packaged Windows build check when release work begins
- itch.io or Steam upload dry-run/checklist steps once public release work begins

The exact commands should be documented here and exposed from the repo root when possible.

Until then, every implementation closeout should report:

- changed files
- whether the work is docs-only or code/assets
- commands run
- commands not run and why
- assumptions made

## AI-Assisted Development Rules

AI agents should:

- read `AGENTS.md` first
- read focused docs before broad changes
- keep edits scoped
- prefer implementation that matches the existing architecture docs
- update docs when changing source-of-truth behavior
- avoid inventing lore or mechanics outside the current scope
- report verification honestly

AI agents should not:

- add systems just because they sound genre-appropriate
- turn open questions into silent decisions
- hide uncertain implementation under confident prose
- rewrite large docs or code areas unrelated to the request
- introduce multiplayer, procedural campaign, or deep colonist simulation early

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

For code:

- behavior matches the relevant docs or the docs are updated
- validation commands pass or failures are reported honestly
- core rules live in the right ownership layer
- the work can be explained from repo evidence

For gameplay:

- the player-facing result is observable
- failure states are defined
- controls and feedback are understandable
- debug or test coverage exists for risky systems
