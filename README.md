# Stratezone

**Stratezone** is an early-stage mission RTS concept: a hostile-world strategy game where the player builds a fragile expedition outpost, protects important workers, manages power networks, and commands military units through authored battlefield scenarios.

The target feel is **RimWorld-style colony stakes** meeting **late-90s military-industrial RTS mission design**. The base should feel alive, vulnerable, and useful, while combat stays readable and command-driven.

## Documentation Map

- [Project Identity](docs/project-identity.md): vision, design pillars, player promise, and scope boundaries.
- [Technical Architecture](docs/technical-architecture.md): intended stack, system ownership, simulation boundaries, and repo shape.
- [Engineering Standards](docs/engineering-standards.md): contributor process, code quality rules, validation expectations, and AI-assisted development guardrails.
- [Product Roadmap](docs/product-roadmap.md): milestone direction and open decisions.
- [Scaffold Plan](docs/scaffold-plan.md): locked Godot 4 C# scaffold target and repo shape.
- [First Landing Mission Spec](docs/first-landing-mission-spec.md): Level 1 target, pacing, map beats, objectives, and fail state.
- [System Contracts](docs/system-contracts.md): first-pass behavior contracts for prototype systems.
- [Content Data Spec](docs/content-data-spec.md): first-pass data shapes for units, buildings, resources, factions, missions, and events.
- [Implementation Checklists](docs/implementation-checklists.md): milestone acceptance checks, done rules, and baseline content IDs.
- [Release Roadmap](docs/release-roadmap.md): path from prototype to itch.io/Steam-ready public builds.

## Current Intent

- Build a playable prototype before chasing full-game scope.
- Focus on one strong mission-style RTS loop: land, stabilize, expand, survive, strike.
- Keep colony stakes light enough to support RTS pacing; this is not a survival game.
- Build the first prototype in Godot 4 with C#.
- Keep visuals readable and production-friendly: top-down 2D, chunky silhouettes, strong UI, particles, and terrain texture work.
- Prefer systems that can become a real packaged indie game for itch.io or Steam.

## Core Fantasy

You command an expedition on a dangerous frontier world. Each level is a fresh scenario. Your outpost depends on a Colony Hub, recruitable workers, barracks-controlled training rules, powered Barracks add-ons, power plants, pylons, extractors/refineries, defensive wall towers, and military production. Enemy forces, resource pressure, and mission threats push back while you scout, defend, escort, intercept, and dismantle hostile infrastructure.

## Primary Player Verbs

- Build colony structures.
- Extend power and build radius.
- Build refineries/extractors on limited resource wells that can run out.
- Expand production with powered Barracks add-ons.
- Protect expensive workers who flee from combat, the on-map commander, and critical infrastructure.
- Command individual units, vehicles, artillery, commanders, and units that perform better in groups.
- Upgrade Defense Towers in place into armed towers when the wall line needs teeth.
- Repair after raids, storms, and sabotage.
- Scout enemy positions and strike weak infrastructure.

## Prototype Direction

The first playable target is a small 5-10 minute fresh-scenario mission, internally called **First Landing**:

1. Start after landing with a deployed base and fragile on-map commander.
2. Build a power plant, pylons, barracks, an extractor/refinery, and basic defenses.
3. Compete for a central resource well and central choke.
4. Scout through black unexplored fog toward a visible private-military enemy.
5. Cut weak infrastructure or upgrade a defensive wall line before pushing the enemy base.
6. Destroy all enemies on the map before the commander is killed.

First prototype roster:

- **Buildings:** Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, Defense Tower.
- **Barracks add-ons:** Armory Annex unlocks Guardian/explosive tech; Vehicle Bay unlocks Rover/heavy-armor capacity.
- **Defensive variants:** Gun Tower and Rocket Tower are preferred in-place upgrades from Defense Towers and keep wall-anchor behavior while adding weaponry.
- **Support and siege:** Med Hall, Logistics / Repair Pad, and Artillery Battery have prototype content records but are not required for the first small mission unless pacing earns them.
- **Units:** Worker, Rifleman, Guardian, Rover, Commander.

For this pass, placeholder shapes are acceptable, art direction can wait until gameplay works, and no story cutscenes are required.

See [docs/product-roadmap.md](docs/product-roadmap.md) for the working roadmap.
