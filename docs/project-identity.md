# Stratezone Project Identity

This document is the source of truth for Stratezone's product identity, player promise, and design boundaries.

Stratezone should become a systems-forward mission RTS about turning fragile battlefield landings into defensible war outposts. It should feel tense, readable, military-industrial, and practical without becoming an opaque sim or a nostalgic clone.

The strongest old-game inspiration is Dominion: Storm Over Gift 3, with useful secondary lessons from classic Command & Conquer-style RTS pacing. The goal is to learn from that fast, readable build-and-command feel without copying exact factions, units, art, maps, names, or story.

The intended player feeling is:

- under pressure, but not overwhelmed
- clever for cutting enemy infrastructure instead of only brute-forcing fights
- protective of the outpost, its expensive grunts, and its commander
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
- recruitable grunts with meaningful mission and economy value
- refinery/extractor placement on limited resource wells that can run out
- pylon-linked power over long distances
- defense towers that create energy walls between paired towers
- individual military units that are commanded directly
- enemy infrastructure that can be scouted, cut, captured, disabled, or destroyed
- mission maps that tell small military-industrial war stories through objectives and pressure events

The early goal is not a survival game, giant sandbox, or persistent colony sim. The early goal is one authored fresh-scenario mission that proves the core fantasy:

> Defend a fragile outpost long enough to turn it into a war machine.

## Design Pillars

### 1. Colony stakes, RTS control

The colony should matter, but the player should still feel like an RTS commander.

Grunts, power, production, repairs, and mission objectives should create pressure. Grunts are expensive recruitable units: it is normal for them to die if the base is under attack, but losing them should hurt because replacing them costs resources and slows the base. Grunts are useless in combat and should flee from attackers rather than fight.

The correct balance is:

- colony systems create stakes and consequences
- RTS controls create agency and momentum
- mission objectives create direction

### 2. Infrastructure is the battlefield

Stratezone should reward players for understanding how bases work.

Good play should often mean:

- cutting power by destroying or disabling a power plant or pylon
- destroying an extractor
- unpowering Barracks upgrades/add-ons to shut off advanced training
- destroying a defense tower to open an energy wall
- disabling a radar station
- capturing a neutral repair platform
- forcing the enemy to fight without production, vision, or defenses

Victory should not only come from making the largest unit blob.

### 3. Readable Military-Industrial Sci-Fi

The game should use strong silhouettes and clear battlefield language.

At normal zoom, the player should quickly understand:

- this is a grunt
- this grunt should be kept away from combat
- this is the commander
- this is a soldier
- this unit works best grouped with others
- this elite or expensive unit can hold ground alone
- this is a powered Barracks add-on
- this is a rover or tank
- this is artillery
- this is a generator
- this is a power plant
- this is a pylon
- this is a defense tower wall anchor
- this is a resource well
- this line or glow means powered territory
- this energy wall blocks enemy movement

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

The first demo should feel like a campaign arc because the missions are authored and ordered, not because the project needs a heavy story layer. Stratezone can remain simulator-like inside each mission: the player should solve real base, power, resource, terrain, and enemy-pressure problems rather than follow scripted cutscenes.

### 6. Maintainable ambition

The project should be ambitious in the experience, not reckless in implementation.

Prefer:

- one strong faction before four weak ones
- one mission with a real arc before a shallow campaign
- data-driven units and buildings once patterns are clear
- simple AI director events before complex emergent storytelling
- practical art pipelines over asset-production fantasies
- AI-assisted art, Illustrator vectorization, cleanup, and turntable-derived directional frames when that is the realistic way to get usable assets

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
- The first demo should be a campaign-like sequence of authored simulation scenarios. Story can stay light until the mission grammar is proven.
- Grunts are expensive recruitable units. They can die like troops, but replacing them costs resources and slows the outpost.
- Grunts are useless in combat and flee from attackers.
- Grunts require player command for construction and repair in the first prototype.
- Grunts read as combat engineers or expedition technicians: valuable utility troops, not life-sim colonists.
- Combat uses individual units with varied cost, strength, and specialty.
- Some units should be stronger when grouped or supported; expensive specialist units may stand a better chance alone.
- Resource gathering uses refinery/extractor buildings placed over scarce limited wells that trickle resources and can deplete.
- The first enemy faction is a private military force with the same basic technology and buildings as the player, reskinned in red or another alternate color until the art direction proves a stronger need.
- Commander units are controllable troops, not abstract heroes. They should be used in the practical Dominion-style RTS spirit: valuable, vulnerable, mission-relevant units on the map.
- The first mission is a small 5-10 minute top-down scenario set in bright readable meadows/fields with light forest.
- First prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower.
- The first defensive structure is the Defense Tower. Two nearby compatible Defense Towers create an energy wall that blocks enemy movement.
- Gun Towers and Rocket Towers are preferred as in-place upgrades from Defense Towers. They keep wall-anchor behavior while adding attacks and higher cost.
- First prototype units are Grunt, Cadet, Rifleman, Guardian, Rover, and Commander.
- Colony Hub is where new units spawn.
- Barracks controls what can be trained by level, allowed troop count, and upgrade unlocks.
- Guardian production should be unlocked by upgrading the Barracks itself. Vehicle Bay remains the first planned powered physical Barracks add-on for Rover/heavy-armor capacity.
- Power Plant generates power in a small radius, and underpowered buildings shut off.
- Pylons link power over long distances.
- The first enemy faction should rebuild and produce from limited resources, racing the player for new wells, but slower than usual in Level 1.
- The first mission objective is to destroy all enemies on the map.
- Fog of war uses black unexplored areas. Explored areas stay visible after scouting instead of reverting to gray shroud, and units/buildings in explored terrain remain visible in real time.
- Tanks are not normally trainable in Level 1, but destroying either player's or enemy's Colony Hub reveals a Medium Tank without changing win/loss conditions by itself.
- The first playable milestone should be playable ugly: placeholder shapes are acceptable, no story cutscenes are required, and art direction can wait until the RTS loop works.
- The first public build should be a demo, not an Early Access or sellable release claim. The current target for that demo is the first five levels, but the project is still pre-demo and does not yet have a reliable finished level-design pipeline.
- Ancient tech is omitted for now.
- The sci-fi tone is military-industrial with restrained future utility tech.
- Failure criteria can vary by mission, including commander killed, main base destroyed, transport lost, convoy objective failed, required Grunt/equipment lost, or combined conditions. Timer-expiry mission failures are not planned for the first demo.
- Quick RTS restart is the expected first-demo failure model. Persistent campaign consequences are later scope.
- Mission starts are authored, with no player-selected loadouts for now. Some missions can begin with partial bases, allotted or irreplaceable troops, or no base at all.
- Level 2 should lean toward a player-built resource race with strategic base-building terrain such as cliffs, water, chokepoints, useful Defense Tower wall positions, and likely first Barracks Guardian upgrade if the mission can carry it.
- Vehicle Bay / Rover production should enter the first five-level demo, likely in Mission 3 or Mission 4.
- Med Hall, Logistics / Repair Pad, and Artillery Battery may enter the first demo one or two at most, and only when a specific mission needs that role.

## Open Vision Questions

These are intentionally unresolved:

- Exact grunt replacement cost relative to basic combat units.
- Exact Level 4 and Level 5 mission archetypes.
- Exact Vehicle Bay/Rover mission slot, currently Mission 3 or Mission 4.
- Which one or two support/siege systems, if any, earn a place in the first demo.
- Exact Level 1 enemy production speed and resource handicap.
