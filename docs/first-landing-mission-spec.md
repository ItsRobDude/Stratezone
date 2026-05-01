# First Landing Mission Spec

This document defines the first playable Stratezone mission target.

First Landing is a small, ugly, playable RTS proof. It should make the player say, "this feels like a real RTS" within the first 10 minutes, even with placeholder shapes.

## Documentation Role

- **Doc role:** Active source of truth for Level 1 mission behavior.
- **Owns:** Level 1 pacing, start state, objectives, failure conditions, map beats, enemy behavior, and first-pass unit/building availability.
- **Does not own:** engine layout, final art, final balance, long-term campaign structure, or detailed implementation architecture.
- **Read when:** building or testing the first playable mission.
- **Do not read for:** Godot project setup or broad product scope.

## Mission Summary

- **Internal name:** First Landing
- **Target length:** 5-10 minutes
- **Pacing:** tame but active
- **Mode:** top-down mission RTS
- **Visual target:** bright, readable meadows or fields with light forest
- **Art requirement:** placeholder shapes are acceptable
- **Story requirement:** no cutscenes for this pass
- **Primary goal:** destroy all enemies on the map
- **Primary fail condition:** commander dies

The mission begins after landing. The player is already on the ground and has time to get bearings, but should not sit in a long safe tutorial.

## Player Start

The player starts with:

- one Colony Hub or deployed starting base
- one controllable Commander at the base
- enough starting resources to begin a small build order
- initial vision around the base
- black unexplored fog beyond the starting area

The player should quickly learn:

- the commander is fragile and important
- power matters immediately
- workers and construction matter
- scouting reveals the enemy edge
- the mission is won by destroying the enemy

## Commander Rules

The Commander is:

- controllable
- fragile
- armed only with a pistol
- a mission fail-condition unit
- best kept near the home base by default
- a valid enemy target

Enemies may target the Commander when they can reach or see him. The Commander should die fast if exposed.

The Commander should not provide buffs in Level 1.

## Economy Rules

Level 1 uses one limited resource.

Resource wells:

- are scarce
- trickle income while an Extractor/Refinery is active
- can deplete
- can be contested by the enemy

Level 1 should include:

- one safer starting well or early-access well for the player
- one contested central well that both sides care about

Extractors/Refineries should have normal building durability, with enough durability that the player can react before losing one instantly.

## Build and Production Rules

Buildings are constructed by workers.

Buildings need power immediately to function. Power affects both:

- where the player can reasonably expand
- whether powered buildings remain active

Unit production rule for Level 1:

- Colony Hub is the spawn location.
- Barracks controls what can be trained, the allowed troop count, and level-based unlocks.
- Barracks add-ons are the preferred unlock model: Armory Annex for Guardian/explosive tech and Vehicle Bay for Rover/heavy-armor capacity.
- Current Level 1 mission data exposes only Worker and Rifleman training. Guardian and Rover records may exist and mission-start Rovers may be present, but advanced Barracks training stays hidden until a later mission or explicit mission setup enables it.

Workers are expensive, recruitable, non-combat units. They require player command for construction and repair.

## Available Buildings

Level 1 should use:

- Colony Hub
- Barracks
- Power Plant
- Pylon
- Extractor/Refinery
- Defense Tower
- Armory Annex or Vehicle Bay only if the first add-on pass fits the pacing
- Gun Tower, if the first armed-tower pass is ready
- Rocket Tower, only if explosive friendly-fire behavior is ready

Any tower-class building can act as a wall anchor if the system supports it.

Tower walls should be an investment. In Level 1, the central choke should be blockable with Defense Towers if the player saves for them. If armed towers are included, they should be upgraded in place from existing Defense Towers so the wall line can stay active during the investment.

## Available Units

Level 1 player units:

- Worker
- Rifleman
- Guardian as a scenario/start-only support unit if authored into the mission
- Rover as a scenario/start-only scouting unit if authored into the mission
- Commander

