# Stratezone System Contracts

This document defines first-pass behavior contracts for the prototype systems.

The contracts are implementation-ready enough to prevent guessing, but intentionally not final balance. Placeholder numbers are tunable and should be replaced by content data once the first scaffold exists.

## Documentation Role

- **Doc role:** Active source of truth for first-pass system behavior.
- **Owns:** system responsibilities, boundaries, inputs, outputs, and prototype acceptance checks.
- **Does not own:** final code layout, final stats, final art, or mission-specific pacing.
- **Read when:** implementing simulation systems, writing tests, or checking whether a scene script owns too much gameplay logic.
- **Do not read for:** player promise, broad roadmap, or Godot setup.

## Global Rule

Simulation owns game truth. Presentation reads it.

Simulation owns resources, power, workers, jobs, combat outcomes, fog truth, mission objectives, enemy production, and saveable state. Presentation owns sprites, animation, selection visuals, particles, camera, audio, and UI rendering.

Scene scripts may submit commands and display results. They should not secretly become the only place where rules live.

## Power System

Owns:

- powered territory
- building power requirements
- Power Plant radius
- Pylon links
- power shutoff state
- build-radius support where relevant

Prototype behavior:

- buildings that require power shut off when unpowered
- Power Plants create local powered area
- Pylons extend or link power over longer distances
- power affects both placement strategy and building function
- power state must be visible to UI/debug surfaces

Tunable placeholders:

- Power Plant local radius: small base cluster
- Pylon link range: long enough to bridge expansion gaps
- power update cadence: immediate or near-immediate after construction/destruction

Acceptance checks:

- destroying or disabling a Pylon can unpower downstream buildings
- an unpowered Barracks cannot provide production/unlock function
- an unpowered Barracks add-on stops contributing its unlock or capacity effect
- an unpowered tower wall anchor drops its wall segment

## Construction and Placement

Owns:

- build legality
- hidden footprint and spacing constraints
- Barracks add-on adjacency legality
- in-place tower upgrade legality
- worker construction jobs
- repair jobs
- powered expansion constraints

Prototype behavior:

- placement should feel freeform, with no visible grid
- structures still need footprints and buffer spacing
- workers construct buildings by player command
- workers construct Barracks add-ons adjacent to the Barracks by player command
- workers can upgrade a Defense Tower into an armed tower variant in place when the player pays the cost and requirements
- workers can repair during danger if the player commands them
- construction should fail clearly if unaffordable, blocked, or outside legal support

Tunable placeholders:

- building buffer: enough to prevent over-cramming on small maps
- build time: short enough for a 5-10 minute Level 1
- repair rate: useful but not combat-dominating

Acceptance checks:

- a worker can start and complete a build job
- a Barracks add-on must be adjacent to a valid Barracks
- a Defense Tower can upgrade in place without losing its wall-anchor identity during the upgrade
- blocked placement is rejected
- an unpowered or unsupported build decision is rejected or clearly marked inactive according to the final implementation choice

## Resource System

Owns:

- one limited resource for the prototype
- resource well capacity
- extraction rate
- storage or available balance
- spending on buildings, workers, and troops

Prototype behavior:

- wells are scarce and can deplete
- active Extractor/Refinery buildings trickle income
- destroyed or unpowered extractors stop income
- the enemy spends from limited resources too
- Level 1 includes one contested central well

Tunable placeholders:

- starting resources: enough for first power and production
- well capacity: enough to matter in a short mission
- trickle rate: readable, not bursty

Acceptance checks:

- income stops when a well depletes
- income stops when the extractor is destroyed or unpowered
- spending cannot go below zero

## Worker System

Owns:

- worker units
- build jobs
- repair jobs
- flee behavior
- worker loss consequences

Prototype behavior:

- workers are expensive recruitable units
- workers have no combat utility
- workers require player command for construction and repair
- workers flee attackers rather than fight
- losing all workers is recoverable if the player can afford replacements
- workers spawn from the Colony Hub when Barracks rules allow training

Tunable placeholders:

- worker cost: expensive relative to Rifleman
- worker health: low to medium
- flee trigger: nearby hostile or damage taken

Acceptance checks:

- a worker does not attack enemies
- a threatened worker flees
- a worker can build and repair when commanded
- a worker can build a Barracks add-on or tower upgrade when commanded and requirements are met

## Barracks Add-on System

Owns:

- Barracks adjacency requirements
- powered add-on state
- training unlock effects
- troop capacity effects
- level-specific add-on availability

Prototype behavior:

- Barracks upgrades are physical modules built by Workers adjacent to the Barracks
- Armory Annex unlocks Guardian training and explosive-weapon tech where the mission allows it
- Vehicle Bay unlocks Rover training and expands heavy-armor capacity where the mission allows it
- add-ons require power; unpowered add-ons stop providing unlocks or capacity until power returns
- expanding the Barracks should create a power-network decision rather than a menu-only upgrade decision

Tunable placeholders:

- add-on footprint: small enough to fit near a starter Barracks but large enough to create placement pressure
- add-on cost: meaningful but not mission-stalling
- heavy-armor capacity delta: mission-specific

Acceptance checks:

- a powered Armory Annex unlocks its allowed advanced training
- a powered Vehicle Bay unlocks its allowed vehicle/heavy-armor capacity effect
- unpowering an add-on removes its effect without destroying trained units
- destroying an add-on removes its effect and makes the Barracks visibly degraded in UI/debug output

## Combat System

Owns:

- attack legality
- damage
- cooldowns
- range
- death
- target selection
- friendly fire rules

Prototype behavior:

