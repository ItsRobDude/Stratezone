# Stratezone

**Stratezone** is an early-stage colony RTS concept: a hostile-world strategy game where the player builds a fragile expedition outpost, manages workers and power networks, and commands military units through sci-fi battlefield missions.

The target feel is **RimWorld-style colony pressure** meeting **late-90s sci-fi RTS mission design**. The base should feel alive, vulnerable, and useful, while combat stays readable and command-driven.

## Documentation Map

- [Project Identity](docs/project-identity.md): vision, design pillars, player promise, and scope boundaries.
- [Technical Architecture](docs/technical-architecture.md): intended stack, system ownership, simulation boundaries, and repo shape.
- [Engineering Standards](docs/engineering-standards.md): contributor process, code quality rules, validation expectations, and AI-assisted development guardrails.
- [Product Roadmap](docs/product-roadmap.md): milestone direction and open decisions.

## Current Intent

- Build a playable prototype before chasing full-game scope.
- Focus on one strong mission loop: land, stabilize, expand, survive, strike.
- Keep colony simulation light enough to support RTS pacing.
- Keep visuals readable and production-friendly: top-down or slight-isometric 2D, simple silhouettes, strong UI, particles, and terrain texture work.
- Prefer systems that can become a real packaged indie game for itch.io or Steam.

## Core Fantasy

You command an expedition on a dangerous alien world. Your outpost depends on workers, generators, pylons, extractors, habitats, defenses, and military production. Enemy forces, storms, resource pressure, and colony failures push back while you scout, defend, and dismantle hostile infrastructure.

## Primary Player Verbs

- Build colony structures.
- Extend power and build radius.
- Extract resources from map-controlled wells.
- Protect workers and critical infrastructure.
- Command squads, vehicles, artillery, and commanders.
- Repair after raids, storms, and sabotage.
- Scout enemy positions and strike weak infrastructure.

## Prototype Direction

The first playable target is a single mission, internally called **First Landing**:

1. Deploy a command crawler into a colony hub.
2. Build power, an extractor, and basic defenses.
3. Survive early enemy or environmental pressure.
4. Repair or activate a mission objective.
5. Push outward, cut enemy infrastructure, and destroy a forward base.

See [docs/product-roadmap.md](docs/product-roadmap.md) for the working roadmap.
