# Stratezone Content Roadmap

This document tracks mission archetypes, map-content ideas, and content-shape decisions that make Stratezone feel rounded without turning it into uncontrolled scope.

It should capture approved direction before implementation, but it should not pretend every idea is committed. When an archetype or content toy needs runtime support, record the smallest mechanic needed and the reason it earns a place.

## Documentation Role

- **Doc role:** Content-shape planning for missions, terrain grammar, neutral structures, commander use, and scenario flavor.
- **Owns:** approved mission archetypes, campaign/demo content sequence ideas, content entry gates, and deferred content cuts.
- **Does not own:** core product identity, engine architecture, data schemas, release packaging, or implementation acceptance checks.
- **Read when:** planning new missions, deciding whether a content idea belongs in the first demo, or checking whether a proposed feature is mission-earned.
- **Do not read for:** low-level simulation contracts or store-readiness process.

## Content Principles

- Build a campaign-like sequence of authored simulation scenarios, not a story-heavy cutscene campaign.
- Keep the first enemy human and same-tech for now, with red or alternate-color presentation.
- Keep missions fresh-scenario based: each level may introduce a new problem, but not a persistent colony layer.
- Make each mission prove one main idea and one secondary pressure at most.
- Prefer resource, power, terrain, scouting, and commander-position problems before adding new unit families.
- Support and siege systems should enter only when a mission needs them, not because the content list feels thin.
- Quick RTS restart is the first-demo failure model; persistent campaign consequences are later scope.

## First Demo Mission Archetype Direction

The current target remains a first public demo with five coherent authored missions. The exact final mission count can change later, but this sequence is the working shape for planning.

### Mission 1: First Landing

Purpose: teach and prove the core RTS loop under light pressure.

Frame:

- The expedition has just landed or crash-landed in a strange new area.
- The player must secure the landing zone, survey the immediate area, neutralize any local threat, and buy time while communications with the home planet, home base, or command network are restored.
- The story framing should stay standard and practical. The mission's job is to teach Stratezone's base/power/resource/combat grammar, not to carry a heavy plot.

Gameplay shape:

- deployed Colony Hub and fragile Commander
- one Grunt, one Guardian, one Rover, and one Commander as scenario-start assets
- player trains Grunt, Cadet, and Rifleman only
- build power, pylons, Barracks, Extractor/Refinery, and defenses
- scout black fog, secure wells, cut weak enemy infrastructure, and destroy enemy forces
- Commander death remains a loss condition

Content boundary:

- no Med Hall, Logistics / Repair Pad, Artillery Battery, or Vehicle Bay requirement
- no heavy story cutscenes
- same-tech human enemy is acceptable even if represented with placeholder colors

### Mission 2: Resource Race and Chokepoints

Purpose: prove strategic base-building through terrain and scarce wells.

Frame:

- Shortly after First Landing, or weeks later, supplies are running short.
- New resource wells are detected nearby while the expedition is still cut off from home base or home planet support.
- The enemy is competing for the same wells with similar technology.

Gameplay shape:

- limited resource wells create an expansion race
- cliffs, water, ridges, or other terrain blockers create readable chokepoints
- Defense Tower walls matter because of map shape
- enemy power and extractor routes are useful strike targets
- player chooses between expanding, walling, repairing, or attacking

Content boundary:

- same-tech enemy only, reskinned red or alternate color
- no new tech layer unless the terrain/resource proof genuinely needs it
- Med Hall, Logistics / Repair Pad, and Artillery stay out unless the mission cannot work without one

### Mission 3: Armory Unlock and Anti-Armor

Purpose: prove the first deliberate tech unlock.

Frame:

- After stabilizing the outpost and securing new resources, the expedition can bring a new production module online.
- The Armory Annex becomes the first clear step from survival setup into battlefield specialization.

Gameplay shape:

- Armory Annex is a physical powered add-on adjacent to Barracks
- Guardian access is earned through mission setup, construction, restoration, or protection
- enemy armor, hardened defenses, or defense anchors justify Guardian use
- power disruption can disable the unlock path
- Guardian must remain a specialist, not a better general-purpose Rifleman

Content boundary:

- prove Armory/Guardian before broadening into multiple unlock systems
- Vehicle Bay/Rover production should stay later unless this mission's proof target changes
- at most one support/siege system may appear if the mission proves a hard need