Level 1 tank rule:

- tanks are not normally trainable in Level 1
- destroying either player's or enemy's Colony Hub reveals a tank from that hub
- the tank reveal does not add, remove, or replace any win/loss condition

First-pass unit intent:

- Riflemen die very quickly and work best with support, numbers, or harassment timing.
- Guardians are beefier than Riflemen but deal slightly less damage; if Barracks add-ons are active, they require a powered Armory Annex.
- Rovers scout and cannot shoot, but can run over and instantly kill exposed basic infantry; if Barracks add-ons are active, they require a powered Vehicle Bay.
- Tanks survive more punishment than infantry but are not part of normal Level 1 production.
- Tanks should shrug off Rifleman fire; the player should need Guardian energy fire, explosive towers, another tank, or infrastructure play to answer heavy armor cleanly.

Explosive friendly fire exists. Normal gunfire does not cause friendly fire.

## Enemy Setup

The first enemy is a private military force.

The enemy should:

- use the same broad technology and building types as the player
- appear with different color, skin, or faction styling
- be visible near the edge of fog early
- be intentionally simple in Level 1
- still demonstrate basic RTS tactics
- rebuild destroyed structures when it has resources
- spend limited resources to rebuild and produce
- attack slowly with small committed groups of 1-3 units instead of sending the whole base at once
- keep some defenders near its base so scouting finds a defended position, not an empty spawn point
- reveal intent through visible units, combat, scouting, and power/building state rather than omniscient HUD messages

Enemy target priority for Level 1:

1. closest visible troop, including the Commander if he is exposed
2. Refinery/Extractor
3. Power Plant or Pylon
4. Barracks
5. Colony Hub
6. Commander when reachable or specifically directed by a mission attack

The enemy should compete for the central well, but Level 1 should run slower than the normal baseline so the player can understand what is happening.

Player alerts should feel like classic RTS command warnings: direct and frequent for player-known events such as enemy spotted, base under attack, extractor under attack, unit under attack, power offline, construction complete, and training complete. Hidden enemy production, rebuilding, or attack planning should not be announced unless a future radar/scanner system explicitly earns that information.

## Map Beats

Level 1 should be small.

Required map elements:

- bright readable starting meadow/field area
- light forest or terrain texture variation for boundaries
- black unexplored fog outside starting vision
- enemy visible at the fog edge or shortly after scouting
- central choke between player and enemy
- contested resource well near or beyond the choke
- enemy pylon weak point that can cut power to an enemy tower

The pylon weak point should teach infrastructure strikes. Destroying it should create a clear tactical opening for players who do not want to brute force the enemy defense.

## Fog of War

Fog uses black unexplored areas.

When explored:

- terrain becomes visible
- the black fog is removed
- enemies and structures in explored terrain remain visible in real time
- enemies that move into never-explored black fog are hidden
- no last-known ghost markers are required

## Win and Loss

Win condition:

- all enemy forces and required enemy structures are destroyed

Loss condition:

- Commander dies

Destroying the player's Colony Hub can reveal a tank but does not itself end the mission for this pass.

The player should clearly understand whether they won, lost, or are still missing an enemy target.

## Placeholder Balance

These values are tunable placeholders for scaffolding and early tests:

- target mission length: 5-10 minutes
- first enemy pressure: after the player has had time to start power and production
- contested well pressure: visible early, not instantly fatal
- infantry durability: meatgrinder-low; Rifleman health starts around 45
- worker cost: expensive relative to Rifleman
- tower cost: high enough to make wall placement a choice
- Barracks add-on cost: meaningful enough to create a power/placement decision, but not enough to stall Level 1
- Colony Hub siege ratio: keep 1200 health and 0.25 ballistic resistance so Riflemen alone are slow base-crackers
- Level 1 enemy economy: slower than normal

Do not treat these as final balance.