- basic infantry follow the meatgrinder rule: they die quickly when caught out of position
- tanks are highly resistant to ballistic fire and force energy, explosive, crush, or other anti-armor answers
- Rifleman is the basic combat unit
- Guardian is beefier than Rifleman but deals slightly less damage
- Rover scouts and cannot shoot, but can run over enemy infantry
- Commander is fragile, controllable, pistol-only, and mission-critical
- buildings preserve siege pacing: basic infantry can harass weak infrastructure, but base-cracking should require explosives, heavy units, or a major numbers advantage
- explosive damage is strong against structures
- normal gunfire does not cause friendly fire
- explosive damage can cause friendly fire

Tunable placeholders:

- Rifleman health: about 45, enough for roughly 2-3 seconds of Rifleman-vs-Rifleman time-to-kill
- Guardian health: medium
- Guardian damage: slightly below Rifleman
- Commander health: low
- tank health: high
- Tank ballistic resistance: about 0.8 so Riflemen are nearly useless against heavy armor
- building explosive resistance: negative, while building ballistic resistance stays high enough to protect the siege ratio
- Colony Hub siege ratio: keep 1200 health and 0.25 ballistic resistance until playtests prove the base cracks too slowly or too quickly

Acceptance checks:

- a Rover cannot shoot
- a Rover can instantly kill basic infantry with crush damage when the crush system is included
- a small Rifleman group cannot efficiently destroy a Colony Hub without siege support
- Riflemen perform poorly against Tanks because of high ballistic resistance
- explosive damage can harm friendly units in range
- commander death triggers mission loss

## Support and Siege Infrastructure

Owns:

- infantry healing rules
- vehicle repair-pad rules
- resource spending for active healing or repair
- artillery target legality
- artillery minimum range
- artillery friendly fire

Prototype behavior:

- Med Hall heals infantry within a radius, requires power, and spends resources only while actually healing.
- Logistics / Repair Pad repairs mechanical units parked on it, requires power, and spends resources only while actually repairing.
- Repair Pad should reduce the need to send fragile Workers to the front line to fix vehicles.
- Artillery Battery is fragile, expensive, powered, static, and cannot target enemies inside its minimum range.
- Artillery Battery is a siege tool for cracking bases and forcing assaults, not a self-defending turret.
- Artillery explosive damage can cause friendly fire.

Tunable placeholders:

- Med Hall heal radius: 5
- Med Hall heal rate: 4 health per second
- Repair Pad repair radius: 2
- Repair Pad repair rate: 5 health per second
- Artillery range: 16
- Artillery minimum range: 5
- Artillery damage: 95 explosive with area radius 3

Acceptance checks:

- an unpowered Med Hall or Repair Pad provides no healing or repair
- Med Hall spends resources only when healing damaged infantry
- Repair Pad repairs vehicles only when they are parked on or near the pad
- Artillery Battery cannot fire at targets inside its minimum range
- Artillery Battery can damage buildings at long range and can harm friendly units in its blast radius

## Tower and Wall System

Owns:

- tower anchor compatibility
- wall link creation
- wall segment blocking
- wall removal when anchors are destroyed, disabled, or unpowered

Prototype behavior:

- Defense Towers create wall links with compatible nearby towers
- Gun Towers and Rocket Towers keep Defense Tower wall-anchor behavior
- Gun Towers and Rocket Towers are normally created by upgrading an existing Defense Tower in place
- upgrading a Defense Tower preserves its wall-anchor identity while the upgrade is underway, unless the tower is destroyed or unpowered
- tower walls block enemy pathing
- tower placement should be expensive enough to matter
- Level 1 central choke can be blocked by tower-wall play

Tunable placeholders:

- wall link range: short enough to require deliberate placement
- tower cost: meaningful investment
- wall update cadence: immediate or near-immediate after anchor state changes

Acceptance checks:

- two compatible powered towers create a blocking segment
- a Defense Tower can upgrade into a Gun Tower or Rocket Tower without opening a powered wall link
- destroying or unpowering either tower removes the segment
- enemies path around or attack through the opened route after a wall drops

## Fog and Scouting

Owns:

- unexplored areas
- explored terrain
- current vision
- scout reveal state

Prototype behavior:

- unexplored areas are black
- explored terrain stays revealed
- enemies are only shown when currently visible
- no last-known enemy ghost markers are required
- Rover is the primary Level 1 scouting unit

Acceptance checks:

- black fog is removed after scouting
- terrain remains visible after exploration
- an enemy that leaves vision is no longer tracked

## Mission Objective System

Owns:

- active objectives
- win/loss checks
- mission-critical units
- mission state display

Prototype behavior:

- Level 1 win condition is destroy all enemies
- Level 1 loss condition is commander death
- destroying either Colony Hub reveals a tank but does not change win/loss conditions by itself
- objective state should be readable in HUD/debug output

Acceptance checks:

- commander death loses the mission
- destroying all required enemy targets wins the mission
- destroying a Colony Hub can reveal a tank without ending the mission

## Enemy Production and AI

Owns:

- enemy rebuild decisions
- enemy production spending
- simple target priorities
- basic pressure timing
- competition for resource wells

Prototype behavior:

- first enemy faction is private military
- enemy uses player-like tech with different color/skin
- enemy resources are limited
- enemy rebuilds only when it can afford to
- Level 1 enemy is slower than normal baseline
- enemy competes for the central well

Level 1 target priority:

1. closest visible troop, including the Commander if he is exposed
2. Refinery/Extractor
3. Power Plant or Pylon
4. Barracks
5. Colony Hub
6. Commander when reachable or mission-directed

Acceptance checks:

- enemy cannot rebuild for free
- enemy can pressure the contested well
- destroying an enemy pylon can disable an enemy tower path
