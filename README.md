# Stratezone

**Stratezone** is an early-stage mission RTS concept: a hostile-world strategy game where the player builds a fragile expedition outpost, protects important workers, manages power networks, and commands military units through authored battlefield scenarios.

The target feel is **RimWorld-style colony stakes** meeting **late-90s military-industrial RTS mission design**. The base should feel alive, vulnerable, and useful, while combat stays readable and command-driven.

## Documentation Map

- [Project Identity](docs/project-identity.md): vision, design pillars, player promise, and scope boundaries.
- [Technical Architecture](docs/technical-architecture.md): intended stack, system ownership, simulation boundaries, and repo shape.
- [Engineering Standards](docs/engineering-standards.md): contributor process, code quality rules, validation expectations, and AI-assisted development guardrails.
- [Product Roadmap](docs/product-roadmap.md): milestone direction and open decisions.

## Current Intent

- Build a playable prototype before chasing full-game scope.
- Focus on one strong mission-style RTS loop: land, stabilize, expand, survive, strike.
- Keep colony stakes light enough to support RTS pacing; this is not a survival game.
- Keep visuals readable and production-friendly: top-down or slight-isometric 2D, simple silhouettes, strong UI, particles, and terrain texture work.
- Prefer systems that can become a real packaged indie game for itch.io or Steam.

## Core Fantasy

You command an expedition on a dangerous frontier world. Each level is a fresh scenario. Your outpost depends on a Colony Hub, recruitable workers, barracks capacity, power plants, extractors/refineries, defenses, and military production. Enemy forces, resource pressure, and mission threats push back while you scout, defend, escort, intercept, and dismantle hostile infrastructure.

## Primary Player Verbs

- Build colony structures.
- Extend power and build radius.
- Build refineries/extractors on map-controlled resource wells.
- Protect expensive workers who flee from combat, the on-map commander, and critical infrastructure.
- Command individual units, vehicles, artillery, commanders, and units that perform better in groups.
- Repair after raids, storms, and sabotage.
- Scout enemy positions and strike weak infrastructure.

## Prototype Direction

The first playable target is a single fresh-scenario mission, internally called **First Landing**:

1. Deploy a command crawler into a colony hub.
2. Build a power plant, barracks, an extractor/refinery, and basic defenses.
3. Defend against early enemy or objective pressure.
4. Build enough force to push outward through fog of war.
5. Destroy all enemies on the map.

First prototype roster:

- **Buildings:** Colony Hub, Barracks, Power Plant, Extractor/Refinery.
- **Units:** Worker, Rifleman, Guardian, Rover, Commander.

See [docs/product-roadmap.md](docs/product-roadmap.md) for the working roadmap.
