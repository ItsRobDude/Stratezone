# Stratezone Project Identity

This document is the source of truth for Stratezone's product identity, player promise, and design boundaries.

Stratezone should become a systems-forward colony RTS about turning a fragile off-world landing into a defensible war outpost. It should feel tense, readable, industrial, and a little strange without becoming an opaque sim or a nostalgic clone.

The intended player feeling is:

- under pressure, but not overwhelmed
- clever for cutting enemy infrastructure instead of only brute-forcing fights
- protective of the outpost and its workers
- rewarded for scouting, preparation, and recovery
- grounded in gritty planetary sci-fi rather than flashy space fantasy

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
- named or counted workers with meaningful colony value
- resource extraction from map-controlled wells
- military units that are commanded directly
- enemy infrastructure that can be scouted, cut, captured, disabled, or destroyed
- mission maps that tell small sci-fi war stories through objectives and pressure events

The early goal is not a giant sandbox. The early goal is one authored mission that proves the core fantasy:

> Keep a fragile colony alive long enough to turn it into a war machine.

## Design Pillars

### 1. Colony stakes, RTS control

The colony should matter, but the player should still feel like an RTS commander.

Workers, power, supply, repairs, and morale should create pressure. They should not bury the player under life-sim detail.

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

### 3. Readable sci-fi

The game should use strong silhouettes and clear battlefield language.

At normal zoom, the player should quickly understand:

- this is a worker
- this is a soldier squad
- this is a rover or tank
- this is artillery
- this is a generator
- this is a pylon
- this is a resource well
- this line or glow means powered territory

Small, clear, slightly chunky assets are better than detailed art that collapses into noise.

### 4. Pressure creates decisions

Storms, raids, sabotage, supply failures, and ancient-tech disturbances should make the player choose.

Good pressure asks:

- repair now or keep building?
- defend the extractor or the habitat?
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

Sandbox, procedural generation, and large campaigns can come later if the mission loop earns them.

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
- a pure Command & Conquer-style tank-spam game
- a full 4X game
- a multiplayer-first RTS
- a giant procedural sandbox before authored missions work
- an automation game where combat is secondary
- a lore encyclopedia disguised as a prototype
- a tech-demo engine project

It can borrow feelings and lessons from older games. It should not depend on copying their exact factions, units, art, names, maps, or story.

## Working Tone

The tone should be industrial frontier sci-fi:

- harsh alien terrain
- modular outpost structures
- utility vehicles and expedition hardware
- military logistics under stress
- ancient buried technology as a pressure source
- human-scale vulnerability inside a larger war

Avoid making everything too sleek, too magical, or too cosmic. The setting should feel like people landed machinery on a hostile world and now have to survive the consequences.

## Open Vision Questions

These are intentionally unresolved:

- Is Stratezone primarily mission-based, campaign-based, or mission-based first with later sandbox?
- Are workers individually named characters, anonymous worker counts, or a hybrid?
- Is the world mostly human factions fighting each other, or are non-human factions central?
- How weird should ancient technology get before it stops feeling grounded?
- Should the colony persist between missions, or does each mission start fresh?
- Should combat use individual units, squads, or a mix?
- Should the midlevel twist usually be environmental, enemy-driven, or ancient-tech-driven?
