# Stratezone Project Identity

This document is the source of truth for Stratezone's product identity, player promise, and design boundaries.

Stratezone should become a systems-forward mission RTS about turning fragile battlefield landings into defensible war outposts. It should feel tense, readable, military-industrial, and practical without becoming an opaque sim or a nostalgic clone.

The intended player feeling is:

- under pressure, but not overwhelmed
- clever for cutting enemy infrastructure instead of only brute-forcing fights
- protective of the outpost, its expensive workers, and its commander
- rewarded for scouting, preparation, and recovery
- grounded in military-industrial near-future warfare rather than flashy space fantasy

## Documentation Role

- **Doc role:** Active source of truth for vision, identity, tone boundaries, and product-level design intent.
- **Owns:** player promise, design pillars, scope boundaries, experience philosophy, and high-level tradeoffs.
- **Does not own:** exact architecture, validation commands, unit stat formulas, final mission scripts, or store release readiness.
- **Read when:** deciding whether a mechanic, feature, faction, art direction, or mission idea belongs in Stratezone.
- **Do not read for:** exact implementation paths, code layout, or build commands.

## Product Goal

Stratezone should be a playable, packageable indie strategy game built around:

- real-time base construction
- powered expansion networks
- recruitable workers with meaningful mission and economy value
- refinery/extractor placement on map-controlled money wells
- individual military units that are commanded directly
- enemy infrastructure that can be scouted, cut, captured, disabled, or destroyed
- mission maps that tell small military-industrial war stories through objectives and pressure events

The early goal is not a survival game, giant sandbox, or persistent colony sim. The early goal is one authored fresh-scenario mission that proves the core fantasy:

> Defend a fragile outpost long enough to turn it into a war machine.

## Design Pillars

### 1. Colony stakes, RTS control

The colony should matter, but the player should still feel like an RTS commander.

Workers, power, production, repairs, and mission objectives should create pressure. Workers are expensive recruitable units: it is normal for them to die if the base is under attack, but losing them should hurt because replacing them costs resources and slows the base.

The correct balance is:

- colony systems create stakes and consequences
- RTS controls create agency and momentum
- mission objectives create direction

### 2. Infrastructure is the battlefield

Stratezone should reward players for understanding how bases work.

Good play should often mean:

- cutting a power pylon
- destroying an extractor
- disabling a radar station
- capturing a neutral repair platform
- forcing the enemy to fight without production, vision, or defenses

Victory should not only come from making the largest unit blob.

### 3. Readable Military-Industrial Sci-Fi

The game should use strong silhouettes and clear battlefield language.

At normal zoom, the player should quickly understand:

- this is a worker
- this is the commander
- this is a soldier
- this unit works best grouped with others
- this elite or expensive unit can hold ground alone
- this is a rover or tank
- this is artillery
- this is a generator
- this is a pylon
- this is a resource well
- this line or glow means powered territory

Small, clear, slightly chunky assets are better than detailed art that collapses into noise.

### 4. Pressure creates decisions

Storms, raids, sabotage, supply failures, convoy threats, transport objectives, and enemy maneuvers should make the player choose.

Good pressure asks:

- repair now or keep building?
- defend the refinery/extractor or the commander?
- expand to the second well or harden the first base?
- push the enemy before the next storm or wait for artillery?

Bad pressure simply punishes the player randomly. Avoid that.

### 5. Authored missions before endless systems

Stratezone should prove itself in authored missions first.

Authored missions let the game test:

- pacing
- objective clarity
- tutorial flow
- base layout pressure
- enemy infrastructure design
- environmental twists

Sandbox, procedural generation, persistence, and large campaigns can come later if the mission loop earns them.

### 6. Maintainable ambition

The project should be ambitious in the experience, not reckless in implementation.

Prefer:

- one strong faction before four weak ones
- one mission with a real arc before a shallow campaign
- data-driven units and buildings once patterns are clear
- simple AI director events before complex emergent storytelling
- practical art pipelines over asset-production fantasies

## What Stratezone Is Not

Stratezone should not become:

- a direct remake of any older RTS
- a pure RimWorld-style colony simulator
- a survival game
- a pure Command & Conquer-style tank-spam game
- a full 4X game
- a multiplayer-first RTS
- a giant procedural sandbox before authored missions work
- a persistent colony sim before fresh campaign scenarios work
- an automation game where combat is secondary
- a lore encyclopedia disguised as a prototype
- a tech-demo engine project
- an ancient-artifact mystery game unless that scope is deliberately reopened

It can borrow feelings and lessons from older games. It should not depend on copying their exact factions, units, art, names, maps, or story.

## Working Tone

The tone should be military-industrial frontier sci-fi:

- harsh alien terrain
- modular outpost structures
- utility vehicles and expedition hardware
- military logistics under stress
- human-scale vulnerability inside a larger war
- restrained near-future utility technology, such as rocket towers, laser-armed troops, sensors, drones, and powered infrastructure

Avoid making everything too sleek, too magical, too cosmic, or too alien-tech-driven. The setting should feel like people landed machinery on a hostile world and now have to survive military consequences.

## Settled Vision Choices

- Stratezone is mission RTS first, not survival sandbox first.
- Each level starts as a fresh scenario, closer to a classic RTS campaign structure than a persistent colony.
- Workers are expensive recruitable units. They can die like troops, but replacing them costs resources and slows the outpost.
- Combat uses individual units with varied cost, strength, and specialty.
- Some units should be stronger when grouped or supported; expensive specialist units may stand a better chance alone.
- Resource gathering uses refinery/extractor buildings placed over money-making wells.
- The first enemy faction is human with similar technology/troops, reskinned and tuned differently.
- The first mission includes an on-map commander troop who must be defended.
- Fog of war is in scope.
- Ancient tech is omitted for now.
- The sci-fi tone is military-industrial with restrained future utility tech.
- Failure criteria can vary by mission, including commander killed, main base destroyed, transport lost, convoy objective failed, or combined conditions.

## Open Vision Questions

These are intentionally unresolved:

- Is the world mostly human factions fighting each other, or are non-human factions central?
- How expensive should a replacement worker be relative to basic combat units?
- Should workers have light self-defense, no attack, or a weak utility sidearm?
- Should the midlevel twist usually be environmental, enemy-driven, logistics-driven, or objective-driven?
- How many mission archetypes belong in the first campaign slice?
