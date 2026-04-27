# AGENTS.md

Guidance for Codex and other coding agents working in this repository.

## Project Identity

Stratezone is a mission-first colony RTS, not a pure base-builder and not a pure Command & Conquer clone.

The durable design rule is:

> RimWorld for colony stakes. Dominion/C&C-style RTS for battlefield control.

Keep the base alive, readable, and tactically relevant. Avoid adding deep simulation just because it is interesting.

Current locked vision choices:

- Workers are important recruitable units. They can die like troops, but losing them should hurt because replacing them costs resources.
- Levels are fresh authored scenarios, closer to a classic RTS campaign than a persistent colony sim.
- Combat uses individual units with varied strength, specialty, and cost. Some units should perform best in groups; elite or expensive units can stand alone better.
- Resource gathering uses Dominion-style wells: place a refinery/extractor over a money-making node and protect it.
- The first enemy faction is human with similar technology/troops, reskinned and tuned differently.
- The first mission has an on-map commander troop who must be defended, and keeping him at home base should be a sensible strategy.
- Fog of war is in scope.
- Ancient-tech mystery is out of scope for now.
- Visual tone is military-industrial with restrained near-future utility tech.
- Failure criteria can vary per mission: commander killed, main base destroyed, convoy failed, transport failed, objective timer expired, or combinations of those.

## Source of Truth

Read the focused docs before making broad changes:

1. `docs/project-identity.md` for player promise, design pillars, and scope boundaries.
2. `docs/technical-architecture.md` for stack direction and system ownership.
3. `docs/engineering-standards.md` for coding, validation, and contribution standards.
4. `docs/product-roadmap.md` for milestone direction and unresolved decisions.
5. Code and assets once implementation begins.

If code and docs drift, fix the drift deliberately. Do not silently turn current implementation accidents into product truth.

## Current Stage

This repo is in pre-production / prototype planning. Until the stack is locked, prefer small documentation, planning, and scaffolding changes over broad implementation.

Current likely technical direction:

- Engine: Godot 4
- Language: C# if targeting native desktop from the start
- Target platforms: Windows first, then itch.io/Steam-friendly desktop packaging
- Visual style: readable 2D top-down or slight-isometric military-industrial sci-fi

If this direction changes, update this file and the README in the same pass.

## Product Guardrails

- Start with one playable mission, not a full sandbox.
- Keep colony systems light and legible.
- Make power/build radius central to base expansion.
- Make workers valuable as expensive recruitable units, not life-sim colonists.
- Prefer infrastructure strikes over simple unit-spam victory.
- Keep factions, lore, and unit rosters small until the core loop is fun.
- Do not add ancient-tech systems, mystery artifacts, or alien-tech progression unless the user reopens that scope.
- Do not add multiplayer before the single-player loop is proven.
- Do not add complex procedural generation before one authored mission works.

## Architecture Guardrails

Keep simulation state separate from presentation.

- Simulation owns resources, workers, jobs, power, combat rules, events, mission objectives, AI, and saveable state.
- Presentation owns sprites, animation, particles, camera, sound, and UI rendering.
- Content data owns units, buildings, factions, missions, maps, event definitions, and balance values.

Avoid burying gameplay rules directly inside scene/UI code. The game should be testable and tunable without opening every visual object.

Before adding a new dependency, framework, content pipeline, or tool, check whether it meaningfully improves the first playable mission. If not, document it as a later option instead of adding it now.

## Documentation Expectations

When adding or changing game direction, update the smallest relevant doc:

- `README.md` for top-level identity and current direction.
- `docs/project-identity.md` for player promise and design boundaries.
- `docs/technical-architecture.md` for stack or system-boundary changes.
- `docs/engineering-standards.md` for validation and process changes.
- `docs/product-roadmap.md` for milestone status and open decisions.
- Future design docs only when a topic needs more detail than these files can hold.

Keep docs concrete. Prefer player verbs, systems, constraints, and examples over vague mood language.

## Validation

Before calling implementation work done, run the relevant project checks once they exist. Until tooling exists, at minimum report:

- What files changed.
- Whether the change is docs-only or implementation.
- Any assumptions made about engine, scope, or platform.
