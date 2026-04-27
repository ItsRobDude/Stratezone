# AGENTS.md

Guidance for Codex and other coding agents working in this repository.

## Project Identity

Stratezone is a mission-first colony RTS, not a pure base-builder and not a pure Command & Conquer clone.

The durable design rule is:

> RimWorld for colony stakes. Dominion/C&C-style RTS for battlefield control.

Keep the base alive, readable, and tactically relevant. Avoid adding deep simulation just because it is interesting.

Current locked vision choices:

- Workers are important recruitable units. They are useless in combat, should flee from attackers, and losing them should hurt because replacing them costs resources.
- Levels are fresh authored scenarios, closer to a classic RTS campaign than a persistent colony sim.
- Combat uses individual units with varied strength, specialty, and cost. Some units should perform best in groups; elite or expensive units can stand alone better.
- Resource gathering uses Dominion-style limited wells: place a refinery/extractor over a resource node, protect it, and expect it to eventually run out.
- The first enemy faction is a private military force with similar technology/troops, reskinned and tuned differently.
- The first mission has a controllable on-map commander troop who must be defended. He is a fragile fail-condition unit with a pistol, and keeping him at home base should be a sensible strategy.
- Fog of war uses classic black unexplored areas. Once explored, an area stays visible rather than returning to gray shroud.
- First prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower.
- First prototype units are Worker, Rifleman, Guardian, Rover, and Commander.
- Colony Hub is where new units spawn.
- Barracks controls what can be trained by level, allowed troop count, and upgrade unlocks.
- Power Plant generates power in a small radius. Underpowered buildings shut off.
- Pylons link power over long distances.
- Defense Towers create an energy wall when placed near another compatible Defense Tower, blocking enemy pathing until a tower is destroyed.
- Gun Towers and Rocket Towers can also act as Defense Tower wall anchors, but cost more because they are armed.
- Enemy bases should rebuild and produce from limited resources, racing the player for additional wells, but Level 1 should do this slower than normal.
- Level 1 is a small 5-10 minute top-down mission in bright readable meadows/fields with light forest.
- Tanks are not normally trainable in Level 1, but destroying either player's or enemy's Colony Hub reveals a tank without changing win/loss conditions by itself.
- Ancient-tech mystery is out of scope for now.
- Visual tone is military-industrial with restrained near-future utility tech.
- Failure criteria can vary per mission: commander killed, main base destroyed, convoy failed, transport failed, objective timer expired, or combinations of those.

## Source of Truth

Read the focused docs before making broad changes:

1. `docs/project-identity.md` for player promise, design pillars, and scope boundaries.
2. `docs/technical-architecture.md` for stack direction and system ownership.
3. `docs/system-contracts.md` for first-pass prototype behavior contracts.
4. `docs/engineering-standards.md` for coding, validation, and contribution standards.
5. `docs/first-landing-mission-spec.md` for Level 1 details.
6. `docs/product-roadmap.md` for milestone direction and unresolved decisions.
7. `docs/release-roadmap.md` for public build, itch.io, and Steam readiness.
8. Code and assets once implementation begins.

If code and docs drift, fix the drift deliberately. Do not silently turn current implementation accidents into product truth.

## Current Stage

This repo is in pre-production / prototype planning. Prefer small documentation, planning, and scaffolding changes over broad implementation until the first Godot project exists.

Locked first prototype technical direction:

- Engine: Godot 4
- Language: C#
- Target platforms: Windows first, then itch.io/Steam-friendly desktop packaging
- Visual style: readable 2D top-down military-industrial sci-fi

If this direction changes, update this file and the README in the same pass.

## Product Guardrails

- Start with one playable mission, not a full sandbox.
- Keep colony systems light and legible.
- Make power/build radius central to base expansion.
- Underpowered buildings should shut off, not merely lose bonuses.
- Power Plants generate local power; Pylons extend/link it over long distances.
- Treat Defense Tower wall links as path-blocking gameplay, not cosmetic VFX.
- Make workers valuable as expensive recruitable units, not life-sim colonists.
- Workers must not be given combat utility in the first prototype; fleeing is their defensive behavior.
- Building placement should not show a visible grid, but structures need spacing/buffer constraints so small maps cannot be over-crammed.
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
- `docs/scaffold-plan.md` for first Godot scaffold expectations.
- `docs/first-landing-mission-spec.md` for Level 1 mission changes.
- `docs/system-contracts.md` for first-pass system behavior changes.
- `docs/engineering-standards.md` for validation and process changes.
- `docs/product-roadmap.md` for milestone status and open decisions.
- `docs/release-roadmap.md` for public-build or storefront-readiness changes.
- Future design docs only when a topic needs more detail than these files can hold.

Keep docs concrete. Prefer player verbs, systems, constraints, and examples over vague mood language.

## Validation

Before calling implementation work done, run the relevant project checks once they exist. Until tooling exists, at minimum report:

- What files changed.
- Whether the change is docs-only or implementation.
- Any assumptions made about engine, scope, or platform.