### Commander Field Operation

Purpose: use the Commander as a real troop without turning every mission into an escort problem.

Frame:

- Commander presence can represent battlefield authorization, coordination, or command-link stability.
- Barracks, Vehicle Bay, or other production hubs may require the Commander at base or inside a command radius in some missions, but this should be used sparingly.

Gameplay shape:

- Commander remains controllable, vulnerable, and mission-relevant
- a mission may ask the Commander to inspect, activate, secure, or coordinate one important objective
- if the Commander leaves base, some production or command function can pause or degrade
- the player should usually be able to keep the Commander protected unless the mission is explicitly about a commander field task

Content boundary:

- do not make repeated commander errands a campaign crutch
- do not turn Commander into a superhero or universal ability button
- avoid stacking this with convoy/transport pressure unless the mission is specifically built around that stress

### Transport, Convoy, or Extraction

Purpose: add movement-objective variety without repeating the commander-field mission.

Frame:

- A transport needs to be escorted, intercepted, destroyed, or used for extraction.
- The mission can end with extraction after destroying an enemy transport or clearing a route.

Gameplay shape:

- temporary defenses and route control matter
- enemy pressure should be readable before it hits the transport
- the player may need to destroy an enemy transport as the offensive objective instead of only protecting a friendly one
- extraction should feel like a tactical closeout, not a long escort slog

Content boundary:

- avoid making this feel like a commander escort with different art
- pathing, targeting, and UI clarity must be strong before this becomes a first-demo requirement
- likely better after the Level 2 resource/chokepoint grammar is proven

### Siege Breaker

Purpose: prove that Stratezone is not only about direct infantry blobs.

Frame:

- The enemy has a fortified base or protected infrastructure that cannot be efficiently cracked by basic infantry alone.
- The player needs power sabotage, Rocket Towers, a forward position, or a siege tool to break the position.

Gameplay shape:

- scouting reveals why direct attack is inefficient
- infrastructure strikes soften the defense
- Artillery Battery can earn a place here if the mission specifically needs long-range base pressure
- Rocket Towers, Guardian support, or captured forward infrastructure can provide alternative routes

Content boundary:

- do not add Artillery Battery until a mission actually needs long-range siege
- keep the siege answer readable and limited; one new answer is enough
- preserve quick restart and avoid a long slow grind

### Broken Outpost Recovery

Purpose: make repair, stabilization, and rescue feel like core Stratezone verbs.

Frame:

- The player starts after damage, a raid, storm, failed landing, or sabotage.
- The immediate objective is to restore power, repair essential structures, find missing troops, and counterattack.

Gameplay shape:

- low power, damaged buildings, limited materials, and missing Grunts create early triage
- the player may discover stranded or captured friendly troops hidden in fog
- hidden friendly groups should not be preemptively killed by enemy AI before discovery
- discovered troops can become reinforcements or optional rescue objectives

Content boundary:

- hidden rescue groups need explicit mission rules: unrevealed friendly units must be reserved, hidden, or protected until the player discovers them
- avoid turning the mission into a frustrating scavenger hunt
- keep the recovery arc short enough that the RTS pace remains active

### Two-Front Defense

Purpose: create higher-stress tactical defense after controls and readability are stronger.

Frame:

- The player must protect two separated assets, such as base plus refinery, base plus commander, or base plus transport route.

Gameplay shape:

- threat timing is authored and readable
- the player has enough warning and tools to make an informed choice
- walling, power, scouting, and mobile troops all matter

Content boundary:

- this should be later or carefully authored because it can become too intense quickly
- do not use it before selection, alerts, pathing, and failure messaging are polished enough

## Current Recommendation

Working first-demo sequence:

1. First Landing
2. Resource Race and Chokepoints
3. Armory Unlock and Anti-Armor
4. Broken Outpost Recovery or Commander Field Operation
5. Siege Breaker, Transport/Extraction, or carefully authored Two-Front Defense

The fourth and fifth mission slots remain open. The current preference is to use those slots to round out Stratezone's identity without adding too many new systems: recovery/rescue, commander use, transport/extraction, or siege should each earn its place through one clean mission proof.

## Next Content Planning Topic

The next planning section should be terrain grammar: cliffs, water, chokepoints, roads, forest/vision blockers, bridges/fords, high ground/ridges, and how those map features support Defense Tower walls, resource races, and scouting.
